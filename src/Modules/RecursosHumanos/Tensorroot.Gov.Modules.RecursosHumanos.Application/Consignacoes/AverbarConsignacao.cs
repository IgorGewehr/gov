using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>
/// AVERBA um contrato de consignacao para um servidor (Lei 14.131/2021). A averbacao so e aceita se a
/// parcela couber na MARGEM DISPONIVEL do balde da rubrica na competencia informada — a margem e calculada
/// a partir da base consignavel APURADA DA FOLHA e do que ja esta comprometido pelos contratos averbados
/// (invariante critica no agregado <see cref="ContratoConsignacao"/>).
/// </summary>
/// <param name="ServidorId">Servidor consignante.</param>
/// <param name="ConsignatariaId">Consignataria destinataria.</param>
/// <param name="CodigoRubrica">Codigo da rubrica consignavel (define grupo de margem/categoria).</param>
/// <param name="NumeroContratoExterno">Numero do contrato no sistema da consignataria (opcional).</param>
/// <param name="ValorParcela">Valor mensal da parcela (&gt; 0).</param>
/// <param name="QuantidadeParcelas">Quantidade de parcelas (&gt; 0).</param>
/// <param name="DataAverbacao">Data da averbacao (define a competencia base da margem).</param>
public sealed record AverbarConsignacaoCommand(
    Guid ServidorId,
    Guid ConsignatariaId,
    string CodigoRubrica,
    string? NumeroContratoExterno,
    decimal ValorParcela,
    int QuantidadeParcelas,
    DateOnly DataAverbacao) : ICommand<Guid>;

/// <summary>Regras de validacao da averbacao de consignacao.</summary>
public sealed class AverbarConsignacaoValidator : AbstractValidator<AverbarConsignacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AverbarConsignacaoValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.ConsignatariaId).NotEmpty().WithMessage("Consignataria e obrigatoria.");
        RuleFor(c => c.CodigoRubrica).NotEmpty().MaximumLength(30).WithMessage("Rubrica consignavel e obrigatoria (max. 30).");
        RuleFor(c => c.ValorParcela).GreaterThan(0m).WithMessage("Valor da parcela deve ser maior que zero.");
        RuleFor(c => c.QuantidadeParcelas).GreaterThan(0).WithMessage("Quantidade de parcelas deve ser maior que zero.");
    }
}

/// <summary>Handler da averbacao de consignacao (valida a margem do balde na competencia).</summary>
public sealed class AverbarConsignacaoHandler(
    IServidorRepository servidores,
    IConsignatariaRepository consignatarias,
    IRubricaConsignavelRepository rubricas,
    IContratoConsignacaoRepository contratos,
    CalculadoraMargemConsignavel calculadoraMargem,
    ITenantContext tenant,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AverbarConsignacaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AverbarConsignacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidorId = new ServidorId(request.ServidorId);
        _ = await servidores.ObterPorIdAsync(servidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        var consignataria = await consignatarias.ObterPorIdAsync(new ConsignatariaId(request.ConsignatariaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Consignataria nao encontrada.");

        var rubrica = await rubricas.ObterAtivaPorCodigoAsync(request.CodigoRubrica, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rubrica consignavel inexistente ou inativa.");

        // A margem da competencia da averbacao: base apurada da folha + percentuais vigentes - comprometido
        // pelos contratos averbados (reserva legal por balde). O agregado valida a invariante (parcela <=
        // disponivel do balde) e nasce com snapshot de grupo/categoria da rubrica.
        var competencia = Competencia.De(request.DataAverbacao.Year, request.DataAverbacao.Month);
        var margem = await calculadoraMargem.CalcularAsync(request.ServidorId, competencia, cancellationToken).ConfigureAwait(false);

        var contrato = ContratoConsignacao.Averbar(
            tenant.TenantId,
            servidorId,
            consignataria.Id,
            consignataria.EstaAtiva,
            rubrica,
            request.NumeroContratoExterno,
            request.ValorParcela,
            request.QuantidadeParcelas,
            request.DataAverbacao,
            margem);
        contratos.Adicionar(contrato);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return contrato.Id.Value;
    }
}

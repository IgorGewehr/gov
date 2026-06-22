using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto;

/// <summary>
/// Registra uma marcacao (batida) de ponto de um servidor no AFD (imutavel), atribuindo o proximo NSR
/// sequencial do tenant (REP). Vedada a marcacao automatica — a hora deve refletir a batida real.
/// </summary>
/// <param name="ServidorId">Servidor da marcacao.</param>
/// <param name="DataHora">Data/hora exata da batida.</param>
/// <param name="Sentido">Sentido (1=Entrada, 2=Saida).</param>
/// <param name="Origem">Origem (1=REP-C, 2=REP-A, 3=REP-P); quando nulo, usa o padrao do tenant.</param>
public sealed record RegistrarMarcacaoCommand(
    Guid ServidorId,
    DateTimeOffset DataHora,
    int Sentido,
    int? Origem) : ICommand<long>;

/// <summary>Regras de validacao da marcacao de ponto.</summary>
public sealed class RegistrarMarcacaoValidator : AbstractValidator<RegistrarMarcacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarMarcacaoValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        // LICAO: validar enum por Enum.IsDefined sobre o tipo, nunca IsInEnum sobre o int do contrato.
        RuleFor(c => c.Sentido).Must(s => Enum.IsDefined(typeof(SentidoMarcacao), s))
            .WithMessage("Sentido invalido (1=Entrada, 2=Saida).");
        RuleFor(c => c.Origem!.Value).Must(o => Enum.IsDefined(typeof(TipoRep), o))
            .When(c => c.Origem.HasValue)
            .WithMessage("Origem invalida (1=REP-C, 2=REP-A, 3=REP-P).");
    }
}

/// <summary>Handler da marcacao de ponto: calcula o proximo NSR e grava o registro imutavel.</summary>
public sealed class RegistrarMarcacaoHandler(
    IMarcacaoPontoRepository marcacoes,
    IServidorPontoConsulta servidores,
    IParametrosPontoProvider parametros,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarMarcacaoCommand, long>
{
    /// <inheritdoc />
    public async Task<long> Handle(RegistrarMarcacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dados = await servidores.ObterAsync(request.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);
        var origem = (TipoRep)(request.Origem ?? config.OrigemPadrao);

        // NSR sequencial e sem lacunas por tenant: proximo = ultimo persistido + 1 (ou 1, se nao houver).
        var ultimo = await marcacoes.ObterUltimoNsrAsync(cancellationToken).ConfigureAwait(false);
        var nsr = ultimo is { } valor ? Nsr.De(valor).Proximo() : Nsr.Primeiro();

        var marcacao = MarcacaoPonto.Registrar(
            tenantContext.TenantId,
            request.ServidorId,
            dados.Cpf,
            nsr,
            request.DataHora,
            (SentidoMarcacao)request.Sentido,
            origem);

        marcacoes.Adicionar(marcacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return nsr.Valor;
    }
}

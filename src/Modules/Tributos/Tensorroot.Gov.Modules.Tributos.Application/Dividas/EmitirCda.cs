using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>Dados da CDA emitida (read model) — os requisitos legais da LEF art. 2º §5º.</summary>
/// <param name="Numero">Número da CDA.</param>
/// <param name="NomeDevedor">Nome do devedor (inc. I).</param>
/// <param name="ValorOriginario">Valor originário (inc. II).</param>
/// <param name="OrigemNatureza">Origem e natureza (inc. III).</param>
/// <param name="FundamentoLegal">Fundamento legal (inc. III).</param>
/// <param name="DataInscricao">Data da inscrição (inc. V).</param>
/// <param name="NumeroInscricao">Número da inscrição (inc. V).</param>
public sealed record CdaEmitidaDto(
    string Numero,
    string NomeDevedor,
    decimal ValorOriginario,
    string OrigemNatureza,
    string FundamentoLegal,
    DateOnly DataInscricao,
    long NumeroInscricao);

/// <summary>
/// Emite a Certidão de Dívida Ativa (CDA) de um título inscrito, validando os requisitos legais
/// obrigatórios (LEF art. 2º §5º I–VI / CTN art. 202). RECUSA (lança) se faltar requisito → nulidade
/// evitada na origem. O nome do devedor é resolvido do contribuinte (inc. I).
/// </summary>
/// <param name="DividaAtivaId">Dívida ativa.</param>
/// <param name="NumeroCda">Número da CDA.</param>
/// <param name="DataBaseEncargos">Data-base para descrever a forma de cálculo dos encargos.</param>
/// <param name="DomicilioDevedor">Domicílio do devedor (inc. I, opcional).</param>
/// <param name="CoResponsaveis">Co-responsáveis (inc. I, opcional).</param>
/// <param name="ProcessoAdministrativo">Nº do processo administrativo (inc. VI, opcional).</param>
public sealed record EmitirCdaCommand(
    Guid DividaAtivaId,
    string NumeroCda,
    DateOnly DataBaseEncargos,
    string? DomicilioDevedor = null,
    string? CoResponsaveis = null,
    string? ProcessoAdministrativo = null) : ICommand<CdaEmitidaDto>;

/// <summary>Regras de validação da emissão de CDA.</summary>
public sealed class EmitirCdaValidator : AbstractValidator<EmitirCdaCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirCdaValidator()
    {
        RuleFor(comando => comando.DividaAtivaId).NotEmpty();
        RuleFor(comando => comando.NumeroCda).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da emissão de CDA.</summary>
public sealed class EmitirCdaHandler(
    IDividaAtivaRepository dividas,
    IContribuinteRepository contribuintes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EmitirCdaCommand, CdaEmitidaDto>
{
    /// <inheritdoc />
    public async Task<CdaEmitidaDto> Handle(EmitirCdaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var divida = await dividas.ObterPorIdAsync(new DividaAtivaId(request.DividaAtivaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dívida ativa não encontrada.");

        var contribuinte = await contribuintes.ObterPorIdAsync(divida.ContribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte (devedor) da dívida não encontrado.");

        // O domínio (CertidaoDividaAtiva.Emitir) RECUSA se faltar requisito legal — nulidade evitada.
        var certidao = divida.EmitirCda(
            request.NumeroCda,
            contribuinte.Nome,
            request.DomicilioDevedor,
            request.CoResponsaveis,
            request.DataBaseEncargos,
            request.ProcessoAdministrativo);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CdaEmitidaDto(
            certidao.Numero,
            certidao.NomeDevedor,
            certidao.ValorOriginario.Valor,
            certidao.OrigemNatureza,
            certidao.FundamentoLegal,
            certidao.DataInscricao,
            certidao.NumeroInscricao);
    }
}

using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>
/// Inscreve um lançamento VENCIDO e não pago (qualquer espécie) em Dívida Ativa, gerando o título com os
/// dados do crédito (origem/natureza, fundamento legal, valor originário) e a regra de encargos
/// PARAMETRIZÁVEL por tenant (multa/juros/correção — lei municipal). A constituição definitiva (início da
/// prescrição) é informada; se ausente, usa o vencimento do lançamento — NUNCA o relógio (CLAUDE.md §16).
/// </summary>
/// <param name="LancamentoId">Lançamento de origem (vencido e em aberto).</param>
/// <param name="FundamentoLegal">Fundamento legal da dívida (inc. III da CDA — lei municipal).</param>
/// <param name="MultaMoraPercentual">Multa de mora (% sobre o originário) — lei municipal.</param>
/// <param name="JurosMoraPercentualMensal">Juros de mora (% a.m.) — lei municipal.</param>
/// <param name="CorrecaoPercentualMensal">Correção monetária (% a.m.) — lei municipal.</param>
/// <param name="FundamentoEncargos">Fundamento legal dos encargos (CTM/REFIS).</param>
/// <param name="DataConstituicaoDefinitiva">Constituição definitiva (início da prescrição). Default: vencimento.</param>
/// <param name="DataInscricao">Data da inscrição. Default: a constituição definitiva efetiva.</param>
/// <param name="AnosPrescricao">Prazo prescricional (anos) — default 5 (CTN art. 174).</param>
public sealed record InscreverEmDividaAtivaCommand(
    Guid LancamentoId,
    string FundamentoLegal,
    decimal MultaMoraPercentual,
    decimal JurosMoraPercentualMensal,
    decimal CorrecaoPercentualMensal,
    string FundamentoEncargos,
    DateOnly? DataConstituicaoDefinitiva = null,
    DateOnly? DataInscricao = null,
    int AnosPrescricao = DividaAtiva.AnosPrescricao) : ICommand<Guid>;

/// <summary>Regras de validação da inscrição em Dívida Ativa.</summary>
public sealed class InscreverEmDividaAtivaValidator : AbstractValidator<InscreverEmDividaAtivaCommand>
{
    /// <summary>Define as regras.</summary>
    public InscreverEmDividaAtivaValidator()
    {
        RuleFor(c => c.LancamentoId).NotEmpty();
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
        RuleFor(c => c.FundamentoEncargos).NotEmpty().MaximumLength(300);
        RuleFor(c => c.MultaMoraPercentual).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.JurosMoraPercentualMensal).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.CorrecaoPercentualMensal).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.AnosPrescricao).GreaterThanOrEqualTo(1);
    }
}

/// <summary>Handler da inscrição em Dívida Ativa.</summary>
public sealed class InscreverEmDividaAtivaHandler(
    ILancamentoRepository lancamentos,
    IDividaAtivaRepository dividas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<InscreverEmDividaAtivaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(InscreverEmDividaAtivaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lancamento = await lancamentos.ObterPorIdAsync(new LancamentoId(request.LancamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Lançamento não encontrado.");

        // Marco para a transição de estado do lançamento (verificação "vencido"): HOJE no FUSO do tenant
        // (UTC-3) — perto da meia-noite o UTC já virou o dia seguinte e a comparação "vencido" erraria por
        // um dia. Usa o relógio APENAS para a transição administrativa, NÃO para a contagem de prescrição
        // (essa usa datas do fato).
        var hoje = dataHoje.Hoje();

        // Regra de domínio: só inscreve se em aberto e vencido.
        lancamento.InscreverEmDividaAtiva(hoje);

        // Reprodutibilidade (CLAUDE.md §16): a prescrição corre da constituição definitiva (data do fato).
        // Se não informada, adota-se o vencimento do crédito como marco — determinístico, sem relógio.
        var constituicaoDefinitiva = request.DataConstituicaoDefinitiva ?? lancamento.Vencimento;
        var dataInscricao = request.DataInscricao ?? constituicaoDefinitiva;

        var regraEncargos = RegraEncargosDivida.Criar(
            request.MultaMoraPercentual,
            request.JurosMoraPercentualMensal,
            request.CorrecaoPercentualMensal,
            request.FundamentoEncargos);

        var numeroInscricao = await dividas.ObterProximoNumeroInscricaoAsync(cancellationToken).ConfigureAwait(false);

        var divida = DividaAtiva.Inscrever(
            tenant.TenantId,
            lancamento.ContribuinteId,
            lancamento.Id,
            lancamento.TipoTributo,
            lancamento.ValorPrincipal,
            lancamento.Vencimento,
            constituicaoDefinitiva,
            dataInscricao,
            numeroInscricao,
            $"{lancamento.TipoTributo} — competência {lancamento.Competencia}",
            request.FundamentoLegal,
            regraEncargos,
            request.AnosPrescricao);

        dividas.Adicionar(divida);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return divida.Id.Value;
    }
}

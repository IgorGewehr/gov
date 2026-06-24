using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

namespace Tensorroot.Gov.Modules.Legislativo.Application.LimiteCamara;

/// <summary>Parcela de despesa no demonstrativo (projecao de leitura).</summary>
/// <param name="Natureza">Natureza da despesa.</param>
/// <param name="Valor">Valor realizado.</param>
/// <param name="Descricao">Descricao (opcional).</param>
public sealed record DespesaCamaraDto(string Natureza, decimal Valor, string? Descricao);

/// <summary>
/// Demonstrativo do art. 29-A (prestacao local; transmissao ao TCE-RS = M10): o teto da despesa total
/// (caput), o subteto da folha (§1), o realizado x limite e os semaforos, com a base de receita e a
/// faixa populacional aplicada.
/// </summary>
/// <param name="ApuracaoId">Identificador da apuracao.</param>
/// <param name="Exercicio">Exercicio orcamentario sob teto.</param>
/// <param name="ExercicioBaseReceita">Exercicio da receita de base (anterior).</param>
/// <param name="Situacao">Situacao do demonstrativo (Rascunho/Consolidada).</param>
/// <param name="Populacao">Populacao do municipio.</param>
/// <param name="PercentualFaixa">Percentual-limite da faixa aplicada (fracao).</param>
/// <param name="ReceitaTributaria">Receita tributaria do exercicio anterior.</param>
/// <param name="Transferencias">Transferencias do exercicio anterior.</param>
/// <param name="BaseReceita">Base total (tributaria + transferencias).</param>
/// <param name="TetoDespesaTotal">Teto da despesa total (base x percentual).</param>
/// <param name="DespesaTotalRealizada">Despesa total realizada sujeita ao teto.</param>
/// <param name="MargemTeto">Folga do teto (negativa se excede).</param>
/// <param name="UtilizacaoTeto">Fracao de utilizacao do teto.</param>
/// <param name="SemaforoTeto">Semaforo do teto.</param>
/// <param name="RepasseRecebido">Repasse/duodecimo recebido.</param>
/// <param name="SubtetoFolha">Subteto da folha (repasse x §1).</param>
/// <param name="FolhaRealizada">Folha realizada.</param>
/// <param name="MargemSubtetoFolha">Folga do subteto de folha (negativa se excede).</param>
/// <param name="UtilizacaoSubtetoFolha">Fracao de utilizacao do subteto de folha.</param>
/// <param name="SemaforoFolha">Semaforo do subteto de folha.</param>
/// <param name="InativosNoTeto">Indica se inativos/pensionistas integraram o teto (EC 109).</param>
/// <param name="Irregular">Indica estouro de qualquer limite.</param>
/// <param name="Despesas">Parcelas de despesa discriminadas.</param>
public sealed record DemonstrativoArt29ADto(
    Guid ApuracaoId,
    int Exercicio,
    int ExercicioBaseReceita,
    string Situacao,
    int Populacao,
    decimal PercentualFaixa,
    decimal ReceitaTributaria,
    decimal Transferencias,
    decimal BaseReceita,
    decimal TetoDespesaTotal,
    decimal DespesaTotalRealizada,
    decimal MargemTeto,
    decimal UtilizacaoTeto,
    string SemaforoTeto,
    decimal RepasseRecebido,
    decimal SubtetoFolha,
    decimal FolhaRealizada,
    decimal MargemSubtetoFolha,
    decimal UtilizacaoSubtetoFolha,
    string SemaforoFolha,
    bool InativosNoTeto,
    bool Irregular,
    IReadOnlyList<DespesaCamaraDto> Despesas);

/// <summary>Obtem o demonstrativo do art. 29-A de um exercicio (tenant-scoped).</summary>
/// <param name="Exercicio">Exercicio orcamentario sob teto.</param>
public sealed record ObterDemonstrativoArt29AQuery(int Exercicio) : IQuery<DemonstrativoArt29ADto?>;

/// <summary>Handler do demonstrativo do art. 29-A.</summary>
public sealed class ObterDemonstrativoArt29AHandler(IApuracaoArt29ARepository apuracoes)
    : IQueryHandler<ObterDemonstrativoArt29AQuery, DemonstrativoArt29ADto?>
{
    /// <inheritdoc />
    public async Task<DemonstrativoArt29ADto?> Handle(ObterDemonstrativoArt29AQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apuracao = await apuracoes.ObterPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        if (apuracao is null)
        {
            return null;
        }

        return Montar(apuracao);
    }

    /// <summary>Projeta uma apuracao no demonstrativo (reutilizavel por outros handlers).</summary>
    /// <param name="apuracao">Apuracao a projetar.</param>
    /// <returns>Demonstrativo do art. 29-A.</returns>
    public static DemonstrativoArt29ADto Montar(ApuracaoArt29A apuracao)
    {
        ArgumentNullException.ThrowIfNull(apuracao);

        var resultado = apuracao.Apurar();
        var despesas = apuracao.Despesas
            .Select(item => new DespesaCamaraDto(item.Natureza.ToString(), item.Valor, item.Descricao))
            .ToList();

        return new DemonstrativoArt29ADto(
            apuracao.Id.Value,
            apuracao.Exercicio,
            apuracao.BaseReceita.ExercicioReferencia,
            apuracao.Situacao.ToString(),
            apuracao.Populacao,
            resultado.PercentualFaixa,
            apuracao.BaseReceita.ReceitaTributaria,
            apuracao.BaseReceita.Transferencias,
            resultado.BaseReceita,
            resultado.TetoDespesaTotal,
            resultado.DespesaTotalRealizada,
            resultado.MargemTeto,
            resultado.UtilizacaoTeto,
            resultado.SemaforoTeto.ToString(),
            resultado.RepasseRecebido,
            resultado.SubtetoFolha,
            resultado.FolhaRealizada,
            resultado.MargemSubtetoFolha,
            resultado.UtilizacaoSubtetoFolha,
            resultado.SemaforoFolha.ToString(),
            resultado.InativosNoTeto,
            resultado.Irregular,
            despesas);
    }
}

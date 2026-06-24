namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>
/// Veredito CALCULADO da apuracao do art. 29-A (derivado, nao persistido como estado proprio): teto da
/// despesa total, subteto da folha sobre o repasse, valores realizados e os semaforos. Imutavel.
/// </summary>
/// <param name="PercentualFaixa">Percentual-limite da faixa populacional aplicada (fracao; ex.: 0,07).</param>
/// <param name="BaseReceita">Base de calculo (receita tributaria + transferencias do exercicio anterior).</param>
/// <param name="TetoDespesaTotal">Teto da despesa total da Camara = base x percentual da faixa (caput).</param>
/// <param name="DespesaTotalRealizada">Despesa total da Camara sujeita ao teto (aplica a regra temporal EC 109).</param>
/// <param name="MargemTeto">Folga (teto - realizado); negativa quando excede.</param>
/// <param name="UtilizacaoTeto">Fracao de utilizacao do teto (realizado / teto).</param>
/// <param name="SemaforoTeto">Semaforo do teto da despesa total.</param>
/// <param name="RepasseRecebido">Repasse/duodecimo efetivamente recebido pela Camara no exercicio.</param>
/// <param name="SubtetoFolha">Subteto da folha = repasse x percentual do §1 (ex.: 70%).</param>
/// <param name="FolhaRealizada">Folha realizada da Camara (ativos + inativos/pensionistas elegiveis).</param>
/// <param name="MargemSubtetoFolha">Folga do subteto de folha (subteto - folha); negativa quando excede.</param>
/// <param name="UtilizacaoSubtetoFolha">Fracao de utilizacao do subteto de folha (folha / subteto).</param>
/// <param name="SemaforoFolha">Semaforo do subteto de folha (§1).</param>
/// <param name="InativosNoTeto">Indica se inativos/pensionistas integraram o teto neste exercicio (EC 109).</param>
public sealed record ResultadoApuracaoArt29A(
    decimal PercentualFaixa,
    decimal BaseReceita,
    decimal TetoDespesaTotal,
    decimal DespesaTotalRealizada,
    decimal MargemTeto,
    decimal UtilizacaoTeto,
    SemaforoLimite SemaforoTeto,
    decimal RepasseRecebido,
    decimal SubtetoFolha,
    decimal FolhaRealizada,
    decimal MargemSubtetoFolha,
    decimal UtilizacaoSubtetoFolha,
    SemaforoLimite SemaforoFolha,
    bool InativosNoTeto)
{
    /// <summary>Indica se houve estouro de qualquer limite (teto OU subteto de folha) — art. 29-A §2/§3.</summary>
    public bool Irregular => SemaforoTeto == SemaforoLimite.Excedido || SemaforoFolha == SemaforoLimite.Excedido;
}

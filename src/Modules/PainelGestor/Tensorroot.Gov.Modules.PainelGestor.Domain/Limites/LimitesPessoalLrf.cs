namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

/// <summary>
/// Limites da Despesa com Pessoal sobre a RCL (LRF — LC 101/2000) para o <b>Poder Executivo municipal</b>,
/// expressos como frações (0..1). O <b>limite legal</b> (art. 19/20) é o teto; o <b>limite prudencial</b>
/// (art. 22, parágrafo único) é 95% do legal; o <b>limite de alerta</b> (art. 59 §1º I) é 90% do legal.
/// Objeto de valor IMUTÁVEL — os percentuais NUNCA são hardcoded no cálculo: vêm do
/// <c>ParametroLimitePessoal</c> versionado por tenant+vigência. Os defaults abaixo são apenas o
/// fallback legal de referência.
/// </summary>
/// <remarks>
/// // TODO(validar-oficial): para o Executivo municipal o limite legal usual é 54% da RCL (LRF art. 20,
/// III, "b"); prudencial 51,3% (95%); alerta 48,6% (90%). A apuração da própria RCL e da base de pessoal
/// foi alterada pela LC 178/2021 — confirmar com o módulo Finanças/TCE-RS antes de remessa real.
/// </remarks>
/// <param name="LimiteLegal">Teto legal da despesa de pessoal sobre a RCL (fração 0..1).</param>
/// <param name="FatorPrudencial">Fator do limite prudencial sobre o legal (default 0,95 — LRF art. 22).</param>
/// <param name="FatorAlerta">Fator do limite de alerta sobre o legal (default 0,90 — LRF art. 59 §1º I).</param>
public readonly record struct LimitesPessoalLrf(
    decimal LimiteLegal,
    decimal FatorPrudencial,
    decimal FatorAlerta)
{
    /// <summary>Teto legal de referência do Executivo municipal: 54% da RCL. // TODO(validar-oficial).</summary>
    public const decimal LimiteLegalExecutivoMunicipalPadrao = 0.54m;

    /// <summary>Fator prudencial legal: 95% do limite (LRF art. 22, parágrafo único).</summary>
    public const decimal FatorPrudencialPadrao = 0.95m;

    /// <summary>Fator de alerta legal: 90% do limite (LRF art. 59 §1º, I).</summary>
    public const decimal FatorAlertaPadrao = 0.90m;

    /// <summary>Limites de referência (fallback legal) do Executivo municipal.</summary>
    public static LimitesPessoalLrf PadraoExecutivoMunicipal { get; } =
        new(LimiteLegalExecutivoMunicipalPadrao, FatorPrudencialPadrao, FatorAlertaPadrao);

    /// <summary>Limite prudencial efetivo (95% do legal por padrão) — fração 0..1.</summary>
    public decimal LimitePrudencial => LimiteLegal * FatorPrudencial;

    /// <summary>Limite de alerta efetivo (90% do legal por padrão) — fração 0..1.</summary>
    public decimal LimiteAlerta => LimiteLegal * FatorAlerta;

    /// <summary>
    /// Classifica o percentual da RCL comprometido com pessoal frente aos três limites (semáforo).
    /// Determinístico (sem relógio). Vermelho ≥ legal; amarelo ≥ prudencial (ou ≥ alerta); verde abaixo.
    /// </summary>
    /// <param name="percentualDaRcl">Despesa de pessoal / RCL (fração 0..1).</param>
    /// <returns>Situação do indicador (semáforo).</returns>
    public SituacaoLimite Classificar(decimal percentualDaRcl)
    {
        if (percentualDaRcl >= LimiteLegal)
        {
            return SituacaoLimite.Excedido;
        }

        // Faixa de atenção: a partir do alerta (90%) já se sinaliza; o prudencial (95%) é o mesmo nível
        // amarelo (mais severo), mas para o gestor a cor permanece "Alerta" — a distância ao teto é o detalhe.
        return percentualDaRcl >= LimiteAlerta ? SituacaoLimite.Alerta : SituacaoLimite.Adequado;
    }
}

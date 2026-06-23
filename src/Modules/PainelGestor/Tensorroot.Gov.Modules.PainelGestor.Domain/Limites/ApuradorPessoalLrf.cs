namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

/// <summary>Resultado da apuração do indicador de Despesa com Pessoal (LRF).</summary>
/// <param name="DespesaPessoal">Despesa total com pessoal (base LRF) acumulada no período.</param>
/// <param name="Rcl">Receita Corrente Líquida (denominador).</param>
/// <param name="PercentualDaRcl">Despesa de pessoal / RCL (fração 0..1; 0 se RCL indisponível).</param>
/// <param name="Limites">Limites vigentes aplicados (legal/prudencial/alerta).</param>
/// <param name="Situacao">Semáforo do indicador.</param>
public readonly record struct ApuracaoPessoalLrf(
    decimal DespesaPessoal,
    decimal Rcl,
    decimal PercentualDaRcl,
    LimitesPessoalLrf Limites,
    SituacaoLimite Situacao);

/// <summary>
/// Função de domínio pura que apura o indicador de Despesa com Pessoal da LRF: % da RCL comprometido e
/// o semáforo frente aos limites legal/prudencial/alerta. Determinística e sem relógio (a janela temporal
/// é decidida por quem alimenta a despesa e a RCL — o apurador só divide e classifica). Quando a RCL
/// ainda não foi publicada (0 ou negativa), o percentual é 0 e a situação fica <c>Indeterminado</c> —
/// nunca divide por zero nem inventa um número.
/// </summary>
public static class ApuradorPessoalLrf
{
    /// <summary>Apura o indicador de pessoal da LRF.</summary>
    /// <param name="despesaPessoal">Despesa total com pessoal (base LRF) — numerador.</param>
    /// <param name="rcl">Receita Corrente Líquida — denominador.</param>
    /// <param name="limites">Limites vigentes (parametrizados por tenant+vigência).</param>
    /// <returns>A apuração (percentual + semáforo).</returns>
    public static ApuracaoPessoalLrf Apurar(decimal despesaPessoal, decimal rcl, LimitesPessoalLrf limites)
    {
        if (rcl <= 0m)
        {
            // RCL ausente/inválida: não há denominador legítimo — indeterminado (deny-by-default de cálculo).
            return new ApuracaoPessoalLrf(despesaPessoal, rcl < 0m ? 0m : rcl, 0m, limites, SituacaoLimite.Indeterminado);
        }

        var percentual = despesaPessoal / rcl;
        var situacao = limites.Classificar(percentual);
        return new ApuracaoPessoalLrf(despesaPessoal, rcl, percentual, limites, situacao);
    }
}

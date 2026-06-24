namespace Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

/// <summary>
/// Maquina de estados do convenio federal RECEBIDO (fluxo A — Dec. 11.531/2023):
/// <c>EmProposta -&gt; Celebrado -&gt; EmExecucao -&gt; EmPrestacaoContas -&gt; EmAnalise -&gt;
/// (Aprovado | AprovadoComRessalva | Rejeitado) | Inadimplente</c>. Os tres primeiros resultados de analise
/// e a inadimplencia sao TERMINAIS (A-INV-11).
/// </summary>
public enum SituacaoConvenioRecebido
{
    /// <summary>Proposta + plano de trabalho cadastrados, aguardando aprovacao/celebracao.</summary>
    EmProposta = 1,

    /// <summary>Convenio celebrado (assinado), aguardando o empenho da contrapartida para executar.</summary>
    Celebrado = 2,

    /// <summary>Em execucao fisico-financeira (liberacao de parcelas + contrapartida + rendimentos).</summary>
    EmExecucao = 3,

    /// <summary>Em prestacao de contas (parcial continua e/ou final).</summary>
    EmPrestacaoContas = 4,

    /// <summary>PC submetida e em analise pelo concedente.</summary>
    EmAnalise = 5,

    /// <summary>Aprovado (terminal).</summary>
    Aprovado = 6,

    /// <summary>Aprovado com ressalva (terminal).</summary>
    AprovadoComRessalva = 7,

    /// <summary>Rejeitado (terminal) — enseja TCE/devolucao.</summary>
    Rejeitado = 8,

    /// <summary>Inadimplente (terminal-bloqueio) — nao libera novas parcelas.</summary>
    Inadimplente = 9,
}

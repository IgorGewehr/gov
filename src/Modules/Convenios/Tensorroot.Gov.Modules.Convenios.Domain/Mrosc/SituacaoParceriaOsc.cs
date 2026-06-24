namespace Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;

/// <summary>
/// Maquina de estados da parceria-saida OSC (fluxo B — MROSC, Lei 13.019/2014):
/// <c>EmSelecao -&gt; Celebrada -&gt; EmExecucao -&gt; EmPrestacaoContas -&gt; EmAnalise -&gt;
/// (Aprovada | AprovadaComRessalva | Rejeitada) | Inadimplente</c>. A selecao (chamamento OU dispensa/
/// inexigibilidade) e modelada em <c>FormaSelecao</c>, NAO como sub-estados. Estados de resultado e
/// inadimplencia sao terminais (B-INV-11); a inadimplencia BLOQUEIA novos repasses (B-INV-9 — gatilho LRF).
/// </summary>
public enum SituacaoParceriaOsc
{
    /// <summary>Em selecao (chamamento publico OU dispensa/inexigibilidade).</summary>
    EmSelecao = 1,

    /// <summary>Celebrada (termo assinado), aguardando inicio da execucao.</summary>
    Celebrada = 2,

    /// <summary>Em execucao (repasses + monitoramento).</summary>
    EmExecucao = 3,

    /// <summary>Em prestacao de contas (OSC presta a Administracao).</summary>
    EmPrestacaoContas = 4,

    /// <summary>PC recebida e em analise.</summary>
    EmAnalise = 5,

    /// <summary>Aprovada (terminal).</summary>
    Aprovada = 6,

    /// <summary>Aprovada com ressalva (terminal).</summary>
    AprovadaComRessalva = 7,

    /// <summary>Rejeitada (terminal).</summary>
    Rejeitada = 8,

    /// <summary>Inadimplente (terminal-bloqueio) — BLOQUEIA novos repasses (LRF art. 48).</summary>
    Inadimplente = 9,
}

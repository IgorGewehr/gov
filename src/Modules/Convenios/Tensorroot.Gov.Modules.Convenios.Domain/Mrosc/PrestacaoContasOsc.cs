using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;

/// <summary>
/// Situacao da PC da OSC (fluxo B):
/// <c>Pendente -&gt; Recebida -&gt; EmAnalise -&gt; [EmSaneamento -&gt; EmAnalise] -&gt; (Aprovada | AprovadaComRessalva | Rejeitada)</c>.
/// </summary>
public enum SituacaoPrestacaoOsc
{
    /// <summary>Aberta (a OSC ainda nao entregou), com prazo de entrega 90+30.</summary>
    Pendente = 1,

    /// <summary>Recebida da OSC (inicia o prazo de analise de 150 d).</summary>
    Recebida = 2,

    /// <summary>Em analise.</summary>
    EmAnalise = 3,

    /// <summary>Em saneamento (prazo do tenant, uma vez).</summary>
    EmSaneamento = 4,

    /// <summary>Aprovada (terminal).</summary>
    Aprovada = 5,

    /// <summary>Aprovada com ressalva (terminal).</summary>
    AprovadaComRessalva = 6,

    /// <summary>Rejeitada (terminal).</summary>
    Rejeitada = 7,
}

/// <summary>
/// Prestacao de contas da OSC (entidade filha unica do agregado <see cref="ParceriaOsc"/>; Lei 13.019 arts.
/// 64-72 + Dec. 8.726 arts. 56-67). Carrega o prazo de ENTREGA (90 d, prorrogavel +30 uma vez — B-INV-6),
/// o prazo de ANALISE (150 d — B-INV-7), o saneamento limitado (45 d — B-INV-8) e a devolucao com indice
/// (B-INV-12). Os prazos chegam ja resolvidos do agregado (que detem o calendario).
/// </summary>
public sealed class PrestacaoContasOsc : Entity<Guid>
{
    private PrestacaoContasOsc()
    {
    }

    private PrestacaoContasOsc(Guid id, PrazoLegal prazoEntrega)
        : base(id)
    {
        PrazoEntrega = prazoEntrega;
        Situacao = SituacaoPrestacaoOsc.Pendente;
        EntregaProrrogada = false;
        SaneamentoConcedido = false;
    }

    /// <summary>Situacao na sub-maquina de estados.</summary>
    public SituacaoPrestacaoOsc Situacao { get; private set; }

    /// <summary>Prazo legal de ENTREGA pela OSC (90 d a partir do fim da vigencia — B-INV-6). Calculado.</summary>
    public PrazoLegal PrazoEntrega { get; private set; } = default!;

    /// <summary>Indica se a entrega ja foi prorrogada (+30 d, uma vez — B-INV-6).</summary>
    public bool EntregaProrrogada { get; private set; }

    /// <summary>Data de recebimento da PC da OSC (nula enquanto pendente).</summary>
    public DateOnly? DataRecebimento { get; private set; }

    /// <summary>Prazo legal de ANALISE (150 d a partir do recebimento — B-INV-7). Nulo ate o recebimento.</summary>
    public PrazoLegal? PrazoAnalise { get; private set; }

    /// <summary>Prazo de saneamento concedido (uma vez — B-INV-8). Nulo se nao houve.</summary>
    public PrazoLegal? PrazoSaneamento { get; private set; }

    /// <summary>Indica se o saneamento ja foi concedido (B-INV-8).</summary>
    public bool SaneamentoConcedido { get; private set; }

    /// <summary>Resultado da analise (nulo ate a conclusao).</summary>
    public ResultadoAnalise? Resultado { get; private set; }

    /// <summary>Valor de devolucao (glosas + saldo) atualizado pelo indice (B-INV-12).</summary>
    public Dinheiro? ValorDevolucao { get; private set; }

    /// <summary>Abre a PC pendente com o prazo de entrega ja calculado (90 d apos a vigencia — B-INV-6).</summary>
    /// <param name="prazoEntrega">Prazo de entrega resolvido pelo agregado.</param>
    /// <returns>Nova PC pendente.</returns>
    public static PrestacaoContasOsc Abrir(PrazoLegal prazoEntrega)
    {
        ArgumentNullException.ThrowIfNull(prazoEntrega);
        return new PrestacaoContasOsc(Guid.NewGuid(), prazoEntrega);
    }

    /// <summary>
    /// Prorroga UMA vez o prazo de entrega da OSC (+30 d — B-INV-6). O novo prazo chega ja resolvido do
    /// agregado.
    /// </summary>
    /// <param name="prazoProrrogado">Prazo de entrega prorrogado, resolvido pelo agregado.</param>
    /// <exception cref="InvalidOperationException">Se ja prorrogado ou a PC ja foi recebida.</exception>
    public void ProrrogarEntrega(PrazoLegal prazoProrrogado)
    {
        ArgumentNullException.ThrowIfNull(prazoProrrogado);
        if (Situacao != SituacaoPrestacaoOsc.Pendente)
        {
            throw new InvalidOperationException($"So PC pendente prorroga a entrega. Situacao atual: {Situacao}.");
        }

        if (EntregaProrrogada)
        {
            throw new InvalidOperationException("A prorrogacao da entrega ja foi concedida (uma unica vez — B-INV-6).");
        }

        PrazoEntrega = prazoProrrogado;
        EntregaProrrogada = true;
    }

    /// <summary>
    /// Recebe a PC da OSC (Pendente -&gt; Recebida), calculando o prazo de analise (150 d — B-INV-7), ja
    /// resolvido do agregado.
    /// </summary>
    /// <param name="dataRecebimento">Data de recebimento.</param>
    /// <param name="prazoAnalise">Prazo de analise resolvido pelo agregado.</param>
    /// <exception cref="InvalidOperationException">Se a PC nao estiver pendente.</exception>
    public void Receber(DateOnly dataRecebimento, PrazoLegal prazoAnalise)
    {
        ArgumentNullException.ThrowIfNull(prazoAnalise);
        if (Situacao != SituacaoPrestacaoOsc.Pendente)
        {
            throw new InvalidOperationException($"So PC pendente pode ser recebida. Situacao atual: {Situacao}.");
        }

        DataRecebimento = dataRecebimento;
        PrazoAnalise = prazoAnalise;
        Situacao = SituacaoPrestacaoOsc.Recebida;
    }

    /// <summary>Inicia a analise (Recebida -&gt; EmAnalise).</summary>
    /// <exception cref="InvalidOperationException">Se a PC nao estiver recebida.</exception>
    public void IniciarAnalise()
    {
        if (Situacao != SituacaoPrestacaoOsc.Recebida)
        {
            throw new InvalidOperationException($"So PC recebida entra em analise. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoPrestacaoOsc.EmAnalise;
    }

    /// <summary>Abre o saneamento (45 d, uma vez — B-INV-8). O prazo chega resolvido do agregado.</summary>
    /// <param name="prazoSaneamento">Prazo de saneamento resolvido pelo agregado.</param>
    /// <exception cref="InvalidOperationException">Se nao estiver em analise ou ja concedido (B-INV-8).</exception>
    public void AbrirSaneamento(PrazoLegal prazoSaneamento)
    {
        ArgumentNullException.ThrowIfNull(prazoSaneamento);
        if (Situacao != SituacaoPrestacaoOsc.EmAnalise)
        {
            throw new InvalidOperationException($"Saneamento exige PC em analise. Situacao atual: {Situacao}.");
        }

        if (SaneamentoConcedido)
        {
            throw new InvalidOperationException("O saneamento ja foi concedido para esta PC (uma unica vez — B-INV-8).");
        }

        PrazoSaneamento = prazoSaneamento;
        SaneamentoConcedido = true;
        Situacao = SituacaoPrestacaoOsc.EmSaneamento;
    }

    /// <summary>Retoma a analise apos o saneamento (EmSaneamento -&gt; EmAnalise).</summary>
    /// <exception cref="InvalidOperationException">Se nao estiver em saneamento.</exception>
    public void RetomarAnalise()
    {
        if (Situacao != SituacaoPrestacaoOsc.EmSaneamento)
        {
            throw new InvalidOperationException($"So PC em saneamento retoma a analise. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoPrestacaoOsc.EmAnalise;
    }

    /// <summary>Conclui a analise com o resultado (B-INV-11) — sempre a partir de EmAnalise.</summary>
    /// <param name="resultado">Resultado (aprovada/ressalva/rejeitada).</param>
    /// <exception cref="InvalidOperationException">Se a PC nao estiver em analise.</exception>
    public void Concluir(ResultadoAnalise resultado)
    {
        if (Situacao != SituacaoPrestacaoOsc.EmAnalise)
        {
            throw new InvalidOperationException($"Conclusao exige PC em analise. Situacao atual: {Situacao}.");
        }

        Resultado = resultado;
        Situacao = resultado switch
        {
            ResultadoAnalise.Aprovada => SituacaoPrestacaoOsc.Aprovada,
            ResultadoAnalise.AprovadaComRessalva => SituacaoPrestacaoOsc.AprovadaComRessalva,
            ResultadoAnalise.Rejeitada => SituacaoPrestacaoOsc.Rejeitada,
            _ => throw new ArgumentOutOfRangeException(nameof(resultado), resultado, "Resultado de analise invalido."),
        };
    }

    /// <summary>
    /// Calcula e fixa o valor de devolucao (B-INV-12): saldo/glosas atualizados pelo indice do tenant
    /// conforme o atraso.
    /// </summary>
    /// <param name="saldoEGlosas">Saldo nao aplicado + rendimentos retidos + glosas.</param>
    /// <param name="indice">Indice de devolucao (Selic) do tenant.</param>
    /// <param name="diasAtraso">Dias decorridos desde o vencimento da devolucao.</param>
    public void CalcularDevolucao(Dinheiro saldoEGlosas, IndiceDevolucao indice, int diasAtraso)
    {
        ArgumentNullException.ThrowIfNull(saldoEGlosas);
        ArgumentNullException.ThrowIfNull(indice);
        ValorDevolucao = indice.Atualizar(saldoEGlosas, diasAtraso);
    }

    /// <summary>Verdadeiro se a PC ainda nao foi entregue (subsidia o gatilho de inadimplencia — B-INV-6/9).</summary>
    public bool NaoEntregue => Situacao == SituacaoPrestacaoOsc.Pendente;
}

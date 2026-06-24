using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

/// <summary>
/// Situacao da PC do convenio (fluxo A):
/// <c>Pendente -&gt; Submetida -&gt; EmAnalise -&gt; [EmSaneamento -&gt; EmAnalise] -&gt; (Aprovada | AprovadaComRessalva | Rejeitada)</c>.
/// </summary>
public enum SituacaoPrestacaoConvenio
{
    /// <summary>Aberta, ainda nao submetida.</summary>
    Pendente = 1,

    /// <summary>Submetida ao concedente (inicia a contagem do prazo de analise).</summary>
    Submetida = 2,

    /// <summary>Em analise pelo concedente.</summary>
    EmAnalise = 3,

    /// <summary>Em saneamento de pendencias (prazo do tenant, uma vez).</summary>
    EmSaneamento = 4,

    /// <summary>Aprovada (terminal da PC).</summary>
    Aprovada = 5,

    /// <summary>Aprovada com ressalva (terminal da PC).</summary>
    AprovadaComRessalva = 6,

    /// <summary>Rejeitada (terminal da PC).</summary>
    Rejeitada = 7,
}

/// <summary>
/// Prestacao de contas do convenio recebido (entidade filha do agregado <see cref="ConvenioRecebido"/>):
/// parcial (continua, por parcela/etapa) ou final (apos a vigencia). Carrega a sub-maquina de estados, o
/// prazo de analise CALCULADO (A-INV-8), o saneamento limitado (A-INV-9) e, na final, o calculo de
/// devolucao com indice (A-INV-6/Selic). Os prazos chegam ja resolvidos do agregado (que tem o calendario).
/// </summary>
public sealed class PrestacaoContasConvenio : Entity<Guid>
{
    private PrestacaoContasConvenio()
    {
    }

    private PrestacaoContasConvenio(Guid id, TipoPrestacaoConvenio tipo, int? numeroEtapa, string competenciaRef)
        : base(id)
    {
        Tipo = tipo;
        NumeroEtapa = numeroEtapa;
        CompetenciaRef = competenciaRef;
        Situacao = SituacaoPrestacaoConvenio.Pendente;
    }

    /// <summary>Tipo (parcial/final).</summary>
    public TipoPrestacaoConvenio Tipo { get; private set; }

    /// <summary>
    /// Numero ESTRUTURADO da etapa/parcela coberta pela PC parcial (A-INV-4): correlaciona a PC ao numero de
    /// ordem da parcela do cronograma de forma deterministica, sem depender de substring de <see cref="CompetenciaRef"/>
    /// (texto livre). Nulo na PC final (que nao cobre uma etapa especifica).
    /// </summary>
    public int? NumeroEtapa { get; private set; }

    /// <summary>Competencia/etapa de referencia, texto livre descritivo (ex.: "2026/06" ou "Etapa 2"). NAO usado para correlacao (A-INV-4).</summary>
    public string CompetenciaRef { get; private set; } = default!;

    /// <summary>Situacao na sub-maquina de estados da PC.</summary>
    public SituacaoPrestacaoConvenio Situacao { get; private set; }

    /// <summary>Data de submissao (nula enquanto pendente).</summary>
    public DateOnly? DataSubmissao { get; private set; }

    /// <summary>Prazo legal de analise (calculado na submissao via calendario — A-INV-8). Nulo enquanto pendente.</summary>
    public PrazoLegal? PrazoAnalise { get; private set; }

    /// <summary>Prazo de saneamento concedido (uma vez — A-INV-9). Nulo se nao houve saneamento.</summary>
    public PrazoLegal? PrazoSaneamento { get; private set; }

    /// <summary>Indica se o saneamento ja foi concedido (limita a uma vez — A-INV-9).</summary>
    public bool SaneamentoConcedido { get; private set; }

    /// <summary>Resultado da analise (nulo enquanto nao concluida).</summary>
    public ResultadoAnalise? Resultado { get; private set; }

    /// <summary>Valor de saldo a devolver, atualizado pelo indice (preenchido na conclusao da PC final).</summary>
    public Dinheiro? ValorDevolucao { get; private set; }

    /// <summary>Cria uma PC parcial pendente, vinculada DETERMINISTICAMENTE ao numero da etapa/parcela (A-INV-4).</summary>
    /// <param name="numeroEtapa">Numero estruturado da etapa/parcela coberta (deve ser positivo).</param>
    /// <param name="competenciaRef">Competencia/etapa de referencia, texto livre descritivo (obrigatorio).</param>
    /// <returns>Nova PC parcial.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o numero da etapa nao for positivo.</exception>
    public static PrestacaoContasConvenio CriarParcial(int numeroEtapa, string competenciaRef)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(numeroEtapa, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(competenciaRef);
        return new PrestacaoContasConvenio(Guid.NewGuid(), TipoPrestacaoConvenio.Parcial, numeroEtapa, competenciaRef.Trim());
    }

    /// <summary>Cria a PC final pendente (sem etapa: cobre o convenio como um todo apos a vigencia).</summary>
    /// <param name="competenciaRef">Referencia (ex.: "Final").</param>
    /// <returns>Nova PC final.</returns>
    public static PrestacaoContasConvenio CriarFinal(string competenciaRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competenciaRef);
        return new PrestacaoContasConvenio(Guid.NewGuid(), TipoPrestacaoConvenio.Final, numeroEtapa: null, competenciaRef.Trim());
    }

    /// <summary>
    /// Submete a PC, calculando o prazo de analise via parametro do tenant (A-INV-8): parcial usa o prazo
    /// parcial; final, o prazo final. O prazo e CALCULADO pelo agregado (que detem o calendario) e injetado.
    /// </summary>
    /// <param name="dataSubmissao">Data de submissao (relogio externo).</param>
    /// <param name="prazoAnalise">Prazo de analise ja resolvido (parcial/final) pelo agregado.</param>
    /// <exception cref="InvalidOperationException">Se a PC nao estiver pendente.</exception>
    public void Submeter(DateOnly dataSubmissao, PrazoLegal prazoAnalise)
    {
        ArgumentNullException.ThrowIfNull(prazoAnalise);
        if (Situacao != SituacaoPrestacaoConvenio.Pendente)
        {
            throw new InvalidOperationException($"PC {Situacao} nao pode ser submetida.");
        }

        DataSubmissao = dataSubmissao;
        PrazoAnalise = prazoAnalise;
        Situacao = SituacaoPrestacaoConvenio.Submetida;
    }

    /// <summary>Inicia a analise (Submetida -&gt; EmAnalise).</summary>
    /// <exception cref="InvalidOperationException">Se a PC nao estiver submetida.</exception>
    public void IniciarAnalise()
    {
        if (Situacao != SituacaoPrestacaoConvenio.Submetida)
        {
            throw new InvalidOperationException($"PC {Situacao} nao pode entrar em analise.");
        }

        Situacao = SituacaoPrestacaoConvenio.EmAnalise;
    }

    /// <summary>
    /// Abre o saneamento (A-INV-9): concede o prazo (uma unica vez) a partir da notificacao de pendencia.
    /// </summary>
    /// <param name="prazoSaneamento">Prazo de saneamento ja resolvido pelo agregado.</param>
    /// <exception cref="InvalidOperationException">Se nao estiver em analise ou o saneamento ja foi concedido (A-INV-9).</exception>
    public void AbrirSaneamento(PrazoLegal prazoSaneamento)
    {
        ArgumentNullException.ThrowIfNull(prazoSaneamento);
        if (Situacao != SituacaoPrestacaoConvenio.EmAnalise)
        {
            throw new InvalidOperationException($"Saneamento exige PC em analise. Situacao atual: {Situacao}.");
        }

        if (SaneamentoConcedido)
        {
            throw new InvalidOperationException("O saneamento ja foi concedido para esta PC (uma unica vez — A-INV-9).");
        }

        PrazoSaneamento = prazoSaneamento;
        SaneamentoConcedido = true;
        Situacao = SituacaoPrestacaoConvenio.EmSaneamento;
    }

    /// <summary>Retoma a analise apos o saneamento (EmSaneamento -&gt; EmAnalise).</summary>
    /// <exception cref="InvalidOperationException">Se nao estiver em saneamento.</exception>
    public void RetomarAnalise()
    {
        if (Situacao != SituacaoPrestacaoConvenio.EmSaneamento)
        {
            throw new InvalidOperationException($"So PC em saneamento retoma a analise. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoPrestacaoConvenio.EmAnalise;
    }

    /// <summary>
    /// Conclui a analise com o resultado (A-INV-9/11): so a partir de <see cref="SituacaoPrestacaoConvenio.EmAnalise"/>.
    /// Rejeicao a partir do saneamento exige antes <see cref="RetomarAnalise"/> (a conclusao parte sempre de EmAnalise).
    /// </summary>
    /// <param name="resultado">Resultado (aprovada/ressalva/rejeitada).</param>
    /// <exception cref="InvalidOperationException">Se a PC nao estiver em analise.</exception>
    public void Concluir(ResultadoAnalise resultado)
    {
        if (Situacao != SituacaoPrestacaoConvenio.EmAnalise)
        {
            throw new InvalidOperationException($"Conclusao exige PC em analise. Situacao atual: {Situacao}.");
        }

        Resultado = resultado;
        Situacao = resultado switch
        {
            ResultadoAnalise.Aprovada => SituacaoPrestacaoConvenio.Aprovada,
            ResultadoAnalise.AprovadaComRessalva => SituacaoPrestacaoConvenio.AprovadaComRessalva,
            ResultadoAnalise.Rejeitada => SituacaoPrestacaoConvenio.Rejeitada,
            _ => throw new ArgumentOutOfRangeException(nameof(resultado), resultado, "Resultado de analise invalido."),
        };
    }

    /// <summary>
    /// Calcula e fixa o valor de devolucao da PC FINAL (A-INV-6): saldo nao aplicado + rendimentos retidos,
    /// atualizado pelo <paramref name="indice"/> (Selic do tenant) conforme o <paramref name="diasAtraso"/>.
    /// </summary>
    /// <param name="saldoNaoAplicado">Saldo + rendimentos nao reaplicados no objeto.</param>
    /// <param name="indice">Indice de devolucao (Selic) do tenant.</param>
    /// <param name="diasAtraso">Dias decorridos desde o vencimento da devolucao.</param>
    /// <exception cref="InvalidOperationException">Se nao for a PC final.</exception>
    public void CalcularDevolucao(Dinheiro saldoNaoAplicado, IndiceDevolucao indice, int diasAtraso)
    {
        ArgumentNullException.ThrowIfNull(saldoNaoAplicado);
        ArgumentNullException.ThrowIfNull(indice);
        if (Tipo != TipoPrestacaoConvenio.Final)
        {
            throw new InvalidOperationException("So a PC final calcula devolucao de saldo (A-INV-6).");
        }

        ValorDevolucao = indice.Atualizar(saldoNaoAplicado, diasAtraso);
    }

    /// <summary>Verdadeiro se a PC ja foi submetida (subsidia a regra de liberacao por etapa — A-INV-4).</summary>
    public bool FoiSubmetida => Situacao is not SituacaoPrestacaoConvenio.Pendente;

    /// <summary>Verdadeiro se a PC chegou a um desfecho terminal.</summary>
    public bool Concluida => Situacao is SituacaoPrestacaoConvenio.Aprovada
        or SituacaoPrestacaoConvenio.AprovadaComRessalva
        or SituacaoPrestacaoConvenio.Rejeitada;
}

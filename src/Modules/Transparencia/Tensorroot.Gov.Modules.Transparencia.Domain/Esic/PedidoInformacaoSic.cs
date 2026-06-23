using Tensorroot.Gov.Modules.Transparencia.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

/// <summary>Identificador forte do agregado <see cref="PedidoInformacaoSic"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PedidoInformacaoSicId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PedidoInformacaoSicId"/>.</returns>
    public static PedidoInformacaoSicId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Pedido de acesso a informacao (e-SIC — transparencia passiva, LAI Lei 12.527/2011). Raiz de agregado
/// rico do modulo Transparencia. Ciclo:
/// <c>Aberto -&gt; EmAtendimento -&gt; (Respondido|Indeferido) -&gt; [RecursoAberto -&gt; RecursoRespondido] -&gt; Encerrado</c>.
/// <para>
/// <b>Invariantes-chave:</b> o prazo legal e CALCULADO (DataAbertura + 20 dias uteis via
/// <see cref="ICalendarioDiasUteis"/>), nunca digitado; a prorrogacao e UNICA, justificada e so antes do
/// vencimento (+10 dias uteis — LAI art. 11 §2o); resposta/indeferimento sempre fundamentados; protocolo
/// unico por (tenant, ano). Cada transicao emite Domain Event.
/// </para>
/// </summary>
public sealed class PedidoInformacaoSic : AggregateRoot<PedidoInformacaoSicId>, IMustHaveTenant
{
    /// <summary>Prazo legal base de resposta (LAI art. 11 §1o): 20 dias.</summary>
    public const int DiasPrazoBase = 20;

    /// <summary>Prorrogacao legal maxima (LAI art. 11 §2o): +10 dias.</summary>
    public const int DiasProrrogacao = 10;

    private PedidoInformacaoSic()
    {
    }

    private PedidoInformacaoSic(
        PedidoInformacaoSicId id,
        Guid tenantId,
        ProtocoloSic protocolo,
        Solicitante solicitante,
        string descricao,
        FormaResposta formaResposta,
        DateOnly dataAbertura,
        DateOnly prazoResposta)
        : base(id)
    {
        TenantId = tenantId;
        Protocolo = protocolo;
        Solicitante = solicitante;
        Descricao = descricao;
        FormaResposta = formaResposta;
        DataAbertura = dataAbertura;
        PrazoResposta = prazoResposta;
        Situacao = SituacaoPedidoSic.Aberto;
        RaiseDomainEvent(new PedidoSicAberto(id, protocolo.Valor, prazoResposta));
    }

    /// <summary>Tenant (ente publico) dono do pedido.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Protocolo unico por (tenant, ano).</summary>
    public ProtocoloSic Protocolo { get; private set; } = default!;

    /// <summary>Dados do solicitante (PII — superficie interna apenas).</summary>
    public Solicitante Solicitante { get; private set; } = default!;

    /// <summary>Descricao do pedido (LAI veda exigir motivacao).</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Forma de resposta solicitada.</summary>
    public FormaResposta FormaResposta { get; private set; }

    /// <summary>Data de abertura/protocolizacao.</summary>
    public DateOnly DataAbertura { get; private set; }

    /// <summary>Prazo legal de resposta (DataAbertura + 20 dias uteis). Calculado, nunca digitado.</summary>
    public DateOnly PrazoResposta { get; private set; }

    /// <summary>Prazo apos prorrogacao (+10 dias uteis), quando houver.</summary>
    public DateOnly? ProrrogadoAte { get; private set; }

    /// <summary>Justificativa da prorrogacao (LAI art. 11 §2o — obrigatoria).</summary>
    public string? MotivoProrrogacao { get; private set; }

    /// <summary>Situacao atual (maquina de estados).</summary>
    public SituacaoPedidoSic Situacao { get; private set; }

    /// <summary>Resposta do orgao (nula enquanto nao respondido).</summary>
    public RespostaSic? Resposta { get; private set; }

    /// <summary>Fundamento legal do indeferimento (nulo se nao indeferido — LAI art. 11 §1o).</summary>
    public string? FundamentoIndeferimento { get; private set; }

    /// <summary>Recurso administrativo (nulo se nao interposto).</summary>
    public RecursoSic? Recurso { get; private set; }

    /// <summary>Prazo efetivo vigente (prorrogado, se houver; senao o base).</summary>
    public DateOnly PrazoVigente => ProrrogadoAte ?? PrazoResposta;

    /// <summary>
    /// Abre um pedido e-SIC, calculando o prazo legal (DataAbertura + 20 dias uteis) via calendario do
    /// tenant. Protocolo gerado pelo sequencial do (tenant, ano).
    /// </summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="protocolo">Protocolo gerado (unico por tenant/ano).</param>
    /// <param name="solicitante">Solicitante.</param>
    /// <param name="descricao">Descricao do pedido (obrigatoria).</param>
    /// <param name="formaResposta">Forma de resposta desejada.</param>
    /// <param name="dataAbertura">Data de abertura.</param>
    /// <param name="calendario">Calendario de dias uteis do tenant (prazo calculado).</param>
    /// <returns>Novo <see cref="PedidoInformacaoSic"/> em <c>Aberto</c>.</returns>
    /// <exception cref="ArgumentException">Se a descricao for vazia.</exception>
    public static PedidoInformacaoSic Abrir(
        Guid tenantId,
        ProtocoloSic protocolo,
        Solicitante solicitante,
        string descricao,
        FormaResposta formaResposta,
        DateOnly dataAbertura,
        ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(protocolo);
        ArgumentNullException.ThrowIfNull(solicitante);
        ArgumentNullException.ThrowIfNull(calendario);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        var prazo = calendario.SomarDiasUteis(dataAbertura, DiasPrazoBase);
        return new PedidoInformacaoSic(
            PedidoInformacaoSicId.New(),
            tenantId,
            protocolo,
            solicitante,
            descricao.Trim(),
            formaResposta,
            dataAbertura,
            prazo);
    }

    /// <summary>Inicia o atendimento (<c>Aberto -&gt; EmAtendimento</c>).</summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <c>Aberto</c>.</exception>
    public void IniciarAtendimento()
    {
        if (Situacao != SituacaoPedidoSic.Aberto)
        {
            throw new InvalidOperationException($"So pedidos Abertos entram em atendimento. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoPedidoSic.EmAtendimento;
        RaiseDomainEvent(new PedidoSicEmAtendimento(Id, Protocolo.Valor));
    }

    /// <summary>
    /// Prorroga o prazo em +10 dias uteis (LAI art. 11 §2o). Invariantes: prorrogacao UNICA, motivo
    /// obrigatorio e somente ANTES do vencimento do prazo base.
    /// </summary>
    /// <param name="motivo">Justificativa da prorrogacao (obrigatoria).</param>
    /// <param name="hoje">Data de referencia (para a regra "antes do vencimento").</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se ja prorrogado, encerrado ou apos o vencimento.</exception>
    public void Prorrogar(string motivo, DateOnly hoje, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        if (Situacao is SituacaoPedidoSic.Respondido or SituacaoPedidoSic.Indeferido
            or SituacaoPedidoSic.RecursoAberto or SituacaoPedidoSic.RecursoRespondido or SituacaoPedidoSic.Encerrado)
        {
            throw new InvalidOperationException($"Pedido {Situacao} nao pode ser prorrogado.");
        }

        if (ProrrogadoAte is not null)
        {
            throw new InvalidOperationException("A prorrogacao do prazo ja foi concedida (unica — LAI art. 11 §2o).");
        }

        if (hoje > PrazoResposta)
        {
            throw new InvalidOperationException("A prorrogacao so pode ser concedida antes do vencimento do prazo.");
        }

        ProrrogadoAte = calendario.SomarDiasUteis(PrazoResposta, DiasProrrogacao);
        MotivoProrrogacao = motivo.Trim();
        RaiseDomainEvent(new PedidoSicProrrogado(Id, Protocolo.Valor, ProrrogadoAte.Value));
    }

    /// <summary>
    /// Responde o pedido (<c>Aberto/EmAtendimento -&gt; Respondido</c>). Resposta sempre fundamentada
    /// (texto obrigatorio na VO). Resposta apos o prazo vigente NAO bloqueia, mas marca atraso para
    /// indicador (LAI/CGU).
    /// </summary>
    /// <param name="resposta">Resposta do orgao.</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao admitir resposta.</exception>
    public void Responder(RespostaSic resposta)
    {
        ArgumentNullException.ThrowIfNull(resposta);
        if (Situacao is not (SituacaoPedidoSic.Aberto or SituacaoPedidoSic.EmAtendimento))
        {
            throw new InvalidOperationException($"Pedido {Situacao} nao admite resposta direta.");
        }

        Resposta = resposta;
        Situacao = SituacaoPedidoSic.Respondido;
        var emAtraso = resposta.Data > PrazoVigente;
        RaiseDomainEvent(new PedidoSicRespondido(Id, Protocolo.Valor, resposta.Data, emAtraso));
    }

    /// <summary>
    /// Indefere o pedido com fundamento legal obrigatorio (LAI art. 11 §1o / art. 13-14)
    /// (<c>Aberto/EmAtendimento -&gt; Indeferido</c>).
    /// </summary>
    /// <param name="fundamentoLegal">Fundamento legal do indeferimento (obrigatorio).</param>
    /// <param name="data">Data do indeferimento.</param>
    /// <exception cref="ArgumentException">Se o fundamento for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao admitir indeferimento.</exception>
    public void Indeferir(string fundamentoLegal, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        if (Situacao is not (SituacaoPedidoSic.Aberto or SituacaoPedidoSic.EmAtendimento))
        {
            throw new InvalidOperationException($"Pedido {Situacao} nao admite indeferimento.");
        }

        FundamentoIndeferimento = fundamentoLegal.Trim();
        Situacao = SituacaoPedidoSic.Indeferido;
        RaiseDomainEvent(new PedidoSicIndeferido(Id, Protocolo.Valor, data));
    }

    /// <summary>
    /// Interpoe recurso contra a resposta/indeferimento (LAI art. 15)
    /// (<c>Respondido/Indeferido -&gt; RecursoAberto</c>).
    /// </summary>
    /// <param name="instancia">Instancia recorrida.</param>
    /// <param name="fundamento">Fundamento do recurso (obrigatorio).</param>
    /// <param name="dataInterposicao">Data de interposicao.</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao admitir recurso.</exception>
    public void InterporRecurso(InstanciaRecurso instancia, string fundamento, DateOnly dataInterposicao)
    {
        if (Situacao is not (SituacaoPedidoSic.Respondido or SituacaoPedidoSic.Indeferido or SituacaoPedidoSic.RecursoRespondido))
        {
            throw new InvalidOperationException($"Pedido {Situacao} nao admite interposicao de recurso.");
        }

        Recurso = RecursoSic.Interpor(instancia, fundamento, dataInterposicao);
        Situacao = SituacaoPedidoSic.RecursoAberto;
        RaiseDomainEvent(new PedidoSicRecursoInterposto(Id, Protocolo.Valor, instancia, dataInterposicao));
    }

    /// <summary>
    /// Decide o recurso pendente (<c>RecursoAberto -&gt; RecursoRespondido</c>).
    /// </summary>
    /// <param name="resultado">Resultado da decisao.</param>
    /// <param name="decisao">Texto da decisao (obrigatorio).</param>
    /// <param name="dataDecisao">Data da decisao.</param>
    /// <exception cref="InvalidOperationException">Se nao houver recurso pendente.</exception>
    public void DecidirRecurso(ResultadoRecurso resultado, string decisao, DateOnly dataDecisao)
    {
        if (Situacao != SituacaoPedidoSic.RecursoAberto || Recurso is null)
        {
            throw new InvalidOperationException("Nao ha recurso pendente de decisao.");
        }

        Recurso = Recurso.Decidir(resultado, decisao, dataDecisao);
        Situacao = SituacaoPedidoSic.RecursoRespondido;
        RaiseDomainEvent(new PedidoSicRecursoDecidido(Id, Protocolo.Valor, resultado, dataDecisao));
    }

    /// <summary>Encerra o pedido (estado terminal).</summary>
    /// <exception cref="InvalidOperationException">Se ainda estiver aberto/em atendimento sem desfecho.</exception>
    public void Encerrar()
    {
        if (Situacao is SituacaoPedidoSic.Aberto or SituacaoPedidoSic.EmAtendimento)
        {
            throw new InvalidOperationException("Pedido sem resposta/indeferimento nao pode ser encerrado.");
        }

        Situacao = SituacaoPedidoSic.Encerrado;
        RaiseDomainEvent(new PedidoSicEncerrado(Id, Protocolo.Valor));
    }
}

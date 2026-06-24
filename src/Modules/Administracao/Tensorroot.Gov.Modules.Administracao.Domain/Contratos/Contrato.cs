using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

/// <summary>Identificador forte do agregado <see cref="Contrato"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ContratoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ContratoId"/>.</returns>
    public static ContratoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Contrato administrativo (Lei 14.133/2021 — NLLC): instrumento que formaliza a relacao entre a
/// Administracao e o fornecedor vencedor de uma licitacao (ou de contratacao direta por dispensa/
/// inexigibilidade). Admite aditivos (limite de 25%, ate 50% em reforma — art. 125), apostilamentos
/// (dispensam termo aditivo — art. 136) e garantia de execucao (ate 5%/10% — art. 96/98). A publicacao
/// no PNCP e condicao de eficacia (art. 174) e a vigencia depende de credito orcamentario (art. 105-106;
/// LRF). Raiz de agregado tenant-scoped.
/// </summary>
public sealed partial class Contrato : AggregateRoot<ContratoId>, IMustHaveTenant
{
    private readonly List<Aditivo> _aditivos = [];
    private readonly List<Apostilamento> _apostilamentos = [];
    private readonly List<Garantia> _garantias = [];

    private Contrato()
    {
    }

    private Contrato(
        ContratoId id,
        Guid tenantId,
        Guid? licitacaoId,
        Guid fornecedorId,
        OrigemContratacao origem,
        string objeto,
        ValorMonetario valorContratado,
        DateOnly vigenciaInicio,
        DateOnly vigenciaFim,
        EmpenhoRef? empenhoRef)
        : base(id)
    {
        TenantId = tenantId;
        LicitacaoId = licitacaoId;
        FornecedorId = fornecedorId;
        OrigemContratacao = origem;
        Objeto = objeto;
        ValorContratado = valorContratado;
        ValorAtual = valorContratado;
        VigenciaInicio = vigenciaInicio;
        VigenciaFim = vigenciaFim;
        EmpenhoRef = empenhoRef;
        DotacaoConfirmada = false;
        PublicadoNoPncp = false;
        Situacao = SituacaoContrato.Assinado;
        RaiseDomainEvent(new ContratoAssinado(id, fornecedorId, valorContratado.Valor));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Licitacao de origem (nulo se contratacao direta).</summary>
    public Guid? LicitacaoId { get; private set; }

    /// <summary>Fornecedor contratado.</summary>
    public Guid FornecedorId { get; private set; }

    /// <summary>Fundamento da contratacao.</summary>
    public OrigemContratacao OrigemContratacao { get; private set; }

    /// <summary>Descricao do objeto contratado.</summary>
    public string Objeto { get; private set; } = default!;

    /// <summary>Valor global original do contrato.</summary>
    public ValorMonetario ValorContratado { get; private set; } = default!;

    /// <summary>Valor vigente apos aditivos/apostilamentos.</summary>
    public ValorMonetario ValorAtual { get; private set; } = default!;

    /// <summary>Inicio da vigencia.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Fim da vigencia.</summary>
    public DateOnly VigenciaFim { get; private set; }

    /// <summary>Referencia ao empenho de Financas (quando confirmada a dotacao).</summary>
    public EmpenhoRef? EmpenhoRef { get; private set; }

    /// <summary>Cobertura orcamentaria confirmada (via Integration Event de Financas) — I-8.</summary>
    public bool DotacaoConfirmada { get; private set; }

    /// <summary>Situacao atual no ciclo de vida.</summary>
    public SituacaoContrato Situacao { get; private set; }

    /// <summary>Identificador no PNCP, quando publicado.</summary>
    public string? NumeroContratoPncp { get; private set; }

    /// <summary>Eficacia obtida pela publicacao no PNCP (art. 174) — I-7.</summary>
    public bool PublicadoNoPncp { get; private set; }

    /// <summary>Termos aditivos do contrato.</summary>
    public IReadOnlyCollection<Aditivo> Aditivos => _aditivos;

    /// <summary>Apostilamentos do contrato.</summary>
    public IReadOnlyCollection<Apostilamento> Apostilamentos => _apostilamentos;

    /// <summary>Garantias de execucao do contrato.</summary>
    public IReadOnlyCollection<Garantia> Garantias => _garantias;

    /// <summary>
    /// Celebra (assina) um contrato, deixando-o na situacao inicial <see cref="SituacaoContrato.Assinado"/>
    /// e emitindo <see cref="ContratoAssinado"/> (I-6). Valida objeto/valor/vigencia e o enquadramento da origem.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="licitacaoId">Licitacao de origem (obrigatoria se origem = Licitacao; nula nas diretas) — I-4.</param>
    /// <param name="fornecedorId">Fornecedor contratado.</param>
    /// <param name="origem">Fundamento da contratacao.</param>
    /// <param name="objeto">Descricao do objeto (obrigatoria) — I-1.</param>
    /// <param name="valorContratado">Valor global (obrigatorio) — I-2.</param>
    /// <param name="vigenciaInicio">Inicio da vigencia.</param>
    /// <param name="vigenciaFim">Fim da vigencia (nao anterior ao inicio) — I-3.</param>
    /// <param name="empenhoRef">Referencia ao empenho, quando ja informada na celebracao.</param>
    /// <param name="fornecedorImpedido">
    /// Indica se o fornecedor tem sancao impeditiva (impedimento/inidoneidade) vigente na data da celebracao
    /// — aferido pelo handler sobre o agregado <c>Fornecedor</c> (limite de agregado). Fail-closed (BUG-A1).
    /// </param>
    /// <returns>Novo <see cref="Contrato"/> em <see cref="SituacaoContrato.Assinado"/>.</returns>
    /// <exception cref="ArgumentException">Se o objeto for vazio (I-1) ou a vigencia for invalida (I-3).</exception>
    /// <exception cref="ArgumentNullException">Se o valor contratado for nulo (I-2).</exception>
    /// <exception cref="InvalidOperationException">Se o enquadramento origem x licitacao for incoerente (I-4) ou o fornecedor estiver impedido (BUG-A1; art. 14/156).</exception>
    public static Contrato Celebrar(
        Guid tenantId,
        Guid? licitacaoId,
        Guid fornecedorId,
        OrigemContratacao origem,
        string objeto,
        ValorMonetario valorContratado,
        DateOnly vigenciaInicio,
        DateOnly vigenciaFim,
        bool fornecedorImpedido,
        EmpenhoRef? empenhoRef = null)
    {
        // I-1: objeto obrigatorio.
        ArgumentException.ThrowIfNullOrWhiteSpace(objeto);
        // I-2: valor obrigatorio.
        ArgumentNullException.ThrowIfNull(valorContratado);

        // BUG-A1: fail-closed. Fornecedor com sancao impeditiva vigente nao pode celebrar contrato
        // (art. 14 e art. 156, III/IV da Lei 14.133/2021), em qualquer origem (licitacao ou direta).
        if (fornecedorImpedido)
        {
            throw new InvalidOperationException(
                "Fornecedor com sancao impeditiva vigente (impedimento/inidoneidade) nao pode celebrar contrato (art. 14/156 Lei 14.133/2021).");
        }

        // I-3: vigencia coerente.
        if (vigenciaFim < vigenciaInicio)
        {
            throw new ArgumentException("Fim da vigencia nao pode ser anterior ao inicio.", nameof(vigenciaFim));
        }

        // I-4: coerencia origem x licitacao.
        if (origem == OrigemContratacao.Licitacao && (licitacaoId is null || licitacaoId == Guid.Empty))
        {
            throw new InvalidOperationException("Licitacao de origem e obrigatoria quando a origem e Licitacao.");
        }

        if (origem != OrigemContratacao.Licitacao && licitacaoId is not null && licitacaoId != Guid.Empty)
        {
            throw new InvalidOperationException("Licitacao de origem e vedada nas contratacoes diretas (Dispensa/Inexigibilidade).");
        }

        return new Contrato(
            ContratoId.New(),
            tenantId,
            origem == OrigemContratacao.Licitacao ? licitacaoId : null,
            fornecedorId,
            origem,
            objeto,
            valorContratado,
            vigenciaInicio,
            vigenciaFim,
            empenhoRef);
    }

    /// <summary>
    /// Publica o contrato no PNCP, marcando <see cref="PublicadoNoPncp"/> e emitindo
    /// <see cref="ContratoPublicadoPncp"/> (condicao de eficacia — art. 174). Se a dotacao ja estiver
    /// confirmada, o contrato transita internamente para <see cref="SituacaoContrato.Eficaz"/>.
    /// </summary>
    /// <param name="numeroContratoPncp">Identificador atribuido pelo PNCP (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o numero do PNCP for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o contrato estiver encerrado (I-14).</exception>
    public void PublicarContratoPncp(string numeroContratoPncp)
    {
        GarantirNaoEncerrado();
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroContratoPncp);

        NumeroContratoPncp = numeroContratoPncp;
        PublicadoNoPncp = true;
        RaiseDomainEvent(new ContratoPublicadoPncp(Id, numeroContratoPncp));
        AvaliarEficacia();
    }

    /// <summary>
    /// Confirma a cobertura orcamentaria (acionada pelo consumo de <c>EmpenhoEmitidoIntegrationEvent</c>
    /// de Financas), gravando a <see cref="EmpenhoRef"/> e, se ja publicado no PNCP, tornando o contrato
    /// <see cref="SituacaoContrato.Eficaz"/> (I-8).
    /// </summary>
    /// <param name="empenhoRef">Referencia ao empenho emitido.</param>
    /// <exception cref="InvalidOperationException">Se o contrato estiver encerrado (I-14).</exception>
    public void ConfirmarDotacao(EmpenhoRef empenhoRef)
    {
        GarantirNaoEncerrado();
        EmpenhoRef = empenhoRef;
        DotacaoConfirmada = true;
        AvaliarEficacia();
    }

    /// <summary>
    /// Bloqueia a eficacia (acionada pelo consumo de <c>DotacaoIndisponivelIntegrationEvent</c> de
    /// Financas): mantem o contrato sem cobertura orcamentaria e impede o inicio da execucao (I-8).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o contrato estiver encerrado (I-14).</exception>
    public void BloquearEficaciaPorDotacaoIndisponivel()
    {
        GarantirNaoEncerrado();
        DotacaoConfirmada = false;
        if (Situacao == SituacaoContrato.Eficaz)
        {
            Situacao = SituacaoContrato.Assinado;
        }
    }

    /// <summary>
    /// Inicia a execucao do contrato (passa a <see cref="SituacaoContrato.EmExecucao"/>), exigindo
    /// eficacia: publicacao no PNCP (I-7) e dotacao confirmada (I-8).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se faltar publicacao no PNCP, dotacao ou se nao estiver Eficaz.</exception>
    public void IniciarExecucao()
    {
        GarantirNaoEncerrado();

        // I-7: eficacia exige publicacao no PNCP.
        if (!PublicadoNoPncp)
        {
            throw new InvalidOperationException("Inicio da execucao exige publicacao no PNCP (art. 174).");
        }

        // I-8: vigencia exige credito orcamentario.
        if (!DotacaoConfirmada)
        {
            throw new InvalidOperationException("Inicio da execucao exige dotacao orcamentaria confirmada (art. 105-106; LRF).");
        }

        if (Situacao != SituacaoContrato.Eficaz)
        {
            throw new InvalidOperationException($"Inicio da execucao exige contrato Eficaz. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoContrato.EmExecucao;
    }

    /// <summary>
    /// Registra um apostilamento (reajuste/dotacao/correcao) que dispensa termo aditivo (art. 136; I-13).
    /// </summary>
    /// <param name="tipo">Tipo do apostilamento.</param>
    /// <param name="descricao">Descricao da alteracao (obrigatoria).</param>
    /// <param name="dataRegistro">Data do registro.</param>
    /// <returns>O <see cref="Apostilamento"/> registrado.</returns>
    /// <exception cref="ArgumentException">Se a descricao for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se o contrato estiver encerrado (I-14).</exception>
    public Apostilamento Apostilar(TipoApostilamento tipo, string descricao, DateOnly dataRegistro)
    {
        GarantirNaoEncerrado();
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        var numero = _apostilamentos.Count + 1;
        var apostilamento = Apostilamento.Registrar(numero, tipo, descricao, dataRegistro);
        _apostilamentos.Add(apostilamento);
        return apostilamento;
    }

    /// <summary>
    /// Presta uma garantia de execucao, limitada a 5% do valor (ate 10% em obras de grande vulto —
    /// art. 96/98; I-12).
    /// </summary>
    /// <param name="modalidade">Modalidade da garantia.</param>
    /// <param name="percentual">Percentual sobre o valor (ate 5%/10%).</param>
    /// <param name="valor">Valor prestado.</param>
    /// <param name="validadeFim">Data-fim de validade.</param>
    /// <param name="ehGrandeVulto">Indica obra de grande vulto (limite ampliado a 10%) — I-12.</param>
    /// <returns>A <see cref="Garantia"/> registrada.</returns>
    /// <exception cref="InvalidOperationException">Se o contrato estiver encerrado (I-14) ou o percentual exceder o limite (I-12).</exception>
    public Garantia PrestarGarantia(
        ModalidadeGarantia modalidade,
        decimal percentual,
        ValorMonetario valor,
        DateOnly validadeFim,
        bool ehGrandeVulto = false)
    {
        GarantirNaoEncerrado();
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentOutOfRangeException.ThrowIfNegative(percentual);

        // I-12: limite fino 5% (ate 10% em grande vulto).
        var limite = ehGrandeVulto ? LimiteGarantiaGrandeVulto : LimiteGarantiaComum;
        if (percentual > limite)
        {
            throw new InvalidOperationException(
                $"Garantia excede o limite legal ({limite}%). Percentual informado: {percentual}%.");
        }

        var garantia = Garantia.Registrar(modalidade, percentual, valor, validadeFim);
        _garantias.Add(garantia);
        return garantia;
    }

    /// <summary>
    /// Encerra o contrato por termino da vigencia/conclusao do objeto (passa a
    /// <see cref="SituacaoContrato.Encerrado"/>), emitindo <see cref="ContratoEncerrado"/>.
    /// </summary>
    /// <remarks>
    /// BUG-A7: o encerramento por conclusao de objeto exige execucao efetiva (<see cref="SituacaoContrato.EmExecucao"/>).
    /// Um contrato apenas <see cref="SituacaoContrato.Eficaz"/> que nunca executou nao se "encerra" — extingue-se por
    /// <see cref="Rescindir"/> (extincao antecipada). O evento distingue encerramento normal (vigencia decorrida) de
    /// antecipado, conforme <paramref name="dataReferencia"/>.
    /// </remarks>
    /// <param name="dataReferencia">Data de referencia do encerramento (relogio externo via handler).</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for EmExecucao (BUG-A7).</exception>
    public void Encerrar(DateOnly dataReferencia)
    {
        if (Situacao != SituacaoContrato.EmExecucao)
        {
            throw new InvalidOperationException(
                $"Encerramento por conclusao do objeto exige contrato EmExecucao; para extincao antecipada de contrato sem execucao use Rescindir. Situacao atual: {Situacao}.");
        }

        var antecipado = dataReferencia < VigenciaFim;
        Situacao = SituacaoContrato.Encerrado;
        RaiseDomainEvent(new ContratoEncerrado(Id, antecipado));
    }

    /// <summary>
    /// Rescinde o contrato (extincao antecipada; passa a <see cref="SituacaoContrato.Rescindido"/>),
    /// exigindo motivacao (I-15) e emitindo <see cref="ContratoRescindido"/>.
    /// </summary>
    /// <param name="motivo">Motivacao do ato administrativo (obrigatoria) — I-15.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio (I-15).</exception>
    /// <exception cref="InvalidOperationException">Se o contrato ja estiver encerrado (I-14).</exception>
    public void Rescindir(string motivo)
    {
        GarantirNaoEncerrado();
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        Situacao = SituacaoContrato.Rescindido;
        RaiseDomainEvent(new ContratoRescindido(Id, motivo));
    }

    /// <summary>Limite legal de alteracao quantitativa acumulada para bens/servicos (art. 125).</summary>
    public const decimal LimiteAditivoQuantitativo = 25m;

    /// <summary>Limite legal de acrescimo acumulado em reforma de edificio/equipamento (art. 125).</summary>
    public const decimal LimiteAditivoReforma = 50m;

    /// <summary>Limite legal da garantia de execucao em contratos comuns (art. 96).</summary>
    public const decimal LimiteGarantiaComum = 5m;

    /// <summary>Limite legal da garantia de execucao em obras de grande vulto (art. 98).</summary>
    public const decimal LimiteGarantiaGrandeVulto = 10m;

    private void AvaliarEficacia()
    {
        if (Situacao == SituacaoContrato.Assinado && PublicadoNoPncp && DotacaoConfirmada)
        {
            Situacao = SituacaoContrato.Eficaz;
        }
    }

    private void GarantirNaoEncerrado()
    {
        // I-14: contrato encerrado/rescindido nao admite novas transicoes.
        if (Situacao is SituacaoContrato.Encerrado or SituacaoContrato.Rescindido)
        {
            throw new InvalidOperationException($"Contrato encerrado nao admite novas transicoes. Situacao atual: {Situacao}.");
        }
    }
}

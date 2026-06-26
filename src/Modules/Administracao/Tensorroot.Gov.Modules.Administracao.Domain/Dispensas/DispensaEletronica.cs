using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

/// <summary>Identificador forte do agregado <see cref="DispensaEletronica"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DispensaEletronicaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DispensaEletronicaId"/>.</returns>
    public static DispensaEletronicaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Procedimento de contratacao direta por DISPENSA em razao do valor, na forma ELETRONICA (Lei
/// 14.133/2021, art. 75, I e II; IN SEGES/ME 67/2021 — Sistema de Dispensa Eletronica). Conduz a
/// disputa competitiva por menor preco/maior desconto entre fornecedores: aviso de contratacao direta
/// (prazo minimo de divulgacao) -> etapa de envio de lances sucessivos -> julgamento/classificacao ->
/// negociacao com o melhor colocado -> habilitacao -> homologacao pela autoridade competente.
/// Fail-closed quanto ao limite legal: o valor total estimado NAO pode exceder o limite de dispensa
/// vigente (parametrizavel por tenant — Dec. 12.343/2024 e atualizacoes anuais pelo IPCA-E, art. 182),
/// nem se pode homologar fornecedor com sancao impeditiva vigente (art. 14/156). Raiz de agregado,
/// tenant-scoped.
/// </summary>
public sealed partial class DispensaEletronica : AggregateRoot<DispensaEletronicaId>, IMustHaveTenant
{
    private readonly List<ItemDispensa> _itens = [];
    private readonly List<CotacaoDispensa> _cotacoes = [];

    private DispensaEletronica()
    {
    }

    private DispensaEletronica(
        DispensaEletronicaId id,
        Guid tenantId,
        string objeto,
        FundamentoDispensaValor fundamento,
        CriterioJulgamentoDispensa criterioJulgamento,
        ValorMonetario limiteLegalVigente,
        string limiteLegalNormaFonte,
        Guid? etpId,
        Guid? termoReferenciaId)
        : base(id)
    {
        TenantId = tenantId;
        Objeto = objeto;
        Fundamento = fundamento;
        CriterioJulgamento = criterioJulgamento;
        LimiteLegalVigente = limiteLegalVigente;
        LimiteLegalNormaFonte = limiteLegalNormaFonte;
        EtpId = etpId;
        TermoReferenciaId = termoReferenciaId;
        Situacao = SituacaoDispensa.Aberta;
        RaiseDomainEvent(new DispensaAberta(id, fundamento));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Descricao do objeto da contratacao direta.</summary>
    public string Objeto { get; private set; } = default!;

    /// <summary>Fundamento legal da dispensa em razao do valor (art. 75, I ou II).</summary>
    public FundamentoDispensaValor Fundamento { get; private set; }

    /// <summary>Criterio de julgamento (menor preco/maior desconto — IN 67/2021, art. 1 §2).</summary>
    public CriterioJulgamentoDispensa CriterioJulgamento { get; private set; }

    /// <summary>Limite legal de dispensa vigente aplicado a este procedimento (parametrizavel por tenant).</summary>
    public ValorMonetario LimiteLegalVigente { get; private set; } = default!;

    /// <summary>Norma-fonte do limite legal aplicado (rastreabilidade da parametrizacao).</summary>
    public string LimiteLegalNormaFonte { get; private set; } = default!;

    /// <summary>Referencia ao Estudo Tecnico Preliminar (ETP), quando exigido.</summary>
    public Guid? EtpId { get; private set; }

    /// <summary>Referencia ao Termo de Referencia (TR).</summary>
    public Guid? TermoReferenciaId { get; private set; }

    /// <summary>Situacao atual no ciclo de vida do procedimento.</summary>
    public SituacaoDispensa Situacao { get; private set; }

    /// <summary>Numero/identificador do aviso de contratacao direta publicado, quando ha.</summary>
    public string? NumeroAviso { get; private set; }

    /// <summary>Data/hora de abertura da etapa de lances (definida na publicacao do aviso).</summary>
    public DateTimeOffset? AberturaDisputa { get; private set; }

    /// <summary>Cotacao vencedora indicada no julgamento.</summary>
    public Guid? CotacaoVencedoraId { get; private set; }

    /// <summary>Identificador da contratacao no PNCP, quando publicado (art. 174).</summary>
    public string? NumeroPncp { get; private set; }

    /// <summary>Itens objeto da dispensa.</summary>
    public IReadOnlyCollection<ItemDispensa> Itens => _itens;

    /// <summary>Cotacoes (lances) recebidas.</summary>
    public IReadOnlyCollection<CotacaoDispensa> Cotacoes => _cotacoes;

    /// <summary>Valor total estimado da dispensa (soma dos itens).</summary>
    public ValorMonetario ValorTotalEstimado
        => _itens.Aggregate(ValorMonetario.Zero, (acumulado, item) => acumulado.Somar(item.ValorTotalEstimado));

    /// <summary>
    /// Abre uma dispensa eletronica (rascunho): nasce <c>Aberta</c>, para cadastro de itens. O limite
    /// legal vigente e a norma-fonte sao injetados pela borda (parametros do tenant), nunca embutidos no
    /// dominio.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="objeto">Descricao do objeto da contratacao direta.</param>
    /// <param name="fundamento">Fundamento legal (art. 75, I ou II).</param>
    /// <param name="criterioJulgamento">Criterio de julgamento (menor preco/maior desconto).</param>
    /// <param name="limiteLegalVigente">Limite de dispensa vigente para o fundamento (parametro do tenant).</param>
    /// <param name="limiteLegalNormaFonte">Norma-fonte do limite (ex.: "Dec. 12.343/2024").</param>
    /// <param name="etpId">Referencia ao ETP (opcional).</param>
    /// <param name="termoReferenciaId">Referencia ao TR (opcional).</param>
    /// <returns>Nova <see cref="DispensaEletronica"/> em situacao <c>Aberta</c>.</returns>
    /// <exception cref="ArgumentException">Objeto/norma-fonte vazios.</exception>
    /// <exception cref="ArgumentNullException">Limite legal nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Fundamento/criterio fora do enum.</exception>
    public static DispensaEletronica Abrir(
        Guid tenantId,
        string objeto,
        FundamentoDispensaValor fundamento,
        CriterioJulgamentoDispensa criterioJulgamento,
        ValorMonetario limiteLegalVigente,
        string limiteLegalNormaFonte,
        Guid? etpId = null,
        Guid? termoReferenciaId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objeto);
        ArgumentNullException.ThrowIfNull(limiteLegalVigente);
        ArgumentException.ThrowIfNullOrWhiteSpace(limiteLegalNormaFonte);
        if (!Enum.IsDefined(fundamento))
        {
            throw new ArgumentOutOfRangeException(nameof(fundamento), "Fundamento de dispensa invalido.");
        }

        if (!Enum.IsDefined(criterioJulgamento))
        {
            throw new ArgumentOutOfRangeException(nameof(criterioJulgamento), "Criterio de julgamento invalido.");
        }

        return new DispensaEletronica(
            DispensaEletronicaId.New(),
            tenantId,
            objeto,
            fundamento,
            criterioJulgamento,
            limiteLegalVigente,
            limiteLegalNormaFonte,
            etpId,
            termoReferenciaId);
    }

    /// <summary>Adiciona um item a dispensa em rascunho (situacao <c>Aberta</c>).</summary>
    /// <param name="itemCatalogoId">Referencia ao catalogo (opcional).</param>
    /// <param name="descricao">Descricao do objeto do item.</param>
    /// <param name="quantidade">Quantidade demandada.</param>
    /// <param name="valorUnitarioEstimado">Valor unitario estimado/orcado.</param>
    /// <returns>Identificador do item criado.</returns>
    /// <exception cref="InvalidOperationException">Se a dispensa nao estiver Aberta, ou o total exceder o limite legal vigente.</exception>
    public ItemDispensaId AdicionarItem(Guid? itemCatalogoId, string descricao, decimal quantidade, ValorMonetario valorUnitarioEstimado)
    {
        if (Situacao != SituacaoDispensa.Aberta)
        {
            throw new InvalidOperationException($"Itens so podem ser adicionados com a dispensa Aberta. Situacao atual: {Situacao}.");
        }

        var numero = _itens.Count == 0 ? 1 : _itens.Max(item => item.Numero) + 1;
        var item = ItemDispensa.Criar(numero, itemCatalogoId, descricao, quantidade, valorUnitarioEstimado);

        // Fail-closed do teto legal: o valor total estimado nao pode ultrapassar o limite de dispensa
        // vigente (art. 75, I/II; Dec. 12.343/2024). Recusa-se a inclusao que estouraria o teto — uma
        // dispensa por valor acima do limite e ato nulo (exigiria licitacao).
        var novoTotal = ValorTotalEstimado.Somar(item.ValorTotalEstimado);
        if (novoTotal.MaiorQue(LimiteLegalVigente))
        {
            throw new InvalidOperationException(
                $"Valor total estimado ({novoTotal}) excede o limite de dispensa vigente ({LimiteLegalVigente} — {LimiteLegalNormaFonte}); use a modalidade licitatoria adequada (art. 75 Lei 14.133/2021).");
        }

        _itens.Add(item);
        return item.Id;
    }

    /// <summary>
    /// Publica o aviso de contratacao direta (IN SEGES/ME 67/2021; art. 75 §3): exige ao menos um item e
    /// passa a dispensa a <c>AvisoPublicado</c>. A data de abertura da disputa deve respeitar o prazo
    /// minimo de divulgacao (validado na borda contra o parametro do tenant).
    /// </summary>
    /// <param name="numeroAviso">Identificador/numero do aviso publicado.</param>
    /// <param name="aberturaDisputa">Data/hora de abertura da etapa de lances.</param>
    /// <exception cref="ArgumentException">Numero do aviso vazio.</exception>
    /// <exception cref="InvalidOperationException">Dispensa nao Aberta ou sem itens.</exception>
    public void PublicarAviso(string numeroAviso, DateTimeOffset aberturaDisputa)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroAviso);
        if (Situacao != SituacaoDispensa.Aberta)
        {
            throw new InvalidOperationException($"A publicacao do aviso exige dispensa Aberta. Situacao atual: {Situacao}.");
        }

        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("A dispensa exige ao menos um item para publicar o aviso.");
        }

        NumeroAviso = numeroAviso;
        AberturaDisputa = aberturaDisputa;
        Situacao = SituacaoDispensa.AvisoPublicado;
        RaiseDomainEvent(new AvisoDispensaPublicado(Id, numeroAviso, aberturaDisputa));
    }

    /// <summary>
    /// Abre a etapa de envio de lances (IN SEGES/ME 67/2021, art. 9): so apos a data/hora de abertura
    /// definida no aviso. Passa a dispensa a <c>EmDisputa</c>.
    /// </summary>
    /// <param name="agora">Instante corrente (relogio externo via handler).</param>
    /// <exception cref="InvalidOperationException">Dispensa nao AvisoPublicado ou antes da data de abertura.</exception>
    public void AbrirDisputa(DateTimeOffset agora)
    {
        if (Situacao != SituacaoDispensa.AvisoPublicado)
        {
            throw new InvalidOperationException($"A abertura da disputa exige aviso publicado. Situacao atual: {Situacao}.");
        }

        if (AberturaDisputa is { } abertura && agora < abertura)
        {
            throw new InvalidOperationException(
                $"A disputa so pode ser aberta a partir de {abertura:O} (prazo minimo de divulgacao do aviso).");
        }

        Situacao = SituacaoDispensa.EmDisputa;
        RaiseDomainEvent(new DisputaDispensaAberta(Id));
    }

    /// <summary>
    /// Registra um lance (cotacao) de um fornecedor para um item, durante a etapa de disputa
    /// (<c>EmDisputa</c>). Lances sucessivos do mesmo fornecedor para o mesmo item so sao aceitos se
    /// MELHORAREM a oferta vigente conforme o criterio (menor preco: estritamente menor). Fornecedor com
    /// sancao impeditiva vigente nao pode cotar (fail-closed; art. 14/156 — aferido na borda).
    /// </summary>
    /// <param name="fornecedorId">Fornecedor proponente.</param>
    /// <param name="itemId">Item cotado.</param>
    /// <param name="valor">Valor ofertado.</param>
    /// <param name="dataRegistro">Momento do registro (relogio externo via handler).</param>
    /// <param name="fornecedorImpedido">Indica sancao impeditiva vigente do fornecedor (aferido na borda).</param>
    /// <returns>Identificador da cotacao registrada.</returns>
    /// <exception cref="InvalidOperationException">Dispensa fora de disputa, item inexistente, fornecedor impedido, ou lance que nao melhora a oferta.</exception>
    public CotacaoDispensaId RegistrarLance(Guid fornecedorId, ItemDispensaId itemId, ValorMonetario valor, DateTimeOffset dataRegistro, bool fornecedorImpedido)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (Situacao != SituacaoDispensa.EmDisputa)
        {
            throw new InvalidOperationException($"Lances so podem ser registrados com a disputa aberta. Situacao atual: {Situacao}.");
        }

        if (_itens.All(item => item.Id != itemId))
        {
            throw new InvalidOperationException("Item nao pertence a esta dispensa.");
        }

        // Fail-closed: fornecedor com sancao impeditiva vigente nao pode disputar (art. 14/156 Lei 14.133/2021).
        if (fornecedorImpedido)
        {
            throw new InvalidOperationException(
                "Fornecedor com sancao impeditiva vigente (impedimento/inidoneidade) nao pode cotar (art. 14/156 Lei 14.133/2021).");
        }

        // Lance sucessivo deve MELHORAR a oferta vigente do fornecedor para o item (IN 67/2021 — lances
        // decrescentes/sucessivos). Para menor preco: estritamente menor; para maior desconto: estritamente maior.
        var melhorVigente = MelhorLanceVigenteDe(fornecedorId, itemId);
        if (melhorVigente is not null && !LanceMelhora(valor, melhorVigente.Valor))
        {
            throw new InvalidOperationException(
                $"Lance ({valor}) nao melhora a oferta vigente do fornecedor para o item ({melhorVigente.Valor}) pelo criterio {CriterioJulgamento}.");
        }

        var sequencia = _cotacoes.Count == 0 ? 1L : _cotacoes.Max(cotacao => cotacao.Sequencia) + 1L;
        var cotacao = CotacaoDispensa.Registrar(fornecedorId, itemId, valor, dataRegistro, sequencia);
        _cotacoes.Add(cotacao);
        return cotacao.Id;
    }

    /// <summary>
    /// Encerra a disputa e julga: classifica a melhor cotacao pelo criterio e indica a vencedora,
    /// passando a dispensa a <c>EmJulgamento</c>. Quando ha mais de um item, julga pelo MENOR valor
    /// total agregado por fornecedor (proposta global), em linha com o julgamento por menor preco do
    /// procedimento. Empate: prevalece o lance de menor sequencia (primeiro a ofertar).
    /// </summary>
    /// <exception cref="InvalidOperationException">Dispensa fora de disputa ou sem cotacao valida.</exception>
    public void EncerrarDisputaEJulgar()
    {
        if (Situacao != SituacaoDispensa.EmDisputa)
        {
            throw new InvalidOperationException($"O julgamento exige disputa aberta. Situacao atual: {Situacao}.");
        }

        var melhor = MelhorCotacaoGlobal()
            ?? throw new InvalidOperationException("Nao ha cotacao valida para julgar; declare fracassada ou deserta.");

        melhor.Classificar(1);
        melhor.MarcarVencedora();
        CotacaoVencedoraId = melhor.Id.Value;
        Situacao = SituacaoDispensa.EmJulgamento;
    }

    /// <summary>
    /// Homologa o resultado (autoridade competente): exige <c>EmJulgamento</c>, cotacao vencedora
    /// definida e vencedor habilitado/apto. Fail-closed: vencedor com sancao impeditiva vigente nao pode
    /// ser homologado (art. 14/156 — aferido na borda).
    /// </summary>
    /// <param name="vencedorHabilitado">Indica se o fornecedor vencedor foi habilitado (regularidade fiscal/social/trabalhista; aferido na borda/handler).</param>
    /// <param name="fornecedorVencedorImpedido">Indica sancao impeditiva vigente do vencedor (aferido na borda).</param>
    /// <exception cref="InvalidOperationException">Dispensa fora de julgamento, sem vencedora, vencedor nao habilitado ou impedido.</exception>
    public void Homologar(bool vencedorHabilitado, bool fornecedorVencedorImpedido)
    {
        if (Situacao != SituacaoDispensa.EmJulgamento)
        {
            throw new InvalidOperationException($"A homologacao exige dispensa EmJulgamento. Situacao atual: {Situacao}.");
        }

        if (CotacaoVencedoraId is null)
        {
            throw new InvalidOperationException("A homologacao exige cotacao vencedora definida.");
        }

        var vencedora = _cotacoes.FirstOrDefault(cotacao => cotacao.Id.Value == CotacaoVencedoraId.Value)
            ?? throw new InvalidOperationException("Cotacao vencedora nao pertence a esta dispensa.");

        if (!vencedorHabilitado)
        {
            throw new InvalidOperationException("A homologacao exige vencedor habilitado (regularidade fiscal, social e trabalhista — IN 67/2021).");
        }

        if (fornecedorVencedorImpedido)
        {
            throw new InvalidOperationException(
                "Vencedor com sancao impeditiva vigente (impedimento/inidoneidade) nao pode ser homologado (art. 14/156 Lei 14.133/2021).");
        }

        Situacao = SituacaoDispensa.Homologada;
        RaiseDomainEvent(new DispensaHomologada(Id, CotacaoVencedoraId.Value, vencedora.FornecedorId, ValorAdjudicado()));
    }

    /// <summary>Registra a publicacao do procedimento no PNCP (art. 174).</summary>
    /// <param name="numeroPncp">Identificador da contratacao no PNCP.</param>
    /// <exception cref="ArgumentException">Numero do PNCP vazio.</exception>
    public void RegistrarPublicacaoPncp(string numeroPncp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroPncp);
        NumeroPncp = numeroPncp;
    }

    /// <summary>Encerra por inexistencia de cotacao valida/habilitada.</summary>
    /// <param name="motivo">Motivacao do ato.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Dispensa encerrada ou com cotacao valida.</exception>
    public void DeclararFracassada(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirNaoEncerrada();
        if (MelhorCotacaoGlobal() is not null)
        {
            throw new InvalidOperationException("Existe cotacao valida; a dispensa nao pode ser declarada fracassada.");
        }

        Situacao = SituacaoDispensa.Fracassada;
        RaiseDomainEvent(new DispensaFracassada(Id, motivo));
    }

    /// <summary>Encerra por ausencia total de cotacoes.</summary>
    /// <exception cref="InvalidOperationException">Dispensa encerrada ou com cotacao recebida.</exception>
    public void DeclararDeserta()
    {
        GarantirNaoEncerrada();
        if (_cotacoes.Count > 0)
        {
            throw new InvalidOperationException("Existe cotacao recebida; a dispensa nao pode ser declarada deserta.");
        }

        Situacao = SituacaoDispensa.Deserta;
        RaiseDomainEvent(new DispensaDeserta(Id));
    }

    /// <summary>Revoga por conveniencia/oportunidade.</summary>
    /// <param name="motivo">Motivacao do ato administrativo.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Dispensa encerrada.</exception>
    public void Revogar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirNaoEncerrada();
        Situacao = SituacaoDispensa.Revogada;
        RaiseDomainEvent(new DispensaRevogada(Id, motivo));
    }

    /// <summary>Anula por ilegalidade.</summary>
    /// <param name="motivo">Motivacao do ato administrativo (vicio de legalidade).</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Dispensa encerrada.</exception>
    public void Anular(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirNaoEncerrada();
        Situacao = SituacaoDispensa.Anulada;
        RaiseDomainEvent(new DispensaAnulada(Id, motivo));
    }

    /// <summary>Fornecedor da cotacao vencedora, quando indicada.</summary>
    /// <returns>Identificador do fornecedor vencedor, ou <c>null</c>.</returns>
    public Guid? FornecedorVencedorId()
        => CotacaoVencedoraId is null
            ? null
            : _cotacoes.FirstOrDefault(cotacao => cotacao.Id.Value == CotacaoVencedoraId.Value)?.FornecedorId;

    /// <summary>Valor adjudicado (valor total agregado do fornecedor vencedor), quando indicada a vencedora.</summary>
    /// <returns>Valor adjudicado, ou zero quando nao ha vencedora.</returns>
    public decimal ValorAdjudicado()
    {
        if (CotacaoVencedoraId is null)
        {
            return 0m;
        }

        var fornecedorVencedor = FornecedorVencedorId();
        return fornecedorVencedor is null ? 0m : TotalAgregadoDoFornecedor(fornecedorVencedor.Value).Valor;
    }
}

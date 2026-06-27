using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;

/// <summary>Identificador forte do agregado <see cref="Credenciamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CredenciamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CredenciamentoId"/>.</returns>
    public static CredenciamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Edital de CREDENCIAMENTO (Lei 14.133/2021, art. 78, I e art. 79): forma auxiliar de contratacao em
/// que a Administracao convoca, por CHAMAMENTO PUBLICO PERMANENTEMENTE ABERTO (art. 79, par. unico),
/// todos os interessados que satisfacam as condicoes do edital para, querendo, credenciarem-se. Por nao
/// haver competicao entre eles — todos os habilitados podem ser contratados, a precos FIXADOS pela
/// Administracao — a contratacao se processa por INEXIGIBILIDADE de licitacao (art. 74, IV).
///
/// As tres hipoteses autorizadoras (art. 79, I a III) sao modeladas em <see cref="HipoteseCredenciamento"/>:
/// (I) contratacao paralela e nao excludente; (II) selecao a criterio do beneficiario do servico;
/// (III) mercados fluidos. Os itens credenciaveis carregam o preco fixado (sem disputa). Mantem o rol de
/// credenciados (inscricoes), com ingresso a qualquer tempo enquanto vigente. Raiz de agregado, tenant-scoped.
/// </summary>
public sealed class Credenciamento : AggregateRoot<CredenciamentoId>, IMustHaveTenant
{
    private readonly List<ItemCredenciamento> _itens = [];
    private readonly List<Credenciado> _credenciados = [];

    private Credenciamento()
    {
    }

    private Credenciamento(
        CredenciamentoId id,
        Guid tenantId,
        string objeto,
        HipoteseCredenciamento hipotese,
        DateOnly vigenciaInicio,
        DateOnly vigenciaFim,
        string fundamentacaoLegal,
        Guid? etpId,
        Guid? termoReferenciaId)
        : base(id)
    {
        TenantId = tenantId;
        Objeto = objeto;
        Hipotese = hipotese;
        VigenciaInicio = vigenciaInicio;
        VigenciaFim = vigenciaFim;
        FundamentacaoLegal = fundamentacaoLegal;
        EtpId = etpId;
        TermoReferenciaId = termoReferenciaId;
        Situacao = SituacaoCredenciamento.EmElaboracao;
        RaiseDomainEvent(new CredenciamentoAberto(id, hipotese));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Descricao do objeto do credenciamento.</summary>
    public string Objeto { get; private set; } = default!;

    /// <summary>Hipotese autorizadora do credenciamento (art. 79, I a III).</summary>
    public HipoteseCredenciamento Hipotese { get; private set; }

    /// <summary>Situacao atual no ciclo de vida do edital.</summary>
    public SituacaoCredenciamento Situacao { get; private set; }

    /// <summary>Numero/identificador do edital de chamamento, quando publicado.</summary>
    public string? NumeroEdital { get; private set; }

    /// <summary>Inicio da vigencia do edital de chamamento.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Fim da vigencia do edital (apos o qual nao se recebem novas inscricoes).</summary>
    public DateOnly VigenciaFim { get; private set; }

    /// <summary>Fundamentacao legal do credenciamento (rastreabilidade do amparo — art. 74, IV c/c art. 79).</summary>
    public string FundamentacaoLegal { get; private set; } = default!;

    /// <summary>Referencia ao Estudo Tecnico Preliminar (ETP), quando exigido.</summary>
    public Guid? EtpId { get; private set; }

    /// <summary>Referencia ao Termo de Referencia (TR).</summary>
    public Guid? TermoReferenciaId { get; private set; }

    /// <summary>Numero de controle no PNCP, quando publicado (art. 79 c/c art. 174 — divulgacao obrigatoria).</summary>
    public string? NumeroPncp { get; private set; }

    /// <summary>Itens credenciaveis (objeto + preco fixado pela Administracao).</summary>
    public IReadOnlyCollection<ItemCredenciamento> Itens => _itens;

    /// <summary>Rol de inscricoes/credenciados (ingresso a qualquer tempo).</summary>
    public IReadOnlyCollection<Credenciado> Credenciados => _credenciados;

    /// <summary>Quantidade de interessados atualmente aptos (credenciados vigentes).</summary>
    public int QuantidadeCredenciadosAptos => _credenciados.Count(c => c.EstaApto());

    /// <summary>
    /// Abre um edital de credenciamento (rascunho): nasce <c>EmElaboracao</c>, para cadastro de itens e
    /// condicoes. A hipotese autorizadora (art. 79, I a III) e definida na abertura.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="objeto">Descricao do objeto do credenciamento.</param>
    /// <param name="hipotese">Hipotese autorizadora (art. 79, I a III).</param>
    /// <param name="vigenciaInicio">Inicio da vigencia do edital.</param>
    /// <param name="vigenciaFim">Fim da vigencia do edital.</param>
    /// <param name="fundamentacaoLegal">Amparo legal (ex.: "Art. 74, IV c/c art. 79, I, Lei 14.133/2021").</param>
    /// <param name="etpId">Referencia ao ETP (opcional).</param>
    /// <param name="termoReferenciaId">Referencia ao TR (opcional).</param>
    /// <returns>Novo <see cref="Credenciamento"/> em <c>EmElaboracao</c>.</returns>
    /// <exception cref="ArgumentException">Objeto/fundamentacao vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Hipotese fora do enum.</exception>
    /// <exception cref="InvalidOperationException">Vigencia final anterior a inicial.</exception>
    public static Credenciamento Abrir(
        Guid tenantId,
        string objeto,
        HipoteseCredenciamento hipotese,
        DateOnly vigenciaInicio,
        DateOnly vigenciaFim,
        string fundamentacaoLegal,
        Guid? etpId = null,
        Guid? termoReferenciaId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objeto);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentacaoLegal);
        if (!Enum.IsDefined(hipotese))
        {
            throw new ArgumentOutOfRangeException(nameof(hipotese), "Hipotese de credenciamento invalida (art. 79, I a III).");
        }

        if (vigenciaFim < vigenciaInicio)
        {
            throw new InvalidOperationException("A vigencia final do credenciamento nao pode ser anterior a inicial.");
        }

        return new Credenciamento(
            CredenciamentoId.New(),
            tenantId,
            objeto.Trim(),
            hipotese,
            vigenciaInicio,
            vigenciaFim,
            fundamentacaoLegal.Trim(),
            etpId,
            termoReferenciaId);
    }

    /// <summary>Adiciona um item credenciavel (preco fixado) ao edital em elaboracao.</summary>
    /// <param name="itemCatalogoId">Referencia ao catalogo (opcional).</param>
    /// <param name="descricao">Descricao do objeto credenciavel.</param>
    /// <param name="unidadeMedida">Unidade de prestacao/medida.</param>
    /// <param name="precoFixado">Preco unitario fixado pela Administracao.</param>
    /// <returns>Identificador do item criado.</returns>
    /// <exception cref="InvalidOperationException">Edital nao esta EmElaboracao.</exception>
    public ItemCredenciamentoId AdicionarItem(Guid? itemCatalogoId, string descricao, string unidadeMedida, ValorMonetario precoFixado)
    {
        GarantirEmElaboracao();
        var numero = _itens.Count == 0 ? 1 : _itens.Max(item => item.Numero) + 1;
        var item = ItemCredenciamento.Criar(numero, itemCatalogoId, descricao, unidadeMedida, precoFixado);
        _itens.Add(item);
        return item.Id;
    }

    /// <summary>Remove um item do edital em elaboracao.</summary>
    /// <param name="itemId">Item a remover.</param>
    /// <exception cref="InvalidOperationException">Edital nao esta EmElaboracao ou item inexistente.</exception>
    public void RemoverItem(ItemCredenciamentoId itemId)
    {
        GarantirEmElaboracao();
        var item = _itens.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Item nao pertence a este credenciamento.");
        _itens.Remove(item);
    }

    /// <summary>Ajusta o preco fixado de um item (revisao da tabela do edital, com o edital em elaboracao).</summary>
    /// <param name="itemId">Item a ajustar.</param>
    /// <param name="novoPreco">Novo preco fixado.</param>
    /// <exception cref="InvalidOperationException">Edital nao esta EmElaboracao ou item inexistente.</exception>
    public void AjustarPrecoItem(ItemCredenciamentoId itemId, ValorMonetario novoPreco)
    {
        GarantirEmElaboracao();
        var item = _itens.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Item nao pertence a este credenciamento.");
        item.AjustarPreco(novoPreco);
    }

    /// <summary>
    /// Publica o chamamento publico do credenciamento (Lei 14.133/2021, art. 79, par. unico): exige ao
    /// menos um item e abre o edital a inscricoes permanentes (<c>ChamamentoAberto</c>).
    /// </summary>
    /// <param name="numeroEdital">Numero/identificador do edital de chamamento.</param>
    /// <exception cref="ArgumentException">Numero do edital vazio.</exception>
    /// <exception cref="InvalidOperationException">Edital nao esta EmElaboracao ou sem itens.</exception>
    public void PublicarChamamento(string numeroEdital)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroEdital);
        GarantirEmElaboracao();
        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("O credenciamento exige ao menos um item para publicar o chamamento.");
        }

        NumeroEdital = numeroEdital.Trim();
        Situacao = SituacaoCredenciamento.ChamamentoAberto;
        RaiseDomainEvent(new ChamamentoCredenciamentoPublicado(Id, NumeroEdital));
    }

    /// <summary>Suspende temporariamente o recebimento de inscricoes (ato motivado), sem encerrar o edital.</summary>
    /// <param name="motivo">Motivacao da suspensao.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Edital nao esta com chamamento aberto.</exception>
    public void SuspenderChamamento(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoCredenciamento.ChamamentoAberto)
        {
            throw new InvalidOperationException($"A suspensao exige chamamento aberto. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoCredenciamento.Suspenso;
    }

    /// <summary>Reabre o recebimento de inscricoes apos suspensao.</summary>
    /// <exception cref="InvalidOperationException">Edital nao esta suspenso.</exception>
    public void ReabrirChamamento()
    {
        if (Situacao != SituacaoCredenciamento.Suspenso)
        {
            throw new InvalidOperationException($"A reabertura exige chamamento Suspenso. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoCredenciamento.ChamamentoAberto;
    }

    /// <summary>
    /// Inscreve um interessado no credenciamento (protocolo de adesao). Permite ingresso a qualquer tempo
    /// enquanto o chamamento estiver aberto (art. 79, par. unico). Fail-closed: um mesmo fornecedor nao
    /// pode ter duas inscricoes ativas (em analise ou credenciada) no mesmo edital.
    /// </summary>
    /// <param name="fornecedorId">Fornecedor interessado.</param>
    /// <param name="dataInscricao">Data de protocolo.</param>
    /// <returns>Identificador da inscricao criada.</returns>
    /// <exception cref="InvalidOperationException">Chamamento nao aberto, edital fora de vigencia ou fornecedor ja com inscricao ativa.</exception>
    public CredenciadoId Inscrever(Guid fornecedorId, DateOnly dataInscricao)
    {
        if (Situacao != SituacaoCredenciamento.ChamamentoAberto)
        {
            throw new InvalidOperationException($"Inscricoes so sao recebidas com o chamamento aberto. Situacao atual: {Situacao}.");
        }

        if (dataInscricao < VigenciaInicio || dataInscricao > VigenciaFim)
        {
            throw new InvalidOperationException(
                $"Inscricao ({dataInscricao:O}) fora da vigencia do edital ({VigenciaInicio:O} a {VigenciaFim:O}).");
        }

        var jaInscrito = _credenciados.Any(c =>
            c.FornecedorId == fornecedorId
            && c.Situacao is SituacaoCredenciado.EmAnalise or SituacaoCredenciado.Credenciado or SituacaoCredenciado.Suspenso);
        if (jaInscrito)
        {
            throw new InvalidOperationException("Fornecedor ja possui inscricao ativa neste credenciamento.");
        }

        var inscricao = Credenciado.Inscrever(fornecedorId, dataInscricao);
        _credenciados.Add(inscricao);
        RaiseDomainEvent(new InteressadoInscrito(Id, inscricao.Id, fornecedorId));
        return inscricao.Id;
    }

    /// <summary>Defere uma inscricao (habilitacao documental): o interessado torna-se credenciado apto.</summary>
    /// <param name="credenciadoId">Inscricao a deferir.</param>
    /// <param name="data">Data do deferimento.</param>
    /// <param name="fornecedorImpedido">Indica sancao impeditiva vigente do fornecedor (aferido na borda).</param>
    /// <exception cref="InvalidOperationException">Inscricao inexistente, fora de analise ou fornecedor impedido.</exception>
    public void DeferirInscricao(CredenciadoId credenciadoId, DateOnly data, bool fornecedorImpedido)
    {
        var inscricao = ObterInscricao(credenciadoId);
        inscricao.Deferir(data, fornecedorImpedido);
        RaiseDomainEvent(new InteressadoCredenciado(Id, inscricao.Id, inscricao.FornecedorId));
    }

    /// <summary>Indefere uma inscricao por nao atendimento das condicoes do edital.</summary>
    /// <param name="credenciadoId">Inscricao a indeferir.</param>
    /// <param name="motivo">Motivacao do indeferimento.</param>
    /// <exception cref="InvalidOperationException">Inscricao inexistente ou fora de analise.</exception>
    public void IndeferirInscricao(CredenciadoId credenciadoId, string motivo)
        => ObterInscricao(credenciadoId).Indeferir(motivo);

    /// <summary>Suspende um credenciado (descumprimento sanavel).</summary>
    /// <param name="credenciadoId">Inscricao a suspender.</param>
    /// <param name="motivo">Motivacao da suspensao.</param>
    /// <exception cref="InvalidOperationException">Inscricao inexistente ou nao apta.</exception>
    public void SuspenderCredenciado(CredenciadoId credenciadoId, string motivo)
        => ObterInscricao(credenciadoId).Suspender(motivo);

    /// <summary>Reabilita um credenciado suspenso.</summary>
    /// <param name="credenciadoId">Inscricao a reabilitar.</param>
    /// <exception cref="InvalidOperationException">Inscricao inexistente ou nao suspensa.</exception>
    public void ReabilitarCredenciado(CredenciadoId credenciadoId)
        => ObterInscricao(credenciadoId).Reabilitar();

    /// <summary>Descredencia um interessado (terminal): a pedido, descumprimento ou sancao impeditiva.</summary>
    /// <param name="credenciadoId">Inscricao a descredenciar.</param>
    /// <param name="data">Data do descredenciamento.</param>
    /// <param name="motivo">Motivacao do ato.</param>
    /// <exception cref="InvalidOperationException">Inscricao inexistente ou ja terminal.</exception>
    public void Descredenciar(CredenciadoId credenciadoId, DateOnly data, string motivo)
    {
        var inscricao = ObterInscricao(credenciadoId);
        inscricao.Descredenciar(data, motivo);
        RaiseDomainEvent(new InteressadoDescredenciado(Id, inscricao.Id, inscricao.FornecedorId, motivo));
    }

    /// <summary>Encerra o credenciamento por decurso de vigencia/conveniencia (terminal).</summary>
    /// <param name="motivo">Motivacao do ato.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Edital ja terminal.</exception>
    public void Encerrar(string motivo)
        => AlcancarTerminal(SituacaoCredenciamento.Encerrado, motivo);

    /// <summary>Anula o edital por ilegalidade (terminal).</summary>
    /// <param name="motivo">Motivacao do ato (vicio de legalidade).</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Edital ja terminal.</exception>
    public void Anular(string motivo)
        => AlcancarTerminal(SituacaoCredenciamento.Anulado, motivo);

    /// <summary>Revoga o edital por conveniencia/oportunidade (terminal).</summary>
    /// <param name="motivo">Motivacao do ato administrativo.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Edital ja terminal.</exception>
    public void Revogar(string motivo)
        => AlcancarTerminal(SituacaoCredenciamento.Revogado, motivo);

    /// <summary>Registra a divulgacao do credenciamento no PNCP (art. 174).</summary>
    /// <param name="numeroPncp">Numero de controle no PNCP.</param>
    /// <exception cref="ArgumentException">Numero do PNCP vazio.</exception>
    public void RegistrarPublicacaoPncp(string numeroPncp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroPncp);
        NumeroPncp = numeroPncp.Trim();
    }

    private void AlcancarTerminal(SituacaoCredenciamento terminal, string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is SituacaoCredenciamento.Encerrado or SituacaoCredenciamento.Anulado or SituacaoCredenciamento.Revogado)
        {
            throw new InvalidOperationException($"Credenciamento em estado terminal nao admite novo encerramento. Situacao atual: {Situacao}.");
        }

        Situacao = terminal;
        RaiseDomainEvent(new CredenciamentoEncerrado(Id, terminal, motivo.Trim()));
    }

    private Credenciado ObterInscricao(CredenciadoId credenciadoId)
        => _credenciados.FirstOrDefault(c => c.Id == credenciadoId)
            ?? throw new InvalidOperationException("Inscricao nao pertence a este credenciamento.");

    private void GarantirEmElaboracao()
    {
        if (Situacao != SituacaoCredenciamento.EmElaboracao)
        {
            throw new InvalidOperationException($"Operacao exige edital EmElaboracao. Situacao atual: {Situacao}.");
        }
    }
}

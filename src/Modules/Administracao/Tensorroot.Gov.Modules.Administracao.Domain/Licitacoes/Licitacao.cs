using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

/// <summary>Identificador forte do agregado <see cref="Licitacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LicitacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LicitacaoId"/>.</returns>
    public static LicitacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Procedimento de selecao competitiva da proposta mais vantajosa para a Administracao,
/// conduzido sob a Lei 14.133/2021 (NLLC). Assume as modalidades Pregao, Concorrencia e
/// DialogoCompetitivo, ou contratacao direta (Dispensa/Inexigibilidade). Encerra-se por
/// homologacao, fracasso, desercao, revogacao ou anulacao. A publicidade no PNCP e obrigatoria
/// (art. 174). Raiz de agregado, tenant-scoped.
/// </summary>
public sealed class Licitacao : AggregateRoot<LicitacaoId>, IMustHaveTenant
{
    private readonly List<Lote> _lotes = [];
    private readonly List<Proposta> _propostas = [];
    private readonly List<Habilitacao> _habilitacoes = [];
    private readonly List<Recurso> _recursos = [];

    private Licitacao()
    {
    }

    private Licitacao(
        LicitacaoId id,
        Guid tenantId,
        string objeto,
        ModalidadeLicitacao modalidade,
        CriterioJulgamento criterioJulgamento,
        ValorMonetario valorEstimado,
        Guid? etpId,
        Guid? termoReferenciaId)
        : base(id)
    {
        TenantId = tenantId;
        Objeto = objeto;
        Modalidade = modalidade;
        CriterioJulgamento = criterioJulgamento;
        ValorEstimado = valorEstimado;
        EtpId = etpId;
        TermoReferenciaId = termoReferenciaId;
        Situacao = SituacaoLicitacao.Aberta;
        RaiseDomainEvent(new LicitacaoAberta(id, modalidade));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Descricao do objeto licitado.</summary>
    public string Objeto { get; private set; } = default!;

    /// <summary>Modalidade do certame (art. 28/74/75).</summary>
    public ModalidadeLicitacao Modalidade { get; private set; }

    /// <summary>Criterio de julgamento (art. 33).</summary>
    public CriterioJulgamento CriterioJulgamento { get; private set; }

    /// <summary>Valor estimado/orcado da contratacao.</summary>
    public ValorMonetario ValorEstimado { get; private set; } = default!;

    /// <summary>Referencia ao Estudo Tecnico Preliminar (ETP).</summary>
    public Guid? EtpId { get; private set; }

    /// <summary>Referencia ao Termo de Referencia (TR).</summary>
    public Guid? TermoReferenciaId { get; private set; }

    /// <summary>Situacao atual no ciclo de vida do certame.</summary>
    public SituacaoLicitacao Situacao { get; private set; }

    /// <summary>Identificador da contratacao no PNCP, quando publicado (art. 174).</summary>
    public string? NumeroEditalPncp { get; private set; }

    /// <summary>Proposta vencedora indicada no julgamento.</summary>
    public Guid? PropostaVencedoraId { get; private set; }

    /// <summary>Lotes do certame.</summary>
    public IReadOnlyCollection<Lote> Lotes => _lotes;

    /// <summary>Propostas recebidas.</summary>
    public IReadOnlyCollection<Proposta> Propostas => _propostas;

    /// <summary>Habilitacoes verificadas.</summary>
    public IReadOnlyCollection<Habilitacao> Habilitacoes => _habilitacoes;

    /// <summary>Recursos interpostos.</summary>
    public IReadOnlyCollection<Recurso> Recursos => _recursos;

    /// <summary>
    /// Abre a licitacao: publica o edital e inicia o certame (situacao inicial Aberta).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="objeto">Descricao do objeto licitado.</param>
    /// <param name="modalidade">Modalidade do certame.</param>
    /// <param name="criterioJulgamento">Criterio de julgamento.</param>
    /// <param name="valorEstimado">Valor estimado/orcado.</param>
    /// <param name="etpId">Referencia ao ETP (opcional).</param>
    /// <param name="termoReferenciaId">Referencia ao TR (opcional).</param>
    /// <returns>Nova <see cref="Licitacao"/> em situacao Aberta.</returns>
    /// <exception cref="ArgumentException">I-1: objeto vazio/nulo.</exception>
    /// <exception cref="ArgumentNullException">I-2: valor estimado nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">I-3: modalidade/criterio fora do enum.</exception>
    /// <exception cref="InvalidOperationException">I-5: combinacao modalidade x criterio invalida (Pregao).</exception>
    public static Licitacao Abrir(
        Guid tenantId,
        string objeto,
        ModalidadeLicitacao modalidade,
        CriterioJulgamento criterioJulgamento,
        ValorMonetario valorEstimado,
        Guid? etpId = null,
        Guid? termoReferenciaId = null)
    {
        // I-1
        ArgumentException.ThrowIfNullOrWhiteSpace(objeto);
        // I-2
        ArgumentNullException.ThrowIfNull(valorEstimado);
        // I-3
        if (!Enum.IsDefined(modalidade))
        {
            throw new ArgumentOutOfRangeException(nameof(modalidade), "Modalidade invalida.");
        }

        if (!Enum.IsDefined(criterioJulgamento))
        {
            throw new ArgumentOutOfRangeException(nameof(criterioJulgamento), "Criterio de julgamento invalido.");
        }

        // I-5
        if (modalidade == ModalidadeLicitacao.Pregao
            && criterioJulgamento is not (CriterioJulgamento.MenorPreco or CriterioJulgamento.MaiorDesconto))
        {
            throw new InvalidOperationException("Pregao admite apenas menor preco ou maior desconto (art. 6, XLI / art. 28, I).");
        }

        // I-4: situacao inicial Aberta + evento LicitacaoAberta (no construtor).
        return new Licitacao(
            LicitacaoId.New(),
            tenantId,
            objeto,
            modalidade,
            criterioJulgamento,
            valorEstimado,
            etpId,
            termoReferenciaId);
    }

    /// <summary>Adiciona um lote ao certame Aberto.</summary>
    /// <param name="numero">Numero do lote.</param>
    /// <param name="descricao">Descricao do objeto do lote.</param>
    /// <param name="valorEstimado">Valor estimado do lote.</param>
    /// <returns>Identificador do lote criado.</returns>
    /// <exception cref="InvalidOperationException">Se o certame nao estiver Aberto.</exception>
    public LoteId AdicionarLote(int numero, string descricao, ValorMonetario valorEstimado)
    {
        if (Situacao != SituacaoLicitacao.Aberta)
        {
            throw new InvalidOperationException($"Lotes so podem ser adicionados com o certame Aberto. Situacao atual: {Situacao}.");
        }

        var lote = Lote.Criar(numero, descricao, valorEstimado);
        _lotes.Add(lote);
        return lote.Id;
    }

    /// <summary>Registra uma proposta recebida de um licitante (certame em andamento).</summary>
    /// <param name="fornecedorId">Licitante proponente.</param>
    /// <param name="loteId">Lote disputado.</param>
    /// <param name="valor">Valor ofertado.</param>
    /// <returns>Identificador da proposta registrada.</returns>
    /// <exception cref="InvalidOperationException">Se o certame nao estiver em andamento.</exception>
    public PropostaId RegistrarProposta(Guid fornecedorId, LoteId loteId, ValorMonetario valor)
    {
        GarantirEmAndamento();
        var proposta = Proposta.Registrar(fornecedorId, loteId, valor);
        _propostas.Add(proposta);
        return proposta.Id;
    }

    /// <summary>
    /// Publica o edital no Portal Nacional de Contratacoes Publicas (art. 174). Mantem a situacao Aberta.
    /// </summary>
    /// <param name="numeroEditalPncp">Identificador da contratacao no PNCP.</param>
    /// <exception cref="ArgumentException">I-13: numero do edital vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao for Aberta.</exception>
    public void PublicarEditalPncp(string numeroEditalPncp)
    {
        // I-13
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroEditalPncp);
        if (Situacao != SituacaoLicitacao.Aberta)
        {
            throw new InvalidOperationException($"A publicacao no PNCP exige certame Aberto. Situacao atual: {Situacao}.");
        }

        NumeroEditalPncp = numeroEditalPncp;
        RaiseDomainEvent(new EditalPublicadoNoPncp(Id, numeroEditalPncp));
    }

    /// <summary>
    /// Julga as propostas: indica a vencedora e passa o certame a EmJulgamento (I-6).
    /// </summary>
    /// <param name="propostaVencedoraId">Proposta indicada como vencedora.</param>
    /// <exception cref="InvalidOperationException">I-6: situacao diferente de Aberta ou proposta invalida.</exception>
    public void JulgarPropostas(Guid propostaVencedoraId)
    {
        // I-6
        if (Situacao != SituacaoLicitacao.Aberta)
        {
            throw new InvalidOperationException($"O julgamento exige certame Aberto. Situacao atual: {Situacao}.");
        }

        var vencedora = _propostas.FirstOrDefault(p => p.Id.Value == propostaVencedoraId)
            ?? throw new InvalidOperationException("Proposta vencedora nao pertence ao certame.");

        if (vencedora.Situacao == SituacaoProposta.Desclassificada)
        {
            throw new InvalidOperationException("Proposta desclassificada nao pode ser indicada vencedora.");
        }

        if (vencedora.Classificacao is null)
        {
            vencedora.Classificar(1);
        }

        vencedora.MarcarVencedora();
        PropostaVencedoraId = propostaVencedoraId;
        Situacao = SituacaoLicitacao.EmJulgamento;
    }

    /// <summary>
    /// Verifica a habilitacao de um licitante (I-7). Nao altera a situacao do certame.
    /// </summary>
    /// <param name="fornecedorId">Licitante verificado.</param>
    /// <param name="resultado">Resultado da habilitacao.</param>
    /// <param name="motivo">Motivo (opcional).</param>
    /// <exception cref="InvalidOperationException">Se o certame nao estiver Aberto ou EmJulgamento.</exception>
    public void HabilitarLicitante(Guid fornecedorId, ResultadoHabilitacao resultado, string? motivo)
    {
        // I-7: so sobre certame Aberta ou EmJulgamento.
        if (Situacao is not (SituacaoLicitacao.Aberta or SituacaoLicitacao.EmJulgamento))
        {
            throw new InvalidOperationException($"A habilitacao exige certame Aberto ou EmJulgamento. Situacao atual: {Situacao}.");
        }

        _habilitacoes.Add(Habilitacao.Registrar(fornecedorId, resultado, motivo));
    }

    /// <summary>
    /// Homologa o resultado (I-8/I-9): exige EmJulgamento, proposta vencedora definida e vencedor Habilitado.
    /// </summary>
    /// <exception cref="InvalidOperationException">I-8: situacao diferente de EmJulgamento, sem vencedor ou vencedor nao habilitado.</exception>
    public void Homologar()
    {
        // I-8
        if (Situacao != SituacaoLicitacao.EmJulgamento)
        {
            throw new InvalidOperationException($"A homologacao exige certame EmJulgamento. Situacao atual: {Situacao}.");
        }

        if (PropostaVencedoraId is null)
        {
            throw new InvalidOperationException("A homologacao exige proposta vencedora definida.");
        }

        var vencedora = _propostas.FirstOrDefault(p => p.Id.Value == PropostaVencedoraId.Value)
            ?? throw new InvalidOperationException("Proposta vencedora nao pertence ao certame.");

        var fornecedorVencedorId = vencedora.FornecedorId;
        var habilitado = _habilitacoes
            .Where(h => h.FornecedorId == fornecedorVencedorId)
            .OrderBy(_ => 0)
            .LastOrDefault();

        if (habilitado is null || habilitado.Resultado != ResultadoHabilitacao.Habilitado)
        {
            throw new InvalidOperationException("A homologacao exige vencedor Habilitado.");
        }

        // I-9
        Situacao = SituacaoLicitacao.Homologada;
        RaiseDomainEvent(new LicitacaoHomologada(Id, PropostaVencedoraId.Value, fornecedorVencedorId));
    }

    /// <summary>
    /// Encerra o certame por inexistencia de proposta valida/habilitada (I-10/I-11).
    /// </summary>
    /// <param name="motivo">Motivacao do ato.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">I-10/I-11: certame encerrado ou com proposta valida/habilitada.</exception>
    public void DeclararFracassada(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        // I-10
        GarantirNaoEncerrada();
        // I-11: nenhuma proposta valida/habilitada.
        if (ExistePropostaValidaHabilitada())
        {
            throw new InvalidOperationException("Existe proposta valida/habilitada; o certame nao pode ser declarado fracassado.");
        }

        Situacao = SituacaoLicitacao.Fracassada;
        RaiseDomainEvent(new LicitacaoFracassada(Id, motivo));
    }

    /// <summary>
    /// Encerra o certame por ausencia total de interessados (I-10/I-11).
    /// </summary>
    /// <exception cref="InvalidOperationException">I-10/I-11: certame encerrado ou com proposta recebida.</exception>
    public void DeclararDeserta()
    {
        // I-10
        GarantirNaoEncerrada();
        // I-11: ausencia total de proposta recebida.
        if (_propostas.Count > 0)
        {
            throw new InvalidOperationException("Existe proposta recebida; o certame nao pode ser declarado deserto.");
        }

        Situacao = SituacaoLicitacao.Deserta;
        RaiseDomainEvent(new LicitacaoDeserta(Id));
    }

    /// <summary>
    /// Revoga o certame por conveniencia/oportunidade (I-10/I-15).
    /// </summary>
    /// <param name="motivo">Motivacao do ato administrativo.</param>
    /// <exception cref="ArgumentException">I-15: motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">I-10: certame encerrado.</exception>
    public void Revogar(string motivo)
    {
        // I-15
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        // I-10
        GarantirNaoEncerrada();
        Situacao = SituacaoLicitacao.Revogada;
        RaiseDomainEvent(new LicitacaoRevogada(Id, motivo));
    }

    /// <summary>
    /// Anula o certame por ilegalidade (I-10/I-15).
    /// </summary>
    /// <param name="motivo">Motivacao do ato administrativo (vicio de legalidade).</param>
    /// <exception cref="ArgumentException">I-15: motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">I-10: certame encerrado.</exception>
    public void Anular(string motivo)
    {
        // I-15
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        // I-10
        GarantirNaoEncerrada();
        Situacao = SituacaoLicitacao.Anulada;
        RaiseDomainEvent(new LicitacaoAnulada(Id, motivo));
    }

    /// <summary>Fornecedor da proposta vencedora, quando indicada.</summary>
    /// <returns>Identificador do fornecedor vencedor, ou <c>null</c>.</returns>
    public Guid? FornecedorVencedorId()
        => PropostaVencedoraId is null
            ? null
            : _propostas.FirstOrDefault(p => p.Id.Value == PropostaVencedoraId.Value)?.FornecedorId;

    /// <summary>Valor adjudicado (valor da proposta vencedora), quando indicada.</summary>
    /// <returns>Valor adjudicado, ou zero quando nao ha vencedora.</returns>
    public decimal ValorAdjudicado()
        => PropostaVencedoraId is null
            ? 0m
            : _propostas.FirstOrDefault(p => p.Id.Value == PropostaVencedoraId.Value)?.Valor.Valor ?? 0m;

    private bool ExistePropostaValidaHabilitada()
    {
        foreach (var proposta in _propostas)
        {
            if (proposta.Situacao is SituacaoProposta.Desclassificada)
            {
                continue;
            }

            var habilitado = _habilitacoes
                .Any(h => h.FornecedorId == proposta.FornecedorId && h.Resultado == ResultadoHabilitacao.Habilitado);
            var inabilitado = _habilitacoes
                .Any(h => h.FornecedorId == proposta.FornecedorId && h.Resultado == ResultadoHabilitacao.Inabilitado);

            // Proposta valida = nao desclassificada e nao inabilitada (sem habilitacao ainda conta como potencialmente valida).
            if (!inabilitado || habilitado)
            {
                return true;
            }
        }

        return false;
    }

    // I-12: certame encerrado nao admite novas transicoes.
    private void GarantirNaoEncerrada()
    {
        if (Situacao is SituacaoLicitacao.Homologada
            or SituacaoLicitacao.Fracassada
            or SituacaoLicitacao.Deserta
            or SituacaoLicitacao.Revogada
            or SituacaoLicitacao.Anulada)
        {
            throw new InvalidOperationException($"Certame encerrado nao admite novas transicoes. Situacao atual: {Situacao}.");
        }
    }

    private void GarantirEmAndamento()
    {
        if (Situacao is not (SituacaoLicitacao.Aberta or SituacaoLicitacao.EmJulgamento))
        {
            throw new InvalidOperationException($"Operacao exige certame em andamento (Aberta/EmJulgamento). Situacao atual: {Situacao}.");
        }
    }
}

using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

/// <summary>Identificador forte do agregado <see cref="PedidoRequisicao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PedidoRequisicaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PedidoRequisicaoId"/>.</returns>
    public static PedidoRequisicaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Pedido de requisição de material do almoxarifado (fluxo self-service multi-item por setor/UO):
/// o setor solicitante abre o pedido (<see cref="SituacaoPedido.Solicitado"/>), a autoridade aprova
/// (<see cref="SituacaoPedido.Aprovado"/>) e o almoxarifado atende, gerando a SAÍDA de estoque por item
/// (baixa do <see cref="ItemEstoque"/> existente, respeitando o saldo — atendimento parcial permitido),
/// fechando em <see cref="SituacaoPedido.Atendido"/>. Não reimplementa a baixa: orquestra
/// <see cref="ItemEstoque.AtenderRequisicao(RequisicaoId, Guid, decimal, DateOnly)"/> por linha (R-3/R-4).
/// O consumo é reconhecido por setor (a despesa nasce no consumo — Lei 4.320/MCASP, herdado do estoque).
/// </summary>
public sealed class PedidoRequisicao : AggregateRoot<PedidoRequisicaoId>, IMustHaveTenant, IMustHaveUnidade
{
    private readonly List<ItemPedido> _itens = [];

    private PedidoRequisicao()
    {
    }

    private PedidoRequisicao(
        PedidoRequisicaoId id,
        Guid tenantId,
        Guid unidadeId,
        string setorSolicitante,
        Guid solicitanteId,
        DateOnly data,
        string? justificativa)
        : base(id)
    {
        TenantId = tenantId;
        UnidadeId = unidadeId;
        SetorSolicitante = setorSolicitante;
        SolicitanteId = solicitanteId;
        Data = data;
        Justificativa = justificativa;
        Situacao = SituacaoPedido.Solicitado;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Unidade Organizacional (UO) consumidora — escopo de autorização e prestação de contas (M1).</summary>
    public Guid UnidadeId { get; private set; }

    /// <summary>Setor solicitante (rótulo do consumo por setor).</summary>
    public string SetorSolicitante { get; private set; } = string.Empty;

    /// <summary>Servidor solicitante (LGPD — quem pediu).</summary>
    public Guid SolicitanteId { get; private set; }

    /// <summary>Data do pedido.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Justificativa livre do pedido (opcional).</summary>
    public string? Justificativa { get; private set; }

    /// <summary>Situação na máquina de estados do pedido.</summary>
    public SituacaoPedido Situacao { get; private set; }

    /// <summary>Autoridade que aprovou o pedido (nulo enquanto não aprovado).</summary>
    public Guid? AprovadorId { get; private set; }

    /// <summary>Data da aprovação (nula enquanto não aprovado).</summary>
    public DateOnly? DataAprovacao { get; private set; }

    /// <summary>Data do atendimento (nula enquanto não atendido).</summary>
    public DateOnly? DataAtendimento { get; private set; }

    /// <summary>Motivo do cancelamento (nulo se não cancelado).</summary>
    public string? MotivoCancelamento { get; private set; }

    /// <summary>Linhas do pedido (item de estoque × quantidade solicitada/atendida).</summary>
    public IReadOnlyCollection<ItemPedido> Itens => _itens;

    /// <summary>Indica se o pedido está em estado terminal (Atendido/Cancelado), não admitindo mais transições.</summary>
    public bool Terminal => Situacao is SituacaoPedido.Atendido or SituacaoPedido.Cancelado;

    /// <summary>
    /// Abre um pedido de requisição self-service com pelo menos uma linha (situação inicial
    /// <see cref="SituacaoPedido.Solicitado"/>). Reúne as linhas por item de estoque (consolidando
    /// quantidades de itens repetidos), nasce válido (R-1).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="unidadeId">UO consumidora (escopo organizacional, M1).</param>
    /// <param name="setorSolicitante">Setor solicitante (rótulo do consumo).</param>
    /// <param name="solicitanteId">Servidor solicitante.</param>
    /// <param name="data">Data do pedido.</param>
    /// <param name="justificativa">Justificativa opcional.</param>
    /// <param name="linhas">Linhas do pedido (item de estoque, quantidade solicitada).</param>
    /// <returns>Novo <see cref="PedidoRequisicao"/> em situação Solicitado.</returns>
    /// <exception cref="ArgumentException">Se o setor solicitante for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a UO/solicitante não forem informados.</exception>
    /// <exception cref="InvalidOperationException">Se não houver ao menos uma linha (R-1).</exception>
    public static PedidoRequisicao Abrir(
        Guid tenantId,
        Guid unidadeId,
        string setorSolicitante,
        Guid solicitanteId,
        DateOnly data,
        string? justificativa,
        IEnumerable<(ItemEstoqueId ItemEstoqueId, decimal Quantidade)> linhas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(setorSolicitante);
        ArgumentNullException.ThrowIfNull(linhas);
        if (unidadeId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(unidadeId), "O pedido exige a UO consumidora.");
        }

        if (solicitanteId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(solicitanteId), "O pedido exige o solicitante.");
        }

        var pedido = new PedidoRequisicao(
            PedidoRequisicaoId.New(), tenantId, unidadeId, setorSolicitante, solicitanteId, data, justificativa);

        // R-1: consolida itens repetidos somando quantidades; pelo menos uma linha.
        var consolidadas = linhas
            .GroupBy(linha => linha.ItemEstoqueId)
            .Select(grupo => (ItemEstoqueId: grupo.Key, Quantidade: grupo.Sum(item => item.Quantidade)))
            .ToList();

        if (consolidadas.Count == 0)
        {
            throw new InvalidOperationException("O pedido de requisição exige ao menos uma linha de item.");
        }

        foreach (var (itemEstoqueId, quantidade) in consolidadas)
        {
            pedido._itens.Add(ItemPedido.Criar(itemEstoqueId, quantidade));
        }

        pedido.RaiseDomainEvent(new PedidoRequisicaoAberto(pedido.Id, unidadeId, setorSolicitante, consolidadas.Count));
        return pedido;
    }

    /// <summary>
    /// Aprova o pedido (Solicitado → Aprovado) — habilita o atendimento. A autorização propriamente dita
    /// (RBAC/escopo do setor) é validada na camada de aplicação; aqui guarda-se a invariante de transição (R-2).
    /// </summary>
    /// <param name="aprovadorId">Autoridade que aprova.</param>
    /// <param name="data">Data da aprovação.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o aprovador não for informado.</exception>
    /// <exception cref="InvalidOperationException">Se o pedido não estiver em Solicitado (R-2).</exception>
    public void Aprovar(Guid aprovadorId, DateOnly data)
    {
        if (aprovadorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(aprovadorId), "A aprovação exige a autoridade aprovadora.");
        }

        // R-2: só se aprova um pedido em Solicitado (transições fora da ordem são bloqueadas).
        if (Situacao != SituacaoPedido.Solicitado)
        {
            throw new InvalidOperationException(
                $"Só é possível aprovar um pedido em Solicitado. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoPedido.Aprovado;
        AprovadorId = aprovadorId;
        DataAprovacao = data;
        RaiseDomainEvent(new PedidoRequisicaoAprovado(Id, aprovadorId));
    }

    /// <summary>
    /// Registra o atendimento de uma linha do pedido após a baixa efetiva no estoque (R-3/R-4): exige o
    /// pedido Aprovado, acumula a quantidade atendida na linha (atendimento parcial permitido) e não muta o
    /// estoque (a baixa é feita pelo agregado <see cref="ItemEstoque"/> na mesma transação, orquestrada na aplicação).
    /// </summary>
    /// <param name="itemPedidoId">Linha do pedido atendida.</param>
    /// <param name="quantidadeAtendida">Quantidade efetivamente baixada do estoque.</param>
    /// <exception cref="InvalidOperationException">Se o pedido não estiver Aprovado ou a linha não existir.</exception>
    public void RegistrarAtendimentoDeLinha(ItemPedidoId itemPedidoId, decimal quantidadeAtendida)
    {
        if (Situacao != SituacaoPedido.Aprovado)
        {
            throw new InvalidOperationException(
                $"O atendimento exige um pedido Aprovado. Situação atual: {Situacao}.");
        }

        var linha = _itens.FirstOrDefault(item => item.Id == itemPedidoId)
            ?? throw new InvalidOperationException("Linha do pedido não encontrada.");

        linha.RegistrarAtendimento(quantidadeAtendida);
    }

    /// <summary>
    /// Conclui o atendimento do pedido (Aprovado → Atendido) após o registro das baixas de todas as linhas
    /// atendíveis. Terminal de sucesso; exige que ao menos uma linha tenha sido atendida (R-4).
    /// </summary>
    /// <param name="data">Data do atendimento.</param>
    /// <exception cref="InvalidOperationException">Se o pedido não estiver Aprovado ou nada tiver sido atendido.</exception>
    public void ConcluirAtendimento(DateOnly data)
    {
        if (Situacao != SituacaoPedido.Aprovado)
        {
            throw new InvalidOperationException(
                $"A conclusão do atendimento exige um pedido Aprovado. Situação atual: {Situacao}.");
        }

        var totalAtendido = _itens.Sum(item => item.QuantidadeAtendida);
        // R-4: não se fecha um atendimento sem qualquer baixa (saldo insuficiente em todas as linhas).
        if (totalAtendido <= 0m)
        {
            throw new InvalidOperationException("Nenhuma quantidade foi atendida — atendimento não pode ser concluído.");
        }

        Situacao = SituacaoPedido.Atendido;
        DataAtendimento = data;

        var totalmenteAtendido = _itens.All(item => item.TotalmenteAtendido);
        RaiseDomainEvent(new PedidoRequisicaoAtendido(Id, UnidadeId, SetorSolicitante, totalmenteAtendido));
    }

    /// <summary>
    /// Cancela o pedido (Solicitado/Aprovado → Cancelado), terminal sem efeito de estoque. Não é possível
    /// cancelar um pedido já atendido (a saída de estoque é irreversível por aqui — usar estorno/entrada) (R-5).
    /// </summary>
    /// <param name="motivo">Motivo do cancelamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o pedido estiver em estado terminal (R-5).</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        // R-5: pedido terminal (Atendido/Cancelado) não admite cancelamento.
        if (Terminal)
        {
            throw new InvalidOperationException(
                $"Pedido em estado terminal não admite cancelamento. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoPedido.Cancelado;
        MotivoCancelamento = motivo;
    }
}

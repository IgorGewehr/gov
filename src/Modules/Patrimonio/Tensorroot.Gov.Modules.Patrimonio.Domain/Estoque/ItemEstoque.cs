using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

/// <summary>Identificador forte do agregado <see cref="ItemEstoque"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemEstoqueId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemEstoqueId"/>.</returns>
    public static ItemEstoqueId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Item de consumo do almoxarifado, controlado por saldo, lotes e movimentos (entrada/saída)
/// e requisições. Mensurado pelo menor entre custo e valor realizável líquido (NBC TSP 12),
/// com custeio PEPS ou médio; a despesa é reconhecida no consumo (Lei 4.320 / MCASP).
/// </summary>
public sealed class ItemEstoque : AggregateRoot<ItemEstoqueId>, IMustHaveTenant
{
    private readonly List<Lote> _lotes = [];
    private readonly List<MovimentoEstoque> _movimentos = [];
    private readonly List<Requisicao> _requisicoes = [];

    private ItemEstoque()
    {
    }

    private ItemEstoque(
        ItemEstoqueId id,
        Guid tenantId,
        string codigo,
        string descricao,
        string unidadeMedida,
        MetodoCusteio metodoCusteio,
        PontoPedido pontoPedido,
        CurvaABC classificacaoAbc)
        : base(id)
    {
        TenantId = tenantId;
        Codigo = codigo;
        Descricao = descricao;
        UnidadeMedida = unidadeMedida;
        MetodoCusteio = metodoCusteio;
        PontoPedido = pontoPedido;
        ClassificacaoAbc = classificacaoAbc;
        Saldo = SaldoAlmoxarifado.Zero;
        CustoMedio = ValorMonetario.Zero;
        Situacao = SituacaoItemEstoque.Ativo;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Código do item no catálogo de almoxarifado.</summary>
    public string Codigo { get; private set; } = string.Empty;

    /// <summary>Descrição do item.</summary>
    public string Descricao { get; private set; } = string.Empty;

    /// <summary>Unidade de medida (un, kg, cx, L).</summary>
    public string UnidadeMedida { get; private set; } = string.Empty;

    /// <summary>Quantidade total disponível (Σ saldo dos lotes).</summary>
    public SaldoAlmoxarifado Saldo { get; private set; }

    /// <summary>Saldo mínimo de reposição.</summary>
    public PontoPedido PontoPedido { get; private set; }

    /// <summary>Método de custeio da saída: PEPS ou médio.</summary>
    public MetodoCusteio MetodoCusteio { get; private set; }

    /// <summary>Custo médio ponderado atual (quando custeio médio).</summary>
    public ValorMonetario CustoMedio { get; private set; } = ValorMonetario.Zero;

    /// <summary>Valor realizável líquido informado para o teste do menor entre custo e VRL (I-2).</summary>
    public ValorMonetario? ValorRealizavelLiquido { get; private set; }

    /// <summary>Classificação na Curva ABC.</summary>
    public CurvaABC ClassificacaoAbc { get; private set; }

    /// <summary>Situação do item (ativo/inativo).</summary>
    public SituacaoItemEstoque Situacao { get; private set; }

    /// <summary>Lotes do item (ordenados pelo PEPS por data de entrada).</summary>
    public IReadOnlyCollection<Lote> Lotes => _lotes;

    /// <summary>Entradas e saídas do item.</summary>
    public IReadOnlyCollection<MovimentoEstoque> Movimentos => _movimentos;

    /// <summary>Requisições atendidas/pendentes do item.</summary>
    public IReadOnlyCollection<Requisicao> Requisicoes => _requisicoes;

    /// <summary>Indica se o item está movimentável (situação ativa, I-9).</summary>
    public bool Movimentavel => Situacao == SituacaoItemEstoque.Ativo;

    /// <summary>
    /// Cadastra um novo item de estoque no almoxarifado: nasce ativo, com saldo zero (I-10).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigo">Código do item.</param>
    /// <param name="descricao">Descrição.</param>
    /// <param name="unidadeMedida">Unidade de medida.</param>
    /// <param name="metodoCusteio">Método de custeio (PEPS/médio).</param>
    /// <param name="pontoPedido">Ponto de pedido (saldo-gatilho de reposição).</param>
    /// <param name="classificacaoAbc">Classe ABC.</param>
    /// <returns>Novo <see cref="ItemEstoque"/>.</returns>
    /// <exception cref="ArgumentException">Se algum campo obrigatório for inválido.</exception>
    public static ItemEstoque Cadastrar(
        Guid tenantId,
        string codigo,
        string descricao,
        string unidadeMedida,
        MetodoCusteio metodoCusteio,
        PontoPedido pontoPedido,
        CurvaABC classificacaoAbc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeMedida);

        return new ItemEstoque(
            ItemEstoqueId.New(),
            tenantId,
            codigo,
            descricao,
            unidadeMedida,
            metodoCusteio,
            pontoPedido,
            classificacaoAbc);
    }

    /// <summary>
    /// Registra a entrada de itens: cria um lote (PEPS), recalcula o custo médio ponderado (médio)
    /// e incrementa o saldo — sem reconhecer despesa (I-4/I-5).
    /// </summary>
    /// <param name="quantidade">Quantidade que ingressa (estritamente positiva, I-11).</param>
    /// <param name="custoUnitario">Custo unitário de entrada.</param>
    /// <param name="dataEntrada">Data da entrada.</param>
    /// <param name="validade">Validade opcional do lote.</param>
    /// <param name="documento">Documento de respaldo (NF/recebimento).</param>
    /// <exception cref="ArgumentNullException">Se o custo unitário for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se o item estiver inativo (I-9).</exception>
    public void RegistrarEntrada(
        decimal quantidade,
        ValorMonetario custoUnitario,
        DateOnly dataEntrada,
        DateOnly? validade,
        string documento)
    {
        ArgumentNullException.ThrowIfNull(custoUnitario);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        GarantirMovimentavel();

        // I-5 (médio): custo médio ponderado = (saldo×médio + qtd×custo) / (saldo+qtd), antes de incrementar.
        if (MetodoCusteio == MetodoCusteio.Medio)
        {
            var saldoAtual = Saldo.Quantidade;
            var totalAtual = CustoMedio.Valor * saldoAtual;
            var totalEntrada = custoUnitario.Valor * quantidade;
            var novaQuantidade = saldoAtual + quantidade;
            var novoMedio = novaQuantidade > 0m ? (totalAtual + totalEntrada) / novaQuantidade : 0m;
            CustoMedio = ValorMonetario.De(novoMedio);
        }

        var lote = Lote.Registrar(custoUnitario, quantidade, dataEntrada, validade);
        _lotes.Add(lote);

        Saldo = Saldo.Acrescentar(quantidade);
        _movimentos.Add(MovimentoEstoque.Entrada(quantidade, custoUnitario, dataEntrada, documento));
    }

    /// <summary>
    /// Atende uma requisição de material: verifica o saldo (I-1), valora pelo custeio (I-3/I-8),
    /// gera o movimento de saída, reduz o saldo, reconhece a despesa no consumo (I-4/I-7) e,
    /// se o saldo resultante atingir o ponto de pedido, sinaliza a reposição (I-6).
    /// </summary>
    /// <param name="requisicaoId">Identificador da requisição.</param>
    /// <param name="solicitanteId">Solicitante.</param>
    /// <param name="quantidade">Quantidade requisitada (estritamente positiva, I-11).</param>
    /// <param name="data">Data do atendimento.</param>
    /// <returns>O custo total da saída valorado pelo método de custeio.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se o item estiver inativo (I-9) ou o saldo for insuficiente (I-1).</exception>
    public ValorMonetario AtenderRequisicao(
        RequisicaoId requisicaoId,
        Guid solicitanteId,
        decimal quantidade,
        DateOnly data)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        GarantirMovimentavel();

        // I-1: saldo suficiente verificado ANTES de valorar e baixar.
        if (quantidade > Saldo.Quantidade)
        {
            throw new InvalidOperationException("Saldo insuficiente para atender a requisição.");
        }

        var custoTotal = ValorarSaida(quantidade);
        var valorUnitario = ValorMonetario.De(quantidade > 0m ? custoTotal.Valor / quantidade : 0m);

        var requisicao = Requisicao.Abrir(requisicaoId, solicitanteId, quantidade, data);
        requisicao.Atender();
        _requisicoes.Add(requisicao);

        _movimentos.Add(MovimentoEstoque.Saida(quantidade, valorUnitario, data, requisicaoId.ToString()));
        Saldo = Saldo.Baixar(quantidade);

        RaiseDomainEvent(new RequisicaoAtendida(Id, requisicaoId, quantidade));

        // I-6: após a baixa, se o saldo atinge/fica abaixo do ponto de pedido, dispara reposição.
        if (PontoPedido.FoiAtingidoPor(Saldo))
        {
            RaiseDomainEvent(new PontoPedidoAtingido(Id, Saldo.Quantidade));
        }

        return custoTotal;
    }

    /// <summary>
    /// Ajusta o valor realizável líquido e aplica o menor entre custo e VRL (I-2; NBC TSP 12).
    /// </summary>
    /// <param name="valorRealizavelLiquido">VRL informado (maior ou igual a zero).</param>
    /// <param name="data">Data do ajuste de mensuração.</param>
    /// <exception cref="ArgumentNullException">Se o VRL for nulo.</exception>
    public void AjustarValorRealizavelLiquido(ValorMonetario valorRealizavelLiquido, DateOnly data)
    {
        ArgumentNullException.ThrowIfNull(valorRealizavelLiquido);
        _ = data;
        ValorRealizavelLiquido = valorRealizavelLiquido;

        // I-2: quando VRL < custo médio, a mensuração do item adota o VRL (redução ao valor realizável).
        if (valorRealizavelLiquido.MenorQue(CustoMedio))
        {
            CustoMedio = valorRealizavelLiquido;
        }
    }

    /// <summary>Reclassifica o item na Curva ABC (A/B/C).</summary>
    /// <param name="classificacaoAbc">Nova classe.</param>
    public void ReclassificarAbc(CurvaABC classificacaoAbc) => ClassificacaoAbc = classificacaoAbc;

    /// <summary>Inativa o item — só permitido com saldo zero (não se inativa item com estoque).</summary>
    /// <exception cref="InvalidOperationException">Se já estiver inativo ou houver saldo remanescente.</exception>
    public void Inativar()
    {
        if (Situacao == SituacaoItemEstoque.Inativo)
        {
            throw new InvalidOperationException("Item já está inativo.");
        }

        if (Saldo.Quantidade > 0m)
        {
            throw new InvalidOperationException("Não é possível inativar item com saldo remanescente.");
        }

        Situacao = SituacaoItemEstoque.Inativo;
    }

    private ValorMonetario ValorarSaida(decimal quantidade)
        => MetodoCusteio == MetodoCusteio.Peps
            ? ValorarPeps(quantidade)
            : CustoMedio.Multiplicar(quantidade);

    private ValorMonetario ValorarPeps(decimal quantidade)
    {
        // I-8: consome os lotes mais antigos primeiro; custo = soma ponderada das parcelas.
        var restante = quantidade;
        var custo = ValorMonetario.Zero;

        foreach (var lote in _lotes.Where(lote => lote.TemSaldo).OrderBy(lote => lote.DataEntrada))
        {
            if (restante <= 0m)
            {
                break;
            }

            var consumida = lote.Consumir(restante);
            custo = custo.Somar(lote.CustoUnitario.Multiplicar(consumida));
            restante -= consumida;
        }

        return custo;
    }

    private void GarantirMovimentavel()
    {
        if (!Movimentavel)
        {
            throw new InvalidOperationException($"Item inativo não admite movimentação. Situação atual: {Situacao}.");
        }
    }
}

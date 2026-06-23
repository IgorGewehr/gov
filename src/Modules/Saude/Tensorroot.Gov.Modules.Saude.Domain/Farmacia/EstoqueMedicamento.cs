using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

/// <summary>
/// Posicao de estoque de UM medicamento em UM estabelecimento (UBS/farmacia/CAF), com saldo total e
/// lotes com validade. Raiz de agregado e fronteira de consistencia do saldo: entrada cria/reforca
/// lote; saida consome por FEFO (First-Expired, First-Out) ignorando lotes vencidos; estorno devolve.
/// Saldo nunca negativo (I-FARM-1). Espelha o motor de saldo/lote do <c>ItemEstoque</c> de Patrimonio,
/// porem REPLICADO no BC Saude (isolamento de Bounded Context — CLAUDE.md §2): medicamento controlado
/// tem regras proprias que nao pertencem ao almoxarifado geral.
/// </summary>
public sealed class EstoqueMedicamento : AggregateRoot<EstoqueMedicamentoId>, IMustHaveTenant
{
    private readonly List<LoteMedicamento> _lotes = [];

    private EstoqueMedicamento()
    {
    }

    private EstoqueMedicamento(
        EstoqueMedicamentoId id,
        Guid tenantId,
        EstabelecimentoId estabelecimentoId,
        MedicamentoId medicamentoId,
        int pontoDeRessuprimento)
        : base(id)
    {
        TenantId = tenantId;
        EstabelecimentoId = estabelecimentoId;
        MedicamentoId = medicamentoId;
        PontoDeRessuprimento = pontoDeRessuprimento;
        Saldo = 0m;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Estabelecimento (CNES) detentor do estoque (referencia por Id).</summary>
    public EstabelecimentoId EstabelecimentoId { get; private set; }

    /// <summary>Medicamento (catalogo) do estoque (referencia por Id).</summary>
    public MedicamentoId MedicamentoId { get; private set; }

    /// <summary>Saldo total agregado (soma dos saldos dos lotes) — nunca negativo (I-FARM-1).</summary>
    public decimal Saldo { get; private set; }

    /// <summary>Ponto de ressuprimento (quantidade minima abaixo da qual ha alerta de ruptura).</summary>
    public int PontoDeRessuprimento { get; private set; }

    /// <summary>Lotes do estoque (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<LoteMedicamento> Lotes => _lotes;

    /// <summary>Abre a posicao de estoque (saldo zero) de um medicamento num estabelecimento.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="medicamentoId">Medicamento (catalogo).</param>
    /// <param name="pontoDeRessuprimento">Ponto de ressuprimento (>= 0).</param>
    /// <returns>Novo <see cref="EstoqueMedicamento"/> com saldo zero.</returns>
    /// <exception cref="ArgumentException">Se algum identificador de referencia for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o ponto de ressuprimento for negativo.</exception>
    public static EstoqueMedicamento Abrir(
        Guid tenantId,
        EstabelecimentoId estabelecimentoId,
        MedicamentoId medicamentoId,
        int pontoDeRessuprimento = 0)
    {
        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento (CNES) e obrigatorio.", nameof(estabelecimentoId));
        }

        if (medicamentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Medicamento e obrigatorio.", nameof(medicamentoId));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(pontoDeRessuprimento);

        return new EstoqueMedicamento(EstoqueMedicamentoId.New(), tenantId, estabelecimentoId, medicamentoId, pontoDeRessuprimento);
    }

    /// <summary>
    /// Registra a entrada de um lote (compra/transferencia/doacao). Se ja existir lote com mesmo numero
    /// E mesma validade, reforca o saldo; caso contrario, cria um novo lote. Rejeita lote ja vencido
    /// (I-FARM-2). Emite <see cref="EntradaMedicamentoRegistrada"/>.
    /// </summary>
    /// <param name="numeroLote">Numero do lote (do fabricante).</param>
    /// <param name="validade">Validade do lote.</param>
    /// <param name="quantidade">Quantidade recebida (> 0).</param>
    /// <param name="hoje">Data corrente (para validar a validade — nunca hardcoded).</param>
    /// <exception cref="ArgumentException">Se o numero do lote for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se o lote ja estiver vencido na entrada.</exception>
    public void RegistrarEntrada(string numeroLote, DateOnly validade, decimal quantidade, DateOnly hoje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroLote);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantidade, 0m);

        // I-FARM-2: nao entra lote vencido no estoque.
        if (validade < hoje)
        {
            throw new InvalidOperationException("Nao e permitido dar entrada em lote ja vencido.");
        }

        var numero = numeroLote.Trim();
        var existente = _lotes.FirstOrDefault(l => l.NumeroLote == numero && l.Validade == validade);
        if (existente is not null)
        {
            existente.Reforcar(quantidade);
        }
        else
        {
            _lotes.Add(LoteMedicamento.Criar(numero, validade, quantidade));
        }

        Saldo += quantidade;
        RaiseDomainEvent(new EntradaMedicamentoRegistrada(Id, MedicamentoId, EstabelecimentoId, numero, quantidade));
    }

    /// <summary>
    /// Baixa uma quantidade do estoque por FEFO (consome primeiro os lotes que vencem antes), ignorando
    /// lotes vencidos. Exige saldo valido suficiente (I-FARM-1/I-FARM-3). Retorna o rastro das baixas por
    /// lote (rastreabilidade SNGPC). Usado pela dispensacao e pela aplicacao de imunobiologico.
    /// </summary>
    /// <param name="quantidade">Quantidade a baixar (> 0).</param>
    /// <param name="hoje">Data corrente (para ignorar lotes vencidos).</param>
    /// <returns>Lista das baixas por lote (lote, validade, quantidade retirada).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se nao houver saldo valido (nao vencido) suficiente.</exception>
    public IReadOnlyList<BaixaLote> Baixar(decimal quantidade, DateOnly hoje)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantidade, 0m);

        var disponiveis = _lotes
            .Where(l => l.Saldo > 0m && !l.EstaVencido(hoje))
            .OrderBy(l => l.Validade)
            .ToList();

        var saldoValido = disponiveis.Sum(l => l.Saldo);
        if (saldoValido < quantidade)
        {
            throw new InvalidOperationException(
                "Saldo valido (nao vencido) insuficiente para a baixa solicitada.");
        }

        var baixas = new List<BaixaLote>();
        var restante = quantidade;
        foreach (var lote in disponiveis)
        {
            if (restante <= 0m)
            {
                break;
            }

            var consumir = Math.Min(restante, lote.Saldo);
            lote.Consumir(consumir);
            Saldo -= consumir;
            restante -= consumir;
            baixas.Add(new BaixaLote(lote.Id, lote.NumeroLote, lote.Validade, consumir));
        }

        return baixas;
    }

    /// <summary>
    /// Devolve (estorna) ao estoque as baixas de uma dispensacao estornada, recompondo cada lote pelo Id.
    /// Lotes ja inexistentes (purga) sao ignorados com seguranca, mantendo o saldo total coerente.
    /// </summary>
    /// <param name="baixas">Baixas a estornar (lote + quantidade).</param>
    public void Estornar(IEnumerable<BaixaLote> baixas)
    {
        ArgumentNullException.ThrowIfNull(baixas);
        foreach (var baixa in baixas)
        {
            var lote = _lotes.FirstOrDefault(l => l.Id == baixa.LoteId);
            lote?.Devolver(baixa.Quantidade);
            Saldo += baixa.Quantidade;
        }
    }

    /// <summary>Saldo valido (nao vencido) na data de referencia — base do alerta de ruptura.</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns>Soma dos saldos dos lotes nao vencidos.</returns>
    public decimal SaldoValido(DateOnly hoje) => _lotes.Where(l => !l.EstaVencido(hoje)).Sum(l => l.Saldo);

    /// <summary>Indica se o saldo valido esta abaixo do ponto de ressuprimento (alerta de ruptura).</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se em ruptura.</returns>
    public bool EmRuptura(DateOnly hoje) => SaldoValido(hoje) < PontoDeRessuprimento;

    /// <summary>Ajusta o ponto de ressuprimento (parametro operacional).</summary>
    /// <param name="pontoDeRessuprimento">Novo ponto (>= 0).</param>
    public void DefinirPontoDeRessuprimento(int pontoDeRessuprimento)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pontoDeRessuprimento);
        PontoDeRessuprimento = pontoDeRessuprimento;
    }
}

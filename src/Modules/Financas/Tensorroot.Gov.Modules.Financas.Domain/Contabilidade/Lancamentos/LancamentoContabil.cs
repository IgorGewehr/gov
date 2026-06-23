using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;

/// <summary>Identificador forte do agregado <see cref="LancamentoContabil"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LancamentoContabilId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LancamentoContabilId"/>.</returns>
    public static LancamentoContabilId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Origem do lançamento contábil.</summary>
public enum OrigemLancamento
{
    /// <summary>Gerado automaticamente por um fato do ciclo (evento de domínio).</summary>
    EventoAutomatico = 1,

    /// <summary>Lançamento manual do contador.</summary>
    Manual = 2,

    /// <summary>Estorno de outro lançamento.</summary>
    Estorno = 3,

    /// <summary>Lançamento de encerramento de exercício.</summary>
    Encerramento = 4,
}

/// <summary>
/// Linha de entrada do factory de lançamento (conta resolvida + lado + valor). Carrega os snapshots
/// necessários (código, natureza, tipo) para validar as invariantes sem novos carregamentos.
/// </summary>
/// <param name="ContaId">Conta movimentada.</param>
/// <param name="Codigo">Código contábil da conta.</param>
/// <param name="NaturezaInformacao">Natureza da informação da conta.</param>
/// <param name="Tipo">Tipo (Sintética/Analítica) da conta.</param>
/// <param name="Lado">Lado (débito/crédito).</param>
/// <param name="Valor">Valor da partida.</param>
public readonly record struct LinhaLancamento(
    ContaContabilId ContaId,
    CodigoContabil Codigo,
    NaturezaInformacao NaturezaInformacao,
    TipoConta Tipo,
    LadoPartida Lado,
    ValorMonetario Valor);

/// <summary>
/// Lançamento contábil pela técnica das partidas dobradas. Nasce balanceado e homogêneo (mesma
/// natureza de informação) — invariantes verificadas no factory antes de qualquer evento. É imutável
/// após o registro; correção se dá por <see cref="Estornar"/> (gera lançamento inverso).
/// </summary>
public sealed class LancamentoContabil : AggregateRoot<LancamentoContabilId>, IMustHaveTenant
{
    private readonly List<PartidaContabil> _partidas = [];

    private LancamentoContabil()
    {
    }

    private LancamentoContabil(
        LancamentoContabilId id,
        Guid tenantId,
        DateOnly data,
        int exercicio,
        int periodoMes,
        string historico,
        OrigemLancamento origem,
        NaturezaInformacao naturezaInformacao,
        Guid? origemReferenciaId,
        Guid? eventoContabilId)
        : base(id)
    {
        TenantId = tenantId;
        Data = data;
        Exercicio = exercicio;
        PeriodoMes = periodoMes;
        Historico = historico;
        Origem = origem;
        NaturezaInformacao = naturezaInformacao;
        OrigemReferenciaId = origemReferenciaId;
        EventoContabilId = eventoContabilId;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Data do lançamento.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Exercício contábil.</summary>
    public int Exercicio { get; private set; }

    /// <summary>
    /// Mês do período para o balancete/MSC. Em geral 1-12 (derivado da data). Períodos
    /// extraordinários de encerramento usam <c>13</c> (apuração patrimonial/orçamentária no
    /// 31/12) e <c>0</c> (abertura do exercício seguinte, 01/01) — ver DESIGN encerramento §2.
    /// </summary>
    public int PeriodoMes { get; private set; }

    /// <summary>Histórico (narrativa do fato).</summary>
    public string Historico { get; private set; } = default!;

    /// <summary>Origem do lançamento.</summary>
    public OrigemLancamento Origem { get; private set; }

    /// <summary>Natureza da informação homogênea do lançamento.</summary>
    public NaturezaInformacao NaturezaInformacao { get; private set; }

    /// <summary>Identificador do fato/evento orçamentário que originou o lançamento (idempotência).</summary>
    public Guid? OrigemReferenciaId { get; private set; }

    /// <summary>Roteiro (evento contábil) aplicado, se houver.</summary>
    public Guid? EventoContabilId { get; private set; }

    /// <summary>Indica se o lançamento foi estornado.</summary>
    public bool Estornado { get; private set; }

    /// <summary>Lançamento de estorno cruzado (no original) ou original (no estorno).</summary>
    public LancamentoContabilId? LancamentoEstornoId { get; private set; }

    /// <summary>Partidas (somente leitura).</summary>
    public IReadOnlyCollection<PartidaContabil> Partidas => _partidas.AsReadOnly();

    /// <summary>
    /// Registra um lançamento contábil homogêneo e balanceado.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="data">Data do lançamento.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="historico">Histórico.</param>
    /// <param name="origem">Origem.</param>
    /// <param name="origemReferenciaId">Id do fato de origem (idempotência).</param>
    /// <param name="eventoContabilId">Roteiro aplicado.</param>
    /// <param name="linhas">Partidas (≥1 débito e ≥1 crédito, mesma natureza, ΣD=ΣC).</param>
    /// <param name="periodoAberto">Indica se o período (exercício/mês) está aberto.</param>
    /// <param name="periodoMesOverride">
    /// Sobrepõe o mês do período (default = <c>data.Month</c>). Usado SOMENTE pelos períodos
    /// extraordinários do encerramento: <c>13</c> (apuração) e <c>0</c> (abertura). Aceita 0-13.
    /// </param>
    /// <returns>Novo <see cref="LancamentoContabil"/>.</returns>
    /// <exception cref="PartidaDobradaDesbalanceadaException">Se ΣD ≠ ΣC.</exception>
    /// <exception cref="NaturezasMisturadasException">Se cruzar naturezas.</exception>
    /// <exception cref="ContaNaoAnaliticaException">Se alguma conta não for analítica.</exception>
    /// <exception cref="PeriodoContabilFechadoException">Se o período estiver fechado.</exception>
    public static LancamentoContabil Registrar(
        Guid tenantId,
        DateOnly data,
        int exercicio,
        string historico,
        OrigemLancamento origem,
        Guid? origemReferenciaId,
        Guid? eventoContabilId,
        IReadOnlyCollection<LinhaLancamento> linhas,
        bool periodoAberto,
        int? periodoMesOverride = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(historico);
        ArgumentNullException.ThrowIfNull(linhas);

        var periodoMes = periodoMesOverride ?? data.Month;
        if (periodoMes is < 0 or > 13)
        {
            throw new ArgumentOutOfRangeException(nameof(periodoMesOverride), periodoMes, "Periodo do balancete deve estar entre 0 e 13.");
        }

        if (!periodoAberto)
        {
            throw new PeriodoContabilFechadoException(exercicio, data.Month);
        }

        ValidarComposicao(linhas);
        var natureza = ValidarNaturezaHomogenea(linhas);
        ValidarBalanceamento(linhas);

        var lancamento = new LancamentoContabil(
            LancamentoContabilId.New(),
            tenantId,
            data,
            exercicio,
            periodoMes,
            historico,
            origem,
            natureza,
            origemReferenciaId,
            eventoContabilId);

        foreach (var linha in linhas)
        {
            lancamento._partidas.Add(PartidaContabil.Criar(
                linha.ContaId,
                linha.Codigo.Codigo,
                linha.NaturezaInformacao,
                linha.Lado,
                linha.Valor));
        }

        lancamento.RaiseDomainEvent(new LancamentoContabilRegistrado(lancamento.Id, exercicio, lancamento.PeriodoMes));
        return lancamento;
    }

    /// <summary>
    /// Gera um lançamento de estorno com lados invertidos e marca este como estornado, preservando
    /// a trilha de auditoria imutável (CLAUDE.md §4). Nada é apagado.
    /// </summary>
    /// <param name="data">Data do estorno.</param>
    /// <param name="historico">Histórico do estorno.</param>
    /// <param name="periodoAberto">Indica se o período está aberto.</param>
    /// <returns>Novo lançamento de estorno.</returns>
    /// <exception cref="InvalidOperationException">Se já estornado.</exception>
    public LancamentoContabil Estornar(DateOnly data, string historico, bool periodoAberto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(historico);
        if (Estornado)
        {
            throw new InvalidOperationException("Lancamento ja estornado.");
        }

        var linhasInvertidas = _partidas
            .Select(p => new LinhaLancamento(
                p.ContaId,
                CodigoContabil.De(p.CodigoConta),
                p.NaturezaInformacao,
                TipoConta.Analitica,
                p.Lado == LadoPartida.Debito ? LadoPartida.Credito : LadoPartida.Debito,
                p.Valor))
            .ToList();

        var estorno = Registrar(
            TenantId,
            data,
            Exercicio,
            historico,
            OrigemLancamento.Estorno,
            OrigemReferenciaId,
            EventoContabilId,
            linhasInvertidas,
            periodoAberto);

        estorno.LancamentoEstornoId = Id;
        Estornado = true;
        LancamentoEstornoId = estorno.Id;
        RaiseDomainEvent(new LancamentoContabilEstornado(Id, estorno.Id));
        return estorno;
    }

    private static void ValidarComposicao(IReadOnlyCollection<LinhaLancamento> linhas)
    {
        if (linhas.Count < 2)
        {
            throw new RoteiroContabilInvalidoException("Lancamento exige ao menos 2 partidas.");
        }

        if (!linhas.Any(l => l.Lado == LadoPartida.Debito) || !linhas.Any(l => l.Lado == LadoPartida.Credito))
        {
            throw new RoteiroContabilInvalidoException("Lancamento exige ao menos um debito e um credito.");
        }

        foreach (var linha in linhas)
        {
            if (linha.Tipo != TipoConta.Analitica)
            {
                throw new ContaNaoAnaliticaException(linha.Codigo.Codigo);
            }
        }
    }

    private static NaturezaInformacao ValidarNaturezaHomogenea(IReadOnlyCollection<LinhaLancamento> linhas)
    {
        var natureza = linhas.First().NaturezaInformacao;
        if (linhas.Any(l => l.NaturezaInformacao != natureza))
        {
            throw new NaturezasMisturadasException();
        }

        return natureza;
    }

    private static void ValidarBalanceamento(IReadOnlyCollection<LinhaLancamento> linhas)
    {
        var debitos = linhas.Where(l => l.Lado == LadoPartida.Debito)
            .Aggregate(ValorMonetario.Zero, (acc, l) => acc.Somar(l.Valor));
        var creditos = linhas.Where(l => l.Lado == LadoPartida.Credito)
            .Aggregate(ValorMonetario.Zero, (acc, l) => acc.Somar(l.Valor));

        if (debitos.Valor != creditos.Valor)
        {
            throw new PartidaDobradaDesbalanceadaException(debitos.Valor, creditos.Valor);
        }
    }
}

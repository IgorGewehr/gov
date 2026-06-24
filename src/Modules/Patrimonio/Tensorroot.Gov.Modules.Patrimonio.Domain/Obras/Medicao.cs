using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>Identificador forte da entidade filha <see cref="Medicao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MedicaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MedicaoId"/>.</returns>
    public static MedicaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade filha <see cref="ItemMedicao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemMedicaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemMedicaoId"/>.</returns>
    public static ItemMedicaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha de uma <see cref="Medicao"/>: o avanço atribuído a uma etapa do cronograma no período (avanço
/// físico × valor). Persistida com a medição para que a aprovação reconheça os avanços nas etapas (I-5)
/// de forma reprodutível, mesmo após recarga do agregado. Entidade filha imutável.
/// </summary>
public sealed class ItemMedicao : Entity<ItemMedicaoId>
{
    private ItemMedicao()
    {
    }

    private ItemMedicao(ItemMedicaoId id, EtapaCronogramaId etapaId, decimal percentualFisicoNoPeriodo, ValorMonetario valorNoPeriodo)
        : base(id)
    {
        EtapaId = etapaId;
        PercentualFisicoNoPeriodo = percentualFisicoNoPeriodo;
        ValorNoPeriodo = valorNoPeriodo;
    }

    /// <summary>Etapa medida.</summary>
    public EtapaCronogramaId EtapaId { get; private set; }

    /// <summary>Avanço físico da etapa no período (0–100).</summary>
    public decimal PercentualFisicoNoPeriodo { get; private set; }

    /// <summary>Valor medido da etapa no período.</summary>
    public ValorMonetario ValorNoPeriodo { get; private set; } = default!;

    /// <summary>Cria uma linha de medição.</summary>
    /// <param name="etapaId">Etapa medida.</param>
    /// <param name="percentualFisicoNoPeriodo">Avanço físico no período (0–100).</param>
    /// <param name="valorNoPeriodo">Valor medido no período.</param>
    /// <returns>Nova linha de medição.</returns>
    public static ItemMedicao Criar(EtapaCronogramaId etapaId, decimal percentualFisicoNoPeriodo, ValorMonetario valorNoPeriodo)
    {
        ArgumentNullException.ThrowIfNull(valorNoPeriodo);
        ArgumentOutOfRangeException.ThrowIfNegative(percentualFisicoNoPeriodo);
        return new ItemMedicao(ItemMedicaoId.New(), etapaId, percentualFisicoNoPeriodo, valorNoPeriodo);
    }
}

/// <summary>
/// Boletim de medição periódica da obra (entidade filha do agregado <see cref="Obra"/>). A medição
/// aprovada pelo fiscal é o gatilho da liquidação em Finanças (Lei 4.320, art. 63 — verificação do
/// direito do credor). Numeração monotônica por obra e períodos disjuntos são invariantes do agregado
/// (I-7/I-7b). Nasce em <see cref="SituacaoMedicao.Rascunho"/>.
/// </summary>
public sealed class Medicao : Entity<MedicaoId>
{
    private readonly List<ItemMedicao> _itens = [];

    private Medicao()
    {
    }

    private Medicao(
        MedicaoId id,
        int numero,
        int competenciaAno,
        int competenciaMes,
        DateOnly periodoInicio,
        DateOnly periodoFim,
        ValorMonetario valorMedido,
        decimal percentualFisicoNoPeriodo)
        : base(id)
    {
        Numero = numero;
        CompetenciaAno = competenciaAno;
        CompetenciaMes = competenciaMes;
        PeriodoInicio = periodoInicio;
        PeriodoFim = periodoFim;
        ValorMedido = valorMedido;
        PercentualFisicoNoPeriodo = percentualFisicoNoPeriodo;
        Situacao = SituacaoMedicao.Rascunho;
    }

    /// <summary>Número sequencial monotônico da medição na obra (I-7).</summary>
    public int Numero { get; private set; }

    /// <summary>Ano da competência (mês/ano de referência).</summary>
    public int CompetenciaAno { get; private set; }

    /// <summary>Mês da competência (1–12).</summary>
    public int CompetenciaMes { get; private set; }

    /// <summary>Início do período medido (inclusivo).</summary>
    public DateOnly PeriodoInicio { get; private set; }

    /// <summary>Fim do período medido (inclusivo).</summary>
    public DateOnly PeriodoFim { get; private set; }

    /// <summary>Valor medido no período.</summary>
    public ValorMonetario ValorMedido { get; private set; } = default!;

    /// <summary>Avanço físico (ponderado) reconhecido no período (0–100).</summary>
    public decimal PercentualFisicoNoPeriodo { get; private set; }

    /// <summary>Situação do boletim (Rascunho/Aprovada/Rejeitada).</summary>
    public SituacaoMedicao Situacao { get; private set; }

    /// <summary>Fiscal que aprovou a medição (nulo enquanto não aprovada — I-10).</summary>
    public Guid? FiscalAprovadorId { get; private set; }

    /// <summary>Data de aprovação (nula enquanto não aprovada).</summary>
    public DateOnly? DataAprovacao { get; private set; }

    /// <summary>Motivo da rejeição, quando rejeitada.</summary>
    public string? MotivoRejeicao { get; private set; }

    /// <summary>Linhas da medição (avanço por etapa) — lastro do reconhecimento nas etapas (I-5).</summary>
    public IReadOnlyCollection<ItemMedicao> Itens => _itens.AsReadOnly();

    /// <summary>Anexa uma linha (avanço por etapa) à medição. Uso interno do agregado <see cref="Obra"/>.</summary>
    /// <param name="item">Linha de medição.</param>
    internal void AdicionarItem(ItemMedicao item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _itens.Add(item);
    }

    /// <summary>Cria uma medição em rascunho validando período e valor.</summary>
    /// <param name="numero">Número sequencial (positivo; sequência garantida pela raiz).</param>
    /// <param name="competenciaAno">Ano da competência.</param>
    /// <param name="competenciaMes">Mês da competência (1–12).</param>
    /// <param name="periodoInicio">Início do período (inclusivo).</param>
    /// <param name="periodoFim">Fim do período (&gt;= início).</param>
    /// <param name="valorMedido">Valor medido (não nulo).</param>
    /// <param name="percentualFisicoNoPeriodo">Avanço físico no período (0–100).</param>
    /// <returns>Nova medição em <see cref="SituacaoMedicao.Rascunho"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se número/competência/percentual forem inválidos ou o período invertido.</exception>
    public static Medicao Registrar(
        int numero,
        int competenciaAno,
        int competenciaMes,
        DateOnly periodoInicio,
        DateOnly periodoFim,
        ValorMonetario valorMedido,
        decimal percentualFisicoNoPeriodo)
    {
        ArgumentNullException.ThrowIfNull(valorMedido);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(competenciaMes);
        if (competenciaMes > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(competenciaMes), "Mês da competência deve estar entre 1 e 12.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(percentualFisicoNoPeriodo);
        if (percentualFisicoNoPeriodo > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(percentualFisicoNoPeriodo), "Avanço físico no período não pode exceder 100%.");
        }

        if (periodoFim < periodoInicio)
        {
            throw new ArgumentOutOfRangeException(nameof(periodoFim), "Fim do período não pode anteceder o início.");
        }

        return new Medicao(
            MedicaoId.New(),
            numero,
            competenciaAno,
            competenciaMes,
            periodoInicio,
            periodoFim,
            valorMedido,
            percentualFisicoNoPeriodo);
    }

    /// <summary>Indica se o período desta medição se sobrepõe ao intervalo informado (I-7b).</summary>
    /// <param name="inicio">Início do outro intervalo.</param>
    /// <param name="fim">Fim do outro intervalo.</param>
    /// <returns><c>true</c> se houver interseção de datas.</returns>
    public bool SobrepoePeriodo(DateOnly inicio, DateOnly fim)
        => PeriodoInicio <= fim && inicio <= PeriodoFim;

    /// <summary>Aprova a medição (gatilho da liquidação). Só a partir de rascunho.</summary>
    /// <param name="fiscalAprovadorId">Fiscal aprovador (vigente — validado pela raiz, I-10).</param>
    /// <param name="dataAprovacao">Data da aprovação.</param>
    /// <exception cref="InvalidOperationException">Se a medição não estiver em rascunho.</exception>
    internal void Aprovar(Guid fiscalAprovadorId, DateOnly dataAprovacao)
    {
        if (Situacao != SituacaoMedicao.Rascunho)
        {
            throw new InvalidOperationException($"Só é possível aprovar medição em rascunho. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoMedicao.Aprovada;
        FiscalAprovadorId = fiscalAprovadorId;
        DataAprovacao = dataAprovacao;
    }

    /// <summary>Rejeita a medição (não compõe o valor medido acumulado). Só a partir de rascunho.</summary>
    /// <param name="motivo">Motivo da rejeição (obrigatório).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a medição não estiver em rascunho.</exception>
    internal void Rejeitar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoMedicao.Rascunho)
        {
            throw new InvalidOperationException($"Só é possível rejeitar medição em rascunho. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoMedicao.Rejeitada;
        MotivoRejeicao = motivo.Trim();
    }
}

using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>Identificador forte da entidade filha <see cref="EtapaCronograma"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EtapaCronogramaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EtapaCronogramaId"/>.</returns>
    public static EtapaCronogramaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Etapa/marco do cronograma físico-financeiro da obra (a "curva S" do edital): peso físico previsto,
/// valor previsto e janela de datas. O percentual/valor executados são atualizados pelas medições
/// aprovadas (entidade filha do agregado <see cref="Obra"/>; nasce válida).
/// </summary>
public sealed class EtapaCronograma : Entity<EtapaCronogramaId>
{
    private EtapaCronograma()
    {
    }

    private EtapaCronograma(
        EtapaCronogramaId id,
        int ordem,
        string descricao,
        decimal percentualFisicoPrevisto,
        ValorMonetario valorPrevisto,
        DateOnly dataPrevistaInicio,
        DateOnly dataPrevistaFim)
        : base(id)
    {
        Ordem = ordem;
        Descricao = descricao;
        PercentualFisicoPrevisto = percentualFisicoPrevisto;
        ValorPrevisto = valorPrevisto;
        DataPrevistaInicio = dataPrevistaInicio;
        DataPrevistaFim = dataPrevistaFim;
        PercentualFisicoExecutado = 0m;
        ValorMedido = ValorMonetario.Zero;
        Situacao = SituacaoEtapa.Prevista;
    }

    /// <summary>Ordem sequencial da etapa no cronograma.</summary>
    public int Ordem { get; private set; }

    /// <summary>Descrição da etapa (ex.: "Fundação", "Estrutura", "Acabamento").</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Peso físico previsto da etapa no total da obra (0–100).</summary>
    public decimal PercentualFisicoPrevisto { get; private set; }

    /// <summary>Valor previsto da etapa.</summary>
    public ValorMonetario ValorPrevisto { get; private set; } = default!;

    /// <summary>Data prevista de início da etapa.</summary>
    public DateOnly DataPrevistaInicio { get; private set; }

    /// <summary>Data prevista de término da etapa.</summary>
    public DateOnly DataPrevistaFim { get; private set; }

    /// <summary>Percentual físico já executado/medido da etapa (0–100).</summary>
    public decimal PercentualFisicoExecutado { get; private set; }

    /// <summary>Valor já medido (acumulado) da etapa.</summary>
    public ValorMonetario ValorMedido { get; private set; } = default!;

    /// <summary>Situação atual da etapa.</summary>
    public SituacaoEtapa Situacao { get; private set; }

    /// <summary>Cria uma etapa de cronograma validando peso e datas.</summary>
    /// <param name="ordem">Ordem sequencial (positiva).</param>
    /// <param name="descricao">Descrição da etapa (obrigatória).</param>
    /// <param name="percentualFisicoPrevisto">Peso físico previsto (0–100, positivo).</param>
    /// <param name="valorPrevisto">Valor previsto (não nulo).</param>
    /// <param name="dataPrevistaInicio">Data prevista de início.</param>
    /// <param name="dataPrevistaFim">Data prevista de término (&gt;= início).</param>
    /// <returns>Nova etapa em situação <see cref="SituacaoEtapa.Prevista"/>.</returns>
    /// <exception cref="ArgumentException">Se a descrição for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se peso/ordem forem inválidos ou as datas invertidas.</exception>
    public static EtapaCronograma Criar(
        int ordem,
        string descricao,
        decimal percentualFisicoPrevisto,
        ValorMonetario valorPrevisto,
        DateOnly dataPrevistaInicio,
        DateOnly dataPrevistaFim)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentNullException.ThrowIfNull(valorPrevisto);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ordem);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(percentualFisicoPrevisto);
        if (percentualFisicoPrevisto > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(percentualFisicoPrevisto), "Peso físico não pode exceder 100%.");
        }

        if (dataPrevistaFim < dataPrevistaInicio)
        {
            throw new ArgumentOutOfRangeException(nameof(dataPrevistaFim), "Fim previsto não pode anteceder o início previsto.");
        }

        return new EtapaCronograma(
            EtapaCronogramaId.New(),
            ordem,
            descricao.Trim(),
            percentualFisicoPrevisto,
            valorPrevisto,
            dataPrevistaInicio,
            dataPrevistaFim);
    }

    /// <summary>
    /// Reconhece a execução física/financeira de uma medição aprovada sobre esta etapa (I-5): acumula o
    /// percentual físico (limitado a 100) e o valor medido (limitado ao previsto), atualizando a situação.
    /// </summary>
    /// <param name="percentualFisicoNoPeriodo">Avanço físico da etapa no período (0–100).</param>
    /// <param name="valorNoPeriodo">Valor medido da etapa no período.</param>
    /// <exception cref="ArgumentNullException">Se o valor for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o avanço físico for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o acumulado físico ou financeiro exceder o previsto da etapa (I-5).</exception>
    public void ReconhecerMedicao(decimal percentualFisicoNoPeriodo, ValorMonetario valorNoPeriodo)
    {
        ArgumentNullException.ThrowIfNull(valorNoPeriodo);
        ArgumentOutOfRangeException.ThrowIfNegative(percentualFisicoNoPeriodo);

        var novoFisico = PercentualFisicoExecutado + percentualFisicoNoPeriodo;
        if (novoFisico > 100m)
        {
            throw new InvalidOperationException(
                $"Execução física acumulada da etapa '{Descricao}' ({novoFisico}%) excede 100%.");
        }

        var novoValor = ValorMedido.Somar(valorNoPeriodo);
        // I-5: o valor medido da etapa não pode exceder o previsto (extrapolação exige aditivo no cronograma).
        if (novoValor.MaiorQue(ValorPrevisto))
        {
            throw new InvalidOperationException(
                $"Valor medido acumulado da etapa '{Descricao}' excede o previsto ({ValorPrevisto}).");
        }

        PercentualFisicoExecutado = novoFisico;
        ValorMedido = novoValor;
        Situacao = novoFisico >= 100m ? SituacaoEtapa.Concluida : SituacaoEtapa.EmAndamento;
    }
}

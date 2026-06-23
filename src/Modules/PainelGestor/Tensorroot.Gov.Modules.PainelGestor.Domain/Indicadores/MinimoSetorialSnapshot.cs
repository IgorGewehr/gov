namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;

/// <summary>Identificador forte da entidade-filha <see cref="MinimoSetorialSnapshot"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MinimoSetorialSnapshotId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MinimoSetorialSnapshotId"/>.</returns>
    public static MinimoSetorialSnapshotId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha materializada de um mínimo constitucional setorial (Saúde/Educação) consumido da Transparencia
/// via <c>MinimoConstitucionalApuradoIntegrationEvent</c>. Snapshot puro (read model) — o cálculo do
/// percentual/situação é da Transparencia; o Painel só exibe.
/// </summary>
public sealed class MinimoSetorialSnapshot
{
    private MinimoSetorialSnapshot()
    {
    }

    private MinimoSetorialSnapshot(
        MinimoSetorialSnapshotId id,
        string setor,
        decimal receitaBase,
        decimal aplicado,
        decimal percentualAplicado,
        decimal percentualMinimo,
        string situacao)
    {
        Id = id;
        Setor = setor;
        ReceitaBase = receitaBase;
        Aplicado = aplicado;
        PercentualAplicado = percentualAplicado;
        PercentualMinimo = percentualMinimo;
        Situacao = situacao;
    }

    /// <summary>Identidade da linha.</summary>
    public MinimoSetorialSnapshotId Id { get; private init; }

    /// <summary>Setor apurado (Saude/Educacao).</summary>
    public string Setor { get; private set; } = string.Empty;

    /// <summary>Receita-base (impostos + transferências) do setor.</summary>
    public decimal ReceitaBase { get; private set; }

    /// <summary>Valor aplicado computável no setor.</summary>
    public decimal Aplicado { get; private set; }

    /// <summary>Percentual aplicado (0..1).</summary>
    public decimal PercentualAplicado { get; private set; }

    /// <summary>Percentual mínimo (limite) vigente (0..1).</summary>
    public decimal PercentualMinimo { get; private set; }

    /// <summary>Situação apurada (Atingido/NaoAtingido).</summary>
    public string Situacao { get; private set; } = string.Empty;

    /// <summary>Cria uma linha de mínimo setorial materializado.</summary>
    /// <param name="setor">Setor (Saude/Educacao).</param>
    /// <param name="receitaBase">Receita-base do setor.</param>
    /// <param name="aplicado">Valor aplicado computável.</param>
    /// <param name="percentualAplicado">Percentual aplicado (0..1).</param>
    /// <param name="percentualMinimo">Percentual mínimo vigente (0..1).</param>
    /// <param name="situacao">Situação (Atingido/NaoAtingido).</param>
    /// <returns>Nova linha.</returns>
    public static MinimoSetorialSnapshot Criar(
        string setor,
        decimal receitaBase,
        decimal aplicado,
        decimal percentualAplicado,
        decimal percentualMinimo,
        string situacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(setor);
        ArgumentException.ThrowIfNullOrWhiteSpace(situacao);
        return new MinimoSetorialSnapshot(
            MinimoSetorialSnapshotId.New(), setor, receitaBase, aplicado, percentualAplicado, percentualMinimo, situacao);
    }
}

using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>Identificador forte de uma <see cref="SolicitacaoExame"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct SolicitacaoExameId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="SolicitacaoExameId"/>.</returns>
    public static SolicitacaoExameId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Pedido de exame/procedimento vinculado ao atendimento, com justificativa clinica.</summary>
public sealed class SolicitacaoExame : Entity<SolicitacaoExameId>
{
    private SolicitacaoExame()
    {
    }

    private SolicitacaoExame(SolicitacaoExameId id, string procedimento, string justificativa, DateTimeOffset dataHora)
        : base(id)
    {
        Procedimento = procedimento;
        Justificativa = justificativa;
        DataHora = dataHora;
    }

    /// <summary>Procedimento/exame solicitado.</summary>
    public string Procedimento { get; private set; } = default!;

    /// <summary>Justificativa clinica da solicitacao.</summary>
    public string Justificativa { get; private set; } = default!;

    /// <summary>Data/hora da solicitacao.</summary>
    public DateTimeOffset DataHora { get; private set; }

    /// <summary>Registra uma nova solicitacao de exame.</summary>
    /// <param name="procedimento">Procedimento/exame.</param>
    /// <param name="justificativa">Justificativa clinica.</param>
    /// <param name="dataHora">Data/hora.</param>
    /// <returns>Nova <see cref="SolicitacaoExame"/>.</returns>
    /// <exception cref="ArgumentException">Se procedimento ou justificativa forem vazios.</exception>
    public static SolicitacaoExame Registrar(string procedimento, string justificativa, DateTimeOffset dataHora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedimento);
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativa);
        return new SolicitacaoExame(SolicitacaoExameId.New(), procedimento.Trim(), justificativa.Trim(), dataHora);
    }
}

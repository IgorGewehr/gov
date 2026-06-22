using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

/// <summary>Identificador forte de uma <see cref="Movimentacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MovimentacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MovimentacaoId"/>.</returns>
    public static MovimentacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro de tramitacao do processo entre setores/responsaveis (entidade interna,
/// append-only — compoe a trilha documental e nunca e excluida ou editada).
/// </summary>
public sealed class Movimentacao : Entity<MovimentacaoId>
{
    private Movimentacao()
    {
    }

    private Movimentacao(
        MovimentacaoId id,
        Guid? setorOrigemId,
        Guid setorDestinoId,
        string? observacao,
        DateOnly dataMovimentacao)
        : base(id)
    {
        SetorOrigemId = setorOrigemId;
        SetorDestinoId = setorDestinoId;
        Observacao = observacao;
        DataMovimentacao = dataMovimentacao;
    }

    /// <summary>Setor de origem da tramitacao (nulo na primeira movimentacao).</summary>
    public Guid? SetorOrigemId { get; private set; }

    /// <summary>Setor de destino da tramitacao.</summary>
    public Guid SetorDestinoId { get; private set; }

    /// <summary>Observacao opcional da tramitacao.</summary>
    public string? Observacao { get; private set; }

    /// <summary>Data da movimentacao.</summary>
    public DateOnly DataMovimentacao { get; private set; }

    /// <summary>Registra uma nova movimentacao (tramitacao) do processo.</summary>
    /// <param name="setorOrigemId">Setor de origem (nulo na primeira movimentacao).</param>
    /// <param name="setorDestinoId">Setor de destino.</param>
    /// <param name="observacao">Observacao opcional.</param>
    /// <param name="dataMovimentacao">Data da movimentacao.</param>
    /// <returns>Nova <see cref="Movimentacao"/>.</returns>
    public static Movimentacao Registrar(
        Guid? setorOrigemId,
        Guid setorDestinoId,
        string? observacao,
        DateOnly dataMovimentacao)
        => new(MovimentacaoId.New(), setorOrigemId, setorDestinoId, observacao, dataMovimentacao);
}

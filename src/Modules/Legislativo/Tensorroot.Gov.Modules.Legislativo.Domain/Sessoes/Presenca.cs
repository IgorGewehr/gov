using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

/// <summary>Identificador forte de uma <see cref="Presenca"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PresencaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PresencaId"/>.</returns>
    public static PresencaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro de comparecimento de um vereador a uma sessao. Entidade do agregado
/// <see cref="Sessao"/>, integrante da trilha imutavel (append-only) de presencas — prova
/// juridica e LAI. Uma presenca por vereador por sessao (idempotente por <see cref="VereadorId"/>).
/// </summary>
public sealed class Presenca : Entity<PresencaId>
{
    private Presenca()
    {
    }

    private Presenca(PresencaId id, VereadorId vereadorId, DateTimeOffset registradaEm)
        : base(id)
    {
        VereadorId = vereadorId;
        RegistradaEm = registradaEm;
    }

    /// <summary>Vereador que registrou presenca.</summary>
    public VereadorId VereadorId { get; private set; }

    /// <summary>Momento (timestamp) do registro de presenca.</summary>
    public DateTimeOffset RegistradaEm { get; private set; }

    /// <summary>Registra a presenca de um vereador.</summary>
    /// <param name="vereadorId">Vereador presente.</param>
    /// <param name="registradaEm">Momento do registro.</param>
    /// <returns>Nova <see cref="Presenca"/>.</returns>
    public static Presenca Registrar(VereadorId vereadorId, DateTimeOffset registradaEm)
        => new(PresencaId.New(), vereadorId, registradaEm);
}

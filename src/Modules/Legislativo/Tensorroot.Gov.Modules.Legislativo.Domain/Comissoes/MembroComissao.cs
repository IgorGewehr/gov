using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;

/// <summary>Identificador forte de um <see cref="MembroComissao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MembroComissaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MembroComissaoId"/>.</returns>
    public static MembroComissaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Membro de uma <see cref="Comissao"/>: vinculo a um vereador (<see cref="VereadorId"/>) com papel
/// (efetivo/suplente) e cargo de direcao (presidente/vice/relator). Entidade do agregado Comissao.
/// </summary>
public sealed class MembroComissao : Entity<MembroComissaoId>
{
    private MembroComissao()
    {
    }

    private MembroComissao(MembroComissaoId id, VereadorId vereadorId, PapelMembro papel, CargoComissao cargo)
        : base(id)
    {
        VereadorId = vereadorId;
        Papel = papel;
        Cargo = cargo;
    }

    /// <summary>Vereador integrante.</summary>
    public VereadorId VereadorId { get; private set; }

    /// <summary>Papel (efetivo/suplente).</summary>
    public PapelMembro Papel { get; private set; }

    /// <summary>Cargo de direcao na comissao.</summary>
    public CargoComissao Cargo { get; private set; }

    /// <summary>Cria um membro da comissao.</summary>
    /// <param name="vereadorId">Vereador.</param>
    /// <param name="papel">Papel.</param>
    /// <param name="cargo">Cargo de direcao.</param>
    /// <returns>Novo <see cref="MembroComissao"/>.</returns>
    /// <exception cref="ArgumentException">Se papel ou cargo forem invalidos.</exception>
    public static MembroComissao Designar(VereadorId vereadorId, PapelMembro papel, CargoComissao cargo)
    {
        if (!Enum.IsDefined(papel))
        {
            throw new ArgumentException("Papel de membro invalido.", nameof(papel));
        }

        if (!Enum.IsDefined(cargo))
        {
            throw new ArgumentException("Cargo de comissao invalido.", nameof(cargo));
        }

        return new MembroComissao(MembroComissaoId.New(), vereadorId, papel, cargo);
    }

    /// <summary>Altera o cargo de direcao do membro (usado na eleicao da presidencia).</summary>
    /// <param name="cargo">Novo cargo.</param>
    internal void DefinirCargo(CargoComissao cargo) => Cargo = cargo;
}

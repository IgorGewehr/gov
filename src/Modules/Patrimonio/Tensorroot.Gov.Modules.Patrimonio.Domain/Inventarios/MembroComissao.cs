using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

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
/// Membro da comissão de inventário (servidor designado por portaria — entidade-filha de
/// <see cref="Inventario"/>). Reúne o vínculo do responsável e o nome para a trilha de auditoria/TCE.
/// </summary>
public sealed class MembroComissao : Entity<MembroComissaoId>
{
    private MembroComissao()
    {
    }

    private MembroComissao(MembroComissaoId id, Guid responsavelId, string nome, bool presidente)
        : base(id)
    {
        ResponsavelId = responsavelId;
        Nome = nome;
        Presidente = presidente;
    }

    /// <summary>Vínculo (Id) do servidor/responsável designado.</summary>
    public Guid ResponsavelId { get; private set; }

    /// <summary>Nome do membro (para a trilha do levantamento).</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Indica se é o presidente da comissão.</summary>
    public bool Presidente { get; private set; }

    /// <summary>Designa um novo membro de comissão.</summary>
    /// <param name="responsavelId">Vínculo (Id) do servidor designado.</param>
    /// <param name="nome">Nome do membro.</param>
    /// <param name="presidente">Indica se preside a comissão.</param>
    /// <returns>Novo <see cref="MembroComissao"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o responsável não for informado.</exception>
    public static MembroComissao Designar(Guid responsavelId, string nome, bool presidente)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        if (responsavelId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(responsavelId), "Membro da comissão exige vínculo do responsável.");
        }

        return new MembroComissao(MembroComissaoId.New(), responsavelId, nome, presidente);
    }
}

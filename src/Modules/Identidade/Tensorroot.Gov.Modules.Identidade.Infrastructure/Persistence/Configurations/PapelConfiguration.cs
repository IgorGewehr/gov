using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="Papel"/>. As permissoes (conjunto de escopos do
/// catalogo canonico) sao persistidas em coluna JSON, mantendo o agregado coeso. O nome e unico
/// por tenant.
/// </summary>
public sealed class PapelConfiguration : IEntityTypeConfiguration<Papel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Papel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Papeis");
        builder.HasKey(papel => papel.Id);
        builder.Property(papel => papel.Id)
            .HasConversion(id => id.Value, value => new PapelId(value))
            .ValueGeneratedNever();

        builder.Property(papel => papel.TenantId).IsRequired();
        builder.Property(papel => papel.Nome).HasMaxLength(Papel.ComprimentoMaximoNome).IsRequired();

        builder.HasIndex(papel => new { papel.TenantId, papel.Nome }).IsUnique();

        // Permissoes (HashSet<string>) serializadas como coluna JSON via primitive collection.
        var comparador = new ValueComparer<IReadOnlySet<string>>(
            (esquerda, direita) => esquerda!.SetEquals(direita!),
            permissoes => permissoes.Aggregate(0, (hash, permissao) => HashCode.Combine(hash, permissao.GetHashCode(StringComparison.Ordinal))),
            permissoes => new HashSet<string>(permissoes, StringComparer.Ordinal));

        builder.Property<IReadOnlySet<string>>("Permissoes")
            .HasField("_permissoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                permissoes => string.Join('\n', permissoes),
                texto => DesserializarPermissoes(texto))
            .HasColumnName("Permissoes")
            .Metadata.SetValueComparer(comparador);
    }

    private static HashSet<string> DesserializarPermissoes(string texto)
        => string.IsNullOrEmpty(texto)
            ? new HashSet<string>(StringComparer.Ordinal)
            : texto.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
}

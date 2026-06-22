using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Receitas;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do read model <see cref="ReceitaArrecadada"/>.</summary>
public sealed class ReceitaArrecadadaConfiguration : IEntityTypeConfiguration<ReceitaArrecadada>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReceitaArrecadada> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ReceitasArrecadadas");
        builder.HasKey(receita => receita.Id);
        builder.Property(receita => receita.Id)
            .HasConversion(id => id.Value, value => new ReceitaArrecadadaId(value))
            .ValueGeneratedNever();

        builder.Property(receita => receita.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(receita => new { receita.TenantId, receita.OrigemId });
    }
}

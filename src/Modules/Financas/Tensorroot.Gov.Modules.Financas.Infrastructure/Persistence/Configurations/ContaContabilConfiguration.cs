using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ContaContabil"/> (plano de contas PCASP).</summary>
public sealed class ContaContabilConfiguration : IEntityTypeConfiguration<ContaContabil>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContaContabil> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ContasContabeis");
        builder.HasKey(conta => conta.Id);
        builder.Property(conta => conta.Id)
            .HasConversion(id => id.Value, value => new ContaContabilId(value))
            .ValueGeneratedNever();

        builder.Property(conta => conta.Codigo)
            .HasConversion(codigo => codigo.Codigo, valor => CodigoContabil.De(valor))
            .HasColumnName("Codigo")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(conta => conta.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(conta => conta.Funcao).HasMaxLength(1000).IsRequired();
        builder.Property(conta => conta.Funcionamento).HasMaxLength(1000).IsRequired();

        builder.Property(conta => conta.NaturezaInformacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(conta => conta.NaturezaSaldo).HasConversion<string>().HasMaxLength(20);
        builder.Property(conta => conta.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(conta => conta.IndicadorSuperavitFinanceiro).HasConversion<string>().HasMaxLength(20);

        builder.Property(conta => conta.ContaPaiId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : new ContaContabilId(value.Value));

        builder.HasIndex(conta => new { conta.TenantId, conta.Codigo }).IsUnique();
    }
}

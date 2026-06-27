using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="BancoDeHoras"/> (saldo + lancamentos owned).</summary>
public sealed class BancoDeHorasConfiguration : IEntityTypeConfiguration<BancoDeHoras>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BancoDeHoras> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("BancosDeHoras");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(id => id.Value, value => new BancoDeHorasId(value))
            .ValueGeneratedNever();

        builder.Property(b => b.ServidorId).IsRequired();
        builder.Property(b => b.SaldoMinutos);

        // Lancamentos (livro-razao) owned — tabela filha BancosDeHorasLancamentos.
        builder.OwnsMany(b => b.Lancamentos, lancamentos =>
        {
            lancamentos.ToTable("BancosDeHorasLancamentos");
            lancamentos.WithOwner().HasForeignKey("BancoDeHorasId");
            lancamentos.HasKey(l => l.Id);
            lancamentos.Property(l => l.Id)
                .HasConversion(id => id.Value, value => new LancamentoBancoHorasId(value))
                .ValueGeneratedNever();
            lancamentos.Property(l => l.Tipo).HasConversion<string>().HasMaxLength(20);
            lancamentos.Property(l => l.Minutos);
            lancamentos.Property(l => l.Data);
            lancamentos.Property(l => l.Referencia).HasMaxLength(LancamentoBancoHoras.ComprimentoMaximoReferencia).IsRequired();
            lancamentos.Property(l => l.Descricao).HasMaxLength(LancamentoBancoHoras.ComprimentoMaximoDescricao).IsRequired();
            lancamentos.HasIndex("BancoDeHorasId", "Referencia").IsUnique();
        });
        builder.Navigation(b => b.Lancamentos).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Banco unico por servidor no tenant (saldo vivo).
        builder.HasIndex(b => new { b.TenantId, b.ServidorId }).IsUnique();
    }
}

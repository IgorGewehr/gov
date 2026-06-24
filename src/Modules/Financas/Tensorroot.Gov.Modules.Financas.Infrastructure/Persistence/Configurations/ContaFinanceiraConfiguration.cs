using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ContaFinanceira"/> e da filha <see cref="MovimentoFinanceiro"/>.</summary>
public sealed class ContaFinanceiraConfiguration : IEntityTypeConfiguration<ContaFinanceira>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContaFinanceira> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ContasFinanceiras");
        builder.HasKey(conta => conta.Id);
        builder.Property(conta => conta.Id)
            .HasConversion(id => id.Value, value => new ContaFinanceiraId(value))
            .ValueGeneratedNever();

        builder.Property(conta => conta.Nome).HasMaxLength(120).IsRequired();
        builder.Property(conta => conta.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(conta => conta.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.Property(conta => conta.SaldoInicial)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(conta => conta.Saldo)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.OwnsOne(conta => conta.DadosBancarios, banco =>
        {
            banco.Property(b => b.Banco).HasColumnName("Banco").HasMaxLength(20);
            banco.Property(b => b.Agencia).HasColumnName("Agencia").HasMaxLength(20);
            banco.Property(b => b.Conta).HasColumnName("ContaNumero").HasMaxLength(30);
            banco.Property(b => b.Pix).HasColumnName("Pix").HasMaxLength(140);
        });

        builder.OwnsMany(conta => conta.Movimentos, movimentos =>
        {
            movimentos.ToTable("MovimentosFinanceiros");
            movimentos.WithOwner().HasForeignKey("ContaFinanceiraId");
            movimentos.HasKey(m => m.Id);
            movimentos.Property(m => m.Id)
                .HasConversion(id => id.Value, value => new MovimentoFinanceiroId(value))
                .ValueGeneratedNever();
            movimentos.Property(m => m.ContaId)
                .HasConversion(id => id.Value, value => new ContaFinanceiraId(value));
            movimentos.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(25);
            movimentos.Property(m => m.Data).HasColumnType("date");
            movimentos.Property(m => m.Valor)
                .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
                .HasColumnType("decimal(18,2)");
            movimentos.Property(m => m.SaldoApos)
                .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
                .HasColumnType("decimal(18,2)");
            movimentos.Property(m => m.Historico).HasMaxLength(300).IsRequired();
            movimentos.Property(m => m.Documento).HasMaxLength(60);
            movimentos.Property(m => m.ContraparteContaId)
                .HasConversion(
                    id => id == null ? (Guid?)null : id.Value.Value,
                    value => value == null ? null : new ContaFinanceiraId(value.Value));
            movimentos.Property(m => m.DataConciliacao).HasColumnType("date");
            movimentos.HasIndex(m => m.Data);
        });
        builder.Navigation(conta => conta.Movimentos).Metadata.SetField("_movimentos");
        builder.Navigation(conta => conta.Movimentos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(conta => new { conta.TenantId, conta.Nome }).IsUnique();
    }
}

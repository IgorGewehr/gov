using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="OrdemDePagamento"/> e da entidade-filha <see cref="ItemPagamento"/>.</summary>
public sealed class OrdemDePagamentoConfiguration : IEntityTypeConfiguration<OrdemDePagamento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrdemDePagamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OrdensDePagamento");
        builder.HasKey(ordem => ordem.Id);
        builder.Property(ordem => ordem.Id)
            .HasConversion(id => id.Value, value => new OrdemDePagamentoId(value))
            .ValueGeneratedNever();

        builder.Property(ordem => ordem.Numero).HasMaxLength(30).IsRequired();
        builder.Property(ordem => ordem.DataPagamento).HasColumnType("date");

        builder.Property(ordem => ordem.ValorTotal)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(ordem => ordem.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(ordem => ordem.ContaBancaria, conta =>
        {
            conta.Property(c => c.Banco).HasColumnName("ContaBanco").HasMaxLength(20).IsRequired();
            conta.Property(c => c.Agencia).HasColumnName("ContaAgencia").HasMaxLength(20).IsRequired();
            conta.Property(c => c.Conta).HasColumnName("ContaNumero").HasMaxLength(30).IsRequired();
            conta.Property(c => c.Pix).HasColumnName("ContaPix").HasMaxLength(140);
        });
        builder.Navigation(ordem => ordem.ContaBancaria).IsRequired();

        builder.OwnsMany(ordem => ordem.Itens, itens =>
        {
            itens.ToTable("ItensPagamento");
            itens.WithOwner().HasForeignKey("OrdemDePagamentoId");
            itens.HasKey(item => item.Id);
            itens.Property(item => item.Id)
                .HasConversion(id => id.Value, value => new ItemPagamentoId(value))
                .ValueGeneratedNever();
            itens.Property(item => item.LiquidacaoId)
                .HasConversion(id => id.Value, value => new LiquidacaoId(value));
            itens.Property(item => item.Valor)
                .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
                .HasColumnType("decimal(18,2)");
            itens.HasIndex(item => item.LiquidacaoId);
        });
        builder.Navigation(ordem => ordem.Itens).Metadata.SetField("_itens");
        builder.Navigation(ordem => ordem.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(ordem => new { ordem.TenantId, ordem.Numero }).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ItemEstoque"/> e de suas entidades filhas.</summary>
public sealed class ItemEstoqueConfiguration : IEntityTypeConfiguration<ItemEstoque>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ItemEstoque> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ItensEstoque");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemEstoqueId(value))
            .ValueGeneratedNever();

        builder.Property(item => item.Codigo).HasMaxLength(40);
        builder.Property(item => item.Descricao).HasMaxLength(200);
        builder.Property(item => item.UnidadeMedida).HasMaxLength(10);
        builder.Property(item => item.MetodoCusteio).HasConversion<string>().HasMaxLength(10);
        builder.Property(item => item.ClassificacaoAbc).HasConversion<string>().HasMaxLength(1);
        builder.Property(item => item.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.Property(item => item.Saldo)
            .HasConversion(saldo => saldo.Quantidade, valor => SaldoAlmoxarifado.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(item => item.PontoPedido)
            .HasConversion(ponto => ponto.Quantidade, valor => PontoPedido.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(item => item.CustoMedio)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(item => item.ValorRealizavelLiquido)
            .HasConversion(valor => valor!.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        // Propriedade calculada (sem coluna).
        builder.Ignore(item => item.Movimentavel);

        builder.OwnsMany(item => item.Lotes, MapearLotes);
        builder.OwnsMany(item => item.Movimentos, MapearMovimentos);
        builder.OwnsMany(item => item.Requisicoes, MapearRequisicoes);

        builder.Navigation(item => item.Lotes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Movimentos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Requisicoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(item => new { item.TenantId, item.Codigo }).IsUnique();
    }

    private static void MapearLotes(OwnedNavigationBuilder<ItemEstoque, Lote> lotes)
    {
        lotes.ToTable("ItensEstoqueLotes");
        lotes.WithOwner().HasForeignKey("ItemEstoqueId");
        lotes.HasKey(lote => lote.Id);
        lotes.Property(lote => lote.Id)
            .HasConversion(id => id.Value, value => new LoteId(value))
            .ValueGeneratedNever();
        lotes.Property(lote => lote.CustoUnitario)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        lotes.Property(lote => lote.QuantidadeEntrada).HasColumnType("decimal(18,2)");
        lotes.Property(lote => lote.QuantidadeRestante).HasColumnType("decimal(18,2)");
        lotes.Ignore(lote => lote.TemSaldo);
    }

    private static void MapearMovimentos(OwnedNavigationBuilder<ItemEstoque, MovimentoEstoque> movimentos)
    {
        movimentos.ToTable("ItensEstoqueMovimentos");
        movimentos.WithOwner().HasForeignKey("ItemEstoqueId");
        movimentos.HasKey(movimento => movimento.Id);
        movimentos.Property(movimento => movimento.Id)
            .HasConversion(id => id.Value, value => new MovimentoEstoqueId(value))
            .ValueGeneratedNever();
        movimentos.Property(movimento => movimento.Tipo).HasConversion<string>().HasMaxLength(10);
        movimentos.Property(movimento => movimento.Quantidade).HasColumnType("decimal(18,2)");
        movimentos.Property(movimento => movimento.Documento).HasMaxLength(100);
        movimentos.Property(movimento => movimento.ValorUnitario)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        movimentos.Ignore(movimento => movimento.ValorTotal);
    }

    private static void MapearRequisicoes(OwnedNavigationBuilder<ItemEstoque, Requisicao> requisicoes)
    {
        requisicoes.ToTable("ItensEstoqueRequisicoes");
        requisicoes.WithOwner().HasForeignKey("ItemEstoqueId");
        requisicoes.HasKey(requisicao => requisicao.Id);
        requisicoes.Property(requisicao => requisicao.Id)
            .HasConversion(id => id.Value, value => new RequisicaoId(value))
            .ValueGeneratedNever();
        requisicoes.Property(requisicao => requisicao.Quantidade).HasColumnType("decimal(18,2)");
        requisicoes.Property(requisicao => requisicao.Situacao).HasConversion<string>().HasMaxLength(20);
    }
}

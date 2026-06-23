using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="PedidoRequisicao"/> e de suas entidades filhas.</summary>
public sealed class PedidoRequisicaoConfiguration : IEntityTypeConfiguration<PedidoRequisicao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PedidoRequisicao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PedidosRequisicao");
        builder.HasKey(pedido => pedido.Id);
        builder.Property(pedido => pedido.Id)
            .HasConversion(id => id.Value, value => new PedidoRequisicaoId(value))
            .ValueGeneratedNever();

        builder.Property(pedido => pedido.SetorSolicitante).HasMaxLength(200);
        builder.Property(pedido => pedido.Justificativa).HasMaxLength(500);
        builder.Property(pedido => pedido.MotivoCancelamento).HasMaxLength(500);
        builder.Property(pedido => pedido.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(pedido => pedido.Data);
        builder.Property(pedido => pedido.DataAprovacao);
        builder.Property(pedido => pedido.DataAtendimento);

        // Propriedade calculada (sem coluna).
        builder.Ignore(pedido => pedido.Terminal);

        builder.OwnsMany(pedido => pedido.Itens, MapearItens);
        builder.Navigation(pedido => pedido.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Fila de pedidos por (tenant, situacao) e consumo por setor/UO.
        builder.HasIndex(pedido => new { pedido.TenantId, pedido.Situacao });
        builder.HasIndex(pedido => new { pedido.TenantId, pedido.UnidadeId });
    }

    private static void MapearItens(OwnedNavigationBuilder<PedidoRequisicao, ItemPedido> itens)
    {
        itens.ToTable("PedidosRequisicaoItens");
        itens.WithOwner().HasForeignKey("PedidoRequisicaoId");
        itens.HasKey(item => item.Id);
        itens.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemPedidoId(value))
            .ValueGeneratedNever();
        itens.Property(item => item.ItemEstoqueId)
            .HasConversion(id => id.Value, value => new ItemEstoqueId(value));
        itens.Property(item => item.QuantidadeSolicitada).HasColumnType("decimal(18,2)");
        itens.Property(item => item.QuantidadeAtendida).HasColumnType("decimal(18,2)");
        itens.Ignore(item => item.TotalmenteAtendido);
        itens.Ignore(item => item.Pendente);
    }
}

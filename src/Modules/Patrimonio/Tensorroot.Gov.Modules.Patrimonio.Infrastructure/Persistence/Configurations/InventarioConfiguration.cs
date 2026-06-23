using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Inventario"/> e de suas entidades filhas.</summary>
public sealed class InventarioConfiguration : IEntityTypeConfiguration<Inventario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Inventario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Inventarios");
        builder.HasKey(inventario => inventario.Id);
        builder.Property(inventario => inventario.Id)
            .HasConversion(id => id.Value, value => new InventarioId(value))
            .ValueGeneratedNever();

        builder.Property(inventario => inventario.Exercicio);
        builder.Property(inventario => inventario.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(inventario => inventario.Setor).HasMaxLength(200);
        builder.Property(inventario => inventario.Portaria).HasMaxLength(100);
        builder.Property(inventario => inventario.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(inventario => inventario.DataAbertura);
        builder.Property(inventario => inventario.DataEncerramento);

        // Propriedade calculada (sem coluna).
        builder.Ignore(inventario => inventario.Conciliado);

        builder.OwnsMany(inventario => inventario.Comissao, MapearComissao);
        builder.OwnsMany(inventario => inventario.Itens, MapearItens);
        builder.OwnsMany(inventario => inventario.Divergencias, MapearDivergencias);

        builder.Navigation(inventario => inventario.Comissao).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(inventario => inventario.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(inventario => inventario.Divergencias).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unicidade do levantamento por (tenant, exercicio, tipo, setor) — nao duplicar inventario identico.
        builder.HasIndex(inventario => new { inventario.TenantId, inventario.Exercicio, inventario.Tipo, inventario.Setor });
    }

    private static void MapearComissao(OwnedNavigationBuilder<Inventario, MembroComissao> comissao)
    {
        comissao.ToTable("InventariosComissao");
        comissao.WithOwner().HasForeignKey("InventarioId");
        comissao.HasKey(membro => membro.Id);
        comissao.Property(membro => membro.Id)
            .HasConversion(id => id.Value, value => new MembroComissaoId(value))
            .ValueGeneratedNever();
        comissao.Property(membro => membro.Nome).HasMaxLength(200);
    }

    private static void MapearItens(OwnedNavigationBuilder<Inventario, ItemInventario> itens)
    {
        itens.ToTable("InventariosItens");
        itens.WithOwner().HasForeignKey("InventarioId");
        itens.HasKey(item => item.Id);
        itens.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemInventarioId(value))
            .ValueGeneratedNever();
        itens.Property(item => item.BemPatrimonialId)
            .HasConversion(id => id.Value, value => new BemPatrimonialId(value));
        itens.Property(item => item.NumeroTombamento).HasMaxLength(NumeroTombamento.ComprimentoMaximo);
        itens.Property(item => item.DescricaoSnapshot).HasMaxLength(200);
        itens.Property(item => item.LocalizacaoEsperada).HasMaxLength(200);
        itens.Property(item => item.LocalizacaoEncontrada).HasMaxLength(200);
        itens.Property(item => item.Observacao).HasMaxLength(500);
        itens.Property(item => item.SituacaoEncontrada).HasConversion<string>().HasMaxLength(30);
        itens.Property(item => item.ValorContabilSnapshot)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearDivergencias(OwnedNavigationBuilder<Inventario, DivergenciaInventario> divergencias)
    {
        divergencias.ToTable("InventariosDivergencias");
        divergencias.WithOwner().HasForeignKey("InventarioId");
        divergencias.HasKey(divergencia => divergencia.Id);
        divergencias.Property(divergencia => divergencia.Id)
            .HasConversion(id => id.Value, value => new DivergenciaInventarioId(value))
            .ValueGeneratedNever();
        divergencias.Property(divergencia => divergencia.Tipo).HasConversion<string>().HasMaxLength(30);
        divergencias.Property(divergencia => divergencia.Recomendacao).HasConversion<string>().HasMaxLength(20);
        divergencias.Property(divergencia => divergencia.Descricao).HasMaxLength(1000);
        // BemPatrimonialId e nullable (sobra nao tem bem) — converter nullable-safe para Guid?.
        var conversorBemNullable = new ValueConverter<BemPatrimonialId?, Guid?>(
            id => id.HasValue ? id.Value.Value : null,
            value => value.HasValue ? new BemPatrimonialId(value.Value) : null);
        divergencias.Property(divergencia => divergencia.BemPatrimonialId).HasConversion(conversorBemNullable);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Credenciamento"/> (art. 79, Lei 14.133/2021) e filhos.</summary>
public sealed class CredenciamentoConfiguration : IEntityTypeConfiguration<Credenciamento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Credenciamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Credenciamentos");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CredenciamentoId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.Objeto).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.Hipotese).HasConversion<string>().HasMaxLength(40);
        builder.Property(c => c.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.NumeroEdital).HasMaxLength(60);
        builder.Property(c => c.VigenciaInicio);
        builder.Property(c => c.VigenciaFim);
        builder.Property(c => c.FundamentacaoLegal).HasMaxLength(500).IsRequired();
        builder.Property(c => c.EtpId);
        builder.Property(c => c.TermoReferenciaId);
        builder.Property(c => c.NumeroPncp).HasMaxLength(60);
        builder.Ignore(c => c.QuantidadeCredenciadosAptos);

        builder.OwnsMany(c => c.Itens, MapearItens);
        builder.Navigation(c => c.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(c => c.Credenciados, MapearCredenciados);
        builder.Navigation(c => c.Credenciados).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => new { c.TenantId, c.Situacao });
    }

    private static void MapearItens(OwnedNavigationBuilder<Credenciamento, ItemCredenciamento> itens)
    {
        itens.ToTable("CredenciamentosItens");
        itens.WithOwner().HasForeignKey("CredenciamentoId");
        itens.HasKey(i => i.Id);
        itens.Property(i => i.Id)
            .HasConversion(id => id.Value, value => new ItemCredenciamentoId(value))
            .ValueGeneratedNever();
        itens.Property(i => i.Numero);
        itens.Property(i => i.ItemCatalogoId);
        itens.Property(i => i.Descricao).HasMaxLength(1000).IsRequired();
        itens.Property(i => i.UnidadeMedida).HasMaxLength(50).IsRequired();
        itens.Property(i => i.PrecoFixado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearCredenciados(OwnedNavigationBuilder<Credenciamento, Credenciado> credenciados)
    {
        credenciados.ToTable("CredenciamentosCredenciados");
        credenciados.WithOwner().HasForeignKey("CredenciamentoId");
        credenciados.HasKey(c => c.Id);
        credenciados.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CredenciadoId(value))
            .ValueGeneratedNever();
        credenciados.Property(c => c.FornecedorId);
        credenciados.Property(c => c.DataInscricao);
        credenciados.Property(c => c.Situacao).HasConversion<string>().HasMaxLength(20);
        credenciados.Property(c => c.DataCredenciamento);
        credenciados.Property(c => c.DataDescredenciamento);
        credenciados.Property(c => c.Motivo).HasMaxLength(1000);
        credenciados.HasIndex(c => c.FornecedorId);
    }
}

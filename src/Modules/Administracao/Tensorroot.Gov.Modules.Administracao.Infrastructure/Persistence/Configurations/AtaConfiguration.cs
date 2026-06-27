using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Ata"/> (ARP) e de suas entidades filhas.</summary>
public sealed class AtaConfiguration : IEntityTypeConfiguration<Ata>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ata> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Atas");
        builder.HasKey(ata => ata.Id);
        builder.Property(ata => ata.Id)
            .HasConversion(id => id.Value, value => new AtaId(value))
            .ValueGeneratedNever();

        builder.Property(ata => ata.Numero).HasMaxLength(60);
        builder.Property(ata => ata.CnpjOrgaoGerenciador).HasMaxLength(20);
        builder.Property(ata => ata.NomeOrgaoGerenciador).HasMaxLength(200);
        builder.Property(ata => ata.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(ata => ata.VigenciaInicio);
        builder.Property(ata => ata.VigenciaInicioOriginal);
        builder.Property(ata => ata.VigenciaFim);
        builder.Property(ata => ata.Prorrogada);

        builder.OwnsMany(ata => ata.Itens, MapearItens);
        builder.OwnsMany(ata => ata.Adesoes, MapearAdesoes);
        builder.OwnsMany(ata => ata.Participantes, MapearParticipantes);

        builder.Navigation(ata => ata.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(ata => ata.Adesoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(ata => ata.Participantes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(ata => new { ata.TenantId, ata.Numero }).IsUnique();
        builder.HasIndex(ata => new { ata.TenantId, ata.Situacao });
    }

    private static void MapearItens(OwnedNavigationBuilder<Ata, ItemAta> itens)
    {
        itens.ToTable("AtasItens");
        itens.WithOwner().HasForeignKey("AtaId");
        itens.HasKey(item => item.Id);
        itens.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemAtaId(value))
            .ValueGeneratedNever();
        itens.Property(item => item.ItemCatalogoId)
            .HasConversion(id => id.Value, value => new ItemCatalogoId(value));
        itens.Property(item => item.PrecoRegistrado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        itens.Property(item => item.QuantidadeRegistrada).HasColumnType("decimal(18,4)");
        itens.Property(item => item.QuantidadeContratada).HasColumnType("decimal(18,4)");
        itens.Property(item => item.QuantidadeAderida).HasColumnType("decimal(18,4)");
        itens.Ignore(item => item.SaldoDisponivel);
        itens.Ignore(item => item.LimiteTotalAdesao);
        itens.Ignore(item => item.SaldoAdesaoDisponivel);
        itens.Ignore(item => item.LimiteAdesaoPorOrgao);
    }

    private static void MapearAdesoes(OwnedNavigationBuilder<Ata, Adesao> adesoes)
    {
        adesoes.ToTable("AtasAdesoes");
        adesoes.WithOwner().HasForeignKey("AtaId");
        adesoes.HasKey(adesao => adesao.Id);
        adesoes.Property(adesao => adesao.Id)
            .HasConversion(id => id.Value, value => new AdesaoId(value))
            .ValueGeneratedNever();
        adesoes.Property(adesao => adesao.ItemAtaId)
            .HasConversion(id => id.Value, value => new ItemAtaId(value));
        adesoes.Property(adesao => adesao.ItemCatalogoId)
            .HasConversion(id => id.Value, value => new ItemCatalogoId(value));
        adesoes.Property(adesao => adesao.CnpjOrgaoAderente).HasMaxLength(20);
        adesoes.Property(adesao => adesao.OrgaoAderente).HasMaxLength(200);
        adesoes.Property(adesao => adesao.Quantidade).HasColumnType("decimal(18,4)");
        adesoes.Property(adesao => adesao.Data);
        adesoes.HasIndex(adesao => adesao.ItemAtaId);
    }

    private static void MapearParticipantes(OwnedNavigationBuilder<Ata, ParticipanteAta> participantes)
    {
        participantes.ToTable("AtasParticipantes");
        participantes.WithOwner().HasForeignKey("AtaId");
        participantes.HasKey(p => p.Id);
        participantes.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new ParticipanteAtaId(value))
            .ValueGeneratedNever();
        participantes.Property(p => p.CnpjOrgao).HasMaxLength(20);
        participantes.Property(p => p.NomeOrgao).HasMaxLength(200);
        participantes.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20);
    }
}

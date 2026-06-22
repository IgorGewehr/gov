using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="FolhaDePagamento"/> e de suas entidades filhas.</summary>
public sealed class FolhaDePagamentoConfiguration : IEntityTypeConfiguration<FolhaDePagamento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FolhaDePagamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FolhasDePagamento");
        builder.HasKey(folha => folha.Id);
        builder.Property(folha => folha.Id)
            .HasConversion(id => id.Value, value => new FolhaDePagamentoId(value))
            .ValueGeneratedNever();

        builder.Property(folha => folha.Competencia)
            .HasConversion(
                competencia => (competencia.Ano * 100) + competencia.Mes,
                valor => Competencia.De(valor / 100, valor % 100));

        builder.Property(folha => folha.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.Property(folha => folha.TotalProventos).HasColumnType("decimal(18,2)");
        builder.Property(folha => folha.TotalDescontos).HasColumnType("decimal(18,2)");

        builder.Property(folha => folha.TotalLiquido)
            .HasConversion(liquido => liquido.Valor, valor => LiquidoAPagar.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.OwnsMany(folha => folha.Eventos, MapearEventos);
        builder.Navigation(folha => folha.Eventos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(folha => new { folha.TenantId, folha.Competencia }).IsUnique();
    }

    private static void MapearEventos(OwnedNavigationBuilder<FolhaDePagamento, EventoFolha> eventos)
    {
        eventos.ToTable("FolhasEventos");
        eventos.WithOwner().HasForeignKey("FolhaDePagamentoId");
        eventos.HasKey(evento => evento.Id);
        eventos.Property(evento => evento.Id)
            .HasConversion(id => id.Value, value => new EventoFolhaId(value))
            .ValueGeneratedNever();

        eventos.Property(evento => evento.Rubrica)
            .HasConversion(rubrica => rubrica.Codigo, codigo => Rubrica.De(codigo))
            .HasMaxLength(30);

        eventos.Property(evento => evento.Tipo).HasConversion<string>().HasMaxLength(20);

        eventos.Property(evento => evento.BaseCalculo)
            .HasConversion(baseCalculo => baseCalculo.Valor, valor => BaseCalculo.De(valor))
            .HasColumnType("decimal(18,2)");

        eventos.Property(evento => evento.Valor).HasColumnType("decimal(18,2)");

        eventos.Property(evento => evento.RegimePrevidenciario).HasConversion<string>().HasMaxLength(10);

        eventos.Ignore(evento => evento.EhDesconto);
    }
}

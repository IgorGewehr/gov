using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Norma"/> e da trilha de vigencia.</summary>
public sealed class NormaConfiguration : IEntityTypeConfiguration<Norma>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Norma> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Normas");
        builder.HasKey(norma => norma.Id);
        builder.Property(norma => norma.Id)
            .HasConversion(id => id.Value, value => new NormaId(value))
            .ValueGeneratedNever();

        builder.Property(norma => norma.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(norma => norma.SituacaoVigencia).HasConversion<string>().HasMaxLength(20);
        builder.Property(norma => norma.Numero);
        builder.Property(norma => norma.Ano);
        builder.Property(norma => norma.DataPromulgacao);
        builder.Property(norma => norma.DataRevogacao);
        builder.Property(norma => norma.TextoArticulado);

        builder.Property(norma => norma.Ementa)
            .HasConversion(ementa => ementa.Valor, valor => Ementa.De(valor))
            .HasColumnName("Ementa")
            .HasMaxLength(Ementa.ComprimentoMaximo);

        // Coluna-sombra (string crua) para busca textual translatavel em SQL sem passar pelo
        // value converter do VO Ementa (que causaria InvalidCastException ao comparar com string).
        // Mantida em sincronia com a coluna Ementa via gatilho de gravacao no SaveChanges do contexto.
        builder.Property<string>("EmentaBusca").HasMaxLength(Ementa.ComprimentoMaximo);
        builder.HasIndex("TenantId", "EmentaBusca");

        builder.Property(norma => norma.ProposicaoOrigemId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ProposicaoId(value.Value) : null);

        builder.Property(norma => norma.NormaRevogadoraId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new NormaId(value.Value) : null);

        builder.Ignore(norma => norma.Terminal);

        builder.OwnsMany(norma => norma.HistoricoVigencia, MapearHistorico);
        builder.Navigation(norma => norma.HistoricoVigencia).UsePropertyAccessMode(PropertyAccessMode.Field);

        // N-2: unicidade logica (tipo, numero, ano) por tenant.
        builder.HasIndex(norma => new { norma.TenantId, norma.Tipo, norma.Numero, norma.Ano }).IsUnique();
        builder.HasIndex(norma => new { norma.TenantId, norma.SituacaoVigencia });
    }

    private static void MapearHistorico(OwnedNavigationBuilder<Norma, EventoVigencia> eventos)
    {
        eventos.ToTable("NormasHistoricoVigencia");
        eventos.WithOwner().HasForeignKey("NormaOwnerId");
        eventos.HasKey(evento => evento.Id);
        eventos.Property(evento => evento.Id)
            .HasConversion(id => id.Value, value => new EventoVigenciaId(value))
            .ValueGeneratedNever();
        eventos.Property(evento => evento.Tipo).HasConversion<string>().HasMaxLength(20);
        eventos.Property(evento => evento.Data);
        eventos.Property(evento => evento.Observacao);
        eventos.Property(evento => evento.NormaReferenciaId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new NormaId(value.Value) : null);
    }
}

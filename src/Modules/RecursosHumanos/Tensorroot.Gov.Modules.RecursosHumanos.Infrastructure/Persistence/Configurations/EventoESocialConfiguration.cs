using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="EventoESocial"/> (eventos eSocial e maquina de estados).</summary>
public sealed class EventoESocialConfiguration : IEntityTypeConfiguration<EventoESocial>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventoESocial> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EventosESocial");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EventoESocialId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(40);
        builder.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Ambiente).HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.IdEvento).HasMaxLength(40).IsRequired();
        builder.Property(e => e.HashXmlGerado).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Xml).IsRequired();
        builder.Property(e => e.XmlAssinado);
        builder.Property(e => e.ThumbprintCertificado).HasMaxLength(80);
        builder.Property(e => e.ProtocoloLote).HasMaxLength(80);
        builder.Property(e => e.NumeroRecibo).HasMaxLength(80);
        builder.Property(e => e.CodigoErro).HasMaxLength(20);
        builder.Property(e => e.DescricaoErro).HasMaxLength(1000);

        // Value Object ChaveIdempotencia (owned): tipo+idNegocio+competencia (mesma tabela do agregado).
        // Idempotencia de geracao: unica por (tipo, idNegocio, competencia). O isolamento por tenant ja
        // vem do banco DEDICADO por tenant (conexao resolvida por tenant) + Global Query Filter.
        builder.OwnsOne(e => e.ChaveIdempotencia, chave =>
        {
            chave.Property(c => c.TipoEvento).HasConversion<string>().HasColumnName("ChaveTipoEvento").HasMaxLength(40).IsRequired();
            chave.Property(c => c.IdNegocio).HasColumnName("ChaveIdNegocio").HasMaxLength(100).IsRequired();
            chave.Property(c => c.Competencia).HasColumnName("ChaveCompetencia").HasMaxLength(7);

            // Idempotencia de geracao no nivel do banco (defesa em profundidade — o guard PRIMARIO e a
            // verificacao ObterPorChaveAsync antes do insert). Indice unico (tipo, idNegocio, competencia).
            // OBS: em SqlServer este indice filtra linhas com competencia NULL (S-1000/1005/1010/2200/2299);
            // para essas, a unicidade fica garantida pela verificacao na Application. // TODO(prod): avaliar
            // indice unico filtrado complementar (HasFilter por provider) para reforco no banco.
            chave.HasIndex(c => new { c.TipoEvento, c.IdNegocio, c.Competencia }).IsUnique();
        });

        builder.Navigation(e => e.ChaveIdempotencia).IsRequired();

        builder.Ignore(e => e.EhTerminalSucesso);

        builder.HasIndex(e => new { e.TenantId, e.Estado });
    }
}

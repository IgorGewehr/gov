using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Ingestao;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do ledger de idempotência da ingestão <see cref="EventoIngerido"/>. Chave por
/// <c>EventId</c>; índice único por <c>(TenantId, EventId)</c> reforça a deduplicação por tenant.
/// </summary>
public sealed class EventoIngeridoConfiguration : IEntityTypeConfiguration<EventoIngerido>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventoIngerido> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EventosIngeridos");
        builder.HasKey(evento => evento.EventId);
        builder.Property(evento => evento.EventId).ValueGeneratedNever();
        builder.Property(evento => evento.TipoEvento).HasMaxLength(100);
        builder.Property(evento => evento.IngeridoEmUtc);

        builder.HasIndex(evento => new { evento.TenantId, evento.EventId }).IsUnique();
    }
}

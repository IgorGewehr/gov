using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do contador atomico <see cref="SequenciaNup"/>: uma linha por
/// <c>(TenantId, Ano)</c>, chave composta — a "cabeca" da sequencia anual do NUP.
/// </summary>
public sealed class SequenciaNupConfiguration : IEntityTypeConfiguration<SequenciaNup>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SequenciaNup> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SequenciasNup");

        // PK composta (TenantId, Ano): uma linha-contador por tenant x exercicio.
        builder.HasKey(sequencia => new { sequencia.TenantId, sequencia.Ano });

        builder.Property(sequencia => sequencia.Ano);
        builder.Property(sequencia => sequencia.UltimoSequencial);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations.Planejamento;

/// <summary>Mapeamento EF Core do agregado <see cref="CreditoAdicional"/> (altera a LOA).</summary>
public sealed class CreditoAdicionalConfiguration : IEntityTypeConfiguration<CreditoAdicional>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CreditoAdicional> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CreditosAdicionais");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CreditoAdicionalId(value))
            .ValueGeneratedNever();
        builder.Property(c => c.LoaId).HasConversion(id => id.Value, value => new LoaId(value));

        builder.Property(c => c.Especie).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Fonte).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Valor).HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(c => c.AtoAutorizador).HasMaxLength(100).IsRequired();
        builder.Property(c => c.AtoAbertura).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PorDecreto);

        builder.Property(c => c.DotacaoAlvoId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, value => value == null ? null : new DotacaoOrcamentariaId(value.Value));
        builder.Property(c => c.DotacaoAnuladaId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, value => value == null ? null : new DotacaoOrcamentariaId(value.Value));

        builder.HasIndex(c => new { c.TenantId, c.LoaId });
    }
}

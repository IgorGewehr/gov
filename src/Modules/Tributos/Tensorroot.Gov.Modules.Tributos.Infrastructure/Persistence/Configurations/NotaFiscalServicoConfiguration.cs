using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do read model <see cref="NotaFiscalServico"/>.</summary>
public sealed class NotaFiscalServicoConfiguration : IEntityTypeConfiguration<NotaFiscalServico>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NotaFiscalServico> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("NotasFiscaisServico");
        builder.HasKey(nota => nota.Id);
        builder.Property(nota => nota.Id)
            .HasConversion(id => id.Value, value => new NotaFiscalServicoId(value))
            .ValueGeneratedNever();

        builder.Property(nota => nota.ChaveAcesso).HasMaxLength(60);
        builder.Property(nota => nota.PrestadorCnpj).HasMaxLength(14);
        builder.Property(nota => nota.TomadorDocumento).HasMaxLength(14);

        builder.Property(nota => nota.ValorServico)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(nota => nota.ValorIss)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(nota => nota.Competencia)
            .HasConversion(competencia => (competencia.Ano * 100) + competencia.Mes, valor => Competencia.De(valor / 100, valor % 100));

        // Deduplicação no nível do banco: uma NFS-e por chave de acesso por tenant.
        builder.HasIndex(nota => new { nota.TenantId, nota.ChaveAcesso }).IsUnique();
    }
}

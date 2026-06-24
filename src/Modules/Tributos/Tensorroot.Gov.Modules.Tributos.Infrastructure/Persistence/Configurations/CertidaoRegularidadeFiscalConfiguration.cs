using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Certidoes;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="CertidaoRegularidadeFiscal"/> (CND/CPEN).</summary>
public sealed class CertidaoRegularidadeFiscalConfiguration : IEntityTypeConfiguration<CertidaoRegularidadeFiscal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CertidaoRegularidadeFiscal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CertidoesRegularidadeFiscal");
        builder.HasKey(certidao => certidao.Id);
        builder.Property(certidao => certidao.Id)
            .HasConversion(id => id.Value, value => new CertidaoRegularidadeFiscalId(value))
            .ValueGeneratedNever();

        builder.Property(certidao => certidao.ContribuinteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(certidao => certidao.Documento).HasMaxLength(14).IsRequired();
        builder.Property(certidao => certidao.NomeContribuinte).HasMaxLength(200).IsRequired();
        builder.Property(certidao => certidao.InscricaoMunicipal).HasMaxLength(30);
        builder.Property(certidao => certidao.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(certidao => certidao.Numero).HasMaxLength(30).IsRequired();
        builder.Property(certidao => certidao.NumeroSequencial);
        builder.Property(certidao => certidao.DataEmissao);
        builder.Property(certidao => certidao.DataValidade);
        builder.Property(certidao => certidao.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(certidao => certidao.Observacao).HasMaxLength(500);
        builder.Property(certidao => certidao.CodigoAutenticacao).HasMaxLength(32).IsRequired();

        builder.Ignore(certidao => certidao.AtestaRegularidade);

        builder.HasIndex(certidao => new { certidao.TenantId, certidao.Numero }).IsUnique();
        builder.HasIndex(certidao => new { certidao.TenantId, certidao.ContribuinteId });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Imovel"/> (cadastro imobiliário/BCI).</summary>
public sealed class ImovelConfiguration : IEntityTypeConfiguration<Imovel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Imovel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Imoveis");
        builder.HasKey(imovel => imovel.Id);
        builder.Property(imovel => imovel.Id)
            .HasConversion(id => id.Value, value => new ImovelId(value))
            .ValueGeneratedNever();

        builder.Property(imovel => imovel.ProprietarioId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(imovel => imovel.Ativo);

        builder.OwnsOne(imovel => imovel.Identificacao, identificacao =>
        {
            identificacao.Property(p => p.InscricaoMunicipal).HasColumnName("InscricaoMunicipal").HasMaxLength(40).IsRequired();
            identificacao.Property(p => p.CibCodigo).HasColumnName("CibCodigo").HasMaxLength(20);
            identificacao.Property(p => p.MatriculaRgi).HasColumnName("MatriculaRgi").HasMaxLength(40);
            identificacao.HasIndex(p => p.InscricaoMunicipal);
        });

        builder.OwnsOne(imovel => imovel.Endereco, endereco =>
        {
            endereco.Property(p => p.Logradouro).HasColumnName("Logradouro").HasMaxLength(200).IsRequired();
            endereco.Property(p => p.Numero).HasColumnName("Numero").HasMaxLength(20);
            endereco.Property(p => p.Complemento).HasColumnName("Complemento").HasMaxLength(100);
            endereco.Property(p => p.Bairro).HasColumnName("Bairro").HasMaxLength(100).IsRequired();
            endereco.Property(p => p.Cep).HasColumnName("Cep").HasMaxLength(8);
            endereco.Property(p => p.SetorQuadraLote).HasColumnName("SetorQuadraLote").HasMaxLength(60).IsRequired();
            endereco.Property(p => p.FaceQuadra).HasColumnName("FaceQuadra").HasMaxLength(60);
            endereco.Property(p => p.ZonaFiscal).HasColumnName("ZonaFiscal").HasMaxLength(60).IsRequired();
        });

        builder.OwnsOne(imovel => imovel.Caracteristicas, caracteristicas =>
        {
            caracteristicas.Property(p => p.AreaTerreno).HasColumnName("AreaTerreno").HasColumnType("decimal(18,2)");
            caracteristicas.Property(p => p.AreaConstruida).HasColumnName("AreaConstruida").HasColumnType("decimal(18,2)");
            caracteristicas.Property(p => p.TipoUso).HasColumnName("TipoUso").HasConversion<string>().HasMaxLength(20);
            caracteristicas.Property(p => p.PadraoConstrutivo).HasColumnName("PadraoConstrutivo").HasMaxLength(30).IsRequired();
            caracteristicas.Property(p => p.AnoConstrucao).HasColumnName("AnoConstrucao");
            caracteristicas.Property(p => p.FracaoIdeal).HasColumnName("FracaoIdeal").HasColumnType("decimal(9,6)");
        });

        builder.Navigation(imovel => imovel.Identificacao).IsRequired();
        builder.Navigation(imovel => imovel.Endereco).IsRequired();
        builder.Navigation(imovel => imovel.Caracteristicas).IsRequired();

        builder.HasIndex(imovel => new { imovel.TenantId, imovel.ProprietarioId });
    }
}

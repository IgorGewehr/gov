using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Escola"/>.</summary>
public sealed class EscolaConfiguration : IEntityTypeConfiguration<Escola>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Escola> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Escolas");
        builder.HasKey(escola => escola.Id);
        builder.Property(escola => escola.Id)
            .HasConversion(id => id.Value, value => new EscolaId(value))
            .ValueGeneratedNever();

        builder.Property(escola => escola.CodigoInep)
            .HasConversion(codigo => codigo.Valor, valor => CodigoInep.Criar(valor))
            .HasMaxLength(CodigoInep.ComprimentoMaximo);

        builder.Property(escola => escola.Nome).HasMaxLength(200);
        builder.Property(escola => escola.DependenciaAdministrativa).HasConversion<string>().HasMaxLength(20);
        builder.Property(escola => escola.Situacao).HasConversion<string>().HasMaxLength(20);

        // Propriedades calculadas (sem coluna).
        builder.Ignore(escola => escola.Ativa);
        builder.Ignore(escola => escola.Encerrada);

        // Objetos de valor (readonly record struct) mapeados como complex types (mesma tabela).
        builder.ComplexProperty(escola => escola.Endereco, MapearEndereco);
        builder.ComplexProperty(escola => escola.Infraestrutura, MapearInfraestrutura);

        builder.HasIndex(escola => new { escola.TenantId, escola.CodigoInep }).IsUnique();
    }

    private static void MapearEndereco(ComplexPropertyBuilder<Endereco> endereco)
    {
        endereco.Property(local => local.Logradouro).HasColumnName("EnderecoLogradouro").HasMaxLength(200);
        endereco.Property(local => local.Municipio).HasColumnName("EnderecoMunicipio").HasMaxLength(120);
        endereco.Property(local => local.Uf).HasColumnName("EnderecoUf").HasMaxLength(2);
        endereco.Property(local => local.Cep).HasColumnName("EnderecoCep").HasMaxLength(8);
        endereco.Property(local => local.Latitude).HasColumnName("EnderecoLatitude");
        endereco.Property(local => local.Longitude).HasColumnName("EnderecoLongitude");
    }

    private static void MapearInfraestrutura(ComplexPropertyBuilder<Infraestrutura> infraestrutura)
    {
        infraestrutura.Property(infra => infra.NumeroSalas).HasColumnName("InfraNumeroSalas");
        infraestrutura.Property(infra => infra.NumeroDependencias).HasColumnName("InfraNumeroDependencias");
        infraestrutura.Property(infra => infra.PossuiAcessibilidade).HasColumnName("InfraPossuiAcessibilidade");
    }
}

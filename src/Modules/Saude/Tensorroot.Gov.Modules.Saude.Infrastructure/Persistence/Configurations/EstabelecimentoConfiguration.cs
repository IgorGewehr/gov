using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Estabelecimento"/>.</summary>
public sealed class EstabelecimentoConfiguration : IEntityTypeConfiguration<Estabelecimento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Estabelecimento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Estabelecimentos");
        builder.HasKey(estabelecimento => estabelecimento.Id);
        builder.Property(estabelecimento => estabelecimento.Id)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value))
            .ValueGeneratedNever();

        builder.Property(estabelecimento => estabelecimento.Cnes)
            .HasConversion(cnes => cnes.Valor, valor => new CodigoCnes(valor))
            .HasMaxLength(CodigoCnes.Comprimento);

        builder.Property(estabelecimento => estabelecimento.Nome)
            .IsRequired()
            .HasMaxLength(Estabelecimento.ComprimentoNome);

        builder.Property(estabelecimento => estabelecimento.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(estabelecimento => estabelecimento.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(estabelecimento => estabelecimento.Endereco, MapearEndereco);

        // Unicidade do CNES por tenant: um estabelecimento por CNES no ente publico.
        builder.HasIndex(estabelecimento => new { estabelecimento.TenantId, estabelecimento.Cnes }).IsUnique();
        builder.HasIndex(estabelecimento => new { estabelecimento.TenantId, estabelecimento.Nome });
    }

    private static void MapearEndereco(ComplexPropertyBuilder<Endereco> endereco)
    {
        endereco.Property(dados => dados.Logradouro).HasColumnName("Logradouro").HasMaxLength(200);
        endereco.Property(dados => dados.Numero).HasColumnName("Numero").HasMaxLength(30);
        endereco.Property(dados => dados.Bairro).HasColumnName("Bairro").HasMaxLength(120);
        endereco.Property(dados => dados.Municipio).HasColumnName("Municipio").HasMaxLength(120);
        endereco.Property(dados => dados.Uf).HasColumnName("Uf").HasMaxLength(Endereco.ComprimentoUf);
        endereco.Property(dados => dados.Cep).HasColumnName("Cep").HasMaxLength(8);
    }
}

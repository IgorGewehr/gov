using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Credores;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="CredorCadastrado"/> (cadastro de credores).</summary>
public sealed class CredorCadastradoConfiguration : IEntityTypeConfiguration<CredorCadastrado>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CredorCadastrado> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Credores");
        builder.HasKey(credor => credor.Id);
        builder.Property(credor => credor.Id)
            .HasConversion(id => id.Value, value => new CredorId(value))
            .ValueGeneratedNever();

        builder.Property(credor => credor.Nome).HasMaxLength(200).IsRequired();
        builder.Property(credor => credor.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(credor => credor.Documento).HasMaxLength(20).IsRequired();
        builder.Property(credor => credor.Situacao).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.OwnsOne(credor => credor.DadosBancarios, banco =>
        {
            banco.Property(b => b.Banco).HasColumnName("Banco").HasMaxLength(20);
            banco.Property(b => b.Agencia).HasColumnName("Agencia").HasMaxLength(20);
            banco.Property(b => b.Conta).HasColumnName("ContaNumero").HasMaxLength(30);
            banco.Property(b => b.Pix).HasColumnName("Pix").HasMaxLength(140);
        });

        // Documento e a chave de negocio unica por tenant (um credor por CPF/CNPJ).
        builder.HasIndex(credor => new { credor.TenantId, credor.Documento }).IsUnique();
        builder.HasIndex(credor => new { credor.TenantId, credor.Nome });
    }
}

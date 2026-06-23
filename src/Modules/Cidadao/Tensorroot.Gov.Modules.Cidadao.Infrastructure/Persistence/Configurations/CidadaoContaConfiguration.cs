using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="CidadaoConta"/>. A senha e persistida APENAS como hash
/// (coluna <c>SenhaHash</c>); a senha em claro nunca toca o banco. O documento e UNICO POR TENANT
/// (par (TenantId, Documento)) — invariante de banco que sustenta a unicidade da conta-cidadao.
/// </summary>
public sealed class CidadaoContaConfiguration : IEntityTypeConfiguration<CidadaoConta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CidadaoConta> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Contas");
        builder.HasKey(conta => conta.Id);
        builder.Property(conta => conta.Id)
            .HasConversion(id => id.Value, value => new CidadaoContaId(value))
            .ValueGeneratedNever();

        builder.Property(conta => conta.TenantId).IsRequired();

        // Documento (CPF 11 / CNPJ 14 digitos, sem mascara).
        builder.Property(conta => conta.Documento).HasMaxLength(14).IsRequired();
        builder.Property(conta => conta.Nome).HasMaxLength(CidadaoConta.ComprimentoMaximoNome).IsRequired();
        builder.Property(conta => conta.Email).HasMaxLength(200);
        builder.Property(conta => conta.Telefone).HasMaxLength(20);

        // Hash BCrypt ("$2a$..." ~ 60 chars; folga para variantes/custos).
        builder.Property(conta => conta.SenhaHash).HasMaxLength(200).IsRequired();

        builder.Property(conta => conta.Origem).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(conta => conta.SeloGovBr).HasConversion<string>().HasMaxLength(10);
        builder.Property(conta => conta.Ativo).IsRequired();
        builder.Property(conta => conta.EmailConfirmado).IsRequired();

        // UNICIDADE da conta por documento POR TENANT (uma conta por CPF/CNPJ por municipio).
        builder.HasIndex(conta => new { conta.TenantId, conta.Documento }).IsUnique();
    }
}

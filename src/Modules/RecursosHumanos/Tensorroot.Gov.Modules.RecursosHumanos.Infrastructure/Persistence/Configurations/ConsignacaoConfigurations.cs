using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do cadastro mestre <see cref="Consignataria"/>.</summary>
public sealed class ConsignatariaConfiguration : IEntityTypeConfiguration<Consignataria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Consignataria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Consignatarias");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new ConsignatariaId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.Cnpj)
            .HasConversion(cnpj => cnpj.Digitos, digitos => Cnpj.Create(digitos))
            .HasMaxLength(14);

        builder.Property(c => c.RazaoSocial).HasMaxLength(200);
        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Situacao).HasConversion<string>().HasMaxLength(20);

        // Unicidade (TenantId, Cnpj) — uma consignataria por CNPJ por ente.
        builder.HasIndex(c => new { c.TenantId, c.Cnpj }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core do catalogo de <see cref="RubricaConsignavel"/> (parametrizacao por tenant).</summary>
public sealed class RubricaConsignavelConfiguration : IEntityTypeConfiguration<RubricaConsignavel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RubricaConsignavel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RubricasConsignaveis");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RubricaConsignavelId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.Codigo).HasMaxLength(30);
        builder.Property(r => r.Descricao).HasMaxLength(200);
        builder.Property(r => r.Categoria).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.GrupoMargem).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.ContaParaMargem);
        builder.Property(r => r.Ativa);

        // Um codigo de rubrica consignavel por tenant.
        builder.HasIndex(r => new { r.TenantId, r.Codigo }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="ContratoConsignacao"/>.</summary>
public sealed class ContratoConsignacaoConfiguration : IEntityTypeConfiguration<ContratoConsignacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContratoConsignacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ContratosConsignacao");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new ContratoConsignacaoId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.ServidorId)
            .HasConversion(id => id.Value, value => new ServidorId(value));
        builder.Property(c => c.ConsignatariaId)
            .HasConversion(id => id.Value, value => new ConsignatariaId(value));

        builder.Property(c => c.CodigoRubrica).HasMaxLength(30);
        builder.Property(c => c.Categoria).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.GrupoMargem).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.NumeroContratoExterno).HasMaxLength(60);
        builder.Property(c => c.ValorParcela).HasPrecision(18, 2);
        builder.Property(c => c.QuantidadeParcelas);
        builder.Property(c => c.ParcelasPagas);
        builder.Property(c => c.DataAverbacao);
        builder.Property(c => c.Situacao).HasConversion<string>().HasMaxLength(20);

        // Consulta de margem rapida: contratos averbados do servidor (CLAUDE.md §5/§7 — prefixo TenantId).
        builder.HasIndex(c => new { c.TenantId, c.ServidorId, c.Situacao });
        builder.HasIndex(c => new { c.TenantId, c.ConsignatariaId });
    }
}

/// <summary>Mapeamento EF Core de <see cref="ParametrosMargemVigente"/> (percentuais por vigencia).</summary>
public sealed class ParametrosMargemVigenteConfiguration : IEntityTypeConfiguration<ParametrosMargemVigente>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParametrosMargemVigente> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParametrosMargem");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new ParametrosMargemVigenteId(value))
            .ValueGeneratedNever();

        // Competencia (VO Ano/Mes) persistida como inteiro Ano*100+Mes (mesmo padrao de RegraAfastamento).
        builder.Property(p => p.VigenciaInicio)
            .HasConversion(
                competencia => (competencia.Ano * 100) + competencia.Mes,
                valor => Domain.Folha.Competencia.De(valor / 100, valor % 100))
            .HasColumnName("VigenciaInicio");

        builder.Property(p => p.PercentualGeral).HasPrecision(5, 4);
        builder.Property(p => p.PercentualCartaoConsignado).HasPrecision(5, 4);
        builder.Property(p => p.PercentualCartaoBeneficio).HasPrecision(5, 4);

        // Uma versao por (tenant, vigencia).
        builder.HasIndex(p => new { p.TenantId, p.VigenciaInicio }).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core da configuracao do portal publico (slug do ente).</summary>
public sealed class PortalPublicoConfigConfiguration : IEntityTypeConfiguration<PortalPublicoConfig>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PortalPublicoConfig> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PortalPublicoConfig");
        builder.HasKey(config => config.Id);
        builder.Property(config => config.Id)
            .HasConversion(id => id.Value, value => new PortalPublicoConfigId(value))
            .ValueGeneratedNever();

        builder.Property(config => config.Slug).HasMaxLength(120).IsRequired();
        builder.Property(config => config.NomeEnte).HasMaxLength(200).IsRequired();
        builder.Property(config => config.Ativo);

        // Slug unico por tenant; o unico portal por tenant e garantido pelo handler (1 config por tenant).
        builder.HasIndex(config => new { config.TenantId, config.Slug }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core do read model publico <see cref="PublicacaoDespesa"/>.</summary>
public sealed class PublicacaoDespesaConfiguration : IEntityTypeConfiguration<PublicacaoDespesa>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PublicacaoDespesa> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PublicacaoDespesa");
        builder.HasKey(despesa => despesa.Id);
        builder.Property(despesa => despesa.Id).ValueGeneratedNever();

        builder.Property(despesa => despesa.Exercicio);
        builder.Property(despesa => despesa.Fase).HasConversion<string>().HasMaxLength(20);
        builder.Property(despesa => despesa.NumeroEmpenho).HasMaxLength(40);
        builder.Property(despesa => despesa.CredorNomeOuRazao).HasMaxLength(200);
        builder.Property(despesa => despesa.CredorDocMascarado).HasMaxLength(30);
        builder.Property(despesa => despesa.FuncaoSubfuncao).HasMaxLength(10);
        builder.Property(despesa => despesa.FonteRecurso).HasMaxLength(10);
        builder.Property(despesa => despesa.Valor).HasColumnType("decimal(18,2)");
        builder.Property(despesa => despesa.Data);

        // Idempotencia da projecao (I-13): a mesma origem nao duplica linha no tenant.
        builder.HasIndex(despesa => new { despesa.TenantId, despesa.OrigemEventoId }).IsUnique();
        builder.HasIndex(despesa => new { despesa.TenantId, despesa.Exercicio, despesa.Fase });
    }
}

/// <summary>Mapeamento EF Core do read model publico <see cref="PublicacaoReceita"/>.</summary>
public sealed class PublicacaoReceitaConfiguration : IEntityTypeConfiguration<PublicacaoReceita>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PublicacaoReceita> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PublicacaoReceita");
        builder.HasKey(receita => receita.Id);
        builder.Property(receita => receita.Id).ValueGeneratedNever();

        builder.Property(receita => receita.Exercicio);
        builder.Property(receita => receita.RubricaReceita).HasMaxLength(120);
        builder.Property(receita => receita.FonteRecurso).HasMaxLength(10);
        builder.Property(receita => receita.Valor).HasColumnType("decimal(18,2)");
        builder.Property(receita => receita.Data);

        builder.HasIndex(receita => new { receita.TenantId, receita.OrigemEventoId }).IsUnique();
        builder.HasIndex(receita => new { receita.TenantId, receita.Exercicio });
    }
}

/// <summary>Mapeamento EF Core do read model publico <see cref="PublicacaoContrato"/>.</summary>
public sealed class PublicacaoContratoConfiguration : IEntityTypeConfiguration<PublicacaoContrato>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PublicacaoContrato> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PublicacaoContrato");
        builder.HasKey(contrato => contrato.Id);
        builder.Property(contrato => contrato.Id).ValueGeneratedNever();

        builder.Property(contrato => contrato.Exercicio);
        builder.Property(contrato => contrato.NumeroContrato).HasMaxLength(80);
        builder.Property(contrato => contrato.Fornecedor).HasMaxLength(200);
        builder.Property(contrato => contrato.Objeto).HasMaxLength(2000);
        builder.Property(contrato => contrato.Valor).HasColumnType("decimal(18,2)");
        builder.Property(contrato => contrato.Modalidade).HasMaxLength(60);
        builder.Property(contrato => contrato.NumeroContratoPncp).HasMaxLength(80);

        // OrigemEventoId aqui = identificador do CONTRATO (agrega eventos do mesmo contrato).
        builder.HasIndex(contrato => new { contrato.TenantId, contrato.OrigemEventoId }).IsUnique();
        builder.HasIndex(contrato => new { contrato.TenantId, contrato.Exercicio });
    }
}

/// <summary>Mapeamento EF Core do read model publico <see cref="PublicacaoFolhaNominal"/> (sem CPF/matricula).</summary>
public sealed class PublicacaoFolhaNominalConfiguration : IEntityTypeConfiguration<PublicacaoFolhaNominal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PublicacaoFolhaNominal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PublicacaoFolhaNominal");
        builder.HasKey(folha => folha.Id);
        builder.Property(folha => folha.Id).ValueGeneratedNever();

        builder.Property(folha => folha.OrigemEventoId).HasMaxLength(120).IsRequired();
        builder.Property(folha => folha.Competencia).HasMaxLength(7).IsRequired();
        builder.Property(folha => folha.ServidorNome).HasMaxLength(200).IsRequired();
        builder.Property(folha => folha.CargoDescricao).HasMaxLength(200);
        builder.Property(folha => folha.Lotacao).HasMaxLength(200);
        builder.Property(folha => folha.RemuneracaoBruta).HasColumnType("decimal(18,2)");
        builder.Property(folha => folha.Descontos).HasColumnType("decimal(18,2)");
        builder.Property(folha => folha.Liquido).HasColumnType("decimal(18,2)");

        builder.HasIndex(folha => new { folha.TenantId, folha.OrigemEventoId }).IsUnique();
        builder.HasIndex(folha => new { folha.TenantId, folha.Competencia });
    }
}

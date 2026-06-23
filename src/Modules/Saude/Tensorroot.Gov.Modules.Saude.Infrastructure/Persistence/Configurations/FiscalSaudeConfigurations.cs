using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core das regras de classificação ASPS (S-1).</summary>
public sealed class RegraClassificacaoAspsConfiguration : IEntityTypeConfiguration<RegraClassificacaoAsps>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RegraClassificacaoAsps> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RegrasClassificacaoAsps");
        builder.HasKey(regra => regra.Id);
        builder.Property(regra => regra.Id)
            .HasConversion(id => id.Value, value => new RegraClassificacaoAspsId(value))
            .ValueGeneratedNever();

        builder.Property(regra => regra.Funcao).HasMaxLength(2);
        builder.Property(regra => regra.Subfuncao).HasMaxLength(3);
        builder.Property(regra => regra.FonteRecurso).HasMaxLength(10);
        builder.Property(regra => regra.Efeito).HasConversion<string>().HasMaxLength(10);
        builder.Property(regra => regra.Descricao).HasMaxLength(200);
        builder.Property(regra => regra.VigenciaInicio);

        builder.HasIndex(regra => new { regra.TenantId, regra.Funcao, regra.Subfuncao, regra.FonteRecurso, regra.VigenciaInicio });
    }
}

/// <summary>Mapeamento EF Core do Fundo Municipal de Saúde e suas contas por bloco (S-2).</summary>
public sealed class FundoMunicipalSaudeConfiguration : IEntityTypeConfiguration<FundoMunicipalSaude>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FundoMunicipalSaude> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FundosMunicipaisSaude");
        builder.HasKey(fundo => fundo.Id);
        builder.Property(fundo => fundo.Id)
            .HasConversion(id => id.Value, value => new FundoMunicipalSaudeId(value))
            .ValueGeneratedNever();

        builder.Property(fundo => fundo.Nome).HasMaxLength(200);
        builder.Property(fundo => fundo.Cnpj).HasMaxLength(14);

        builder.HasIndex(fundo => fundo.TenantId);

        // Contas por bloco como entidades-filhas do agregado (execução segregada na mesma fronteira).
        builder.OwnsMany(fundo => fundo.Contas, conta =>
        {
            conta.ToTable("ContasBlocoFinanciamentoSaude");
            conta.WithOwner().HasForeignKey(c => c.FundoId);
            conta.HasKey(c => c.Id);
            conta.Property(c => c.Id)
                .HasConversion(id => id.Value, value => new ContaBlocoFinanciamentoId(value))
                .ValueGeneratedNever();
            conta.Property(c => c.FundoId)
                .HasConversion(id => id.Value, value => new FundoMunicipalSaudeId(value));
            conta.Property(c => c.Bloco).HasConversion<string>().HasMaxLength(20);
            conta.Property(c => c.FonteRecurso).HasMaxLength(10);
            conta.Property(c => c.TotalRecebido).HasColumnType("decimal(18,2)");
            conta.Property(c => c.TotalExecutado).HasColumnType("decimal(18,2)");
            // Uma única conta por (fundo, bloco, fonte): a segregação por bloco é invariante. O tenant é
            // herdado do dono (FMS) — owned type não carrega TenantId próprio.
            conta.HasIndex(c => new { c.FundoId, c.Bloco, c.FonteRecurso }).IsUnique();
        });

        builder.Navigation(fundo => fundo.Contas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Mapeamento EF Core do read model de linhas de execução de Saúde (S-1, Via A2).</summary>
public sealed class LinhaExecucaoSaudeConfiguration : IEntityTypeConfiguration<LinhaExecucaoSaude>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LinhaExecucaoSaude> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LinhasExecucaoSaude");
        builder.HasKey(linha => linha.Id);
        builder.Property(linha => linha.Id).ValueGeneratedNever();

        builder.Property(linha => linha.Exercicio);
        builder.Property(linha => linha.Tipo).HasConversion<string>().HasMaxLength(40);
        builder.Property(linha => linha.Funcao).HasMaxLength(2);
        builder.Property(linha => linha.Subfuncao).HasMaxLength(3);
        builder.Property(linha => linha.FonteRecurso).HasMaxLength(10);
        builder.Property(linha => linha.Valor).HasColumnType("decimal(18,2)");
        builder.Property(linha => linha.OrigemHash).HasMaxLength(64);

        builder.HasIndex(linha => new { linha.TenantId, linha.OrigemHash }).IsUnique();
        builder.HasIndex(linha => new { linha.TenantId, linha.Exercicio });
    }
}

/// <summary>Mapeamento EF Core dos percentuais fiscais versionados de Saúde (S-1).</summary>
public sealed class ParametroFiscalSaudeConfiguration : IEntityTypeConfiguration<ParametroFiscalSaude>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParametroFiscalSaude> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParametrosFiscaisSaude");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Chave).HasMaxLength(60);
        builder.Property(p => p.VigenciaInicio);
        builder.Property(p => p.Valor).HasColumnType("decimal(18,6)");

        builder.HasIndex(p => new { p.TenantId, p.Chave, p.VigenciaInicio }).IsUnique();
    }
}

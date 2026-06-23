using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core de <see cref="FonteRecursoVinculado"/> (classificador setorial M7.0.0).</summary>
public sealed class FonteRecursoVinculadoConfiguration : IEntityTypeConfiguration<FonteRecursoVinculado>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FonteRecursoVinculado> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FontesRecursoVinculado");
        builder.HasKey(regra => regra.Id);
        builder.Property(regra => regra.Id)
            .HasConversion(id => id.Value, value => new FonteRecursoVinculadoId(value))
            .ValueGeneratedNever();

        builder.Property(regra => regra.Funcao).HasMaxLength(2);
        builder.Property(regra => regra.FonteRecurso).HasMaxLength(10);
        builder.Property(regra => regra.Setor).HasConversion<string>().HasMaxLength(20);
        builder.Property(regra => regra.ComputaNoMinimo);
        builder.Property(regra => regra.VigenciaInicio);

        builder.HasIndex(regra => new { regra.TenantId, regra.Funcao, regra.FonteRecurso, regra.VigenciaInicio });
    }
}

/// <summary>Mapeamento EF Core de <see cref="CalendarioFederal"/> (prazos M7.0.1).</summary>
public sealed class CalendarioFederalConfiguration : IEntityTypeConfiguration<CalendarioFederal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CalendarioFederal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CalendariosFederais");
        builder.HasKey(prazo => prazo.Id);
        builder.Property(prazo => prazo.Id)
            .HasConversion(id => id.Value, value => new CalendarioFederalId(value))
            .ValueGeneratedNever();

        builder.Property(prazo => prazo.Chave).HasMaxLength(40);
        builder.Property(prazo => prazo.Exercicio);
        builder.Property(prazo => prazo.Periodo);
        builder.Property(prazo => prazo.DataLimite);
        builder.Property(prazo => prazo.Descricao).HasMaxLength(500);

        builder.HasIndex(prazo => new { prazo.TenantId, prazo.Exercicio, prazo.Chave });
    }
}

/// <summary>Mapeamento EF Core de <see cref="ParecerConselho"/> (parecer de conselho M7.0.2).</summary>
public sealed class ParecerConselhoConfiguration : IEntityTypeConfiguration<ParecerConselho>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParecerConselho> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PareceresConselho");
        builder.HasKey(parecer => parecer.Id);
        builder.Property(parecer => parecer.Id)
            .HasConversion(id => id.Value, value => new ParecerConselhoId(value))
            .ValueGeneratedNever();

        builder.Property(parecer => parecer.Conselho).HasConversion<string>().HasMaxLength(20);
        builder.Property(parecer => parecer.Setor).HasConversion<string>().HasMaxLength(20);
        builder.Property(parecer => parecer.Exercicio);
        builder.Property(parecer => parecer.DataParecer);
        builder.Property(parecer => parecer.NumeroResolucao).HasMaxLength(60);
        builder.Property(parecer => parecer.Resultado).HasConversion<string>().HasMaxLength(20);
        builder.Property(parecer => parecer.Observacao).HasMaxLength(2000);

        builder.HasIndex(parecer => new { parecer.TenantId, parecer.Exercicio, parecer.Conselho });
    }
}

/// <summary>Mapeamento EF Core do read model <see cref="LinhaExecucaoFiscal"/> (execução M7.0.0 Via A2).</summary>
public sealed class LinhaExecucaoFiscalConfiguration : IEntityTypeConfiguration<LinhaExecucaoFiscal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LinhaExecucaoFiscal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LinhasExecucaoFiscal");
        builder.HasKey(linha => linha.Id);
        builder.Property(linha => linha.Id)
            .HasConversion(id => id.Value, value => new LinhaExecucaoFiscalId(value))
            .ValueGeneratedNever();

        builder.Property(linha => linha.Exercicio);
        builder.Property(linha => linha.Tipo).HasConversion<string>().HasMaxLength(40);
        builder.Property(linha => linha.Funcao).HasMaxLength(2);
        builder.Property(linha => linha.FonteRecurso).HasMaxLength(10);
        builder.Property(linha => linha.Valor).HasColumnType("decimal(18,2)");
        builder.Property(linha => linha.OrigemHash).HasMaxLength(64);

        // Idempotencia da projecao (I-13): a mesma origem nao duplica linha no tenant.
        builder.HasIndex(linha => new { linha.TenantId, linha.OrigemHash }).IsUnique();
        builder.HasIndex(linha => new { linha.TenantId, linha.Exercicio });
    }
}

/// <summary>Mapeamento EF Core de <see cref="ParametroFiscalVigente"/> (percentuais versionados M7.0.3).</summary>
public sealed class ParametroFiscalVigenteConfiguration : IEntityTypeConfiguration<ParametroFiscalVigente>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParametroFiscalVigente> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParametrosFiscaisVigentes");
        builder.HasKey(parametro => parametro.Id);
        builder.Property(parametro => parametro.Id).ValueGeneratedNever();

        builder.Property(parametro => parametro.Chave).HasMaxLength(60);
        builder.Property(parametro => parametro.VigenciaInicio);
        builder.Property(parametro => parametro.Valor).HasColumnType("decimal(18,6)");

        builder.HasIndex(parametro => new { parametro.TenantId, parametro.Chave, parametro.VigenciaInicio }).IsUnique();
    }
}

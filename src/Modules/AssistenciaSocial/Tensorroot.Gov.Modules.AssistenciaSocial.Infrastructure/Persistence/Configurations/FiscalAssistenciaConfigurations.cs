using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Configurations;

/// <summary>A-1: mapeamento EF Core do FMAS e suas contas por (bloco, piso, fonte).</summary>
public sealed class FundoMunicipalAssistenciaConfiguration : IEntityTypeConfiguration<FundoMunicipalAssistencia>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FundoMunicipalAssistencia> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FundosMunicipaisAssistencia");
        builder.HasKey(fundo => fundo.Id);
        builder.Property(fundo => fundo.Id)
            .HasConversion(id => id.Value, value => new FundoMunicipalAssistenciaId(value))
            .ValueGeneratedNever();

        builder.Property(fundo => fundo.Nome).HasMaxLength(200);
        builder.Property(fundo => fundo.Cnpj).HasMaxLength(14);

        builder.HasIndex(fundo => fundo.TenantId);

        // Contas por (bloco, piso, fonte) como entidades-filhas do agregado (execucao segregada).
        builder.OwnsMany(fundo => fundo.Contas, conta =>
        {
            conta.ToTable("ContasCofinanciamentoSuas");
            conta.WithOwner().HasForeignKey(c => c.FundoId);
            conta.HasKey(c => c.Id);
            conta.Property(c => c.Id)
                .HasConversion(id => id.Value, value => new ContaCofinanciamentoSuasId(value))
                .ValueGeneratedNever();
            conta.Property(c => c.FundoId)
                .HasConversion(id => id.Value, value => new FundoMunicipalAssistenciaId(value));
            conta.Property(c => c.Bloco).HasConversion<string>().HasMaxLength(50);
            conta.Property(c => c.Piso).HasConversion<string>().HasMaxLength(30);
            conta.Property(c => c.FonteRecurso).HasMaxLength(10);
            conta.Property(c => c.TotalRecebido).HasColumnType("decimal(18,2)");
            conta.Property(c => c.TotalExecutado).HasColumnType("decimal(18,2)");
            // Uma unica conta por (fundo, bloco, piso, fonte): a segregacao e invariante. O tenant e
            // herdado do dono (FMAS) — owned type nao carrega TenantId proprio.
            conta.HasIndex(c => new { c.FundoId, c.Bloco, c.Piso, c.FonteRecurso }).IsUnique();
        });

        builder.Navigation(fundo => fundo.Contas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>A-2: mapeamento EF Core do RMA e suas linhas por servico.</summary>
public sealed class RegistroMensalAtendimentoConfiguration : IEntityTypeConfiguration<RegistroMensalAtendimento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RegistroMensalAtendimento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RegistrosMensaisAtendimento");
        builder.HasKey(rma => rma.Id);
        builder.Property(rma => rma.Id)
            .HasConversion(id => id.Value, value => new RegistroMensalAtendimentoId(value))
            .ValueGeneratedNever();

        builder.Property(rma => rma.TipoUnidade).HasConversion<string>().HasMaxLength(20);
        builder.Property(rma => rma.Situacao).HasConversion<string>().HasMaxLength(10);
        builder.Property(rma => rma.FechadoEmUtc);

        // Competencia mapeada como inteiro (Ano*100+Mes), espelhando o agregado Beneficio.
        builder.Property(rma => rma.Competencia)
            .HasConversion(competencia => (competencia.Ano * 100) + competencia.Mes, valor => Competencia.De(valor / 100, valor % 100));

        // TotalAtendimentos e propriedade calculada (sem coluna).
        builder.Ignore(rma => rma.TotalAtendimentos);

        // Uma unica consolidacao por (tenant, unidade, competencia): chave de negocio do RMA.
        builder.HasIndex(rma => new { rma.TenantId, rma.UnidadeAtendimentoId, rma.Competencia })
            .HasDatabaseName("IX_RMA_Tenant_Unidade_Competencia")
            .IsUnique();

        builder.OwnsMany(rma => rma.Linhas, linha =>
        {
            linha.ToTable("LinhasRmaServico");
            linha.WithOwner().HasForeignKey(l => l.RmaId);
            linha.HasKey(l => l.Id);
            linha.Property(l => l.Id)
                .HasConversion(id => id.Value, value => new LinhaRmaServicoId(value))
                .ValueGeneratedNever();
            linha.Property(l => l.RmaId)
                .HasConversion(id => id.Value, value => new RegistroMensalAtendimentoId(value));
            linha.Property(l => l.Servico).HasConversion<string>().HasMaxLength(20);
            linha.Property(l => l.Quantidade);
            linha.HasIndex(l => new { l.RmaId, l.Servico }).IsUnique();
        });

        builder.Navigation(rma => rma.Linhas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>A-0: mapeamento EF Core dos criterios municipais de beneficio eventual versionados.</summary>
public sealed class CriterioBeneficioEventualMunicipalConfiguration : IEntityTypeConfiguration<CriterioBeneficioEventualMunicipal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CriterioBeneficioEventualMunicipal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CriteriosBeneficioEventualMunicipal");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Modalidade).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.VigenciaInicio);
        builder.Property(c => c.MultiploRendaSalarioMinimo).HasColumnType("decimal(18,6)");

        builder.HasIndex(c => new { c.TenantId, c.Modalidade, c.VigenciaInicio }).IsUnique();
    }
}

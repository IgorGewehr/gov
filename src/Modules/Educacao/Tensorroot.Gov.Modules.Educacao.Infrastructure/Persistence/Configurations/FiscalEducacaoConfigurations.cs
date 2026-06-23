using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Fiscal;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core das regras de classificação MDE (E-1).</summary>
public sealed class RegraClassificacaoMdeConfiguration : IEntityTypeConfiguration<RegraClassificacaoMde>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RegraClassificacaoMde> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RegrasClassificacaoMde");
        builder.HasKey(regra => regra.Id);
        builder.Property(regra => regra.Id)
            .HasConversion(id => id.Value, value => new RegraClassificacaoMdeId(value))
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

/// <summary>Mapeamento EF Core da distribuição do FUNDEB e suas contas por origem (E-3).</summary>
public sealed class DistribuicaoFundebConfiguration : IEntityTypeConfiguration<DistribuicaoFundeb>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DistribuicaoFundeb> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DistribuicoesFundeb");
        builder.HasKey(distribuicao => distribuicao.Id);
        builder.Property(distribuicao => distribuicao.Id)
            .HasConversion(id => id.Value, value => new DistribuicaoFundebId(value))
            .ValueGeneratedNever();

        builder.Property(distribuicao => distribuicao.Exercicio);

        // Uma distribuição por (tenant, exercício): a apuração E-2/E-3 ancora no exercício.
        builder.HasIndex(distribuicao => new { distribuicao.TenantId, distribuicao.Exercicio }).IsUnique();

        // Contas por origem como entidades-filhas do agregado (conciliação por origem na mesma fronteira).
        builder.OwnsMany(distribuicao => distribuicao.Contas, conta =>
        {
            conta.ToTable("ContasOrigemFundeb");
            conta.WithOwner().HasForeignKey(c => c.DistribuicaoId);
            conta.HasKey(c => c.Id);
            conta.Property(c => c.Id)
                .HasConversion(id => id.Value, value => new ContaOrigemFundebId(value))
                .ValueGeneratedNever();
            conta.Property(c => c.DistribuicaoId)
                .HasConversion(id => id.Value, value => new DistribuicaoFundebId(value));
            conta.Property(c => c.Origem).HasConversion<string>().HasMaxLength(30);
            conta.Property(c => c.ValorEsperado).HasColumnType("decimal(18,2)");
            conta.Property(c => c.TotalRecebido).HasColumnType("decimal(18,2)");
            // Uma única conta por (distribuição, origem): a conciliação por origem é invariante.
            conta.HasIndex(c => new { c.DistribuicaoId, c.Origem }).IsUnique();
        });

        builder.Navigation(distribuicao => distribuicao.Contas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Mapeamento EF Core do read model de linhas de execução de Educação (E-1, Via A2).</summary>
public sealed class LinhaExecucaoEducacaoConfiguration : IEntityTypeConfiguration<LinhaExecucaoEducacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LinhaExecucaoEducacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LinhasExecucaoEducacao");
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

/// <summary>Mapeamento EF Core dos percentuais fiscais versionados de Educação (E-1/E-2).</summary>
public sealed class ParametroFiscalEducacaoConfiguration : IEntityTypeConfiguration<ParametroFiscalEducacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParametroFiscalEducacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParametrosFiscaisEducacao");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Chave).HasMaxLength(60);
        builder.Property(p => p.VigenciaInicio);
        builder.Property(p => p.Valor).HasColumnType("decimal(18,6)");

        builder.HasIndex(p => new { p.TenantId, p.Chave, p.VigenciaInicio }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core do read model da remuneração dos profissionais da educação (E-2).</summary>
public sealed class RemuneracaoMagisterioExercicioConfiguration : IEntityTypeConfiguration<RemuneracaoMagisterioExercicio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RemuneracaoMagisterioExercicio> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RemuneracoesMagisterio");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Exercicio);
        builder.Property(r => r.RemuneracaoProfissionais).HasColumnType("decimal(18,2)");

        // Um registro por (tenant, exercício): o cruzamento do 70% é por exercício.
        builder.HasIndex(r => new { r.TenantId, r.Exercicio }).IsUnique();
    }
}

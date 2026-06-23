using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Afastamento"/>.</summary>
public sealed class AfastamentoConfiguration : IEntityTypeConfiguration<Afastamento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Afastamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Afastamentos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AfastamentoId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.ServidorId)
            .HasConversion(id => id.Value, value => new ServidorId(value));

        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(40);
        builder.Property(a => a.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Inicio);
        builder.Property(a => a.FimPrevisto);
        builder.Property(a => a.FimEfetivo);
        builder.Property(a => a.Documento).HasMaxLength(200);
        builder.Property(a => a.SuspendeProventos);
        builder.Property(a => a.PercentualRemuneracao).HasPrecision(5, 2);
        builder.Property(a => a.DiasPagosPeloEnte);
        builder.Property(a => a.ContaTempo);
        builder.Property(a => a.CodigoEventoESocial).HasMaxLength(20);

        // Indices SEMPRE prefixados por TenantId (CLAUDE.md §5/§7). Busca por servidor e por situacao.
        builder.HasIndex(a => new { a.TenantId, a.ServidorId, a.Situacao });
        builder.HasIndex(a => new { a.TenantId, a.Tipo });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="RegraAfastamento"/> (parametrizacao por tenant/vigencia).</summary>
public sealed class RegraAfastamentoConfiguration : IEntityTypeConfiguration<RegraAfastamento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RegraAfastamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RegrasAfastamento");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RegraAfastamentoId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(40);

        // Competencia (VO Ano/Mes) persistida como inteiro Ano*100+Mes (mesmo padrao de TabelasLegais).
        builder.Property(r => r.VigenciaInicio)
            .HasConversion(
                competencia => (competencia.Ano * 100) + competencia.Mes,
                valor => Domain.Folha.Competencia.De(valor / 100, valor % 100))
            .HasColumnName("VigenciaInicio");

        builder.Property(r => r.SuspendeProventos);
        builder.Property(r => r.PercentualRemuneracao).HasPrecision(5, 2);
        builder.Property(r => r.DiasPagosPeloEnte);
        builder.Property(r => r.ContaTempo);
        builder.Property(r => r.DuracaoPadraoDias);
        builder.Property(r => r.CodigoEventoESocial).HasMaxLength(20);

        // Uma regra por (tenant, tipo, vigencia) — nao duplicar a mesma versao.
        builder.HasIndex(r => new { r.TenantId, r.Tipo, r.VigenciaInicio }).IsUnique();
    }
}

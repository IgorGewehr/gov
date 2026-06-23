using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="MarcacaoPonto"/> (AFD append-only).</summary>
public sealed class MarcacaoPontoConfiguration : IEntityTypeConfiguration<MarcacaoPonto>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MarcacaoPonto> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PontoMarcacoes");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new MarcacaoPontoId(value))
            .ValueGeneratedNever();

        builder.Property(m => m.Cpf)
            .HasConversion(cpf => cpf.Digitos, digitos => Cpf.Create(digitos))
            .HasMaxLength(11);

        builder.Property(m => m.Nsr)
            .HasConversion(nsr => nsr.Valor, valor => Nsr.De(valor))
            .HasColumnName("Nsr");

        builder.Property(m => m.DataHora);
        builder.Property(m => m.Sentido).HasConversion<string>().HasMaxLength(10);
        builder.Property(m => m.Origem).HasConversion<string>().HasMaxLength(10);

        // Origem de equipamento (ingestao de AFD): REP coletado + NSR original do equipamento.
        builder.Property(m => m.RepId);
        builder.Property(m => m.NsrEquipamento);

        // NSR unico e sequencial por tenant (REP) — garante a sequencia sem lacunas/duplicidade.
        builder.HasIndex(m => new { m.TenantId, m.Nsr }).IsUnique();
        builder.HasIndex(m => new { m.TenantId, m.ServidorId, m.DataHora });

        // IDEMPOTENCIA da ingestao de AFD: a chave natural (TenantId, RepId, NsrEquipamento) e UNICA —
        // reimportar o mesmo AFD nao duplica marcacoes ja ingeridas. Indice FILTRADO so para marcacoes
        // de equipamento (RepId IS NOT NULL): as marcacoes proprias do REP-P (RepId/NsrEquipamento nulos)
        // ficam de fora — no SqlServer um indice unico nao-filtrado rejeitaria varias linhas com NULL.
        // // TODO(prod): o filtro e sintaxe SqlServer; no SQLite (dev) o EnsureCreated ignora o filtro, e
        // o dedup em lote no handler garante a idempotencia independentemente do indice.
        builder.HasIndex(m => new { m.TenantId, m.RepId, m.NsrEquipamento })
            .IsUnique()
            .HasFilter("[RepId] IS NOT NULL");
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="RepConfigurado"/> (parque de equipamentos REP).</summary>
public sealed class RepConfiguradoConfiguration : IEntityTypeConfiguration<RepConfigurado>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RepConfigurado> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PontoReps");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RepConfiguradoId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.IdentificacaoEquipamento).IsRequired().HasMaxLength(120);
        builder.Property(r => r.Marca).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Modos).HasConversion<string>().HasMaxLength(40);
        builder.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(10);
        builder.Property(r => r.EnderecoOuReferencia).HasMaxLength(200);
        builder.Property(r => r.ReferenciaCredencialCofre).HasMaxLength(200);
        builder.Property(r => r.UltimoNsrColetado);
        builder.Property(r => r.Ativo);

        builder.HasIndex(r => new { r.TenantId, r.Ativo });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="JornadaTrabalho"/>.</summary>
public sealed class JornadaTrabalhoConfiguration : IEntityTypeConfiguration<JornadaTrabalho>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<JornadaTrabalho> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PontoJornadas");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id)
            .HasConversion(id => id.Value, value => new JornadaTrabalhoId(value))
            .ValueGeneratedNever();

        builder.Property(j => j.Regime).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(j => j.CargaSemanalEstimadaMinutos);

        builder.HasIndex(j => new { j.TenantId, j.ServidorId, j.Ativa });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="ApuracaoPonto"/>.</summary>
public sealed class ApuracaoPontoConfiguration : IEntityTypeConfiguration<ApuracaoPonto>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApuracaoPonto> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PontoApuracoes");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new ApuracaoPontoId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.Competencia)
            .HasConversion(
                competencia => (competencia.Ano * 100) + competencia.Mes,
                valor => Competencia.De(valor / 100, valor % 100))
            .HasColumnName("Competencia");

        builder.Property(a => a.Situacao).HasConversion<string>().HasMaxLength(15);

        builder.Ignore(a => a.SaldoPeriodoMinutos);
        builder.Ignore(a => a.SaldoBancoHorasAtualMinutos);

        // Uma apuracao por servidor por competencia (no tenant).
        builder.HasIndex(a => new { a.TenantId, a.ServidorId, a.Competencia }).IsUnique();
    }
}

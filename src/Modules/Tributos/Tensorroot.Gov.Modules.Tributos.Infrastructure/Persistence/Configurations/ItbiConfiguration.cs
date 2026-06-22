using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="AliquotaItbi"/>.</summary>
public sealed class AliquotaItbiConfiguration : IEntityTypeConfiguration<AliquotaItbi>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AliquotaItbi> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AliquotasItbi");
        builder.HasKey(aliquota => aliquota.Id);
        builder.Property(aliquota => aliquota.Id)
            .HasConversion(id => id.Value, value => new AliquotaItbiId(value))
            .ValueGeneratedNever();

        builder.Property(aliquota => aliquota.Exercicio);
        builder.Property(aliquota => aliquota.AliquotaGeralPercentual).HasColumnType("decimal(9,4)");
        builder.Property(aliquota => aliquota.AliquotaSfhFinanciadaPercentual).HasColumnType("decimal(9,4)");
        builder.Property(aliquota => aliquota.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(aliquota => aliquota.Vigente);

        builder.HasIndex(aliquota => new { aliquota.TenantId, aliquota.Exercicio });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="TransmissaoImobiliaria"/>.</summary>
public sealed class TransmissaoImobiliariaConfiguration : IEntityTypeConfiguration<TransmissaoImobiliaria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TransmissaoImobiliaria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TransmissoesImobiliarias");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TransmissaoImobiliariaId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.ImovelId)
            .HasConversion(id => id.Value, value => new ImovelId(value));
        builder.Property(t => t.TransmitenteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));
        builder.Property(t => t.AdquirenteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(t => t.Exercicio);
        builder.Property(t => t.ValorDeclarado)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(t => t.ValorVenalReferencia)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(t => t.BaseCalculo)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Origem).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.ProcessoArbitramentoId);
        builder.Property(t => t.AliquotaPercentual).HasColumnType("decimal(9,4)");
        builder.Property(t => t.ImpostoDevido)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasIndex(t => new { t.TenantId, t.ImovelId });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="ProcessoArbitramentoItbi"/> (arbitramento CTN art. 148).</summary>
public sealed class ProcessoArbitramentoItbiConfiguration : IEntityTypeConfiguration<ProcessoArbitramentoItbi>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProcessoArbitramentoItbi> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProcessosArbitramentoItbi");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new ProcessoArbitramentoItbiId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.TransmissaoImobiliariaId)
            .HasConversion(id => id.Value, value => new TransmissaoImobiliariaId(value));

        builder.Property(p => p.NumeroProcesso).HasMaxLength(60).IsRequired();
        builder.Property(p => p.MotivoInstauracao).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.ValorPropostoFisco)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(p => p.FundamentacaoFisco).HasMaxLength(4000).IsRequired();
        builder.Property(p => p.JustificativaContribuinte).HasMaxLength(4000);
        builder.Property(p => p.ValorArbitradoFinal)
            .HasConversion(v => v == null ? (decimal?)null : v.Valor, v => v == null ? null : ValorMonetario.De(v.Value))
            .HasColumnType("decimal(18,2)");
        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.ResponsavelId);
        builder.Property(p => p.DataInstauracao);
        builder.Property(p => p.DataAberturaContraditorio);
        builder.Property(p => p.DataApresentacaoContraditorio);
        builder.Property(p => p.DataDesfecho);

        builder.HasIndex(p => new { p.TenantId, p.TransmissaoImobiliariaId });
        builder.HasIndex(p => new { p.TenantId, p.NumeroProcesso }).IsUnique();
    }
}

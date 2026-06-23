using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="DividaAtiva"/> e suas remessas de protesto.</summary>
public sealed class DividaAtivaConfiguration : IEntityTypeConfiguration<DividaAtiva>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DividaAtiva> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DividasAtivas");
        builder.HasKey(divida => divida.Id);
        builder.Property(divida => divida.Id)
            .HasConversion(id => id.Value, value => new DividaAtivaId(value))
            .ValueGeneratedNever();

        builder.Property(divida => divida.ContribuinteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));
        builder.Property(divida => divida.LancamentoId)
            .HasConversion(id => id.Value, value => new LancamentoId(value));

        builder.Property(divida => divida.TipoTributo).HasConversion<string>().HasMaxLength(30);

        builder.Property(divida => divida.ValorOriginario)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(divida => divida.VencimentoOrigem);
        builder.Property(divida => divida.DataConstituicaoDefinitiva);
        builder.Property(divida => divida.DataInscricao);
        builder.Property(divida => divida.DataUltimaInterrupcaoPrescricao);
        builder.Property(divida => divida.NumeroInscricao);
        builder.Property(divida => divida.AnosPrescricaoParametrizado);

        builder.Property(divida => divida.OrigemNatureza).HasMaxLength(200).IsRequired();
        builder.Property(divida => divida.FundamentoLegal).HasMaxLength(300).IsRequired();

        builder.Property(divida => divida.NumeroCda).HasMaxLength(40);
        builder.Property(divida => divida.Situacao).HasConversion<string>().HasMaxLength(30);

        // Regra de encargos (multa/juros/correção) parametrizável — Owned (mesma tabela).
        builder.OwnsOne(divida => divida.RegraEncargos, encargos =>
        {
            encargos.Property(e => e.MultaMoraPercentual).HasColumnName("EncargosMultaMoraPercentual").HasColumnType("decimal(9,4)");
            encargos.Property(e => e.JurosMoraPercentualMensal).HasColumnName("EncargosJurosMoraPercentualMensal").HasColumnType("decimal(9,4)");
            encargos.Property(e => e.CorrecaoPercentualMensal).HasColumnName("EncargosCorrecaoPercentualMensal").HasColumnType("decimal(9,4)");
            encargos.Property(e => e.FundamentoLegal).HasColumnName("EncargosFundamentoLegal").HasMaxLength(300).IsRequired();
        });
        builder.Navigation(divida => divida.RegraEncargos).IsRequired();

        // Remessas de protesto (entidades-filhas / atos auditáveis).
        builder.HasMany(divida => divida.RemessasProtesto).WithOne().HasForeignKey(r => r.DividaAtivaId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(divida => divida.RemessasProtesto).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Propriedades calculadas (sem coluna).
        builder.Ignore(divida => divida.DataPrescricao);
        builder.Ignore(divida => divida.TermoInicialPrescricao);

        builder.HasIndex(divida => new { divida.TenantId, divida.ContribuinteId });
        builder.HasIndex(divida => new { divida.TenantId, divida.NumeroInscricao }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="RemessaProtesto"/> (ato de protesto ao CRA).</summary>
public sealed class RemessaProtestoConfiguration : IEntityTypeConfiguration<RemessaProtesto>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RemessaProtesto> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RemessasProtesto");
        builder.HasKey(remessa => remessa.Id);
        builder.Property(remessa => remessa.Id)
            .HasConversion(id => id.Value, value => new RemessaProtestoId(value))
            .ValueGeneratedNever();

        builder.Property(remessa => remessa.DividaAtivaId)
            .HasConversion(id => id.Value, value => new DividaAtivaId(value));

        builder.Property(remessa => remessa.NumeroCda).HasMaxLength(40).IsRequired();
        builder.Property(remessa => remessa.IdentificadorCra).HasMaxLength(40).IsRequired();
        builder.Property(remessa => remessa.DataGeracao);
        builder.Property(remessa => remessa.DataTransmissao);
        builder.Property(remessa => remessa.DataRetorno);
        builder.Property(remessa => remessa.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(remessa => remessa.Ocorrencia).HasConversion<string>().HasMaxLength(30);
        builder.Property(remessa => remessa.ProtocoloCartorio).HasMaxLength(60);

        builder.HasIndex(remessa => remessa.DividaAtivaId);
    }
}

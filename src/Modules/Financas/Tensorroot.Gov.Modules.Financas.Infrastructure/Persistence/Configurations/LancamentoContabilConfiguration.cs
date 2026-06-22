using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="LancamentoContabil"/> e da filha <see cref="PartidaContabil"/>.</summary>
public sealed class LancamentoContabilConfiguration : IEntityTypeConfiguration<LancamentoContabil>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LancamentoContabil> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LancamentosContabeis");
        builder.HasKey(lancamento => lancamento.Id);
        builder.Property(lancamento => lancamento.Id)
            .HasConversion(id => id.Value, value => new LancamentoContabilId(value))
            .ValueGeneratedNever();

        builder.Property(lancamento => lancamento.Data).HasColumnType("date");
        builder.Property(lancamento => lancamento.Historico).HasMaxLength(500).IsRequired();
        builder.Property(lancamento => lancamento.Origem).HasConversion<string>().HasMaxLength(20);
        builder.Property(lancamento => lancamento.NaturezaInformacao).HasConversion<string>().HasMaxLength(20);

        builder.Property(lancamento => lancamento.LancamentoEstornoId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : new LancamentoContabilId(value.Value));

        builder.OwnsMany(lancamento => lancamento.Partidas, partidas =>
        {
            partidas.ToTable("PartidasContabeis");
            partidas.WithOwner().HasForeignKey("LancamentoContabilId");
            partidas.HasKey(partida => partida.Id);
            partidas.Property(partida => partida.Id)
                .HasConversion(id => id.Value, value => new PartidaContabilId(value))
                .ValueGeneratedNever();
            partidas.Property(partida => partida.ContaId)
                .HasConversion(id => id.Value, value => new ContaContabilId(value));
            partidas.Property(partida => partida.CodigoConta).HasMaxLength(30).IsRequired();
            partidas.Property(partida => partida.NaturezaInformacao).HasConversion<string>().HasMaxLength(20);
            partidas.Property(partida => partida.Lado).HasConversion<string>().HasMaxLength(10);
            partidas.Property(partida => partida.Valor)
                .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
                .HasColumnType("decimal(18,2)");
            partidas.HasIndex(partida => partida.ContaId);
        });
        builder.Navigation(lancamento => lancamento.Partidas).Metadata.SetField("_partidas");
        builder.Navigation(lancamento => lancamento.Partidas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(lancamento => new { lancamento.TenantId, lancamento.Exercicio, lancamento.PeriodoMes });
        builder.HasIndex(lancamento => new { lancamento.TenantId, lancamento.OrigemReferenciaId, lancamento.EventoContabilId });
    }
}

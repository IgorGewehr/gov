using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do read model consolidado <see cref="IndicadorMunicipioSnapshot"/> e dos mínimos
/// setoriais (owned). Índice único por <c>(TenantId, Exercicio)</c> garante o upsert da ingestão (I-13).
/// </summary>
public sealed class IndicadorMunicipioConfiguration : IEntityTypeConfiguration<IndicadorMunicipioSnapshot>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IndicadorMunicipioSnapshot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("IndicadoresMunicipio");
        builder.HasKey(indicador => indicador.Id);
        builder.Property(indicador => indicador.Id)
            .HasConversion(id => id.Value, value => new IndicadorMunicipioId(value))
            .ValueGeneratedNever();

        builder.Property(indicador => indicador.Exercicio);

        builder.Property(indicador => indicador.DotacaoAtualizada).HasPrecision(18, 2);
        builder.Property(indicador => indicador.Empenhado).HasPrecision(18, 2);
        builder.Property(indicador => indicador.Liquidado).HasPrecision(18, 2);
        builder.Property(indicador => indicador.Pago).HasPrecision(18, 2);

        builder.Property(indicador => indicador.ArrecadacaoTributaria).HasPrecision(18, 2);
        builder.Property(indicador => indicador.DividaAtivaSaldoInscrito).HasPrecision(18, 2);
        builder.Property(indicador => indicador.DividaAtivaSaldoAjuizado).HasPrecision(18, 2);
        builder.Property(indicador => indicador.DividaAtivaRecuperada).HasPrecision(18, 2);

        builder.Property(indicador => indicador.DespesaPessoal).HasPrecision(18, 2);
        builder.Property(indicador => indicador.ReceitaCorrenteLiquida).HasPrecision(18, 2);
        builder.Property(indicador => indicador.RclMesReferencia);

        builder.Property(indicador => indicador.RemessasEnviadas);
        builder.Property(indicador => indicador.RemessasComPrazoVencido);

        // Idempotência da materialização: um consolidado por exercício no tenant.
        builder.HasIndex(indicador => new { indicador.TenantId, indicador.Exercicio }).IsUnique();

        builder.OwnsMany(indicador => indicador.Minimos, MapearMinimos);
        builder.Navigation(indicador => indicador.Minimos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(indicador => indicador.DespesasPessoalMensais, MapearDespesasPessoalMensais);
        builder.Navigation(indicador => indicador.DespesasPessoalMensais).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapearDespesasPessoalMensais(
        OwnedNavigationBuilder<IndicadorMunicipioSnapshot, DespesaPessoalMensalSnapshot> despesas)
    {
        despesas.ToTable("IndicadoresMunicipioDespesaPessoalMensal");
        despesas.WithOwner().HasForeignKey("IndicadorMunicipioId");
        despesas.HasKey(item => item.Id);
        despesas.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new DespesaPessoalMensalSnapshotId(value))
            .ValueGeneratedNever();
        despesas.Property(item => item.Ano);
        despesas.Property(item => item.Mes);
        despesas.Property(item => item.Valor).HasPrecision(18, 2);
        // Uma linha por competência mensal dentro do consolidado do exercício.
        despesas.HasIndex("IndicadorMunicipioId", nameof(DespesaPessoalMensalSnapshot.Mes)).IsUnique();
    }

    private static void MapearMinimos(OwnedNavigationBuilder<IndicadorMunicipioSnapshot, MinimoSetorialSnapshot> minimos)
    {
        minimos.ToTable("IndicadoresMunicipioMinimos");
        minimos.WithOwner().HasForeignKey("IndicadorMunicipioId");
        minimos.HasKey(item => item.Id);
        minimos.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new MinimoSetorialSnapshotId(value))
            .ValueGeneratedNever();
        minimos.Property(item => item.Setor).HasMaxLength(30);
        minimos.Property(item => item.ReceitaBase).HasPrecision(18, 2);
        minimos.Property(item => item.Aplicado).HasPrecision(18, 2);
        minimos.Property(item => item.PercentualAplicado).HasPrecision(9, 6);
        minimos.Property(item => item.PercentualMinimo).HasPrecision(9, 6);
        minimos.Property(item => item.Situacao).HasMaxLength(20);
    }
}

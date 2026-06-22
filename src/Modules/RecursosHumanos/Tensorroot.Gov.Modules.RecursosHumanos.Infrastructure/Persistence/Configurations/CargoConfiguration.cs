using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Cargo"/>.</summary>
public sealed class CargoConfiguration : IEntityTypeConfiguration<Cargo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Cargos");
        builder.HasKey(cargo => cargo.Id);
        builder.Property(cargo => cargo.Id)
            .HasConversion(id => id.Value, value => new CargoId(value))
            .ValueGeneratedNever();

        builder.Property(cargo => cargo.Denominacao).HasMaxLength(200);
        builder.Property(cargo => cargo.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(cargo => cargo.Regime).HasConversion<string>().HasMaxLength(10);
        builder.Property(cargo => cargo.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(cargo => cargo.LeiCriacao).HasMaxLength(200);

        builder.Property(cargo => cargo.Vencimento)
            .HasConversion(vencimento => vencimento.Valor, valor => Vencimento.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(cargo => cargo.PlanoDeCargosId)
            .HasConversion(
                plano => plano!.Value.Value,
                valor => new PlanoDeCargosId(valor));

        builder.ComplexProperty(cargo => cargo.Lotacao, lotacao =>
        {
            lotacao.Property(l => l.InscricaoEstabelecimento).HasColumnName("LotacaoInscricaoEstabelecimento").HasMaxLength(30);
            lotacao.Property(l => l.DenominacaoUnidade).HasColumnName("LotacaoDenominacaoUnidade").HasMaxLength(200);
            lotacao.Property(l => l.CodigoLotacaoTributaria).HasColumnName("LotacaoCodigoTributaria").HasMaxLength(30);
        });

        // VagasDisponiveis e propriedade calculada (sem coluna).
        builder.Ignore(cargo => cargo.VagasDisponiveis);

        builder.HasIndex(cargo => new { cargo.TenantId, cargo.Situacao, cargo.Tipo });
    }
}

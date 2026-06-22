using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="DividaAtiva"/>.</summary>
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

        builder.Property(divida => divida.ValorInscrito)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(divida => divida.NumeroCda).HasMaxLength(40);
        builder.Property(divida => divida.Situacao).HasConversion<string>().HasMaxLength(30);

        // DataPrescricao é propriedade calculada (sem coluna).
        builder.Ignore(divida => divida.DataPrescricao);

        builder.HasIndex(divida => new { divida.TenantId, divida.ContribuinteId });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do catalogo <see cref="Imunobiologico"/>.</summary>
public sealed class ImunobiologicoConfiguration : IEntityTypeConfiguration<Imunobiologico>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Imunobiologico> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Imunobiologicos");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(id => id.Value, value => new ImunobiologicoId(value))
            .ValueGeneratedNever();

        builder.Property(i => i.Nome).HasMaxLength(120).IsRequired();
        builder.Property(i => i.Sigla).HasMaxLength(20).IsRequired();
        builder.Property(i => i.TotalDoses);
        builder.Property(i => i.IntervaloDiasProximaDose);
        builder.Property(i => i.DoseUnica);
        builder.Property(i => i.MedicamentoEstoqueId)
            .HasConversion(id => id!.Value.Value, value => new MedicamentoId(value));
        builder.Property(i => i.Ativo);

        builder.HasIndex(i => new { i.TenantId, i.Sigla });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="CarteiraVacinacao"/> e das doses (filhas).</summary>
public sealed class CarteiraVacinacaoConfiguration : IEntityTypeConfiguration<CarteiraVacinacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CarteiraVacinacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CarteirasVacinacao");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CarteiraVacinacaoId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.PacienteId)
            .HasConversion(id => id.Value, value => new PacienteId(value));

        builder.OwnsMany(c => c.Doses, MapearDoses);
        builder.Navigation(c => c.Doses).UsePropertyAccessMode(PropertyAccessMode.Field);

        // 1-1 por paciente (uma carteira por paciente no tenant).
        builder.HasIndex(c => new { c.TenantId, c.PacienteId }).IsUnique();
    }

    private static void MapearDoses(OwnedNavigationBuilder<CarteiraVacinacao, DoseAplicada> doses)
    {
        doses.ToTable("CarteirasVacinacaoDoses");
        doses.WithOwner().HasForeignKey("CarteiraVacinacaoId");
        doses.HasKey(d => d.Id);
        doses.Property(d => d.Id)
            .HasConversion(id => id.Value, value => new DoseAplicadaId(value))
            .ValueGeneratedNever();
        doses.Property(d => d.ImunobiologicoId)
            .HasConversion(id => id.Value, value => new ImunobiologicoId(value));
        doses.Property(d => d.TipoDose).HasConversion<string>().HasMaxLength(20);
        doses.Property(d => d.NumeroDose);
        doses.Property(d => d.Lote).HasMaxLength(40).IsRequired();
        doses.Property(d => d.AplicadorId)
            .HasConversion(id => id.Value, value => new ProfissionalId(value));
        doses.Property(d => d.DataAplicacao);
        doses.Property(d => d.ProximaDoseAprazada);

        doses.HasIndex(d => d.ProximaDoseAprazada);
    }
}

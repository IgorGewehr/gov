using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="AgendaProfissional"/> e das vagas (slots) owned.</summary>
public sealed class AgendaProfissionalConfiguration : IEntityTypeConfiguration<AgendaProfissional>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgendaProfissional> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AgendasProfissional");
        builder.HasKey(agenda => agenda.Id);
        builder.Property(agenda => agenda.Id)
            .HasConversion(id => id.Value, value => new AgendaProfissionalId(value))
            .ValueGeneratedNever();

        builder.Property(agenda => agenda.ProfissionalId)
            .HasConversion(id => id.Value, value => new ProfissionalId(value));
        builder.Property(agenda => agenda.EstabelecimentoId)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value));

        builder.Property(agenda => agenda.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(agenda => agenda.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(agenda => agenda.Data);
        builder.Property(agenda => agenda.HoraInicio);
        builder.Property(agenda => agenda.HoraFim);
        builder.Property(agenda => agenda.DuracaoSlotMinutos);
        builder.Property(agenda => agenda.CapacidadeVagas);

        builder.OwnsMany(agenda => agenda.Vagas, MapearVagas);
        builder.Navigation(agenda => agenda.Vagas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(agenda => new { agenda.TenantId, agenda.ProfissionalId, agenda.Data });
        builder.HasIndex(agenda => new { agenda.TenantId, agenda.EstabelecimentoId, agenda.Data });
    }

    private static void MapearVagas(OwnedNavigationBuilder<AgendaProfissional, Vaga> vagas)
    {
        vagas.ToTable("AgendaVagas");
        vagas.WithOwner().HasForeignKey("AgendaProfissionalId");
        vagas.HasKey(vaga => vaga.Id);
        vagas.Property(vaga => vaga.Id)
            .HasConversion(id => id.Value, value => new VagaId(value))
            .ValueGeneratedNever();
        vagas.Property(vaga => vaga.DataHora);
        vagas.Property(vaga => vaga.Situacao).HasConversion<string>().HasMaxLength(20);

        // Busca de vaga livre por profissional/data e por situacao (anti-overbooking + disponibilidade).
        vagas.HasIndex("AgendaProfissionalId", "Situacao");
        vagas.HasIndex(vaga => vaga.DataHora);
    }
}

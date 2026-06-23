using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AgendamentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Agendamento.Agendamento;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="AgendamentoRaiz"/> (marcacao).</summary>
public sealed class AgendamentoConfiguration : IEntityTypeConfiguration<AgendamentoRaiz>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgendamentoRaiz> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Agendamentos");
        builder.HasKey(agendamento => agendamento.Id);
        builder.Property(agendamento => agendamento.Id)
            .HasConversion(id => id.Value, value => new AgendamentoId(value))
            .ValueGeneratedNever();

        builder.Property(agendamento => agendamento.PacienteId)
            .HasConversion(id => id.Value, value => new PacienteId(value));
        builder.Property(agendamento => agendamento.ProfissionalId)
            .HasConversion(id => id.Value, value => new ProfissionalId(value));
        builder.Property(agendamento => agendamento.EstabelecimentoId)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value));
        builder.Property(agendamento => agendamento.AgendaId)
            .HasConversion(id => id.Value, value => new AgendaProfissionalId(value));
        builder.Property(agendamento => agendamento.VagaId)
            .HasConversion(id => id.Value, value => new VagaId(value));

        builder.Property(agendamento => agendamento.DataHora);
        builder.Property(agendamento => agendamento.DataMarcacao);
        builder.Property(agendamento => agendamento.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(agendamento => agendamento.Prioridade).HasConversion<string>().HasMaxLength(20);
        builder.Property(agendamento => agendamento.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(agendamento => agendamento.OrigemCancelamento).HasConversion<string>().HasMaxLength(20);
        builder.Property(agendamento => agendamento.MotivoCancelamento).HasMaxLength(500);
        builder.Property(agendamento => agendamento.AtendimentoId);

        builder.HasIndex(agendamento => new { agendamento.TenantId, agendamento.PacienteId, agendamento.DataHora });
        builder.HasIndex(agendamento => new { agendamento.TenantId, agendamento.ProfissionalId, agendamento.DataHora });
        builder.HasIndex(agendamento => new { agendamento.TenantId, agendamento.Situacao });
    }
}

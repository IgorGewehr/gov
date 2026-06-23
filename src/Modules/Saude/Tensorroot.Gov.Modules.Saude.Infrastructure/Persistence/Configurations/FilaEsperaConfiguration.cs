using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="FilaEspera"/>.</summary>
public sealed class FilaEsperaConfiguration : IEntityTypeConfiguration<FilaEspera>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FilaEspera> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FilasEspera");
        builder.HasKey(fila => fila.Id);
        builder.Property(fila => fila.Id)
            .HasConversion(id => id.Value, value => new FilaEsperaId(value))
            .ValueGeneratedNever();

        builder.Property(fila => fila.PacienteId)
            .HasConversion(id => id.Value, value => new PacienteId(value));
        builder.Property(fila => fila.EstabelecimentoId)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value));

        // ProfissionalId opcional (struct nullable): converte preservando o nulo.
        builder.Property(fila => fila.ProfissionalId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (ProfissionalId?)null : new ProfissionalId(value.Value));

        builder.Property(fila => fila.Especialidade).HasMaxLength(FilaEspera.ComprimentoEspecialidade);
        builder.Property(fila => fila.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(fila => fila.Prioridade).HasConversion<string>().HasMaxLength(20);
        builder.Property(fila => fila.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(fila => fila.DataEntrada);
        builder.Property(fila => fila.DataConvocacao);

        // Convocacao por prioridade + ordem de chegada (FIFO dentro da prioridade), filtrando por alvo.
        builder.HasIndex(fila => new { fila.TenantId, fila.EstabelecimentoId, fila.Situacao });
        builder.HasIndex(fila => new { fila.TenantId, fila.ProfissionalId, fila.Situacao });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado de controle <see cref="EncerramentoExercicio"/>.</summary>
public sealed class EncerramentoExercicioConfiguration : IEntityTypeConfiguration<EncerramentoExercicio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EncerramentoExercicio> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("encerramento_exercicio");
        builder.HasKey(encerramento => encerramento.Id);
        builder.Property(encerramento => encerramento.Id)
            .HasConversion(id => id.Value, value => new EncerramentoExercicioId(value))
            .ValueGeneratedNever();

        builder.Property(encerramento => encerramento.Exercicio).IsRequired();
        builder.Property(encerramento => encerramento.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(encerramento => encerramento.IniciadoEmUtc);
        builder.Property(encerramento => encerramento.EncerradoEmUtc);
        builder.Property(encerramento => encerramento.AberturaConcluidaEmUtc);
        builder.Ignore(encerramento => encerramento.Congelado);

        // Um único registro de encerramento por (Tenant, Exercicio).
        builder.HasIndex(encerramento => new { encerramento.TenantId, encerramento.Exercicio }).IsUnique();
    }
}

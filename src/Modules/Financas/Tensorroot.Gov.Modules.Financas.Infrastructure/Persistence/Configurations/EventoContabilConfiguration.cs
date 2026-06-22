using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="EventoContabil"/> e do VO <see cref="LinhaRoteiro"/>.</summary>
public sealed class EventoContabilConfiguration : IEntityTypeConfiguration<EventoContabil>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventoContabil> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EventosContabeis");
        builder.HasKey(evento => evento.Id);
        builder.Property(evento => evento.Id)
            .HasConversion(id => id.Value, value => new EventoContabilId(value))
            .ValueGeneratedNever();

        builder.Property(evento => evento.Fato).HasConversion<string>().HasMaxLength(40);
        builder.Property(evento => evento.Codigo).HasMaxLength(40).IsRequired();
        builder.Property(evento => evento.Nome).HasMaxLength(200).IsRequired();

        builder.OwnsMany(evento => evento.Linhas, linhas =>
        {
            linhas.ToTable("LinhasRoteiroContabil");
            linhas.WithOwner().HasForeignKey("EventoContabilId");
            // Chave técnica sombra (VO sem identidade própria).
            linhas.Property<int>("Id").ValueGeneratedOnAdd();
            linhas.HasKey("Id");
            linhas.Property(linha => linha.Lado).HasConversion<string>().HasMaxLength(10);
            linhas.Property(linha => linha.NaturezaInformacao).HasConversion<string>().HasMaxLength(20);
            linhas.Property(linha => linha.CodigoContaFixo).HasMaxLength(30);
            linhas.Property(linha => linha.Papel).HasConversion<string>().HasMaxLength(40);
            linhas.Property(linha => linha.BaseValor).HasConversion<string>().HasMaxLength(20);
        });
        builder.Navigation(evento => evento.Linhas).Metadata.SetField("_linhas");
        builder.Navigation(evento => evento.Linhas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(evento => new { evento.TenantId, evento.Fato, evento.ExercicioVigenciaInicio });
    }
}

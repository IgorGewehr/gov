using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Configurations;

/// <summary>3d.2: mapeamento EF Core da unidade socioassistencial e seus servicos ofertados.</summary>
public sealed class UnidadeSocioassistencialConfiguration : IEntityTypeConfiguration<UnidadeSocioassistencial>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UnidadeSocioassistencial> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UnidadesSocioassistenciais");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => new UnidadeSocioassistencialId(value))
            .ValueGeneratedNever();

        builder.Property(u => u.Nome).HasMaxLength(200);
        builder.Property(u => u.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.TerritorioCobertura).HasMaxLength(120);
        builder.Property(u => u.Endereco).HasMaxLength(300);

        builder.HasIndex(u => u.TenantId);

        builder.OwnsMany(u => u.Servicos, servico =>
        {
            servico.ToTable("ServicosOfertados");
            servico.WithOwner().HasForeignKey(s => s.UnidadeId);
            servico.HasKey(s => s.Id);
            servico.Property(s => s.Id)
                .HasConversion(id => id.Value, value => new ServicoOfertadoId(value))
                .ValueGeneratedNever();
            servico.Property(s => s.UnidadeId)
                .HasConversion(id => id.Value, value => new UnidadeSocioassistencialId(value));
            servico.Property(s => s.Servico).HasConversion<string>().HasMaxLength(20);
            servico.Property(s => s.CapacidadeMensal);
            // Um unico registro por (unidade, servico): a oferta e atualizada, nao duplicada.
            servico.HasIndex(s => new { s.UnidadeId, s.Servico }).IsUnique();
        });

        builder.Navigation(u => u.Servicos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>3d.2: mapeamento EF Core do formulario consolidado do Censo SUAS.</summary>
public sealed class FormularioCensoSuasConfiguration : IEntityTypeConfiguration<FormularioCensoSuas>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FormularioCensoSuas> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FormulariosCensoSuas");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasConversion(id => id.Value, value => new FormularioCensoSuasId(value))
            .ValueGeneratedNever();

        builder.Property(f => f.UnidadeId)
            .HasConversion(id => id.Value, value => new UnidadeSocioassistencialId(value));

        builder.Property(f => f.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.FechadoEmUtc);

        // Um unico consolidado por (tenant, unidade, exercicio): chave de negocio anual do Censo.
        builder.HasIndex(f => new { f.TenantId, f.UnidadeId, f.Exercicio })
            .HasDatabaseName("IX_Censo_Tenant_Unidade_Exercicio")
            .IsUnique();
    }
}

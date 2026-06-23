using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Configurations;

/// <summary>3d.1: mapeamento EF Core do acompanhamento de condicionalidades do PBF e seus registros.</summary>
public sealed class AcompanhamentoCondicionalidadeConfiguration : IEntityTypeConfiguration<AcompanhamentoCondicionalidade>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AcompanhamentoCondicionalidade> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AcompanhamentosCondicionalidade");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AcompanhamentoCondicionalidadeId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.FamiliaId)
            .HasConversion(id => id.Value, value => new FamiliaId(value));

        builder.Property(a => a.Efeito).HasConversion<string>().HasMaxLength(20);

        // Competencia mapeada como inteiro (Ano*100+Mes), espelhando o RMA/Beneficio.
        builder.Property(a => a.Competencia)
            .HasConversion(competencia => (competencia.Ano * 100) + competencia.Mes, valor => Competencia.De(valor / 100, valor % 100));

        builder.Ignore(a => a.DescumprimentosEfetivos);

        // Um unico acompanhamento por (tenant, familia, competencia): chave de negocio do periodo.
        builder.HasIndex(a => new { a.TenantId, a.FamiliaId, a.Competencia })
            .HasDatabaseName("IX_AcompCondic_Tenant_Familia_Competencia")
            .IsUnique();

        builder.OwnsMany(a => a.Registros, registro =>
        {
            registro.ToTable("RegistrosCondicionalidade");
            registro.WithOwner().HasForeignKey(r => r.AcompanhamentoId);
            registro.HasKey(r => r.Id);
            registro.Property(r => r.Id)
                .HasConversion(id => id.Value, value => new RegistroCondicionalidadeId(value))
                .ValueGeneratedNever();
            registro.Property(r => r.AcompanhamentoId)
                .HasConversion(id => id.Value, value => new AcompanhamentoCondicionalidadeId(value));
            registro.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(40);
            registro.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            registro.Property(r => r.Observacao).HasMaxLength(1000);
            registro.HasIndex(r => r.AcompanhamentoId);
        });

        builder.Navigation(a => a.Registros).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

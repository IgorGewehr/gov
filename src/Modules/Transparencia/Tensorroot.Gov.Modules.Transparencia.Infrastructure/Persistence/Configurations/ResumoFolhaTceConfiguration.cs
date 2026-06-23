using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasFolha;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do read model <see cref="ResumoFolhaTce"/> (snapshot da folha consumido do RH) e
/// suas entidades-filhas (servidores/rubricas/lancamentos), de onde a remessa de folha ao TCE-RS e montada.
/// </summary>
public sealed class ResumoFolhaTceConfiguration : IEntityTypeConfiguration<ResumoFolhaTce>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ResumoFolhaTce> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ResumosFolhaTce");
        builder.HasKey(resumo => resumo.Id);
        builder.Property(resumo => resumo.Id)
            .HasConversion(id => id.Value, value => new ResumoFolhaTceId(value))
            .ValueGeneratedNever();

        builder.Property(resumo => resumo.FolhaDePagamentoId);
        builder.Property(resumo => resumo.Exercicio);
        builder.Property(resumo => resumo.Mes);
        builder.Property(resumo => resumo.TipoFolha).HasMaxLength(30);
        builder.Property(resumo => resumo.DataPagamento);

        // Idempotencia da ponte (I-13): um resumo por folha no tenant.
        builder.HasIndex(resumo => new { resumo.TenantId, resumo.FolhaDePagamentoId }).IsUnique();
        builder.HasIndex(resumo => new { resumo.TenantId, resumo.Exercicio, resumo.Mes });

        builder.OwnsMany(resumo => resumo.Servidores, MapearServidores);
        builder.OwnsMany(resumo => resumo.Rubricas, MapearRubricas);
        builder.OwnsMany(resumo => resumo.Lancamentos, MapearLancamentos);

        builder.Navigation(resumo => resumo.Servidores).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(resumo => resumo.Rubricas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(resumo => resumo.Lancamentos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapearServidores(OwnedNavigationBuilder<ResumoFolhaTce, ServidorFolhaResumo> servidores)
    {
        servidores.ToTable("ResumosFolhaTceServidores");
        servidores.WithOwner().HasForeignKey("ResumoFolhaTceId");
        servidores.HasKey(item => item.Id);
        servidores.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ServidorFolhaResumoId(value))
            .ValueGeneratedNever();
        servidores.Property(item => item.CodigoRegistro).HasMaxLength(20);
        servidores.Property(item => item.Cpf).HasMaxLength(14);
        servidores.Property(item => item.Nome).HasMaxLength(70);
        servidores.Property(item => item.Matricula).HasMaxLength(20);
        servidores.Property(item => item.DataNascimento);
        servidores.Property(item => item.DataAdmissao);
        servidores.Property(item => item.DataDemissao);
        servidores.Property(item => item.CodigoCargo).HasMaxLength(40);
        servidores.Property(item => item.NomeCargo).HasMaxLength(60);
        servidores.Property(item => item.Regime).HasMaxLength(10);
    }

    private static void MapearRubricas(OwnedNavigationBuilder<ResumoFolhaTce, RubricaFolhaResumo> rubricas)
    {
        rubricas.ToTable("ResumosFolhaTceRubricas");
        rubricas.WithOwner().HasForeignKey("ResumoFolhaTceId");
        rubricas.HasKey(item => item.Id);
        rubricas.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new RubricaFolhaResumoId(value))
            .ValueGeneratedNever();
        rubricas.Property(item => item.Codigo).HasMaxLength(30);
        rubricas.Property(item => item.Descricao).HasMaxLength(60);
        rubricas.Property(item => item.Operacao).HasMaxLength(1);
        rubricas.Property(item => item.IncideIrrf);
        rubricas.Property(item => item.IncideRpps);
        rubricas.Property(item => item.IncideInss);
        rubricas.Property(item => item.BaseLegal).HasMaxLength(150);
        rubricas.Property(item => item.ContaPlanoFolhaTce).HasMaxLength(6);
    }

    private static void MapearLancamentos(OwnedNavigationBuilder<ResumoFolhaTce, LancamentoFolhaResumo> lancamentos)
    {
        lancamentos.ToTable("ResumosFolhaTceLancamentos");
        lancamentos.WithOwner().HasForeignKey("ResumoFolhaTceId");
        lancamentos.HasKey(item => item.Id);
        lancamentos.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new LancamentoFolhaResumoId(value))
            .ValueGeneratedNever();
        lancamentos.Property(item => item.CodigoRegistroServidor).HasMaxLength(20);
        lancamentos.Property(item => item.CodigoRubrica).HasMaxLength(30);
        lancamentos.Property(item => item.Operacao).HasMaxLength(1);
        lancamentos.Property(item => item.Valor).HasPrecision(18, 2);
    }
}

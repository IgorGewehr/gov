using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Processo"/> e de suas entidades internas.</summary>
public sealed class ProcessoConfiguration : IEntityTypeConfiguration<Processo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Processo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Processos");
        builder.HasKey(processo => processo.Id);
        builder.Property(processo => processo.Id)
            .HasConversion(id => id.Value, value => new ProcessoId(value))
            .ValueGeneratedNever();

        builder.Property(processo => processo.Nup)
            .HasConversion(nup => nup.Valor, valor => new Nup(valor))
            .HasMaxLength(Nup.ComprimentoMaximo);

        builder.Property(processo => processo.Classificacao)
            .HasConversion(classificacao => classificacao.Codigo, valor => new Classificacao(valor))
            .HasMaxLength(Classificacao.ComprimentoMaximo);

        builder.Property(processo => processo.NivelAcesso).HasConversion<string>().HasMaxLength(20);
        builder.Property(processo => processo.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(processo => processo.OrigemModulo).HasMaxLength(60);

        // Interessado/parte (CPF/CNPJ, somente digitos): ancora do "meus processos" do Portal do Cidadao.
        builder.Property(processo => processo.InteressadoDocumento).HasMaxLength(14);

        builder.ComplexProperty(processo => processo.Prazo, MapearPrazo);

        builder.OwnsMany(processo => processo.Despachos, MapearDespachos);
        builder.OwnsMany(processo => processo.Movimentacoes, MapearMovimentacoes);

        builder.Navigation(processo => processo.Despachos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(processo => processo.Movimentacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unicidade do NUP por tenant (Decreto 8.539/2015).
        builder.HasIndex(processo => new { processo.TenantId, processo.Nup }).IsUnique();
        builder.HasIndex(processo => new { processo.TenantId, processo.SetorAtualId });

        // Indice do "meus processos" (Portal do Cidadao): busca por (tenant, documento do interessado).
        builder.HasIndex(processo => new { processo.TenantId, processo.InteressadoDocumento });
    }

    private static void MapearPrazo(ComplexPropertyBuilder<Prazo> prazo)
    {
        prazo.Property(item => item.Inicio).HasColumnName("PrazoInicio");
        prazo.Property(item => item.Fim).HasColumnName("PrazoFim");
    }

    private static void MapearDespachos(OwnedNavigationBuilder<Processo, Despacho> despachos)
    {
        despachos.ToTable("ProcessosDespachos");
        despachos.WithOwner().HasForeignKey("ProcessoId");
        despachos.HasKey(despacho => despacho.Id);
        despachos.Property(despacho => despacho.Id)
            .HasConversion(id => id.Value, value => new DespachoId(value))
            .ValueGeneratedNever();
        despachos.Property(despacho => despacho.Texto).HasMaxLength(4000);
    }

    private static void MapearMovimentacoes(OwnedNavigationBuilder<Processo, Movimentacao> movimentacoes)
    {
        movimentacoes.ToTable("ProcessosMovimentacoes");
        movimentacoes.WithOwner().HasForeignKey("ProcessoId");
        movimentacoes.HasKey(movimentacao => movimentacao.Id);
        movimentacoes.Property(movimentacao => movimentacao.Id)
            .HasConversion(id => id.Value, value => new MovimentacaoId(value))
            .ValueGeneratedNever();
        movimentacoes.Property(movimentacao => movimentacao.Observacao).HasMaxLength(2000);
    }
}

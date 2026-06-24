using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="ApuracaoArt29A"/> (demonstrativo do art. 29-A) e das suas
/// parcelas de despesa. A base de receita e os parametros sao value objects persistidos como colunas
/// owned (sem tabela propria), preservando o snapshot por exercicio.
/// </summary>
public sealed class ApuracaoArt29AConfiguration : IEntityTypeConfiguration<ApuracaoArt29A>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApuracaoArt29A> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ApuracoesArt29A");
        builder.HasKey(apuracao => apuracao.Id);
        builder.Property(apuracao => apuracao.Id)
            .HasConversion(id => id.Value, value => new ApuracaoArt29AId(value))
            .ValueGeneratedNever();

        builder.Property(apuracao => apuracao.Exercicio);
        builder.Property(apuracao => apuracao.Populacao);
        builder.Property(apuracao => apuracao.RepasseRecebido).HasPrecision(18, 2);
        builder.Property(apuracao => apuracao.Situacao).HasConversion<string>().HasMaxLength(20);

        // Base de receita (VO) como colunas owned.
        builder.OwnsOne(apuracao => apuracao.BaseReceita, baseReceita =>
        {
            baseReceita.Property(b => b.ExercicioReferencia).HasColumnName("BaseExercicioReferencia");
            baseReceita.Property(b => b.ReceitaTributaria).HasColumnName("BaseReceitaTributaria").HasPrecision(18, 2);
            baseReceita.Property(b => b.Transferencias).HasColumnName("BaseTransferencias").HasPrecision(18, 2);
        });
        builder.Navigation(apuracao => apuracao.BaseReceita).IsRequired();

        // Parametros RESOLVIDOS (escalares) como colunas owned: snapshot estavel por apuracao.
        builder.OwnsOne(apuracao => apuracao.Parametros, parametros =>
        {
            parametros.Property(p => p.PercentualFaixa).HasColumnName("ParamPercentualFaixa").HasPrecision(9, 6);
            parametros.Property(p => p.SubtetoFolhaSobreRepasse).HasColumnName("ParamSubtetoFolha").HasPrecision(9, 6);
            parametros.Property(p => p.LimiarAtencao).HasColumnName("ParamLimiarAtencao").HasPrecision(9, 6);
            parametros.Property(p => p.ExercicioCorteInativos).HasColumnName("ParamExercicioCorteInativos");
        });
        builder.Navigation(apuracao => apuracao.Parametros).IsRequired();

        // Despesas discriminadas (owned collection em tabela propria).
        builder.OwnsMany(apuracao => apuracao.Despesas, MapearDespesas);
        builder.Navigation(apuracao => apuracao.Despesas).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Uma apuracao por exercicio/tenant (chave logica do demonstrativo anual).
        builder.HasIndex(apuracao => new { apuracao.TenantId, apuracao.Exercicio }).IsUnique();
    }

    private static void MapearDespesas(OwnedNavigationBuilder<ApuracaoArt29A, ItemDespesaCamara> despesas)
    {
        despesas.ToTable("ApuracoesArt29ADespesas");
        despesas.WithOwner().HasForeignKey("ApuracaoOwnerId");
        despesas.HasKey(item => item.Id);
        despesas.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemDespesaCamaraId(value))
            .ValueGeneratedNever();
        despesas.Property(item => item.Natureza).HasConversion<string>().HasMaxLength(30);
        despesas.Property(item => item.Valor).HasPrecision(18, 2);
        despesas.Property(item => item.Descricao).HasMaxLength(200);
        despesas.Ignore(item => item.EhFolha);
    }
}

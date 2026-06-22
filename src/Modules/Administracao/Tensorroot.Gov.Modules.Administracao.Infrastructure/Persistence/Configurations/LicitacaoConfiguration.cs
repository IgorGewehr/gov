using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Licitacao"/> e de suas entidades filhas.</summary>
public sealed class LicitacaoConfiguration : IEntityTypeConfiguration<Licitacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Licitacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Licitacoes");
        builder.HasKey(licitacao => licitacao.Id);
        builder.Property(licitacao => licitacao.Id)
            .HasConversion(id => id.Value, value => new LicitacaoId(value))
            .ValueGeneratedNever();

        builder.Property(licitacao => licitacao.Objeto).HasMaxLength(2000);
        builder.Property(licitacao => licitacao.Modalidade).HasConversion<string>().HasMaxLength(30);
        builder.Property(licitacao => licitacao.CriterioJulgamento).HasConversion<string>().HasMaxLength(30);
        builder.Property(licitacao => licitacao.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(licitacao => licitacao.NumeroEditalPncp).HasMaxLength(60);

        builder.Property(licitacao => licitacao.ValorEstimado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.OwnsMany(licitacao => licitacao.Lotes, MapearLotes);
        builder.OwnsMany(licitacao => licitacao.Propostas, MapearPropostas);
        builder.OwnsMany(licitacao => licitacao.Habilitacoes, MapearHabilitacoes);
        builder.OwnsMany(licitacao => licitacao.Recursos, MapearRecursos);

        builder.Navigation(licitacao => licitacao.Lotes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(licitacao => licitacao.Propostas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(licitacao => licitacao.Habilitacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(licitacao => licitacao.Recursos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(licitacao => new { licitacao.TenantId, licitacao.Situacao });
    }

    private static void MapearLotes(OwnedNavigationBuilder<Licitacao, Lote> lotes)
    {
        lotes.ToTable("LicitacoesLotes");
        lotes.WithOwner().HasForeignKey("LicitacaoId");
        lotes.HasKey(lote => lote.Id);
        lotes.Property(lote => lote.Id)
            .HasConversion(id => id.Value, value => new LoteId(value))
            .ValueGeneratedNever();
        lotes.Property(lote => lote.Descricao).HasMaxLength(2000);
        lotes.Property(lote => lote.ValorEstimado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearPropostas(OwnedNavigationBuilder<Licitacao, Proposta> propostas)
    {
        propostas.ToTable("LicitacoesPropostas");
        propostas.WithOwner().HasForeignKey("LicitacaoId");
        propostas.HasKey(proposta => proposta.Id);
        propostas.Property(proposta => proposta.Id)
            .HasConversion(id => id.Value, value => new PropostaId(value))
            .ValueGeneratedNever();
        propostas.Property(proposta => proposta.LoteId)
            .HasConversion(id => id.Value, value => new LoteId(value));
        propostas.Property(proposta => proposta.Situacao).HasConversion<string>().HasMaxLength(20);
        propostas.Property(proposta => proposta.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearHabilitacoes(OwnedNavigationBuilder<Licitacao, Habilitacao> habilitacoes)
    {
        habilitacoes.ToTable("LicitacoesHabilitacoes");
        habilitacoes.WithOwner().HasForeignKey("LicitacaoId");
        habilitacoes.HasKey(habilitacao => habilitacao.Id);
        habilitacoes.Property(habilitacao => habilitacao.Id)
            .HasConversion(id => id.Value, value => new HabilitacaoId(value))
            .ValueGeneratedNever();
        habilitacoes.Property(habilitacao => habilitacao.Resultado).HasConversion<string>().HasMaxLength(20);
        habilitacoes.Property(habilitacao => habilitacao.Motivo).HasMaxLength(2000);
    }

    private static void MapearRecursos(OwnedNavigationBuilder<Licitacao, Recurso> recursos)
    {
        recursos.ToTable("LicitacoesRecursos");
        recursos.WithOwner().HasForeignKey("LicitacaoId");
        recursos.HasKey(recurso => recurso.Id);
        recursos.Property(recurso => recurso.Id)
            .HasConversion(id => id.Value, value => new RecursoId(value))
            .ValueGeneratedNever();
        recursos.Property(recurso => recurso.Fundamentacao).HasMaxLength(2000);
    }
}

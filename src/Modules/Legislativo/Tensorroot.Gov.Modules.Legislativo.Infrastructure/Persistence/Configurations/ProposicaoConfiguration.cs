using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Proposicao"/> e de suas entidades filhas.</summary>
public sealed class ProposicaoConfiguration : IEntityTypeConfiguration<Proposicao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Proposicao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Proposicoes");
        builder.HasKey(proposicao => proposicao.Id);
        builder.Property(proposicao => proposicao.Id)
            .HasConversion(id => id.Value, value => new ProposicaoId(value))
            .ValueGeneratedNever();

        builder.Property(proposicao => proposicao.Tipo).HasConversion<string>().HasMaxLength(40);
        builder.Property(proposicao => proposicao.Regime).HasConversion<string>().HasMaxLength(20);
        builder.Property(proposicao => proposicao.Situacao).HasConversion<string>().HasMaxLength(30);

        builder.Property(proposicao => proposicao.Ementa)
            .HasConversion(ementa => ementa.Valor, valor => Ementa.De(valor))
            .HasMaxLength(Ementa.ComprimentoMaximo);

        builder.Property(proposicao => proposicao.Autoria)
            .HasConversion(autoria => autoria.Valor, valor => Autoria.De(valor))
            .HasMaxLength(Autoria.ComprimentoMaximo);

        builder.Property(proposicao => proposicao.Protocolo).HasMaxLength(40);
        builder.Property(proposicao => proposicao.NumeroAutografo).HasMaxLength(40);

        // Propriedades calculadas (sem coluna).
        builder.Ignore(proposicao => proposicao.MaioriaExigida);
        builder.Ignore(proposicao => proposicao.EmTramitacao);

        builder.OwnsMany(proposicao => proposicao.Emendas, MapearEmendas);
        builder.OwnsMany(proposicao => proposicao.Substitutivos, MapearSubstitutivos);
        builder.OwnsMany(proposicao => proposicao.Tramitacoes, MapearTramitacoes);

        builder.Navigation(proposicao => proposicao.Emendas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(proposicao => proposicao.Substitutivos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(proposicao => proposicao.Tramitacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(proposicao => new { proposicao.TenantId, proposicao.Situacao });
        builder.HasIndex(proposicao => new { proposicao.TenantId, proposicao.Protocolo }).IsUnique();
    }

    private static void MapearEmendas(OwnedNavigationBuilder<Proposicao, Emenda> emendas)
    {
        emendas.ToTable("ProposicoesEmendas");
        emendas.WithOwner().HasForeignKey("ProposicaoOwnerId");
        emendas.HasKey(emenda => emenda.Id);
        emendas.Property(emenda => emenda.Id)
            .HasConversion(id => id.Value, value => new EmendaId(value))
            .ValueGeneratedNever();
        emendas.Property(emenda => emenda.ProposicaoId)
            .HasConversion(id => id.Value, value => new ProposicaoId(value));
        emendas.Property(emenda => emenda.Texto).HasMaxLength(4000);
        emendas.Property(emenda => emenda.Autoria)
            .HasConversion(autoria => autoria.Valor, valor => Autoria.De(valor))
            .HasMaxLength(Autoria.ComprimentoMaximo);
    }

    private static void MapearSubstitutivos(OwnedNavigationBuilder<Proposicao, Substitutivo> substitutivos)
    {
        substitutivos.ToTable("ProposicoesSubstitutivos");
        substitutivos.WithOwner().HasForeignKey("ProposicaoOwnerId");
        substitutivos.HasKey(substitutivo => substitutivo.Id);
        substitutivos.Property(substitutivo => substitutivo.Id)
            .HasConversion(id => id.Value, value => new SubstitutivoId(value))
            .ValueGeneratedNever();
        substitutivos.Property(substitutivo => substitutivo.ProposicaoId)
            .HasConversion(id => id.Value, value => new ProposicaoId(value));
        substitutivos.Property(substitutivo => substitutivo.Texto).HasMaxLength(8000);
        substitutivos.Property(substitutivo => substitutivo.Autoria)
            .HasConversion(autoria => autoria.Valor, valor => Autoria.De(valor))
            .HasMaxLength(Autoria.ComprimentoMaximo);
    }

    private static void MapearTramitacoes(OwnedNavigationBuilder<Proposicao, Tramitacao> tramitacoes)
    {
        tramitacoes.ToTable("ProposicoesTramitacoes");
        tramitacoes.WithOwner().HasForeignKey("ProposicaoOwnerId");
        tramitacoes.HasKey(tramitacao => tramitacao.Id);
        tramitacoes.Property(tramitacao => tramitacao.Id)
            .HasConversion(id => id.Value, value => new TramitacaoId(value))
            .ValueGeneratedNever();
        tramitacoes.Property(tramitacao => tramitacao.ProposicaoId)
            .HasConversion(id => id.Value, value => new ProposicaoId(value));
        tramitacoes.Property(tramitacao => tramitacao.Fase).HasConversion<string>().HasMaxLength(20);
        tramitacoes.Property(tramitacao => tramitacao.Comissao).HasMaxLength(100);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;
using ProposicaoId = Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes.ProposicaoId;
using SessaoId = Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes.SessaoId;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Votacao"/> e de suas entidades filhas.</summary>
public sealed class VotacaoConfiguration : IEntityTypeConfiguration<Votacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Votacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Votacoes");
        builder.HasKey(votacao => votacao.Id);
        builder.Property(votacao => votacao.Id)
            .HasConversion(id => id.Value, value => new VotacaoId(value))
            .ValueGeneratedNever();

        builder.Property(votacao => votacao.SessaoId)
            .HasConversion(id => id.Value, value => new SessaoId(value));
        builder.Property(votacao => votacao.ProposicaoId)
            .HasConversion(id => id.Value, value => new ProposicaoId(value));

        builder.Property(votacao => votacao.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(votacao => votacao.MaioriaExigida).HasConversion<string>().HasMaxLength(20);
        builder.Property(votacao => votacao.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(votacao => votacao.Resultado).HasConversion<string>().HasMaxLength(20);
        builder.Property(votacao => votacao.TotalMembros);
        builder.Property(votacao => votacao.Presentes);
        builder.Property(votacao => votacao.Turno);

        // Propriedades calculadas (sem coluna).
        builder.Ignore(votacao => votacao.VotosSim);
        builder.Ignore(votacao => votacao.VotosNao);
        builder.Ignore(votacao => votacao.Abstencoes);

        builder.OwnsMany(votacao => votacao.Votos, MapearVotos);
        builder.Navigation(votacao => votacao.Votos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(votacao => new { votacao.TenantId, votacao.ProposicaoId });
    }

    private static void MapearVotos(OwnedNavigationBuilder<Votacao, Voto> votos)
    {
        votos.ToTable("VotacoesVotos");
        votos.WithOwner().HasForeignKey("VotacaoOwnerId");
        votos.HasKey(voto => voto.Id);
        votos.Property(voto => voto.Id)
            .HasConversion(id => id.Value, value => new VotoId(value))
            .ValueGeneratedNever();
        votos.Property(voto => voto.VereadorId)
            .HasConversion(id => id.Value, value => new VereadorId(value));
        votos.Property(voto => voto.Sentido).HasConversion<string>().HasMaxLength(20);
    }
}

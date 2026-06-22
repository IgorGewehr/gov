using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Sessao"/> e de suas entidades filhas.</summary>
public sealed class SessaoConfiguration : IEntityTypeConfiguration<Sessao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sessao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Sessoes");
        builder.HasKey(sessao => sessao.Id);
        builder.Property(sessao => sessao.Id)
            .HasConversion(id => id.Value, value => new SessaoId(value))
            .ValueGeneratedNever();

        builder.Property(sessao => sessao.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(sessao => sessao.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(sessao => sessao.TotalMembros);

        builder.Property(sessao => sessao.DataHora)
            .HasConversion(dataHora => dataHora.Valor, valor => DataHora.De(valor));

        // Propriedades calculadas (sem coluna).
        builder.Ignore(sessao => sessao.QuorumInstalacao);
        builder.Ignore(sessao => sessao.Terminal);

        builder.OwnsMany(sessao => sessao.Presencas, MapearPresencas);
        builder.OwnsMany(sessao => sessao.OrdemDoDia, MapearOrdemDoDia);

        builder.Navigation(sessao => sessao.Presencas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(sessao => sessao.OrdemDoDia).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(sessao => new { sessao.TenantId, sessao.Situacao });
    }

    private static void MapearPresencas(OwnedNavigationBuilder<Sessao, Presenca> presencas)
    {
        presencas.ToTable("SessoesPresencas");
        presencas.WithOwner().HasForeignKey("SessaoOwnerId");
        presencas.HasKey(presenca => presenca.Id);
        presencas.Property(presenca => presenca.Id)
            .HasConversion(id => id.Value, value => new PresencaId(value))
            .ValueGeneratedNever();
        presencas.Property(presenca => presenca.VereadorId)
            .HasConversion(id => id.Value, value => new VereadorId(value));
    }

    private static void MapearOrdemDoDia(OwnedNavigationBuilder<Sessao, ItemOrdemDoDia> itens)
    {
        itens.ToTable("SessoesOrdemDoDia");
        itens.WithOwner().HasForeignKey("SessaoOwnerId");
        itens.HasKey(item => item.Id);
        itens.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemId(value))
            .ValueGeneratedNever();
        itens.Property(item => item.ProposicaoId)
            .HasConversion(id => id.Value, value => new ProposicaoId(value));
    }
}

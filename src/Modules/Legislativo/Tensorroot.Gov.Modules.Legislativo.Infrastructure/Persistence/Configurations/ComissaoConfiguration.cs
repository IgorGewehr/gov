using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Comissao"/> e de seus membros.</summary>
public sealed class ComissaoConfiguration : IEntityTypeConfiguration<Comissao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Comissao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Comissoes");
        builder.HasKey(comissao => comissao.Id);
        builder.Property(comissao => comissao.Id)
            .HasConversion(id => id.Value, value => new ComissaoId(value))
            .ValueGeneratedNever();

        builder.Property(comissao => comissao.Nome).HasMaxLength(Comissao.NomeMaximo);
        builder.Property(comissao => comissao.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(comissao => comissao.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.Ignore(comissao => comissao.Presidente);
        builder.Ignore(comissao => comissao.Terminal);

        builder.OwnsMany(comissao => comissao.Membros, MapearMembros);
        builder.Navigation(comissao => comissao.Membros).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(comissao => new { comissao.TenantId, comissao.Situacao });
    }

    private static void MapearMembros(OwnedNavigationBuilder<Comissao, MembroComissao> membros)
    {
        membros.ToTable("ComissoesMembros");
        membros.WithOwner().HasForeignKey("ComissaoOwnerId");
        membros.HasKey(membro => membro.Id);
        membros.Property(membro => membro.Id)
            .HasConversion(id => id.Value, value => new MembroComissaoId(value))
            .ValueGeneratedNever();
        membros.Property(membro => membro.VereadorId)
            .HasConversion(id => id.Value, value => new VereadorId(value));
        membros.Property(membro => membro.Papel).HasConversion<string>().HasMaxLength(20);
        membros.Property(membro => membro.Cargo).HasConversion<string>().HasMaxLength(20);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="TribunaSessao"/>, inscricoes e pausas.</summary>
public sealed class TribunaSessaoConfiguration : IEntityTypeConfiguration<TribunaSessao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TribunaSessao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Tribunas");
        builder.HasKey(tribuna => tribuna.Id);
        builder.Property(tribuna => tribuna.Id)
            .HasConversion(id => id.Value, value => new TribunaSessaoId(value))
            .ValueGeneratedNever();

        builder.Property(tribuna => tribuna.SessaoId)
            .HasConversion(id => id.Value, value => new SessaoId(value));

        builder.Property(tribuna => tribuna.TempoPadraoOrador);

        builder.Ignore(tribuna => tribuna.OradorEmUso);

        builder.OwnsMany(tribuna => tribuna.Inscricoes, MapearInscricoes);
        builder.Navigation(tribuna => tribuna.Inscricoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Uma tribuna por sessao (vinculo interno).
        builder.HasIndex(tribuna => new { tribuna.TenantId, tribuna.SessaoId }).IsUnique();
    }

    private static void MapearInscricoes(OwnedNavigationBuilder<TribunaSessao, InscricaoOrador> inscricoes)
    {
        inscricoes.ToTable("TribunaInscricoes");
        inscricoes.WithOwner().HasForeignKey("TribunaOwnerId");
        inscricoes.HasKey(inscricao => inscricao.Id);
        inscricoes.Property(inscricao => inscricao.Id)
            .HasConversion(id => id.Value, value => new InscricaoOradorId(value))
            .ValueGeneratedNever();
        inscricoes.Property(inscricao => inscricao.VereadorId)
            .HasConversion(id => id.Value, value => new VereadorId(value));
        inscricoes.Property(inscricao => inscricao.Fase).HasConversion<string>().HasMaxLength(30);
        inscricoes.Property(inscricao => inscricao.Situacao).HasConversion<string>().HasMaxLength(20);
        inscricoes.Property(inscricao => inscricao.Ordem);
        inscricoes.Property(inscricao => inscricao.TempoConcedido);
        inscricoes.Property(inscricao => inscricao.IniciadoEm);
        inscricoes.Property(inscricao => inscricao.EncerradoEm);
        inscricoes.Property(inscricao => inscricao.PausaIniciadaEm);
        inscricoes.Property(inscricao => inscricao.Apartes);

        inscricoes.Ignore(inscricao => inscricao.Pausada);
        inscricoes.Ignore(inscricao => inscricao.TotalPausas);
        inscricoes.Ignore(inscricao => inscricao.TempoUtilizado);
        inscricoes.Ignore(inscricao => inscricao.Excedente);

        inscricoes.OwnsMany(inscricao => inscricao.Pausas, MapearPausas);
        inscricoes.Navigation(inscricao => inscricao.Pausas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapearPausas(OwnedNavigationBuilder<InscricaoOrador, PausaFala> pausas)
    {
        pausas.ToTable("TribunaPausas");
        pausas.WithOwner().HasForeignKey("InscricaoOwnerId");
        pausas.Property<int>("Id").ValueGeneratedOnAdd();
        pausas.HasKey("Id");
        pausas.Property(pausa => pausa.Inicio);
        pausas.Property(pausa => pausa.Fim);
        pausas.Ignore(pausa => pausa.Duracao);
    }
}

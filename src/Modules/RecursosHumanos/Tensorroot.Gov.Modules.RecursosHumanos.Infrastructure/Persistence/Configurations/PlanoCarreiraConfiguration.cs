using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="PlanoCarreira"/>.</summary>
public sealed class PlanoCarreiraConfiguration : IEntityTypeConfiguration<PlanoCarreira>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanoCarreira> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PlanosCarreira");
        builder.HasKey(plano => plano.Id);
        builder.Property(plano => plano.Id)
            .HasConversion(id => id.Value, value => new PlanoCarreiraId(value))
            .ValueGeneratedNever();

        builder.Property(plano => plano.DenominacaoCarreira).HasMaxLength(PlanoCarreira.ComprimentoMaximoDenominacao);
        builder.Property(plano => plano.LeiInstituicao).HasMaxLength(200);

        builder.Property(plano => plano.VencimentoBase)
            .HasConversion(vencimento => vencimento.Valor, valor => Vencimento.De(valor))
            .HasColumnName("VencimentoBase")
            .HasColumnType("decimal(18,2)");

        builder.Property(plano => plano.NumeroClasses);
        builder.Property(plano => plano.NumeroReferencias);
        builder.Property(plano => plano.PercentualEntreReferencias).HasColumnType("decimal(7,4)");
        builder.Property(plano => plano.PercentualEntreClasses).HasColumnType("decimal(7,4)");
        builder.Property(plano => plano.IntersticioMeses);
        builder.Property(plano => plano.NotaMinimaProgressao).HasColumnType("decimal(5,2)");
        builder.Property(plano => plano.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(plano => new { plano.TenantId, plano.Situacao });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="EnquadramentoServidor"/> e da entidade filha.</summary>
public sealed class EnquadramentoServidorConfiguration : IEntityTypeConfiguration<EnquadramentoServidor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EnquadramentoServidor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EnquadramentosCarreira");
        builder.HasKey(enquadramento => enquadramento.Id);
        builder.Property(enquadramento => enquadramento.Id)
            .HasConversion(id => id.Value, value => new EnquadramentoServidorId(value))
            .ValueGeneratedNever();

        builder.Property(enquadramento => enquadramento.ServidorId)
            .HasConversion(servidor => servidor.Value, valor => new ServidorId(valor));

        builder.Property(enquadramento => enquadramento.PlanoCarreiraId)
            .HasConversion(plano => plano.Value, valor => new PlanoCarreiraId(valor));

        builder.Property(enquadramento => enquadramento.ClasseAtual);
        builder.Property(enquadramento => enquadramento.ReferenciaAtual);
        builder.Ignore(enquadramento => enquadramento.PosicaoAtual);

        // Um enquadramento vigente por servidor/tenant.
        builder.HasIndex(enquadramento => new { enquadramento.TenantId, enquadramento.ServidorId }).IsUnique();

        builder.OwnsMany(enquadramento => enquadramento.Movimentacoes, movimentacao =>
        {
            movimentacao.ToTable("MovimentacoesCarreira");
            movimentacao.WithOwner().HasForeignKey("EnquadramentoServidorId");
            movimentacao.HasKey(m => m.Id);
            movimentacao.Property(m => m.Id)
                .HasConversion(id => id.Value, value => new MovimentacaoCarreiraId(value))
                .ValueGeneratedNever();
            movimentacao.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(30);
            movimentacao.Property(m => m.ClasseOrigem);
            movimentacao.Property(m => m.ReferenciaOrigem);
            movimentacao.Property(m => m.ClasseDestino);
            movimentacao.Property(m => m.ReferenciaDestino);
            movimentacao.Property(m => m.VencimentoResultante).HasColumnType("decimal(18,2)");
            movimentacao.Property(m => m.Criterio).HasConversion<string>().HasMaxLength(30);
            movimentacao.Property(m => m.DataEfeito);
            movimentacao.Property(m => m.Fundamento).HasMaxLength(500);
            movimentacao.Property(m => m.PortariaId);
        });

        builder.Navigation(enquadramento => enquadramento.Movimentacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

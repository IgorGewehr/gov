using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using PrescricaoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PrescricaoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do catalogo <see cref="Medicamento"/>.</summary>
public sealed class MedicamentoConfiguration : IEntityTypeConfiguration<Medicamento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Medicamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Medicamentos");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new MedicamentoId(value))
            .ValueGeneratedNever();

        builder.Property(m => m.PrincipioAtivo).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Apresentacao).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Concentracao).HasMaxLength(60).IsRequired();
        builder.Property(m => m.Forma).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Unidade).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Controle).HasConversion<string>().HasMaxLength(40);
        builder.Property(m => m.CodigoCatmat).HasMaxLength(20);
        builder.Property(m => m.Ativo);
        builder.Ignore(m => m.ExigeReceitaControlada);

        builder.HasIndex(m => new { m.TenantId, m.PrincipioAtivo });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="EstoqueMedicamento"/> e dos lotes (filhos).</summary>
public sealed class EstoqueMedicamentoConfiguration : IEntityTypeConfiguration<EstoqueMedicamento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EstoqueMedicamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EstoquesMedicamento");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EstoqueMedicamentoId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.EstabelecimentoId)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value));
        builder.Property(e => e.MedicamentoId)
            .HasConversion(id => id.Value, value => new MedicamentoId(value));
        builder.Property(e => e.Saldo).HasPrecision(18, 3);
        builder.Property(e => e.PontoDeRessuprimento);

        builder.OwnsMany(e => e.Lotes, MapearLotes);
        builder.Navigation(e => e.Lotes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => new { e.TenantId, e.EstabelecimentoId, e.MedicamentoId }).IsUnique();
    }

    private static void MapearLotes(OwnedNavigationBuilder<EstoqueMedicamento, LoteMedicamento> lotes)
    {
        lotes.ToTable("EstoquesMedicamentoLotes");
        lotes.WithOwner().HasForeignKey("EstoqueMedicamentoId");
        lotes.HasKey(l => l.Id);
        lotes.Property(l => l.Id)
            .HasConversion(id => id.Value, value => new LoteMedicamentoId(value))
            .ValueGeneratedNever();
        lotes.Property(l => l.NumeroLote).HasMaxLength(40).IsRequired();
        lotes.Property(l => l.Validade);
        lotes.Property(l => l.Saldo).HasPrecision(18, 3);
        lotes.Property(l => l.QuantidadeEntrada).HasPrecision(18, 3);
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="Dispensacao"/> e dos itens dispensados (filhos).</summary>
public sealed class DispensacaoConfiguration : IEntityTypeConfiguration<Dispensacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Dispensacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Dispensacoes");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(id => id.Value, value => new DispensacaoId(value))
            .ValueGeneratedNever();

        builder.Property(d => d.PacienteId)
            .HasConversion(id => id.Value, value => new PacienteId(value));
        builder.Property(d => d.EstabelecimentoId)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value));
        builder.Property(d => d.ProfissionalId)
            .HasConversion(id => id.Value, value => new ProfissionalId(value));
        builder.Property(d => d.PrescricaoId)
            .HasConversion(id => id!.Value.Value, value => new PrescricaoId(value));
        builder.Property(d => d.DataHora);
        builder.Property(d => d.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.OwnsMany(d => d.Itens, MapearItens);
        builder.Navigation(d => d.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(d => new { d.TenantId, d.PacienteId });
    }

    private static void MapearItens(OwnedNavigationBuilder<Dispensacao, ItemDispensado> itens)
    {
        itens.ToTable("DispensacoesItens");
        itens.WithOwner().HasForeignKey("DispensacaoId");
        itens.HasKey(i => i.Id);
        itens.Property(i => i.Id)
            .HasConversion(id => id.Value, value => new ItemDispensadoId(value))
            .ValueGeneratedNever();
        itens.Property(i => i.MedicamentoId)
            .HasConversion(id => id.Value, value => new MedicamentoId(value));
        itens.Property(i => i.Quantidade).HasPrecision(18, 3);
        itens.Property(i => i.Posologia).HasMaxLength(500).IsRequired();

        // Rastro de baixas FEFO (lote/validade/quantidade) — owned children do item dispensado.
        itens.OwnsMany(i => i.Baixas, MapearBaixas);
        itens.Navigation(i => i.Baixas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapearBaixas(OwnedNavigationBuilder<ItemDispensado, BaixaLote> baixas)
    {
        baixas.ToTable("DispensacoesItensBaixas");
        baixas.WithOwner().HasForeignKey("ItemDispensadoId");

        // Chave-sombra Guid GERADA NO CLIENTE (portavel SQL Server/SQLite): uma chave int Identity
        // composta nao auto-incrementa no SQLite (a coluna deixa de ser alias de rowid).
        baixas.Property<Guid>("Id").ValueGeneratedOnAdd();
        baixas.HasKey("Id");

        baixas.Property(b => b.LoteId)
            .HasConversion(id => id.Value, value => new LoteMedicamentoId(value));
        baixas.Property(b => b.NumeroLote).HasMaxLength(40);
        baixas.Property(b => b.Validade);
        baixas.Property(b => b.Quantidade).HasPrecision(18, 3);
    }
}

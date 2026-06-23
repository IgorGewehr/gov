using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="EstabelecimentoFiscalizavel"/> (VISA).</summary>
public sealed class EstabelecimentoFiscalizavelConfiguration : IEntityTypeConfiguration<EstabelecimentoFiscalizavel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EstabelecimentoFiscalizavel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EstabelecimentosFiscalizaveis");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EstabelecimentoFiscalizavelId(value))
            .ValueGeneratedNever();

        // Documento (CNPJ/CPF) persistido como "J:/F:" + digitos num unico campo (VO via conversao).
        builder.Property(e => e.Documento)
            .HasColumnName("DocumentoPersistido")
            .HasConversion(d => d.ParaPersistencia(), v => DocumentoResponsavel.DePersistencia(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.RazaoSocial).HasMaxLength(EstabelecimentoFiscalizavel.ComprimentoNome).IsRequired();
        builder.Property(e => e.Ramo).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Risco).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(e => e.Endereco, endereco =>
        {
            endereco.Property(en => en.Logradouro).HasColumnName("Logradouro").HasMaxLength(200);
            endereco.Property(en => en.Bairro).HasColumnName("Bairro").HasMaxLength(100);
            endereco.Property(en => en.Municipio).HasColumnName("Municipio").HasMaxLength(100);
            endereco.Property(en => en.Uf).HasColumnName("Uf").HasMaxLength(2);
            endereco.Property(en => en.Cep).HasColumnName("Cep").HasMaxLength(8);
        });

        // O indice unico usa a coluna do documento (mapeada como "DocumentoPersistido" via value converter).
        builder.HasIndex(e => new { e.TenantId, e.Documento, e.RazaoSocial }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Situacao });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="Inspecao"/> e dos itens do roteiro (filhos).</summary>
public sealed class InspecaoConfiguration : IEntityTypeConfiguration<Inspecao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Inspecao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Inspecoes");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(id => id.Value, value => new InspecaoId(value))
            .ValueGeneratedNever();

        builder.Property(i => i.EstabelecimentoFiscalizavelId)
            .HasConversion(id => id.Value, value => new EstabelecimentoFiscalizavelId(value));
        builder.Property(i => i.FiscalId)
            .HasConversion(id => id!.Value.Value, value => new ProfissionalId(value));
        builder.Property(i => i.DataInspecao);
        builder.Property(i => i.Roteiro).HasMaxLength(120);
        builder.Property(i => i.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Resultado).HasConversion<string>().HasMaxLength(30);

        builder.OwnsMany(i => i.Itens, MapearItens);
        builder.Navigation(i => i.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(i => new { i.TenantId, i.EstabelecimentoFiscalizavelId });
        builder.HasIndex(i => new { i.TenantId, i.DataInspecao });
    }

    private static void MapearItens(OwnedNavigationBuilder<Inspecao, ItemInspecao> itens)
    {
        itens.ToTable("InspecoesItens");
        itens.WithOwner().HasForeignKey("InspecaoId");
        itens.HasKey(it => it.Id);
        itens.Property(it => it.Id)
            .HasConversion(id => id.Value, value => new ItemInspecaoId(value))
            .ValueGeneratedNever();
        itens.Property(it => it.Requisito).HasMaxLength(200).IsRequired();
        itens.Property(it => it.Conformidade).HasConversion<string>().HasMaxLength(20);
        itens.Property(it => it.Observacao).HasMaxLength(1000);
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="AutoVisa"/> (autos de infracao/intimacao).</summary>
public sealed class AutoVisaConfiguration : IEntityTypeConfiguration<AutoVisa>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AutoVisa> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AutosVisa");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AutoVisaId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.EstabelecimentoFiscalizavelId)
            .HasConversion(id => id.Value, value => new EstabelecimentoFiscalizavelId(value));
        builder.Property(a => a.InspecaoId)
            .HasConversion(id => id.Value, value => new InspecaoId(value));
        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Numero).HasMaxLength(40).IsRequired();
        builder.Property(a => a.Fundamentacao).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.DataLavratura);
        builder.Property(a => a.PrazoFinal);
        builder.Property(a => a.ValorMulta).HasPrecision(18, 2);
        builder.Property(a => a.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Defesa).HasMaxLength(4000);

        builder.HasIndex(a => new { a.TenantId, a.Numero }).IsUnique();
        builder.HasIndex(a => new { a.TenantId, a.Situacao });
        builder.HasIndex(a => new { a.TenantId, a.PrazoFinal });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="LicencaSanitaria"/> (alvara sanitario).</summary>
public sealed class LicencaSanitariaConfiguration : IEntityTypeConfiguration<LicencaSanitaria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LicencaSanitaria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LicencasSanitarias");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, value => new LicencaSanitariaId(value))
            .ValueGeneratedNever();

        builder.Property(l => l.EstabelecimentoFiscalizavelId)
            .HasConversion(id => id.Value, value => new EstabelecimentoFiscalizavelId(value));
        builder.Property(l => l.InspecaoId)
            .HasConversion(id => id!.Value.Value, value => new InspecaoId(value));
        builder.Property(l => l.Numero).HasMaxLength(40).IsRequired();
        builder.Property(l => l.EmitidaEm);
        builder.Property(l => l.ValidadeAte);
        builder.Property(l => l.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => new { l.TenantId, l.EstabelecimentoFiscalizavelId });
        builder.HasIndex(l => new { l.TenantId, l.Situacao, l.ValidadeAte });
    }
}

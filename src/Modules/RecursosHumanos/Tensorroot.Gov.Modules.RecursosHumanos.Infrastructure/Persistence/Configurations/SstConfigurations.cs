using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ExameOcupacional"/> (ASO — S-2220/PCMSO).</summary>
public sealed class ExameOcupacionalConfiguration : IEntityTypeConfiguration<ExameOcupacional>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExameOcupacional> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SstExamesOcupacionais");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ExameOcupacionalId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.ServidorId)
            .HasConversion(id => id.Value, value => new ServidorId(value));

        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Resultado).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.DataExame);
        builder.Property(e => e.DataProximoExame);
        builder.Property(e => e.Observacao).HasMaxLength(1000);
        builder.Property(e => e.MotivoCancelamento).HasMaxLength(500);

        // Medico responsavel (owned): CRM/UF + nome.
        builder.OwnsOne(e => e.Medico, medico =>
        {
            medico.Property(m => m.Nome).HasColumnName("MedicoNome").HasMaxLength(MedicoResponsavel.ComprimentoMaximoNome).IsRequired();
            medico.Property(m => m.NrConselho).HasColumnName("MedicoNrConselho").HasMaxLength(MedicoResponsavel.ComprimentoMaximoConselho).IsRequired();
            medico.Property(m => m.UfConselho).HasColumnName("MedicoUfConselho").HasMaxLength(2).IsRequired();
        });
        builder.Navigation(e => e.Medico).IsRequired();

        // Exames complementares (List<string> de codigos da Tabela 27) serializados em coluna textual
        // (join por '\n'), via campo de apoio — mesmo padrao de PapelConfiguration (permissoes).
        var comparadorExames = new ValueComparer<List<string>>(
            (esquerda, direita) => esquerda!.SequenceEqual(direita!),
            exames => exames.Aggregate(0, (hash, codigo) => HashCode.Combine(hash, codigo.GetHashCode(StringComparison.Ordinal))),
            exames => exames.ToList());

        builder.Property<List<string>>("_examesComplementares")
            .HasField("_examesComplementares")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                exames => string.Join('\n', exames),
                texto => DesserializarExames(texto))
            .HasColumnName("ExamesComplementares")
            .Metadata.SetValueComparer(comparadorExames);
        builder.Ignore(e => e.ExamesComplementares);

        builder.HasIndex(e => new { e.TenantId, e.ServidorId });
        builder.HasIndex(e => new { e.TenantId, e.DataProximoExame });
    }

    private static List<string> DesserializarExames(string texto)
        => string.IsNullOrEmpty(texto)
            ? []
            : [.. texto.Split('\n', StringSplitOptions.RemoveEmptyEntries)];
}

/// <summary>Mapeamento EF Core do agregado <see cref="ExposicaoAgenteNocivo"/> (S-2240/PPP).</summary>
public sealed class ExposicaoAgenteNocivoConfiguration : IEntityTypeConfiguration<ExposicaoAgenteNocivo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExposicaoAgenteNocivo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SstExposicoesAgenteNocivo");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ExposicaoAgenteNocivoId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.ServidorId)
            .HasConversion(id => id.Value, value => new ServidorId(value));

        builder.Property(e => e.InicioExposicao);
        builder.Property(e => e.FimExposicao);
        builder.Property(e => e.SetorAtividade).HasMaxLength(ExposicaoAgenteNocivo.ComprimentoMaximoSetor).IsRequired();
        builder.Property(e => e.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.MotivoCancelamento).HasMaxLength(500);
        builder.Ignore(e => e.Vigente);

        // Agentes nocivos (owned collection) — itens do periodo de exposicao.
        builder.OwnsMany(e => e.Agentes, agentes =>
        {
            agentes.ToTable("SstExposicaoAgentes");
            agentes.WithOwner().HasForeignKey("ExposicaoAgenteNocivoId");
            agentes.Property<int>("Id").ValueGeneratedOnAdd();
            agentes.HasKey("Id");
            agentes.Property(a => a.Codigo).HasColumnName("Codigo").HasMaxLength(AgenteNocivo.ComprimentoMaximoCodigo).IsRequired();
            agentes.Property(a => a.Descricao).HasColumnName("Descricao").HasMaxLength(AgenteNocivo.ComprimentoMaximoDescricao).IsRequired();
            agentes.Property(a => a.Intensidade).HasColumnName("Intensidade").HasColumnType("decimal(18,4)");
            agentes.Property(a => a.UnidadeMedida).HasColumnName("UnidadeMedida").HasMaxLength(20);
            agentes.Property(a => a.UtilizaEpc).HasColumnName("UtilizaEpc");
            agentes.Property(a => a.UtilizaEpi).HasColumnName("UtilizaEpi");
            agentes.Ignore(a => a.EhAusenciaDeAgente);
        });
        builder.Navigation(e => e.Agentes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => new { e.TenantId, e.ServidorId });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="ComunicacaoAcidente"/> (CAT — S-2210).</summary>
public sealed class ComunicacaoAcidenteConfiguration : IEntityTypeConfiguration<ComunicacaoAcidente>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ComunicacaoAcidente> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SstComunicacoesAcidente");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ComunicacaoAcidenteId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.ServidorId)
            .HasConversion(id => id.Value, value => new ServidorId(value));

        builder.Property(e => e.TipoCat).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.TipoAcidente).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.DataHoraAcidente);
        builder.Property(e => e.HouveObito);
        builder.Property(e => e.DataObito);
        builder.Property(e => e.DescricaoSituacao).HasMaxLength(ComunicacaoAcidente.ComprimentoMaximoDescricao).IsRequired();
        builder.Property(e => e.Cid).HasMaxLength(ComunicacaoAcidente.ComprimentoMaximoCid);
        builder.Property(e => e.ParteCorpoAtingida).HasMaxLength(100);
        builder.Property(e => e.AgenteCausador).HasMaxLength(100);
        builder.Property(e => e.MotivoCancelamento).HasMaxLength(500);

        builder.Property(e => e.CatOrigem)
            .HasConversion(
                origem => origem!.Value.Value,
                valor => new ComunicacaoAcidenteId(valor));

        builder.HasIndex(e => new { e.TenantId, e.ServidorId });
    }
}

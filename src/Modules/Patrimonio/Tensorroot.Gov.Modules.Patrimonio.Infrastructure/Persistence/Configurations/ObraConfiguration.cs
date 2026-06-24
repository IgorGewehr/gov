using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Obra"/> (schema "patrimonio") e de suas entidades filhas.</summary>
public sealed class ObraConfiguration : IEntityTypeConfiguration<Obra>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Obra> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Obras");
        builder.HasKey(obra => obra.Id);
        builder.Property(obra => obra.Id)
            .HasConversion(id => id.Value, value => new ObraId(value))
            .ValueGeneratedNever();

        builder.Property(obra => obra.ContratoId);
        builder.Property(obra => obra.FornecedorId);
        builder.Property(obra => obra.Objeto).HasMaxLength(500);
        builder.Property(obra => obra.RegimeExecucao).HasConversion<string>().HasMaxLength(30);
        builder.Property(obra => obra.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(obra => obra.DataAssinaturaContrato);
        builder.Property(obra => obra.DataInicioOrdemServico);
        builder.Property(obra => obra.DataConclusao);
        builder.Property(obra => obra.PercentualFisicoAcumulado).HasColumnType("decimal(9,4)");
        builder.Property(obra => obra.FiscalDesignadoId);

        // BemPatrimonialId? — nulo até a incorporação (I-13). Converter nullable-safe para Guid?.
        var conversorBemNullable = new ValueConverter<BemPatrimonialId?, Guid?>(
            id => id.HasValue ? id.Value.Value : null,
            value => value.HasValue ? new BemPatrimonialId(value.Value) : null);
        builder.Property(obra => obra.BemPatrimonialId).HasConversion(conversorBemNullable);

        builder.Property(obra => obra.ValorContratado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(obra => obra.ValorMedidoAcumulado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        // Localizacao (VO) como owned single — colunas inline na tabela Obras (municipio LIKE-avel direto).
        builder.OwnsOne(obra => obra.Localizacao, localizacao =>
        {
            localizacao.Property(local => local.Logradouro).HasColumnName("Logradouro").HasMaxLength(300);
            localizacao.Property(local => local.Municipio).HasColumnName("Municipio").HasMaxLength(120);
            localizacao.Property(local => local.Uf).HasColumnName("Uf").HasMaxLength(2);
            localizacao.Property(local => local.Latitude).HasColumnName("Latitude").HasColumnType("decimal(9,6)");
            localizacao.Property(local => local.Longitude).HasColumnName("Longitude").HasColumnType("decimal(9,6)");
            localizacao.Property(local => local.GeoCodigo).HasColumnName("GeoCodigo").HasMaxLength(60);
        });
        builder.Navigation(obra => obra.Localizacao).IsRequired();

        // Propriedades calculadas (sem coluna).
        builder.Ignore(obra => obra.TemCronograma);

        builder.OwnsMany(obra => obra.Etapas, MapearEtapas);
        builder.OwnsMany(obra => obra.Medicoes, MapearMedicoes);
        builder.OwnsMany(obra => obra.RegistrosDiarios, MapearRdos);
        builder.OwnsMany(obra => obra.DesignacoesFiscais, MapearDesignacoes);
        builder.OwnsMany(obra => obra.Ocorrencias, MapearOcorrencias);
        builder.OwnsMany(obra => obra.Paralisacoes, MapearParalisacoes);

        builder.Navigation(obra => obra.Etapas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(obra => obra.Medicoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(obra => obra.RegistrosDiarios).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(obra => obra.DesignacoesFiscais).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(obra => obra.Ocorrencias).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(obra => obra.Paralisacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Vínculo NLLC por ID (cross-context): uma obra por contrato no tenant.
        builder.HasIndex(obra => new { obra.TenantId, obra.ContratoId }).IsUnique();
        builder.HasIndex(obra => new { obra.TenantId, obra.Situacao });
    }

    private static void MapearEtapas(OwnedNavigationBuilder<Obra, EtapaCronograma> etapas)
    {
        etapas.ToTable("ObrasEtapas");
        etapas.WithOwner().HasForeignKey("ObraId");
        etapas.HasKey(etapa => etapa.Id);
        etapas.Property(etapa => etapa.Id)
            .HasConversion(id => id.Value, value => new EtapaCronogramaId(value))
            .ValueGeneratedNever();
        etapas.Property(etapa => etapa.Descricao).HasMaxLength(200);
        etapas.Property(etapa => etapa.Situacao).HasConversion<string>().HasMaxLength(20);
        etapas.Property(etapa => etapa.PercentualFisicoPrevisto).HasColumnType("decimal(9,4)");
        etapas.Property(etapa => etapa.PercentualFisicoExecutado).HasColumnType("decimal(9,4)");
        etapas.Property(etapa => etapa.ValorPrevisto)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        etapas.Property(etapa => etapa.ValorMedido)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearMedicoes(OwnedNavigationBuilder<Obra, Medicao> medicoes)
    {
        medicoes.ToTable("ObrasMedicoes");
        medicoes.WithOwner().HasForeignKey("ObraId");
        medicoes.HasKey(medicao => medicao.Id);
        medicoes.Property(medicao => medicao.Id)
            .HasConversion(id => id.Value, value => new MedicaoId(value))
            .ValueGeneratedNever();
        medicoes.Property(medicao => medicao.Situacao).HasConversion<string>().HasMaxLength(20);
        medicoes.Property(medicao => medicao.MotivoRejeicao).HasMaxLength(500);
        medicoes.Property(medicao => medicao.PercentualFisicoNoPeriodo).HasColumnType("decimal(9,4)");
        medicoes.Property(medicao => medicao.ValorMedido)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        // Numeração única por obra (I-7).
        medicoes.HasIndex("ObraId", nameof(Medicao.Numero)).IsUnique();

        // Linhas da medição (avanço por etapa) — lastro do reconhecimento nas etapas (I-5).
        medicoes.OwnsMany(medicao => medicao.Itens, itens =>
        {
            itens.ToTable("ObrasMedicoesItens");
            itens.WithOwner().HasForeignKey("MedicaoId");
            itens.HasKey(item => item.Id);
            itens.Property(item => item.Id)
                .HasConversion(id => id.Value, value => new ItemMedicaoId(value))
                .ValueGeneratedNever();
            itens.Property(item => item.EtapaId)
                .HasConversion(id => id.Value, value => new EtapaCronogramaId(value));
            itens.Property(item => item.PercentualFisicoNoPeriodo).HasColumnType("decimal(9,4)");
            itens.Property(item => item.ValorNoPeriodo)
                .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
                .HasColumnType("decimal(18,2)");
        });
        medicoes.Navigation(medicao => medicao.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapearRdos(OwnedNavigationBuilder<Obra, RegistroDiarioObra> rdos)
    {
        rdos.ToTable("ObrasRdos");
        rdos.WithOwner().HasForeignKey("ObraId");
        rdos.HasKey(rdo => rdo.Id);
        rdos.Property(rdo => rdo.Id)
            .HasConversion(id => id.Value, value => new RegistroDiarioObraId(value))
            .ValueGeneratedNever();
        rdos.Property(rdo => rdo.CondicaoTempo).HasMaxLength(60);
        rdos.Property(rdo => rdo.EquipamentosMobilizados).HasMaxLength(1000);
        rdos.Property(rdo => rdo.AtividadesExecutadas).HasMaxLength(2000);
        rdos.Property(rdo => rdo.Ocorrencias).HasMaxLength(2000);
        // RDO único por dia/obra (I-9).
        rdos.HasIndex("ObraId", nameof(RegistroDiarioObra.Data)).IsUnique();
    }

    private static void MapearDesignacoes(OwnedNavigationBuilder<Obra, DesignacaoFiscal> designacoes)
    {
        designacoes.ToTable("ObrasDesignacoesFiscais");
        designacoes.WithOwner().HasForeignKey("ObraId");
        designacoes.HasKey(designacao => designacao.Id);
        designacoes.Property(designacao => designacao.Id)
            .HasConversion(id => id.Value, value => new DesignacaoFiscalId(value))
            .ValueGeneratedNever();
        designacoes.Property(designacao => designacao.AtoDesignacao).HasMaxLength(200);
    }

    private static void MapearOcorrencias(OwnedNavigationBuilder<Obra, OcorrenciaFiscalizacao> ocorrencias)
    {
        ocorrencias.ToTable("ObrasOcorrencias");
        ocorrencias.WithOwner().HasForeignKey("ObraId");
        ocorrencias.HasKey(ocorrencia => ocorrencia.Id);
        ocorrencias.Property(ocorrencia => ocorrencia.Id)
            .HasConversion(id => id.Value, value => new OcorrenciaFiscalizacaoId(value))
            .ValueGeneratedNever();
        ocorrencias.Property(ocorrencia => ocorrencia.Tipo).HasConversion<string>().HasMaxLength(20);
        ocorrencias.Property(ocorrencia => ocorrencia.Descricao).HasMaxLength(1000);
    }

    private static void MapearParalisacoes(OwnedNavigationBuilder<Obra, EventoParalisacao> paralisacoes)
    {
        paralisacoes.ToTable("ObrasParalisacoes");
        paralisacoes.WithOwner().HasForeignKey("ObraId");
        paralisacoes.HasKey(paralisacao => paralisacao.Id);
        paralisacoes.Property(paralisacao => paralisacao.Id)
            .HasConversion(id => id.Value, value => new EventoParalisacaoId(value))
            .ValueGeneratedNever();
        paralisacoes.Property(paralisacao => paralisacao.Motivo).HasConversion<string>().HasMaxLength(30);
        paralisacoes.Ignore(paralisacao => paralisacao.EmAberto);
    }
}

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Contrato"/> e de suas entidades filhas.</summary>
public sealed class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Contratos");
        builder.HasKey(contrato => contrato.Id);
        builder.Property(contrato => contrato.Id)
            .HasConversion(id => id.Value, value => new ContratoId(value))
            .ValueGeneratedNever();

        builder.Property(contrato => contrato.Objeto).HasMaxLength(2000);
        builder.Property(contrato => contrato.OrigemContratacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(contrato => contrato.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(contrato => contrato.NumeroContratoPncp).HasMaxLength(60);

        // W9.1: marco inicial do prazo PNCP (art. 94) + flag de intempestividade da divulgacao.
        builder.Property(contrato => contrato.DataAssinatura);
        builder.Property(contrato => contrato.PublicacaoPncpVencida);

        // Prazo de divulgacao no PNCP (VO PrazoPncp) — serializado em coluna unica, nullable (legados sem
        // prazo). Reidratado por PrazoPncp.Reidratar a partir dos valores ja resolvidos (sem reler o
        // calendario): o vencimento persistido e a fonte de verdade do ato praticado na celebracao.
        builder.Property(contrato => contrato.PrazoPublicacaoPncp)
            .HasColumnName("PrazoPublicacaoPncp")
            .HasMaxLength(120)
            .HasConversion(prazo => SerializarPrazoPncp(prazo), valor => DesserializarPrazoPncp(valor));

        builder.Property(contrato => contrato.ValorContratado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(contrato => contrato.ValorAtual)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        // Empenho (Value Object opcional, struct) — referencia ao ato de empenho de Financas.
        // Persistido em coluna unica ("{empenhoId}|{numeroEmpenho}"); nulo quando a dotacao nao foi confirmada.
        builder.Property(contrato => contrato.EmpenhoRef)
            .HasColumnName("EmpenhoRef")
            .HasMaxLength(80)
            .HasConversion(empenho => Serializar(empenho), valor => Desserializar(valor));

        // Propriedade calculada (sem coluna).
        builder.Ignore(contrato => contrato.PercentualQuantitativoAcumulado);

        builder.OwnsMany(contrato => contrato.Aditivos, MapearAditivos);
        builder.OwnsMany(contrato => contrato.Apostilamentos, MapearApostilamentos);
        builder.OwnsMany(contrato => contrato.Garantias, MapearGarantias);

        builder.Navigation(contrato => contrato.Aditivos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(contrato => contrato.Apostilamentos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(contrato => contrato.Garantias).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(contrato => new { contrato.TenantId, contrato.FornecedorId });
        builder.HasIndex(contrato => new { contrato.TenantId, contrato.Situacao });
    }

    private const char SeparadorEmpenho = '|';

    private static string? Serializar(EmpenhoRef? empenho)
        => empenho is null
            ? null
            : empenho.Value.EmpenhoId.ToString("D") + SeparadorEmpenho + empenho.Value.NumeroEmpenho;

    private static EmpenhoRef? Desserializar(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        var separador = valor.IndexOf(SeparadorEmpenho, StringComparison.Ordinal);
        var empenhoId = Guid.Parse(valor[..separador]);
        var numeroEmpenho = valor[(separador + 1)..];
        return EmpenhoRef.De(empenhoId, numeroEmpenho);
    }

    // Formato: "{tipo}|{dataAssinatura:O}|{quantidade}|{unidade}|{dataLimite:O}|{normaFonte}".
    // A norma-fonte vai por ultimo (pode conter '|'? nao; mas split com count limita o risco).
    private const char SeparadorPrazo = '|';

    private static string? SerializarPrazoPncp(PrazoPncp? prazo)
        => prazo is null
            ? null
            : string.Join(
                SeparadorPrazo,
                (int)prazo.Tipo,
                prazo.DataAssinatura.ToString("O", CultureInfo.InvariantCulture),
                prazo.Prazo.Quantidade,
                (int)prazo.Prazo.Unidade,
                prazo.DataLimitePublicacao.ToString("O", CultureInfo.InvariantCulture),
                prazo.NormaFonte);

    private static PrazoPncp? DesserializarPrazoPncp(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        // Limita a 6 segmentos para preservar a norma-fonte intacta no ultimo campo.
        var partes = valor.Split(SeparadorPrazo, 6);
        var tipo = (TipoPrazoPncp)int.Parse(partes[0], CultureInfo.InvariantCulture);
        var dataAssinatura = DateOnly.Parse(partes[1], CultureInfo.InvariantCulture);
        var quantidade = int.Parse(partes[2], CultureInfo.InvariantCulture);
        var unidade = (UnidadePrazo)int.Parse(partes[3], CultureInfo.InvariantCulture);
        var dataLimite = DateOnly.Parse(partes[4], CultureInfo.InvariantCulture);
        var normaFonte = partes[5];
        return PrazoPncp.Reidratar(tipo, dataAssinatura, quantidade, unidade, dataLimite, normaFonte);
    }

    private static void MapearAditivos(OwnedNavigationBuilder<Contrato, Aditivo> aditivos)
    {
        aditivos.ToTable("ContratosAditivos");
        aditivos.WithOwner().HasForeignKey("ContratoId");
        aditivos.HasKey(aditivo => aditivo.Id);
        aditivos.Property(aditivo => aditivo.Id)
            .HasConversion(id => id.Value, value => new AditivoId(value))
            .ValueGeneratedNever();
        aditivos.Property(aditivo => aditivo.Tipo).HasConversion<string>().HasMaxLength(20);
        aditivos.Property(aditivo => aditivo.PercentualSobreValorOriginal).HasColumnType("decimal(9,4)");
        aditivos.Property(aditivo => aditivo.Justificativa).HasMaxLength(2000);
        aditivos.Property(aditivo => aditivo.ValorDelta)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        aditivos.Ignore(aditivo => aditivo.EhQuantitativo);
    }

    private static void MapearApostilamentos(OwnedNavigationBuilder<Contrato, Apostilamento> apostilamentos)
    {
        apostilamentos.ToTable("ContratosApostilamentos");
        apostilamentos.WithOwner().HasForeignKey("ContratoId");
        apostilamentos.HasKey(apostilamento => apostilamento.Id);
        apostilamentos.Property(apostilamento => apostilamento.Id)
            .HasConversion(id => id.Value, value => new ApostilamentoId(value))
            .ValueGeneratedNever();
        apostilamentos.Property(apostilamento => apostilamento.Tipo).HasConversion<string>().HasMaxLength(20);
        apostilamentos.Property(apostilamento => apostilamento.Descricao).HasMaxLength(2000);
    }

    private static void MapearGarantias(OwnedNavigationBuilder<Contrato, Garantia> garantias)
    {
        garantias.ToTable("ContratosGarantias");
        garantias.WithOwner().HasForeignKey("ContratoId");
        garantias.HasKey(garantia => garantia.Id);
        garantias.Property(garantia => garantia.Id)
            .HasConversion(id => id.Value, value => new GarantiaId(value))
            .ValueGeneratedNever();
        garantias.Property(garantia => garantia.Modalidade).HasConversion<string>().HasMaxLength(30);
        garantias.Property(garantia => garantia.Percentual).HasColumnType("decimal(9,4)");
        garantias.Property(garantia => garantia.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }
}

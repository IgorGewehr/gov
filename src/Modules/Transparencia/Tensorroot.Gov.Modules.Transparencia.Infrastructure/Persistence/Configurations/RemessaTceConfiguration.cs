using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="RemessaTce"/> e de suas entidades/objetos de valor filhos.</summary>
public sealed class RemessaTceConfiguration : IEntityTypeConfiguration<RemessaTce>
{
    /// <summary>Conversor de <see cref="ReadOnlyMemory{Byte}"/> para <c>byte[]</c> (conteúdo de arquivos/blobs).</summary>
    internal static readonly ValueConverter<ReadOnlyMemory<byte>, byte[]> ConteudoConverter =
        new(memoria => memoria.ToArray(), bytes => new ReadOnlyMemory<byte>(bytes));

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RemessaTce> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RemessasTce");
        builder.HasKey(remessa => remessa.Id);
        builder.Property(remessa => remessa.Id)
            .HasConversion(id => id.Value, value => new RemessaTceId(value))
            .ValueGeneratedNever();

        builder.Property(remessa => remessa.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(remessa => remessa.DataLimite);
        builder.Property(remessa => remessa.DataGeracao);
        builder.Property(remessa => remessa.DataEnvio);
        builder.Property(remessa => remessa.NomeArquivoZip).HasMaxLength(120);
        builder.Property(remessa => remessa.ProtocoloTce).HasMaxLength(120);
        builder.Property("_alertaPrazoEmitido").HasColumnName("AlertaPrazoEmitido");

        // Periodo (objeto de valor) — embutido na linha da remessa.
        builder.OwnsOne(remessa => remessa.Periodo, MapearPeriodo);
        builder.Navigation(remessa => remessa.Periodo).IsRequired();

        // Leiaute (objeto de valor) — embutido na linha da remessa.
        builder.OwnsOne(remessa => remessa.Leiaute, MapearLeiaute);
        builder.Navigation(remessa => remessa.Leiaute).IsRequired();

        // HashIntegridade do pacote (objeto de valor opcional até a geração concluir).
        builder.OwnsOne(remessa => remessa.HashIntegridade, MapearHash);

        // RDI mais recente (entidade-filha) com suas ocorrências.
        builder.OwnsOne(remessa => remessa.ResultadoValidacao, MapearResultadoValidacao);

        // Arquivos componentes (entidades-filhas) com seus registros estruturados.
        builder.OwnsMany(remessa => remessa.Arquivos, MapearArquivos);

        builder.Navigation(remessa => remessa.ResultadoValidacao).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(remessa => remessa.Arquivos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(remessa => remessa.TenantId);
    }

    private static void MapearPeriodo(OwnedNavigationBuilder<RemessaTce, Domain.RemessasTce.Periodo> periodo)
    {
        periodo.Property(item => item.Exercicio).HasColumnName("Exercicio");
        periodo.Property(item => item.Tipo).HasConversion<string>().HasColumnName("PeriodoTipo").HasMaxLength(20);
        periodo.Property(item => item.Numero).HasColumnName("PeriodoNumero");
    }

    private static void MapearLeiaute(OwnedNavigationBuilder<RemessaTce, Leiaute> leiaute)
    {
        leiaute.Property(item => item.Codigo).HasColumnName("LeiauteCodigo").HasMaxLength(40);
        leiaute.Property(item => item.Versao).HasColumnName("LeiauteVersao").HasMaxLength(20);
    }

    private static void MapearHash(OwnedNavigationBuilder<RemessaTce, HashIntegridade> hash)
    {
        hash.Property(item => item.Algoritmo).HasColumnName("HashAlgoritmo").HasMaxLength(20);
        hash.Property(item => item.Valor).HasColumnName("HashValor").HasMaxLength(128);
    }

    private static void MapearResultadoValidacao(OwnedNavigationBuilder<RemessaTce, ResultadoValidacao> resultado)
    {
        resultado.ToTable("RemessasTceResultadosValidacao");
        resultado.WithOwner().HasForeignKey("RemessaTceId");
        resultado.HasKey(item => item.Id);
        resultado.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ResultadoValidacaoId(value))
            .ValueGeneratedNever();
        resultado.Property(item => item.PossuiErro);
        resultado.Property(item => item.QuantidadeErros);
        resultado.Property(item => item.QuantidadeAvisos);
        resultado.Property(item => item.ValidadoEm);
        resultado.Property(item => item.LeiauteVersao).HasMaxLength(20);

        resultado.OwnsMany(item => item.Ocorrencias, MapearOcorrencias);
        resultado.Navigation(item => item.Ocorrencias).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapearOcorrencias(OwnedNavigationBuilder<ResultadoValidacao, OcorrenciaValidacao> ocorrencias)
    {
        ocorrencias.ToTable("RemessasTceOcorrenciasValidacao");
        ocorrencias.WithOwner().HasForeignKey("ResultadoValidacaoId");
        ocorrencias.HasKey(item => item.Id);
        ocorrencias.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new OcorrenciaValidacaoId(value))
            .ValueGeneratedNever();
        ocorrencias.Property(item => item.Arquivo).HasMaxLength(260);
        ocorrencias.Property(item => item.Linha);
        ocorrencias.Property(item => item.Severidade).HasConversion<string>().HasMaxLength(20);
        ocorrencias.Property(item => item.Mensagem).HasMaxLength(1000);
    }

    private static void MapearArquivos(OwnedNavigationBuilder<RemessaTce, ArquivoRemessa> arquivos)
    {
        arquivos.ToTable("RemessasTceArquivos");
        arquivos.WithOwner().HasForeignKey("RemessaTceId");
        arquivos.HasKey(item => item.Id);
        arquivos.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ArquivoRemessaId(value))
            .ValueGeneratedNever();
        arquivos.Property(item => item.NomeArquivo).HasMaxLength(260);
        arquivos.Property(item => item.Conteudo).HasConversion(ConteudoConverter);

        arquivos.OwnsOne(item => item.Hash, hash =>
        {
            hash.Property(valor => valor.Algoritmo).HasColumnName("HashAlgoritmo").HasMaxLength(20);
            hash.Property(valor => valor.Valor).HasColumnName("HashValor").HasMaxLength(128);
        });
        arquivos.Navigation(item => item.Hash).IsRequired();

        arquivos.OwnsMany(item => item.Registros, MapearRegistros);
        arquivos.Navigation(item => item.Registros).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapearRegistros(OwnedNavigationBuilder<ArquivoRemessa, RegistroLeiaute> registros)
    {
        registros.ToTable("RemessasTceRegistros");
        registros.WithOwner().HasForeignKey("ArquivoRemessaId");
        registros.HasKey(item => item.Id);
        registros.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new RegistroLeiauteId(value))
            .ValueGeneratedNever();
        registros.Property(item => item.Tipo).HasMaxLength(40);
        registros.Property(item => item.Conteudo);
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="ConvenioRecebido"/> (fluxo A) e suas entidades filhas. VOs ricos
/// (Plano com listas, Vigencia, Concedente, Contrapartida, prazos das PCs) sao serializados em colunas unicas
/// (portavel SQLite/SqlServer). Repasses, Rendimentos e PCs sao owned-many em tabelas proprias.
/// </summary>
public sealed class ConvenioRecebidoConfiguration : IEntityTypeConfiguration<ConvenioRecebido>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConvenioRecebido> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ConveniosRecebidos");
        builder.HasKey(convenio => convenio.Id);
        builder.Property(convenio => convenio.Id)
            .HasConversion(id => id.Value, value => new ConvenioRecebidoId(value))
            .ValueGeneratedNever();

        builder.Property(convenio => convenio.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(convenio => convenio.MotivoInadimplencia).HasMaxLength(2000);

        // Vigencia (VO com prorrogacoes) — coluna unica JSON, nullable ate a celebracao.
        builder.Property(convenio => convenio.Vigencia)
            .HasColumnName("Vigencia")
            .HasConversion(vigencia => Serializadores.SerializarVigencia(vigencia), valor => Serializadores.DesserializarVigencia(valor));

        // Concedente (VO owned).
        builder.OwnsOne(convenio => convenio.Concedente, concedente =>
        {
            concedente.Property(c => c.Cnpj)
                .HasColumnName("ConcedenteCnpj").HasMaxLength(14).IsRequired()
                .HasConversion(cnpj => cnpj.Digitos, valor => Cnpj.Create(valor));
            concedente.Property(c => c.Nome).HasColumnName("ConcedenteNome").HasMaxLength(200).IsRequired();
            concedente.Property(c => c.Esfera).HasColumnName("ConcedenteEsfera").HasConversion<string>().HasMaxLength(20);
            concedente.Property(c => c.SistemaOrigem).HasColumnName("ConcedenteSistemaOrigem").HasConversion<string>().HasMaxLength(20);
            concedente.Property(c => c.NumeroConvenioTransferegov).HasColumnName("NumeroConvenioTransferegov").HasMaxLength(60);
        });
        builder.Navigation(convenio => convenio.Concedente).IsRequired();

        // Contrapartida (VO owned, opcional ate a celebracao).
        builder.OwnsOne(convenio => convenio.Contrapartida, contrapartida =>
        {
            contrapartida.Property(c => c.Modalidade).HasColumnName("ContrapartidaModalidade").HasConversion<string>().HasMaxLength(20);
            contrapartida.Property(c => c.PercentualMinimo).HasColumnName("ContrapartidaPercentualMinimo").HasColumnType("decimal(9,4)");
            contrapartida.Property(c => c.NormaFontePercentual).HasColumnName("ContrapartidaNormaFonte").HasMaxLength(200);
            contrapartida.Property(c => c.ValorPactuado)
                .HasColumnName("ContrapartidaValorPactuado").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor.Valor, valor => Dinheiro.De(valor));
            contrapartida.Property(c => c.ValorEmpenhado)
                .HasColumnName("ContrapartidaValorEmpenhado").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor.Valor, valor => Dinheiro.De(valor));
        });

        // Plano de trabalho (entidade filha unica — owned). Listas de etapas/parcelas em colunas JSON.
        builder.OwnsOne(convenio => convenio.Plano, plano =>
        {
            plano.ToTable("ConveniosRecebidosPlano");
            plano.WithOwner().HasForeignKey("ConvenioRecebidoId");
            plano.HasKey("ConvenioRecebidoId");
            plano.Property(p => p.Objeto).HasColumnName("Objeto").HasMaxLength(1000).IsRequired();
            plano.Property(p => p.Aprovado).HasColumnName("PlanoAprovado");
            plano.Property(p => p.ValorRepasse)
                .HasColumnName("ValorRepasse").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor.Valor, valor => Dinheiro.De(valor));
            plano.Property(p => p.ValorContrapartida)
                .HasColumnName("PlanoValorContrapartida").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor.Valor, valor => Dinheiro.De(valor));
            // Listas de structs (etapas/parcelas) serializadas em coluna unica JSON, mapeadas pelo backing
            // field (a navegacao publica e IReadOnlyList; o EF le/escreve o campo _etapas/_parcelas).
            plano.Property(p => p.Etapas)
                .HasField("_etapas")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("EtapasJson")
                .HasConversion(EtapasConverter, EtapasComparer);
            plano.Property(p => p.Parcelas)
                .HasField("_parcelas")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("ParcelasJson")
                .HasConversion(ParcelasConverter, ParcelasComparer);
            plano.Ignore(p => p.ValorGlobal);
        });
        builder.Navigation(convenio => convenio.Plano).IsRequired();

        // Repasses (entidade filha — owned-many).
        builder.OwnsMany(convenio => convenio.Repasses, repasses =>
        {
            repasses.ToTable("ConveniosRecebidosRepasses");
            repasses.WithOwner().HasForeignKey("ConvenioRecebidoId");
            repasses.HasKey(repasse => repasse.Id);
            repasses.Property(repasse => repasse.Id).ValueGeneratedNever();
            repasses.Property(repasse => repasse.NumeroOrdem);
            repasses.Property(repasse => repasse.DataPrevista);
            repasses.Property(repasse => repasse.DataLiberada);
            repasses.Property(repasse => repasse.Situacao).HasConversion<string>().HasMaxLength(20);
            repasses.Property(repasse => repasse.Valor)
                .HasColumnName("Valor").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor.Valor, valor => Dinheiro.De(valor));
        });
        builder.Navigation(convenio => convenio.Repasses).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Rendimentos (entidade filha — owned-many).
        builder.OwnsMany(convenio => convenio.Rendimentos, rendimentos =>
        {
            rendimentos.ToTable("ConveniosRecebidosRendimentos");
            rendimentos.WithOwner().HasForeignKey("ConvenioRecebidoId");
            rendimentos.HasKey(rendimento => rendimento.Id);
            rendimentos.Property(rendimento => rendimento.Id).ValueGeneratedNever();
            rendimentos.Property(rendimento => rendimento.Data);
            rendimentos.Property(rendimento => rendimento.Aplicado);
            rendimentos.Property(rendimento => rendimento.Valor)
                .HasColumnName("Valor").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor.Valor, valor => Dinheiro.De(valor));
        });
        builder.Navigation(convenio => convenio.Rendimentos).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Prestacoes de contas (entidade filha — owned-many). Prazos (VO) em colunas JSON; devolucao decimal.
        builder.OwnsMany(convenio => convenio.Prestacoes, prestacoes =>
        {
            prestacoes.ToTable("ConveniosRecebidosPrestacoes");
            prestacoes.WithOwner().HasForeignKey("ConvenioRecebidoId");
            prestacoes.HasKey(pc => pc.Id);
            prestacoes.Property(pc => pc.Id).ValueGeneratedNever();
            prestacoes.Property(pc => pc.Tipo).HasConversion<string>().HasMaxLength(20);
            prestacoes.Property(pc => pc.CompetenciaRef).HasMaxLength(60).IsRequired();
            prestacoes.Property(pc => pc.Situacao).HasConversion<string>().HasMaxLength(30);
            prestacoes.Property(pc => pc.DataSubmissao);
            prestacoes.Property(pc => pc.SaneamentoConcedido);
            prestacoes.Property(pc => pc.Resultado).HasConversion<string?>().HasMaxLength(30);
            prestacoes.Property(pc => pc.PrazoAnalise)
                .HasColumnName("PrazoAnalise")
                .HasConversion(prazo => Serializadores.SerializarPrazo(prazo), valor => Serializadores.DesserializarPrazo(valor));
            prestacoes.Property(pc => pc.PrazoSaneamento)
                .HasColumnName("PrazoSaneamento")
                .HasConversion(prazo => Serializadores.SerializarPrazo(prazo), valor => Serializadores.DesserializarPrazo(valor));
            prestacoes.Property(pc => pc.ValorDevolucao)
                .HasColumnName("ValorDevolucao").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor == null ? (decimal?)null : valor.Valor, valor => valor == null ? null : Dinheiro.De(valor.Value));
        });
        builder.Navigation(convenio => convenio.Prestacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(convenio => new { convenio.TenantId, convenio.Situacao });
    }

    // DTOs de serializacao (Dinheiro tem ctor privado e nao desserializa por JSON; mapeamos por decimal).
    private sealed record EtapaDto(int Ordem, string Descricao, decimal Valor, DateOnly InicioPrevisto, DateOnly FimPrevisto);

    private sealed record ParcelaDto(int NumeroOrdem, decimal Valor, DateOnly DataPrevista);

    private static readonly ValueConverter<IReadOnlyList<EtapaPlanoTrabalho>, string> EtapasConverter =
        new(
            etapas => JsonSerializer.Serialize(
                etapas.Select(e => new EtapaDto(e.Ordem, e.Descricao, e.Valor.Valor, e.InicioPrevisto, e.FimPrevisto)).ToList(), Json),
            valor => (JsonSerializer.Deserialize<List<EtapaDto>>(valor, Json) ?? new List<EtapaDto>())
                .Select(d => new EtapaPlanoTrabalho(d.Ordem, d.Descricao, Dinheiro.De(d.Valor), d.InicioPrevisto, d.FimPrevisto))
                .ToList());

    private static readonly ValueComparer<IReadOnlyList<EtapaPlanoTrabalho>> EtapasComparer =
        new(
            (a, b) => (a ?? new List<EtapaPlanoTrabalho>()).SequenceEqual(b ?? new List<EtapaPlanoTrabalho>()),
            lista => lista.Aggregate(0, (hash, etapa) => HashCode.Combine(hash, etapa.GetHashCode())),
            lista => (IReadOnlyList<EtapaPlanoTrabalho>)lista.ToList());

    private static readonly ValueConverter<IReadOnlyList<ParcelaPrevista>, string> ParcelasConverter =
        new(
            parcelas => JsonSerializer.Serialize(
                parcelas.Select(p => new ParcelaDto(p.NumeroOrdem, p.Valor.Valor, p.DataPrevista)).ToList(), Json),
            valor => (JsonSerializer.Deserialize<List<ParcelaDto>>(valor, Json) ?? new List<ParcelaDto>())
                .Select(d => new ParcelaPrevista(d.NumeroOrdem, Dinheiro.De(d.Valor), d.DataPrevista))
                .ToList());

    private static readonly ValueComparer<IReadOnlyList<ParcelaPrevista>> ParcelasComparer =
        new(
            (a, b) => (a ?? new List<ParcelaPrevista>()).SequenceEqual(b ?? new List<ParcelaPrevista>()),
            lista => lista.Aggregate(0, (hash, parcela) => HashCode.Combine(hash, parcela.GetHashCode())),
            lista => (IReadOnlyList<ParcelaPrevista>)lista.ToList());
}

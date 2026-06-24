using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="ParceriaOsc"/> (fluxo B — MROSC) e suas entidades filhas. VOs
/// ricos (Osc com certidoes, FormaSelecao, Plano com metas/parcelas, Vigencia, PC com prazos) sao serializados
/// em colunas unicas (portavel SQLite/SqlServer). Repasses sao owned-many em tabela propria.
/// </summary>
public sealed class ParceriaOscConfiguration : IEntityTypeConfiguration<ParceriaOsc>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParceriaOsc> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParceriasOsc");
        builder.HasKey(parceria => parceria.Id);
        builder.Property(parceria => parceria.Id)
            .HasConversion(id => id.Value, value => new ParceriaOscId(value))
            .ValueGeneratedNever();

        builder.Property(parceria => parceria.TipoInstrumento).HasConversion<string>().HasMaxLength(30);
        builder.Property(parceria => parceria.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(parceria => parceria.MotivoInadimplencia).HasMaxLength(2000);
        builder.Property(parceria => parceria.GestorParceriaId);
        builder.Property(parceria => parceria.ComissaoMonitoramentoId);
        builder.Ignore(parceria => parceria.PermiteRepasse);

        // Vigencia (VO) — coluna unica JSON, nullable ate a celebracao.
        builder.Property(parceria => parceria.Vigencia)
            .HasColumnName("Vigencia")
            .HasConversion(vigencia => Serializadores.SerializarVigencia(vigencia), valor => Serializadores.DesserializarVigencia(valor));

        // OSC (VO owned). Certidoes (lista de struct) em coluna JSON pelo backing field.
        builder.OwnsOne(parceria => parceria.Osc, osc =>
        {
            osc.Property(o => o.Cnpj)
                .HasColumnName("OscCnpj").HasMaxLength(14).IsRequired()
                .HasConversion(cnpj => cnpj.Digitos, valor => Cnpj.Create(valor));
            osc.Property(o => o.RazaoSocial).HasColumnName("OscRazaoSocial").HasMaxLength(200).IsRequired();
            osc.Property(o => o.NaturezaJuridica).HasColumnName("OscNaturezaJuridica").HasMaxLength(120).IsRequired();
            osc.Property(o => o.ExperienciaPrevia).HasColumnName("OscExperienciaPrevia");
            osc.Property(o => o.CapacidadeTecnica).HasColumnName("OscCapacidadeTecnica");
            osc.Property(o => o.Certidoes)
                .HasField("_certidoes")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("OscCertidoesJson")
                .HasConversion(CertidoesConverter, CertidoesComparer);
        });
        builder.Navigation(parceria => parceria.Osc).IsRequired();

        // FormaSelecao (VO owned).
        builder.OwnsOne(parceria => parceria.FormaSelecao, forma =>
        {
            forma.Property(f => f.Tipo).HasColumnName("SelecaoTipo").HasConversion<string>().HasMaxLength(20);
            forma.Property(f => f.ProcessoId).HasColumnName("SelecaoProcessoId");
            forma.Property(f => f.Edital).HasColumnName("SelecaoEdital").HasMaxLength(120);
            forma.Property(f => f.EditalHomologado).HasColumnName("SelecaoEditalHomologado");
            forma.Property(f => f.FundamentoLegal).HasColumnName("SelecaoFundamentoLegal").HasMaxLength(120);
            forma.Property(f => f.Justificativa).HasColumnName("SelecaoJustificativa").HasMaxLength(4000);
            forma.Ignore(f => f.AptaParaCelebrar);
        });
        builder.Navigation(parceria => parceria.FormaSelecao).IsRequired();

        // Plano de trabalho (entidade filha — owned, opcional ate o registro). Metas/parcelas em coluna JSON.
        builder.OwnsOne(parceria => parceria.Plano, plano =>
        {
            plano.ToTable("ParceriasOscPlano");
            plano.WithOwner().HasForeignKey("ParceriaOscId");
            plano.HasKey("ParceriaOscId");
            plano.Property(p => p.Objeto).HasColumnName("Objeto").HasMaxLength(1000).IsRequired();
            plano.Property(p => p.Aprovado).HasColumnName("PlanoAprovado");
            plano.Property(p => p.ValorGlobal)
                .HasColumnName("ValorGlobal").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor.Valor, valor => Dinheiro.De(valor));
            plano.Property(p => p.Metas)
                .HasField("_metas")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("MetasJson")
                .HasConversion(MetasConverter, MetasComparer);
            plano.Property(p => p.Parcelas)
                .HasField("_parcelas")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("ParcelasJson")
                .HasConversion(ParcelasConverter, ParcelasComparer);
        });

        // Repasses (entidade filha — owned-many).
        builder.OwnsMany(parceria => parceria.Repasses, repasses =>
        {
            repasses.ToTable("ParceriasOscRepasses");
            repasses.WithOwner().HasForeignKey("ParceriaOscId");
            repasses.HasKey(repasse => repasse.Id);
            repasses.Property(repasse => repasse.Id).ValueGeneratedNever();
            repasses.Property(repasse => repasse.NumeroOrdem);
            repasses.Property(repasse => repasse.DataPrevista);
            repasses.Property(repasse => repasse.DataLiberada);
            repasses.Property(repasse => repasse.Condicionantes).HasMaxLength(500);
            repasses.Property(repasse => repasse.Situacao).HasConversion<string>().HasMaxLength(20);
            repasses.Property(repasse => repasse.EmpenhoId);
            repasses.Property(repasse => repasse.LiquidacaoId);
            repasses.Property(repasse => repasse.PagamentoId);
            repasses.Ignore(repasse => repasse.ExecucaoOrcamentariaCompleta);
            repasses.Property(repasse => repasse.Valor)
                .HasColumnName("Valor").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor.Valor, valor => Dinheiro.De(valor));
        });
        builder.Navigation(parceria => parceria.Repasses).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Prestacao de contas da OSC (entidade filha unica — owned). Prazos (VO) em colunas JSON.
        builder.OwnsOne(parceria => parceria.Prestacao, pc =>
        {
            pc.ToTable("ParceriasOscPrestacao");
            pc.WithOwner().HasForeignKey("ParceriaOscId");
            pc.HasKey(p => p.Id);
            pc.Property(p => p.Id).ValueGeneratedNever();
            pc.Property(p => p.Situacao).HasConversion<string>().HasMaxLength(30);
            pc.Property(p => p.EntregaProrrogada);
            pc.Property(p => p.DataRecebimento);
            pc.Property(p => p.SaneamentoConcedido);
            pc.Property(p => p.Resultado).HasConversion<string?>().HasMaxLength(30);
            pc.Ignore(p => p.NaoEntregue);
            pc.Property(p => p.PrazoEntrega)
                .HasColumnName("PrazoEntrega").IsRequired()
                .HasConversion(prazo => Serializadores.SerializarPrazo(prazo)!, valor => Serializadores.DesserializarPrazo(valor)!);
            pc.Property(p => p.PrazoAnalise)
                .HasColumnName("PrazoAnalise")
                .HasConversion(prazo => Serializadores.SerializarPrazo(prazo), valor => Serializadores.DesserializarPrazo(valor));
            pc.Property(p => p.PrazoSaneamento)
                .HasColumnName("PrazoSaneamento")
                .HasConversion(prazo => Serializadores.SerializarPrazo(prazo), valor => Serializadores.DesserializarPrazo(valor));
            pc.Property(p => p.ValorDevolucao)
                .HasColumnName("ValorDevolucao").HasColumnType("decimal(18,2)")
                .HasConversion(valor => valor == null ? (decimal?)null : valor.Valor, valor => valor == null ? null : Dinheiro.De(valor.Value));
        });

        builder.HasIndex(parceria => new { parceria.TenantId, parceria.Situacao });
    }

    private sealed record CertidaoDto(string Tipo, DateOnly ValidaAte);

    private static readonly ValueConverter<IReadOnlyList<CertidaoRegularidade>, string> CertidoesConverter =
        new(
            certidoes => JsonSerializer.Serialize(
                certidoes.Select(c => new CertidaoDto(c.Tipo, c.ValidaAte)).ToList(), Json),
            valor => (JsonSerializer.Deserialize<List<CertidaoDto>>(valor, Json) ?? new List<CertidaoDto>())
                .Select(d => new CertidaoRegularidade(d.Tipo, d.ValidaAte))
                .ToList());

    private static readonly ValueComparer<IReadOnlyList<CertidaoRegularidade>> CertidoesComparer =
        new(
            (a, b) => (a ?? new List<CertidaoRegularidade>()).SequenceEqual(b ?? new List<CertidaoRegularidade>()),
            lista => lista.Aggregate(0, (hash, c) => HashCode.Combine(hash, c.GetHashCode())),
            lista => (IReadOnlyList<CertidaoRegularidade>)lista.ToList());

    private static readonly ValueConverter<IReadOnlyList<MetaOsc>, string> MetasConverter =
        new(
            metas => JsonSerializer.Serialize(metas, Json),
            valor => JsonSerializer.Deserialize<List<MetaOsc>>(valor, Json) ?? new List<MetaOsc>());

    private static readonly ValueComparer<IReadOnlyList<MetaOsc>> MetasComparer =
        new(
            (a, b) => (a ?? new List<MetaOsc>()).SequenceEqual(b ?? new List<MetaOsc>()),
            lista => lista.Aggregate(0, (hash, m) => HashCode.Combine(hash, m.GetHashCode())),
            lista => (IReadOnlyList<MetaOsc>)lista.ToList());

    private sealed record ParcelaRepasseDto(int NumeroOrdem, decimal Valor, DateOnly DataPrevista, string Condicionantes);

    private static readonly ValueConverter<IReadOnlyList<ParcelaRepasse>, string> ParcelasConverter =
        new(
            parcelas => JsonSerializer.Serialize(
                parcelas.Select(p => new ParcelaRepasseDto(p.NumeroOrdem, p.Valor.Valor, p.DataPrevista, p.Condicionantes)).ToList(), Json),
            valor => (JsonSerializer.Deserialize<List<ParcelaRepasseDto>>(valor, Json) ?? new List<ParcelaRepasseDto>())
                .Select(d => new ParcelaRepasse(d.NumeroOrdem, Dinheiro.De(d.Valor), d.DataPrevista, d.Condicionantes))
                .ToList());

    private static readonly ValueComparer<IReadOnlyList<ParcelaRepasse>> ParcelasComparer =
        new(
            (a, b) => (a ?? new List<ParcelaRepasse>()).SequenceEqual(b ?? new List<ParcelaRepasse>()),
            lista => lista.Aggregate(0, (hash, p) => HashCode.Combine(hash, p.GetHashCode())),
            lista => (IReadOnlyList<ParcelaRepasse>)lista.ToList());
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="CertidaoTempoServico"/> (CTC — tempo de servico/contribuicao).
/// Os periodos (VO) sao mapeados como colecao OWNED em tabela-filha (preservando o documento integral, com
/// fatores e abatimentos por linha).
/// </summary>
public sealed class CertidaoTempoServicoConfiguration : IEntityTypeConfiguration<CertidaoTempoServico>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CertidaoTempoServico> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CertidoesTempoServico");
        builder.HasKey(certidao => certidao.Id);
        builder.Property(certidao => certidao.Id)
            .HasConversion(id => id.Value, value => new CertidaoTempoServicoId(value))
            .ValueGeneratedNever();

        builder.Property(certidao => certidao.ServidorId)
            .HasConversion(id => id.Value, value => new ServidorId(value));

        builder.Property(certidao => certidao.Exercicio);
        builder.Property(certidao => certidao.Sequencial);
        builder.Property(certidao => certidao.Finalidade).HasConversion<string>().HasMaxLength(30);
        builder.Property(certidao => certidao.DataEmissao);
        builder.Property(certidao => certidao.OrgaoEmissor).HasMaxLength(CertidaoTempoServico.ComprimentoMaximoOrgaoEmissor).IsRequired();
        builder.Property(certidao => certidao.FinalidadeDescrita).HasMaxLength(CertidaoTempoServico.ComprimentoMaximoFinalidadeDescrita);
        builder.Property(certidao => certidao.Observacao).HasMaxLength(CertidaoTempoServico.ComprimentoMaximoObservacao);
        builder.Property(certidao => certidao.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(certidao => certidao.MotivoAnulacao).HasMaxLength(500);

        // Codigo de autenticacao (VO): coluna unica por tenant para validacao publica direta por codigo.
        builder.Property(certidao => certidao.CodigoAutenticacao)
            .HasConversion(codigo => codigo.Valor, valor => CodigoAutenticacao.De(valor))
            .HasColumnName("CodigoAutenticacao")
            .HasMaxLength(CodigoAutenticacao.Comprimento)
            .IsRequired();

        // Numeracao oficial unica por (tenant, exercicio, sequencial) — a sequencia nao se repete no ano.
        builder.HasIndex(certidao => new { certidao.TenantId, certidao.Exercicio, certidao.Sequencial }).IsUnique();
        builder.HasIndex(certidao => new { certidao.TenantId, certidao.ServidorId });
        builder.HasIndex(certidao => new { certidao.TenantId, certidao.CodigoAutenticacao }).IsUnique();

        // Periodos computados (VO) como colecao OWNED em tabela-filha.
        builder.OwnsMany(certidao => certidao.Periodos, MapearPeriodos);
        builder.Navigation(certidao => certidao.Periodos).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Totais sao derivados dos periodos — nao persistir (evita estado redundante divergente).
        builder.Ignore(certidao => certidao.Numero);
        builder.Ignore(certidao => certidao.TotalDias);
        builder.Ignore(certidao => certidao.TotalDiasLiquidos);
        builder.Ignore(certidao => certidao.TempoTotal);
    }

    private static void MapearPeriodos(OwnedNavigationBuilder<CertidaoTempoServico, PeriodoTempo> periodos)
    {
        periodos.ToTable("CertidoesTempoServicoPeriodos");

        // VO sem identidade propria: o EF gera e GERENCIA a chave tecnica da colecao owned — FK do dono
        // (CertidaoTempoServicoId) + ordinal int "Id" auto-incrementado — preservando a sequencia do
        // documento sem o agregado precisar materializar um Id artificial. Nao redeclaramos a chave para
        // nao perder o gerenciamento automatico do ordinal pelo EF.
        periodos.WithOwner().HasForeignKey("CertidaoTempoServicoId");

        periodos.Property(periodo => periodo.Inicio);
        periodos.Property(periodo => periodo.Fim);
        periodos.Property(periodo => periodo.DiasNaoComputaveis);
        periodos.Property(periodo => periodo.Natureza).HasConversion<string>().HasMaxLength(20);
        periodos.Property(periodo => periodo.Fator).HasColumnType("decimal(5,2)");
        periodos.Property(periodo => periodo.RegimeOrigem).HasConversion<string?>().HasMaxLength(20);
        periodos.Property(periodo => periodo.Origem).HasMaxLength(200);
        periodos.Property(periodo => periodo.Observacao).HasMaxLength(500);

        // Derivados do periodo (dias brutos/liquidos/equivalentes) nao sao persistidos.
        periodos.Ignore(periodo => periodo.DiasBrutos);
        periodos.Ignore(periodo => periodo.DiasLiquidos);
        periodos.Ignore(periodo => periodo.DiasEquivalentes);
    }
}

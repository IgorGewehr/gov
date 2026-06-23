using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="PedidoInformacaoSic"/> (e-SIC / LAI). VOs mapeadas como
/// owned (Protocolo, Solicitante, Resposta, Recurso). Indice unico (TenantId, Protocolo) — protocolo
/// unico por (tenant, ano), e a unicidade do protocolo ja embute o ano.
/// </summary>
public sealed class PedidoInformacaoSicConfiguration : IEntityTypeConfiguration<PedidoInformacaoSic>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PedidoInformacaoSic> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PedidoInformacaoSic");
        builder.HasKey(pedido => pedido.Id);
        builder.Property(pedido => pedido.Id)
            .HasConversion(id => id.Value, value => new PedidoInformacaoSicId(value))
            .ValueGeneratedNever();

        builder.Property(pedido => pedido.Descricao).HasMaxLength(4000).IsRequired();
        builder.Property(pedido => pedido.FormaResposta).HasConversion<string>().HasMaxLength(30);
        builder.Property(pedido => pedido.DataAbertura);
        builder.Property(pedido => pedido.PrazoResposta);
        builder.Property(pedido => pedido.ProrrogadoAte);
        builder.Property(pedido => pedido.MotivoProrrogacao).HasMaxLength(2000);
        builder.Property(pedido => pedido.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(pedido => pedido.FundamentoIndeferimento).HasMaxLength(4000);

        // Protocolo (VO owned): a unicidade por (tenant, ano) e garantida pela geracao tenant-scoped do
        // sequencial (ProximoSequencialAsync) + Global Query Filter; aqui mantemos um indice de BUSCA
        // (nao-unico, pois a coluna pertence ao tipo owned) por Valor para a consulta por protocolo.
        builder.OwnsOne(pedido => pedido.Protocolo, protocolo =>
        {
            protocolo.Property(p => p.Ano).HasColumnName("ProtocoloAno");
            protocolo.Property(p => p.Sequencial).HasColumnName("ProtocoloSequencial");
            protocolo.Property(p => p.Valor).HasColumnName("Protocolo").HasMaxLength(20).IsRequired();
            protocolo.HasIndex(p => p.Valor).IsUnique();
        });
        builder.Navigation(pedido => pedido.Protocolo).IsRequired();

        // Solicitante (VO owned) — PII; visivel so na superficie interna autenticada.
        builder.OwnsOne(pedido => pedido.Solicitante, solicitante =>
        {
            solicitante.Property(s => s.Nome).HasColumnName("SolicitanteNome").HasMaxLength(200).IsRequired();
            solicitante.Property(s => s.Documento).HasColumnName("SolicitanteDocumento").HasMaxLength(20);
            solicitante.Property(s => s.Contato).HasColumnName("SolicitanteContato").HasMaxLength(254);
            solicitante.Property(s => s.Anonimo).HasColumnName("SolicitanteAnonimo");
        });
        builder.Navigation(pedido => pedido.Solicitante).IsRequired();

        // Resposta (VO owned, opcional).
        builder.OwnsOne(pedido => pedido.Resposta, resposta =>
        {
            resposta.Property(r => r.Texto).HasColumnName("RespostaTexto").HasMaxLength(8000);
            resposta.Property(r => r.ReferenciaAnexo).HasColumnName("RespostaAnexo").HasMaxLength(120);
            resposta.Property(r => r.Data).HasColumnName("RespostaData");
        });

        // Recurso (VO owned, opcional).
        builder.OwnsOne(pedido => pedido.Recurso, recurso =>
        {
            recurso.Property(r => r.Instancia).HasColumnName("RecursoInstancia").HasConversion<string>().HasMaxLength(20);
            recurso.Property(r => r.Fundamento).HasColumnName("RecursoFundamento").HasMaxLength(4000);
            recurso.Property(r => r.DataInterposicao).HasColumnName("RecursoDataInterposicao");
            recurso.Property(r => r.Resultado).HasColumnName("RecursoResultado").HasConversion<string>().HasMaxLength(30);
            recurso.Property(r => r.Decisao).HasColumnName("RecursoDecisao").HasMaxLength(8000);
            recurso.Property(r => r.DataDecisao).HasColumnName("RecursoDataDecisao");
        });

        builder.HasIndex(pedido => new { pedido.TenantId, pedido.Situacao });
    }
}

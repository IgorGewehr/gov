using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using AtendimentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="AtendimentoRaiz"/> e de suas entidades filhas.</summary>
public sealed class AtendimentoConfiguration : IEntityTypeConfiguration<AtendimentoRaiz>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AtendimentoRaiz> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Atendimentos");
        builder.HasKey(atendimento => atendimento.Id);
        builder.Property(atendimento => atendimento.Id)
            .HasConversion(id => id.Value, value => new AtendimentoId(value))
            .ValueGeneratedNever();

        builder.Property(atendimento => atendimento.PacienteId)
            .HasConversion(id => id.Value, value => new PacienteId(value));
        builder.Property(atendimento => atendimento.EstabelecimentoId)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value));
        builder.Property(atendimento => atendimento.ProfissionalId)
            .HasConversion(id => id.Value, value => new ProfissionalId(value));

        builder.Property(atendimento => atendimento.DataHora);

        builder.Property(atendimento => atendimento.Competencia)
            .HasConversion(
                competencia => (competencia.Ano * 100) + competencia.Mes,
                valor => Competencia.De(valor / 100, valor % 100));

        builder.Property(atendimento => atendimento.Modalidade).HasConversion<string>().HasMaxLength(20);
        builder.Property(atendimento => atendimento.NivelGarantia).HasConversion<string>().HasMaxLength(10);
        builder.Property(atendimento => atendimento.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(atendimento => atendimento.ProtocoloRnds).HasMaxLength(60);

        builder.Property(atendimento => atendimento.Cid)
            .HasConversion(
                cid => cid!.Value.Codigo,
                codigo => Cid.De(codigo))
            .HasMaxLength(10);
        builder.Property(atendimento => atendimento.Ciap)
            .HasConversion(
                ciap => ciap!.Value.Codigo,
                codigo => Ciap.De(codigo))
            .HasMaxLength(4);

        builder.OwnsOne(atendimento => atendimento.Assinatura, MapearAssinatura);

        builder.OwnsMany(atendimento => atendimento.Evolucoes, MapearEvolucoes);
        builder.OwnsMany(atendimento => atendimento.Prescricoes, MapearPrescricoes);
        builder.OwnsMany(atendimento => atendimento.SolicitacoesExame, MapearSolicitacoesExame);

        builder.Navigation(atendimento => atendimento.Evolucoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(atendimento => atendimento.Prescricoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(atendimento => atendimento.SolicitacoesExame).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(atendimento => new { atendimento.TenantId, atendimento.PacienteId });
    }

    private static void MapearAssinatura(OwnedNavigationBuilder<AtendimentoRaiz, AssinaturaDigital> assinatura)
    {
        assinatura.Property(dados => dados.CertificadoIcpBrasil).HasColumnName("AssinaturaCertificado").HasMaxLength(200);
        assinatura.Property(dados => dados.Hash).HasColumnName("AssinaturaHash").HasMaxLength(200);
        assinatura.Property(dados => dados.Carimbo).HasColumnName("AssinaturaCarimbo");
        assinatura.Property(dados => dados.Nivel).HasColumnName("AssinaturaNivel").HasConversion<string>().HasMaxLength(10);
    }

    private static void MapearEvolucoes(OwnedNavigationBuilder<AtendimentoRaiz, EvolucaoSOAP> evolucoes)
    {
        evolucoes.ToTable("AtendimentosEvolucoes");
        evolucoes.WithOwner().HasForeignKey("AtendimentoId");
        evolucoes.HasKey(evolucao => evolucao.Id);
        evolucoes.Property(evolucao => evolucao.Id)
            .HasConversion(id => id.Value, value => new EvolucaoSOAPId(value))
            .ValueGeneratedNever();
        evolucoes.Property(evolucao => evolucao.Subjetivo);
        evolucoes.Property(evolucao => evolucao.Objetivo);
        evolucoes.Property(evolucao => evolucao.Avaliacao);
        evolucoes.Property(evolucao => evolucao.Plano);
        evolucoes.Property(evolucao => evolucao.DataHora);
        evolucoes.Property(evolucao => evolucao.Assinada);
        evolucoes.Property(evolucao => evolucao.EhAdendo);
        evolucoes.Property(evolucao => evolucao.EvolucaoReferenciadaId);
    }

    private static void MapearPrescricoes(OwnedNavigationBuilder<AtendimentoRaiz, Prescricao> prescricoes)
    {
        prescricoes.ToTable("AtendimentosPrescricoes");
        prescricoes.WithOwner().HasForeignKey("AtendimentoId");
        prescricoes.HasKey(prescricao => prescricao.Id);
        prescricoes.Property(prescricao => prescricao.Id)
            .HasConversion(id => id.Value, value => new PrescricaoId(value))
            .ValueGeneratedNever();
        prescricoes.Property(prescricao => prescricao.Item).HasMaxLength(200);
        prescricoes.Property(prescricao => prescricao.Posologia).HasMaxLength(500);
        prescricoes.Property(prescricao => prescricao.DataHora);
    }

    private static void MapearSolicitacoesExame(OwnedNavigationBuilder<AtendimentoRaiz, SolicitacaoExame> solicitacoes)
    {
        solicitacoes.ToTable("AtendimentosSolicitacoesExame");
        solicitacoes.WithOwner().HasForeignKey("AtendimentoId");
        solicitacoes.HasKey(solicitacao => solicitacao.Id);
        solicitacoes.Property(solicitacao => solicitacao.Id)
            .HasConversion(id => id.Value, value => new SolicitacaoExameId(value))
            .ValueGeneratedNever();
        solicitacoes.Property(solicitacao => solicitacao.Procedimento).HasMaxLength(200);
        solicitacoes.Property(solicitacao => solicitacao.Justificativa).HasMaxLength(500);
        solicitacoes.Property(solicitacao => solicitacao.DataHora);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="SolicitacaoRegulacao"/>.</summary>
public sealed class SolicitacaoRegulacaoConfiguration : IEntityTypeConfiguration<SolicitacaoRegulacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SolicitacaoRegulacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SolicitacoesRegulacao");
        builder.HasKey(solicitacao => solicitacao.Id);
        builder.Property(solicitacao => solicitacao.Id)
            .HasConversion(id => id.Value, value => new SolicitacaoRegulacaoId(value))
            .ValueGeneratedNever();

        builder.Property(solicitacao => solicitacao.PacienteId)
            .HasConversion(id => id.Value, value => new PacienteId(value));
        builder.Property(solicitacao => solicitacao.EstabelecimentoSolicitanteId)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value));
        builder.Property(solicitacao => solicitacao.ProfissionalSolicitanteId)
            .HasConversion(id => id.Value, value => new ProfissionalId(value));

        builder.Property(solicitacao => solicitacao.Justificativa).HasMaxLength(1000);
        builder.Property(solicitacao => solicitacao.DataSolicitacao);
        builder.Property(solicitacao => solicitacao.DataAutorizacao);
        builder.Property(solicitacao => solicitacao.ProtocoloSisreg).HasMaxLength(60);

        builder.Property(solicitacao => solicitacao.Prioridade).HasConversion<string>().HasMaxLength(20);
        builder.Property(solicitacao => solicitacao.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(solicitacao => solicitacao.Procedimento, MapearProcedimento);
        builder.ComplexProperty(solicitacao => solicitacao.Cota, MapearCota);

        builder.HasIndex(solicitacao => new { solicitacao.TenantId, solicitacao.Situacao });
        builder.HasIndex(solicitacao => new { solicitacao.TenantId, solicitacao.PacienteId });
    }

    private static void MapearProcedimento(ComplexPropertyBuilder<Procedimento> procedimento)
    {
        procedimento.Property(dados => dados.CodigoSigtap).HasColumnName("ProcedimentoCodigoSigtap").HasMaxLength(20);
        procedimento.Property(dados => dados.Descricao).HasColumnName("ProcedimentoDescricao").HasMaxLength(200);
    }

    private static void MapearCota(ComplexPropertyBuilder<Cota> cota)
    {
        cota.Property(dados => dados.Disponivel).HasColumnName("CotaDisponivel");
        cota.Property(dados => dados.Total).HasColumnName("CotaTotal");
    }
}

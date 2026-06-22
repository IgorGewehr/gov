using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="DeclaracaoFiscal"/> e de suas entidades/objetos de valor filhos.</summary>
public sealed class DeclaracaoFiscalConfiguration : IEntityTypeConfiguration<DeclaracaoFiscal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeclaracaoFiscal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DeclaracoesFiscais");
        builder.HasKey(declaracao => declaracao.Id);
        builder.Property(declaracao => declaracao.Id)
            .HasConversion(id => id.Value, value => new DeclaracaoFiscalId(value))
            .ValueGeneratedNever();

        builder.Property(declaracao => declaracao.TipoDeclaracao).HasConversion<string>().HasMaxLength(20);
        builder.Property(declaracao => declaracao.Exercicio);
        builder.Property(declaracao => declaracao.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(declaracao => declaracao.DataLimite);
        builder.Property(declaracao => declaracao.DataConsolidacao);
        builder.Property(declaracao => declaracao.DataTransmissao);
        builder.Property(declaracao => declaracao.ProtocoloSiconfi).HasMaxLength(60);
        builder.Property(declaracao => declaracao.MotivoRejeicao).HasMaxLength(1000);

        // Periodos (objetos de valor opcionais conforme o tipo) embutidos na linha.
        builder.OwnsOne(declaracao => declaracao.Competencia, competencia =>
        {
            competencia.Property(item => item.Ano).HasColumnName("CompetenciaAno");
            competencia.Property(item => item.Mes).HasColumnName("CompetenciaMes");
        });

        builder.OwnsOne(declaracao => declaracao.Bimestre, bimestre =>
        {
            bimestre.Property(item => item.Ano).HasColumnName("BimestreAno");
            bimestre.Property(item => item.Numero).HasColumnName("BimestreNumero");
        });

        builder.OwnsOne(declaracao => declaracao.Quadrimestre, quadrimestre =>
        {
            quadrimestre.Property(item => item.Ano).HasColumnName("QuadrimestreAno");
            quadrimestre.Property(item => item.Numero).HasColumnName("QuadrimestreNumero");
        });

        // Matriz de Saldos Contabeis (entidade-filha) com suas linhas.
        builder.OwnsOne(declaracao => declaracao.Matriz, MapearMatriz);
        builder.Navigation(declaracao => declaracao.Matriz).IsRequired();

        builder.HasIndex(declaracao => new { declaracao.TenantId, declaracao.Exercicio, declaracao.TipoDeclaracao });
    }

    private static void MapearMatriz(OwnedNavigationBuilder<DeclaracaoFiscal, MatrizSaldos> matriz)
    {
        matriz.ToTable("DeclaracoesFiscaisMatrizes");
        matriz.WithOwner().HasForeignKey("DeclaracaoFiscalId");
        matriz.HasKey(item => item.Id);
        matriz.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new MatrizSaldosId(value))
            .ValueGeneratedNever();

        // Totais e balanceamento sao calculados (sem coluna).
        matriz.Ignore(item => item.TotalDebitos);
        matriz.Ignore(item => item.TotalCreditos);
        matriz.Ignore(item => item.EstaBalanceada);

        matriz.OwnsMany(item => item.Linhas, MapearLinhas);
        matriz.Navigation(item => item.Linhas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapearLinhas(OwnedNavigationBuilder<MatrizSaldos, LinhaContabil> linhas)
    {
        linhas.ToTable("DeclaracoesFiscaisLinhas");
        linhas.WithOwner().HasForeignKey("MatrizSaldosId");
        linhas.HasKey(item => item.Id);
        linhas.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new LinhaContabilId(value))
            .ValueGeneratedNever();
        linhas.Property(item => item.ContaPcasp).HasMaxLength(30);
        linhas.Property(item => item.NaturezaSaldo).HasConversion<string>().HasMaxLength(20);
        linhas.Property(item => item.InformacaoComplementar).HasMaxLength(500);

        linhas.Property(item => item.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnName("Valor")
            .HasColumnType("decimal(18,2)");
    }
}

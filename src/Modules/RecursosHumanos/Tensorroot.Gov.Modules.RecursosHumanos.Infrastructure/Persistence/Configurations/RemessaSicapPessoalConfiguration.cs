using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="RemessaSicapPessoal"/> e dos atos (entidade filha).</summary>
public sealed class RemessaSicapPessoalConfiguration : IEntityTypeConfiguration<RemessaSicapPessoal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RemessaSicapPessoal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RemessasSicapPessoal");
        builder.HasKey(remessa => remessa.Id);
        builder.Property(remessa => remessa.Id)
            .HasConversion(id => id.Value, value => new RemessaSicapPessoalId(value))
            .ValueGeneratedNever();

        builder.Property(remessa => remessa.CodigoOrgao);
        builder.Property(remessa => remessa.SequencialLote);
        builder.Property(remessa => remessa.DataGeracaoLote);
        builder.Property(remessa => remessa.VersaoLeiaute);
        builder.Property(remessa => remessa.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(remessa => remessa.GeradaEm);
        builder.Property(remessa => remessa.ProtocoloTransmissao).HasMaxLength(100);
        builder.Ignore(remessa => remessa.QuantidadeAtos);

        // Sequencia de lote unica por orgao/tenant (NRO_MOV).
        builder.HasIndex(remessa => new { remessa.TenantId, remessa.CodigoOrgao, remessa.SequencialLote }).IsUnique();
        builder.HasIndex(remessa => new { remessa.TenantId, remessa.Situacao });

        builder.OwnsMany(remessa => remessa.Atos, ato =>
        {
            ato.ToTable("AtosAdmissaoSicap");
            ato.WithOwner().HasForeignKey("RemessaSicapPessoalId");
            ato.HasKey(a => a.Id);
            ato.Property(a => a.Id)
                .HasConversion(id => id.Value, value => new AtoAdmissaoSicapId(value))
                .ValueGeneratedNever();

            ato.Property(a => a.ServidorId)
                .HasConversion(
                    servidor => servidor!.Value.Value,
                    valor => new ServidorId(valor));

            ato.Property(a => a.IdentificadorAto).HasMaxLength(AtoAdmissaoSicap.ComprimentoMaximoIdentificador);
            ato.Property(a => a.TipoAto).HasConversion<string>().HasMaxLength(40);
            ato.Property(a => a.Regime).HasConversion<string>().HasMaxLength(20);
            ato.Property(a => a.Cpf).HasMaxLength(15);
            ato.Property(a => a.Nome).HasMaxLength(70);
            ato.Property(a => a.DataNascimento);
            ato.Property(a => a.DescricaoCargo).HasMaxLength(AtoAdmissaoSicap.ComprimentoMaximoCargo);
            ato.Property(a => a.CargaHorariaSemanal);
            ato.Property(a => a.ClassificacaoConcurso);
            ato.Property(a => a.DataAto);
            ato.Property(a => a.DataHistorica);
            ato.Property(a => a.DataTermino);
            ato.Property(a => a.MotivoExtincao).HasConversion<string>().HasMaxLength(40);
        });

        builder.Navigation(remessa => remessa.Atos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

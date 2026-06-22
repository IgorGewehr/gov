using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Empenho"/> (modelo de saldo).</summary>
public sealed class EmpenhoConfiguration : IEntityTypeConfiguration<Empenho>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Empenho> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Empenhos");
        builder.HasKey(empenho => empenho.Id);
        builder.Property(empenho => empenho.Id)
            .HasConversion(id => id.Value, value => new EmpenhoId(value))
            .ValueGeneratedNever();

        builder.Property(empenho => empenho.Numero).HasMaxLength(30).IsRequired();
        builder.Property(empenho => empenho.DotacaoId)
            .HasConversion(id => id.Value, value => new DotacaoOrcamentariaId(value));

        builder.OwnsOne(empenho => empenho.Credor, credor =>
        {
            credor.Property(c => c.Nome).HasColumnName("CredorNome").HasMaxLength(200).IsRequired();
            credor.Property(c => c.Tipo).HasColumnName("CredorTipo").HasConversion<string>().HasMaxLength(20).IsRequired();
            credor.Property(c => c.Documento).HasColumnName("CredorDocumento").HasMaxLength(20).IsRequired();
        });
        builder.Navigation(empenho => empenho.Credor).IsRequired();

        builder.Property(empenho => empenho.ValorEmpenhado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(empenho => empenho.ValorAnulado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(empenho => empenho.ValorLiquidado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(empenho => empenho.ValorPago)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(empenho => empenho.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(empenho => empenho.Situacao).HasConversion<string>().HasMaxLength(30);

        // TODO(revisao-contabil): habilitar token de concorrencia otimista (rowversion/xmin) nas raizes
        // de saldo sob SqlServer para impedir corrida de empenhos no mesmo saldo. Omitido aqui porque o
        // provider de desenvolvimento (SQLite, arquivo compartilhado via EnsureCreated) nao popula
        // rowversion automaticamente, o que quebraria updates. Definir por provider na fase de hardening.

        // Derivados não persistidos.
        builder.Ignore(empenho => empenho.SaldoEmpenhado);
        builder.Ignore(empenho => empenho.SaldoALiquidar);
        builder.Ignore(empenho => empenho.SaldoAPagar);

        builder.HasIndex(empenho => new { empenho.TenantId, empenho.Exercicio, empenho.Numero }).IsUnique();
        builder.HasIndex(empenho => empenho.DotacaoId);
    }
}

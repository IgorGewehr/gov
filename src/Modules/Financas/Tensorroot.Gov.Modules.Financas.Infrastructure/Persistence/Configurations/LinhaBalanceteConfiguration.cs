using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core da projeção <see cref="LinhaBalancete"/> (read model do balancete).</summary>
public sealed class LinhaBalanceteConfiguration : IEntityTypeConfiguration<LinhaBalancete>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LinhaBalancete> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("balancete_conta");
        builder.HasKey(linha => new { linha.TenantId, linha.ContaId, linha.Exercicio, linha.PeriodoMes });

        builder.Property(linha => linha.CodigoConta).HasMaxLength(30).IsRequired();
        builder.Property(linha => linha.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(linha => linha.NaturezaSaldo).HasConversion<string>().HasMaxLength(20);
        builder.Property(linha => linha.NaturezaInformacao).HasConversion<string>().HasMaxLength(20);

        builder.Property(linha => linha.SaldoAnterior).HasColumnType("decimal(18,2)");
        builder.Property(linha => linha.TotalDebitos).HasColumnType("decimal(18,2)");
        builder.Property(linha => linha.TotalCreditos).HasColumnType("decimal(18,2)");
        builder.Property(linha => linha.SaldoAtual).HasColumnType("decimal(18,2)");

        builder.HasIndex(linha => new { linha.TenantId, linha.Exercicio, linha.PeriodoMes });
    }
}

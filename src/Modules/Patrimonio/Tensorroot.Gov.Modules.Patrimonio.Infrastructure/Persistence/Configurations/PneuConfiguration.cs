using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Pneu"/> (item de frota controlado individualmente).</summary>
public sealed class PneuConfiguration : IEntityTypeConfiguration<Pneu>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Pneu> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Pneus");
        builder.HasKey(pneu => pneu.Id);
        builder.Property(pneu => pneu.Id)
            .HasConversion(id => id.Value, value => new PneuId(value))
            .ValueGeneratedNever();

        builder.Property(pneu => pneu.NumeroFogo).HasMaxLength(40);
        builder.Property(pneu => pneu.Marca).HasMaxLength(60);
        builder.Property(pneu => pneu.Modelo).HasMaxLength(80);
        builder.Property(pneu => pneu.Medida).HasMaxLength(30);
        builder.Property(pneu => pneu.Dot).HasMaxLength(8);
        builder.Property(pneu => pneu.Situacao).HasConversion<string>().HasMaxLength(20);

        // Sulco (VO readonly record struct) -> decimal(4,1); o piso legal é parametrizável (não fixado).
        builder.Property(pneu => pneu.SulcoNovo)
            .HasConversion(sulco => sulco.Milimetros, valor => Sulco.De(valor))
            .HasColumnType("decimal(4,1)");
        builder.Property(pneu => pneu.SulcoAtual)
            .HasConversion(sulco => sulco.Milimetros, valor => Sulco.De(valor))
            .HasColumnType("decimal(4,1)");

        builder.Property(pneu => pneu.ValorAquisicao)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(pneu => pneu.CustoRecapagens)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(pneu => pneu.VeiculoAtualId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (VeiculoId?)null : new VeiculoId(value.Value));

        // OdometroInstalacao (int?) é coluna simples.

        // Posição de montagem (eixo/lado) -> string reversível "eixo:lado" (códigos numéricos dos enums),
        // permitindo a comparação de igualdade de slot (unicidade de posição) na consulta. Nula = não instalado.
        builder.Property(pneu => pneu.PosicaoAtual)
            .HasConversion(ConversorPosicao)
            .HasMaxLength(8);

        // Propriedades calculadas (sem coluna).
        builder.Ignore(pneu => pneu.EstaInstalado);
        builder.Ignore(pneu => pneu.EstaDescartado);
        builder.Ignore(pneu => pneu.CustoTotal);
        builder.Ignore(pneu => pneu.CustoPorKm);
        builder.Ignore(pneu => pneu.PercentualBandaRemanescente);

        // NumeroFogo é único por tenant (chave operacional de rastreio do pneu individual).
        builder.HasIndex(pneu => new { pneu.TenantId, pneu.NumeroFogo }).IsUnique();
        builder.HasIndex("TenantId", "VeiculoAtualId");
    }

    // Conversor da posição nullable: persiste "{eixo}:{lado}" (inteiros dos enums); null -> coluna nula.
    // As duas direções delegam a métodos estáticos (apenas chamadas de método na árvore de expressão).
    private static readonly ValueConverter<PosicaoPneu?, string?> ConversorPosicao = new(
        posicao => ConverterParaTexto(posicao),
        texto => ConverterDeTexto(texto));

    private static string? ConverterParaTexto(PosicaoPneu? posicao)
    {
        if (posicao is not { } valor)
        {
            return null;
        }

        return ((int)valor.Eixo).ToString(CultureInfo.InvariantCulture)
            + ":"
            + ((int)valor.Lado).ToString(CultureInfo.InvariantCulture);
    }

    private static PosicaoPneu? ConverterDeTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var partes = texto.Split(':', 2);
        var eixo = (Eixo)int.Parse(partes[0], CultureInfo.InvariantCulture);
        var lado = (LadoMontagem)int.Parse(partes[1], CultureInfo.InvariantCulture);
        return PosicaoPneu.De(eixo, lado);
    }
}

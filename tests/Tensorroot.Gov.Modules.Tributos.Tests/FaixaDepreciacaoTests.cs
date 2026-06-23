using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do interpretador de chaves de faixa de depreciação por idade (IP-R4): casamento por
/// INTERVALO, faixa aberta no topo, idade única e rejeição de chaves malformadas.
/// </summary>
public sealed class FaixaDepreciacaoTests
{
    [Theory]
    [InlineData("0-5", 0, 5)]
    [InlineData("6-10", 6, 10)]
    [InlineData(" 11 - 20 ", 11, 20)] // tolera espaços
    [InlineData("7", 7, 7)]           // idade única
    public void Interpreta_intervalos_fechados_e_idade_unica(string chave, int minEsperado, int maxEsperado)
    {
        var ok = FaixaDepreciacao.TentarInterpretar(chave, out var minimo, out var maximo);

        ok.Should().BeTrue();
        minimo.Should().Be(minEsperado);
        maximo.Should().Be(maxEsperado);
    }

    [Fact]
    public void Interpreta_faixa_aberta_no_topo()
    {
        var ok = FaixaDepreciacao.TentarInterpretar("31+", out var minimo, out var maximo);

        ok.Should().BeTrue();
        minimo.Should().Be(31);
        maximo.Should().Be(int.MaxValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("10-5")]   // max < min
    [InlineData("-3")]     // negativo
    [InlineData("5-")      // hífen sem máximo
    ]
    [InlineData("1-2-3")]  // formato inválido
    public void Rejeita_chaves_malformadas_sem_lancar(string? chave)
    {
        var ok = FaixaDepreciacao.TentarInterpretar(chave, out _, out _);

        ok.Should().BeFalse();
    }
}

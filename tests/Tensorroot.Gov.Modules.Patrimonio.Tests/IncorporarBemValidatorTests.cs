using FluentAssertions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Regressao do validador de <see cref="IncorporarBemCommand"/>: o campo <c>Tipo</c> e transportado como
/// <c>int</c> no comando (DTO da API). O <c>FluentValidation.IsInEnum()</c> so valida quando a propriedade
/// JA e do tipo enum — em <c>int</c> ele falha SEMPRE, o que tornava o endpoint <c>POST /api/patrimonio/bens</c>
/// inutilizavel por HTTP (400 mesmo com Tipo valido). O validador passou a usar <c>Enum.IsDefined(TipoBem)</c>.
/// </summary>
public sealed class IncorporarBemValidatorTests
{
    private static IncorporarBemCommand Comando(int tipo)
        => new("Computador", tipo, 4000m, 400m, 60, new DateOnly(2025, 1, 10), "Compra NE 1/2025");

    [Theory]
    [InlineData(1)] // Movel
    [InlineData(2)] // Imovel
    public void Tipo_valido_de_TipoBem_deve_passar(int tipo)
    {
        var resultado = new IncorporarBemValidator().Validate(Comando(tipo));

        resultado.IsValid.Should().BeTrue();
        ((TipoBem)tipo).Should().BeDefined();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Tipo_fora_de_TipoBem_deve_reprovar(int tipo)
    {
        var resultado = new IncorporarBemValidator().Validate(Comando(tipo));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(IncorporarBemCommand.Tipo));
    }
}

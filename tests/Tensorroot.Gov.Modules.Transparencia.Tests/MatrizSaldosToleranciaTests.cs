using FluentAssertions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Regressão P2-1 (AUDITORIA-FINAL): <see cref="MatrizSaldos.EstaBalanceada"/> deve tolerar até R$ 0,01
/// de diferença por arredondamento (partida dobrada do MCASP), alinhada à <c>MatrizSaldosContabeis</c>
/// de Finanças (<c>ToleranciaFechamento=0,01</c>). Antes usava <c>Equals</c> exato e divergia do agregado
/// de Finanças — uma matriz com D=C a menos de um centavo era reprovada aqui e aprovada lá.
/// </summary>
public sealed class MatrizSaldosToleranciaTests
{
    private static MatrizSaldos Com(decimal debito, decimal credito)
        => MatrizSaldos.Montar(
        [
            LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(debito)),
            LinhaContabil.Criar("2.1.1.1.01.00", NaturezaSaldo.Credor, ValorMonetario.De(credito)),
        ]);

    [Fact] // D=C exato: balanceada (caso base).
    public void Debitos_iguais_a_creditos_e_balanceada()
        => Com(1_000m, 1_000m).EstaBalanceada.Should().BeTrue();

    [Fact] // Diferença de exatamente 1 centavo (arredondamento): ACEITA — o coração da correção P2-1.
    public void Diferenca_de_um_centavo_e_aceita()
    {
        Com(1_000.01m, 1_000m).EstaBalanceada.Should().BeTrue();
        Com(1_000m, 1_000.01m).EstaBalanceada.Should().BeTrue();
    }

    [Fact] // Diferença de 2 centavos: REPROVADA — a tolerância é de UM centavo, não folga arbitrária.
    public void Diferenca_de_dois_centavos_e_reprovada()
        => Com(1_000.02m, 1_000m).EstaBalanceada.Should().BeFalse();
}

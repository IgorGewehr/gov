using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Regressao P1-5 da AUDITORIA-FINAL: a prescricao de creditos do banco de horas e idempotente E NAO
/// DESTROI horas validas quando o job avanca o limite mes a mes. Como os lancamentos de credito originais
/// nunca sao removidos, sem descontar o ja-prescrito um mesmo credito vencido seria recontado a cada limite
/// posterior, prescrevendo a mais. Prova: so o vencido AINDA NAO prescrito sai, limitado ao saldo vigente.
/// </summary>
public sealed class BancoDeHorasPrescricaoTests
{
    private static readonly Guid Tenant = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static int MinutosPrescritos(BancoDeHoras banco)
        => banco.Lancamentos.Where(l => l.Tipo == TipoLancamentoBancoHoras.Prescricao).Sum(l => l.Minutos);

    [Fact] // Avancar o limite mes a mes NAO re-prescreve o credito ja prescrito (o bug original: recontava).
    public void Avancar_o_limite_nao_prescreve_o_mesmo_credito_duas_vezes()
    {
        var banco = BancoDeHoras.Abrir(Tenant, Guid.NewGuid());
        banco.Creditar(600, new DateOnly(2026, 1, 10), "ap:1", "Extra antigo");

        // 1a execucao (limite junho): o credito de janeiro esta vencido -> prescreve os 600, saldo zera.
        var primeira = banco.PrescreverCreditosAnterioresA(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1));
        primeira.Should().Be(600);
        banco.SaldoMinutos.Should().Be(0);

        // 2a execucao (limite julho, mais a frente): o MESMO credito segue < limite, mas ja foi prescrito.
        // Nao pode prescrever de novo (saldo ja zerado; ja-prescrito descontado) -> 0.
        var segunda = banco.PrescreverCreditosAnterioresA(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1));
        segunda.Should().Be(0);

        banco.SaldoMinutos.Should().Be(0);
        MinutosPrescritos(banco).Should().Be(600); // total prescrito = 600 (uma vez), nunca 1200.
    }

    [Fact] // Idempotente por limite: reexecutar o MESMO limite e no-op (a referencia carimba o limite).
    public void Reexecutar_o_mesmo_limite_e_idempotente()
    {
        var banco = BancoDeHoras.Abrir(Tenant, Guid.NewGuid());
        banco.Creditar(300, new DateOnly(2026, 1, 10), "ap:1", "Extra antigo");

        banco.PrescreverCreditosAnterioresA(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1)).Should().Be(300);
        banco.PrescreverCreditosAnterioresA(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2)).Should().Be(0);

        MinutosPrescritos(banco).Should().Be(300);
    }

    [Fact] // Credito novo (apos o limite) sobrevive: so o vencido E ainda-nao-prescrito sai; o resto fica intacto.
    public void Apenas_o_vencido_nao_prescrito_e_consumido_preservando_credito_vigente()
    {
        var banco = BancoDeHoras.Abrir(Tenant, Guid.NewGuid());
        banco.Creditar(400, new DateOnly(2026, 1, 10), "ap:1", "Extra antigo (vencera)");
        banco.Creditar(250, new DateOnly(2026, 9, 10), "ap:2", "Extra recente (dentro da janela)");

        // Limite junho: so o credito de janeiro (400) esta vencido. O de setembro e' vigente e nao prescreve.
        var prescrito = banco.PrescreverCreditosAnterioresA(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1));

        prescrito.Should().Be(400);
        banco.SaldoMinutos.Should().Be(250); // 400 + 250 - 400 prescrito = 250 (credito recente intacto).
    }
}

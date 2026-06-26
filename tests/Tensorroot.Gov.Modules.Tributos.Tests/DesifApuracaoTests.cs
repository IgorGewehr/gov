using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Desif;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova da apuração da DES-IF (Módulo 2 — Apuração Mensal do ISSQN, modelo conceitual ABRASF): o ISSQN
/// devido BRUTO é Σ(base × alíquota) por subtítulo COSIF (Registro 0430) e o ISSQN A RECOLHER é o bruto
/// menos as deduções/incentivos/depósitos judiciais (Registro 0440). Domínio puro/determinístico; sem
/// número mágico (alíquotas vêm da lei municipal por subtítulo). Fecha o gap de PARIDADE-PoC (SAPI).
/// </summary>
public sealed class DesifApuracaoTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static DeclaracaoDesif Abrir() =>
        DeclaracaoDesif.Abrir(Tenant, ContribuinteId.New(), Competencia.De(2026, 3), "CTM art. X — DES-IF mensal");

    [Fact] // Registro 0430: o ISSQN devido bruto é a soma de base × alíquota por subtítulo COSIF.
    public void Devido_bruto_e_soma_por_subtitulo_base_vezes_aliquota()
    {
        var desif = Abrir();

        // Subtítulo 1: receita de tarifas 100.000,00 × 5% = 5.000,00
        desif.EscriturarSubtitulo("7.1.7.99.00-8", "0101", "15.01", "Tarifas bancárias", ValorMonetario.De(100_000m), 5m);
        // Subtítulo 2: receita de cobrança 40.000,00 × 2,5% = 1.000,00
        desif.EscriturarSubtitulo("7.1.9.99.00-1", "0102", "15.10", "Cobrança", ValorMonetario.De(40_000m), 2.5m);

        desif.QuantidadeSubtitulos.Should().Be(2);
        desif.ReceitaTributavelTotal.Valor.Should().Be(140_000m);
        desif.IssqnDevidoBruto.Valor.Should().Be(6_000m); // 5.000 + 1.000
    }

    [Fact] // Registro 0440: ISSQN a recolher = devido bruto − (deduções + incentivos + depósitos judiciais).
    public void A_recolher_subtrai_deducoes_incentivos_e_depositos()
    {
        var desif = Abrir();
        desif.EscriturarSubtitulo("7.1.7.99.00-8", "0101", "15.01", "Tarifas", ValorMonetario.De(100_000m), 5m); // 5.000 bruto

        desif.Entregar(
            deducoesReceita: ValorMonetario.De(500m),
            incentivosFiscais: ValorMonetario.De(300m),
            depositosJudiciais: ValorMonetario.De(200m),
            dataEntrega: new DateOnly(2026, 4, 10));

        desif.Situacao.Should().Be(SituacaoDesif.Entregue);
        desif.IssqnARecolher.Valor.Should().Be(4_000m); // 5.000 − (500 + 300 + 200)
        desif.DataEntrega.Should().Be(new DateOnly(2026, 4, 10));
        desif.DomainEvents.Should().ContainItemsAssignableTo<DeclaracaoDesifEntregue>();
    }

    [Fact] // Fail-closed (CLAUDE.md §7): deduções não podem exceder o ISSQN devido bruto (sem crédito negativo).
    public void Deducoes_acima_do_devido_bruto_sao_rejeitadas()
    {
        var desif = Abrir();
        desif.EscriturarSubtitulo("7.1.7.99.00-8", "0101", "15.01", "Tarifas", ValorMonetario.De(10_000m), 5m); // 500 bruto

        var entregaInvalida = () => desif.Entregar(
            ValorMonetario.De(600m), ValorMonetario.Zero, ValorMonetario.Zero, new DateOnly(2026, 4, 10));

        entregaInvalida.Should().Throw<ArgumentOutOfRangeException>();
        desif.Situacao.Should().Be(SituacaoDesif.EmElaboracao); // permanece editável (não fechou)
    }

    [Fact] // Não se entrega uma DES-IF sem subtítulos escriturados.
    public void Entrega_sem_subtitulos_e_rejeitada()
    {
        var desif = Abrir();

        var entregaVazia = () => desif.Entregar(
            ValorMonetario.Zero, ValorMonetario.Zero, ValorMonetario.Zero, new DateOnly(2026, 4, 10));

        entregaVazia.Should().Throw<InvalidOperationException>();
    }
}

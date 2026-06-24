using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// F-4 (W10.6) — Cancelamento de Resto a Pagar passa a respeitar a janela de prazo/decadência
/// (Decreto 93.872/86 e normas TCE-RS), parametrizável por tenant — fail-closed. Antes o
/// <c>Cancelar</c> só validava o teto de saldo e admitia cancelamento a qualquer tempo.
/// </summary>
public sealed class RestoAPagarCancelamentoPrazoTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static RestoAPagar InscreverNaoProcessado(int exercicioInscricao, decimal valor = 1_000m)
        => RestoAPagar.Inscrever(
            Tenant,
            EmpenhoId.New(),
            ClassificacaoRestoAPagar.NaoProcessado,
            ValorMonetario.De(valor),
            exercicioOrigem: exercicioInscricao,
            exercicioInscricao: exercicioInscricao);

    [Fact]
    public void Cancela_dentro_da_janela_de_vigencia()
    {
        // Inscrito em 2025; validade de 1 exercício → vigência até 2026 (inclusive). Referência 2026 = OK.
        var resto = InscreverNaoProcessado(2025);
        var politica = PoliticaPrazoRestoAPagar.De(validadeExerciciosNaoProcessado: 1, validadeExerciciosProcessado: 1);

        resto.Cancelar(ValorMonetario.De(400m), new DateOnly(2026, 3, 1), politica);

        resto.SaldoAPagar.Valor.Should().Be(600m);
        resto.Situacao.Should().Be(SituacaoRestoAPagar.Inscrito, "cancelamento parcial mantém saldo remanescente");
    }

    [Fact]
    public void Recusa_cancelamento_apos_decadencia_fail_closed()
    {
        // Inscrito em 2025; validade de 1 exercício → limite 2026. Referência 2027 está fora da janela.
        var resto = InscreverNaoProcessado(2025);
        var politica = PoliticaPrazoRestoAPagar.De(validadeExerciciosNaoProcessado: 1, validadeExerciciosProcessado: 1);

        var acao = () => resto.Cancelar(ValorMonetario.De(400m), new DateOnly(2027, 1, 2), politica);

        acao.Should().Throw<PrazoCancelamentoRestoAPagarException>()
            .WithMessage("*fora da janela legal*");
        resto.SaldoAPagar.Valor.Should().Be(1_000m, "nada é cancelado quando o prazo decaiu");
    }

    [Fact]
    public void Recusa_cancelamento_antes_do_exercicio_de_inscricao()
    {
        // Data de referência anterior à inscrição é incoerente — fail-closed.
        var resto = InscreverNaoProcessado(2025);
        var politica = PoliticaPrazoRestoAPagar.De(validadeExerciciosNaoProcessado: 1, validadeExerciciosProcessado: 1);

        var acao = () => resto.Cancelar(ValorMonetario.De(400m), new DateOnly(2024, 12, 31), politica);

        acao.Should().Throw<PrazoCancelamentoRestoAPagarException>();
    }

    [Fact]
    public void Politica_e_parametrizavel_janela_maior_admite_cancelamento_posterior()
    {
        // Tenant configura 3 exercícios de validade para Não Processado → limite 2028; referência 2028 = OK.
        var resto = InscreverNaoProcessado(2025);
        var politica = PoliticaPrazoRestoAPagar.De(validadeExerciciosNaoProcessado: 3, validadeExerciciosProcessado: 3);

        resto.Cancelar(ValorMonetario.De(1_000m), new DateOnly(2028, 6, 1), politica);

        resto.SaldoAPagar.Valor.Should().Be(0m);
        resto.Situacao.Should().Be(SituacaoRestoAPagar.Cancelado);
    }

    [Fact]
    public void Politica_negativa_e_rejeitada_no_dominio()
    {
        var acao = () => PoliticaPrazoRestoAPagar.De(validadeExerciciosNaoProcessado: -1, validadeExerciciosProcessado: 1);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}

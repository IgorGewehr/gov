using FluentAssertions;
using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;
using Xunit;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Testes-chave de dominio da Onda 3c-1 (Saude operacional): Farmacia (estoque de medicamento +
/// dispensacao com baixa FEFO e recusa por falta de saldo valido) e Imunizacao (registro de
/// aplicacao de dose + aprazamento da proxima dose na carteira). Dominio puro — sem persistencia.
/// </summary>
public sealed class FarmaciaImunizacaoDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateOnly Hoje = new(2026, 6, 23);

    private static EstoqueMedicamento NovoEstoque(out MedicamentoId medId, out EstabelecimentoId estabId)
    {
        medId = MedicamentoId.New();
        estabId = new EstabelecimentoId(Guid.NewGuid());
        return EstoqueMedicamento.Abrir(Tenant, estabId, medId, pontoDeRessuprimento: 10);
    }

    // ---------- FARMACIA ----------

    [Fact]
    public void Entrada_de_medicamento_aumenta_o_saldo_do_estoque()
    {
        var estoque = NovoEstoque(out _, out _);

        estoque.RegistrarEntrada("L001", Hoje.AddMonths(12), 100m, Hoje);

        estoque.Saldo.Should().Be(100m);
        estoque.SaldoValido(Hoje).Should().Be(100m);
        estoque.DomainEvents.Should().ContainSingle(e => e is EntradaMedicamentoRegistrada);
    }

    [Fact]
    public void Dispensar_ao_paciente_baixa_o_estoque_por_fefo()
    {
        var estoque = NovoEstoque(out _, out _);
        // Dois lotes; FEFO deve consumir primeiro o que vence antes.
        estoque.RegistrarEntrada("LOTE-VENCE-ANTES", Hoje.AddMonths(2), 30m, Hoje);
        estoque.RegistrarEntrada("LOTE-VENCE-DEPOIS", Hoje.AddMonths(10), 30m, Hoje);

        var baixas = estoque.Baixar(40m, Hoje);

        estoque.Saldo.Should().Be(20m, "60 em estoque menos 40 dispensados");
        baixas.Should().HaveCount(2);
        baixas[0].NumeroLote.Should().Be("LOTE-VENCE-ANTES");
        baixas[0].Quantidade.Should().Be(30m, "FEFO consome integralmente o lote que vence antes");
        baixas[1].NumeroLote.Should().Be("LOTE-VENCE-DEPOIS");
        baixas[1].Quantidade.Should().Be(10m);
    }

    [Fact]
    public void Dispensar_sem_saldo_suficiente_deve_recusar_e_nao_alterar_estoque()
    {
        var estoque = NovoEstoque(out _, out _);
        estoque.RegistrarEntrada("L001", Hoje.AddMonths(6), 5m, Hoje);

        var acao = () => estoque.Baixar(10m, Hoje);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*insuficiente*");
        estoque.Saldo.Should().Be(5m, "estoque permanece intacto quando a dispensacao e recusada");
    }

    [Fact]
    public void Lote_vencido_nao_e_usado_na_baixa_mesmo_com_saldo_nominal()
    {
        var estoque = NovoEstoque(out _, out _);
        // Entra valido, depois "viaja no tempo": na data de baixa o lote ja venceu.
        estoque.RegistrarEntrada("L-CURTO", Hoje.AddDays(5), 50m, Hoje);

        var depoisDoVencimento = Hoje.AddDays(10);
        estoque.SaldoValido(depoisDoVencimento).Should().Be(0m);
        var acao = () => estoque.Baixar(1m, depoisDoVencimento);
        acao.Should().Throw<InvalidOperationException>("nao ha saldo valido — lote vencido nao dispensa");
    }

    [Fact]
    public void Dispensacao_concluida_emite_evento_por_item_para_trilha_lgpd()
    {
        var estoque = NovoEstoque(out var medId, out var estabId);
        estoque.RegistrarEntrada("L001", Hoje.AddMonths(12), 100m, Hoje);
        var baixas = estoque.Baixar(2m, Hoje);

        var dispensacao = Dispensacao.Iniciar(
            Tenant, new PacienteId(Guid.NewGuid()), estabId, new ProfissionalId(Guid.NewGuid()), DateTimeOffset.UtcNow);
        dispensacao.AdicionarItem(medId, 2m, "1 cp 12/12h", baixas);
        dispensacao.Concluir();

        dispensacao.DomainEvents.Should().ContainSingle(e => e is MedicamentoDispensado);
    }

    // ---------- IMUNIZACAO ----------

    [Fact]
    public void Registrar_aplicacao_de_dose_grava_na_carteira_e_apraza_a_proxima()
    {
        var carteira = CarteiraVacinacao.Abrir(Tenant, new PacienteId(Guid.NewGuid()));
        // Esquema de 2 doses, intervalo de 60 dias.
        var hepatiteB = Imunobiologico.Cadastrar(Tenant, "Hepatite B", "HB", totalDoses: 2, intervaloDiasProximaDose: 60, doseUnica: false);

        var dose = carteira.RegistrarDose(hepatiteB, TipoDose.Primeira, numeroDose: 1, "LOTE-VAC-1", new ProfissionalId(Guid.NewGuid()), Hoje);

        carteira.Doses.Should().HaveCount(1);
        dose.NumeroDose.Should().Be(1);
        dose.ProximaDoseAprazada.Should().Be(Hoje.AddDays(60), "aprazamento da proxima dose = data + intervalo do esquema");
        carteira.DomainEvents.Should().ContainSingle(e => e is DoseAplicadaRegistrada);
    }

    [Fact]
    public void Ultima_dose_do_esquema_nao_apraza_proxima()
    {
        var carteira = CarteiraVacinacao.Abrir(Tenant, new PacienteId(Guid.NewGuid()));
        var hepatiteB = Imunobiologico.Cadastrar(Tenant, "Hepatite B", "HB", totalDoses: 2, intervaloDiasProximaDose: 60, doseUnica: false);
        var prof = new ProfissionalId(Guid.NewGuid());

        carteira.RegistrarDose(hepatiteB, TipoDose.Primeira, 1, "L1", prof, Hoje);
        var segunda = carteira.RegistrarDose(hepatiteB, TipoDose.Segunda, 2, "L2", prof, Hoje.AddDays(60));

        segunda.ProximaDoseAprazada.Should().BeNull("a ultima dose do esquema nao gera aprazamento");
        carteira.SituacaoPara(hepatiteB, Hoje.AddDays(60)).Should().Be(SituacaoVacinal.Completo);
    }

    [Fact]
    public void Aprazamento_vencido_aparece_na_busca_ativa()
    {
        var carteira = CarteiraVacinacao.Abrir(Tenant, new PacienteId(Guid.NewGuid()));
        var hepatiteB = Imunobiologico.Cadastrar(Tenant, "Hepatite B", "HB", totalDoses: 2, intervaloDiasProximaDose: 30, doseUnica: false);

        carteira.RegistrarDose(hepatiteB, TipoDose.Primeira, 1, "L1", new ProfissionalId(Guid.NewGuid()), Hoje);

        // Passados 45 dias, o aprazamento (Hoje+30) ja venceu e a 2a dose nao foi aplicada.
        var vencidos = carteira.AprazamentosVencidos(Hoje.AddDays(45));
        vencidos.Should().ContainSingle();
        carteira.SituacaoPara(hepatiteB, Hoje.AddDays(45)).Should().Be(SituacaoVacinal.Atrasado);
    }

    [Fact]
    public void Nao_registra_dose_alem_do_esquema()
    {
        var carteira = CarteiraVacinacao.Abrir(Tenant, new PacienteId(Guid.NewGuid()));
        var doseUnica = Imunobiologico.Cadastrar(Tenant, "Febre Amarela", "FA", totalDoses: 1, intervaloDiasProximaDose: 0, doseUnica: true);

        var acao = () => carteira.RegistrarDose(doseUnica, TipoDose.Segunda, 2, "L1", new ProfissionalId(Guid.NewGuid()), Hoje);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*excede o esquema*");
    }
}

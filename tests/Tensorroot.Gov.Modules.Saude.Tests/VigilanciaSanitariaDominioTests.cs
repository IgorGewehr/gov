using FluentAssertions;
using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Testes-chave de dominio da Onda 3c-2 (Saude — Vigilancia Sanitaria): o ciclo
/// <c>inspecao → pendencias → auto/intimacao → licenca sanitaria</c>. Cobre os invariantes
/// criticos (rules.md): resultado da inspecao DERIVADO dos itens (I-VISA-3), imutabilidade da
/// inspecao concluida (I-VISA-1), item nao conforme exige observacao (I-VISA-4), regra de multa
/// do auto (I-VISA-2), maquina de estado de prazos do auto (defesa/regularizacao/revelia) e a
/// emissao de licenca com validade. Dominio puro — sem persistencia.
/// </summary>
public sealed class VigilanciaSanitariaDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly Hoje = new(2026, 6, 23);
    private const string CnpjValido = "11.222.333/0001-81";

    private static EstabelecimentoFiscalizavel NovoEstabelecimento(GrauRiscoSanitario risco = GrauRiscoSanitario.Alto)
        => EstabelecimentoFiscalizavel.Cadastrar(
            Tenant,
            DocumentoResponsavel.Criar(CnpjValido),
            "Restaurante Bom Prato Ltda",
            RamoVisa.Alimentacao,
            risco,
            EnderecoVisa.Criar("Rua das Flores, 100", "Centro", "Maximiliano de Almeida", "RS", "99970-000"));

    private static Inspecao NovaInspecao(EstabelecimentoFiscalizavel estab)
        => Inspecao.Abrir(Tenant, estab.Id, Hoje, fiscalId: null, roteiro: "Roteiro Alimentacao RDC 216/2004");

    // ---------- INSPECAO: resultado DERIVADO dos itens (I-VISA-3) ----------

    [Fact]
    public void Inspecao_sem_item_nao_conforme_e_Aprovada()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Higienizacao de superficies", ConformidadeItem.Conforme, null);
        inspecao.RegistrarItem("Controle de pragas", ConformidadeItem.Conforme, null);

        var resultado = inspecao.Concluir(houveInfracaoGrave: false);

        resultado.Should().Be(ResultadoInspecao.Aprovado);
        inspecao.Situacao.Should().Be(SituacaoInspecao.Concluida);
        inspecao.QuantidadePendencias().Should().Be(0);
        inspecao.HabilitaLicenca().Should().BeTrue();
        inspecao.DomainEvents.Should().ContainSingle(e => e is InspecaoConcluida);
    }

    [Fact]
    public void Inspecao_com_pendencia_sanavel_resulta_AprovadoComPendencias_e_habilita_licenca()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Higienizacao de superficies", ConformidadeItem.Conforme, null);
        inspecao.RegistrarItem("Validade dos produtos", ConformidadeItem.NaoConforme, "2 itens com validade vencida na prateleira");

        var resultado = inspecao.Concluir(houveInfracaoGrave: false);

        resultado.Should().Be(ResultadoInspecao.AprovadoComPendencias, "ha pendencia sem infracao grave");
        inspecao.QuantidadePendencias().Should().Be(1);
        inspecao.HabilitaLicenca().Should().BeTrue("aprovado com pendencias sanaveis ainda habilita licenciamento");
    }

    [Fact]
    public void Inspecao_com_pendencia_e_infracao_grave_e_Reprovada_e_nao_habilita_licenca()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Ausencia de esgoto a ceu aberto", ConformidadeItem.NaoConforme, "Esgoto exposto na area de manipulacao");

        var resultado = inspecao.Concluir(houveInfracaoGrave: true);

        resultado.Should().Be(ResultadoInspecao.Reprovado, "pendencia + risco iminente eleva a reprovacao");
        inspecao.HabilitaLicenca().Should().BeFalse("inspecao reprovada nao habilita licenca");
    }

    [Fact]
    public void Item_nao_conforme_sem_observacao_e_recusado_I_VISA_4()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);

        var acao = () => inspecao.RegistrarItem("Controle de temperatura", ConformidadeItem.NaoConforme, observacao: "  ");

        acao.Should().Throw<ArgumentException>().WithMessage("*observacao*");
    }

    [Fact]
    public void Inspecao_concluida_e_imutavel_I_VISA_1()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Higienizacao", ConformidadeItem.Conforme, null);
        inspecao.Concluir(houveInfracaoGrave: false);

        var registrar = () => inspecao.RegistrarItem("Tardio", ConformidadeItem.Conforme, null);
        var concluir = () => inspecao.Concluir(false);
        var cancelar = () => inspecao.Cancelar("tentativa");

        registrar.Should().Throw<InvalidOperationException>("inspecao concluida nao aceita novos itens");
        concluir.Should().Throw<InvalidOperationException>("nao reconclui");
        cancelar.Should().Throw<InvalidOperationException>("concluida nao cancela");
    }

    [Fact]
    public void Inspecao_sem_itens_nao_pode_concluir()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);

        var acao = () => inspecao.Concluir(false);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*ao menos um item*");
    }

    // ---------- AUTO: pendencia → intimacao/infracao + prazos (I-VISA-2) ----------

    [Fact]
    public void Auto_de_intimacao_nao_admite_multa_e_infracao_exige_valor_I_VISA_2()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Validade", ConformidadeItem.NaoConforme, "produto vencido");
        inspecao.Concluir(false);

        var intimacaoComMulta = () => AutoVisa.Lavrar(
            Tenant, estab.Id, inspecao.Id, TipoAutoVisa.Intimacao, "AI-001", "Pendencias", Hoje, Hoje.AddDays(30), valorMulta: 100m);
        var infracaoSemMulta = () => AutoVisa.Lavrar(
            Tenant, estab.Id, inspecao.Id, TipoAutoVisa.Infracao, "AI-002", "Infracao sanitaria", Hoje, Hoje.AddDays(15), valorMulta: null);

        intimacaoComMulta.Should().Throw<InvalidOperationException>().WithMessage("*intimacao nao comporta multa*");
        infracaoSemMulta.Should().Throw<InvalidOperationException>().WithMessage("*exige valor de multa*");
    }

    [Fact]
    public void Auto_exige_prazo_posterior_a_lavratura()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Validade", ConformidadeItem.NaoConforme, "produto vencido");
        inspecao.Concluir(false);

        var acao = () => AutoVisa.Lavrar(
            Tenant, estab.Id, inspecao.Id, TipoAutoVisa.Intimacao, "AI-003", "Pendencias", Hoje, Hoje, valorMulta: null);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*posterior*");
    }

    [Fact]
    public void Intimacao_lavrada_a_partir_de_pendencia_pode_ser_regularizada_no_prazo()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Validade", ConformidadeItem.NaoConforme, "produto vencido");
        inspecao.Concluir(false);
        var auto = AutoVisa.Lavrar(
            Tenant, estab.Id, inspecao.Id, TipoAutoVisa.Intimacao, "AI-010", "Sanar pendencias em 30 dias", Hoje, Hoje.AddDays(30), valorMulta: null);

        auto.DomainEvents.Should().ContainSingle(e => e is AutoVisaLavrado);
        auto.Regularizar(Hoje.AddDays(20));

        auto.Situacao.Should().Be(SituacaoAutoVisa.Regularizado);
        auto.DomainEvents.Should().Contain(e => e is AutoVisaJulgado);
    }

    [Fact]
    public void Intimacao_nao_regularizada_no_prazo_vira_Indeferida_por_revelia()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Validade", ConformidadeItem.NaoConforme, "produto vencido");
        inspecao.Concluir(false);
        var auto = AutoVisa.Lavrar(
            Tenant, estab.Id, inspecao.Id, TipoAutoVisa.Intimacao, "AI-011", "Sanar pendencias", Hoje, Hoje.AddDays(30), valorMulta: null);

        auto.PrazoVencido(Hoje.AddDays(31)).Should().BeTrue();
        auto.EncerrarPorDecursoDePrazo(Hoje.AddDays(31));

        auto.Situacao.Should().Be(SituacaoAutoVisa.Indeferido, "decurso de prazo sem regularizacao mantem a penalidade");
        // Idempotente: rechamar nao altera nem reemite.
        auto.EncerrarPorDecursoDePrazo(Hoje.AddDays(40));
        auto.Situacao.Should().Be(SituacaoAutoVisa.Indeferido);
    }

    [Fact]
    public void Auto_de_infracao_com_defesa_pode_ser_deferido_cancelando_a_penalidade()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Estrutura", ConformidadeItem.NaoConforme, "parede mofada");
        inspecao.Concluir(houveInfracaoGrave: true);
        var auto = AutoVisa.Lavrar(
            Tenant, estab.Id, inspecao.Id, TipoAutoVisa.Infracao, "AI-020", "Art. 10 Lei 6.437/77", Hoje, Hoje.AddDays(15), valorMulta: 5000m);

        auto.ApresentarDefesa("Razoes de defesa", Hoje.AddDays(5));
        auto.Situacao.Should().Be(SituacaoAutoVisa.DefesaApresentada);

        auto.Julgar(deferir: true);
        auto.Situacao.Should().Be(SituacaoAutoVisa.Deferido, "defesa acolhida cancela a penalidade");
    }

    [Fact]
    public void Defesa_fora_do_prazo_e_recusada()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Estrutura", ConformidadeItem.NaoConforme, "parede mofada");
        inspecao.Concluir(houveInfracaoGrave: true);
        var auto = AutoVisa.Lavrar(
            Tenant, estab.Id, inspecao.Id, TipoAutoVisa.Infracao, "AI-021", "Infracao", Hoje, Hoje.AddDays(15), valorMulta: 5000m);

        var acao = () => auto.ApresentarDefesa("Tardia", Hoje.AddDays(16));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*expirado*");
    }

    // ---------- LICENCA: emissao com validade + renovacao + cassacao ----------

    [Fact]
    public void Licenca_emitida_nasce_vigente_com_validade_futura_e_emite_evento()
    {
        var estab = NovoEstabelecimento();
        var inspecao = NovaInspecao(estab);
        inspecao.RegistrarItem("Higienizacao", ConformidadeItem.Conforme, null);
        inspecao.Concluir(false);

        var validade = Hoje.AddYears(1);
        var licenca = LicencaSanitaria.Emitir(Tenant, estab.Id, "ALV-2026-0001", Hoje, validade, inspecao.Id);

        licenca.Situacao.Should().Be(SituacaoLicenca.Vigente);
        licenca.ValidadeAte.Should().Be(validade);
        licenca.EstaValida(Hoje).Should().BeTrue();
        licenca.EstaValida(validade.AddDays(1)).Should().BeFalse("apos a validade nao esta mais valida");
        licenca.DomainEvents.Should().ContainSingle(e => e is LicencaSanitariaEmitida);
    }

    [Fact]
    public void Licenca_com_validade_nao_posterior_a_emissao_e_recusada()
    {
        var estab = NovoEstabelecimento();

        var acao = () => LicencaSanitaria.Emitir(Tenant, estab.Id, "ALV-X", Hoje, Hoje, inspecaoId: null);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*posterior*");
    }

    [Fact]
    public void Licenca_vencida_por_decurso_e_renovada_gera_nova_vigencia()
    {
        var estab = NovoEstabelecimento();
        var licenca = LicencaSanitaria.Emitir(Tenant, estab.Id, "ALV-2025", Hoje.AddYears(-1), Hoje.AddDays(-1), inspecaoId: null);

        licenca.AvaliarVigencia(Hoje);
        licenca.Situacao.Should().Be(SituacaoLicenca.Vencida);

        var nova = licenca.Renovar("ALV-2026", Hoje, Hoje.AddYears(1), inspecaoId: null);
        nova.Situacao.Should().Be(SituacaoLicenca.Vigente);
        nova.ValidadeAte.Should().Be(Hoje.AddYears(1));
    }

    [Fact]
    public void Licenca_cassada_emite_evento_e_nao_pode_ser_renovada_I_VISA_7()
    {
        var estab = NovoEstabelecimento();
        var licenca = LicencaSanitaria.Emitir(Tenant, estab.Id, "ALV-2026", Hoje, Hoje.AddYears(1), inspecaoId: null);

        licenca.Cassar("Auto de penalidade indeferido — risco a saude publica");

        licenca.Situacao.Should().Be(SituacaoLicenca.Cassada);
        licenca.DomainEvents.Should().ContainSingle(e => e is LicencaSanitariaCassada);

        var renovar = () => licenca.Renovar("ALV-NOVA", Hoje, Hoje.AddYears(1), inspecaoId: null);
        renovar.Should().Throw<InvalidOperationException>().WithMessage("*cassada*");
    }

    // ---------- ESTABELECIMENTO: classificacao de risco + interdicao ----------

    [Fact]
    public void Estabelecimento_de_baixo_risco_dispensa_licenciamento_previo_Lei_13874()
    {
        var baixoRisco = NovoEstabelecimento(GrauRiscoSanitario.Baixo);
        baixoRisco.DispensaLicenciamentoPrevio().Should().BeTrue();

        var altoRisco = NovoEstabelecimento(GrauRiscoSanitario.Alto);
        altoRisco.DispensaLicenciamentoPrevio().Should().BeFalse("risco alto exige inspecao previa");
    }

    [Fact]
    public void Estabelecimento_interditado_nao_e_fiscalizavel_ate_levantar_a_interdicao()
    {
        var estab = NovoEstabelecimento();
        estab.Interditar("Risco iminente a saude — inspecao reprovada");

        estab.Situacao.Should().Be(SituacaoEstabelecimentoVisa.Interditado);
        estab.EstaFiscalizavel().Should().BeFalse();
        estab.DomainEvents.Should().ContainSingle(e => e is EstabelecimentoVisaInterditado);

        estab.LevantarInterdicao();
        estab.EstaFiscalizavel().Should().BeTrue();
    }
}

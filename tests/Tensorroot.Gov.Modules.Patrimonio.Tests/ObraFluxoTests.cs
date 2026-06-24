using FluentAssertions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Tempo;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Cobertura das invariantes do agregado <see cref="Obra"/> (W9.3): teto de medição (I-1), coerência
/// físico-financeira do cronograma (I-3), percentual físico derivado (I-6), medição numerada com lastro
/// de RDO (I-7/I-8), RDO único por dia (I-9), aprovação por fiscal designado (I-10), conclusão →
/// incorporação (I-12/I-13) e relógio do art. 94 §3 (I-14). Domínio puro, sem I/O.
/// </summary>
public sealed class ObraFluxoTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Contrato = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Fornecedor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Fiscal = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateOnly Assinatura = new(2026, 1, 5);

    private static Obra NovaObra(decimal valorContratado = 1_000_000m)
        => Obra.AbrirObra(
            Tenant,
            Contrato,
            Fornecedor,
            "Construcao de escola municipal",
            LocalizacaoObra.Criar("Maximiliano de Almeida", "RS"),
            RegimeExecucao.EmpreitadaPorPrecoGlobal,
            ValorMonetario.De(valorContratado),
            Assinatura);

    private static IReadOnlyList<EtapaCronograma> CronogramaCheio(decimal total)
    {
        var metade = ValorMonetario.De(total / 2m);
        return
        [
            EtapaCronograma.Criar(1, "Fundacao", 50m, metade, new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 31)),
            EtapaCronograma.Criar(2, "Estrutura", 50m, metade, new DateOnly(2026, 4, 1), new DateOnly(2026, 5, 31)),
        ];
    }

    private static Obra ObraEmExecucao(decimal valorContratado = 1_000_000m)
    {
        var obra = NovaObra(valorContratado);
        obra.DefinirCronograma(CronogramaCheio(valorContratado));
        obra.DesignarFiscal(Fiscal, Assinatura, "Portaria 1/2026");
        obra.EmitirOrdemInicio(new DateOnly(2026, 2, 1));
        return obra;
    }

    [Fact] // Abertura: nasce planejada, valor medido zero, evento ObraAberta.
    public void Abertura_nasce_planejada_e_emite_evento()
    {
        var obra = NovaObra();

        obra.Situacao.Should().Be(SituacaoObra.Planejada);
        obra.ValorMedidoAcumulado.Valor.Should().Be(0m);
        obra.PercentualFisicoAcumulado.Should().Be(0m);
        obra.DomainEvents.Should().ContainSingle(evento => evento is ObraAberta);
    }

    [Fact] // I-3: cronograma deve fechar 100% físico e o valor contratado.
    public void Invariante_3_cronograma_incoerente_e_rejeitado()
    {
        var obra = NovaObra(1_000_000m);
        var incoerente = new List<EtapaCronograma>
        {
            EtapaCronograma.Criar(1, "Unica", 80m, ValorMonetario.De(1_000_000m), new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1)),
        };

        var acao = () => obra.DefinirCronograma(incoerente);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9: RDO único por dia na mesma obra.
    public void Invariante_9_rdo_duplicado_no_dia_e_rejeitado()
    {
        var obra = ObraEmExecucao();
        var dia = new DateOnly(2026, 2, 2);
        obra.RegistrarRdo(dia, "Bom", 10, "Retroescavadeira", "Escavacao", Fornecedor);

        var acao = () => obra.RegistrarRdo(dia, "Bom", 12, "Caminhao", "Aterro", Fornecedor);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-10: medição só é aprovada pelo fiscal designado vigente.
    public void Invariante_10_aprovacao_por_fiscal_nao_designado_e_rejeitada()
    {
        var obra = ObraEmExecucao();
        var etapa = obra.Etapas.First();
        obra.RegistrarRdo(new DateOnly(2026, 2, 2), "Bom", 10, "Equip", "Atividade", Fornecedor);
        var medicaoId = obra.RegistrarMedicao(2026, 2, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 2),
            [new Obra.AvancoEtapa(etapa.Id, 100m, ValorMonetario.De(500_000m))]);

        var outroFiscal = Guid.NewGuid();
        var acao = () => obra.AprovarMedicao(medicaoId, outroFiscal, new DateOnly(2026, 2, 3));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8: medição sem RDO cobrindo o período não é aprovada.
    public void Invariante_8_medicao_sem_rdo_no_periodo_nao_aprova()
    {
        var obra = ObraEmExecucao();
        var etapa = obra.Etapas.First();
        // Período de 2 dias, RDO só no primeiro.
        obra.RegistrarRdo(new DateOnly(2026, 2, 2), "Bom", 10, "Equip", "Atividade", Fornecedor);
        var medicaoId = obra.RegistrarMedicao(2026, 2, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 3),
            [new Obra.AvancoEtapa(etapa.Id, 100m, ValorMonetario.De(500_000m))]);

        var acao = () => obra.AprovarMedicao(medicaoId, Fiscal, new DateOnly(2026, 2, 4));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-1 + I-6: medição aprovada compõe o acumulado e deriva o % físico; teto respeitado.
    public void Invariante_1_e_6_medicao_aprovada_compoe_acumulado_e_percentual()
    {
        var obra = ObraEmExecucao(1_000_000m);
        var etapa = obra.Etapas.First(); // peso 50%, previsto 500.000
        obra.RegistrarRdo(new DateOnly(2026, 2, 2), "Bom", 10, "Equip", "Fundacao", Fornecedor);
        var medicaoId = obra.RegistrarMedicao(2026, 2, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 2),
            [new Obra.AvancoEtapa(etapa.Id, 100m, ValorMonetario.De(500_000m))]);

        obra.AprovarMedicao(medicaoId, Fiscal, new DateOnly(2026, 2, 3));

        obra.ValorMedidoAcumulado.Valor.Should().Be(500_000m);
        // Etapa 100% executada com peso 50% -> 50% físico acumulado da obra (I-6 derivado).
        obra.PercentualFisicoAcumulado.Should().Be(50m);
        obra.DomainEvents.Should().Contain(evento => evento is MedicaoAprovada);
    }

    [Fact] // I-1: medição que excede o contratado é rejeitada (exige aditivo).
    public void Invariante_1_medicao_acima_do_contratado_e_rejeitada()
    {
        var obra = ObraEmExecucao(1_000_000m);
        var etapa = obra.Etapas.First();
        obra.RegistrarRdo(new DateOnly(2026, 2, 2), "Bom", 10, "Equip", "Fundacao", Fornecedor);
        // Valor da etapa acima do previsto da etapa (500.000) — barrado já no reconhecimento (I-5).
        var medicaoId = obra.RegistrarMedicao(2026, 2, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 2),
            [new Obra.AvancoEtapa(etapa.Id, 100m, ValorMonetario.De(600_000m))]);

        var acao = () => obra.AprovarMedicao(medicaoId, Fiscal, new DateOnly(2026, 2, 3));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-7b: períodos de medição não se sobrepõem.
    public void Invariante_7b_periodos_de_medicao_nao_se_sobrepoem()
    {
        var obra = ObraEmExecucao();
        var etapa = obra.Etapas.First();
        obra.RegistrarRdo(new DateOnly(2026, 2, 2), "Bom", 10, "Equip", "Atividade", Fornecedor);
        obra.RegistrarMedicao(2026, 2, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 10),
            [new Obra.AvancoEtapa(etapa.Id, 50m, ValorMonetario.De(100_000m))]);

        var acao = () => obra.RegistrarMedicao(2026, 2, new DateOnly(2026, 2, 5), new DateOnly(2026, 2, 15),
            [new Obra.AvancoEtapa(etapa.Id, 10m, ValorMonetario.De(50_000m))]);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-11: RDO bloqueado em obra paralisada; reinício destrava.
    public void Invariante_11_paralisacao_bloqueia_rdo_e_reinicio_destrava()
    {
        var obra = ObraEmExecucao();
        obra.Paralisar(MotivoParalisacao.Clima, new DateOnly(2026, 2, 10));

        var acaoParalisada = () => obra.RegistrarRdo(new DateOnly(2026, 2, 11), "Chuva", 0, "Nenhum", "Parada", Fornecedor);
        acaoParalisada.Should().Throw<InvalidOperationException>();

        obra.Reiniciar(new DateOnly(2026, 2, 15));
        obra.Situacao.Should().Be(SituacaoObra.EmExecucao);
    }

    [Fact] // I-12 + I-13: conclusão exige 100% físico; incorporação é única.
    public void Invariante_12_e_13_conclusao_exige_100_e_incorporacao_unica()
    {
        var obra = ObraEmExecucao(1_000_000m);
        var etapas = obra.Etapas.OrderBy(etapa => etapa.Ordem).ToList();

        obra.RegistrarRdo(new DateOnly(2026, 2, 2), "Bom", 10, "Equip", "Fundacao", Fornecedor);
        var m1 = obra.RegistrarMedicao(2026, 2, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 2),
            [new Obra.AvancoEtapa(etapas[0].Id, 100m, ValorMonetario.De(500_000m))]);
        obra.AprovarMedicao(m1, Fiscal, new DateOnly(2026, 2, 3));

        // Antes de concluir a 2ª etapa, a conclusão é barrada (I-12).
        var acaoPrematura = () => obra.Concluir(new DateOnly(2026, 6, 1));
        acaoPrematura.Should().Throw<InvalidOperationException>();

        obra.RegistrarRdo(new DateOnly(2026, 4, 2), "Bom", 10, "Equip", "Estrutura", Fornecedor);
        var m2 = obra.RegistrarMedicao(2026, 4, new DateOnly(2026, 4, 2), new DateOnly(2026, 4, 2),
            [new Obra.AvancoEtapa(etapas[1].Id, 100m, ValorMonetario.De(500_000m))]);
        obra.AprovarMedicao(m2, Fiscal, new DateOnly(2026, 4, 3));

        obra.PercentualFisicoAcumulado.Should().Be(100m);
        obra.Concluir(new DateOnly(2026, 6, 1));
        obra.Situacao.Should().Be(SituacaoObra.Concluida);

        var bemId = BemPatrimonialId.New();
        obra.Incorporar(bemId);
        obra.Situacao.Should().Be(SituacaoObra.Incorporada);
        obra.BemPatrimonialId.Should().Be(bemId);

        // I-13: reincorporar é no-op (idempotente) — não troca o bem.
        obra.Incorporar(BemPatrimonialId.New());
        obra.BemPatrimonialId.Should().Be(bemId);
    }

    [Fact] // I-2: aditivo reajusta o teto; teto abaixo do já medido é rejeitado.
    public void Invariante_2_aditivo_reajusta_teto()
    {
        var obra = ObraEmExecucao(1_000_000m);
        var etapa = obra.Etapas.First();
        obra.RegistrarRdo(new DateOnly(2026, 2, 2), "Bom", 10, "Equip", "Fundacao", Fornecedor);
        var medicaoId = obra.RegistrarMedicao(2026, 2, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 2),
            [new Obra.AvancoEtapa(etapa.Id, 100m, ValorMonetario.De(500_000m))]);
        obra.AprovarMedicao(medicaoId, Fiscal, new DateOnly(2026, 2, 3));

        obra.AplicarAditivoValor(ValorMonetario.De(1_200_000m));
        obra.ValorContratado.Valor.Should().Be(1_200_000m);

        var acao = () => obra.AplicarAditivoValor(ValorMonetario.De(400_000m));
        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-14: relógio do art. 94 §3 (25 d.u. após assinatura) via calendário transversal.
    public void Invariante_14_prazo_art94_assinatura_calculado()
    {
        var obra = NovaObra();
        var calendario = new CalendarioCorridoFake();
        var parametro = new PrazoArt94Parametro(25, UnidadePrazo.DiasUteis, "Lei 14.133/2021 art. 94 §3");

        var prazo = obra.CalcularPrazoArt94(TipoPrazoArt94.Assinatura25, parametro, calendario);

        prazo.Inicio.Should().Be(Assinatura);
        prazo.Quantidade.Should().Be(25);
        // Calendário fake "corrido" (todo dia é útil): 5/1 + 25 d.u. = 30/1.
        prazo.Vencimento.Should().Be(new DateOnly(2026, 1, 30));
    }

    // Calendário determinístico de teste: todos os dias são úteis (isola a regra de prazo do feriado real).
    private sealed class CalendarioCorridoFake : ICalendarioDiasUteis
    {
        public DateOnly AdicionarDiasUteis(DateOnly inicio, int diasUteis) => inicio.AddDays(diasUteis);

        public bool EhDiaUtil(DateOnly data) => true;

        public DateOnly ProximoDiaUtil(DateOnly data) => data;

        public int DiasUteisEntre(DateOnly a, DateOnly b) => b.DayNumber - a.DayNumber;
    }
}

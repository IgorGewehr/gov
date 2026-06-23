using System.Text.Json;
using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Regressao do VINCULO planejamento→execucao: os eventos de dominio do planejamento sao gravados
/// no Outbox (serializados) e re-hidratados (desserializados) pelo drain antes de chegarem ao
/// handler que faz a dotacao NASCER da LOA. Se algum value object embarcado no evento nao puder ser
/// desserializado pelo System.Text.Json (ex.: construtor privado sem [JsonConstructor]), a mensagem
/// vira poison/dead-letter e a dotacao NUNCA e gerada — exatamente o bug detectado no runtime.
/// Este teste reproduz o round-trip EXATO do Outbox (ConvertDomainEventsToOutboxInterceptor serializa
/// com evento.GetType(); OutboxPublisher desserializa via Type.GetType + JsonSerializer.Deserialize).
/// </summary>
public sealed class PlanejamentoOutboxRoundTripTests
{
    private static object RoundTrip(object evento)
    {
        // Espelha o interceptor: serializa pelo tipo concreto do evento.
        var json = JsonSerializer.Serialize(evento, evento.GetType());
        // Espelha o publisher: resolve o tipo pelo AssemblyQualifiedName e desserializa.
        var tipo = Type.GetType(evento.GetType().AssemblyQualifiedName!)!;
        return JsonSerializer.Deserialize(json, tipo)!;
    }

    [Fact]
    public void ItemLoaEntrouEmExecucao_sobrevive_ao_roundtrip_do_outbox()
    {
        var classificacao = ClassificacaoOrcamentaria.De(
            "02", "0201", "10.301.0001", CategoriaEconomica.DespesasCorrentes, "500");
        var original = new ItemLoaEntrouEmExecucao(
            LoaId.New(),
            ItemDespesaFixadaId.New(),
            2026,
            classificacao,
            ValorMonetario.De(500000m),
            AcaoPpaId.New());

        var rehidratado = (ItemLoaEntrouEmExecucao)RoundTrip(original);

        // O VO embarcado deve ser reconstruido fielmente (era ele que quebrava o drain).
        rehidratado.Should().Be(original);
        rehidratado.Classificacao.Should().Be(classificacao);
        rehidratado.Classificacao.ParaTexto().Should().Be(classificacao.ParaTexto());
        rehidratado.ValorFixado.Valor.Should().Be(500000m);
    }

    [Theory]
    [MemberData(nameof(TodosOsEventosDePlanejamento))]
    public void Evento_de_planejamento_sobrevive_ao_roundtrip_do_outbox(object evento)
    {
        var rehidratado = RoundTrip(evento);
        rehidratado.Should().Be(evento);
    }

    public static TheoryData<object> TodosOsEventosDePlanejamento()
    {
        var classificacao = ClassificacaoOrcamentaria.De(
            "02", "0201", "10.301.0001", CategoriaEconomica.DespesasCorrentes, "500");
        return new TheoryData<object>
        {
            new PpaVigente(PpaId.New(), 2026, 2029),
            new LdoVigente(Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo.LdoId.New(), 2026),
            new LoaAprovada(LoaId.New(), 2026),
            new ItemLoaEntrouEmExecucao(
                LoaId.New(), ItemDespesaFixadaId.New(), 2026, classificacao, ValorMonetario.De(123.45m), AcaoPpaId.New()),
            new CreditoAdicionalAberto(
                Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos.CreditoAdicionalId.New(), LoaId.New(), 50000m),
        };
    }
}

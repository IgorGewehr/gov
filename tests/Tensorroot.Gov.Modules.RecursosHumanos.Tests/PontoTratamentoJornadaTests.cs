using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura do PTRP (<see cref="TratamentoJornada"/>): pareamento entrada/saida, apuracao de horas
/// trabalhadas, horas extras e faltas/atrasos respeitando a tolerancia, e o saldo do banco de horas.
/// Servico de dominio PURO — testes deterministicos sem I/O.
/// </summary>
public sealed class PontoTratamentoJornadaTests
{
    private static readonly Guid Tenant = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Servidor = Guid.NewGuid();

    private static JornadaTrabalho Jornada8h(int tolerancia = 10)
        => JornadaTrabalho.Definir(Tenant, Servidor, cargaDiariaMinutos: 480, intervaloMinutos: 60, RegimeJornada.Celetista, new DateOnly(2026, 1, 1), tolerancia);

    private static InstanteMarcacao Em(int dia, int hora, int minuto, SentidoMarcacao sentido)
        => new(new DateTimeOffset(2026, 6, dia, hora, minuto, 0, TimeSpan.Zero), sentido);

    [Fact] // Jornada cumprida exatamente (8h): sem extra nem falta.
    public void Jornada_cumprida_sem_extra_nem_falta()
    {
        // 08:00->12:00 (240) + 13:00->17:00 (240) = 480 = carga.
        var marcacoes = new[]
        {
            Em(1, 8, 0, SentidoMarcacao.Entrada),
            Em(1, 12, 0, SentidoMarcacao.Saida),
            Em(1, 13, 0, SentidoMarcacao.Entrada),
            Em(1, 17, 0, SentidoMarcacao.Saida),
        };

        var resultado = TratamentoJornada.Apurar(marcacoes, Jornada8h());

        resultado.MinutosTrabalhados.Should().Be(480);
        resultado.MinutosExtras.Should().Be(0);
        resultado.MinutosFalta.Should().Be(0);
        resultado.SaldoBancoHorasMinutos.Should().Be(0);
    }

    [Fact] // 1h de hora extra alem da tolerancia.
    public void Hora_extra_apurada_alem_da_tolerancia()
    {
        // 08:00->12:00 (240) + 13:00->18:00 (300) = 540 = 480 + 60 extra.
        var marcacoes = new[]
        {
            Em(2, 8, 0, SentidoMarcacao.Entrada),
            Em(2, 12, 0, SentidoMarcacao.Saida),
            Em(2, 13, 0, SentidoMarcacao.Entrada),
            Em(2, 18, 0, SentidoMarcacao.Saida),
        };

        var resultado = TratamentoJornada.Apurar(marcacoes, Jornada8h());

        resultado.MinutosTrabalhados.Should().Be(540);
        resultado.MinutosExtras.Should().Be(60);
        resultado.MinutosFalta.Should().Be(0);
    }

    [Fact] // Atraso/falta de 30 min alem da tolerancia.
    public void Falta_atraso_apurada_alem_da_tolerancia()
    {
        // 08:30->12:00 (210) + 13:00->17:00 (240) = 450 = 480 - 30 falta.
        var marcacoes = new[]
        {
            Em(3, 8, 30, SentidoMarcacao.Entrada),
            Em(3, 12, 0, SentidoMarcacao.Saida),
            Em(3, 13, 0, SentidoMarcacao.Entrada),
            Em(3, 17, 0, SentidoMarcacao.Saida),
        };

        var resultado = TratamentoJornada.Apurar(marcacoes, Jornada8h());

        resultado.MinutosTrabalhados.Should().Be(450);
        resultado.MinutosFalta.Should().Be(30);
        resultado.MinutosExtras.Should().Be(0);
        resultado.SaldoBancoHorasMinutos.Should().Be(-30);
    }

    [Fact] // Diferenca dentro da tolerancia (10 min): nem extra nem falta.
    public void Diferenca_dentro_da_tolerancia_nao_gera_extra_nem_falta()
    {
        // 08:00->12:00 (240) + 13:00->17:08 (248) = 488 = 480 + 8 (<= 10 tolerancia).
        var marcacoes = new[]
        {
            Em(4, 8, 0, SentidoMarcacao.Entrada),
            Em(4, 12, 0, SentidoMarcacao.Saida),
            Em(4, 13, 0, SentidoMarcacao.Entrada),
            Em(4, 17, 8, SentidoMarcacao.Saida),
        };

        var resultado = TratamentoJornada.Apurar(marcacoes, Jornada8h());

        resultado.MinutosExtras.Should().Be(0);
        resultado.MinutosFalta.Should().Be(0);
    }

    [Fact] // Marcacao impar (entrada sem saida) e sinalizada para o espelho.
    public void Marcacao_impar_e_sinalizada()
    {
        var marcacoes = new[]
        {
            Em(5, 8, 0, SentidoMarcacao.Entrada),
            Em(5, 12, 0, SentidoMarcacao.Saida),
            Em(5, 13, 0, SentidoMarcacao.Entrada), // sem saida
        };

        var resultado = TratamentoJornada.Apurar(marcacoes, Jornada8h());

        resultado.Dias.Should().ContainSingle();
        resultado.Dias[0].MarcacaoImpar.Should().BeTrue();
    }

    [Fact] // Banco de horas acumula extras de varios dias menos faltas.
    public void Banco_de_horas_acumula_saldo_do_periodo()
    {
        var marcacoes = new[]
        {
            // Dia 1: +60 extra.
            Em(1, 8, 0, SentidoMarcacao.Entrada), Em(1, 12, 0, SentidoMarcacao.Saida),
            Em(1, 13, 0, SentidoMarcacao.Entrada), Em(1, 18, 0, SentidoMarcacao.Saida),
            // Dia 2: -30 falta.
            Em(2, 8, 30, SentidoMarcacao.Entrada), Em(2, 12, 0, SentidoMarcacao.Saida),
            Em(2, 13, 0, SentidoMarcacao.Entrada), Em(2, 17, 0, SentidoMarcacao.Saida),
        };

        var resultado = TratamentoJornada.Apurar(marcacoes, Jornada8h());

        resultado.MinutosExtras.Should().Be(60);
        resultado.MinutosFalta.Should().Be(30);
        resultado.SaldoBancoHorasMinutos.Should().Be(30);
    }
}

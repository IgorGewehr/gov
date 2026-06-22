using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Cobertura da reconciliação contra o SICONFI (API de Dados Abertos — SOMENTE consulta): detecta
/// divergências entre o valor calculado localmente e o publicado. NÃO é envio.
/// </summary>
public sealed class ReconciliacaoSiconfiTests
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static DeclaracaoFiscal DeclaracaoComContas(decimal devedor, decimal credor)
    {
        var matriz = MatrizSaldos.Montar(
        [
            LinhaContabil.Criar("111110100", NaturezaSaldo.Devedor, ValorMonetario.De(devedor)),
            LinhaContabil.Criar("211110000", NaturezaSaldo.Credor, ValorMonetario.De(credor)),
        ]);

        return DeclaracaoFiscal.ConsolidarMatriz(
            TenantA,
            TipoDeclaracaoFiscal.Msc,
            2026,
            Competencia.De(2026, 6),
            null,
            null,
            new DateOnly(2026, 7, 31),
            new DateOnly(2026, 7, 1),
            matriz);
    }

    [Fact] // O simulado publica 1.000,00 (bate) e 900,00 (diverge do credor 1.000,00) ⇒ 1 divergencia.
    public async Task Reconciliacao_detecta_divergencia()
    {
        var declaracao = DeclaracaoComContas(1_000m, 1_000m);
        var repo = new FakeDeclaracaoRepository(declaracao);
        var handler = new ReconciliarSiconfiHandler(
            repo,
            new SimuladoConsultaSiconfi(),
            NullLogger<ReconciliarSiconfiHandler>.Instance);

        var resultado = await handler.Handle(
            new ReconciliarSiconfiCommand(declaracao.Id.Value, "4312377"),
            CancellationToken.None);

        resultado.Conciliado.Should().BeFalse();
        resultado.Divergencias.Should().ContainSingle()
            .Which.Conta.Should().Be("211110000");
        resultado.Divergencias[0].ValorLocal.Should().Be(1_000m);
        resultado.Divergencias[0].ValorPublicado.Should().Be(900m);
        resultado.Divergencias[0].Diferenca.Should().Be(100m);
    }

    [Fact] // Quando os valores locais batem com o publicado, fica conciliado (sem divergencias).
    public async Task Reconciliacao_conciliada_quando_valores_batem()
    {
        // Matriz balanceada (1.000 devedor == 1.000 credor); consulta publica os mesmos valores.
        var declaracao = DeclaracaoComContas(1_000m, 1_000m);
        var repo = new FakeDeclaracaoRepository(declaracao);
        var consulta = new FakeConsultaSiconfi(
        [
            new ValorPublicadoSiconfi("111110100", "saldo", 1_000m),
            new ValorPublicadoSiconfi("211110000", "saldo", 1_000m),
        ]);
        var handler = new ReconciliarSiconfiHandler(repo, consulta, NullLogger<ReconciliarSiconfiHandler>.Instance);

        var resultado = await handler.Handle(
            new ReconciliarSiconfiCommand(declaracao.Id.Value, "4312377"),
            CancellationToken.None);

        resultado.Conciliado.Should().BeTrue();
        resultado.Divergencias.Should().BeEmpty();
        resultado.Indisponivel.Should().BeFalse();
    }

    [Fact] // API de Dados Abertos fora do ar (apos Polly) ⇒ DEGRADA GRACIOSAMENTE: Indisponivel, sem excecao.
    public async Task Reconciliacao_degrada_graciosamente_quando_siconfi_indisponivel()
    {
        var declaracao = DeclaracaoComContas(1_000m, 1_000m);
        var repo = new FakeDeclaracaoRepository(declaracao);
        var consulta = new ConsultaSiconfiIndisponivel();
        var handler = new ReconciliarSiconfiHandler(repo, consulta, NullLogger<ReconciliarSiconfiHandler>.Instance);

        var resultado = await handler.Handle(
            new ReconciliarSiconfiCommand(declaracao.Id.Value, "4312377"),
            CancellationToken.None);

        resultado.Indisponivel.Should().BeTrue();
        resultado.Conciliado.Should().BeFalse();
        resultado.Divergencias.Should().BeEmpty();
        resultado.Observacao.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class ConsultaSiconfiIndisponivel : IConsultaSiconfi
    {
        public Task<IReadOnlyList<ValorPublicadoSiconfi>> ConsultarValoresAsync(
            ConsultaSiconfiCriterio criterio, CancellationToken cancellationToken)
            => throw new HttpRequestException("Connection refused (simulado: SICONFI fora do ar)");

        public Task<IReadOnlyList<EntregaSiconfi>> ConsultarEntregasAsync(
            string idEnte, int exercicio, CancellationToken cancellationToken)
            => throw new HttpRequestException("Connection refused (simulado: SICONFI fora do ar)");
    }

    private sealed class FakeConsultaSiconfi(IReadOnlyList<ValorPublicadoSiconfi> valores) : IConsultaSiconfi
    {
        public Task<IReadOnlyList<ValorPublicadoSiconfi>> ConsultarValoresAsync(
            ConsultaSiconfiCriterio criterio, CancellationToken cancellationToken)
            => Task.FromResult(valores);

        public Task<IReadOnlyList<EntregaSiconfi>> ConsultarEntregasAsync(
            string idEnte, int exercicio, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<EntregaSiconfi>>([]);
    }

    private sealed class FakeDeclaracaoRepository(DeclaracaoFiscal declaracao) : IDeclaracaoFiscalRepository
    {
        public void Adicionar(DeclaracaoFiscal declaracaoFiscal)
        {
        }

        public Task<DeclaracaoFiscal?> ObterPorIdAsync(DeclaracaoFiscalId id, CancellationToken cancellationToken)
            => Task.FromResult<DeclaracaoFiscal?>(declaracao.Id == id ? declaracao : null);

        public Task<bool> ExisteVigenteAsync(
            TipoDeclaracaoFiscal tipo, int exercicio, int? mes, int? numeroBimestre, int? numeroQuadrimestre, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<IReadOnlyList<DeclaracaoFiscal>> ListarPorExercicioAsync(
            int exercicio, TipoDeclaracaoFiscal? tipo, SituacaoDeclaracaoFiscal? situacao, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DeclaracaoFiscal>>([declaracao]);
    }
}

using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura de integracao do ponto sobre SQLite em memoria: NSR sequencial e sem lacunas por tenant,
/// AFD append-only persistido, apuracao por servidor/competencia e ISOLAMENTO por tenant (Global Query
/// Filter). Reproduz a logica de NSR do handler (ultimo + 1) sobre o repositorio real.
/// </summary>
public sealed class PontoMarcacaoIntegracaoTests : RecursosHumanosTestBase
{
    private static readonly Cpf CpfServidor = Cpf.Create("39053344705");

    private static async Task<long> RegistrarProximaAsync(
        MarcacaoPontoRepository repo,
        Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.RecursosHumanosDbContext ctx,
        Guid tenantId,
        Guid servidorId,
        DateTimeOffset quando,
        SentidoMarcacao sentido)
    {
        var ultimo = await repo.ObterUltimoNsrAsync(default);
        var nsr = ultimo is { } v ? Nsr.De(v).Proximo() : Nsr.Primeiro();
        repo.Adicionar(MarcacaoPonto.Registrar(tenantId, servidorId, CpfServidor, nsr, quando, sentido, TipoRep.RepP));
        await ctx.SaveChangesAsync();
        return nsr.Valor;
    }

    [Fact] // NSR e sequencial e sem lacunas (1,2,3,...) por tenant.
    public async Task Nsr_e_sequencial_e_sem_lacunas()
    {
        await using var ctx = CriarContexto(TenantA);
        var repo = new MarcacaoPontoRepository(ctx);
        var servidor = Guid.NewGuid();

        var nsr1 = await RegistrarProximaAsync(repo, ctx, TenantA, servidor, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), SentidoMarcacao.Entrada);
        var nsr2 = await RegistrarProximaAsync(repo, ctx, TenantA, servidor, new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), SentidoMarcacao.Saida);
        var nsr3 = await RegistrarProximaAsync(repo, ctx, TenantA, servidor, new DateTimeOffset(2026, 6, 1, 13, 0, 0, TimeSpan.Zero), SentidoMarcacao.Entrada);

        nsr1.Should().Be(1);
        nsr2.Should().Be(2);
        nsr3.Should().Be(3);
    }

    [Fact] // O NSR e proprio de CADA tenant (isolamento): cada REP comeca em 1.
    public async Task Nsr_e_isolado_por_tenant()
    {
        var servidor = Guid.NewGuid();

        await using (var ctxA = CriarContexto(TenantA))
        {
            var repoA = new MarcacaoPontoRepository(ctxA);
            await RegistrarProximaAsync(repoA, ctxA, TenantA, servidor, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), SentidoMarcacao.Entrada);
            await RegistrarProximaAsync(repoA, ctxA, TenantA, servidor, new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), SentidoMarcacao.Saida);
        }

        await using var ctxB = CriarContexto(TenantB);
        var repoB = new MarcacaoPontoRepository(ctxB);

        // Tenant B nao enxerga as marcacoes de A (Global Query Filter) -> seu proximo NSR e 1.
        var ultimoB = await repoB.ObterUltimoNsrAsync(default);
        ultimoB.Should().BeNull();

        var nsrB = await RegistrarProximaAsync(repoB, ctxB, TenantB, servidor, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), SentidoMarcacao.Entrada);
        nsrB.Should().Be(1);
    }

    [Fact] // Marcacoes de um servidor numa competencia retornam ordenadas por data/hora.
    public async Task Lista_por_servidor_competencia_ordena_por_datahora()
    {
        await using var ctx = CriarContexto(TenantA);
        var repo = new MarcacaoPontoRepository(ctx);
        var servidor = Guid.NewGuid();

        await RegistrarProximaAsync(repo, ctx, TenantA, servidor, new DateTimeOffset(2026, 6, 10, 13, 0, 0, TimeSpan.Zero), SentidoMarcacao.Entrada);
        await RegistrarProximaAsync(repo, ctx, TenantA, servidor, new DateTimeOffset(2026, 6, 10, 8, 0, 0, TimeSpan.Zero), SentidoMarcacao.Entrada);

        var lista = await repo.ListarPorServidorCompetenciaAsync(servidor, Competencia.De(2026, 6), default);

        lista.Should().HaveCount(2);
        lista[0].DataHora.Hour.Should().Be(8);
        lista[1].DataHora.Hour.Should().Be(13);
    }

    [Fact] // Apuracao persiste e e isolada por tenant; saldo do banco de horas reflete extras-faltas.
    public async Task Apuracao_persiste_saldo_banco_de_horas()
    {
        await using var ctx = CriarContexto(TenantA);
        var repo = new ApuracaoPontoRepository(ctx);
        var servidor = Guid.NewGuid();

        var resultado = new ResultadoApuracaoCompetencia(
            MinutosTrabalhados: 9600, MinutosDevidos: 9600, MinutosExtras: 120, MinutosFalta: 30, Dias: []);
        var apuracao = ApuracaoPonto.Apurar(TenantA, servidor, Competencia.De(2026, 6), resultado, saldoBancoHorasAnteriorMinutos: 0);
        repo.Adicionar(apuracao);
        await ctx.SaveChangesAsync();

        var persistida = await repo.ObterPorServidorCompetenciaAsync(servidor, Competencia.De(2026, 6), default);

        persistida.Should().NotBeNull();
        persistida!.MinutosExtras.Should().Be(120);
        persistida.MinutosFalta.Should().Be(30);
        persistida.SaldoBancoHorasAtualMinutos.Should().Be(90);
    }

    [Fact] // O saldo anterior do banco de horas e herdado da competencia anterior do servidor.
    public async Task Saldo_anterior_herdado_da_competencia_anterior()
    {
        await using var ctx = CriarContexto(TenantA);
        var repo = new ApuracaoPontoRepository(ctx);
        var servidor = Guid.NewGuid();

        var maio = new ResultadoApuracaoCompetencia(9600, 9600, 100, 0, []);
        repo.Adicionar(ApuracaoPonto.Apurar(TenantA, servidor, Competencia.De(2026, 5), maio));
        await ctx.SaveChangesAsync();

        var saldoAnterior = await repo.ObterSaldoBancoHorasAnteriorAsync(servidor, Competencia.De(2026, 6), default);

        saldoAnterior.Should().Be(100);
    }
}

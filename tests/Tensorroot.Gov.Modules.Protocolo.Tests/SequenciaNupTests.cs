using FluentAssertions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo;
using Xunit;

namespace Tensorroot.Gov.Modules.Protocolo.Tests;

/// <summary>
/// Cobertura do sequencial atomico do NUP (Peca 3 / W9.4): contador SequenciaNup por (TenantId, Ano),
/// reset anual e isolamento por tenant. Em SQLite (testes) a escrita e serializada e o contador usa
/// Max+1; o indice unico (TenantId, Nup) e o backstop. Espelha SequenciaNup.rules.md.
/// </summary>
public sealed class SequenciaNupTests : ProtocoloTestBase
{
    private sealed class TimeProviderFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    [Fact] // C-2: primeiro NUP do ano cria o contador com sequencial 1.
    public async Task Primeiro_nup_do_ano_comeca_em_um()
    {
        await using var contexto = CriarContexto(TenantA);
        var gerador = new NupSequencialGenerator(
            contexto, new TenantContextFake(TenantA), new TimeProviderFixo(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)));

        var nup = await gerador.GerarAsync(CancellationToken.None);
        await contexto.SaveChangesAsync();

        nup.Valor.Should().StartWith("000001/2026-");
    }

    [Fact] // C-1: alocacoes sequenciais geram NUPs DISTINTOS (000001, 000002, 000003).
    public async Task Alocacoes_sequenciais_geram_nups_distintos()
    {
        await using var contexto = CriarContexto(TenantA);
        var tempo = new TimeProviderFixo(new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero));
        var gerador = new NupSequencialGenerator(contexto, new TenantContextFake(TenantA), tempo);

        var nups = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var nup = await gerador.GerarAsync(CancellationToken.None);
            await contexto.SaveChangesAsync();
            nups.Add(nup.Valor);
        }

        nups.Should().OnlyHaveUniqueItems();
        nups[0].Should().StartWith("000001/2026-");
        nups[1].Should().StartWith("000002/2026-");
        nups[2].Should().StartWith("000003/2026-");
    }

    [Fact] // C-3: reset anual — novo exercicio reinicia em 1 sem afetar o anterior.
    public async Task Reset_anual_reinicia_o_sequencial()
    {
        await using var contexto = CriarContexto(TenantA);
        var tenant = new TenantContextFake(TenantA);

        var em2025 = new NupSequencialGenerator(contexto, tenant, new TimeProviderFixo(new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero)));
        var nup2025 = await em2025.GerarAsync(CancellationToken.None);
        await contexto.SaveChangesAsync();

        var em2026 = new NupSequencialGenerator(contexto, tenant, new TimeProviderFixo(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)));
        var nup2026 = await em2026.GerarAsync(CancellationToken.None);
        await contexto.SaveChangesAsync();

        nup2025.Valor.Should().StartWith("000001/2025-");
        nup2026.Valor.Should().StartWith("000001/2026-");
    }

    [Fact] // C-4: 50 alocacoes do mesmo tenant x ano via contador atomico => zero colisao de NUP.
    public async Task Cinquenta_alocacoes_nunca_colidem()
    {
        await using var contexto = CriarContexto(TenantA);
        var tempo = new TimeProviderFixo(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
        var gerador = new NupSequencialGenerator(contexto, new TenantContextFake(TenantA), tempo);

        var nups = new List<string>();
        for (var i = 0; i < 50; i++)
        {
            var nup = await gerador.GerarAsync(CancellationToken.None);
            await contexto.SaveChangesAsync();
            nups.Add(nup.Valor);
        }

        nups.Should().OnlyHaveUniqueItems();
        nups.Should().HaveCount(50);
        nups[^1].Should().StartWith("000050/2026-");
    }

    [Fact] // C-5: backstop — o indice unico (TenantId, Nup) BARRA um NUP duplicado forcado (defesa final).
    public async Task Indice_unico_barra_nup_duplicado_forcado()
    {
        await using var contexto = CriarContexto(TenantA);
        var dataAutuacao = new DateOnly(2026, 8, 1);
        var nupColidente = new Nup("000001/2026-58");

        contexto.Processos.Add(Processo.Autuar(
            TenantA, nupColidente, new Classificacao("040"), NivelDeAcesso.Publico,
            requerimentoId: null, origemModulo: null, origemId: null, dataAutuacao));
        await contexto.SaveChangesAsync();

        // Segunda insercao com o MESMO NUP do mesmo tenant: o indice unico precisa explodir.
        contexto.Processos.Add(Processo.Autuar(
            TenantA, nupColidente, new Classificacao("040"), NivelDeAcesso.Publico,
            requerimentoId: null, origemModulo: null, origemId: null, dataAutuacao));

        var acao = async () => await contexto.SaveChangesAsync();
        await acao.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>();
    }
}

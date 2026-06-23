using FluentAssertions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Fiscal;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Testes de integração (SQLite) do read model de execução fiscal e da apuração ponta a ponta:
/// projeção de receita-base + despesas, classificação por função/fonte e isolamento por tenant.
/// </summary>
public sealed class NucleoFiscalReadModelTests : TransparenciaTestBase
{
    private const int Exercicio = 2026;

    [Fact]
    public async Task Apura_minimos_de_ponta_a_ponta_a_partir_das_linhas_de_execucao()
    {
        await using var contexto = CriarContexto(TenantA);
        var regras = new FonteRecursoVinculadoRepository(contexto);
        var linhas = new LinhaExecucaoFiscalRepository(contexto);

        // Regras de classificação do tenant.
        await regras.AdicionarAsync(FonteRecursoVinculado.Criar(TenantA, "10", SetorMinimo.Saude, new DateOnly(2026, 1, 1)), default);
        await regras.AdicionarAsync(FonteRecursoVinculado.Criar(TenantA, "12", SetorMinimo.Educacao, new DateOnly(2026, 1, 1)), default);

        // Execução: receita-base 1.000.000; Saúde 150.000 (15%); Educação 240.000 (24%); 50.000 fora (função 04).
        await linhas.AdicionarAsync(LinhaExecucaoFiscal.ReceitaBase(TenantA, Exercicio, 1_000_000m, "rec-1"), default);
        await linhas.AdicionarAsync(LinhaExecucaoFiscal.Despesa(TenantA, Exercicio, "10", "0500", 150_000m, "des-saude-1"), default);
        await linhas.AdicionarAsync(LinhaExecucaoFiscal.Despesa(TenantA, Exercicio, "12", "0540", 240_000m, "des-educ-1"), default);
        await linhas.AdicionarAsync(LinhaExecucaoFiscal.Despesa(TenantA, Exercicio, "04", null, 50_000m, "des-adm-1"), default);
        await contexto.SaveChangesAsync();

        var readModel = new ExecucaoSetorialReadModel(contexto, regras);
        var execucao = await readModel.ObterExecucaoAsync(Exercicio, default);

        execucao.ReceitaBaseImpostosTransferencias.Should().Be(1_000_000m);

        var parametros = new ParametroMinimoProvider(contexto);
        var vigentes = await parametros.ObterParametrosAsync(Exercicio, default);

        var indicadores = ApuradorMinimo.ApurarTodos(
            execucao.ReceitaBaseImpostosTransferencias, execucao.DespesasPorSetor, vigentes);

        var saude = indicadores.Single(i => i.Setor == SetorMinimo.Saude);
        var educacao = indicadores.Single(i => i.Setor == SetorMinimo.Educacao);

        saude.PercentualMinimo.Should().Be(0.15m, "default legal LC 141 sem parâmetro do tenant");
        saude.PercentualAplicado.Should().Be(0.15m);
        saude.Situacao.Should().Be(SituacaoMinimo.Atingido);

        educacao.PercentualMinimo.Should().Be(0.25m, "default legal CF 212 sem parâmetro do tenant");
        educacao.PercentualAplicado.Should().Be(0.24m);
        educacao.Situacao.Should().Be(SituacaoMinimo.NaoAtingido);
    }

    [Fact]
    public async Task Despesa_nao_computavel_por_fonte_nao_entra_no_minimo()
    {
        await using var contexto = CriarContexto(TenantA);
        var regras = new FonteRecursoVinculadoRepository(contexto);
        var linhas = new LinhaExecucaoFiscalRepository(contexto);

        await regras.AdicionarAsync(FonteRecursoVinculado.Criar(TenantA, "10", SetorMinimo.Saude, new DateOnly(2026, 1, 1)), default);
        // Fonte 9999 da saúde (ex.: saneamento, LC 141 art. 4º) não computa.
        await regras.AdicionarAsync(FonteRecursoVinculado.Criar(TenantA, "10", SetorMinimo.Saude, new DateOnly(2026, 1, 1), "9999", computaNoMinimo: false), default);

        await linhas.AdicionarAsync(LinhaExecucaoFiscal.ReceitaBase(TenantA, Exercicio, 1_000_000m, "rec-1"), default);
        await linhas.AdicionarAsync(LinhaExecucaoFiscal.Despesa(TenantA, Exercicio, "10", "0500", 150_000m, "des-1"), default);
        await linhas.AdicionarAsync(LinhaExecucaoFiscal.Despesa(TenantA, Exercicio, "10", "9999", 80_000m, "des-2"), default);
        await contexto.SaveChangesAsync();

        var execucao = await new ExecucaoSetorialReadModel(contexto, regras).ObterExecucaoAsync(Exercicio, default);
        var saude = execucao.DespesasPorSetor.Single(d => d.Setor == SetorMinimo.Saude);

        // Só os 150.000 computáveis entram; os 80.000 da fonte 9999 ficam de fora.
        saude.AplicadoComputavel.Should().Be(150_000m);
    }

    [Fact]
    public async Task Percentual_parametrizado_por_tenant_sobrepoe_o_default_legal()
    {
        await using var contexto = CriarContexto(TenantA);
        var regras = new FonteRecursoVinculadoRepository(contexto);

        // Tenant exige 18% em saúde (Lei Orgânica), versionado.
        contexto.ParametrosFiscaisVigentes.Add(
            ParametroFiscalVigente.Criar(TenantA, ParametroMinimoProvider.ChaveMinimoSaude, new DateOnly(2026, 1, 1), 0.18m));
        await contexto.SaveChangesAsync();

        var parametros = await new ParametroMinimoProvider(contexto).ObterParametrosAsync(Exercicio, default);
        var saude = parametros.Single(p => p.Setor == SetorMinimo.Saude);

        saude.PercentualMinimo.Should().Be(0.18m);
        _ = regras; // mantém o padrão de arrange; sem uso direto aqui.
    }



    [Fact]
    public async Task Isolamento_por_tenant_no_read_model()
    {
        // Tenant A semeia execução; Tenant B não enxerga nada (Global Query Filter).
        await using (var contextoA = CriarContexto(TenantA))
        {
            var regrasA = new FonteRecursoVinculadoRepository(contextoA);
            var linhasA = new LinhaExecucaoFiscalRepository(contextoA);
            await regrasA.AdicionarAsync(FonteRecursoVinculado.Criar(TenantA, "10", SetorMinimo.Saude, new DateOnly(2026, 1, 1)), default);
            await linhasA.AdicionarAsync(LinhaExecucaoFiscal.ReceitaBase(TenantA, Exercicio, 1_000_000m, "rec-A"), default);
            await linhasA.AdicionarAsync(LinhaExecucaoFiscal.Despesa(TenantA, Exercicio, "10", "0500", 150_000m, "des-A"), default);
            await contextoA.SaveChangesAsync();
        }

        await using var contextoB = CriarContexto(TenantB);
        var execucaoB = await new ExecucaoSetorialReadModel(contextoB, new FonteRecursoVinculadoRepository(contextoB))
            .ObterExecucaoAsync(Exercicio, default);

        execucaoB.ReceitaBaseImpostosTransferencias.Should().Be(0m, "Tenant B não vê dados do Tenant A");
        execucaoB.DespesasPorSetor.Should().BeEmpty();
    }

    [Fact]
    public async Task Projecao_e_idempotente_por_origem_hash()
    {
        await using var contexto = CriarContexto(TenantA);
        var linhas = new LinhaExecucaoFiscalRepository(contexto);

        await linhas.AdicionarAsync(LinhaExecucaoFiscal.ReceitaBase(TenantA, Exercicio, 500m, "rec-x"), default);
        await contexto.SaveChangesAsync();

        var jaExiste = await linhas.ExisteAsync("rec-x", default);
        jaExiste.Should().BeTrue("o hash de origem permite ao consumidor evitar duplicar a projeção");
    }
}

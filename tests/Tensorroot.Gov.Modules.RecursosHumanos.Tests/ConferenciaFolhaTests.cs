using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura da CONFERENCIA DE PRE-FECHAMENTO da folha (P0-7): o handler de leitura
/// <see cref="ObterConferenciaFolhaHandler"/> deve devolver os totais (geral + por rubrica) e as
/// quatro divergencias que o conferente precisa ver ANTES de fechar — sem alterar o calculo, sem
/// relogio (reproduzivel) e tenant-scoped.
/// </summary>
public sealed class ConferenciaFolhaTests : RecursosHumanosTestBase
{
    private const decimal Teto = 50_000m;

    private sealed class ParametrosFolhaProviderFake(decimal limiteVariacao) : IParametrosFolhaProvider
    {
        public Task<ParametrosFolha> ObterAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ParametrosFolha
            {
                TetoRemuneratorio = Teto,
                LimiteVariacaoLiquidoConferencia = limiteVariacao,
            });
    }

    private static Servidor SemearServidor(RecursosHumanosDbContext ctx, string cpf, string matricula, string nome)
    {
        var servidor = Servidor.Admitir(
            TenantA,
            Cpf.Create(cpf),
            Matricula.De(matricula),
            DadosPessoais.Criar(nome, new DateOnly(1985, 4, 2)),
            CargoId.New(),
            RegimePrevidenciario.Rpps,
            new DateOnly(2010, 3, 1));
        ctx.Servidores.Add(servidor);
        return servidor;
    }

    private static ObterConferenciaFolhaHandler CriarHandler(RecursosHumanosDbContext ctx, decimal limiteVariacao = 1_000m)
        => new(
            new FolhaDePagamentoRepository(ctx),
            new ServidorRepository(ctx),
            new ParametrosFolhaProviderFake(limiteVariacao));

    [Fact] // Totais geral + por rubrica + contagens; e a divergencia (d) confere (proventos - descontos = liquido).
    public async Task Conferencia_devolve_totais_e_totais_conferem()
    {
        Guid folhaId;
        var competencia = Competencia.De(2026, 5);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = SemearServidor(ctx, "52998224725", "0001", "Maria da Silva");
            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(8_000m), 8_000m, RegimePrevidenciario.Rpps);
            folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("9001"), TipoEvento.Desconto, BaseCalculo.De(8_000m), 1_120m, RegimePrevidenciario.Rpps);
            folha.Calcular(Teto, new DateOnly(2026, 5, 30));
            ctx.FolhasDePagamento.Add(folha);
            folhaId = folha.Id.Value;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var conferencia = await CriarHandler(ctx).Handle(new ObterConferenciaFolhaQuery(folhaId), default);

            conferencia.Should().NotBeNull();
            conferencia!.TotalProventos.Should().Be(8_000m);
            conferencia.TotalDescontos.Should().Be(1_120m);
            conferencia.TotalLiquido.Should().Be(6_880m);
            conferencia.QuantidadeServidores.Should().Be(1);
            conferencia.QuantidadeLancamentos.Should().Be(2);
            conferencia.TotaisConferem.Should().BeTrue();
            conferencia.TemDivergencias.Should().BeFalse();
            conferencia.TotaisPorRubrica.Should().HaveCount(2);
            conferencia.TotaisPorRubrica.Should().Contain(linha => linha.Rubrica == "1001" && linha.Tipo == "Provento" && linha.Total == 8_000m);
            conferencia.TotaisPorRubrica.Should().Contain(linha => linha.Rubrica == "9001" && linha.Tipo == "Desconto" && linha.Total == 1_120m);
        }
    }

    [Fact] // (a) Servidor ativo SEM nenhum lancamento na folha aparece na divergencia.
    public async Task Conferencia_detecta_ativo_sem_lancamento()
    {
        Guid folhaId;
        Guid semLancamentoId;
        var competencia = Competencia.De(2026, 5);

        await using (var ctx = CriarContexto(TenantA))
        {
            var comLancamento = SemearServidor(ctx, "52998224725", "0001", "Com Lancamento");
            var semLancamento = SemearServidor(ctx, "11144477735", "0002", "Sem Lancamento");
            semLancamentoId = semLancamento.Id.Value;

            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(comLancamento.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(5_000m), 5_000m, RegimePrevidenciario.Rpps);
            folha.Calcular(Teto, new DateOnly(2026, 5, 30));
            ctx.FolhasDePagamento.Add(folha);
            folhaId = folha.Id.Value;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var conferencia = await CriarHandler(ctx).Handle(new ObterConferenciaFolhaQuery(folhaId), default);

            conferencia!.ServidoresAtivosSemLancamento.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new ServidorSemLancamento(semLancamentoId, "0002", "Sem Lancamento"));
            conferencia.TemDivergencias.Should().BeTrue();
        }
    }

    [Fact] // (b) Servidor com liquido insuficiente (descontos >= proventos) reusa o sinal do P0-5.
    public async Task Conferencia_detecta_liquido_insuficiente()
    {
        Guid folhaId;
        Guid insuficienteId;
        var competencia = Competencia.De(2026, 5);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = SemearServidor(ctx, "52998224725", "0001", "Endividado");
            insuficienteId = servidor.Id.Value;

            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(3_000m), 3_000m, RegimePrevidenciario.Rpps);
            folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("CONSIG"), TipoEvento.Desconto, BaseCalculo.De(3_000m), 3_500m, RegimePrevidenciario.Rpps);
            folha.Calcular(Teto, new DateOnly(2026, 5, 30));
            ctx.FolhasDePagamento.Add(folha);
            folhaId = folha.Id.Value;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var conferencia = await CriarHandler(ctx).Handle(new ObterConferenciaFolhaQuery(folhaId), default);

            var linha = conferencia!.ServidoresComLiquidoInsuficiente.Should().ContainSingle().Subject;
            linha.ServidorId.Should().Be(insuficienteId);
            linha.Matricula.Should().Be("0001");
            linha.TotalProventos.Should().Be(3_000m);
            linha.TotalDescontos.Should().Be(3_500m);
            conferencia.TotalLiquido.Should().Be(0m);
            conferencia.TotaisConferem.Should().BeTrue();
            conferencia.TemDivergencias.Should().BeTrue();
        }
    }

    [Fact] // (c) Variacao do liquido vs competencia anterior acima do limiar e sinalizada; abaixo, nao.
    public async Task Conferencia_detecta_variacao_suspeita_vs_competencia_anterior()
    {
        Guid folhaAtualId;
        Guid saltouId;
        var anterior = Competencia.De(2026, 4);
        var atual = Competencia.De(2026, 5);

        await using (var ctx = CriarContexto(TenantA))
        {
            var saltou = SemearServidor(ctx, "52998224725", "0001", "Saltou"); // 2.000 -> 9.000 (Delta 7.000)
            var estavel = SemearServidor(ctx, "11144477735", "0002", "Estavel"); // 4.000 -> 4.200 (Delta 200)
            saltouId = saltou.Id.Value;

            var folhaAnterior = FolhaDePagamento.Abrir(TenantA, anterior);
            folhaAnterior.AdicionarEvento(saltou.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(2_000m), 2_000m, RegimePrevidenciario.Rpps);
            folhaAnterior.AdicionarEvento(estavel.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(4_000m), 4_000m, RegimePrevidenciario.Rpps);
            folhaAnterior.Calcular(Teto, new DateOnly(2026, 4, 30));
            ctx.FolhasDePagamento.Add(folhaAnterior);

            var folhaAtual = FolhaDePagamento.Abrir(TenantA, atual);
            folhaAtual.AdicionarEvento(saltou.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(9_000m), 9_000m, RegimePrevidenciario.Rpps);
            folhaAtual.AdicionarEvento(estavel.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(4_200m), 4_200m, RegimePrevidenciario.Rpps);
            folhaAtual.Calcular(Teto, new DateOnly(2026, 5, 30));
            ctx.FolhasDePagamento.Add(folhaAtual);
            folhaAtualId = folhaAtual.Id.Value;

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var conferencia = await CriarHandler(ctx, limiteVariacao: 1_000m)
                .Handle(new ObterConferenciaFolhaQuery(folhaAtualId), default);

            conferencia!.CompetenciaAnterior.Should().Be("2026-04");
            var variacao = conferencia.ServidoresComVariacaoSuspeita.Should().ContainSingle().Subject;
            variacao.ServidorId.Should().Be(saltouId);
            variacao.LiquidoAnterior.Should().Be(2_000m);
            variacao.LiquidoAtual.Should().Be(9_000m);
            variacao.Variacao.Should().Be(7_000m);
            variacao.VariacaoPercentual.Should().Be(350m);
            conferencia.TemDivergencias.Should().BeTrue();
        }
    }

    [Fact] // O limiar de variacao e parametrizavel: a sobrescrita da query suprime a divergencia (c).
    public async Task Conferencia_limiar_de_variacao_parametrizavel_pela_query()
    {
        Guid folhaAtualId;
        var anterior = Competencia.De(2026, 4);
        var atual = Competencia.De(2026, 5);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = SemearServidor(ctx, "52998224725", "0001", "Saltou");

            var folhaAnterior = FolhaDePagamento.Abrir(TenantA, anterior);
            folhaAnterior.AdicionarEvento(servidor.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(2_000m), 2_000m, RegimePrevidenciario.Rpps);
            folhaAnterior.Calcular(Teto, new DateOnly(2026, 4, 30));
            ctx.FolhasDePagamento.Add(folhaAnterior);

            var folhaAtual = FolhaDePagamento.Abrir(TenantA, atual);
            folhaAtual.AdicionarEvento(servidor.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(9_000m), 9_000m, RegimePrevidenciario.Rpps);
            folhaAtual.Calcular(Teto, new DateOnly(2026, 5, 30));
            ctx.FolhasDePagamento.Add(folhaAtual);
            folhaAtualId = folhaAtual.Id.Value;

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            // Limiar alto (10.000) acima do |Delta| (7.000) -> sem divergencia de variacao; eco do limiar usado.
            var conferencia = await CriarHandler(ctx)
                .Handle(new ObterConferenciaFolhaQuery(folhaAtualId, LimiteVariacaoLiquido: 10_000m), default);

            conferencia!.LimiteVariacaoLiquido.Should().Be(10_000m);
            conferencia.ServidoresComVariacaoSuspeita.Should().BeEmpty();
        }
    }

    [Fact] // Sem folha anterior, nao ha base de comparacao: variacao vazia e competencia anterior nula.
    public async Task Conferencia_sem_folha_anterior_nao_compara_variacao()
    {
        Guid folhaId;
        var competencia = Competencia.De(2026, 5);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = SemearServidor(ctx, "52998224725", "0001", "Novo");
            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(5_000m), 5_000m, RegimePrevidenciario.Rpps);
            folha.Calcular(Teto, new DateOnly(2026, 5, 30));
            ctx.FolhasDePagamento.Add(folha);
            folhaId = folha.Id.Value;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var conferencia = await CriarHandler(ctx).Handle(new ObterConferenciaFolhaQuery(folhaId), default);

            conferencia!.CompetenciaAnterior.Should().BeNull();
            conferencia.ServidoresComVariacaoSuspeita.Should().BeEmpty();
        }
    }

    [Fact] // Folha inexistente no tenant devolve nulo (404 na borda) — nunca vaza outro tenant.
    public async Task Conferencia_folha_inexistente_devolve_nulo()
    {
        await using var ctx = CriarContexto(TenantA);
        var conferencia = await CriarHandler(ctx).Handle(new ObterConferenciaFolhaQuery(Guid.NewGuid()), default);
        conferencia.Should().BeNull();
    }
}

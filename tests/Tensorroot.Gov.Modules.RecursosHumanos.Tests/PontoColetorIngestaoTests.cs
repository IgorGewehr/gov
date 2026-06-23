using FluentAssertions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura de integracao do COLETOR DE PONTO sobre SQLite em memoria: ingestao IDEMPOTENTE de AFD
/// (reimportar nao duplica), dedup por (REP, NSR-equipamento), resolucao CPF -&gt; servidor, fail-closed
/// na integridade (CRC) e ISOLAMENTO por tenant (Global Query Filter). Usa o handler/repositorios reais.
/// </summary>
public sealed class PontoColetorIngestaoTests : RecursosHumanosTestBase
{
    private const string CpfServidor = "39053344705";
    private const string CpfOutro = "11144477735";
    private static readonly Cnpj CnpjEnte = Cnpj.Create("11222333000181");
    private static readonly DateOnly Nomeacao = new(2026, 1, 5);

    private static IngestarMarcacoesAfdHandler CriarHandler(RecursosHumanosDbContext ctx, Guid tenantId)
        => new(
            new ParserAfd(),
            new MarcacaoPontoRepository(ctx),
            new RepRepository(ctx),
            new ServidorPontoConsulta(ctx),
            new TenantContextStub(tenantId),
            ctx);

    private static async Task<Servidor> SemearServidorAsync(RecursosHumanosDbContext ctx, Guid tenantId, string cpf, string matricula)
    {
        var servidor = Servidor.Admitir(
            tenantId,
            Cpf.Create(cpf),
            Matricula.De(matricula),
            DadosPessoais.Criar("Maria da Silva", new DateOnly(1990, 3, 20)),
            CargoId.New(),
            RegimePrevidenciario.Rpps,
            Nomeacao);
        ctx.Servidores.Add(servidor);
        await ctx.SaveChangesAsync();
        return servidor;
    }

    private static async Task<RepConfigurado> SemearRepAsync(RecursosHumanosDbContext ctx, Guid tenantId)
    {
        var rep = RepConfigurado.Registrar(
            tenantId, "REP-TESTE-001", MarcaRep.ArquivoAfd, ModoColeta.Arquivo, TipoRep.RepC);
        ctx.PontoReps.Add(rep);
        await ctx.SaveChangesAsync();
        return rep;
    }

    private static byte[] GerarAfd(int qtd, long nsrInicial = 1)
    {
        var cpfs = new[] { Cpf.Create(CpfServidor), Cpf.Create(CpfOutro) };
        var linhas = new List<LinhaMarcacaoAfd>();
        for (var i = 0; i < qtd; i++)
        {
            linhas.Add(new LinhaMarcacaoAfd(
                Nsr.De(nsrInicial + i),
                cpfs[i % cpfs.Length],
                new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero).AddHours(i),
                TipoRep.RepC));
        }

        var cabecalho = new CabecalhoAfd(CnpjEnte, "MUNICIPIO", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), DateTimeOffset.Now);
        return GeradorAfd.Gerar(cabecalho, linhas);
    }

    [Fact] // Ingestao basica: marcacoes com CPF de servidor existente sao ingeridas e vinculadas.
    public async Task Ingere_marcacoes_e_vincula_por_cpf()
    {
        await using var ctx = CriarContexto(TenantA);
        await SemearServidorAsync(ctx, TenantA, CpfServidor, "MAT-1");
        await SemearServidorAsync(ctx, TenantA, CpfOutro, "MAT-2");
        var rep = await SemearRepAsync(ctx, TenantA);
        var handler = CriarHandler(ctx, TenantA);

        var resultado = await handler.Handle(new IngestarMarcacoesAfdCommand(rep.Id.Value, GerarAfd(4), null), default);

        resultado.Novas.Should().Be(4);
        resultado.Duplicadas.Should().Be(0);
        resultado.PendentesDeVinculo.Should().Be(0);
        resultado.IntegridadeOk.Should().BeTrue();
        ctx.PontoMarcacoes.Count().Should().Be(4);
    }

    [Fact] // IDEMPOTENCIA: reimportar o MESMO AFD nao duplica (dedup por (REP, NSR-equipamento)).
    public async Task Reimportar_mesmo_afd_nao_duplica()
    {
        await using var ctx = CriarContexto(TenantA);
        await SemearServidorAsync(ctx, TenantA, CpfServidor, "MAT-1");
        await SemearServidorAsync(ctx, TenantA, CpfOutro, "MAT-2");
        var rep = await SemearRepAsync(ctx, TenantA);
        var handler = CriarHandler(ctx, TenantA);
        var afd = GerarAfd(4);

        var primeira = await handler.Handle(new IngestarMarcacoesAfdCommand(rep.Id.Value, afd, null), default);
        var segunda = await handler.Handle(new IngestarMarcacoesAfdCommand(rep.Id.Value, afd, null), default);

        primeira.Novas.Should().Be(4);
        segunda.Novas.Should().Be(0);
        segunda.Duplicadas.Should().Be(4);
        ctx.PontoMarcacoes.Count().Should().Be(4); // nada duplicado
    }

    [Fact] // Coleta incremental: um 2o AFD com NSR novos acrescenta so o que falta.
    public async Task Reimportar_com_nsr_novos_acrescenta_apenas_os_novos()
    {
        await using var ctx = CriarContexto(TenantA);
        await SemearServidorAsync(ctx, TenantA, CpfServidor, "MAT-1");
        await SemearServidorAsync(ctx, TenantA, CpfOutro, "MAT-2");
        var rep = await SemearRepAsync(ctx, TenantA);
        var handler = CriarHandler(ctx, TenantA);

        await handler.Handle(new IngestarMarcacoesAfdCommand(rep.Id.Value, GerarAfd(2, nsrInicial: 1), null), default);
        // Segundo lote sobrepoe NSR 2 (duplicado) e traz 3,4 (novos).
        var segunda = await handler.Handle(new IngestarMarcacoesAfdCommand(rep.Id.Value, GerarAfd(3, nsrInicial: 2), null), default);

        segunda.Novas.Should().Be(2);
        segunda.Duplicadas.Should().Be(1);
        ctx.PontoMarcacoes.Count().Should().Be(4);

        var repAtualizado = await new RepRepository(ctx).ObterPorIdAsync(rep.Id, default);
        repAtualizado!.UltimoNsrColetado.Should().Be(4); // cursor avancou para o maior NSR ingerido.
    }

    [Fact] // CPF sem servidor no tenant -> pendente de vinculo (auditavel), NAO ingerido nem descartado.
    public async Task Cpf_sem_servidor_fica_pendente_de_vinculo()
    {
        await using var ctx = CriarContexto(TenantA);
        // Semeia apenas UM dos dois CPFs do AFD.
        await SemearServidorAsync(ctx, TenantA, CpfServidor, "MAT-1");
        var rep = await SemearRepAsync(ctx, TenantA);
        var handler = CriarHandler(ctx, TenantA);

        var resultado = await handler.Handle(new IngestarMarcacoesAfdCommand(rep.Id.Value, GerarAfd(4), null), default);

        // 4 marcacoes: 2 do CpfServidor (vinculadas) + 2 do CpfOutro (pendentes).
        resultado.Novas.Should().Be(2);
        resultado.PendentesDeVinculo.Should().Be(2);
        ctx.PontoMarcacoes.Count().Should().Be(2);
    }

    [Fact] // FAIL-CLOSED: AFD com CRC adulterado e recusado por inteiro (nada e ingerido).
    public async Task Afd_com_crc_adulterado_e_recusado()
    {
        await using var ctx = CriarContexto(TenantA);
        await SemearServidorAsync(ctx, TenantA, CpfServidor, "MAT-1");
        await SemearServidorAsync(ctx, TenantA, CpfOutro, "MAT-2");
        var rep = await SemearRepAsync(ctx, TenantA);
        var handler = CriarHandler(ctx, TenantA);

        var afd = GerarAfd(2);
        var texto = System.Text.Encoding.Latin1.GetString(afd);
        var adulterado = System.Text.Encoding.Latin1.GetBytes(
            texto.Replace(CpfServidor, CpfOutro, StringComparison.Ordinal));

        var acao = async () => await handler.Handle(new IngestarMarcacoesAfdCommand(rep.Id.Value, adulterado, null), default);

        await acao.Should().ThrowAsync<InvalidOperationException>();
        ctx.PontoMarcacoes.Count().Should().Be(0);
    }

    [Fact] // ISOLAMENTO: REP/servidor de A nao sao vistos por B; ingerir em B com REP de A falha.
    public async Task Ingestao_e_isolada_por_tenant()
    {
        RepConfiguradoId repAId;
        await using (var ctxA = CriarContexto(TenantA))
        {
            await SemearServidorAsync(ctxA, TenantA, CpfServidor, "MAT-1");
            await SemearServidorAsync(ctxA, TenantA, CpfOutro, "MAT-2");
            var repA = await SemearRepAsync(ctxA, TenantA);
            repAId = repA.Id;
            var handlerA = CriarHandler(ctxA, TenantA);
            await handlerA.Handle(new IngestarMarcacoesAfdCommand(repA.Id.Value, GerarAfd(4), null), default);
        }

        await using var ctxB = CriarContexto(TenantB);
        var handlerB = CriarHandler(ctxB, TenantB);

        // O REP de A nao existe para B (Global Query Filter) -> ingestao falha (REP nao cadastrado).
        var acao = async () => await handlerB.Handle(new IngestarMarcacoesAfdCommand(repAId.Value, GerarAfd(4), null), default);
        await acao.Should().ThrowAsync<InvalidOperationException>();

        // B nao enxerga nenhuma marcacao de A.
        ctxB.PontoMarcacoes.Count().Should().Be(0);
    }
}

/// <summary>Contexto de tenant fixo para exercitar os handlers de coleta nos testes.</summary>
internal sealed class TenantContextStub(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

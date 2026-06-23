using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura de integracao do pipeline eSocial ponta-a-ponta sobre SQLite: gera (do dominio: folha
/// fechada/servidor/rubrica), assina (Cofre fake), empacota+transmite (gateway SIMULADO) e consulta o
/// retorno (recibo). Cobre tambem a idempotencia de geracao (nao duplica por chave de negocio).
/// </summary>
public sealed class ESocialPipelineIntegracaoTests : RecursosHumanosTestBase
{
    private static readonly TimeProvider Relogio = TimeProvider.System;

    private sealed class EmpregadorProviderFake : IEmpregadorESocialProvider
    {
        public Task<ParametrosESocial> ObterAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ParametrosESocial
            {
                CnpjEnte = "11222333000181",
                NomeEnte = "MUNICIPIO X",
                ClassTrib = "01",
                CnpjEfr = "99888777000166",
                InicioValidade = "2026-06",
                Ambiente = AmbienteESocial.ProducaoRestrita,
            });
    }

    // Cofre fake: "assina" devolvendo o XML embrulhado + Signature simulada (sem cripto real).
    private sealed class AssinaturaFake : IAssinaturaEmEscopoDedicado
    {
        public Task<byte[]> AssinarCmsAsync(ReadOnlyMemory<byte> conteudo, OpcoesAssinaturaCms opcoes, CancellationToken cancellationToken)
            => Task.FromResult(conteudo.ToArray());

        public Task<ResultadoAssinatura> AssinarXmlAsync(ReadOnlyMemory<byte> xmlUtf8, OpcoesAssinaturaXml opcoes, CancellationToken cancellationToken)
        {
            // Insere um <Signature/> antes de </eSocial> mantendo XML bem-formado (suficiente para o teste).
            var texto = Encoding.UTF8.GetString(xmlUtf8.Span);
            var assinado = texto.Replace("</eSocial>", "<Signature>FAKE</Signature></eSocial>", StringComparison.Ordinal);
            return Task.FromResult(new ResultadoAssinatura(Encoding.UTF8.GetBytes(assinado), "THUMB-TEST", "hash"));
        }
    }

    private static GeradorEventoApplicationService Gerador(RecursosHumanosDbContext ctx)
        => new(new EventoESocialRepository(ctx), ctx, new TenantContextFake(TenantA), Relogio);

    private static async Task<Guid> SemearFolhaFechadaAsync(RecursosHumanosDbContext ctx, RegimePrevidenciario regime)
    {
        var servidor = Servidor.Admitir(
            TenantA, Cpf.Create("52998224725"), Matricula.De("M-0001"),
            DadosPessoais.Criar("Fulano", new DateOnly(1990, 1, 1)), CargoId.New(), regime, new DateOnly(2026, 1, 5));
        ctx.Servidores.Add(servidor);

        var folha = FolhaDePagamento.Abrir(TenantA, Competencia.De(2026, 6));
        folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("VENC"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, regime);
        folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("INSS"), TipoEvento.Desconto, BaseCalculo.De(5000m), 550m, regime);
        folha.Calcular(50000m, new DateOnly(2026, 7, 1));
        folha.Fechar(new DateOnly(2026, 7, 2));
        ctx.FolhasDePagamento.Add(folha);
        await ctx.SaveChangesAsync();
        return folha.Id.Value;
    }

    [Fact] // Geracao a partir da folha fechada: 1 S-1202 por servidor RPPS, persistido em Gerado.
    public async Task Gera_remuneracao_da_folha_fechada()
    {
        await using var ctx = CriarContexto(TenantA);
        var folhaId = await SemearFolhaFechadaAsync(ctx, RegimePrevidenciario.Rpps);

        var handler = new GerarRemuneracaoFolhaHandler(
            new EmpregadorProviderFake(), new FolhaDePagamentoRepository(ctx), new ServidorRepository(ctx), Gerador(ctx));

        var ids = await handler.Handle(new GerarRemuneracaoFolhaCommand(folhaId), CancellationToken.None);

        ids.Should().HaveCount(1);
        var evento = await ctx.EventosESocial.SingleAsync();
        evento.Tipo.Should().Be(TipoEventoESocial.S1202RemuneracaoRpps);
        evento.Estado.Should().Be(EstadoEventoESocial.Gerado);
    }

    [Fact] // Idempotencia: gerar 2x a mesma folha nao duplica o evento (mesma chave de negocio).
    public async Task Geracao_e_idempotente_por_chave()
    {
        await using var ctx = CriarContexto(TenantA);
        var folhaId = await SemearFolhaFechadaAsync(ctx, RegimePrevidenciario.Rpps);
        var handler = new GerarRemuneracaoFolhaHandler(
            new EmpregadorProviderFake(), new FolhaDePagamentoRepository(ctx), new ServidorRepository(ctx), Gerador(ctx));

        await handler.Handle(new GerarRemuneracaoFolhaCommand(folhaId), CancellationToken.None);
        await handler.Handle(new GerarRemuneracaoFolhaCommand(folhaId), CancellationToken.None);

        (await ctx.EventosESocial.CountAsync()).Should().Be(1);
    }

    [Fact] // Fail-closed: sem CNPJ do ente, a geracao e recusada.
    public async Task Geracao_sem_cnpj_falha_closed()
    {
        await using var ctx = CriarContexto(TenantA);
        var folhaId = await SemearFolhaFechadaAsync(ctx, RegimePrevidenciario.Rpps);
        var empregadorVazio = new EmpregadorVazioFake();
        var handler = new GerarRemuneracaoFolhaHandler(
            empregadorVazio, new FolhaDePagamentoRepository(ctx), new ServidorRepository(ctx), Gerador(ctx));

        var acao = async () => await handler.Handle(new GerarRemuneracaoFolhaCommand(folhaId), CancellationToken.None);

        await acao.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact] // Pipeline completo: gerar -> assinar -> transmitir -> consultar => Processado com recibo.
    public async Task Pipeline_completo_gera_assina_transmite_consulta()
    {
        await using var ctx = CriarContexto(TenantA);
        var folhaId = await SemearFolhaFechadaAsync(ctx, RegimePrevidenciario.Rpps);
        var gateway = new ESocialGatewaySimulado(NullLogger<ESocialGatewaySimulado>.Instance);

        // 1) Gerar.
        await new GerarRemuneracaoFolhaHandler(new EmpregadorProviderFake(), new FolhaDePagamentoRepository(ctx), new ServidorRepository(ctx), Gerador(ctx))
            .Handle(new GerarRemuneracaoFolhaCommand(folhaId), CancellationToken.None);
        var eventoId = (await ctx.EventosESocial.SingleAsync()).Id.Value;

        // 2) Assinar (Cofre fake).
        await new AssinarEventoESocialHandler(new EventoESocialRepository(ctx), new AssinaturaFake(), ctx, Relogio)
            .Handle(new AssinarEventoESocialCommand(eventoId), CancellationToken.None);
        (await ctx.EventosESocial.SingleAsync()).Estado.Should().Be(EstadoEventoESocial.Assinado);

        // 3) Transmitir (empacota + gateway SIMULADO).
        var transmitidos = await new TransmitirEventosAssinadosHandler(new EmpregadorProviderFake(), new EventoESocialRepository(ctx), gateway, ctx, Relogio)
            .Handle(new TransmitirEventosAssinadosCommand(), CancellationToken.None);
        transmitidos.Should().Be(1);
        var transmitido = await ctx.EventosESocial.SingleAsync();
        transmitido.Estado.Should().Be(EstadoEventoESocial.Transmitido);
        transmitido.ProtocoloLote.Should().NotBeNullOrEmpty();

        // 4) Consultar retorno (recibo) => Processado.
        var processados = await new ConsultarRetornosESocialHandler(new EventoESocialRepository(ctx), gateway, ctx, Relogio)
            .Handle(new ConsultarRetornosESocialCommand(), CancellationToken.None);
        processados.Should().Be(1);
        var final = await ctx.EventosESocial.SingleAsync();
        final.Estado.Should().Be(EstadoEventoESocial.Processado);
        final.NumeroRecibo.Should().NotBeNullOrEmpty();
    }

    private sealed class EmpregadorVazioFake : IEmpregadorESocialProvider
    {
        public Task<ParametrosESocial> ObterAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ParametrosESocial { CnpjEnte = null });
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.MinhaFolha;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo RecursosHumanos.</summary>
internal static partial class RecursosHumanosEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/recursoshumanos").WithTags("RecursosHumanos");

        MapearServidores(grupo);
        MapearCargos(grupo);
        MapearRubricas(grupo);
        MapearTabelasLegais(grupo);
        MapearFolha(grupo);
        MapearRelatorios(grupo);
        MapearConsignacoes(grupo);
        MapearCicloAnual(grupo);
        MapearPonto(grupo);
        MapearESocial(grupo);
        MapearMinhaFolha(grupo);
    }

    private static void MapearMinhaFolha(RouteGroupBuilder grupo)
    {
        // AUTOSSERVICO DO SERVIDOR ("Minha Folha"). Gated por 'autosservico.proprio' (papel Servidor).
        // SEGURANCA dado-proprio A PROVA DE BALA: NENHUM endpoint aceita um servidorId do cliente — o
        // servidor e SEMPRE resolvido do PROPRIO usuario autenticado (vinculo usuario<->servidor) no
        // handler. Tentar ver outro servidor e impossivel: nao ha parametro de servidor a forjar.
        var minha = grupo.MapGroup("/minha-folha").WithTags("RecursosHumanos.MinhaFolha");

        // VINCULO usuario<->servidor (GESTAO do RH; nao e autosservico — gated por gerenciar).
        // Declara que um usuario do Identidade E um servidor; ancora os endpoints proprios.
        minha.MapPost("/vinculos", async (
            VincularUsuarioAoServidorCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // MEU CONTRACHEQUE por competencia e tipo (mensal/13o/ferias/rescisao). So o do PROPRIO servidor.
        minha.MapGet("/contracheque", async (
            int ano, int mes, TipoFolha? tipo, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterMeuContrachequeQuery(ano, mes, tipo ?? TipoFolha.Mensal), ct)))
            .RequirePermission("autosservico.proprio");

        // MEU ESPELHO DE PONTO (apuracao da minha jornada) numa competencia. So o do PROPRIO servidor.
        minha.MapGet("/espelho-ponto", async (
            int ano, int mes, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterMeuEspelhoDePontoQuery(ano, mes), ct)))
            .RequirePermission("autosservico.proprio");

        // MINHAS FERIAS num ano (folhas de ferias com minhas verbas). So as do PROPRIO servidor.
        minha.MapGet("/ferias", async (
            int ano, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterMinhasFeriasQuery(ano), ct)))
            .RequirePermission("autosservico.proprio");

        // MEU INFORME DE RENDIMENTOS anual (rendimentos/previdencia/IRRF). So o do PROPRIO servidor.
        minha.MapGet("/informe-rendimentos", async (
            int ano, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterMeuInformeDeRendimentosQuery(ano), ct)))
            .RequirePermission("autosservico.proprio");
    }

    private static void MapearCicloAnual(RouteGroupBuilder grupo)
    {
        // Ciclo anual da folha: 13o salario, ferias e rescisao — cada um gera sua folha (Tipo proprio),
        // reusa rubricas/tabelas e o motor de calculo (design FOLHA-CICLO-ANUAL-DESIGN).
        var ciclo = grupo.MapGroup("/ciclo-anual");

        // 13o salario (parcela 1 = adiantamento sem desconto; parcela 2 = integral com INSS/IRRF do 13o).
        ciclo.MapPost("/decimo-terceiro", async (
            GerarDecimoTerceiroCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { folhaId = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Ferias: remuneracao + 1/3 constitucional + abono pecuniario opcional.
        ciclo.MapPost("/ferias", async (
            GerarFeriasCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { folhaId = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Rescisao: verbas rescisorias por tipo de desligamento e regime (matriz parametrizavel).
        ciclo.MapPost("/rescisao", async (
            GerarVerbasRescisoriasCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { folhaId = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearESocial(RouteGroupBuilder grupo)
    {
        var esocial = grupo.MapGroup("/esocial");

        // Geracao de eventos de tabela (a partir da config do tenant / catalogo de rubricas).
        esocial.MapPost("/eventos/s1000", async (ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(new GerarS1000Command(), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        esocial.MapPost("/eventos/s1005", async (GerarS1005Command comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        esocial.MapPost("/eventos/s1010", async (GerarS1010Command comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Eventos nao-periodicos (do agregado Servidor).
        esocial.MapPost("/eventos/s2200", async (GerarS2200Command comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        esocial.MapPost("/eventos/s2299", async (GerarS2299Command comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Eventos periodicos (da folha fechada/paga): remuneracao (S-1200/1202), pagamentos (S-1210), fechamento (S-1299).
        esocial.MapPost("/folhas/{folhaId:guid}/remuneracao", async (Guid folhaId, ISender sender, CancellationToken ct)
            => Results.Ok(new { ids = await sender.Send(new GerarRemuneracaoFolhaCommand(folhaId), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        esocial.MapPost("/folhas/{folhaId:guid}/pagamentos", async (Guid folhaId, ISender sender, CancellationToken ct)
            => Results.Ok(new { ids = await sender.Send(new GerarPagamentosFolhaCommand(folhaId), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        esocial.MapPost("/folhas/{folhaId:guid}/fechamento-esocial", async (Guid folhaId, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(new GerarFechamentoFolhaCommand(folhaId), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Assinatura (A1 via Cofre) de um evento Gerado.
        esocial.MapPost("/eventos/{eventoId:guid}/assinatura", async (Guid eventoId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AssinarEventoESocialCommand(eventoId), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Transmissao em lote (empacota Assinados, envia, guarda protocolo).
        esocial.MapPost("/transmissao", async (ISender sender, CancellationToken ct)
            => Results.Ok(new { transmitidos = await sender.Send(new TransmitirEventosAssinadosCommand(), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Consulta/processamento dos retornos (recibos/erros por evento).
        esocial.MapPost("/retornos", async (ISender sender, CancellationToken ct)
            => Results.Ok(new { processados = await sender.Send(new ConsultarRetornosESocialCommand(), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Inspecao/auditoria: lista os eventos eSocial do tenant (estado, XML gerado, protocolo, recibo).
        esocial.MapGet("/eventos", async (ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarEventosESocialQuery(), ct)))
            .RequirePermission("recursoshumanos.ver");
    }

    private static void MapearPonto(RouteGroupBuilder grupo)
    {
        var ponto = grupo.MapGroup("/ponto");

        MapearColeta(ponto);

        // Define/substitui a jornada/escala do servidor.
        ponto.MapPost("/jornadas", async (
            DefinirJornadaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Registra uma marcacao (batida); devolve o NSR sequencial atribuido.
        ponto.MapPost("/marcacoes", async (
            RegistrarMarcacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { nsr = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Apura a jornada (PTRP) de um servidor numa competencia (trata sem alterar o AFD).
        ponto.MapPost("/apuracoes", async (
            ApurarJornadaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Fecha a apuracao (congela espelho/AEJ; gancho p/ folha via Outbox).
        ponto.MapPost("/apuracoes/{apuracaoId:guid}/fechamento", async (
            Guid apuracaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new FecharApuracaoJornadaCommand(apuracaoId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Gera o AFD (Arquivo Fonte de Dados) de um periodo, assinado em CAdES (.p7s).
        ponto.MapGet("/afd", async (
            DateOnly inicio, DateOnly fim, bool? assinar, ISender sender, CancellationToken cancellationToken) =>
        {
            var artefato = await sender.Send(new GerarAfdQuery(inicio, fim, assinar ?? true), cancellationToken);
            return Results.File(artefato.Conteudo, "text/plain", artefato.NomeArquivo);
        })
            .RequirePermission("recursoshumanos.ver");

        // Gera o AEJ (Arquivo Eletronico de Jornada) de uma competencia, assinado em CAdES (.p7s).
        ponto.MapGet("/aej", async (
            int ano, int mes, bool? assinar, ISender sender, CancellationToken cancellationToken) =>
        {
            var artefato = await sender.Send(new GerarAejQuery(ano, mes, assinar ?? true), cancellationToken);
            return Results.File(artefato.Conteudo, "text/plain", artefato.NomeArquivo);
        })
            .RequirePermission("recursoshumanos.ver");
    }

    private static void MapearColeta(RouteGroupBuilder ponto)
    {
        // COLETOR DE PONTO (hardware REP -> AFD -> nosso dominio). Cadastro do parque, importacao de
        // arquivo (driver universal) e coleta online (driver por fabricante, atras de ACL).
        var reps = ponto.MapGroup("/reps");

        // Cadastra um REP (equipamento) no parque do ente.
        reps.MapPost("/", async (
            RegistrarRepCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // IMPORTACAO DE ARQUIVO AFD (upload pela UI ou pendrive da porta fiscal) — ingestao idempotente.
        // Recebe os bytes crus do AFD (text/plain ou application/octet-stream) e o REP de origem na rota.
        reps.MapPost("/{repId:guid}/afd/importar", async (
            Guid repId, HttpRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            using var memoria = new MemoryStream();
            await request.Body.CopyToAsync(memoria, cancellationToken);
            var resultado = await sender.Send(
                new ImportarAfdCommand(repId, memoria.ToArray(), AssinaturaCades: null), cancellationToken);
            return Results.Ok(resultado);
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // COLETA ONLINE de um REP (driver por fabricante; SIMULADO por padrao) — ingestao idempotente.
        reps.MapPost("/{repId:guid}/coletar", async (
            Guid repId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ColetarRepCommand(repId), cancellationToken)))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearRubricas(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/rubricas", async (
            CriarRubricaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/rubricas/vigentes", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarRubricasQuery(ano, mes), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");
    }

    private static void MapearTabelasLegais(RouteGroupBuilder grupo)
    {
        // Semeia INSS/IRRF federais oficiais (RPPS NAO: depende de lei municipal — fail-closed).
        grupo.MapPost("/tabelas-legais/semear-federais", async (
            ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new SemearTabelasFederaisCommand(), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/tabelas-legais/rpps", async (
            CriarTabelaRppsCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearServidores(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/servidores", async (
            AdmitirServidorCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/servidores/ativos", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarServidoresAtivosQuery(), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        // NAVEGABILIDADE (Onda 0): lista/busca paginada de servidores por nome/matricula, filtros
        // situacao/regime/cargo. CPF mascarado na projecao (LGPD).
        grupo.MapGet("/servidores", async (
            string? termo, SituacaoServidor? situacao, RegimePrevidenciario? regime, Guid? cargoId,
            int? pagina, int? tamanho, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new BuscarServidoresQuery(termo, situacao, regime, cargoId, pagina, tamanho), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        // NAVEGABILIDADE (Onda 0): FICHA FUNCIONAL completa do servidor (dados pessoais + vinculo/cargo +
        // timeline do vinculo + dependentes + historico de folhas e ponto). CPF mascarado (LGPD).
        grupo.MapGet("/servidores/{servidorId:guid}/ficha-funcional", async (
            Guid servidorId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterFichaFuncionalQuery(servidorId), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        // P0-1: vincula pensao alimenticia judicial (percentual ou valor fixo) — deduz IRRF + desconto/repasse.
        grupo.MapPost("/servidores/{servidorId:guid}/pensao-alimenticia", async (
            Guid servidorId, PensaoAlimenticiaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AdicionarPensaoAlimenticiaCommand(
                servidorId,
                payload.Beneficiario,
                payload.Modalidade,
                payload.Percentual,
                payload.BaseIncidencia,
                payload.ValorFixo,
                payload.ProcessoJudicial), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/servidores/por-matricula/{matricula}", async (
            string matricula, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterServidorPorMatriculaQuery(matricula), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapPost("/servidores/{servidorId:guid}/posse", async (
            Guid servidorId, PossePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarPosseCommand(servidorId, payload.DataPosse), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/servidores/{servidorId:guid}/exercicio", async (
            Guid servidorId, ExercicioPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new IniciarExercicioCommand(servidorId, payload.DataExercicio), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/servidores/{servidorId:guid}/estabilidade", async (
            Guid servidorId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConcederEstabilidadeCommand(servidorId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // AFASTAMENTOS TIPADOS (Onda 1): o usuario escolhe o TIPO; o efeito na folha (suspende/reduz/
        // proporcionaliza, conta tempo) vem da regra do tipo (parametrizada por tenant). Retorna o id do
        // afastamento criado para encerramento/cancelamento posteriores.
        grupo.MapPost("/servidores/{servidorId:guid}/afastamentos", async (
            Guid servidorId, RegistrarAfastamentoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                id = await sender.Send(
                    new RegistrarAfastamentoCommand(servidorId, payload.Tipo, payload.Inicio, payload.FimPrevisto, payload.Documento),
                    cancellationToken),
            }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Lista o historico de afastamentos do servidor (ficha de afastamentos).
        grupo.MapGet("/servidores/{servidorId:guid}/afastamentos", async (
            Guid servidorId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAfastamentosDoServidorQuery(servidorId), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        // Encerra um afastamento vigente (retorno do servidor); grava o fim efetivo.
        grupo.MapPost("/afastamentos/{afastamentoId:guid}/encerramento", async (
            Guid afastamentoId, EncerrarAfastamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarAfastamentoCommand(afastamentoId, payload.FimEfetivo), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Cancela um afastamento vigente lancado por engano/revogado (sem efeito na folha).
        grupo.MapPost("/afastamentos/{afastamentoId:guid}/cancelamento", async (
            Guid afastamentoId, CancelarAfastamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarAfastamentoCommand(afastamentoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Busca paginada de afastamentos por tipo/situacao/competencia (navegabilidade).
        grupo.MapGet("/afastamentos", async (
            TipoAfastamento? tipo, SituacaoAfastamento? situacao, int? ano, int? mes,
            int? pagina, int? tamanho, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new BuscarAfastamentosQuery(tipo, situacao, ano, mes, pagina, tamanho), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapPost("/servidores/{servidorId:guid}/desligamento", async (
            Guid servidorId, DesligamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DesligarServidorCommand(servidorId, payload.DataDesligamento, payload.Motivo), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearCargos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/cargos", async (
            CriarCargoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/cargos/{cargoId:guid}", async (
            Guid cargoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterCargoPorIdQuery(cargoId), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapGet("/cargos/com-vagas", async (
            TipoCargo? tipo, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarCargosComVagasQuery(tipo), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapPost("/cargos/{cargoId:guid}/provimento", async (
            Guid cargoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ProverCargoCommand(cargoId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/cargos/{cargoId:guid}/vacancia", async (
            Guid cargoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new VagarCargoCommand(cargoId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/cargos/{cargoId:guid}/vencimento", async (
            Guid cargoId, AlterarVencimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AlterarVencimentoCommand(cargoId, payload.NovoVencimento), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/cargos/{cargoId:guid}/extincao", async (
            Guid cargoId, ExtinguirCargoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ExtinguirCargoCommand(cargoId, payload.LeiExtincao), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private sealed record PossePayload(DateOnly DataPosse);

    private sealed record ExercicioPayload(DateOnly DataExercicio);

    private sealed record RegistrarAfastamentoPayload(
        TipoAfastamento Tipo,
        DateOnly Inicio,
        DateOnly? FimPrevisto,
        string? Documento);

    private sealed record EncerrarAfastamentoPayload(DateOnly FimEfetivo);

    private sealed record CancelarAfastamentoPayload(string Motivo);

    private sealed record DesligamentoPayload(DateOnly DataDesligamento, string Motivo);

    private sealed record AlterarVencimentoPayload(decimal NovoVencimento);

    private sealed record ExtinguirCargoPayload(string LeiExtincao);

    private sealed record PensaoAlimenticiaPayload(
        string Beneficiario,
        ModalidadePensao Modalidade,
        decimal Percentual,
        BasePensao BaseIncidencia,
        decimal ValorFixo,
        string ProcessoJudicial);
}

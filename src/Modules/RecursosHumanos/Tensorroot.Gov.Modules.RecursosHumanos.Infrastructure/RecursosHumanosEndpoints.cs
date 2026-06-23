using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo RecursosHumanos.</summary>
internal static class RecursosHumanosEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/recursoshumanos").WithTags("RecursosHumanos");

        MapearServidores(grupo);
        MapearCargos(grupo);
        MapearRubricas(grupo);
        MapearTabelasLegais(grupo);
        MapearFolha(grupo);
        MapearCicloAnual(grupo);
        MapearPonto(grupo);
        MapearESocial(grupo);
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

        grupo.MapPost("/servidores/{servidorId:guid}/afastamento", async (
            Guid servidorId, AfastamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAfastamentoCommand(servidorId, payload.Inicio, payload.Fim, payload.Motivo), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

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

    private static void MapearFolha(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/folhas", async (
            AbrirFolhaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/folhas/por-competencia", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterFolhaPorCompetenciaQuery(ano, mes), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapPost("/folhas/{folhaId:guid}/eventos", async (
            Guid folhaId, EventoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AdicionarEventoCommand(
                folhaId, payload.ServidorId, payload.Rubrica, payload.Tipo, payload.BaseCalculo, payload.Valor), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Apura INSS/RPPS/IRRF por servidor (motor + tabelas parametrizadas) com a folha ainda aberta.
        grupo.MapPost("/folhas/{folhaId:guid}/apuracao-legal", async (
            Guid folhaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ApurarDescontosLegaisCommand(folhaId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/folhas/{folhaId:guid}/calculo", async (
            Guid folhaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CalcularFolhaCommand(folhaId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/folhas/{folhaId:guid}/fechamento", async (
            Guid folhaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new FecharFolhaCommand(folhaId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/folhas/{folhaId:guid}/pagamento", async (
            Guid folhaId, PagamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EfetuarPagamentoCommand(folhaId, payload.DataPagamento), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/folhas/{folhaId:guid}/servidores/{servidorId:guid}/contracheque", async (
            Guid folhaId, Guid servidorId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterContrachequeDoServidorQuery(folhaId, servidorId), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");
    }

    private sealed record PossePayload(DateOnly DataPosse);

    private sealed record ExercicioPayload(DateOnly DataExercicio);

    private sealed record AfastamentoPayload(DateOnly Inicio, DateOnly? Fim, string Motivo);

    private sealed record DesligamentoPayload(DateOnly DataDesligamento, string Motivo);

    private sealed record AlterarVencimentoPayload(decimal NovoVencimento);

    private sealed record ExtinguirCargoPayload(string LeiExtincao);

    private sealed record EventoPayload(Guid ServidorId, string Rubrica, TipoEvento Tipo, decimal BaseCalculo, decimal Valor);

    private sealed record PagamentoPayload(DateOnly DataPagamento);
}

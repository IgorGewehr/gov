using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Saude.Application.Agendamento;
using Tensorroot.Gov.Modules.Saude.Application.Atendimento;
using Tensorroot.Gov.Modules.Saude.Application.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Application.Fiscal;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Application.Profissionais;
using Tensorroot.Gov.Modules.Saude.Application.Regulacao;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo Saude.</summary>
internal static class SaudeEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/saude").WithTags("Saude");

        MapearPacientes(grupo);
        MapearEstabelecimentos(grupo);
        MapearProfissionais(grupo);
        MapearAtendimentos(grupo);
        MapearRegulacao(grupo);
        MapearAgendamento(grupo);
        MapearFiscal(grupo);
    }

    private static void MapearAgendamento(RouteGroupBuilder grupo)
    {
        // Agenda do profissional/UBS (grade → vagas).
        grupo.MapPost("/agendas", async (
            AbrirAgendaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.agenda.gerenciar");

        grupo.MapGet("/agendas/{agendaId:guid}", async (
            Guid agendaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterAgendaPorIdQuery(agendaId), cancellationToken))).RequirePermission("saude.agenda.ver");

        grupo.MapPost("/agendas/{agendaId:guid}/publicacao", async (
            Guid agendaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PublicarAgendaCommand(agendaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.gerenciar");

        grupo.MapPost("/agendas/{agendaId:guid}/bloqueio", async (
            Guid agendaId, MotivoAgendaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new BloquearDiaAgendaCommand(agendaId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.gerenciar");

        grupo.MapPost("/agendas/{agendaId:guid}/reabertura", async (
            Guid agendaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReabrirDiaAgendaCommand(agendaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.gerenciar");

        grupo.MapGet("/agendas/vagas", async (
            Guid? profissional, Guid? estabelecimento, DateOnly? de, DateOnly? ate, TipoAtendimentoAgenda? tipo,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarVagasLivresQuery(profissional, estabelecimento, de, ate, tipo), cancellationToken)))
            .RequirePermission("saude.agenda.ver");

        // Agendamentos (marcar/confirmar/cancelar/falta/realizar).
        grupo.MapPost("/agendamentos", async (
            MarcarAgendamentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.agenda.marcar");

        grupo.MapGet("/agendamentos", async (
            Guid? paciente, Guid? profissional, DateOnly? data, SituacaoAgendamento? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarAgendamentosQuery(paciente, profissional, data, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("saude.agenda.ver");

        grupo.MapPost("/agendamentos/{agendamentoId:guid}/confirmacao", async (
            Guid agendamentoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConfirmarAgendamentoCommand(agendamentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.marcar");

        grupo.MapPost("/agendamentos/{agendamentoId:guid}/cancelamento", async (
            Guid agendamentoId, CancelarAgendamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarAgendamentoCommand(agendamentoId, payload.Motivo, payload.Origem), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.marcar");

        grupo.MapPost("/agendamentos/{agendamentoId:guid}/falta", async (
            Guid agendamentoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarFaltaCommand(agendamentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.marcar");

        grupo.MapPost("/agendamentos/{agendamentoId:guid}/realizacao", async (
            Guid agendamentoId, RealizarAgendamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RealizarAgendamentoCommand(agendamentoId, payload.AtendimentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.marcar");

        // Fila de espera (entrar/listar/convocar/remover).
        grupo.MapPost("/fila-espera", async (
            EntrarNaFilaDeEsperaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.agenda.marcar");

        grupo.MapGet("/fila-espera", async (
            Guid? estabelecimento, SituacaoFilaEspera? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarFilaDeEsperaQuery(estabelecimento, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("saude.agenda.ver");

        grupo.MapPost("/fila-espera/{filaId:guid}/convocacao", async (
            Guid filaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConvocarDaFilaDeEsperaCommand(filaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.marcar");

        grupo.MapPost("/fila-espera/{filaId:guid}/remocao", async (
            Guid filaId, MotivoAgendaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RemoverDaFilaDeEsperaCommand(filaId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.agenda.marcar");
    }

    private static void MapearEstabelecimentos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/estabelecimentos", async (
            CadastrarEstabelecimentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        grupo.MapGet("/estabelecimentos", async (
            string? termo, TipoEstabelecimento? tipo, SituacaoEstabelecimento? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarEstabelecimentosQuery(termo, tipo, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("saude.ver");

        grupo.MapGet("/estabelecimentos/{estabelecimentoId:guid}", async (
            Guid estabelecimentoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterEstabelecimentoPorIdQuery(estabelecimentoId), cancellationToken))).RequirePermission("saude.ver");

        grupo.MapPut("/estabelecimentos/{estabelecimentoId:guid}", async (
            Guid estabelecimentoId, AtualizarEstabelecimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarEstabelecimentoCommand(estabelecimentoId, payload.Nome, payload.Tipo, payload.Endereco), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/estabelecimentos/{estabelecimentoId:guid}/inativacao", async (
            Guid estabelecimentoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new InativarEstabelecimentoCommand(estabelecimentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/estabelecimentos/{estabelecimentoId:guid}/reativacao", async (
            Guid estabelecimentoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReativarEstabelecimentoCommand(estabelecimentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private static void MapearProfissionais(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/profissionais", async (
            CadastrarProfissionalCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        grupo.MapGet("/profissionais", async (
            string? termo, string? cbo, Guid? estabelecimentoId, SituacaoProfissional? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarProfissionaisQuery(termo, cbo, estabelecimentoId, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("saude.ver");

        grupo.MapGet("/profissionais/{profissionalId:guid}", async (
            Guid profissionalId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterProfissionalPorIdQuery(profissionalId), cancellationToken))).RequirePermission("saude.ver");

        grupo.MapPost("/profissionais/{profissionalId:guid}/vinculos", async (
            Guid profissionalId, VincularProfissionalPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new VincularProfissionalCommand(profissionalId, payload.EstabelecimentoId, payload.Cbo, payload.DataInicio), cancellationToken) }))
            .RequirePermission("saude.gerenciar");

        grupo.MapPost("/profissionais/{profissionalId:guid}/vinculos/encerramento", async (
            Guid profissionalId, EncerrarVinculoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarVinculoProfissionalCommand(profissionalId, payload.EstabelecimentoId, payload.DataFim), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/profissionais/{profissionalId:guid}/inativacao", async (
            Guid profissionalId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new InativarProfissionalCommand(profissionalId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private static void MapearFiscal(RouteGroupBuilder grupo)
    {
        // S-1 (Via A2): projeta linhas de execucao fiscal de Saude (receita-base + despesas por
        // funcao/subfuncao/fonte) no read model, idempotente por OrigemHash. Alimentador da apuracao ASPS.
        grupo.MapPost("/fiscal/execucao", async (
            RegistrarExecucaoSaudeCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { registradas = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        // S-1: apuracao do minimo de 15% ASPS (LC 141/2012) por exercicio.
        grupo.MapGet("/fiscal/asps/{exercicio:int}", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ApurarAspsQuery(exercicio), cancellationToken))).RequirePermission("saude.ver");

        // S-2: abrir a unidade gestora do Fundo Municipal de Saude.
        grupo.MapPost("/fiscal/fms", async (
            AbrirFundoMunicipalSaudeCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        // S-2: execucao segregada por bloco do FMS.
        grupo.MapGet("/fiscal/fms/{fundoId:guid}/execucao", async (
            Guid fundoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterExecucaoFmsQuery(fundoId), cancellationToken))).RequirePermission("saude.ver");

        // S-2: receber parcela do FNS num bloco/fonte (Custeio/Investimento, Port. 3.992/2017).
        grupo.MapPost("/fiscal/fms/{fundoId:guid}/parcelas", async (
            Guid fundoId, ParcelaFmsPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReceberParcelaFnsCommand(fundoId, payload.Bloco, payload.FonteRecurso, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        // S-2: executar despesa num bloco/fonte (transposicao entre blocos e vedada).
        grupo.MapPost("/fiscal/fms/{fundoId:guid}/execucoes", async (
            Guid fundoId, ParcelaFmsPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ExecutarDespesaBlocoCommand(fundoId, payload.Bloco, payload.FonteRecurso, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private static void MapearPacientes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/pacientes", async (
            CadastrarPacienteCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        // NAVEGABILIDADE (Onda 0): lista/busca paginada de pacientes por nome/CNS/CPF, filtro situacao.
        // PII sensivel (LGPD): exige o verbo FINO e GERA TRILHA DE ACESSO (a query e ISensivelLgpd).
        grupo.MapGet("/pacientes", async (
            string? termo, SituacaoPaciente? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarPacientesQuery(termo, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("saude.prontuario.ler");

        // LG-A3: leitura de conteudo clinico identificavel exige o verbo FINO (separado de "saude.ver").
        grupo.MapGet("/pacientes/por-cns/{cns}", async (
            string cns, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPacientePorCnsQuery(cns), cancellationToken))).RequirePermission("saude.prontuario.ler");

        grupo.MapGet("/pacientes/{pacienteId:guid}/historico-clinico", async (
            Guid pacienteId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterHistoricoClinicoDoPacienteQuery(pacienteId), cancellationToken))).RequirePermission("saude.prontuario.ler");

        grupo.MapPut("/pacientes/{pacienteId:guid}", async (
            Guid pacienteId, AtualizarCadastroPacientePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarCadastroPacienteCommand(pacienteId, payload.Identificacao, payload.Endereco), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/pacientes/{pacienteId:guid}/confirmacao-cadsus", async (
            Guid pacienteId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConfirmarCadastroNoCadsusCommand(pacienteId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/pacientes/{pacienteId:guid}/condicoes", async (
            Guid pacienteId, RegistrarCondicaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarCondicaoDeSaudeCommand(pacienteId, payload.Codigo, payload.Descricao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/pacientes/{pacienteId:guid}/alergias", async (
            Guid pacienteId, RegistrarAlergiaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAlergiaCommand(pacienteId, payload.Substancia, payload.Gravidade), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/pacientes/{pacienteId:guid}/inativacao", async (
            Guid pacienteId, InativarPacientePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new InativarPacienteCommand(pacienteId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private static void MapearAtendimentos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/atendimentos", async (
            RegistrarAtendimentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        // LG-A3: o detalhe e o historico de atendimentos contem conteudo clinico (SOAP/CID) — verbo fino.
        grupo.MapGet("/atendimentos/{atendimentoId:guid}", async (
            Guid atendimentoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterAtendimentoPorIdQuery(atendimentoId), cancellationToken))).RequirePermission("saude.prontuario.ler");

        grupo.MapGet("/pacientes/{pacienteId:guid}/atendimentos", async (
            Guid pacienteId, DateOnly? de, DateOnly? ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAtendimentosDoPacienteQuery(pacienteId, de, ate), cancellationToken))).RequirePermission("saude.prontuario.ler");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/evolucoes", async (
            Guid atendimentoId, AdicionarEvolucaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AdicionarEvolucaoSOAPCommand(
                atendimentoId, payload.Subjetivo, payload.Objetivo, payload.Avaliacao, payload.Plano, payload.Cid, payload.Ciap), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/assinatura", async (
            Guid atendimentoId, AssinarAtendimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AssinarAtendimentoCommand(atendimentoId, payload.CertificadoIcpBrasil, payload.Hash), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/adendos", async (
            Guid atendimentoId, AdicionarAdendoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AdicionarAdendoCommand(
                atendimentoId, payload.EvolucaoReferenciadaId, payload.Texto, payload.CertificadoIcpBrasil, payload.Hash), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/rnds", async (
            Guid atendimentoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CompartilharAtendimentoNaRNDSCommand(atendimentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/sisab", async (
            Guid atendimentoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new LancarAtendimentoNoSISABCommand(atendimentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/cancelamento", async (
            Guid atendimentoId, CancelarAtendimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarAtendimentoCommand(atendimentoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private static void MapearRegulacao(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/regulacao/solicitacoes", async (
            SolicitarRegulacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        grupo.MapGet("/regulacao/solicitacoes/{solicitacaoId:guid}", async (
            Guid solicitacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterSolicitacaoRegulacaoPorIdQuery(solicitacaoId), cancellationToken))).RequirePermission("saude.ver");

        grupo.MapGet("/regulacao/fila", async (
            string? codigoSigtap, Prioridade? prioridade, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarFilaDeRegulacaoQuery(codigoSigtap, prioridade), cancellationToken))).RequirePermission("saude.ver");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/autorizacao", async (
            Guid solicitacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AutorizarSolicitacaoRegulacaoCommand(solicitacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/negativa", async (
            Guid solicitacaoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new NegarSolicitacaoRegulacaoCommand(solicitacaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/devolucao", async (
            Guid solicitacaoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DevolverSolicitacaoRegulacaoCommand(solicitacaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/execucao", async (
            Guid solicitacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ExecutarSolicitacaoRegulacaoCommand(solicitacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/cancelamento", async (
            Guid solicitacaoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarSolicitacaoRegulacaoCommand(solicitacaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private sealed record AtualizarCadastroPacientePayload(IdentificacaoDto Identificacao, EnderecoDto Endereco);

    private sealed record RegistrarCondicaoPayload(string Codigo, string Descricao);

    private sealed record RegistrarAlergiaPayload(string Substancia, string Gravidade);

    private sealed record InativarPacientePayload(string Motivo);

    private sealed record AdicionarEvolucaoPayload(
        string Subjetivo, string Objetivo, string Avaliacao, string Plano, string? Cid, string? Ciap);

    private sealed record AssinarAtendimentoPayload(string CertificadoIcpBrasil, string Hash);

    private sealed record AdicionarAdendoPayload(Guid EvolucaoReferenciadaId, string Texto, string CertificadoIcpBrasil, string Hash);

    private sealed record CancelarAtendimentoPayload(string Motivo);

    private sealed record MotivoPayload(string Motivo);

    private sealed record ParcelaFmsPayload(BlocoFinanciamentoSaude Bloco, string FonteRecurso, decimal Valor);

    private sealed record AtualizarEstabelecimentoPayload(string Nome, TipoEstabelecimento Tipo, EnderecoEstabelecimentoDto Endereco);

    private sealed record VincularProfissionalPayload(Guid EstabelecimentoId, string Cbo, DateOnly DataInicio);

    private sealed record EncerrarVinculoPayload(Guid EstabelecimentoId, DateOnly DataFim);

    private sealed record MotivoAgendaPayload(string Motivo);

    private sealed record CancelarAgendamentoPayload(string Motivo, OrigemCancelamento Origem);

    private sealed record RealizarAgendamentoPayload(Guid? AtendimentoId);
}

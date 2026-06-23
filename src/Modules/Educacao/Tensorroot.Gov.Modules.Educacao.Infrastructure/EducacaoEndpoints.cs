using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Educacao.Application.Alunos;
using Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Application.Escolas;
using Tensorroot.Gov.Modules.Educacao.Application.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Application.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Application.Merenda;
using Tensorroot.Gov.Modules.Educacao.Application.Transporte;
using Tensorroot.Gov.Modules.Educacao.Application.Turmas;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo Educacao.</summary>
internal static class EducacaoEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/educacao").WithTags("Educacao");

        MapearEscolas(grupo);
        MapearAlunos(grupo);
        MapearTurmas(grupo);
        MapearMatriculas(grupo);
        MapearDiarios(grupo);
        MapearFiscal(grupo);
        MapearMerenda(grupo);
        MapearTransporte(grupo);
    }

    private static void MapearAlunos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/alunos", async (
            CadastrarAlunoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPut("/alunos/{alunoId:guid}", async (
            Guid alunoId, AtualizarAlunoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarAlunoCommand(alunoId, payload.DadosCivis, payload.Endereco), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/alunos/{alunoId:guid}/responsaveis", async (
            Guid alunoId, ResponsavelPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Created($"/api/educacao/alunos/{alunoId}/responsaveis",
                new { id = await sender.Send(new AdicionarResponsavelCommand(alunoId, payload), cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/alunos/{alunoId:guid}/inativacao", async (
            Guid alunoId, InativarAlunoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new InativarAlunoCommand(alunoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapGet("/alunos", async (
            string? termo, SituacaoAluno? situacao, int? pagina, int? tamanho, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarAlunosQuery(termo, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapGet("/alunos/{alunoId:guid}", async (
            Guid alunoId, ISender sender, CancellationToken cancellationToken) =>
        {
            var ficha = await sender.Send(new ObterAlunoQuery(alunoId), cancellationToken);
            return ficha is null ? Results.NotFound() : Results.Ok(ficha);
        }).RequirePermission("educacao.ver");
    }

    private static void MapearTurmas(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/turmas", async (
            CriarTurmaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/turmas/{turmaId:guid}/abertura", async (
            Guid turmaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AbrirTurmaCommand(turmaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/turmas/{turmaId:guid}/encerramento", async (
            Guid turmaId, EncerrarTurmaPayload? payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarTurmaCommand(turmaId, payload?.EncerramentoAnoLetivo ?? false), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPut("/turmas/{turmaId:guid}/vagas", async (
            Guid turmaId, AjustarVagasPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AjustarVagasCommand(turmaId, payload.Vagas), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapGet("/turmas", async (
            Guid? escolaId, int? anoLetivo, Turno? turno, Etapa? etapa, SituacaoTurma? situacao,
            int? pagina, int? tamanho, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new BuscarTurmasQuery(escolaId, anoLetivo, turno, etapa, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapGet("/turmas/{turmaId:guid}", async (
            Guid turmaId, ISender sender, CancellationToken cancellationToken) =>
        {
            var ficha = await sender.Send(new ObterTurmaQuery(turmaId), cancellationToken);
            return ficha is null ? Results.NotFound() : Results.Ok(ficha);
        }).RequirePermission("educacao.ver");
    }

    private static void MapearFiscal(RouteGroupBuilder grupo)
    {
        // E-1: alimenta a execucao fiscal (Via A2) e apura o minimo de 25% MDE (CF art. 212).
        grupo.MapPost("/fiscal/execucao", async (
            RegistrarExecucaoEducacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { registradas = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapGet("/fiscal/mde/{exercicio:int}", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ApurarMdeQuery(exercicio), cancellationToken)))
            .RequirePermission("educacao.ver");

        // E-3: distribuicao do FUNDEB (parcelas/conciliacao por origem VAAF/VAAT/VAAR).
        grupo.MapPost("/fiscal/fundeb", async (
            AbrirDistribuicaoFundebCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/fiscal/fundeb/{distribuicaoId:guid}/esperado", async (
            Guid distribuicaoId, DefinirEsperadoFundebPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DefinirEsperadoFundebCommand(distribuicaoId, payload.Origem, payload.ValorEsperado), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/fiscal/fundeb/{distribuicaoId:guid}/parcelas", async (
            Guid distribuicaoId, ReceberParcelaFundebPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReceberParcelaFundebCommand(distribuicaoId, payload.Origem, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        // E-2: cruzamento com a folha (remuneracao dos profissionais) e apuracao do piso de 70%.
        grupo.MapPost("/fiscal/fundeb/remuneracao", async (
            RegistrarRemuneracaoMagisterioCommand comando, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(comando, cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapGet("/fiscal/fundeb/{exercicio:int}/aplicacao", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ApurarFundeb70Query(exercicio), cancellationToken)))
            .RequirePermission("educacao.ver");
    }

    private static void MapearEscolas(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/escolas", async (
            CredenciarEscolaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapGet("/escolas", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarEscolasDaRedeQuery(), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapGet("/escolas/{codigoInep}", async (
            string codigoInep, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterEscolaPorCodigoInepQuery(codigoInep), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapPut("/escolas/{escolaId:guid}/dados-censo", async (
            Guid escolaId, AtualizarDadosCensoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarDadosCensoCommand(escolaId, payload.Endereco, payload.Infraestrutura), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/escolas/{escolaId:guid}/desativacao", async (
            Guid escolaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DesativarEscolaCommand(escolaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");
    }

    private static void MapearMatriculas(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/matriculas", async (
            MatricularAlunoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/matriculas/rematricula", async (
            RematricularAlunoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/matriculas/{matriculaId:guid}/transferencia", async (
            Guid matriculaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new TransferirAlunoCommand(matriculaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/matriculas/{matriculaId:guid}/encerramento", async (
            Guid matriculaId, EncerrarMatriculaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarMatriculaCommand(matriculaId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/matriculas/{matriculaId:guid}/situacao-aluno", async (
            Guid matriculaId, RegistrarSituacaoDoAlunoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarSituacaoDoAlunoCommand(matriculaId, payload.Rendimento, payload.Movimento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapGet("/alunos/{alunoId:guid}/matriculas", async (
            Guid alunoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterMatriculasDoAlunoQuery(alunoId), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapGet("/turmas/{turmaId:guid}/matricula-inicial", async (
            Guid turmaId, DateOnly dataReferencia, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarMatriculasInicialDaTurmaQuery(turmaId, dataReferencia), cancellationToken)))
            .RequirePermission("educacao.ver");
    }

    private static void MapearDiarios(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/diarios", async (
            AbrirDiarioClasseCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/diarios/{diarioId:guid}/frequencias", async (
            Guid diarioId, RegistrarFrequenciaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarFrequenciaCommand(diarioId, payload.Data, payload.Presente, payload.CargaHorariaAula), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/diarios/{diarioId:guid}/notas", async (
            Guid diarioId, LancarNotaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new LancarNotaCommand(diarioId, payload.ComponenteCurricularId, payload.Periodo, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/diarios/{diarioId:guid}/aulas", async (
            Guid diarioId, RegistrarAulaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAulaCommand(diarioId, payload.Data, payload.Conteudo, payload.DiaLetivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/diarios/{diarioId:guid}/apuracao", async (
            Guid diarioId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ApurarResultadoCommand(diarioId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapGet("/diarios/{diarioId:guid}/frequencia", async (
            Guid diarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterFrequenciaDoDiarioQuery(diarioId), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapGet("/matriculas/{matriculaId:guid}/diario", async (
            Guid matriculaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterDiarioDaMatriculaQuery(matriculaId), cancellationToken)))
            .RequirePermission("educacao.ver");

        MapearDiarioTurma(grupo);
    }

    private static void MapearDiarioTurma(RouteGroupBuilder grupo)
    {
        // Diario coletivo (sub-onda 3a): chamada e notas da turma inteira + boletim/historico.
        grupo.MapGet("/turmas/{turmaId:guid}/diario", async (
            Guid turmaId, DateOnly data, ISender sender, CancellationToken cancellationToken) =>
        {
            var view = await sender.Send(new ObterDiarioDaTurmaQuery(turmaId, data), cancellationToken);
            return view is null ? Results.NotFound() : Results.Ok(view);
        }).RequirePermission("educacao.ver");

        grupo.MapPost("/turmas/{turmaId:guid}/diario/frequencias", async (
            Guid turmaId, RegistrarFrequenciaTurmaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                lancados = await sender.Send(
                    new RegistrarFrequenciaTurmaCommand(turmaId, payload.Data, payload.CargaHorariaAula, payload.Presencas),
                    cancellationToken),
            }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/turmas/{turmaId:guid}/diario/notas", async (
            Guid turmaId, LancarNotasTurmaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                lancados = await sender.Send(
                    new LancarNotasTurmaCommand(turmaId, payload.ComponenteCurricularId, payload.Periodo, payload.Notas),
                    cancellationToken),
            }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapGet("/matriculas/{matriculaId:guid}/boletim", async (
            Guid matriculaId, ISender sender, CancellationToken cancellationToken) =>
        {
            var boletim = await sender.Send(new ObterBoletimQuery(matriculaId), cancellationToken);
            return boletim is null ? Results.NotFound() : Results.Ok(boletim);
        }).RequirePermission("educacao.ver");

        grupo.MapGet("/alunos/{alunoId:guid}/historico-escolar", async (
            Guid alunoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterHistoricoEscolarQuery(alunoId), cancellationToken)))
            .RequirePermission("educacao.ver");
    }

    private static void MapearMerenda(RouteGroupBuilder grupo)
    {
        // Merenda PNAE (sub-onda 3b): cardapio semanal + distribuicao/consumo de generos do estoque.
        var merenda = grupo.MapGroup("/merenda");

        merenda.MapPost("/cardapios", async (
            PlanejarCardapioCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        merenda.MapPost("/cardapios/{cardapioId:guid}/itens", async (
            Guid cardapioId, AdicionarItemCardapioPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                id = await sender.Send(
                    new AdicionarItemCardapioCommand(
                        cardapioId, payload.Dia, payload.Refeicao, payload.GeneroEstoqueId,
                        payload.QuantidadePerCapita, payload.UnidadeMedida),
                    cancellationToken),
            }))
            .RequirePermission("educacao.gerenciar");

        merenda.MapPost("/cardapios/{cardapioId:guid}/publicacao", async (
            Guid cardapioId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PublicarCardapioCommand(cardapioId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        merenda.MapGet("/cardapios", async (
            Guid? escolaId, DateOnly? semana, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarCardapiosQuery(escolaId, semana), cancellationToken)))
            .RequirePermission("educacao.ver");

        merenda.MapGet("/cardapios/{cardapioId:guid}", async (
            Guid cardapioId, ISender sender, CancellationToken cancellationToken) =>
        {
            var ficha = await sender.Send(new ObterCardapioQuery(cardapioId), cancellationToken);
            return ficha is null ? Results.NotFound() : Results.Ok(ficha);
        }).RequirePermission("educacao.ver");

        merenda.MapPost("/distribuicoes", async (
            RegistrarDistribuicaoMerendaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        merenda.MapGet("/consumo", async (
            Guid escolaId, DateOnly de, DateOnly ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterConsumoMerendaQuery(escolaId, de, ate), cancellationToken)))
            .RequirePermission("educacao.ver");
    }

    private static void MapearTransporte(RouteGroupBuilder grupo)
    {
        // Transporte PNATE (sub-onda 3b): rotas + alunos transportados (reusa Veiculo/Aluno por Id).
        var transporte = grupo.MapGroup("/transporte");

        transporte.MapPost("/rotas", async (
            CriarRotaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        transporte.MapGet("/rotas", async (
            Guid? escolaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterRotasPorEscolaQuery(escolaId), cancellationToken)))
            .RequirePermission("educacao.ver");

        transporte.MapPost("/rotas/{rotaId:guid}/alunos", async (
            Guid rotaId, VincularAlunoRotaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                id = await sender.Send(
                    new VincularAlunoRotaCommand(rotaId, payload.AlunoId, payload.MatriculaId, payload.PontoEmbarque),
                    cancellationToken),
            }))
            .RequirePermission("educacao.gerenciar");

        transporte.MapGet("/rotas/{rotaId:guid}/alunos", async (
            Guid rotaId, ISender sender, CancellationToken cancellationToken) =>
        {
            var ficha = await sender.Send(new ListarAlunosDaRotaQuery(rotaId), cancellationToken);
            return ficha is null ? Results.NotFound() : Results.Ok(ficha);
        }).RequirePermission("educacao.ver");

        transporte.MapPost("/rotas/{rotaId:guid}/alunos/{alunoTransportadoId:guid}/desligamento", async (
            Guid rotaId, Guid alunoTransportadoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DesligarAlunoRotaCommand(rotaId, alunoTransportadoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        transporte.MapPost("/rotas/{rotaId:guid}/ativacao", async (
            Guid rotaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtivarRotaCommand(rotaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        transporte.MapPost("/rotas/{rotaId:guid}/encerramento", async (
            Guid rotaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarRotaCommand(rotaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");
    }

    private sealed record AdicionarItemCardapioPayload(
        DiaSemanaCardapio Dia,
        TipoRefeicao Refeicao,
        Guid GeneroEstoqueId,
        decimal QuantidadePerCapita,
        string UnidadeMedida);

    private sealed record VincularAlunoRotaPayload(Guid AlunoId, Guid? MatriculaId, string PontoEmbarque);

    private sealed record AtualizarDadosCensoPayload(
        Domain.ValueObjects.Endereco Endereco,
        Domain.ValueObjects.Infraestrutura Infraestrutura);

    private sealed record EncerrarMatriculaPayload(MotivoEncerramento Motivo);

    private sealed record RegistrarSituacaoDoAlunoPayload(Rendimento Rendimento, Movimento Movimento);

    private sealed record RegistrarFrequenciaPayload(DateOnly Data, bool Presente, int CargaHorariaAula);

    private sealed record LancarNotaPayload(Guid ComponenteCurricularId, string Periodo, decimal Valor);

    private sealed record RegistrarAulaPayload(DateOnly Data, string Conteudo, bool DiaLetivo);

    private sealed record RegistrarFrequenciaTurmaPayload(
        DateOnly Data,
        int CargaHorariaAula,
        IReadOnlyList<FrequenciaAlunoLote> Presencas);

    private sealed record LancarNotasTurmaPayload(
        Guid ComponenteCurricularId,
        string Periodo,
        IReadOnlyList<NotaAlunoLote> Notas);

    private sealed record AtualizarAlunoPayload(DadosCivisPayload DadosCivis, EnderecoAlunoPayload Endereco);

    private sealed record InativarAlunoPayload(string Motivo);

    private sealed record EncerrarTurmaPayload(bool EncerramentoAnoLetivo);

    private sealed record AjustarVagasPayload(int Vagas);

    private sealed record DefinirEsperadoFundebPayload(OrigemRecursoFundeb Origem, decimal ValorEsperado);

    private sealed record ReceberParcelaFundebPayload(OrigemRecursoFundeb Origem, decimal Valor);
}

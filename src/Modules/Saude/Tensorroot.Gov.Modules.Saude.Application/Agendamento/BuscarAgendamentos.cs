using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Common;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using Tensorroot.Gov.SharedKernel;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>
/// Busca paginada de agendamentos por paciente/profissional/data/situacao. Como expoe o vinculo de um
/// paciente a um atendimento (dado de saude, LGPD art. 11), implementa <see cref="ISensivelLgpd"/> e
/// GERA TRILHA DE ACESSO (LG-3). Tenant-scoped via Global Query Filter; read-only.
/// </summary>
/// <param name="PacienteId">Filtro opcional por paciente.</param>
/// <param name="ProfissionalId">Filtro opcional por profissional.</param>
/// <param name="Data">Filtro opcional por data do atendimento.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarAgendamentosQuery(
    Guid? PacienteId,
    Guid? ProfissionalId,
    DateOnly? Data,
    SituacaoAgendamento? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<AgendamentoItemLista>>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "Agendamento";

    /// <inheritdoc />
    public string? EntidadeId => PacienteId?.ToString();

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.TutelaDaSaude;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisSaude.Aplicaveis;
}

/// <summary>Handler da busca paginada de agendamentos.</summary>
public sealed class BuscarAgendamentosHandler(IAgendamentoRepository agendamentos)
    : IQueryHandler<BuscarAgendamentosQuery, ResultadoPaginado<AgendamentoItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<AgendamentoItemLista>> Handle(
        BuscarAgendamentosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var pacienteId = request.PacienteId is { } p && p != Guid.Empty ? new PacienteId(p) : (PacienteId?)null;
        var profissionalId = request.ProfissionalId is { } pr && pr != Guid.Empty ? new ProfissionalId(pr) : (ProfissionalId?)null;

        var (itens, total) = await agendamentos
            .BuscarAsync(pacienteId, profissionalId, request.Data, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(agendamento => new AgendamentoItemLista(
                agendamento.Id.Value,
                agendamento.PacienteId.Value,
                agendamento.ProfissionalId.Value,
                agendamento.EstabelecimentoId.Value,
                agendamento.DataHora,
                agendamento.Tipo.ToString(),
                agendamento.Prioridade.ToString(),
                agendamento.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<AgendamentoItemLista>(projetados, total, pagina, tamanho);
    }
}

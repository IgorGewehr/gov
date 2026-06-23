using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>Obtem a ficha de uma grade (com a contagem de vagas). Tenant-scoped; read-only.</summary>
/// <param name="AgendaId">Identificador da agenda.</param>
public sealed record ObterAgendaPorIdQuery(Guid AgendaId) : IQuery<AgendaDetalhe>;

/// <summary>Handler da obtencao de agenda por id.</summary>
public sealed class ObterAgendaPorIdHandler(IAgendaProfissionalRepository agendas)
    : IQueryHandler<ObterAgendaPorIdQuery, AgendaDetalhe>
{
    /// <inheritdoc />
    public async Task<AgendaDetalhe> Handle(ObterAgendaPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agenda = await agendas.ObterPorIdAsync(new AgendaProfissionalId(request.AgendaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Agenda nao encontrada.");

        var livres = agenda.Vagas.Count(vaga => vaga.EstaLivre);
        return new AgendaDetalhe(
            agenda.Id.Value,
            agenda.ProfissionalId.Value,
            agenda.EstabelecimentoId.Value,
            agenda.Tipo.ToString(),
            agenda.Data,
            agenda.HoraInicio,
            agenda.HoraFim,
            agenda.DuracaoSlotMinutos,
            agenda.CapacidadeVagas,
            agenda.Situacao.ToString(),
            agenda.Vagas.Count,
            livres);
    }
}

/// <summary>Lista as vagas LIVRES por profissional/estabelecimento/tipo numa janela de datas. Tenant-scoped; read-only.</summary>
/// <param name="ProfissionalId">Filtro opcional por profissional.</param>
/// <param name="EstabelecimentoId">Filtro opcional por estabelecimento.</param>
/// <param name="De">Inicio da janela (inclusivo).</param>
/// <param name="Ate">Fim da janela (inclusivo).</param>
/// <param name="Tipo">Filtro opcional por natureza.</param>
public sealed record BuscarVagasLivresQuery(
    Guid? ProfissionalId,
    Guid? EstabelecimentoId,
    DateOnly? De,
    DateOnly? Ate,
    TipoAtendimentoAgenda? Tipo) : IQuery<IReadOnlyList<VagaLivreItem>>;

/// <summary>Handler da busca de vagas livres.</summary>
public sealed class BuscarVagasLivresHandler(IAgendaProfissionalRepository agendas)
    : IQueryHandler<BuscarVagasLivresQuery, IReadOnlyList<VagaLivreItem>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VagaLivreItem>> Handle(BuscarVagasLivresQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profissionalId = request.ProfissionalId is { } p && p != Guid.Empty ? new ProfissionalId(p) : (ProfissionalId?)null;
        var estabelecimentoId = request.EstabelecimentoId is { } e && e != Guid.Empty ? new EstabelecimentoId(e) : (EstabelecimentoId?)null;

        var vagas = await agendas
            .BuscarVagasLivresAsync(profissionalId, estabelecimentoId, request.De, request.Ate, request.Tipo, cancellationToken)
            .ConfigureAwait(false);

        return vagas
            .Select(vaga => new VagaLivreItem(
                vaga.AgendaId,
                vaga.VagaId,
                vaga.ProfissionalId,
                vaga.EstabelecimentoId,
                vaga.Tipo.ToString(),
                vaga.DataHora))
            .ToList();
    }
}

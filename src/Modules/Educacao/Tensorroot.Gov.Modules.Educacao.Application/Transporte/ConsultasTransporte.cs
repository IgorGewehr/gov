using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

namespace Tensorroot.Gov.Modules.Educacao.Application.Transporte;

/// <summary>Lista rotas de transporte por escola (picker do front). Tenant-scoped; read-only.</summary>
/// <param name="EscolaId">Filtro opcional por escola.</param>
public sealed record ObterRotasPorEscolaQuery(Guid? EscolaId) : IQuery<IReadOnlyList<RotaTransporteItemLista>>;

/// <summary>Handler da listagem de rotas por escola.</summary>
public sealed class ObterRotasPorEscolaHandler(IRotaTransporteRepository rotas)
    : IQueryHandler<ObterRotasPorEscolaQuery, IReadOnlyList<RotaTransporteItemLista>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RotaTransporteItemLista>> Handle(
        ObterRotasPorEscolaQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var escolaId = request.EscolaId is { } id ? new EscolaId(id) : (EscolaId?)null;
        var itens = await rotas.ListarPorEscolaAsync(escolaId, cancellationToken).ConfigureAwait(false);
        return itens.Select(TransporteMapeamento.ParaItemLista).ToList();
    }
}

/// <summary>Lista os alunos transportados de uma rota (ficha da rota). Tenant-scoped; read-only.</summary>
/// <param name="RotaId">Rota.</param>
public sealed record ListarAlunosDaRotaQuery(Guid RotaId) : IQuery<RotaTransporteDto?>;

/// <summary>Handler da listagem de alunos da rota.</summary>
public sealed class ListarAlunosDaRotaHandler(IRotaTransporteRepository rotas)
    : IQueryHandler<ListarAlunosDaRotaQuery, RotaTransporteDto?>
{
    /// <inheritdoc />
    public async Task<RotaTransporteDto?> Handle(ListarAlunosDaRotaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rota = await rotas.ObterPorIdAsync(new RotaTransporteId(request.RotaId), cancellationToken).ConfigureAwait(false);
        return rota is null ? null : TransporteMapeamento.ParaDto(rota);
    }
}

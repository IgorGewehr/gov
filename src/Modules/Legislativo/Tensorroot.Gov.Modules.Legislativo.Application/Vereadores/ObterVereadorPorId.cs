using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Vereadores;

/// <summary>Obtem o detalhe de um vereador (tenant-scoped).</summary>
/// <param name="VereadorId">Vereador a consultar.</param>
public sealed record ObterVereadorPorIdQuery(Guid VereadorId) : IQuery<VereadorResumo>;

/// <summary>Handler da consulta de detalhe do vereador.</summary>
public sealed class ObterVereadorPorIdHandler(IVereadorRepository vereadores)
    : IQueryHandler<ObterVereadorPorIdQuery, VereadorResumo>
{
    /// <inheritdoc />
    public async Task<VereadorResumo> Handle(ObterVereadorPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vereador = await vereadores.ObterPorIdAsync(new VereadorId(request.VereadorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Vereador nao encontrado.");

        return ListarVereadoresHandler.Mapear(vereador);
    }
}

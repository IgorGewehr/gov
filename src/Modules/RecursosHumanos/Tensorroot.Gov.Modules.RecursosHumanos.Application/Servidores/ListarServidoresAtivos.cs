using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Lista os servidores ativos do tenant (situacao diferente de <c>Desligado</c>; CPF mascarado — LGPD).</summary>
public sealed record ListarServidoresAtivosQuery : IQuery<IReadOnlyList<ServidorResumo>>;

/// <summary>Handler da consulta de servidores ativos.</summary>
public sealed class ListarServidoresAtivosHandler(IServidorRepository servidores)
    : IQueryHandler<ListarServidoresAtivosQuery, IReadOnlyList<ServidorResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ServidorResumo>> Handle(
        ListarServidoresAtivosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ativos = await servidores.ListarAtivosAsync(cancellationToken).ConfigureAwait(false);

        return ativos
            .Select(ProjetarServidor.ParaResumo)
            .ToList();
    }
}

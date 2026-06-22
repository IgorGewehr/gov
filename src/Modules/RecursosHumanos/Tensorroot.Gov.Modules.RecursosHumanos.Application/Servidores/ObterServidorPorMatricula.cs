using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Obtem o resumo de um servidor pela matricula (tenant-scoped; CPF mascarado — LGPD).</summary>
/// <param name="Matricula">Matricula do vinculo.</param>
public sealed record ObterServidorPorMatriculaQuery(string Matricula) : IQuery<ServidorResumo?>;

/// <summary>Handler da consulta de servidor por matricula.</summary>
public sealed class ObterServidorPorMatriculaHandler(IServidorRepository servidores)
    : IQueryHandler<ObterServidorPorMatriculaQuery, ServidorResumo?>
{
    /// <inheritdoc />
    public async Task<ServidorResumo?> Handle(ObterServidorPorMatriculaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores
            .ObterPorMatriculaAsync(Matricula.De(request.Matricula), cancellationToken)
            .ConfigureAwait(false);

        return servidor is null ? null : ProjetarServidor.ParaResumo(servidor);
    }
}

using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Turmas;

/// <summary>
/// Obtem a ficha de uma turma (capacidade, matriculados e vagas disponiveis). A lista nominal de
/// matriculados por data de referencia continua disponivel no endpoint de matricula inicial da turma.
/// </summary>
/// <param name="TurmaId">Identificador da turma.</param>
public sealed record ObterTurmaQuery(Guid TurmaId) : IQuery<TurmaFicha?>;

/// <summary>Handler da consulta da ficha de turma.</summary>
public sealed class ObterTurmaHandler(ITurmaRepository turmas)
    : IQueryHandler<ObterTurmaQuery, TurmaFicha?>
{
    /// <inheritdoc />
    public async Task<TurmaFicha?> Handle(ObterTurmaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var turma = await turmas.ObterPorIdAsync(new TurmaId(request.TurmaId), cancellationToken).ConfigureAwait(false);
        return turma is null ? null : TurmaMapeamento.ParaFicha(turma);
    }
}

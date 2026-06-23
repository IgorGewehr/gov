using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

namespace Tensorroot.Gov.Modules.Educacao.Application.Alunos;

/// <summary>Obtem a ficha completa de um aluno (dados civis + endereco + responsaveis).</summary>
/// <param name="AlunoId">Identificador do aluno.</param>
public sealed record ObterAlunoQuery(Guid AlunoId) : IQuery<AlunoFicha?>;

/// <summary>Handler da consulta da ficha do aluno.</summary>
public sealed class ObterAlunoHandler(IAlunoRepository alunos)
    : IQueryHandler<ObterAlunoQuery, AlunoFicha?>
{
    /// <inheritdoc />
    public async Task<AlunoFicha?> Handle(ObterAlunoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aluno = await alunos.ObterPorIdAsync(new AlunoId(request.AlunoId), cancellationToken).ConfigureAwait(false);
        return aluno is null ? null : AlunoMapeamento.ParaFicha(aluno);
    }
}

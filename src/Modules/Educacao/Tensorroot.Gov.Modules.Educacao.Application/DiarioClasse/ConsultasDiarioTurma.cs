using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

/// <summary>Obtem a grade de chamada do diario coletivo da turma em uma data (tenant-scoped).</summary>
/// <param name="TurmaId">Turma.</param>
/// <param name="Data">Data da chamada.</param>
public sealed record ObterDiarioDaTurmaQuery(Guid TurmaId, DateOnly Data) : IQuery<DiarioTurmaView?>;

/// <summary>Handler da consulta do diario coletivo da turma.</summary>
public sealed class ObterDiarioDaTurmaHandler(IDiarioTurmaReadModel readModel)
    : IQueryHandler<ObterDiarioDaTurmaQuery, DiarioTurmaView?>
{
    /// <inheritdoc />
    public Task<DiarioTurmaView?> Handle(ObterDiarioDaTurmaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return readModel.ObterDiarioDaTurmaAsync(request.TurmaId, request.Data, cancellationToken);
    }
}

/// <summary>Obtem o boletim de uma matricula (medias por componente, frequencia e resultado).</summary>
/// <param name="MatriculaId">Matricula.</param>
public sealed record ObterBoletimQuery(Guid MatriculaId) : IQuery<BoletimAlunoView?>;

/// <summary>Handler da consulta do boletim.</summary>
public sealed class ObterBoletimHandler(IDiarioTurmaReadModel readModel)
    : IQueryHandler<ObterBoletimQuery, BoletimAlunoView?>
{
    /// <inheritdoc />
    public Task<BoletimAlunoView?> Handle(ObterBoletimQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return readModel.ObterBoletimAsync(request.MatriculaId, cancellationToken);
    }
}

/// <summary>Obtem o historico escolar longitudinal do aluno (tenant-scoped).</summary>
/// <param name="AlunoId">Aluno.</param>
public sealed record ObterHistoricoEscolarQuery(Guid AlunoId) : IQuery<HistoricoEscolarView>;

/// <summary>Handler da consulta do historico escolar.</summary>
public sealed class ObterHistoricoEscolarHandler(IDiarioTurmaReadModel readModel)
    : IQueryHandler<ObterHistoricoEscolarQuery, HistoricoEscolarView>
{
    /// <inheritdoc />
    public Task<HistoricoEscolarView> Handle(ObterHistoricoEscolarQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return readModel.ObterHistoricoEscolarAsync(request.AlunoId, cancellationToken);
    }
}

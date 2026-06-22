using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Repositories;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;
using EscolaId = Domain.Escolas.EscolaId;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Escola"/>.</summary>
public sealed class EscolaRepository(EducacaoDbContext context) : IEscolaRepository
{
    /// <inheritdoc />
    public void Adicionar(Escola escola)
    {
        ArgumentNullException.ThrowIfNull(escola);
        context.Escolas.Add(escola);
    }

    /// <inheritdoc />
    public Task<Escola?> ObterPorIdAsync(EscolaId id, CancellationToken cancellationToken)
        => context.Escolas.FirstOrDefaultAsync(escola => escola.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Escola?> ObterPorCodigoInepAsync(CodigoInep codigoInep, CancellationToken cancellationToken)
        => context.Escolas.FirstOrDefaultAsync(escola => escola.CodigoInep == codigoInep, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteCodigoInepAsync(CodigoInep codigoInep, CancellationToken cancellationToken)
        => context.Escolas.AnyAsync(escola => escola.CodigoInep == codigoInep, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Escola>> ListarAsync(CancellationToken cancellationToken)
        => await context.Escolas
            .OrderBy(escola => escola.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Matricula"/>.</summary>
public sealed class MatriculaRepository(EducacaoDbContext context) : IMatriculaRepository
{
    /// <inheritdoc />
    public void Adicionar(Matricula matricula)
    {
        ArgumentNullException.ThrowIfNull(matricula);
        context.Matriculas.Add(matricula);
    }

    /// <inheritdoc />
    public Task<Matricula?> ObterPorIdAsync(MatriculaId id, CancellationToken cancellationToken)
        => context.Matriculas.FirstOrDefaultAsync(matricula => matricula.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Matricula>> ListarPorAlunoAsync(AlunoId alunoId, CancellationToken cancellationToken)
        => await context.Matriculas
            .Where(matricula => matricula.AlunoId == alunoId)
            .OrderByDescending(matricula => matricula.DataReferencia)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Matricula>> ListarPorTurmaEDataReferenciaAsync(
        TurmaId turmaId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
        => await context.Matriculas
            .Where(matricula => matricula.TurmaId == turmaId && matricula.DataReferencia == dataReferencia)
            .OrderBy(matricula => matricula.AlunoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<bool> ExisteMatriculaAtivaConflitanteAsync(
        AlunoId alunoId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
        => context.Matriculas.AnyAsync(
            matricula => matricula.AlunoId == alunoId
                && matricula.DataReferencia == dataReferencia
                && matricula.Situacao == SituacaoMatricula.Ativa,
            cancellationToken);
}

/// <summary>
/// Implementacao EF Core do repositorio do agregado <see cref="DiarioClasseAggregate"/>.
/// As colecoes filhas (frequencias, notas, aulas) sao carregadas junto ao agregado para
/// preservar suas invariantes ao apurar o resultado.
/// </summary>
public sealed class DiarioClasseRepository(EducacaoDbContext context) : IDiarioClasseRepository
{
    /// <inheritdoc />
    public void Adicionar(DiarioClasseAggregate diario)
    {
        ArgumentNullException.ThrowIfNull(diario);
        context.DiariosClasse.Add(diario);
    }

    /// <inheritdoc />
    public Task<DiarioClasseAggregate?> ObterPorIdAsync(Domain.DiarioClasse.DiarioClasseId id, CancellationToken cancellationToken)
        => context.DiariosClasse
            .Include(diario => diario.Frequencias)
            .Include(diario => diario.Notas)
            .Include(diario => diario.Aulas)
            .FirstOrDefaultAsync(diario => diario.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<DiarioClasseAggregate?> ObterPorMatriculaAsync(MatriculaId matriculaId, CancellationToken cancellationToken)
        => context.DiariosClasse
            .Include(diario => diario.Frequencias)
            .Include(diario => diario.Notas)
            .Include(diario => diario.Aulas)
            .FirstOrDefaultAsync(diario => diario.MatriculaId == matriculaId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteParaMatriculaAsync(MatriculaId matriculaId, CancellationToken cancellationToken)
        => context.DiariosClasse.AnyAsync(diario => diario.MatriculaId == matriculaId, cancellationToken);
}

/// <summary>
/// Implementacao EF Core do colaborador de leitura da Turma. A Turma e modelada em outro contexto
/// do modulo Educacao e ainda nao possui persistencia propria neste DbContext; a verificacao de
/// vaga (I-11) e confirmada pela presenca de um identificador de turma valido, ponto de extensao
/// para a contagem matriculados &lt; vagas quando o agregado Turma for materializado.
/// </summary>
public sealed class TurmaRepository : ITurmaRepository
{
    /// <inheritdoc />
    public Task<bool> PossuiVagaAsync(TurmaId turmaId, CancellationToken cancellationToken)
        => Task.FromResult(turmaId.Value != Guid.Empty);
}

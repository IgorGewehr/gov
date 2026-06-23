using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>
/// Implementacao EF Core do read model do diario coletivo, boletim e historico escolar (sub-onda 3a).
/// Projeta sobre os agregados ja existentes do modulo (Turma, Matricula, Aluno e o diario 1-1 por
/// matricula) com JOIN intra-modulo — sem entidade de dominio nova e sem tabela propria. Todas as
/// consultas sao read-only (AsNoTracking) e tenant-scoped via Global Query Filter do DbContext.
/// </summary>
public sealed class DiarioTurmaReadModel(EducacaoDbContext context) : IDiarioTurmaReadModel
{
    /// <inheritdoc />
    public async Task<DiarioTurmaView?> ObterDiarioDaTurmaAsync(Guid turmaId, DateOnly data, CancellationToken cancellationToken)
    {
        var id = new TurmaId(turmaId);
        var turma = await context.Turmas
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (turma is null)
        {
            return null;
        }

        // Matriculas Ativas da turma + nome do aluno (JOIN intra-modulo).
        var alunos = await (
            from matricula in context.Matriculas.AsNoTracking()
            where matricula.TurmaId == id && matricula.Situacao == SituacaoMatricula.Ativa
            join aluno in context.Alunos.AsNoTracking() on matricula.AlunoId equals aluno.Id
            orderby aluno.DadosCivis.Nome
            select new { MatriculaId = matricula.Id, AlunoId = aluno.Id, Nome = aluno.DadosCivis.Nome })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var matriculaIds = alunos.Select(a => a.MatriculaId).ToList();

        // Diarios 1-1 (com frequencias) dessas matriculas, carregados para calcular presenca do dia e %.
        var diarios = await context.DiariosClasse
            .AsNoTracking()
            .Include(diario => diario.Frequencias)
            .Where(diario => matriculaIds.Contains(diario.MatriculaId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var porMatricula = diarios.ToDictionary(diario => diario.MatriculaId.Value);

        var linhas = alunos.Select(aluno =>
        {
            porMatricula.TryGetValue(aluno.MatriculaId.Value, out var diario);
            bool? presente = diario?.Frequencias
                .Where(f => f.Data == data)
                .Select(f => (bool?)f.Presente)
                .LastOrDefault();

            return new LinhaDiarioTurma(
                aluno.MatriculaId.Value,
                aluno.AlunoId.Value,
                aluno.Nome,
                diario?.Id.Value,
                presente,
                diario?.PercentualFrequencia ?? 0m);
        }).ToList();

        return new DiarioTurmaView(
            turma.Id.Value,
            turma.EscolaId.Value,
            turma.AnoLetivo,
            turma.Serie,
            turma.Turno.ToString(),
            data,
            linhas);
    }

    /// <inheritdoc />
    public async Task<BoletimAlunoView?> ObterBoletimAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        var id = new MatriculaId(matriculaId);
        var matricula = await context.Matriculas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (matricula is null)
        {
            return null;
        }

        var diario = await context.DiariosClasse
            .AsNoTracking()
            .Include(d => d.Notas)
            .Include(d => d.Frequencias)
            .FirstOrDefaultAsync(d => d.MatriculaId == id, cancellationToken)
            .ConfigureAwait(false);

        var medias = diario is null
            ? []
            : ApurarMedias(diario);

        return new BoletimAlunoView(
            matricula.Id.Value,
            matricula.AlunoId.Value,
            matricula.TurmaId.Value,
            diario?.Id.Value,
            diario?.Situacao.ToString(),
            diario?.PercentualFrequencia ?? 0m,
            diario?.DiasLetivosRegistrados ?? 0,
            diario?.Resultado?.ToString(),
            medias);
    }

    /// <inheritdoc />
    public async Task<HistoricoEscolarView> ObterHistoricoEscolarAsync(Guid alunoId, CancellationToken cancellationToken)
    {
        var id = new AlunoId(alunoId);

        var matriculas = await context.Matriculas
            .AsNoTracking()
            .Where(m => m.AlunoId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (matriculas.Count == 0)
        {
            return new HistoricoEscolarView(alunoId, []);
        }

        var turmaIds = matriculas.Select(m => m.TurmaId).Distinct().ToList();
        var turmas = await context.Turmas
            .AsNoTracking()
            .Where(t => turmaIds.Contains(t.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var porTurma = turmas.ToDictionary(t => t.Id.Value);

        var matriculaIds = matriculas.Select(m => m.Id).ToList();
        var diarios = await context.DiariosClasse
            .AsNoTracking()
            .Include(d => d.Frequencias)
            .Where(d => matriculaIds.Contains(d.MatriculaId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var porMatricula = diarios.ToDictionary(d => d.MatriculaId.Value);

        var itens = matriculas
            .Select(matricula =>
            {
                porTurma.TryGetValue(matricula.TurmaId.Value, out var turma);
                porMatricula.TryGetValue(matricula.Id.Value, out var diario);

                return new ItemHistoricoEscolar(
                    matricula.Id.Value,
                    matricula.TurmaId.Value,
                    matricula.EscolaId.Value,
                    turma?.AnoLetivo ?? 0,
                    turma?.Serie ?? string.Empty,
                    matricula.Situacao.ToString(),
                    diario?.PercentualFrequencia,
                    diario?.Resultado?.ToString());
            })
            .OrderByDescending(item => item.AnoLetivo)
            .ThenBy(item => item.Serie)
            .ToList();

        return new HistoricoEscolarView(alunoId, itens);
    }

    private static List<MediaComponente> ApurarMedias(DiarioClasseAggregate diario)
        => diario.Notas
            .GroupBy(nota => nota.Componente.Value)
            .Select(grupo => new MediaComponente(
                grupo.Key,
                Math.Round(grupo.Average(nota => nota.Valor), 2, MidpointRounding.AwayFromZero),
                grupo.Count()))
            .OrderBy(media => media.ComponenteCurricularId)
            .ToList();
}

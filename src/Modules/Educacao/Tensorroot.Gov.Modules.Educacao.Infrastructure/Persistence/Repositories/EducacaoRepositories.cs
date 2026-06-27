using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.ValueObjects;

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
    public async Task<IReadOnlyList<Matricula>> ListarAtivasPorTurmaAsync(
        TurmaId turmaId,
        CancellationToken cancellationToken)
        => await context.Matriculas
            .Where(matricula => matricula.TurmaId == turmaId && matricula.Situacao == SituacaoMatricula.Ativa)
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
    public async Task<IReadOnlyList<DiarioClasseAggregate>> ListarPorMatriculasAsync(
        IReadOnlyCollection<MatriculaId> matriculaIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(matriculaIds);
        if (matriculaIds.Count == 0)
        {
            return [];
        }

        var ids = matriculaIds.ToArray();
        return await context.DiariosClasse
            .Include(diario => diario.Frequencias)
            .Include(diario => diario.Notas)
            .Include(diario => diario.Aulas)
            .Where(diario => ids.Contains(diario.MatriculaId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> ExisteParaMatriculaAsync(MatriculaId matriculaId, CancellationToken cancellationToken)
        => context.DiariosClasse.AnyAsync(diario => diario.MatriculaId == matriculaId, cancellationToken);
}

/// <summary>
/// Implementacao EF Core do repositorio do agregado <see cref="Turma"/>. A Turma passa a ser
/// materializada neste DbContext (Onda 1): a verificacao de vaga (I-T2) consulta o estado real do
/// agregado (situacao Aberta e matriculados &lt; vagas), substituindo o stub historico.
/// </summary>
public sealed class TurmaRepository(EducacaoDbContext context) : ITurmaRepository
{
    /// <inheritdoc />
    public void Adicionar(Turma turma)
    {
        ArgumentNullException.ThrowIfNull(turma);
        context.Turmas.Add(turma);
    }

    /// <inheritdoc />
    public Task<Turma?> ObterPorIdAsync(TurmaId id, CancellationToken cancellationToken)
        => context.Turmas.FirstOrDefaultAsync(turma => turma.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> PossuiVagaAsync(TurmaId turmaId, CancellationToken cancellationToken)
        => context.Turmas.AnyAsync(
            turma => turma.Id == turmaId
                && turma.Situacao == SituacaoTurma.Aberta
                && turma.Matriculados < turma.Vagas,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteDuplicadaAsync(
        EscolaId escolaId,
        int anoLetivo,
        string serie,
        Turno turno,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serie);
        var serieNormalizada = serie.Trim();
        return context.Turmas.AnyAsync(
            turma => turma.EscolaId == escolaId
                && turma.AnoLetivo == anoLetivo
                && turma.Serie == serieNormalizada
                && turma.Turno == turno,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Turma> Itens, int Total)> BuscarAsync(
        EscolaId? escolaId,
        int? anoLetivo,
        Turno? turno,
        Etapa? etapa,
        SituacaoTurma? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Turmas.AsNoTracking();

        if (escolaId is { } escola)
        {
            consulta = consulta.Where(turma => turma.EscolaId == escola);
        }

        if (anoLetivo is { } ano)
        {
            consulta = consulta.Where(turma => turma.AnoLetivo == ano);
        }

        if (turno is { } turnoFiltro)
        {
            consulta = consulta.Where(turma => turma.Turno == turnoFiltro);
        }

        if (etapa is { } etapaFiltro)
        {
            consulta = consulta.Where(turma => turma.Etapa == etapaFiltro);
        }

        if (situacao is { } situacaoFiltro)
        {
            consulta = consulta.Where(turma => turma.Situacao == situacaoFiltro);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(turma => turma.AnoLetivo)
            .ThenBy(turma => turma.Serie)
            .ThenBy(turma => turma.Turno)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

/// <summary>
/// Implementacao EF Core do repositorio do agregado <see cref="Aluno"/>. Sempre tenant-scoped via
/// Global Query Filter; carrega os responsaveis junto ao agregado para preservar suas invariantes.
/// </summary>
public sealed class AlunoRepository(EducacaoDbContext context) : IAlunoRepository
{
    /// <inheritdoc />
    public void Adicionar(Aluno aluno)
    {
        ArgumentNullException.ThrowIfNull(aluno);
        context.Alunos.Add(aluno);
    }

    /// <inheritdoc />
    public Task<Aluno?> ObterPorIdAsync(AlunoId id, CancellationToken cancellationToken)
        => context.Alunos
            .Include(aluno => aluno.Responsaveis)
            .FirstOrDefaultAsync(aluno => aluno.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteCpfAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        // Cpf e mapeado por value converter (VO -> string): a igualdade do VO inteiro e translatavel para SQL;
        // navegar em aluno.Cpf.Digitos NAO seria traduzivel (a propriedade nao vira coluna). Tenant-scoped pelo filtro global.
        return context.Alunos.AnyAsync(aluno => aluno.Cpf == cpf, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Aluno> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoAluno? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Alunos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNormalizado = termo.Trim();
            // Cpf e mapeado por value converter (VO -> string), nao fatiavel por LIKE/Contains (a propriedade
            // Digitos nao vira coluna): casa por igualdade exata do VO quando o termo for um CPF valido completo;
            // o nome casa por trecho.
            var digitos = new string(termoNormalizado.Where(char.IsDigit).ToArray());
            var cpfExato = Cpf.TryCreate(digitos, out var cpf) ? cpf : null;
            consulta = consulta.Where(aluno =>
                aluno.DadosCivis.Nome.Contains(termoNormalizado)
                || (cpfExato != null && aluno.Cpf == cpfExato));
        }

        if (situacao is { } situacaoFiltro)
        {
            consulta = consulta.Where(aluno => aluno.Situacao == situacaoFiltro);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(aluno => aluno.DadosCivis.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

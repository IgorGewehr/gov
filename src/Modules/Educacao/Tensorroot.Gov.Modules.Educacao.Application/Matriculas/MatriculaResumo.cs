namespace Tensorroot.Gov.Modules.Educacao.Application.Matriculas;

/// <summary>Projecao de leitura de uma matricula (base da Matricula Inicial do Censo).</summary>
/// <param name="Id">Identificador da matricula.</param>
/// <param name="AlunoId">Aluno vinculado.</param>
/// <param name="TurmaId">Turma de enturmacao.</param>
/// <param name="EscolaId">Escola da matricula.</param>
/// <param name="Situacao">Situacao atual (Ativa/Transferida/Concluida/Abandono).</param>
/// <param name="DataReferencia">Data de referencia do Censo (Matricula Inicial).</param>
public sealed record MatriculaResumo(
    Guid Id,
    Guid AlunoId,
    Guid TurmaId,
    Guid EscolaId,
    string Situacao,
    DateOnly DataReferencia);

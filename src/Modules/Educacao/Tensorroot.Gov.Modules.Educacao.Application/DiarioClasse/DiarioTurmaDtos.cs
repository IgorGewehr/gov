using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>
/// Projecao da linha de chamada de um aluno na grade do diario coletivo da turma (uma linha por
/// matricula ativa). Materializada por <see cref="IDiarioTurmaReadModel"/> a partir do diario 1-1
/// da matricula; quando ainda nao ha diario aberto, <see cref="DiarioId"/> e nulo (apto a abrir).
/// </summary>
/// <param name="MatriculaId">Matricula do aluno na turma.</param>
/// <param name="AlunoId">Aluno (FK logica ao agregado Aluno).</param>
/// <param name="NomeAluno">Nome civil do aluno (para a grade).</param>
/// <param name="DiarioId">Diario 1-1 da matricula, quando aberto; nulo se ainda nao existir.</param>
/// <param name="Presente">Presenca registrada na data consultada; nula se sem lancamento no dia.</param>
/// <param name="PercentualFrequencia">Percentual de frequencia acumulado do diario.</param>
public sealed record LinhaDiarioTurma(
    Guid MatriculaId,
    Guid AlunoId,
    string NomeAluno,
    Guid? DiarioId,
    bool? Presente,
    decimal PercentualFrequencia);

/// <summary>
/// Read model do diario coletivo da turma em uma data (chamada): cabecalho da turma + uma linha por
/// aluno ativo, com a presenca daquele dia. Tudo tenant-scoped via Global Query Filter.
/// </summary>
/// <param name="TurmaId">Turma consultada.</param>
/// <param name="EscolaId">Escola da turma.</param>
/// <param name="AnoLetivo">Ano letivo.</param>
/// <param name="Serie">Serie/ano.</param>
/// <param name="Turno">Turno.</param>
/// <param name="Data">Data da chamada consultada.</param>
/// <param name="Linhas">Linhas (alunos ativos) da grade de chamada.</param>
public sealed record DiarioTurmaView(
    Guid TurmaId,
    Guid EscolaId,
    int AnoLetivo,
    string Serie,
    string Turno,
    DateOnly Data,
    IReadOnlyList<LinhaDiarioTurma> Linhas);

/// <summary>Media de um componente curricular no boletim (agregando as notas lancadas no diario).</summary>
/// <param name="ComponenteCurricularId">Componente curricular (FK logica — catalogo e P3/M10).</param>
/// <param name="Media">Media aritmetica das notas lancadas do componente.</param>
/// <param name="QuantidadeNotas">Quantidade de notas que compoem a media.</param>
public sealed record MediaComponente(
    Guid ComponenteCurricularId,
    decimal Media,
    int QuantidadeNotas);

/// <summary>
/// Read model do boletim do aluno em um diario (uma matricula): medias por componente, percentual de
/// frequencia, dias letivos e o resultado apurado, quando houver. Tenant-scoped.
/// </summary>
/// <param name="MatriculaId">Matricula do boletim.</param>
/// <param name="AlunoId">Aluno.</param>
/// <param name="TurmaId">Turma.</param>
/// <param name="DiarioId">Diario 1-1 da matricula, quando aberto; nulo se ainda nao existir.</param>
/// <param name="Situacao">Situacao do diario (Aberto/Apurado); nula se sem diario.</param>
/// <param name="PercentualFrequencia">Percentual de frequencia acumulado.</param>
/// <param name="DiasLetivosRegistrados">Dias letivos registrados.</param>
/// <param name="Resultado">Resultado apurado, quando houver.</param>
/// <param name="Medias">Medias por componente curricular.</param>
public sealed record BoletimAlunoView(
    Guid MatriculaId,
    Guid AlunoId,
    Guid TurmaId,
    Guid? DiarioId,
    string? Situacao,
    decimal PercentualFrequencia,
    int DiasLetivosRegistrados,
    string? Resultado,
    IReadOnlyList<MediaComponente> Medias);

/// <summary>Item longitudinal do historico escolar (um por matricula/diario do aluno na rede).</summary>
/// <param name="MatriculaId">Matricula.</param>
/// <param name="TurmaId">Turma.</param>
/// <param name="EscolaId">Escola.</param>
/// <param name="AnoLetivo">Ano letivo da turma.</param>
/// <param name="Serie">Serie/ano.</param>
/// <param name="SituacaoMatricula">Situacao da matricula (Ativa/Transferida/Concluida/Abandono).</param>
/// <param name="PercentualFrequencia">Percentual de frequencia do diario, quando houver.</param>
/// <param name="Resultado">Resultado apurado do diario, quando houver.</param>
public sealed record ItemHistoricoEscolar(
    Guid MatriculaId,
    Guid TurmaId,
    Guid EscolaId,
    int AnoLetivo,
    string Serie,
    string SituacaoMatricula,
    decimal? PercentualFrequencia,
    string? Resultado);

/// <summary>
/// Read model do historico escolar longitudinal do aluno: a serie de matriculas/diarios ao longo dos
/// anos letivos, ordenada do mais recente ao mais antigo. Tenant-scoped.
/// </summary>
/// <param name="AlunoId">Aluno consultado.</param>
/// <param name="Itens">Itens do historico (uma matricula por ano letivo/turma).</param>
public sealed record HistoricoEscolarView(
    Guid AlunoId,
    IReadOnlyList<ItemHistoricoEscolar> Itens);

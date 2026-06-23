using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Turmas;

/// <summary>Item da lista/busca de turmas (picker do front — com vagas disponiveis).</summary>
/// <param name="Id">Identificador da turma.</param>
/// <param name="EscolaId">Escola da turma.</param>
/// <param name="AnoLetivo">Ano letivo.</param>
/// <param name="Etapa">Etapa/modalidade (descricao).</param>
/// <param name="Serie">Serie/ano.</param>
/// <param name="Turno">Turno (descricao).</param>
/// <param name="Vagas">Capacidade de vagas.</param>
/// <param name="Matriculados">Total de matriculados.</param>
/// <param name="VagasDisponiveis">Vagas ainda disponiveis.</param>
/// <param name="Situacao">Situacao atual (descricao).</param>
public sealed record TurmaItemLista(
    Guid Id,
    Guid EscolaId,
    int AnoLetivo,
    string Etapa,
    string Serie,
    string Turno,
    int Vagas,
    int Matriculados,
    int VagasDisponiveis,
    string Situacao);

/// <summary>Matriculado projetado na ficha da turma.</summary>
/// <param name="MatriculaId">Identificador da matricula.</param>
/// <param name="AlunoId">Aluno matriculado.</param>
/// <param name="DataReferencia">Data de referencia do Censo.</param>
/// <param name="Situacao">Situacao da matricula (descricao).</param>
public sealed record TurmaMatriculadoDto(
    Guid MatriculaId,
    Guid AlunoId,
    DateOnly DataReferencia,
    string Situacao);

/// <summary>Ficha da turma (+ lista de matriculados na data de referencia).</summary>
/// <param name="Id">Identificador da turma.</param>
/// <param name="EscolaId">Escola da turma.</param>
/// <param name="AnoLetivo">Ano letivo.</param>
/// <param name="Etapa">Etapa/modalidade (descricao).</param>
/// <param name="Serie">Serie/ano.</param>
/// <param name="Turno">Turno (descricao).</param>
/// <param name="Vagas">Capacidade de vagas.</param>
/// <param name="Matriculados">Total de matriculados (contador do agregado).</param>
/// <param name="VagasDisponiveis">Vagas ainda disponiveis.</param>
/// <param name="Situacao">Situacao atual (descricao).</param>
public sealed record TurmaFicha(
    Guid Id,
    Guid EscolaId,
    int AnoLetivo,
    string Etapa,
    string Serie,
    string Turno,
    int Vagas,
    int Matriculados,
    int VagasDisponiveis,
    string Situacao);

/// <summary>Mapeamentos do agregado <see cref="Turma"/> para os DTOs de leitura.</summary>
internal static class TurmaMapeamento
{
    /// <summary>Projeta a turma no item de lista.</summary>
    /// <param name="turma">Turma de origem.</param>
    /// <returns>Item de lista.</returns>
    public static TurmaItemLista ParaItemLista(Turma turma)
    {
        ArgumentNullException.ThrowIfNull(turma);
        return new TurmaItemLista(
            turma.Id.Value,
            turma.EscolaId.Value,
            turma.AnoLetivo,
            turma.Etapa.ToString(),
            turma.Serie,
            turma.Turno.ToString(),
            turma.Vagas,
            turma.Matriculados,
            turma.VagasDisponiveis,
            turma.Situacao.ToString());
    }

    /// <summary>Projeta a ficha da turma.</summary>
    /// <param name="turma">Turma de origem.</param>
    /// <returns>Ficha.</returns>
    public static TurmaFicha ParaFicha(Turma turma)
    {
        ArgumentNullException.ThrowIfNull(turma);
        return new TurmaFicha(
            turma.Id.Value,
            turma.EscolaId.Value,
            turma.AnoLetivo,
            turma.Etapa.ToString(),
            turma.Serie,
            turma.Turno.ToString(),
            turma.Vagas,
            turma.Matriculados,
            turma.VagasDisponiveis,
            turma.Situacao.ToString());
    }
}

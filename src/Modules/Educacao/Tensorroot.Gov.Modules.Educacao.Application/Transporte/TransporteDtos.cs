using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

namespace Tensorroot.Gov.Modules.Educacao.Application.Transporte;

/// <summary>Aluno transportado projetado na ficha/lista da rota.</summary>
/// <param name="Id">Identificador do vinculo.</param>
/// <param name="AlunoId">Aluno (por Id).</param>
/// <param name="MatriculaId">Matricula vigente (opcional, por Id).</param>
/// <param name="PontoEmbarque">Ponto de embarque.</param>
/// <param name="Ativo">Indica se o vinculo esta ativo.</param>
public sealed record AlunoTransportadoDto(
    Guid Id,
    Guid AlunoId,
    Guid? MatriculaId,
    string PontoEmbarque,
    bool Ativo);

/// <summary>Item da lista de rotas (picker do front).</summary>
/// <param name="Id">Identificador da rota.</param>
/// <param name="EscolaId">Escola atendida.</param>
/// <param name="Nome">Nome da rota.</param>
/// <param name="Turno">Turno (descricao).</param>
/// <param name="Modalidade">Modalidade (descricao).</param>
/// <param name="VeiculoId">Veiculo da Frota (por Id), se Proprio.</param>
/// <param name="Quilometragem">Quilometragem do itinerario.</param>
/// <param name="Situacao">Situacao (descricao).</param>
/// <param name="TotalAtivos">Total de alunos ativos.</param>
public sealed record RotaTransporteItemLista(
    Guid Id,
    Guid EscolaId,
    string Nome,
    string Turno,
    string Modalidade,
    Guid? VeiculoId,
    decimal Quilometragem,
    string Situacao,
    int TotalAtivos);

/// <summary>Ficha da rota (+ alunos transportados).</summary>
/// <param name="Id">Identificador da rota.</param>
/// <param name="EscolaId">Escola atendida.</param>
/// <param name="Nome">Nome da rota.</param>
/// <param name="Turno">Turno (descricao).</param>
/// <param name="Modalidade">Modalidade (descricao).</param>
/// <param name="VeiculoId">Veiculo da Frota (por Id), se Proprio.</param>
/// <param name="Quilometragem">Quilometragem do itinerario.</param>
/// <param name="Situacao">Situacao (descricao).</param>
/// <param name="Alunos">Alunos transportados.</param>
public sealed record RotaTransporteDto(
    Guid Id,
    Guid EscolaId,
    string Nome,
    string Turno,
    string Modalidade,
    Guid? VeiculoId,
    decimal Quilometragem,
    string Situacao,
    IReadOnlyList<AlunoTransportadoDto> Alunos);

/// <summary>Mapeamentos de projecao do dominio de Transporte para DTOs.</summary>
internal static class TransporteMapeamento
{
    public static RotaTransporteItemLista ParaItemLista(RotaTransporte rota)
        => new(
            rota.Id.Value,
            rota.EscolaId.Value,
            rota.Nome,
            rota.Turno.ToString(),
            rota.Modalidade.ToString(),
            rota.VeiculoId,
            rota.Quilometragem,
            rota.Situacao.ToString(),
            rota.TotalAtivos);

    public static RotaTransporteDto ParaDto(RotaTransporte rota)
        => new(
            rota.Id.Value,
            rota.EscolaId.Value,
            rota.Nome,
            rota.Turno.ToString(),
            rota.Modalidade.ToString(),
            rota.VeiculoId,
            rota.Quilometragem,
            rota.Situacao.ToString(),
            rota.Alunos.Select(ParaAlunoDto).ToList());

    public static AlunoTransportadoDto ParaAlunoDto(AlunoTransportado aluno)
        => new(
            aluno.Id.Value,
            aluno.AlunoId.Value,
            aluno.MatriculaId?.Value,
            aluno.PontoEmbarque,
            aluno.Ativo);
}

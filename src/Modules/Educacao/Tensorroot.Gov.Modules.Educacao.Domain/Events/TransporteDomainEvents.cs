using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Events;

/// <summary>Rota de transporte criada (situacao inicial Planejada).</summary>
/// <param name="RotaId">Identificador da rota.</param>
public sealed record RotaTransporteCriada(RotaTransporteId RotaId) : IDomainEvent;

/// <summary>Aluno vinculado a uma rota de transporte.</summary>
/// <param name="RotaId">Identificador da rota.</param>
/// <param name="AlunoId">Aluno vinculado.</param>
public sealed record AlunoVinculadoRota(RotaTransporteId RotaId, AlunoId AlunoId) : IDomainEvent;

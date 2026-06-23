using Tensorroot.Gov.SharedKernel;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>Profissional de saude cadastrado no acervo local (evento emitido na criacao).</summary>
/// <param name="ProfissionalId">Identificador do profissional.</param>
public sealed record ProfissionalCadastrado(ProfissionalId ProfissionalId) : IDomainEvent;

/// <summary>Vinculo CNES/CBO aberto para o profissional num estabelecimento.</summary>
/// <param name="ProfissionalId">Identificador do profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES) do vinculo.</param>
/// <param name="Cbo">Ocupacao (CBO) do vinculo.</param>
public sealed record VinculoProfissionalAberto(ProfissionalId ProfissionalId, EstabelecimentoId EstabelecimentoId, string Cbo) : IDomainEvent;

/// <summary>Vinculo CNES/CBO do profissional encerrado num estabelecimento.</summary>
/// <param name="ProfissionalId">Identificador do profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES) do vinculo.</param>
public sealed record VinculoProfissionalEncerrado(ProfissionalId ProfissionalId, EstabelecimentoId EstabelecimentoId) : IDomainEvent;

/// <summary>Profissional inativado (desligamento).</summary>
/// <param name="ProfissionalId">Identificador do profissional.</param>
public sealed record ProfissionalInativado(ProfissionalId ProfissionalId) : IDomainEvent;

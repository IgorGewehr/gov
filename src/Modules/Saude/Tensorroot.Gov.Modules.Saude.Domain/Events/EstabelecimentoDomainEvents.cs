using Tensorroot.Gov.SharedKernel;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>Estabelecimento de saude (CNES) cadastrado no acervo local (evento emitido na criacao).</summary>
/// <param name="EstabelecimentoId">Identificador do estabelecimento.</param>
/// <param name="Cnes">Codigo CNES (7 digitos).</param>
public sealed record EstabelecimentoCadastrado(EstabelecimentoId EstabelecimentoId, string Cnes) : IDomainEvent;

/// <summary>Dados cadastrais do estabelecimento atualizados.</summary>
/// <param name="EstabelecimentoId">Identificador do estabelecimento.</param>
public sealed record EstabelecimentoAtualizado(EstabelecimentoId EstabelecimentoId) : IDomainEvent;

/// <summary>Estabelecimento inativado (encerramento/suspensao) — nao recebe novos atendimentos.</summary>
/// <param name="EstabelecimentoId">Identificador do estabelecimento.</param>
public sealed record EstabelecimentoInativado(EstabelecimentoId EstabelecimentoId) : IDomainEvent;

/// <summary>Estabelecimento reativado.</summary>
/// <param name="EstabelecimentoId">Identificador do estabelecimento.</param>
public sealed record EstabelecimentoReativado(EstabelecimentoId EstabelecimentoId) : IDomainEvent;

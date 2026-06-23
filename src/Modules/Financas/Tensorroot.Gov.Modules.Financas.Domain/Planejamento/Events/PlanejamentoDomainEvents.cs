using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Events;

/// <summary>PPA entrou em vigor (lei sancionada).</summary>
/// <param name="PpaId">Identificador do PPA.</param>
/// <param name="AnoInicio">Primeiro ano do quadriênio.</param>
/// <param name="AnoFim">Último ano do quadriênio.</param>
public sealed record PpaVigente(PpaId PpaId, int AnoInicio, int AnoFim) : IDomainEvent;

/// <summary>LDO entrou em vigor (habilita a LOA do exercício).</summary>
/// <param name="LdoId">Identificador da LDO.</param>
/// <param name="Exercicio">Exercício de referência.</param>
public sealed record LdoVigente(LdoId LdoId, int Exercicio) : IDomainEvent;

/// <summary>LOA aprovada (compatibilidade PPA/LDO validada).</summary>
/// <param name="LoaId">Identificador da LOA.</param>
/// <param name="Exercicio">Exercício.</param>
public sealed record LoaAprovada(LoaId LoaId, int Exercicio) : IDomainEvent;

/// <summary>
/// Um item de despesa fixada da LOA entrou em execução: o handler de integração converte
/// em UMA <see cref="DotacaoOrcamentaria"/> (a dotação nasce da LOA). Idempotente por item.
/// </summary>
/// <param name="LoaId">LOA de origem.</param>
/// <param name="ItemId">Item de despesa fixada (QDD).</param>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Classificacao">Classificação orçamentária do item.</param>
/// <param name="ValorFixado">Valor fixado (= dotação inicial).</param>
/// <param name="AcaoPpaId">Ação do PPA de origem.</param>
public sealed record ItemLoaEntrouEmExecucao(
    LoaId LoaId,
    ItemDespesaFixadaId ItemId,
    int Exercicio,
    ClassificacaoOrcamentaria Classificacao,
    ValorMonetario ValorFixado,
    AcaoPpaId AcaoPpaId) : IDomainEvent;

/// <summary>Crédito adicional aberto (altera a LOA — Lei 4.320/64 arts. 40-46).</summary>
/// <param name="CreditoId">Identificador do crédito adicional.</param>
/// <param name="LoaId">LOA alterada.</param>
/// <param name="Valor">Valor do crédito.</param>
public sealed record CreditoAdicionalAberto(CreditoAdicionalId CreditoId, LoaId LoaId, decimal Valor) : IDomainEvent;

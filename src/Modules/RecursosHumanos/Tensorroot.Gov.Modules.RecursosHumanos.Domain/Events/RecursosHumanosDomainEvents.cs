using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Cargo publico criado na estrutura de pessoal.</summary>
/// <param name="CargoId">Identificador do cargo.</param>
/// <param name="Denominacao">Denominacao legal do cargo.</param>
/// <param name="Tipo">Tipo do cargo (efetivo/comissionado/temporario).</param>
public sealed record CargoCriado(CargoId CargoId, string Denominacao, TipoCargo Tipo) : IDomainEvent;

/// <summary>Cargo provido (uma vaga preenchida).</summary>
/// <param name="CargoId">Identificador do cargo.</param>
public sealed record CargoProvido(CargoId CargoId) : IDomainEvent;

/// <summary>Cargo passou a vago (ultima vaga ocupada foi liberada).</summary>
/// <param name="CargoId">Identificador do cargo.</param>
public sealed record CargoVago(CargoId CargoId) : IDomainEvent;

/// <summary>Vencimento-base do cargo alterado (sujeito a abate-teto na folha — CF art. 37, XI).</summary>
/// <param name="CargoId">Identificador do cargo.</param>
/// <param name="NovoVencimento">Novo valor do vencimento.</param>
public sealed record VencimentoAlterado(CargoId CargoId, decimal NovoVencimento) : IDomainEvent;

/// <summary>Cargo extinto por lei (estado terminal).</summary>
/// <param name="CargoId">Identificador do cargo.</param>
/// <param name="LeiExtincao">Lei que extinguiu o cargo.</param>
public sealed record CargoExtinto(CargoId CargoId, string LeiExtincao) : IDomainEvent;

namespace Tensorroot.Gov.BuildingBlocks.Application;

/// <summary>
/// Blocos de construção da Aplicação: pipeline behaviors do MediatR (Validation, Logging, UnitOfWork/Transaction, Idempotency).
/// Marca de montagem (assembly marker) para varredura por reflexão (MediatR /
/// FluentValidation / Injeção de Dependência) e para os testes de arquitetura.
/// Não possui comportamento.
/// </summary>
public sealed class AssemblyReference;

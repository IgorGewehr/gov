namespace Tensorroot.Gov.Modules.Transparencia.Contracts;

/// <summary>
/// Módulo Transparencia — Contratos públicos (Integration Events + DTOs). ÚNICA superfície visível a outros Bounded Contexts.
/// Marca de montagem (assembly marker) para varredura por reflexão (MediatR /
/// FluentValidation / Injeção de Dependência) e para os testes de arquitetura.
/// Não possui comportamento.
/// </summary>
public sealed class AssemblyReference;

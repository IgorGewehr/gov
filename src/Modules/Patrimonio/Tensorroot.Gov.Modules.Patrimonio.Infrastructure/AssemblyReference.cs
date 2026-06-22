namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure;

/// <summary>
/// Módulo Patrimonio — Infraestrutura (DbContext isolado por schema, mapeamentos EF Core, repositórios, integrações externas).
/// Marca de montagem (assembly marker) para varredura por reflexão (MediatR /
/// FluentValidation / Injeção de Dependência) e para os testes de arquitetura.
/// Não possui comportamento.
/// </summary>
public sealed class AssemblyReference;

namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Núcleo compartilhado: primitivos de domínio (Entity, AggregateRoot, ValueObject), Value Objects genéricos (Cnpj, Cpf), IMustHaveTenant, OutboxMessage e abstrações transversais.
/// Marca de montagem (assembly marker) para varredura por reflexão (MediatR /
/// FluentValidation / Injeção de Dependência) e para os testes de arquitetura.
/// Não possui comportamento.
/// </summary>
public sealed class AssemblyReference;

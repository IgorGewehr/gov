namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>Fornece a identidade e a origem do usuário da requisição atual (para auditoria).</summary>
public interface ICurrentUser
{
    /// <summary>Identificador do usuário (claim "sub"/NameIdentifier), se autenticado.</summary>
    string? UserId { get; }

    /// <summary>Nome de exibição do usuário, se disponível.</summary>
    string? UserName { get; }

    /// <summary>Endereço IP de origem da requisição, se disponível.</summary>
    string? IpAddress { get; }
}

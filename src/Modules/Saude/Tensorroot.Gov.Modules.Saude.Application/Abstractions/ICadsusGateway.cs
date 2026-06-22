using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Anti-Corruption Layer (ACL) sincrona para validacao do CNS na base nacional CADSUS
/// (PIX/PDQ). A implementacao concreta vive na Infrastructure, com timeout, retry e
/// circuit breaker (Polly) e mapeamento explicito de erros. Falha de disponibilidade
/// nao confirma o cadastro (mantem <see cref="Paciente.CnsConfirmado"/> falso — ver regra I-2/B-5).
/// </summary>
public interface ICadsusGateway
{
    /// <summary>Valida/confirma um CNS junto ao CADSUS.</summary>
    /// <param name="cns">Cartao Nacional de Saude a validar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o CNS foi confirmado; caso contrario, <c>false</c>.</returns>
    Task<bool> ValidarCnsAsync(Cns cns, CancellationToken cancellationToken);
}

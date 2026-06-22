using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Dados do servidor necessarios ao ponto (CPF para o AFD; regime para a aplicabilidade da 671).</summary>
/// <param name="Cpf">CPF do servidor (leiaute AFD com CPF).</param>
/// <param name="Regime">Regime previdenciario (proxy de estatutario/celetista para a Portaria 671).</param>
public sealed record DadosPontoServidor(Cpf Cpf, RegimePrevidenciario Regime);

/// <summary>
/// Consulta dados do servidor para o ponto (CPF e regime), abstraindo o agregado Servidor (mesmo
/// modulo) sem acoplar os handlers ao repositorio concreto. Respeita o Global Query Filter por tenant.
/// </summary>
public interface IServidorPontoConsulta
{
    /// <summary>Obtem CPF e regime do servidor no tenant atual.</summary>
    /// <param name="servidorId">Identificador do servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados de ponto, ou <c>null</c> se o servidor nao existir no tenant.</returns>
    Task<DadosPontoServidor?> ObterAsync(Guid servidorId, CancellationToken cancellationToken);
}

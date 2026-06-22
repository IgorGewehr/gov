using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

/// <summary>
/// Gerador do Numero Unico de Protocolo (NUP) — produz um identificador unico e imutavel por tenant
/// na autuacao do processo (Decreto 8.539/2015). A implementacao (sequencia/algoritmo institucional)
/// reside na Infrastructure do Protocolo; a unicidade e reforcada pelo indice unico (TenantId, Nup).
/// </summary>
public interface INupGenerator
{
    /// <summary>Gera um novo NUP unico para o tenant corrente.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Novo <see cref="Nup"/> unico no tenant.</returns>
    Task<Nup> GerarAsync(CancellationToken cancellationToken);
}

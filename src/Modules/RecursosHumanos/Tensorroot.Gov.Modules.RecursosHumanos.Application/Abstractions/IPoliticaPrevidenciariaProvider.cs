using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Fornece a <see cref="PoliticaPrevidenciaria"/> vigente do tenant (tem RPPS proprio?) a partir da
/// configuracao do ente, pois NAO ha agregado de "ente/empregador" no RH. Substitui o roteamento fixo de
/// regime que existia no agregado <c>Cargo</c> — o vinculo previdenciario e parametro do tenant.
/// </summary>
public interface IPoliticaPrevidenciariaProvider
{
    /// <summary>Obtem a politica previdenciaria do tenant atual.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Politica previdenciaria (default <see cref="PoliticaPrevidenciaria.SomenteRgps"/>).</returns>
    Task<PoliticaPrevidenciaria> ObterAsync(CancellationToken cancellationToken);
}

using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Previdencia;

/// <summary>
/// Fornece a <see cref="PoliticaPrevidenciaria"/> do tenant a partir da configuracao (secao
/// <see cref="ParametrosPrevidencia.SecaoConfiguracao"/> / Key Vault), jamais <em>hardcoded</em>
/// (CLAUDE.md §5/§7). Default: municipio SEM RPPS proprio (RGPS/INSS p/ todo o quadro).
/// </summary>
public sealed class PoliticaPrevidenciariaProvider(IConfiguration configuration) : IPoliticaPrevidenciariaProvider
{
    /// <inheritdoc />
    public Task<PoliticaPrevidenciaria> ObterAsync(CancellationToken cancellationToken)
    {
        var parametros = configuration
            .GetSection(ParametrosPrevidencia.SecaoConfiguracao)
            .Get<ParametrosPrevidencia>() ?? new ParametrosPrevidencia();
        return Task.FromResult(new PoliticaPrevidenciaria(parametros.PossuiRppsProprio));
    }
}

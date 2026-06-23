using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.ESocial;

/// <summary>
/// Fornece os <see cref="ParametrosESocial"/> vigentes do tenant a partir da configuracao (secao
/// <see cref="ParametrosESocial.SecaoConfiguracao"/> / Key Vault), jamais <em>hardcoded</em>
/// (CLAUDE.md §5/§7). Nao ha agregado de empregador no RH (ESOCIAL-SPEC §1.1).
/// </summary>
public sealed class EmpregadorESocialProvider(IConfiguration configuration) : IEmpregadorESocialProvider
{
    /// <inheritdoc />
    public Task<ParametrosESocial> ObterAsync(CancellationToken cancellationToken)
    {
        var parametros = configuration
            .GetSection(ParametrosESocial.SecaoConfiguracao)
            .Get<ParametrosESocial>() ?? new ParametrosESocial();
        return Task.FromResult(parametros);
    }
}

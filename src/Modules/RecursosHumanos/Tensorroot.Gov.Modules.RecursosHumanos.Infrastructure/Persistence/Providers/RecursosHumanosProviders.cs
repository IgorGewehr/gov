using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;

/// <summary>
/// Fornece os <see cref="ParametrosFolha"/> vigentes do tenant a partir da configuracao
/// (secao <see cref="ParametrosFolha.SecaoConfiguracao"/>), jamais <em>hardcoded</em> (CLAUDE.md S7).
/// </summary>
public sealed class ParametrosFolhaProvider(IConfiguration configuration) : IParametrosFolhaProvider
{
    /// <inheritdoc />
    public Task<ParametrosFolha> ObterAsync(CancellationToken cancellationToken)
    {
        var parametros = configuration
            .GetSection(ParametrosFolha.SecaoConfiguracao)
            .Get<ParametrosFolha>() ?? new ParametrosFolha();
        return Task.FromResult(parametros);
    }
}

/// <summary>
/// Consulta o regime previdenciario vigente de um servidor a partir do agregado <see cref="Servidor"/>
/// (mesmo modulo), respeitando o Global Query Filter por tenant.
/// </summary>
public sealed class ServidorRegimeConsulta(RecursosHumanosDbContext context) : IServidorRegimeConsulta
{
    /// <inheritdoc />
    public async Task<RegimePrevidenciario?> ObterRegimeAsync(Guid servidorId, CancellationToken cancellationToken)
    {
        var id = new ServidorId(servidorId);
        var servidor = await context.Servidores
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return servidor?.Regime;
    }
}

/// <summary>
/// Verifica a vigencia de uma rubrica na tabela eSocial S-1010 na competencia. Implementacao base
/// que considera vigentes as rubricas configuradas para o tenant; sem catalogo configurado, aceita
/// qualquer rubrica nao vazia (substituivel por integracao S-1010 real — §11).
/// </summary>
public sealed class RubricaS1010Consulta(IConfiguration configuration) : IRubricaS1010Consulta
{
    private const string SecaoRubricas = "RecursosHumanos:RubricasS1010";

    /// <inheritdoc />
    public Task<bool> EstaVigenteAsync(Rubrica rubrica, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        ArgumentNullException.ThrowIfNull(competencia);

        var configuradas = configuration
            .GetSection(SecaoRubricas)
            .Get<string[]>();

        var vigente = configuradas is null or { Length: 0 }
            ? !string.IsNullOrWhiteSpace(rubrica.Codigo)
            : Array.Exists(configuradas, codigo => string.Equals(codigo, rubrica.Codigo, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(vigente);
    }
}

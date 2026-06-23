using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;

/// <summary>
/// Fornece os <see cref="ParametrosPonto"/> vigentes do tenant a partir da configuracao (secao
/// <see cref="ParametrosPonto.SecaoConfiguracao"/>), jamais <em>hardcoded</em> (CLAUDE.md §7).
/// </summary>
public sealed class ParametrosPontoProvider(IConfiguration configuration) : IParametrosPontoProvider
{
    /// <inheritdoc />
    public Task<ParametrosPonto> ObterAsync(CancellationToken cancellationToken)
    {
        var parametros = configuration
            .GetSection(ParametrosPonto.SecaoConfiguracao)
            .Get<ParametrosPonto>() ?? new ParametrosPonto();
        return Task.FromResult(parametros);
    }
}

/// <summary>
/// Consulta CPF e regime de um servidor para o ponto, a partir do agregado <see cref="Servidor"/>
/// (mesmo modulo), respeitando o Global Query Filter por tenant.
/// </summary>
public sealed class ServidorPontoConsulta(RecursosHumanosDbContext context) : IServidorPontoConsulta
{
    /// <inheritdoc />
    public async Task<DadosPontoServidor?> ObterAsync(Guid servidorId, CancellationToken cancellationToken)
    {
        var id = new ServidorId(servidorId);
        var servidor = await context.Servidores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return servidor is null ? null : new DadosPontoServidor(servidor.Cpf, servidor.Regime);
    }

    /// <inheritdoc />
    public async Task<Guid?> ResolverServidorPorCpfAsync(string cpf, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cpf);

        // O CPF e VO com conversor para a string de digitos; comparamos pela coluna convertida.
        // Respeita o Global Query Filter por tenant (so resolve servidores do tenant atual).
        var alvo = Cpf.Create(cpf);
        var servidor = await context.Servidores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Cpf == alvo, cancellationToken)
            .ConfigureAwait(false);
        return servidor?.Id.Value;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

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

    /// <inheritdoc />
    public async Task<DadosCalculoServidor?> ObterDadosCalculoAsync(Guid servidorId, CancellationToken cancellationToken)
    {
        var id = new ServidorId(servidorId);
        var servidor = await context.Servidores
            .AsNoTracking()
            .Include(s => s.Dependentes)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return servidor is null
            ? null
            : new DadosCalculoServidor(servidor.Regime, servidor.Dependentes.Count);
    }
}

/// <summary>
/// Fornece as tabelas legais (INSS/IRRF/RPPS) vigentes na competencia para o tenant atual,
/// selecionando a versao cuja vigencia inicial e a mais recente menor ou igual a competencia
/// (respeitando o Global Query Filter por tenant). Os valores sao dado parametrizado, nunca hardcoded.
/// </summary>
public sealed class TabelasLegaisProvider(RecursosHumanosDbContext context) : ITabelasLegaisProvider
{
    // VigenciaInicio e propriedade convertida (Competencia <-> int), nao traduzivel em ordenacao por
    // Ano/Mes na query. As tabelas legais sao poucas linhas por tenant: carrega as do tenant (ja
    // filtrado pelo Global Query Filter) e seleciona a vigencia mais recente <= competencia em memoria.
    private static int Ordinal(Competencia competencia) => (competencia.Ano * 100) + competencia.Mes;

    /// <inheritdoc />
    public async Task<TabelaInss?> ObterInssVigenteAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = Ordinal(competencia);
        var tabelas = await context.TabelasInss.Include(t => t.Faixas).ToListAsync(cancellationToken).ConfigureAwait(false);
        return tabelas
            .Where(t => Ordinal(t.VigenciaInicio) <= alvo)
            .OrderByDescending(t => Ordinal(t.VigenciaInicio))
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<TabelaIrrf?> ObterIrrfVigenteAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = Ordinal(competencia);
        var tabelas = await context.TabelasIrrf.Include(t => t.Faixas).ToListAsync(cancellationToken).ConfigureAwait(false);
        return tabelas
            .Where(t => Ordinal(t.VigenciaInicio) <= alvo)
            .OrderByDescending(t => Ordinal(t.VigenciaInicio))
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<TabelaRpps?> ObterRppsVigenteAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = Ordinal(competencia);
        var tabelas = await context.TabelasRpps.Include(t => t.Faixas).ToListAsync(cancellationToken).ConfigureAwait(false);
        return tabelas
            .Where(t => Ordinal(t.VigenciaInicio) <= alvo)
            .OrderByDescending(t => Ordinal(t.VigenciaInicio))
            .FirstOrDefault();
    }
}

/// <summary>
/// Verifica a vigencia de uma rubrica na tabela eSocial S-1010 na competencia. Consulta primeiro o
/// catalogo parametrizavel de <c>RubricaFolha</c> do tenant (fonte de verdade); na ausencia de
/// qualquer rubrica cadastrada, cai para a lista de configuracao (compatibilidade) e, por fim,
/// aceita rubrica nao vazia (substituivel por integracao S-1010 real — §11).
/// </summary>
public sealed class RubricaS1010Consulta(RecursosHumanosDbContext context, IConfiguration configuration) : IRubricaS1010Consulta
{
    private const string SecaoRubricas = "RecursosHumanos:RubricasS1010";

    /// <inheritdoc />
    public async Task<bool> EstaVigenteAsync(Rubrica rubrica, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        ArgumentNullException.ThrowIfNull(competencia);

        // Fonte de verdade: catalogo parametrizavel do tenant (vigente na competencia).
        if (await context.Rubricas.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var alvo = (competencia.Ano * 100) + competencia.Mes;
            var candidatas = await context.Rubricas
                .Where(r => r.Ativa && r.Codigo == rubrica)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return candidatas.Exists(r => ((r.VigenciaInicio.Ano * 100) + r.VigenciaInicio.Mes) <= alvo);
        }

        // Compatibilidade: lista de configuracao; por fim, aceita rubrica nao vazia.
        var configuradas = configuration.GetSection(SecaoRubricas).Get<string[]>();
        return configuradas is null or { Length: 0 }
            ? !string.IsNullOrWhiteSpace(rubrica.Codigo)
            : Array.Exists(configuradas, codigo => string.Equals(codigo, rubrica.Codigo, StringComparison.OrdinalIgnoreCase));
    }
}

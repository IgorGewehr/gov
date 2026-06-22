using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;

/// <summary>
/// Implementacao EF Core de <see cref="IParametroVigenteProvider"/>: le os parametros versionados
/// por vigencia (tabela <see cref="ParametroVigente"/>), parametrizaveis por tenant — nunca
/// hardcoded (Beneficio I-1; CLAUDE.md secao 7). Seleciona a vigencia mais recente que nao
/// ultrapassa a competencia de referencia.
/// </summary>
public sealed class ParametroVigenteProvider(AssistenciaSocialDbContext context) : IParametroVigenteProvider
{
    private const string ChaveSalarioMinimo = "SalarioMinimo";

    /// <inheritdoc />
    public async Task<ValorMonetario?> ObterSalarioMinimoVigenteAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        // A competencia de referencia mapeia para o primeiro dia do mes correspondente.
        var referencia = new DateOnly(competencia.Ano, competencia.Mes, 1);

        var parametro = await context.ParametrosVigentes
            .Where(item => item.Chave == ChaveSalarioMinimo && item.VigenciaInicio <= referencia)
            .OrderByDescending(item => item.VigenciaInicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // B-11: ausencia de parametro para a competencia retorna nulo (nao decide com valor inventado).
        return parametro is null ? null : ValorMonetario.De(parametro.Valor);
    }
}

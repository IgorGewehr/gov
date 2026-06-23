using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;

/// <summary>
/// <b>A-0 — implementacao EF Core de <see cref="ICriterioBeneficioEventualProvider"/>.</b> Le o criterio
/// MUNICIPAL de beneficio eventual versionado por tenant+vigencia (<see cref="CriterioBeneficioEventualMunicipal"/>),
/// selecionando a vigencia mais recente que nao ultrapassa a competencia. NAO retorna nenhum default
/// federal quando o tenant nao parametrizou: devolve <c>null</c> (o teto de 1/4 SM foi revogado pela Lei
/// 12.435/2011 e nunca e aplicado). Tenant-scoped via Global Query Filter.
/// </summary>
public sealed class CriterioBeneficioEventualProvider(AssistenciaSocialDbContext context) : ICriterioBeneficioEventualProvider
{
    /// <inheritdoc />
    public async Task<CriterioBeneficioEventual?> ObterCriterioVigenteAsync(
        ModalidadeBeneficioEventual modalidade,
        Competencia competencia,
        CancellationToken cancellationToken)
    {
        var referencia = new DateOnly(competencia.Ano, competencia.Mes, 1);

        var registro = await context.CriteriosBeneficioEventual
            .Where(item => item.Modalidade == modalidade && item.VigenciaInicio <= referencia)
            .OrderByDescending(item => item.VigenciaInicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // Ausencia de criterio municipal NAO vira default federal (A-0/§16): retorna nulo.
        return registro?.ParaDominio();
    }
}

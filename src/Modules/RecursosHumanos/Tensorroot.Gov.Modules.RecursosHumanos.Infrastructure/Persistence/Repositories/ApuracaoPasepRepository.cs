using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Pasep;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ApuracaoPasep"/>.</summary>
public sealed class ApuracaoPasepRepository(RecursosHumanosDbContext context) : IApuracaoPasepRepository
{
    /// <inheritdoc />
    public void Adicionar(ApuracaoPasep apuracao)
    {
        ArgumentNullException.ThrowIfNull(apuracao);
        context.ApuracoesPasep.Add(apuracao);
    }

    /// <inheritdoc />
    public Task<ApuracaoPasep?> ObterPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return context.ApuracoesPasep.FirstOrDefaultAsync(a => a.Competencia == competencia, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteParaCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return context.ApuracoesPasep.AnyAsync(a => a.Competencia == competencia, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApuracaoPasep>> ListarPorAnoAsync(int ano, CancellationToken cancellationToken)
    {
        // Competencia e' inteiro (Ano*100 + Mes) via value converter; o intervalo do ano e' [ano*100+1, ano*100+12].
        // Materializa filtrado por intervalo e ordena por mes em memoria (lote pequeno: <=12 por tenant/ano).
        var todas = await context.ApuracoesPasep
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return todas
            .Where(a => a.Competencia.Ano == ano)
            .OrderBy(a => a.Competencia.Mes)
            .ToList();
    }
}

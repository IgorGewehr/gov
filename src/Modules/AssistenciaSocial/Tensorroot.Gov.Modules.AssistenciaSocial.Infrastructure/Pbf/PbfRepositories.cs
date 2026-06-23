using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Pbf;

/// <summary>Implementacao EF Core do repositorio do acompanhamento de condicionalidades do PBF (3d.1).</summary>
public sealed class AcompanhamentoCondicionalidadeRepository(AssistenciaSocialDbContext context) : IAcompanhamentoCondicionalidadeRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(AcompanhamentoCondicionalidade acompanhamento, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(acompanhamento);
        await context.AcompanhamentosCondicionalidade.AddAsync(acompanhamento, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AcompanhamentoCondicionalidade?> ObterPorIdAsync(AcompanhamentoCondicionalidadeId id, CancellationToken cancellationToken)
        => await context.AcompanhamentosCondicionalidade
            .Include(a => a.Registros)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<AcompanhamentoCondicionalidade?> ObterPorFamiliaCompetenciaAsync(FamiliaId familiaId, Competencia competencia, CancellationToken cancellationToken)
        => await context.AcompanhamentosCondicionalidade
            .Include(a => a.Registros)
            .FirstOrDefaultAsync(a => a.FamiliaId == familiaId && a.Competencia == competencia, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AcompanhamentoCondicionalidade>> ListarPorCompetenciaAsync(
        Competencia competencia,
        EfeitoDescumprimento? efeitoMinimo,
        CancellationToken cancellationToken)
    {
        var consulta = context.AcompanhamentosCondicionalidade
            .Include(a => a.Registros)
            .Where(a => a.Competencia == competencia);

        if (efeitoMinimo is { } minimo)
        {
            consulta = consulta.Where(a => a.Efeito >= minimo);
        }

        return await consulta
            .OrderByDescending(a => a.Efeito)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Ingestao;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório do read model consolidado (<see cref="IndicadorMunicipioSnapshot"/>). Tenant-scoped pelo
/// Global Query Filter do <see cref="PainelGestorDbContext"/> — nunca filtra por TenantId manualmente.
/// </summary>
public sealed class IndicadorMunicipioRepository(PainelGestorDbContext context) : IIndicadorMunicipioRepository
{
    /// <inheritdoc />
    public Task<IndicadorMunicipioSnapshot?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.Indicadores
            .Include(indicador => indicador.Minimos)
            .Include(indicador => indicador.DespesasPessoalMensais)
            .FirstOrDefaultAsync(indicador => indicador.Exercicio == exercicio, cancellationToken);

    /// <inheritdoc />
    public void Adicionar(IndicadorMunicipioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        context.Indicadores.Add(snapshot);
    }
}

/// <summary>
/// Implementação da idempotência da ingestão sobre o ledger <see cref="EventoIngerido"/>. O
/// <see cref="TimeProvider"/> carimba apenas o momento de AUDITORIA da ingestão (nunca entra em cálculo
/// de indicador — CLAUDE.md: sem relógio em cálculo). Tenant-scoped pelo Global Query Filter.
/// </summary>
public sealed class IngestaoIdempotencia(PainelGestorDbContext context, TimeProvider timeProvider) : IIngestaoIdempotencia
{
    /// <inheritdoc />
    public Task<bool> JaProcessadoAsync(Guid eventId, CancellationToken cancellationToken)
        => context.EventosIngeridos.AnyAsync(evento => evento.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public void Registrar(Guid eventId, Guid tenantId, string tipoEvento)
    {
        var registro = EventoIngerido.Criar(eventId, tenantId, tipoEvento, timeProvider.GetUtcNow().UtcDateTime);
        context.EventosIngeridos.Add(registro);
    }
}

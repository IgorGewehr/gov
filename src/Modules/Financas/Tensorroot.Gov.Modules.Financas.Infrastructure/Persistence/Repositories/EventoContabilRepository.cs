using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de eventos contábeis (roteiros).</summary>
public sealed class EventoContabilRepository(FinancasDbContext context) : IEventoContabilRepository
{
    /// <inheritdoc />
    public void Adicionar(EventoContabil evento)
    {
        ArgumentNullException.ThrowIfNull(evento);
        context.EventosContabeis.Add(evento);
    }

    /// <inheritdoc />
    public async Task<EventoContabil?> ObterVigenteAsync(FatoContabil fato, int exercicio, CancellationToken cancellationToken)
    {
        // Pré-filtra por fato/ativo no banco; a vigência por exercício (com fim nulo) é avaliada em memória.
        var candidatos = await context.EventosContabeis
            .Include(evento => evento.Linhas)
            .Where(evento => evento.Fato == fato && evento.Ativo && evento.ExercicioVigenciaInicio <= exercicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return candidatos
            .Where(evento => evento.VigenteEm(exercicio))
            .OrderByDescending(evento => evento.ExercicioVigenciaInicio)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<bool> ExisteParaFatoAsync(FatoContabil fato, CancellationToken cancellationToken)
        => context.EventosContabeis.AnyAsync(evento => evento.Fato == fato, cancellationToken);
}

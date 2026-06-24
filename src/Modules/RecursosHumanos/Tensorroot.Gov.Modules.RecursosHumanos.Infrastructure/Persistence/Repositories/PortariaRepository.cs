using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Portaria"/>.</summary>
public sealed class PortariaRepository(RecursosHumanosDbContext context) : IPortariaRepository
{
    /// <inheritdoc />
    public void Adicionar(Portaria portaria)
    {
        ArgumentNullException.ThrowIfNull(portaria);
        context.Portarias.Add(portaria);
    }

    /// <inheritdoc />
    public Task<Portaria?> ObterPorIdAsync(PortariaId id, CancellationToken cancellationToken)
        => context.Portarias.FirstOrDefaultAsync(portaria => portaria.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<int> ProximoSequencialAsync(int exercicio, CancellationToken cancellationToken)
    {
        // Maior sequencial do exercicio no tenant (Global Query Filter aplica o TenantId) + 1.
        var maximo = await context.Portarias
            .Where(portaria => portaria.Exercicio == exercicio)
            .Select(portaria => (int?)portaria.Sequencial)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        return (maximo ?? 0) + 1;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Portaria> Itens, int Total)> BuscarAsync(
        TipoPortaria? tipo,
        SituacaoPortaria? situacao,
        int? exercicio,
        Guid? servidorId,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Portarias.AsQueryable();

        if (tipo is { } filtroTipo)
        {
            consulta = consulta.Where(portaria => portaria.Tipo == filtroTipo);
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(portaria => portaria.Situacao == filtroSituacao);
        }

        if (exercicio is { } filtroExercicio)
        {
            consulta = consulta.Where(portaria => portaria.Exercicio == filtroExercicio);
        }

        if (servidorId is { } sid)
        {
            var alvo = new ServidorId(sid);
            consulta = consulta.Where(portaria => portaria.ServidorId == alvo);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderByDescending(portaria => portaria.Exercicio)
            .ThenByDescending(portaria => portaria.Sequencial)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

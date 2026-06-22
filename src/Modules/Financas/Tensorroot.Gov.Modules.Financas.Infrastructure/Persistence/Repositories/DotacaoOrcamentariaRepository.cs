using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de dotações orçamentárias.</summary>
public sealed class DotacaoOrcamentariaRepository(FinancasDbContext context) : IDotacaoOrcamentariaRepository
{
    /// <inheritdoc />
    public void Adicionar(DotacaoOrcamentaria dotacao)
    {
        ArgumentNullException.ThrowIfNull(dotacao);
        context.Dotacoes.Add(dotacao);
    }

    /// <inheritdoc />
    public Task<DotacaoOrcamentaria?> ObterPorIdAsync(DotacaoOrcamentariaId id, CancellationToken cancellationToken)
        => context.Dotacoes.FirstOrDefaultAsync(dotacao => dotacao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DotacaoOrcamentaria>> ListarPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => await context.Dotacoes
            .Where(dotacao => dotacao.Exercicio == exercicio)
            .OrderBy(dotacao => dotacao.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

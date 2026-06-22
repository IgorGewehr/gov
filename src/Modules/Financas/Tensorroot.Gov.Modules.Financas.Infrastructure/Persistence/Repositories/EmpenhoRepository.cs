using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de empenhos.</summary>
public sealed class EmpenhoRepository(FinancasDbContext context) : IEmpenhoRepository
{
    /// <inheritdoc />
    public void Adicionar(Empenho empenho)
    {
        ArgumentNullException.ThrowIfNull(empenho);
        context.Empenhos.Add(empenho);
    }

    /// <inheritdoc />
    public Task<Empenho?> ObterPorIdAsync(EmpenhoId id, CancellationToken cancellationToken)
        => context.Empenhos.FirstOrDefaultAsync(empenho => empenho.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Empenho>> ListarPorDotacaoAsync(DotacaoOrcamentariaId dotacaoId, CancellationToken cancellationToken)
        => await context.Empenhos
            .Where(empenho => empenho.DotacaoId == dotacaoId)
            .OrderBy(empenho => empenho.Numero)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Empenho>> ListarComSaldoAbertoPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
    {
        // Pré-filtra no banco os empenhos ainda não inscritos/totalmente pagos; o teste fino de
        // saldo a inscrever (empenhado − pago) usa o invariante de domínio em memória.
        var candidatos = await context.Empenhos
            .Where(empenho => empenho.Exercicio == exercicio
                && empenho.Situacao != SituacaoEmpenho.Anulado
                && empenho.Situacao != SituacaoEmpenho.TotalmentePago
                && empenho.Situacao != SituacaoEmpenho.InscritoRestosAPagar)
            .OrderBy(empenho => empenho.Numero)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return candidatos.Where(empenho => empenho.PossuiSaldoParaRestosAPagar()).ToList();
    }
}

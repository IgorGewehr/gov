using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de guias de recolhimento.</summary>
public sealed class GuiaRecolhimentoRepository(FinancasDbContext context) : IGuiaRecolhimentoRepository
{
    /// <inheritdoc />
    public void Adicionar(GuiaRecolhimento guia)
    {
        ArgumentNullException.ThrowIfNull(guia);
        context.GuiasRecolhimento.Add(guia);
    }

    /// <inheritdoc />
    public Task<GuiaRecolhimento?> ObterPorIdAsync(GuiaRecolhimentoId id, CancellationToken cancellationToken)
        => context.GuiasRecolhimento
            .Include(guia => guia.Itens)
            .FirstOrDefaultAsync(guia => guia.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<GuiaRecolhimento>> ListarAsync(SituacaoGuiaRecolhimento? situacao, CancellationToken cancellationToken)
        => await context.GuiasRecolhimento
            .Include(guia => guia.Itens)
            .Where(guia => situacao == null || guia.Situacao == situacao)
            .OrderByDescending(guia => guia.Competencia)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

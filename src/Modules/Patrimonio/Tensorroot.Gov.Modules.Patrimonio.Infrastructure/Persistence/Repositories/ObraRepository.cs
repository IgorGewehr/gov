using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório do agregado <see cref="Obra"/>.</summary>
public sealed class ObraRepository(PatrimonioDbContext context) : IObraRepository
{
    /// <inheritdoc />
    public void Adicionar(Obra obra)
    {
        ArgumentNullException.ThrowIfNull(obra);
        context.Obras.Add(obra);
    }

    /// <inheritdoc />
    public Task<Obra?> ObterPorIdAsync(ObraId id, CancellationToken cancellationToken)
        => context.Obras.FirstOrDefaultAsync(obra => obra.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteParaContratoAsync(Guid contratoId, CancellationToken cancellationToken)
        => context.Obras.AnyAsync(obra => obra.ContratoId == contratoId, cancellationToken);

    /// <inheritdoc />
    public Task<Obra?> ObterPorContratoAsync(Guid contratoId, CancellationToken cancellationToken)
        => context.Obras.FirstOrDefaultAsync(obra => obra.ContratoId == contratoId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Obra>> ListarEmExecucaoAsync(CancellationToken cancellationToken)
        => await context.Obras
            .Where(obra => obra.Situacao == SituacaoObra.EmExecucao)
            .OrderBy(obra => obra.DataAssinaturaContrato)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Obra> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoObra? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Obras.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // Busca em Objeto (coluna string real) e Municipio (owned, coluna real), case-insensitive via collation.
            var padrao = BuscaTexto.MontarPadraoContains(termo);
            consulta = consulta.Where(obra =>
                EF.Functions.Like(obra.Objeto, padrao, "\\")
                || EF.Functions.Like(obra.Localizacao.Municipio, padrao, "\\"));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(obra => obra.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(obra => obra.Objeto)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

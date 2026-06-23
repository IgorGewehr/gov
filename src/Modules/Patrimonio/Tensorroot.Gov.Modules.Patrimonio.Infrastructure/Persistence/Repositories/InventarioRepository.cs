using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório do agregado <see cref="Inventario"/>.</summary>
public sealed class InventarioRepository(PatrimonioDbContext context) : IInventarioRepository
{
    /// <inheritdoc />
    public void Adicionar(Inventario inventario)
    {
        ArgumentNullException.ThrowIfNull(inventario);
        context.Inventarios.Add(inventario);
    }

    /// <inheritdoc />
    public Task<Inventario?> ObterPorIdAsync(InventarioId id, CancellationToken cancellationToken)
        // Owned collections (Comissao/Itens/Divergencias) sao carregadas via Include implicito do EF
        // para owned types; o FirstOrDefault materializa o agregado completo no tenant corrente.
        => context.Inventarios.FirstOrDefaultAsync(inventario => inventario.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SnapshotBem>> CarregarSnapshotAcervoAsync(string? setor, CancellationToken cancellationToken)
    {
        // Le os bens ativos no acervo (Tombado/Cedido) do tenant corrente (Global Query Filter) com as
        // movimentacoes, para inferir a localizacao esperada (ultima movimentacao). Materializa em memoria
        // para projetar o VO ValorMonetario e a localizacao sem traduzir VOs no SQL.
        var bens = await context.Bens
            .Where(bem => bem.Situacao == SituacaoBemPatrimonial.Tombado || bem.Situacao == SituacaoBemPatrimonial.Cedido)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var snapshots = bens
            .Select(bem => new
            {
                Bem = bem,
                Localizacao = bem.Movimentacoes
                    .OrderByDescending(movimentacao => movimentacao.Data)
                    .Select(movimentacao => movimentacao.LocalizacaoDestino)
                    .FirstOrDefault(),
            })
            .Where(item => setor is null
                || (item.Localizacao != null
                    && item.Localizacao.Contains(setor, StringComparison.OrdinalIgnoreCase)))
            .Select(item => new SnapshotBem(
                item.Bem.Id,
                item.Bem.NumeroTombamento?.Valor,
                item.Bem.Descricao,
                string.IsNullOrWhiteSpace(item.Localizacao) ? null : item.Localizacao,
                item.Bem.ValorContabil))
            .ToList();

        return snapshots;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Inventario> Itens, int Total)> BuscarAsync(
        int? exercicio,
        string? setor,
        SituacaoInventario? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Inventarios.AsQueryable();

        if (exercicio is { } filtroExercicio)
        {
            consulta = consulta.Where(inventario => inventario.Exercicio == filtroExercicio);
        }

        if (!string.IsNullOrWhiteSpace(setor))
        {
            var padrao = BuscaTexto.MontarPadraoContains(setor);
            consulta = consulta.Where(inventario =>
                inventario.Setor != null && EF.Functions.Like(inventario.Setor, padrao, "\\"));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(inventario => inventario.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderByDescending(inventario => inventario.Exercicio)
            .ThenByDescending(inventario => inventario.DataAbertura)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using DomainPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do catalogo de medicamentos (tenant-scoped via Global Query Filter).</summary>
public sealed class MedicamentoRepository(SaudeDbContext context) : IMedicamentoRepository
{
    /// <inheritdoc />
    public void Adicionar(Medicamento medicamento)
    {
        ArgumentNullException.ThrowIfNull(medicamento);
        context.Medicamentos.Add(medicamento);
    }

    /// <inheritdoc />
    public Task<Medicamento?> ObterPorIdAsync(MedicamentoId id, CancellationToken cancellationToken)
        => context.Medicamentos.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Medicamento> Itens, int Total)> BuscarAsync(
        string? termo, bool apenasAtivos, int pagina, int tamanho, CancellationToken cancellationToken)
    {
        var consulta = context.Medicamentos.AsQueryable();

        if (apenasAtivos)
        {
            consulta = consulta.Where(m => m.Ativo);
        }

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = "%" + termo.Trim() + "%";
            consulta = consulta.Where(m =>
                EF.Functions.Like(m.PrincipioAtivo, padrao) || EF.Functions.Like(m.Apresentacao, padrao));
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);
        var itens = await consulta
            .OrderBy(m => m.PrincipioAtivo)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

/// <summary>Implementacao EF Core do estoque de medicamentos (tenant-scoped).</summary>
public sealed class EstoqueMedicamentoRepository(SaudeDbContext context) : IEstoqueMedicamentoRepository
{
    /// <inheritdoc />
    public void Adicionar(EstoqueMedicamento estoque)
    {
        ArgumentNullException.ThrowIfNull(estoque);
        context.EstoquesMedicamento.Add(estoque);
    }

    /// <inheritdoc />
    public Task<EstoqueMedicamento?> ObterPorIdAsync(EstoqueMedicamentoId id, CancellationToken cancellationToken)
        => context.EstoquesMedicamento.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<EstoqueMedicamento?> ObterPorEstabelecimentoEMedicamentoAsync(
        EstabelecimentoId estabelecimentoId, MedicamentoId medicamentoId, CancellationToken cancellationToken)
        => context.EstoquesMedicamento
            .FirstOrDefaultAsync(e => e.EstabelecimentoId == estabelecimentoId && e.MedicamentoId == medicamentoId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EstoqueMedicamento>> ListarPorEstabelecimentoAsync(
        EstabelecimentoId estabelecimentoId, CancellationToken cancellationToken)
    {
        // Ordena por Saldo (decimal) em memoria: o SQLite nao traduz ORDER BY de decimal.
        var estoques = await context.EstoquesMedicamento
            .Where(e => e.EstabelecimentoId == estabelecimentoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return estoques
            .OrderByDescending(e => e.Saldo)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EstoqueMedicamento>> ListarComLotesAVencerAsync(
        DateOnly ate, CancellationToken cancellationToken)
    {
        // Owned children (lotes) sao sempre materializados; filtra em memoria para evitar dependencia
        // de traducao de Any() sobre owned collection no provider. Tenant ja aplicado pelo Global Filter.
        var todos = await context.EstoquesMedicamento
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return todos
            .Where(e => e.Lotes.Any(l => l.Saldo > 0m && l.Validade <= ate))
            .ToList();
    }
}

/// <summary>Implementacao EF Core das dispensacoes ao paciente (tenant-scoped).</summary>
public sealed class DispensacaoRepository(SaudeDbContext context) : IDispensacaoRepository
{
    /// <inheritdoc />
    public void Adicionar(Dispensacao dispensacao)
    {
        ArgumentNullException.ThrowIfNull(dispensacao);
        context.Dispensacoes.Add(dispensacao);
    }

    /// <inheritdoc />
    public Task<Dispensacao?> ObterPorIdAsync(DispensacaoId id, CancellationToken cancellationToken)
        => context.Dispensacoes.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dispensacao>> ListarPorPacienteAsync(
        DomainPacienteId pacienteId, DateOnly? de, DateOnly? ate, CancellationToken cancellationToken)
    {
        var consulta = context.Dispensacoes.Where(d => d.PacienteId == pacienteId);

        if (de is { } dataInicial)
        {
            var inicio = new DateTimeOffset(dataInicial.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            consulta = consulta.Where(d => d.DataHora >= inicio);
        }

        if (ate is { } dataFinal)
        {
            var fim = new DateTimeOffset(dataFinal.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            consulta = consulta.Where(d => d.DataHora <= fim);
        }

        // Ordena por DataHora (DateTimeOffset) em memoria: o SQLite nao traduz ORDER BY de DateTimeOffset.
        var dispensacoes = await consulta
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return dispensacoes
            .OrderByDescending(d => d.DataHora)
            .ToList();
    }
}

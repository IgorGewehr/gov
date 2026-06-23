using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;
using DomainPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do catalogo de imunobiologicos (tenant-scoped via Global Query Filter).</summary>
public sealed class ImunobiologicoRepository(SaudeDbContext context) : IImunobiologicoRepository
{
    /// <inheritdoc />
    public void Adicionar(Imunobiologico imunobiologico)
    {
        ArgumentNullException.ThrowIfNull(imunobiologico);
        context.Imunobiologicos.Add(imunobiologico);
    }

    /// <inheritdoc />
    public Task<Imunobiologico?> ObterPorIdAsync(ImunobiologicoId id, CancellationToken cancellationToken)
        => context.Imunobiologicos.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Imunobiologico>> ListarAtivosAsync(CancellationToken cancellationToken)
        => await context.Imunobiologicos
            .Where(i => i.Ativo)
            .OrderBy(i => i.Sigla)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core das carteiras de vacinacao (tenant-scoped).</summary>
public sealed class CarteiraVacinacaoRepository(SaudeDbContext context) : ICarteiraVacinacaoRepository
{
    /// <inheritdoc />
    public void Adicionar(CarteiraVacinacao carteira)
    {
        ArgumentNullException.ThrowIfNull(carteira);
        context.CarteirasVacinacao.Add(carteira);
    }

    /// <inheritdoc />
    public Task<CarteiraVacinacao?> ObterPorPacienteAsync(DomainPacienteId pacienteId, CancellationToken cancellationToken)
        => context.CarteirasVacinacao.FirstOrDefaultAsync(c => c.PacienteId == pacienteId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CarteiraVacinacao>> ListarComAprazamentoVencidoAsync(
        DateOnly ate, CancellationToken cancellationToken)
    {
        // Owned children (doses) sao sempre materializados; filtra em memoria a regra de busca ativa
        // (ultima dose com aprazamento vencido e sem dose subsequente). Tenant ja aplicado pelo filtro.
        var todas = await context.CarteirasVacinacao
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return todas
            .Where(c => c.AprazamentosVencidos(ate).Count > 0)
            .ToList();
    }
}

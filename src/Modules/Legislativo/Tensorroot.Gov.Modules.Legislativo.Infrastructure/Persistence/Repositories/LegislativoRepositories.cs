using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Proposicao"/>.</summary>
public sealed class ProposicaoRepository(LegislativoDbContext context) : IProposicaoRepository
{
    /// <inheritdoc />
    public void Adicionar(Proposicao proposicao)
    {
        ArgumentNullException.ThrowIfNull(proposicao);
        context.Proposicoes.Add(proposicao);
    }

    /// <inheritdoc />
    public Task<Proposicao?> ObterPorIdAsync(ProposicaoId id, CancellationToken cancellationToken)
        => context.Proposicoes.FirstOrDefaultAsync(proposicao => proposicao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Proposicao>> ListarPorSituacaoAsync(SituacaoProposicao situacao, CancellationToken cancellationToken)
        => await context.Proposicoes
            .Where(proposicao => proposicao.Situacao == situacao)
            .OrderBy(proposicao => proposicao.DataApresentacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Sessao"/>.</summary>
public sealed class SessaoRepository(LegislativoDbContext context) : ISessaoRepository
{
    /// <inheritdoc />
    public void Adicionar(Sessao sessao)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        context.Sessoes.Add(sessao);
    }

    /// <inheritdoc />
    public Task<Sessao?> ObterPorIdAsync(SessaoId id, CancellationToken cancellationToken)
        => context.Sessoes.FirstOrDefaultAsync(sessao => sessao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Sessao>> ListarPorSituacaoAsync(SituacaoSessao situacao, CancellationToken cancellationToken)
        => await context.Sessoes
            .Where(sessao => sessao.Situacao == situacao)
            .OrderBy(sessao => sessao.DataHora.Valor)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Votacao"/>.</summary>
public sealed class VotacaoRepository(LegislativoDbContext context) : IVotacaoRepository
{
    /// <inheritdoc />
    public void Adicionar(Votacao votacao)
    {
        ArgumentNullException.ThrowIfNull(votacao);
        context.Votacoes.Add(votacao);
    }

    /// <inheritdoc />
    public Task<Votacao?> ObterPorIdAsync(VotacaoId id, CancellationToken cancellationToken)
        => context.Votacoes.FirstOrDefaultAsync(votacao => votacao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Votacao>> ListarAsync(SessaoId? sessaoId, CancellationToken cancellationToken)
    {
        var consulta = context.Votacoes.Include(votacao => votacao.Votos).AsQueryable();
        if (sessaoId is { } sessao)
        {
            consulta = consulta.Where(votacao => votacao.SessaoId == sessao);
        }

        // Abertas primeiro (foco do painel ao vivo), depois por turno — ordem determinista.
        return await consulta
            .OrderByDescending(votacao => votacao.Situacao == SituacaoVotacao.Aberta)
            .ThenBy(votacao => votacao.Turno)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Vereador"/>.</summary>
public sealed class VereadorRepository(LegislativoDbContext context) : IVereadorRepository
{
    /// <inheritdoc />
    public void Adicionar(Vereador vereador)
    {
        ArgumentNullException.ThrowIfNull(vereador);
        context.Vereadores.Add(vereador);
    }

    /// <inheritdoc />
    public Task<Vereador?> ObterPorIdAsync(VereadorId id, CancellationToken cancellationToken)
        => context.Vereadores.FirstOrDefaultAsync(vereador => vereador.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Vereador>> ListarAsync(CancellationToken cancellationToken)
        => await context.Vereadores
            .OrderBy(vereador => vereador.NomeParlamentar)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<VereadorId, string>> ResolverNomesAsync(
        IReadOnlyCollection<VereadorId> ids,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return new Dictionary<VereadorId, string>();
        }

        var alvo = ids.Distinct().ToList();
        var pares = await context.Vereadores
            .Where(vereador => alvo.Contains(vereador.Id))
            .Select(vereador => new { vereador.Id, vereador.NomeParlamentar })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return pares.ToDictionary(par => par.Id, par => par.NomeParlamentar);
    }

    /// <inheritdoc />
    public Task<int> ContarAsync(CancellationToken cancellationToken)
        => context.Vereadores.CountAsync(cancellationToken);
}

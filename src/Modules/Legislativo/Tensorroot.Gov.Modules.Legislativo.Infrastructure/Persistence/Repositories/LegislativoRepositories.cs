using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;
using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;
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

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Norma"/>.</summary>
public sealed class NormaRepository(LegislativoDbContext context) : INormaRepository
{
    /// <inheritdoc />
    public void Adicionar(Norma norma)
    {
        ArgumentNullException.ThrowIfNull(norma);
        context.Normas.Add(norma);
    }

    /// <inheritdoc />
    public Task<Norma?> ObterPorIdAsync(NormaId id, CancellationToken cancellationToken)
        => context.Normas.FirstOrDefaultAsync(norma => norma.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteAsync(TipoNorma tipo, int numero, int ano, CancellationToken cancellationToken)
        => context.Normas.AnyAsync(
            norma => norma.Tipo == tipo && norma.Numero == numero && norma.Ano == ano,
            cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Norma> Itens, int Total)> BuscarAsync(FiltroNormas filtro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var consulta = context.Normas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Termo))
        {
            // Busca case-insensitive (LIKE) pela coluna-sombra string crua "EmentaBusca"
            // (sincronizada no SaveChanges), evitando o value converter do VO Ementa que causaria
            // InvalidCastException. Wildcards do termo sao escapados para tratar % e _ como literais.
            // BUG-3: normaliza o termo (lowercase + sem diacriticos) igual a coluna-sombra EmentaBusca,
            // para que "acacias" case "Acácias" e "sao joao" case "São João" sem depender da collation.
            var padrao = "%" + EscaparLike(TextoBusca.Normalizar(filtro.Termo)) + "%";
            consulta = consulta.Where(norma =>
                EF.Functions.Like(EF.Property<string>(norma, "EmentaBusca"), padrao, "\\"));
        }

        if (filtro.Tipo is { } tipo)
        {
            consulta = consulta.Where(norma => norma.Tipo == tipo);
        }

        if (filtro.Numero is { } numero)
        {
            consulta = consulta.Where(norma => norma.Numero == numero);
        }

        if (filtro.Ano is { } ano)
        {
            consulta = consulta.Where(norma => norma.Ano == ano);
        }

        if (filtro.Situacao is { } situacao)
        {
            consulta = consulta.Where(norma => norma.SituacaoVigencia == situacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderByDescending(norma => norma.Ano)
            .ThenByDescending(norma => norma.Numero)
            .Skip((filtro.Pagina - 1) * filtro.Tamanho)
            .Take(filtro.Tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    // Escapa os curingas do LIKE (\, %, _) para que o termo do usuario seja tratado como literal.
    private static string EscaparLike(string termo)
        => termo.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="EdicaoDiario"/>.</summary>
public sealed class EdicaoDiarioRepository(LegislativoDbContext context) : IEdicaoDiarioRepository
{
    /// <inheritdoc />
    public void Adicionar(EdicaoDiario edicao)
    {
        ArgumentNullException.ThrowIfNull(edicao);
        context.DiarioEdicoes.Add(edicao);
    }

    /// <inheritdoc />
    public Task<EdicaoDiario?> ObterPorIdAsync(EdicaoDiarioId id, CancellationToken cancellationToken)
        => context.DiarioEdicoes.FirstOrDefaultAsync(edicao => edicao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<int> ProximoNumeroAsync(int ano, CancellationToken cancellationToken)
    {
        var maximo = await context.DiarioEdicoes
            .Where(edicao => edicao.Ano == ano)
            .Select(edicao => (int?)edicao.Numero)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        return (maximo ?? 0) + 1;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<EdicaoDiario> Itens, int Total)> ListarAsync(
        int? ano,
        SituacaoEdicao? situacao,
        bool apenasPublicadas,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.DiarioEdicoes.AsQueryable();

        if (apenasPublicadas)
        {
            consulta = consulta.Where(edicao => edicao.Situacao == SituacaoEdicao.Publicada);
        }
        else if (situacao is { } s)
        {
            consulta = consulta.Where(edicao => edicao.Situacao == s);
        }

        if (ano is { } a)
        {
            consulta = consulta.Where(edicao => edicao.Ano == a);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderByDescending(edicao => edicao.Ano)
            .ThenByDescending(edicao => edicao.Numero)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="TribunaSessao"/>.</summary>
public sealed class TribunaSessaoRepository(LegislativoDbContext context) : ITribunaSessaoRepository
{
    /// <inheritdoc />
    public void Adicionar(TribunaSessao tribuna)
    {
        ArgumentNullException.ThrowIfNull(tribuna);
        context.Tribunas.Add(tribuna);
    }

    /// <inheritdoc />
    public Task<TribunaSessao?> ObterPorIdAsync(TribunaSessaoId id, CancellationToken cancellationToken)
        => context.Tribunas.FirstOrDefaultAsync(tribuna => tribuna.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<TribunaSessao?> ObterPorSessaoAsync(SessaoId sessaoId, CancellationToken cancellationToken)
        => context.Tribunas.FirstOrDefaultAsync(tribuna => tribuna.SessaoId == sessaoId, cancellationToken);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Comissao"/>.</summary>
public sealed class ComissaoRepository(LegislativoDbContext context) : IComissaoRepository
{
    /// <inheritdoc />
    public void Adicionar(Comissao comissao)
    {
        ArgumentNullException.ThrowIfNull(comissao);
        context.Comissoes.Add(comissao);
    }

    /// <inheritdoc />
    public Task<Comissao?> ObterPorIdAsync(ComissaoId id, CancellationToken cancellationToken)
        => context.Comissoes.FirstOrDefaultAsync(comissao => comissao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Comissao>> ListarAsync(CancellationToken cancellationToken)
        => await context.Comissoes
            .OrderBy(comissao => comissao.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ApuracaoArt29A"/>.</summary>
public sealed class ApuracaoArt29ARepository(LegislativoDbContext context) : IApuracaoArt29ARepository
{
    /// <inheritdoc />
    public void Adicionar(ApuracaoArt29A apuracao)
    {
        ArgumentNullException.ThrowIfNull(apuracao);
        context.ApuracoesArt29A.Add(apuracao);
    }

    /// <inheritdoc />
    public Task<ApuracaoArt29A?> ObterPorIdAsync(ApuracaoArt29AId id, CancellationToken cancellationToken)
        => context.ApuracoesArt29A
            .Include(apuracao => apuracao.Despesas)
            .FirstOrDefaultAsync(apuracao => apuracao.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<ApuracaoArt29A?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.ApuracoesArt29A
            .Include(apuracao => apuracao.Despesas)
            .FirstOrDefaultAsync(apuracao => apuracao.Exercicio == exercicio, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteParaExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.ApuracoesArt29A.AnyAsync(apuracao => apuracao.Exercicio == exercicio, cancellationToken);
}

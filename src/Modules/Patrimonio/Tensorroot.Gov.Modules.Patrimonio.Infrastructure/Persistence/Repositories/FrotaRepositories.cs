using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório do agregado <see cref="Pneu"/>.</summary>
public sealed class PneuRepository(PatrimonioDbContext context) : IPneuRepository
{
    /// <inheritdoc />
    public void Adicionar(Pneu pneu)
    {
        ArgumentNullException.ThrowIfNull(pneu);
        context.Pneus.Add(pneu);
    }

    /// <inheritdoc />
    public Task<Pneu?> ObterPorIdAsync(PneuId id, CancellationToken cancellationToken)
        => context.Pneus.FirstOrDefaultAsync(pneu => pneu.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteNumeroFogoAsync(string numeroFogo, CancellationToken cancellationToken)
        => context.Pneus.AnyAsync(pneu => pneu.NumeroFogo == numeroFogo, cancellationToken);

    /// <inheritdoc />
    public Task<bool> PosicaoOcupadaAsync(VeiculoId veiculoId, PosicaoPneu posicao, PneuId exceto, CancellationToken cancellationToken)
        => context.Pneus.AnyAsync(
            pneu => pneu.Situacao == SituacaoPneu.Instalado
                && pneu.VeiculoAtualId == veiculoId
                && pneu.PosicaoAtual == posicao
                && pneu.Id != exceto,
            cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Pneu> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoPneu? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Pneus.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // Busca em NumeroFogo/Marca/Modelo/Medida (colunas string reais), case-insensitive via collation.
            var padrao = BuscaTexto.MontarPadraoContains(termo);
            consulta = consulta.Where(pneu =>
                EF.Functions.Like(pneu.NumeroFogo, padrao, "\\")
                || EF.Functions.Like(pneu.Marca, padrao, "\\")
                || EF.Functions.Like(pneu.Modelo, padrao, "\\")
                || EF.Functions.Like(pneu.Medida, padrao, "\\"));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(pneu => pneu.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(pneu => pneu.NumeroFogo)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pneu>> ListarInstaladosDoVeiculoAsync(VeiculoId veiculoId, CancellationToken cancellationToken)
        => await context.Pneus
            .Where(pneu => pneu.Situacao == SituacaoPneu.Instalado && pneu.VeiculoAtualId == veiculoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pneu>> ListarNoLimiteDeSulcoAsync(decimal sulcoMinimoMilimetros, CancellationToken cancellationToken)
    {
        // Pneus em rodagem (instalados) cujo sulco atual atingiu o mínimo legal ou ficou abaixo dele.
        // O sulco é VO convertido para decimal: a comparação traduz para a coluna subjacente.
        var instalados = await context.Pneus
            .Where(pneu => pneu.Situacao == SituacaoPneu.Instalado)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return instalados
            .Where(pneu => pneu.SulcoAtual.AtingiuMinimoLegal(sulcoMinimoMilimetros))
            .OrderBy(pneu => pneu.SulcoAtual.Milimetros)
            .ToList();
    }
}

/// <summary>Implementação EF Core do repositório do agregado <see cref="Apolice"/>.</summary>
public sealed class ApoliceRepository(PatrimonioDbContext context) : IApoliceRepository
{
    /// <inheritdoc />
    public void Adicionar(Apolice apolice)
    {
        ArgumentNullException.ThrowIfNull(apolice);
        context.Apolices.Add(apolice);
    }

    /// <inheritdoc />
    public Task<Apolice?> ObterPorIdAsync(ApoliceId id, CancellationToken cancellationToken)
        => context.Apolices.FirstOrDefaultAsync(apolice => apolice.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Apolice>> ListarDoVeiculoAsync(VeiculoId veiculoId, CancellationToken cancellationToken)
        => await context.Apolices
            .Where(apolice => apolice.VeiculoId == veiculoId)
            .OrderByDescending(apolice => apolice.FimVigencia)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApoliceVencendoLinha>> ListarVencendoAsync(
        DateOnly referencia,
        DateOnly ate,
        CancellationToken cancellationToken)
    {
        // Apólices vigentes com fim de vigência até o limite (inclui já vencidas, com dias negativos),
        // acompanhadas da placa do veículo segurado. Join Apolice -> Veiculo (mesmo contexto; Global Query
        // Filter aplica o tenant em ambos). Placa é VO convertido: materializa-se a projeção em memória.
        var linhas = await (
            from apolice in context.Apolices
            where apolice.Situacao == SituacaoApolice.Vigente && apolice.FimVigencia <= ate
            join veiculo in context.Veiculos on apolice.VeiculoId equals veiculo.Id
            select new { apolice, veiculo })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return linhas
            .Select(linha => new ApoliceVencendoLinha(
                linha.apolice.Id.Value,
                linha.apolice.VeiculoId.Value,
                linha.veiculo.Placa.Valor,
                linha.apolice.Categoria.ToString(),
                linha.apolice.Seguradora,
                linha.apolice.NumeroApolice,
                linha.apolice.FimVigencia,
                linha.apolice.DiasParaVencer(referencia),
                linha.apolice.Situacao.ToString()))
            .OrderBy(linha => linha.DiasParaVencer)
            .ToList();
    }
}

/// <summary>Implementação EF Core do repositório do agregado <see cref="Condutor"/>.</summary>
public sealed class CondutorRepository(PatrimonioDbContext context) : ICondutorRepository
{
    /// <inheritdoc />
    public void Adicionar(Condutor condutor)
    {
        ArgumentNullException.ThrowIfNull(condutor);
        context.Condutores.Add(condutor);
    }

    /// <inheritdoc />
    public Task<Condutor?> ObterPorIdAsync(CondutorId id, CancellationToken cancellationToken)
        => context.Condutores.FirstOrDefaultAsync(condutor => condutor.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteCnhAsync(string numeroCnh, CancellationToken cancellationToken)
        => context.Condutores.AnyAsync(condutor => condutor.NumeroCnh == numeroCnh, cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Condutor> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoCondutor? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Condutores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // Busca em Nome/Cpf/NumeroCnh (colunas string reais), case-insensitive via collation.
            var padrao = BuscaTexto.MontarPadraoContains(termo);
            consulta = consulta.Where(condutor =>
                EF.Functions.Like(condutor.Nome, padrao, "\\")
                || EF.Functions.Like(condutor.Cpf, padrao, "\\")
                || EF.Functions.Like(condutor.NumeroCnh, padrao, "\\"));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(condutor => condutor.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(condutor => condutor.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Condutor>> ListarCnhVencendoAsync(
        DateOnly referencia,
        DateOnly ate,
        CancellationToken cancellationToken)
        => await context.Condutores
            .Where(condutor => condutor.Situacao != SituacaoCondutor.Inativo && condutor.ValidadeCnh <= ate)
            .OrderBy(condutor => condutor.ValidadeCnh)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

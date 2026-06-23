using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório do agregado <see cref="BemPatrimonial"/>.</summary>
public sealed class BemPatrimonialRepository(PatrimonioDbContext context) : IBemPatrimonialRepository
{
    /// <inheritdoc />
    public void Adicionar(BemPatrimonial bemPatrimonial)
    {
        ArgumentNullException.ThrowIfNull(bemPatrimonial);
        context.Bens.Add(bemPatrimonial);
    }

    /// <inheritdoc />
    public Task<BemPatrimonial?> ObterPorIdAsync(BemPatrimonialId id, CancellationToken cancellationToken)
        => context.Bens.FirstOrDefaultAsync(bem => bem.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BemPatrimonial>> ListarDepreciaveisAsync(CancellationToken cancellationToken)
        // BUG-P3: bem Cedido permanece no acervo e deprecia (MCASP) — filtro aceita Tombado OU Cedido.
        => await context.Bens
            .Where(bem => (bem.Situacao == SituacaoBemPatrimonial.Tombado || bem.Situacao == SituacaoBemPatrimonial.Cedido)
                && bem.EmCondicoesDeUso)
            .OrderBy(bem => bem.DataIncorporacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório do agregado <see cref="Veiculo"/>.</summary>
public sealed class VeiculoRepository(PatrimonioDbContext context) : IVeiculoRepository
{
    /// <inheritdoc />
    public void Adicionar(Veiculo veiculo)
    {
        ArgumentNullException.ThrowIfNull(veiculo);
        context.Veiculos.Add(veiculo);
    }

    /// <inheritdoc />
    public Task<Veiculo?> ObterPorIdAsync(VeiculoId id, CancellationToken cancellationToken)
        => context.Veiculos.FirstOrDefaultAsync(veiculo => veiculo.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteRenavamAsync(Domain.ValueObjects.Renavam renavam, CancellationToken cancellationToken)
        => context.Veiculos.AnyAsync(veiculo => veiculo.Renavam == renavam, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Abastecimento>> ListarAbastecimentosAsync(
        VeiculoId veiculoId,
        DateOnly de,
        DateOnly ate,
        CancellationToken cancellationToken)
    {
        var veiculo = await context.Veiculos
            .FirstOrDefaultAsync(item => item.Id == veiculoId, cancellationToken)
            .ConfigureAwait(false);

        if (veiculo is null)
        {
            return [];
        }

        return veiculo.Abastecimentos
            .Where(abastecimento => abastecimento.Data >= de && abastecimento.Data <= ate)
            .OrderBy(abastecimento => abastecimento.Data)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MultaComVeiculo>> ListarMultasPendentesAsync(CancellationToken cancellationToken)
    {
        var veiculos = await context.Veiculos
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return veiculos
            .SelectMany(veiculo => veiculo.Multas
                .Where(multa => multa.Situacao != SituacaoMulta.Paga)
                .Select(multa => new MultaComVeiculo(
                    multa.Id.Value,
                    veiculo.Id.Value,
                    veiculo.Placa.Valor,
                    multa.CodigoInfracaoCtb,
                    multa.Valor.Valor,
                    multa.DataInfracao)))
            .OrderBy(multa => multa.DataInfracao)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LicenciamentoPendente>> ListarLicenciamentosPendentesAsync(
        int exercicio,
        CancellationToken cancellationToken)
    {
        var veiculos = await context.Veiculos
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return veiculos
            .Where(veiculo => !veiculo.Licenciamentos.Any(
                licenciamento => licenciamento.Exercicio == exercicio
                    && licenciamento.Situacao == SituacaoLicenciamento.Regular))
            .Select(veiculo => new LicenciamentoPendente(
                veiculo.Id.Value,
                veiculo.Placa.Valor,
                exercicio,
                veiculo.Licenciamentos
                    .Where(licenciamento => licenciamento.Exercicio == exercicio)
                    .Select(licenciamento => licenciamento.ValorIpva.Valor)
                    .FirstOrDefault(),
                SituacaoLicenciamento.Pendente.ToString()))
            .OrderBy(pendente => pendente.Placa)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Veiculo>> ListarDepreciaveisAsync(CancellationToken cancellationToken)
        // BUG-P4: veículo é-um bem patrimonial e deprecia por MCASP — ativo no acervo e em condições de uso.
        => await context.Veiculos
            .Where(veiculo => (veiculo.Situacao == SituacaoBemPatrimonial.Tombado
                    || veiculo.Situacao == SituacaoBemPatrimonial.Cedido)
                && veiculo.EmCondicoesDeUso)
            .OrderBy(veiculo => veiculo.DataIncorporacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório do agregado <see cref="ItemEstoque"/>.</summary>
public sealed class ItemEstoqueRepository(PatrimonioDbContext context) : IItemEstoqueRepository
{
    /// <inheritdoc />
    public void Adicionar(ItemEstoque item)
    {
        ArgumentNullException.ThrowIfNull(item);
        context.ItensEstoque.Add(item);
    }

    /// <inheritdoc />
    public Task<ItemEstoque?> ObterPorIdAsync(ItemEstoqueId id, CancellationToken cancellationToken)
        => context.ItensEstoque.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> CodigoExisteAsync(string codigo, CancellationToken cancellationToken)
        => context.ItensEstoque.AnyAsync(item => item.Codigo == codigo, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemEstoque>> ListarAbaixoDoPontoPedidoAsync(CancellationToken cancellationToken)
    {
        var itens = await context.ItensEstoque
            .Where(item => item.Situacao == SituacaoItemEstoque.Ativo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return itens
            .Where(item => item.PontoPedido.FoiAtingidoPor(item.Saldo))
            .OrderBy(item => item.Codigo)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemEstoque>> ListarAtivosAsync(CancellationToken cancellationToken)
        => await context.ItensEstoque
            .Where(item => item.Situacao == SituacaoItemEstoque.Ativo)
            .OrderBy(item => item.Codigo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

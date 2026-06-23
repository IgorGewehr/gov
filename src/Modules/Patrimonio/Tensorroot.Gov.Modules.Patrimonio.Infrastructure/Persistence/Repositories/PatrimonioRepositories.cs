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

    /// <inheritdoc />
    public async Task<(IReadOnlyList<BemPatrimonial> Itens, int Total)> BuscarAsync(
        string? termo,
        TipoBem? tipo,
        SituacaoBemPatrimonial? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Bens.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // Busca em Descricao e NumeroTombamento (colunas string reais), case-insensitive via LOWER().
            var padrao = BuscaTexto.MontarPadraoContains(termo);
            consulta = consulta.Where(bem =>
                EF.Functions.Like(bem.Descricao, padrao, "\\")
                || (EF.Property<string?>(bem, "TombamentoBusca") != null
                    && EF.Functions.Like(EF.Property<string>(bem, "TombamentoBusca"), padrao, "\\")));
        }

        if (tipo is { } filtroTipo)
        {
            consulta = consulta.Where(bem => bem.Tipo == filtroTipo);
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(bem => bem.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(bem => bem.Descricao)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
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

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Veiculo> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoBemPatrimonial? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Veiculos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // Busca em Descricao, Placa e Renavam (colunas string reais), case-insensitive via LOWER().
            var padrao = BuscaTexto.MontarPadraoContains(termo);
            consulta = consulta.Where(veiculo =>
                EF.Functions.Like(veiculo.Descricao, padrao, "\\")
                || EF.Functions.Like(EF.Property<string>(veiculo, "PlacaBusca"), padrao, "\\")
                || EF.Functions.Like(EF.Property<string>(veiculo, "RenavamBusca"), padrao, "\\"));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(veiculo => veiculo.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(veiculo => EF.Property<string>(veiculo, "PlacaBusca"))
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustoVeiculoLinha>> ProjetarCustosPorVeiculoAsync(
        DateOnly de,
        DateOnly ate,
        CancellationToken cancellationToken)
    {
        // As coleções (abastecimentos/OS/multas) são owned tables do agregado; materializamos os veículos
        // (Global Query Filter aplica o tenant) e agregamos em memória — frota municipal tem cardinalidade
        // baixa e as coleções já vêm carregadas com o agregado.
        var veiculos = await context.Veiculos
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return veiculos
            .Select(veiculo =>
            {
                var abastecimentos = veiculo.Abastecimentos
                    .Where(a => a.Data >= de && a.Data <= ate)
                    .ToList();

                var gastoCombustivel = abastecimentos.Sum(a => a.Valor.Valor);
                var litros = abastecimentos.Sum(a => a.Litros);

                var gastoManutencao = veiculo.OrdensServico
                    .Where(os => os.Situacao == SituacaoOrdemServico.Concluida
                        && os.DataConclusao is { } conclusao
                        && conclusao >= de && conclusao <= ate)
                    .Sum(os => os.CustoRealizado!.Valor);

                var gastoMultas = veiculo.Multas
                    .Where(m => m.DataInfracao >= de && m.DataInfracao <= ate)
                    .Sum(m => m.Valor.Valor);

                // Km rodados no período: diferença entre o maior e o menor odômetro dos abastecimentos
                // do período (proxy de operação; consumo só é determinável com >= 2 abastecimentos e litros > 0).
                var kmRodados = 0;
                decimal? consumo = null;
                if (abastecimentos.Count >= 2)
                {
                    var odometros = abastecimentos.Select(a => a.Odometro.Valor).ToList();
                    kmRodados = odometros.Max() - odometros.Min();
                    if (litros > 0m && kmRodados > 0)
                    {
                        consumo = Math.Round(kmRodados / litros, 2);
                    }
                }

                return new CustoVeiculoLinha(
                    veiculo.Id.Value,
                    veiculo.Placa.Valor,
                    veiculo.Descricao,
                    gastoCombustivel,
                    litros,
                    gastoManutencao,
                    gastoMultas,
                    kmRodados,
                    consumo);
            })
            .OrderByDescending(linha => linha.CustoTotal)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CnhVencendoLinha>> ListarCnhVencendoAsync(
        DateOnly referencia,
        DateOnly ate,
        CancellationToken cancellationToken)
    {
        var veiculos = await context.Veiculos
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return veiculos
            .SelectMany(veiculo => veiculo.Motoristas
                .Where(motorista => motorista.ValidadeCnh <= ate)
                .Select(motorista => new CnhVencendoLinha(
                    veiculo.Id.Value,
                    veiculo.Placa.Valor,
                    motorista.Id.Value,
                    motorista.Nome,
                    motorista.Cnh,
                    motorista.CategoriaCnh,
                    motorista.ValidadeCnh,
                    motorista.ValidadeCnh.DayNumber - referencia.DayNumber)))
            .OrderBy(linha => linha.ValidadeCnh)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ManutencaoAbertaLinha>> ListarManutencoesAbertasAsync(CancellationToken cancellationToken)
    {
        var veiculos = await context.Veiculos
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return veiculos
            .SelectMany(veiculo => veiculo.OrdensServico
                .Where(os => os.Situacao == SituacaoOrdemServico.Aberta)
                .Select(os => new ManutencaoAbertaLinha(
                    veiculo.Id.Value,
                    veiculo.Placa.Valor,
                    os.Id.Value,
                    os.Descricao,
                    os.CustoEstimado.Valor,
                    os.Odometro.Valor)))
            .OrderBy(linha => linha.Placa)
            .ToList();
    }
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

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ItemEstoque> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoItemEstoque? situacao,
        CurvaABC? classificacaoAbc,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.ItensEstoque.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // Busca em Codigo e Descricao (colunas string reais), case-insensitive via LOWER().
            var padrao = BuscaTexto.MontarPadraoContains(termo);
            consulta = consulta.Where(item =>
                EF.Functions.Like(item.Codigo, padrao, "\\")
                || EF.Functions.Like(item.Descricao, padrao, "\\"));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(item => item.Situacao == filtroSituacao);
        }

        if (classificacaoAbc is { } filtroAbc)
        {
            consulta = consulta.Where(item => item.ClassificacaoAbc == filtroAbc);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(item => item.Codigo)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

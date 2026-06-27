using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota.Pneus;

/// <summary>Resumo de um pneu em listagem (busca/painel).</summary>
/// <param name="Id">Identificador do pneu.</param>
/// <param name="NumeroFogo">Número de fogo.</param>
/// <param name="Marca">Marca.</param>
/// <param name="Modelo">Modelo.</param>
/// <param name="Medida">Medida.</param>
/// <param name="Situacao">Situação no ciclo de vida.</param>
/// <param name="SulcoAtualMilimetros">Sulco atual aferido (mm).</param>
/// <param name="PercentualBandaRemanescente">Percentual de banda remanescente (0–100).</param>
/// <param name="KmAcumulado">Quilômetros totais rodados.</param>
/// <param name="Recapagens">Número de recapagens.</param>
/// <param name="CustoPorKm">Custo por km rodado (nulo se sem rodagem).</param>
/// <param name="VeiculoAtualId">Veículo onde está instalado (nulo se não instalado).</param>
/// <param name="PosicaoAtual">Código da posição atual (nulo se não instalado).</param>
public sealed record PneuResumo(
    Guid Id,
    string NumeroFogo,
    string Marca,
    string Modelo,
    string Medida,
    string Situacao,
    decimal SulcoAtualMilimetros,
    decimal PercentualBandaRemanescente,
    int KmAcumulado,
    int Recapagens,
    decimal? CustoPorKm,
    Guid? VeiculoAtualId,
    string? PosicaoAtual);

/// <summary>Detalhe completo de um pneu.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="NumeroFogo">Número de fogo.</param>
/// <param name="Marca">Marca.</param>
/// <param name="Modelo">Modelo.</param>
/// <param name="Medida">Medida.</param>
/// <param name="Dot">Código DOT.</param>
/// <param name="Situacao">Situação no ciclo de vida.</param>
/// <param name="SulcoNovoMilimetros">Sulco de fábrica/recapado (mm).</param>
/// <param name="SulcoAtualMilimetros">Sulco atual aferido (mm).</param>
/// <param name="PercentualBandaRemanescente">Percentual de banda remanescente (0–100).</param>
/// <param name="VidaUtilKmEstimada">Vida útil estimada por km.</param>
/// <param name="KmAcumulado">Quilômetros totais rodados.</param>
/// <param name="Recapagens">Número de recapagens.</param>
/// <param name="ValorAquisicao">Valor de aquisição.</param>
/// <param name="CustoRecapagens">Custo acumulado de recapagens.</param>
/// <param name="CustoTotal">Custo total (aquisição + recapagens).</param>
/// <param name="CustoPorKm">Custo por km rodado (nulo se sem rodagem).</param>
/// <param name="VeiculoAtualId">Veículo onde está instalado.</param>
/// <param name="PosicaoAtual">Código da posição atual.</param>
/// <param name="DataAquisicao">Data de aquisição.</param>
public sealed record PneuDetalhe(
    Guid Id,
    string NumeroFogo,
    string Marca,
    string Modelo,
    string Medida,
    string? Dot,
    string Situacao,
    decimal SulcoNovoMilimetros,
    decimal SulcoAtualMilimetros,
    decimal PercentualBandaRemanescente,
    int VidaUtilKmEstimada,
    int KmAcumulado,
    int Recapagens,
    decimal ValorAquisicao,
    decimal CustoRecapagens,
    decimal CustoTotal,
    decimal? CustoPorKm,
    Guid? VeiculoAtualId,
    string? PosicaoAtual,
    DateOnly DataAquisicao);

/// <summary>Pneu posicionado no layout de eixos de um veículo.</summary>
/// <param name="PneuId">Identificador do pneu.</param>
/// <param name="NumeroFogo">Número de fogo.</param>
/// <param name="Eixo">Eixo de montagem.</param>
/// <param name="Lado">Lado/posição de montagem.</param>
/// <param name="PosicaoCodigo">Código curto da posição (ex.: "E1-ESQ").</param>
/// <param name="SulcoAtualMilimetros">Sulco atual (mm).</param>
/// <param name="PercentualBandaRemanescente">Percentual de banda remanescente (0–100).</param>
public sealed record PneuNaPosicao(
    Guid PneuId,
    string NumeroFogo,
    Eixo Eixo,
    LadoMontagem Lado,
    string PosicaoCodigo,
    decimal SulcoAtualMilimetros,
    decimal PercentualBandaRemanescente);

/// <summary>Busca paginada de pneus por termo/situação.</summary>
/// <param name="Termo">Termo livre (número de fogo/marca/modelo/medida); nulo lista tudo.</param>
/// <param name="Situacao">Filtro opcional por situação.</param>
/// <param name="Pagina">Página (base 1).</param>
/// <param name="Tamanho">Tamanho da página.</param>
public sealed record BuscarPneusQuery(string? Termo, SituacaoPneu? Situacao, int Pagina, int Tamanho)
    : IQuery<PaginaPneus>;

/// <summary>Página de pneus.</summary>
/// <param name="Itens">Itens da página.</param>
/// <param name="Total">Total que atende ao filtro.</param>
/// <param name="Pagina">Página corrente.</param>
/// <param name="Tamanho">Tamanho da página.</param>
public sealed record PaginaPneus(IReadOnlyList<PneuResumo> Itens, int Total, int Pagina, int Tamanho);

/// <summary>Handler da busca de pneus.</summary>
public sealed class BuscarPneusHandler(IPneuRepository pneus) : IQueryHandler<BuscarPneusQuery, PaginaPneus>
{
    private const int TamanhoMaximo = 100;

    /// <inheritdoc />
    public async Task<PaginaPneus> Handle(BuscarPneusQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pagina = request.Pagina < 1 ? 1 : request.Pagina;
        var tamanho = request.Tamanho is < 1 or > TamanhoMaximo ? 20 : request.Tamanho;

        var (itens, total) = await pneus
            .BuscarAsync(request.Termo, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        return new PaginaPneus(itens.Select(MapearResumo).ToList(), total, pagina, tamanho);
    }

    internal static PneuResumo MapearResumo(Pneu pneu) => new(
        pneu.Id.Value,
        pneu.NumeroFogo,
        pneu.Marca,
        pneu.Modelo,
        pneu.Medida,
        pneu.Situacao.ToString(),
        pneu.SulcoAtual.Milimetros,
        pneu.PercentualBandaRemanescente,
        pneu.KmAcumulado,
        pneu.Recapagens,
        pneu.CustoPorKm,
        pneu.VeiculoAtualId?.Value,
        pneu.PosicaoAtual?.Codigo);
}

/// <summary>Obtém o detalhe de um pneu.</summary>
/// <param name="PneuId">Identificador do pneu.</param>
public sealed record ObterPneuQuery(Guid PneuId) : IQuery<PneuDetalhe?>;

/// <summary>Handler do detalhe de pneu.</summary>
public sealed class ObterPneuHandler(IPneuRepository pneus) : IQueryHandler<ObterPneuQuery, PneuDetalhe?>
{
    /// <inheritdoc />
    public async Task<PneuDetalhe?> Handle(ObterPneuQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pneu = await pneus.ObterPorIdAsync(new PneuId(request.PneuId), cancellationToken).ConfigureAwait(false);
        if (pneu is null)
        {
            return null;
        }

        return new PneuDetalhe(
            pneu.Id.Value,
            pneu.NumeroFogo,
            pneu.Marca,
            pneu.Modelo,
            pneu.Medida,
            pneu.Dot,
            pneu.Situacao.ToString(),
            pneu.SulcoNovo.Milimetros,
            pneu.SulcoAtual.Milimetros,
            pneu.PercentualBandaRemanescente,
            pneu.VidaUtilKmEstimada,
            pneu.KmAcumulado,
            pneu.Recapagens,
            pneu.ValorAquisicao.Valor,
            pneu.CustoRecapagens.Valor,
            pneu.CustoTotal,
            pneu.CustoPorKm,
            pneu.VeiculoAtualId?.Value,
            pneu.PosicaoAtual?.Codigo,
            pneu.DataAquisicao);
    }
}

/// <summary>Lista o layout de pneus instalados num veículo (posicionamento por eixo/lado).</summary>
/// <param name="VeiculoId">Veículo.</param>
public sealed record ObterLayoutPneusDoVeiculoQuery(Guid VeiculoId) : IQuery<IReadOnlyList<PneuNaPosicao>>;

/// <summary>Handler do layout de pneus do veículo.</summary>
public sealed class ObterLayoutPneusDoVeiculoHandler(IPneuRepository pneus)
    : IQueryHandler<ObterLayoutPneusDoVeiculoQuery, IReadOnlyList<PneuNaPosicao>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PneuNaPosicao>> Handle(
        ObterLayoutPneusDoVeiculoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var instalados = await pneus
            .ListarInstaladosDoVeiculoAsync(new VeiculoId(request.VeiculoId), cancellationToken)
            .ConfigureAwait(false);

        return instalados
            .Where(pneu => pneu.PosicaoAtual is not null)
            .Select(pneu =>
            {
                var posicao = pneu.PosicaoAtual!.Value;
                return new PneuNaPosicao(
                    pneu.Id.Value,
                    pneu.NumeroFogo,
                    posicao.Eixo,
                    posicao.Lado,
                    posicao.Codigo,
                    pneu.SulcoAtual.Milimetros,
                    pneu.PercentualBandaRemanescente);
            })
            .OrderBy(linha => linha.Eixo)
            .ThenBy(linha => linha.Lado)
            .ToList();
    }
}

/// <summary>Lista os pneus em rodagem no limite (ou abaixo) do sulco mínimo legal — candidatos a troca/recapagem.</summary>
public sealed record ListarPneusNoLimiteDeSulcoQuery : IQuery<IReadOnlyList<PneuResumo>>;

/// <summary>Handler dos pneus no limite de sulco. Lê o sulco mínimo parametrizado por tenant.</summary>
public sealed class ListarPneusNoLimiteDeSulcoHandler(IPneuRepository pneus, IFrotaParametrosProvider parametros)
    : IQueryHandler<ListarPneusNoLimiteDeSulcoQuery, IReadOnlyList<PneuResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PneuResumo>> Handle(
        ListarPneusNoLimiteDeSulcoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sulcoMinimo = parametros.SulcoMinimoLegalMilimetros();
        var noLimite = await pneus.ListarNoLimiteDeSulcoAsync(sulcoMinimo, cancellationToken).ConfigureAwait(false);
        return noLimite.Select(BuscarPneusHandler.MapearResumo).ToList();
    }
}

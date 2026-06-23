using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Common;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Projecao de leitura de um afastamento (ficha/lista).</summary>
/// <param name="Id">Identificador do afastamento.</param>
/// <param name="ServidorId">Servidor afastado.</param>
/// <param name="Tipo">Tipo legal do afastamento.</param>
/// <param name="Inicio">Inicio.</param>
/// <param name="FimPrevisto">Fim previsto (nulo quando indeterminado).</param>
/// <param name="FimEfetivo">Fim efetivo (nulo enquanto vigente).</param>
/// <param name="Situacao">Situacao no ciclo de vida.</param>
/// <param name="SuspendeProventos">Se o ente suspende proventos.</param>
/// <param name="PercentualRemuneracao">Percentual mantido pelo ente.</param>
/// <param name="DiasPagosPeloEnte">Dias iniciais pagos pelo ente.</param>
/// <param name="ContaTempo">Se conta tempo de servico.</param>
/// <param name="Documento">Documento de respaldo.</param>
public sealed record AfastamentoResumo(
    Guid Id,
    Guid ServidorId,
    TipoAfastamento Tipo,
    DateOnly Inicio,
    DateOnly? FimPrevisto,
    DateOnly? FimEfetivo,
    SituacaoAfastamento Situacao,
    bool SuspendeProventos,
    decimal PercentualRemuneracao,
    int DiasPagosPeloEnte,
    bool ContaTempo,
    string? Documento)
{
    /// <summary>Projeta o agregado para o resumo de leitura.</summary>
    /// <param name="a">Afastamento.</param>
    /// <returns>Resumo.</returns>
    public static AfastamentoResumo De(Afastamento a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return new AfastamentoResumo(
            a.Id.Value,
            a.ServidorId.Value,
            a.Tipo,
            a.Inicio,
            a.FimPrevisto,
            a.FimEfetivo,
            a.Situacao,
            a.SuspendeProventos,
            a.PercentualRemuneracao,
            a.DiasPagosPeloEnte,
            a.ContaTempo,
            a.Documento);
    }
}

/// <summary>Lista os afastamentos (historico) de um servidor.</summary>
/// <param name="ServidorId">Servidor.</param>
public sealed record ListarAfastamentosDoServidorQuery(Guid ServidorId) : IQuery<IReadOnlyList<AfastamentoResumo>>;

/// <summary>Handler da listagem de afastamentos do servidor.</summary>
public sealed class ListarAfastamentosDoServidorHandler(IAfastamentoRepository afastamentos)
    : IQueryHandler<ListarAfastamentosDoServidorQuery, IReadOnlyList<AfastamentoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AfastamentoResumo>> Handle(
        ListarAfastamentosDoServidorQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await afastamentos.ListarPorServidorAsync(request.ServidorId, cancellationToken).ConfigureAwait(false);
        return itens.Select(AfastamentoResumo.De).ToList();
    }
}

/// <summary>Busca paginada de afastamentos por tipo/situacao/competencia (navegabilidade).</summary>
/// <param name="Tipo">Filtro opcional por tipo.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Ano">Ano da competencia de filtro (opcional, em par com <paramref name="Mes"/>).</param>
/// <param name="Mes">Mes da competencia de filtro (opcional, em par com <paramref name="Ano"/>).</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarAfastamentosQuery(
    TipoAfastamento? Tipo,
    SituacaoAfastamento? Situacao,
    int? Ano,
    int? Mes,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<AfastamentoResumo>>;

/// <summary>Handler da busca paginada de afastamentos.</summary>
public sealed class BuscarAfastamentosHandler(IAfastamentoRepository afastamentos)
    : IQueryHandler<BuscarAfastamentosQuery, ResultadoPaginado<AfastamentoResumo>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<AfastamentoResumo>> Handle(
        BuscarAfastamentosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);
        var competencia = request.Ano is { } ano && request.Mes is { } mes
            ? Competencia.De(ano, mes)
            : null;

        var (itens, total) = await afastamentos
            .BuscarAsync(request.Tipo, request.Situacao, competencia, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens.Select(AfastamentoResumo.De).ToList();
        return new ResultadoPaginado<AfastamentoResumo>(projetados, total, pagina, tamanho);
    }
}

using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Resumo de uma folha de pagamento por competencia (projecao de leitura).</summary>
/// <param name="Id">Identificador da folha.</param>
/// <param name="Competencia">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="TotalProventos">Soma dos proventos.</param>
/// <param name="TotalDescontos">Soma dos descontos.</param>
/// <param name="TotalLiquido">Total liquido.</param>
/// <param name="DataFechamento">Data do fechamento, se houver.</param>
public sealed record FolhaResumo(
    Guid Id,
    string Competencia,
    string Situacao,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal TotalLiquido,
    DateOnly? DataFechamento);

/// <summary>Obtem a folha da competencia no tenant atual (tenant-scoped).</summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
public sealed record ObterFolhaPorCompetenciaQuery(int Ano, int Mes) : IQuery<FolhaResumo?>;

/// <summary>Handler da consulta de folha por competencia.</summary>
public sealed class ObterFolhaPorCompetenciaHandler(IFolhaDePagamentoRepository folhas)
    : IQueryHandler<ObterFolhaPorCompetenciaQuery, FolhaResumo?>
{
    /// <inheritdoc />
    public async Task<FolhaResumo?> Handle(ObterFolhaPorCompetenciaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var competencia = Competencia.De(request.Ano, request.Mes);
        var folha = await folhas.ObterPorCompetenciaAsync(competencia, cancellationToken).ConfigureAwait(false);
        if (folha is null)
        {
            return null;
        }

        return new FolhaResumo(
            folha.Id.Value,
            folha.Competencia.ToString(),
            folha.Situacao.ToString(),
            folha.TotalProventos,
            folha.TotalDescontos,
            folha.TotalLiquido.Valor,
            folha.DataFechamento);
    }
}

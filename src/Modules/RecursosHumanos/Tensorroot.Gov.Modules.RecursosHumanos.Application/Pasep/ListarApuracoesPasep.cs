using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Pasep;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Pasep;

/// <summary>Resumo de leitura de uma apuracao do PASEP.</summary>
/// <param name="Id">Identificador da apuracao.</param>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
/// <param name="BaseContribuicao">Base de calculo (folha bruta).</param>
/// <param name="Aliquota">Aliquota aplicada (percentual).</param>
/// <param name="Valor">Valor apurado da contribuicao.</param>
/// <param name="Situacao">Situacao atual.</param>
public sealed record ApuracaoPasepResumo(
    Guid Id,
    int Ano,
    int Mes,
    decimal BaseContribuicao,
    decimal Aliquota,
    decimal Valor,
    SituacaoApuracaoPasep Situacao);

/// <summary>Lista as apuracoes de PASEP de um ano (painel/navegabilidade); read-only.</summary>
/// <param name="Ano">Ano das competencias.</param>
public sealed record ListarApuracoesPasepQuery(int Ano) : IQuery<IReadOnlyList<ApuracaoPasepResumo>>;

/// <summary>Handler da listagem de apuracoes de PASEP.</summary>
public sealed class ListarApuracoesPasepHandler(IApuracaoPasepRepository apuracoes)
    : IQueryHandler<ListarApuracoesPasepQuery, IReadOnlyList<ApuracaoPasepResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ApuracaoPasepResumo>> Handle(ListarApuracoesPasepQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await apuracoes.ListarPorAnoAsync(request.Ano, cancellationToken).ConfigureAwait(false);
        return itens
            .Select(a => new ApuracaoPasepResumo(
                a.Id.Value,
                a.Competencia.Ano,
                a.Competencia.Mes,
                a.BaseContribuicao,
                a.Aliquota,
                a.Valor,
                a.Situacao))
            .ToList();
    }
}

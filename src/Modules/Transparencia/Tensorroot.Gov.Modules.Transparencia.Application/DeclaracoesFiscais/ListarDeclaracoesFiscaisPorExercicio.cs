using System.Globalization;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;

/// <summary>Resumo de uma declaracao fiscal do exercicio (secao 6.2 das regras).</summary>
/// <param name="Id">Identificador da declaracao.</param>
/// <param name="TipoDeclaracao">Especie do demonstrativo.</param>
/// <param name="Exercicio">Ano de exercicio.</param>
/// <param name="Periodo">Periodo de referencia (competencia/bimestre/quadrimestre/exercicio).</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataLimite">Prazo legal/parametrizado de transmissao.</param>
/// <param name="DataTransmissao">Data de transmissao ao SICONFI, se transmitida.</param>
public sealed record DeclaracaoFiscalResumo(
    Guid Id,
    string TipoDeclaracao,
    int Exercicio,
    string Periodo,
    string Situacao,
    DateOnly DataLimite,
    DateOnly? DataTransmissao);

/// <summary>Lista as declaracoes fiscais do exercicio (tenant-scoped), com filtros opcionais.</summary>
/// <param name="Exercicio">Ano de exercicio.</param>
/// <param name="Tipo">Filtro opcional por tipo de declaracao.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
public sealed record ListarDeclaracoesFiscaisPorExercicioQuery(
    int Exercicio,
    TipoDeclaracaoFiscal? Tipo,
    SituacaoDeclaracaoFiscal? Situacao) : IQuery<IReadOnlyList<DeclaracaoFiscalResumo>>;

/// <summary>Handler da listagem de declaracoes fiscais por exercicio.</summary>
public sealed class ListarDeclaracoesFiscaisPorExercicioHandler(IDeclaracaoFiscalRepository declaracoes)
    : IQueryHandler<ListarDeclaracoesFiscaisPorExercicioQuery, IReadOnlyList<DeclaracaoFiscalResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DeclaracaoFiscalResumo>> Handle(
        ListarDeclaracoesFiscaisPorExercicioQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var declaracoesExercicio = await declaracoes
            .ListarPorExercicioAsync(request.Exercicio, request.Tipo, request.Situacao, cancellationToken)
            .ConfigureAwait(false);

        return declaracoesExercicio
            .Select(declaracao => new DeclaracaoFiscalResumo(
                declaracao.Id.Value,
                declaracao.TipoDeclaracao.ToString(),
                declaracao.Exercicio,
                DescreverPeriodo(declaracao),
                declaracao.Situacao.ToString(),
                declaracao.DataLimite,
                declaracao.DataTransmissao))
            .ToList();
    }

    private static string DescreverPeriodo(DeclaracaoFiscal declaracao)
        => declaracao.TipoDeclaracao switch
        {
            TipoDeclaracaoFiscal.Msc => declaracao.Competencia?.ToString() ?? string.Empty,
            TipoDeclaracaoFiscal.Rreo => declaracao.Bimestre?.ToString() ?? string.Empty,
            TipoDeclaracaoFiscal.Rgf => declaracao.Quadrimestre?.ToString() ?? string.Empty,
            _ => declaracao.Exercicio.ToString(CultureInfo.InvariantCulture),
        };
}

using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;

namespace Tensorroot.Gov.Modules.Financas.Application.RestosAPagar;

/// <summary>Resumo de um Resto a Pagar para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="EmpenhoId">Empenho de origem.</param>
/// <param name="Classificacao">Processado/Não Processado.</param>
/// <param name="ValorInscrito">Valor inscrito.</param>
/// <param name="ValorLiquidado">Valor liquidado.</param>
/// <param name="ValorPago">Valor pago.</param>
/// <param name="ValorCancelado">Valor cancelado.</param>
/// <param name="SaldoAPagar">Saldo a pagar.</param>
/// <param name="ExercicioOrigem">Exercício de origem.</param>
/// <param name="ExercicioInscricao">Exercício de inscrição.</param>
/// <param name="Situacao">Situação atual.</param>
public sealed record RestoAPagarResumo(
    Guid Id,
    Guid EmpenhoId,
    string Classificacao,
    decimal ValorInscrito,
    decimal ValorLiquidado,
    decimal ValorPago,
    decimal ValorCancelado,
    decimal SaldoAPagar,
    int ExercicioOrigem,
    int ExercicioInscricao,
    string Situacao);

/// <summary>Lista os Restos a Pagar de um exercício de inscrição.</summary>
/// <param name="ExercicioInscricao">Exercício de inscrição.</param>
public sealed record ListarRestosAPagarPorExercicioQuery(int ExercicioInscricao) : IQuery<IReadOnlyList<RestoAPagarResumo>>;

/// <summary>Handler da listagem de Restos a Pagar por exercício de inscrição.</summary>
public sealed class ListarRestosAPagarPorExercicioHandler(IRestoAPagarRepository restos)
    : IQueryHandler<ListarRestosAPagarPorExercicioQuery, IReadOnlyList<RestoAPagarResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RestoAPagarResumo>> Handle(
        ListarRestosAPagarPorExercicioQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontrados = await restos.ListarPorExercicioInscricaoAsync(request.ExercicioInscricao, cancellationToken).ConfigureAwait(false);
        return encontrados
            .Select(r => new RestoAPagarResumo(
                r.Id.Value,
                r.EmpenhoId.Value,
                r.Classificacao.ToString(),
                r.ValorInscrito.Valor,
                r.ValorLiquidado.Valor,
                r.ValorPago.Valor,
                r.ValorCancelado.Valor,
                r.SaldoAPagar.Valor,
                r.ExercicioOrigem,
                r.ExercicioInscricao,
                r.Situacao.ToString()))
            .ToList();
    }
}

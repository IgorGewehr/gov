using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Resumo de um bem depreciável para a competência (usado no processamento mensal).</summary>
/// <param name="Id">Identificador do bem.</param>
/// <param name="NumeroTombamento">Número de tombo.</param>
/// <param name="ValorContabil">Valor contábil atual.</param>
/// <param name="ValorResidual">Resíduo estimado.</param>
/// <param name="ParcelaMensal">Parcela mensal linear de depreciação.</param>
public sealed record BemDepreciavelResumo(
    Guid Id,
    string NumeroTombamento,
    decimal ValorContabil,
    decimal ValorResidual,
    decimal ParcelaMensal);

/// <summary>Lista os bens depreciáveis do tenant para a competência informada.</summary>
/// <param name="Ano">Ano da competência.</param>
/// <param name="Mes">Mês da competência (1 a 12).</param>
public sealed record ListarBensDepreciaveisQuery(int Ano, int Mes) : IQuery<IReadOnlyList<BemDepreciavelResumo>>;

/// <summary>Handler da consulta de bens depreciáveis.</summary>
public sealed class ListarBensDepreciaveisHandler(IBemPatrimonialRepository bens)
    : IQueryHandler<ListarBensDepreciaveisQuery, IReadOnlyList<BemDepreciavelResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BemDepreciavelResumo>> Handle(
        ListarBensDepreciaveisQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var depreciaveis = await bens.ListarDepreciaveisAsync(cancellationToken).ConfigureAwait(false);

        return depreciaveis
            .Select(bem => new BemDepreciavelResumo(
                bem.Id.Value,
                bem.NumeroTombamento?.Valor ?? string.Empty,
                bem.ValorContabil.Valor,
                bem.ValorResidual.Valor,
                bem.ParcelaMensal))
            .ToList();
    }
}

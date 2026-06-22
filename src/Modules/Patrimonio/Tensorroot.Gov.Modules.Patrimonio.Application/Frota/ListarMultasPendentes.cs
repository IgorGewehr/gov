using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Resumo de uma multa pendente para leitura.</summary>
/// <param name="Id">Identificador da multa.</param>
/// <param name="VeiculoId">Veículo autuado.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="CodigoInfracaoCtb">Código de infração do CTB.</param>
/// <param name="Valor">Valor da multa.</param>
/// <param name="DataInfracao">Data da infração.</param>
public sealed record MultaResumo(
    Guid Id,
    Guid VeiculoId,
    string Placa,
    string CodigoInfracaoCtb,
    decimal Valor,
    DateOnly DataInfracao);

/// <summary>Lista as multas não pagas (pendentes/em recurso) do tenant (tenant-scoped).</summary>
public sealed record ListarMultasPendentesQuery : IQuery<IReadOnlyList<MultaResumo>>;

/// <summary>Handler da consulta de multas pendentes.</summary>
public sealed class ListarMultasPendentesHandler(IVeiculoRepository veiculos)
    : IQueryHandler<ListarMultasPendentesQuery, IReadOnlyList<MultaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MultaResumo>> Handle(
        ListarMultasPendentesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var multas = await veiculos.ListarMultasPendentesAsync(cancellationToken).ConfigureAwait(false);

        return multas
            .Select(multa => new MultaResumo(
                multa.MultaId,
                multa.VeiculoId,
                multa.Placa,
                multa.CodigoInfracaoCtb,
                multa.Valor,
                multa.DataInfracao))
            .ToList();
    }
}

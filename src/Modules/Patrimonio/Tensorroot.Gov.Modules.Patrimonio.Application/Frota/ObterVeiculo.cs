using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Projeção de detalhe de um veículo para leitura.</summary>
/// <param name="Id">Identificador do veículo.</param>
/// <param name="NumeroTombamento">Número de tombamento, se tombado.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="Renavam">RENAVAM do veículo.</param>
/// <param name="Odometro">Quilometragem atual.</param>
/// <param name="Horimetro">Horas de uso atuais.</param>
/// <param name="ValorContabil">Valor contábil atual.</param>
/// <param name="Situacao">Situação patrimonial.</param>
/// <param name="MotoristaAtualId">Motorista atualmente designado, se houver.</param>
public sealed record VeiculoDetalhe(
    Guid Id,
    string? NumeroTombamento,
    string Placa,
    string Renavam,
    int Odometro,
    decimal Horimetro,
    decimal ValorContabil,
    string Situacao,
    Guid? MotoristaAtualId);

/// <summary>Obtém o detalhe de um veículo (tenant-scoped).</summary>
/// <param name="VeiculoId">Veículo.</param>
public sealed record ObterVeiculoQuery(Guid VeiculoId) : IQuery<VeiculoDetalhe?>;

/// <summary>Handler da consulta de detalhe do veículo.</summary>
public sealed class ObterVeiculoHandler(IVeiculoRepository veiculos)
    : IQueryHandler<ObterVeiculoQuery, VeiculoDetalhe?>
{
    /// <inheritdoc />
    public async Task<VeiculoDetalhe?> Handle(ObterVeiculoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(request.VeiculoId), cancellationToken).ConfigureAwait(false);
        if (veiculo is null)
        {
            return null;
        }

        return new VeiculoDetalhe(
            veiculo.Id.Value,
            veiculo.NumeroTombamento,
            veiculo.Placa.Valor,
            veiculo.Renavam.Digitos,
            veiculo.Odometro.Valor,
            veiculo.Horimetro.Valor,
            veiculo.ValorContabil.Valor,
            veiculo.Situacao.ToString(),
            veiculo.MotoristaAtualId);
    }
}

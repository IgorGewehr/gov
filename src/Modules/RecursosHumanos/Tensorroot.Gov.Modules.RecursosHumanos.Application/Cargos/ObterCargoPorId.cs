using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;

/// <summary>Detalhe de um cargo para leitura.</summary>
/// <param name="Id">Identificador do cargo.</param>
/// <param name="Denominacao">Denominacao legal.</param>
/// <param name="Tipo">Tipo do cargo.</param>
/// <param name="Vencimento">Valor do vencimento-base.</param>
/// <param name="Lotacao">Denominacao da unidade de lotacao.</param>
/// <param name="Regime">Regime previdenciario.</param>
/// <param name="QuantidadeVagas">Vagas autorizadas.</param>
/// <param name="VagasOcupadas">Vagas providas.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="LeiCriacao">Lei de criacao.</param>
public sealed record CargoDetalhe(
    Guid Id,
    string Denominacao,
    string Tipo,
    decimal Vencimento,
    string Lotacao,
    string Regime,
    int QuantidadeVagas,
    int VagasOcupadas,
    string Situacao,
    string LeiCriacao);

/// <summary>Obtem um cargo por identificador.</summary>
/// <param name="CargoId">Cargo.</param>
public sealed record ObterCargoPorIdQuery(Guid CargoId) : IQuery<CargoDetalhe?>;

/// <summary>Handler da consulta de cargo por identificador.</summary>
public sealed class ObterCargoPorIdHandler(ICargoRepository cargos)
    : IQueryHandler<ObterCargoPorIdQuery, CargoDetalhe?>
{
    /// <inheritdoc />
    public async Task<CargoDetalhe?> Handle(ObterCargoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cargo = await cargos.ObterPorIdAsync(new CargoId(request.CargoId), cancellationToken).ConfigureAwait(false);
        if (cargo is null)
        {
            return null;
        }

        return new CargoDetalhe(
            cargo.Id.Value,
            cargo.Denominacao,
            cargo.Tipo.ToString(),
            cargo.Vencimento.Valor,
            cargo.Lotacao.DenominacaoUnidade,
            cargo.Regime.ToString(),
            cargo.QuantidadeVagas,
            cargo.VagasOcupadas,
            cargo.Situacao.ToString(),
            cargo.LeiCriacao);
    }
}

using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Apolice"/>.</summary>
public interface IApoliceRepository
{
    /// <summary>Marca uma nova apólice para inserção.</summary>
    /// <param name="apolice">Apólice a adicionar.</param>
    void Adicionar(Apolice apolice);

    /// <summary>Obtém uma apólice por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A apólice, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Apolice?> ObterPorIdAsync(ApoliceId id, CancellationToken cancellationToken);

    /// <summary>Lista as apólices de um veículo (todas as categorias e situações).</summary>
    /// <param name="veiculoId">Veículo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Apólices do veículo, mais recentes primeiro.</returns>
    Task<IReadOnlyList<Apolice>> ListarDoVeiculoAsync(VeiculoId veiculoId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista as apólices a vencer dentro da janela informada a partir da data de referência (inclui as
    /// já vencidas, com dias negativos) — base do alerta de renovação. Tenant-scoped.
    /// </summary>
    /// <param name="referencia">Data de referência (hoje).</param>
    /// <param name="ate">Limite superior da janela de vencimento (inclusivo).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Apólices vigentes/vencidas com fim de vigência até o limite, com a placa do veículo.</returns>
    Task<IReadOnlyList<ApoliceVencendoLinha>> ListarVencendoAsync(
        DateOnly referencia,
        DateOnly ate,
        CancellationToken cancellationToken);
}

/// <summary>Linha de apólice a vencer/vencida acompanhada da placa do veículo (alerta de renovação).</summary>
/// <param name="ApoliceId">Identificador da apólice.</param>
/// <param name="VeiculoId">Identificador do veículo segurado.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="Categoria">Categoria do seguro.</param>
/// <param name="Seguradora">Seguradora.</param>
/// <param name="NumeroApolice">Número da apólice.</param>
/// <param name="FimVigencia">Fim da vigência.</param>
/// <param name="DiasParaVencer">Dias até o vencimento (negativo se já vencida).</param>
/// <param name="Situacao">Situação da apólice.</param>
public sealed record ApoliceVencendoLinha(
    Guid ApoliceId,
    Guid VeiculoId,
    string Placa,
    string Categoria,
    string Seguradora,
    string NumeroApolice,
    DateOnly FimVigencia,
    int DiasParaVencer,
    string Situacao);

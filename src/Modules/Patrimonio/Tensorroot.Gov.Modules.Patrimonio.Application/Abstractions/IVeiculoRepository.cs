using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Veiculo"/>.</summary>
public interface IVeiculoRepository
{
    /// <summary>Marca um novo veículo para inserção.</summary>
    /// <param name="veiculo">Veículo a adicionar.</param>
    void Adicionar(Veiculo veiculo);

    /// <summary>Obtém um veículo por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O veículo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Veiculo?> ObterPorIdAsync(VeiculoId id, CancellationToken cancellationToken);

    /// <summary>Indica se já existe um veículo com o RENAVAM informado no tenant (I-2).</summary>
    /// <param name="renavam">RENAVAM a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o RENAVAM já estiver em uso.</returns>
    Task<bool> ExisteRenavamAsync(Domain.ValueObjects.Renavam renavam, CancellationToken cancellationToken);

    /// <summary>Lista os abastecimentos de um veículo em um período.</summary>
    /// <param name="veiculoId">Veículo.</param>
    /// <param name="de">Data inicial (inclusiva).</param>
    /// <param name="ate">Data final (inclusiva).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Abastecimentos do veículo no período.</returns>
    Task<IReadOnlyList<Abastecimento>> ListarAbastecimentosAsync(
        VeiculoId veiculoId,
        DateOnly de,
        DateOnly ate,
        CancellationToken cancellationToken);

    /// <summary>Lista as multas pendentes/em recurso de todos os veículos do tenant.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Multas não pagas, com a placa do veículo.</returns>
    Task<IReadOnlyList<MultaComVeiculo>> ListarMultasPendentesAsync(CancellationToken cancellationToken);

    /// <summary>Lista os veículos sem licenciamento regular no exercício informado (I-11).</summary>
    /// <param name="exercicio">Exercício (ano).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Veículos com pendência de licenciamento no exercício.</returns>
    Task<IReadOnlyList<LicenciamentoPendente>> ListarLicenciamentosPendentesAsync(
        int exercicio,
        CancellationToken cancellationToken);
}

/// <summary>Projeção de leitura de uma multa acompanhada da placa do veículo.</summary>
/// <param name="MultaId">Identificador da multa.</param>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="CodigoInfracaoCtb">Código de infração do CTB.</param>
/// <param name="Valor">Valor da multa.</param>
/// <param name="DataInfracao">Data da infração.</param>
public sealed record MultaComVeiculo(
    Guid MultaId,
    Guid VeiculoId,
    string Placa,
    string CodigoInfracaoCtb,
    decimal Valor,
    DateOnly DataInfracao);

/// <summary>Projeção de leitura de uma pendência de licenciamento por veículo/exercício.</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="Exercicio">Exercício (ano).</param>
/// <param name="ValorIpva">Valor de IPVA conhecido para o exercício (zero se desconhecido).</param>
/// <param name="Situacao">Situação do licenciamento.</param>
public sealed record LicenciamentoPendente(
    Guid VeiculoId,
    string Placa,
    int Exercicio,
    decimal ValorIpva,
    string Situacao);

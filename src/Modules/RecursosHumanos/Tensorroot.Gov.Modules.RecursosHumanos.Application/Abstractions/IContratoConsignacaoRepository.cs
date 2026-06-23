using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="ContratoConsignacao"/> (tenant-scoped via Global Query Filter).</summary>
public interface IContratoConsignacaoRepository
{
    /// <summary>Marca um novo contrato de consignacao para insercao.</summary>
    /// <param name="contrato">Contrato a adicionar.</param>
    void Adicionar(ContratoConsignacao contrato);

    /// <summary>Obtem um contrato por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O contrato, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ContratoConsignacao?> ObterPorIdAsync(ContratoConsignacaoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista as consignacoes AVERBADAS (vigentes) de um servidor — base para apurar o COMPROMETIDO por
    /// balde da margem e para lancar os descontos na folha.
    /// </summary>
    /// <param name="servidorId">Servidor consignante.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contratos averbados do servidor.</returns>
    Task<IReadOnlyList<ContratoConsignacao>> ListarAverbadasDoServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);

    /// <summary>Lista todas as consignacoes de um servidor (historico, qualquer situacao).</summary>
    /// <param name="servidorId">Servidor consignante.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contratos do servidor.</returns>
    Task<IReadOnlyList<ContratoConsignacao>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);
}

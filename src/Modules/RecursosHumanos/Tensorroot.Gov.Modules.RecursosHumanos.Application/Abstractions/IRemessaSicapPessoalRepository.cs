using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="RemessaSicapPessoal"/> (remessas de pessoal SICAP-AP/SIAPES).</summary>
public interface IRemessaSicapPessoalRepository
{
    /// <summary>Marca uma nova remessa para insercao.</summary>
    /// <param name="remessa">Remessa a adicionar.</param>
    void Adicionar(RemessaSicapPessoal remessa);

    /// <summary>Obtem uma remessa (com os atos) por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A remessa, ou <c>null</c> se inexistente no tenant.</returns>
    Task<RemessaSicapPessoal?> ObterPorIdAsync(RemessaSicapPessoalId id, CancellationToken cancellationToken);

    /// <summary>
    /// Apura o proximo sequencial de lote (NRO_MOV) para um orgao no tenant (maior sequencial + 1;
    /// inicia em 1). Mantem a sequencia de remessas por orgao.
    /// </summary>
    /// <param name="codigoOrgao">Codigo do orgao remetente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Proximo sequencial disponivel para o orgao.</returns>
    Task<int> ProximoSequencialAsync(int codigoOrgao, CancellationToken cancellationToken);

    /// <summary>Lista as remessas do tenant (ordenadas por orgao/sequencial decrescente); para a navegabilidade.</summary>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Remessas do tenant.</returns>
    Task<IReadOnlyList<RemessaSicapPessoal>> ListarAsync(SituacaoRemessaSicap? situacao, CancellationToken cancellationToken);
}

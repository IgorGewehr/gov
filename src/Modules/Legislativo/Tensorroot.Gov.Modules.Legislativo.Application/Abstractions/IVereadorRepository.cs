using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Vereador"/>.</summary>
public interface IVereadorRepository
{
    /// <summary>Marca um novo vereador para insercao.</summary>
    /// <param name="vereador">Vereador a adicionar.</param>
    void Adicionar(Vereador vereador);

    /// <summary>Obtem um vereador por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O vereador, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Vereador?> ObterPorIdAsync(VereadorId id, CancellationToken cancellationToken);

    /// <summary>Lista os vereadores do tenant (ordenados por nome parlamentar).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Vereadores do tenant.</returns>
    Task<IReadOnlyList<Vereador>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resolve o nome parlamentar de cada identificador informado (para painel/ata/votos nominais),
    /// sem expor o agregado. Ids sem cadastro nao aparecem no mapa (o consumidor exibe o GUID cru).
    /// </summary>
    /// <param name="ids">Identificadores de vereadores a resolver.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Mapa <see cref="VereadorId"/> → nome parlamentar.</returns>
    Task<IReadOnlyDictionary<VereadorId, string>> ResolverNomesAsync(
        IReadOnlyCollection<VereadorId> ids,
        CancellationToken cancellationToken);

    /// <summary>Conta quantos vereadores existem no tenant (base para o seed idempotente).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de vereadores cadastrados.</returns>
    Task<int> ContarAsync(CancellationToken cancellationToken);
}

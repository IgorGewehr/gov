using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using DominioPlano = Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira.PlanoCarreira;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="DominioPlano"/> (planos de cargos e salarios).</summary>
public interface IPlanoCarreiraRepository
{
    /// <summary>Marca um novo plano de carreira para insercao.</summary>
    /// <param name="plano">Plano a adicionar.</param>
    void Adicionar(DominioPlano plano);

    /// <summary>Obtem um plano de carreira por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O plano, ou <c>null</c> se inexistente no tenant.</returns>
    Task<DominioPlano?> ObterPorIdAsync(PlanoCarreiraId id, CancellationToken cancellationToken);

    /// <summary>Lista os planos de carreira do tenant (ordenados por denominacao).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Planos do tenant.</returns>
    Task<IReadOnlyList<DominioPlano>> ListarAsync(CancellationToken cancellationToken);
}

/// <summary>Repositorio do agregado <see cref="EnquadramentoServidor"/> (posicao do servidor na carreira).</summary>
public interface IEnquadramentoServidorRepository
{
    /// <summary>Marca um novo enquadramento para insercao.</summary>
    /// <param name="enquadramento">Enquadramento a adicionar.</param>
    void Adicionar(EnquadramentoServidor enquadramento);

    /// <summary>Obtem o enquadramento (com movimentacoes) por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O enquadramento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<EnquadramentoServidor?> ObterPorIdAsync(EnquadramentoServidorId id, CancellationToken cancellationToken);

    /// <summary>Obtem o enquadramento vigente de um servidor (com o historico de movimentacoes).</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O enquadramento, ou <c>null</c> se o servidor nao estiver enquadrado.</returns>
    Task<EnquadramentoServidor?> ObterPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);

    /// <summary>Indica se o servidor ja possui enquadramento (unicidade por servidor/tenant).</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe enquadramento.</returns>
    Task<bool> ExisteParaServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);
}

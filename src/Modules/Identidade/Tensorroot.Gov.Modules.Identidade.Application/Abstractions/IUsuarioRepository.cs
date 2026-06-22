using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Usuario"/> (sempre tenant-scoped).</summary>
public interface IUsuarioRepository
{
    /// <summary>Marca um novo usuario para insercao.</summary>
    /// <param name="usuario">Usuario a adicionar.</param>
    void Adicionar(Usuario usuario);

    /// <summary>Obtem um usuario por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O usuario, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Usuario?> ObterPorIdAsync(UsuarioId id, CancellationToken cancellationToken);

    /// <summary>Obtem um usuario pelo e-mail de login (respeitando o filtro de tenant).</summary>
    /// <param name="email">E-mail de login.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O usuario, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe um usuario com o e-mail informado no tenant.</summary>
    /// <param name="email">E-mail de login.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o e-mail ja estiver em uso no tenant.</returns>
    Task<bool> EmailEmUsoAsync(Email email, CancellationToken cancellationToken);

    /// <summary>Lista os usuarios do tenant.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuarios do tenant.</returns>
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtem um usuario por e-mail dentro de um tenant explicito, para uso na AUTENTICACAO — que
    /// precede a resolucao do tenant (ITenantContext) a partir do JWT. A implementacao deve
    /// filtrar por <paramref name="tenantId"/> de forma explicita e auditada (sem desabilitar o
    /// Global Query Filter de forma irrestrita).
    /// </summary>
    /// <param name="tenantId">Tenant alvo da autenticacao.</param>
    /// <param name="email">E-mail de login.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O usuario do tenant, ou <c>null</c> se inexistente.</returns>
    Task<Usuario?> ObterParaAutenticacaoAsync(Guid tenantId, Email email, CancellationToken cancellationToken);
}

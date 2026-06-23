using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="VinculoServidorUsuario"/> (vinculo usuario&#8596;servidor).</summary>
public interface IVinculoServidorUsuarioRepository
{
    /// <summary>Marca um novo vinculo para insercao.</summary>
    /// <param name="vinculo">Vinculo a adicionar.</param>
    void Adicionar(VinculoServidorUsuario vinculo);

    /// <summary>
    /// Resolve o <see cref="ServidorId"/> do usuario informado no tenant atual (Global Query Filter).
    /// E a consulta-ANCORA do autosservico: o handler passa SEMPRE o usuario AUTENTICADO, nunca um id
    /// vindo do cliente.
    /// </summary>
    /// <param name="usuarioId">Usuario (subject do JWT) autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O servidor vinculado, ou <c>null</c> quando o usuario nao e um servidor neste tenant.</returns>
    Task<ServidorId?> ResolverServidorDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe vinculo para o usuario no tenant atual.</summary>
    /// <param name="usuarioId">Usuario a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o usuario ja estiver vinculado a um servidor.</returns>
    Task<bool> UsuarioJaVinculadoAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Indica se o servidor ja esta vinculado a algum usuario no tenant atual.</summary>
    /// <param name="servidorId">Servidor a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o servidor ja estiver vinculado a um usuario.</returns>
    Task<bool> ServidorJaVinculadoAsync(ServidorId servidorId, CancellationToken cancellationToken);
}

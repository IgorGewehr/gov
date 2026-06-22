using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

/// <summary>Token de acesso emitido apos autenticacao bem-sucedida.</summary>
/// <param name="AccessToken">JWT assinado.</param>
/// <param name="ExpiraEm">Instante de expiracao (UTC).</param>
public sealed record TokenEmitido(string AccessToken, DateTimeOffset ExpiraEm);

/// <summary>
/// Porta de emissao de token. A implementacao (Infrastructure) assina um JWT contendo o tenant,
/// o subject (usuario) e as permissoes efetivas como claims, com expiracao parametrizavel por tenant.
/// </summary>
public interface IEmissorToken
{
    /// <summary>Emite um JWT para o usuario autenticado com suas permissoes efetivas.</summary>
    /// <param name="usuarioId">Identificador do usuario (subject).</param>
    /// <param name="tenantId">Tenant dono do usuario.</param>
    /// <param name="tenantNome">Nome do tenant (ente publico), para a claim "tenant_name".</param>
    /// <param name="nome">Nome de exibicao do usuario.</param>
    /// <param name="email">E-mail de login do usuario.</param>
    /// <param name="permissoesEfetivas">Permissoes efetivas a embutir como claims.</param>
    /// <returns>Token emitido.</returns>
    TokenEmitido Emitir(
        UsuarioId usuarioId,
        Guid tenantId,
        string? tenantNome,
        string nome,
        string email,
        IReadOnlyCollection<string> permissoesEfetivas);
}

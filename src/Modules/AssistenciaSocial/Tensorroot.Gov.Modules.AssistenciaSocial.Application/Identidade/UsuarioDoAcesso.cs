using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Identidade;

/// <summary>
/// Resolve a identidade do usuario REAL da requisicao (principal autenticado) para gravar na trilha
/// de acesso ao prontuario sigiloso (LG-1). O identificador NUNCA vem do cliente: e derivado da claim
/// <c>sub</c> do JWT via <see cref="ICurrentUser"/>, tornando a trilha NAO forjavel (CLAUDE.md §6, I-7).
/// </summary>
public static class UsuarioDoAcesso
{
    /// <summary>
    /// Obtem o GUID do usuario autenticado a partir de <see cref="ICurrentUser.UserId"/> (claim
    /// <c>sub</c>). Falha-alto se nao houver principal autenticado ou se a claim nao for um GUID:
    /// uma leitura sigilosa SEM identidade rastreavel jamais pode prosseguir (deny-by-default).
    /// </summary>
    /// <param name="currentUser">Identidade do principal autenticado da requisicao.</param>
    /// <returns>GUID do usuario que realiza o acesso.</returns>
    /// <exception cref="InvalidOperationException">
    /// Quando nao ha usuario autenticado ou a claim <c>sub</c> nao e um GUID valido.
    /// </exception>
    public static Guid Resolver(ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        if (string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            throw new InvalidOperationException(
                "Acesso a conteudo sigiloso exige usuario autenticado; nenhuma identidade no contexto (LG-1).");
        }

        if (!Guid.TryParse(currentUser.UserId, out var usuarioId) || usuarioId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A identidade do usuario autenticado (claim 'sub') nao e um GUID valido; trilha de acesso negada (LG-1).");
        }

        return usuarioId;
    }
}

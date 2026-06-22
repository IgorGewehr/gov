namespace Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

/// <summary>
/// Porta para o indice central de login (email → tenant), mantido no banco de CONTROLE da
/// plataforma. Como os usuarios vivem no banco DEDICADO de cada tenant, o login precisa de um
/// registro central que mapeie o e-mail ao seu tenant. A implementacao (Infrastructure) escreve
/// nesse indice ao provisionar usuarios. NUNCA registra credenciais — apenas email e tenant.
/// </summary>
public interface IRegistroLoginCentral
{
    /// <summary>Registra a associacao email → tenant. Falha se o e-mail ja pertencer a outro tenant.</summary>
    /// <param name="email">E-mail de login normalizado.</param>
    /// <param name="tenantId">Tenant dono do usuario.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RegistrarAsync(string email, Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Atualiza a associacao quando o e-mail de login muda (remove o antigo, registra o novo).</summary>
    /// <param name="emailAntigo">E-mail anterior.</param>
    /// <param name="emailNovo">Novo e-mail.</param>
    /// <param name="tenantId">Tenant dono do usuario.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AtualizarAsync(string emailAntigo, string emailNovo, Guid tenantId, CancellationToken cancellationToken);
}

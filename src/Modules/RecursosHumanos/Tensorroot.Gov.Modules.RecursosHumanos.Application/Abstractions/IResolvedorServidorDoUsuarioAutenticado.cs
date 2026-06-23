using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Porta-ANCORA do autosservico ("Minha Folha"): resolve o <see cref="ServidorId"/> do usuario
/// ATUALMENTE AUTENTICADO (subject do JWT, lido do <c>ICurrentUser</c>), nunca de um id informado
/// pelo cliente. E o coracao do ABAC dado-proprio a prova de bala: todo endpoint proprio obtem o
/// servidor por aqui e SO consulta dados desse servidor.
/// </summary>
public interface IResolvedorServidorDoUsuarioAutenticado
{
    /// <summary>
    /// Resolve o servidor do usuario autenticado. LANCA <see cref="UsuarioSemVinculoServidorException"/>
    /// quando nao ha usuario autenticado ou quando o usuario nao tem vinculo de servidor no tenant —
    /// negar por padrao (sem vinculo = sem dado-proprio).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O servidor do proprio usuario autenticado.</returns>
    /// <exception cref="UsuarioSemVinculoServidorException">Quando nao ha vinculo resolvivel.</exception>
    Task<ServidorId> ResolverServidorAtualAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Erro de DENY-BY-DEFAULT do autosservico: o usuario autenticado nao possui (ou nao se pode
/// resolver) um vinculo de servidor no tenant atual. Sem o vinculo, NAO ha dado-proprio a expor — o
/// acesso e abortado (e a tentativa pode ser auditada na trilha LGPD pelo resolvedor).
/// </summary>
public sealed class UsuarioSemVinculoServidorException : Exception
{
    /// <summary>Cria a excecao com a mensagem padrao (sem vazar o id do usuario).</summary>
    public UsuarioSemVinculoServidorException()
        : base("Usuario autenticado nao esta vinculado a um servidor neste tenant; autosservico negado.")
    {
    }
}

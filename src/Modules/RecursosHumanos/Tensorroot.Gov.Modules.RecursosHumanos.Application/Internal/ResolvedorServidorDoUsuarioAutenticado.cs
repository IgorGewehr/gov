using System.Globalization;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Internal;

/// <summary>
/// Implementacao da ancora do autosservico: le o subject ("sub") do <see cref="ICurrentUser"/> e
/// resolve, pelo vinculo do tenant atual, o servidor do PROPRIO usuario. NUNCA aceita um id de
/// servidor vindo do cliente — o id e SEMPRE derivado do principal autenticado (ABAC dado-proprio).
/// <para>
/// Quando nao ha vinculo (usuario que nao e servidor, ou sub ausente/invalido), sela uma NEGATIVA na
/// trilha de acesso LGPD (accountability: quem tentou ver dado-proprio sem ter vinculo) e LANCA
/// <see cref="UsuarioSemVinculoServidorException"/> — negar por padrao.
/// </para>
/// </summary>
public sealed class ResolvedorServidorDoUsuarioAutenticado(
    ICurrentUser currentUser,
    IVinculoServidorUsuarioRepository vinculos,
    IRegistroAcessoSensivel registroAcesso)
    : IResolvedorServidorDoUsuarioAutenticado
{
    /// <summary>Nome do recurso na trilha de negativa quando nao ha vinculo de servidor.</summary>
    public const string RecursoAutosservico = "AutosservicoServidor";

    /// <inheritdoc />
    public async Task<ServidorId> ResolverServidorAtualAsync(CancellationToken cancellationToken)
    {
        if (!TryObterUsuarioAutenticado(out var usuarioId))
        {
            await NegarAsync(entidadeId: null, cancellationToken).ConfigureAwait(false);
            throw new UsuarioSemVinculoServidorException();
        }

        var servidorId = await vinculos
            .ResolverServidorDoUsuarioAsync(usuarioId, cancellationToken)
            .ConfigureAwait(false);

        if (servidorId is null)
        {
            await NegarAsync(usuarioId.ToString(), cancellationToken).ConfigureAwait(false);
            throw new UsuarioSemVinculoServidorException();
        }

        return servidorId.Value;
    }

    private bool TryObterUsuarioAutenticado(out Guid usuarioId)
    {
        usuarioId = Guid.Empty;
        var sub = currentUser.UserId;
        return !string.IsNullOrWhiteSpace(sub)
            && Guid.TryParse(sub, CultureInfo.InvariantCulture, out usuarioId)
            && usuarioId != Guid.Empty;
    }

    private Task NegarAsync(string? entidadeId, CancellationToken cancellationToken)
        => registroAcesso.RegistrarNegacaoAsync(
            RecursoAutosservico,
            entidadeId,
            // Autosservico de dado pessoal proprio: base legal seria o exercicio do direito do
            // titular (LGPD art. 18) sobre seus dados; a negativa registra a tentativa sem vinculo.
            BaseLegalLgpd.ExercicioDeDireitos,
            cancellationToken);
}

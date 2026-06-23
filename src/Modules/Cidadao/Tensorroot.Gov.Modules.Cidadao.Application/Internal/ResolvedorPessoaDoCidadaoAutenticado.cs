using System.Globalization;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Cidadao.Application.Internal;

/// <summary>
/// Implementacao da ancora dado-proprio do Portal do Cidadao (copia fiel do
/// <c>ResolvedorServidorDoUsuarioAutenticado</c> do Minha Folha): le o subject ("sub") do
/// <see cref="ICurrentUser"/> e resolve, pela conta-cidadao do tenant atual, a PESSOA do PROPRIO
/// cidadao. NUNCA aceita um id/CPF vindo do cliente — o documento e SEMPRE derivado do principal.
/// <para>
/// Quando nao ha conta (sub ausente/invalido, sem conta no tenant, ou conta inativa), sela uma NEGATIVA
/// na trilha de acesso LGPD (accountability) e LANCA <see cref="CidadaoSemVinculoException"/> — negar por
/// padrao (sem conta = sem dado-proprio).
/// </para>
/// </summary>
public sealed class ResolvedorPessoaDoCidadaoAutenticado(
    ICurrentUser currentUser,
    ICidadaoContaRepository contas,
    IRegistroAcessoSensivel registroAcesso)
    : IResolvedorPessoaDoCidadaoAutenticado
{
    /// <summary>Nome do recurso na trilha de negativa quando nao ha conta-cidadao.</summary>
    public const string RecursoPortalCidadao = "PortalCidadao";

    /// <inheritdoc />
    public async Task<PessoaCidadaoRef> ResolverPessoaAtualAsync(CancellationToken cancellationToken)
    {
        if (!TryObterContaAutenticada(out var contaId))
        {
            await NegarAsync(entidadeId: null, cancellationToken).ConfigureAwait(false);
            throw new CidadaoSemVinculoException();
        }

        var conta = await contas.ObterPorIdAsync(contaId, cancellationToken).ConfigureAwait(false);

        // Deny-by-default: sem conta no tenant OU conta inativa => negativa selada + excecao.
        if (conta is null || !conta.Ativo)
        {
            await NegarAsync(contaId.ToString(), cancellationToken).ConfigureAwait(false);
            throw new CidadaoSemVinculoException();
        }

        return new PessoaCidadaoRef(conta.Documento, conta.Nome);
    }

    private bool TryObterContaAutenticada(out CidadaoContaId contaId)
    {
        contaId = default;
        var sub = currentUser.UserId;
        if (string.IsNullOrWhiteSpace(sub)
            || !Guid.TryParse(sub, CultureInfo.InvariantCulture, out var id)
            || id == Guid.Empty)
        {
            return false;
        }

        contaId = new CidadaoContaId(id);
        return true;
    }

    private Task NegarAsync(string? entidadeId, CancellationToken cancellationToken)
        => registroAcesso.RegistrarNegacaoAsync(
            RecursoPortalCidadao,
            entidadeId,
            // Acesso a dado-proprio do titular (LGPD art. 18); a negativa registra a tentativa sem conta.
            BaseLegalLgpd.ExercicioDeDireitos,
            cancellationToken);
}

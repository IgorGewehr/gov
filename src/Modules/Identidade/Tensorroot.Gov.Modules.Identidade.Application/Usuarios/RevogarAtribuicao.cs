using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Permissoes;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>
/// Revoga TODAS as atribuicoes de um papel de um usuario numa dada UO (MODELO §3/§8 I8). Exige do
/// revogador ESCOPO ADMINISTRATIVO sobre a UO alvo (mesma porta de entrada da concessao — quem nao
/// administra a UO nao pode revogar atribuicoes nela). A revogacao tem efeito imediato no escopo
/// efetivo (a resolucao por requisicao deixa de considerar a atribuicao removida).
/// </summary>
/// <param name="UsuarioId">Usuario alvo.</param>
/// <param name="PapelId">Papel a revogar.</param>
/// <param name="UnidadeId">UO cujo escopo sera revogado.</param>
public sealed record RevogarAtribuicaoCommand(Guid UsuarioId, Guid PapelId, Guid UnidadeId) : ICommand;

/// <summary>Regras de validacao da revogacao de atribuicao.</summary>
public sealed class RevogarAtribuicaoValidator : AbstractValidator<RevogarAtribuicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RevogarAtribuicaoValidator()
    {
        RuleFor(comando => comando.UsuarioId).NotEmpty();
        RuleFor(comando => comando.PapelId).NotEmpty();
        RuleFor(comando => comando.UnidadeId).NotEmpty();
    }
}

/// <summary>Handler da revogacao de atribuicao (exige escopo administrativo sobre a UO).</summary>
public sealed class RevogarAtribuicaoHandler(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    IUnidadeRepository unidades,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<RevogarAtribuicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RevogarAtribuicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        var unidadeId = new UnidadeOrganizacionalId(request.UnidadeId);

        // Escopo administrativo sobre a UO alvo: o revogador precisa cobrir {UO} com
        // 'identidade.usuarios.gerenciar' (mesma exigencia da concessao, sem subarvore).
        var revogador = await ResolverRevogadorAsync(cancellationToken).ConfigureAwait(false);
        var escopo = await CalculadoraPermissoesEfetivas
            .ResolverEscopoAsync(revogador, papeis, unidades, clock.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);

        var todasUnidades = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
        var arvore = ArvoreUnidades.Construir(todasUnidades);
        var escopoAlvo = arvore.Expandir(unidadeId, incluiSubunidades: false);

        if (!escopo.CobreEscopo(Permissoes.IdentidadeUsuariosGerenciar, escopoAlvo))
        {
            throw new ConcessaoNaoAutorizadaException(
                ResultadoConcessao.Negar(MotivoConcessaoNegada.ForaDoEscopoAdministrativo));
        }

        usuario.RevogarPapel(new PapelId(request.PapelId), unidadeId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Usuario> ResolverRevogadorAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var revogadorGuid))
        {
            throw new InvalidOperationException("Revogador nao identificado no contexto da requisicao.");
        }

        return await usuarios.ObterPorIdAsync(new UsuarioId(revogadorGuid), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Revogador nao encontrado no tenant.");
    }
}

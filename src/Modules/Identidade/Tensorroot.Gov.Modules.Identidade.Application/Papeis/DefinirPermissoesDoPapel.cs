using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Application.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Papeis;

/// <summary>Redefine integralmente o conjunto de permissoes de um papel.</summary>
/// <param name="PapelId">Papel alvo.</param>
/// <param name="Permissoes">Conjunto desejado de permissoes (do catalogo canonico).</param>
public sealed record DefinirPermissoesDoPapelCommand(Guid PapelId, IReadOnlyCollection<string> Permissoes) : ICommand;

/// <summary>Regras de validacao da definicao de permissoes do papel.</summary>
public sealed class DefinirPermissoesDoPapelValidator : AbstractValidator<DefinirPermissoesDoPapelCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirPermissoesDoPapelValidator()
    {
        RuleFor(comando => comando.PapelId).NotEmpty();
        RuleFor(comando => comando.Permissoes).NotNull();
    }
}

/// <summary>
/// Handler da definicao de permissoes do papel. APLICA A REGRA I4 ("nao delega o que nao tem" —
/// MODELO §3.1.c) na COMPOSICAO: o usuario atual so empacota num papel permissoes que ele PROPRIO
/// possui de forma global no tenant (<see cref="AutorizacaoDeComposicaoDePapel"/>). Sem isso, quem
/// tem apenas <c>identidade.usuarios.gerenciar</c> escalaria permissoes nao-possuidas (achado AA-1).
/// </summary>
public sealed class DefinirPermissoesDoPapelHandler(
    IPapelRepository papeis,
    IUsuarioRepository usuarios,
    IUnidadeRepository unidades,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<DefinirPermissoesDoPapelCommand>
{
    /// <inheritdoc />
    public async Task Handle(DefinirPermissoesDoPapelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var papel = await papeis.ObterPorIdAsync(new PapelId(request.PapelId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Papel nao encontrado.");

        // === REGRA I4 (provada no dominio): o compositor so empacota permissoes que ele PROPRIO
        // possui de forma global no tenant. Negar por padrao, nunca silencioso. ===
        await ProvarCoberturaI4Async(request.Permissoes, cancellationToken).ConfigureAwait(false);

        // O dominio rejeita escopos fora do catalogo canonico (negar por padrao).
        papel.DefinirPermissoes(request.Permissoes);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProvarCoberturaI4Async(IReadOnlyCollection<string> permissoes, CancellationToken cancellationToken)
    {
        var compositor = await ResolverCompositorAsync(cancellationToken).ConfigureAwait(false);
        var escopoCompositor = await CalculadoraPermissoesEfetivas
            .ResolverEscopoAsync(compositor, papeis, unidades, clock.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);

        var todasUnidades = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
        var arvore = ArvoreUnidades.Construir(todasUnidades);

        var resultado = AutorizacaoDeComposicaoDePapel.Verificar(escopoCompositor, permissoes, arvore);
        if (!resultado.Permitida)
        {
            throw new ConcessaoNaoAutorizadaException(resultado);
        }
    }

    private async Task<Usuario> ResolverCompositorAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var compositorGuid))
        {
            throw new InvalidOperationException("Compositor nao identificado no contexto da requisicao.");
        }

        return await usuarios.ObterPorIdAsync(new UsuarioId(compositorGuid), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Compositor nao encontrado no tenant.");
    }
}

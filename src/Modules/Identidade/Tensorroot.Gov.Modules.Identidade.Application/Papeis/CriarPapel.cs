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

/// <summary>Cria um papel (perfil RBAC) no tenant atual.</summary>
/// <param name="Nome">Nome do papel (unico por tenant).</param>
/// <param name="Permissoes">Permissoes iniciais (do catalogo canonico).</param>
public sealed record CriarPapelCommand(string Nome, IReadOnlyCollection<string>? Permissoes = null) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de papel.</summary>
public sealed class CriarPapelValidator : AbstractValidator<CriarPapelCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarPapelValidator()
        => RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(Papel.ComprimentoMaximoNome);
}

/// <summary>
/// Handler da criacao de papel. APLICA A REGRA I4 ("nao delega o que nao tem" — MODELO §3.1.c) na
/// COMPOSICAO inicial: o usuario atual so semeia o papel com permissoes que ele PROPRIO possui de
/// forma global no tenant (<see cref="AutorizacaoDeComposicaoDePapel"/>) — fecha a metade de criacao
/// do achado AA-1 (papel nasce sem permissoes nao-possuidas pelo criador).
/// </summary>
public sealed class CriarPapelHandler(
    IPapelRepository papeis,
    IUsuarioRepository usuarios,
    IUnidadeRepository unidades,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider clock)
    : ICommandHandler<CriarPapelCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarPapelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await papeis.NomeEmUsoAsync(request.Nome, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe um papel com este nome no tenant.");
        }

        // === REGRA I4 (provada no dominio): o criador so semeia o papel com permissoes que ele
        // PROPRIO possui de forma global no tenant. Negar por padrao, nunca silencioso. ===
        var permissoesIniciais = request.Permissoes ?? [];
        if (permissoesIniciais.Count > 0)
        {
            await ProvarCoberturaI4Async(permissoesIniciais, cancellationToken).ConfigureAwait(false);
        }

        // A validacao de permissoes conhecidas e garantida pelo dominio (Papel.DefinirPermissoes).
        var papel = Papel.Criar(tenant.TenantId, request.Nome, request.Permissoes);

        papeis.Adicionar(papel);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return papel.Id.Value;
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

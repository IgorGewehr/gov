using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Redefine integralmente o conjunto de papeis (perfis RBAC) de um usuario.</summary>
/// <param name="UsuarioId">Usuario alvo.</param>
/// <param name="PapeisIds">Conjunto desejado de papeis (substitui o atual).</param>
public sealed record DefinirPapeisDoUsuarioCommand(Guid UsuarioId, IReadOnlyCollection<Guid> PapeisIds) : ICommand;

/// <summary>Regras de validacao da definicao de papeis do usuario.</summary>
public sealed class DefinirPapeisDoUsuarioValidator : AbstractValidator<DefinirPapeisDoUsuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirPapeisDoUsuarioValidator()
    {
        RuleFor(comando => comando.UsuarioId).NotEmpty();
        RuleFor(comando => comando.PapeisIds).NotNull();
    }
}

/// <summary>
/// Handler da definicao de papeis do usuario (ponte de compatibilidade que atribui no escopo GLOBAL
/// do tenant — UO raiz com subunidades). APLICA A REGRA I4 ("nao delega o que nao tem" —
/// MODELO §3.1.c): como a atribuicao e GLOBAL, o concedente (usuario atual) precisa provar cobertura
/// I4 de CADA papel no escopo global (<see cref="AutorizacaoDeConcessao"/>) — o MESMO teste que o
/// caminho seguro <c>POST /usuarios/{id}/atribuicoes</c> ja faz. Sem isso, quem tem apenas
/// <c>identidade.usuarios.gerenciar</c> auto-atribuiria papeis de admin pleno (achado AA-2).
/// </summary>
public sealed class DefinirPapeisDoUsuarioHandler(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    IUnidadeRepository unidades,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<DefinirPapeisDoUsuarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(DefinirPapeisDoUsuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        var papeisIds = request.PapeisIds.Select(id => new PapelId(id)).ToArray();
        IReadOnlyList<Papel> encontrados = [];
        if (papeisIds.Length > 0)
        {
            encontrados = await papeis.ObterPorIdsAsync(papeisIds, cancellationToken).ConfigureAwait(false);
            if (encontrados.Count != papeisIds.Distinct().Count())
            {
                throw new InvalidOperationException("Um ou mais papeis informados nao existem no tenant.");
            }
        }

        var unidadesDoTenant = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
        var raiz = unidadesDoTenant.FirstOrDefault(unidade => unidade.UnidadePaiId is null);

        // === REGRA I4 (provada no dominio): a redefinicao atribui no escopo GLOBAL (raiz + subarvore).
        // O concedente precisa cobrir, por I4, cada papel nesse escopo — IGUAL ao caminho /atribuicoes.
        // Negar por padrao, nunca silencioso. ===
        if (encontrados.Count > 0)
        {
            await ProvarCoberturaI4GlobalAsync(encontrados, unidadesDoTenant, raiz, cancellationToken).ConfigureAwait(false);
        }

        usuario.DefinirPapeis(papeisIds);

        // Reancora na UO raiz real do tenant (a ponte de compat cria na sentinela RaizPendente).
        if (papeisIds.Length > 0 && raiz is not null)
        {
            usuario.ReancorarAtribuicoesPendentesNaRaiz(raiz.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProvarCoberturaI4GlobalAsync(
        IReadOnlyList<Papel> papeisAlvo,
        IReadOnlyList<UnidadeOrganizacional> unidadesDoTenant,
        UnidadeOrganizacional? raiz,
        CancellationToken cancellationToken)
    {
        // A atribuicao global so existe ancorada numa raiz real. Sem raiz, nega por padrao.
        if (raiz is null)
        {
            throw new ConcessaoNaoAutorizadaException(
                ResultadoConcessao.Negar(MotivoConcessaoNegada.ForaDoEscopoAdministrativo));
        }

        var concedente = await ResolverConcedenteAsync(cancellationToken).ConfigureAwait(false);
        var escopoConcedente = await CalculadoraPermissoesEfetivas
            .ResolverEscopoAsync(concedente, papeis, unidades, clock.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);

        var arvore = ArvoreUnidades.Construir(unidadesDoTenant);

        // Cada papel e provado no escopo GLOBAL (raiz com subunidades) — o mesmo alcance que
        // DefinirPapeis materializa (IncluiSubunidades=true sobre a raiz).
        foreach (var papel in papeisAlvo)
        {
            var resultado = AutorizacaoDeConcessao.Verificar(
                escopoConcedente,
                papel.Permissoes,
                raiz.Id,
                incluiSubunidades: true,
                arvore);

            if (!resultado.Permitida)
            {
                throw new ConcessaoNaoAutorizadaException(resultado);
            }
        }
    }

    private async Task<Usuario> ResolverConcedenteAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var concedenteGuid))
        {
            throw new InvalidOperationException("Concedente nao identificado no contexto da requisicao.");
        }

        return await usuarios.ObterPorIdAsync(new UsuarioId(concedenteGuid), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Concedente nao encontrado no tenant.");
    }
}

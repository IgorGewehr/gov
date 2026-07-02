using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Provisiona um novo usuario no tenant atual.</summary>
/// <param name="Nome">Nome de exibicao.</param>
/// <param name="Email">E-mail de login (unico por tenant).</param>
/// <param name="Senha">Senha em claro (sera transformada em hash; nunca persistida em claro).</param>
/// <param name="PapeisIds">Papeis iniciais a atribuir (opcional).</param>
public sealed record CriarUsuarioCommand(
    string Nome,
    string Email,
    string Senha,
    IReadOnlyCollection<Guid>? PapeisIds = null) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de usuario.</summary>
public sealed class CriarUsuarioValidator : AbstractValidator<CriarUsuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarUsuarioValidator()
    {
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(Usuario.ComprimentoMaximoNome);
        RuleFor(comando => comando.Email)
            .NotEmpty()
            .MaximumLength(Email.ComprimentoMaximo)
            .Must(email => Domain.ValueObjects.Email.TentarCriar(email, out _))
            .WithMessage("E-mail invalido.");
        RuleFor(comando => comando.Senha).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}

/// <summary>Handler da criacao de usuario.</summary>
public sealed class CriarUsuarioHandler(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    ISenhaHasher hasher,
    IRegistroLoginCentral registroLoginCentral,
    IUnitOfWork unitOfWork,
    IUnidadeRepository unidades,
    ITenantContext tenant,
    ICurrentUser currentUser,
    TimeProvider clock)
    : ICommandHandler<CriarUsuarioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarUsuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = Email.De(request.Email);

        if (await usuarios.EmailEmUsoAsync(email, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe um usuario com este e-mail no tenant.");
        }

        var papeisIds = (request.PapeisIds ?? []).Select(id => new PapelId(id)).ToArray();
        var unidadesDoTenant = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
        var raiz = unidadesDoTenant.FirstOrDefault(unidade => unidade.UnidadePaiId is null);

        if (papeisIds.Length > 0)
        {
            var encontrados = await papeis.ObterPorIdsAsync(papeisIds, cancellationToken).ConfigureAwait(false);
            if (encontrados.Count != papeisIds.Distinct().Count())
            {
                throw new InvalidOperationException("Um ou mais papeis informados nao existem no tenant.");
            }

            // S1 — REGRA I4 ("nao delega o que nao tem"): os papeis INICIAIS sao atribuidos no escopo
            // GLOBAL (raiz + subarvore), logo o concedente (usuario atual) precisa provar cobertura I4 de
            // CADA papel — o MESMO teste do POST /usuarios/{id}/atribuicoes e do DefinirPapeis. Antes,
            // criar-com-papeis pulava essa prova, permitindo escalacao lateral. Negar por padrao.
            await ProvarCoberturaI4GlobalAsync(encontrados, unidadesDoTenant, raiz, cancellationToken).ConfigureAwait(false);
        }

        // Reserva a unicidade GLOBAL do e-mail no indice central ANTES de persistir o usuario no
        // banco do tenant: se o e-mail ja pertencer a outro tenant, falha sem deixar usuario orfao.
        await registroLoginCentral.RegistrarAsync(email.Valor, tenant.TenantId, cancellationToken).ConfigureAwait(false);

        var senhaHash = hasher.Hash(request.Senha);
        var usuario = Usuario.Criar(tenant.TenantId, request.Nome, email, senhaHash, papeisIds);

        // Âncora na UO RAIZ REAL do tenant: a ponte de compatibilidade cria as atribuições na
        // sentinela RaizPendente (Guid.Empty); reancoramos na raiz (mesmo tratamento do seed) para
        // que o escopo seja resolvível server-side e a regra de escopo/I4 valha para este usuário.
        if (papeisIds.Length > 0 && raiz is not null)
        {
            usuario.ReancorarAtribuicoesPendentesNaRaiz(raiz.Id);
        }

        usuarios.Adicionar(usuario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return usuario.Id.Value;
    }

    // Prova I4 GLOBAL — replica o teste de DefinirPapeisDoUsuarioHandler (fonte da regra). O concedente
    // precisa cobrir, no escopo raiz+subarvore, cada papel que esta concedendo.
    private async Task ProvarCoberturaI4GlobalAsync(
        IReadOnlyList<Papel> papeisAlvo,
        IReadOnlyList<UnidadeOrganizacional> unidadesDoTenant,
        UnidadeOrganizacional? raiz,
        CancellationToken cancellationToken)
    {
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

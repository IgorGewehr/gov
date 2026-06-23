using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>
/// Atribui um papel a um usuario COM ESCOPO de UO (MODELO §2.2/§3) — APLICANDO A REGRA I4: o
/// concedente (usuario atual) so pode atribuir o papel P na UO X se ele PROPRIO possui todas as
/// permissoes de P em X (ou em um ancestral) E tem escopo administrativo sobre X. A regra e provada
/// no dominio (<see cref="AutorizacaoDeConcessao"/>), nao apenas na UI.
/// </summary>
/// <param name="UsuarioId">Usuario que recebera o papel.</param>
/// <param name="PapelId">Papel a atribuir.</param>
/// <param name="UnidadeId">UO raiz do escopo.</param>
/// <param name="IncluiSubunidades">Se a atribuicao alcanca os descendentes da UO (I5).</param>
/// <param name="VigenciaInicio">Inicio da vigencia (nulo = agora).</param>
/// <param name="VigenciaFim">Fim opcional da vigencia (nulo = aberta).</param>
public sealed record AtribuirPapelAoUsuarioCommand(
    Guid UsuarioId,
    Guid PapelId,
    Guid UnidadeId,
    bool IncluiSubunidades,
    DateTimeOffset? VigenciaInicio = null,
    DateTimeOffset? VigenciaFim = null) : ICommand;

/// <summary>Regras de validacao da atribuicao de papel com escopo.</summary>
public sealed class AtribuirPapelAoUsuarioValidator : AbstractValidator<AtribuirPapelAoUsuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public AtribuirPapelAoUsuarioValidator()
    {
        RuleFor(comando => comando.UsuarioId).NotEmpty();
        RuleFor(comando => comando.PapelId).NotEmpty();
        RuleFor(comando => comando.UnidadeId).NotEmpty();
        RuleFor(comando => comando.VigenciaFim)
            .GreaterThanOrEqualTo(comando => comando.VigenciaInicio)
            .When(comando => comando.VigenciaInicio is not null && comando.VigenciaFim is not null)
            .WithMessage("O fim da vigencia nao pode ser anterior ao inicio.");
    }
}

/// <summary>Handler da atribuicao de papel com escopo (prova I4 + limite de profundidade D3).</summary>
public sealed class AtribuirPapelAoUsuarioHandler(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    IUnidadeRepository unidades,
    ICurrentUser currentUser,
    IPoliticaDelegacaoProvider politicaDelegacao,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<AtribuirPapelAoUsuarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtribuirPapelAoUsuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var agora = clock.GetUtcNow();

        // Alvo da atribuicao (no tenant atual — filtro global aplicado).
        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        // Papel a conceder — precisamos das suas permissoes para provar I4.
        var papelId = new PapelId(request.PapelId);
        var papel = await papeis.ObterPorIdAsync(papelId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Papel nao encontrado no tenant.");

        // UO alvo (deve existir no tenant).
        var unidadeId = new UnidadeOrganizacionalId(request.UnidadeId);
        _ = await unidades.ObterPorIdAsync(unidadeId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade organizacional nao encontrada no tenant.");

        // === REGRA I4 (provada no dominio): resolve o ESCOPO EFETIVO do concedente e verifica que
        // ele cobre poder administrativo + todas as permissoes do papel no escopo alvo. ===
        var concedente = await ResolverConcedenteAsync(cancellationToken).ConfigureAwait(false);
        var escopoConcedente = await CalculadoraPermissoesEfetivas
            .ResolverEscopoAsync(concedente, papeis, unidades, agora, cancellationToken)
            .ConfigureAwait(false);

        var todasUnidades = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
        var arvore = ArvoreUnidades.Construir(todasUnidades);

        // AA-5/D3: e DELEGACAO quando o concedente atribui a um TERCEIRO (concedente != alvo). A
        // profundidade da nova atribuicao herda a profundidade do PODER ADMINISTRATIVO do concedente
        // + 1; o teto (parametrizavel por tenant) barra cadeias infinitas de subdelegacao.
        var ehDelegacao = concedente.Id != usuario.Id;
        var profundidadeDoConcedente = escopoConcedente.ProfundidadeDeDelegacao(
            DomainPermissoes.IdentidadeUsuariosGerenciar);
        var politica = await politicaDelegacao.ObterAsync(cancellationToken).ConfigureAwait(false);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopoConcedente,
            papel.Permissoes,
            unidadeId,
            request.IncluiSubunidades,
            arvore,
            ehDelegacao,
            profundidadeDoConcedente,
            politica);

        if (!resultado.Permitida)
        {
            throw new ConcessaoNaoAutorizadaException(resultado);
        }

        var inicio = request.VigenciaInicio ?? agora;
        var vigencia = Vigencia.Criar(inicio, request.VigenciaFim);
        var origem = ProcedenciaDoConcedente(concedente.Id, usuario.Id);

        usuario.AtribuirPapel(
            papelId, unidadeId, request.IncluiSubunidades, vigencia, origem, resultado.ProfundidadeResultante);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // Concessao DIRETA quando o concedente atribui a si mesmo (semeadura/auto-administracao do
    // admin raiz) ou quando nao se distingue delegacao; DELEGADA quando um terceiro concede,
    // registrando quem concedeu (I8). A ConcessaoDelegacao completa (D3) entra em M1.x.
    private static OrigemAtribuicao ProcedenciaDoConcedente(UsuarioId concedenteId, UsuarioId alvoId)
        => concedenteId == alvoId
            ? OrigemAtribuicao.Direta()
            : OrigemAtribuicao.Delegada(concedenteId);

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

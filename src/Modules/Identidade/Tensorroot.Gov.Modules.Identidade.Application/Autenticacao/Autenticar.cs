using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Identidade.Application.Autenticacao;

/// <summary>Resultado de uma autenticacao bem-sucedida.</summary>
/// <param name="UsuarioId">Identificador do usuario autenticado.</param>
/// <param name="TenantId">Tenant do usuario.</param>
/// <param name="Nome">Nome de exibicao.</param>
/// <param name="Email">E-mail de login.</param>
/// <param name="PermissoesEfetivas">Permissoes efetivas (uniao dos papeis) embutidas no token.</param>
/// <param name="Token">Token de acesso (JWT) emitido.</param>
public sealed record ResultadoAutenticacao(
    Guid UsuarioId,
    Guid TenantId,
    string Nome,
    string Email,
    IReadOnlyCollection<string> PermissoesEfetivas,
    TokenEmitido Token);

/// <summary>
/// Autentica um usuario por e-mail e senha dentro de um tenant e emite o token de acesso.
/// E SEGURANCA CRITICA: nao revela se a falha foi e-mail inexistente, senha incorreta ou conta
/// inativa (mensagem uniforme), e executa a verificacao de hash mesmo quando o usuario nao existe
/// para mitigar enumeracao por timing.
/// </summary>
/// <param name="TenantId">Tenant alvo da autenticacao.</param>
/// <param name="Email">E-mail de login.</param>
/// <param name="Senha">Senha em claro informada.</param>
/// <param name="TenantNome">Nome do tenant (opcional), para a claim "tenant_name" do token.</param>
public sealed record AutenticarCommand(Guid TenantId, string Email, string Senha, string? TenantNome = null)
    : ICommand<ResultadoAutenticacao>;

/// <summary>Regras de validacao da autenticacao.</summary>
public sealed class AutenticarValidator : AbstractValidator<AutenticarCommand>
{
    /// <summary>Define as regras.</summary>
    public AutenticarValidator()
    {
        RuleFor(comando => comando.TenantId).NotEmpty();
        RuleFor(comando => comando.Email).NotEmpty();
        RuleFor(comando => comando.Senha).NotEmpty();
    }
}

/// <summary>
/// Excecao de autenticacao com mensagem deliberadamente generica para nao vazar a causa exata
/// da falha (e-mail, senha ou estado da conta) a um atacante.
/// </summary>
public sealed class AutenticacaoFalhouException : Exception
{
    /// <summary>Cria a excecao com a mensagem padrao generica.</summary>
    public AutenticacaoFalhouException()
        : base("Credenciais invalidas.")
    {
    }
}

/// <summary>Handler do caso de uso de autenticacao.</summary>
public sealed class AutenticarHandler(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    IUnidadeRepository unidades,
    ISenhaHasher hasher,
    IEmissorToken emissorToken,
    TimeProvider clock)
    : ICommandHandler<AutenticarCommand, ResultadoAutenticacao>
{
    // Hash "falso" usado para igualar o custo de verificacao quando o usuario nao existe
    // (defesa contra enumeracao de contas por analise de tempo de resposta).
    private const string SenhaFalsaParaTimingEstavel = "senha-inexistente-para-timing-estavel";

    /// <inheritdoc />
    public async Task<ResultadoAutenticacao> Handle(AutenticarCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Email.TentarCriar(request.Email, out var email) || email is null)
        {
            // E-mail mal formado: ainda assim gasta tempo de hash antes de falhar.
            hasher.Hash(SenhaFalsaParaTimingEstavel);
            throw new AutenticacaoFalhouException();
        }

        var usuario = await usuarios
            .ObterParaAutenticacaoAsync(request.TenantId, email, cancellationToken)
            .ConfigureAwait(false);

        if (usuario is null)
        {
            // Verifica contra um hash sintetico para manter o tempo de resposta estavel.
            hasher.Verificar(request.Senha, hasher.Hash(SenhaFalsaParaTimingEstavel));
            throw new AutenticacaoFalhouException();
        }

        var senhaConfere = hasher.Verificar(request.Senha, usuario.SenhaHash);
        if (!senhaConfere || !usuario.Ativo)
        {
            throw new AutenticacaoFalhouException();
        }

        var permissoesEfetivas = await CalculadoraPermissoesEfetivas
            .ResolverAsync(usuario, papeis, unidades, clock.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);

        var token = emissorToken.Emitir(
            usuario.Id,
            usuario.TenantId,
            request.TenantNome,
            usuario.Nome,
            usuario.Email.Valor,
            permissoesEfetivas);

        return new ResultadoAutenticacao(
            usuario.Id.Value,
            usuario.TenantId,
            usuario.Nome,
            usuario.Email.Valor,
            permissoesEfetivas,
            token);
    }
}

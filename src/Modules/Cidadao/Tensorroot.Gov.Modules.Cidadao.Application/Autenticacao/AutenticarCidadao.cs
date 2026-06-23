using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Cidadao.Application.Autenticacao;

/// <summary>Resultado de uma autenticacao de cidadao bem-sucedida.</summary>
/// <param name="ContaId">Identificador da conta-cidadao.</param>
/// <param name="TenantId">Tenant (municipio) da conta.</param>
/// <param name="Nome">Nome de exibicao.</param>
/// <param name="Token">Token de acesso (JWT) do cidadao.</param>
public sealed record ResultadoAutenticacaoCidadao(
    Guid ContaId,
    Guid TenantId,
    string Nome,
    TokenCidadaoEmitido Token);

/// <summary>
/// Autentica um cidadao por CPF/CNPJ + senha no tenant (municipio) corrente e emite o token escopado ao
/// tenant com a claim <c>tipo=cidadao</c> (sem permissoes RBAC). SEGURANCA CRITICA: nao revela se a
/// falha foi documento inexistente, senha incorreta ou conta inativa (mensagem uniforme) e executa a
/// verificacao de hash MESMO quando a conta nao existe (mitiga enumeracao por timing) — mesma tecnica do
/// <c>AutenticarHandler</c> do Identidade.
/// </summary>
/// <param name="Documento">CPF/CNPJ (com ou sem mascara).</param>
/// <param name="Senha">Senha em claro informada.</param>
public sealed record AutenticarCidadaoCommand(string Documento, string Senha)
    : ICommand<ResultadoAutenticacaoCidadao>;

/// <summary>Regras de validacao da autenticacao do cidadao.</summary>
public sealed class AutenticarCidadaoValidator : AbstractValidator<AutenticarCidadaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AutenticarCidadaoValidator()
    {
        RuleFor(comando => comando.Documento).NotEmpty();
        RuleFor(comando => comando.Senha).NotEmpty();
    }
}

/// <summary>
/// Excecao de autenticacao do cidadao com mensagem generica deliberada (nao vaza a causa exata da
/// falha — documento, senha ou estado da conta — a um atacante).
/// </summary>
public sealed class AutenticacaoCidadaoFalhouException : Exception
{
    /// <summary>Cria a excecao com a mensagem padrao generica.</summary>
    public AutenticacaoCidadaoFalhouException()
        : base("Credenciais invalidas.")
    {
    }
}

/// <summary>Handler do caso de uso de autenticacao do cidadao.</summary>
public sealed class AutenticarCidadaoHandler(
    ICidadaoContaRepository contas,
    ISenhaHasherCidadao hasher,
    IEmissorTokenCidadao emissorToken)
    : ICommandHandler<AutenticarCidadaoCommand, ResultadoAutenticacaoCidadao>
{
    // Hash "falso" para igualar o custo de verificacao quando a conta nao existe (anti-enumeracao).
    private const string SenhaFalsaParaTimingEstavel = "senha-inexistente-para-timing-estavel";

    /// <inheritdoc />
    public async Task<ResultadoAutenticacaoCidadao> Handle(AutenticarCidadaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normaliza o documento; documento mal formado ainda gasta tempo de hash antes de falhar.
        string? documento = null;
        if (Cpf.TryCreate(request.Documento, out var cpf) && cpf is not null)
        {
            documento = cpf.Digitos;
        }
        else if (Cnpj.TryCreate(request.Documento, out var cnpj) && cnpj is not null)
        {
            documento = cnpj.Digitos;
        }

        if (documento is null)
        {
            hasher.Hash(SenhaFalsaParaTimingEstavel);
            throw new AutenticacaoCidadaoFalhouException();
        }

        var conta = await contas.ObterPorDocumentoAsync(documento, cancellationToken).ConfigureAwait(false);
        if (conta is null)
        {
            // Verifica contra hash sintetico para manter o tempo de resposta estavel.
            hasher.Verificar(request.Senha, hasher.Hash(SenhaFalsaParaTimingEstavel));
            throw new AutenticacaoCidadaoFalhouException();
        }

        var senhaConfere = hasher.Verificar(request.Senha, conta.SenhaHash);
        if (!senhaConfere || !conta.Ativo)
        {
            throw new AutenticacaoCidadaoFalhouException();
        }

        var token = emissorToken.Emitir(conta.Id, conta.TenantId, conta.Nome, conta.Documento, conta.SeloGovBr);

        return new ResultadoAutenticacaoCidadao(conta.Id.Value, conta.TenantId, conta.Nome, token);
    }
}

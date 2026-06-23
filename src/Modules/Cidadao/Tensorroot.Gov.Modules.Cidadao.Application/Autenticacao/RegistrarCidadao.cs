using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Cidadao.Application.Autenticacao;

/// <summary>
/// Cadastro SELF-SERVICE da conta-cidadao (CPF/CNPJ + senha) no tenant (municipio) corrente. Valida o
/// documento pelo VO do SharedKernel, faz o hash da senha (a senha em claro nunca toca o dominio/banco)
/// e cria a <see cref="CidadaoConta"/> com <see cref="OrigemConta.CadastroLocal"/>. O par
/// (TenantId, Documento) e UNICO — duplicidade e recusada (sem vazar timing de existencia).
/// </summary>
/// <param name="Documento">CPF ou CNPJ (com ou sem mascara).</param>
/// <param name="Nome">Nome/razao social.</param>
/// <param name="Senha">Senha em claro (sera hasheada; nunca persistida em claro).</param>
/// <param name="Email">E-mail de contato (opcional).</param>
/// <param name="Telefone">Telefone (opcional).</param>
public sealed record RegistrarCidadaoCommand(
    string Documento,
    string Nome,
    string Senha,
    string? Email = null,
    string? Telefone = null) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de cidadao.</summary>
public sealed class RegistrarCidadaoValidator : AbstractValidator<RegistrarCidadaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarCidadaoValidator()
    {
        RuleFor(comando => comando.Documento)
            .NotEmpty()
            .Must(documento => Cpf.TryCreate(documento, out _) || Cnpj.TryCreate(documento, out _))
            .WithMessage("Documento (CPF/CNPJ) invalido.");

        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(CidadaoConta.ComprimentoMaximoNome);

        // Senha minima forte (parametro de produto; o gov.br substitui no fluxo OIDC). // TODO(M10-creds).
        RuleFor(comando => comando.Senha).NotEmpty().MinimumLength(8).WithMessage("Senha deve ter ao menos 8 caracteres.");
    }
}

/// <summary>Erro de unicidade: ja existe conta-cidadao para o documento no tenant.</summary>
public sealed class CidadaoJaCadastradoException : Exception
{
    /// <summary>Cria a excecao com a mensagem padrao.</summary>
    public CidadaoJaCadastradoException()
        : base("Ja existe uma conta para este documento neste municipio.")
    {
    }
}

/// <summary>Handler do cadastro de cidadao.</summary>
public sealed class RegistrarCidadaoHandler(
    ICidadaoContaRepository contas,
    ISenhaHasherCidadao hasher,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<RegistrarCidadaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarCidadaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normaliza o documento para DIGITOS pelo VO valido (CPF ou CNPJ).
        var documento = Cpf.TryCreate(request.Documento, out var cpf) && cpf is not null
            ? cpf.Digitos
            : Cnpj.Create(request.Documento).Digitos;

        if (await contas.DocumentoJaCadastradoAsync(documento, cancellationToken).ConfigureAwait(false))
        {
            throw new CidadaoJaCadastradoException();
        }

        var senhaHash = hasher.Hash(request.Senha);
        var conta = CidadaoConta.CriarLocal(tenant.TenantId, documento, request.Nome, senhaHash, request.Email, request.Telefone);

        contas.Adicionar(conta);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // // TODO(M10-creds): disparar confirmacao de e-mail (Outbox) e, para atos "prata", 2FA.
        return conta.Id.Value;
    }
}

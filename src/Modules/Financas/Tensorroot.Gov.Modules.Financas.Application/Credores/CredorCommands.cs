using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Credores;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Credores;

/// <summary>Cadastra um novo credor/fornecedor do ente.</summary>
/// <param name="Nome">Nome/razão social.</param>
/// <param name="Tipo">Tipo de pessoa (1=Fisica, 2=Juridica).</param>
/// <param name="Documento">Documento (CPF/CNPJ) com ou sem máscara.</param>
/// <param name="Banco">Código do banco (opcional — dados bancários).</param>
/// <param name="Agencia">Agência (opcional).</param>
/// <param name="Conta">Conta (opcional).</param>
/// <param name="Pix">Chave PIX (opcional).</param>
public sealed record CadastrarCredorCommand(
    string Nome,
    TipoPessoa Tipo,
    string Documento,
    string? Banco = null,
    string? Agencia = null,
    string? Conta = null,
    string? Pix = null) : ICommand<Guid>;

/// <summary>Atualiza nome/dados bancários de um credor.</summary>
/// <param name="CredorId">Credor a atualizar.</param>
/// <param name="Nome">Novo nome (opcional — null mantém).</param>
/// <param name="Banco">Código do banco (opcional).</param>
/// <param name="Agencia">Agência (opcional).</param>
/// <param name="Conta">Conta (opcional).</param>
/// <param name="Pix">Chave PIX (opcional).</param>
public sealed record AtualizarCredorCommand(
    Guid CredorId,
    string? Nome,
    string? Banco,
    string? Agencia,
    string? Conta,
    string? Pix) : ICommand;

/// <summary>Inativa um credor (não recebe novos empenhos).</summary>
/// <param name="CredorId">Credor.</param>
public sealed record InativarCredorCommand(Guid CredorId) : ICommand;

/// <summary>Reativa um credor inativo.</summary>
/// <param name="CredorId">Credor.</param>
public sealed record ReativarCredorCommand(Guid CredorId) : ICommand;

/// <summary>Validação do cadastro de credor.</summary>
public sealed class CadastrarCredorValidator : AbstractValidator<CadastrarCredorCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarCredorValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Tipo).IsInEnum();
        RuleFor(c => c.Documento).NotEmpty().MaximumLength(20);
    }
}

/// <summary>Handler do cadastro de credor (unicidade por documento no tenant).</summary>
public sealed class CadastrarCredorHandler(
    ICredorRepository credores,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<CadastrarCredorCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarCredorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dadosBancarios = MontarDadosBancarios(request.Banco, request.Agencia, request.Conta, request.Pix);

        // Cria o agregado (valida documento) para normalizar antes de checar unicidade.
        var credor = CredorCadastrado.Cadastrar(
            tenant.TenantId,
            request.Nome,
            request.Tipo,
            request.Documento,
            dadosBancarios);

        if (await credores.ExisteComDocumentoAsync(credor.Documento, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Ja existe credor cadastrado com o documento {credor.Documento}.");
        }

        credores.Adicionar(credor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return credor.Id.Value;
    }

    internal static ContaBancaria? MontarDadosBancarios(string? banco, string? agencia, string? conta, string? pix)
    {
        var algumPreenchido = !string.IsNullOrWhiteSpace(banco)
            || !string.IsNullOrWhiteSpace(agencia)
            || !string.IsNullOrWhiteSpace(conta);
        if (!algumPreenchido)
        {
            return null;
        }

        // ContaBancaria exige banco/agencia/conta; campos parciais sao invalidos (ArgumentException).
        return ContaBancaria.De(banco!, agencia!, conta!, pix);
    }
}

/// <summary>Handler da atualização de credor.</summary>
public sealed class AtualizarCredorHandler(
    ICredorRepository credores,
    IUnitOfWork unitOfWork) : ICommandHandler<AtualizarCredorCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtualizarCredorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var credor = await credores.ObterPorIdAsync(new CredorId(request.CredorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Credor {request.CredorId} nao encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Nome))
        {
            credor.AlterarNome(request.Nome);
        }

        // Dados bancarios so sao tocados se algum campo bancario veio no comando.
        if (request.Banco is not null || request.Agencia is not null || request.Conta is not null || request.Pix is not null)
        {
            credor.DefinirDadosBancarios(
                CadastrarCredorHandler.MontarDadosBancarios(request.Banco, request.Agencia, request.Conta, request.Pix));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da inativação de credor.</summary>
public sealed class InativarCredorHandler(
    ICredorRepository credores,
    IUnitOfWork unitOfWork) : ICommandHandler<InativarCredorCommand>
{
    /// <inheritdoc />
    public async Task Handle(InativarCredorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credor = await credores.ObterPorIdAsync(new CredorId(request.CredorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Credor {request.CredorId} nao encontrado.");
        credor.Inativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da reativação de credor.</summary>
public sealed class ReativarCredorHandler(
    ICredorRepository credores,
    IUnitOfWork unitOfWork) : ICommandHandler<ReativarCredorCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReativarCredorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credor = await credores.ObterPorIdAsync(new CredorId(request.CredorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Credor {request.CredorId} nao encontrado.");
        credor.Reativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

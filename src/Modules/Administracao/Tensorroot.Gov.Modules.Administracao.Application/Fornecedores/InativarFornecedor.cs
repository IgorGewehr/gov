using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;

namespace Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;

/// <summary>Inativa o cadastro do fornecedor (passa a <c>Inativo</c>).</summary>
/// <param name="FornecedorId">Fornecedor a inativar.</param>
public sealed record InativarFornecedorCommand(Guid FornecedorId) : ICommand;

/// <summary>Regras de validacao da inativacao de fornecedor.</summary>
public sealed class InativarFornecedorValidator : AbstractValidator<InativarFornecedorCommand>
{
    /// <summary>Define as regras.</summary>
    public InativarFornecedorValidator()
    {
        RuleFor(comando => comando.FornecedorId).NotEmpty();
    }
}

/// <summary>Handler da inativacao de fornecedor.</summary>
public sealed class InativarFornecedorHandler(
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork)
    : ICommandHandler<InativarFornecedorCommand>
{
    /// <inheritdoc />
    public async Task Handle(InativarFornecedorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(request.FornecedorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fornecedor nao encontrado.");

        fornecedor.Inativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

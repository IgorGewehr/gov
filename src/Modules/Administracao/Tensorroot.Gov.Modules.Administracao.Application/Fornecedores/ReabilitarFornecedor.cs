using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;

/// <summary>Reabilita o fornecedor apos cumprimento/encerramento das sancoes impeditivas (I-11).</summary>
/// <param name="FornecedorId">Fornecedor a reabilitar.</param>
public sealed record ReabilitarFornecedorCommand(Guid FornecedorId) : ICommand;

/// <summary>Regras de validacao da reabilitacao de fornecedor.</summary>
public sealed class ReabilitarFornecedorValidator : AbstractValidator<ReabilitarFornecedorCommand>
{
    /// <summary>Define as regras.</summary>
    public ReabilitarFornecedorValidator()
    {
        RuleFor(comando => comando.FornecedorId).NotEmpty();
    }
}

/// <summary>Handler da reabilitacao de fornecedor.</summary>
public sealed class ReabilitarFornecedorHandler(
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork,
    IDataHojeTenant dataHoje)
    : ICommandHandler<ReabilitarFornecedorCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReabilitarFornecedorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(request.FornecedorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fornecedor nao encontrado.");

        // Data da reabilitacao (encerra a janela de impedimento) no FUSO do tenant (UTC-3).
        var hoje = dataHoje.Hoje();
        fornecedor.Reabilitar(hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

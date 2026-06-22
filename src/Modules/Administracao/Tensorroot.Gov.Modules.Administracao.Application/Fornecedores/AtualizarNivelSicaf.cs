using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;

namespace Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;

/// <summary>Atualiza o nivel cadastral do fornecedor consultado no SICAF (art. 87).</summary>
/// <param name="FornecedorId">Fornecedor a atualizar.</param>
/// <param name="Nivel">Novo nivel cadastral no SICAF.</param>
public sealed record AtualizarNivelSicafCommand(Guid FornecedorId, NivelCadastralSICAF Nivel) : ICommand;

/// <summary>Regras de validacao da atualizacao de nivel SICAF.</summary>
public sealed class AtualizarNivelSicafValidator : AbstractValidator<AtualizarNivelSicafCommand>
{
    /// <summary>Define as regras.</summary>
    public AtualizarNivelSicafValidator()
    {
        RuleFor(comando => comando.FornecedorId).NotEmpty();
        RuleFor(comando => comando.Nivel).IsInEnum();
    }
}

/// <summary>Handler da atualizacao de nivel SICAF.</summary>
public sealed class AtualizarNivelSicafHandler(
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AtualizarNivelSicafCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtualizarNivelSicafCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(request.FornecedorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fornecedor nao encontrado.");

        fornecedor.AtualizarNivelSicaf(request.Nivel);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

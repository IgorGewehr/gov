using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Catalogo;

/// <summary>Atualiza dados descritivos de um item de catalogo.</summary>
/// <param name="ItemId">Identificador do item.</param>
/// <param name="Descricao">Nova descricao.</param>
/// <param name="UnidadeFornecimento">Nova unidade de fornecimento.</param>
/// <param name="Classe">Nova classe/classificacao (opcional).</param>
public sealed record AtualizarItemCatalogoCommand(
    Guid ItemId,
    string Descricao,
    string UnidadeFornecimento,
    string? Classe) : ICommand;

/// <summary>Inativa um item de catalogo.</summary>
/// <param name="ItemId">Identificador do item.</param>
public sealed record InativarItemCatalogoCommand(Guid ItemId) : ICommand;

/// <summary>Reativa um item de catalogo previamente inativado.</summary>
/// <param name="ItemId">Identificador do item.</param>
public sealed record ReativarItemCatalogoCommand(Guid ItemId) : ICommand;

/// <summary>Regras de validacao da atualizacao de item de catalogo.</summary>
public sealed class AtualizarItemCatalogoValidator : AbstractValidator<AtualizarItemCatalogoCommand>
{
    /// <summary>Define as regras.</summary>
    public AtualizarItemCatalogoValidator()
    {
        RuleFor(c => c.ItemId).NotEmpty();
        RuleFor(c => c.Descricao).NotEmpty().MaximumLength(500);
        RuleFor(c => c.UnidadeFornecimento).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Classe).MaximumLength(120);
    }
}

/// <summary>Handler da atualizacao de item de catalogo.</summary>
public sealed class AtualizarItemCatalogoHandler(ICatalogoRepository catalogo, IUnitOfWork unitOfWork)
    : ICommandHandler<AtualizarItemCatalogoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtualizarItemCatalogoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await catalogo.ObterPorIdAsync(new ItemCatalogoId(request.ItemId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de catalogo nao encontrado.");
        item.Atualizar(request.Descricao, request.UnidadeFornecimento, request.Classe);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da inativacao de item de catalogo.</summary>
public sealed class InativarItemCatalogoHandler(ICatalogoRepository catalogo, IUnitOfWork unitOfWork)
    : ICommandHandler<InativarItemCatalogoCommand>
{
    /// <inheritdoc />
    public async Task Handle(InativarItemCatalogoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await catalogo.ObterPorIdAsync(new ItemCatalogoId(request.ItemId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de catalogo nao encontrado.");
        item.Inativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da reativacao de item de catalogo.</summary>
public sealed class ReativarItemCatalogoHandler(ICatalogoRepository catalogo, IUnitOfWork unitOfWork)
    : ICommandHandler<ReativarItemCatalogoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReativarItemCatalogoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await catalogo.ObterPorIdAsync(new ItemCatalogoId(request.ItemId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de catalogo nao encontrado.");
        item.Reativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

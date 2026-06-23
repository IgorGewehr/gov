using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Registra um bem físico encontrado sem tombo ("sobra"/achado) durante a contagem.</summary>
/// <param name="InventarioId">Inventário em contagem.</param>
/// <param name="Descricao">Descrição do achado.</param>
/// <param name="Localizacao">Localização onde foi encontrado.</param>
/// <param name="ValorEstimado">Valor estimado do achado.</param>
public sealed record RegistrarBemNaoCadastradoCommand(
    Guid InventarioId,
    string Descricao,
    string Localizacao,
    decimal ValorEstimado) : ICommand;

/// <summary>Regras de validação do registro de sobra.</summary>
public sealed class RegistrarBemNaoCadastradoValidator : AbstractValidator<RegistrarBemNaoCadastradoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarBemNaoCadastradoValidator()
    {
        RuleFor(comando => comando.InventarioId).NotEmpty();
        RuleFor(comando => comando.Descricao).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.Localizacao).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.ValorEstimado).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler do registro de bem não cadastrado (sobra).</summary>
public sealed class RegistrarBemNaoCadastradoHandler(
    IInventarioRepository inventarios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarBemNaoCadastradoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarBemNaoCadastradoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventario = await inventarios.ObterPorIdAsync(new InventarioId(request.InventarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventário não encontrado.");

        inventario.RegistrarBemNaoCadastrado(request.Descricao, request.Localizacao, request.ValorEstimado);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

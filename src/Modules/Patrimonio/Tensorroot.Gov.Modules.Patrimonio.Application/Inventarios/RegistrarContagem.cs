using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Registra a contagem física de um item do snapshot do inventário.</summary>
/// <param name="InventarioId">Inventário em contagem.</param>
/// <param name="BemPatrimonialId">Bem contado (deve constar do snapshot).</param>
/// <param name="SituacaoEncontrada">Situação física (1 = Localizado, 2 = NaoLocalizado, 3 = LocalizadoOutroSetor, 4 = Inservivel).</param>
/// <param name="LocalizacaoEncontrada">Localização encontrada (opcional).</param>
/// <param name="Observacao">Observação livre (opcional).</param>
public sealed record RegistrarContagemCommand(
    Guid InventarioId,
    Guid BemPatrimonialId,
    int SituacaoEncontrada,
    string? LocalizacaoEncontrada,
    string? Observacao) : ICommand;

/// <summary>Regras de validação do registro de contagem.</summary>
public sealed class RegistrarContagemValidator : AbstractValidator<RegistrarContagemCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarContagemValidator()
    {
        RuleFor(comando => comando.InventarioId).NotEmpty();
        RuleFor(comando => comando.BemPatrimonialId).NotEmpty();
        RuleFor(comando => comando.SituacaoEncontrada).Must(situacao => Enum.IsDefined((SituacaoEncontrada)situacao))
            .WithMessage("Situação encontrada inválida.");
        RuleFor(comando => comando.LocalizacaoEncontrada).MaximumLength(200);
        RuleFor(comando => comando.Observacao).MaximumLength(500);
    }
}

/// <summary>Handler do registro de contagem física.</summary>
public sealed class RegistrarContagemHandler(
    IInventarioRepository inventarios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarContagemCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarContagemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventario = await inventarios.ObterPorIdAsync(new InventarioId(request.InventarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventário não encontrado.");

        inventario.RegistrarContagem(
            new BemPatrimonialId(request.BemPatrimonialId),
            (SituacaoEncontrada)request.SituacaoEncontrada,
            request.LocalizacaoEncontrada,
            request.Observacao);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

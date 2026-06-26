using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>Adiciona um item a uma dispensa em rascunho (situacao <c>Aberta</c>).</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="ItemCatalogoId">Referencia ao item de catalogo (opcional).</param>
/// <param name="Descricao">Descricao do objeto do item.</param>
/// <param name="Quantidade">Quantidade demandada.</param>
/// <param name="ValorUnitarioEstimado">Valor unitario estimado/orcado.</param>
public sealed record AdicionarItemDispensaCommand(
    Guid DispensaId,
    Guid? ItemCatalogoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitarioEstimado) : ICommand<Guid>;

/// <summary>Regras de validacao da inclusao de item.</summary>
public sealed class AdicionarItemDispensaValidator : AbstractValidator<AdicionarItemDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarItemDispensaValidator()
    {
        RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
        RuleFor(comando => comando.Descricao).NotEmpty().MaximumLength(500).WithMessage("Descricao e obrigatoria (max. 500).");
        RuleFor(comando => comando.Quantidade).GreaterThan(0).WithMessage("Quantidade deve ser positiva.");
        RuleFor(comando => comando.ValorUnitarioEstimado).GreaterThan(0).WithMessage("Valor unitario estimado deve ser positivo.");
    }
}

/// <summary>Handler da inclusao de item de dispensa.</summary>
public sealed class AdicionarItemDispensaHandler(
    IDispensaRepository dispensas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarItemDispensaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarItemDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");

        var itemId = dispensa.AdicionarItem(
            request.ItemCatalogoId,
            request.Descricao,
            request.Quantidade,
            ValorMonetario.De(request.ValorUnitarioEstimado));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return itemId.Value;
    }
}

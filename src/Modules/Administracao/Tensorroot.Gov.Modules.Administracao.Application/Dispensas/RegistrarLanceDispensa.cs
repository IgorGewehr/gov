using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>Registra um lance (cotacao) de um fornecedor para um item, na etapa de disputa.</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="ItemId">Item cotado.</param>
/// <param name="FornecedorId">Fornecedor proponente.</param>
/// <param name="Valor">Valor (unitario) ofertado.</param>
public sealed record RegistrarLanceDispensaCommand(
    Guid DispensaId,
    Guid ItemId,
    Guid FornecedorId,
    decimal Valor) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de lance.</summary>
public sealed class RegistrarLanceDispensaValidator : AbstractValidator<RegistrarLanceDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarLanceDispensaValidator()
    {
        RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
        RuleFor(comando => comando.ItemId).NotEmpty().WithMessage("Item e obrigatorio.");
        RuleFor(comando => comando.FornecedorId).NotEmpty().WithMessage("Fornecedor e obrigatorio.");
        RuleFor(comando => comando.Valor).GreaterThan(0).WithMessage("Valor do lance deve ser positivo.");
    }
}

/// <summary>Handler do registro de lance.</summary>
public sealed class RegistrarLanceDispensaHandler(
    IDispensaRepository dispensas,
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarLanceDispensaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarLanceDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");

        var agora = timeProvider.GetUtcNow();

        // Fail-closed: aferir a aptidao do fornecedor (sancao impeditiva vigente) no limite de agregado e
        // passa-la a Dispensa, que recusa a cotacao de impedido (art. 14/156 Lei 14.133/2021).
        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(request.FornecedorId), cancellationToken).ConfigureAwait(false);
        var fornecedorImpedido = fornecedor is not null && fornecedor.EstaImpedido(DateOnly.FromDateTime(agora.UtcDateTime));

        var cotacaoId = dispensa.RegistrarLance(
            request.FornecedorId,
            new ItemDispensaId(request.ItemId),
            ValorMonetario.De(request.Valor),
            agora,
            fornecedorImpedido);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return cotacaoId.Value;
    }
}

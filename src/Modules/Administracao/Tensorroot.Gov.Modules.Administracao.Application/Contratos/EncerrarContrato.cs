using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Encerra o contrato por termino da vigencia/conclusao do objeto.</summary>
/// <param name="ContratoId">Contrato a encerrar.</param>
public sealed record EncerrarContratoCommand(Guid ContratoId) : ICommand;

/// <summary>Regras de validacao do encerramento.</summary>
public sealed class EncerrarContratoValidator : AbstractValidator<EncerrarContratoCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarContratoValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty();
    }
}

/// <summary>Handler do encerramento do contrato.</summary>
public sealed class EncerrarContratoHandler(IContratoRepository contratos, IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarContratoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarContratoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato nao encontrado.");

        contrato.Encerrar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

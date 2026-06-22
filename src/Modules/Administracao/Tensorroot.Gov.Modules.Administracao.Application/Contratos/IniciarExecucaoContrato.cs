using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Inicia a execucao do contrato (exige eficacia: PNCP + dotacao) — I-7/I-8.</summary>
/// <param name="ContratoId">Contrato a iniciar.</param>
public sealed record IniciarExecucaoContratoCommand(Guid ContratoId) : ICommand;

/// <summary>Regras de validacao do inicio de execucao.</summary>
public sealed class IniciarExecucaoContratoValidator : AbstractValidator<IniciarExecucaoContratoCommand>
{
    /// <summary>Define as regras.</summary>
    public IniciarExecucaoContratoValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty();
    }
}

/// <summary>Handler do inicio de execucao do contrato.</summary>
public sealed class IniciarExecucaoContratoHandler(IContratoRepository contratos, IUnitOfWork unitOfWork)
    : ICommandHandler<IniciarExecucaoContratoCommand>
{
    /// <inheritdoc />
    public async Task Handle(IniciarExecucaoContratoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato nao encontrado.");

        contrato.IniciarExecucao();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

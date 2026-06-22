using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Registra a posse de um servidor nomeado dentro do prazo legal.</summary>
/// <param name="ServidorId">Servidor a empossar.</param>
/// <param name="DataPosse">Data da posse.</param>
public sealed record RegistrarPosseCommand(Guid ServidorId, DateOnly DataPosse) : ICommand;

/// <summary>Regras de validacao do registro de posse.</summary>
public sealed class RegistrarPosseValidator : AbstractValidator<RegistrarPosseCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarPosseValidator()
    {
        RuleFor(comando => comando.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(comando => comando.DataPosse).NotEmpty().WithMessage("Data de posse e obrigatoria.");
    }
}

/// <summary>Handler do registro de posse.</summary>
public sealed class RegistrarPosseHandler(IServidorRepository servidores, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarPosseCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarPosseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        servidor.RegistrarPosse(request.DataPosse);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

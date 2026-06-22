using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Escolas;

/// <summary>Desativa uma escola (estado terminal). So permitido a partir de credenciada.</summary>
/// <param name="EscolaId">Identificador da escola.</param>
public sealed record DesativarEscolaCommand(Guid EscolaId) : ICommand;

/// <summary>Handler da desativacao de escola.</summary>
public sealed class DesativarEscolaHandler(
    IEscolaRepository escolas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DesativarEscolaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DesativarEscolaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var escola = await escolas.ObterPorIdAsync(new EscolaId(request.EscolaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Escola nao encontrada.");

        escola.Desativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

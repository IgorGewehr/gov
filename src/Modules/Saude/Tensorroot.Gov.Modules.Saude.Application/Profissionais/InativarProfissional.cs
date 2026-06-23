using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Profissionais;

/// <summary>Inativa um profissional (desligamento) — estado terminal.</summary>
/// <param name="ProfissionalId">Identificador do profissional.</param>
public sealed record InativarProfissionalCommand(Guid ProfissionalId) : ICommand;

/// <summary>Handler da inativacao de profissional.</summary>
public sealed class InativarProfissionalHandler(
    IProfissionalCadastroRepository profissionais,
    IUnitOfWork unitOfWork)
    : ICommandHandler<InativarProfissionalCommand>
{
    /// <inheritdoc />
    public async Task Handle(InativarProfissionalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profissional = await profissionais
            .ObterPorIdAsync(new ProfissionalId(request.ProfissionalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Profissional nao encontrado.");

        profissional.Inativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

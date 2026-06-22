using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Matriculas;

/// <summary>Transfere o aluno de uma matricula Ativa (movimento para outra escola/turma).</summary>
/// <param name="MatriculaId">Identificador da matricula a transferir.</param>
public sealed record TransferirAlunoCommand(Guid MatriculaId) : ICommand;

/// <summary>Handler da transferencia de aluno.</summary>
public sealed class TransferirAlunoHandler(
    IMatriculaRepository matriculas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<TransferirAlunoCommand>
{
    /// <inheritdoc />
    public async Task Handle(TransferirAlunoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var matricula = await matriculas.ObterPorIdAsync(new MatriculaId(request.MatriculaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Matricula nao encontrada.");

        // Regra de dominio: exige situacao Ativa (I-5); estado terminal nao admite transicao (I-9).
        matricula.Transferir();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

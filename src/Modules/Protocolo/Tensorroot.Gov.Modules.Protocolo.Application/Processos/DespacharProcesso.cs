using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Processos;

/// <summary>
/// Registra um despacho (manifestacao/decisao de autoridade) no processo, sem alterar a situacao;
/// preserva a trilha documental append-only. Protegido por invariantes de dominio.
/// </summary>
/// <param name="ProcessoId">Processo a despachar.</param>
/// <param name="Texto">Conteudo do despacho.</param>
/// <param name="AutoridadeId">Autoridade que profere o despacho.</param>
public sealed record DespacharProcessoCommand(
    Guid ProcessoId,
    string Texto,
    Guid AutoridadeId) : ICommand;

/// <summary>Handler do despacho de processo.</summary>
public sealed class DespacharProcessoHandler(
    IProcessoRepository processos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<DespacharProcessoCommand>
{
    /// <inheritdoc />
    public async Task Handle(DespacharProcessoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processo = await processos.ObterPorIdAsync(new ProcessoId(request.ProcessoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        processo.Despachar(request.Texto, request.AutoridadeId, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

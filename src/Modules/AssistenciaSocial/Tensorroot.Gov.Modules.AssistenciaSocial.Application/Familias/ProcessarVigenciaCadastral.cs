using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;

/// <summary>
/// Processa a vigencia cadastral de uma familia: se a ultima atualizacao do CadUnico tiver mais
/// de 24 meses, sinaliza <see cref="SituacaoFamilia.AtualizacaoVencida"/> (I-5/I-6). Idempotente.
/// </summary>
/// <param name="FamiliaId">Identificador da familia.</param>
public sealed record ProcessarVigenciaCadastralCommand(Guid FamiliaId) : ICommand;

/// <summary>Handler do processamento de vigencia cadastral.</summary>
public sealed class ProcessarVigenciaCadastralHandler(
    IFamiliaRepository familias,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<ProcessarVigenciaCadastralCommand>
{
    /// <inheritdoc />
    public async Task Handle(ProcessarVigenciaCadastralCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = tenant.TenantId;

        var familia = await familias.ObterPorIdAsync(new FamiliaId(request.FamiliaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Familia nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        familia.ProcessarVigenciaCadastral(hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

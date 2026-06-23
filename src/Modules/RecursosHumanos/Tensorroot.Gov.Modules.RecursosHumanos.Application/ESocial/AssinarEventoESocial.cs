using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;

/// <summary>
/// Assina o XML de um evento <c>Gerado</c> com o A1 do ente via Cofre (perfil eSocial: XML-DSig
/// Enveloped, C14N inclusiva, RSA-SHA256, KeyInfo so X509, Reference URI="" — ESOCIAL-SPEC §2.4) e
/// transita para <c>Assinado</c>. Idempotente (re-assinar Assinado e no-op). Usa
/// <see cref="IAssinaturaEmEscopoDedicado"/> (guarda H5: handler do RH ja resolveu o seu DbContext).
/// </summary>
/// <param name="EventoId">Identificador do evento eSocial.</param>
public sealed record AssinarEventoESocialCommand(Guid EventoId) : ICommand;

/// <summary>Handler da assinatura de evento eSocial.</summary>
public sealed class AssinarEventoESocialHandler(
    IEventoESocialRepository eventos,
    IAssinaturaEmEscopoDedicado assinatura,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AssinarEventoESocialCommand>
{
    /// <inheritdoc />
    public async Task Handle(AssinarEventoESocialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var evento = await eventos.ObterPorIdAsync(new EventoESocialId(request.EventoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Evento eSocial nao encontrado.");

        if (evento.Estado == EstadoEventoESocial.Assinado)
        {
            return; // Idempotente.
        }

        // ESOCIAL-SPEC §2.4: zero codigo novo de assinatura — o destino eSocial fixa o perfil correto.
        var resultado = await assinatura
            .AssinarXmlAsync(evento.Xml, new OpcoesAssinaturaXml(DestinoAssinatura.ESocial), cancellationToken)
            .ConfigureAwait(false);

        evento.RegistrarAssinatura(resultado.XmlAssinado, resultado.Thumbprint, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

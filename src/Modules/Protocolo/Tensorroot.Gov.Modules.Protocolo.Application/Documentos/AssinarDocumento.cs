using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Contracts;
using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Documentos;

/// <summary>
/// Assina um documento juntado/valido conforme a criticidade do ato (Lei 14.063/2020 +
/// Decreto 10.543/2020); rejeita nivel de assinatura inferior ao exigido.
/// </summary>
/// <param name="DocumentoId">Documento a assinar.</param>
/// <param name="SignatarioId">Sujeito que assina.</param>
/// <param name="Tipo">Nivel da assinatura aplicada.</param>
public sealed record AssinarDocumentoCommand(
    Guid DocumentoId,
    Guid SignatarioId,
    TipoAssinatura Tipo) : ICommand;

/// <summary>Regras de validacao da assinatura de documento.</summary>
public sealed class AssinarDocumentoValidator : AbstractValidator<AssinarDocumentoCommand>
{
    /// <summary>Define as regras.</summary>
    public AssinarDocumentoValidator()
    {
        RuleFor(comando => comando.DocumentoId).NotEmpty();
        RuleFor(comando => comando.SignatarioId).NotEmpty();
        RuleFor(comando => comando.Tipo).IsInEnum();
    }
}

/// <summary>Handler da assinatura de documento por criticidade.</summary>
public sealed class AssinarDocumentoHandler(
    IDocumentoRepository documentos,
    ICarimbadorDeTempo carimbador,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AssinarDocumentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AssinarDocumentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documento = await documentos.ObterPorIdAsync(new DocumentoId(request.DocumentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Documento nao encontrado.");

        // W9.4 (Peca 1): carimba o HASH do documento (RFC 3161 / ACL + Polly). A ACT atesta o hash,
        // nunca o conteudo. O carimbo retornado e vinculado a esse hash e validado no dominio (Assinar).
        var carimbo = await carimbador.CarimbarAsync(documento.Hash, cancellationToken).ConfigureAwait(false);
        documento.Assinar(request.SignatarioId, request.Tipo, carimbo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new DocumentoAssinadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            documento.Id.Value,
            documento.ProcessoId ?? Guid.Empty,
            request.Tipo.ToString());

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}

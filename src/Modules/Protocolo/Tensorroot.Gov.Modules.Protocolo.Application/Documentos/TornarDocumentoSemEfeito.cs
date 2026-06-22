using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Documentos;

/// <summary>
/// Torna um documento juntado/assinado sem efeito (terminal), preservando o registro na trilha
/// documental. Nao ha comando de exclusao: esta e a unica "remocao" logica permitida (Lei 11.419/2006).
/// </summary>
/// <param name="DocumentoId">Documento a tornar sem efeito.</param>
/// <param name="Motivo">Motivo obrigatorio do "sem efeito".</param>
public sealed record TornarDocumentoSemEfeitoCommand(Guid DocumentoId, string Motivo) : ICommand;

/// <summary>Regras de validacao do "tornar sem efeito".</summary>
public sealed class TornarDocumentoSemEfeitoValidator : AbstractValidator<TornarDocumentoSemEfeitoCommand>
{
    /// <summary>Define as regras.</summary>
    public TornarDocumentoSemEfeitoValidator()
    {
        RuleFor(comando => comando.DocumentoId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler do "tornar documento sem efeito".</summary>
public sealed class TornarDocumentoSemEfeitoHandler(
    IDocumentoRepository documentos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<TornarDocumentoSemEfeitoCommand>
{
    /// <inheritdoc />
    public async Task Handle(TornarDocumentoSemEfeitoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documento = await documentos.ObterPorIdAsync(new DocumentoId(request.DocumentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Documento nao encontrado.");

        documento.TornarSemEfeito(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

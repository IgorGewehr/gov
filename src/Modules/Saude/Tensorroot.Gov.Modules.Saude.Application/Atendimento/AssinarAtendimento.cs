using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Contracts;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Assina o atendimento em ICP-Brasil (NGS2), tornando-o imutavel.</summary>
/// <param name="AtendimentoId">Atendimento a assinar.</param>
/// <param name="CertificadoIcpBrasil">Identificacao do certificado ICP-Brasil (NGS2).</param>
/// <param name="Hash">Hash criptografico da assinatura.</param>
public sealed record AssinarAtendimentoCommand(
    Guid AtendimentoId,
    string CertificadoIcpBrasil,
    string Hash) : ICommand;

/// <summary>Regras de validacao da assinatura de atendimento.</summary>
public sealed class AssinarAtendimentoValidator : AbstractValidator<AssinarAtendimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public AssinarAtendimentoValidator()
    {
        RuleFor(comando => comando.AtendimentoId).NotEmpty().WithMessage("Atendimento e obrigatorio.");
        RuleFor(comando => comando.CertificadoIcpBrasil).NotEmpty().WithMessage("Certificado ICP-Brasil (NGS2) e obrigatorio.");
        RuleFor(comando => comando.Hash).NotEmpty().WithMessage("Hash da assinatura e obrigatorio.");
    }
}

/// <summary>Handler da assinatura de atendimento.</summary>
public sealed class AssinarAtendimentoHandler(
    IAtendimentoRepository atendimentos,
    IAssinaturaIcpBrasilService assinaturaService,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AssinarAtendimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AssinarAtendimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var atendimento = await atendimentos
            .ObterPorIdAsync(new AtendimentoId(request.AtendimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Atendimento nao encontrado.");

        // I-3/I-4: valida o certificado ICP-Brasil e produz a assinatura NGS2.
        var assinatura = await assinaturaService
            .AssinarAsync(request.CertificadoIcpBrasil, request.Hash, cancellationToken)
            .ConfigureAwait(false);

        atendimento.Assinar(assinatura);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new AtendimentoAssinadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            atendimento.Id.Value,
            atendimento.PacienteId.Value);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}

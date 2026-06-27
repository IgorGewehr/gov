using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.TempoServico;

/// <summary>
/// Anula uma certidao de tempo emitida (torna sem efeito; estado terminal). A numeracao consumida e'
/// preservada (a sequencia do exercicio nao retrocede).
/// </summary>
/// <param name="CertidaoId">Identificador da certidao.</param>
/// <param name="Motivo">Motivo da anulacao.</param>
public sealed record AnularCertidaoTempoServicoCommand(Guid CertidaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da anulacao de certidao.</summary>
public sealed class AnularCertidaoTempoServicoValidator : AbstractValidator<AnularCertidaoTempoServicoCommand>
{
    /// <summary>Define as regras.</summary>
    public AnularCertidaoTempoServicoValidator()
    {
        RuleFor(comando => comando.CertidaoId).NotEmpty().WithMessage("Certidao e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().WithMessage("Motivo da anulacao e obrigatorio.");
    }
}

/// <summary>Handler da anulacao de certidao.</summary>
public sealed class AnularCertidaoTempoServicoHandler(
    ICertidaoTempoServicoRepository certidoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AnularCertidaoTempoServicoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AnularCertidaoTempoServicoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var certidao = await certidoes.ObterPorIdAsync(new CertidaoTempoServicoId(request.CertidaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Certidao nao encontrada.");

        certidao.Anular(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

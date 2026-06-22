using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Fecha a competencia (so a partir de Calculada — I-7), disparando S-1299/S-1210/totalizadores/DCTFWeb (I-8).</summary>
/// <param name="FolhaDePagamentoId">Folha a fechar.</param>
public sealed record FecharFolhaCommand(Guid FolhaDePagamentoId) : ICommand;

/// <summary>Regras de validacao do fechamento de folha.</summary>
public sealed class FecharFolhaValidator : AbstractValidator<FecharFolhaCommand>
{
    /// <summary>Define as regras.</summary>
    public FecharFolhaValidator()
    {
        RuleFor(comando => comando.FolhaDePagamentoId)
            .NotEmpty()
            .WithMessage("Folha e obrigatoria.");
    }
}

/// <summary>
/// Handler do fechamento de folha. Publica o evento de integracao
/// <see cref="FolhaFechadaIntegrationEvent"/> (Contabilidade/Empenho — despesa de pessoal — I-8).
/// </summary>
public sealed class FecharFolhaHandler(
    IFolhaDePagamentoRepository folhas,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    TimeProvider timeProvider)
    : ICommandHandler<FecharFolhaCommand>
{
    /// <inheritdoc />
    public async Task Handle(FecharFolhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;

        // I-7: o agregado garante que so fecha a partir de Calculada.
        folha.Fechar(DateOnly.FromDateTime(agoraUtc));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new FolhaFechadaIntegrationEvent(
            Guid.NewGuid(),
            agoraUtc,
            folha.TenantId,
            folha.Id.Value,
            folha.Competencia.ToString(),
            folha.TotalLiquido.Valor);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}

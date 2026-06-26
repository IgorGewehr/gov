using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>
/// Homologa a dispensa (ato da autoridade competente): valida o resultado e habilita a contratacao
/// direta. O <paramref name="VencedorHabilitado"/> reflete a verificacao de habilitacao (regularidade
/// fiscal, social e trabalhista — IN SEGES/ME 67/2021) registrada pela equipe; a aptidao do vencedor
/// quanto a sancao impeditiva e rechecada aqui (fail-closed). Publica o evento de integracao.
/// </summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="VencedorHabilitado">Indica se o fornecedor vencedor foi habilitado (regularidade exigida).</param>
public sealed record HomologarDispensaCommand(Guid DispensaId, bool VencedorHabilitado) : ICommand;

/// <summary>Regras de validacao da homologacao de dispensa.</summary>
public sealed class HomologarDispensaValidator : AbstractValidator<HomologarDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public HomologarDispensaValidator()
        => RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
}

/// <summary>Handler da homologacao de dispensa.</summary>
public sealed class HomologarDispensaHandler(
    IDispensaRepository dispensas,
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<HomologarDispensaCommand>
{
    /// <inheritdoc />
    public async Task Handle(HomologarDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");

        var agora = timeProvider.GetUtcNow().UtcDateTime;

        // Fail-closed: rechecar a aptidao do vencedor no ato da homologacao (sancao impeditiva pode ter
        // sobrevindo). Limite de agregado: consulta o Fornecedor e passa a aptidao a Dispensa, que recusa
        // (art. 14/156 Lei 14.133/2021).
        var vencedorId = dispensa.FornecedorVencedorId();
        var vencedorImpedido = vencedorId is { } vid
            && await EstaImpedidoAsync(vid, agora, cancellationToken).ConfigureAwait(false);

        dispensa.Homologar(request.VencedorHabilitado, vencedorImpedido);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var fornecedorVencedorId = dispensa.FornecedorVencedorId() ?? Guid.Empty;
        var valorAdjudicado = dispensa.ValorAdjudicado();

        await publisher.Publish(
            new DispensaHomologadaIntegrationEvent(
                Guid.NewGuid(),
                agora,
                tenant.TenantId,
                dispensa.Id.Value,
                fornecedorVencedorId,
                valorAdjudicado),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> EstaImpedidoAsync(Guid fornecedorId, DateTime referencia, CancellationToken cancellationToken)
    {
        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(fornecedorId), cancellationToken).ConfigureAwait(false);
        return fornecedor is not null && fornecedor.EstaImpedido(DateOnly.FromDateTime(referencia));
    }
}

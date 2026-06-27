using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.SharedKernel.Tempo;

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
    IIntegrationEventWriter integrationEvents,
    ITenantContext tenant,
    IDataHojeTenant dataHoje,
    TimeProvider timeProvider)
    : ICommandHandler<HomologarDispensaCommand>
{
    /// <inheritdoc />
    public async Task Handle(HomologarDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");

        // Instante ABSOLUTO do Integration Event (Outbox/ordenacao global) -> UTC. P2-9: a afericao da
        // sancao (dia CIVIL) usa a data do FUSO do tenant (UTC-3) via IDataHojeTenant, nunca o UTC cru:
        // a noite no Brasil, o "hoje" UTC ja virou o dia seguinte e mascararia/anteciparia o impedimento.
        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        var hoje = dataHoje.Hoje();

        // Fail-closed: rechecar a aptidao do vencedor no ato da homologacao (sancao impeditiva pode ter
        // sobrevindo). Limite de agregado: consulta o Fornecedor e passa a aptidao a Dispensa, que recusa
        // (art. 14/156 Lei 14.133/2021).
        var vencedorId = dispensa.FornecedorVencedorId();
        var vencedorImpedido = vencedorId is { } vid
            && await EstaImpedidoAsync(vid, hoje, cancellationToken).ConfigureAwait(false);

        dispensa.Homologar(request.VencedorHabilitado, vencedorImpedido);

        var fornecedorVencedorId = dispensa.FornecedorVencedorId() ?? Guid.Empty;
        var valorAdjudicado = dispensa.ValorAdjudicado();

        // P1-3: Integration Event enfileirado no Outbox na MESMA transacao do SaveChanges (consistencia
        // transacional — CLAUDE.md §8/§10), como o irmao PublicarContratoNoPncp. Antes era publicado
        // in-process via IPublisher APOS o commit: um crash entre commit e publish perdia o evento (sem
        // retry/idempotencia). O OutboxPublisher despacha ao consumidor ao drenar (at-least-once).
        integrationEvents.Enfileirar(
            new DispensaHomologadaIntegrationEvent(
                Guid.NewGuid(),
                agoraUtc,
                tenant.TenantId,
                dispensa.Id.Value,
                fornecedorVencedorId,
                valorAdjudicado));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> EstaImpedidoAsync(Guid fornecedorId, DateOnly referencia, CancellationToken cancellationToken)
    {
        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(fornecedorId), cancellationToken).ConfigureAwait(false);
        return fornecedor is not null && fornecedor.EstaImpedido(referencia);
    }
}

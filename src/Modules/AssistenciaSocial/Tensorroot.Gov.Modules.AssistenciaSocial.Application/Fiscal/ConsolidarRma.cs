using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Fiscal;

/// <summary>
/// A-2: abre (se preciso) e (re)consolida o RMA de uma unidade numa competencia, DERIVANDO os volumes
/// por servico do Prontuario SUAS/atendimentos ja existentes (evita dupla digitacao). Idempotente
/// enquanto a competencia esta aberta.
/// </summary>
/// <param name="UnidadeAtendimentoId">Unidade (CRAS/CREAS/Centro POP) a consolidar.</param>
/// <param name="Competencia">Competencia (ano/mes) de referencia.</param>
public sealed record ConsolidarRmaCommand(Guid UnidadeAtendimentoId, Competencia Competencia) : ICommand<Guid>;

/// <summary>Validacao da consolidacao do RMA.</summary>
public sealed class ConsolidarRmaValidator : AbstractValidator<ConsolidarRmaCommand>
{
    /// <summary>Define as regras.</summary>
    public ConsolidarRmaValidator()
    {
        RuleFor(c => c.UnidadeAtendimentoId).NotEmpty().WithMessage("Unidade de atendimento e obrigatoria.");
        RuleFor(c => c.Competencia).Must(c => c.EhValida()).WithMessage("Competencia (ano/mes) e obrigatoria e valida.");
    }
}

/// <summary>Handler da consolidacao do RMA a partir do prontuario.</summary>
public sealed class ConsolidarRmaHandler(
    IRegistroMensalAtendimentoRepository rmas,
    IConsolidacaoRmaReadModel consolidacao,
    IUnidadeAtendimentoTipoLookup unidades,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConsolidarRmaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConsolidarRmaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tipoUnidade = await unidades.ObterTipoAsync(request.UnidadeAtendimentoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade de atendimento inexistente no tenant.");

        var rma = await rmas.ObterPorUnidadeCompetenciaAsync(request.UnidadeAtendimentoId, request.Competencia, cancellationToken).ConfigureAwait(false);
        if (rma is null)
        {
            rma = RegistroMensalAtendimento.Abrir(tenant.TenantId, request.UnidadeAtendimentoId, tipoUnidade, request.Competencia);
            await rmas.AdicionarAsync(rma, cancellationToken).ConfigureAwait(false);
        }

        // A consolidacao DERIVA do prontuario (fonte unica) — nao ha digitacao paralela de volumes.
        var contagens = await consolidacao.ContarAtendimentosPorServicoAsync(request.UnidadeAtendimentoId, request.Competencia, cancellationToken).ConfigureAwait(false);
        rma.ConsolidarDoProntuario(contagens);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rma.Id.Value;
    }
}

/// <summary>A-2: fecha (sela) o RMA de uma competencia para envio ao MDS (RMA/SAGI).</summary>
/// <param name="RmaId">Identificador do RMA.</param>
public sealed record FecharRmaCommand(Guid RmaId) : ICommand;

/// <summary>Validacao do fechamento do RMA.</summary>
public sealed class FecharRmaValidator : AbstractValidator<FecharRmaCommand>
{
    /// <summary>Define as regras.</summary>
    public FecharRmaValidator() => RuleFor(c => c.RmaId).NotEmpty();
}

/// <summary>Handler do fechamento do RMA.</summary>
public sealed class FecharRmaHandler(
    IRegistroMensalAtendimentoRepository rmas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IPublisher publisher)
    : ICommandHandler<FecharRmaCommand>
{
    /// <inheritdoc />
    public async Task Handle(FecharRmaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rma = await rmas.ObterPorIdAsync(new RegistroMensalAtendimentoId(request.RmaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("RMA inexistente no tenant.");

        var agora = timeProvider.GetUtcNow().UtcDateTime;
        rma.Fechar(agora);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Integration Event (Outbox): a competencia foi selada e esta pronta para envio ao MDS.
        var evento = new RmaFechadoIntegrationEvent(
            Guid.NewGuid(),
            agora,
            tenant.TenantId,
            rma.Id.Value,
            rma.UnidadeAtendimentoId,
            rma.Competencia.ToString(),
            rma.TotalAtendimentos);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}

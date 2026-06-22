using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>
/// Homologa a licitacao (art. 71): ato da autoridade (ordenador de despesa) que valida o resultado.
/// Passa o certame a <c>Homologada</c> e publica os eventos de integracao via Outbox.
/// </summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
public sealed record HomologarLicitacaoCommand(Guid LicitacaoId) : ICommand;

/// <summary>Regras de validacao da homologacao de licitacao.</summary>
public sealed class HomologarLicitacaoValidator : AbstractValidator<HomologarLicitacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public HomologarLicitacaoValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
    }
}

/// <summary>Handler da homologacao de licitacao.</summary>
public sealed class HomologarLicitacaoHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<HomologarLicitacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(HomologarLicitacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        licitacao.Homologar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var agora = timeProvider.GetUtcNow().UtcDateTime;
        var fornecedorVencedorId = licitacao.FornecedorVencedorId() ?? Guid.Empty;
        var valorAdjudicado = licitacao.ValorAdjudicado();

        await publisher.Publish(
            new LicitacaoHomologadaIntegrationEvent(
                Guid.NewGuid(),
                agora,
                tenant.TenantId,
                licitacao.Id.Value,
                fornecedorVencedorId,
                valorAdjudicado),
            cancellationToken).ConfigureAwait(false);

        // B-14: aquisicao de bem permanente tambem sinaliza o Patrimonio (tombamento).
        await publisher.Publish(
            new AquisicaoBemHomologadaIntegrationEvent(
                Guid.NewGuid(),
                agora,
                tenant.TenantId,
                licitacao.Id.Value,
                licitacao.Objeto,
                valorAdjudicado),
            cancellationToken).ConfigureAwait(false);
    }
}

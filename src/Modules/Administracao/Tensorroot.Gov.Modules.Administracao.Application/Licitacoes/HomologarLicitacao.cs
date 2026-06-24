using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
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
    IFornecedorRepository fornecedores,
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

        var agora = timeProvider.GetUtcNow().UtcDateTime;

        // BUG-A1: rechecar a aptidao do vencedor no ato da homologacao (sancao impeditiva pode ter sobrevindo
        // entre habilitacao e homologacao). Limite de agregado: consulta o Fornecedor e passa a aptidao ao
        // agregado Licitacao, que recusa fail-closed (art. 14/156 Lei 14.133/2021).
        var vencedorId = licitacao.FornecedorVencedorId();
        var vencedorImpedido = vencedorId is { } vid
            && await EstaImpedidoAsync(vid, agora, cancellationToken).ConfigureAwait(false);

        licitacao.Homologar(vencedorImpedido);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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

    private async Task<bool> EstaImpedidoAsync(Guid fornecedorId, DateTime referencia, CancellationToken cancellationToken)
    {
        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(fornecedorId), cancellationToken).ConfigureAwait(false);
        return fornecedor is not null && fornecedor.EstaImpedido(DateOnly.FromDateTime(referencia));
    }
}

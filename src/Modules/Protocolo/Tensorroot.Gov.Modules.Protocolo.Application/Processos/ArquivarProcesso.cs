using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Contracts;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Processos;

/// <summary>Arquiva um processo (terminal); a guarda passa a reger-se pela TTD/CONARQ.</summary>
/// <param name="ProcessoId">Processo a arquivar.</param>
/// <param name="Motivo">Motivo do arquivamento (opcional).</param>
public sealed record ArquivarProcessoCommand(Guid ProcessoId, string? Motivo) : ICommand;

/// <summary>Regras de validacao do arquivamento de processo.</summary>
public sealed class ArquivarProcessoValidator : AbstractValidator<ArquivarProcessoCommand>
{
    /// <summary>Define as regras.</summary>
    public ArquivarProcessoValidator()
    {
        RuleFor(comando => comando.ProcessoId).NotEmpty();
    }
}

/// <summary>Handler do arquivamento de processo.</summary>
public sealed class ArquivarProcessoHandler(
    IProcessoRepository processos,
    ITabelaTemporalidadeRepository tabelas,
    IDestinacaoProcessoRepository destinacoes,
    IMotorTemporalidade motor,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<ArquivarProcessoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ArquivarProcessoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processo = await processos.ObterPorIdAsync(new ProcessoId(request.ProcessoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        processo.Arquivar(request.Motivo, hoje);

        // Peca 2 (W9.4): no arquivamento, calcula e registra a ficha de destinacao (CONARQ) — na MESMA
        // transacao (consistencia). Resolve a regra da TTD ATIVA do tenant para a classe do processo; se
        // nao houver TTD/regra, o processo arquiva sem ficha (a destinacao pode ser definida depois).
        await RegistrarDestinacaoAsync(processo, hoje, cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new ProcessoArquivadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            processo.Nup.Valor);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }

    private async Task RegistrarDestinacaoAsync(Processo processo, DateOnly hoje, CancellationToken cancellationToken)
    {
        var ja = await destinacoes.ObterPorProcessoAsync(processo.Id.Value, cancellationToken).ConfigureAwait(false);
        if (ja is not null)
        {
            return; // ficha ja existe — idempotente.
        }

        var ttd = await tabelas.ObterAtivaAsync(cancellationToken).ConfigureAwait(false);
        var regra = ttd?.ResolverRegra(processo.Classificacao.Codigo);
        if (regra is null)
        {
            return; // sem TTD/regra ativa para a classe — nao bloqueia o arquivamento.
        }

        // eventoBase resolvido do EventoContagem da regra (data de arquivamento por padrao).
        var eventoBase = regra.EventoContagem switch
        {
            EventoContagem.DataAutuacao => processo.DataAutuacao,
            _ => hoje, // DataArquivamento (padrao) e AprovacaoContas (sem data dedicada no Protocolo) usam hoje.
        };

        var plano = motor.Calcular(regra, eventoBase);
        var ficha = DestinacaoProcesso.Criar(
            tenant.TenantId,
            processo.Id.Value,
            processo.Classificacao.Codigo,
            plano.FimGuardaCorrente,
            plano.FimGuardaIntermediaria,
            plano.Destinacao,
            plano.DataAptidaoEliminacao);

        destinacoes.Adicionar(ficha);
    }
}

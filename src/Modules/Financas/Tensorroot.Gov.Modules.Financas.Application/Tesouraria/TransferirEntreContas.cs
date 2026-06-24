using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Tesouraria;

/// <summary>Transfere recurso entre duas contas da tesouraria (operação auditada, atômica).</summary>
/// <param name="ContaOrigemId">Conta debitada.</param>
/// <param name="ContaDestinoId">Conta creditada.</param>
/// <param name="Data">Data da transferência.</param>
/// <param name="Valor">Valor (positivo).</param>
/// <param name="Historico">Descrição.</param>
public sealed record TransferirEntreContasCommand(
    Guid ContaOrigemId,
    Guid ContaDestinoId,
    DateOnly Data,
    decimal Valor,
    string Historico) : ICommand;

/// <summary>Validação da transferência.</summary>
public sealed class TransferirEntreContasValidator : AbstractValidator<TransferirEntreContasCommand>
{
    /// <summary>Define as regras.</summary>
    public TransferirEntreContasValidator()
    {
        RuleFor(c => c.ContaOrigemId).NotEmpty();
        RuleFor(c => c.ContaDestinoId).NotEmpty()
            .NotEqual(c => c.ContaOrigemId).WithMessage("Conta destino deve ser diferente da origem.");
        RuleFor(c => c.Valor).GreaterThan(0m);
        RuleFor(c => c.Historico).NotEmpty().MaximumLength(300);
    }
}

/// <summary>
/// Handler da transferência: debita a origem e credita o destino na MESMA UnitOfWork
/// (transacional). A vedação de descoberto está no agregado origem.
/// </summary>
public sealed class TransferirEntreContasHandler(
    IContaFinanceiraRepository contas,
    IUnitOfWork unitOfWork) : ICommandHandler<TransferirEntreContasCommand>
{
    /// <inheritdoc />
    public async Task Handle(TransferirEntreContasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var origem = await contas.ObterPorIdAsync(new ContaFinanceiraId(request.ContaOrigemId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Conta origem {request.ContaOrigemId} nao encontrada.");
        var destino = await contas.ObterPorIdAsync(new ContaFinanceiraId(request.ContaDestinoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Conta destino {request.ContaDestinoId} nao encontrada.");

        var valor = ValorMonetario.De(request.Valor);
        origem.DebitarTransferencia(request.Data, valor, request.Historico, destino.Id);
        destino.CreditarTransferencia(request.Data, valor, request.Historico, origem.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Concilia manualmente um movimento contra o extrato bancário (casamento manual).</summary>
/// <param name="ContaId">Conta dona do movimento.</param>
/// <param name="MovimentoId">Movimento a conciliar.</param>
/// <param name="DataConciliacao">Data do casamento.</param>
public sealed record ConciliarMovimentoCommand(
    Guid ContaId,
    Guid MovimentoId,
    DateOnly DataConciliacao) : ICommand;

/// <summary>Handler da conciliação manual.</summary>
public sealed class ConciliarMovimentoHandler(
    IContaFinanceiraRepository contas,
    IUnitOfWork unitOfWork) : ICommandHandler<ConciliarMovimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConciliarMovimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var conta = await contas.ObterPorIdAsync(new ContaFinanceiraId(request.ContaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Conta {request.ContaId} nao encontrada.");

        var movimento = conta.Movimentos.FirstOrDefault(m => m.Id.Value == request.MovimentoId)
            ?? throw new InvalidOperationException($"Movimento {request.MovimentoId} nao encontrado na conta.");

        movimento.Conciliar(request.DataConciliacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

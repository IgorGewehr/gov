using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Tesouraria;

/// <summary>Registra um recebimento (entrada) numa conta da tesouraria.</summary>
/// <param name="ContaId">Conta movimentada.</param>
/// <param name="Data">Data do movimento.</param>
/// <param name="Valor">Valor (positivo).</param>
/// <param name="Historico">Descrição/origem.</param>
/// <param name="Documento">Documento de referência (opcional).</param>
public sealed record RegistrarRecebimentoCommand(
    Guid ContaId,
    DateOnly Data,
    decimal Valor,
    string Historico,
    string? Documento) : ICommand<Guid>;

/// <summary>Registra um pagamento (saída) numa conta da tesouraria.</summary>
/// <param name="ContaId">Conta movimentada.</param>
/// <param name="Data">Data do movimento.</param>
/// <param name="Valor">Valor (positivo).</param>
/// <param name="Historico">Descrição/credor.</param>
/// <param name="Documento">Documento de referência (opcional).</param>
public sealed record RegistrarPagamentoCaixaCommand(
    Guid ContaId,
    DateOnly Data,
    decimal Valor,
    string Historico,
    string? Documento) : ICommand<Guid>;

/// <summary>Validação comum de movimento simples.</summary>
public sealed class RegistrarRecebimentoValidator : AbstractValidator<RegistrarRecebimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarRecebimentoValidator()
    {
        RuleFor(c => c.ContaId).NotEmpty();
        RuleFor(c => c.Valor).GreaterThan(0m);
        RuleFor(c => c.Historico).NotEmpty().MaximumLength(300);
    }
}

/// <summary>Validação do pagamento de caixa.</summary>
public sealed class RegistrarPagamentoCaixaValidator : AbstractValidator<RegistrarPagamentoCaixaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarPagamentoCaixaValidator()
    {
        RuleFor(c => c.ContaId).NotEmpty();
        RuleFor(c => c.Valor).GreaterThan(0m);
        RuleFor(c => c.Historico).NotEmpty().MaximumLength(300);
    }
}

/// <summary>Handler do recebimento.</summary>
public sealed class RegistrarRecebimentoHandler(
    IContaFinanceiraRepository contas,
    IUnitOfWork unitOfWork) : ICommandHandler<RegistrarRecebimentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarRecebimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var conta = await contas.ObterPorIdAsync(new ContaFinanceiraId(request.ContaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Conta {request.ContaId} nao encontrada.");

        var movimento = conta.RegistrarRecebimento(request.Data, ValorMonetario.De(request.Valor), request.Historico, request.Documento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return movimento.Id.Value;
    }
}

/// <summary>Handler do pagamento de caixa.</summary>
public sealed class RegistrarPagamentoCaixaHandler(
    IContaFinanceiraRepository contas,
    IUnitOfWork unitOfWork) : ICommandHandler<RegistrarPagamentoCaixaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarPagamentoCaixaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var conta = await contas.ObterPorIdAsync(new ContaFinanceiraId(request.ContaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Conta {request.ContaId} nao encontrada.");

        var movimento = conta.RegistrarPagamento(request.Data, ValorMonetario.De(request.Valor), request.Historico, request.Documento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return movimento.Id.Value;
    }
}

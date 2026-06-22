using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Pagamentos;

/// <summary>Item de uma ordem de pagamento (liquidação a quitar).</summary>
/// <param name="LiquidacaoId">Liquidação quitada.</param>
/// <param name="Valor">Valor do item.</param>
public sealed record ItemOrdemPagamento(Guid LiquidacaoId, decimal Valor);

/// <summary>Emite uma ordem de pagamento (3º estágio — em montagem).</summary>
/// <param name="Numero">Número da ordem.</param>
/// <param name="DataPagamento">Data do pagamento.</param>
/// <param name="Banco">Código do banco.</param>
/// <param name="Agencia">Agência.</param>
/// <param name="Conta">Conta.</param>
/// <param name="Pix">Chave PIX (opcional).</param>
/// <param name="Itens">Liquidações a quitar.</param>
public sealed record EmitirOrdemDePagamentoCommand(
    string Numero,
    DateOnly DataPagamento,
    string Banco,
    string Agencia,
    string Conta,
    string? Pix,
    IReadOnlyList<ItemOrdemPagamento> Itens) : ICommand<Guid>;

/// <summary>Regras de validação da emissão de ordem de pagamento.</summary>
public sealed class EmitirOrdemDePagamentoValidator : AbstractValidator<EmitirOrdemDePagamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirOrdemDePagamentoValidator()
    {
        RuleFor(c => c.Numero).NotEmpty().MaximumLength(30);
        RuleFor(c => c.Banco).NotEmpty().MaximumLength(10);
        RuleFor(c => c.Agencia).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Conta).NotEmpty().MaximumLength(30);
        RuleFor(c => c.Itens).NotEmpty();
        RuleForEach(c => c.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.LiquidacaoId).NotEmpty();
            item.RuleFor(i => i.Valor).GreaterThan(0m);
        });
    }
}

/// <summary>Handler da emissão de ordem de pagamento (cria em montagem, valida saldos por item).</summary>
public sealed class EmitirOrdemDePagamentoHandler(
    IOrdemDePagamentoRepository ordens,
    ILiquidacaoRepository liquidacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<EmitirOrdemDePagamentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(EmitirOrdemDePagamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conta = ContaBancaria.De(request.Banco, request.Agencia, request.Conta, request.Pix);
        var ordem = OrdemDePagamento.Emitir(tenant.TenantId, request.Numero, request.DataPagamento, conta);

        foreach (var item in request.Itens)
        {
            var liquidacao = await liquidacoes.ObterPorIdAsync(new LiquidacaoId(item.LiquidacaoId), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Liquidacao {item.LiquidacaoId} nao encontrada.");

            var valor = ValorMonetario.De(item.Valor);
            if (valor.EhMaiorQue(liquidacao.SaldoAPagar))
            {
                throw new InvalidOperationException($"Valor do item excede o saldo a pagar da liquidacao {item.LiquidacaoId}.");
            }

            ordem.AdicionarItem(liquidacao.Id, valor);
        }

        ordens.Adicionar(ordem);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ordem.Id.Value;
    }
}

using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Tesouraria;

/// <summary>Abre uma conta da tesouraria (bancária ou caixa) com saldo inicial.</summary>
/// <param name="Nome">Nome/identificação da conta.</param>
/// <param name="Tipo">Espécie (1=Bancaria, 2=Caixa).</param>
/// <param name="Banco">Código do banco (obrigatório se bancária).</param>
/// <param name="Agencia">Agência (obrigatório se bancária).</param>
/// <param name="Conta">Conta (obrigatório se bancária).</param>
/// <param name="Pix">Chave PIX (opcional).</param>
/// <param name="SaldoInicial">Saldo de abertura (≥ 0).</param>
public sealed record AbrirContaFinanceiraCommand(
    string Nome,
    TipoContaFinanceira Tipo,
    string? Banco,
    string? Agencia,
    string? Conta,
    string? Pix,
    decimal SaldoInicial) : ICommand<Guid>;

/// <summary>Regras de validação da abertura de conta.</summary>
public sealed class AbrirContaFinanceiraValidator : AbstractValidator<AbrirContaFinanceiraCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirContaFinanceiraValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Tipo).IsInEnum();
        RuleFor(c => c.SaldoInicial).GreaterThanOrEqualTo(0m);
        When(c => c.Tipo == TipoContaFinanceira.Bancaria, () =>
        {
            RuleFor(c => c.Banco).NotEmpty().MaximumLength(20);
            RuleFor(c => c.Agencia).NotEmpty().MaximumLength(20);
            RuleFor(c => c.Conta).NotEmpty().MaximumLength(30);
        });
    }
}

/// <summary>Handler da abertura de conta da tesouraria.</summary>
public sealed class AbrirContaFinanceiraHandler(
    IContaFinanceiraRepository contas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<AbrirContaFinanceiraCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirContaFinanceiraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dadosBancarios = request.Tipo == TipoContaFinanceira.Bancaria
            ? ContaBancaria.De(request.Banco!, request.Agencia!, request.Conta!, request.Pix)
            : null;

        var conta = ContaFinanceira.Abrir(
            tenant.TenantId,
            request.Nome,
            request.Tipo,
            dadosBancarios,
            ValorMonetario.De(request.SaldoInicial));

        contas.Adicionar(conta);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return conta.Id.Value;
    }
}

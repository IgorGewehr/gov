using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Saude.Application.Fiscal;

/// <summary>S-2: cria a unidade gestora do Fundo Municipal de Saúde (FMS).</summary>
/// <param name="Nome">Nome da unidade gestora.</param>
/// <param name="Cnpj">CNPJ da unidade gestora.</param>
public sealed record AbrirFundoMunicipalSaudeCommand(string Nome, string Cnpj) : ICommand<Guid>;

/// <summary>Validação da abertura do FMS.</summary>
public sealed class AbrirFundoMunicipalSaudeValidator : AbstractValidator<AbrirFundoMunicipalSaudeCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirFundoMunicipalSaudeValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().WithMessage("Nome da unidade gestora e obrigatorio.");
        RuleFor(c => c.Cnpj).NotEmpty().WithMessage("CNPJ da unidade gestora e obrigatorio.");
    }
}

/// <summary>Handler da abertura do FMS.</summary>
public sealed class AbrirFundoMunicipalSaudeHandler(
    IFundoMunicipalSaudeRepository fundos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirFundoMunicipalSaudeCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirFundoMunicipalSaudeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fundo = FundoMunicipalSaude.Criar(tenant.TenantId, request.Nome, request.Cnpj);
        await fundos.AdicionarAsync(fundo, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return fundo.Id.Value;
    }
}

/// <summary>S-2: registra o recebimento de uma parcela do FNS num bloco/fonte do FMS.</summary>
/// <param name="FundoId">Identificador do fundo.</param>
/// <param name="Bloco">Bloco de financiamento (Custeio/Investimento).</param>
/// <param name="FonteRecurso">Fonte/destinação de recurso (PCASP) vinculada.</param>
/// <param name="Valor">Valor recebido (&gt; 0).</param>
public sealed record ReceberParcelaFnsCommand(
    Guid FundoId,
    BlocoFinanciamentoSaude Bloco,
    string FonteRecurso,
    decimal Valor) : ICommand;

/// <summary>Validação do recebimento de parcela.</summary>
public sealed class ReceberParcelaFnsValidator : AbstractValidator<ReceberParcelaFnsCommand>
{
    /// <summary>Define as regras.</summary>
    public ReceberParcelaFnsValidator()
    {
        RuleFor(c => c.FundoId).NotEmpty();
        RuleFor(c => c.Bloco).IsInEnum();
        RuleFor(c => c.FonteRecurso).NotEmpty().WithMessage("Fonte de recurso e obrigatoria (segregacao por bloco).");
        RuleFor(c => c.Valor).GreaterThan(0m).WithMessage("Valor da parcela deve ser positivo.");
    }
}

/// <summary>Handler do recebimento de parcela.</summary>
public sealed class ReceberParcelaFnsHandler(
    IFundoMunicipalSaudeRepository fundos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReceberParcelaFnsCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReceberParcelaFnsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fundo = await fundos.ObterPorIdAsync(new FundoMunicipalSaudeId(request.FundoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fundo Municipal de Saude inexistente no tenant.");

        fundo.ReceberParcela(request.Bloco, request.FonteRecurso, request.Valor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>S-2: registra a execução de despesa num bloco/fonte do FMS (respeita o saldo segregado).</summary>
/// <param name="FundoId">Identificador do fundo.</param>
/// <param name="Bloco">Bloco de financiamento.</param>
/// <param name="FonteRecurso">Fonte/destinação de recurso (PCASP) vinculada.</param>
/// <param name="Valor">Valor executado (&gt; 0).</param>
public sealed record ExecutarDespesaBlocoCommand(
    Guid FundoId,
    BlocoFinanciamentoSaude Bloco,
    string FonteRecurso,
    decimal Valor) : ICommand;

/// <summary>Validação da execução de despesa.</summary>
public sealed class ExecutarDespesaBlocoValidator : AbstractValidator<ExecutarDespesaBlocoCommand>
{
    /// <summary>Define as regras.</summary>
    public ExecutarDespesaBlocoValidator()
    {
        RuleFor(c => c.FundoId).NotEmpty();
        RuleFor(c => c.Bloco).IsInEnum();
        RuleFor(c => c.FonteRecurso).NotEmpty();
        RuleFor(c => c.Valor).GreaterThan(0m).WithMessage("Valor executado deve ser positivo.");
    }
}

/// <summary>Handler da execução de despesa.</summary>
public sealed class ExecutarDespesaBlocoHandler(
    IFundoMunicipalSaudeRepository fundos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ExecutarDespesaBlocoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ExecutarDespesaBlocoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fundo = await fundos.ObterPorIdAsync(new FundoMunicipalSaudeId(request.FundoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fundo Municipal de Saude inexistente no tenant.");

        // O invariante de transposição vedada entre blocos vive no agregado (lança se exceder o saldo).
        fundo.ExecutarDespesa(request.Bloco, request.FonteRecurso, request.Valor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>Suspende uma consignacao averbada (deixa de lancar na folha; libera margem).</summary>
/// <param name="ContratoConsignacaoId">Contrato a suspender.</param>
/// <param name="Motivo">Motivo da suspensao (obrigatorio).</param>
public sealed record SuspenderConsignacaoCommand(Guid ContratoConsignacaoId, string Motivo) : ICommand;

/// <summary>Reativa uma consignacao suspensa (re-checa a margem na competencia informada).</summary>
/// <param name="ContratoConsignacaoId">Contrato a reativar.</param>
/// <param name="DataReferencia">Data (define a competencia) para re-checar a margem.</param>
public sealed record ReativarConsignacaoCommand(Guid ContratoConsignacaoId, DateOnly DataReferencia) : ICommand;

/// <summary>Cancela uma consignacao antes da quitacao (libera margem).</summary>
/// <param name="ContratoConsignacaoId">Contrato a cancelar.</param>
/// <param name="Motivo">Motivo do cancelamento (obrigatorio).</param>
public sealed record CancelarConsignacaoCommand(Guid ContratoConsignacaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da suspensao de consignacao.</summary>
public sealed class SuspenderConsignacaoValidator : AbstractValidator<SuspenderConsignacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public SuspenderConsignacaoValidator()
    {
        RuleFor(c => c.ContratoConsignacaoId).NotEmpty().WithMessage("Contrato e obrigatorio.");
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(300).WithMessage("Motivo e obrigatorio (max. 300).");
    }
}

/// <summary>Regras de validacao da reativacao de consignacao.</summary>
public sealed class ReativarConsignacaoValidator : AbstractValidator<ReativarConsignacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ReativarConsignacaoValidator()
        => RuleFor(c => c.ContratoConsignacaoId).NotEmpty().WithMessage("Contrato e obrigatorio.");
}

/// <summary>Regras de validacao do cancelamento de consignacao.</summary>
public sealed class CancelarConsignacaoValidator : AbstractValidator<CancelarConsignacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarConsignacaoValidator()
    {
        RuleFor(c => c.ContratoConsignacaoId).NotEmpty().WithMessage("Contrato e obrigatorio.");
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(300).WithMessage("Motivo e obrigatorio (max. 300).");
    }
}

/// <summary>Handler da suspensao de consignacao.</summary>
public sealed class SuspenderConsignacaoHandler(
    IContratoConsignacaoRepository contratos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SuspenderConsignacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(SuspenderConsignacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var contrato = await contratos.ObterPorIdAsync(new ContratoConsignacaoId(request.ContratoConsignacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato de consignacao nao encontrado.");
        contrato.Suspender(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da reativacao de consignacao (re-checa a margem, ignorando o proprio contrato no comprometido).</summary>
public sealed class ReativarConsignacaoHandler(
    IContratoConsignacaoRepository contratos,
    CalculadoraMargemConsignavel calculadoraMargem,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReativarConsignacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReativarConsignacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var contrato = await contratos.ObterPorIdAsync(new ContratoConsignacaoId(request.ContratoConsignacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato de consignacao nao encontrado.");

        // Suspensa nao consome margem; recalcular ignorando o proprio contrato evita contar a propria parcela.
        var competencia = Competencia.De(request.DataReferencia.Year, request.DataReferencia.Month);
        var margem = await calculadoraMargem
            .CalcularAsync(contrato.ServidorId.Value, competencia, cancellationToken, contrato.Id)
            .ConfigureAwait(false);

        contrato.Reativar(margem);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do cancelamento de consignacao.</summary>
public sealed class CancelarConsignacaoHandler(
    IContratoConsignacaoRepository contratos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarConsignacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarConsignacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var contrato = await contratos.ObterPorIdAsync(new ContratoConsignacaoId(request.ContratoConsignacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato de consignacao nao encontrado.");
        contrato.Cancelar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

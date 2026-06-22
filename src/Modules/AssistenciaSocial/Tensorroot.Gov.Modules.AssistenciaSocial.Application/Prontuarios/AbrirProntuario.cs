using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;

/// <summary>Abre um prontuario SUAS ao iniciar o acompanhamento de uma familia (I-1, I-2).</summary>
/// <param name="FamiliaId">Familia a acompanhar.</param>
/// <param name="UnidadeAtendimentoId">CRAS/CREAS responsavel pelo acompanhamento.</param>
public sealed record AbrirProntuarioCommand(
    Guid FamiliaId,
    Guid UnidadeAtendimentoId) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de prontuario.</summary>
public sealed class AbrirProntuarioValidator : AbstractValidator<AbrirProntuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirProntuarioValidator()
    {
        RuleFor(comando => comando.FamiliaId).NotEmpty().WithMessage("Familia e obrigatoria.");
        RuleFor(comando => comando.UnidadeAtendimentoId).NotEmpty().WithMessage("Unidade de atendimento e obrigatoria.");
    }
}

/// <summary>Handler da abertura de prontuario.</summary>
public sealed class AbrirProntuarioHandler(
    IFamiliaRepository familias,
    IUnidadeAtendimentoRepository unidades,
    IProntuarioSuasRepository prontuarios,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AbrirProntuarioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirProntuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _ = await familias.ObterPorIdAsync(new FamiliaId(request.FamiliaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Familia nao encontrada.");

        _ = await unidades.ObterPorIdAsync(request.UnidadeAtendimentoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade de atendimento nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var prontuario = ProntuarioSuas.Abrir(tenant.TenantId, request.FamiliaId, request.UnidadeAtendimentoId, hoje);

        prontuarios.Adicionar(prontuario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return prontuario.Id.Value;
    }
}

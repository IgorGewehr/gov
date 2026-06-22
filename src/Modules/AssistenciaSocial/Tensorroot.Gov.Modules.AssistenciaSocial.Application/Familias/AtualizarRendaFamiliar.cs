using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;

/// <summary>
/// Atualiza a composicao/renda de uma familia: recalcula a renda per capita, renova o marco
/// cadastral (I-7) e, se vencida, regulariza (I-12).
/// </summary>
/// <param name="FamiliaId">Identificador da familia.</param>
/// <param name="Membros">Nova composicao familiar (rendas + parentescos).</param>
public sealed record AtualizarRendaFamiliarCommand(
    Guid FamiliaId,
    IReadOnlyList<MembroFamiliarDto> Membros) : ICommand;

/// <summary>Regras de validacao da atualizacao de renda familiar.</summary>
public sealed class AtualizarRendaFamiliarValidator : AbstractValidator<AtualizarRendaFamiliarCommand>
{
    /// <summary>Define as regras.</summary>
    public AtualizarRendaFamiliarValidator()
    {
        RuleFor(comando => comando.FamiliaId)
            .NotEmpty()
            .WithMessage("Identificador da familia e obrigatorio.");

        RuleFor(comando => comando.Membros)
            .NotEmpty()
            .WithMessage("A familia deve ter ao menos um membro.");
    }
}

/// <summary>Handler da atualizacao de renda familiar.</summary>
public sealed class AtualizarRendaFamiliarHandler(
    IFamiliaRepository familias,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AtualizarRendaFamiliarCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtualizarRendaFamiliarCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = tenant.TenantId;

        var familia = await familias.ObterPorIdAsync(new FamiliaId(request.FamiliaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Familia nao encontrada.");

        var membros = request.Membros
            .Select(membro => MembroFamiliar.Registrar(
                Cpf.Create(membro.Cpf),
                (Parentesco)membro.Parentesco,
                membro.DataNascimento,
                ValorMonetario.De(membro.RendaIndividual),
                membro.EhPcd))
            .ToList();

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        familia.AtualizarRendaFamiliar(membros, hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

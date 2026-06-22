using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Designa (vincula) um motorista habilitado a um veículo da frota.</summary>
/// <param name="VeiculoId">Veículo.</param>
/// <param name="Nome">Nome do motorista.</param>
/// <param name="Cnh">Número da CNH.</param>
/// <param name="CategoriaCnh">Categoria da CNH.</param>
/// <param name="ValidadeCnh">Validade da CNH.</param>
public sealed record DesignarMotoristaCommand(
    Guid VeiculoId,
    string Nome,
    string Cnh,
    string CategoriaCnh,
    DateOnly ValidadeCnh) : ICommand;

/// <summary>Regras de validação da designação de motorista.</summary>
public sealed class DesignarMotoristaValidator : AbstractValidator<DesignarMotoristaCommand>
{
    /// <summary>Define as regras.</summary>
    /// <param name="timeProvider">Relógio para checagem de validade da CNH (I-9).</param>
    public DesignarMotoristaValidator(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        RuleFor(comando => comando.VeiculoId).NotEmpty();
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(150);
        RuleFor(comando => comando.Cnh).NotEmpty();
        RuleFor(comando => comando.ValidadeCnh)
            .Must(validade => validade >= hoje)
            .WithMessage("CNH vencida não pode ser designada.");
    }
}

/// <summary>Handler da designação de motorista.</summary>
public sealed class DesignarMotoristaHandler(
    IVeiculoRepository veiculos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<DesignarMotoristaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DesignarMotoristaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(request.VeiculoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Veículo não encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        veiculo.DesignarMotorista(
            request.Nome,
            request.Cnh,
            request.CategoriaCnh,
            request.ValidadeCnh,
            hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

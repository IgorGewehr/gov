using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Registra um apostilamento que dispensa termo aditivo (Lei 14.133/2021, art. 136; I-13).</summary>
/// <param name="ContratoId">Contrato a apostilar.</param>
/// <param name="Tipo">Tipo do apostilamento.</param>
/// <param name="Descricao">Descricao da alteracao apostilada.</param>
public sealed record ApostilarContratoCommand(Guid ContratoId, TipoApostilamento Tipo, string Descricao) : ICommand<Guid>;

/// <summary>Regras de validacao do apostilamento.</summary>
public sealed class ApostilarContratoValidator : AbstractValidator<ApostilarContratoCommand>
{
    /// <summary>Define as regras.</summary>
    public ApostilarContratoValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty();
        RuleFor(comando => comando.Tipo).IsInEnum();
        RuleFor(comando => comando.Descricao).NotEmpty();
    }
}

/// <summary>Handler do apostilamento.</summary>
public sealed class ApostilarContratoHandler(IContratoRepository contratos, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<ApostilarContratoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ApostilarContratoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato nao encontrado.");

        var apostilamento = contrato.Apostilar(
            request.Tipo,
            request.Descricao,
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return apostilamento.Id.Value;
    }
}

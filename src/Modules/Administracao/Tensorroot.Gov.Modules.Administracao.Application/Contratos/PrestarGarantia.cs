using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Presta uma garantia de execucao do contrato (limite 5%/10% — art. 96/98; I-12).</summary>
/// <param name="ContratoId">Contrato a garantir.</param>
/// <param name="Modalidade">Modalidade da garantia.</param>
/// <param name="Percentual">Percentual sobre o valor (ate 5%/10%).</param>
/// <param name="Valor">Valor prestado.</param>
/// <param name="ValidadeFim">Data-fim de validade.</param>
/// <param name="EhGrandeVulto">Indica obra de grande vulto (limite ampliado a 10%).</param>
public sealed record PrestarGarantiaCommand(
    Guid ContratoId,
    ModalidadeGarantia Modalidade,
    decimal Percentual,
    decimal Valor,
    DateOnly ValidadeFim,
    bool EhGrandeVulto = false) : ICommand<Guid>;

/// <summary>Regras de validacao da prestacao de garantia.</summary>
public sealed class PrestarGarantiaValidator : AbstractValidator<PrestarGarantiaCommand>
{
    /// <summary>Define as regras.</summary>
    public PrestarGarantiaValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty();
        RuleFor(comando => comando.Modalidade).IsInEnum();
        RuleFor(comando => comando.Percentual).InclusiveBetween(0, 10);
        RuleFor(comando => comando.Valor).GreaterThan(0);
    }
}

/// <summary>Handler da prestacao de garantia.</summary>
public sealed class PrestarGarantiaHandler(IContratoRepository contratos, IUnitOfWork unitOfWork)
    : ICommandHandler<PrestarGarantiaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(PrestarGarantiaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato nao encontrado.");

        var garantia = contrato.PrestarGarantia(
            request.Modalidade,
            request.Percentual,
            ValorMonetario.De(request.Valor),
            request.ValidadeFim,
            request.EhGrandeVulto);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return garantia.Id.Value;
    }
}

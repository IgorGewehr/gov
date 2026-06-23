using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>
/// Parametriza uma rubrica consignavel (categoria/prioridade, balde de margem e se conta para a base).
/// </summary>
/// <param name="Codigo">Codigo da rubrica (referencia S-1010).</param>
/// <param name="Descricao">Descricao legivel.</param>
/// <param name="Categoria">Categoria (prioridade no corte por margem).</param>
/// <param name="GrupoMargem">Balde de margem consumido.</param>
/// <param name="ContaParaMargem">Se a propria rubrica compoe a base da margem (proventos permanentes).</param>
public sealed record DefinirRubricaConsignavelCommand(
    string Codigo,
    string Descricao,
    CategoriaConsignavel Categoria,
    GrupoMargem GrupoMargem,
    bool ContaParaMargem) : ICommand<Guid>;

/// <summary>Regras de validacao da parametrizacao de rubrica consignavel.</summary>
public sealed class DefinirRubricaConsignavelValidator : AbstractValidator<DefinirRubricaConsignavelCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirRubricaConsignavelValidator()
    {
        RuleFor(c => c.Codigo).NotEmpty().MaximumLength(30).WithMessage("Codigo e obrigatorio (max. 30).");
        RuleFor(c => c.Descricao).NotEmpty().MaximumLength(200).WithMessage("Descricao e obrigatoria (max. 200).");
        RuleFor(c => c.Categoria).IsInEnum().WithMessage("Categoria invalida.");
        RuleFor(c => c.GrupoMargem).IsInEnum().WithMessage("Grupo de margem invalido.");
    }
}

/// <summary>Handler da parametrizacao de rubrica consignavel.</summary>
public sealed class DefinirRubricaConsignavelHandler(
    IRubricaConsignavelRepository rubricas,
    ITenantContext tenant,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DefinirRubricaConsignavelCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(DefinirRubricaConsignavelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await rubricas.ExistePorCodigoAsync(request.Codigo, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe rubrica consignavel com este codigo no tenant.");
        }

        var rubrica = RubricaConsignavel.Definir(
            tenant.TenantId,
            request.Codigo,
            request.Descricao,
            request.Categoria,
            request.GrupoMargem,
            request.ContaParaMargem);
        rubricas.Adicionar(rubrica);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rubrica.Id.Value;
    }
}

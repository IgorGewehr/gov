using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using DominioPlano = Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.PlanoCarreira;

/// <summary>
/// Institui um plano de cargos, carreiras e salarios (PCCS): a matriz CLASSE x REFERENCIA e suas regras
/// de movimentacao (percentuais de step/classe, interstncio e nota minima). O vencimento de cada celula
/// e' derivado deterministicamente do vencimento-base — sem numero magico (CLAUDE.md §7).
/// </summary>
/// <param name="DenominacaoCarreira">Denominacao da carreira regida.</param>
/// <param name="LeiInstituicao">Lei municipal que institui o plano.</param>
/// <param name="VencimentoBase">Vencimento da celula de ingresso (classe 1, referencia 1).</param>
/// <param name="NumeroClasses">Numero de classes (faixas verticais).</param>
/// <param name="NumeroReferencias">Numero de referencias (steps horizontais) por classe.</param>
/// <param name="PercentualEntreReferencias">Percentual entre referencias consecutivas (em [0, 100)).</param>
/// <param name="PercentualEntreClasses">Percentual entre classes consecutivas (em [0, 100)).</param>
/// <param name="IntersticioMeses">Interstncio (meses) exigido para a progressao horizontal.</param>
/// <param name="NotaMinimaProgressao">Nota minima de avaliacao para progressao por merecimento (em [0, 100]).</param>
public sealed record InstituirPlanoCarreiraCommand(
    string DenominacaoCarreira,
    string LeiInstituicao,
    decimal VencimentoBase,
    int NumeroClasses,
    int NumeroReferencias,
    decimal PercentualEntreReferencias,
    decimal PercentualEntreClasses,
    int IntersticioMeses,
    decimal NotaMinimaProgressao) : ICommand<Guid>;

/// <summary>Regras de validacao da instituicao de plano de carreira.</summary>
public sealed class InstituirPlanoCarreiraValidator : AbstractValidator<InstituirPlanoCarreiraCommand>
{
    /// <summary>Define as regras.</summary>
    public InstituirPlanoCarreiraValidator()
    {
        RuleFor(comando => comando.DenominacaoCarreira)
            .NotEmpty()
            .MaximumLength(DominioPlano.ComprimentoMaximoDenominacao);

        RuleFor(comando => comando.LeiInstituicao).NotEmpty();
        RuleFor(comando => comando.VencimentoBase).GreaterThan(0m);
        RuleFor(comando => comando.NumeroClasses).GreaterThanOrEqualTo(DominioPlano.DimensaoMinima);
        RuleFor(comando => comando.NumeroReferencias).GreaterThanOrEqualTo(DominioPlano.DimensaoMinima);
        RuleFor(comando => comando.PercentualEntreReferencias).InclusiveBetween(0m, 99.9999m);
        RuleFor(comando => comando.PercentualEntreClasses).InclusiveBetween(0m, 99.9999m);
        RuleFor(comando => comando.IntersticioMeses).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.NotaMinimaProgressao).InclusiveBetween(0m, 100m);
    }
}

/// <summary>Handler da instituicao de plano de carreira.</summary>
public sealed class InstituirPlanoCarreiraHandler(
    IPlanoCarreiraRepository planos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<InstituirPlanoCarreiraCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(InstituirPlanoCarreiraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plano = DominioPlano.Instituir(
            tenant.TenantId,
            request.DenominacaoCarreira,
            request.LeiInstituicao,
            Vencimento.De(request.VencimentoBase),
            request.NumeroClasses,
            request.NumeroReferencias,
            request.PercentualEntreReferencias,
            request.PercentualEntreClasses,
            request.IntersticioMeses,
            request.NotaMinimaProgressao);

        planos.Adicionar(plano);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plano.Id.Value;
    }
}

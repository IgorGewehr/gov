using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Pasep;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Pasep;

/// <summary>
/// Apura o PASEP de uma competencia: a BASE e' a folha bruta (soma do total de proventos de TODAS as
/// folhas da competencia — mensal + ciclo anual), e o VALOR = base x aliquota. A aliquota vem dos
/// <see cref="Configuracao.ParametrosFolha"/> do tenant (1% padrao, parametrizavel), salvo sobrescrita
/// pontual no comando. Unica por <c>(TenantId, Competencia)</c>. Transmissao/recolhimento real = M10.
/// </summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
/// <param name="AliquotaOverride">Aliquota (percentual) sobrescrita; nula usa a parametrizada do tenant.</param>
public sealed record ApurarPasepCommand(int Ano, int Mes, decimal? AliquotaOverride) : ICommand<Guid>;

/// <summary>Regras de validacao da apuracao do PASEP.</summary>
public sealed class ApurarPasepValidator : AbstractValidator<ApurarPasepCommand>
{
    /// <summary>Define as regras.</summary>
    public ApurarPasepValidator()
    {
        RuleFor(comando => comando.Ano).InclusiveBetween(2000, 2100).WithMessage("Ano invalido.");
        RuleFor(comando => comando.Mes).InclusiveBetween(1, 12).WithMessage("Mes invalido.");
        RuleFor(comando => comando.AliquotaOverride!.Value)
            .GreaterThan(0m)
            .LessThanOrEqualTo(100m)
            .When(comando => comando.AliquotaOverride is not null)
            .WithMessage("Aliquota do PASEP deve estar em (0, 100].");
    }
}

/// <summary>Handler da apuracao do PASEP.</summary>
public sealed class ApurarPasepHandler(
    IApuracaoPasepRepository apuracoes,
    IFolhaDePagamentoRepository folhas,
    IParametrosFolhaProvider parametros,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ApurarPasepCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ApurarPasepCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var competencia = Competencia.De(request.Ano, request.Mes);

        // Unicidade por (tenant, competencia): re-apuracao exige excluir/recalcular (fora deste escopo).
        if (await apuracoes.ExisteParaCompetenciaAsync(competencia, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe apuracao de PASEP para esta competencia.");
        }

        // BASE = folha bruta da competencia (soma dos proventos de todas as folhas: mensal + ciclo anual).
        var folhasDaCompetencia = await folhas.ListarPorCompetenciaAsync(competencia, cancellationToken).ConfigureAwait(false);
        var baseContribuicao = folhasDaCompetencia.Sum(folha => folha.TotalProventos);

        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);
        var aliquota = request.AliquotaOverride ?? config.AliquotaPasep;

        var apuracao = ApuracaoPasep.Apurar(tenant.TenantId, competencia, baseContribuicao, aliquota);

        apuracoes.Adicionar(apuracao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return apuracao.Id.Value;
    }
}

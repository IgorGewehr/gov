using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;

namespace Tensorroot.Gov.Modules.Tributos.Application.Itbi;

/// <summary>
/// Configura a alíquota do ITBI de um exercício (LEI MUNICIPAL): alíquota geral e alíquota da parcela
/// financiada pelo SFH; e a publica. Nenhuma alíquota é hardcoded. Ver M6-DESIGN §3.1.
/// </summary>
/// <param name="Exercicio">Exercício fiscal.</param>
/// <param name="AliquotaGeralPercentual">Alíquota geral em %.</param>
/// <param name="AliquotaSfhFinanciadaPercentual">Alíquota da parcela financiada pelo SFH em % (use a geral se não houver redução).</param>
/// <param name="FundamentoLegal">Lei municipal do ITBI.</param>
/// <param name="Publicar">Se verdadeiro, publica após configurar.</param>
public sealed record ConfigurarAliquotaItbiCommand(
    int Exercicio,
    decimal AliquotaGeralPercentual,
    decimal AliquotaSfhFinanciadaPercentual,
    string FundamentoLegal,
    bool Publicar = true) : ICommand<Guid>;

/// <summary>Regras de validação da configuração da alíquota do ITBI.</summary>
public sealed class ConfigurarAliquotaItbiValidator : AbstractValidator<ConfigurarAliquotaItbiCommand>
{
    /// <summary>Define as regras.</summary>
    public ConfigurarAliquotaItbiValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.AliquotaGeralPercentual).InclusiveBetween(0m, 100m);
        RuleFor(c => c.AliquotaSfhFinanciadaPercentual).InclusiveBetween(0m, 100m);
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
    }
}

/// <summary>Handler da configuração da alíquota do ITBI.</summary>
public sealed class ConfigurarAliquotaItbiHandler(
    IAliquotaItbiRepository aliquotas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConfigurarAliquotaItbiCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConfigurarAliquotaItbiCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aliquota = AliquotaItbi.Criar(
            tenant.TenantId,
            request.Exercicio,
            request.AliquotaGeralPercentual,
            request.AliquotaSfhFinanciadaPercentual,
            request.FundamentoLegal);

        if (request.Publicar)
        {
            aliquota.Publicar();
        }

        aliquotas.Adicionar(aliquota);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return aliquota.Id.Value;
    }
}

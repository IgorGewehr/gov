using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

namespace Tensorroot.Gov.Modules.Tributos.Application.Pgv;

/// <summary>Faixa de progressividade de alíquota do IPTU.</summary>
/// <param name="ValorVenalMinimo">Limite inferior do valor venal (inclusivo).</param>
/// <param name="ValorVenalMaximo">Limite superior (exclusivo); use <see cref="TabelaAliquotaIptu.SemTeto"/> para "sem teto".</param>
/// <param name="AliquotaPercentual">Alíquota em % (ex.: 1.0 = 1%).</param>
public sealed record FaixaAliquotaInput(decimal ValorVenalMinimo, decimal ValorVenalMaximo, decimal AliquotaPercentual);

/// <summary>
/// Configura a tabela de alíquotas do IPTU de um exercício (LEI MUNICIPAL): faixas de
/// progressividade por valor venal, predial × territorial; e a publica. Nenhuma alíquota é
/// hardcoded — todas vêm do comando. Ver M6-DESIGN §1.3.
/// </summary>
/// <param name="Exercicio">Exercício fiscal (ano).</param>
/// <param name="Edificado">Tabela predial (true) ou territorial (false).</param>
/// <param name="FundamentoLegal">Lei municipal de alíquotas.</param>
/// <param name="Faixas">Faixas de progressividade (contínuas a partir de zero).</param>
/// <param name="Publicar">Se verdadeiro, publica a tabela após a configuração.</param>
public sealed record ConfigurarTabelaAliquotaIptuCommand(
    int Exercicio,
    bool Edificado,
    string FundamentoLegal,
    IReadOnlyList<FaixaAliquotaInput> Faixas,
    bool Publicar = true) : ICommand<Guid>;

/// <summary>Regras de validação da configuração da tabela de alíquotas.</summary>
public sealed class ConfigurarTabelaAliquotaIptuValidator : AbstractValidator<ConfigurarTabelaAliquotaIptuCommand>
{
    /// <summary>Define as regras.</summary>
    public ConfigurarTabelaAliquotaIptuValidator()
    {
        RuleFor(comando => comando.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(comando => comando.FundamentoLegal).NotEmpty().MaximumLength(300);
        RuleFor(comando => comando.Faixas).NotEmpty().WithMessage("Defina ao menos uma faixa de alíquota.");
        RuleForEach(comando => comando.Faixas).ChildRules(faixa =>
        {
            faixa.RuleFor(f => f.ValorVenalMinimo).GreaterThanOrEqualTo(0m);
            faixa.RuleFor(f => f.ValorVenalMaximo).GreaterThan(0m);
            faixa.RuleFor(f => f.AliquotaPercentual).GreaterThanOrEqualTo(0m);
        });
    }
}

/// <summary>Handler da configuração da tabela de alíquotas do IPTU.</summary>
public sealed class ConfigurarTabelaAliquotaIptuHandler(
    ITabelaAliquotaIptuRepository tabelas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConfigurarTabelaAliquotaIptuCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConfigurarTabelaAliquotaIptuCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tabela = TabelaAliquotaIptu.Criar(tenant.TenantId, request.Exercicio, request.Edificado, request.FundamentoLegal);

        foreach (var faixa in request.Faixas)
        {
            tabela.AdicionarFaixa(faixa.ValorVenalMinimo, faixa.ValorVenalMaximo, faixa.AliquotaPercentual);
        }

        if (request.Publicar)
        {
            tabela.Publicar();
        }

        tabelas.Adicionar(tabela);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tabela.Id.Value;
    }
}

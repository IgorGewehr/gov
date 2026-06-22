using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;

namespace Tensorroot.Gov.Modules.Tributos.Application.Iss;

/// <summary>Item de alíquota do ISS por item da lista LC 116 (lei municipal).</summary>
/// <param name="ItemListaServico">Item da lista LC 116 (ex.: "7.02").</param>
/// <param name="AliquotaPercentual">Alíquota em % (ex.: 2.0 = 2%).</param>
/// <param name="RetencaoObrigatoria">Retenção na fonte obrigatória por lei municipal.</param>
/// <param name="SubstituicaoTributaria">Substituição tributária por lei municipal.</param>
public sealed record ItemAliquotaIssInput(
    string ItemListaServico,
    decimal AliquotaPercentual,
    bool RetencaoObrigatoria = false,
    bool SubstituicaoTributaria = false);

/// <summary>
/// Configura a tabela de alíquotas do ISS por item da lista LC 116 (LEI MUNICIPAL) com vigência, e a
/// publica. Nenhuma alíquota é hardcoded — todas vêm do comando. Ver M6-DESIGN §2.2.
/// </summary>
/// <param name="VigenciaInicioAaaaMm">Início de vigência (AAAAMM).</param>
/// <param name="FundamentoLegal">Lei municipal de alíquotas do ISS.</param>
/// <param name="Itens">Itens de alíquota por item da lista LC 116.</param>
/// <param name="Publicar">Se verdadeiro, publica após configurar.</param>
public sealed record ConfigurarTabelaAliquotaIssCommand(
    int VigenciaInicioAaaaMm,
    string FundamentoLegal,
    IReadOnlyList<ItemAliquotaIssInput> Itens,
    bool Publicar = true) : ICommand<Guid>;

/// <summary>Regras de validação da configuração da tabela de alíquotas do ISS.</summary>
public sealed class ConfigurarTabelaAliquotaIssValidator : AbstractValidator<ConfigurarTabelaAliquotaIssCommand>
{
    /// <summary>Define as regras.</summary>
    public ConfigurarTabelaAliquotaIssValidator()
    {
        RuleFor(c => c.VigenciaInicioAaaaMm).GreaterThanOrEqualTo(190001);
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Itens).NotEmpty().WithMessage("Defina ao menos um item de alíquota.");
        RuleForEach(c => c.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ItemListaServico).NotEmpty();
            item.RuleFor(i => i.AliquotaPercentual).InclusiveBetween(0m, 100m);
        });
    }
}

/// <summary>Handler da configuração da tabela de alíquotas do ISS.</summary>
public sealed class ConfigurarTabelaAliquotaIssHandler(
    ITabelaAliquotaIssRepository tabelas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConfigurarTabelaAliquotaIssCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConfigurarTabelaAliquotaIssCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tabela = TabelaAliquotaIss.Criar(tenant.TenantId, request.VigenciaInicioAaaaMm, request.FundamentoLegal);

        foreach (var item in request.Itens)
        {
            tabela.DefinirItem(item.ItemListaServico, item.AliquotaPercentual, item.RetencaoObrigatoria, item.SubstituicaoTributaria);
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

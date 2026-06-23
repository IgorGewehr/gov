using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Taxas;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Taxas;

/// <summary>Uma faixa da tabela de taxa (modo PorFaixa) na configuração.</summary>
/// <param name="LimiteInferior">Limite inferior (inclusivo) da quantidade-base.</param>
/// <param name="LimiteSuperior">Limite superior (inclusivo); nulo = sem teto.</param>
/// <param name="Valor">Valor da taxa na faixa (R$).</param>
public sealed record FaixaTaxaInput(decimal LimiteInferior, decimal? LimiteSuperior, decimal Valor);

/// <summary>
/// Configura (cria e publica) a tabela de uma taxa municipal (LEI MUNICIPAL — CTM): espécie
/// (polícia/serviço/licença), modo de cálculo (fixo/por unidade/por faixa) e faixas. Nenhum valor é
/// hardcoded. CTN art. 80 + SV 29: a quantidade-base é um elemento físico, nunca o capital. Ver M6-DESIGN §3.2.
/// </summary>
/// <param name="Codigo">Código da taxa no CTM.</param>
/// <param name="Descricao">Descrição.</param>
/// <param name="Especie">Espécie (polícia/serviço/licença).</param>
/// <param name="ModoCalculo">Modo de cálculo.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
/// <param name="ValorBase">Valor fixo / unitário (R$).</param>
/// <param name="FundamentoLegal">Artigo do CTM.</param>
/// <param name="Faixas">Faixas (somente no modo PorFaixa).</param>
/// <param name="Publicar">Se verdadeiro, publica após configurar.</param>
public sealed record ConfigurarTabelaTaxaCommand(
    string Codigo,
    string Descricao,
    EspecieTaxa Especie,
    ModoCalculoTaxa ModoCalculo,
    int Exercicio,
    decimal ValorBase,
    string FundamentoLegal,
    IReadOnlyList<FaixaTaxaInput>? Faixas = null,
    bool Publicar = true) : ICommand<Guid>;

/// <summary>Regras de validação da configuração da tabela de taxa.</summary>
public sealed class ConfigurarTabelaTaxaValidator : AbstractValidator<ConfigurarTabelaTaxaCommand>
{
    /// <summary>Define as regras.</summary>
    public ConfigurarTabelaTaxaValidator()
    {
        RuleFor(c => c.Codigo).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Descricao).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Especie).IsInEnum();
        RuleFor(c => c.ModoCalculo).IsInEnum();
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.ValorBase).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
        // No modo PorFaixa, exige ao menos uma faixa (a publicação falharia sem faixas).
        RuleFor(c => c.Faixas)
            .NotEmpty()
            .When(c => c.ModoCalculo == ModoCalculoTaxa.PorFaixa)
            .WithMessage("O modo PorFaixa exige ao menos uma faixa.");
    }
}

/// <summary>Handler da configuração da tabela de taxa.</summary>
public sealed class ConfigurarTabelaTaxaHandler(
    ITabelaTaxaRepository tabelas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConfigurarTabelaTaxaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConfigurarTabelaTaxaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tabela = TabelaTaxa.Criar(
            tenant.TenantId,
            request.Codigo,
            request.Descricao,
            request.Especie,
            request.ModoCalculo,
            request.Exercicio,
            ValorMonetario.De(request.ValorBase),
            request.FundamentoLegal);

        if (request.Faixas is not null)
        {
            foreach (var faixa in request.Faixas)
            {
                tabela.DefinirFaixa(faixa.LimiteInferior, faixa.LimiteSuperior, ValorMonetario.De(faixa.Valor));
            }
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

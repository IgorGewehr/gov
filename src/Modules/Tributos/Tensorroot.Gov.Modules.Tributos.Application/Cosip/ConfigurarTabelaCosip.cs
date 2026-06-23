using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Cosip;

/// <summary>Uma faixa de consumo da tabela de COSIP na configuração.</summary>
/// <param name="Classe">Classe de consumidor.</param>
/// <param name="ConsumoMinimoKwh">Consumo mínimo (inclusivo) em kWh.</param>
/// <param name="ConsumoMaximoKwh">Consumo máximo (inclusivo) em kWh; nulo = sem teto.</param>
/// <param name="Valor">Valor da COSIP na faixa (R$).</param>
public sealed record FaixaCosipInput(ClasseConsumidorCosip Classe, decimal ConsumoMinimoKwh, decimal? ConsumoMaximoKwh, decimal Valor);

/// <summary>
/// Configura (cria e publica) a tabela de COSIP de um exercício (LEI MUNICIPAL — CF art. 149-A): faixas
/// de consumo (kWh) por classe de consumidor, progressivas (RE 573.675). Nenhum valor é hardcoded.
/// Ver M6-DESIGN §3.4.
/// </summary>
/// <param name="Exercicio">Exercício fiscal.</param>
/// <param name="FundamentoLegal">Lei municipal de COSIP.</param>
/// <param name="Faixas">Faixas de consumo por classe.</param>
/// <param name="Publicar">Se verdadeiro, publica após configurar.</param>
public sealed record ConfigurarTabelaCosipCommand(
    int Exercicio,
    string FundamentoLegal,
    IReadOnlyList<FaixaCosipInput> Faixas,
    bool Publicar = true) : ICommand<Guid>;

/// <summary>Regras de validação da configuração da tabela de COSIP.</summary>
public sealed class ConfigurarTabelaCosipValidator : AbstractValidator<ConfigurarTabelaCosipCommand>
{
    /// <summary>Define as regras.</summary>
    public ConfigurarTabelaCosipValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Faixas).NotEmpty();
        RuleForEach(c => c.Faixas).ChildRules(faixa =>
        {
            faixa.RuleFor(f => f.Classe).IsInEnum();
            faixa.RuleFor(f => f.ConsumoMinimoKwh).GreaterThanOrEqualTo(0m);
            faixa.RuleFor(f => f.Valor).GreaterThanOrEqualTo(0m);
        });
    }
}

/// <summary>Handler da configuração da tabela de COSIP.</summary>
public sealed class ConfigurarTabelaCosipHandler(
    ITabelaCosipRepository tabelas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConfigurarTabelaCosipCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConfigurarTabelaCosipCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tabela = TabelaCosip.Criar(tenant.TenantId, request.Exercicio, request.FundamentoLegal);

        foreach (var faixa in request.Faixas)
        {
            tabela.DefinirFaixa(faixa.Classe, faixa.ConsumoMinimoKwh, faixa.ConsumoMaximoKwh, ValorMonetario.De(faixa.Valor));
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

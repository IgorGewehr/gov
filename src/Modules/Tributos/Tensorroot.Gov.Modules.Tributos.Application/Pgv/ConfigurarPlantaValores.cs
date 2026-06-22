using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

namespace Tensorroot.Gov.Modules.Tributos.Application.Pgv;

/// <summary>Valor unitário (VUT/VUC) de uma zona fiscal na PGV.</summary>
/// <param name="ZonaFiscal">Código da zona fiscal.</param>
/// <param name="ValorM2Terreno">VUT (R$/m²).</param>
/// <param name="ValorM2Construcao">VUC (R$/m²).</param>
public sealed record ZonaPgvInput(string ZonaFiscal, decimal ValorM2Terreno, decimal ValorM2Construcao);

/// <summary>Fator de correção da PGV.</summary>
/// <param name="Tipo">Categoria do fator.</param>
/// <param name="Chave">Chave do fator.</param>
/// <param name="Multiplicador">Multiplicador (&gt; 0).</param>
public sealed record FatorPgvInput(TipoFatorPgv Tipo, string Chave, decimal Multiplicador);

/// <summary>
/// Configura a Planta Genérica de Valores (PGV) de um exercício a partir dos parâmetros da LEI
/// MUNICIPAL: zonas (VUT/VUC) e fatores, e a publica (torna vigente). Nenhum valor é hardcoded —
/// todos vêm do comando (lei do município). Ver M6-DESIGN §1.2.
/// </summary>
/// <param name="Exercicio">Exercício fiscal (ano).</param>
/// <param name="FundamentoLegal">Lei/decreto municipal que institui a PGV.</param>
/// <param name="Zonas">Zonas fiscais com VUT/VUC.</param>
/// <param name="Fatores">Fatores de correção (padrão, depreciação, uso).</param>
/// <param name="Publicar">Se verdadeiro, publica a PGV (vigente) após a configuração.</param>
public sealed record ConfigurarPlantaValoresCommand(
    int Exercicio,
    string FundamentoLegal,
    IReadOnlyList<ZonaPgvInput> Zonas,
    IReadOnlyList<FatorPgvInput> Fatores,
    bool Publicar = true) : ICommand<Guid>;

/// <summary>Regras de validação da configuração da PGV.</summary>
public sealed class ConfigurarPlantaValoresValidator : AbstractValidator<ConfigurarPlantaValoresCommand>
{
    /// <summary>Define as regras.</summary>
    public ConfigurarPlantaValoresValidator()
    {
        RuleFor(comando => comando.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(comando => comando.FundamentoLegal).NotEmpty().MaximumLength(300);
        RuleFor(comando => comando.Zonas).NotEmpty().WithMessage("Defina ao menos uma zona fiscal.");
        RuleForEach(comando => comando.Zonas).ChildRules(zona =>
        {
            zona.RuleFor(z => z.ZonaFiscal).NotEmpty().MaximumLength(60);
            zona.RuleFor(z => z.ValorM2Terreno).GreaterThanOrEqualTo(0m);
            zona.RuleFor(z => z.ValorM2Construcao).GreaterThanOrEqualTo(0m);
        });
        RuleForEach(comando => comando.Fatores).ChildRules(fator =>
        {
            fator.RuleFor(f => f.Tipo)
                .Must(tipo => Enum.IsDefined(tipo))
                .WithMessage("Tipo de fator inválido.");
            fator.RuleFor(f => f.Chave).NotEmpty().MaximumLength(60);
            fator.RuleFor(f => f.Multiplicador).GreaterThan(0m);
        });
    }
}

/// <summary>Handler da configuração da PGV.</summary>
public sealed class ConfigurarPlantaValoresHandler(
    IPlantaValoresRepository plantas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConfigurarPlantaValoresCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConfigurarPlantaValoresCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var planta = PlantaValores.Criar(tenant.TenantId, request.Exercicio, request.FundamentoLegal);

        foreach (var zona in request.Zonas)
        {
            planta.DefinirZona(zona.ZonaFiscal, zona.ValorM2Terreno, zona.ValorM2Construcao);
        }

        foreach (var fator in request.Fatores)
        {
            planta.DefinirFator(fator.Tipo, fator.Chave, fator.Multiplicador);
        }

        if (request.Publicar)
        {
            planta.Publicar();
        }

        plantas.Adicionar(planta);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return planta.Id.Value;
    }
}

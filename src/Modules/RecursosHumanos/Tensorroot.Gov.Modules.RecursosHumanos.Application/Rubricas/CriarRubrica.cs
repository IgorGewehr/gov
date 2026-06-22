using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Rubricas;

/// <summary>Cria uma rubrica (verba) parametrizavel da folha, com incidencias e vigencia.</summary>
/// <param name="Codigo">Codigo da rubrica (S-1010).</param>
/// <param name="Descricao">Descricao legivel.</param>
/// <param name="Natureza">Natureza (1=Provento, 2=Desconto, 3=Informativa, 4=InformativaDedutora).</param>
/// <param name="AnoVigencia">Ano de inicio de vigencia.</param>
/// <param name="MesVigencia">Mes de inicio de vigencia (1 a 12).</param>
/// <param name="IncideInss">Integra base de INSS.</param>
/// <param name="IncideRpps">Integra base de RPPS.</param>
/// <param name="IncideIrrf">Integra base do IRRF.</param>
/// <param name="IncideFgts">Integra base do FGTS.</param>
/// <param name="ValorFixo">Valor fixo (opcional; exclusivo com percentual).</param>
/// <param name="Percentual">Percentual sobre a base (opcional; exclusivo com valor fixo).</param>
public sealed record CriarRubricaCommand(
    string Codigo,
    string Descricao,
    int Natureza,
    int AnoVigencia,
    int MesVigencia,
    bool IncideInss,
    bool IncideRpps,
    bool IncideIrrf,
    bool IncideFgts,
    decimal? ValorFixo,
    decimal? Percentual) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de rubrica.</summary>
public sealed class CriarRubricaValidator : AbstractValidator<CriarRubricaCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarRubricaValidator()
    {
        RuleFor(c => c.Codigo).NotEmpty().MaximumLength(RubricaFolha.CodigoComprimentoMaximo)
            .WithMessage($"Codigo e obrigatorio (max. {RubricaFolha.CodigoComprimentoMaximo} caracteres).");
        RuleFor(c => c.Descricao).NotEmpty().MaximumLength(200)
            .WithMessage("Descricao e obrigatoria.");
        // LICAO: validar enum por Enum.IsDefined sobre o tipo, nunca IsInEnum sobre o int do contrato.
        RuleFor(c => c.Natureza).Must(n => Enum.IsDefined(typeof(NaturezaRubrica), n))
            .WithMessage("Natureza invalida (1=Provento, 2=Desconto, 3=Informativa, 4=InformativaDedutora).");
        RuleFor(c => c.AnoVigencia).InclusiveBetween(2000, 2100);
        RuleFor(c => c.MesVigencia).InclusiveBetween(1, 12);
        RuleFor(c => c.ValorFixo).GreaterThan(0m).When(c => c.ValorFixo.HasValue)
            .WithMessage("Valor fixo deve ser maior que zero.");
        RuleFor(c => c.Percentual).InclusiveBetween(0m, 1m).When(c => c.Percentual.HasValue)
            .WithMessage("Percentual deve estar entre 0 e 1 (fracao decimal).");
        RuleFor(c => c).Must(c => !(c.ValorFixo.HasValue && c.Percentual.HasValue))
            .WithMessage("Rubrica nao pode ter valor fixo e percentual simultaneamente.");
    }
}

/// <summary>Handler da criacao de rubrica parametrizavel.</summary>
public sealed class CriarRubricaHandler(
    IRubricaFolhaRepository rubricas,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CriarRubricaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarRubricaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var codigo = Rubrica.De(request.Codigo);
        if (await rubricas.CodigoExisteAsync(codigo, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Ja existe rubrica ativa com o codigo {codigo.Codigo} no tenant.");
        }

        var rubrica = RubricaFolha.Criar(
            tenantContext.TenantId,
            codigo,
            request.Descricao,
            (NaturezaRubrica)request.Natureza,
            Competencia.De(request.AnoVigencia, request.MesVigencia),
            request.IncideInss,
            request.IncideRpps,
            request.IncideIrrf,
            request.IncideFgts,
            request.ValorFixo,
            request.Percentual);

        rubricas.Adicionar(rubrica);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rubrica.Id.Value;
    }
}

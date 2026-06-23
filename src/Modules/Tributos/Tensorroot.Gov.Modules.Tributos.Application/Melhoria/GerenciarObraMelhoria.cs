using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Melhoria;

/// <summary>
/// Publica o edital de uma obra de Contribuição de Melhoria (CTN art. 82): memorial, custo, zona, fator
/// de absorção, parcela a financiar e prazo de impugnação ≥ 30 dias. Nenhum lançamento ocorre antes do
/// encerramento do prazo. Ver M6-DESIGN §3.5.
/// </summary>
/// <param name="IdentificacaoObra">Identificação da obra.</param>
/// <param name="MemorialDescritivo">Memorial descritivo do projeto.</param>
/// <param name="CustoTotalObra">Custo total orçado (R$).</param>
/// <param name="ParcelaCustoFinanciadaPercentual">Percentual do custo a financiar (0 a 100).</param>
/// <param name="ZonaBeneficiada">Zona beneficiada.</param>
/// <param name="FatorAbsorcaoPercentual">Fator de absorção do benefício (0 a 100).</param>
/// <param name="DataPublicacaoEdital">Data de publicação do edital.</param>
/// <param name="FimPrazoImpugnacao">Fim do prazo de impugnação (≥ 30 dias após a publicação).</param>
/// <param name="FundamentoLegal">Lei específica da obra + CTM.</param>
public sealed record PublicarEditalMelhoriaCommand(
    string IdentificacaoObra,
    string MemorialDescritivo,
    decimal CustoTotalObra,
    decimal ParcelaCustoFinanciadaPercentual,
    string ZonaBeneficiada,
    decimal FatorAbsorcaoPercentual,
    DateOnly DataPublicacaoEdital,
    DateOnly FimPrazoImpugnacao,
    string FundamentoLegal) : ICommand<Guid>;

/// <summary>Regras de validação da publicação do edital.</summary>
public sealed class PublicarEditalMelhoriaValidator : AbstractValidator<PublicarEditalMelhoriaCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarEditalMelhoriaValidator()
    {
        RuleFor(c => c.IdentificacaoObra).NotEmpty().MaximumLength(120);
        RuleFor(c => c.MemorialDescritivo).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.CustoTotalObra).GreaterThan(0m);
        RuleFor(c => c.ParcelaCustoFinanciadaPercentual).InclusiveBetween(0.01m, 100m);
        RuleFor(c => c.ZonaBeneficiada).NotEmpty().MaximumLength(200);
        RuleFor(c => c.FatorAbsorcaoPercentual).InclusiveBetween(0.01m, 100m);
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
        // CTN art. 82, II: prazo de impugnação não inferior a 30 dias (também reforçado no domínio).
        RuleFor(c => c)
            .Must(c => c.FimPrazoImpugnacao.DayNumber - c.DataPublicacaoEdital.DayNumber >= ObraContribuicaoMelhoria.PrazoMinimoImpugnacaoDias)
            .WithMessage($"O prazo de impugnação deve ser de no mínimo {ObraContribuicaoMelhoria.PrazoMinimoImpugnacaoDias} dias (CTN art. 82, II).");
    }
}

/// <summary>Handler da publicação do edital.</summary>
public sealed class PublicarEditalMelhoriaHandler(
    IObraContribuicaoMelhoriaRepository obras,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<PublicarEditalMelhoriaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(PublicarEditalMelhoriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = ObraContribuicaoMelhoria.PublicarEdital(
            tenant.TenantId,
            request.IdentificacaoObra,
            request.MemorialDescritivo,
            ValorMonetario.De(request.CustoTotalObra),
            request.ParcelaCustoFinanciadaPercentual,
            request.ZonaBeneficiada,
            request.FatorAbsorcaoPercentual,
            request.DataPublicacaoEdital,
            request.FimPrazoImpugnacao,
            request.FundamentoLegal);

        obras.Adicionar(obra);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return obra.Id.Value;
    }
}

/// <summary>
/// Adiciona um imóvel beneficiado à obra, com a valorização individual apurada (limite individual da
/// contribuição — CTN art. 81). Só durante a fase de edital. Ver M6-DESIGN §3.5.
/// </summary>
/// <param name="ObraId">Obra de Contribuição de Melhoria.</param>
/// <param name="ImovelId">Imóvel beneficiado.</param>
/// <param name="ProprietarioId">Contribuinte proprietário.</param>
/// <param name="ValorizacaoIndividual">Valorização individual apurada (R$).</param>
public sealed record AdicionarImovelBeneficiadoCommand(
    Guid ObraId,
    Guid ImovelId,
    Guid ProprietarioId,
    decimal ValorizacaoIndividual) : ICommand;

/// <summary>Regras de validação da inclusão de imóvel beneficiado.</summary>
public sealed class AdicionarImovelBeneficiadoValidator : AbstractValidator<AdicionarImovelBeneficiadoCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarImovelBeneficiadoValidator()
    {
        RuleFor(c => c.ObraId).NotEmpty();
        RuleFor(c => c.ImovelId).NotEmpty();
        RuleFor(c => c.ProprietarioId).NotEmpty();
        RuleFor(c => c.ValorizacaoIndividual).GreaterThan(0m);
    }
}

/// <summary>Handler da inclusão de imóvel beneficiado.</summary>
public sealed class AdicionarImovelBeneficiadoHandler(
    IObraContribuicaoMelhoriaRepository obras,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarImovelBeneficiadoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AdicionarImovelBeneficiadoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraContribuicaoMelhoriaId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra de Contribuição de Melhoria não encontrada.");

        obra.AdicionarImovelBeneficiado(
            new ImovelId(request.ImovelId),
            new ContribuinteId(request.ProprietarioId),
            ValorMonetario.De(request.ValorizacaoIndividual));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Encerra o prazo de impugnação ao edital (CTN art. 82): só a partir da data de fim do prazo. Habilita o
/// rateio. Ver M6-DESIGN §3.5.
/// </summary>
/// <param name="ObraId">Obra de Contribuição de Melhoria.</param>
/// <param name="DataReferencia">Data de referência (deve ser ≥ fim do prazo).</param>
public sealed record EncerrarPrazoImpugnacaoCommand(Guid ObraId, DateOnly DataReferencia) : ICommand;

/// <summary>Regras de validação do encerramento do prazo de impugnação.</summary>
public sealed class EncerrarPrazoImpugnacaoValidator : AbstractValidator<EncerrarPrazoImpugnacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarPrazoImpugnacaoValidator() => RuleFor(c => c.ObraId).NotEmpty();
}

/// <summary>Handler do encerramento do prazo de impugnação.</summary>
public sealed class EncerrarPrazoImpugnacaoHandler(
    IObraContribuicaoMelhoriaRepository obras,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarPrazoImpugnacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarPrazoImpugnacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraContribuicaoMelhoriaId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra de Contribuição de Melhoria não encontrada.");

        obra.EncerrarPrazoImpugnacao(request.DataReferencia);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Convenios.Application.Mrosc;

/// <summary>
/// Inicia a selecao de uma parceria-saida OSC (fluxo B): cria a parceria em <c>EmSelecao</c> com a OSC, o
/// tipo de instrumento e a forma de selecao (chamamento OU dispensa/inexigibilidade — fundamentada, B-INV-1).
/// </summary>
/// <param name="Osc">Dados da OSC.</param>
/// <param name="TipoInstrumento">Tipo de instrumento (colaboracao/fomento/cooperacao).</param>
/// <param name="FormaSelecao">Forma de selecao.</param>
public sealed record IniciarSelecaoParceriaCommand(
    OscPayload Osc,
    TipoInstrumentoMrosc TipoInstrumento,
    FormaSelecaoPayload FormaSelecao) : ICommand<Guid>;

/// <summary>Regras de validacao do inicio de selecao.</summary>
public sealed class IniciarSelecaoParceriaValidator : AbstractValidator<IniciarSelecaoParceriaCommand>
{
    /// <summary>Define as regras.</summary>
    public IniciarSelecaoParceriaValidator()
    {
        RuleFor(comando => comando.Osc).NotNull();
        RuleFor(comando => comando.Osc.Cnpj).NotEmpty();
        RuleFor(comando => comando.Osc.RazaoSocial).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.Osc.NaturezaJuridica).NotEmpty().MaximumLength(120);
        RuleFor(comando => comando.TipoInstrumento).IsInEnum();
        RuleFor(comando => comando.FormaSelecao).NotNull();
        RuleFor(comando => comando.FormaSelecao.Tipo).IsInEnum();
        // Direta exige fundamento + justificativa; chamamento exige edital.
        RuleFor(comando => comando.FormaSelecao.Edital)
            .NotEmpty()
            .When(comando => comando.FormaSelecao.Tipo == TipoFormaSelecao.Chamamento)
            .WithMessage("Chamamento exige edital.");
        RuleFor(comando => comando.FormaSelecao.FundamentoLegal)
            .NotEmpty()
            .When(comando => comando.FormaSelecao.Tipo != TipoFormaSelecao.Chamamento)
            .WithMessage("Contratacao direta exige fundamento legal (art. 30/31).");
        RuleFor(comando => comando.FormaSelecao.Justificativa)
            .NotEmpty()
            .When(comando => comando.FormaSelecao.Tipo != TipoFormaSelecao.Chamamento)
            .WithMessage("Contratacao direta exige justificativa.");
    }
}

/// <summary>Handler do inicio de selecao da parceria.</summary>
public sealed class IniciarSelecaoParceriaHandler(
    IParceriaOscRepository parcerias, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<IniciarSelecaoParceriaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(IniciarSelecaoParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var certidoes = request.Osc.Certidoes
            .Select(certidao => new CertidaoRegularidade(certidao.Tipo, certidao.ValidaAte))
            .ToList();

        var osc = Osc.Criar(
            Cnpj.Create(request.Osc.Cnpj),
            request.Osc.RazaoSocial,
            request.Osc.NaturezaJuridica,
            request.Osc.ExperienciaPrevia,
            request.Osc.CapacidadeTecnica,
            certidoes);

        var formaSelecao = MapearFormaSelecao(request.FormaSelecao);

        var parceria = ParceriaOsc.IniciarSelecao(tenant.TenantId, osc, request.TipoInstrumento, formaSelecao);
        parcerias.Adicionar(parceria);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return parceria.Id.Value;
    }

    private static FormaSelecao MapearFormaSelecao(FormaSelecaoPayload payload) => payload.Tipo switch
    {
        TipoFormaSelecao.Chamamento => FormaSelecao.PorChamamento(
            payload.ProcessoId ?? Guid.NewGuid(), payload.Edital ?? string.Empty, payload.EditalHomologado),
        TipoFormaSelecao.Dispensa => FormaSelecao.PorDispensa(
            payload.FundamentoLegal ?? string.Empty, payload.Justificativa ?? string.Empty),
        TipoFormaSelecao.Inexigibilidade => FormaSelecao.PorInexigibilidade(
            payload.FundamentoLegal ?? string.Empty, payload.Justificativa ?? string.Empty),
        _ => throw new ArgumentOutOfRangeException(nameof(payload), payload.Tipo, "Forma de selecao invalida."),
    };
}

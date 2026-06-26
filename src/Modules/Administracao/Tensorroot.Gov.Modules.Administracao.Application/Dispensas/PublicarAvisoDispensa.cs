using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>
/// Publica o aviso de contratacao direta da dispensa (IN SEGES/ME 67/2021; art. 75 §3). A data de
/// abertura da disputa deve respeitar o prazo minimo de divulgacao (parametrizavel por tenant), validado
/// aqui contra o calendario de dias uteis.
/// </summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="NumeroAviso">Identificador/numero do aviso publicado.</param>
/// <param name="AberturaDisputa">Data/hora de abertura da etapa de lances.</param>
public sealed record PublicarAvisoDispensaCommand(
    Guid DispensaId,
    string NumeroAviso,
    DateTimeOffset AberturaDisputa) : ICommand;

/// <summary>Regras de validacao da publicacao do aviso.</summary>
public sealed class PublicarAvisoDispensaValidator : AbstractValidator<PublicarAvisoDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarAvisoDispensaValidator()
    {
        RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
        RuleFor(comando => comando.NumeroAviso).NotEmpty().MaximumLength(60).WithMessage("Numero do aviso e obrigatorio (max. 60).");
    }
}

/// <summary>Handler da publicacao do aviso de contratacao direta.</summary>
public sealed class PublicarAvisoDispensaHandler(
    IDispensaRepository dispensas,
    IDispensaParametros parametros,
    ICalendarioDiasUteis calendario,
    IDataHojeTenant dataHoje,
    IUnitOfWork unitOfWork)
    : ICommandHandler<PublicarAvisoDispensaCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarAvisoDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");

        // Prazo minimo de divulgacao do aviso (IN 67/2021): a abertura da disputa nao pode ser antes de
        // "hoje + N dias uteis" (N parametrizavel por tenant — sem numero magico, CLAUDE.md §16). "Hoje" no
        // fuso do tenant (UTC-3); o calendario aplica feriados municipais do tenant.
        var hoje = dataHoje.Hoje();
        var prazoMinimoDiasUteis = parametros.PrazoMinimoDivulgacaoDiasUteis();
        var dataMinimaAbertura = PrazoLegal.Criar(
            hoje,
            prazoMinimoDiasUteis,
            UnidadePrazo.DiasUteis,
            "IN SEGES/ME 67/2021 (prazo minimo de divulgacao do aviso)",
            calendario).Vencimento;

        var dataAbertura = DateOnly.FromDateTime(request.AberturaDisputa.Date);
        if (dataAbertura < dataMinimaAbertura)
        {
            throw new InvalidOperationException(
                $"Abertura da disputa ({dataAbertura:yyyy-MM-dd}) viola o prazo minimo de divulgacao do aviso: minimo {dataMinimaAbertura:yyyy-MM-dd} ({prazoMinimoDiasUteis} dias uteis; IN SEGES/ME 67/2021).");
        }

        dispensa.PublicarAviso(request.NumeroAviso, request.AberturaDisputa);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

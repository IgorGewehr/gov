using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto;

/// <summary>
/// Define (ou substitui) a jornada/escala de um servidor: carga diaria, intervalo, tolerancia e regime
/// (estatutario/celetista). A vigente anterior, se houver, e desativada.
/// </summary>
/// <param name="ServidorId">Servidor da jornada.</param>
/// <param name="CargaDiariaMinutos">Carga diaria contratada (minutos).</param>
/// <param name="IntervaloMinutos">Intervalo intrajornada (minutos).</param>
/// <param name="Regime">Regime (1=Estatutario, 2=Celetista).</param>
/// <param name="VigenciaInicio">Data inicial de vigencia.</param>
/// <param name="ToleranciaMinutos">Tolerancia diaria (minutos).</param>
public sealed record DefinirJornadaCommand(
    Guid ServidorId,
    int CargaDiariaMinutos,
    int IntervaloMinutos,
    int Regime,
    DateOnly VigenciaInicio,
    int ToleranciaMinutos) : ICommand<Guid>;

/// <summary>Regras de validacao da definicao de jornada.</summary>
public sealed class DefinirJornadaValidator : AbstractValidator<DefinirJornadaCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirJornadaValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.CargaDiariaMinutos).InclusiveBetween(1, JornadaTrabalho.MinutosNoDia);
        RuleFor(c => c.IntervaloMinutos).InclusiveBetween(0, JornadaTrabalho.MinutosNoDia);
        RuleFor(c => c.ToleranciaMinutos).InclusiveBetween(0, JornadaTrabalho.MinutosNoDia);
        // LICAO: validar enum por Enum.IsDefined sobre o tipo, nunca IsInEnum sobre o int do contrato.
        RuleFor(c => c.Regime).Must(r => Enum.IsDefined(typeof(RegimeJornada), r))
            .WithMessage("Regime invalido (1=Estatutario, 2=Celetista).");
    }
}

/// <summary>Handler da definicao de jornada.</summary>
public sealed class DefinirJornadaHandler(
    IJornadaTrabalhoRepository jornadas,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DefinirJornadaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(DefinirJornadaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Substitui a vigente anterior (preserva o historico desativando-a).
        var vigente = await jornadas.ObterVigenteAsync(request.ServidorId, request.VigenciaInicio, cancellationToken).ConfigureAwait(false);
        vigente?.Desativar();

        var jornada = JornadaTrabalho.Definir(
            tenantContext.TenantId,
            request.ServidorId,
            request.CargaDiariaMinutos,
            request.IntervaloMinutos,
            (RegimeJornada)request.Regime,
            request.VigenciaInicio,
            request.ToleranciaMinutos);

        jornadas.Adicionar(jornada);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return jornada.Id.Value;
    }
}

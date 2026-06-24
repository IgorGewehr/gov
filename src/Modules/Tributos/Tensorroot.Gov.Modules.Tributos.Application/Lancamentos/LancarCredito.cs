using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Lancamentos;

/// <summary>Constitui (lança) um crédito tributário para um contribuinte.</summary>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
/// <param name="TipoTributo">Espécie tributária.</param>
/// <param name="Ano">Ano da competência.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="ValorPrincipal">Valor principal.</param>
/// <param name="Vencimento">Data de vencimento.</param>
public sealed record LancarCreditoCommand(
    Guid ContribuinteId,
    TipoTributo TipoTributo,
    int Ano,
    int Mes,
    decimal ValorPrincipal,
    DateOnly Vencimento) : ICommand<Guid>;

/// <summary>Regras de validação do lançamento de crédito.</summary>
public sealed class LancarCreditoValidator : AbstractValidator<LancarCreditoCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarCreditoValidator()
    {
        RuleFor(comando => comando.ContribuinteId).NotEmpty();
        RuleFor(comando => comando.TipoTributo).IsInEnum();
        RuleFor(comando => comando.Ano).GreaterThanOrEqualTo(1900);
        RuleFor(comando => comando.Mes).InclusiveBetween(1, 12);
        RuleFor(comando => comando.ValorPrincipal).GreaterThan(0m);
    }
}

/// <summary>Handler do lançamento de crédito tributário.</summary>
public sealed class LancarCreditoHandler(
    IContribuinteRepository contribuintes,
    ILancamentoRepository lancamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<LancarCreditoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(LancarCreditoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        var contribuinte = await contribuintes.ObterPorIdAsync(contribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte não encontrado.");

        // Fato gerador na competência informada; data da constituição = "hoje" administrativo (sem
        // relógio no domínio — CLAUDE.md §16). A decadência (CTN art. 173, I) é aferida no agregado.
        var dataFatoGerador = new DateOnly(request.Ano, request.Mes, DateTime.DaysInMonth(request.Ano, request.Mes));
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var lancamento = Lancamento.Lancar(
            tenant.TenantId,
            contribuinte.Id,
            request.TipoTributo,
            Competencia.De(request.Ano, request.Mes),
            ValorMonetario.De(request.ValorPrincipal),
            request.Vencimento,
            dataFatoGerador,
            hoje);

        lancamentos.Adicionar(lancamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return lancamento.Id.Value;
    }
}

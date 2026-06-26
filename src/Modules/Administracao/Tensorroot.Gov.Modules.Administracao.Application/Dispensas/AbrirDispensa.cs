using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>
/// Abre uma dispensa eletronica em razao do valor (rascunho — nasce <c>Aberta</c>; Lei 14.133/2021,
/// art. 75, I/II; IN SEGES/ME 67/2021). O limite legal vigente e a norma-fonte sao resolvidos do tenant.
/// </summary>
/// <param name="Objeto">Descricao do objeto da contratacao direta.</param>
/// <param name="Fundamento">Fundamento legal (art. 75, I ou II).</param>
/// <param name="CriterioJulgamento">Criterio de julgamento (menor preco/maior desconto).</param>
/// <param name="EtpId">Referencia ao Estudo Tecnico Preliminar (opcional).</param>
/// <param name="TermoReferenciaId">Referencia ao Termo de Referencia (opcional).</param>
public sealed record AbrirDispensaCommand(
    string Objeto,
    FundamentoDispensaValor Fundamento,
    CriterioJulgamentoDispensa CriterioJulgamento,
    Guid? EtpId,
    Guid? TermoReferenciaId) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de dispensa.</summary>
public sealed class AbrirDispensaValidator : AbstractValidator<AbrirDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirDispensaValidator()
    {
        RuleFor(comando => comando.Objeto)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Objeto e obrigatorio (max. 500 caracteres).");
        RuleFor(comando => comando.Fundamento).IsInEnum().WithMessage("Fundamento de dispensa invalido.");
        RuleFor(comando => comando.CriterioJulgamento).IsInEnum().WithMessage("Criterio de julgamento invalido.");
    }
}

/// <summary>Handler da abertura de dispensa.</summary>
public sealed class AbrirDispensaHandler(
    IDispensaRepository dispensas,
    IDispensaParametros parametros,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirDispensaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Limite legal vigente resolvido do tenant (parametrizavel — Dec. 12.807/2025; nunca literal no agregado).
        var limite = parametros.LimiteVigente(request.Fundamento);

        var dispensa = DispensaEletronica.Abrir(
            tenant.TenantId,
            request.Objeto,
            request.Fundamento,
            request.CriterioJulgamento,
            ValorMonetario.De(limite.Valor),
            limite.NormaFonte,
            request.EtpId,
            request.TermoReferenciaId);

        dispensas.Adicionar(dispensa);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return dispensa.Id.Value;
    }
}

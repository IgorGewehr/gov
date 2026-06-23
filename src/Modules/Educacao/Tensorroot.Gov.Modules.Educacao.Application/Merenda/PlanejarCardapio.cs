using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

namespace Tensorroot.Gov.Modules.Educacao.Application.Merenda;

/// <summary>Planeja um cardapio semanal (PNAE) para uma escola/faixa etaria/semana.</summary>
/// <param name="EscolaId">Escola do cardapio.</param>
/// <param name="FaixaEtaria">Faixa etaria PNAE.</param>
/// <param name="Semana">Primeiro dia da semana (segunda-feira).</param>
public sealed record PlanejarCardapioCommand(
    Guid EscolaId,
    FaixaEtariaPnae FaixaEtaria,
    DateOnly Semana) : ICommand<Guid>;

/// <summary>Regras de validacao do planejamento de cardapio.</summary>
public sealed class PlanejarCardapioValidator : AbstractValidator<PlanejarCardapioCommand>
{
    /// <summary>Define as regras.</summary>
    public PlanejarCardapioValidator()
    {
        RuleFor(comando => comando.EscolaId).NotEmpty().WithMessage("Escola obrigatoria.");
        RuleFor(comando => comando.FaixaEtaria).IsInEnum().WithMessage("Faixa etaria PNAE invalida.");
        RuleFor(comando => comando.Semana).NotEmpty().WithMessage("Semana obrigatoria.");
    }
}

/// <summary>Handler do planejamento de cardapio: valida a escola e cria o cardapio (situacao Planejado).</summary>
public sealed class PlanejarCardapioHandler(
    ICardapioRepository cardapios,
    IEscolaRepository escolas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<PlanejarCardapioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(PlanejarCardapioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var escolaId = new EscolaId(request.EscolaId);
        var escola = await escolas.ObterPorIdAsync(escolaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Escola nao encontrada.");
        if (escola.Encerrada)
        {
            throw new InvalidOperationException("Escola desativada nao admite cardapio.");
        }

        var cardapio = Cardapio.Planejar(tenant.TenantId, escolaId, request.FaixaEtaria, request.Semana);
        cardapios.Adicionar(cardapio);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return cardapio.Id.Value;
    }
}

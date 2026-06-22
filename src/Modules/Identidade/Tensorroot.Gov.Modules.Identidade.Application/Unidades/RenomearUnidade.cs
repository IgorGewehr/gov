using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

namespace Tensorroot.Gov.Modules.Identidade.Application.Unidades;

/// <summary>Renomeia uma UO e/ou altera o seu tipo.</summary>
/// <param name="UnidadeId">UO alvo.</param>
/// <param name="Nome">Novo nome de exibicao.</param>
/// <param name="Tipo">Novo tipo (natureza administrativa).</param>
public sealed record RenomearUnidadeCommand(Guid UnidadeId, string Nome, TipoUnidade Tipo) : ICommand;

/// <summary>Regras de validacao da renomeacao de UO.</summary>
public sealed class RenomearUnidadeValidator : AbstractValidator<RenomearUnidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public RenomearUnidadeValidator()
    {
        RuleFor(comando => comando.UnidadeId).NotEmpty();
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(UnidadeOrganizacional.ComprimentoMaximoNome);
        RuleFor(comando => comando.Tipo).IsInEnum();
    }
}

/// <summary>Handler da renomeacao de UO.</summary>
public sealed class RenomearUnidadeHandler(IUnidadeRepository unidades, IUnitOfWork unitOfWork)
    : ICommandHandler<RenomearUnidadeCommand>
{
    /// <inheritdoc />
    public async Task Handle(RenomearUnidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var unidade = await unidades.ObterPorIdAsync(new UnidadeOrganizacionalId(request.UnidadeId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade organizacional nao encontrada.");

        unidade.Editar(request.Nome, request.Tipo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

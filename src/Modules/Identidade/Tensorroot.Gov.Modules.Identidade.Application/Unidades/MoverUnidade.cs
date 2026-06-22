using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

namespace Tensorroot.Gov.Modules.Identidade.Application.Unidades;

/// <summary>
/// Move uma UO para baixo de um novo pai (reparenteamento). A aplicacao RESOLVE a cadeia de
/// ancestrais do novo pai (server-side) e a entrega ao dominio (<c>DefinirPai</c>), que valida a
/// invariante de subarvore conexa e aciclica (I5): mover uma UO para baixo de um de seus
/// descendentes (ou de si mesma) e rejeitado.
/// </summary>
/// <param name="UnidadeId">UO a mover.</param>
/// <param name="NovoPaiId">UO que passara a ser o pai.</param>
public sealed record MoverUnidadeCommand(Guid UnidadeId, Guid NovoPaiId) : ICommand;

/// <summary>Regras de validacao do reparenteamento de UO.</summary>
public sealed class MoverUnidadeValidator : AbstractValidator<MoverUnidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public MoverUnidadeValidator()
    {
        RuleFor(comando => comando.UnidadeId).NotEmpty();
        RuleFor(comando => comando.NovoPaiId).NotEmpty();
        RuleFor(comando => comando.NovoPaiId)
            .NotEqual(comando => comando.UnidadeId)
            .WithMessage("Uma unidade nao pode ser pai dela mesma.");
    }
}

/// <summary>Handler do reparenteamento de UO.</summary>
public sealed class MoverUnidadeHandler(IUnidadeRepository unidades, IUnitOfWork unitOfWork)
    : ICommandHandler<MoverUnidadeCommand>
{
    /// <inheritdoc />
    public async Task Handle(MoverUnidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var unidadeId = new UnidadeOrganizacionalId(request.UnidadeId);
        var novoPaiId = new UnidadeOrganizacionalId(request.NovoPaiId);

        var unidade = await unidades.ObterPorIdAsync(unidadeId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade organizacional nao encontrada.");

        var novoPai = await unidades.ObterPorIdAsync(novoPaiId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade organizacional pai nao encontrada no tenant.");

        // Resolve a cadeia de ancestrais do NOVO PAI (do pai imediato ate a raiz), exclusiva de si
        // mesmo. A arvore e construida a partir de TODAS as UOs do tenant (filtro global aplicado).
        var todas = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
        var arvore = ArvoreUnidades.Construir(todas);

        // ComAncestrais inclui a propria UO; o dominio espera a cadeia do pai (inclusive ele) — o que
        // e exatamente ComAncestrais(novoPai). DefinirPai rejeita se a UO movida aparecer nessa cadeia.
        var ancestraisDoNovoPai = arvore.ComAncestrais(novoPai.Id);

        unidade.DefinirPai(novoPai.Id, ancestraisDoNovoPai.ToArray());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

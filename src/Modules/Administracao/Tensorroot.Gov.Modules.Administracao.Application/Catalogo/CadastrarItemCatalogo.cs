using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Catalogo;

/// <summary>Cadastra um item padronizado no catalogo de materiais/servicos (CATMAT/CATSER).</summary>
/// <param name="Codigo">Codigo padronizado (unico por tenant).</param>
/// <param name="Natureza">Material ou servico.</param>
/// <param name="Descricao">Descricao padronizada.</param>
/// <param name="UnidadeFornecimento">Unidade de fornecimento (UN, KG, HORA, MES...).</param>
/// <param name="Classe">Classe/classificacao (opcional).</param>
public sealed record CadastrarItemCatalogoCommand(
    string Codigo,
    NaturezaItem Natureza,
    string Descricao,
    string UnidadeFornecimento,
    string? Classe) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de item de catalogo.</summary>
public sealed class CadastrarItemCatalogoValidator : AbstractValidator<CadastrarItemCatalogoCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarItemCatalogoValidator()
    {
        RuleFor(c => c.Codigo).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Natureza).IsInEnum();
        RuleFor(c => c.Descricao).NotEmpty().MaximumLength(500);
        RuleFor(c => c.UnidadeFornecimento).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Classe).MaximumLength(120);
    }
}

/// <summary>Handler do cadastro de item de catalogo.</summary>
public sealed class CadastrarItemCatalogoHandler(
    ICatalogoRepository catalogo,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarItemCatalogoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarItemCatalogoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await catalogo.ExistePorCodigoAsync(request.Codigo.Trim(), cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe item de catalogo com este codigo no tenant.");
        }

        var item = ItemCatalogo.Cadastrar(
            tenant.TenantId,
            request.Codigo,
            request.Natureza,
            request.Descricao,
            request.UnidadeFornecimento,
            request.Classe);

        catalogo.Adicionar(item);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return item.Id.Value;
    }
}

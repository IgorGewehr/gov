using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

namespace Tensorroot.Gov.Modules.Identidade.Application.Unidades;

/// <summary>
/// Cria uma Unidade Organizacional (UO) no tenant atual. Se <paramref name="UnidadePaiId"/> for
/// nulo, cria a UO RAIZ; caso contrario, cria uma filha do pai informado (que deve existir no tenant).
/// </summary>
/// <param name="Codigo">Codigo estavel (ex.: <c>SMS</c>) — unico por tenant.</param>
/// <param name="Nome">Nome de exibicao.</param>
/// <param name="Tipo">Natureza administrativa.</param>
/// <param name="UnidadePaiId">Pai na arvore; <c>null</c> cria a raiz.</param>
public sealed record CriarUnidadeCommand(
    string Codigo,
    string Nome,
    TipoUnidade Tipo,
    Guid? UnidadePaiId = null) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de UO.</summary>
public sealed class CriarUnidadeValidator : AbstractValidator<CriarUnidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarUnidadeValidator()
    {
        RuleFor(comando => comando.Codigo).NotEmpty().MaximumLength(UnidadeOrganizacional.ComprimentoMaximoCodigo);
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(UnidadeOrganizacional.ComprimentoMaximoNome);
        RuleFor(comando => comando.Tipo).IsInEnum();
    }
}

/// <summary>Handler da criacao de UO.</summary>
public sealed class CriarUnidadeHandler(
    IUnidadeRepository unidades,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CriarUnidadeCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarUnidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await unidades.CodigoEmUsoAsync(request.Codigo, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe uma unidade organizacional com este codigo no tenant.");
        }

        UnidadeOrganizacional unidade;
        if (request.UnidadePaiId is { } paiGuid)
        {
            var paiId = new UnidadeOrganizacionalId(paiGuid);
            var pai = await unidades.ObterPorIdAsync(paiId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Unidade organizacional pai nao encontrada no tenant.");

            unidade = UnidadeOrganizacional.CriarFilha(tenant.TenantId, request.Codigo, request.Nome, request.Tipo, pai.Id);
        }
        else
        {
            unidade = UnidadeOrganizacional.CriarRaiz(tenant.TenantId, request.Codigo, request.Nome, request.Tipo);
        }

        unidades.Adicionar(unidade);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return unidade.Id.Value;
    }
}

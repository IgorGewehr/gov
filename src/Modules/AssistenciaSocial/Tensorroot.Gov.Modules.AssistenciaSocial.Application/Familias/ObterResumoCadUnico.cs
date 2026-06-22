using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Integracoes;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;

/// <summary>
/// Folha resumo do CadUnico (read model federal) projetada para uma familia — somente leitura,
/// isolada por <c>TenantId</c>; nunca trafega entre tenants (I-9, README secao 6).
/// </summary>
/// <param name="NisMascarado">NIS do responsavel familiar, mascarado.</param>
/// <param name="RendaFamiliarDeclarada">Renda familiar declarada na base federal.</param>
/// <param name="QuantidadeMembros">Quantidade de membros na composicao federal.</param>
/// <param name="DataUltimaAtualizacao">Data da ultima atualizacao do cadastro na base federal.</param>
/// <param name="DentroDaVigencia">Indica se o cadastro federal esta dentro da vigencia (24 meses).</param>
public sealed record ResumoCadUnico(
    string NisMascarado,
    decimal RendaFamiliarDeclarada,
    int QuantidadeMembros,
    DateOnly DataUltimaAtualizacao,
    bool DentroDaVigencia);

/// <summary>Projeta o resumo do CadUnico de uma familia (tenant-scoped).</summary>
/// <param name="FamiliaId">Familia a consultar.</param>
public sealed record ObterResumoCadUnicoQuery(Guid FamiliaId) : IQuery<ResumoCadUnico>;

/// <summary>Handler da projecao do resumo do CadUnico.</summary>
public sealed class ObterResumoCadUnicoHandler(
    IFamiliaRepository familias,
    ICadUnicoReadModel cadUnico)
    : IQueryHandler<ObterResumoCadUnicoQuery, ResumoCadUnico>
{
    /// <inheritdoc />
    public async Task<ResumoCadUnico> Handle(ObterResumoCadUnicoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var familia = await familias.ObterPorIdAsync(new FamiliaId(request.FamiliaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Familia nao encontrada.");

        var resultado = await cadUnico.ProjetarResumoAsync(familia.Nis.Digitos, cancellationToken).ConfigureAwait(false);

        var dentroDaVigencia = resultado.NisLocalizado
            && resultado.DataUltimaAtualizacao.AddMonths(Familia.MesesValidadeCadastro)
                >= DateOnly.FromDateTime(DateTime.UtcNow);

        return new ResumoCadUnico(
            familia.Nis.Mascarado,
            resultado.RendaFamiliarDeclarada,
            resultado.QuantidadeMembros,
            resultado.DataUltimaAtualizacao,
            dentroDaVigencia);
    }
}

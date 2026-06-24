using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Pca;

namespace Tensorroot.Gov.Modules.Administracao.Application.Pca;

/// <summary>Item do PCA, para o detalhe do plano.</summary>
/// <param name="ItemPcaId">Identificador do item no plano.</param>
/// <param name="ItemCatalogoId">Item de catalogo a contratar.</param>
/// <param name="Quantidade">Quantidade pretendida.</param>
/// <param name="ValorEstimado">Valor estimado total.</param>
/// <param name="TrimestreDesejado">Trimestre desejado (1 a 4).</param>
/// <param name="Justificativa">Justificativa da necessidade.</param>
public sealed record ItemPcaDetalhe(
    Guid ItemPcaId,
    Guid ItemCatalogoId,
    decimal Quantidade,
    decimal ValorEstimado,
    int TrimestreDesejado,
    string? Justificativa);

/// <summary>Detalhe completo do PCA (itens e total estimado).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Exercicio">Ano do exercicio.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="NumeroPncp">Numero de controle no PNCP, quando publicado.</param>
/// <param name="ValorTotalEstimado">Soma dos valores estimados dos itens.</param>
/// <param name="Itens">Itens de contratacao pretendida.</param>
public sealed record PcaDetalhe(
    Guid Id,
    int Exercicio,
    string Situacao,
    string? NumeroPncp,
    decimal ValorTotalEstimado,
    IReadOnlyList<ItemPcaDetalhe> Itens);

/// <summary>Obtem o PCA de um exercicio (com itens).</summary>
/// <param name="Exercicio">Ano do exercicio.</param>
public sealed record ObterPcaPorExercicioQuery(int Exercicio) : IQuery<PcaDetalhe?>;

/// <summary>Handler do detalhe do PCA por exercicio.</summary>
public sealed class ObterPcaPorExercicioHandler(IPcaRepository planos)
    : IQueryHandler<ObterPcaPorExercicioQuery, PcaDetalhe?>
{
    /// <inheritdoc />
    public async Task<PcaDetalhe?> Handle(ObterPcaPorExercicioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plano = await planos.ObterPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        if (plano is null)
        {
            return null;
        }

        return new PcaDetalhe(
            plano.Id.Value,
            plano.Exercicio,
            plano.Situacao.ToString(),
            plano.NumeroPncp,
            plano.ValorTotalEstimado.Valor,
            plano.Itens.Select(i => new ItemPcaDetalhe(
                i.Id.Value,
                i.ItemCatalogoId.Value,
                i.Quantidade,
                i.ValorEstimado.Valor,
                i.TrimestreDesejado,
                i.Justificativa)).ToList());
    }
}

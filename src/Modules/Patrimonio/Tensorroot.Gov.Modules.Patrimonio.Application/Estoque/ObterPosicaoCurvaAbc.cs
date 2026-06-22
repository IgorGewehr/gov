using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>Posição agregada de itens por classe da Curva ABC.</summary>
/// <param name="Classe">Classe ABC (A/B/C).</param>
/// <param name="QuantidadeItens">Quantidade de itens na classe.</param>
/// <param name="ValorTotal">Valor total estocado na classe (saldo × custo médio).</param>
public sealed record PosicaoAbcResumo(
    string Classe,
    int QuantidadeItens,
    decimal ValorTotal);

/// <summary>Obtém a posição agregada dos itens ativos por classe da Curva ABC (tenant-scoped).</summary>
public sealed record ObterPosicaoCurvaAbcQuery() : IQuery<IReadOnlyList<PosicaoAbcResumo>>;

/// <summary>Handler da consulta de posição da Curva ABC.</summary>
public sealed class ObterPosicaoCurvaAbcHandler(IItemEstoqueRepository itens)
    : IQueryHandler<ObterPosicaoCurvaAbcQuery, IReadOnlyList<PosicaoAbcResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PosicaoAbcResumo>> Handle(
        ObterPosicaoCurvaAbcQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ativos = await itens.ListarAtivosAsync(cancellationToken).ConfigureAwait(false);

        return ativos
            .GroupBy(item => item.ClassificacaoAbc)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new PosicaoAbcResumo(
                grupo.Key.ToString(),
                grupo.Count(),
                grupo.Sum(item => item.Saldo.Quantidade * item.CustoMedio.Valor)))
            .ToList();
    }
}

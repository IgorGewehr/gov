using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Resumo de uma movimentação patrimonial para leitura.</summary>
/// <param name="Id">Identificador da movimentação.</param>
/// <param name="LocalizacaoOrigem">Localização de origem.</param>
/// <param name="LocalizacaoDestino">Localização de destino.</param>
/// <param name="ResponsavelId">Responsável de destino.</param>
/// <param name="Data">Data da movimentação.</param>
public sealed record MovimentacaoResumo(
    Guid Id,
    string LocalizacaoOrigem,
    string LocalizacaoDestino,
    Guid ResponsavelId,
    DateOnly Data);

/// <summary>Lista as movimentações de um bem patrimonial (tenant-scoped).</summary>
/// <param name="BemPatrimonialId">Bem a consultar.</param>
public sealed record ListarMovimentacoesDoBemQuery(Guid BemPatrimonialId)
    : IQuery<IReadOnlyList<MovimentacaoResumo>>;

/// <summary>Handler da consulta de movimentações do bem.</summary>
public sealed class ListarMovimentacoesDoBemHandler(IBemPatrimonialRepository bens)
    : IQueryHandler<ListarMovimentacoesDoBemQuery, IReadOnlyList<MovimentacaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MovimentacaoResumo>> Handle(
        ListarMovimentacoesDoBemQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        return bem.Movimentacoes
            .Select(movimentacao => new MovimentacaoResumo(
                movimentacao.Id.Value,
                movimentacao.LocalizacaoOrigem,
                movimentacao.LocalizacaoDestino,
                movimentacao.ResponsavelId,
                movimentacao.Data))
            .ToList();
    }
}

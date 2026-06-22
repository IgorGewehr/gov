using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Projeção de detalhe de um bem patrimonial para leitura.</summary>
/// <param name="Id">Identificador do bem.</param>
/// <param name="NumeroTombamento">Número de tombo, se tombado.</param>
/// <param name="Descricao">Descrição do bem.</param>
/// <param name="Tipo">Tipo do bem.</param>
/// <param name="ValorInicial">Valor de incorporação.</param>
/// <param name="ValorResidual">Resíduo estimado.</param>
/// <param name="ValorContabil">Valor contábil atual.</param>
/// <param name="VidaUtilMeses">Vida útil em meses.</param>
/// <param name="DataIncorporacao">Data de incorporação.</param>
/// <param name="Situacao">Situação atual.</param>
public sealed record BemPatrimonialDetalhe(
    Guid Id,
    string? NumeroTombamento,
    string Descricao,
    string Tipo,
    decimal ValorInicial,
    decimal ValorResidual,
    decimal ValorContabil,
    int VidaUtilMeses,
    DateOnly DataIncorporacao,
    string Situacao);

/// <summary>Obtém o detalhe de um bem patrimonial (tenant-scoped).</summary>
/// <param name="BemPatrimonialId">Bem a consultar.</param>
public sealed record ObterBemPatrimonialQuery(Guid BemPatrimonialId) : IQuery<BemPatrimonialDetalhe>;

/// <summary>Handler da consulta de detalhe do bem patrimonial.</summary>
public sealed class ObterBemPatrimonialHandler(IBemPatrimonialRepository bens)
    : IQueryHandler<ObterBemPatrimonialQuery, BemPatrimonialDetalhe>
{
    /// <inheritdoc />
    public async Task<BemPatrimonialDetalhe> Handle(ObterBemPatrimonialQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        return new BemPatrimonialDetalhe(
            bem.Id.Value,
            bem.NumeroTombamento?.Valor,
            bem.Descricao,
            bem.Tipo.ToString(),
            bem.ValorInicial.Valor,
            bem.ValorResidual.Valor,
            bem.ValorContabil.Valor,
            bem.VidaUtilMeses,
            bem.DataIncorporacao,
            bem.Situacao.ToString());
    }
}

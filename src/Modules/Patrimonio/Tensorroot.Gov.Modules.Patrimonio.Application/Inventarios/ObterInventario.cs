using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Membro da comissão (leitura).</summary>
/// <param name="ResponsavelId">Vínculo do servidor designado.</param>
/// <param name="Nome">Nome do membro.</param>
/// <param name="Presidente">Indica se preside a comissão.</param>
public sealed record MembroComissaoDto(Guid ResponsavelId, string Nome, bool Presidente);

/// <summary>Linha do inventário (leitura): snapshot congelado + contagem física.</summary>
/// <param name="Id">Identificador da linha.</param>
/// <param name="BemPatrimonialId">Bem do acervo.</param>
/// <param name="NumeroTombamento">Tombo (nulo se não tombado).</param>
/// <param name="DescricaoSnapshot">Descrição congelada.</param>
/// <param name="LocalizacaoEsperada">Localização esperada.</param>
/// <param name="ValorContabilSnapshot">Valor contábil congelado.</param>
/// <param name="SituacaoEncontrada">Situação física apurada (nula se não contado).</param>
/// <param name="LocalizacaoEncontrada">Localização encontrada.</param>
/// <param name="Contado">Indica se já foi conferido.</param>
public sealed record ItemInventarioDto(
    Guid Id,
    Guid BemPatrimonialId,
    string? NumeroTombamento,
    string DescricaoSnapshot,
    string? LocalizacaoEsperada,
    decimal ValorContabilSnapshot,
    string? SituacaoEncontrada,
    string? LocalizacaoEncontrada,
    bool Contado);

/// <summary>Detalhe de um inventário (ficha + comissão + itens + divergências).</summary>
/// <param name="Id">Identificador do inventário.</param>
/// <param name="Exercicio">Exercício (ano-base).</param>
/// <param name="Tipo">Tipo do inventário.</param>
/// <param name="Setor">Setor escopo.</param>
/// <param name="Portaria">Portaria de designação.</param>
/// <param name="Situacao">Situação no levantamento.</param>
/// <param name="DataAbertura">Data de abertura.</param>
/// <param name="DataEncerramento">Data de encerramento (se encerrado).</param>
/// <param name="Comissao">Membros da comissão.</param>
/// <param name="Itens">Linhas do inventário.</param>
/// <param name="Divergencias">Divergências apuradas.</param>
public sealed record InventarioDetalhe(
    Guid Id,
    int Exercicio,
    string Tipo,
    string? Setor,
    string Portaria,
    string Situacao,
    DateOnly DataAbertura,
    DateOnly? DataEncerramento,
    IReadOnlyList<MembroComissaoDto> Comissao,
    IReadOnlyList<ItemInventarioDto> Itens,
    IReadOnlyList<DivergenciaInventarioDto> Divergencias);

/// <summary>Obtém o detalhe de um inventário (tenant-scoped).</summary>
/// <param name="InventarioId">Inventário a consultar.</param>
public sealed record ObterInventarioQuery(Guid InventarioId) : IQuery<InventarioDetalhe>;

/// <summary>Handler da consulta de detalhe do inventário.</summary>
public sealed class ObterInventarioHandler(IInventarioRepository inventarios)
    : IQueryHandler<ObterInventarioQuery, InventarioDetalhe>
{
    /// <inheritdoc />
    public async Task<InventarioDetalhe> Handle(ObterInventarioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventario = await inventarios.ObterPorIdAsync(new InventarioId(request.InventarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventário não encontrado.");

        return new InventarioDetalhe(
            inventario.Id.Value,
            inventario.Exercicio,
            inventario.Tipo.ToString(),
            inventario.Setor,
            inventario.Portaria,
            inventario.Situacao.ToString(),
            inventario.DataAbertura,
            inventario.DataEncerramento,
            inventario.Comissao.Select(m => new MembroComissaoDto(m.ResponsavelId, m.Nome, m.Presidente)).ToList(),
            inventario.Itens.Select(i => new ItemInventarioDto(
                i.Id.Value,
                i.BemPatrimonialId.Value,
                i.NumeroTombamento,
                i.DescricaoSnapshot,
                i.LocalizacaoEsperada,
                i.ValorContabilSnapshot.Valor,
                i.SituacaoEncontrada?.ToString(),
                i.LocalizacaoEncontrada,
                i.Contado)).ToList(),
            inventario.Divergencias.Select(d => new DivergenciaInventarioDto(
                d.Id.Value,
                d.Tipo.ToString(),
                d.BemPatrimonialId?.Value,
                d.Descricao,
                d.Recomendacao.ToString())).ToList());
    }
}

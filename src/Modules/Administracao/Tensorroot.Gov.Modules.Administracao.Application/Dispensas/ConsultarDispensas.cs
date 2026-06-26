using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>Resumo de um item da dispensa para a projecao de detalhe.</summary>
/// <param name="ItemId">Identificador do item.</param>
/// <param name="Numero">Numero do item.</param>
/// <param name="ItemCatalogoId">Referencia ao catalogo, quando padronizado.</param>
/// <param name="Descricao">Descricao do objeto do item.</param>
/// <param name="Quantidade">Quantidade demandada.</param>
/// <param name="ValorUnitarioEstimado">Valor unitario estimado.</param>
/// <param name="ValorTotalEstimado">Valor total estimado do item.</param>
public sealed record ItemDispensaResumo(
    Guid ItemId,
    int Numero,
    Guid? ItemCatalogoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitarioEstimado,
    decimal ValorTotalEstimado);

/// <summary>Resumo de uma cotacao da dispensa para a projecao de detalhe.</summary>
/// <param name="CotacaoId">Identificador da cotacao.</param>
/// <param name="FornecedorId">Fornecedor proponente.</param>
/// <param name="ItemId">Item cotado.</param>
/// <param name="Valor">Valor ofertado.</param>
/// <param name="Classificacao">Classificacao na disputa.</param>
/// <param name="Situacao">Situacao da cotacao.</param>
public sealed record CotacaoDispensaResumo(
    Guid CotacaoId,
    Guid FornecedorId,
    Guid ItemId,
    decimal Valor,
    int? Classificacao,
    string Situacao);

/// <summary>Projecao de detalhe de uma dispensa eletronica para leitura.</summary>
/// <param name="Id">Identificador da dispensa.</param>
/// <param name="Objeto">Descricao do objeto.</param>
/// <param name="Fundamento">Fundamento legal (art. 75, I ou II).</param>
/// <param name="CriterioJulgamento">Criterio de julgamento.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="ValorTotalEstimado">Valor total estimado da dispensa.</param>
/// <param name="LimiteLegalVigente">Limite de dispensa vigente aplicado.</param>
/// <param name="LimiteLegalNormaFonte">Norma-fonte do limite aplicado.</param>
/// <param name="NumeroAviso">Numero do aviso de contratacao direta, quando publicado.</param>
/// <param name="AberturaDisputa">Data/hora de abertura da disputa, quando definida.</param>
/// <param name="NumeroPncp">Identificador no PNCP, quando publicado.</param>
/// <param name="CotacaoVencedoraId">Cotacao vencedora indicada, se houver.</param>
/// <param name="Itens">Itens da dispensa.</param>
/// <param name="Cotacoes">Cotacoes (lances) recebidas.</param>
public sealed record DispensaDetalhe(
    Guid Id,
    string Objeto,
    string Fundamento,
    string CriterioJulgamento,
    string Situacao,
    decimal ValorTotalEstimado,
    decimal LimiteLegalVigente,
    string LimiteLegalNormaFonte,
    string? NumeroAviso,
    DateTimeOffset? AberturaDisputa,
    string? NumeroPncp,
    Guid? CotacaoVencedoraId,
    IReadOnlyList<ItemDispensaResumo> Itens,
    IReadOnlyList<CotacaoDispensaResumo> Cotacoes);

/// <summary>Resumo de uma dispensa para listagem.</summary>
/// <param name="Id">Identificador da dispensa.</param>
/// <param name="Objeto">Descricao do objeto.</param>
/// <param name="Fundamento">Fundamento legal.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="ValorTotalEstimado">Valor total estimado.</param>
/// <param name="NumeroAviso">Numero do aviso, quando publicado.</param>
public sealed record DispensaResumo(
    Guid Id,
    string Objeto,
    string Fundamento,
    string Situacao,
    decimal ValorTotalEstimado,
    string? NumeroAviso);

/// <summary>Obtem o detalhe de uma dispensa (tenant-scoped).</summary>
/// <param name="DispensaId">Dispensa a consultar.</param>
public sealed record ObterDispensaPorIdQuery(Guid DispensaId) : IQuery<DispensaDetalhe?>;

/// <summary>Lista as dispensas do tenant por situacao.</summary>
/// <param name="Situacao">Situacao a filtrar.</param>
public sealed record ListarDispensasPorSituacaoQuery(SituacaoDispensa Situacao) : IQuery<IReadOnlyList<DispensaResumo>>;

/// <summary>Handler da consulta de detalhe da dispensa.</summary>
public sealed class ObterDispensaPorIdHandler(IDispensaRepository dispensas)
    : IQueryHandler<ObterDispensaPorIdQuery, DispensaDetalhe?>
{
    /// <inheritdoc />
    public async Task<DispensaDetalhe?> Handle(ObterDispensaPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false);
        if (dispensa is null)
        {
            return null;
        }

        return new DispensaDetalhe(
            dispensa.Id.Value,
            dispensa.Objeto,
            dispensa.Fundamento.ToString(),
            dispensa.CriterioJulgamento.ToString(),
            dispensa.Situacao.ToString(),
            dispensa.ValorTotalEstimado.Valor,
            dispensa.LimiteLegalVigente.Valor,
            dispensa.LimiteLegalNormaFonte,
            dispensa.NumeroAviso,
            dispensa.AberturaDisputa,
            dispensa.NumeroPncp,
            dispensa.CotacaoVencedoraId,
            dispensa.Itens
                .Select(item => new ItemDispensaResumo(
                    item.Id.Value,
                    item.Numero,
                    item.ItemCatalogoId,
                    item.Descricao,
                    item.Quantidade,
                    item.ValorUnitarioEstimado.Valor,
                    item.ValorTotalEstimado.Valor))
                .ToList(),
            dispensa.Cotacoes
                .Select(cotacao => new CotacaoDispensaResumo(
                    cotacao.Id.Value,
                    cotacao.FornecedorId,
                    cotacao.ItemId.Value,
                    cotacao.Valor.Valor,
                    cotacao.Classificacao,
                    cotacao.Situacao.ToString()))
                .ToList());
    }
}

/// <summary>Handler da listagem de dispensas por situacao.</summary>
public sealed class ListarDispensasPorSituacaoHandler(IDispensaRepository dispensas)
    : IQueryHandler<ListarDispensasPorSituacaoQuery, IReadOnlyList<DispensaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DispensaResumo>> Handle(ListarDispensasPorSituacaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensas2 = await dispensas.ListarPorSituacaoAsync(request.Situacao, cancellationToken).ConfigureAwait(false);

        return dispensas2
            .Select(dispensa => new DispensaResumo(
                dispensa.Id.Value,
                dispensa.Objeto,
                dispensa.Fundamento.ToString(),
                dispensa.Situacao.ToString(),
                dispensa.ValorTotalEstimado.Valor,
                dispensa.NumeroAviso))
            .ToList();
    }
}

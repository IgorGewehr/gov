using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Retencoes;

/// <summary>Faixa da tabela de IRRF/PJ para exibição.</summary>
/// <param name="Codigo">Código do enquadramento.</param>
/// <param name="Descricao">Descrição da natureza.</param>
/// <param name="Aliquota">Alíquota (fração).</param>
/// <param name="CodigoReceitaDarf">Código de receita DARF.</param>
public sealed record FaixaIrrfDto(string Codigo, string Descricao, decimal Aliquota, string CodigoReceitaDarf);

/// <summary>Tabela de IRRF/PJ vigente para exibição.</summary>
/// <param name="VigenciaInicio">Início de vigência.</param>
/// <param name="VigenciaFim">Fim de vigência.</param>
/// <param name="ValorMinimoRetencao">Valor mínimo a reter (dispensa abaixo).</param>
/// <param name="Faixas">Faixas/enquadramentos.</param>
public sealed record TabelaIrrfDto(
    DateOnly VigenciaInicio,
    DateOnly? VigenciaFim,
    decimal ValorMinimoRetencao,
    IReadOnlyList<FaixaIrrfDto> Faixas);

/// <summary>Consulta a tabela de IRRF/PJ vigente em uma data.</summary>
/// <param name="Data">Data de referência.</param>
public sealed record ConsultarTabelaIrrfQuery(DateOnly Data) : IQuery<TabelaIrrfDto?>;

/// <summary>Handler da consulta à tabela de IRRF/PJ.</summary>
public sealed class ConsultarTabelaIrrfHandler(ITabelaIrrfServicosRepository tabelas)
    : IQueryHandler<ConsultarTabelaIrrfQuery, TabelaIrrfDto?>
{
    /// <inheritdoc />
    public async Task<TabelaIrrfDto?> Handle(ConsultarTabelaIrrfQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tabela = await tabelas.ObterVigenteAsync(request.Data, cancellationToken).ConfigureAwait(false);
        if (tabela is null)
        {
            return null;
        }

        return new TabelaIrrfDto(
            tabela.VigenciaInicio,
            tabela.VigenciaFim,
            tabela.ValorMinimoRetencao,
            tabela.Faixas.Select(f => new FaixaIrrfDto(f.Codigo, f.Descricao, f.Aliquota, f.CodigoReceitaDarf)).ToList());
    }
}

/// <summary>Item de guia para exibição.</summary>
/// <param name="GuiaRecolhimentoId">Identificador.</param>
/// <param name="Natureza">Natureza.</param>
/// <param name="CodigoReceita">Código de receita.</param>
/// <param name="ValorTotal">Valor total.</param>
/// <param name="Competencia">Competência.</param>
/// <param name="DataVencimento">Vencimento.</param>
/// <param name="Situacao">Situação.</param>
/// <param name="DataRecolhimento">Data do recolhimento.</param>
/// <param name="QuantidadeItens">Quantidade de retenções.</param>
public sealed record GuiaRecolhimentoDto(
    Guid GuiaRecolhimentoId,
    NaturezaRetencao Natureza,
    string? CodigoReceita,
    decimal ValorTotal,
    DateOnly Competencia,
    DateOnly DataVencimento,
    SituacaoGuiaRecolhimento Situacao,
    DateOnly? DataRecolhimento,
    int QuantidadeItens);

/// <summary>Lista guias de recolhimento (filtro opcional por situação).</summary>
/// <param name="Situacao">Situação (nula = todas).</param>
public sealed record ListarGuiasRecolhimentoQuery(SituacaoGuiaRecolhimento? Situacao) : IQuery<IReadOnlyList<GuiaRecolhimentoDto>>;

/// <summary>Handler da listagem de guias.</summary>
public sealed class ListarGuiasRecolhimentoHandler(IGuiaRecolhimentoRepository guias)
    : IQueryHandler<ListarGuiasRecolhimentoQuery, IReadOnlyList<GuiaRecolhimentoDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<GuiaRecolhimentoDto>> Handle(ListarGuiasRecolhimentoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lista = await guias.ListarAsync(request.Situacao, cancellationToken).ConfigureAwait(false);
        return lista.Select(g => new GuiaRecolhimentoDto(
            g.Id.Value,
            g.Natureza,
            g.CodigoReceita,
            g.ValorTotal.Valor,
            g.Competencia,
            g.DataVencimento,
            g.Situacao,
            g.DataRecolhimento,
            g.Itens.Count)).ToList();
    }
}

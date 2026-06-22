using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Resumo de um aditivo na projecao de detalhe do contrato.</summary>
/// <param name="AditivoId">Identificador do aditivo.</param>
/// <param name="Numero">Numero sequencial.</param>
/// <param name="Tipo">Tipo do aditivo.</param>
/// <param name="Percentual">Percentual sobre o valor original.</param>
/// <param name="PublicadoNoPncp">Eficacia obtida pela publicacao no PNCP.</param>
public sealed record AditivoResumo(Guid AditivoId, int Numero, string Tipo, decimal Percentual, bool PublicadoNoPncp);

/// <summary>Resumo de uma garantia na projecao de detalhe do contrato.</summary>
/// <param name="GarantiaId">Identificador da garantia.</param>
/// <param name="Modalidade">Modalidade da garantia.</param>
/// <param name="Percentual">Percentual sobre o valor.</param>
/// <param name="Valor">Valor prestado.</param>
/// <param name="ValidadeFim">Data-fim de validade.</param>
public sealed record GarantiaResumo(Guid GarantiaId, string Modalidade, decimal Percentual, decimal Valor, DateOnly ValidadeFim);

/// <summary>Projecao de detalhe de um contrato para leitura.</summary>
/// <param name="Id">Identificador do contrato.</param>
/// <param name="LicitacaoId">Licitacao de origem, se houver.</param>
/// <param name="FornecedorId">Fornecedor contratado.</param>
/// <param name="Origem">Fundamento da contratacao.</param>
/// <param name="Objeto">Descricao do objeto.</param>
/// <param name="ValorContratado">Valor global original.</param>
/// <param name="ValorAtual">Valor vigente apos alteracoes.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="PublicadoNoPncp">Eficacia obtida pela publicacao no PNCP.</param>
/// <param name="DotacaoConfirmada">Cobertura orcamentaria confirmada.</param>
/// <param name="NumeroContratoPncp">Identificador no PNCP, quando publicado.</param>
/// <param name="Aditivos">Aditivos do contrato.</param>
/// <param name="Garantias">Garantias do contrato.</param>
public sealed record ContratoDetalhe(
    Guid Id,
    Guid? LicitacaoId,
    Guid FornecedorId,
    string Origem,
    string Objeto,
    decimal ValorContratado,
    decimal ValorAtual,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    string Situacao,
    bool PublicadoNoPncp,
    bool DotacaoConfirmada,
    string? NumeroContratoPncp,
    IReadOnlyList<AditivoResumo> Aditivos,
    IReadOnlyList<GarantiaResumo> Garantias);

/// <summary>Obtem o detalhe de um contrato (tenant-scoped).</summary>
/// <param name="ContratoId">Contrato a consultar.</param>
public sealed record ObterContratoPorIdQuery(Guid ContratoId) : IQuery<ContratoDetalhe?>;

/// <summary>Handler da consulta de detalhe do contrato.</summary>
public sealed class ObterContratoPorIdHandler(IContratoRepository contratos)
    : IQueryHandler<ObterContratoPorIdQuery, ContratoDetalhe?>
{
    /// <inheritdoc />
    public async Task<ContratoDetalhe?> Handle(ObterContratoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false);
        if (contrato is null)
        {
            return null;
        }

        return new ContratoDetalhe(
            contrato.Id.Value,
            contrato.LicitacaoId,
            contrato.FornecedorId,
            contrato.OrigemContratacao.ToString(),
            contrato.Objeto,
            contrato.ValorContratado.Valor,
            contrato.ValorAtual.Valor,
            contrato.VigenciaInicio,
            contrato.VigenciaFim,
            contrato.Situacao.ToString(),
            contrato.PublicadoNoPncp,
            contrato.DotacaoConfirmada,
            contrato.NumeroContratoPncp,
            contrato.Aditivos
                .Select(a => new AditivoResumo(a.Id.Value, a.Numero, a.Tipo.ToString(), a.PercentualSobreValorOriginal, a.PublicadoNoPncp))
                .ToList(),
            contrato.Garantias
                .Select(g => new GarantiaResumo(g.Id.Value, g.Modalidade.ToString(), g.Percentual, g.Valor.Valor, g.ValidadeFim))
                .ToList());
    }
}

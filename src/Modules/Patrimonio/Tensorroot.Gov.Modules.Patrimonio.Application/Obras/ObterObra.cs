using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>Projeção de uma etapa do cronograma para leitura.</summary>
/// <param name="Id">Identificador da etapa.</param>
/// <param name="Ordem">Ordem sequencial.</param>
/// <param name="Descricao">Descrição.</param>
/// <param name="PercentualFisicoPrevisto">Peso físico previsto.</param>
/// <param name="ValorPrevisto">Valor previsto.</param>
/// <param name="PercentualFisicoExecutado">Percentual físico executado.</param>
/// <param name="ValorMedido">Valor medido acumulado.</param>
/// <param name="Situacao">Situação da etapa.</param>
public sealed record EtapaCronogramaDto(
    Guid Id,
    int Ordem,
    string Descricao,
    decimal PercentualFisicoPrevisto,
    decimal ValorPrevisto,
    decimal PercentualFisicoExecutado,
    decimal ValorMedido,
    string Situacao);

/// <summary>Projeção de uma medição para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Numero">Número sequencial.</param>
/// <param name="CompetenciaAno">Ano da competência.</param>
/// <param name="CompetenciaMes">Mês da competência.</param>
/// <param name="PeriodoInicio">Início do período.</param>
/// <param name="PeriodoFim">Fim do período.</param>
/// <param name="ValorMedido">Valor medido.</param>
/// <param name="PercentualFisicoNoPeriodo">Avanço físico no período.</param>
/// <param name="Situacao">Situação da medição.</param>
/// <param name="DataAprovacao">Data de aprovação (se aprovada).</param>
public sealed record MedicaoDto(
    Guid Id,
    int Numero,
    int CompetenciaAno,
    int CompetenciaMes,
    DateOnly PeriodoInicio,
    DateOnly PeriodoFim,
    decimal ValorMedido,
    decimal PercentualFisicoNoPeriodo,
    string Situacao,
    DateOnly? DataAprovacao);

/// <summary>Projeção de um RDO para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Data">Data do RDO.</param>
/// <param name="CondicaoTempo">Condição de tempo.</param>
/// <param name="EfetivoMaoDeObra">Efetivo de mão de obra.</param>
/// <param name="AtividadesExecutadas">Atividades executadas.</param>
/// <param name="Ocorrencias">Ocorrências (se houver).</param>
public sealed record RdoDto(
    Guid Id,
    DateOnly Data,
    string CondicaoTempo,
    int EfetivoMaoDeObra,
    string AtividadesExecutadas,
    string? Ocorrencias);

/// <summary>Projeção de uma ocorrência de fiscalização para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Data">Data.</param>
/// <param name="Tipo">Tipo.</param>
/// <param name="Descricao">Descrição.</param>
public sealed record OcorrenciaDto(Guid Id, DateOnly Data, string Tipo, string Descricao);

/// <summary>Detalhe completo de uma obra (ficha) para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="ContratoId">Contrato de origem.</param>
/// <param name="FornecedorId">Contratada.</param>
/// <param name="BemPatrimonialId">Bem patrimonial resultante (se incorporada).</param>
/// <param name="Objeto">Objeto da obra.</param>
/// <param name="Municipio">Município.</param>
/// <param name="Uf">UF.</param>
/// <param name="RegimeExecucao">Regime de execução.</param>
/// <param name="ValorContratado">Valor contratado.</param>
/// <param name="ValorMedidoAcumulado">Valor medido acumulado.</param>
/// <param name="PercentualFisicoAcumulado">Percentual físico acumulado.</param>
/// <param name="Situacao">Situação.</param>
/// <param name="DataAssinaturaContrato">Data de assinatura.</param>
/// <param name="DataInicioOrdemServico">Data da ordem de início.</param>
/// <param name="DataConclusao">Data de conclusão.</param>
/// <param name="FiscalDesignadoId">Fiscal designado vigente.</param>
/// <param name="Etapas">Cronograma.</param>
/// <param name="Medicoes">Medições.</param>
/// <param name="RegistrosDiarios">RDOs.</param>
/// <param name="Ocorrencias">Ocorrências de fiscalização.</param>
public sealed record ObraDetalhe(
    Guid Id,
    Guid ContratoId,
    Guid FornecedorId,
    Guid? BemPatrimonialId,
    string Objeto,
    string Municipio,
    string Uf,
    string RegimeExecucao,
    decimal ValorContratado,
    decimal ValorMedidoAcumulado,
    decimal PercentualFisicoAcumulado,
    string Situacao,
    DateOnly DataAssinaturaContrato,
    DateOnly? DataInicioOrdemServico,
    DateOnly? DataConclusao,
    Guid? FiscalDesignadoId,
    IReadOnlyList<EtapaCronogramaDto> Etapas,
    IReadOnlyList<MedicaoDto> Medicoes,
    IReadOnlyList<RdoDto> RegistrosDiarios,
    IReadOnlyList<OcorrenciaDto> Ocorrencias);

/// <summary>Obtém o detalhe (ficha) de uma obra (tenant-scoped).</summary>
/// <param name="ObraId">Obra alvo.</param>
public sealed record ObterObraQuery(Guid ObraId) : IQuery<ObraDetalhe?>;

/// <summary>Handler da consulta de detalhe da obra.</summary>
public sealed class ObterObraHandler(IObraRepository obras) : IQueryHandler<ObterObraQuery, ObraDetalhe?>
{
    /// <inheritdoc />
    public async Task<ObraDetalhe?> Handle(ObterObraQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false);
        if (obra is null)
        {
            return null;
        }

        return new ObraDetalhe(
            obra.Id.Value,
            obra.ContratoId,
            obra.FornecedorId,
            obra.BemPatrimonialId?.Value,
            obra.Objeto,
            obra.Localizacao.Municipio,
            obra.Localizacao.Uf,
            obra.RegimeExecucao.ToString(),
            obra.ValorContratado.Valor,
            obra.ValorMedidoAcumulado.Valor,
            obra.PercentualFisicoAcumulado,
            obra.Situacao.ToString(),
            obra.DataAssinaturaContrato,
            obra.DataInicioOrdemServico,
            obra.DataConclusao,
            obra.FiscalDesignadoId,
            [.. obra.Etapas.OrderBy(etapa => etapa.Ordem).Select(etapa => new EtapaCronogramaDto(
                etapa.Id.Value,
                etapa.Ordem,
                etapa.Descricao,
                etapa.PercentualFisicoPrevisto,
                etapa.ValorPrevisto.Valor,
                etapa.PercentualFisicoExecutado,
                etapa.ValorMedido.Valor,
                etapa.Situacao.ToString()))],
            [.. obra.Medicoes.OrderBy(medicao => medicao.Numero).Select(medicao => new MedicaoDto(
                medicao.Id.Value,
                medicao.Numero,
                medicao.CompetenciaAno,
                medicao.CompetenciaMes,
                medicao.PeriodoInicio,
                medicao.PeriodoFim,
                medicao.ValorMedido.Valor,
                medicao.PercentualFisicoNoPeriodo,
                medicao.Situacao.ToString(),
                medicao.DataAprovacao))],
            [.. obra.RegistrosDiarios.OrderBy(rdo => rdo.Data).Select(rdo => new RdoDto(
                rdo.Id.Value,
                rdo.Data,
                rdo.CondicaoTempo,
                rdo.EfetivoMaoDeObra,
                rdo.AtividadesExecutadas,
                rdo.Ocorrencias))],
            [.. obra.Ocorrencias.OrderBy(ocorrencia => ocorrencia.Data).Select(ocorrencia => new OcorrenciaDto(
                ocorrencia.Id.Value,
                ocorrencia.Data,
                ocorrencia.Tipo.ToString(),
                ocorrencia.Descricao))]);
    }
}

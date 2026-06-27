using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Credores;

namespace Tensorroot.Gov.Modules.Financas.Application.Credores;

/// <summary>Resumo de um credor cadastrado.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nome">Nome/razão social.</param>
/// <param name="Tipo">Tipo de pessoa.</param>
/// <param name="Documento">Documento normalizado.</param>
/// <param name="Banco">Banco (se houver dados bancários).</param>
/// <param name="Agencia">Agência.</param>
/// <param name="Conta">Conta.</param>
/// <param name="Pix">Chave PIX.</param>
/// <param name="Situacao">Situação.</param>
public sealed record CredorResumo(
    Guid Id,
    string Nome,
    string Tipo,
    string Documento,
    string? Banco,
    string? Agencia,
    string? Conta,
    string? Pix,
    string Situacao);

/// <summary>Extrato consolidado do credor (cabeçalho + empenhos + retenções + totais).</summary>
/// <param name="Credor">Dados do credor.</param>
/// <param name="Itens">Empenhos com execução acumulada.</param>
/// <param name="Retencoes">Retenções/consignações do credor agrupadas por natureza.</param>
/// <param name="TotalEmpenhado">Soma do empenhado (líquido de anulações).</param>
/// <param name="TotalLiquidado">Soma do liquidado.</param>
/// <param name="TotalPago">Soma do pago.</param>
/// <param name="SaldoAPagar">Liquidado − Pago (bruto, antes das retenções em aberto).</param>
/// <param name="TotalRetido">Soma de tudo o que foi retido do credor (todas as naturezas).</param>
/// <param name="TotalRetidoAReter">Soma das retenções ainda não recolhidas a terceiros.</param>
public sealed record CredorExtrato(
    CredorResumo Credor,
    IReadOnlyList<CredorExtratoItem> Itens,
    IReadOnlyList<CredorRetencaoResumo> Retencoes,
    decimal TotalEmpenhado,
    decimal TotalLiquidado,
    decimal TotalPago,
    decimal SaldoAPagar,
    decimal TotalRetido,
    decimal TotalRetidoAReter);

/// <summary>Lista credores do tenant (filtro opcional por termo nome/documento).</summary>
/// <param name="Termo">Termo de busca; <c>null</c> = todos.</param>
public sealed record ListarCredoresQuery(string? Termo) : IQuery<IReadOnlyList<CredorResumo>>;

/// <summary>Obtém um credor por identificador.</summary>
/// <param name="CredorId">Identificador.</param>
public sealed record ObterCredorQuery(Guid CredorId) : IQuery<CredorResumo?>;

/// <summary>Obtém o extrato consolidado de um credor (empenhos/liquidações/pagamentos).</summary>
/// <param name="CredorId">Identificador do credor.</param>
/// <param name="Exercicio">Exercício a filtrar; <c>null</c> = todos.</param>
public sealed record ObterExtratoCredorQuery(Guid CredorId, int? Exercicio) : IQuery<CredorExtrato?>;

/// <summary>Mapeamento de credores para DTO.</summary>
internal static class CredorMapper
{
    public static CredorResumo Mapear(CredorCadastrado c) => new(
        c.Id.Value,
        c.Nome,
        c.Tipo.ToString(),
        c.Documento,
        c.DadosBancarios?.Banco,
        c.DadosBancarios?.Agencia,
        c.DadosBancarios?.Conta,
        c.DadosBancarios?.Pix,
        c.Situacao.ToString());
}

/// <summary>Handler da listagem de credores.</summary>
public sealed class ListarCredoresHandler(ICredorRepository credores)
    : IQueryHandler<ListarCredoresQuery, IReadOnlyList<CredorResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CredorResumo>> Handle(ListarCredoresQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lista = await credores.ListarAsync(request.Termo, cancellationToken).ConfigureAwait(false);
        return lista.Select(CredorMapper.Mapear).ToList();
    }
}

/// <summary>Handler da obtenção de credor por id.</summary>
public sealed class ObterCredorHandler(ICredorRepository credores)
    : IQueryHandler<ObterCredorQuery, CredorResumo?>
{
    /// <inheritdoc />
    public async Task<CredorResumo?> Handle(ObterCredorQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credor = await credores.ObterPorIdAsync(new CredorId(request.CredorId), cancellationToken).ConfigureAwait(false);
        return credor is null ? null : CredorMapper.Mapear(credor);
    }
}

/// <summary>Handler do extrato consolidado do credor.</summary>
public sealed class ObterExtratoCredorHandler(
    ICredorRepository credores,
    ICredorExtratoConsulta extratoConsulta) : IQueryHandler<ObterExtratoCredorQuery, CredorExtrato?>
{
    /// <inheritdoc />
    public async Task<CredorExtrato?> Handle(ObterExtratoCredorQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var credor = await credores.ObterPorIdAsync(new CredorId(request.CredorId), cancellationToken).ConfigureAwait(false);
        if (credor is null)
        {
            return null;
        }

        var itens = await extratoConsulta
            .ListarEmpenhosDoCredorAsync(credor.Documento, request.Exercicio, cancellationToken)
            .ConfigureAwait(false);

        var retencoes = await extratoConsulta
            .ListarRetencoesDoCredorAsync(credor.Documento, request.Exercicio, cancellationToken)
            .ConfigureAwait(false);

        var totalEmpenhado = itens.Sum(i => i.ValorEmpenhado - i.ValorAnulado);
        var totalLiquidado = itens.Sum(i => i.ValorLiquidado);
        var totalPago = itens.Sum(i => i.ValorPago);

        return new CredorExtrato(
            CredorMapper.Mapear(credor),
            itens,
            retencoes,
            totalEmpenhado,
            totalLiquidado,
            totalPago,
            SaldoAPagar: totalLiquidado - totalPago,
            TotalRetido: retencoes.Sum(r => r.ValorRetido),
            TotalRetidoAReter: retencoes.Sum(r => r.ValorAReter));
    }
}

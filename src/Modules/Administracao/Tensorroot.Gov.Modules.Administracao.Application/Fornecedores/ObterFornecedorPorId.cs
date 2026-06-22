using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;

namespace Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;

/// <summary>Resumo de uma sancao para projecao de leitura.</summary>
/// <param name="Id">Identificador da sancao.</param>
/// <param name="Tipo">Tipo da sancao (nome do enum).</param>
/// <param name="DataInicio">Inicio da vigencia.</param>
/// <param name="DataFim">Termo final da vigencia (nulo = sem termo).</param>
/// <param name="ProcessoAdministrativo">Processo administrativo de embasamento.</param>
/// <param name="ValorMulta">Valor da multa, quando aplicavel.</param>
public sealed record SancaoResumo(
    Guid Id,
    string Tipo,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string ProcessoAdministrativo,
    decimal? ValorMulta);

/// <summary>Projecao de detalhe de um fornecedor para leitura.</summary>
/// <param name="Id">Identificador do fornecedor.</param>
/// <param name="Cnpj">CNPJ formatado (com mascara).</param>
/// <param name="RazaoSocial">Razao social.</param>
/// <param name="NivelCadastralSICAF">Nivel cadastral no SICAF (nome do enum).</param>
/// <param name="Situacao">Situacao cadastral (nome do enum).</param>
/// <param name="EstaImpedido">Indica se ha sancao impeditiva vigente na referencia atual.</param>
/// <param name="Sancoes">Historico de sancoes.</param>
public sealed record FornecedorDetalhe(
    Guid Id,
    string Cnpj,
    string RazaoSocial,
    string NivelCadastralSICAF,
    string Situacao,
    bool EstaImpedido,
    IReadOnlyList<SancaoResumo> Sancoes);

/// <summary>Obtem o detalhe de um fornecedor por identificador (tenant-scoped).</summary>
/// <param name="FornecedorId">Fornecedor a consultar.</param>
public sealed record ObterFornecedorPorIdQuery(Guid FornecedorId) : IQuery<FornecedorDetalhe?>;

/// <summary>Handler da consulta de detalhe do fornecedor por identificador.</summary>
public sealed class ObterFornecedorPorIdHandler(
    IFornecedorRepository fornecedores,
    TimeProvider timeProvider)
    : IQueryHandler<ObterFornecedorPorIdQuery, FornecedorDetalhe?>
{
    /// <inheritdoc />
    public async Task<FornecedorDetalhe?> Handle(ObterFornecedorPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(request.FornecedorId), cancellationToken).ConfigureAwait(false);
        if (fornecedor is null)
        {
            return null;
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return FornecedorProjecao.ParaDetalhe(fornecedor, hoje);
    }
}

/// <summary>Projecoes de leitura compartilhadas do agregado <see cref="Fornecedor"/>.</summary>
internal static class FornecedorProjecao
{
    /// <summary>Projeta um fornecedor em seu detalhe de leitura.</summary>
    /// <param name="fornecedor">Fornecedor de origem.</param>
    /// <param name="hoje">Data de referencia para apurar o impedimento.</param>
    /// <returns>Projecao <see cref="FornecedorDetalhe"/>.</returns>
    public static FornecedorDetalhe ParaDetalhe(Fornecedor fornecedor, DateOnly hoje)
        => new(
            fornecedor.Id.Value,
            fornecedor.Cnpj.Formatar(),
            fornecedor.RazaoSocial,
            fornecedor.NivelCadastralSICAF.ToString(),
            fornecedor.Situacao.ToString(),
            fornecedor.EstaImpedido(hoje),
            fornecedor.Sancoes
                .Select(sancao => new SancaoResumo(
                    sancao.Id.Value,
                    sancao.Tipo.ToString(),
                    sancao.DataInicio,
                    sancao.DataFim,
                    sancao.ProcessoAdministrativo,
                    sancao.ValorMulta?.Valor))
                .ToList());
}

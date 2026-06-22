using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;

/// <summary>Projecao de detalhe de uma declaracao fiscal para leitura (secao 6.1 das regras).</summary>
/// <param name="Id">Identificador da declaracao.</param>
/// <param name="TipoDeclaracao">Especie do demonstrativo.</param>
/// <param name="Exercicio">Ano de exercicio.</param>
/// <param name="Competencia">Competencia (MM/AAAA), quando MSC.</param>
/// <param name="Bimestre">Bimestre, quando RREO.</param>
/// <param name="Quadrimestre">Quadrimestre, quando RGF.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataLimite">Prazo legal/parametrizado de transmissao.</param>
/// <param name="DataTransmissao">Data de transmissao ao SICONFI, se transmitida.</param>
/// <param name="ProtocoloSiconfi">Protocolo retornado pelo SICONFI.</param>
/// <param name="TotalDebitos">Total dos saldos devedores da matriz.</param>
/// <param name="TotalCreditos">Total dos saldos credores da matriz.</param>
public sealed record DeclaracaoFiscalDetalhe(
    Guid Id,
    string TipoDeclaracao,
    int Exercicio,
    string? Competencia,
    string? Bimestre,
    string? Quadrimestre,
    string Situacao,
    DateOnly DataLimite,
    DateOnly? DataTransmissao,
    string? ProtocoloSiconfi,
    decimal TotalDebitos,
    decimal TotalCreditos);

/// <summary>Obtem o detalhe de uma declaracao fiscal (tenant-scoped).</summary>
/// <param name="DeclaracaoFiscalId">Declaracao a consultar.</param>
public sealed record ObterDeclaracaoFiscalPorIdQuery(Guid DeclaracaoFiscalId) : IQuery<DeclaracaoFiscalDetalhe?>;

/// <summary>Handler da consulta de detalhe da declaracao fiscal.</summary>
public sealed class ObterDeclaracaoFiscalPorIdHandler(IDeclaracaoFiscalRepository declaracoes)
    : IQueryHandler<ObterDeclaracaoFiscalPorIdQuery, DeclaracaoFiscalDetalhe?>
{
    /// <inheritdoc />
    public async Task<DeclaracaoFiscalDetalhe?> Handle(
        ObterDeclaracaoFiscalPorIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var declaracao = await declaracoes
            .ObterPorIdAsync(new DeclaracaoFiscalId(request.DeclaracaoFiscalId), cancellationToken)
            .ConfigureAwait(false);

        if (declaracao is null)
        {
            return null;
        }

        return new DeclaracaoFiscalDetalhe(
            declaracao.Id.Value,
            declaracao.TipoDeclaracao.ToString(),
            declaracao.Exercicio,
            declaracao.Competencia?.ToString(),
            declaracao.Bimestre?.ToString(),
            declaracao.Quadrimestre?.ToString(),
            declaracao.Situacao.ToString(),
            declaracao.DataLimite,
            declaracao.DataTransmissao,
            declaracao.ProtocoloSiconfi,
            declaracao.Matriz.TotalDebitos.Valor,
            declaracao.Matriz.TotalCreditos.Valor);
    }
}

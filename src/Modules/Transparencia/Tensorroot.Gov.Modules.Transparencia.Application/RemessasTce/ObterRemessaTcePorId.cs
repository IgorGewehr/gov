using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>Projeção de detalhe de uma remessa ao TCE-RS para leitura.</summary>
/// <param name="Id">Identificador da remessa.</param>
/// <param name="Periodo">Período (competência/exercício).</param>
/// <param name="Leiaute">Leiaute versionado (código e versão).</param>
/// <param name="Situacao">Situação atual.</param>
/// <param name="HashIntegridade">Hash de integridade do pacote, se presente.</param>
/// <param name="DataLimite">Prazo legal/parametrizado de envio.</param>
/// <param name="DataGeracao">Data de geração do pacote.</param>
/// <param name="DataEnvio">Data de transmissão, se enviada.</param>
/// <param name="NomeArquivoZip">Nome do ZIP nomeado, após empacotamento (artefato de auditoria).</param>
/// <param name="ProtocoloTce">Protocolo/recibo registrado pelo operador (ato humano de transmissão).</param>
/// <param name="PossuiErroValidacao">Indica se o RDI apontou erro.</param>
/// <param name="QuantidadeErros">Quantidade de erros no RDI.</param>
public sealed record RemessaTceDetalhe(
    Guid Id,
    string Periodo,
    string Leiaute,
    string Situacao,
    string? HashIntegridade,
    DateOnly DataLimite,
    DateOnly DataGeracao,
    DateOnly? DataEnvio,
    string? NomeArquivoZip,
    string? ProtocoloTce,
    bool PossuiErroValidacao,
    int QuantidadeErros);

/// <summary>Obtém o detalhe de uma remessa ao TCE-RS (tenant-scoped).</summary>
/// <param name="RemessaTceId">Remessa a consultar.</param>
public sealed record ObterRemessaTcePorIdQuery(Guid RemessaTceId) : IQuery<RemessaTceDetalhe?>;

/// <summary>Handler da consulta de detalhe da remessa.</summary>
public sealed class ObterRemessaTcePorIdHandler(IRemessaTceRepository remessas)
    : IQueryHandler<ObterRemessaTcePorIdQuery, RemessaTceDetalhe?>
{
    /// <inheritdoc />
    public async Task<RemessaTceDetalhe?> Handle(ObterRemessaTcePorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas.ObterPorIdAsync(new RemessaTceId(request.RemessaTceId), cancellationToken).ConfigureAwait(false);
        if (remessa is null)
        {
            return null;
        }

        return new RemessaTceDetalhe(
            remessa.Id.Value,
            remessa.Periodo.ToString(),
            remessa.Leiaute.ToString(),
            remessa.Situacao.ToString(),
            remessa.HashIntegridade?.ToString(),
            remessa.DataLimite,
            remessa.DataGeracao,
            remessa.DataEnvio,
            remessa.NomeArquivoZip,
            remessa.ProtocoloTce,
            remessa.ResultadoValidacao?.PossuiErro ?? false,
            remessa.ResultadoValidacao?.QuantidadeErros ?? 0);
    }
}

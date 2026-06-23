using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;

/// <summary>
/// Dados de conexao com um REP, resolvidos no momento da coleta. Carrega apenas a REFERENCIA da
/// credencial (nome do segredo no Key Vault) — nunca a credencial em si (CLAUDE.md §5/§6).
/// </summary>
/// <param name="EnderecoOuReferencia">Host/porta ou referencia logica do equipamento.</param>
/// <param name="ReferenciaCredencialCofre">Nome do segredo no Azure Key Vault; <c>null</c> se nao houver.</param>
/// <param name="IdentificacaoEquipamento">Nº de serie/fabricante, para a trilha de origem.</param>
public sealed record RepConexao(
    string? EnderecoOuReferencia,
    string? ReferenciaCredencialCofre,
    string IdentificacaoEquipamento);

/// <summary>
/// Lote de AFD coletado de um equipamento — a "lingua franca" da ingestao. Todo driver de fabricante,
/// qualquer que seja o transporte (arquivo, TCP/SDK, REST/cloud), CONVERGE para entregar este mesmo
/// AFD posicional 671, consumido por um unico pipeline de ingestao.
/// </summary>
/// <param name="ConteudoAfd">AFD posicional 671 (ISO-8859-1).</param>
/// <param name="AssinaturaCades">Assinatura CAdES detached (.p7s) do AFD, quando o REP a fornece; opcional.</param>
/// <param name="Marca">Marca/fabricante que produziu o lote.</param>
/// <param name="IdentificacaoEquipamento">Nº de serie/fabricante (trilha de origem).</param>
public sealed record LoteAfdColetado(
    byte[] ConteudoAfd,
    byte[]? AssinaturaCades,
    MarcaRep Marca,
    string IdentificacaoEquipamento);

/// <summary>
/// Porta de coleta por fabricante/modo (ACL — CLAUDE.md §8): "me devolva o AFD do equipamento a partir
/// do ultimo NSR ja ingerido". O contrato e estavel na Application; as implementacoes (REST/SDK/USB)
/// ficam na Infrastructure, isolando o proprietario do fabricante na borda. // TODO(prod: SDK proprietario)
/// por marca (Control iD/Henry/Madis/Topdata/Dimep).
/// </summary>
public interface IColetorRep
{
    /// <summary>Marca/fabricante atendida por este driver.</summary>
    MarcaRep Marca { get; }

    /// <summary>Modos de transporte suportados (arquivo/TCP-SDK/REST-cloud).</summary>
    ModoColeta Modos { get; }

    /// <summary>
    /// Coleta INCREMENTAL: pede o AFD a partir de <paramref name="ultimoNsrConhecido"/> (quando o REP
    /// suporta filtro por NSR — Henry/Control iD suportam). Quando nao suporta, devolve o AFD completo
    /// e a ingestao deduplica por <c>(REP, NSR)</c>. Drivers de rede aplicam Polly + ACL na Infrastructure.
    /// </summary>
    /// <param name="conexao">Dados de conexao do equipamento.</param>
    /// <param name="ultimoNsrConhecido">Ultimo NSR de equipamento ja ingerido (0 = coletar tudo).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lote de AFD coletado.</returns>
    Task<LoteAfdColetado> ColetarAsync(RepConexao conexao, long ultimoNsrConhecido, CancellationToken cancellationToken);
}

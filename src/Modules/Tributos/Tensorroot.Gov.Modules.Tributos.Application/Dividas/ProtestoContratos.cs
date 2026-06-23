using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>
/// Dados mínimos de uma CDA para montar o arquivo de remessa ao CRA estadual. O leiaute exato (posições,
/// códigos) é responsabilidade do adapter (ACL) por CRA. // TODO(validar-oficial): leiaute CRA-RS/IEPTB-RS.
/// </summary>
/// <param name="NumeroCda">Número da CDA.</param>
/// <param name="NomeDevedor">Nome do devedor.</param>
/// <param name="DocumentoDevedor">CPF/CNPJ do devedor (sem máscara).</param>
/// <param name="ValorTitulo">Valor do título a protestar (R$).</param>
/// <param name="DataInscricao">Data da inscrição em Dívida Ativa.</param>
public sealed record TituloProtesto(
    string NumeroCda,
    string NomeDevedor,
    string DocumentoDevedor,
    decimal ValorTitulo,
    DateOnly DataInscricao);

/// <summary>Resultado da geração do arquivo de remessa de protesto pelo adapter (ACL).</summary>
/// <param name="IdentificadorCra">CRA estadual de destino.</param>
/// <param name="ConteudoRemessa">Conteúdo do arquivo de remessa (leiaute do CRA).</param>
public sealed record ArquivoRemessaProtesto(string IdentificadorCra, string ConteudoRemessa);

/// <summary>Registro de retorno do CRA/cartório (mapeado pelo adapter a partir do arquivo de retorno).</summary>
/// <param name="NumeroCda">Número da CDA referenciada.</param>
/// <param name="Ocorrencia">Ocorrência mapeada (lavrado/pago/sustado/rejeitado).</param>
/// <param name="ProtocoloCartorio">Protocolo do cartório, quando houver.</param>
public sealed record RetornoProtesto(string NumeroCda, OcorrenciaProtesto Ocorrencia, string? ProtocoloCartorio);

/// <summary>
/// Gateway (Anti-Corruption Layer) do protesto extrajudicial junto ao CRA estadual (Lei 9.492/97). A
/// integração real é convênio à parte — modelada atrás desta porta, versionada por CRA. O adapter
/// simulado serve a dev/testes; o adapter de produção implementa o leiaute oficial do CRA-RS/IEPTB-RS.
/// </summary>
public interface IProtestoCraGateway
{
    /// <summary>CRA estadual atendido por este adapter (ex.: "CRA-RS").</summary>
    string IdentificadorCra { get; }

    /// <summary>Gera o arquivo de remessa de um título de protesto (leiaute do CRA).</summary>
    /// <param name="titulo">Dados da CDA a protestar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O arquivo de remessa gerado.</returns>
    Task<ArquivoRemessaProtesto> GerarRemessaAsync(TituloProtesto titulo, CancellationToken cancellationToken);

    /// <summary>Interpreta um arquivo de retorno do CRA, mapeando-o para ocorrências de domínio.</summary>
    /// <param name="conteudoRetorno">Conteúdo bruto do arquivo de retorno do CRA.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Registros de retorno mapeados.</returns>
    Task<IReadOnlyList<RetornoProtesto>> InterpretarRetornoAsync(string conteudoRetorno, CancellationToken cancellationToken);
}

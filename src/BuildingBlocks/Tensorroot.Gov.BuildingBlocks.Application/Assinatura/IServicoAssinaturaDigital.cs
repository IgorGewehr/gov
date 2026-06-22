namespace Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

/// <summary>
/// PORTA transversal de assinatura digital com o certificado A1 do tenant (A1-DESIGN §5).
/// Atras desta porta: cofre (envelope encryption), decifragem SO-EM-MEMORIA (EphemeralKeySet),
/// validacao da cadeia ICP-Brasil, Polly nas operacoes de Key Vault e trilha de auditoria de CADA
/// uso (sucesso E falha). A chave privada NUNCA e serializada, logada ou retornada por estes metodos.
/// </summary>
public interface IServicoAssinaturaDigital
{
    /// <summary>
    /// Assina um documento XML (XML-DSig Enveloped, RSA-SHA256, canonicalizacao conforme o destino —
    /// A1-DESIGN §4) com o A1 ATIVO do tenant corrente.
    /// </summary>
    /// <param name="xmlUtf8">Documento XML a assinar, em bytes (UTF-8).</param>
    /// <param name="opcoes">Opcoes/destino que selecionam o perfil criptografico.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O XML assinado e os metadados de auditoria (sem material sensivel).</returns>
    Task<ResultadoAssinatura> AssinarXmlAsync(
        ReadOnlyMemory<byte> xmlUtf8,
        OpcoesAssinaturaXml opcoes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Assina um conteudo binario/.TXT produzindo CMS/PKCS#7 (CAdES) — A1-DESIGN §4.2.
    /// // TODO(validar-oficial): habilitar por destino somente onde o orgao exigir assinatura embutida.
    /// </summary>
    /// <param name="conteudo">Conteudo a assinar.</param>
    /// <param name="opcoes">Opcoes/destino da assinatura CMS.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O envelope CMS (DER) assinado.</returns>
    Task<byte[]> AssinarCmsAsync(
        ReadOnlyMemory<byte> conteudo,
        OpcoesAssinaturaCms opcoes,
        CancellationToken cancellationToken);

    /// <summary>Devolve os metadados (SEM chave) do certificado A1 ativo do tenant corrente.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados nao sigilosos do certificado.</returns>
    Task<CertificadoInfo> ObterInfoCertificadoAsync(CancellationToken cancellationToken);
}

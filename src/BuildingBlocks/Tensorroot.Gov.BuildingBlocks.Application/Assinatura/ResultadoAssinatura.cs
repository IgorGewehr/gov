namespace Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

/// <summary>
/// Resultado de uma assinatura XML-DSig: o XML assinado (em bytes UTF-8) e metadados NAO sigilosos
/// para auditoria. NUNCA carrega chave privada nem senha (A1-DESIGN §3.2/§6).
/// </summary>
/// <param name="XmlAssinado">Documento XML ja assinado, serializado em bytes (UTF-8).</param>
/// <param name="Thumbprint">Thumbprint do certificado usado (identificacao para a trilha).</param>
/// <param name="HashArtefatoSha256">Hash SHA-256 (hex) do artefato assinado — o "o que" da auditoria.</param>
public sealed record ResultadoAssinatura(
    byte[] XmlAssinado,
    string Thumbprint,
    string HashArtefatoSha256);

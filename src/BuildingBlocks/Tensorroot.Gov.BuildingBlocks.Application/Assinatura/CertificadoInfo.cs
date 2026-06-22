namespace Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

/// <summary>
/// Metadados NAO sigilosos do certificado A1 ativo do tenant (titular, validade, thumbprint).
/// NUNCA carrega o .pfx, a senha ou a chave privada — e o unico shape do certificado que pode
/// trafegar por API/DTO/log (A1-DESIGN §2: "expor so metadados").
/// </summary>
/// <param name="Titular">CN/Razao Social do e-CNPJ (nao sigiloso).</param>
/// <param name="CnpjTitular">CNPJ do titular, sem mascara.</param>
/// <param name="Thumbprint">Impressao digital SHA-256 do certificado (identificacao).</param>
/// <param name="NotBeforeUtc">Inicio da validade (UTC).</param>
/// <param name="NotAfterUtc">Fim da validade (UTC).</param>
/// <param name="Serie">Serie do certificado ("A1").</param>
/// <param name="Status">Situacao do certificado no cofre.</param>
public sealed record CertificadoInfo(
    string Titular,
    string CnpjTitular,
    string Thumbprint,
    DateTime NotBeforeUtc,
    DateTime NotAfterUtc,
    string Serie,
    string Status);

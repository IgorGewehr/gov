using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Servico de assinatura/validacao digital ICP-Brasil (NGS2) do prontuario (CFM 1.821/2007;
/// Lei 13.787/2018). A implementacao concreta vive na Infrastructure; certificados A1/A3 por tenant
/// no Azure Key Vault. Falha de validacao impede a assinatura (mantem o registro mutavel — I-3).
/// </summary>
public interface IAssinaturaIcpBrasilService
{
    /// <summary>Valida o certificado/hash ICP-Brasil e produz a assinatura digital NGS2.</summary>
    /// <param name="certificadoIcpBrasil">Identificacao do certificado ICP-Brasil.</param>
    /// <param name="hash">Hash criptografico do conteudo assinado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A <see cref="AssinaturaDigital"/> NGS2 validada.</returns>
    Task<AssinaturaDigital> AssinarAsync(string certificadoIcpBrasil, string hash, CancellationToken cancellationToken);
}

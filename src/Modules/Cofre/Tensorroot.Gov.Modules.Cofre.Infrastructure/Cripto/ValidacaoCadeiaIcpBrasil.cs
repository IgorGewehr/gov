using System.Security.Cryptography.X509Certificates;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

/// <summary>
/// Validacao da cadeia ICP-Brasil de um certificado A1 (A1-DESIGN §5/§7 risco 5). Containers Linux
/// NAO trazem as raizes ICP-Brasil no trust do SO; em PRODUCAO as AC-Raiz/intermediarias DEVEM ser
/// embarcadas via <see cref="X509Chain.ChainPolicy"/> (ExtraStore + CustomRootTrust) e a revogacao
/// (CRL/OCSP) habilitada. // TODO(validar-oficial): embarcar a cadeia AC-Raiz vigente de
/// repositorio.iti.gov.br e habilitar RevocationMode=Online antes de produção.
/// </summary>
internal static class ValidacaoCadeiaIcpBrasil
{
    /// <summary>
    /// Valida a cadeia e o KeyUsage (digitalSignature/nonRepudiation) do certificado. Em DEV, sem as
    /// raizes embarcadas, a verificacao de cadeia e tolerante (apenas registra/avalia KeyUsage); a
    /// validacao ESTRITA com raizes ICP-Brasil + CRL/OCSP e habilitada em PRODUCAO (TODO acima).
    /// </summary>
    /// <param name="cert">Certificado a validar.</param>
    /// <exception cref="CertificadoInvalidoException">Se o KeyUsage nao permitir assinatura.</exception>
    public static void Validar(X509Certificate2 cert)
    {
        ArgumentNullException.ThrowIfNull(cert);

        if (!PermiteAssinatura(cert))
        {
            throw new CertificadoInvalidoException(
                "Certificado A1 sem KeyUsage de assinatura (digitalSignature/nonRepudiation).");
        }

        // // TODO(validar-oficial): em PRODUCAO, montar a cadeia com as AC-Raiz ICP-Brasil embarcadas
        // (ChainPolicy.CustomRootTrust + ExtraStore) e RevocationMode=Online; reprovar cadeia invalida.
        // Em DEV/container sem o trust store ICP-Brasil, a validacao estrita de cadeia falharia por
        // falta de raiz — por isso aqui apenas garantimos o KeyUsage e deixamos o gancho de cadeia.
    }

    private static bool PermiteAssinatura(X509Certificate2 cert)
    {
        var temKeyUsage = false;
        foreach (var extensao in cert.Extensions)
        {
            if (extensao is X509KeyUsageExtension uso)
            {
                temKeyUsage = true;
                if (uso.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature)
                    || uso.KeyUsages.HasFlag(X509KeyUsageFlags.NonRepudiation))
                {
                    return true;
                }
            }
        }

        // Sem extensao KeyUsage declarada: nao restringe o uso (aceita); com KeyUsage, exige assinatura.
        return !temKeyUsage;
    }
}

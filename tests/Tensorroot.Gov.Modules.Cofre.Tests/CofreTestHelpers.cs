using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

namespace Tensorroot.Gov.Modules.Cofre.Tests;

/// <summary>Utilitarios de teste: gera um .pfx autoassinado (tipo e-CNPJ) e um provedor de KEK em memoria.</summary>
internal static class CofreTestHelpers
{
    public const string SenhaPfx = "senha-de-teste-123";

    /// <summary>Gera um .pfx autoassinado RSA-2048 com KeyUsage de assinatura, valido por 1 ano.</summary>
    public static byte[] GerarPfx(string cn = "EMPRESA TESTE LTDA:11222333000181")
    {
        using var rsa = RSA.Create(2048);
        var pedido = new CertificateRequest($"CN={cn}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        pedido.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, critical: true));

        using var cert = pedido.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(1));
        return cert.Export(X509ContentType.Pfx, SenhaPfx);
    }

    /// <summary>Carrega o .pfx com chave utilizavel (sem EphemeralKeySet, nao suportado no macOS de dev).</summary>
    public static X509Certificate2 CarregarPfx(byte[] pfx)
        => new(pfx, SenhaPfx, X509KeyStorageFlags.Exportable);

    /// <summary>Cria o provedor de KEK de DEV (config) com uma KEK aleatoria valida.</summary>
    public static IProvedorKek CriarProvedorKek()
    {
        var kek = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var options = Options.Create(new CofreOptions { ProvedorKek = "Config", KekBase64 = kek });
        return new ProvedorKekConfig(options);
    }
}

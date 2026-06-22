using System.Text.Json;
using FluentAssertions;
using Tensorroot.Gov.Modules.Cofre.Domain;
using Xunit;

namespace Tensorroot.Gov.Modules.Cofre.Tests;

/// <summary>
/// Prova que a chave privada (e o material em claro) NUNCA escapa por ToString ou serializacao
/// (A1-DESIGN §6/§8): a entidade so carrega bytes CIFRADOS; nao ha propriedade de chave/senha em claro.
/// </summary>
public sealed class NaoVazamentoChaveTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Entidade_nao_expoe_chave_privada_nem_senha_em_claro()
    {
        // A entidade so tem campos CIFRADOS — nao ha propriedade "PrivateKey", "Pfx" ou "Senha" em claro.
        var propriedades = typeof(CertificadoA1Cofre).GetProperties().Select(p => p.Name).ToArray();

        propriedades.Should().NotContain("PrivateKey");
        propriedades.Should().NotContain("ChavePrivada");
        propriedades.Should().NotContain("Senha");
        propriedades.Should().NotContain("Pfx");
        // Confirma que o que existe e o material cifrado (sufixo Cipher/Nonce/Tag/Wrapped).
        propriedades.Should().Contain("PfxCipher").And.Contain("SenhaCipher").And.Contain("DekWrapped");
    }

    [Fact]
    public void ToString_nao_revela_material_cifrado()
    {
        var certificado = MontarComMaterialMarcado(out var marcador);

        certificado.ToString().Should().NotContain(Convert.ToBase64String(marcador));
    }

    [Fact]
    public void Serializacao_json_nao_inclui_senha_em_claro()
    {
        var certificado = MontarComMaterialMarcado(out _);

        // Serializar a entidade nunca produz a senha/pfx em claro (so bytes cifrados, ja opacos).
        var json = JsonSerializer.Serialize(certificado);

        json.Should().NotContain(CofreTestHelpers.SenhaPfx);
    }

    private static CertificadoA1Cofre MontarComMaterialMarcado(out byte[] marcador)
    {
        marcador = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };
        var pfx = new MaterialCifrado(marcador, new byte[MaterialCifrado.TamanhoNonce], new byte[MaterialCifrado.TamanhoTag]);
        var senha = new MaterialCifrado(new byte[] { 1, 2 }, new byte[MaterialCifrado.TamanhoNonce], new byte[MaterialCifrado.TamanhoTag]);

        return CertificadoA1Cofre.Cadastrar(
            Tenant, "TITULAR TESTE", "11222333000181", "ABC123", DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddYears(1), pfx, senha, new byte[] { 7, 7 }, "kek-v1", DateTime.UtcNow);
    }
}

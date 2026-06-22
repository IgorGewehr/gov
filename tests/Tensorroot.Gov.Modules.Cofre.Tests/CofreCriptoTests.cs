using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Tensorroot.Gov.Modules.Cofre.Domain;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;
using Xunit;

namespace Tensorroot.Gov.Modules.Cofre.Tests;

/// <summary>
/// Testes de seguranca do nucleo criptografico do cofre (A1-DESIGN §8): round-trip cifra→decifra,
/// integridade AEAD (tag/AAD), e a garantia de que a chave privada nao vaza por ToString/serializacao.
/// </summary>
public sealed class CofreCriptoTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Cifra_e_decifra_round_trip_recupera_chave_privada_utilizavel()
    {
        var cripto = new CofreCripto(CofreTestHelpers.CriarProvedorKek());
        var pfx = CofreTestHelpers.GerarPfx();

        var cifragem = await cripto.CifrarParaCadastroAsync(Tenant, pfx, CofreTestHelpers.SenhaPfx, DateTime.UtcNow, default);
        var certificado = MontarEntidade(cifragem);

        // Decifra SO-EM-MEMORIA e usa a chave privada para assinar/verificar — prova o round-trip.
        var ok = await cripto.UsarCertificadoAsync(certificado, cert =>
        {
            using var rsa = cert.GetRSAPrivateKey();
            rsa.Should().NotBeNull();
            var dados = new byte[] { 1, 2, 3, 4, 5 };
            var assinatura = rsa!.SignData(dados, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return rsa.VerifyData(dados, assinatura, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }, default);

        ok.Should().BeTrue();
        certificado.Thumbprint.Should().Be(cifragem.Thumbprint);
    }

    [Fact]
    public async Task Material_persistido_nunca_e_o_pfx_em_claro()
    {
        var cripto = new CofreCripto(CofreTestHelpers.CriarProvedorKek());
        var pfx = CofreTestHelpers.GerarPfx();
        var pfxCopia = (byte[])pfx.Clone();

        var cifragem = await cripto.CifrarParaCadastroAsync(Tenant, pfx, CofreTestHelpers.SenhaPfx, DateTime.UtcNow, default);

        cifragem.Pfx.Cipher.Should().NotEqual(pfxCopia);
        cifragem.Pfx.Nonce.Should().HaveCount(MaterialCifrado.TamanhoNonce);
        cifragem.Pfx.Tag.Should().HaveCount(MaterialCifrado.TamanhoTag);
        cifragem.DekWrapped.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Nonce_e_unico_por_cifragem()
    {
        var cripto = new CofreCripto(CofreTestHelpers.CriarProvedorKek());

        var a = await cripto.CifrarParaCadastroAsync(Tenant, CofreTestHelpers.GerarPfx(), CofreTestHelpers.SenhaPfx, DateTime.UtcNow, default);
        var b = await cripto.CifrarParaCadastroAsync(Tenant, CofreTestHelpers.GerarPfx(), CofreTestHelpers.SenhaPfx, DateTime.UtcNow, default);

        a.Pfx.Nonce.Should().NotEqual(b.Pfx.Nonce);
    }

    [Fact]
    public async Task Adulterar_um_byte_do_cipher_faz_a_decifragem_falhar()
    {
        var cripto = new CofreCripto(CofreTestHelpers.CriarProvedorKek());
        var cifragem = await cripto.CifrarParaCadastroAsync(Tenant, CofreTestHelpers.GerarPfx(), CofreTestHelpers.SenhaPfx, DateTime.UtcNow, default);
        var certificado = MontarEntidade(cifragem);
        certificado.PfxCipher[0] ^= 0xFF; // 1 byte adulterado

        var acao = async () => await cripto.UsarCertificadoAsync(certificado, _ => true, default);

        await acao.Should().ThrowAsync<CertificadoInvalidoException>();
    }

    [Fact]
    public async Task Trocar_o_tenant_do_blob_faz_a_decifragem_falhar_AAD()
    {
        // AAD = TenantId||Thumbprint: usar o blob com OUTRO tenant invalida a autenticacao (risco 8).
        var cripto = new CofreCripto(CofreTestHelpers.CriarProvedorKek());
        var cifragem = await cripto.CifrarParaCadastroAsync(Tenant, CofreTestHelpers.GerarPfx(), CofreTestHelpers.SenhaPfx, DateTime.UtcNow, default);

        var outroTenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var certificadoForjado = CertificadoA1Cofre.Cadastrar(
            outroTenant, cifragem.Titular, "11222333000181", cifragem.Thumbprint, cifragem.NotBeforeUtc,
            cifragem.NotAfterUtc, cifragem.Pfx, cifragem.Senha, cifragem.DekWrapped, cifragem.KekKeyId, DateTime.UtcNow);

        var acao = async () => await cripto.UsarCertificadoAsync(certificadoForjado, _ => true, default);

        await acao.Should().ThrowAsync<CertificadoInvalidoException>();
    }

    [Fact]
    public async Task Pfx_expirado_e_rejeitado_no_cadastro()
    {
        var cripto = new CofreCripto(CofreTestHelpers.CriarProvedorKek());
        var pfx = CofreTestHelpers.GerarPfx();

        // Avanca o "agora" para depois da validade (1 ano) — deve recusar.
        var acao = async () => await cripto.CifrarParaCadastroAsync(Tenant, pfx, CofreTestHelpers.SenhaPfx, DateTime.UtcNow.AddYears(2), default);

        await acao.Should().ThrowAsync<CertificadoInvalidoException>();
    }

    [Fact]
    public async Task Senha_incorreta_e_rejeitada_sem_vazar_material()
    {
        var cripto = new CofreCripto(CofreTestHelpers.CriarProvedorKek());
        var pfx = CofreTestHelpers.GerarPfx();

        var acao = async () => await cripto.CifrarParaCadastroAsync(Tenant, pfx, "senha-errada", DateTime.UtcNow, default);

        var excecao = await acao.Should().ThrowAsync<CertificadoInvalidoException>();
        excecao.Which.Message.Should().NotContain("senha-errada");
    }

    private static CertificadoA1Cofre MontarEntidade(CifragemResultado c)
        => CertificadoA1Cofre.Cadastrar(
            Tenant, c.Titular, "11222333000181", c.Thumbprint, c.NotBeforeUtc, c.NotAfterUtc,
            c.Pfx, c.Senha, c.DekWrapped, c.KekKeyId, DateTime.UtcNow);
}

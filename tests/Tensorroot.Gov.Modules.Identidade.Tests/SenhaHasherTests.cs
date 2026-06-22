using FluentAssertions;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;
using Xunit;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Cobertura do hash de senha (BCrypt): o hash nunca expoe a senha em claro, salga (hashes
/// distintos para a mesma senha), verifica a senha correta e rejeita a incorreta/hash malformado.
/// E SEGURANCA CRITICA.
/// </summary>
public sealed class SenhaHasherTests
{
    // Work factor baixo apenas nos testes para acelerar (a producao usa o padrao 12).
    private readonly SenhaHasher _hasher = new(workFactor: 4);

    [Fact]
    public void Hash_nao_armazena_a_senha_em_claro_e_usa_prefixo_bcrypt()
    {
        const string senha = "Senha@Forte123";

        var hash = _hasher.Hash(senha);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotContain(senha);
        hash.Should().StartWith("$2"); // identificador do algoritmo BCrypt
    }

    [Fact]
    public void Hash_salga_gerando_hashes_diferentes_para_a_mesma_senha()
    {
        const string senha = "Senha@Forte123";

        var primeiro = _hasher.Hash(senha);
        var segundo = _hasher.Hash(senha);

        primeiro.Should().NotBe(segundo);
    }

    [Fact]
    public void Verificar_aceita_a_senha_correta()
    {
        const string senha = "Senha@Forte123";
        var hash = _hasher.Hash(senha);

        _hasher.Verificar(senha, hash).Should().BeTrue();
    }

    [Fact]
    public void Verificar_rejeita_a_senha_incorreta()
    {
        var hash = _hasher.Hash("Senha@Forte123");

        _hasher.Verificar("SenhaErrada", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Verificar_rejeita_senha_vazia(string senha)
    {
        var hash = _hasher.Hash("Senha@Forte123");

        _hasher.Verificar(senha, hash).Should().BeFalse();
    }

    [Fact]
    public void Verificar_rejeita_hash_malformado_sem_lancar()
    {
        var acao = () => _hasher.Verificar("qualquer", "hash-invalido-nao-bcrypt");

        acao.Should().NotThrow();
        _hasher.Verificar("qualquer", "hash-invalido-nao-bcrypt").Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hash_rejeita_senha_vazia(string senha)
    {
        var acao = () => _hasher.Hash(senha);

        acao.Should().Throw<ArgumentException>();
    }
}

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.Modules.Cofre.Infrastructure;
using Xunit;

namespace Tensorroot.Gov.Modules.Cofre.Tests;

/// <summary>
/// SEGURANCA CRITICA — fecha o achado W10.6 CF-2 (CLAUDE.md §6 "segredos so no Key Vault"). A selecao do
/// provedor de KEK (chave-mestra que embrulha TODAS as DEKs de TODOS os tenants) vinha de string pura e
/// caia silenciosamente no provedor "Config" (KEK AES de config/env, modo DEV) quando ausente. Em PROD
/// mal-configurado o sistema subia com a chave-mestra FORA do HSM. Estes testes PROVAM o fail-fast no
/// boot: em ambiente != Development, <see cref="CofreModule.AddModule"/> ABORTA se o provedor nao for
/// KeyVault; em Development o provedor de config segue permitido.
/// </summary>
public sealed class CofreFailFastProducaoTests
{
    [Theory]
    [InlineData("Production", null)]    // ausente -> cairia em "Config" (DEV) silenciosamente
    [InlineData("Production", "Config")]
    [InlineData("Staging", "Config")]   // qualquer ambiente != Development e tratado como producao
    [InlineData("Production", "qualquer-coisa")]
    public void Boot_em_producao_sem_KeyVault_FALHA_rapido(string ambiente, string? provedorKek)
    {
        var services = new ServiceCollection();
        var configuration = ConstruirConfig(ambiente, provedorKek);

        var acao = () => new CofreModule().AddModule(services, configuration);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProvedorKek*KeyVault*");
    }

    [Fact]
    public void Boot_em_producao_com_KeyVault_e_permitido()
    {
        var services = new ServiceCollection();
        var configuration = ConstruirConfig("Production", "KeyVault");

        var acao = () => new CofreModule().AddModule(services, configuration);

        // KeyVault em PROD e o unico caminho aceito — nao aborta.
        acao.Should().NotThrow();
    }

    [Fact]
    public void Boot_em_desenvolvimento_com_provedor_Config_e_permitido()
    {
        var services = new ServiceCollection();
        var configuration = ConstruirConfig("Development", "Config");

        var acao = () => new CofreModule().AddModule(services, configuration);

        // "Config" (KEK de config/env) e EXCLUSIVO de DEV — permitido apenas fora de producao.
        acao.Should().NotThrow();
    }

    private static IConfiguration ConstruirConfig(string ambiente, string? provedorKek)
    {
        var pares = new Dictionary<string, string?>
        {
            // Chave canonica do Host (mesma populada por UseEnvironment/CreateBuilder) — o guard a le
            // PRIMEIRO, espelhando app.Environment.IsDevelopment() do Program.cs.
            [Microsoft.Extensions.Hosting.HostDefaults.EnvironmentKey] = ambiente,
            ["Database:Provider"] = "Sqlite",
        };

        if (provedorKek is not null)
        {
            pares["Cofre:ProvedorKek"] = provedorKek;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(pares).Build();
    }
}

using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.IntegrationTests.Infrastructure;

/// <summary>
/// Fabrica de aplicacao para os testes E2E HTTP (W9.8). Sobe o ApiHost REAL (Composition Root, todos
/// os modulos) em <c>TestServer</c> IN-MEMORY — sem socket/porta TCP — de modo que NUNCA colide com o
/// ApiHost de desenvolvimento (:5080).
///
/// Isolamento de execucao (ambiente SEM Docker, decisao de plataforma):
/// <list type="bullet">
///   <item>Ambiente = <c>Development</c> + <c>Database:Provider=Sqlite</c> (fallback EnsureCreated do host).</item>
///   <item>Banco de CONTROLE (plataforma) e bancos DEDICADOS por tenant em um diretorio TEMPORARIO
///         exclusivo desta fabrica — descartado no Dispose. Cada classe de teste = um banco proprio.</item>
///   <item>Segredo JWT de teste fixo (HS256) para emitir tokens reais com claims/permissoes
///         (mesmo formato do <c>EmissorToken</c> de producao — ver <see cref="EmissorTokenDeTeste"/>).</item>
/// </list>
///
/// O <c>content root</c> aponta para o diretorio temporario: o auto-seed de DEV do host (que provisiona
/// um tenant demo com bancos por convencao relativa <c>tenant_*.db</c>) grava DENTRO do temp, nao no repo.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Segredo HS256 de teste (>=32 bytes). Espelha o "Jwt:Secret" injetado na configuracao.</summary>
    public const string JwtSecret = "INTEGRATION-TESTS-ONLY-segredo-hs256-de-no-minimo-32-bytes-para-w98";

    /// <summary>Emissor/audiencia esperados pelo ApiHost (default do Program.cs).</summary>
    public const string JwtIssuer = "tensorroot.gov";

    /// <summary>Audiencia esperada pelo ApiHost.</summary>
    public const string JwtAudience = "tensorroot.gov";

    private readonly string _diretorioRaiz =
        Path.Combine(Path.GetTempPath(), "ttgov-itests-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Provider de configuracao MUTAVEL (in-memory) que sobrepoe as fontes default. Permite ao harness
    /// trocar, em tempo de execucao, o e-mail do administrador semeado (<c>Identidade:Admin:Email</c>)
    /// ANTES de migrar cada tenant — necessario porque o indice central de login (email -> tenant) e
    /// GLOBAL (uma producao da unicidade): cada ente onboard com e-mail de admin proprio. No harness, com
    /// varios tenants no mesmo banco de controle, reaproveitar o mesmo e-mail colidiria nesse indice.
    /// </summary>
    private readonly MemoryConfigurationProvider _configMutavel =
        new(new MemoryConfigurationSource());

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Directory.CreateDirectory(_diretorioRaiz);

        // O content root é o diretório temporário: bancos por convenção (tenant_*.db) e o auto-seed de
        // DEV ficam confinados ao temp — sem poluir o repositório nem o host da :5080.
        builder.UseContentRoot(_diretorioRaiz);
        builder.UseEnvironment(Environments.Development);

        var caminhoPlataforma = Path.Combine(_diretorioRaiz, "plataforma.db");

        // IMPORTANTE: o ApiHost le `Jwt:Secret`/`Database:Provider`/etc DIRETAMENTE de
        // `builder.Configuration` nas top-level statements do Program.cs (ANTES de Build). Com o content
        // root apontado ao diretorio TEMPORARIO (sem appsettings*.json), a config so chega ate o host se
        // for injetada na cadeia de configuracao do PROPRIO IWebHostBuilder. `UseSetting` grava nessa
        // cadeia de forma deterministica (lida imediatamente por WebApplication.CreateBuilder), enquanto o
        // callback de `ConfigureAppConfiguration` so e aplicado quando a porcao WebHost finaliza — tarde
        // demais para o `builder.Configuration["Jwt:Secret"]` do Program.cs. Por isso usamos UseSetting.
        var ajustes = new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:Platform"] = $"Data Source={caminhoPlataforma}",

            // JWT: segredo de teste (token auto-emitido pelo harness, validado pelo host).
            ["Jwt:Secret"] = JwtSecret,
            ["Jwt:Issuer"] = JwtIssuer,
            ["Jwt:Audience"] = JwtAudience,
            ["Jwt:DuracaoMinutos"] = "60",

            // Cofre: KEK local de DEV (envelope encryption A1), sem Key Vault nos testes.
            ["Cofre:ProvedorKek"] = "Config",
            ["Cofre:KekKeyIdDev"] = "itests-kek-v1",
            ["Cofre:KekBase64"] = Convert.ToBase64String(new byte[32]),
        };

        foreach (var (chave, valor) in ajustes)
        {
            builder.UseSetting(chave, valor);
        }

        // Fonte MUTAVEL no topo da cadeia: o e-mail do admin e lido por SemearAdministradorAsync via
        // IConfiguration (singleton) durante a MIGRACAO de cada modulo — tempo de execucao, bem depois do
        // Build —, entao o callback de ConfigureAppConfiguration e o momento correto (ao contrario do
        // Jwt:Secret, lido no top-level). O harness reescreve este provider antes de cada provisionamento.
        builder.ConfigureAppConfiguration((_, config) =>
            config.Add(new MutableConfigurationSource(_configMutavel)));
    }

    /// <summary>Fonte que devolve o provider mutavel compartilhado (in-memory) ja instanciado.</summary>
    private sealed class MutableConfigurationSource(IConfigurationProvider provider) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => provider;
    }

    /// <summary>
    /// Connection string SQLite ABSOLUTA, no diretorio temporario da fabrica, para o banco dedicado de
    /// um tenant. Garante isolamento fisico por tenant E por execucao (nunca a :5080).
    /// </summary>
    public string ConexaoTenant(Guid tenantId)
        => string.Create(CultureInfo.InvariantCulture, $"Data Source={Path.Combine(_diretorioRaiz, $"tenant_{tenantId:N}.db")}");

    /// <summary>
    /// Provisiona um tenant para o teste: grava o catalogo da plataforma (via servico PUBLICO) e MIGRA
    /// o banco dedicado de cada modulo licenciado. Replica a orquestracao do <c>TenantProvisioner</c> do
    /// ApiHost usando apenas tipos PUBLICOS (o teste edita somente tests/ — nao toca o ApiHost).
    /// </summary>
    /// <param name="cnpj">CNPJ do ente.</param>
    /// <param name="nome">Nome do ente.</param>
    /// <param name="poder">Poder (Executivo/Legislativo).</param>
    /// <param name="modulos">Modulos a licenciar (default: todos os descobertos).</param>
    /// <returns>Id do tenant provisionado, com conexao dedicada no diretorio temporario.</returns>
    public async Task<Guid> ProvisionarTenantAsync(
        string cnpj,
        string nome,
        PoderTenant poder,
        IReadOnlyList<string>? modulos = null)
    {
        // Garante o boot do host (banco de controle criado/migrado + auto-seed concluido).
        _ = Services;

        var modulosTodos = Services.GetServices<IModule>().Select(m => m.Name).ToList();
        var licenciados = modulos ?? modulosTodos;

        Guid tenantId;

        // 1) Catalogo da plataforma (servico publico).
        //
        // IMPORTANTE: o `TenantProvisioningService` GERA o Id do tenant internamente (compoe a AAD do
        // envelope que cifra a connection string em repouso) e DESCARTA qualquer Id que o chamador tivesse
        // em mente. Logo, NAO podemos passar uma connection string ja calculada a partir de um Id
        // provisorio (ela apontaria para um arquivo `tenant_<id-descartado>.db` que nunca migramos →
        // "no such table" em request-time). Provisionamos SEM conexao, obtemos o Id REAL e so entao
        // ROTACIONAMOS o catalogo para a conexao dedicada absoluta desse Id (mesmo arquivo que migramos
        // no passo 2). RotacionarConexaoAsync tambem invalida o cache de conexao — alinhando catalogo,
        // migracao e resolucao em request-time no MESMO arquivo SQLite.
        await using (var escopo = Services.CreateAsyncScope())
        {
            var provisioning = escopo.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
            tenantId = await provisioning.ProvisionarAsync(cnpj, nome, poder, connectionString: null, licenciados, CancellationToken.None);
            await provisioning.RotacionarConexaoAsync(tenantId, ConexaoTenant(tenantId), CancellationToken.None);
        }

        var conexao = ConexaoTenant(tenantId);

        // E-mail de admin UNICO por tenant: o indice central (email -> tenant) e global. Reescreve o
        // provider mutavel ANTES da migracao da Identidade (que semeia o admin lendo esta chave). Os testes
        // emitem tokens DIRETAMENTE (EmissorTokenDeTeste), entao o e-mail semeado nao precisa ser previsivel.
        _configMutavel.Set("Identidade:Admin:Email", $"admin-{tenantId:N}@itests.tensorroot.gov");
        var raizConfig = Services.GetService<IConfigurationRoot>();
        raizConfig?.Reload();

        // 2) Migra (cria schema) + semeia o banco dedicado de cada modulo licenciado.
        var modulosInstancias = Services.GetServices<IModule>()
            .Where(m => licenciados.Contains(m.Name, StringComparer.Ordinal))
            .ToList();

        foreach (var modulo in modulosInstancias)
        {
            await using var escopoModulo = Services.CreateAsyncScope();
            escopoModulo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;
            await modulo.MigrarBancoAsync(conexao, "Sqlite", tenantId, escopoModulo.ServiceProvider, CancellationToken.None);
        }

        return tenantId;
    }

    /// <summary>
    /// Drena o Outbox de TODOS os modulos do tenant SOB DEMANDA (sem esperar os 30s do
    /// <c>OutboxBackgroundService</c>): replica a logica de drenagem por modulo em escopo dedicado, com
    /// <c>TenantOverride</c>. Essencial para as asserts cross-module deterministas (ex.: o
    /// <c>ContratoAssinadoIntegrationEvent</c> chegar ao consumidor de Patrimonio/Financas).
    /// </summary>
    public async Task DrenarOutboxAsync(Guid tenantId)
    {
        var modulos = Services.GetServices<IModule>().ToList();
        foreach (var modulo in modulos)
        {
            await using var escopo = Services.CreateAsyncScope();
            escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;
            await modulo.DrenarOutboxAsync(escopo.ServiceProvider, CancellationToken.None);
        }
    }

    /// <summary>
    /// Executa uma acao dentro de um escopo de DI com o tenant FORCADO (TenantOverride) — usado para
    /// semear/inspecionar agregados via repositorio quando nao ha endpoint HTTP correspondente.
    /// </summary>
    public async Task DentroDoTenantAsync(Guid tenantId, Func<IServiceProvider, Task> acao)
    {
        ArgumentNullException.ThrowIfNull(acao);
        await using var escopo = Services.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;
        await acao(escopo.ServiceProvider);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                if (Directory.Exists(_diretorioRaiz))
                {
                    Directory.Delete(_diretorioRaiz, recursive: true);
                }
            }
            catch (IOException)
            {
                // Arquivos SQLite ainda em handle do GC: a limpeza do temp do SO resolve depois.
            }
            catch (UnauthorizedAccessException)
            {
                // idem.
            }
        }
    }
}

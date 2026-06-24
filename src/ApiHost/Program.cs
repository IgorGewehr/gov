using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Tensorroot.Gov.ApiHost.Admin;
using Tensorroot.Gov.ApiHost.ErrorHandling;
using Tensorroot.Gov.ApiHost.Modularity;
using Tensorroot.Gov.ApiHost.Outbox;
using Tensorroot.Gov.ApiHost.Provisioning;
using Tensorroot.Gov.ApiHost.Tenancy;
using Tensorroot.Gov.ApiHost.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Application;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Tempo;
using Tensorroot.Gov.Platform;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// === Observabilidade: Serilog (logs estruturados) ===
builder.Services.AddSerilog(configuration => configuration
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// === Observabilidade: OpenTelemetry (traces + metrics) — ENRICHERS multi-tenant (W9.7) ===
// Instrumentação completa wired + enriquecimento por tenant.id/CorrelationId nos 3 sinais. O
// EnriquecedorTenantSpanProcessor propaga as dimensões do Baggage (semeado do JWT pelo
// ContextoCorrelacaoMiddleware) para TODO span — inclusive HttpClient às integrações governamentais.
// A métrica de requisição do AspNetCore recebe a dimensão tenant.id via IHttpMetricsTagsFeature (no
// ContextoCorrelacaoMiddleware). O Meter do Outbox/DLQ é registrado abaixo (AddMeter).
// // TODO(M10): exportador OTLP → Azure Monitor (infra/credenciais de produção) — só os enrichers no M9.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Tensorroot.Gov.ApiHost"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProcessor<Tensorroot.Gov.ApiHost.Observabilidade.EnriquecedorTenantSpanProcessor>())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // Métricas Outbox/DLQ (W9.7): pendentes/publicadas/dead-lettered por tenant.
        .AddMeter(Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox.OutboxMetrics.MeterName));

// === Contexto de requisição (tenant / usuário) ===
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// === Tratamento GLOBAL de erros (RFC 7807 / application/problem+json) ===
// Toda exceção não tratada (domínio, autorização/deny, validação) vira um ProblemDetails idiomático,
// SEM stack trace no corpo (nem em Development — a API nunca vaza stack; o detalhe fica só no log
// Serilog com TraceId/TenantId). O mapa exceção→status vive em MapaExcecaoStatus.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = contexto =>
    {
        // traceId SEMPRE presente, mesmo nos ProblemDetails produzidos por outras partes do pipeline
        // (ex.: 401 do JWT, 404 de rota) — uniformiza a correlação com o log.
        contexto.ProblemDetails.Extensions.TryAdd(
            "traceId",
            System.Diagnostics.Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier);
    });
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

// === Blocos de construção (TimeProvider + interceptors) + pipeline MediatR ===
builder.Services.AddInfrastructureBuildingBlocks();
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssembly(typeof(ApplicationBuildingBlocks).Assembly));
builder.Services.AddApplicationPipeline();

// === Servico transversal de DIAS UTEIS / PRAZOS LEGAIS (registro CENTRAL e UNICO — W9.1) ===
// ICalendarioDiasUteis + IFeriadosTenantProvider, Scoped (seguem o TenantContext). Consumido por
// Administracao (PNCP/art. 94), Transparencia (e-SIC) e futuras ondas (Obras §3, Convenios). Os
// modulos CONSOMEM por DI — nao recriam.
builder.Services.AddCalendarioDiasUteis();

// === Plataforma multi-tenant: catálogo de licenças de módulo por tenant ===
builder.Services.AddPlatform(
    builder.Configuration["Database:Provider"] ?? "Sqlite",
    builder.Configuration.GetConnectionString("Platform") ?? "Data Source=plataforma.db");

// Override de tenant por escopo (provisionamento e jobs de segundo plano).
builder.Services.AddScoped<TenantOverride>();

// Drenagem do Outbox: SOBRESCREVE o despachante default (escopo atual) por um que ISOLA cada
// mensagem em escopo de DI próprio (com TenantOverride da mensagem). Sem isso, uma mensagem cujos
// handlers tocam módulos distintos (ex.: domain event de Finanças + integration event de Administração)
// resolveria dois ModuleDbContext no mesmo escopo e acionaria a guarda H5. Vive no ApiHost porque
// depende de IServiceScopeFactory + TenantOverride (camada superior ao BuildingBlocks).
builder.Services.AddScoped<IOutboxMessageDispatcher, ScopedOutboxMessageDispatcher>();

// Assinatura A1 em ESCOPO DEDICADO: permite a handlers de outros modulos (ex.: AFD/AEJ do
// RecursosHumanos) assinarem via Cofre sem co-resolver o CofreDbContext no mesmo escopo do seu
// proprio ModuleDbContext (gatilho da guarda H5). Mesma motivacao do ScopedOutboxMessageDispatcher.
builder.Services.AddScoped<IAssinaturaEmEscopoDedicado, AssinaturaEmEscopoDedicado>();

// Portal do Cidadao: consulta de leitura cidada (Tributos/Protocolo via Contracts) em ESCOPO DEDICADO.
// O handler do portal ja resolveu o CidadaoDbContext no escopo da requisicao (ancora dado-proprio +
// trilha LGPD); a consulta do modulo-fonte resolveria um SEGUNDO ModuleDbContext lado-a-lado (gatilho
// da guarda H5). Isola a consulta num escopo proprio. Mesma motivacao do AssinaturaEmEscopoDedicado.
builder.Services.AddScoped<Tensorroot.Gov.Modules.Cidadao.Application.Abstractions.IConsultaCidadaoEmEscopoDedicado,
    Tensorroot.Gov.ApiHost.Cidadao.ConsultaCidadaoEmEscopoDedicado>();

// INVARIANTE DE BLOQUEIO do empenho (W9.1): Financas checa o status PNCP do contrato (Administracao via
// Contracts) em ESCOPO DEDICADO — o EmpenharHandler ja resolveu o FinancasDbContext; consultar a porta do
// Administracao no mesmo escopo resolveria o AdministracaoDbContext lado-a-lado (gatilho da guarda H5).
builder.Services.AddScoped<Tensorroot.Gov.Modules.Financas.Application.Abstractions.IConsultaContratoEmEscopoDedicado,
    Tensorroot.Gov.ApiHost.Financas.ConsultaContratoEmEscopoDedicado>();

// === Segurança: JWT Bearer (token AUTO-EMITIDO pelo módulo Identidade, HS256) ===
// Validamos o token assinado com o segredo simétrico de "Jwt:Secret" (Key Vault em produção).
// Sem Authority externa: o emissor é o próprio sistema.
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret não configurado (obrigatório para validar o token auto-emitido).");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "tensorroot.gov";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "tensorroot.gov";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false; // preserva "sub"/"email"/"perm" sem remapear para URIs do .NET
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });

// RBAC: provedor de políticas dinâmicas por permissão (claims "perm") + handler.
builder.Services.AddRbacPermissoes();

// === Deny-by-default GLOBAL (trava, não convenção): FallbackPolicy = RequireAuthenticatedUser ===
// Todo endpoint SEM metadado de autorização explícito (sem [Authorize]/RequirePermission/RequireAuthorization)
// passa a EXIGIR usuário autenticado. Assim, esquecer a anotação num endpoint novo FALHA FECHADO (401),
// em vez de expô-lo anonimamente. As superfícies legitimamente ANÔNIMAS (health, raiz, login/cadastro do
// cidadão e da identidade, portal público de transparência/e-SIC, e o Swagger só em Development) são
// marcadas com AllowAnonymous() — que tem precedência sobre a FallbackPolicy. Esta política só atua quando
// NENHUMA outra (Authorize/AllowAnonymous) está presente no endpoint.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// === Rate limiting (proteção contra abuso) ===
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
            }));

    // Policy DEDICADA "publico": superfície ANÔNIMA do portal de transparência
    // (/publico/transparencia/{slug}/...) é alvo de DoS/scraping. Limite PRÓPRIO, mais
    // restritivo que o global: SEMPRE particionado por IP (cidadão não tem token/Identity.Name),
    // janela curta e fila pequena (rejeita cedo em vez de enfileirar carga de abuso).
    options.AddPolicy("publico", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
            }));
});

// === Documentação (Swagger/OpenAPI) + health ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// JSON: aceita enums por NOME (ex.: "Secretaria") além de número — melhor DX e menos 400 na borda.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// === Ativação modular: descobre e registra os módulos licenciáveis por tenant ===
var modules = ModuleRegistry.Discover();
foreach (var module in modules)
{
    module.AddModule(builder.Services, builder.Configuration);
}

// Mapa rota→módulo para o gating de licenciamento (/api/<modulo> em minúsculas).
var rotasModulos = modules.ToDictionary(
    module => module.Name.ToLowerInvariant(),
    module => module.Name,
    StringComparer.Ordinal);

// Registra os módulos descobertos para injeção (provisionamento e drenagem de Outbox).
foreach (var module in modules)
{
    builder.Services.AddSingleton<IModule>(module);
}

builder.Services.AddSingleton<TenantProvisioner>();
builder.Services.AddHostedService<OutboxBackgroundService>();

// Contexto SOMENTE-LEITURA da trilha de auditoria do tenant (banco dedicado) p/ o visualizador admin.
builder.Services.AddDbContext<AuditoriaReadDbContext>((serviceProvider, options) =>
{
    var conexao = serviceProvider.GetRequiredService<ITenantConnectionResolver>().ResolveConnectionString();
    if (string.Equals(builder.Configuration["Database:Provider"], "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(conexao);
    }
    else
    {
        options.UseSqlite(conexao);
    }
});

// Registro de handlers do Outbox (evento → tipos de handler), capturado da coleção de serviços APÓS
// todos os módulos terem registrado seus INotificationHandler. Usado pelo ScopedOutboxMessageDispatcher
// para isolar cada consumidor em escopo próprio (guarda H5 com eventos consumidos por múltiplos módulos).
builder.Services.AddSingleton(OutboxHandlerRegistry.Construir(builder.Services));

var app = builder.Build();

app.UseSerilogRequestLogging();

// Tratamento global de erros — PRIMEIRO middleware após o logging, para capturar exceções de TODO o
// resto do pipeline (autenticação, gating de licença, endpoints e handlers MediatR). Responde
// application/problem+json via ProblemDetailsExceptionHandler. Sem opções → usa o IExceptionHandler.
app.UseExceptionHandler();

// Converte respostas de erro SEM corpo (ex.: 401/403/404 produzidos pelo próprio pipeline) em
// ProblemDetails, mantendo o contrato application/problem+json uniforme em toda a API.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// === Enrichers de observabilidade multi-tenant (W9.7) ===
// APÓS a autenticação (tenant já resolvido do JWT): injeta tenant.id + CorrelationId nos 3 sinais
// (logs via LogContext, traces via Activity/Baggage propagado pelo span processor, métricas via
// IHttpMetricsTagsFeature) e ecoa o CorrelationId na resposta. Antes do aquecimento/gating para que
// QUALQUER log/trace subsequente da requisição já carregue a identidade do tenant.
app.UseMiddleware<Tensorroot.Gov.ApiHost.Observabilidade.ContextoCorrelacaoMiddleware>();

// === P0-2: AQUECIMENTO ASSÍNCRONO do cache de conexão do tenant (anti thread-pool starvation) ===
// Roda APÓS a autenticação (tenant já resolvido do JWT) e ANTES de qualquer factory de DbContext de
// módulo. Decifra a connection string protegida (Key Vault em PROD) com `await`, FORA do factory
// síncrono do EF. Assim, quando os handlers/endpoints construírem seus DbContext, o
// TenantConnectionResolver bate no CACHE QUENTE e NUNCA bloqueia uma thread do pool em I/O ao Key Vault.
app.Use(async (context, next) =>
{
    var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
    if (tenantContext.HasTenant
        && context.RequestServices.GetRequiredService<ITenantConnectionResolver>() is TenantConnectionResolver resolver)
    {
        await resolver.AquecerAsync(context.RequestAborted);
    }

    await next();
});

// === Gating de licenciamento: bloqueia módulos não licenciados para o tenant ===
app.Use(async (context, next) =>
{
    var caminho = context.Request.Path.Value ?? string.Empty;
    if (caminho.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && caminho.Length > 5)
    {
        var segmento = caminho[5..].Split('/', 2)[0].ToLowerInvariant();
        if (rotasModulos.TryGetValue(segmento, out var moduloNome))
        {
            var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
            if (tenantContext.HasTenant)
            {
                var moduleProvider = context.RequestServices.GetRequiredService<ITenantModuleProvider>();
                if (!await moduleProvider.IsModuleEnabledAsync(tenantContext.TenantId, moduloNome, context.RequestAborted))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { erro = $"Módulo '{moduloNome}' não licenciado para este tenant." });
                    return;
                }
            }
        }
    }

    await next();
});

// Health check: ANÔNIMO (probes de liveness/readiness não têm token). AllowAnonymous tem precedência
// sobre a FallbackPolicy deny-by-default.
app.MapHealthChecks("/health").AllowAnonymous();

// Endpoint administrativo: provisiona um tenant (catálogo + migração do banco dedicado).
// AÇÃO DE OPERADOR DA PLATAFORMA (Tensorroot), NÃO de administrador de tenant (MODELO §7): exige a
// permissão de PLATAFORMA "plataforma.tenants.provisionar". Esse escopo NÃO pertence a
// Permissoes.Todas — logo o papel "Administrador" semeado por tenant nunca o recebe (deny-by-default
// preservado: admin de tenant ≠ operador de plataforma, garantido por construção no catálogo).
app.MapPost("/admin/tenants", async (
    ProvisionarTenantRequest requisicao,
    TenantProvisioner provisioner,
    CancellationToken cancellationToken) =>
{
    var tenantId = await provisioner.ProvisionarAsync(
        requisicao.Cnpj, requisicao.Nome, requisicao.Poder, requisicao.ConnectionString,
        requisicao.Modulos, cancellationToken);
    return Results.Ok(new { tenantId });
}).RequirePermission(Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes.PlataformaTenantsProvisionar);

// Endpoints administrativos (só-admin): configuração de licenças de módulo por tenant.
AdminEndpoints.Map(app);

foreach (var module in modules)
{
    module.MapEndpoints(app);
}

// Raiz: cartão de visita público (produto + módulos ativos). ANÔNIMO — AllowAnonymous tem precedência
// sobre a FallbackPolicy deny-by-default.
app.MapGet("/", () => Results.Ok(new
{
    produto = "Tensorroot.Gov",
    fase = 2,
    modulosAtivos = modules.Select(module => module.Name),
})).AllowAnonymous();

// Garante o banco de CONTROLE (plataforma) criado/migrado no startup.
// P0-5: a EVOLUÇÃO de schema do banco de controle DEVE passar por migrations — `EnsureCreatedAsync`
// cria o schema IGNORANDO migrations e NUNCA aplica migrations futuras (banco sem `__EFMigrationsHistory`),
// travando todo upgrade ("coluna inexistente" em runtime). Política fixada: PRODUÇÃO é EXCLUSIVAMENTE
// SqlServer (fail-fast no boot se for outro provider). `EnsureCreatedAsync` fica reservado APENAS ao
// fallback de DESENVOLVIMENTO (SQLite), onde não há pipeline de migrations do banco de controle.
await using (var escopoStartup = app.Services.CreateAsyncScope())
{
    var plataforma = escopoStartup.ServiceProvider.GetRequiredService<PlatformDbContext>();
    var provider = app.Configuration["Database:Provider"];
    var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);

    if (!app.Environment.IsDevelopment() && !ehSqlServer)
    {
        // Fail-fast: em PROD, qualquer provider != SqlServer é configuração inválida (trava evolutiva
        // de schema + trilha de auditoria fisicamente alterável fora de SqlServer — ver P1-7).
        throw new InvalidOperationException(
            $"Database:Provider inválido em ambiente '{app.Environment.EnvironmentName}': '{provider ?? "(ausente)"}'. " +
            "PRODUÇÃO exige Database:Provider=SqlServer (migrations + trilha WORM). " +
            "O fallback SQLite/EnsureCreated é exclusivo de DESENVOLVIMENTO.");
    }

    if (ehSqlServer)
    {
        // SEMPRE migrar o banco de controle: aplica migrations pendentes num banco existente.
        await plataforma.Database.MigrateAsync();
    }
    else
    {
        // DEV-only (SQLite): sem pipeline de migrations do banco de controle.
        await plataforma.Database.EnsureCreatedAsync();
    }
}

// === FAN-OUT de migração dos tenants existentes (database-per-tenant — W9.7) ===
// Após migrar o banco de CONTROLE, leva as evoluções de schema por MÓDULO (ex.: a tabela InboxMessages
// do Inbox idempotente) a TODOS os bancos dedicados já provisionados. Idempotente (MigrateAsync aplica só
// pendentes; SchemaProvisioner é idempotente). Em DEV (SQLite) roda igualmente — o tenant demo abaixo já
// nasce com o schema novo, mas tenants criados em execuções ANTERIORES recebem o Inbox aqui. Resiliente:
// um banco indisponível não pode travar o boot da aplicação inteira.
await using (var escopoFanOut = app.Services.CreateAsyncScope())
{
    try
    {
        await escopoFanOut.ServiceProvider.GetRequiredService<TenantProvisioner>()
            .MigrarTenantsExistentesAsync(CancellationToken.None);
    }
    catch (Exception excecao)
    {
        app.Logger.LogError(excecao, "Falha no fan-out de migração dos tenants existentes (boot prossegue).");
    }
}

// === Bootstrap de DEV: provisiona o tenant demo (+ admin) se ainda não houver tenant. ===
// Resolve o "ovo-e-galinha" do onboarding e faz o login real funcionar de imediato.
// Admin padrão (appsettings): admin@tensorroot.gov / Mudar@123.
if (app.Environment.IsDevelopment())
{
    await using var escopoSeed = app.Services.CreateAsyncScope();
    var catalogo = escopoSeed.ServiceProvider.GetRequiredService<PlatformDbContext>();
    if (!await catalogo.Tenants.AnyAsync())
    {
        var provisioner = escopoSeed.ServiceProvider.GetRequiredService<TenantProvisioner>();
        await provisioner.ProvisionarAsync(
            "11.222.333/0001-81",
            "Prefeitura de Maximiliano de Almeida/RS",
            PoderTenant.Executivo,
            connectionString: null,
            modules.Select(module => module.Name).ToList(),
            CancellationToken.None);
    }
}

app.Run();

/// <summary>Ponto de entrada exposto para testes de integração (WebApplicationFactory).</summary>
public partial class Program
{
}

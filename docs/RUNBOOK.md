# RUNBOOK — Tensorroot.Gov

Guia operacional **prático** para um dev novo. Tudo aqui foi verificado contra os
scripts e arquivos reais do repositório (`global.json`, `Directory.Build.props`,
`src/ApiHost/Program.cs`, `src/Web/vite.config.ts`, `.github/workflows/ci.yml`).

> Sobre a versão do .NET: os projetos têm como **alvo `net8.0`**
> (`Directory.Build.props`), mas o `global.json` usa `rollForward: latestMajor`,
> então **o SDK .NET 10 compila** a solução normalmente. Para **executar** o binário
> (que está pinado em `net8.0` no `runtimeconfig.json`) numa máquina que só tem o
> **runtime .NET 10**, você precisa de `DOTNET_ROLL_FORWARD=Major`. É por isso que
> esse env var aparece nos comandos de `run`/`test` abaixo, e **não** no de `build`.

---

## 1. Pré-requisitos

| Ferramenta | Versão | Observação |
|---|---|---|
| .NET SDK | 8.0+ (10.x funciona via `rollForward: latestMajor`) | `dotnet --version` |
| .NET Runtime | 8 **ou** 10 | se for só o 10, use `DOTNET_ROLL_FORWARD=Major` p/ rodar |
| Node.js | 18+ (LTS) | para o frontend em `src/Web` |
| npm | acompanha o Node | |

Banco em DEV: **SQLite** (provider default em `appsettings.json` →
`"Database": { "Provider": "Sqlite" }`). Nenhum SQL Server/Azure SQL é necessário
localmente — os arquivos `.db` são criados automaticamente no startup.

Sanidade:

```bash
dotnet --version          # 8.x ou 10.x
dotnet --list-runtimes    # confirme um Microsoft.AspNetCore.App 8.x OU 10.x
node --version            # >= 18
```

---

## 2. Build

A partir da raiz do repo (`/Users/igorgewehr/Development/Tensorroot.Gov`):

```bash
dotnet restore Tensorroot.Gov.sln
dotnet build  Tensorroot.Gov.sln -c Release --no-restore
```

Build **não precisa** de `DOTNET_ROLL_FORWARD` (o `global.json` já cobre o SDK).
Verificado: `dotnet build` da `Tensorroot.Gov.ApiHost.csproj` compila com 0 erros /
0 avisos no SDK 10.0.301.

Pontos de atenção do build (são **erros**, não avisos — vêm do `Directory.Build.props`):

- `TreatWarningsAsErrors=true` — qualquer warning quebra o build.
- `NuGetAudit` ligado — pacote com vulnerabilidade conhecida quebra o build.
- `Nullable=enable`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile=true`
  (todo membro público precisa de doc XML).

Build só do host (iteração rápida):

```bash
dotnet build src/ApiHost/Tensorroot.Gov.ApiHost.csproj -c Debug
```

Saída do host: `src/ApiHost/bin/<Config>/net8.0/Tensorroot.Gov.ApiHost.dll`.

---

## 3. Testar

### 3.1 Backend (.NET — inclui as Fitness Functions)

```bash
DOTNET_ROLL_FORWARD=Major dotnet test Tensorroot.Gov.sln -c Release
```

> O `DOTNET_ROLL_FORWARD=Major` é necessário se você só tem o runtime .NET 10 — o
> test host roda os assemblies `net8.0`. Com runtime 8 instalado, pode omitir.

Há ~18 projetos de teste em `tests/` (um por módulo + `Platform.Tests` +
`Integracoes.Tests` + `ArchitectureTests`). Rodar um só:

```bash
DOTNET_ROLL_FORWARD=Major dotnet test tests/Tensorroot.Gov.Modules.Financas.Tests/Tensorroot.Gov.Modules.Financas.Tests.csproj
```

### 3.2 Frontend (`src/Web`)

```bash
cd src/Web
npm ci            # primeira vez (ou npm install)
npm test          # vitest run (scripts.test → "vitest run")
npx tsc --noEmit  # checagem de tipos isolada
npm run build     # tsc && vite build  (build de produção, valida tipos + bundla)
npm run lint      # eslint + stylelint
```

Vitest está configurado em `vite.config.ts` (jsdom, setup em
`src/test/setup.ts`, inclui `src/**/*.{test,spec}.{ts,tsx}`).

---

## 4. Subir local

Dois processos: **API (.NET)** na porta **5080** e **Vite** na **5173**. O Vite faz
proxy de `/api` → `http://localhost:5080` (definido em `vite.config.ts`).

> Não há `launchSettings.json` / pasta `Properties` no `ApiHost`, então **a porta
> 5080 não está fixada no repo** — você precisa passá-la explicitamente. O proxy do
> Vite espera 5080; use exatamente essa porta.

### 4.1 API (terminal 1)

```bash
ASPNETCORE_ENVIRONMENT=Development \
DOTNET_ROLL_FORWARD=Major \
dotnet run --project src/ApiHost/Tensorroot.Gov.ApiHost.csproj --urls http://localhost:5080
```

Ou rodando o DLL já buildado:

```bash
ASPNETCORE_ENVIRONMENT=Development DOTNET_ROLL_FORWARD=Major \
  dotnet src/ApiHost/bin/Debug/net8.0/Tensorroot.Gov.ApiHost.dll --urls http://localhost:5080
```

No startup, em **Development**, o `Program.cs`:

1. Cria/migra o banco de **controle** da plataforma (`plataforma.db`,
   `EnsureCreatedAsync` em SQLite).
2. **Bootstrap do tenant demo** se ainda não houver tenant: provisiona a
   *Prefeitura de Maximiliano de Almeida/RS* (CNPJ `11.222.333/0001-81`, Poder
   Executivo) com **todos os módulos licenciados**, e semeia o admin.
3. O `SchemaProvisioner` (`EnsureCreatedAsync`) cria as tabelas de cada módulo no
   **banco dedicado do tenant** (database-per-tenant; arquivo SQLite por tenant via
   `TenantConnectionResolver.ConexaoPadrao(tenantId)`).

Credenciais do admin demo (de `appsettings.Development.json`, seção
`Identidade:Admin`, com fallback hardcoded em `IdentidadeModule`):

```
email: admin@tensorroot.gov
senha: Mudar@123
```

URLs úteis com a API no ar:

- `http://localhost:5080/`        → produto + módulos ativos
- `http://localhost:5080/health`  → health check
- `http://localhost:5080/swagger` → Swagger UI (só em Development)

### 4.2 Frontend (terminal 2)

```bash
cd src/Web
npm ci          # se ainda não instalou
npm run dev     # vite, sobe em http://localhost:5173
```

Abra `http://localhost:5173`. Chamadas a `/api/...` são proxiadas para a API na 5080.

---

## 5. Fluxo: criar → persistir → auditar um registro

O eixo é **multi-tenant + JWT auto-emitido + auditoria imutável**. Passo a passo via API:

### 5.1 Login (obtém o JWT — resolve o tenant pelo e-mail)

`POST /api/identidade/login` é **anônimo**. O corpo é `{ Email, Senha }`
(`record LoginPayload(string Email, string Senha)`). O índice central resolve o
tenant pelo e-mail e autentica no banco dedicado do tenant.

```bash
TOKEN=$(curl -s -X POST http://localhost:5080/api/identidade/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@tensorroot.gov","senha":"Mudar@123"}' \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["accessToken"])')
echo "$TOKEN"
```

A resposta é `{ accessToken, expiraEm }`. O **tenant não vai em header** — ele está
embutido na claim `tenant_id` do JWT (`TenantContext` lê `tenant_id` do token; veja
`src/ApiHost/Tenancy/TenantContext.cs`). Basta enviar o Bearer.

### 5.2 Criar/persistir um registro (qualquer módulo licenciado)

Toda escrita exige `Authorization: Bearer $TOKEN`. O middleware de **gating de
licenciamento** (`Program.cs`) bloqueia com **403** rotas `/api/<modulo>` de módulos
não licenciados para o tenant. RBAC dinâmico exige a **permissão** correspondente
(claim `perm`); o admin semeado recebe `Permissoes.Todas` do tenant.

```bash
curl -X POST http://localhost:5080/api/identidade/usuarios \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{ ... payload do recurso ... }'
```

A persistência grava no **banco dedicado do tenant**. Eventos de domínio viram
integration events via **Outbox** (drenado pelo `OutboxBackgroundService`).

### 5.3 Auditar

Toda mutação de estado gera trilha **imutável**. Consulte pelo visualizador admin
(exige permissão `admin.auditoria.ver`):

```bash
curl -G http://localhost:5080/api/admin/auditoria \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode 'entidade=Usuario' \
  --data-urlencode 'pagina=1' \
  --data-urlencode 'tamanho=50'
```

Filtros: `entidade`, `usuario`, `acao`, `pagina`, `tamanho` (máx. por página
limitado no código). Retorna `{ total, pagina, tamanho, itens[] }`, cada item com
`EntityName`, `EntityId`, `Action`, `UserId`, `IpAddress`, `TimestampUtc`,
`OldValues`, `NewValues`. A leitura sai do `AuditoriaReadDbContext` (banco dedicado
do tenant, somente-leitura).

---

## 6. Provisionamento por tenant + SchemaProvisioner (DEV SQLite)

- **Banco DEDICADO por tenant** (não schema-por-módulo). O
  `ITenantConnectionResolver` resolve a connection string; em DEV cada tenant é um
  arquivo SQLite (`TenantConnectionResolver.ConexaoPadrao(tenantId)`).
- O **banco de controle/plataforma** (`plataforma.db`) guarda o catálogo de tenants
  e licenças de módulo, além do índice central de login (e-mail → tenant).
- `TenantProvisioner.ProvisionarAsync(...)` (`src/ApiHost/Provisioning/`): grava o
  catálogo e, para cada módulo licenciado, chama `module.MigrarBancoAsync(...)`, que
  por sua vez aplica o **`SchemaProvisioner.AplicarAsync`**
  (`src/BuildingBlocks/.../Multitenancy/SchemaProvisioner.cs`) — em DEV é
  `EnsureCreatedAsync` (cria tabelas faltantes; vários módulos compartilham o arquivo
  do tenant). O módulo Identidade ainda **semeia o admin** do tenant.

Provisionar um tenant novo manualmente (ação de **operador de plataforma**, não de
admin de tenant — exige a permissão `plataforma.tenants.provisionar`, que **não**
está em `Permissoes.Todas`):

```bash
curl -X POST http://localhost:5080/admin/tenants \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
        "cnpj": "00.000.000/0001-00",
        "nome": "Câmara Municipal de Exemplo/RS",
        "poder": "Legislativo",
        "connectionString": null,
        "modulos": ["Identidade","Financas","Legislativo","Transparencia"]
      }'
```

`connectionString: null` → usa o caminho SQLite padrão do tenant. Resposta:
`{ tenantId }`.

### Resetar o estado local

Em DEV, apague os arquivos `.db` (controle + tenants) e suba a API de novo — o
bootstrap recria o tenant demo + admin. Os `.db` ficam no diretório de trabalho do
processo (ex.: `plataforma.db` ao lado de onde você rodou `dotnet run`). O
`.gitignore` ignora os artefatos de banco.

---

## 7. Onde ficam os segredos

| Segredo | DEV | PROD |
|---|---|---|
| `Jwt:Secret` (HS256, auto-emitido) | `appsettings.Development.json` (valor `DEV-ONLY-...`) | **Azure Key Vault** |
| KEK do Cofre (envelope encryption A1) | `Cofre:ProvedorKek=Config` + `Cofre__KekBase64` via **variável de ambiente** (nunca versionar) | `Cofre:ProvedorKek=KeyVault` + `Cofre:KeyVaultUri` |
| Connection strings | `appsettings.json` (`ConnectionStrings:Platform = plataforma.db`) | Key Vault / config segura |

Regras (CONVENCOES-ENGENHARIA.md §6/§7, ADR-0008 envelope encryption):

- **Nunca** versionar a KEK nem segredos de produção no repo.
- Em DEV, fornecer a KEK por env: `export Cofre__KekBase64=<base64-de-32-bytes>`
  (provedor `Config`, `KekKeyIdDev=dev-local-kek-v1`).
- Em PROD, `ProvedorKek=KeyVault` faz o wrap/unwrap da DEK no Key Vault; o A1 fica
  **cifrado no banco do tenant** e a KEK **nunca sai** do Key Vault.

---

## 8. Troubleshooting comum

**`Jwt:Secret não configurado` no startup**
A API lança `InvalidOperationException` se `Jwt:Secret` faltar. Garanta
`ASPNETCORE_ENVIRONMENT=Development` (carrega `appsettings.Development.json`) ou
forneça `Jwt__Secret` por env.

**Porta 5080 ocupada (`address already in use`)**
Ache e mate o processo, ou rode em outra porta (e ajuste o proxy do Vite):

```bash
lsof -ti tcp:5080 | xargs kill        # libera a 5080
# alternativa: --urls http://localhost:5081  (e mude o target em vite.config.ts)
```

**401 Unauthorized**
- Faltou o header `Authorization: Bearer <token>` ou o token expirou (`expiraEm`;
  duração padrão 60 min — `Jwt:DuracaoMinutos`).
- Login retorna 401 genérico ("Credenciais inválidas") tanto para senha errada
  quanto para e-mail inexistente (anti-enumeration, por design). Confira
  e-mail/senha do admin demo.
- Sem `ClockSkew` suficiente: o relógio da máquina muito fora do ar pode invalidar o
  token (skew de 1 min).

**403 Forbidden numa rota `/api/<modulo>`**
Módulo **não licenciado** para o tenant (gating em `Program.cs`) **ou** falta a
**permissão** RBAC. Licencie o módulo (endpoints admin em `AdminEndpoints`) ou use o
admin demo (tem `Permissoes.Todas`). `403` em `/admin/tenants` é esperado para admin
de tenant — esse endpoint é de **operador de plataforma**.

**Não consigo *rodar* o binário: erro de framework `net8.0` não encontrado**
Você só tem o runtime .NET 10. Rode com `DOTNET_ROLL_FORWARD=Major` (ou
`--roll-forward Major`). O `runtimeconfig.json` do host pede `Microsoft.NETCore.App`
e `Microsoft.AspNetCore.App` 8.0.0.

**Build vermelho no meio do workflow**
Lembre que `TreatWarningsAsErrors=true` e `NuGetAudit` estão ligados: um *warning* de
nullability, doc XML faltando, code style, ou pacote vulnerável **falha** o build.
Leia a primeira linha de erro real (não o resumo). Para isolar, builde só o projeto
que mudou: `dotnet build <projeto>.csproj`.

**Frontend não enxerga a API (CORS / 404 em `/api`)**
A API precisa estar na **5080** (alvo do proxy do Vite). Se subiu noutra porta,
ajuste `target` em `src/Web/vite.config.ts` ou rode a API na 5080.

**Banco demo "sujo" / quero recomeçar**
Pare a API, apague `plataforma.db` e os `.db` de tenant, suba de novo (Seção 6).

---

## 9. Fitness Functions (testes de arquitetura)

São testes **NetArchTest** que blindam a Clean Architecture e o isolamento entre
Bounded Contexts (CONVENCOES-ENGENHARIA.md §2/§12). Ficam em
`tests/Tensorroot.Gov.ArchitectureTests/` (`FitnessFunctions.cs`,
`MaintainabilityTests.cs`, `SpecCodeConsistency.cs`). Exemplos do que validam: Domain
não depende de EF Core / ASP.NET; sem dependências cruzadas entre módulos.

Rodar só elas:

```bash
DOTNET_ROLL_FORWARD=Major dotnet test \
  tests/Tensorroot.Gov.ArchitectureTests/Tensorroot.Gov.ArchitectureTests.csproj
```

No CI (`.github/workflows/ci.yml`) elas rodam junto com `dotnet test` da solução
inteira (job *Build, Test e Auditoria*), que também executa
`dotnet list ... package --vulnerable` (auditoria informativa) — espelhe esse fluxo
localmente antes de abrir PR:

```bash
dotnet restore Tensorroot.Gov.sln
dotnet build   Tensorroot.Gov.sln -c Release --no-restore
DOTNET_ROLL_FORWARD=Major dotnet test Tensorroot.Gov.sln -c Release --no-build
```

---

## Apêndice — Geração de módulos de regras (Rules-as-Code)

A fábrica de regras tem um gerador em `tools/workflows/gerar-modulo-de-regras.js`
(Node). Use-o para criar/atualizar os artefatos `*.rules.md` + manifesto conforme o
processo Rules-as-Code:

```bash
node tools/workflows/gerar-modulo-de-regras.js
```

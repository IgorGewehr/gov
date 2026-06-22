# Verificação Cética — Injeção, Exposição de Segredos e Upload

> Verificação **NO CÓDIGO** de cada achado do red-team (`ataque-injecao-segredos.md`).
> Critério: cada item é **VULNERABILIDADE REAL** (confirmada com evidência arquivo:linha) ou
> **FALSO-POSITIVO / mitigado** (rebaixado, com a evidência da mitigação).
> Metodologia: leitura direta dos arquivos citados + checagem do pipeline + busca por filtros/
> interceptors/testes que mitiguem. Sem runtime (5080 não tocado). Data: 2026-06-22.

## Veredito resumido

| # | Achado | Severidade original | Veredito | Severidade final |
|---|--------|--------------------|----------|------------------|
| 1 | Connection string do tenant em claro no catálogo | ALTA | **REAL — confirmado** | **ALTA** |
| 2 | Sem handler global de exceção (vaza stack/detalhe) | MÉDIA | **REAL — confirmado** | **MÉDIA** |
| 3 | Provisionamento aceita ConnectionString crua do body | MÉDIA | **REAL — confirmado** | **MÉDIA** |
| 4 | `appsettings.Development.json` versionado com segredo/senha | MÉDIA | **REAL — confirmado** | **MÉDIA** (DEV-only → rebaixável a BAIXA com ressalva) |
| 5 | HS256 sem validação de tamanho mínimo do segredo | MÉDIA | **REAL — confirmado** | **MÉDIA** |
| 6 | Validação de cadeia ICP-Brasil é no-op (só KeyUsage) | MÉDIA | **REAL — confirmado** | **MÉDIA** (ALTA para go-live de produção) |
| 7 | `Type.GetType` na desserialização do Outbox | BAIXA | **REAL — confirmado** (risco baixo hoje) | **BAIXA** |
| 8 | DDL do AuditTrail interpola `Schema` | BAIXA (defesa-em-profundidade) | **FALSO-POSITIVO como SQLi** — não injetável; dívida válida | **INFO/BAIXA** |
| 9 | Rate limiting não protege login por conta | BAIXA | **REAL — confirmado** | **BAIXA** |
| 10 | BCrypt trunca em 72 bytes | BAIXA | **REAL — confirmado** | **BAIXA** |
| 11 | `excecao.Message` ecoado ao cliente | INFO | **REAL como padrão de risco** — mensagens atuais seguras | **INFO** |

**Total: 9 vulnerabilidades reais (1 ALTA, 5 MÉDIA, 3 BAIXA) + 1 INFO (padrão de risco) + 1 falso-positivo de SQLi rebaixado (#8).**

O núcleo criptográfico do Cofre e a proteção XXE foram **reconfirmados como sólidos** (seção final).

---

## Confirmação item a item

### #1 [ALTA] Connection string em claro no catálogo — **REAL**
**Evidência no código:**
- `src/Platform/Tensorroot.Gov.Platform/Tenancy/Tenancy.cs:51` — `public string? ConnectionString { get; private set; }` (texto puro na entidade `Tenant`).
- `src/Platform/Tensorroot.Gov.Platform/Persistence/PlatformDbContext.cs:35` — `builder.Property(tenant => tenant.ConnectionString).HasMaxLength(500);` — **sem `HasConversion`/cifra de coluna**. Confirmado: a coluna persiste a string crua.
- Origem do dado: `src/ApiHost/Program.cs:221-230` → `TenantProvisioner.ProvisionarAsync` (`src/ApiHost/Provisioning/TenantProvisioner.cs:33-34,39-41`) grava a string recebida.
- `TenantConnectionResolver.cs:85-92` lê `tenant.ConnectionString` direto do catálogo e a serve (com cache em memória de 5 min) para abrir os bancos dedicados.

**Sem mitigação:** não há interceptor de cifra, nem referência a Key Vault para esse campo. Em PROD (`Database:Provider = SqlServer`) a coluna contém credenciais do banco dedicado de cada município.
**Veredito: VULNERABILIDADE REAL.** Viola CLAUDE.md §6 ("segredos só no Key Vault"). Mantém ALTA: leitura do banco de controle (backup, replica, insider, SQLi futura) expõe as credenciais de **todos** os tenants.

> Ressalva honesta: em DEV o campo é tipicamente `null` (o seed passa `connectionString: null`, `Program.cs:275`) e cai no fallback SQLite por convenção (`ConexaoPadrao`, `TenantConnectionResolver.cs:99-100`). O risco materializa-se em PROD, quando o provisionamento passar strings reais de SqlServer. A vulnerabilidade é estrutural (o modelo permite e o caminho de PROD exige), não cosmética.

### #2 [MÉDIA] Sem handler global de exceção — **REAL**
**Evidência no código:** busca em todo `src/` por `UseExceptionHandler|AddProblemDetails|UseDeveloperExceptionPage|UseStatusCodePages` retornou **zero ocorrências**. O pipeline (`Program.cs:176-212`) tem `UseSerilogRequestLogging`, `UseRateLimiter`, `UseAuthentication/Authorization` e o middleware de gating, **mas nenhum tratamento central de exceção**.
Exceções não tratadas reais existem no caminho quente: `AlterarSenha.cs:38` (`throw new InvalidOperationException("Usuario nao encontrado.")`) e `TenantConnectionResolver.cs:79` (`InvalidOperationException` de tenant ausente) sobem sem captura.
**Veredito: VULNERABILIDADE REAL (MÉDIA).** O corpo exato (stack trace x 500 vazio) depende do ambiente — **[a confirmar em runtime]** —, mas a **ausência** do hardening é fato no código. Em PROD GovTech sob TCE, deve haver `ProblemDetails` curado.

### #3 [MÉDIA] Provisionamento aceita ConnectionString crua do body — **REAL**
**Evidência no código:** `Program.cs:221-228` desserializa `ProvisionarTenantRequest` direto do body e repassa `requisicao.ConnectionString` para `provisioner.ProvisionarAsync`; `TenantProvisioner.cs:39-49` usa a string como `conexao` e chama `module.MigrarBancoAsync(conexao, ...)`, que **abre conexão real e migra schema**. Não há allow-list de host nem bloqueio de keywords do provider.
**Mitigação parcial existente:** o endpoint exige a permissão de plataforma `PlataformaTenantsProvisionar` (`Program.cs:230`), que não pertence a `Permissoes.Todas` — logo admin de tenant não a recebe. Isso **reduz o conjunto de atacantes** (precisa de operador de plataforma ou token de plataforma comprometido), mas **não elimina** o SSRF-de-banco / injeção de parâmetros de conexão.
**Veredito: VULNERABILIDADE REAL (MÉDIA).** A barreira de autorização justifica manter MÉDIA (não ALTA), mas o campo não-validado é superfície real de pivot lateral.

### #4 [MÉDIA] `appsettings.Development.json` versionado com segredo/senha — **REAL**
**Evidência no código:**
- `appsettings.Development.json:12` — `"Secret": "DEV-ONLY-troque-este-segredo-de-32-bytes-ou-mais-em-producao-via-KeyVault"`.
- `:18-21` — `Identidade:Admin` = `admin@tensorroot.gov` / `Mudar@123`.
- `.gitignore:26-27` ignora `secrets.json` e `appsettings.*.Local.json`, **mas NÃO** `appsettings.Development.json` (confirmado: nenhuma linha do `.gitignore` casa com esse arquivo). Logo ele **está versionado**.
- O seed (`Program.cs:264-279`) provisiona o tenant demo; o admin padrão vem desse appsettings.
**Veredito: VULNERABILIDADE REAL.** Rebaixável a **BAIXA** porque é explicitamente DEV (`RequireHttpsMetadata` e bootstrap só em `IsDevelopment()`), mas mantém peso porque (a) credencial conhecida em repo é antipadrão e (b) se um ambiente de homologação acessível subir com esse arquivo, qualquer um forja JWT (segredo conhecido) ou loga como admin. Risco condicionado ao deploy de homologação.

### #5 [MÉDIA] HS256 sem validação de tamanho mínimo — **REAL**
**Evidência no código:**
- `Program.cs:83-102` — lê `Jwt:Secret`, valida apenas **não-nulo** (`?? throw`), e cria `new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))` **sem checar comprimento**.
- `EmissorToken.cs:45-77` — na emissão, valida só `string.IsNullOrWhiteSpace(_opcoes.Secret)` (linha 45) e cria a chave (linha 76) **sem checar tamanho**.
Não há `>= 32 bytes`/`KeySize` em nenhum dos dois pontos, nem teste que o cubra (busca em `tests/` por validação de tamanho de segredo: nada).
**Veredito: VULNERABILIDADE REAL (MÉDIA).** Um segredo curto em PROD (erro de config) é aceito silenciosamente, viabilizando brute-force offline da chave HS256 e forja de tokens com qualquer claim `perm` (bypass do RBAC). Deve falhar-rápido no startup e na emissão.

### #6 [MÉDIA] Validação ICP-Brasil é no-op — **REAL**
**Evidência no código:** `ValidacaoCadeiaIcpBrasil.cs`:
- `Validar` (linhas 22-36) só chama `PermiteAssinatura` e **comenta o TODO** de cadeia/revogação (linhas 32-35) — a verificação de cadeia/CRL/OCSP está **desligada**.
- `PermiteAssinatura` (linhas 38-56) — `return !temKeyUsage;` (linha 55): **na ausência de extensão KeyUsage, retorna `true` (aceita)**. Confirmado o comportamento descrito.
- Caminho de upload: `CofreEndpoints.cs:34-71` recebe o `.pfx` e chama a custódia → `CofreCripto.CifrarParaCadastroAsync` → `ValidacaoCadeiaIcpBrasil.Validar` (`CofreCripto.cs:70`). Um cert autoassinado que abra com a senha e tenha (ou omita) KeyUsage é aceito e custodiado como A1 ativo.
**Veredito: VULNERABILIDADE REAL.** MÉDIA hoje (endpoint exige `admin.certificado.gerenciar`), **ALTA como bloqueador de go-live**: documentos eSocial/TCE assinados com cert não confiável → repúdio/rejeição pelo órgão. O próprio código admite no TODO que a validação estrita é obrigatória "antes de produção".

### #7 [BAIXA] `Type.GetType` no Outbox — **REAL (risco baixo)**
**Evidência no código:** `OutboxPublisher.cs:64-66` — `Type.GetType(mensagem.Type)` seguido de `JsonSerializer.Deserialize(mensagem.Content, tipo)`.
**Atenuantes confirmados:** a coluna `Type` é escrita pelo próprio sistema ao enfileirar (não é entrada direta do usuário); System.Text.Json sem `TypeNameHandling` limita gadgets; o despacho é isolado por escopo (linhas 69-72). O risco exige um atacante já capaz de escrever na tabela Outbox (SQLi futura/insider).
**Veredito: REAL como superfície desnecessária, severidade BAIXA.** Recomenda-se allow-list de tipos (nome lógico → `Type`).

### #8 [BAIXA] DDL do AuditTrail interpola `Schema` — **FALSO-POSITIVO como SQLi (dívida válida)**
**Evidência no código:** `SchemaProvisioner.cs`:
- `:62` — `var schema = (contexto as ModuleDbContext)?.Schema ?? "dbo";` — `Schema` é **literal do código do módulo**, não entrada de usuário.
- `:66-92` — interpola `{schema}` no `CREATE TRIGGER` via `ExecuteSqlRawAsync`.
- `:139` — `ALTER TABLE ... ADD COLUMN "{coluna}" {definicao}` — `coluna`/`definicao` são **constantes** passadas em `:120-122` ("AttemptCount", "NextAttemptUtc", "DeadLetteredOnUtc" + definições fixas).
- `:36-41` — o `GenerateCreateScript` é gerado pelo EF e só sofre `Replace` de literais (`CREATE TABLE ` → `... IF NOT EXISTS `). Nenhum dos três pontos toca dado externo.
**Veredito: NÃO é SQLi explorável hoje (FALSO-POSITIVO como vulnerabilidade ativa).** Rebaixo para **INFO/dívida de defesa-em-profundidade**: se um dia o schema virar configurável por dado externo, a montagem por string é frágil — então a recomendação (validar `^[a-z][a-z0-9_]*$` / `QUOTENAME`) permanece válida como guarda preventiva, sem urgência.

### #9 [BAIXA] Rate limiting não protege login por conta — **REAL**
**Evidência no código:**
- `Program.cs:114-127` — limiter global particiona por `User.Identity?.Name ?? RemoteIpAddress ?? "anonymous"`, 100 req/min.
- `IdentidadeEndpoints.cs:29-63` — `/login` é `AllowAnonymous` (sem `Name` → particiona por IP).
- Busca por lockout/backoff por conta no módulo Identidade: **nenhum código** (`lockout/Tentativa/bloqueio` só aparecem em artefatos `bin/`, não em fonte). O handler `Autenticar.cs:77-103` não conta falhas nem trava conta.
**Mitigações reais confirmadas (que limitam o impacto):** mensagem de falha **constante** "Credenciais invalidas." (`Autenticar.cs:57`), verificação de hash sintético mesmo sem usuário (`:84,95`) contra enumeração por timing, e BCrypt(12) lento (`SenhaHasher.cs:14`).
**Veredito: VULNERABILIDADE REAL (BAIXA).** Sem trava por conta/backoff/CAPTCHA, um pool de IPs distribui o brute-force. Efetividade real depende do volume — **[a confirmar em runtime]**. Recomenda-se limiter por e-mail-alvo no `/login`.

### #10 [BAIXA] BCrypt trunca em 72 bytes — **REAL**
**Evidência no código:** `AlterarSenha.cs:21` — `MaximumLength(256)`; `SenhaHasher.cs:24-28` — `BCryptNet.HashPassword(senha, workFactor)` **sem pré-hash SHA-256**. Senhas > 72 bytes têm o excedente silenciosamente ignorado pelo BCrypt.
**Veredito: VULNERABILIDADE REAL (BAIXA).** Impacto limitado (≥73 bytes incomum). Recomenda-se pré-hash SHA-256(Base64) antes do BCrypt, ou limitar input a ≤72 bytes.

### #11 [INFO] `excecao.Message` ecoado ao cliente — **REAL como padrão de risco; conteúdo atual seguro**
**Evidência no código:**
- `CofreEndpoints.cs:69,85,114` — `erro = excecao.Message` de `CertificadoInvalidoException`. As mensagens inspecionadas são genéricas e a de cripto **não** vaza material (`CofreCripto.cs:93-94,146`).
- `IdentidadeEndpoints.cs:126,142` — `erro = excecao.Message` de `ConcessaoNaoAutorizadaException` (mensagem de domínio curada).
**Veredito: INFO confirmado.** Hoje as mensagens são seguras (exceções de domínio explicitamente curadas), mas é um padrão que, combinado com a ausência de sanitização central (#2), vaza se uma exceção futura embutir dado sensível. Manter como INFO + recomendar `ProblemDetails` central.

---

## Reconfirmação dos pontos SEGUROS (auditoria do red-team defensivo)

Verifiquei também as afirmações de "não-achado" — todas **procedem**:

- **SQLi:** confirmado que os únicos `ExecuteSqlRaw` são `SchemaProvisioner.cs:41,92` (DDL com literais controlados pelo código). Nenhum `FromSqlRaw`/`ExecuteSqlRaw` com entrada de usuário. Filtros do visualizador de auditoria (`AdminEndpoints.cs:100-114`) usam LINQ parametrizado por EF; paginação com `Math.Clamp(..., 1, 200)` (`:98`). Gateway ADN (`AdnNfseGateway.cs:19-21`) usa `Uri.EscapeDataString` no CNPJ.
- **XXE:** `AssinadorXmlDsig.cs:37-39` cria `XmlReader` com `XmlResolver = null` e `DtdProcessing.Prohibit`. Confirmado.
- **Cofre/KEK:** AES-256-GCM com nonce aleatório por operação (`AesGcmEnvelope.cs:28`), AAD = TenantId||Thumbprint (`CofreCripto.cs:181-182`), `EphemeralKeySet` (`CofreCripto.cs:169`), `CryptographicOperations.ZeroMemory` no `finally` (`CofreCripto.cs:98-100,150-152`), KEK exigida via config externa e validada em 32 bytes, **nunca hardcoded** (`ProvedorKekConfig.cs:26-37`). Mensagens de erro de cripto não vazam material (`CofreCripto.cs:94,146`). Confirmado sólido.
- **Upload .pfx:** valida `HasFormContentType` (`CofreEndpoints.cs:39`), exige campos (`:49`), limita a 256 KB (`:24,54`), processa só em `MemoryStream` (`:59`), nunca grava em disco. Confirmado.
- **Login:** mensagem de falha constante e indistinguível (`Autenticar.cs:57`) + hash sintético contra timing (`:84,95`). Confirmado.

### Nuance adicional encontrada (não estava no relatório original)
- `CofreCripto.cs:171-176` — no fallback de **macOS dev**, o `.pfx` é carregado com `X509KeyStorageFlags.Exportable` (em vez de `EphemeralKeySet`, não suportado no macOS). Está corretamente **cercado por `catch (PlatformNotSupportedException)`** e documentado como dev-local; PROD roda em container Linux com `EphemeralKeySet`. **Não é vulnerabilidade** (não atinge produção), mas registro como observação: garantir que nenhum host de PROD seja macOS para que esse ramo nunca execute fora de dev.

---

## Conclusão

Dos 11 achados do red-team: **9 são vulnerabilidades reais** (1 ALTA `#1`, 5 MÉDIA `#2 #3 #4 #5 #6`, 3 BAIXA `#7 #9 #10`), **1 é INFO real** (`#11`, padrão de risco com conteúdo atual seguro) e **1 é falso-positivo como SQLi** (`#8` — não injetável; rebaixado a dívida de defesa-em-profundidade INFO/BAIXA).

Nenhum achado foi inflado de forma indevida; o red-team já havia marcado corretamente `#8` como "não injetável hoje" — esta verificação apenas formaliza o rebaixamento. Os pontos criptográficos e anti-XXE permanecem **sólidos e reconfirmados**.

**Prioridade de correção:** `#1` (ALTA, cifra/referência Key Vault) → `#6` (bloqueador de go-live de produção) → `#2`/`#5` (hardening de borda e fail-fast) → `#3` → demais.

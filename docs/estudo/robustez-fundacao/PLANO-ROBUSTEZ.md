# PLANO DE ROBUSTEZ DA FUNDAÇÃO — Tensorroot.Gov

> Consolidação das 3 auditorias adversariais (Mensageria/Outbox · Tenancy/Auth/Auditoria · Startup/DI/Resiliência externa).
> Lente única: **o que, em PRODUÇÃO REAL (SqlServer + Azure Key Vault, múltiplos tenants, carga concorrente), faz a prefeitura TRAVAR, ENGASGAR ou CORROMPER dado?**
> Análise READ-ONLY. Linhas-chave reconfirmadas contra o código atual antes desta consolidação.

Classificação:
- **P0** — pode TRAVAR/parar a prefeitura ou CORROMPER dado. Bloqueador de go-live. Corrigir **antes** de qualquer carga real do piloto.
- **P1** — degradação séria (disponibilidade, sessão, schema em upgrade). Endurecer antes da Onda 2.
- **P2** — robustez incremental / observabilidade. Não trava hoje; reduz risco e ruído.

---

## SUMÁRIO EXECUTIVO

A fundação está **sólida no caminho feliz** e nas regressões já corrigidas: GQF nega-por-padrão (`1=0`, nunca `Guid.Empty`), ordem de interceptors centralizada (Tenant→Audit→Outbox), cross-tenant guard correto, Outbox com dead-letter + backoff exponencial + seleção que tira poison da cabeça, Polly nas chamadas HTTP reais, ProblemDetails global que nunca derruba o pipeline, captive-deps conhecidas e por-design.

**Mas existem 5 achados P0 que, sob produção real, travam ou corrompem a fundação** — e nenhum deles aparece em DEV (SQLite + KEK síncrona), por isso passaram pelos testes. São, em ordem de letalidade:

1. **Hash-chain de auditoria com corrida na `Sequencia`** → corrompe a trilha imutável do TCE e dispara alarme FALSO de adulteração. (Tenancy/Auditoria)
2. **`sync-over-async` em chamada de rede ao Key Vault no hot path de resolução de conexão** → thread-pool starvation → API inteira para de responder sob carga. (Startup/DI)
3. **Domain events com Value Object de classe são indesserializáveis** → nascem poison, queimam 5 tentativas e viram dead-letter silencioso a cada fechamento de folha / cadastro de fornecedor / juntada de documento. (Mensageria)
4. **`Autoria` (record struct não-posicional) desserializa SEM erro mas com `Valor == null`** → corrupção silenciosa de dado. (Mensageria)
5. **`EnsureCreatedAsync` no banco de controle (fallback não-SqlServer)** → impede migrations → trava evolutiva de schema. (Startup/DI)

Os achados P0-1 (auditoria) e P0-2 (KeyVault) são os mais críticos: o primeiro **corrompe o ativo mais sensível** (a trilha imutável) e o segundo **derruba a API inteira**, não só o fluxo afetado.

---

# P0 — TRAVA / CORRUPÇÃO (bloqueadores de go-live)

## P0-1 — Hash-chain de auditoria: corrida na `Sequencia` corrompe a cadeia e gera falso "adulterada"
**Origem:** Auditoria Tenancy/Auth/Auditoria.
**Arquivos:**
- `src/BuildingBlocks/.../Auditing/AuditSaveChangesInterceptor.cs:239-267` (`UltimoSeloDoTenant[Async]` — `SELECT ... ORDER BY Sequencia DESC LIMIT 1`, sem lock, fora de transação serializável — **reconfirmado**)
- `src/BuildingBlocks/.../ModelBuilderExtensions.cs:118` (índice `(TenantId, Sequencia)` deliberadamente **não-único**)
- `src/BuildingBlocks/.../Auditing/RegistroAcessoSensivel.cs:67-102` (mesma leitura+insert para acesso LGPD)
- `src/BuildingBlocks/.../Auditing/VerificadorTrilhaAuditoria.cs:58-62` (trata sequência repetida como adulteração)

**Cenário de produção:** múltiplas requisições concorrentes do **mesmo tenant** (lote de empenhos, importação de servidores na folha, `NfseSync` batendo no mesmo tenant que um operador). Request A e B leem `UltimoSelo = N` ao mesmo tempo; ambas gravam `Sequencia = N+1` com `HashAnterior = hash(N)`. Cadeia bifurca / sequência duplica. Não reproduz em DEV (SQLite, 1 arquivo por tenant, baixa concorrência).

**Impacto:** corrupção da trilha imutável **+ falso positivo de adulteração** ao Tribunal de Contas. O endpoint `/api/admin/auditoria/verificacao` passa a reportar "adulterada" sem que ninguém tenha adulterado. É o cenário "corrompe a fundação" explicitamente proibido.

**Correção:** tornar a alocação de `Sequencia` **atômica**.
- Mínimo viável: índice **ÚNICO filtrado** em `(TenantId, Sequencia)` para `Sequencia > 0` (filtered index SqlServer) → colisão vira `DbUpdateException` de chave duplicada (detectável + re-tentável) em vez de corrupção silenciosa.
- Robusto: alocar a sequência sob `UPDLOCK, HOLDLOCK` (ou tabela contador por tenant com `UPDATE ... SET seq=seq+1 OUTPUT`) **dentro da mesma transação** do `SaveChanges`, envolvendo insert da trilha + entidades.

**Esforço:** M (mínimo viável) a G (robusto com contador transacional). Recomenda-se o índice único filtrado já + contador transacional na Onda 1.

## P0-2 — `sync-over-async` em chamada de REDE ao Key Vault no hot path → thread-pool starvation
**Origem:** Auditoria Startup/DI/Resiliência.
**Arquivo:** `src/Platform/Tensorroot.Gov.Platform/Tenancy/TenantConnectionResolver.cs:101` (**reconfirmado**):
```csharp
? protetor.RevelarAsync(id, conexao, CancellationToken.None).GetAwaiter().GetResult()
```
**Cadeia:** `ResolveConnectionString()` (síncrono, exigido pela API `ITenantConnectionResolver`) → `ProtetorConexaoTenant.RevelarAsync` → `IProvedorKek.RevelarDekAsync`. Em PROD (`Cofre:ProvedorKek=KeyVault`) isso é uma **chamada HTTP ao Azure Key Vault/HSM** (Polly: timeout 10s + 3 retries + circuit breaker), executada de forma **bloqueante**.

**Cenário de produção:** toda construção de `DbContext` de módulo chama `ResolveConnectionString()` no factory de DI. A cada **cache miss** (TTL de 5 min, primeira req do tenant, ou pós-rotação) a thread da requisição bloqueia em I/O de rede por até ~30s. Sob carga (múltiplos tenants, KeyVault lento) → threads do pool esgotam → **a API inteira para de responder** (todas as rotas, não só as do Cofre). Clássico que derruba ASP.NET em PROD e **não aparece em DEV** (KEK síncrona via `Task.FromResult`).

**Impacto:** TRAVA TOTAL sob carga; mesmo sem starvation, latência de até 30s por cache miss.

**Correção:** eliminar o blocking no hot path.
- Preferencial: decifrar a connection string **assíncrona FORA do factory** — resolver/decifrar no início da requisição (middleware) e cachear o valor em claro no escopo.
- Mínimo aceitável para go-live: **pré-aquecer/warm** o cache de conexões por tenant no provisionamento e no startup, TTL longo + invalidação explícita, reduzindo drasticamente os cache misses no hot path.
- NÃO resolve: `Task.Run(...).GetAwaiter().GetResult()` — não elimina a starvation.

**Esforço:** G (resolução assíncrona ponta-a-ponta esbarra no factory síncrono do EF) / M (warm-cache + TTL longo como mitigação de go-live). Registrar como **bloqueador de go-live em PROD com KeyVault**.

## P0-3 — Domain events com Value Object de CLASSE são indesserializáveis → poison garantido
**Origem:** Auditoria Mensageria/Outbox.
**Arquivos:**
- `src/BuildingBlocks/.../Outbox/OutboxPublisher.cs:64-67` — `JsonSerializer.Deserialize(content, tipo)` com STJ **default, sem `JsonSerializerOptions`, sem `JsonConverter` de VO** (**reconfirmado**)
- `src/BuildingBlocks/.../Outbox/ConvertDomainEventsToOutboxInterceptor.cs:57-66` — serializa **TODO** domain event ao Outbox, independente de haver consumidor
- VOs afetados (private ctor + getter-only, sem `[JsonConstructor]`):
  - `Competencia` (`RecursosHumanos.Domain/Folha/Competencia.cs`) → em `FolhaAberta`, `FolhaCalculada`, `FolhaFechada`, `PagamentoEfetuado`, `PEPLancadoNoSISAB`
  - `Matricula` (`RecursosHumanos.Domain/Servidores/ValueObjects.cs`) → em `ServidorAdmitido`
  - `Cnpj` (`SharedKernel/ValueObjects/Cnpj.cs`) → em `FornecedorCadastrado`
  - `Hash` (`Protocolo.Domain/ValueObjects/Hash.cs`) → em `DocumentoJuntado`

**Cenário de produção:** prefeitura fecha folha (M5 entregue) → eventos gravados no Outbox na mesma transação. `OutboxBackgroundService` (a cada 30s) tenta desserializar **antes de checar handler** → lança → `AttemptCount++` → 5 ciclos → `DeadLetteredOnUtc`. O evento não ter handler é irrelevante: morre **antes** do dispatcher.

**Impacto:** degradação contínua + ruído operacional que **mascara** falhas reais de fila; tabela Outbox inflada de dead-letters. Pior: se amanhã alguém registrar um handler legítimo para `FolhaFechada`, **o evento nunca será entregue** (morre na desserialização). Atinge fluxos centrais já entregues — folha, fornecedor, protocolo, Saúde.

**Correção (escolher 1, recomendada a opção 1):**
1. Registrar `JsonConverter` de Value Object + `JsonSerializerOptions` **compartilhado** entre serialize (interceptor/writer) e deserialize (publisher). Conserto de fundação; cobre todos os VOs e os futuros.
2. Converter os VOs de classe em uso para `readonly record struct` **posicional** (STJ usa o ctor primário).
3. Adicionar `[JsonConstructor]` + ctor compatível em cada VO (padrão que `ValorMonetario`/`ClassificacaoOrcamentaria` já seguem).

**Esforço:** M (opção 1 — um converter + options central). Combina com P1-4 (encolher superfície).

## P0-4 — `Autoria` (record struct não-posicional) desserializa SEM erro, com perda silenciosa
**Origem:** Auditoria Mensageria/Outbox.
**Arquivo:** `Legislativo.Domain/Proposicoes/Autoria.cs:4-12` — `readonly record struct Autoria` com `private Autoria(string valor)` (não-primário) + `Valor { get; }` (getter-only). Embutido em `ProposicaoApresentada`.

**Cenário:** STJ instancia o struct via ctor sem-parâmetro implícito e tenta setar `Valor`, que não tem `set`/`init` → ignora → `Valor == null`. **Não lança** — a mensagem é marcada `ProcessedOnUtc` com autoria vazia.

**Impacto:** corrupção silenciosa (pior que travar). Hoje sem handler, mas bomba-relógio: qualquer futuro consumidor de `ProposicaoApresentada` recebe autoria nula sem sinal de erro.

**Correção:** tornar `Autoria` **posicional** (`readonly record struct Autoria(string Valor)`) com validação em factory — ou usar o conversor da P0-3.

**Esforço:** P.

## P0-5 — `EnsureCreatedAsync` no banco de CONTROLE (fallback não-SqlServer) → impede migrations
**Origem:** Auditoria Startup/DI/Resiliência.
**Arquivo:** `src/ApiHost/Program.cs:279-290`:
```csharp
if (... "SqlServer" ...) await plataforma.Database.MigrateAsync();
else                     await plataforma.Database.EnsureCreatedAsync();
```
**Cenário:** `EnsureCreatedAsync` cria o schema **ignorando migrations** e nunca aplica migrations futuras num banco existente. Se PROD subir em provider != SqlServer, a próxima versão **não migra** → `SaveChanges` lança "coluna inexistente" em runtime. Bancos `EnsureCreated` e `Migrate` são incompatíveis (sem `__EFMigrationsHistory`).

**Impacto:** corrupção evolutiva / trava de gravação no upgrade. Severidade depende da política de provider de PROD: **P0 se PROD não for exclusivamente SqlServer; P1 se for** (então é só DEV/SQLite).

**Correção:** usar `MigrateAsync()` sempre que houver migrations; reservar `EnsureCreatedAsync` **apenas** para testes in-memory. **Confirmar e fixar a política de provider de produção (SqlServer obrigatório).**

**Esforço:** P (a correção) + decisão de política.

---

# P1 — DEGRADAÇÃO SÉRIA

## P1-1 — `Type` persistido como `AssemblyQualifiedName` (com `Version=`) → poison após bump de versão
**Origem:** Mensageria. **Arquivos:** `ConvertDomainEventsToOutboxInterceptor.cs:63`, `ModuleIntegrationEventWriter.cs:34` gravam `AssemblyQualifiedName`; `OutboxPublisher.cs:64` resolve via `Type.GetType` (**reconfirmado**).
**Cenário:** Outbox com mensagens pendentes/backoff → deploy sobe versão do assembly → `Type.GetType` com versão antiga não resolve → `InvalidOperationException` → 5 falhas → dead-letter de mensagens **válidas** geradas minutos antes do deploy.
**Impacto:** perda de eventos de integração legítimos a cada release que coincida com fila não-vazia (produção contínua).
**Correção:** persistir nome de tipo **estável sem versão** (`FullName + ", " + AssemblyName` sem `Version`/token, ou mapa nome-lógico→tipo no `OutboxHandlerRegistry`) e resolver por ele.
**Esforço:** M (atenção à compatibilidade com mensagens já gravadas no formato antigo).

## P1-2 — Migração de tenant NÃO ocorre no boot; só on-demand no provisionamento
**Origem:** Startup/DI. **Arquivos:** `Program.cs:279-310` (boot migra só Platform + seed DEV), `TenantProvisioner.cs:43-51` (migra só ao provisionar).
**Cenário:** nova versão com migration nova num módulo → bancos dedicados de tenants **já provisionados** não são migrados em lugar nenhum no boot → primeira operação que toca a coluna nova lança.
**Impacto:** degradação por tenant após upgrade.
**Correção:** rotina "migrate-on-startup" iterando tenants ativos (ou job de migração no pipeline de deploy), com tratamento de falha que **não derrube o processo** (falha de um tenant não trava os demais — mesmo padrão do `OutboxBackgroundService`).
**Esforço:** M.

## P1-3 — Segundo `sync-over-async` no hot path de autorização (escopo de unidade)
**Origem:** Startup/DI. **Arquivo:** `Modules/Identidade/.../Seguranca/TenantUnidadeContext.cs:68-70` — `.ResolverUnidadesLegiveisAsync(...).GetAwaiter().GetResult()`.
**Cenário:** executado **por requisição autenticada** no filtro de escopo organizacional (query ao banco do tenant). Menos grave que P0-2 (banco local), mas fonte adicional de bloqueio de thread-pool sob carga.
**Impacto:** bloqueio de thread por requisição autenticada (P0 se o banco do tenant for remoto e lento).
**Correção:** tornar a resolução assíncrona ou cachear por requisição resolvido async no pipeline.
**Esforço:** M.

## P1-4 — Outbox materializa TODO domain event (a maioria sem handler)
**Origem:** Mensageria. **Arquivos:** `ConvertDomainEventsToOutboxInterceptor.cs:57` (serializa todos) + `ScopedOutboxMessageDispatcher.cs:44` (loop sobre handlers vazio).
**Cenário:** ~150 domain events, a grande maioria com 0 handler (`CargoCriado`, `PosseRegistrada`, `SessaoAberta`...), serializados a cada `SaveChanges`. Os que desserializam OK passam batidos (marcados `ProcessedOnUtc` — correto, não travam), mas amplificam P0-3/P1-1 e o custo de I/O do drain.
**Impacto:** degradação leve + amplificador dos P0.
**Correção:** interceptor só materializa eventos que (a) são `IIntegrationEvent` ou (b) têm handler no `OutboxHandlerRegistry`. Domain events in-process puros não transitam pelo Outbox — encolhe drasticamente a superfície de poison.
**Esforço:** M.

## P1-5 — `Jwt:Secret` sem validação de comprimento → crash no 1º token (HS256 ≥ 256 bits)
**Origem:** Startup/DI. **Arquivo:** `Program.cs:100-101,119`.
**Cenário:** boot só falha se `Jwt:Secret` **ausente**. Se presente porém curto (< 32 bytes UTF-8), `SymmetricSecurityKey` aceita no boot mas `JwtBearer` lança no 1º request autenticado → 401/500 em todo login logo após deploy, sem aviso no startup.
**Impacto:** indisponibilidade de auth pós-deploy sem fail-fast.
**Correção:** validar `bytes ≥ 32` no boot e abortar com mensagem clara (fail-fast). (O fail-fast por KEK/Vault ausente já é correto e por design — combinar com esta validação.)
**Esforço:** P.

## P1-6 — JWT sem refresh token → expiração derruba a sessão "no meio" da operação
**Origem:** Tenancy/Auth. **Arquivos:** `Identidade.Infrastructure/Seguranca/EmissorToken.cs:50-51,79-88`; `Program.cs:105-124`.
**Cenário:** só access token; sem endpoint de refresh. Com `ValidateLifetime=true` + `ClockSkew=1min`, ao expirar o usuário recebe 401 abrupto no meio de fluxo longo (montagem de empenho, fechamento de folha). Se `DuracaoMinutos` for curto em PROD, vira interrupção recorrente.
**Impacto:** degradação de disponibilidade percebida (trava o trabalho, não o sistema).
**Correção:** refresh token rotativo/revogável ou sliding expiration; mínimo: `DuracaoMinutos` generoso (ex.: 8h de expediente) + re-auth silencioso no front. Confirmar valor no appsettings de PROD.
**Esforço:** M (refresh token) / P (ajuste de duração + tratamento no front).

## P1-7 — WORM trigger só em SqlServer + sem unicidade física da trilha
**Origem:** Tenancy/Auditoria. **Arquivos:** `Multitenancy/SchemaProvisioner.cs:59-93`, `ModelBuilderExtensions.cs:116-118`.
**Cenário:** trigger `INSTEAD OF UPDATE/DELETE` só é criado em SqlServer; em SQLite a defesa é só hash-chain (detecção, não prevenção). Sem proteção de INSERT duplicado (ligado a P0-1). Se o piloto subir em SQLite, a trilha é fisicamente alterável.
**Impacto:** trilha alterável fisicamente fora de SqlServer (depende do provider de PROD).
**Correção:** garantir SqlServer no piloto/PROD (validar `Database:Provider`) ou impedir boot com trilha sobre SQLite em ambiente não-dev. Evoluir para ledger/temporal table (TODO em `SchemaProvisioner.cs:56`).
**Esforço:** M.

## P1-8 — Conteúdo canônico do hash usa JSON de `Dictionary` sem ordem garantida
**Origem:** Tenancy/Auditoria. **Arquivos:** `AuditSaveChangesInterceptor.cs:104-105,161-162`, `AuditHashChain.cs:58-59`.
**Cenário:** `OldValues`/`NewValues` serializados de `Dictionary<string,object?>` (ordem de iteração não-contratual). Hoje bate porque o verificador re-sela a partir do JSON já persistido; risco latente de divergência verificador↔gravado em recompute a partir de fonte re-serializada.
**Impacto:** latente; falso "adulterada" em cenários de recompute.
**Correção:** serializar com chaves ordenadas (`SortedDictionary` ou ordering determinístico) e documentar a dependência.
**Esforço:** P.

---

# P2 — ROBUSTEZ INCREMENTAL / OBSERVABILIDADE

## P2-1 — Sem alerta/visibilidade ao dead-letter; teto fixo global
**Origem:** Mensageria. `OutboxMessage.cs:55` (`MaxAttempts=5` global), `OutboxPublisher.cs:96-110` (`RegistrarFalha` seta `DeadLetteredOnUtc` e silencia). Mensagem morre sem ninguém saber.
**Correção:** log estruturado + métrica OTel ao dead-letter (`TenantId`/`Type`/`Error`); idealmente painel/endpoint de dead-letters para reprocessamento manual. **Esforço:** M.

## P2-2 — Rate limiter cai para IP compartilhado (NAT) → um tenant auto-bloqueia
**Origem:** Tenancy/Auth. `Program.cs:131-144`: partição = `User.Identity?.Name ?? RemoteIpAddress ?? "anonymous"`. Antes do `UseAuthentication`, todos atrás do mesmo IP público da prefeitura compartilham uma janela de 100/min → 50 operadores num NAT podem auto-bloquear o login (429) em pico.
**Correção:** particionar por `tenant_id`+IP quando autenticado; limite mais alto e distinto para `/api/identidade/login`. **Esforço:** M.

## P2-3 — `TrilhaAcessoSensivelBehavior` propaga a corrida do P0-1 e aborta leitura legítima
**Origem:** Tenancy/Auditoria. `TrilhaAcessoSensivelBehavior.cs:62-77`, `RegistroAcessoSensivel.cs:102`. `SaveChanges` extra pós-handler de query sensível **propaga exceção de propósito**; combinado com P0-1, uma colisão de sequência ao selar acesso de leitura de prontuário **aborta a leitura legítima**.
**Correção:** mesma do P0-1 (atomicidade); some quando P0-1 for corrigido. Avaliar se falha de selagem deve abortar a leitura ou registrar+degradar. **Esforço:** incluído no P0-1.

## P2-4 — Gateways governamentais simulados sem Polly (hoje in-process)
**Origem:** Startup/DI. eSocial/TCE/protesto/CadÚnico são simulados in-process. Quando virarem reais, **exigir** o padrão Polly (timeout/retry/circuit-breaker) já marcado nos `// TODO(prod:...)`, como `AdnNfseGateway`/`ConsultaSiconfiHttp`/`ProvedorKekKeyVault` já fazem. **Esforço:** M por gateway, na onda de integrações.

## P2-5 — Registros singleton de `IColetorRep` — confirmar consumo
**Origem:** Startup/DI. `RecursosHumanosModule.cs:96-97,106` registra dois `IColetorRep`; o `IColetorRepFactory` resolve `IEnumerable` (OK), mas confirmar que ninguém injeta `IColetorRep` singular (last-wins). **Esforço:** P (verificação).

---

## PONTOS CONFIRMADOS SADIOS (não mexer)

- **GQF nega sem tenant:** `ModuleDbContext.NegarPorFaltaDeTenant` + `ModelBuilderExtensions.cs:57-61` degeneram para `1=0`, nunca `Guid.Empty`. Não regrediu.
- **Ordem dos interceptors:** centralizada (`ModuleInterceptorRegistration.cs:43-46`, Tenant→Audit→Outbox). Correto.
- **Cross-tenant guard XT-1** (`AdminEndpoints.cs:198-199`) e `TenantSaveChangesInterceptor:33-62` (cross-tenant → 403, não 500). Sem falso positivo.
- **Handler global** (`ProblemDetailsExceptionHandler.cs`): resolve tenant em try/catch que nunca derruba o pipeline; nunca vaza stack. Captive dep singleton→scoped via `RequestServices` é **por design e correto**.
- **Outbox — o que está bom:** dead-letter existe (`OutboxPublisher.cs:101-106`, sem retry infinito); backoff exponencial com teto (30s→15min); seleção tira poison da cabeça (`:49-53`); índice de drain cobre a query (`ModelBuilderExtensions.cs:101`); guarda H5 + isolamento por handler corretos; idempotência at-least-once do único consumidor real (`PagamentoEfetuado`→`ContabilizarPagamentoHandler`) delegada ao motor por `OrigemReferenciaId` (= `OrdemDePagamentoId.Value`), `SaveChanges` só se `gerados > 0` — reprocessamento seguro.
- **Fail-fast por KEK/Vault ausente** (`ProvedorKekConfig.cs:26-37`, `ProvedorKekKeyVault.cs:33-37`): correto e desejável. Não mexer.
- **Workers + OutboxBackgroundService:** loops resilientes (try/catch por tenant/módulo, `OperationCanceledException` quebra sem matar o processo, `PeriodicTimer`). Corretos.

---

## ORDEM DE HARDENING RECOMENDADA (ondas de build)

### ONDA 1 — Bloqueadores de go-live (corrigir ANTES de qualquer carga concorrente real do piloto)
Os 5 P0, na ordem de letalidade:
1. **P0-1** — Atomicidade da `Sequencia` da hash-chain (índice único filtrado já + contador transacional). *É o único achado que efetivamente CORROMPE a fundação e dispara alarme falso de adulteração ao TCE.*
2. **P0-2** — Eliminar o `sync-over-async` ao Key Vault no hot path (warm-cache + TTL longo como mínimo de go-live; resolução async fora do factory como alvo). *Único achado que derruba a API inteira.*
3. **P0-3** — Conversor de Value Object + `JsonSerializerOptions` compartilhado serialize↔deserialize (destrava folha/fornecedor/protocolo/Saúde de uma vez).
4. **P0-4** — `Autoria` posicional (fecha a corrupção silenciosa).
5. **P0-5** — `MigrateAsync()` sempre + fixar política SqlServer-only em PROD.

> Decisão de política transversal à Onda 1: **PROD é exclusivamente SqlServer + KeyVault.** Confirmar e travar no boot. Resolve/rebaixa P0-5, P1-7 e parte do P0-2.

### ONDA 2 — Endurecimento pré-escala (antes de novos tenants / próximo deploy com fila viva)
6. **P1-1** — Type estável sem versão (antes do próximo deploy com fila não-vazia).
7. **P1-4** — Outbox só para integration events / com handler (encolhe a superfície de poison).
8. **P1-2** — Migrate-on-startup para tenants ativos, resiliente por tenant.
9. **P1-5** — Validar comprimento do `Jwt:Secret` no boot (fail-fast).
10. **P1-3** — Remover o segundo `sync-over-async` (autorização/escopo de unidade).
11. **P1-8** — Conteúdo canônico do hash com chaves ordenadas.
12. **P1-7** — Garantir/validar SqlServer para a trilha (ligado à decisão de política da Onda 1).
13. **P1-6** — Refresh token / sliding expiration + duração generosa.

### ONDA 3 — Robustez incremental e observabilidade
14. **P2-1** — Alerta + métrica OTel ao dead-letter + painel de reprocessamento.
15. **P2-2** — Rate limiter particionado por tenant+IP; limite próprio para login.
16. **P2-3** — Já resolvido por P0-1; revisar política abortar-vs-degradar na selagem de acesso.
17. **P2-4** — Padrão Polly nos gateways governamentais quando deixarem de ser simulados.
18. **P2-5** — Confirmar consumo de `IColetorRep`.

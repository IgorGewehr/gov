# RED-TEAM — Relatório Consolidado de Segurança (Tensorroot.Gov)

> Consolidação dos quatro pares ataque/verificação (authn-authz, isolamento-tenant,
> injeção-segredos, lgpd-dados) **re-confirmados independentemente no código** por este red-team
> externo. Análise estática de `src/`; runtime (5080) NÃO tocado. Itens **[a confirmar]** dependem
> de instância. Cada confirmação abaixo foi reaberta pessoalmente (arquivo:linha citado é o que eu
> li, não apenas o que os relatórios afirmam).
>
> Perfil: revisor adversarial externo. Data: 2026-06-22.

---

## Veredito (12 linhas)

A criptografia é o ponto mais forte do sistema: o Cofre (AES-256-GCM, nonce por operação,
AAD=TenantId‖Thumbprint, ZeroMemory, KEK via env) e a proteção anti-XXE do assinador XML são
sólidos e foram reconfirmados. A borda HTTP tem deny-by-default real (`PermissaoHandler` só
aprova com a claim exata) e não há SQLi explorável (só DDL com literais). **Porém o produto NÃO
está pronto para processar dinheiro público e dado de saúde/CadÚnico/folha sob TCE.** Há um caminho
COMPLETO de autoescalonamento de "admin de RBAC" a "admin pleno do tenant" (composição de papel e
PUT de papéis ignoram a invariante I4 de delegação), uma quebra REAL de isolamento entre tenants no
banco de CONTROLE (admin de A liga/desliga módulos de B), e a trilha LGPD — o ativo probatório
central do produto — está furada nos três eixos: forjável no SUAS (`usuarioId` vem do cliente),
inexistente em Saúde (leitura sem rastro), e armazenando CPF/NIS/clínico em CLARO recuperável por
um único `GET /api/admin/auditoria`. Some-se: connection string de PROD em texto puro no catálogo
e ausência total de handler global de exceção. **Os críticos são bloqueadores de go-live, não
hardening.** A arquitetura é boa; a execução das bordas de identidade, multi-tenant-de-controle e
auditoria LGPD não acompanhou. Recomendo travar produção até fechar os 6 críticos.

---

## Placar de confirmados (re-verificados por mim)

| Severidade | Qtd | IDs |
|---|---|---|
| **CRÍTICO** | **6** | AA-1 (papel sem I4), AA-2 (PUT papéis sem I4), XT-1 (cross-tenant módulos), LG-1 (trilha SUAS forjável), LG-2 (Saúde sem trilha de leitura), LG-3 (auditoria em claro + visualizador cru) |
| **ALTO** | **5** | SEC-1 (conn string em claro), AA-5 (delegação ilimitada sem D3), LG-A2 (base legal LGPD ausente), LG-A3 (`*.ver` grosso demais), XT-2 (GQF degrada p/ `Guid.Empty`) |
| **MÉDIO** | **9** | SEC-2 (sem exception handler), SEC-3 (conn string crua no provisionamento), SEC-5 (HS256 sem tamanho mín.), SEC-6 (ICP-Brasil no-op), AA-6 (JWT sem `ValidAlgorithms`), AA-7 (sem ABAC por UO na borda), SEC-4 (segredo DEV versionado), XT-4 (cache de conexão obsoleto), LG-M2 (Serilog loga CNS/querystring) |
| **BAIXO** | **8** | AA-4 (gating fail-open), AA-8 (sem revogação de token), AA-9/SEC-9 (rate-limit login fraco), SEC-7 (`Type.GetType` Outbox), SEC-10 (BCrypt 72B), XT-3b (tenant por reflexão de string), XT-5 (`sub` não-GUID desliga filtro UO), XT-6 (interceptor no-op sem tenant), LG-B1 (`Cpf.Formatar` sem mascarado) |
| **INFO/dívida** | 3 | SEC-8 (DDL interpola schema — não injetável hoje), SEC-11 (`excecao.Message` ecoado), XT-7 (migração com GQF latente) |

**Total confirmado: 6 críticos · 5 altos · 9 médios · 8 baixos.** Falsos-positivos: apenas SEC-8
(rebaixado a dívida — schema é literal do código, não injetável). Nenhum crítico foi inflado: re-li
cada um no código-fonte.

---

## CRÍTICOS (bloqueadores de go-live) — confirmados arquivo:linha

### AA-1 — Composição de papel sem prova I4 (escala permissões não-possuídas)
**`Modules/Identidade/.../Papeis/DefinirPermissoesDoPapel.cs:26-40`** (e `CriarPapel.cs`).
Confirmei: `DefinirPermissoesDoPapelHandler(IPapelRepository papeis, IUnitOfWork unitOfWork)` —
**não injeta `ICurrentUser` nem calculadora de escopo**. A única checagem é
`papel.DefinirPermissoes(request.Permissoes)`, que só valida `DomainPermissoes.EhConhecida`
(catálogo). Quem tem `identidade.usuarios.gerenciar` empacota num papel permissões fiscais/saúde/TCE
que não possui.
**Contraste comprobatório:** `AtribuirPapelAoUsuario.cs:53` injeta `ICurrentUser` e `:82-89` chama
`CalculadoraPermissoesEfetivas.ResolverEscopoAsync` + `AutorizacaoDeConcessao.Verificar` — esse é o
padrão correto que falta aqui.
**Correção:** injetar `ICurrentUser` + `CalculadoraPermissoesEfetivas` e exigir cobertura I4 de cada
permissão adicionada; OU mover composição de papel para escopo de plataforma fora de `Permissoes.Todas`.

### AA-2 — `PUT /usuarios/{id}/papeis` atribui em escopo GLOBAL sem I4
**`Modules/Identidade/.../Usuarios/DefinirPapeisDoUsuario.cs:27-66`**.
Confirmei: `DefinirPapeisDoUsuarioHandler(IUsuarioRepository, IPapelRepository, IUnidadeRepository,
IUnitOfWork)` — **sem `ICurrentUser`, sem `AutorizacaoDeConcessao`**. `:52` chama
`usuario.DefinirPapeis(papeisIds)` (cria atribuições em `RaizPendente`, `IncluiSubunidades=true`) e
`:55-63` reancora na raiz real → **escopo global**. Existe um caminho seguro paralelo
(`POST /usuarios/{id}/atribuicoes` → prova I4) e este que o contorna.
**Cadeia AA-1+AA-2:** quem tem só `identidade.usuarios.gerenciar` cria papel (AA-1), enche de
`Permissoes.Todas`, e se auto-atribui em escopo global (AA-2). Relogin recalcula `perm` → admin pleno.
**Correção:** deprecar o PUT e expor só `/atribuicoes`; ou fazer o PUT provar cobertura I4 por papel.

### XT-1 — Cross-tenant: admin de A configura licenças de módulo de B
**`ApiHost/Admin/AdminEndpoints.cs:31-78`** + **`Permissoes.cs:201`** + **`TenantProvisioningService.cs:60-69`**.
Confirmei diretamente: o grupo `/api/admin/tenants/{tenantId:guid}/modulos` tem gate único
`.RequirePermission("admin.modulos.configurar")` (`:34`); o `tenantId` vem **da rota** (`:38,55`) e
é passado cru para `provisioningService.DefinirModuloAsync(tenantId, ...)` (`:75`), que escreve em
`context.TenantModules` por `tenantId` arbitrário (`TenantProvisioningService.cs:62-69`, PlatformDbContext,
**sem GQF**). **`admin.modulos.configurar` PERTENCE a `Permissoes.Todas` (`Permissoes.cs:201`)** — todo
admin de qualquer tenant a tem. **Nenhuma comparação `rota.tenantId == ITenantContext.TenantId`.**
**Prova de que é desvio, não convenção:** no MESMO arquivo, o grupo de auditoria (`:100,144`) filtra
por `tenant.TenantId` do JWT — o padrão correto. E o provisionamento (`Program.cs:230`) exige
`PlataformaTenantsProvisionar`, deliberadamente **fora** de `Todas` (`Permissoes.cs:155-166`, comentário
explícito). Os endpoints de módulos quebraram exatamente esse padrão.
**Impacto:** DoS direcionado (desliga Saúde de B → gating responde 403 a todo `/api/saude/*` de B),
ativação de módulo não pago, enumeração de licenças. É o ÚNICO A→B real (banco de controle não é
isolado por tenant). **[a confirmar]** requer conhecer o GUID de B (vaza por suporte/provisionamento).
**Correção:** trocar para permissão de plataforma fora de `Todas`; OU remover `tenantId` da rota e
derivar de `ITenantContext`; e, imediato, `if (tenantId != tenant.TenantId) return 403` auditado.

### LG-1 — Trilha de acesso ao prontuário SUAS é FORJÁVEL
**`Modules/AssistenciaSocial/.../AssistenciaSocialEndpoints.cs:111-113`** +
**`Application/Prontuarios/ObterProntuarioDaFamilia.cs:41-44,78`**.
Confirmei: a assinatura HTTP é `(Guid familiaId, Guid usuarioId, string motivoAcesso, ...)` —
`usuarioId` e `motivoAcesso` vêm da **query string controlada pelo atacante**; nenhum `ICurrentUser`
é injetado. O handler grava `prontuario.RegistrarAcesso(request.UsuarioId, request.MotivoAcesso, ...)`
(`:78`) — o valor do cliente cru na trilha imutável. Mesmo padrão no `POST /prontuarios/{id}/acessos`
(`AssistenciaSocialEndpoints.cs:104-107`, `RegistrarAcessoProntuario.cs`).
**Impacto:** assistente lê prontuário com violação contra menor (ECA/art. 11 LGPD) passando
`usuarioId=<colega>` → a trilha "à prova de adulteração" sela um valor **já falso na origem** e
incrimina terceiro. Destrói o valor probatório da trilha (CLAUDE.md §6).
**Correção:** remover `usuarioId` da assinatura; derivar de `ICurrentUser.UserId` no handler; registrar
`UserId`+`IpAddress` do principal. `motivoAcesso` pode permanecer como entrada validada.

### LG-2 — Leituras de Saúde NÃO geram trilha de acesso (docstring mente)
**`Modules/Saude/.../Pacientes/ObterHistoricoClinicoDoPaciente.cs:31,37`** e
**`ObterPacientePorCns.cs`**.
Confirmei: `ObterHistoricoClinicoDoPacienteHandler(IPacienteRepository pacientes)` — **só lê e
projeta**, sem `IUnitOfWork`, sem `RegistrarAcesso`. O docstring (`:31`) afirma "gera trilha de
acesso ao prontuario" — **falso**. O único interceptor de trilha (`AuditSaveChangesInterceptor.cs:25`)
só dispara em `SavingChanges` (escrita); o pipeline MediatR não tem behavior de leitura sensível.
**Impacto:** varredura de CID-10/alergias/condições crônicas de toda a população **sem nenhum rastro**
(nem banco, nem trilha, nem log). Viola CLAUDE.md §6 e art. 37 LGPD.
**Correção:** behavior MediatR transversal para queries `SensivelLGPD` registrando
`{Tenant,UserId,Ip,Entidade,EntityId,BaseLegal,Ts}` em tabela append-only encadeada.

### LG-3 — Auditoria de escrita guarda CPF/NIS/clínico em CLARO; visualizador devolve cru
**`BuildingBlocks/.../Auditing/AuditSaveChangesInterceptor.cs:111-135`** +
**`AdminEndpoints.cs:121-124`** + **`RecursosHumanos/.../ServidorConfiguration.cs`**.
Confirmei: a redação é **opt-in** — `:111` `entry.Entity is IHasRedactedAuditFields` e só então
substitui por `RedactionMarker`; senão serializa `CurrentValue`/`OriginalValue` cru (`:128-135`). O
**único** implementador em código-fonte (fora de `bin/`) é `Cofre/CertificadoA1Cofre.cs`. `Servidor`
persiste CPF como `cpf.Digitos` (11 dígitos) → cada insert/update grava CPF completo em `NewValues`. O
visualizador `GET /api/admin/auditoria` projeta `OldValues`/`NewValues` **na íntegra** (`AdminEndpoints.cs:121-124`).
**Impacto:** o masking de leitura do RH (`***.NNN.***-**`) é contornável — `GET /api/admin/auditoria?entidade=Servidor`
devolve CPF cru de toda a folha; `entidade=Familia` → NIS/renda. A trilha virou banco-sombra em claro.
**Correção:** redação deny-by-default por `NivelSensibilidade` (CPF/NIS/CNS/clínico mascarados por
padrão no interceptor); visualizador mascara por sensibilidade da coluna + permissão segregada para
"desmascarar" (com trilha de leitura própria).

---

## ALTOS — confirmados

- **SEC-1 — Connection string de PROD em texto puro no catálogo.**
  `Platform/.../PlatformDbContext.cs:35` — `builder.Property(t => t.ConnectionString).HasMaxLength(500)`
  **sem `HasConversion`/cifra**. Confirmei: não há interceptor de cifra nem referência a Key Vault para
  esse campo. Em PROD (SqlServer) a coluna contém usuário/senha do banco dedicado de cada município →
  dump/replica/insider/SQLi futura expõe credenciais de **todos** os tenants. Viola CLAUDE.md §6.
  *Ressalva honesta:* em DEV é `null` (fallback SQLite); o risco materializa em PROD. **Correção:**
  guardar só referência a secret do Key Vault e resolver em runtime; ou `HasConversion` com envelope+AAD.

- **AA-5 — Delegação ilimitada (D3 ausente).** `AtribuirPapelAoUsuario.cs:112-115` trata
  `concedente == alvo` como `Direta`; `AutorizacaoDeConcessao` só prova cobertura I4, não limita
  propagação nem profundidade (D3 adiado). Qualquer titular de `X` em escopo global propaga `X` sem
  teto. **Amplificador de AA-1/AA-2.** **Correção:** implementar D3 (subconjunto delegável); separar
  "usar" de "delegar".

- **LG-A2 — Base legal LGPD (art. 7/11/14) não modelada/registrada.** Nenhum handler sensível captura
  hipótese legal estruturada; `MotivoAcesso` é texto livre. Em fiscalização ANPD/TCE não há
  accountability por acesso. **Correção:** enum `BaseLegal` obrigatório nas operações `SensivelLGPD`,
  persistido com a trilha (LG-2).

- **LG-A3 — `saude.ver`/`assistenciasocial.ver` grossos demais.** `SaudeEndpoints.cs:34,38,84,...` e
  `AssistenciaSocialEndpoints.cs:46-54,111-119` — a mesma claim libera listagem minimizada E conteúdo
  sigiloso (prontuário com violação contra menor, histórico clínico). Sem clearance por
  `NivelSensibilidade`. **Correção:** separar verbos (`saude.prontuario.ler`) + clearance ABAC.

- **XT-2 — GQF degrada para `TenantId == Guid.Empty` sem tenant.** `ModuleDbContext.cs:45`
  (`HasTenant ? TenantId : Guid.Empty`) + `ModelBuilderExtensions.cs:49`
  (`Expression.Equal(tenantProperty, currentTenant)` — **sem flag de deny análoga ao
  `FiltrarPorUnidade` de `:52-64`**). "Sem tenant" vira "tenant zero" em vez de `1=0`. **Confinado ao
  banco físico de UM tenant** (database-per-tenant impede A→B de negócio), por isso ALTO e não crítico;
  mas quebra o deny-by-default e expõe linhas órfãs. **Correção:** predicado retorna `1=0` quando
  `!HasTenant` e não for `SistemaTenantContext`; para contexto de sistema, não instalar o filtro.

---

## MÉDIOS — confirmados

- **SEC-2 — Sem handler global de exceção.** `grep` em `ApiHost/Program.cs`: **zero**
  `UseExceptionHandler`/`AddProblemDetails`/`UseDeveloperExceptionPage`. Exceções de domínio
  (`AlterarSenha.cs:38`, `ObterHistoricoClinicoDoPaciente.cs:46` "Paciente nao encontrado",
  `TenantConnectionResolver` tenant ausente) sobem sem tratamento → potencial vazamento de
  stack/detalhe e **canal lateral de existência de indivíduo** em base de saúde. **[a confirmar]** corpo
  exato em PROD. **Correção:** `IExceptionHandler` + `ProblemDetails` neutro; 404 genérico idêntico para
  "não existe" e "sem permissão".

- **SEC-3 — Provisionamento aceita `ConnectionString` crua do body.** `Program.cs:227` →
  `TenantProvisioner.ProvisionarAsync` (`:34,49`) → `MigrarBancoAsync(conexao, ...)` abre conexão real.
  Sem allow-list de host/keywords → SSRF-de-banco / injeção de parâmetros. Mitigado em parte por exigir
  `PlataformaTenantsProvisionar`. **Correção:** derivar conexão de template server-side + id de tenant.

- **SEC-5 — HS256 sem tamanho mínimo de segredo.** `Program.cs:102`
  (`new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))` sem checar `Length`) e idem na emissão
  (`EmissorToken.cs`). Segredo curto em PROD é aceito → brute-force offline → forja de qualquer `perm`.
  **Correção:** abortar startup/emissão se `< 32 bytes`.

- **SEC-6 — Validação ICP-Brasil é no-op.** `ValidacaoCadeiaIcpBrasil.cs`: `Validar` só chama
  `PermiteAssinatura`; cadeia/CRL/OCSP é `// TODO(validar-oficial)`. Pior: `PermiteAssinatura` termina em
  `return !temKeyUsage` — **cert sem extensão KeyUsage é ACEITO**. Cert autoassinado que abra com a senha
  é custodiado como A1 ativo → eSocial/TCE assinados com cert não confiável (repúdio/rejeição).
  **MÉDIO hoje / ALTA para go-live.** **Correção:** CustomRootTrust + AC-Raiz ICP-Brasil + RevocationMode=Online;
  exigir KeyUsage de assinatura presente.

- **AA-6 — JWT sem `ValidAlgorithms`/`RequireSignedTokens`.** `Program.cs:96-102` valida
  issuer/audience/lifetime/key mas não fixa algoritmo. Sob HS256 simétrico, RS256→HS256 não se aplica e
  libs recentes rejeitam `alg:none` por default → risco é regressão silenciosa. **Correção:** fixar
  `ValidAlgorithms=["HS256"]`, `RequireSignedTokens=true`, `RequireExpirationTime=true`.

- **AA-7 — Sem ABAC por UO na borda.** Token carrega `perm` plana sem UO (`EmissorToken.cs:67-74`);
  `PermissaoHandler` só checa presença da claim. Qualquer titular de `saude.ver` lê o PEP de qualquer
  paciente do tenant — superexposição LGPD (não bypass). **Correção:** emitir UO na claim / consultar
  `EscopoEfetivo` e filtrar por `UnidadesDaPermissao` nos módulos sensíveis.

- **SEC-4 — `appsettings.Development.json` versionado com segredo JWT + senha admin.** `.gitignore`
  ignora `secrets.json` mas não `appsettings.Development.json`. DEV-only, mas perigoso se homologação
  acessível subir com ele. **Correção:** mover para `secrets.json`/env; placeholders no arquivo.

- **XT-4 — Cache de conexão obsoleto.** `TenantConnectionResolver.cs` (singleton, TTL 5min,
  `Invalidar` in-process); rotação fora de `RotacionarConexaoAsync` não invalida e outras réplicas
  nunca recebem → janela servindo banco antigo do MESMO tenant. **Correção:** invalidação distribuída
  (pub-sub) disparada pela rotação no Key Vault.

- **LG-M2 — Serilog loga CNS/querystring sensível.** `Program.cs:176` `UseSerilogRequestLogging()` sem
  enricher; CNS no path (`SaudeEndpoints.cs:32`), `motivoAcesso` na querystring. `LoggingBehavior` está
  OK (só nome do request). **[a confirmar]** retenção no sink de PROD. **Correção:** mover identificadores
  sensíveis para o corpo; redigir path/querystring.

---

## BAIXOS — confirmados (resumo)

- **AA-4** Gating de licença fail-open: `Program.cs:198` só verifica `if (tenantContext.HasTenant)` e só
  casa `/api/<seg>` por string-matching → defense-in-depth invertido. **Correção:** fail-closed por metadata de grupo.
- **AA-8** Sem revogação de token (`jti` emitido mas sem denylist; TTL 60min) → papel revogado vale até 1h.
- **AA-9/SEC-9** Rate-limit do `/login` só por IP (sem trava por conta/backoff) → password spraying distribuído.
- **SEC-7** `OutboxPublisher.cs:64` `Type.GetType(mensagem.Type)` — superfície de type-confusion (baixo: `Type` é escrito pelo sistema). **Correção:** allow-list nome→Type.
- **SEC-10** BCrypt trunca em 72 bytes (`AlterarSenha` aceita 256). **Correção:** pré-hash SHA-256.
- **XT-3b** Tenant do Integration Event por reflexão de string `GetProperty("TenantId")` → `Guid.Empty` silencioso se a convenção mudar. **Correção:** `IMustHaveTenant` obrigatório + teste de arquitetura.
- **XT-5** `TenantUnidadeContext.cs:57` `sub` não-GUID → `_deveFiltrar=false` (abre todas as UOs). Não explorável com emissor de 1ª parte (sempre `sub` GUID); fechar antes de SSO/federado.
- **XT-6** `TenantSaveChangesInterceptor.cs:35` no-op quando `!HasTenant` (não carimba nem bloqueia). Complemento de gravação do XT-2.
- **LG-B1** `Cpf.Formatar()`/`ToString()` expõem CPF completo; sem `Mascarado` canônico (NIS já tem). Risco de regressão de masking.

---

## O que está SÓLIDO (reconfirmado — para não enviesar)

- **Núcleo cripto do Cofre:** AES-256-GCM com nonce aleatório por operação, AAD=TenantId‖Thumbprint,
  `EphemeralKeySet`, `CryptographicOperations.ZeroMemory` no `finally`, KEK via env (nunca hardcoded),
  validada em 32 bytes. Endpoints nunca retornam .pfx/senha/chave. Upload limitado a 256KB, só em memória.
- **Anti-XXE:** `AssinadorXmlDsig.cs:37-39` — `XmlResolver=null` + `DtdProcessing.Prohibit`.
- **Anti-SQLi:** nenhum `FromSqlRaw`/`ExecuteSqlRaw` com entrada de usuário; só DDL com literais. Filtros
  de auditoria por LINQ/EF parametrizado; paginação com `Math.Clamp(..,1,200)`. ADN usa `Uri.EscapeDataString`.
- **Deny-by-default real:** `PermissaoHandler` só `Succeed` com a claim exata; `PermissionPolicyProvider`
  só materializa políticas `perm:` com `RequireAuthenticatedUser`. Sem fallback permissivo.
- **Login anti-enumeração:** mensagem constante "Credenciais invalidas." + hash sintético mesmo sem
  usuário (anti-timing) + reaplicação explícita de `TenantId == tenantId` sobre o DB dedicado.
- **Separação plataforma×tenant para provisionamento:** `PlataformaTenantsProvisionar` fora de `Todas`
  (o padrão correto que XT-1 deveria ter copiado).
- **I4 corretamente provada no caminho `/atribuicoes`** (`AtribuirPapelAoUsuario.cs:53,82-89`). O furo
  está nos caminhos administrativos que a contornam (AA-1/AA-2), não na prova.
- **Isolamento físico database-per-tenant** + `TenantOverride` re-setado por escopo em Outbox/assinatura.
  O visualizador de auditoria filtra corretamente por `tenant.TenantId` do JWT.

---

## Hardening priorizado pré-produção

**Bloqueadores absolutos de go-live (fazer ANTES de qualquer dado real):**
1. **XT-1** — segregar permissão de configuração de módulos para escopo de plataforma (fora de `Todas`)
   + validar `tenantId` da rota == JWT. *(quebra de isolamento entre tenants, explorável hoje)*
2. **AA-2 + AA-1** — fechar os dois caminhos que ignoram I4 (deprecar PUT `/papeis`; provar I4 em
   composição de papel). *(cadeia única de autoescalonamento a admin pleno)*
3. **LG-1** — derivar `usuarioId` da trilha SUAS de `ICurrentUser`, nunca do cliente. *(trilha forjável)*
4. **LG-3** — redação de auditoria deny-by-default por sensibilidade + mascarar no visualizador.
5. **LG-2** — behavior MediatR de trilha de leitura para recursos `SensivelLGPD` (Saúde/Assistência/RH).
6. **SEC-1** — tirar a connection string do catálogo em claro (referência ao Key Vault).

**Antes de produção (alto/médio):**
7. **SEC-6** — ativar validação estrita de cadeia ICP-Brasil + revogação (eSocial/TCE).
8. **SEC-2** — handler global de exceção com `ProblemDetails` neutro (fecha SEC-2, SEC-11, e o canal
   lateral de A1/LG-A1).
9. **SEC-5 / AA-6** — fail-fast de tamanho de segredo HS256 + fixar `ValidAlgorithms`/`RequireSignedTokens`.
10. **LG-A2 / LG-A3 / AA-7** — base legal estruturada + clearance ABAC por `NivelSensibilidade` + UO no
    enforcement, antes do go-live dos módulos sensíveis.
11. **SEC-3** — não aceitar connection string crua no provisionamento (template server-side).
12. **AA-5** — implementar D3 (delegação por subconjunto) para conter propagação lateral.

**Endurecimento contínuo (baixo / dívida):**
13. Família `Guid.Empty` em conjunto (XT-2/XT-6/XT-7): trocar "sem tenant" por "negar/lançar".
14. XT-4 (invalidação distribuída de cache), XT-3b/XT-5 (convenções frágeis de tenant), AA-8 (revogação
    de token), AA-9 (rate-limit dedicado no login), SEC-7 (allow-list de tipos no Outbox), SEC-10
    (pré-hash BCrypt), SEC-4 (tirar segredo DEV do repo), LG-B1 (`Cpf.Mascarado` canônico), LG-M2
    (redação de log), AA-4 (gating fail-closed).
15. NetArchTest novo: reprovar Integration Event sem tenant tipado; reprovar handler de leitura sensível
    sem trilha; reprovar endpoint que lê `usuarioId`/tenant da rota/querystring para fins de auditoria.

---

## Notas de honestidade

- **Nenhum crítico foi inflado.** Re-li cada AA-1/AA-2/XT-1/LG-1/LG-2/LG-3 no código e confirmei a
  ausência exata de `ICurrentUser`/I4/comparação de tenant/trilha/redação.
- **Único falso-positivo herdado:** SEC-8 (DDL interpola `Schema`) — `Schema` é literal do código do
  módulo, não entrada de usuário; não injetável hoje. Mantido como dívida de defesa-em-profundidade.
- **Rebaixamentos honestos por database-per-tenant:** XT-2 (CRÍTICO→ALTO) e o "cross-tenant de
  auditoria" do achado original (não há mistura A↔B — o banco é dedicado). O único A→B real é XT-1,
  porque o banco de **controle** (Plataforma) não é isolado por tenant.
- **Itens [a confirmar] em runtime:** corpo do 500 em PROD (SEC-2), retenção de querystring no sink
  (LG-M2), enumerabilidade do GUID do tenant-vítima (XT-1), corrida na `Sequencia` `Guid.Empty`,
  janela de cache obsoleto multi-instância (XT-4). Nenhum desses muda a EXISTÊNCIA do defeito — só a
  magnitude/facilidade.
```

# Verificacao Cetica — AuthN / AuthZ (Tensorroot.Gov)

> Verificacao independente, item-a-item, do red-team `ataque-authn-authz.md`. Cada achado foi
> reaberto **no codigo** (arquivo:linha) para decidir: **VULNERABILIDADE REAL** (com evidencia) ou
> **FALSO-POSITIVO / mitigado** (rebaixado, com a mitigacao apontada). Verificador: revisor externo
> cetico, analise estatica, sem runtime. Data: 2026-06-22.

## Veredito agregado

| # | Sev. (red-team) | Veredito | Sev. ajustada |
|---|---|---|---|
| A1 | CRITICA | **REAL — CONFIRMADA** | CRITICA |
| A2 | CRITICA | **REAL — CONFIRMADA** | CRITICA |
| A3 | CRITICA | **REAL — CONFIRMADA** | CRITICA |
| A4 | ALTA | **REAL, porem rebaixada** (defense-in-depth invertido; sem alvo explorado hoje) | MEDIA |
| A5 | ALTA | **REAL — CONFIRMADA** (by-design para raiz; vira escalonamento via A1/A2) | ALTA (condicional) |
| A6 | MEDIA | **REAL — CONFIRMADA** (hardening ausente; nao explorável hoje sob HS256) | MEDIA→BAIXA |
| A7 | MEDIA | **REAL — CONFIRMADA** (superexposicao LGPD, nao bypass) | MEDIA |
| A8 | BAIXA | **REAL — CONFIRMADA** | BAIXA |
| A9 | BAIXA | **REAL — CONFIRMADA** | BAIXA |

**9 de 9 confirmados como reais.** Nenhum falso-positivo puro. Tres (A4, A6, A7) tiveram a
severidade reavaliada: A4 e A6 sao posturas frageis sem caminho de exploracao na borda atual (rebaixados);
A7 nao e bypass mas e superexposicao real de dado sensivel (mantido MEDIA). Os tres CRITICOS (A1/A2/A3)
sao **bloqueadores de go-live** confirmados com evidencia direta.

---

## A1 — Composicao de papel sem prova I4 · **REAL — CRITICA**

**Evidencia.**
- `DefinirPermissoesDoPapelHandler.Handle` (`DefinirPermissoesDoPapel.cs:30-40`) injeta **apenas**
  `IPapelRepository` + `IUnitOfWork`. **Nao injeta** `ICurrentUser` nem qualquer calculadora de escopo
  efetivo. A unica validacao e `papel.DefinirPermissoes(request.Permissoes)`.
- `Papel.DefinirPermissoes` (`Papel.cs:84-102`) so checa `DomainPermissoes.EhConhecida(permissao)`
  (`Papel.cs:91`) — i.e. "esta no catalogo". Nao ha qualquer verificacao de posse pelo concedente.
- `CriarPapelHandler` (`CriarPapel.cs:30-46`) idem: `Papel.Criar(tenant.TenantId, nome, permissoes)`
  sem `ICurrentUser`. Pode nascer ja com todas as permissoes.
- Borda: PUT `/papeis/{id}/permissoes` e POST `/papeis` (`IdentidadeEndpoints.cs:165-174`) sob o grupo
  `admin` cujo unico gate e `IdentidadeUsuariosGerenciar` (`IdentidadeEndpoints.cs:66`).
- `IdentidadeUsuariosGerenciar` pertence a `Permissoes.Todas` (`Permissoes.cs:200`), logo o papel
  "Administrador" semeado (`IdentidadeModule.cs:162`) o possui — e qualquer titular dele pode compor papel.

**Contraste comprobatorio.** O caminho correto (`AtribuirPapelAoUsuarioHandler`) injeta
`ICurrentUser` e chama `CalculadoraPermissoesEfetivas.ResolverEscopoAsync` +
`AutorizacaoDeConcessao.Verificar` (`AtribuirPapelAoUsuario.cs:53,82-99`). A ausencia desse mesmo
padrao em `DefinirPermissoesDoPapel`/`CriarPapel` e a vulnerabilidade. **Confirmada.**

---

## A2 — PUT `/usuarios/{id}/papeis` atribui em escopo GLOBAL sem I4 · **REAL — CRITICA**

**Evidencia.**
- `DefinirPapeisDoUsuarioHandler.Handle` (`DefinirPapeisDoUsuario.cs:35-66`): injeta
  `IUsuarioRepository`, `IPapelRepository`, `IUnidadeRepository`, `IUnitOfWork`. **Nao injeta
  `ICurrentUser`** e **nao invoca `AutorizacaoDeConcessao`**. A unica checagem e existencia dos papeis
  no tenant (`DefinirPapeisDoUsuario.cs:45-49`). Linha 52: `usuario.DefinirPapeis(papeisIds)` sem prova.
- `Usuario.DefinirPapeis` (`Usuario.cs:162-184`) cria, para cada papel, uma `AtribuicaoDePapel` em
  `UnidadeOrganizacionalId.RaizPendente`, `incluiSubunidades: true`, `Vigencia.Aberta`,
  `OrigemAtribuicao.Direta()` (`Usuario.cs:174-179`) — **escopo global**.
- O handler reancora na raiz real do tenant (`DefinirPapeisDoUsuario.cs:55-63` →
  `ReancorarAtribuicoesPendentesNaRaiz`), consolidando o escopo GLOBAL.
- Borda: PUT `/usuarios/{id}/papeis` (`IdentidadeEndpoints.cs:98-103`), mesmo gate
  `IdentidadeUsuariosGerenciar`. As permissoes sao recalculadas no login
  (`Autenticar.cs:105-115` → `CalculadoraPermissoesEfetivas.ResolverAsync`), entao apos relogar o
  alvo carrega tudo do papel.

**Cadeia A2+A1 confirmada.** Quem tem so `identidade.usuarios.gerenciar` cria papel (A1), enche-o de
`Permissoes.Todas`, e via PUT `/papeis` se auto-atribui em escopo global (A2). Escalonamento completo
de admin-de-RBAC para admin-pleno-do-tenant, contornando I4. **Confirmada.** E o pior dos tres.

---

## A3 — Cross-tenant: admin de um tenant altera licencas de OUTRO tenant · **REAL — CRITICA**

**Evidencia.**
- Grupo `MapGroup("/api/admin/tenants/{tenantId:guid}/modulos")` (`AdminEndpoints.cs:31-34`), gate
  unico `RequirePermission("admin.modulos.configurar")` (`AdminEndpoints.cs:34`).
- `admin.modulos.configurar` (`AdminModulosConfigurar`) **pertence a `Permissoes.Todas`**
  (`Permissoes.cs:201`) — todo "Administrador" de qualquer tenant o possui.
- O `tenantId` e lido **da rota** (parametro de handler) e passado direto:
  - GET: `moduleProvider.EnabledModulesAsync(tenantId, ...)` (`AdminEndpoints.cs:43`).
  - PUT: `provisioningService.DefinirModuloAsync(tenantId, nomeCanonico, ...)` (`AdminEndpoints.cs:75`).
- `TenantProvisioningService.DefinirModuloAsync` (`TenantProvisioningService.cs:60-82`) escreve em
  `context.TenantModules` (PlatformDbContext, catalogo global de controle — **sem Global Query Filter
  por tenant**) para o `tenantId` arbitrario recebido. Cria o vinculo se nao existir (`:66-70`).
- **Em nenhum ponto** se compara o `tenantId` da rota com o `tenant_id` do JWT. Nao ha `ITenantContext`
  injetado neste grupo.

**Contraste no MESMO arquivo (prova de que e desvio, nao convencao).** Os endpoints de auditoria
filtram por `tenant.TenantId` **do contexto** (`AdminEndpoints.cs:100,108,144`), nunca por id de rota —
exatamente o padrao seguro que o grupo de modulos deixou de seguir.

**Nao mitigado por teste.** `LicenciamentoTests.cs` so prova o mecanismo de licenciamento (um tenant
acessa so o que licenciou), e ate **demonstra** que `DefinirModuloAsync(tenantId, ...)` escreve no
`tenantId` passado sem guarda (`LicenciamentoTests.cs:41-46`). Nao ha teste de autorizacao cross-tenant.

**Quebra direta de isolamento multi-tenant (CLAUDE.md §5).** Admin do Tenant A desliga/ativa modulos do
Tenant B (DoS, habilitacao de modulo nao pago, enumeracao de licencas). **Confirmada.**
Pre-condicao real: o GUID do Tenant B precisa ser conhecido (`[a confirmar]` em runtime — nao e
enumeravel por forca bruta de GUID, mas vaza por provisionamento/suporte/respostas de outros fluxos).
A pre-condicao **reduz a facilidade**, nao a criticidade: o controle de acesso esta ausente.

---

## A4 — Gating de licenciamento "fail-open" · REAL, **rebaixada para MEDIA**

**Evidencia.** Middleware `Program.cs:189-212`: so verifica licenca `if (tenantContext.HasTenant)`
(`Program.cs:198`); sem tenant, segue. E so atua quando o 1o segmento de `/api/<seg>` casa em
`rotasModulos` (`Program.cs:192-195`) — string-matching, nao metadata de grupo.

**Por que rebaixar.** Confirmado como postura **fail-open / defense-in-depth invertido**, mas:
(a) a defesa real continua sendo o `RequirePermission` por endpoint (deny-by-default solido — ver
"pontos fortes") e a falha do `IdentidadeDbContext` ao resolver conexao sem tenant; (b) na borda atual
**nao foi encontrado** endpoint sensivel alcancavel sem tenant resolvido que dependa apenas do gating.
O risco e de **regressao futura** (endpoint anonimo novo, ou prefixo de rota que nao casa com o nome
canonico do modulo → escapa do gate de licenca → exercita modulo nao licenciado: impacto de
cobranca/compliance, nao de vazamento de dado). Real, mas sem caminho de exploracao hoje → **MEDIA**.

---

## A5 — Auto-atribuicao via I4 (admin raiz) · **REAL — ALTA (condicional)**

**Evidencia.**
- Admin raiz semeado com `Permissoes.Todas` em escopo global na raiz com `IncluiSubunidades=true`
  (`IdentidadeModule.cs:162-178`) → cobre tudo, pode atribuir tudo a qualquer um.
- `ProcedenciaDoConcedente` trata `concedente == alvo` como `OrigemAtribuicao.Direta()`
  (`AtribuirPapelAoUsuario.cs:112-115`) — auto-atribuicao permitida por construcao.
- D3 (subconjunto delegavel / teto de profundidade) ausente: `AutorizacaoDeConcessao` prova apenas
  cobertura I4, nao limita propagacao. Confirmado por ausencia de logica de delegabilidade no caminho.

**Avaliacao.** Para o admin raiz e *by design* e aceitavel. O risco real e **composto**: a seguranca do
sistema depende de A1/A2 estarem fechados. Como A1/A2 estao **abertos e confirmados**, qualquer titular
de `identidade.usuarios.gerenciar` chega a admin pleno. Mantida **ALTA**, com a nota de que sua
explorabilidade hoje vem de A1/A2 (nao e um furo isolado novo, e o amplificador deles).

---

## A6 — JWT sem `ValidAlgorithms`/`RequireSignedTokens` · REAL, **MEDIA→BAIXA**

**Evidencia.** `Program.cs:94-106`: `TokenValidationParameters` define issuer/audience/lifetime/
signing key e `ValidateIssuerSigningKey=true`, mas **nao** define `ValidAlgorithms`,
`RequireSignedTokens`, `RequireExpirationTime` nem `ValidTypes`. Emissao em HS256
(`EmissorToken.cs:77`, `SecurityAlgorithms.HmacSha256`).

**Por que rebaixar.** Confirmada a ausencia do hardening explicito. Porem: chave **simetrica** HS256 →
o ataque classico RS256→HS256 (algorithm confusion) **nao se aplica** (nao ha chave publica RSA
exposta); e o `Microsoft.IdentityModel` recente rejeita `alg:none` por padrao quando ha
`IssuerSigningKey`. Risco concreto = **regressao silenciosa** numa atualizacao de lib/config, nao
exploravel hoje. Hardening recomendado (fixar `ValidAlgorithms=["HS256"]`, `RequireSignedTokens=true`,
`RequireExpirationTime=true`), severidade efetiva **BAIXA**. `[a confirmar]` em runtime se `alg:none`
ja e rejeitado pelo default — provavel que sim.

---

## A7 — Enforcement por UO (ABAC fino) ausente na borda · **REAL — MEDIA**

**Evidencia.**
- `PermissaoHandler` (`PermissionAuthorization.cs:24-43`) so checa **presenca** da claim `perm` igual
  ao escopo (`:32-39`). Nao consulta UO.
- O token carrega **uma claim `perm` plana por permissao, sem UO** (`EmissorToken.cs:67-74`). O dominio
  modela escopo rico (UO → permissao) mas a projecao embutida e plana.
- Consequencia na borda: `/pacientes/{id}/historico-clinico` e `/pacientes/por-cns/{cns}` exigem apenas
  `saude.ver` (`SaudeEndpoints.cs:34,38`) — qualquer titular de `saude.ver` le o PEP de **qualquer**
  paciente do tenant, sem segmentacao por UBS/equipe.

**Avaliacao.** Nao e bypass (a permissao existe), e **superexposicao de dado sensivel sob LGPD**
(CLAUDE.md §6) — exatamente o que o modelo de UO deveria conter e que nao chega ao enforcement.
Real e relevante para go-live de Saude/Assistencia/RH. Mantida **MEDIA**.

---

## A8 — Sem revogacao de token · **REAL — BAIXA**

**Evidencia.** `EmissorToken` emite `jti` (`EmissorToken.cs:56`) mas **nao ha denylist/`jti` store**
consultado no pipeline (nenhuma referencia a denylist no `Program.cs`/handlers). TTL 60 min
(`JwtOptions.cs:22`, `DuracaoMinutos=60`). `perm` sao fotografadas na emissao
(`Autenticar.cs:105-115`); revogar papel/desativar usuario nao invalida tokens vivos. Janela de ate
60 min apos desligamento de servidor com acesso a folha/CPF/saude. **Confirmada.** BAIXA.

---

## A9 — Rate limit sem particao dedicada no `/login` · **REAL — BAIXA**

**Evidencia.** `Program.cs:114-127`: limitador global particiona por `User.Identity?.Name` ou IP, 100
req/min, janela fixa. `/api/identidade/login` e `AllowAnonymous()` (`IdentidadeEndpoints.cs:63`) → sem
`name` → cai na particao por IP. Nao ha politica dedicada e mais estrita para login. Permite password
spraying lento e compartilhamento de janela atras de NAT. **Confirmada.** BAIXA.

---

## Pontos fortes reconfirmados (sem enviesar)

- **Deny-by-default real:** `PermissaoHandler` so `Succeed` na presenca exata da claim
  (`PermissionAuthorization.cs:32-39`); ausencia reprova. Sem fallback permissivo. **Confirmado.**
- **`PermissionPolicyProvider`** so materializa politicas prefixadas `perm:` e exige
  `RequireAuthenticatedUser()` (`PermissionAuthorization.cs:68-83`); demais delegam ao default. Sem
  brecha de politica implicita. **Confirmado.**
- **Login anti-enumeracao:** mensagem uniforme + hash mesmo sem usuario / e-mail malformado
  (`Autenticar.cs:81-97`). **Confirmado.**
- **Separacao plataforma × tenant** para provisionamento: `PlataformaTenantsProvisionar` **fora** de
  `Permissoes.Todas` (`Permissoes.cs:161-166`), gate do POST `/admin/tenants` (`Program.cs:230`). Padrao
  correto — **que A3 deveria ter copiado** para `admin.modulos.configurar`. **Confirmado.**
- **I4 corretamente provada** no caminho `/atribuicoes` (`AtribuirPapelAoUsuario.cs:79-99`). O furo nao
  esta na prova, e nos caminhos administrativos que a contornam (A1/A2). **Confirmado.**

---

## Conclusao do verificador

- **Reais: 9/9.** **Falsos-positivos: 0.**
- **Confirmados com evidencia direta (criticos, bloqueadores de go-live): A1, A2, A3.**
- **Confirmados, severidade mantida:** A5 (ALTA condicional a A1/A2), A7 (MEDIA), A8 (BAIXA), A9 (BAIXA).
- **Confirmados, rebaixados** por ausencia de caminho de exploracao na borda atual: A4 (ALTA→MEDIA),
  A6 (MEDIA→BAIXA).

Ordem de correcao recomendada (inalterada vs. red-team): **A2 → A3 → A1**, depois A7 (pre-go-live de
modulos sensiveis), A4/A6 como hardening, A5 (D3) e A8/A9 a seguir.

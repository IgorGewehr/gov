# Red-Team — AuthN / AuthZ (Tensorroot.Gov)

> Análise adversarial **estática** do código em `src/`. Foco: bypass de JWT, escalonamento de
> privilégio RBAC/ABAC, endpoints sem `RequirePermission`, políticas com fallback permissivo,
> burla da regra I4 (delegação), operador-de-plataforma × admin-de-tenant.
> Itens marcados **[a confirmar]** exigem runtime/instância para prova final.

Data: 2026-06-22 · Perfil: revisor externo (sem acesso ao runtime; não subi a 5080).

---

## Resumo executivo

**9 achados** (3 críticos, 2 altos, 2 médios, 2 baixos).

A borda HTTP está bem coberta no agregado (quase todos os endpoints de módulo carregam
`RequirePermission`, deny-by-default real no `PermissaoHandler`, login com mensagens uniformes e
hash-timing estável). **O problema não é a borda — é a camada de administração de identidade**, onde
três defeitos compõem um caminho completo de **autoescalonamento de admin-de-tenant para poder total
do tenant** e um **cruzamento de fronteira entre tenants** no endpoint de licenciamento de módulos.
A regra I4 está corretamente provada no domínio (`AutorizacaoDeConcessao`), mas **dois caminhos
administrativos a contornam inteiramente**, esvaziando a invariante.

| # | Severidade | Achado |
|---|---|---|
| A1 | **CRÍTICA** | `DefinirPermissoesDoPapel` permite conceder a um papel QUALQUER permissão sem possuí-la (burla I4) |
| A2 | **CRÍTICA** | `DefinirPapeisDoUsuario` (PUT `/usuarios/{id}/papeis`) atribui papel em escopo GLOBAL sem passar por I4 |
| A3 | **CRÍTICA** | Admin de Tenant A altera licenças de módulo do Tenant B (`/api/admin/tenants/{tenantId}/modulos`) |
| A4 | **ALTA** | Gating de licenciamento "fail-open" quando não há tenant resolvido / fora de `/api/<modulo>` |
| A5 | **ALTA** | Auto-atribuição via I4: admin raiz pode escalar a si mesmo qualquer papel que ele criar |
| A6 | **MÉDIA** | JWT sem `ValidAlgorithms`/`RequireSignedTokens` explícitos (defesa-em-profundidade) |
| A7 | **MÉDIA** | Enforcement por UO (ABAC fino) inexistente na borda HTTP — só RBAC plano "perm" |
| A8 | **BAIXA** | Sem invalidação/denylist de token (revogação de papel só vale no próximo login) |
| A9 | **BAIXA** | Rate limiter particiona por `name` claim; sem partição dedicada e mais estrita no `/login` |

---

## A1 — `DefinirPermissoesDoPapel` permite escalonar permissões que o concedente não tem (burla I4) · CRÍTICA

**Arquivos:**
- `src/Modules/Identidade/Tensorroot.Gov.Modules.Identidade.Application/Papeis/DefinirPermissoesDoPapel.cs:26-40`
- `src/Modules/Identidade/Tensorroot.Gov.Modules.Identidade.Application/Papeis/CriarPapel.cs:30-46`
- `src/Modules/Identidade/Tensorroot.Gov.Modules.Identidade.Domain/Papeis/Papel.cs:84-102`
- Exposto em `IdentidadeEndpoints.cs:165-174` (POST `/papeis`, PUT `/papeis/{id}/permissoes`), gate único: `identidade.usuarios.gerenciar` (`IdentidadeEndpoints.cs:66`)

**Problema.** A única validação ao definir as permissões de um papel é
`DomainPermissoes.EhConhecida(permissao)` (`Papel.cs:91`) — i.e., "está no catálogo". **Não há
verificação de que o concedente possui ele próprio essas permissões.** A regra I4 só é aplicada em
`AtribuirPapelAoUsuario`; a *composição do papel* fica totalmente livre.

**Cenário de exploração.** Um usuário com **apenas** `identidade.usuarios.gerenciar` (que é o
escopo administrativo de identidade, presente em `Permissoes.Todas` e, portanto, no papel
"Administrador") faz:

```
POST /api/identidade/papeis          { "nome": "x", "permissoes": [] }      → papelId
PUT  /api/identidade/papeis/{papelId}/permissoes
     { "permissoes": ["financas.pagamento.ordenar","saude.ver","transparencia.remessa.transmitir", ...todas...] }
```

O papel passa a carregar permissões fiscais/sensíveis que o concedente não possui. Em seguida (ver
A2/A5) ele atribui esse papel a si ou a um cúmplice. **Resultado: ordenar pagamento, ler prontuário
de saúde, transmitir remessa TCE — sem nunca ter recebido essas permissões.** A invariante "não
delega o que não tem" é esvaziada pela porta dos fundos da definição de papel.

**Correção.** Provar I4 na composição do papel: ao definir/alterar permissões, exigir que o
escopo efetivo do `currentUser` cubra **cada** permissão sendo adicionada (em alguma UO). Injetar
`ICurrentUser` + `CalculadoraPermissoesEfetivas` no handler e rejeitar (403) permissões não
possuídas. Idem em `CriarPapel`. Alternativa mínima: restringir `DefinirPermissoesDoPapel` a um
escopo de plataforma/superadmin dedicado (fora de `Permissoes.Todas`), como já é feito com
`PlataformaTenantsProvisionar`.

---

## A2 — PUT `/usuarios/{id}/papeis` atribui papel em escopo GLOBAL sem passar por I4 · CRÍTICA

**Arquivos:**
- `src/Modules/Identidade/Tensorroot.Gov.Modules.Identidade.Application/Usuarios/DefinirPapeisDoUsuario.cs:35-66`
- `src/Modules/Identidade/Tensorroot.Gov.Modules.Identidade.Domain/Usuarios/Usuario.cs:162-184` (`DefinirPapeis` → escopo `RaizPendente`, `IncluiSubunidades=true`, vigência aberta)
- `IdentidadeEndpoints.cs:98-103`

**Problema.** Há **dois** caminhos para vincular papéis a usuários:
- `POST /usuarios/{id}/atribuicoes` → `AtribuirPapelAoUsuario` → **prova I4** (`AtribuirPapelAoUsuario.cs:89-99`). ✅
- `PUT /usuarios/{id}/papeis` → `DefinirPapeisDoUsuario` → `usuario.DefinirPapeis(...)` → **nenhuma prova I4** (`DefinirPapeisDoUsuario.cs:52`). ❌

O caminho PUT redefine integralmente os papéis e cria atribuições com `IncluiSubunidades=true` na
raiz (`Usuario.cs:174-179` + reancoragem na raiz real em `DefinirPapeisDoUsuario.cs:55-63`), ou
seja, **escopo GLOBAL**. Não há concedente, não há checagem de cobertura, não há verificação de que
o concedente sequer administra a UO.

**Cenário de exploração.** Com `identidade.usuarios.gerenciar`:

```
GET  /api/identidade/papeis                      → descobre o papelId do "Administrador" (Permissoes.Todas)
PUT  /api/identidade/usuarios/{meuId}/papeis      { "papeisIds": ["<id-do-Administrador>"] }
```

Faz logout/login (as `perm` são recalculadas no login — `AutenticarHandler.cs:105-115`) e volta com
**todas as permissões do catálogo em escopo global**. Privilege escalation completo de admin-de-RBAC
para admin-pleno-do-tenant, contornando I4 inteiramente. Combinado com A1, nem é preciso existir um
papel pré-pronto: cria-se um e enche-se de tudo.

**Correção.** Eliminar o caminho que não prova I4, OU fazê-lo provar: para cada papel
adicionado/removido, exigir cobertura I4 do concedente no escopo (aqui, o escopo global → o
concedente teria de cobrir a raiz inteira para todas as permissões do papel). Dado que `DefinirPapeis`
nasce com escopo global, na prática só um superadmin global deveria poder usá-lo. O ideal é deprecar
o endpoint PUT `/papeis` e expor apenas `/atribuicoes` (que já é correto).

---

## A3 — Cross-tenant: admin de um tenant altera licenças de módulo de OUTRO tenant · CRÍTICA

**Arquivos:**
- `src/ApiHost/Admin/AdminEndpoints.cs:31-78` (grupo `/api/admin/tenants/{tenantId:guid}/modulos`)
- `src/Platform/Tensorroot.Gov.Platform/Tenancy/TenantProvisioningService.cs:60-82` (`DefinirModuloAsync`)
- `src/Platform/Tensorroot.Gov.Platform/Tenancy/TenantModuleProvider.cs:33-38` (`EnabledModulesAsync`)

**Problema.** O `tenantId` é lido **da rota** e usado direto contra o `PlatformDbContext` global
(catálogo de controle, sem Global Query Filter por tenant). A única proteção é
`.RequirePermission("admin.modulos.configurar")` (`AdminEndpoints.cs:34`) — e esse escopo está em
`Permissoes.Todas` (`Permissoes.cs:201`), logo **todo "Administrador" de qualquer tenant o possui**.
**Em nenhum ponto se verifica que o `tenantId` da rota é igual ao `tenant_id` do JWT do chamador.**

**Cenário de exploração.** Admin do Tenant A (CNPJ X), autenticado normalmente, dispara:

```
PUT /api/admin/tenants/{ID_DO_TENANT_B}/modulos/Saude     { "ativo": false }   → DoS no Tenant B (desliga Saúde)
PUT /api/admin/tenants/{ID_DO_TENANT_B}/modulos/Financas  { "ativo": true  }   → habilita módulo não pago
GET /api/admin/tenants/{ID_DO_TENANT_B}/modulos                                 → enumera licenças do Tenant B
```

`DefinirModuloAsync` grava o `TenantModule` para o `tenantId` arbitrário (`TenantProvisioningService.cs:62-81`).
O gating de runtime (`Program.cs:201`) passa a negar/permitir módulos no tenant-vítima conforme a
tabela adulterada. Como prefeitura-Executivo e câmara-Legislativo são tenants distintos e há piloto
multi-tenant, isto é **quebra direta do isolamento multi-tenant** (CLAUDE.md §5: "vazamento entre
tenants = falha crítica") e um vetor de DoS/abuso comercial inter-cliente. **[a confirmar]** o
`{ID_DO_TENANT_B}` precisa ser conhecido (GUID) — mas é enumerável por força bruta de GUID? Não;
porém vaza por provisionamento, suporte, ou pela própria resposta de outros endpoints.

**Correção.** Tratar `/api/admin/tenants/{id}/modulos` como **operação de plataforma**, não de
tenant: ou (a) mover para um escopo de plataforma dedicado (`plataforma.modulos.configurar`, fora de
`Permissoes.Todas`, como já feito com `PlataformaTenantsProvisionar`); ou (b) se for mesmo
auto-serviço do tenant, **validar `tenantId` da rota == `ITenantContext.TenantId`** (retornar 403 se
divergir) antes de qualquer escrita. O mesmo vale para os endpoints de auditoria
(`AdminEndpoints.cs:87-150`): lá o filtro usa `tenant.TenantId` do contexto (correto), mas o de
módulos usa o da rota (errado).

---

## A4 — Gating de licenciamento "fail-open" sem tenant resolvido · ALTA

**Arquivo:** `src/ApiHost/Program.cs:189-212`

**Problema.** O middleware de gating só bloqueia `if (tenantContext.HasTenant)`
(`Program.cs:198`). Sem tenant resolvido, a verificação de licença é **pulada** e a requisição segue.
Adicionalmente, o gating só atua sobre `/api/<modulo>` cujo segmento bate em `rotasModulos`
(`Program.cs:192-195`); rotas que não seguem essa convenção não são verificadas pelo gating
(dependem 100% do `RequirePermission` de cada endpoint).

**Cenário de exploração.** A defesa real continua sendo o `RequirePermission` + a falha do
`IdentidadeDbContext` ao resolver conexão sem tenant — então não é bypass de dados por si só. Mas a
postura é "fail-open": qualquer regressão futura que torne um endpoint anônimo, ou um módulo cujo
prefixo de rota não case com o nome canônico, **não recebe o gating de licenciamento**, permitindo
exercitar um módulo **não licenciado** para o tenant (cobrança/compliance). É um *defense-in-depth*
invertido. **[a confirmar]** o impacto concreto depende de existir endpoint sensível alcançável sem
tenant — não encontrei um na borda atual, mas a regra está frágil.

**Correção.** Tornar o gating *fail-closed*: para caminhos `/api/<modulo>`, **exigir** tenant
resolvido (401 se ausente) e licença ativa (403 se ausente), em vez de só verificar quando há tenant.
Considerar aplicar o gate por convenção de grupo (metadata no `MapGroup` de cada módulo) em vez de
*string-matching* de rota.

---

## A5 — Auto-atribuição via I4: admin raiz escala a si mesmo qualquer papel que criar · ALTA

**Arquivos:**
- `src/Modules/Identidade/Tensorroot.Gov.Modules.Identidade.Application/Usuarios/AtribuirPapelAoUsuario.cs:89-115`
- `src/Modules/Identidade/Tensorroot.Gov.Modules.Identidade.Domain/Usuarios/EscopoEfetivo.cs:118-122` (`CobreEscopo`)

**Problema.** I4 exige que o concedente **cubra** as permissões do papel no escopo. O admin raiz
semeado (`IdentidadeModule.cs:162-167`) recebe `Permissoes.Todas` em escopo global — portanto cobre
tudo e pode atribuir tudo a qualquer um, **inclusive a si mesmo** (`ProcedenciaDoConcedente` trata
`concedente == alvo` como `Direta`, `AtribuirPapelAoUsuario.cs:112-115`). Isto é *by design* para o
admin raiz, mas significa que **a segurança do sistema depende inteiramente de A1/A2 estarem
fechados**: como A1 deixa compor papéis com permissões não-possuídas e A2 deixa atribuir sem I4, um
admin que só deveria gerir identidade vira admin pleno. Mesmo *sem* A1/A2, qualquer titular de uma
permissão `X` em escopo global pode propagá-la livremente a terceiros (delegação ilimitada, sem teto
de profundidade nem `PermissoesDelegaveis` — D3 está adiado, conforme `AutorizacaoDeConcessao.cs:59`).

**Cenário.** Conta de "gestor de RH" com `recursoshumanos.gerenciar` global cria papel "RH+" e o
distribui a 50 contas. Sem D3 (subconjunto delegável) nem clearance D5, não há contenção da
propagação lateral.

**Correção.** Implementar D3 (limite de delegação: o concedente só delega um **subconjunto**
explicitamente marcado como delegável) e considerar separar "usar permissão" de "delegar permissão".
Curto prazo: documentar e restringir contas com escopo global; nunca semear admin raiz com escopo de
`identidade.usuarios.gerenciar` global a contas operacionais.

---

## A6 — JWT sem restrição explícita de algoritmo / tokens assinados · MÉDIA

**Arquivo:** `src/ApiHost/Program.cs:94-106`

**Problema.** `TokenValidationParameters` valida issuer, audience, lifetime e a chave de assinatura,
mas **não** define `ValidAlgorithms` nem `RequireSignedTokens = true`. Com chave simétrica HS256 o
ataque clássico de *algorithm confusion* RS256→HS256 não se aplica (não há chave pública RSA
exposta), e o `Microsoft.IdentityModel` recente rejeita `alg:none` por padrão quando há
`IssuerSigningKey`. Ainda assim, depender do default em sistema que processa dinheiro público é
frágil. Também não há `TokenValidationParameters.ValidTypes` (header `typ`).

**Cenário.** **[a confirmar]** em runtime — provável que `alg:none` já seja rejeitado pelo default da
biblioteca; o risco é regressão silenciosa numa atualização/configuração.

**Correção.** Fixar explicitamente:
`ValidAlgorithms = ["HS256"]`, `RequireSignedTokens = true`, `RequireExpirationTime = true`. Isso
torna a postura *fail-closed* e imune a mudanças de default.

---

## A7 — Enforcement por UO (ABAC fino) ausente na borda HTTP · MÉDIA

**Arquivos:**
- `src/BuildingBlocks/.../Authorization/PermissionAuthorization.cs:24-43` (handler só checa presença da claim `perm`)
- `EscopoEfetivo.cs:100-122` (`UnidadesDaPermissao`/`CobreEscopo` existem, mas só são usados na concessão I4)
- Token: uma claim `perm` plana por permissão, **sem a UO** (`EmissorToken.cs:67-74`)

**Problema.** O domínio modela escopo rico (permissão → conjunto de UOs), mas a borda HTTP só sabe
"possui `saude.ver`?" — não "possui `saude.ver` **nesta UBS**?". Como o token carrega apenas a
projeção plana (`EscopoEfetivo.Permissoes`, `EscopoEfetivo.cs:31-38`), **qualquer** titular de
`saude.ver` lê o histórico clínico de **qualquer** paciente do tenant
(`SaudeEndpoints.cs:36-38`), idem CadÚnico/Assistência, folha/CPF no RH. Para dados sensíveis sob
LGPD (CLAUDE.md §6), RBAC plano por tenant é grosseiro demais: não há segmentação por unidade/equipe.

**Cenário.** Recepcionista de uma UBS com `saude.ver` enumera o PEP de toda a rede municipal. Não é
*bypass* (a permissão existe), mas é **superexposição de dado sensível** — exatamente o que o modelo
de UO foi desenhado para conter e que não chega ao enforcement.

**Correção.** Levar o escopo por UO ao enforcement: emitir a UO na claim (ou consultar
`EscopoEfetivo` no handler) e filtrar consultas por `UnidadesDaPermissao`/`CobreEscopo` nos endpoints
de leitura de dados sensíveis. Pelo menos nos módulos Saúde/Assistência/RH antes do go-live.

---

## A8 — Sem revogação de token (papel revogado só vale no próximo login) · BAIXA

**Arquivos:** `EmissorToken.cs:50-89` (TTL 60 min, `JwtOptions.cs:22`), sem denylist/`jti` store.

**Problema.** As `perm` são fotografadas na emissão. Revogar um papel/atribuição
(`RevogarAtribuicao`, `Desativar` usuário) **não invalida tokens já emitidos** — a vítima/atacante
permanece com acesso por até 60 min. Para desligamento de servidor com acesso a folha/CPF/saúde,
essa janela é relevante.

**Correção.** Denylist de `jti` (já emitido em `EmissorToken.cs:56`) consultada no pipeline, ou TTL
curto + refresh, ou verificação de `Ativo`/versão-de-credencial por requisição em operações
sensíveis.

---

## A9 — Partição de rate limit por `name` e ausência de limite dedicado no `/login` · BAIXA

**Arquivo:** `src/ApiHost/Program.cs:114-127`

**Problema.** O limitador global particiona por `User.Identity.Name` (claim `name`) ou IP. O `name`
vem de token assinado (não forjável), mas `/api/identidade/login` é anônimo → cai na partição por IP
(100 req/min). Um atacante atrás de NAT/proxy compartilha a janela; e 100/min ainda permite
*password spraying* lento. Não há limite **mais estrito e dedicado** para o login.

**Correção.** Política de rate limit dedicada e mais agressiva para `/login` (por IP **e** por e-mail
alvo), com *backoff* exponencial e bloqueio temporário após N falhas.

---

## Pontos fortes observados (para não enviesar)

- Deny-by-default real: `PermissaoHandler` só dá `Succeed` na presença exata da claim
  (`PermissionAuthorization.cs:32-39`); ausência reprova. Sem fallback permissivo.
- `PermissionPolicyProvider` delega ao default só políticas **não** prefixadas com `perm:`
  (`PermissionAuthorization.cs:68-83`) — sem brecha de política implícita aprovando tudo.
- Cobertura de `RequirePermission` praticamente total nos módulos de negócio (Saúde, Finanças,
  RH, Patrimônio, etc. — 1 guard por endpoint ou por grupo).
- Login: mensagem uniforme + verificação de hash mesmo sem usuário (anti-enumeração por timing,
  `Autenticar.cs:72-97`); índice central com unicidade global de e-mail por tenant.
- Separação operador-de-plataforma × admin-de-tenant **bem feita** para provisionamento:
  `PlataformaTenantsProvisionar` fora de `Permissoes.Todas` (`Permissoes.cs:161-166`,
  `Program.cs:221-230`). **O mesmo padrão deveria ter sido aplicado a `admin.modulos.configurar`
  (A3).**
- I4 corretamente provada no domínio para o caminho `/atribuicoes` (`AutorizacaoDeConcessao.cs`).
  O furo não está na prova, e sim nos caminhos administrativos que a contornam (A1/A2).

---

## Os 3 piores (prioridade de correção)

1. **A2** — PUT `/usuarios/{id}/papeis` ignora I4 e atribui em escopo global → autoescalonamento a
   admin pleno do tenant.
2. **A3** — Admin de um tenant manipula licenças de módulo de outro tenant (cross-tenant / DoS /
   abuso comercial), por falta de checagem `tenantId-rota == tenant-do-JWT`.
3. **A1** — Composição de papel sem prova I4 permite empacotar permissões não-possuídas (fiscais,
   saúde, remessa TCE) e, com A2, recebê-las.

> A2+A1 formam uma cadeia única de escalonamento; A3 é uma quebra independente de isolamento
> multi-tenant. Recomenda-se tratá-los como **bloqueadores de go-live**.

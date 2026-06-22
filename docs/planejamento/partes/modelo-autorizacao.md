# Modelo de Autorização (alvo) — Tensorroot.Gov

> **Escopo deste documento.** Projetar o modelo de autorização **alvo** (entidades, regras de concessão,
> invariantes, GAP) para um ERP GovTech multi-tenant que processa dinheiro público e dados sensíveis sob
> escrutínio do TCE-RS. Lido contra o código atual em `src/Modules/Identidade`, `src/BuildingBlocks/...Authorization`,
> `src/ApiHost/Tenancy` e o diagnóstico em `docs/diagnostico/`.
> **Não** reescreve a constituição (`CLAUDE.md` §5/§6) nem o diagnóstico — estende-os.
> Marcações **[a confirmar]** = ponto jurídico/normativo a validar antes de implementar (CLAUDE.md §16).
> Data: 2026-06-22.

---

## 0. Resumo executivo (o salto)

Hoje temos **RBAC plano, tenant-scoped**: `Usuario → Papel → Permissao` (catálogo canônico de 27 escopos
`modulo.ver`/`modulo.gerenciar`), com permissões efetivas materializadas em **claims `perm` no JWT** e
*deny-by-default* na borda HTTP (`RequirePermission`). É sólido, mas tem **três limites estruturais**
para uma prefeitura real:

1. **Não há dimensão organizacional.** Quem pode `financas.gerenciar` pode gerenciar as finanças do
   tenant **inteiro** — não existe "empenho só da Secretaria de Saúde". Toda a granularidade é por módulo.
2. **Não há ABAC.** Não há como condicionar acesso a **atributos do recurso** (unidade dona, sensibilidade
   LGPD, exercício, valor) nem **atributos do sujeito** (lotação, unidade gestora). A trilha de leitura
   LGPD exigida não tem âncora de escopo.
3. **Não há delegação de administração com escopo.** `identidade.usuarios.gerenciar` é tudo-ou-nada
   no tenant: quem tem, cria/edita **qualquer** usuário e atribui **qualquer** papel — pode conceder
   `saude.gerenciar` mesmo sem possuí-la. Viola a regra do dono: **"não delega o que não tem"**.

O alvo é **RBAC + ABAC com dimensão de Unidade Organizacional (UO)** e **delegação de administração com
escopo**, mantendo a **separação por poder via tenant** (Executivo e Legislativo = CNPJs/tenants distintos —
isso **não muda** e é o limite mais externo). Decisão de autorização passa a ser uma função:

```
permitir(sujeito, ação, recurso, ambiente) =
      tenant(sujeito) == tenant(recurso)              -- invariante de plataforma (já existe)
  AND possuiPermissao(sujeito, ação)                  -- RBAC (já existe)
  AND escopoUO(sujeito, ação) ⊇ unidade(recurso)      -- ABAC dimensão UO   (novo)
  AND nivelSensibilidade(sujeito, ação) ≥ sens(recurso)-- ABAC sensibilidade (novo)
```

---

## 1. Modelo ATUAL (lido no código)

| Elemento | Onde | Forma |
|---|---|---|
| `Usuario` (AggregateRoot, `IMustHaveTenant`) | `Identidade.Domain/Usuarios/Usuario.cs` | `TenantId`, `Email` (único/tenant via índice central), `Ativo`, `HashSet<PapelId>` |
| `Papel` (AggregateRoot, `IMustHaveTenant`) | `Identidade.Domain/Papeis/Papel.cs` | `TenantId`, `Nome` (único/tenant), `HashSet<string>` de permissões; rejeita escopo fora do catálogo |
| `Permissoes` (catálogo canônico, `FrozenSet`) | `Identidade.Domain/Permissoes/Permissoes.cs` | 22 escopos `modulo.ver`/`modulo.gerenciar` + 5 transversais (identidade, admin.modulos, admin.certificado, admin.auditoria, documentos.assinar) |
| Permissões efetivas | `Application/Internal/CalculadoraPermissoesEfetivas.cs` | **união** dos `Papel.Permissoes` do usuário (sem escopo, sem negação explícita) |
| Token | `Infrastructure/Seguranca/EmissorToken.cs` | JWT HS256; 1 claim `perm` por permissão efetiva + `tenant_id` + `tenant_name` |
| Enforcement | `BuildingBlocks.Infrastructure/Authorization/PermissionAuthorization.cs` | política dinâmica `perm:<escopo>`; handler aprova só se a claim `perm` existe (deny-by-default) |
| Tenant runtime | `ApiHost/Tenancy/TenantContext.cs` | `tenant_id` do JWT (ou `TenantOverride` em workers/login) |
| Sujeito runtime | `ApiHost/Tenancy/CurrentUser.cs` | `sub`/`name`/`email`/IP para auditoria |
| Isolamento | `CLAUDE.md` §5 | DB-per-tenant + Global Query Filter + `TenantSaveChangesInterceptor` |

**Propriedades boas a preservar:** catálogo canônico imutável (negar-por-padrão na concessão),
permissões efetivas como **união** de papéis, enforcement na borda por claim, tenant como fronteira dura.

**Lacunas (diagnóstico `ESTADO-ATUAL.md` §Plataforma):** `/admin/tenants` sem RBAC (escalonamento);
auditoria sem **trilha de leitura LGPD**; nenhuma noção de unidade/lotação; delegação inexistente.

---

## 2. Entidades do modelo ALVO

> Tudo abaixo é **tenant-scoped** (`IMustHaveTenant`) e vive no módulo **Identidade**, exceto o vínculo
> de UO de cada recurso de negócio, que é atributo das próprias entidades dos outros módulos (ver §6).

### 2.1 Unidade Organizacional (UO) — **nova dimensão**

`UnidadeOrganizacional` (AggregateRoot, `IMustHaveTenant`): a árvore de Secretarias/Departamentos/Setores
do ente. **Hierárquica** (auto-relacionamento `UnidadePaiId?`).

| Campo | Descrição |
|---|---|
| `Id`, `TenantId` | identidade forte + tenant |
| `Codigo` | código estável (ex.: `SMS`, `SMS.VIG`) — usado em trilha e remessas |
| `Nome` | "Secretaria Municipal de Saúde" |
| `UnidadePaiId?` | árvore; raiz = `null` |
| `Tipo` | `Secretaria` / `Departamento` / `Setor` / `Gabinete` / `Fundo` [a confirmar: aderência à "Unidade Gestora/Unidade Orçamentária" do PCASP/MSC e ao cadastro de UG do TCE-RS] |
| `UnidadeGestoraContabil?` | vínculo opcional à UG contábil (ponte para Finanças/MSC) [a confirmar] |
| `Ativa` | desativação preserva histórico (nunca deleta — auditoria) |

**Por que importa para o dono (contabilidade/TCE):** empenho, liquidação, dotação e patrimônio são
naturalmente **por Unidade Orçamentária/Gestora**. Amarrar a UO de autorização à UG contábil dá, de graça,
o escopo correto para "o setor de compras da Saúde só empenha na dotação da Saúde".

### 2.2 Escopo de Unidade (vínculo sujeito→UO) — **ABAC do sujeito**

`AtribuicaoDePapel` substitui o `HashSet<PapelId>` plano de `Usuario`. Cada atribuição **carrega um escopo de UO**:

| Campo | Descrição |
|---|---|
| `UsuarioId`, `PapelId`, `TenantId` | quem, qual papel |
| `UnidadeId` | UO **em que** este papel vale (raiz da subárvore) |
| `IncluiSubunidades` | `true` → o papel vale na UO e em todos os descendentes (Secretaria + Departamentos) |
| `Vigencia` | `Inicio`/`Fim?` — atribuição temporária (substituição de férias, mandato) |
| `Origem` | `Direta` (admin do tenant) ou `Delegada` (por delegação — §4) + quem concedeu |

> Um mesmo usuário pode ter o papel "Gestor" na Saúde **e** "Consulta" na Educação: duas atribuições,
> escopos disjuntos. As **permissões efetivas deixam de ser globais** e passam a ser **(permissão, conjunto de UOs)**.

### 2.3 Papel — quase inalterado

`Papel` permanece um agrupador nomeado de permissões do catálogo. **Não** carrega UO (o escopo vem da
atribuição, não do papel — assim o mesmo papel "Gestor de Compras" é reutilizável em qualquer secretaria).
Acréscimo: marcar papéis **`Sistema`** (imutáveis, semeados) vs **`DoTenant`** (editáveis).

### 2.4 Permissão — catálogo evolui de 2 para 3 níveis por módulo + sensibilidade

- **Manter** `modulo.ver` / `modulo.gerenciar` (compatibilidade).
- **[a confirmar]** Acrescentar verbos finos onde há ato com efeito legal/fiscal, p.ex.
  `financas.empenho.assinar`, `financas.exercicio.encerrar`, `transparencia.remessa.transmitir`,
  `protocolo.documento.assinar` — segregação de funções (SoD) exigida pelo TCE.
- **Sensibilidade do dado** como atributo do recurso, não como permissão: ver §2.5.

### 2.5 Nível de Sensibilidade — **ABAC do recurso (LGPD)**

`NivelSensibilidade` (enum de domínio, ordenado): `Publico < Interno < Restrito < SensivelLGPD`.
- Cada recurso de negócio declara seu nível (ex.: prontuário SUAS / PEP Saúde / dados de menor na
  Educação = `SensivelLGPD`; empenho = `Interno`; dado de Transparência = `Publico`).
- O acesso exige que o sujeito tenha **clearance ≥** sensibilidade do recurso, concedido por permissão
  dedicada (ex.: `saude.dados.sensiveis.acessar`) **e** registrado na **trilha de leitura LGPD** (CLAUDE.md §6:
  "quem leu o quê, quando, por quê" — hoje ausente, ver `GAP-E-ROADMAP.md` Eixo 6).

### 2.6 Concessão de Delegação — **delegação de administração com escopo**

`ConcessaoDelegacao` (AggregateRoot, `IMustHaveTenant`): registra que um usuário-administrador delegou a
**outro** o direito de administrar (criar/atribuir/desativar) **dentro de um escopo limitado**.

| Campo | Descrição |
|---|---|
| `DelegantId`, `DelegadoId`, `TenantId` | quem delega, quem recebe |
| `UnidadeEscopo` + `IncluiSubunidades` | a subárvore de UO onde o delegado pode administrar |
| `PermissoesDelegaveis` | subconjunto **das permissões que o delegant possui** que o delegado pode conceder a terceiros |
| `Vigencia` | janela temporal |
| `PodeSubdelegar` | `false` por padrão (ver invariante I7) |

Isto materializa "**secretário cria/gerencia usuários só na própria secretaria e não concede o que não tem**".

---

## 3. Regras de concessão (quem pode conceder o quê)

1. **Concessão de papel = atribuição com escopo.** Atribuir papel P ao usuário U na unidade X exige que
   o concedente: (a) tenha permissão de administração (`identidade.usuarios.gerenciar`); (b) tenha **escopo
   administrativo** sobre X (X ∈ subárvore que ele administra); (c) **possua, ele próprio**, todas as
   permissões contidas em P, no escopo X ou superior. → *"não delega o que não tem"*.
2. **Catálogo fechado.** Nenhum escopo fora de `Permissoes.Todas` é persistível (mantém o comportamento
   atual de `Papel.DefinirPermissoes`).
3. **Sensibilidade não escala por papel comum.** Conceder uma permissão `*.dados.sensiveis.acessar` exige
   que o concedente também a possua **e** uma base legal LGPD registrada na concessão [a confirmar: forma
   do registro de base legal / encarregado].
4. **Atos com SoD são indelegáveis em bloco.** Permissões de *assinar/transmitir/encerrar exercício* só
   podem ser concedidas por um administrador do tenant (não por delegado), e nunca acumuladas com a função
   que produz o ato (ex.: quem liquida não paga) [a confirmar: matriz de segregação exigida pelo TCE-RS].
5. **Separação por poder é absoluta.** Não existe concessão cross-tenant; um administrador do Executivo
   **não** enxerga nem administra usuários da Câmara (tenant distinto). Não há "super-admin global de cliente".

---

## 4. Delegação de administração — algoritmo "não delega o que não tem"

Quando o **delegado** D tenta conceder o papel P ao usuário U na unidade X, o sistema aprova **somente se TODAS**:

```
(D1) X ∈ escopoAdministrativo(D)                      -- está dentro da subárvore delegada a D
(D2) U ∈ escopoAdministrativo(D)                      -- só administra gente do seu escopo
(D3) permissoes(P) ⊆ ConcessaoDelegacao(D).PermissoesDelegaveis
(D4) permissoes(P) ⊆ permissoesEfetivas(D, em X)      -- D realmente possui o que vai conceder, em X
(D5) sensibilidade(P) ≤ clearance(D)                  -- não eleva sensibilidade acima da própria
(D6) vigência da concessão e da atribuição válidas em 'agora'
```

**Exemplo (o do dono):** o Secretário de Saúde tem `ConcessaoDelegacao{ escopo=SMS, delegáveis=[saude.ver,
saude.gerenciar, identidade.usuarios.gerenciar] }`. Ele cria a enfermeira-chefe como usuária **na UBS Central**
(subunidade de SMS — OK por D1/D2) e lhe dá o papel "Operador de Saúde" (⊆ delegáveis e ⊆ as dele — OK D3/D4).
Se ele tentar dar `legislativo.gerenciar` → **negado por D3/D4** (não consta nas delegáveis e ele não a possui).
Se tentar criar usuário lotado na Educação → **negado por D2** (fora de SMS). Não enxerga prontuários de outra
secretaria por D5 + filtro de UO (§5).

---

## 5. Como a decisão é tomada em runtime (enforcement)

**Borda HTTP (igual hoje, mantém deny-by-default):** `RequirePermission` continua barrando por **presença
da permissão** (verbo + módulo). É o filtro grosso.

**Filtro fino por UO (novo, em duas camadas):**
- **Query-side:** assim como o **Global Query Filter** de tenant, acrescentar um **filtro de UO** que
  restringe leituras às UOs do escopo efetivo do sujeito (derivado das `AtribuicaoDePapel`). Isto resolve
  "listar empenhos" devolvendo só os da(s) secretaria(s) do usuário, sem cada handler reimplementar.
- **Command-side:** o handler valida `unidade(recurso) ∈ escopoUO(sujeito, permissão)` antes de mutar
  (guard de domínio/aplicação), porque o recurso-alvo pode vir por id.

**Transporte do escopo no token:** o JWT deixa de levar só `perm:X`. Opções:
- **(a) claims compactas** `scope:<perm>@<UOcodes>` quando o nº de UOs é pequeno; ou
- **(b) token enxuto + resolução server-side** do escopo a cada requisição (cache por usuário), evitando
  tokens gigantes em prefeituras com muitas unidades. **Recomendado (b)** [a confirmar: tamanho de token,
  TTL de cache, invalidação ao revogar atribuição].

**Sensibilidade:** verificada no command/query-side (clearance ≥ sensibilidade) + emissão obrigatória do
evento de **trilha de leitura LGPD** em todo acesso a recurso `SensivelLGPD`.

---

## 6. Impacto nos módulos de negócio

- Entidades de negócio que hoje são só `IMustHaveTenant` passam a expor, onde fizer sentido, um
  **`UnidadeId`** (a UO dona/responsável) e um **`NivelSensibilidade`**. Candidatos prioritários (foco fiscal):
  Finanças (Empenho/Dotação por UO/UG), Patrimônio (bem por setor), RH (servidor por lotação), e os
  sensíveis Saúde/Assistência/Educação.
- Introduzir interface marcadora `IMustHaveUnidade { Guid UnidadeId }` em `SharedKernel`, espelhando
  `IMustHaveTenant`, para o filtro de UO aplicar por reflexão (sem acoplar módulos). **Cross-module continua
  só por Contracts** (CLAUDE.md §2): a UO é um Guid + código, não uma referência ao agregado da Identidade.
- A ponte **UO ↔ Unidade Gestora contábil** alinha o escopo de autorização com a MSC/PCASP e as remessas
  TCE-RS (`docs/architecture/contabilidade-pcasp-tce.md`) — autorização e prestação de contas falam a
  mesma dimensão organizacional. [a confirmar: granularidade da UG no leiaute SIAPC/PAD do exercício.]

---

## 7. Invariantes (o que NUNCA pode quebrar)

- **I1 — Fronteira de tenant é absoluta.** Nenhuma decisão de autorização atravessa tenant; Executivo e
  Legislativo são isolados por construção (DB-per-tenant + query filter). Cross-tenant = bug crítico.
- **I2 — Deny-by-default em todas as camadas.** Ausência de permissão **e** ausência de escopo de UO negam.
- **I3 — Catálogo de permissões é fechado e imutável em runtime.** Só escopos de `Permissoes.Todas`.
- **I4 — Não delega o que não tem.** Toda concessão é ⊆ das permissões efetivas do concedente no escopo
  (regras D3/D4). Provado no domínio, não só na UI.
- **I5 — Escopo de UO é subárvore conexa.** `IncluiSubunidades` expande para descendentes; nunca concede
  unidade fora da subárvore administrada.
- **I6 — Sensibilidade é monotônica.** Ninguém concede clearance acima da própria (D5); acesso a
  `SensivelLGPD` sempre gera trilha de leitura.
- **I7 — Subdelegação fechada por padrão.** `PodeSubdelegar=false`; profundidade de subdelegação limitada
  e auditada quando habilitada (evita cadeias de privilégio incontroláveis).
- **I8 — Atribuições e delegações têm vigência e são auditadas.** Conceder/revogar gera trilha imutável
  (antes/depois, quem, quando, IP) — exigência do TCE.
- **I9 — Segregação de funções (SoD).** Permissões conflitantes [a confirmar: matriz oficial] não coexistem
  no mesmo sujeito dentro da mesma UO (ex.: liquidar × pagar; emitir × auditar).
- **I10 — Revogação é imediata.** Revogar atribuição/delegação invalida o escopo na próxima decisão
  (server-side) — não esperar expirar o JWT para recursos sensíveis/financeiros.

---

## 8. GAP vs. o que temos hoje

| Capacidade alvo | Hoje | GAP | Esforço |
|---|---|---|---|
| Tenant como fronteira | ✅ DB-per-tenant + query filter | — (preservar) | — |
| RBAC plano por módulo | ✅ `Papel`+`Permissoes`+claim `perm` | — (base reaproveitada) | — |
| Catálogo canônico fechado | ✅ `FrozenSet` + `EhConhecida` | estender com verbos finos/SoD | 🟡 baixo |
| **Unidade Organizacional (árvore)** | ❌ inexistente | criar agregado `UnidadeOrganizacional` + CRUD + seed | 🔴 alto |
| **Atribuição papel→UO (ABAC sujeito)** | ❌ `Usuario._papeis` é só `HashSet<PapelId>` | trocar por `AtribuicaoDePapel` com escopo+vigência+origem | 🔴 alto |
| **Filtro de leitura por UO** | ❌ só filtro de tenant | `IMustHaveUnidade` + global filter por UO + guards de command | 🔴 alto |
| **Sensibilidade do dado (ABAC recurso)** | ❌ inexistente | enum `NivelSensibilidade` + clearance + verbos `*.sensiveis.*` | 🟠 médio |
| **Trilha de leitura LGPD** | ❌ ausente (ESTADO-ATUAL §Plataforma) | evento + store imutável de acesso a `SensivelLGPD` | 🟠 médio (cruza Eixo 6) |
| **Delegação de admin com escopo** | ❌ `identidade.usuarios.gerenciar` é tudo-ou-nada | `ConcessaoDelegacao` + algoritmo D1–D6 | 🔴 alto |
| Segregação de funções (SoD) | ❌ união livre de papéis | regra I9 + matriz | 🟠 médio [a confirmar] |
| Escopo no token | ⚠️ só `perm:X` global | resolução server-side de escopo (recomendado) + cache/revogação | 🟠 médio |
| `/admin/tenants` protegido | ❌ só `.RequireAuthorization()` | RBAC real (já no `GAP-E-ROADMAP` 1.6) | 🔴 (já priorizado) |
| Auditoria de concessão/revogação | ⚠️ trilha existe mas não-imutável de fato | I8 + WORM/hash-chain (Eixo 6) | 🟠 médio |

**Ordem sugerida (sem furar a prioridade do dono):** este modelo é **pré-requisito de qualidade** do
Eixo 1 (M1), pois o escopo por UO/UG e a SoD são exigências do TCE para contabilidade e prestação de contas.
Mínimo viável para M1: UO + Atribuição com escopo + filtro de UO + RBAC no `/admin/tenants` + verbos SoD de
Finanças. Delegação completa e sensibilidade/LGPD podem seguir em M1.x/M2.

---

## 9. Pontos jurídicos/normativos a confirmar [a confirmar]

1. Aderência do `Tipo`/`UnidadeGestoraContabil` da UO ao conceito de **Unidade Gestora/Orçamentária** do
   PCASP/MSC e ao cadastro de UG do **TCE-RS** (granularidade do SIAPC/PAD do exercício).
2. **Matriz oficial de Segregação de Funções (SoD)** exigida pelo TCE-RS para o ciclo da despesa
   (empenho/liquidação/pagamento) e para assinatura/transmissão de remessas.
3. Forma de registro da **base legal LGPD** e papel do **Encarregado (DPO)** na concessão de clearance a
   dados sensíveis (Saúde/Assistência/menores na Educação).
4. Verbos finos a adicionar ao catálogo (atos com efeito legal): assinatura (Lei 14.063/2020),
   transmissão de remessa, encerramento de exercício.
5. Política de subdelegação admissível no setor público (profundidade, prazo, formalização).
6. TTL de token / janela de revogação aceitável para recursos financeiros e sensíveis.

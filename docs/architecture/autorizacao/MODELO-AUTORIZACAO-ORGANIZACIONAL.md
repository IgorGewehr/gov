# Modelo de Autorização Organizacional — Tensorroot.Gov

> **Documento-alvo consolidado.** Reúne, num único modelo implementável, o desenho de autorização do
> Tensorroot.Gov: **unidades organizacionais**, **RBAC + ABAC**, **delegação com escopo**, **separação
> de poderes por tenant** e **segregação LGPD**, com o **GAP vs. o código atual** e um **plano de
> implementação** (entidades, migrations, UI).
>
> Consolida e supera os estudos de planejamento que lhe deram origem (organograma real
> prefeitura/câmara, fundamento jurídico de separação de poderes + LGPD, e modelo-alvo técnico);
> o conteúdo durável desses estudos vive aqui e no **ADR-0007**.
>
> Lido contra o código real: `src/Modules/Identidade/...Domain/{Usuarios,Papeis,Permissoes}` e
> `CONVENCOES-ENGENHARIA.md` §5/§6. **Não** reescreve as convenções nem o diagnóstico — estende-os.
> Marcações **[a confirmar]** = ponto jurídico/normativo a validar antes de implementar (CONVENCOES-ENGENHARIA.md §8).
> Piloto: Maximiliano de Almeida/RS (TCE-RS). Data: 2026-06-22.

---

## 0. Resumo executivo (o salto, em 10 linhas)

1. **Hoje:** RBAC plano, tenant-scoped — `Usuario.HashSet<PapelId>` → `Papel.HashSet<string>` (catálogo
   canônico de 27 escopos `modulo.ver`/`modulo.gerenciar`), claims `perm` no JWT, deny-by-default na borda.
2. **Limite 1 — sem dimensão organizacional:** quem tem `financas.gerenciar` gerencia o tenant inteiro; não
   existe "empenho só da Secretaria de Saúde".
3. **Limite 2 — sem ABAC:** não há condicionamento por atributo do recurso (UO dona, sensibilidade LGPD) nem
   do sujeito (lotação); a trilha de leitura LGPD não tem âncora de escopo.
4. **Limite 3 — sem delegação com escopo:** `identidade.usuarios.gerenciar` é tudo-ou-nada — viola a regra do
   dono **"não delega o que não tem"**.
5. **Alvo:** RBAC + ABAC com **Unidade Organizacional (UO)** + **delegação de administração com escopo**,
   mantendo **separação de poderes por tenant** (Executivo ≠ Legislativo = CNPJs distintos — invariante dura).
6. Decisão de autorização vira função: `tenant(s)==tenant(r)` **E** `possuiPermissão` **E** `escopoUO(s)⊇UO(r)`
   **E** `clearance(s)≥sensibilidade(r)`.
7. **Regra-mãe (I4):** toda concessão ⊆ permissões efetivas do concedente no escopo — provado no domínio.
8. **Separação LGPD:** sensibilidade é atributo do recurso (`Publico<Interno<Restrito<SensivelLGPD`), reforçada
   pelo isolamento de módulo (RH não enxerga PEP/SUAS por contrato) + trilha de **leitura** (hoje ausente).
9. **GAP:** UO, atribuição papel→UO, filtro de UO, sensibilidade, delegação e trilha de leitura **não existem**;
   tenant-boundary e RBAC plano já existem e são reaproveitados.
10. **Cobertura:** UO + `AtribuicaoDePapel` com escopo + filtro de UO + RBAC no `/admin/tenants` +
    verbos SoD de Finanças, além de delegação completa + sensibilidade/LGPD.

---

## 1. Modelo ATUAL (lido no código)

| Elemento | Onde | Forma |
|---|---|---|
| `Usuario` (AggregateRoot, `IMustHaveTenant`) | `Identidade.Domain/Usuarios/Usuario.cs` | `TenantId`, `Email`, `Ativo`, **`HashSet<PapelId>`** (plano, sem escopo) |
| `Papel` (AggregateRoot, `IMustHaveTenant`) | `Identidade.Domain/Papeis/Papel.cs` | `TenantId`, `Nome` único/tenant, `HashSet<string>`; rejeita escopo fora do catálogo |
| `Permissoes` (catálogo `FrozenSet`) | `Identidade.Domain/Permissoes/Permissoes.cs` | 22 escopos `modulo.ver`/`modulo.gerenciar` + 5 transversais (`identidade.usuarios.gerenciar`, `admin.modulos.configurar`, `admin.certificado.gerenciar`, `admin.auditoria.ver`, `documentos.assinar`) |
| Permissões efetivas | `Application/Internal/CalculadoraPermissoesEfetivas.cs` | **união** dos `Papel.Permissoes` (sem escopo, sem negação explícita) |
| Token | `Infrastructure/Seguranca/EmissorToken.cs` | JWT; 1 claim `perm` por permissão + `tenant_id`/`tenant_name` |
| Enforcement | `BuildingBlocks.Infrastructure/Authorization` | política `perm:<escopo>`, deny-by-default por presença de claim |
| Tenant / isolamento | `ApiHost/Tenancy` + `CONVENCOES-ENGENHARIA.md` §5 | DB-per-tenant + Global Query Filter + `TenantSaveChangesInterceptor` |

**Propriedades boas a preservar:** catálogo canônico imutável (`EhConhecida` nega-por-padrão na concessão);
permissões efetivas como **união** de papéis; enforcement por claim na borda; **tenant como fronteira dura**.

---

## 2. Entidades do modelo ALVO (todas `IMustHaveTenant`, módulo Identidade)

### 2.1 `UnidadeOrganizacional` — nova dimensão (árvore)
Árvore de Secretarias/Departamentos/Setores do ente, auto-relacionada (`UnidadePaiId?`).

| Campo | Descrição |
|---|---|
| `Id`, `TenantId` | identidade + tenant |
| `Codigo` | código estável (`SMS`, `SMS.VIG`) — usado em trilha e remessas |
| `Nome` | "Secretaria Municipal de Saúde" |
| `UnidadePaiId?` | árvore; raiz = `null` |
| `Tipo` | `Secretaria`/`Departamento`/`Setor`/`Gabinete`/`Fundo` [a confirmar: aderência a Unidade Gestora/Orçamentária do PCASP/MSC e ao cadastro de UG do TCE-RS] |
| `UnidadeGestoraContabilId?` | ponte opcional à UG contábil (Finanças/MSC) [a confirmar] |
| `Ativa` | desativa preservando histórico (nunca deleta — auditoria) |

**Por que importa ao dono (contabilidade/TCE):** empenho, dotação e patrimônio são por Unidade
Orçamentária/Gestora. Amarrar a UO à UG dá, de graça, "o compras da Saúde só empenha na dotação da Saúde",
e alinha autorização e prestação de contas à **mesma** dimensão organizacional (SIAPC/PAD, MSC/SICONFI).

### 2.2 `AtribuicaoDePapel` — ABAC do sujeito (substitui `HashSet<PapelId>`)
| Campo | Descrição |
|---|---|
| `UsuarioId`, `PapelId`, `TenantId` | quem, qual papel |
| `UnidadeId` | UO (raiz da subárvore) em que o papel vale |
| `IncluiSubunidades` | `true` → vale na UO e em todos os descendentes |
| `Vigencia` (`Inicio`/`Fim?`) | atribuição temporária (férias, mandato) |
| `Origem` | `Direta` (admin do tenant) ou `Delegada` (§4) + quem concedeu |

Permissões efetivas deixam de ser globais e passam a ser **(permissão, conjunto de UOs)**.

### 2.3 `Papel` — quase inalterado
Agrupador nomeado de permissões do catálogo; **não** carrega UO (escopo vem da atribuição → mesmo papel
"Gestor de Compras" é reutilizável em qualquer secretaria). Acréscimo: flag `Sistema` (imutável, semeado)
vs `DoTenant` (editável).

### 2.4 `Permissao` — catálogo evolui (compatível) + verbos SoD
- **Manter** `modulo.ver`/`modulo.gerenciar`.
- **[a confirmar]** Acrescentar verbos finos onde há ato com efeito legal/fiscal: `financas.empenho.assinar`,
  `financas.liquidacao.atestar`, `financas.pagamento.ordenar`, `financas.exercicio.encerrar`,
  `transparencia.remessa.transmitir`, `protocolo.documento.assinar` — base da Segregação de Funções (SoD).

### 2.5 `NivelSensibilidade` — ABAC do recurso (LGPD)
Enum ordenado `Publico < Interno < Restrito < SensivelLGPD`. Cada recurso declara seu nível (PEP Saúde,
prontuário SUAS, dado de menor na Educação, sigilo fiscal = `SensivelLGPD`; empenho = `Interno`;
Transparência = `Publico`). Acesso exige **clearance ≥** sensibilidade (via permissão dedicada, ex.
`saude.dados.sensiveis.acessar`) **e** gera **trilha de leitura LGPD** (quem leu o quê, quando, por quê).

### 2.6 `ConcessaoDelegacao` — delegação de administração com escopo
| Campo | Descrição |
|---|---|
| `DelegantId`, `DelegadoId`, `TenantId` | quem delega, quem recebe |
| `UnidadeEscopo` + `IncluiSubunidades` | subárvore onde o delegado administra |
| `PermissoesDelegaveis` | subconjunto **das permissões que o delegant possui** |
| `Vigencia` | janela temporal |
| `PodeSubdelegar` | `false` por padrão (I7) |

Materializa "secretário cria/gerencia usuários só na própria secretaria e não concede o que não tem".

---

## 3. Regras de concessão (quem pode conceder o quê)

1. **Concessão = atribuição com escopo.** Atribuir papel P a U na unidade X exige do concedente: (a)
   `identidade.usuarios.gerenciar`; (b) **escopo administrativo** sobre X; (c) **possuir, ele próprio**, todas
   as permissões de P em X ou superior → *"não delega o que não tem"*.
2. **Catálogo fechado** (mantém `Papel.DefinirPermissoes`/`Permissoes.EhConhecida`).
3. **Sensibilidade não escala por papel comum:** conceder `*.dados.sensiveis.acessar` exige possuí-la **e**
   base legal LGPD registrada [a confirmar: forma do registro e papel do DPO].
4. **Atos com SoD são indelegáveis em bloco:** assinar/ordenar/transmitir/encerrar exercício só por admin do
   tenant, nunca acumulados com a função que produz o ato (quem liquida ≠ quem paga) [a confirmar: matriz TCE-RS].
5. **Separação por poder é absoluta:** nenhuma concessão cross-tenant; admin do Executivo não enxerga a Câmara;
   **não existe super-admin global de cliente**.

---

## 4. Delegação — algoritmo "não delega o que não tem"

Delegado **D** concede papel **P** a usuário **U** na unidade **X**: aprovar **somente se TODAS**:
```
(D1) X ∈ escopoAdministrativo(D)
(D2) U ∈ escopoAdministrativo(D)
(D3) permissoes(P) ⊆ ConcessaoDelegacao(D).PermissoesDelegaveis
(D4) permissoes(P) ⊆ permissoesEfetivas(D, em X)
(D5) sensibilidade(P) ≤ clearance(D)
(D6) vigência da concessão e da atribuição válidas em 'agora'
```
**Exemplo do dono:** Secretário de Saúde com `ConcessaoDelegacao{escopo=SMS, delegáveis=[saude.ver,
saude.gerenciar, identidade.usuarios.gerenciar]}` cria a enfermeira-chefe na UBS Central (subunidade de SMS —
OK D1/D2) com papel "Operador de Saúde" (⊆ delegáveis e ⊆ as dele — OK D3/D4). Tentar `legislativo.gerenciar`
→ negado (D3/D4). Lotar na Educação → negado (D2). Não vê prontuários de outra secretaria (D5 + filtro de UO).

---

## 5. Enforcement em runtime

- **Borda HTTP (igual hoje):** `RequirePermission` barra por presença do verbo+módulo (filtro grosso,
  deny-by-default).
- **Filtro fino por UO (novo):**
  - *Query-side:* análogo ao Global Query Filter de tenant — **filtro de UO** que restringe leituras às UOs do
    escopo efetivo do sujeito (derivado das `AtribuicaoDePapel`), aplicado por reflexão via marcador
    `IMustHaveUnidade { Guid UnidadeId }` em SharedKernel.
  - *Command-side:* o handler valida `unidade(recurso) ∈ escopoUO(sujeito, permissão)` antes de mutar (guard).
- **Sensibilidade:** clearance ≥ sensibilidade no command/query-side + emissão obrigatória do evento de
  **trilha de leitura LGPD** em todo acesso a `SensivelLGPD`.
- **Token:** **recomendado (b)** token enxuto + resolução server-side do escopo por requisição (cache por
  usuário) — evita JWT gigante em prefeituras com muitas UOs e permite **revogação imediata** (I10)
  [a confirmar: TTL/janela de revogação para recursos financeiros e sensíveis].

---

## 6. Impacto nos módulos de negócio
Entidades de negócio ganham, onde fizer sentido, `UnidadeId` (UO dona) e `NivelSensibilidade`. Prioridade
fiscal: Finanças (Empenho/Dotação por UO/UG), Patrimônio (bem por setor), RH (servidor por lotação) e os
sensíveis Saúde/Assistência/Educação. **Cross-module continua só por `*.Contracts`** (CONVENCOES-ENGENHARIA.md §2): a UO é
um `Guid`+código, não referência ao agregado da Identidade. A ponte UO↔UG alinha autorização e prestação de
contas (SIAPC/PAD, MSC) [a confirmar: granularidade da UG no leiaute do exercício].

---

## 7. Separação de poderes por tenant (limite mais externo — não muda)
Executivo (Prefeitura) e Legislativo (Câmara) do mesmo município são **tenants distintos** (CNPJs distintos),
por **simetria constitucional** (CF arts. 2º/25/29/31) e **independência dos Poderes**. O Prefeito **jamais**
acessa o sistema da Câmara e vice-versa (ingerência inconstitucional; quebraria o controle externo recíproco).
Único acoplamento legítimo = **troca de artefatos auditados**: repasse do **duodécimo** (CF 168/29-A) como
integração inter-tenant via Outbox, consolidação contábil e remessas TCE-RS/SICONFI por poder — **nunca**
leitura cruzada de banco. **Papéis especiais** (todos restritos ao próprio tenant): Controlador/Controle
Interno (CF 74 — leitura ampla intra-tenant, não muta, não vê sensível individual sem processo), Procurador
(sigilo fiscal para dívida ativa/execução — CTN 198), Auditor TCE-RS (externo, por remessa/requisição formal,
somente leitura auditada). Admin da Plataforma (Tensorroot) provisiona/licencia — **sem leitura de negócio**.

---

## 8. Invariantes (o que NUNCA pode quebrar)
- **I1** Fronteira de tenant absoluta (cross-tenant = bug crítico).
- **I2** Deny-by-default em todas as camadas (ausência de permissão **e** de escopo de UO negam).
- **I3** Catálogo de permissões fechado/imutável em runtime.
- **I4** **Não delega o que não tem** — toda concessão ⊆ permissões efetivas do concedente no escopo (D3/D4),
  provado no domínio, não só na UI.
- **I5** Escopo de UO é subárvore conexa (`IncluiSubunidades` expande para descendentes).
- **I6** Sensibilidade monotônica (ninguém concede clearance acima da própria; `SensivelLGPD` sempre gera trilha).
- **I7** Subdelegação fechada por padrão (`PodeSubdelegar=false`; profundidade limitada/auditada).
- **I8** Atribuições/delegações têm vigência e geram trilha imutável (antes/depois, quem, quando, IP).
- **I9** Segregação de funções (SoD): permissões conflitantes [a confirmar: matriz] não coexistem no mesmo
  sujeito na mesma UO (liquidar × pagar; emitir × auditar).
- **I10** Revogação imediata (server-side) — não esperar o JWT expirar para recursos sensíveis/financeiros.

---

## 9. GAP vs. o que temos hoje

| Capacidade alvo | Hoje (no código) | GAP | Esforço |
|---|---|---|---|
| Tenant como fronteira | ✅ DB-per-tenant + query filter | preservar | — |
| RBAC plano por módulo | ✅ `Papel`+`Permissoes`+claim `perm` | base reaproveitada | — |
| Catálogo canônico fechado | ✅ `FrozenSet`+`EhConhecida` | estender com verbos finos/SoD | 🟡 baixo |
| **UO (árvore)** | ❌ inexistente | agregado `UnidadeOrganizacional` + CRUD + seed | 🔴 alto |
| **Atribuição papel→UO (ABAC sujeito)** | ❌ `Usuario._papeis` é `HashSet<PapelId>` plano | trocar por `AtribuicaoDePapel` (escopo+vigência+origem) | 🔴 alto |
| **Filtro de leitura por UO** | ❌ só filtro de tenant | `IMustHaveUnidade` + global filter de UO + guards de command | 🔴 alto |
| **Sensibilidade do dado (ABAC recurso)** | ❌ inexistente | `NivelSensibilidade` + clearance + verbos `*.sensiveis.*` | 🟠 médio |
| **Trilha de leitura LGPD** | ❌ ausente (só trilha de mutação) | evento + store imutável de acesso a `SensivelLGPD` | 🟠 médio (cruza Eixo 6) |
| **Delegação de admin com escopo** | ❌ `identidade.usuarios.gerenciar` tudo-ou-nada | `ConcessaoDelegacao` + algoritmo D1–D6 | 🔴 alto |
| SoD (segregação de funções) | ❌ união livre de papéis | regra I9 + matriz | 🟠 médio [a confirmar] |
| Escopo no token | ⚠️ só `perm:X` global | resolução server-side + cache/revogação | 🟠 médio |
| `/admin/tenants` protegido | ❌ só `.RequireAuthorization()` | RBAC real | 🔴 (priorizado) |
| Auditoria de concessão/revogação | ⚠️ trilha existe, não-imutável de fato | I8 + WORM/hash-chain (Eixo 6) | 🟠 médio |

---

## 10. Plano de implementação

### 10.1 Entidades / domínio (módulo Identidade)
- `UnidadeOrganizacional` (AggregateRoot) com `UnidadeOrganizacionalId`, árvore + invariante de subárvore conexa.
- `AtribuicaoDePapel` (substitui `Usuario._papeis`): entidade filha do `Usuario` ou agregado próprio, com
  `UnidadeId`, `IncluiSubunidades`, `Vigencia` (VO), `Origem` (VO `Direta`/`Delegada`+concedente).
- `ConcessaoDelegacao` (AggregateRoot) com algoritmo D1–D6 no domínio (não na UI).
- `NivelSensibilidade` (enum de domínio ordenado) em SharedKernel.
- `Permissoes`: acrescentar verbos finos/SoD ao `FrozenSet` (compatível — escopos novos, nada removido).
- `IMustHaveUnidade { Guid UnidadeId }` em SharedKernel (espelha `IMustHaveTenant`).
- `Papel`: flag `Sistema` vs `DoTenant`.

### 10.2 Migrations (schema `identidade`, EF Core 8, Fluent API)
1. `unidades_organizacionais` (auto-FK `UnidadePaiId`, índice único `TenantId+Codigo`, FK opcional UG contábil).
2. `atribuicoes_papel` (FKs Usuario/Papel/Unidade, colunas de vigência/origem) + **migração de dados**:
   converter cada par `(Usuario, PapelId)` atual numa `AtribuicaoDePapel` na **UO raiz** com
   `IncluiSubunidades=true`, `Origem=Direta` (compatibilidade — comportamento global preservado no go-live).
3. `concessoes_delegacao` (+ tabela de junção `PermissoesDelegaveis`).
4. Coluna `NivelSensibilidade` nas entidades sensíveis (Saúde/Assistência/Educação/Tributos) e `UnidadeId`
   nas entidades fiscais (Finanças/Patrimônio/RH) — migrations **por módulo** (CONVENCOES-ENGENHARIA.md §9).
5. Store de **trilha de leitura LGPD** (append-only) — coordenar com Eixo 6 (WORM/hash-chain).

### 10.3 Aplicação / enforcement
- `CalculadoraPermissoesEfetivas`: passar a devolver **(permissão → conjunto de UOs)** em vez de set plano.
- Resolução server-side do escopo por requisição (cache por usuário + invalidação ao revogar — I10).
- Global Query Filter de UO por reflexão sobre `IMustHaveUnidade`; guards de command-side.
- Handlers de concessão/delegação validam D1–D6 e I9 (SoD) e emitem trilha (I8).

### 10.4 UI (React / gov.br DS — CONVENCOES-ENGENHARIA.md §13)
- Tela **Estrutura Organizacional**: árvore de UOs (CRUD, ativar/desativar, vínculo UG contábil).
- Tela **Usuário → Atribuições**: papel + UO + incluir subunidades + vigência (substitui o seletor plano de papéis).
- Tela **Delegações**: conceder/revogar com escopo, mostrando apenas permissões que o concedente possui (UI
  reflete I4, mas a regra é provada no domínio).
- **Banner de sensibilidade** + justificativa obrigatória ao abrir recurso `SensivelLGPD` (alimenta a trilha de leitura).
- RBAC real na tela `/admin/tenants` (corrige gap atual).

### 10.5 Cobertura do modelo
Este modelo é **pré-requisito de qualidade** da contabilidade/TCE: escopo por UO/UG e SoD são exigências do TCE.
O modelo provê o conjunto completo:
- UO + `AtribuicaoDePapel` com escopo + filtro de UO + RBAC no `/admin/tenants` + verbos SoD de Finanças.
- Delegação completa (D1–D6), sensibilidade/clearance, trilha de leitura LGPD, SoD I9 plena,
  resolução de escopo server-side com revogação imediata.

---

## 11. Pontos jurídicos/normativos a confirmar [a confirmar]
1. Aderência de `Tipo`/`UnidadeGestoraContabil` da UO ao conceito de Unidade Gestora/Orçamentária do PCASP/MSC
   e ao cadastro de UG do TCE-RS (granularidade do SIAPC/PAD do exercício).
2. Matriz oficial de **Segregação de Funções (SoD)** do TCE-RS para o ciclo da despesa e assinatura/transmissão.
3. Forma de registro da **base legal LGPD** e papel do **Encarregado (DPO)** na concessão de clearance sensível.
4. Verbos finos de atos com efeito legal (assinatura — Lei 14.063/2020; transmissão de remessa; encerramento).
5. Política de **subdelegação** admissível no setor público (profundidade, prazo, formalização).
6. TTL de token / janela de revogação aceitável para recursos financeiros e sensíveis.
7. Estrutura administrativa real do piloto (Maximiliano de Almeida/RS) — definida por lei municipal,
   **parametrizável por tenant**, nunca hard-coded.

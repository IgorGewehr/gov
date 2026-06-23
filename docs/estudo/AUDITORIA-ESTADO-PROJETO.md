# RADIOGRAFIA DO ESTADO DO PROJETO — Tensorroot.Gov

**Auditoria independente de profundidade + qualidade + governança**
Engenheiro-chefe / auditor isento. **Read-only — este documento é informativo; nada foi corrigido.**

- **Régua:** PLANO-PROFUNDIDADE (Ondas 0–3) + CLAUDE.md (domínio rico, padrão-referência Tributos, sem números mágicos, BDD/spec travável) + fitness functions reais.
- **Âncora:** estado ATUAL do código/testes/rules.md/migrations (pós-53 commits, ~206k linhas em `src`, 14 módulos), **nunca** as fotos pré-M0.
- **Data:** 2026-06-23.
- **Escopo:** consolidação de 5 frentes de auditoria — Cluster FISCAL/CORE, Cluster RH/Patrimônio/Social, Cluster LEGISLATIVO/Plataforma e a Auditoria de Governança Transversal (8 pilares).

---

## 1. VEREDITO GERAL

**Estamos NO PADRÃO de profundidade e qualidade, e MAJORITARIAMENTE no padrão de governança — com um (1) risco sistêmico real de produção a sanar antes do M10.** Os 14 módulos exibem domínio rico de ponta a ponta no nível do padrão-referência Tributos (máquinas de estado, invariantes numeradas e rastreáveis ao rules.md, citação legal, eventos por transição, zero modelo anêmico, zero número mágico — percentuais/prazos/limites são constantes nomeadas ou parâmetros por tenant/vigência), as Ondas 0–3 do PLANO-PROFUNDIDADE **fecharam de verdade** (os "smoking guns" de rasura que o próprio plano citava — Aluno/Turma inexistentes, Inventário com 0 ocorrências, `PossuiVaga` checando `id != Guid.Empty` — estão **corrigidos no código**), e as fitness functions são reais e verdes (isolamento de módulo por Contracts, negação cross-tenant, SpecCodeConsistency bidirecional, maintainability <500 sem god-file de produção). O drift de código residual é estreito e conhecido: **largura operacional** (o "GUID digitado" ainda é a porta relacional em formulários de Tributos/Administração/Finanças — Onda 0/1) e a **casca pública React do Portal de Transparência** (Onda 2, back-end pronto, SPA cidadão WIP). O único achado **fora do padrão** é de governança e não de domínio: **9 migrations escritas à mão sem `.Designer.cs`**, deixando os ModelSnapshot de Saúde/Finanças comprovadamente atrás do modelo — bomba-relógio para `MigrateAsync` em produção. Honestamente: o sistema não está raso e não tem drift estrutural; o maior "drift" é **documental** (o PLANO-PROFUNDIDADE descreve um passado já superado) e o maior **risco** é **operacional de deploy** (EF snapshots), não de qualidade de domínio.

---

## 2. TABELA POR MÓDULO

Profundidade: **completo** / raso · Qualidade: **no padrão** / drift / abaixo.

| Cluster | Módulo | Profundidade | Qualidade | Nota | Âncora / observação |
|---|---|---|---|---|---|
| FISCAL | **Tributos** (referência) | Completo | No padrão | padrão-ouro | `DividaAtiva.cs` exemplar: prescrição CTN 174 por datas-do-fato, máquina de estados, encargos parametrizáveis. 15 arq. de teste |
| FISCAL | **Financas** | Completo | No padrão | No padrão | Empenho→Liquidação→Pagamento→RP (4.320), PCASP/MCASP, PPA/LDO/LOA, MSC, DCASP. `Empenho.cs:247` fail-closed C-B6. 21 arq. de teste |
| FISCAL | **Transparencia** | Completo (back-end além da foto) | No padrão (ressalva SPA) | No padrão | TCE-RS SIAPC/PAD + SICONFI/MSC + e-SIC (LAI) + Núcleo Fiscal + Portal Público no back-end. **SPA cidadão WIP**. `ApuradorMinimo.cs` 100% parametrizável |
| FISCAL | **Administracao** | Completo | No padrão | No padrão | Licitações (14.133), SICAF/sanções, Contratos+Aditivos+PNCP. `Contrato.cs:28` invariantes I-1..I-15 + limites legais nomeados. 6 arq. de teste |
| RH/SOCIAL | **Educação** | Completo | No padrão | 9,0 | Aluno+Turma agora são agregados (eram o bloqueador nº1). `Turma.cs:202-227` PossuiVaga real (`Matriculados < Vagas`). PNAE/PNATE |
| RH/SOCIAL | **RH** | Completo | No padrão | 9,3 | Afastamentos tipados c/ efeito real na folha (snapshot de regra), Consignações margem 14.131 (35/5/5 parametrizável). eSocial, ponto 671. ~220 testes |
| RH/SOCIAL | **Patrimônio** | Completo | No padrão | 9,0 | Inventário completo c/ `Conciliar()` físico×contábil + divergências tipadas. Painel de Frota. ~111 testes |
| RH/SOCIAL | **Saúde** | Completo | No padrão | 8,8 | CNES/Profissional agora agregados (eram GUIDs). Farmácia/lote-validade, SISREG, Vigilância, ASPS/FMS. ~119 testes / ~94 rotas |
| RH/SOCIAL | **Assistência Social** | Completo | No padrão | 8,7 | SUAS/CadÚnico, PBF, IGD, FMAS, RMA. **A-0 corrigido e blindado** (`CriterioElegibilidade.cs:78` guard contra teto 1/4 SM revogado). ~90 testes |
| PLATAFORMA | **Legislativo** (referência) | Completo | No padrão | 9,5 | Ciclo completo Proposição→Votação→Autógrafo→Sanção/Veto. 53 handlers, 158 .cs. SpecCode bate exato. Front 7,2k LOC + a11y |
| PLATAFORMA | **Protocolo** | Completo | No padrão | 9,0 | PAE (9.784) + Documentos/assinatura (14.063, hash, carimbo). Âncora anti-IDOR p/ Cidadão. 46 testes |
| PLATAFORMA | **Identidade** | Completo | No padrão (sem rules.md) | 9,0 | RBAC+ABAC, delegação c/ I4 enforced (`CalculadoraPermissoesEfetivas` + `ConcessaoNaoAutorizadaException`). 97 testes. **Zero rules.md** |
| PLATAFORMA | **Cofre** | Completo | No padrão (modo DEV) | 9,5 | Envelope encryption real DEK/KEK AES-256-GCM, AAD tenant+thumbprint, trilha imutável. **Cripto/ICP em modo DEV** (M10). Só 22 testes |
| PLATAFORMA | **PainelGestor** | Completo | No padrão (sem rules.md) | 8,5 | Read model BI por Integration Events, apurador LRF (% RCL), semáforo de mínimos. 17 testes. **Zero rules.md** |

---

## 3. ESTADO DOS 8 PILARES DE GOVERNANÇA

| # | Pilar | Status | Síntese |
|---|---|---|---|
| 1 | **ArchitectureTests / Isolamento de módulo** | ✅ VERDE | Zero referência cross-module a camada interna; toda dependência cruzada via `*.Contracts`. Domain↛Infra e Application↛Infra blindados. Portal Cidadão (WIP) feito certo (interfaces de Contracts). *Menor: Identidade/Cofre internos não trancados por fitness function, mas sem violação atual.* |
| 2 | **SpecCodeConsistency (rules.md ↔ código)** | ⚠️ AMARELO | 74 rules.md em 11 módulos, manifesto bidirecional travado no CI. **Buraco:** o teste só itera módulos que TÊM rules.md → **Identidade e PainelGestor têm comandos e ZERO rules.md** → fora da checagem de drift. Identidade (RBAC/multi-tenant) é o pior caso. |
| 3 | **Maintainability (<500 linhas)** | ✅ VERDE | 0 arquivos de produção >500 linhas. Únicos >500 são Migrations/Designer/Snapshot EF (auto-gerados, isentos). Nenhum god-file apesar de +200k linhas. |
| 4 | **Multi-tenant / GQF / IMustHaveTenant** | ✅ VERDE | GQF por reflexão sobre toda entidade `IMustHaveTenant`; deny-by-default = `1=0` (nunca `Guid.Empty`). ~70 entidades sem tenant são filhas de agregado (sem DbSet próprio) — modelagem DDD correta, sem vazamento. |
| 5 | **Auditoria hash-chain** | ✅ VERDE | Cadeia por tenant, leitura atômica do selo (UPDLOCK/HOLDLOCK), `AuditTrail` init-only imutável, índice único filtrado `(TenantId, Sequencia)`. Intacta e endurecida. |
| 6 | **Segurança — deny-by-default / permissões / LGPD** | ⚠️ AMARELO | `RequirePermission` em todos os 24 arquivos de endpoint; catálogo canônico em `Permissoes.cs`; trilha LGPD sensível + LG-1 fitness function + redação PII recursiva. **Ressalva:** sem `FallbackPolicy` global no host → deny-by-default é **convenção, não trava** (endpoint novo sem decorator nasce aberto). |
| 7 | **Design System (frontend)** | ✅ VERDE | 20 componentes-base, 204 usos de `var(--token)`, paleta centralizada, gov.br DS presente. Só 2 `.tsx` com hex literal (cosmético). |
| 8 | **Consistência de migração / snapshot EF** | 🔴 VERMELHO | **9 migrations hand-written sem `.Designer.cs`** (Financas 3, Saude 4, RH 1, Identidade 1) → ModelSnapshot nunca regenerado. `SaudeDbContextModelSnapshot` comprovadamente sem Vigilância/Agendamento/Farmácia/Imunização. Próximo `migrations add` gera diff fantasma → `MigrateAsync` quebra em prod. Nenhuma fitness function guarda snapshot↔modelo. |

**Resumo:** no padrão (5): 1, 3, 4, 5, 7. Em atenção (2): 2, 6. Fora do padrão (1): 8.

---

## 4. DRIFT & RISCOS SISTÊMICOS PRIORIZADOS

1. **🔴 P0 — Snapshots EF não-regenerados (risco de produção real).** 9 migrations escritas à mão sem Designer; `SaudeDbContextModelSnapshot` e `FinancasDbContextModelSnapshot` atrás do modelo. Próximo `migrations add` gera diff que tenta recriar tabelas existentes → `MigrateAsync` quebra no deploy. Ponto cego: nenhuma fitness function detecta. É o único achado **fora do padrão** e o vetor mais provável de falha em go-live.

2. **⚠️ P1 — SpecCodeConsistency cego para Identidade e PainelGestor.** Ambos têm `ICommand/IQuery` e **zero rules.md** → toda a superfície de comando/evento fica fora da checagem de drift. A rica máquina de autorização da Identidade (núcleo RBAC/multi-tenant) e o read model do PainelGestor não estão sob a fitness function spec↔código — buraco de **governança**, não de código.

3. **⚠️ P1 — Largura operacional: o "GUID digitado" como porta relacional (Onda 0/1 incompleta no cluster FISCAL).** Formulários front-end pedem GUID cru em vez de picker/autocomplete: `tributos/LancarItbiModal.tsx:123`, `ApurarCosipPage.tsx:113`, `EmitirAlvaraPage.tsx:124`, `administracao/contrato/ContratoFormModal.tsx:151/181/257`, `administracao/licitacao/LicitacaoFormModal.tsx:219`, +6 em Finanças. Domínio rico; o drift é de **largura de seleção operacional** (padrão transversal nº2 do plano). Esforço P/M.

4. **⚠️ P2 — Cofre em modo DEV + cobertura de testes assimétrica nas peças de maior risco.** A cripto/ICP-Brasil do Cofre está correta e isolada, mas roda com KEK de config (TODO Key Vault wrap/unwrap) e validação de cadeia não-estrita (CRL/OCSP TODO prod) — deferido ao M10, mas é dependência crítica (habilita eSocial, remessas TCE-RS, Protocolo 14.063) e **não pode escorregar**. Agrava: Cofre tem só **22 testes** para o componente mais sensível à segurança, e PainelGestor 17 (vs. RH ~220, Identidade 97) — faltam casos negativos (decifrar com AAD de outro tenant, material redigido na trilha, cert expirado/revogado).

5. **⚠️ P2 — Deny-by-default é convenção, não trava no host + Portal Público/Transparência sem SPA cidadão.** (a) Sem `FallbackPolicy`/`RequireAuthenticatedUser` global no `Program.cs`, a negação por padrão depende de cada endpoint declarar `RequirePermission` — hoje 100% disciplinado, frágil sob crescimento. (b) O Portal Público/e-SIC existe no back-end com rotas reais (`/publico/transparencia/{slug}`) mas o **SPA cidadão React não foi construído** (front só referencia as rotas como texto); é a maior distância nome×função do plano, reduzida a "falta a casca pública" (não mais 0%).

**Drift documental (transversal, não é risco de código mas pode enganar avaliações):** o `PLANO-PROFUNDIDADE.md` está desatualizado e contradiz o código — ainda descreve como pendentes itens já entregues (Aluno/Turma, Inventário, `PossuiVaga`). Recomenda-se marcá-lo como "foto histórica pré-Ondas", em linha com a própria diretriz de "ancorar no estado atual, nunca nas fotos pré-M0".

**Não-drift (corretamente deferido ao M10):** integrações oficiais via gateways `Simulado*` e marcadores `// TODO(validar-leiaute-*)` — concentrados em Transparência (51 marcadores TCE/SICONFI), Saúde (RNDS/SISAB/SISREG/CADSUS/HÓRUS), Assistência (CadÚnico), RH (eSocial). Operação local pronta; troca por integração real depende de cred/cert A1. **Atenção de rastreabilidade:** o risco de PoC oficial concentra-se em Transparência (layout exato SIAPC/MSC) e há um item fino — `CalendarioDiasUteisPadrao.cs:11 // TODO(parametrizar-feriados)` — que afeta o cálculo de prazo do e-SIC e dos mínimos (hoje sem feriados municipais por tenant).

---

## 5. RECOMENDAÇÕES — AGORA vs. W10.6 (M10)

### Endereçar AGORA (risco de produção / governança que escapa às fitness functions)
1. **(P0) Regenerar os 9 snapshots EF** — `dotnet ef migrations add` limpo por módulo (Financas, Saude, RH, Identidade) para realinhar ModelSnapshot↔modelo. **+ Adicionar fitness function** que falhe se `dotnet ef migrations has-pending-model-changes` — fecha o ponto cego permanentemente. É a única ação que evita quebra de `MigrateAsync` em go-live.
2. **(P1) Criar rules.md de Identidade e PainelGestor** (`Usuario.rules.md`/`Permissoes.rules.md`, `IndicadorMunicipio.rules.md`) — ou estender SpecCodeConsistency para exigir manifesto de todo módulo com `ICommand/IQuery`. Coloca o núcleo RBAC sob a trava de drift.
3. **(P2) Adicionar `FallbackPolicy` global no host** — converte deny-by-default de convenção em trava (endpoint novo sem `RequirePermission` nasce fechado). Mudança pequena, blindagem grande sob crescimento.

### Endereçar no W10.6 / M10 (polimento adversarial + validação oficial)
4. **(P2) Adensar testes negativos do Cofre** (proporcional ao risco): decifrar com AAD de outro tenant deve falhar, material redigido nunca na trilha, cert expirado/revogado bloqueia assinatura. Idem reforço de invariantes em Saúde (criticidade clínica/LGPD) e Assistência.
5. **(M10) Promover Cofre de DEV→PROD**: KEK no Key Vault (wrap/unwrap), validação ICP-Brasil estrita (CRL/OCSP). Dependência crítica — priorizar no início do M10.
6. **(M10) Integrações oficiais**: validar leiautes TCE-RS/SICONFI (SIAPC/PAD/MSC), trocar gateways `Simulado*` por reais com cred/cert A1, parametrizar feriados municipais por tenant (`CalendarioDiasUteisPadrao`).
7. **(M10) Construir o SPA cidadão** do Portal de Transparência/e-SIC sobre o back-end já pronto — fecha a maior distância nome×função.
8. **(W10.6) Cobrir a largura operacional** — substituir GUID digitado por picker/autocomplete nos formulários de Tributos/Administração/Finanças (Onda 0/1 residual).
9. **(Documental) Anotar o PLANO-PROFUNDIDADE.md** como foto histórica pré-Ondas, para não ancorar avaliações futuras no passado.

---

*Conclusão honesta para o dono:* a "constituição" (CLAUDE.md + fitness functions) segurou bem o crescimento rápido — domínio rico de verdade, no padrão Tributos, sem god-files, multi-tenant e auditoria endurecidos. O ponto cego único e crítico é a **disciplina de EF snapshots** (sem fitness function que a guarde) — corrigível em horas e a ação mais importante antes do M10. O restante é largura operacional, casca pública e validação oficial — todos conhecidos, dimensionados e no caminho do plano.

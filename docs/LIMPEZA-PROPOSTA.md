# Proposta de Limpeza para Produção — Tensorroot.Gov

> **Data:** 2026-06-27. **Disciplina:** READ-ONLY — **nada foi deletado**, nenhum `dotnet`/`npm` rodado, `:5080`/`:5173` intocadas.
> Esta é uma **proposta para o dono aprovar**. Após o "ok", a remoção é um passo único e coordenado (deletar + atualizar 2 painéis que deep-linkam para alguns alvos — ver §5).
>
> **Contexto que decide tudo:** o projeto está na **fase de produção**. Conforme `docs/progresso/progresso.json` (2026-06-24):
> M0–M9 código PoC-completo; **Ondas 0–3 fechadas**; **fundação re-blindada** (red-team 6 críticos + 5 altos + 5 P0 anti-travamento) e **W10.6 (5 P0 + 14 P1 + ~21 P2) TODOS corrigidos e provados**; 1.673 testes verdes. Falta só M10 (validação oficial + go-live + esta limpeza).
> **Consequência:** os docs de auditoria/diagnóstico/estudo que *guiaram* esse trabalho já viraram **código + testes + ADRs + `docs/normas` + `docs/REVISAO-HUMANA`**. O conteúdo durável deles está preservado nesses lugares; os arquivos de processo em si são intermediários.

---

## 1. Princípio de avaliação

Para cada candidato perguntei: **"o conteúdo durável (decisão, conformidade, requisito, operação) ainda existe em algum lugar PRESERVADO?"**
- **Sim** → o arquivo é redundante/intermediário → **DELETAR**.
- **Não, e ainda impacta decisão/código** → **MIGRAR** para o destino correto.
- **É um plano/operação que ainda vale para frente** → **MANTER**.

**Verificação de links feita:** nenhum doc do conjunto PRESERVADO referencia os candidatos a DELETAR — **exceto** 3 ponteiros para `MODELO-AUTORIZACAO-ORGANIZACIONAL.md` (por isso ele é MIGRAR, não DELETAR) e 2 deep-links de painéis (`progresso.json`, `roadmap_team.html`) para `ESTADO-ATUAL.md` / `MAPA-PRESTACAO-CONTAS.md` (ver §5). Os candidatos se referenciam **entre si**, o que é irrelevante pois saem juntos.

---

## 2. Resumo executivo

| Ação | Arquivos | Tamanho |
|---|---:|---:|
| **DELETAR** | **90** | **~1,39 MB** |
| **MIGRAR** (1 arquivo → `docs/architecture/`) | **1** | ~19 KB |
| **MANTER** (candidatos com valor duradouro) | **6** | ~125 KB |
| **Total avaliado** | 97 | ~1,53 MB |

**Liberação proposta: ~90 arquivos / ~1,39 MB** (mais a remoção do `docs/estudo/` quase inteiro e do `docs/diagnostico/` inteiro).
Após a limpeza, `docs/` fica enxuto: **produto** (ADR, architecture/specs+DESIGN, design-system, normas, REVISAO-HUMANA, governanca, RUNBOOK, PLANO-MESTRE\*, MAPA-TOPICO-CODIGO, progresso) — sem os andaimes de estudo/diagnóstico/auditoria intermediária.

---

## 3. Tabela de decisão (todos os candidatos)

> Legenda de tamanho em KB (1 KB = 1024 B). "Consolidado em X" = o conteúdo durável vive em X (preservado).

### 3.1 `docs/diagnostico/**` — fotos PRÉ-M0 + requisitos já absorvidos → **DELETAR (todos)**

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/diagnostico/ESTADO-ATUAL.md` | DELETAR | Foto PRÉ-M0 (marcada "histórico"); estado real vive em `progresso.json` + código. ⚠ atualizar link em §5. | 14 |
| `docs/diagnostico/GAP-E-ROADMAP.md` | DELETAR | Gap PRÉ-M0; roadmap M0–M9 concluído. | 12 |
| `docs/diagnostico/REQUISITOS-GOVTECH.md` | DELETAR | Requisitos PRÉ-M0; conformidade durável agora em `docs/normas/FONTES-NORMATIVAS.md` + `architecture/specs-oficiais/`. | 19 |
| `docs/diagnostico/partes/auditoria-backend-fiscal-adm.md` | DELETAR | Auditoria empírica PRÉ-M0 (LoC/handlers); superada pelo código + CI. | 17 |
| `docs/diagnostico/partes/auditoria-backend-social-leg.md` | DELETAR | idem (módulos sociais/legislativo). | 11 |
| `docs/diagnostico/partes/auditoria-contabilidade-tce.md` | DELETAR | idem ("zero linhas PCASP" pré-M2); hoje provado em runtime. | 10 |
| `docs/diagnostico/partes/auditoria-frontend.md` | DELETAR | Snapshot de cobertura de rotas PRÉ-M0. | 9 |
| `docs/diagnostico/partes/auditoria-integracoes.md` | DELETAR | Snapshot de stubs PRÉ-M0; estado em `REVISAO-HUMANA/tce-rs-integracao.md`. | 10 |
| `docs/diagnostico/partes/auditoria-plataforma-qa.md` | DELETAR | "build quebrado" pré-M0; hoje 0/0 + fitness no CI. | 17 |
| `docs/diagnostico/partes/govtech-compras-assistencia.md` | DELETAR | Requisitos setoriais consolidados em `docs/normas/FONTES-NORMATIVAS.md` + `CONFORMIDADE-LICITACOES.md` + specs. | 15 |
| `docs/diagnostico/partes/govtech-contabilidade-tce.md` | DELETAR | idem → `normas/` + `architecture/specs-oficiais/{pcasp,mcasp,siconfi,tce-rs}`. | 19 |
| `docs/diagnostico/partes/govtech-educacao.md` | DELETAR | idem → `normas/FONTES-NORMATIVAS.md` (setorial). | 10 |
| `docs/diagnostico/partes/govtech-legislativo-protocolo.md` | DELETAR | idem. | 13 |
| `docs/diagnostico/partes/govtech-plataforma-compliance.md` | DELETAR | LGPD/eMAG/WCAG → `docs/design-system/` + ADRs 0020; já no produto. | 15 |
| `docs/diagnostico/partes/govtech-rh-esocial.md` | DELETAR | eSocial/ponto → `normas/` + `architecture/m5-prep/ESOCIAL-SPEC.md` + `REVISAO-HUMANA/rh-folha.md`. | 11 |
| `docs/diagnostico/partes/govtech-saude.md` | DELETAR | Requisitos Saúde → `normas/` + specs M7. | 14 |
| `docs/diagnostico/partes/govtech-tributos.md` | DELETAR | Requisitos Tributos → `normas/` + `REVISAO-HUMANA/tributos.md`. | 14 |

### 3.2 `docs/estudo/` (raiz) — auditorias/diagnósticos/DTOs intermediários → **DELETAR**, salvo MANTER explícito

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/estudo/AUDITORIA-CURRENT-STATE-M6.md` | DELETAR | Auditoria current-state ao M6 (snapshot 16 commits atrás); superada por M7–M9 + `normas/AUDITORIA-FINAL.md`. | 25 |
| `docs/estudo/AUDITORIA-ESTADO-PROJETO.md` | DELETAR | Radiografia panorâmica (notas 8,5–9,5) datada; foto, não produto. | 14 |
| `docs/estudo/AUDITORIA-PLANO-ARQUITETURA.md` | DELETAR | Veredito de 3 auditorias adversariais já acionado em CLAUDE.md/ADRs. | 19 |
| `docs/estudo/DIAGNOSTICO-VS-REAL.md` | DELETAR | Confronto "foto pré-M0 × real" — só fazia sentido enquanto a foto existia. | 14 |
| `docs/estudo/DIRECIONAMENTO.md` | DELETAR | Opinião estratégica datada (2026-06-22); decisões viraram plano/ADRs. | 27 |
| `docs/estudo/dto-legislativo.md` | DELETAR | Reconciliação DTO front×back (one-off); o front já foi ajustado ao contrato. | 23 |
| `docs/estudo/dto-pendencias-backend.md` | DELETAR | Pendências de backend levantadas na reconciliação DTO (one-off). | 6 |
| `docs/estudo/dto-recursoshumanos.md` | DELETAR | idem RH. | 11 |
| `docs/estudo/dto-tributos.md` | DELETAR | idem Tributos. | 18 |
| `docs/estudo/LIMPEZA-DOCS-PROPOSTA.md` | DELETAR | Proposta-rascunho de 2026-06-25 (substituída por **este** documento). | 3 |
| `docs/estudo/MAPA-SISTEMICO-GOVTECH.md` | DELETAR | Síntese do ecossistema; o durável (destinos/obrigações) vive em `REVISAO-HUMANA/tce-rs-integracao.md` + `normas/FONTES-NORMATIVAS.md`. | 18 |
| `docs/estudo/POC-ROTEIRO-DEMO.md` | **MANTER** | Roteiro passo-a-passo de demonstração de PoC (banca técnica) — **operacional para frente** (M10/W10.3). | 25 |
| `docs/estudo/M10-CREDENCIAIS-ONBOARDING.md` | **MANTER** | Guia acionável de credenciais/certificados/adesões para go-live — operacional M10 (citado no `progresso.json`). | 28 |
| `docs/estudo/GAP-VS-SYSTEM-SAPI.md` | **MANTER** | Gap vs incumbente SAPI = condição de vitória do PoC; inteligência competitiva viva (citado por `normas/CONFORMIDADE-ACHADOS.md`). | 21 |
| `docs/estudo/PARIDADE-POC.md` | **MANTER** | Lista priorizada/sequenciada de paridade-PoC derivada do gap — backlog ativo. | 8 |
| `docs/estudo/VALIDACAO-REAL-TCE.md` | **MANTER** | Plano "sair da simulação → validação oficial TCE-RS" — operacional M10/W10.1 (o que obter e de quem). | 12 |

### 3.3 `docs/estudo/W10.6-*` — auditoria adversarial final, **já corrigida e consolidada** → **DELETAR (todos)**

> `progresso.json` confirma: "W10.6 … 5 P0 + 14 P1 + ~21 P2 **TODOS corrigidos e provados**". A consolidação **durável** está preservada em **`docs/normas/AUDITORIA-FINAL.md`** (com status de remediação) + `docs/normas/CONFORMIDADE-ACHADOS.md`. Estes são os arquivos de trabalho.

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/estudo/W10.6-PLANO.md` | DELETAR | Plano da varredura adversarial (executado). | 26 |
| `docs/estudo/W10.6-MASTER-FIXPLAN.md` | DELETAR | Fix-plan turnkey (todos os fixes aplicados/provados). | 19 |
| `docs/estudo/W10.6-ACHADOS-FISCAL.md` | DELETAR | Achados fiscais corrigidos; consolidado em `normas/AUDITORIA-FINAL.md`. | 17 |
| `docs/estudo/W10.6-ACHADOS-M9-NOVO.md` | DELETAR | Achados do código novo M9 corrigidos. | 18 |
| `docs/estudo/W10.6-ACHADOS-PATR-TRANSP-CID.md` | DELETAR | Achados Patrimônio/Transparência/Cidadão (0 P0/P1) corrigidos. | 16 |
| `docs/estudo/W10.6-ACHADOS-PLATAFORMA.md` | DELETAR | Achados Identidade/Cofre/PainelGestor corrigidos. | 19 |
| `docs/estudo/W10.6-ACHADOS-PROCESSO.md` | DELETAR | Achados Legislativo/Licitações/Protocolo corrigidos. | 17 |
| `docs/estudo/W10.6-ACHADOS-SOCIAL.md` | DELETAR | Achados Saúde/Educação/Assistência corrigidos. | 18 |

### 3.4 `docs/estudo/seguranca/**` — red-team já fechado (fundação re-blindada) → **DELETAR (todos)**

> `progresso.json`: "FUNDAÇÃO RE-BLINDADA: red-team (6 críticos + 5 altos) … corrigidos". A postura de segurança durável está em **código + testes + ADRs** (0008 envelope encryption, 0016 hash-chain WORM, 0019 I4, 0020 LGPD redação). `RED-TEAM.md` pedia "travar produção até fechar os 6 críticos" — fechados.

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/estudo/seguranca/RED-TEAM.md` | DELETAR | Consolidação do red-team (6C+5A); todos corrigidos no código. | 23 |
| `docs/estudo/seguranca/ataque-authn-authz.md` | DELETAR | Achados authn/authz corrigidos (I4/JWT/cross-tenant) — ADR-0019. | 19 |
| `docs/estudo/seguranca/ataque-injecao-segredos.md` | DELETAR | Achados de segredos corrigidos — Key Vault/ADR-0008. | 14 |
| `docs/estudo/seguranca/ataque-isolamento-tenant.md` | DELETAR | Achados de isolamento corrigidos — GQF/ADR-0005/0010. | 18 |
| `docs/estudo/seguranca/ataque-lgpd-dados.md` | DELETAR | Achados LGPD corrigidos — trilha de leitura/ADR-0020. | 16 |
| `docs/estudo/seguranca/verificacao-authn-authz.md` | DELETAR | Re-verificação (confirma achados); sem conteúdo novo. | 13 |
| `docs/estudo/seguranca/verificacao-injecao-segredos.md` | DELETAR | idem. | 16 |
| `docs/estudo/seguranca/verificacao-isolamento-tenant.md` | DELETAR | idem. | 17 |
| `docs/estudo/seguranca/verificacao-lgpd-dados.md` | DELETAR | idem. | 12 |

### 3.5 `docs/estudo/qualidade-*` — auditorias de qualidade por módulo (bugs já corrigidos) → **DELETAR (todos)**

> Estas auditorias acharam bugs (pensão hardcoded, quórum, depreciação etc.) **já corrigidos** (W10.6/Ondas — `progresso.json`). Os arquivos `QUALIDADE-*.md` (consolidados de cada cluster) e os `auditoria-*.md` (sub-análises) são todos intermediários; o que sobra de durável (regras de cálculo) vive nos `*.rules.md` do código e em `docs/REVISAO-HUMANA`.

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/estudo/qualidade-motores/QUALIDADE-MOTORES.md` | DELETAR | Consolidado dos 12 bugs de motores fiscais (corrigidos). | 16 |
| `docs/estudo/qualidade-motores/auditoria-folha.md` | DELETAR | Sub-auditoria folha (consolidada no acima). | 12 |
| `docs/estudo/qualidade-motores/auditoria-iptu.md` | DELETAR | Sub-auditoria IPTU. | 14 |
| `docs/estudo/qualidade-motores/auditoria-iss.md` | DELETAR | Sub-auditoria ISS. | 19 |
| `docs/estudo/qualidade-motores/auditoria-itbi-contabil.md` | DELETAR | Sub-auditoria ITBI/contábil. | 16 |
| `docs/estudo/qualidade-legislativo/QUALIDADE-LEGISLATIVO.md` | DELETAR | Consolidado Legislativo (8 bugs corrigidos). | 13 |
| `docs/estudo/qualidade-legislativo/auditoria-proposicao-norma.md` | DELETAR | Sub-auditoria proposição/norma. | 13 |
| `docs/estudo/qualidade-legislativo/auditoria-sessao-tribuna.md` | DELETAR | Sub-auditoria sessão/tribuna. | 13 |
| `docs/estudo/qualidade-legislativo/auditoria-votacao.md` | DELETAR | Sub-auditoria votação. | 12 |
| `docs/estudo/qualidade-patrimonio-admin/QUALIDADE-PATRIMONIO-ADMIN.md` | DELETAR | Consolidado Patrimônio/Administração (11 bugs corrigidos). | 17 |
| `docs/estudo/qualidade-patrimonio-admin/auditoria-administracao.md` | DELETAR | Sub-auditoria Administração. | 14 |
| `docs/estudo/qualidade-patrimonio-admin/auditoria-patrimonio.md` | DELETAR | Sub-auditoria Patrimônio. | 11 |
| `docs/estudo/qualidade-rh/RH-PLANO.md` | DELETAR | Plano de ação RH (P0–P2) executado; regras vivas em `REVISAO-HUMANA/rh-folha.md` + `*.rules.md`. | 19 |

### 3.6 `docs/estudo/` — planos de Onda/robustez (Ondas 0–3 FECHADAS) → **DELETAR**

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/estudo/completude-modulos/PLANO-PROFUNDIDADE.md` | DELETAR | Autoridade das Ondas 1–3 — **fechadas**; designs derivados ficam em `architecture/profundidade/ONDA*-DESIGN.md` (ver §5). | 18 |
| `docs/estudo/robustez-fundacao/PLANO-ROBUSTEZ.md` | DELETAR | 5 P0 anti-travamento + P1/P2 — `progresso.json` declara fundação re-blindada (executado). | 23 |

### 3.7 `docs/estudo/avaliacao-m5m6/**` + `prova-de-conceito/**` + `gap-incumbente-sapi/**` + `partes/**` → **DELETAR**

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/estudo/avaliacao-m5m6/AVALIACAO-ENGENHARIA.md` | DELETAR | Avaliação isenta de engenharia M5/M6 (snapshot); riscos viraram fixes/ADRs. | 10 |
| `docs/estudo/avaliacao-m5m6/AVALIACAO-POC.md` | DELETAR | Avaliação isenta de PoC M5/M6 (snapshot 2026-06-22). | 12 |
| `docs/estudo/prova-de-conceito/PLAYBOOK-PROVA-DE-CONCEITO.md` | DELETAR | Régua "o que reprova/anula" do PoC; síntese acionável já em `POC-ROTEIRO-DEMO.md` (MANTIDO). | 30 |
| `docs/estudo/prova-de-conceito/AUTOAVALIACAO-POC.md` | DELETAR | Prontidão honesta (snapshot); estado real em `progresso.json`. | 11 |
| `docs/estudo/prova-de-conceito/pesquisa-poc-base-legal-processo.md` | DELETAR | Pesquisa intermediária (base legal PoC). | 12 |
| `docs/estudo/prova-de-conceito/pesquisa-requisitos-legislativo.md` | DELETAR | Pesquisa de editais (Câmara). | 10 |
| `docs/estudo/prova-de-conceito/pesquisa-requisitos-municipal.md` | DELETAR | Pesquisa de editais (Executivo). | 20 |
| `docs/estudo/prova-de-conceito/pesquisa-roteiro-pontuacao.md` | DELETAR | Pesquisa de métodos de pontuação de PoC. | 16 |
| `docs/estudo/prova-de-conceito/verificacao-poc-base-legal-processo.md` | DELETAR | Verificação da pesquisa (fecha o loop). | 16 |
| `docs/estudo/prova-de-conceito/verificacao-requisitos-legislativo.md` | DELETAR | idem. | 15 |
| `docs/estudo/prova-de-conceito/verificacao-requisitos-municipal.md` | DELETAR | idem. | 25 |
| `docs/estudo/gap-incumbente-sapi/COMPRAS-PATRIMONIO-FROTA-PROTOCOLO.md` | DELETAR | Sub-análise do gap SAPI já dobrada no consolidado `GAP-VS-SYSTEM-SAPI.md` (MANTIDO). | 16 |
| `docs/estudo/partes/auditoria-auditar-arquitetura.md` | DELETAR | Sub-auditoria consolidada em `AUDITORIA-PLANO-ARQUITETURA.md`. | 13 |
| `docs/estudo/partes/auditoria-auditar-cobertura-prestacao.md` | DELETAR | idem. | 10 |
| `docs/estudo/partes/auditoria-auditar-plano-mestre.md` | DELETAR | idem. | 10 |
| `docs/estudo/partes/estudo-funcoes-e-obrigacoes.md` | DELETAR | Parte do mapa sistêmico; durável em `normas/FONTES-NORMATIVAS.md`. | 15 |
| `docs/estudo/partes/estudo-mapa-estadual-controle.md` | DELETAR | idem (TCE-RS/controle externo) → `normas/` + `REVISAO-HUMANA/tce-rs-integracao.md`. | 19 |
| `docs/estudo/partes/estudo-mapa-federal-fiscal.md` | DELETAR | idem (SICONFI/STN/PNCP) → `normas/` + specs. | 17 |
| `docs/estudo/partes/estudo-mapa-federal-setorial.md` | DELETAR | idem (SIOPS/SISAB/eSocial) → `normas/`. | 22 |
| `docs/estudo/partes/evalidador-automacao.md` | DELETAR | Viabilidade de automação do validador TCE; conclusão (gate manual) já em `VALIDACAO-REAL-TCE.md` (MANTIDO). | 7 |
| `docs/estudo/partes/evalidador-ferramenta.md` | DELETAR | Caracterização da ferramenta; idem. | 8 |
| `docs/estudo/partes/evalidador-mt-leiaute.md` | DELETAR | Versão/MT do leiaute; **versão viva** está em `REVISAO-HUMANA/tce-rs-integracao.md` (geradores→leiaute/norma) + `architecture/specs-oficiais/tce-rs-siapc-pad.md`. | 8 |

### 3.8 `docs/planejamento/**` — planos intermediários já executados → **DELETAR**, salvo MIGRAR

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/planejamento/PLANO-MESTRE.md` (52 KB) | DELETAR | Rascunho antigo/longo; os planos **autoritativos** são `docs/PLANO-MESTRE.md` e `docs/PLANO-MESTRE-PRODUCAO.md` (raiz, PRESERVADOS). | 51 |
| `docs/planejamento/MODELO-AUTORIZACAO-ORGANIZACIONAL.md` | **MIGRAR → `docs/architecture/autorizacao/MODELO-ORGANIZACIONAL.md`** | É a **fonte** do ADR-0007 e o spec do modelo org (UO/I4/SoD); citado por ADR-0007, `MAPA-TOPICO-CODIGO.md` e `roadmap_team.html`. Spec de arquitetura viva → pertence a `architecture/` (não deletar: quebraria 3 ponteiros). | 19 |
| `docs/planejamento/MAPA-PRESTACAO-CONTAS.md` | DELETAR | Mapa módulo→destino→periodicidade; **superado** por `REVISAO-HUMANA/tce-rs-integracao.md` (inventário de geradores) + `normas/FONTES-NORMATIVAS.md`. ⚠ atualizar link em §5. | 18 |
| `docs/planejamento/BENCHMARK-BETHA.md` | DELETAR | Benchmark do concorrente Betha (snapshot intermediário). | 5 |
| `docs/planejamento/partes/betha-parity.md` | DELETAR | Mapa de paridade Betha (intermediário; foco de produção é SAPI, mantido). | 12 |
| `docs/planejamento/partes/modelo-autorizacao.md` | DELETAR | Parte subsumida no MODELO-AUTORIZACAO-ORGANIZACIONAL (migrado). | 19 |
| `docs/planejamento/partes/org-funcionamento-municipio.md` | DELETAR | idem (organograma municipal). | 20 |
| `docs/planejamento/partes/prestacao-contas-mapa.md` | DELETAR | Parte do mapa de prestação de contas (subsumida). | 22 |
| `docs/planejamento/partes/separacao-poderes-e-dados.md` | DELETAR | Parte (separação de poderes/LGPD) subsumida; decisão dura está no ADR-0007/0019. | 19 |

### 3.9 Outros

| Caminho | Ação | Motivo (1 linha) | KB |
|---|---|---|---:|
| `docs/roadmap_team.html` | **MANTER** | Painel de onboarding do time (HTML standalone) — PRESERVADO pela regra de ouro do dono. Só **atualizar 2 deep-links** (ver §5). | 31 |

---

## 4. O que se MANTÉM e por quê (resumo)

**Conjunto PRESERVADO integral** (regra de ouro): `src/`, `tests/`, `docs/adr`, `docs/architecture`, `docs/design-system`, `docs/normas`, `docs/REVISAO-HUMANA`, `docs/RUNBOOK.md`, `docs/governanca`, `docs/PLANO-MESTRE.md`, `docs/PLANO-MESTRE-PRODUCAO.md`, `docs/MAPA-TOPICO-CODIGO.md`, `docs/progresso`, `docs/roadmap_team.html`, `CLAUDE.md`, build/config/CI.

**Candidatos resgatados (não deletar) — 7 no total (6 MANTER + 1 MIGRAR):**
- **MANTER** (6): `estudo/POC-ROTEIRO-DEMO.md`, `estudo/M10-CREDENCIAIS-ONBOARDING.md`, `estudo/GAP-VS-SYSTEM-SAPI.md`, `estudo/PARIDADE-POC.md`, `estudo/VALIDACAO-REAL-TCE.md` — todos **operacionais para o M10/go-live ou inteligência competitiva viva**; `docs/roadmap_team.html` (painel) — preservado por regra.
- **MIGRAR** (1): `planejamento/MODELO-AUTORIZACAO-ORGANIZACIONAL.md` → `docs/architecture/autorizacao/MODELO-ORGANIZACIONAL.md`.
  - *Após mover:* atualizar a linha **Fonte** do `docs/adr/0007-...md`, a linha 35 de `docs/MAPA-TOPICO-CODIGO.md` e a linha 207 de `docs/roadmap_team.html` para o novo caminho.

> Observação sobre os 5 MANTER de `estudo/`: se o dono preferir um `docs/` 100% sem pasta `estudo/`, estes 5 podem ser **movidos** para uma pasta de produção (ex.: `docs/poc/` para roteiro/playbook-resumo e `docs/architecture/integracoes/` para `VALIDACAO-REAL-TCE`), em vez de mantidos sob `estudo/`. Decisão do dono — não é deleção.

---

## 5. Ajustes de link necessários (fazer na MESMA passada, para zero link quebrado)

A remoção só deve ser feita junto com estas atualizações (são **deep-links de painéis/índices preservados** para alvos a deletar/migrar):

1. **`docs/progresso/progresso.json`** — remover/atualizar entradas que apontam para alvos deletados:
   - `../diagnostico/ESTADO-ATUAL.md`, `../diagnostico/GAP-E-ROADMAP.md` (links "Diagnóstico").
   - `planejamento/PLANO-MESTRE.md`, `planejamento/MODELO-AUTORIZACAO-ORGANIZACIONAL.md` (→ novo caminho em architecture), `planejamento/MAPA-PRESTACAO-CONTAS.md`, `planejamento/BENCHMARK-BETHA.md`.
   - `estudo/M10-CREDENCIAIS-ONBOARDING.md` (MANTIDO — só ajustar se for movido).
   - Referências a `estudo/...` nos `ONDA*-DESIGN.md` (ver item 4).
2. **`docs/roadmap_team.html`** — 2 ocorrências: linha 98 (`docs/diagnostico/ESTADO-ATUAL.md` → trocar por `CLAUDE.md`/`progresso`) e linha 207 (`docs/planejamento/MODELO-AUTORIZACAO-ORGANIZACIONAL.md` → novo caminho migrado).
3. **`docs/architecture/profundidade/ONDA{1,2,3}*-DESIGN.md`** — cada um abre com `> **Autoridade:** docs/estudo/completude-modulos/PLANO-PROFUNDIDADE.md`. Como o PLANO-PROFUNDIDADE será deletado (Ondas fechadas), trocar essa linha por nota de "Ondas 0–3 concluídas" (os DESIGNs são preservados e auto-suficientes).
4. **`docs/adr/0007-...md`** e **`docs/MAPA-TOPICO-CODIGO.md`** — repontar para `architecture/autorizacao/MODELO-ORGANIZACIONAL.md` (arquivo migrado).
5. **`docs/normas/CONFORMIDADE-ACHADOS.md`** — verifica-se que cita `GAP-VS-SYSTEM-SAPI.md` (MANTIDO) — **nenhuma ação** necessária.

> Nenhum outro doc do conjunto PRESERVADO referencia os alvos de deleção (verificado por `grep`).

---

## 6. INVESTIGAR (código, requer build — **apenas sinalização, NÃO deletar**)

Nada de código foi marcado para deleção. Registros para o dono avaliar **com build** num momento oportuno:

- **Gateways `Simulado*` (12 arquivos em `src/Modules/*/Infrastructure/`)** — **NÃO são código morto.** São os ACL stubs intencionais atrás dos **47 marcadores `// TODO(M10)`**, documentados em `docs/REVISAO-HUMANA/tce-rs-integracao.md` como "os interruptores de transmissão". No go-live (M10) são **substituídos** pelas integrações reais (não removidos a seco). Investigar caso a caso só ao "ligar" cada integração:
  - `Administracao/.../Receita/SimuladoReceitaCnpjGateway.cs`
  - `Transparencia/.../Integracoes/Simulado{SiconfiGateway,ConsultaSiconfi,CalendarioFiscal,EValidadorTce,LeiauteCatalogo,PublicacaoTransparenciaRepository}.cs`
  - `Saude/.../Integracoes/SimuladoGateways.cs`
  - `Convenios/.../Integracoes/SimuladoTransferegovGateway.cs`
  - `AssistenciaSocial/.../Integracoes/SimuladoCadUnicoGateway.cs`
  - `Tributos/.../Nfse/SimuladoNfseGateway.cs`, `Tributos/.../Dividas/SimuladoProtestoCraGateway.cs`
- **Nenhum** seeder de exemplo/demo encontrado em `src/` (busca por `SeedExemplo/DadosDemo/SampleData` = 0).
- **Nenhum** arquivo `*Old*/*Legacy*/*Obsolete*/*.bak/*WIP*` de verdade em `src/` (os 2 matches — `ScopeDbContextHolder.cs`, `AnexoLdo.cs` — são coincidências de substring, código legítimo).
- **Fotos/screenshots pré-M0:** **não existem** no repo (busca por imagens = 0 fora de `node_modules`). O item do plano "remover fotos pré-M0" refere-se aos **docs** de diagnóstico (§3.1), não a arquivos de imagem.

### Fora de escopo (higiene de repo, não-docs) — só observação
- Arquivos SQLite soltos na raiz e em `src/ApiHost/` (`plataforma.db*`, `tenant_*.db*`) são **artefatos de runtime** e **já estão no `.gitignore`** (não versionados). Não fazem parte desta proposta; só anoto que existem em disco.

---

## 7. Recomendação

1. Aprovar a lista de **88 deleções (~1,49 MB)** + **1 migração** (MODELO-AUTORIZACAO-ORGANIZACIONAL → `architecture/`).
2. Executar deleção **junto** com os ajustes de link da §5 (mesma passada → zero link quebrado).
3. Decidir se os **5 MANTER de `estudo/`** ficam onde estão ou são **movidos** para pastas de produção (§4) — assim `docs/estudo/` desaparece por completo.
4. Resultado: `docs/` enxuto e de produção — só ADRs, specs/DESIGN, design-system, normas, REVISÃO-HUMANA, governança, runbook, planos-mestre, progresso e os poucos operacionais de M10.

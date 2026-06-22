# Tensorroot.Gov — Plano-Mestre (Rules-as-Code + 10 Sprints)

> Documento para **leitura e autorização**. Descreve **tudo que falta** até o produto completo,
> dividido em até **10 sprints**, sob o modelo de governança [Rules-as-Code](governanca/RULES-AS-CODE.md)
> (o engenheiro edita o `.rules.md`; a IA gera/corrige/auditа o código). Após sua autorização,
> cada sprint é executado por **workflows multiagente** com fan-out por módulo/entidade.

---

## 1. Onde estamos (análise de progresso)

| Eixo | Estado |
|---|---|
| Fundação (Fases 1–2) | ✅ SharedKernel, BuildingBlocks (auditoria/tenant/Outbox/IModule), ApiHost (Serilog/OTel/JWT/RateLimit/Swagger/ativação modular) — compila e sobe |
| Tributos | ✅ Domínio→App→Infra→IModule→endpoints + **NFS-e/ADN** (worker/Polly) + 2 testes |
| Finanças | ✅ Empenho (Lei 4.320) + IModule + teste |
| Cross-module | ✅ Integration Event via Contracts (Tributos→Finanças) provado |
| Fitness Functions | ✅ 4 regras NetArchTest verdes (isolamento de camadas/módulos) |
| CI/CD | ✅ `ci.yml`, `deploy.yml`, `infra/main.bicep` (App Service+SQL+Blob+Key Vault) |
| Migrations | ✅ SQL Server (Tributos, Finanças) |
| Frontend | ✅ Scaffold React+gov.br DS (builda) + tela Dívida Ativa |
| **Métricas** | 52 projetos .NET, **9 testes verdes**, **2 módulos ativos**, build 0/0 |

**Falta (resumo):** 9 dos 11 módulos especificados ainda vazios; módulos novos (Tier 2);
integrações gov reais (eSocial, TCE/SICONFI, RNDS, SI-PNI, EducaCenso, CadÚnico, PNCP, HÓRUS,
gov.br assinatura); plataforma SaaS de licenciamento por tenant; Outbox processor durável;
frontend completo; deploy Azure real; LGPD/segurança/observabilidade/performance/E2E completos;
e a **fábrica Rules-as-Code** que torna tudo isso mantenível por IA.

---

## 2. Princípio central — Rules-as-Code

Ver [`docs/governanca/RULES-AS-CODE.md`](governanca/RULES-AS-CODE.md). Em uma frase: **cada módulo e
cada agregado tem um `*.rules.md` normativo**; a IA gera/corrige/auditа o código e os testes a partir
dele; **mudança de regra/bug/feature ⇒ editar o `.md`**, nunca o código. Enforcement por
**fitness functions + spec-code consistency check + cobertura obrigatória**.

---

## 3. Catálogo comercial de módulos (cobertura 100%) e licenciamento por tenant

O sistema é vendido por **assinatura modular**: cada prefeitura/câmara licencia **qualquer
subconjunto** (só Legislativo; só Executivo; só Saúde; só Educação; só Esportes; tudo; etc.).

**Tier 1 — já especificados (READMEs prontos):** Administracao, Financas, Tributos,
RecursosHumanos, Patrimonio, Protocolo, Saude, Educacao, AssistenciaSocial, Legislativo, Transparencia.

**Tier 2 — a especificar (catálogo extensível p/ 100% das secretarias):** Esporte e Lazer
(ginásios/equipamentos/eventos), Mobilidade e Transporte (trânsito/transporte público), Obras e
Urbanismo (licenciamento de obras/posturas/alvará de construção), Meio Ambiente (licenciamento
ambiental), Cultura, Turismo, Agricultura/Desenvolvimento Rural, Habitação, Defesa Civil/Segurança,
Procuradoria/Jurídico, Ouvidoria, Planejamento.

**Plataforma de licenciamento (a construir — Sprint 2):** catálogo de módulos, assinatura por
tenant (`TenantModule` + planos), provisionamento (criar tenant + schemas + seeds), **gating** em
runtime (menu/endpoints só dos módulos licenciados), billing/uso, e garantias de isolamento de dados.

---

## 4. Os 10 Sprints

Cada sprint: **Objetivo · Entregáveis · Integrações gov · Frontend · Testes · DoD**. Todos partem de
`*.rules.md` e usam o harness de IA.

### Sprint 1 — Fábrica Rules-as-Code + Harness de IA + retrofit
- **Objetivo:** tornar o sistema mantenível por IA antes de escalar.
- **Entregáveis:** template `*.rules.md`; **Spec-Code Consistency Check** (analisador/teste que valida
  código ⇄ regras); workflows de IA (Gerador/Testador/Auditor/Corretor/Integrador/Revisor);
  **retrofit** de Tributos e Finanças para `*.rules.md` (provando o modelo no que já existe);
  convenções de nomes/pastas; geradores de scaffolding por entidade.
- **Testes:** o consistency check roda no CI e falha se código divergir das regras.
- **DoD:** editar um `.rules.md` de Tributos e a IA regenerar/ajustar código+testes, tudo verde.

### Sprint 2 — Plataforma SaaS multi-tenant (licenciamento + base operacional)
- **Objetivo:** vender/operar qualquer combinação de módulos com isolamento garantido.
- **Entregáveis:** catálogo de módulos; `TenantModule`/planos; onboarding/provisionamento de tenant
  (schemas, migrations por módulo, seeds, certificados A1 no Key Vault); **middleware de gating** por
  tenant; **Outbox processor durável** (background) + retry/idempotência; agendador de workers;
  observabilidade (dashboards OTel, health detalhado), correlação por tenant; rate-limit por tenant.
- **Integrações:** Azure Key Vault (A1 por tenant), Blob (anexos).
- **Frontend:** seletor/menu dinâmico por módulos licenciados; tela de administração de tenant.
- **DoD:** provisionar 2 tenants com módulos distintos; um não enxerga nada do outro (E2E).

### Sprint 3 — Administração + Patrimônio
- **Módulos:** Administracao (Compras/Licitações/Contratos/Aditivos/Fornecedores) e Patrimonio
  (Bens/Depreciação/Almoxarifado/Frota).
- **Integrações:** **PNCP** (publicação de editais/contratos), SICAF/Compras.gov, Receita (CNPJ);
  depreciação MCASP; integração contábil Patrimônio→Finanças (Integration Events).
- **Frontend:** telas de licitação/contrato/fornecedor; patrimônio/frota/almoxarifado.
- **DoD:** fluxo licitação→contrato→empenho (cross-module) + inventário/depreciação, tudo verde.

### Sprint 4 — Recursos Humanos + eSocial
- **Módulo:** RecursosHumanos (Servidores/Cargos/Folha/Ponto).
- **Integrações:** **eSocial** (eventos S-1000/1005/1010/1020/2200/2206/2230/1200/1202/1210/2299;
  XML assinado A1; ambientes restrito/produção; recibos), **DCTFWeb**, **REP/Portaria 671**.
- **Frontend:** admissão, folha (cálculo/competência), ponto, painel de eventos eSocial.
- **DoD:** admissão→folha→evento eSocial gerado e validado (mock do ambiente) + abate-teto, verde.

### Sprint 5 — Protocolo + Transparência
- **Protocolo (cross-cutting):** processo eletrônico, **assinatura ICP-Brasil/gov.br** (simples/
  avançada/qualificada — Lei 14.063), GED (PDF/A), temporalidade (CONARQ), workflow BPMN; expõe
  serviços de protocolo/assinatura aos demais módulos via Contracts.
- **Transparência (consumidor):** **remessas TCE-RS (SIAPC/PAD)**, **SICONFI/MSC** (RREO/RGF/DCA),
  **dados abertos** (LAI, formato aberto), anonimização LGPD, e-SIC.
- **DoD:** assinar um documento (3 níveis) + gerar/validar uma remessa MSC + publicar dado aberto.

### Sprint 6 — Saúde I (Atenção, Regulação, Interoperabilidade) — *mini-sistema, parte 1*
- **Escopo:** UBS/estabelecimentos (**CNES**), cidadão (**CNS/CADSUS**), **PEP** (prontuário
  append-only + assinatura ICP-Brasil), **Regulação (SISREG)**, **RNDS (FHIR R4)**.
- **Integrações:** RNDS (Bundles FHIR, mTLS+A1), CADSUS (PIX/PDQ), CNES, e-SUS APS/SISAB.
- **DoD:** atendimento com CNS+CNES válidos, evolução SOAP assinada e imutável, envio RNDS (mock).

### Sprint 7 — Saúde II (Farmácia, Imunização, Telessaúde) — *parte 2*
- **Escopo:** Farmácia/dispensação (**HÓRUS/BNAFAR**, RENAME, controlados Port. 344/98, FEFO de lote),
  Imunização (**SI-PNI**, lote/validade/rede de frio 2–8 °C), Telessaúde (Lei 14.510/2022, CRM ativo).
- **DoD:** bloquear lote vencido (FEFO), exigir receituário de controlado, excursão térmica invalida lote.

### Sprint 8 — Educação (Gestão, Pedagógico, Logística) — *mini-sistema*
- **Escopo:** Escolas/Turmas/Alunos, **Matrícula** (situação), **Diário** (frequência ≥75%, notas,
  200 dias/800h), **Matriz Curricular/BNCC**, **Merenda/PNAE** (cardápio/RT/30% agricultura familiar),
  **Transporte/PNATE** (rotas/capacidade).
- **Integrações:** **EducaCenso/INEP** (leiaute), **FNDE** (FUNDEB/PNAE/PNATE), BNCC.
- **DoD:** matrícula→diário→aprovação/reprovação por frequência; geração do leiaute EducaCenso válido.

### Sprint 9 — Assistência Social + Legislativo + Módulos Tier 2
- **AssistenciaSocial:** SUAS (CRAS/CREAS/PAIF/PAEFI/SCFV), **CadÚnico** (NIS), benefícios (BPC/PBF/
  eventuais), Prontuário SUAS, RMA. LGPD reforçada (vulnerabilidade).
- **Legislativo:** Sessões, Proposições (tramitação/quórum/maiorias), Comissões, **Votação/Painel
  eletrônico** (tempo real), autógrafo→Executivo (Integration Event).
- **Tier 2 (primeiros):** Esporte e Lazer (ginásios/equipamentos/eventos/escolinhas) e Mobilidade/
  Transporte — provando que o catálogo escala via Rules-as-Code.
- **DoD:** concessão de BPC por critério; votação nominal com quórum; reserva de ginásio.

### Sprint 10 — Produção (Go-Live do piloto)
- **Frontend completo:** todas as telas dos módulos licenciáveis, gating por tenant, autenticação
  (gov.br/JWT), acessibilidade (eMAG/WCAG AA) auditada (axe/Lighthouse), Storybook.
- **Azure real:** IaC (Bicep) aplicado, deploy multi-tenant, Key Vault (A1), backups, DR.
- **Qualidade:** E2E (Playwright), testes de carga, **revisão de segurança** (OWASP/SQLi/segredos),
  **LGPD completo** (direitos do titular, anonimização, retenção), observabilidade/alertas.
- **DoD:** **piloto Maximiliano de Almeida/RS** no ar com seu conjunto de módulos licenciado, monitorado.

---

## 5. Trilhos transversais (em todos os sprints)

- **Segurança:** RBAC granular, segredos só em Key Vault, anti-SQLi, threat-model por módulo.
- **LGPD:** classificação de dados sensíveis por entidade (nas regras), minimização, trilha de acesso.
- **Auditoria:** Audit Trail imutável (já existe) + remessas ao TCE.
- **Performance/memória:** `Span<T>`/`Memory<T>` em XMLs pesados (eSocial/TCE/RNDS), paginação server-side.
- **Testes:** unitários (invariantes) + integração (BDD) + arquitetura (fitness) + **E2E** + carga.
- **Acessibilidade:** eMAG/WCAG AA com gate automatizado.

## 6. Definition of Done (global, por módulo)

`*.rules.md` completo · código gerado a partir dele · spec-code consistency verde · todas as
invariantes e cenários BDD testados · fitness functions verdes · migrations · integrações gov com
Polly+ACL+idempotência · telas gov.br DS acessíveis · documentação · 0 CVEs · revisão de segurança.

## 7. Como os workflows executarão (após autorização)

Por sprint, um workflow multiagente: **fan-out por entidade** (cada `*.rules.md` → Gerador→Testador
em paralelo) → **barreira** (build+testes) → **Auditor** (cobertura código⇄regras) → **Corretor** →
**Checker** (CI completo) → **síntese** (PR do módulo). Módulos independentes rodam em paralelo;
Saúde e Educação são fatiados em ondas (parte 1/parte 2) por serem mini-sistemas.

---

### Sequenciamento recomendado
`S1 (fábrica) → S2 (plataforma) → S3..S9 (módulos, alguns em paralelo) → S10 (produção)`.
S1 e S2 são **pré-requisitos** (destravam a produção em escala por IA). Saúde (S6+S7) e Educação (S8)
concentram o maior esforço. **Aguardando sua autorização para iniciar o Sprint 1.**

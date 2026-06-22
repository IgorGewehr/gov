# M9 — DESIGN pronto-para-implementar (Suprimentos avançados + Compliance + Robustez/QA)

> ARQUITETO do M9. Baseado em `pesquisa-*.md` + `verificacao-*.md` (m9-prep) e na inspeção do
> repo. **Disciplina §16:** cada ponto traz **CONFIANÇA**, **docs a obter** e **o que reusa**.
> Nada de número legal *hardcoded* — todo prazo/percentual é parâmetro por tenant com norma-fonte
> versionada (CLAUDE.md §7). Data: 2026-06-22.

---

## RESUMO EXECUTIVO (15 linhas)

1. **PNCP deve ser ANTECIPADO** — recomendação principal. Hoje `Contrato` empenha (move dinheiro) mas a publicação no PNCP é só um *flag* de domínio sem cliente real. Art. 94 da NLLC torna a divulgação **condição de eficácia**; sem ela o contrato é **ineficaz** (urgência: **nulo**) → glosa do TCE. É dívida de compliance ativa, não feature nova. **Antecipar para o início do M9 (frente bloqueante M9-A).**
2. **Correção legal obrigatória:** `Contrato.rules.md` atribui eficácia/prazos ao **art. 174** — está impreciso. Eficácia + prazos (20/10/25/45 d.u.) = **art. 94**; art. 174 = portal/rol. Corrigir `fontes_legais`, I-7/I-11 e textos.
3. **Citação errada:** "Decreto 11.462/2023 regula o PNCP" é falso (regula SRP). Gestão do PNCP = **Decreto 10.764/2021**. Corrigir.
4. **Relógio de prazo do art. 94** não existe no domínio: persistir `DataAssinatura`, calcular deadline em **dias úteis** + **calendário de feriados nacional+municipal por tenant**, e alertar prazo a vencer/vencido.
5. **Almoxarifado e Frota já têm rules + domínio** (ItemEstoque v1.0, Veiculo v2.0) → M9 implementa Application/Infra/testes e integrações, **não** reescreve.
6. **Obras** não existe: novo agregado em Patrimonio (ou submódulo) com medição/RDO e o gancho do art. 94 §3 (25/45 d.u.).
7. **Protocolo** já modela Hash SHA-256, `CarimboDeTempo` (VO sem cliente), 3 níveis de assinatura e imutabilidade → falta **cliente TSP RFC 3161 real (ACT ICP-Brasil)** e **motor de temporalidade/destinação (CONARQ/e-ARQ v2)**.
8. **Legislativo Norma** existe sem **URN LexML / export XML** → adicionar VO de URN + serializador.
9. **Convênios** é módulo novo: dois fluxos distintos (Transferegov/D11.531 entrada × MROSC/L13019 saída), state machines e prazos separados.
10. **Robustez/QA** endurece o que existe: Inbox idempotente, audit hash-chain HMAC + WORM, OTel com `tenant.id` nos 3 sinais, IntegrationTests (Testcontainers+WireMock) e gate WCAG 2.1 AA (axe-core).
11. **Versões a fixar antes de codar:** DOC-ICP-12 **v2.1** (Res. 188/2021), Portaria AN/MGI **174/2024** (revoga a 47), e-ARQ **v2** (Res. 50/2022), Res. CONARQ 37 de **19/12/2012**.
12. **Art. 29-A (Legislativo):** vigência diferida da EC 109/2021 (inativos só a partir da legislatura de 2025) → regra temporal por exercício, não constante.
13. Tudo atrás de **ACL + Polly + Outbox/Inbox**, idempotente, multi-tenant, auditado (CLAUDE.md §5/§8/§11).
14. **Ordem:** M9-A PNCP+relógio (bloqueante) → M9-B Suprimentos (almox/frota/obras) → M9-C Protocolo/Legislativo compliance → M9-D Convênios → M9-E Robustez/QA transversal.
15. **Pendências [a confirmar] que travam código:** provider do DB (lock do relay), schemas exatos da API PNCP v2.5, prazo de PC do convenente (D11.531), regra dos 20% (Portaria Conjunta 33/2023), metadados e-ARQ v2.

---

## FRENTE M9-A — PNCP (BLOQUEANTE: eficácia do contrato). CONFIANÇA: ALTA (jurídico) / MÉDIA (API)

### Por que antecipar
O sistema já dispara `ContratoAssinado → EmpenhoEmitido`. Empenhar não dá eficácia: **art. 94, caput** —
"divulgação no PNCP é condição indispensável para a eficácia do contrato e de seus aditamentos". Quem já
empenha sem publicar opera contratos ineficazes. **Logo o PNCP é o item mais urgente do M9** e deve abrir o marco.

### Correções no domínio existente (reuso direto)
- **Reusa:** `Contrato.cs`, `Contrato.rules.md`, `PublicarContratoNoPncp{Command,Handler}.cs`,
  `PublicarEditalNoPncp.cs`, `ContratoPublicadoPncpIntegrationEvent`, `AditivoCeleradoIntegrationEvent`,
  invariantes I-7/I-8/I-11, Outbox do contexto Administracao.
- **Corrigir (CONFIANÇA ALTA — CONFIRMADO na verificação):**
  - Trocar base legal de eficácia/prazos: **art. 174 → art. 94** em `fontes_legais`, I-7, I-11, linhas 17/29/58/94/144/148/376/406 de `Contrato.rules.md`. Manter art. 174 só como "portal/rol".
  - Trocar "Decreto 11.462/2023 (regulamenta o PNCP)" → **Decreto 10.764/2021** (gestão/CGRNCP). Reservar 11.462/2023 ao tema **SRP/ata de registro de preços**.

### Novo: relógio de prazo do art. 94 (CONFIANÇA ALTA — não existe hoje)
- Persistir `DataAssinatura` no `Contrato`; VO `PrazoPncp` que calcula deadline em **dias úteis** por modalidade:
  **20** (licitação, inc. I), **10** (contratação direta, inc. II), **obras 25** (§3, após assinatura) e **45**
  (§3, após conclusão). Todos como **parâmetro por tenant com norma-fonte**, nunca constante.
- **Calendário de feriados** (novo, transversal): serviço `ICalendarioDiasUteis` com feriados **nacionais +
  municipais por tenant** (parametrizável). Reusado também por Convênios e Protocolo.
- Eventos/alertas: `PrazoPncpAVencer` / `PrazoPncpVencido` (risco de ineficácia/nulidade) — alimentam Portal do Gestor (M8).

### Novo: cliente PNCP real (ACL) — CONFIANÇA MÉDIA (contrato de API a confirmar)
- ACL `IPncpGateway` em Administracao.Infrastructure: login `POST /v1/usuarios/login` → **JWT Bearer (~1h, renovar)**;
  bases `treina.pncp.gov.br/api/pncp` (homolog) e `pncp.gov.br/api/pncp` (prod).
- Resiliência **Polly** (timeout/retry/circuit breaker via `Microsoft.Extensions.Http.Resilience`), **idempotência**
  por chave da contratação, chamado pelo **handler do Outbox** (já é o padrão das rules). Credenciais **só no Key Vault** (§5).
- Pré-cadastro em ordem: **órgão → unidade administrativa → compra/edital → contrato → aditivo → arquivos**.
- Ampliar publicação além do contrato: **edital, ata de registro de preços (SRP), termo aditivo** (rol art. 174 §2º). PCA: decidir se entra no M9 ou em Planejamento de Compras [a confirmar escopo].
- **Docs a obter:** Manual de Integração PNCP **v2.5** (Jun/2026) + Swagger de manutenção/consulta (paths e JSON-schemas exatos por endpoint — **não extraídos**, PDF 433p não parseável); validar em `treina.pncp.gov.br` antes de codar. Texto literal do art. 94 no Planalto (host recusou conexão; validado via TCE-SP).

---

## FRENTE M9-B — SUPRIMENTOS (Almoxarifado / Frota / Obras)

### Almoxarifado — CONFIANÇA ALTA. Reuso: MÁXIMO
- **Reusa:** `ItemEstoque.rules.md` (v1.0) + `ItemEstoque.cs` (domínio já implementado): saldo não-negativo,
  PEPS/médio, despesa no consumo, menor entre custo e VRL, ponto de pedido, curva ABC, `PontoPedidoAtingidoIntegrationEvent`.
- **Falta no M9:** camada **Application** (handlers/validators dos comandos já especificados), **Infrastructure**
  (EF config/migrations no schema `patrimonio`), **testes** (12 cenários BDD já escritos no rules), e fechar o
  laço entrada-de-estoque consumindo `ContratoAssinadoIntegrationEvent` (recebimento → `RegistrarEntrada`).
- **Docs:** NBC TSP 12 / MCASP (já citados); sem pendência legal nova.

### Frota — CONFIANÇA ALTA. Reuso: MÁXIMO
- **Reusa:** `Veiculo.rules.md` (v2.0, composição com `BemPatrimonial` por `BemPatrimonialId`) + `Veiculo.cs`:
  abastecimento sob cota, OS, multas, licenciamento/IPVA, motorista/CNH, odômetro/horímetro monotônicos.
- **Falta no M9:** Application/Infra/testes; integração **gestor de combustível** (ACL+Polly) e **DETRAN/CTB**
  (consulta multas/licenciamento — **confirmar layout/versão atual**, §16) marcadas como previstas no rules.
- **Docs:** CTB Lei 9.503/1997 (já citado); layout DETRAN/gestor de combustível [a confirmar].

### Obras — CONFIANÇA MÉDIA. Reuso: PARCIAL (novo agregado)
- **Não existe rules nem código.** Criar agregado (Patrimonio ou submódulo de Administracao — **decidir**: a obra
  decorre de contrato da NLLC, mas o ativo resultante é patrimonial). Modelar **medições/boletim (RDO)**, cronograma
  físico-financeiro, e o gancho do **art. 94 §3** (quantitativos/preços em **25 d.u.** após assinatura; executados em
  **45 d.u.** após conclusão) → reusa o relógio de prazo de M9-A.
- **Reusa:** padrão de agregado por composição do `Veiculo` (vínculo a `BemPatrimonial`); Outbox/eventos de Patrimonio.
- **Docs:** Lei 14.133 art. 94 §3 (CONFIRMADO) + normas de medição de obra pública [a confirmar instrumento].

---

## FRENTE M9-C — COMPLIANCE DOCUMENTAL (Protocolo ICP/CONARQ + Legislativo LexML)

### Carimbo do tempo (ACT / RFC 3161) — CONFIANÇA ALTA (norma) / MÉDIA (integração)
- **Reusa:** `Documento.rules.md` + `Documento.cs` já têm `Hash` (SHA-256), VO `CarimboDeTempo`, PDF/A, imutabilidade.
- **Falta:** **cliente TSP real** (RFC 3161: TSQ→TST, falhas via `PKIFailureInfo`) contra uma **ACT credenciada
  ICP-Brasil**, atrás de ACL+Polly, persistindo o **TST** vinculado ao hash. Carimbo é **facultativo** na ICP-Brasil,
  mas é a prova de tempestividade do protocolo.
- **Docs a obter (FIXAR VERSÃO):** **DOC-ICP-12 v2.1** (Res. CG ICP-Brasil **188/2021** — a pesquisa citou v2.0/Res.172,
  desatualizado), DOC-ICP-11, RFC 3161/3628; definir **qual ACT** contratar e custo por carimbo [decisão comercial].

### Assinatura eletrônica (Lei 14.063/2020) — CONFIANÇA ALTA. Reuso: ALTO
- **Reusa:** `TipoAssinatura` (Simples/Avançada/Qualificada) + `NivelMinimoPorCriticidade` + I-5 já implementados;
  Key Vault/A1 do M2 para a qualificada (ICP-Brasil).
- **Refinar:** qualificada **obrigatória** para chefes de Poder e **para atos de transferência/registro de bens
  imóveis** (art. 5º §2º IV — relevante a Patrimonio/ITBI); nível mínimo configurável por tipo de ato (parametrização).
- **Docs:** Lei 14.063/2020 (numeração fina art. 4º/5º a reconferir no Planalto antes de exibir em UI); Decreto 10.543/2020 (referência federal, não vinculação municipal direta).

### GED + Temporalidade (CONARQ / e-ARQ Brasil v2) — CONFIANÇA ALTA (norma) / MÉDIA (modelo)
- **Falta:** **motor de temporalidade/destinação** — plano de classificação configurável, captura com **metadados
  arquivísticos**, cálculo de prazos de guarda (corrente/intermediária) e **destinação** (eliminação/guarda permanente),
  trilha de preservação/autenticidade (amarra com hash+assinatura+carimbo).
- **FIXAR VERSÃO:** **e-ARQ Brasil v2** (Res. CONARQ **50/2022**); autenticidade Res. CONARQ **37, de 19/12/2012**
  (a pesquisa escreveu 19/04 — corrigir); base de TTD federal mudou de Portaria 47/2020 (**revogada**) para **Portaria
  AN/MGI 174/2024**. **Docs a obter:** lista de **metadados obrigatórios do e-ARQ v2** (~225, extrair do PDF — pendente);
  TTD de atividades-fim do RS/município [a confirmar].

### Legislativo — LexML (URN + XML) — CONFIANÇA ALTA. Reuso: ALTO
- **Reusa:** `Norma.cs`/`Norma.rules.md` já existem (sem URN/LexML).
- **Falta:** VO `UrnLexml` (identificador persistente, esfera `br;rs;<municipio>`) + **export XML LexML** (schema
  v1.0 RC1/2009 — **é a versão de referência atual**, confirmado; lineage Akoma Ntoso/Norme in Rete) + opcional consulta à API LexML.
- **Art. 29-A (limite de despesa da Câmara):** motor de cálculo com **regra temporal por exercício** — a inclusão de
  inativos/pensionistas (EC 109/2021) só vale **a partir da legislatura de 2025** (art. 7º EC 109); faixas 7/6/5/4,5/4/3,5%
  parametrizadas por população (tratar borda 100.000 como regra explícita). Integra PCASP (M2) + TCE-RS (M4).
- **Docs:** CF art. 29-A + EC 58/2009 + EC 109/2021 (texto literal §§ no Planalto antes de UI).

---

## FRENTE M9-D — CONVÊNIOS (Transferegov/D11.531 + MROSC/L13019). CONFIANÇA: ALTA (núcleo) / MÉDIA (prazos)

- **Módulo novo.** Dois fluxos **separados** (state machines e prazos distintos — não reusar a mesma máquina):
  - **(A) Convênios federais recebidos** (Transferegov, **Decreto 11.531/2023**; operacionalização **D11.271/2022**):
    proposta/plano → execução físico-financeira → **PC contínua** (inicia na 1ª parcela) → PC final → análise
    (**60 d** informatizado / **180 d** convencional, art. 21; saneamento 45 d).
  - **(B) Parcerias-saída com OSCs** (MROSC, **Lei 13.019/2014**): chamamento público (art. 24) ou dispensa/inexigibilidade
    (arts. 30/31/32) → Termo de Colaboração (art. 16) / Fomento (art. 17) / Acordo de Cooperação → Plano de Trabalho (art. 22)
    → PC final OSC **90+30 d** (art. 69) → análise **150 d** (art. 71) + **saneamento 45 d** (art. 70).
- **Reusa:** padrão de agregado+Outbox dos demais módulos; relógio de dias úteis / calendário de M9-A; ACL+Polly.
- **NÃO modelar como constante (risco crítico — §16):** regra dos **20%** de 1º desembolso (base PI 424/2016 **revogada**
  pela **Portaria Conjunta MGI/MF/CGU 33/2023**); gatilho **Selic 30º dia**; **"180 dias" do art. 30** (texto diz "prazo do
  instrumento original"). Todos parametrizados por tenant com norma-fonte.
- **Integração Transferegov:** API existe, mas a maioria é **restrita a órgãos integrados** (exige convênio de integração);
  começar pela **leitura de dados abertos DTPAR** (idempotente, ACL). **Docs:** D11.531 (prazo PC do convenente), Portaria
  Conjunta 33/2023, Swagger Transferegov.

---

## FRENTE M9-E — ROBUSTEZ / OTel / QA / WCAG (transversal). CONFIANÇA: ALTA

- **Inbox idempotente** (`event_id` por handler+tenant) ao lado do Outbox existente — garante at-least-once sem duplicar
  lançamento contábil. **Reusa:** Outbox/poison já testado (`OutboxPoisonTests`, `OutboxDispatchIsolationTests`).
- **Auditoria tamper-evident:** evoluir `AuditTrail` para **append-only com hash-chain HMAC** (chave no Key Vault),
  grant **INSERT-only**, e **arquivamento WORM** (Azure Immutable Blob, retenção). Amarra com temporalidade CONARQ (M9-C).
- **OTel multi-tenant:** Enricher injeta `tenant.id` nos **3 sinais**; middleware de **Baggage** no entrypoint (derivar do
  JWT, não confiar em header); **cardinalidade**: `tenant.id` só em traces/logs, `tenant.tier`/agregado em métricas;
  exportador OTLP→Azure Monitor (FASE 5). **Reusa:** OTel já wired em `ApiHost/Program.cs`.
- **Métricas de Outbox/DLQ** (idade do evento, taxa entrada×saída, profundidade DLQ).
- **Testes:** projeto `*.IntegrationTests` com **Testcontainers** (provider real) + **WireMock** (PNCP/Transferegov/ACT) +
  **WebApplicationFactory** + mock de auth; **Playwright E2E** dos fluxos críticos (licitação→contrato→PNCP→empenho).
- **Acessibilidade (base legal):** **eMAG** (Portaria SLTI/MP 3/2007) + **WCAG 2.1 AA** + **axe-core/Playwright** como
  **gate de CI** no `src/Web`; componentes acessíveis por construção. **Docs:** LBI 13.146/2015 (arts. 53/63) + ABNT NBR
  17225/17060; obrigatoriedade de VLibras municipal [a confirmar].
- **Pendência que trava 2 itens:** **provider do `ModuleDbContext`** (Postgres `FOR UPDATE SKIP LOCKED` vs SQL Server
  `ROWLOCK,UPDLOCK,READPAST`) define o lock do relay e o grant INSERT-only.

---

## ORDEM DE IMPLEMENTAÇÃO RECOMENDADA

1. **M9-A — PNCP + relógio de prazo + calendário de dias úteis (BLOQUEANTE).** Fecha a dívida de eficácia já ativa e
   entrega o calendário reusado por Convênios/Protocolo. Inclui as correções legais (art. 94, Dec. 10.764).
2. **M9-B — Suprimentos.** Almoxarifado e Frota primeiro (reuso máximo: só faltam Application/Infra/testes); **Obras** por
   último (agregado novo, depende do relógio de M9-A para o §3).
3. **M9-C — Compliance documental.** Cliente TSP/ACT + motor de temporalidade (Protocolo); URN/XML LexML + art. 29-A (Legislativo).
4. **M9-D — Convênios.** Módulo novo; consome o calendário de M9-A; dois fluxos separados.
5. **M9-E — Robustez/QA.** Transversal, em paralelo desde M9-A (Inbox/OTel beneficiam todas as frentes); WORM amarra com M9-C.

> **Pré-requisito de qualidade (todas as frentes):** specs BDD `.md` antes do código (§1); NetArchTest preserva
> isolamento; toda I/O externa idempotente atrás de ACL+Polly+Outbox/Inbox; multi-tenant + auditoria imutável.

---

## CONSOLIDADO [a confirmar] que TRAVA implementação (obter antes de codar a frente)

| # | Item | Trava | Fonte a obter |
|---|---|---|---|
| 1 | Paths/JSON-schemas da API PNCP **v2.5** | M9-A cliente | Manual v2.5 + Swagger treina/prod |
| 2 | Provider real do `ModuleDbContext` | M9-E lock/grant | repo/infra |
| 3 | Prazo de PC do convenente + 20% (Port. Conjunta 33/2023) | M9-D | D11.531 + Portaria 33/2023 |
| 4 | "180 dias" art. 30 L13019 (ou "prazo original") | M9-D | L13019 art. 30 (Planalto) |
| 5 | Metadados obrigatórios e-ARQ v2 (~225) | M9-C GED | PDF EARQV203MAI2022 |
| 6 | TTD atividades-fim RS/município | M9-C GED | Arquivo Público RS |
| 7 | ACT a contratar + custo/carimbo | M9-C TSP | decisão comercial |
| 8 | Escopo M9 do PCA/edital/ata no PNCP | M9-A | dono do produto |
| 9 | Textos legais literais (Planalto recusou): art. 94, Lei 14.063 art.4º/5º, art. 29-A §§ | UI de M9-A/C | Planalto |
| 10 | Submódulo de Obras (Administracao × Patrimonio) | M9-B Obras | dono do produto |

---

## VERSÕES NORMATIVAS A FIXAR (corrigidas na verificação — usar estas)

- PNCP: eficácia/prazos = **art. 94**; portal/rol = **art. 174**; gestão = **Decreto 10.764/2021** (SRP = 11.462/2023).
- Carimbo do tempo: **DOC-ICP-12 v2.1** (Res. **188/2021**), não v2.0/Res.172.
- GED: **e-ARQ Brasil v2** (Res. **50/2022**); autenticidade Res. **37 de 19/12/2012**; TTD-base **Portaria AN/MGI 174/2024** (47/2020 revogada).
- LexML: **XML Schema v1.0 RC1 (2009)** é a referência atual.
- Art. 29-A: **EC 109/2021 art. 7º** — inativos/pensionistas no teto só a partir da **legislatura de 2025**.

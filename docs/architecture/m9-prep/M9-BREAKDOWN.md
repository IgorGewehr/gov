# M9 — BREAKDOWN REAL (o mapa do que falta de verdade)

> ARQUITETO. Mapa **ancorado no código atual** (não nas fotos pré-M0). Confirma o que **já foi
> entregue em ondas anteriores** (marca ✅) e isola **o M9 que realmente resta**. Sub-ondas seguem
> a numeração oficial do `PLANO-MESTRE.md` (**W9.1–W9.10**). Cada FALTA traz: entidades/escopo,
> reuso, esforço (P/M/G), o que depende de credencial (defere ao M10) e a posição na sequência.
> Disciplina CLAUDE.md: domínio rico, cross-module só via `*.Contracts`, integrações atrás de
> ACL+Polly+Outbox, multi-tenant + auditoria imutável. Data: 2026-06-23. READ-ONLY (sem build/run).

---

## 1) JÁ FEITO — confirmado no código (não relistar como pendente)

✅ **Almoxarifado / Requisição (Onda 3b)** — `Patrimonio.Domain/Estoque` (`ItemEstoque`, `Lote`, `MovimentoEstoque`, `Requisicao`) + `Domain/Requisicoes` (`PedidoRequisicao`, `ItemPedido`) + Application completo (`Estoque/*`: CadastrarItemEstoque, RegistrarEntrada, AtenderRequisicao, ReclassificarAbc, AjustarVRL, ListarAbaixoDoPontoPedido…; `Requisicoes/*`: Abrir/Aprovar/Atender/Cancelar/Obter/Buscar) + Infra (`ItemEstoqueConfiguration`, `PedidoRequisicaoConfiguration`, migration `RequisicaoAlmoxarifado`) + `rules/ItemEstoque.rules.md` e `PedidoRequisicao.rules.md`. **Laço entrada-de-estoque já fechado:** `ReceberContratoAssinadoHandler` consome `ContratoAssinadoIntegrationEvent` (cross-module via Contracts).

✅ **Frota — painel + ciclo (Onda 3a)** — `Patrimonio.Domain/Frota` (`Veiculo` composto com `BemPatrimonial`) + Application completo (`Frota/*`: TombarVeiculo, IncorporarVeiculo, RegistrarAbastecimento, AbrirOrdemServico/ConcluirManutencao, RegistrarMulta, RegistrarLicenciamento, DesignarMotorista, ObterPainelFrota, ObterCustoPorVeiculo, ListarCnhVencendo/LicenciamentosPendentes/MultasPendentes/ManutencoesAbertas) + Infra (`VeiculoConfiguration`, repos) + `rules/Veiculo.rules.md`.

✅ **Licitações / Contratos / Aditivos — Lei 14.133 (Administração, M9 original já em código)** — `Administracao.Domain/Licitacoes` (Licitacao, Proposta, Habilitacao, Lote, Recurso) + `Domain/Contratos` (Contrato, Aditivo, Apostilamento, Garantia, EmpenhoRef) + `Domain/Fornecedores` (Fornecedor, Sancao) + Application completo (abrir/julgar/homologar/anular/revogar licitação; celebrar/iniciar/rescindir/encerrar contrato; aditivo; apostilamento; sanção/SICAF) + `ContratoAssinado`/`LicitacaoHomologada`/`AditivoCelebrado`/`FornecedorSancionado` IntegrationEvents + `rules/Licitacao|Contrato|Fornecedor.rules.md`.

✅ **Robustez da fundação (hardening P0)** — **Outbox resiliente** com `AttemptCount` + `NextAttemptUtc` (backoff) + `DeadLetteredOnUtc` (dead-letter, sem reprocesso eterno) em `SharedKernel/OutboxMessage` + índice de drenagem; **AuditTrail hash-chain** (`AuditHashChain`, `AuditSaveChangesInterceptor`, `IVerificadorTrilhaAuditoria`, `UltimoSeloReader`) com encadeamento por tenant + genesis; **WORM** via trigger `INSTEAD OF UPDATE/DELETE` no `SchemaProvisioner`; **RFC 7807** `ProblemDetails` + `ProblemDetailsExceptionHandler` (traceId sempre); **rate-limit** global (`AddRateLimiter`/PartitionedRateLimiter) no ApiHost; **OpenTelemetry** com tracing+metrics wired em `Program.cs`; **acessibilidade** parcial (`axe-core`/`vitest-axe` + ~7 provas `.a11y.test.tsx`).

✅ **Protocolo básico** — `Protocolo.Domain` (`Processo`, `Movimentacao`, `Despacho`, `Documento`) + VOs `CarimboDeTempo` e `Assinatura` (3 níveis Lei 14.063) + Hash/PDF-A/imutabilidade + Classificação/temporalidade **modeladas como conceito** (enums citam TTD/CONARQ) + migrations Inicial/OutboxResiliencia. `CarimboDeTempoLocalService` (relógio local — placeholder declarado da ACT real).

---

## 2) FALTA — o M9 de verdade que resta (por sub-onda oficial)

> Legenda esforço: **P** ≤ ~2 dias · **M** ~3–6 dias · **G** > 1 semana. "Defere M10" = a entrega
> de **código/contrato/spec** cabe no M9; só a **transmissão/assinatura oficial real** espera credencial.

### W9.1 — PNCP: publicação BLOQUEANTE da eficácia do contrato 🟠 — esforço **G**
- **Estado real:** `PublicarContratoNoPncpCommand` apenas **recebe** um `NumeroContratoPncp` externo, seta flag no `Contrato` e publica `ContratoPublicadoPncpIntegrationEvent`. **Não há cliente HTTP, nem relógio de prazo do art. 94, nem bloqueio de execução financeira.** `ContratoAssinado → EmpenhoEmitido` dispara sem exigir nº PNCP → contratos **ineficazes** (art. 94 NLLC).
- **Entidades/escopo:** (a) ACL `IPncpGateway` em `Administracao.Infrastructure` (login JWT ~1h, pré-cadastro órgão→unidade→compra/edital→contrato→aditivo→arquivos; Polly + idempotência por chave; chamado pelo handler do Outbox); (b) VO `PrazoPncp` + `DataAssinatura` no Contrato (deadlines 20/10 d.u. e obras 25/45 §3, **parâmetro por tenant**); (c) serviço transversal `ICalendarioDiasUteis` (feriados nacionais+municipais por tenant); (d) **invariante de bloqueio**: contrato sem nº de controle PNCP **não empenha**; (e) eventos `PrazoPncpAVencer`/`PrazoPncpVencido` → Portal do Gestor. **Correções legais:** eficácia/prazos `art. 174 → art. 94`; gestão PNCP `Dec. 11.462/2023 → Dec. 10.764/2021` (11.462 = SRP).
- **Reuso:** Contrato/rules/handlers/eventos existentes; padrão Outbox+Polly; **calendário já existe em Transparencia/e-SIC** (`ICalendarioDiasUteis`) — **promover para serviço transversal compartilhado**, não duplicar.
- **Defere M10:** transmissão real ao PNCP de produção (credenciais/JWT, base `pncp.gov.br`) e validação contra `treina.pncp.gov.br`. **No M9:** ACL + invariante de bloqueio + relógio + correções legais, testado contra **WireMock** com schemas do Manual v2.5.

### W9.3 (parte Obras) — OBRAS / ObrasPro 🟢 — esforço **G**
- **Estado real:** **inexistente** (nenhum agregado de obra; só `Melhoria`/CTN em Tributos, não relacionado). Frota já feita (ver ✅).
- **Entidades/escopo:** novo agregado `Obra` (medição/boletim RDO, cronograma físico-financeiro, fiscalização), vínculo a `Contrato` (NLLC) e ao ativo `BemPatrimonial`; gancho do **art. 94 §3** (25 d.u. após assinatura / 45 d.u. após conclusão) reusando o relógio de W9.1; **remessa SICOE** (obras) ao TCE-RS `[OFICIAL]`.
- **Reuso:** padrão de composição do `Veiculo` (`BemPatrimonialId`); Outbox/eventos de Patrimonio; relógio de W9.1; pipeline de remessa TCE do M4 (Transparencia/RemessaTce).
- **Defere M10:** transmissão real do SICOE ao TCE-RS (cert A1/endpoint). **No M9:** agregado + medição/RDO + geração do arquivo SICOE + relógio §3. **Decisão de produto pendente:** submódulo em Administração × Patrimonio.

### W9.4 — PROTOCOLO avançado: carimbo ACT ICP-Brasil + GED/CONARQ 🟡 — esforço **M**
- **Estado real:** VO `CarimboDeTempo` + `CarimboDeTempoLocalService` (relógio **local**, declarado placeholder); assinatura 3 níveis modelada; temporalidade/TTD só como **enum/conceito**, sem motor.
- **Entidades/escopo:** (a) cliente **TSP RFC 3161 real** (TSQ→TST, `PKIFailureInfo`) contra **ACT credenciada ICP-Brasil**, atrás de ACL+Polly, persistindo o TST vinculado ao hash (substitui o local); (b) **motor de temporalidade/destinação** (e-ARQ v2 / CONARQ): plano de classificação configurável, metadados arquivísticos, cálculo de prazos de guarda corrente/intermediária + destinação (eliminação/guarda permanente), amarrado a hash+assinatura+WORM; (c) corrigir race condition do sequencial NUP.
- **Reuso:** `Documento`/`CarimboDeTempo`/`Assinatura`/Hash/PDF-A; Key Vault/A1 do M2 (assinatura qualificada); WORM/hash-chain da fundação.
- **Defere M10:** **carimbo ACT real** depende de **ACT contratada + custo/carimbo** (decisão comercial) e cert. **No M9:** ACL TSP testável (WireMock), motor de temporalidade completo, fix do NUP. Fixar versões: DOC-ICP-12 **v2.1** (Res. 188/2021), e-ARQ **v2** (Res. 50/2022), Res. CONARQ **37/2012**, Portaria AN/MGI **174/2024**.

### W9.6 — CONVÊNIOS + Terceiro Setor (MROSC) + PC Transferegov.br 🟠 — esforço **G**
- **Estado real:** **módulo inexistente** (nenhum agregado convênio/MROSC/Transferegov; nenhuma referência a Selic de convênio).
- **Entidades/escopo:** **dois fluxos separados** (state machines/prazos distintos): **(A) Convênios federais recebidos** (Transferegov/Dec. 11.531/2023: proposta/plano → execução físico-financeira → PC contínua/final → análise 60/180 d, saneamento 45 d); **(B) Parcerias-saída OSC (MROSC Lei 13.019/2014)**: chamamento/dispensa → Termo Colaboração/Fomento/Cooperação → Plano de Trabalho → PC OSC 90+30 d → análise 150 d + saneamento 45 d. PC vinculada à execução orçamentária; gatilho de inadimplência (LRF → bloqueia novos repasses).
- **Reuso:** padrão agregado+Outbox; relógio/calendário de W9.1; ACL+Polly; execução orçamentária do M2/M3.
- **Defere M10:** PC real no **Transferegov.br** (integração restrita a órgãos integrados → começar por leitura de dados abertos DTPAR). **No M9:** os dois agregados + state machines + PC parcial/final modelada. **Não modelar como constante:** regra dos 20% (Portaria Conjunta 33/2023), Selic 30º dia, "180 d" do art. 30 — tudo parâmetro por tenant com norma-fonte.

### W9.5 — LEGISLATIVO: LexML + prestação art. 29-A 🟢 — esforço **M**
- **Estado real:** `Norma` existe **sem URN/LexML**; nenhum motor de teto art. 29-A.
- **Entidades/escopo:** (a) VO `UrnLexml` (esfera `br;rs;<municipio>`) + **export XML LexML** (Schema v1.0 RC1/2009) + PDF-A/dados abertos; (b) motor **art. 29-A** (limite de despesa da Câmara) com **regra temporal por exercício** — inativos/pensionistas no teto só a partir da legislatura de **2025** (EC 109/2021 art. 7º); faixas 7/6/5/4,5/4/3,5% por população (borda 100.000 explícita); integra PCASP (M2) + remessa TCE-RS (M4, tenant Câmara).
- **Reuso:** `Norma`/`Proposicao`; PCASP/duodécimo do M2; pipeline TCE do M4.
- **Defere M10:** transmissão da prestação 29-A ao TCE-RS (cert/endpoint). **No M9:** URN+export LexML + motor 29-A.

### W9.8 — QA E2E + acessibilidade WCAG 2.1 AA 🟡 — esforço **M**
- **Estado real:** **não há** projeto `*.IntegrationTests`, **não há** E2E HTTP sobre `WebApplicationFactory`, **não há** Playwright (só `axe-core`/`vitest-axe` em ~7 telas). Sem WireMock.
- **Entidades/escopo:** projeto `*.IntegrationTests` (Testcontainers + WireMock para PNCP/Transferegov/ACT/TSP + `WebApplicationFactory` + mock auth), incl. **isolamento cross-tenant nos 11 módulos**; cobertura medida no CI; **axe-core/WCAG 2.1 AA/eMAG em todas as rotas** (estender as ~7 provas existentes); E2E do fluxo crítico licitação→contrato→PNCP→empenho; gating `<Can>` e quebra de modais >600 linhas.
- **Reuso:** `OutboxPoisonTests`/`OutboxDispatchIsolationTests`; padrão `.a11y.test.tsx` já existente; WireMock servirá W9.1/W9.4/W9.6.
- **Defere M10:** nada de credencial — **transversal, todo no M9** (rede de proteção para validação oficial).

### Robustez residual (W9.7) — em grande parte ✅; resta **Inbox + OTel multi-tenant + banco-por-tenant**
- **Estado real:** Outbox dead-letter ✅, hash-chain ✅, WORM ✅, RFC7807 ✅, rate-limit ✅, OTel wired ✅. **Faltam:** (a) **Inbox idempotente** no consumidor (`event_id` por handler+tenant) — **não existe** (only Outbox); (b) **enrichers OTel multi-tenant** (`tenant.id`/`CorrelationId` nos 3 sinais + exportador OTLP→Azure Monitor + Baggage do JWT); (c) métricas de Outbox/DLQ; (d) convergência **banco-dedicado por tenant** (ADR-0005: migrations por módulo *por banco*, `MigrateAsync` em lote, rotação de connection string + invalidação de cache). Esforço **M**. **Defere M10:** exportador→Azure Monitor (infra/creds). **No M9:** Inbox + enrichers + fan-out de migrations.

> **W9.2 (Monitor DF-e)**, **W9.9 (SADIPEM/CDP)** e **W9.10 (conformidade SIAFIC)** constam do PLANO-MESTRE como sub-ondas oficiais de M9 mas **estão fora do recorte deste breakdown** (suprimentos/compliance/QA do enunciado): DF-e é integração fiscal (Tributos), SADIPEM/SIAFIC são Finanças/STN. Listados aqui apenas para rastreabilidade.

---

## SEQUÊNCIA RECOMENDADA (valor PoC × esforço × dependência de credencial)

1. **W9.8 — QA E2E + WCAG (M)** — primeiro: rede de proteção, **zero credencial**, blinda todas as ondas seguintes; cria o harness WireMock que W9.1/W9.4/W9.6 vão reusar.
2. **W9.1 — PNCP bloqueante + relógio + calendário transversal (G)** — maior valor PoC (fecha dívida de eficácia ativa) e **entrega o calendário/relógio que W9.3-Obras e W9.6 consomem**. Inclui correções legais (art. 94 / Dec. 10.764).
3. **Robustez residual — Inbox + OTel multi-tenant (M)** — em paralelo desde W9.1 (Inbox protege o consumo PNCP/Transferegov; OTel observa as integrações novas).
4. **W9.5 — Legislativo LexML + art. 29-A (M)** — independente, alto valor de transparência, sem credencial (export é local; só prestação 29-A toca TCE).
5. **W9.4 — Protocolo ACT/RFC3161 + temporalidade CONARQ (M)** — motor de temporalidade + ACL TSP testável; o carimbo real espera ACT (M10).
6. **W9.3-Obras — agregado Obras + RDO + SICOE (G)** — depende do relógio §3 de W9.1; transmissão SICOE defere M10.
7. **W9.6 — Convênios + MROSC (G)** — por último (módulo novo maior); consome calendário de W9.1; PC real no Transferegov defere M10.

---

## O QUE É M10 (deferido por credencial/decisão oficial — não bloqueia o M9)

- **W9.1:** transmissão real ao PNCP de produção (JWT/credenciais) + validação em `treina.pncp.gov.br`.
- **W9.3-Obras:** transmissão real do **SICOE** ao TCE-RS (cert A1/endpoint).
- **W9.4:** **carimbo de tempo ACT real** (depende de ACT credenciada contratada + custo/carimbo — decisão comercial) e assinatura qualificada em produção.
- **W9.5:** transmissão da prestação **art. 29-A** ao TCE-RS.
- **W9.6:** prestação de contas real no **Transferegov.br** (integração restrita a órgãos integrados).
- **Robustez:** exportador OTLP → **Azure Monitor** (infra/creds de produção).
- **Decisões de produto a confirmar antes de codar:** submódulo de Obras (Administração × Patrimonio); escopo de PCA/edital/ata no PNCP; metadados obrigatórios e-ARQ v2; TTD de atividades-fim RS/município; provider final do `ModuleDbContext` para lock/grant.

**Resumo:** o M9 de código é **W9.8 → W9.1 → (Inbox/OTel) → W9.5 → W9.4 → W9.3-Obras → W9.6**, todo entregável sem credencial real; o que toca **PNCP/SICOE/ACT/29-A/Transferegov/Azure Monitor** em produção é a fronteira oficial do **M10**.

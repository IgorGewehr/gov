# AUDITORIA — Plano-Mestre & Arquitetura (veredito consolidado)

> **Síntese honesta** das três auditorias adversariais
> (`partes/auditoria-auditar-plano-mestre.md`, `partes/auditoria-auditar-arquitetura.md`,
> `partes/auditoria-auditar-cobertura-prestacao.md`) cruzada com `PLANO-MESTRE.md`,
> `MAPA-PRESTACAO-CONTAS.md`, ADRs 0005/0007/0009/0010/0011/0013 e o board de execução (M0–M4
> `completed`, M5–M9 `pending`).
> Marcações `[a confirmar]` = depende de fonte oficial do exercício ou de medição em carga (CLAUDE.md §16).
> Data: 2026-06-22.

---

## Veredito resumido

A **arquitetura é sólida e bem fundamentada** — as decisões mais criticáveis (banco-por-tenant,
Outbox, ABAC/UO) estão justificadas e a contabilidade é **síncrona e transacional**, não
eventualmente consistente (mito do briefing refutado). A **espinha M0→M4 está bem ordenada** e a
meta nº1 do dono foi atingida. Mas há três frentes que erodem a eficiência do plano:

1. **O Plano-Mestre está dessincronizado da realidade** (C1): descreve M0–M4 — já PRONTOS e
   provados — como backlog futuro ("0 linhas", "destravar build"). Como guia de sequenciamento, hoje
   ele induz retrabalho; serve só de histórico.
2. **Dois critérios-de-pronto são factualmente inatingíveis** (C2): W4.2/W4.3 mandam "transmitir
   via SOAP/REST com protocolo", mas o ADR-0009 já provou que **não existe API de upload** no TCE-RS
   nem no SICONFI — o envio é ato humano com certificado pessoal.
3. **Lacunas de compliance que bloqueiam repasse** estão fora do plano: **EFD-Reinf+DCTFWeb**,
   **SADIPEM/CDP** e **encerramento de exercício** — falhas que só aparecem em jan/mar e travam a
   prestação anual ou o CAUC.

Os riscos arquiteturais reais não estão nas grandes decisões, mas na **operação em escala**: cache
de connection string sem invalidação, Outbox sem teto de tentativas (poison eterna) e despacho
serial, e autorização resolvida no banco por requisição com `sync-over-async`. Todos mitigáveis,
nenhum estrutural. **Veredito: plano eficiente na espinha, mas precisa re-sincronizar, corrigir o
mito da transmissão e fechar lacunas fiscais federais.**

**Caminho:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/estudo/AUDITORIA-PLANO-ARQUITETURA.md`

---

## 1. Achados consolidados por severidade

### CRÍTICO

| ID | Achado | Origem | Impacto |
|---|---|---|---|
| **K1** | **Plano-Mestre dessincronizado:** apresenta M0–M4 (prontos/provados, board = `completed`) como backlog ("0 linhas", "18 handlers órfãos", "destravar build"). Copia verbatim `ESTADO-ATUAL`/`GAP`, que são fotos pré-M0. | Plano C1 | Agente que pega o plano re-implementa contabilidade entregue ou trava "corrigindo" build verde. A fonte de sequenciamento contradiz o board de execução. |
| **K2** | **W4.2/W4.3 violam ADR-0009:** critério "transmite via SOAP/REST com protocolo" é FALSO por refutação factual — TCE-RS não tem WS de upload (PAD desktop + ICP pessoal); SICONFI só tem API de **consulta**, MSC é upload manual com e-CPF A3 pessoal. | Plano C2 + ADR-0009 | Critério-de-pronto **inatingível**; leva a construir cliente de envio inexistente ou declarar pronto algo que nunca transmite. Risco direto na meta nº1. |
| **K3** | **Encerramento de exercício sem workflow dedicado:** apuração de superávit/déficit, transposição de saldos, abertura do exercício seguinte e lançamentos de encerramento das contas de resultado estão pulverizados (menção solta em W2.4/W3.3/W4.5). | Plano C3 | Sem encerramento, a MSC de dezembro e a DCA anual nascem incorretas — falha que só aparece em jan/mar e **trava a prestação anual** (a parte mais escrutinada pelo TCE). |
| **K4** | **EFD-Reinf (R-2010/R-4020) + DCTFWeb ausentes:** o ente é tomador/retentor (INSS 11%, IRRF/CSRF sobre as NFS-e que já ingerimos via ADN). EFD-Reinf substituiu a DIRF (extinta 2024) e alimenta a DCTFWeb que confessa o débito. | Cobertura L1 | Mensal (dia 15); multa R$ 500–1.500/mês. Não está na tabela mestra do mapa; só `[a confirmar]` no eSocial. Recolhimento irregular. |
| **K5** | **SADIPEM/CDP (Cadastro da Dívida Pública) ausente:** atualização **anual obrigatória** de toda a dívida do ente (≠ Dívida Ativa do M6, que é a receber). | Cobertura L2 | Anual até 30/jan; não atualizar → **CAUC negativo → bloqueia transferências voluntárias e operações de crédito** (>4.000 municípios já bloqueados por isso). |
| **K6** | **Cache de connection string sem invalidação:** `TenantConnectionCache` singleton com `GetOrAdd` sem TTL nem caminho de invalidação. | Arquitetura C1 | Rotação de segredo/failover/migração de banco → instância segue usando conexão antiga até reinício; instâncias divergem. Apontar para banco obsoleto que move verba pública = incidente sério. |

### ALTO

| ID | Achado | Origem | Mitigação curta |
|---|---|---|---|
| **H1** | **Outbox sem `AttemptCount`/dead-letter:** poison message reprocessa a cada 30s para sempre; uma antiga ocupa a cabeça do `Take(100)` e atrasa as válidas do módulo. | Arq. A3 | Adicionar `AttemptCount`+`NextAttemptUtc` (backoff), dead-letter após N tentativas, métrica/alerta. Baixo esforço, alta robustez. |
| **H2** | **Autorização por requisição via banco + `sync-over-async`:** `.GetAwaiter().GetResult()` no caminho quente de toda entidade `IMustHaveUnidade`; reconstrói a árvore de UOs por request; sem cache cross-request (admitido como adiado). | Arq. A1 | Tornar resolução async ponta-a-ponta; cachear `EscopoEfetivo` por (tenant,usuário) com invalidação na revogação de papel (I10). |
| **H3** | **Ausência total de cache na plataforma:** zero `IMemoryCache`/`HybridCache` em `src/`; módulos licenciados, escopo, roteiros contábeis e árvore de UOs lidos do banco a cada uso — cada roundtrip a um banco distinto. | Arq. A2 | Camada de cache (memória + invalidação por evento) p/ licenças, escopo e roteiros. |
| **H4** | **Despacho de Outbox serial:** ciclo ~`O(N_tenants × M_módulos × RTT)`; pode estourar o intervalo de 30s com a base alvo, atrasando a propagação `MSCGeradaIntegrationEvent` → latência de prestação. | Arq. C2 | `Parallel.ForEachAsync` com concorrência limitada; pular tenants sem pendências (`EXISTS`); cachear descoberta de módulos. `[a confirmar]` ponto de saturação. |
| **H5** | **`ScopeDbContextHolder` last-writer-wins:** dois `ModuleDbContext` no mesmo escopo → só o último confirma, mutações do primeiro descartadas silenciosamente; o próprio `OutboxBackgroundService` já contorna manualmente (escopo por módulo). | Arq. A4 + ADR-0010 | `ModuleUnitOfWork` deve **falhar alto** no 2º `Definir` divergente; teste de arquitetura forçando multi-contexto. |
| **H6** | **W9.7 ainda diz "schema isolado":** ADR-0005 trocou para **banco dedicado por tenant**; o problema real é provisionamento + fan-out de migrations por banco (`SchemaProvisioner`), não "schema isolado". | Plano A1 | Reescrever W9.7 p/ banco-dedicado (migrations por módulo *por banco*, paridade dev/prod). |
| **H7** | **Workflows subdimensionados como linha única:** W2.3+W2.4 (PCASP) eram "semanas vendidas como uma linha" (lição já vivida); **W5.2 (eSocial S-1.3, 8 eventos+SOAP+XSD)** e **W6.2 (motor IPTU+DAM FEBRABAN+carnê)** repetem o erro. | Plano A2 | Quebrar W5.2 em tabelas/periódicos/admissão; quebrar W6.2 em apuração e emissão/arrecadação. |
| **H8** | **Tesouraria/caixa tarde (M4):** pagamento (W2.2) sem conta bancária/caixa modelados gera lançamento que não concilia; conciliação CNAB é insumo do Balanço Financeiro/DFC (M3). | Plano A4 | Subir núcleo de contas bancárias + movimento de caixa p/ M2; deixar só conciliação CNAB 240 em M4. |
| **H9** | **CADPREV/SIOPS não viram workflow próprio:** SIOPS (Saúde, mín. 15% ASPS) embutido em W7.2 com 4 outras coisas; CADPREV/DRPPS (RPPS) ausente; Educacenso 2ª etapa e PNATE anual diluídos. | Plano A3 + Cob. L9 | SIOPS workflow próprio em M7; CADPREV explícito em M5 **se o ente tiver RPPS** (`[a confirmar]`: Maximiliano é RPPS ou RGPS — se RGPS, vira excesso e sai). |
| **H10** | **Transferegov.br ausente:** prestação de contas de convênios/repasses federais (LRF); convênio sem PC aprovada → inadimplência → sem novos repasses. W9.6 é só "paridade Betha", desligado do destino federal. | Cobertura L3 | Ligar W9.6 ao destino Transferegov (PC final + parciais por convênio). |

### MÉDIO

| ID | Achado | Origem |
|---|---|---|
| **M-1** | **Conformidade SIAFIC (Dec. 10.540/2020) não nomeada:** nosso ERP **É o SIAFIC** do ente — há autodeclaração (XML nº 1 nas contas anuais) e requisitos técnicos (base única, trilha, sem 2º SIAFIC) que o próprio sistema deve cumprir/comprovar. | Cobertura L5 |
| **M-2** | **DCTFWeb/MIT como destino próprio** (consolida eSocial+EFD-Reinf): fechamento fiscal (DAE/DCTFWeb) é destino separado, hoje só `[a confirmar]`. | Cobertura L4 |
| **M-3** | **Filtro de UO gera `IN(@p0..@pN)` de tamanho variável** → 1 plano por cardinalidade, pressão no plan cache de entes grandes; injeta `Expression.Constant(context)`. `[a confirmar]` impacto depende do SGBD de prod (DEV é SQLite). | Arq. M1 |
| **M-4** | **Custo operacional do banco-por-tenant subestimado no código** (decisão correta): migrations/monitoração/fan-out por banco já aparecem como gargalo prático (C2/A2). Orçar pooling, janela de migration, teto de tenants/instância. | Arq. M2 |
| **M-5** | **Redundância de fonte:** Plano duplica `ESTADO-ATUAL`/`GAP` em vez de referenciá-los → 3 docs divergem quando o código avança (causa-raiz de K1). | Plano M1 |
| **M-6** | **QA/a11y empurrado para o fim (W9.8 "depende de todos os marcos"):** antipadrão; isolamento cross-tenant só testado na Identidade, 0 testes a11y — risco cresce a cada módulo sem rede. | Plano M2 |
| **M-7** | **W1.6 (LGPD/sensibilidade) com dependência frágil:** módulos sensíveis (Tributos sigilo W6.5, Saúde/Assistência W7.x) herdam débito se os `[a confirmar]` jurídicos (base legal, DPO, subdelegação) não congelarem antes de M6/M7. | Plano M3 |
| **M-8** | **DEFIS/Simples (tomador), e-Financeira, FGTS Digital, MANAD:** provável **não** se aplicam à administração direta — listar para descartar formalmente, não deixar buraco silencioso. | Cobertura L6/L7/L8 |

### BAIXO

| ID | Achado | Origem |
|---|---|---|
| **B-1** | **PNCP bloqueante (art. 94) está em M9** (penúltimo), mas é condição de eficácia de contrato — bloqueia execução financeira. Para piloto que já empenha, é mais urgente que Portal do Cidadão (M8). | Plano B1 |
| **B-2** | **Excessos de escopo (over-engineering):** CNAB 400 legado + layouts por banco (BB/Caixa/Sicredi), XBRL-GL completo p/ MSC, `IConsultaSiconfi` com 7 endpoints — todos especulativos antes do MVP. | Cobertura E1/E2/E3 |
| **B-3** | **Carimbo de data e redação:** cabeçalho 2026-06-22 com conteúdo pré-M0; "reúso do transmissor" (W5.4↔W4.2) é de empacotamento/validação local, não de transmissão (ADR-0009). | Plano B2/B3 |

---

## 2. Reordenações/ajustes recomendados no Plano-Mestre

1. **Re-sincronizar com a realidade (K1, M-5):** adicionar coluna **Status (done/in-progress/pending)**
   por workflow; mover M0–M4 para bloco **"ENTREGUE (ver ADRs)"**; o Plano mantém só *o que fazer +
   sequência + status*, o diagnóstico fica referenciado (não copiado).
2. **Corrigir o mito da transmissão (K2):** reescrever W4.2/W4.3/W5.4 conforme ADR-0009 — escopo =
   **gerar artefato posicional correto + pré-validar RDI local + empacotar (ZIP+hash) + reconciliar
   via API de *consulta* + registrar recibo do ato humano**. Remover "transmite/POST/SOAP".
3. **Inserir W3.4 — Encerramento de exercício contábil (K3)** antes de W4.3/W4.5 (lançamentos de
   encerramento, apuração de resultado, transposição de saldos, abertura do exercício seguinte).
4. **Subir Tesouraria/caixa (H8)** para M2 (contas bancárias + movimento de caixa junto de W2.2/W2.4);
   deixar só conciliação CNAB 240 em M4.
5. **Adicionar destinos fiscais federais:** **EFD-Reinf+DCTFWeb (K4)** como workflow em M5
   (RH+Tributos, cruzando a retenção com a ingestão NFS-e/ADN); **SADIPEM/CDP (K5)** como destino STN
   alimentado pelo passivo PCASP; **Transferegov (H10)** ligado a W9.6.
6. **Promover CADPREV e SIOPS a workflows próprios (H9)** — confirmar antes RPPS vs RGPS do piloto.
7. **Antecipar PNCP bloqueante (B-1)** para logo após M4 (compliance que cresce; piloto já empenha).
8. **Quebrar workflows subdimensionados (H7):** W5.2 (eSocial) e W6.2 (IPTU) em 2–3 sub-workflows.
9. **QA/a11y transversal por workflow (M-6):** cada fullstack entrega seu E2E HTTP + axe-core; W9.8
   vira só consolidação/cobertura no CI.
10. **Atualizar W9.7 (H6)** para banco-dedicado, não "schema isolado".
11. **Enxugar excessos (B-2):** MVP = CNAB 240 + PIX + CSV + `/extrato_entregas`; CNAB 400, layouts
    por banco e XBRL-GL ficam **sob demanda/data-driven**.
12. **Nomear a obrigação SIAFIC (M-1)** no mapa + requisitos que o próprio sistema deve comprovar.

---

## 3. Riscos arquiteturais com mitigação

| Risco | Severidade | Mitigação |
|---|---|---|
| **Connection string obsoleta após rotação/failover** (K6) | Crítico | `IMemoryCache`/`HybridCache` com TTL curto + `Invalidar(tenantId)` chamado pelo fluxo de provisionamento/rotação. |
| **Poison message em loop quente eterno** (H1) | Alto | `AttemptCount`+`NextAttemptUtc` (backoff exponencial) + dead-letter + alerta; `OrderBy` filtrando `NextAttemptUtc <= now`. |
| **Thread-pool starvation no caminho de autorização** (H2) | Alto | Remover `.GetAwaiter().GetResult()`; resolver `EscopoEfetivo` async antes da consulta; cachear por (tenant,usuário) com invalidação por I10. |
| **Sem cache → roundtrip por uso a banco distinto** (H3) | Alto | Camada de cache (memória + invalidação por evento) p/ licenças de módulo, escopo, roteiros contábeis, árvore de UOs. |
| **Latência de prestação cresce com N tenants** (H4) | Alto | Paralelizar despacho do Outbox (concorrência limitada); pular tenants sem pendências; cachear módulos licenciados. Caminho final: broker (ADR-0011). |
| **Mutação descartada silenciosamente no holder** (H5) | Alto | Falhar alto no 2º `Definir` divergente; teste de arquitetura multi-contexto. |
| **Plan-cache pollution pelo `IN` variável** (M-3) | Médio | Em PROD: TVP/`OPENJSON` p/ lista de UOs, ou teto + paginação; medir sob carga. `[a confirmar]` SGBD de prod. |
| **Custo operacional do banco-por-tenant** (M-4) | Médio | Orquestração de migrations em lote com observabilidade por tenant; pooling consciente; runbook de provisionamento/rotação; reconhecer teto prático de tenants/instância. |

---

## 4. Lacunas e excessos de cobertura

**Lacunas (obrigação/destino não coberto):** EFD-Reinf+DCTFWeb (K4, 🔴), SADIPEM/CDP (K5, 🔴),
Transferegov (H10, 🔴), Encerramento de exercício (K3, contábil), conformidade SIAFIC nomeada (M-1),
DCTFWeb/MIT como destino (M-2), CADPREV/SIOPS como workflow (H9). Confirmar formalmente o
descarte de DEFIS/Simples-tomador, e-Financeira, FGTS Digital, MANAD para administração direta (M-8).

**Excessos (planejado e provavelmente desnecessário p/ o piloto):** CNAB 400 + layouts por banco,
XBRL-GL completo para a MSC (CSV homologa), `IConsultaSiconfi` com 7 endpoints (basta
`/extrato_entregas` no MVP). Cortá-los **libera foco** para as lacunas críticas acima. Há **pouco
over-engineering real** — os excessos são de granularidade/escopo, não de destinos inventados.

**O que está comprovadamente BEM (riscos menores que o briefing sugeria):**
- Contabilidade **síncrona e transacional** (não eventualmente consistente) — lançamento PCASP na
  mesma UoW do fato; ΣD=ΣC é invariante de domínio (ADR-0013). O Outbox só carrega integration
  events cross-módulo (MSC→Transparência). **Desenho correto.**
- **Outbox transacional sem dual-write** (interceptor materializa na mesma transação do estado).
- **Defesa em profundidade de tenant** mantida mesmo com banco dedicado (Global Query Filter +
  `TenantSaveChangesInterceptor` que lança em gravação cross-tenant).
- Banco-por-tenant e ABAC/UO são decisões **justificadas** pelo perfil (compliance TCE/LGPD, SoD).

---

## 5. AÇÕES priorizadas (o que mudar/adicionar)

> Ordem = correção/robustez de baixo esforço primeiro, depois performance estrutural, depois plano.

### P0 — Robustez/correção (baixo esforço, alto valor)
1. **Invalidação do cache de connection string** (K6): TTL + `Invalidar(tenantId)` no provisionamento/rotação.
2. **Outbox `AttemptCount`+`NextAttemptUtc`+dead-letter+alerta** (H1); `OrderBy` resiliente a cabeça envenenada.
3. **`ModuleUnitOfWork` falha-alto** no 2º `Definir` divergente + teste de arquitetura multi-contexto (H5).

### P1 — Correção do plano (sem código, destrava sequenciamento)
4. **Re-sincronizar Plano-Mestre** (K1, M-5): coluna Status; M0–M4 = ENTREGUE; parar de copiar diagnóstico.
5. **Reescrever W4.2/W4.3/W5.4 conforme ADR-0009** (K2): gerar+validar+empacotar+reconciliar, **não** transmitir.
6. **Atualizar W9.7 p/ banco-dedicado** (H6); ajustar redação "reúso de transmissor" (B-3).

### P2 — Performance estrutural
7. **Remover `sync-over-async` + cachear `EscopoEfetivo`** no caminho de autorização (H2).
8. **Introduzir camada de cache** (licenças, escopo, roteiros, árvore de UOs) com invalidação por evento (H3).
9. **Paralelizar despacho do Outbox** + skip de tenants sem pendências (H4). `[a confirmar]` ponto de saturação por medição.

### P3 — Fechar lacunas de compliance (antes da virada anual / risco CAUC)
10. **Inserir W3.4 Encerramento de exercício** antes da prestação anual (K3).
11. **Adicionar EFD-Reinf+DCTFWeb** (K4) cruzando retenção ↔ ingestão NFS-e/ADN. `[a confirmar]` regime de órgão público.
12. **Adicionar SADIPEM/CDP** (K5) alimentado pelo passivo PCASP; vincular ao painel de pendências fiscais.
13. **Ligar W9.6 ao Transferegov** (H10); **nomear obrigação SIAFIC** (M-1).
14. **CADPREV/SIOPS como workflows próprios** (H9) — após confirmar RPPS vs RGPS do piloto.

### P4 — Reordenação e enxugamento
15. **Subir Tesouraria/caixa p/ M2** (H8); **antecipar PNCP p/ pós-M4** (B-1).
16. **Quebrar W5.2 (eSocial) e W6.2 (IPTU)** em sub-workflows (H7).
17. **QA/a11y transversal por workflow** (M-6), não concentrado em M9.
18. **Enxugar excessos** p/ MVP: CNAB 240+PIX+CSV+`/extrato_entregas`; resto sob demanda (B-2).

### P5 — Endurecimento e orçamento operacional
19. **Plan cache:** avaliar TVP/`OPENJSON` p/ filtro de UO em PROD (M-3); medir sob carga.
20. **Orçar custo operacional do banco-por-tenant** (M-4): runbook de provisionamento/rotação, pooling, janela de migration.
21. **Fechar `[a confirmar]` jurídicos de W1.6** (base legal LGPD, DPO, subdelegação) antes de M6/M7 (M-7); descartar formalmente DEFIS/e-Financeira/FGTS/MANAD (M-8).

> Itens `[a confirmar]` exigem validação na fonte oficial do exercício ou medição em carga real antes
> de virar requisito/decisão (CLAUDE.md §16).

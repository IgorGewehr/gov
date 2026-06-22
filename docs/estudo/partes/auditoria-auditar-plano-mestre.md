# Auditoria crítica — PLANO-MESTRE.md (auditor cético)

> Auditor adversarial. Fontes lidas: `PLANO-MESTRE.md`, `GAP-E-ROADMAP.md`, `ESTADO-ATUAL.md`,
> `MAPA-PRESTACAO-CONTAS.md`, ADRs 0005/0007/0009/0013, índice m4/m5/m6-prep.
> Data: 2026-06-22. Veredito: a **espinha M0→M3 está bem ordenada**, mas o documento está
> **desatualizado frente ao estado real (M0–M3 concluídos, M4 em curso)** e contém **um
> conflito com ADR-0009 que invalida o critério-de-pronto de dois workflows críticos**.
> O escopo cobre as obrigações do MAPA, com **uma lacuna federal real (SIOPS/CADPREV/Censo INEP
> 2ª etapa não viram workflow próprio)** e **esforço subestimado em 3 pontos**.

---

## CRÍTICO

### C1 — O plano descreve como *backlog futuro* o que já está PRONTO em runtime (documento dessincronizado)
- **Evidência:** a lista de tarefas do projeto marca **M0, M1, M2, M3 = `completed` e M4 = `in_progress`**.
  As ADRs 0007 (RBAC/ABAC/UO/I4), 0008 (A1 envelope), 0013 (PCASP automático) estão **"Aceito"** e o
  contexto de runtime confirma M0–M3 + A1 provados. Porém o `PLANO-MESTRE.md` apresenta **W0.x, W1.x,
  W2.x, W3.x como trabalho a executar** ("hoje 0 linhas", "destravar build", "18 handlers órfãos"),
  copiando verbatim o `ESTADO-ATUAL.md`/`GAP-E-ROADMAP.md` que são **fotos do pré-M0**.
- **Impacto:** um agente que pegar este plano vai **re-implementar contabilidade já entregue** ou
  travar tentando "corrigir o build" que já está verde. O documento mestre não pode contradizer o
  board de execução — é a fonte de sequenciamento.
- **Recomendação:** adicionar coluna **Status (done/in-progress/pending)** por workflow e mover M0–M3
  para um bloco "ENTREGUE (ver ADRs)". Hoje o plano só serve como *histórico*, não como guia.

### C2 — W4.2 e W4.3 violam ADR-0009: o critério-de-pronto "transmite via SOAP/REST com protocolo" é FALSO por refutação factual
- **Evidência:** ADR-0009 ("Aceito", fonte m4-prep + verificação adversarial) **refuta a premissa de
  transmissão por API**: TCE-RS **não tem web service de upload** (envio é PAD desktop + assinatura
  pessoal ICP no e-Protocolo); SICONFI só tem **API de consulta**, MSC é **upload manual** homologado
  com **e-CPF A3 pessoal**. Mas o plano ainda diz, em W4.2: *"cliente de transmissão SICOE (SOAP/REST)…
  transmitida com recibo"* e em W4.3: *"cliente API SICONFI… enviáveis via API SICONFI com protocolo
  real"*. Idem nota de sequenciamento §4 lista PNCP/SICONFI como "transmissão".
- **Impacto:** o critério-de-pronto desses dois workflows **é inatingível** como escrito — leva o
  executor a construir um cliente de envio que não existe, ou a declarar pronto algo que nunca
  transmitirá. Risco direto na **meta nº1 do dono**.
- **Recomendação:** reescrever W4.2/W4.3 conforme ADR-0009 — escopo = **gerar artefato posicional
  correto + pré-validar RDI local + empacotar (ZIP+hash) + reconciliar via API de *consulta* +
  registrar recibo do ato humano**. Remover "transmite/POST/SOAP". A assinatura aqui é a **A1
  institucional só do que assinamos server-side**; o envio é ato humano com cert pessoal.

### C3 — Encerramento de exercício / virada de Restos a Pagar não tem workflow dedicado (dependência fiscal anual mal-ordenada)
- **Evidência:** o `ESTADO-ATUAL` cita `EncerrarExercicio.cs` com TODO `revisao-contabil`; o MAPA exige
  **MSC de Encerramento (anual, 31/mar)**, **DCA (anual)**, **Contas Anuais Res. 1134/2020** e a
  **virada de RP processados/não-processados**. No plano isso aparece pulverizado (menção solta em
  W2.4 "virada de RP", W3.3 "MSC de Encerramento", W4.5 "Res. 1134") mas **nenhum workflow encerra o
  exercício de fato** (apuração de superávit/déficit, transposição de saldos, abertura do exercício
  seguinte, lançamentos de encerramento das contas de resultado).
- **Impacto:** sem encerramento, a MSC de dezembro e a DCA nascem incorretas — falha que só aparece
  em janeiro/março e **trava a prestação anual**, a parte mais escrutinada pelo TCE.
- **Recomendação:** criar **W3.4 — Encerramento de exercício contábil** (lançamentos de encerramento,
  apuração de resultado, transposição de saldos, abertura) **antes** de W4.3/W4.5, como pré-requisito
  da MSC de Encerramento e da DCA.

---

## ALTO

### A1 — W9.7 ainda carrega a premissa "schema isolado" abandonada pela ADR-0005 (banco DEDICADO por tenant)
- **Evidência:** ADR-0005 ("Aceito") trocou *shared-DB/schema-por-módulo* por **banco dedicado por
  tenant** e re-significou a §9 da CLAUDE.md. Mas W9.7 ainda diz "convergir dev/prod (**schema
  isolado**, MigrateAsync)" e o `ESTADO-ATUAL` descreve a divergência SQLite-1-arquivo vs schema. Com
  banco-por-tenant, o problema real passa a ser **provisionamento + fan-out de migrations por banco**
  (`SchemaProvisioner`), não "schema isolado".
- **Recomendação:** reescrever W9.7 para refletir banco-dedicado (migrations por módulo *por banco de
  tenant*, provisionamento idempotente, paridade do `SchemaProvisioner` dev/prod).

### A2 — Esforço subestimado em 3 itens "🔴/🟠" tratados como um workflow só
- **W2.3 (PCASP) + W2.4 (lançamento automático):** carregar PCASP oficial + 3 enfoques MCASP +
  EventoContabil parametrizável + roteiros por fato é, sozinho, semanas — não cabe no mesmo "marco
  dias". O ADR-0013 admite "bloqueado por fonte oficial". **OK estar feito**, mas o plano o vendia como
  uma linha. Lição p/ M5–M9: **W5.2 (eSocial S-1.3, 8 eventos + SOAP + XSD)** e **W6.2 (motor IPTU +
  DAM FEBRABAN + carnê)** estão igualmente subdimensionados num único workflow cada.
- **Recomendação:** quebrar W5.2 em (a) tabelas S-1000/S-1010, (b) periódicos S-1200/1202/1207, (c)
  S-2200/2299/S-1299 + WebService/ambiente restrito. Quebrar W6.2 em apuração e em
  emissão/arrecadação (DAM/PIX/CNAB).

### A3 — Lacunas de obrigação federal que o MAPA lista mas o plano não converte em workflow
- **SIOPS (Saúde, bimestral, mín. 15% ASPS)** aparece só embutido em W7.2 junto de 4 outras coisas;
  **CADPREV/DRPPS (RPPS)** do MAPA §2 **não aparece em nenhum workflow** (W5.x cobre INSS/RPPS no
  cálculo, mas não a remessa CADPREV); **Educacenso 2ª etapa (situação do aluno, fev-mar ano+1)** e
  **PNATE consolidação anual** estão diluídos.
- **Impacto:** RPPS sem CADPREV e Censo sem 2ª etapa = prestação federal incompleta (risco de repasse).
- **Recomendação:** tornar **CADPREV** workflow explícito em M5 e **SIOPS** workflow próprio em M7;
  marcar `[a confirmar]` periodicidade no plano (já marcado no MAPA).

### A4 — W4.4 (Tesouraria/conciliação bancária) está tarde: deveria preceder o "pagamento" real
- **Evidência:** W4.4 depende de W2.4, mas está em M4, **depois** de toda a contabilidade e remessa.
  Pagamento (W2.2) sem conta bancária/movimento de caixa modelado gera lançamento de pagamento que
  **não concilia** — e a conciliação CNAB retorno é insumo do balancete financeiro (DFC, Balanço
  Financeiro das 7 DCASP em W3.2).
- **Recomendação:** subir o núcleo de **contas bancárias + movimento de caixa** para M2 (junto de
  W2.2/W2.4); deixar só a **conciliação CNAB 240** em M4.

---

## MÉDIO

### M1 — Redundância de fonte: o plano duplica ESTADO-ATUAL/GAP em vez de referenciá-los
- Três documentos repetem "0 linhas", "18 handlers órfãos", "3 linhas hardcoded". Como C1 mostra, isso
  vira **dívida de manutenção**: quando o código avança, os três precisam ser editados e divergem.
- **Recomendação:** PLANO-MESTRE mantém só *o que fazer + sequência + status*; diagnóstico fica nos
  docs de diagnóstico (referência, não cópia).

### M2 — W9.8 (E2E HTTP + a11y) "depende de todos os marcos" — antipadrão de QA empurrado para o fim
- Concentrar E2E/cross-tenant/axe-core em M9 contraria a própria nota "incremental". O `ESTADO-ATUAL`
  já aponta **isolamento cross-tenant testado só na Identidade** e **0 testes a11y** — risco que cresce
  a cada módulo entregue sem rede.
- **Recomendação:** transformar em **gate transversal**: cada workflow fullstack entrega seu E2E HTTP +
  axe-core; W9.8 vira só a *consolidação/cobertura medida no CI*, não a criação tardia.

### M3 — W1.6 (sensibilidade/LGPD) e W6/W7 (dados sensíveis) com dependência frágil
- Tributos sigilo fiscal (W6.5), Saúde/Assistência (W7.x) dependem de W1.6, mas W1.6 é 🟠 em M1 e
  Tributos só chega em M6. Se W1.6 não congelar a base legal LGPD/papel do DPO (`[a confirmar]`), os
  módulos sensíveis herdam débito.
- **Recomendação:** fechar os `[a confirmar]` jurídicos de W1.6 (base legal, DPO, subdelegação) como
  pré-condição explícita antes de M6/M7, não como detalhe de M1.

---

## BAIXO

- **B1 — Ordenação M8 (cidadão/BI) vs M9 (PNCP bloqueante):** PNCP é **condição de eficácia de
  contrato (art. 94)** — bloqueia execução financeira. Está em M9 (penúltimo). Para um piloto que já
  empenha, PNCP bloqueante é mais urgente que Portal do Cidadão (M8). Considerar antecipar **W9.1
  (PNCP)** para logo após M4. (Severidade baixa só porque o piloto pode operar com PNCP manual no curto
  prazo, mas é dívida de compliance que cresce.)
- **B2 — Datas no cabeçalho do plano (2026-06-22) iguais à data de hoje** mas conteúdo é pré-M0;
  atualizar carimbo ao re-sincronizar (C1).
- **B3 — W5.4 e W4.2 "reusam" o mesmo transmissor;** com ADR-0009 (sem API), o "reúso" é de
  *empacotamento/validação local*, não de transmissão — ajustar a redação junto de C2.

---

## Resumo da reordenação recomendada

1. **Sincronizar com a realidade (C1):** coluna de status; M0–M3 marcados ENTREGUE; plano vira guia, não foto.
2. **Corrigir C2 (ADR-0009):** reescrever W4.2/W4.3/W5.4 — gerar+validar+empacotar+reconciliar, **não** transmitir.
3. **Inserir W3.4 Encerramento de exercício (C3)** antes da prestação anual.
4. **Subir Tesouraria/caixa (A4)** para M2; deixar só conciliação CNAB em M4.
5. **Antecipar PNCP bloqueante (B1)** para logo após M4.
6. **Quebrar workflows subdimensionados (A2):** PCASP, eSocial, IPTU.
7. **Promover CADPREV e SIOPS a workflows próprios (A3).**
8. **QA/a11y transversal por workflow (M2),** não concentrado em M9.
9. **Atualizar W9.7 para banco-dedicado (A1),** não "schema isolado".

**Arquivo:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/estudo/partes/auditoria-auditar-plano-mestre.md`

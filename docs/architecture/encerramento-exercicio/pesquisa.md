# Encerramento de Exercício Contábil Público (MCASP / PCASP)

> Pesquisa de fundamentação para o GAP do módulo Finanças/Contabilidade do Tensorroot.Gov.
> Sem o encerramento de exercício, a **MSC de dezembro/encerramento** e a **DCA anual** nascem incorretas
> (contas de resultado das classes 3 e 4 não zeradas; saldos de Restos a Pagar não inscritos; resultado
> patrimonial não apurado no PL).
>
> Fonte normativa primária: **STN — IPC 03 "Encerramento de Contas Contábeis no PCASP"** (2013),
> publicada com base na Portaria STN nº 634/2013 e no MCASP. As IPC são de caráter orientador
> (observância facultativa), mas refletem a mecânica de partidas do MCASP.
> Base legal: **Lei nº 4.320/1964** (arts. 35, 36, 38). Calendário/MSC: **Portaria STN nº 642/2019**
> e Regras Gerais da MSC (SICONFI), **Decreto nº 10.540/2020**, **Nota Técnica SEI nº 11577/2019/ME**.

---

## 0. Visão geral e tipos de escrituração na virada de exercício

O encerramento consiste em **ajustar e encerrar contas contábeis** para (a) apurar o resultado do exercício
e (b) preparar a abertura do exercício seguinte (IPC 03, §§10–13). Na transição convivem quatro tipos de escrituração:

1. **Execução normal de dezembro** (mês 12) — inclui a inscrição dos Restos a Pagar.
2. **Encerramento parcial** — preparação (reclassificações de curto/longo prazo, ajustes para perdas, provisões).
3. **Encerramento final** — apuração do resultado (zera classes 3 e 4; encerra classes 5 e 6).
4. **Abertura do exercício seguinte** — transposição/reabertura de saldos.

A IPC 03 usa, didaticamente, **partidas dobradas de 1ª fórmula** (1 débito × 1 crédito), mas o ente pode
operacionalizar por transferência automática de saldos (§17). Os lançamentos abaixo seguem o **PCASP para
Estados e Municípios** (Anexo III da IPC 00).

---

## 1. Apuração do RESULTADO PATRIMONIAL (encerramento das VPA/VPD)

Encerram-se as contas de **VPD (classe 3)** e **VPA (classe 4)** contra a conta de resultado do PL:
`2.3.7.1.1.01.00 — SUPERÁVITS OU DÉFICITS DO EXERCÍCIO`.

- Essa conta **só é movimentada na apuração** e tem **saldo durante 1 dia (31 de dezembro)**;
  antes do encerramento deve estar **zerada** (IPC 03, §§21–23).
- Subgrupo de Resultados Acumulados (`2.3.7`):
  - `2.3.7.1.1.01.00` Superávits ou Déficits **do Exercício**
  - `2.3.7.1.1.02.00` Superávits ou Déficits **de Exercícios Anteriores**
  - `2.3.7.1.1.03.00` **Ajustes** de Exercícios Anteriores
  - `2.3.7.1.1.04.00` Superávits/Déficits de Extinção, Fusão e Cisão (não encerra; permanece)

**Lançamentos de encerramento (31/dez):**

```
(a) VPD — classe 3
    D: 2.3.7.1.1.01.00  Superávits ou Déficits do Exercício
    C: 3.X.X.X.X.XX.XX   Variação Patrimonial Diminutiva

(b) VPA — classe 4
    D: 4.X.X.X.X.XX.XX   Variação Patrimonial Aumentativa
    C: 2.3.7.1.1.01.00  Superávits ou Déficits do Exercício
```

Após esses lançamentos: **todas as contas das classes 3 e 4 ficam com saldo zero** (não transferem saldo);
o **saldo de `2.3.7.1.1.01.00` é o resultado patrimonial do exercício** (superávit credor / déficit devedor)
(IPC 03, §§24–26). Os saldos das contas 3 e 4 **antes** do encerramento alimentam a **DVP** (Demonstração das
Variações Patrimoniais), classes 3 e 4 do PCASP (IPC 03, §30 — também base da DCASP já provada no módulo).

**Lançamentos de abertura (1º de janeiro do exercício seguinte):**

```
Transfere resultado do exercício para acumulados:
    D: 2.3.7.1.1.01.00  Superávits ou Déficits do Exercício
    C: 2.3.7.1.1.02.00  Superávits ou Déficits de Exercícios Anteriores

Encerra Ajustes de Exercícios Anteriores (se houver saldo):
    D: 2.3.7.1.1.03.00  Ajustes de Exercícios Anteriores
    C: 2.3.7.1.1.02.00  Superávits ou Déficits de Exercícios Anteriores
```

A conta `2.3.7.1.1.03.00` **não é encerrada em 31/dez** (seu saldo aparece no Balanço Patrimonial);
ela só é transferida para `...02.00` **na abertura** do exercício seguinte (IPC 03, §§27–28).

---

## 2. Apuração do RESULTADO ORÇAMENTÁRIO (receita × despesa)

O resultado orçamentário é o **confronto entre receita orçamentária realizada/arrecadada e despesa
orçamentária empenhada** (Lei 4.320, **art. 35**: "pertencem ao exercício financeiro as receitas nele
arrecadadas e as despesas nele legalmente empenhadas" — regime de competência orçamentária). É evidenciado
diretamente no **Balanço Orçamentário** (DCASP), não em conta de PL.

No PCASP, o controle orçamentário vive nas **classes 5 (orçamento aprovado/atos potenciais) e 6 (execução)**.
O encerramento dessas contas confere o orçamento aprovado contra a execução (IPC 03, §§33–38):

- **Receita:** `5.2.1` (previsão) × `6.2.1` (execução: a realizar / realizada / deduções).
- **Despesa:** `5.2.2` (dotação/fixação) × `6.2.2` (créditos disponível / indisponível / utilizado).

### Encerramento da execução da RECEITA (contrapartida: `5.2.1.1.1.00.00` Previsão Inicial da Receita Bruta)
```
Receita prevista e não realizada:
    D: 6.2.1.1.0.00.00  Receita a Realizar
    C: 5.2.1.1.1.00.00  Previsão Inicial da Receita Bruta

Receita realizada vs. deduções:
    D: 6.2.1.2.0.00.00  Receita Realizada
    C: 6.2.1.3.X.XX.XX  (-) Deduções da Receita Orçamentária

Receita realizada vs. previsão:
    D: 6.2.1.2.0.00.00  Receita Realizada
    C: 5.2.1.1.1.00.00  Previsão Inicial da Receita Bruta

Previsão de deduções:
    D: 5.2.1.1.2.XX.XX  (-) Previsão de Deduções da Receita
    C: 5.2.1.1.1.00.00  Previsão Inicial da Receita Bruta
```

### Encerramento da execução da DESPESA (contrapartida: `5.2.2.1.1.01.00` Crédito Inicial e `5.2.2.1.2` Dotação Adicional)
Estados detalhados de crédito utilizado (`6.2.2.1.3`): empenhado a liquidar (`...01`), em liquidação (`...02`),
liquidado a pagar (`...03`), liquidado pago (`...04`), e os inscritos em RP (`...05`, `...06`, `...07`).
```
Crédito disponível não utilizado:
    D: 6.2.2.1.1.00.00  Crédito Disponível
    C: 5.2.2.1.1.01.00  Crédito Inicial   (e/ou 5.2.2.1.2.XX.XX Dotação Adicional)

Empenhos liquidados e pagos no exercício:
    D: 6.2.2.1.3.04.00  Crédito Empenhado Liquidado Pago
    C: 5.2.2.1.1.01.00  Crédito Inicial   (e/ou 5.2.2.1.2.XX.XX Dotação Adicional)
```
A IPC 03 distingue dois cenários (§§48–51): **com** controle da origem (inicial × adicional) desde o empenho
(§50) ou **sem** esse controle (§51, encerra tudo contra Crédito Inicial). O subtítulo `5.2.2.1.3` Dotação
Adicional por Fonte encerra-se **verticalmente** contra `5.2.2.1.3.99.00` Valor Global (§§38, 52) — fontes:
superávit financeiro de exerc. anterior, excesso de arrecadação, anulação de dotação, operações de crédito,
reserva de contingência, cancelamentos.

Após o encerramento, **todas as contas das classes 5 e 6 de execução/dotação ficam zeradas** (não transferem
saldo) (IPC 03, §53).

> [a confirmar] O **superávit financeiro** (`5.2.2.1.3.01.00` na fonte; conceito da Lei 4.320 art. 43 §2º:
> diferença positiva entre ativo financeiro e passivo financeiro, mais créditos adicionais transferidos e
> operações de crédito vinculadas) é apurado no **Balanço Patrimonial / Demonstrativo do Superávit Financeiro**
> e serve de fonte para abertura de créditos adicionais no exercício seguinte — confirmar a mecânica exata de
> apuração do ativo/passivo financeiro pela tabela de natureza de saldo (atributo F) no MCASP Parte II/IV.

---

## 3. INSCRIÇÃO DE RESTOS A PAGAR (processados e não processados)

**Base legal — Lei 4.320, art. 36:** "Consideram-se Restos a Pagar as despesas empenhadas mas não pagas
até o dia 31 de dezembro, distinguindo-se as **processadas** das **não processadas**."

- **RP Processados (RPP):** empenhada **e liquidada**, não paga (empenho → liquidação → ∅ pagamento).
- **RP Não Processados (RPNP):** empenhada e **não liquidada** (apenas empenho, ou empenho em liquidação).
- A inscrição deve observar disponibilidade financeira e a LRF (LC 101/2000, art. 42), prevenindo riscos
  ao equilíbrio das contas.
- [a confirmar] Pela 10ª ed. do MCASP, **RPNP liquidados no exercício seguinte mas não pagos são
  transferidos/reclassificados para RPP** (busca; confirmar o lançamento de reclassificação na edição vigente).

**Contas de controle:** `5.3` Inscrição de RP / `6.3` Execução de RP (RPNP em `...1`, RPP em `...2`).

**Inscrição dos RP do exercício corrente** (parte do encerramento da despesa — IPC 03 §§50–51):
```
RPNP a liquidar:
    D: 6.2.2.1.3.01.00  Crédito Empenhado a Liquidar
    C: 6.2.2.1.3.05.00  Empenhos a Liquidar Inscritos em RP Não Processados
    D: 5.3.1.7.0.00.00  RP Não Processados – Inscrição no Exercício
    C: 6.3.1.7.1.00.00  RP Não Processados a Liquidar – Inscrição no Exercício

RPNP em liquidação:
    D: 6.2.2.1.3.02.00  Crédito Empenhado em Liquidação
    C: 6.2.2.1.3.06.00  Empenhos em Liquidação Inscritos em RP Não Processados
    D: 5.3.1.7.0.00.00  RP Não Processados – Inscrição no Exercício
    C: 6.3.1.7.2.00.00  RP Não Processados em Liquidação – Inscrição no Exercício

RPP (liquidados e não pagos):
    D: 6.2.2.1.3.03.00  Crédito Empenhado Liquidado a Pagar
    C: 6.2.2.1.3.07.00  Empenhos Liquidados Inscritos em RP Processados
    D: 5.3.2.7.0.00.00  RP Processados – Inscrição no Exercício
    C: 6.3.2.7.0.00.00  RP Processados – Inscrição no Exercício
```
(Em seguida, as contas `6.2.2.1.3.05/06/07` são encerradas contra Crédito Inicial/Dotação Adicional.)

**Encerramento dos RP de exercícios anteriores** (antes de inscrever os novos — IPC 03 §§40–47): baixa de
RPNP pagos (`D 6.3.1.4.0.00.00 / C 5.3.1.1` ou `5.3.1.2`), cancelados (`6.3.1.9`), e reclassificação de RPP
por exercício (`D 5.3.2.2 / C 5.3.2.1`).

> Impacto no Tensorroot.Gov: a **MSC agregada de dezembro DEVE conter** os lançamentos de inscrição em RP —
> contas `6.2.2.1.3.05/06/07` e `6.3.1.7.1`, `6.3.1.7.2`, `6.3.2.7.0` (Regras Gerais MSC). Sem isso, a MSC
> de dezembro está incompleta.

---

## 4. TRANSPOSIÇÃO / TRANSFERÊNCIA de saldos (Ativo, Passivo, PL) para o exercício seguinte

**Regra geral (IPC 03 §31):** os saldos das **classes 1 (Ativo) e 2 (Passivo)** são **transferidos para o
exercício seguinte**. Contas de **resultado (3 e 4)** e de **execução orçamentária (5 e 6)** **não transferem**
(são zeradas no encerramento). Essa distinção é o **atributo de encerramento** das contas (encerra × não
encerra/transfere saldo).

- **Encerramento parcial** exige reclassificação **curto × longo prazo** (Circulante × Não Circulante,
  critério 12 meses — MCASP / Res. CFC 1.437/13) e ajustes de **ajustes para perdas no ativo** e **provisões
  no passivo** (IPC 03 §§31–32).
- **Abertura (mês 00 / 1º jan):** os saldos das contas que não encerram **migram** como saldo inicial; soma-se
  a transferência do resultado do exercício (§1) e dos ajustes de exercícios anteriores.

> [a confirmar] **Natureza de saldo "permanente" (P) × "financeiro" (F):** o MCASP atribui a cada conta
> patrimonial um indicador de natureza de saldo (atributo Financeiro/Permanente), usado para apurar o
> **superávit financeiro** (ativo financeiro − passivo financeiro) e separar disponibilidades. O enunciado do
> GAP fala em "transposição dos saldos das contas de natureza saldo permanente" — na prática **todas** as
> contas de Ativo/Passivo (F e P) transferem saldo; o atributo F/P serve para classificação/relatórios, não
> para decidir o que transfere. Confirmar a tabela de atributos (F/P) no MCASP Parte IV (PCASP) e Anexos.

---

## 5. CALENDÁRIO e alimentação da MSC / DCA

**Mecânica de períodos (mês 13 / mês 14):** o encerramento ocorre em **períodos extraordinários** —
tipicamente **mês 13** (apuração/distribuição do resultado, zeragem das contas de resultado) e **mês 14**
(transposição/migração dos saldos que não encerram para o **mês 00** do exercício seguinte). A nomenclatura
exata (13/14) **não é padronizada** pela STN — "os procedimentos relativos ao encerramento do exercício não
são padronizados" — cada sistema operacionaliza à sua maneira (Regras Gerais MSC).

**Dois tipos de MSC no SICONFI** (Regras Gerais MSC):
- **MSC Agregada** — mensal; gera RREO e RGF. **A MSC de dezembro deve conter os lançamentos de inscrição em
  RP** (contas `6.2.2.1.3.05/06/07`, `6.3.1.7.1`, `6.3.1.7.2`, `6.3.2.7.0`).
- **MSC de Encerramento** — anual (1×/ano); gera o **rascunho da DCA** (consolidação, LC 101/2000 art. 51).
  - Saldo inicial = **saldo final da MSC Agregada de Dezembro**.
  - `period_change` = **toda a movimentação de encerramento** (independe de o sistema usar mês 13/14).
  - Saldo final = saldos **após apuração e distribuição do resultado**, com **contas de resultado (3 e 4)
    zeradas**.
  - Observa o **Decreto 10.540/2020** (SIAFIC) e a **NT SEI 11577/2019/ME**.
  - [a confirmar] Prazo de envio: documento das Regras Gerais cita "até o último dia de março" (ano de
    referência variável — confirmar o prazo vigente para o exercício corrente do Tensorroot.Gov).

**Por que o GAP quebra MSC/DCA sem encerramento:** sem (1) zerar VPA/VPD → contas 3/4 vão indevidamente para a
MSC de encerramento e a DVP/Balanço Patrimonial; sem (3) inscrever RP → a MSC agregada de dezembro fica
incompleta e o Balanço Orçamentário/DCA subnotifica RP; sem (4) transpor saldos → a abertura (mês 00) e o
Balanço Patrimonial do exercício seguinte nascem errados.

---

## Checklist de implementação (Tensorroot.Gov)

1. [ ] Rotina de **encerramento parcial**: reclassificação curto/longo prazo + ajustes para perdas/provisões.
2. [ ] **Apuração patrimonial**: zerar classes 3 e 4 contra `2.3.7.1.1.01.00`; gerar saldo do resultado.
3. [ ] **Inscrição de RP**: classificar empenhos em RPP/RPNP (a liquidar / em liquidação) e gerar lançamentos `5.3`/`6.3` + encerrar `6.2.2.1.3.05/06/07`. **Deve sair na MSC agregada de dezembro.**
4. [ ] **Encerramento orçamentário**: zerar classes 5 e 6 de execução/dotação; conferir `5.2.x`↔`6.2.x`.
5. [ ] **Atributo de encerramento por conta** (encerra × transfere) + atributo **F/P** [a confirmar tabela MCASP].
6. [ ] **Abertura (mês 00)**: transpor saldos de Ativo/Passivo/PL; transferir resultado e ajustes para `...02.00`.
7. [ ] **MSC de Encerramento (anual)**: saldo inicial = MSC dez; `period_change` = encerramento; saldo final com contas 3/4 zeradas → rascunho DCA.
8. [ ] Conferências: ΣD=ΣC após cada etapa; `2.3.7.1.1.01.00` zerada antes e refletindo o resultado depois; classes 3/4/5/6 de execução zeradas após encerramento.

---

## Fontes (oficiais STN / legislação)

- **STN — IPC 03 "Encerramento de Contas Contábeis no PCASP" (2013)** — fonte primária dos lançamentos e contas (apuração patrimonial §§19–30; ativo/passivo §§31–32; orçamento e RP §§33–54). PDF (cópia TCM-GO): https://www.tcmgo.tc.br/portalgt/wp-content/uploads/2014/12/IPC03_EncerramentoContasContabeisPCASP.pdf
- **Lei nº 4.320/1964** (arts. 35, 36, 38, 43) — exercício financeiro, Restos a Pagar, superávit financeiro. Planalto: https://www.planalto.gov.br/ccivil_03/leis/l4320.htm
- **SICONFI — Matriz de Saldos Contábeis: Regras Gerais (Portaria STN nº 642/2019, Anexo I, ed. 2026)** — MSC agregada × encerramento, mês 13/14, inscrição de RP na MSC de dezembro, DCA: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- **MCASP — Manual de Contabilidade Aplicada ao Setor Público (STN)** — base conceitual (PCASP, DVP, DCASP, natureza de saldo F/P). MCASP 10ª ed. (CNM): https://cnm.org.br/storage/noticias/2023/Links/MCASP%2010%C2%AA%20edic%CC%A7a%CC%83o%20(3).pdf ; MCASP Parte IV (PCASP, TCE-GO): https://tcenet.tce.go.gov.br/Downloads/Arquivos/002980/MCASP-Parte_IV_-_PCASP.pdf
- **Decreto nº 10.540/2020 (SIAFIC)** e **Nota Técnica SEI nº 11577/2019/ME** — citados nas Regras Gerais da MSC de encerramento (referência indireta via SICONFI acima).
- Material de apoio: ENAP — "Abertura/Encerramento do Exercício Contábil" (módulo): https://repositorio.enap.gov.br/bitstream/1/8002/2/M%C3%B3dulo%202%20-%20Abertura%20do%20Exerc%C3%ADcio%20Cont%C3%A1bil%2C%20Regras%20de%20Consist%C3%AAncia%20das%20Informa%C3%A7%C3%B5es%20e%20Encerramento%20do%20Exerc%C3%ADcio%20Cont%C3%A1bil.pdf

> Itens marcados **[a confirmar]** (§16 do briefing): nº exato de RPNP→RPP na edição vigente do MCASP;
> mecânica de apuração do superávit financeiro e tabela de atributo de natureza de saldo F/P; prazo vigente
> de envio da MSC de encerramento. Conferir diretamente no MCASP Parte II/IV vigente.

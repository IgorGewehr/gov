# M6 — Verificação de Fatos: Dívida Ativa, CDA, Protesto e Execução Fiscal

> Auditoria cética da pesquisa `pesquisa-divida-protesto.md`. Cada afirmação factual
> (regra de cálculo, requisito de CDA/protesto, formato ADN/CNAB, transição ISS↔IBS)
> classificada como **CONFIRMADO** / **PLAUSÍVEL-SEM-FONTE** / **INCERTO**, com a fonte
> verificada e o que exige lei municipal/doc oficial.
> Data da verificação: 2026-06-22. Constituição §16: nada hardcoded; FONTE ou `[a confirmar]`.

---

## Resumo executivo

| Classe | Qtd | Itens |
|---|---|---|
| **CONFIRMADO** (fonte oficial/jurisprudência) | 11 | §§1, 2, 4.1, 5.1, 5.2 (CTN/LEF), PIX cobv, requisitos §5 I-VI, §8 LEF, REsp 1.895.557, intercorrente Tema 566, Súm. 314 |
| **PLAUSÍVEL-SEM-FONTE** (verossímil mas sem doc oficial confirmado) | 4 | leiaute CRA genérico, fluxo operacional protesto, PIX via PSP, fluxo CNAB remessa/retorno |
| **INCERTO / CORRIGIR** | 5 | "48 posições" do código de barras, leiaute exato CRA-RS, atribuição do protesto à L9.492 original, ausência da transição ISS→IBS, lei municipal-tipo |

**Confirmados: 11. Incertos/a corrigir: 5 (3 erros/omissões factuais + 2 dependentes de doc oficial).**

---

## 1. Inscrição em Dívida Ativa — **CONFIRMADO**
- Crédito vencido/não pago → Termo de Inscrição (TIDA) → CDA; presunção relativa de certeza/liquidez.
- Base: LEF (Lei 6.830/80) art. 2º. Texto dos incisos §5º confirmado (ver §2).
- Nada a corrigir. O domínio `InscreverEmDividaAtiva` está alinhado.

## 2. Requisitos da CDA (LEF art. 2º §5º, I–VI) — **CONFIRMADO**
Tabela do pesquisa bate **literalmente** com o texto legal:
- I nome devedor/co-responsáveis + domicílio; II valor originário + termo inicial + **forma de cálculo de juros de mora e demais encargos**; III origem, natureza e fundamento legal; IV atualização monetária + fundamento + termo inicial; V data e número da inscrição; VI nº do processo administrativo/auto de infração.
- **§8º (emenda/substituição até decisão de 1ª instância, devolvido prazo de embargos): CONFIRMADO.**
- **Acréscimo ao pesquisa (relevante p/ `SubstituirCda`):**
  - **Súmula 392/STJ**: substituição até a sentença de embargos só para erro **material/formal**, **VEDADA modificação do sujeito passivo** (é novo lançamento). → invariante de domínio: `SubstituirCda` NÃO pode trocar o devedor.
  - **Tema 1350/STJ** (recente, 2025) reforça impossibilidade de substituir CDA para alterar fundamento legal. `[a confirmar — obter tese final/trânsito do Tema 1350]`
- AÇÃO: o versionamento de CDA deve **bloquear** alteração de sujeito passivo e de fundamento legal (apenas correção formal).

## 3. Cobrança administrativa / limiar de ajuizamento — **CONFIRMADO (em parte) + DEPENDE DE LEI MUNICIPAL**
- REsp 1.895.557 confirma que o **Legislativo municipal pode fixar valor mínimo** para atuação da Fazenda (protesto/ajuizamento). → o limiar por faixa de valor é **decisão municipal parametrizável**, como o pesquisa diz. CONFIRMADO que é prerrogativa do município; o valor em si é `[a confirmar — lei municipal de Maximiliano de Almeida/RS]`.
- Parcelamento/REFIS interrompe prescrição (CTN 174 p.ú. IV): CONFIRMADO (ver §5).

## 4. Protesto extrajudicial da CDA

### 4.1 Fundamento legal — **CONFIRMADO, com correção importante**
- Lei 9.492/97 art. 1º p.ú. inclui CDAs de União/Estados/DF/Municípios e autarquias/fundações: CONFIRMADO.
- **CORREÇÃO FACTUAL:** o pesquisa atribui a protestabilidade à Lei 9.492/97 sem ressalva. O parágrafo único foi **ACRESCENTADO pela Lei 12.767/2012** — não constava da redação original de 1997. Esse marco é juridicamente relevante (anterioridade).
- **OMISSÃO:** o pesquisa cita só o STJ (REsp 1.895.557, dispensa de lei local). Falta a **ADI 5.135/DF (STF)**, que declarou **constitucional** o protesto de CDA. É a fonte de maior hierarquia e deve ser citada. → CONFIRMADO via STF.
- REsp 1.895.557 (1ª Turma, Min. Gurgel de Faria, art. 22 I CF, norma federal autoaplicável): CONFIRMADO literalmente.

### 4.2 Fluxo operacional CRA — **PLAUSÍVEL-SEM-FONTE**
- Fluxo apresentante → CRA estadual (IEPTB) → tabelionato → intimação → lavratura → retorno: coerente com docs de fornecedores (Senior) e portais IEPTB, mas **não há doc normativo único oficial** verificado. Genericamente correto.
- Afirmação "ente não recebe/parcela na janela protocolo→lavratura": razoável (pagamento se dá no cartório), mas **sem fonte normativa específica** — tratar como boa prática, não regra confirmada.

### 4.3 Leiaute (CRA/IEPTB/FEBRABAN) — **INCERTO / a confirmar**
- CNAB 240/400 + XML/WebService (CRA21): existem; CONFIRMADO em alto nível.
- **DISCREPÂNCIA:** doc de fornecedor (Senior) menciona **registro de 600 bytes** para o leiaute CRA, não 240/400 — ou seja, **o tamanho/posições variam por CRA estadual e por versão**, exatamente como o pesquisa já ressalva. Mantém-se `[a confirmar]`.
- A marca "G" do apresentante (leiaute SP) é **específica de CRA estadual** — NÃO generalizar para o RS.
- **PENDÊNCIA CRÍTICA (piloto RS):** leiaute exato do **CRA-RS / IEPTB-RS**, posições, códigos de ocorrência, endpoint e convênio — `[a confirmar — obter doc oficial do CRA-RS]`. Sem isso, não há implementação fiel.

## 5. Execução Fiscal e Prescrição — **CONFIRMADO**

### 5.1 Execução fiscal
- CDA como título executivo extrajudicial; ajuizamento no PJe/eproc: CONFIRMADO. Escopo de integração de saída PJe/eproc-RS permanece decisão de produto `[a confirmar]`.

### 5.2 Prescrição (CTN art. 174) — **CONFIRMADO**
- Prazo 5 anos da constituição definitiva: CONFIRMADO.
- Interrupção I — **despacho do juiz que ordena a citação** (redação LC 118/2005; antes era citação pessoal): CONFIRMADO. Aplicável a execuções ajuizadas após 09/06/2005.
- Retroação à data do ajuizamento (CPC art. 240 §1º, antigo 219 §1º) e Súmula 106/STJ: CONFIRMADO.
- Incs. II–IV (protesto judicial; constituição em mora; reconhecimento do débito): CONFIRMADO.
- **Prescrição intercorrente (LEF art. 40):**
  - **CORREÇÃO de referência:** o pesquisa rotula "REsp 1.340.553" como repetitivo; o **tema repetitivo oficial é o Tema 566/STJ** (REsp 1.340.553/RS é o leading case do Tema 566; a tese abrange Temas 566–571). Citar pelo nº do Tema.
  - Tese (Tema 566): prazo de 1 ano de suspensão + quinquênio intercorrente **inicia automaticamente** na ciência da Fazenda sobre não-localização do devedor/bens: CONFIRMADO.
  - Súmula 314/STJ: CONFIRMADO.
  - **OMISSÃO:** a **Lei 14.195/2021** alterou o art. 40 da LEF (regras de marco/contagem) — o pesquisa não menciona. Verificar impacto no "relógio de prescrição". `[a confirmar — texto atual do art. 40 LEF pós-Lei 14.195/2021 no Planalto]`

## 6. Arrecadação (PIX e CNAB)

### 6.1 PIX Cobrança — **CONFIRMADO**
- PIX **cobv** (cobrança com vencimento) suporta juros/multa/desconto/abatimento; QR dinâmico (location URL); DICT mapeia chave→conta; conciliação por webhook do PSP: CONFIRMADO (Manual de Padrões para Iniciação do Pix, Bacen).
- API municipal tipicamente via PSP/banco arrecadador, não Bacen direto: CONFIRMADO/PLAUSÍVEL. PSP do piloto `[a confirmar]`.

### 6.2 CNAB — **CONFIRMADO em estrutura, com 1 erro factual**
- CNAB 240: header de arquivo + lotes (header/detalhe/trailer) + trailer de arquivo; segmentos O/N para arrecadação com/sem código de barras: CONFIRMADO (FEBRABAN v10.11).
- **ERRO FACTUAL a corrigir:** o pesquisa diz "código de barras de arrecadação (48 posições / 4 blocos)".
  - O **código de barras** de arrecadação FEBRABAN tem **44 posições de dados** (campo de 46 com 2 de controle), tipo "**2 de 5 intercalado**".
  - As **48 posições** correspondem à **linha digitável / representação numérica** (4 blocos de 11 dígitos + 4 DVs = 48 dígitos), NÃO ao código de barras.
  - Corrigir para: "linha digitável de 48 dígitos (4 blocos) ↔ código de barras de 44 posições de dados". Fonte: FEBRABAN "Layout Código de Barras" v7.
- Fluxo remessa→banco→retorno→conciliação contra `Lancamento`/`DividaAtiva`: PLAUSÍVEL/correto. Convênio, versão e banco do piloto `[a confirmar]`.

## 7. OMISSÃO CRÍTICA — Transição ISS → IBS (EC 132/2023) — **INCERTO no pesquisa (não tratado)**
O pesquisa não menciona a Reforma Tributária, mas ela impacta diretamente o ciclo de Dívida Ativa/CDA de ISS:
- **EC 132/2023** institui IVA dual (IBS estadual/municipal + CBS federal), que substitui ISS/ICMS/IPI/PIS/Cofins.
- Cronograma CONFIRMADO (fontes oficiais/Câmara/Fazenda): **2026** fase de teste (CBS 0,9% / IBS 0,1% simbólicas); **2027** extinção PIS/Cofins + Imposto Seletivo; **2029–2032** substituição progressiva de ICMS/ISS pelo IBS; **2033** ISS/ICMS/IPI/PIS/Cofins extintos.
- **Implicação de domínio:** CDAs de **ISS** continuarão existindo e em cobrança/execução por **anos após 2033** (prescrição roda 5+ anos; intercorrente mais). O módulo Tributos deve manter o ISS como tributo **histórico/legado cobrável** mesmo após extinção, e modelar IBS como novo. Alíquotas/regras de ambos parametrizáveis por tenant (§16). `[a confirmar — LC 214/2025 e regulamentação IBS para o leiaute de cobrança]`

---

## 3 RISCOS

1. **Leiaute do CRA-RS não confirmado (bloqueante de protesto).** Toda a §4.3 está em `[a confirmar]`: posições, códigos de ocorrência, tamanho de registro (há indício de 600 bytes, não 240/400) e endpoint variam por CRA estadual. Implementar sobre o leiaute genérico/SP gera arquivo **rejeitado** pelo CRA-RS. Mitigação: obter o manual oficial do IEPTB-RS/CRA-RS antes de codar o adapter; isolar atrás de ACL versionada por CRA.

2. **Relógio de prescrição com base legal desatualizada.** O pesquisa não considera a **Lei 14.195/2021** (art. 40 LEF) nem cita o Tema 566 corretamente. Errar marco/contagem da prescrição intercorrente leva a **perda do crédito público** (extinção) ou cobrança indevida (responsabilização perante o TCE-RS). Mitigação: modelar o agregado de prescrição contra o texto **atual** do art. 40 (Planalto pós-2021) + Tema 566/STJ, com eventos auditáveis de suspensão/interrupção; alíquotas/prazos parametrizáveis.

3. **Continuidade ISS→IBS ignorada.** A omissão da EC 132/2023 cria risco de modelar Tributos como se o ISS fosse perene. CDAs de ISS sobrevivem à extinção do tributo (2033) por força da prescrição; cobrança/protesto/execução desses créditos legados precisa coexistir com o novo IBS. Mitigação: tratar tributo como dado versionado por vigência (não enum fixo), permitir CDA de tributo extinto, e aguardar LC 214/2025 + regulamentação para o leiaute de cobrança do IBS.

---

## Fontes verificadas
- LEF (Lei 6.830/80) art. 2º §5º I-VI, §8º — confirmado (Câmara/Planalto; texto literal validado).
- Lei 9.492/97 art. 1º p.ú. (acrescentado pela **Lei 12.767/2012**) — confirmado.
- **STF ADI 5.135/DF** (constitucionalidade do protesto de CDA) — confirmado.
- STJ REsp 1.895.557 (1ª Turma, Gurgel de Faria; dispensa de lei local; art. 22 I CF) — confirmado via portal STJ.
- Súmula 392/STJ (substituição CDA, vedada troca de sujeito passivo); Tema 1350/STJ (fundamento legal) — confirmado/`[a confirmar tese final 1350]`.
- CTN art. 174 (5 anos; LC 118/2005 — despacho ordena citação) — confirmado.
- LEF art. 40 + Súmula 314/STJ + **Tema 566/STJ** (REsp 1.340.553/RS) — confirmado; verificar **Lei 14.195/2021**.
- Bacen — Manual de Padrões para Iniciação do Pix (cobv, QR dinâmico, DICT) — confirmado.
- FEBRABAN — CNAB 240 v10.11 e Layout Código de Barras v7 (44 dados / 48 linha digitável) — confirmado; corrige "48 posições do código de barras".
- EC 132/2023 + cronograma (Câmara/Min. Fazenda): 2026 teste / 2027 CBS / 2029-2032 ISS→IBS / 2033 extinção — confirmado.
- CRA-RS/IEPTB-RS, leiaute exato, PSP/convênio do piloto, lei municipal de Maximiliano de Almeida/RS — **PENDENTES** `[a confirmar — doc oficial]`.

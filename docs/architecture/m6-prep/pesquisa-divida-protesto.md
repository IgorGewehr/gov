# M6 — Pesquisa: Dívida Ativa, CDA, Protesto Extrajudicial e Execução Fiscal

> Preparatório (m6-prep). Tema: inscrição em Dívida Ativa, CDA, cobrança administrativa,
> protesto extrajudicial (CRA/cartório), execução fiscal, prescrição e integrações de
> arrecadação (PIX / CNAB).
>
> Constituição §16: nada de inventar alíquota/regra/leiaute — tudo com FONTE ou
> `[a confirmar — obter doc oficial]`. Alíquotas/encargos/regras SEMPRE parametrizáveis por
> tenant (lei municipal), nunca hardcoded.
>
> Estado do módulo: JÁ EXISTE `DividaAtiva` (domínio + `EmitirCda` + `InscreverEmDividaAtiva`
> + `QuitarDivida` + rules `DividaAtiva.rules.md` v1.0.0). Esta pesquisa **aprofunda** os
> pontos ainda não cobertos (protesto, execução fiscal, prescrição intercorrente, arrecadação).

---

## 1. Inscrição em Dívida Ativa

- Crédito tributário **vencido e não pago** após o vencimento e esgotada a fase de cobrança
  administrativa ordinária → constitui-se a Dívida Ativa via **Termo de Inscrição** (TIDA),
  do qual se extrai a CDA.
- Requisitos do **Termo de Inscrição** = mesmos da CDA (ver §2). A inscrição confere ao
  crédito **presunção (relativa) de certeza e liquidez** e o reveste de exequibilidade.
- A inscrição é o **marco** que viabiliza CDA → protesto/execução. (Ver `InscreverEmDividaAtiva`
  já existente — domínio confere isso a partir de `Lancamento` vencido em aberto.)

FONTES:
- Lei 6.830/80 (LEF), art. 2º e §§ — https://www.planalto.gov.br/ccivil_03/leis/l6830.htm `[a confirmar — confirmar texto consolidado no Planalto]`
- Requisitos TIDA/CDA — https://trilhante.com.br/curso/administracao-tributaria/aula/requisitos-do-tida-e-cda-1

---

## 2. CDA — Certidão de Dívida Ativa (requisitos legais)

**Base: Lei 6.830/80, art. 2º, §5º** — o Termo de Inscrição (e a CDA dele extraída) deve conter:

| Inc. | Requisito | Campo sugerido (parametrizável) |
|---|---|---|
| I | Nome do devedor, co-responsáveis e, se conhecido, domicílio/residência | `Devedor`, `CoResponsaveis[]`, `Domicilio` |
| II | **Valor originário**; termo inicial e **forma de cálculo de juros de mora e demais encargos** previstos em lei/contrato | `ValorOriginario`, `RegraJurosMora` (por tenant) |
| III | Origem, natureza e **fundamento legal** da dívida | `OrigemNatureza`, `FundamentoLegal` (lei municipal) |
| IV | Se aplicável: sujeição a **atualização monetária**, fundamento legal e termo inicial | `RegraCorrecaoMonetaria` (por tenant) |
| V | **Data e número da inscrição** no Registro de Dívida Ativa | `DataInscricao`, `NumeroCda` (já existe) |
| VI | **Nº do processo administrativo / auto de infração**, se neles apurado o valor | `ProcessoAdministrativo` |

- **Nulidade**: ausência de qualquer requisito gera nulidade da CDA (e da execução).
  Presunção de certeza/liquidez é **afastável por prova inequívoca** (art. 3º, parág. único, LEF).
- **Emenda/substituição da CDA**: permitida até a decisão de 1ª instância (art. 2º, §8º, LEF),
  para erro material/formal — devolve prazo de embargos quanto à parte modificada.
  → Implicar máquina de estados que permita `SubstituirCda` versionando a CDA, sem perder
  histórico (auditoria). `[a confirmar — modelar versionamento de CDA no domínio]`

FONTES:
- LEF art. 2º §5º (incisos I–VI) e §8º — https://www.jusbrasil.com.br/topicos/11734425/inciso-ii-do-paragrafo-5-do-artigo-2-da-lei-n-6830-de-22-de-setembro-de-1980
- PGFN — alterações na CDA e ajuizamento — https://www.gov.br/pgfn/pt-br/cidadania-tributaria/por-assunto/execucao-fiscal/alteracoes-na-certidao-de-divida-ativa-cda
- Nulidade por falta de origem/natureza — https://tributarionosbastidores.com.br/2016/08/cda-2/
- (confirmar texto-base) Lei 6.830/80 no Planalto `[a confirmar — obter art. 2º §5º direto do Planalto, não Jusbrasil]`

---

## 3. Cobrança administrativa (pré-protesto / pré-execução)

- Fase amigável: notificação do devedor, oferta de **parcelamento/REFIS** (suspende
  exigibilidade e **interrompe** prescrição — CTN art. 174, parág. único, IV).
- Geração de guia de pagamento (DAM/PIX — §6).
- Boa prática: política de cobrança escalonada por **faixa de valor** e **idade da dívida**,
  parametrizável por tenant (ex.: <R$ X → só protesto; ≥ R$ X → execução). Limiar de
  ajuizamento é decisão municipal. `[a confirmar — definir limites no cadastro do tenant]`

---

## 4. Protesto extrajudicial da CDA (Lei 9.492/97 + CRA)

### 4.1 Fundamento legal
- **Lei 9.492/97, art. 1º, parág. único** inclui expressamente entre os títulos protestáveis
  as **CDAs da União, Estados, DF, Municípios** e respectivas autarquias/fundações.
- **STJ — não exige lei municipal autorizadora**: protesto de título é matéria de direito
  civil/comercial (competência privativa da União, CF art. 22, I); a norma federal é
  autoaplicável. (REsp 1.895.557, 1ª Turma, Min. Gurgel de Faria.)
- Parcelamento após protesto: discussão sobre quem arca com emolumentos de cancelamento
  (devedor x Fazenda) — ver Migalhas.

### 4.2 Fluxo operacional (CRA — Central de Remessa de Arquivos)
1. Município (apresentante) gera **arquivo de remessa** com as CDAs.
2. Envia à **CRA estadual** (IEPTB), que distribui aos tabelionatos competentes.
3. Cartório intima o devedor (prazo legal); não pago → **protesto lavrado**.
4. Cartório/CRA devolve **arquivo de retorno** com ocorrências.
5. Durante a janela protocolo→lavratura, o cartório (não o ente) recebe o pagamento;
   ente não deve receber/parcelar nesse intervalo.

### 4.3 Leiaute (CRA / IEPTB / FEBRABAN)
- Padrão: **CNAB 400** e **CNAB 240** (IEPTB também tem leiaute XML/WebService — CRA21).
- Documento de referência: "Protesto de Títulos — Arquivo Magnético v4.3" (CRA/FEBRABAN).
- Ocorrências de retorno relevantes: **protesto lavrado**, **pago/retirado**, **sustado**.
- Indício (leiaute estadual SP): apresentante insere marca para declarar CDA regularmente
  inscrita / termo emitido (ex.: letra "G" em posição do registro de remessa) — **varia por
  CRA estadual**. `[a confirmar — obter leiaute exato do CRA-RS / IEPTB-RS, posições e códigos]`

FONTES:
- Lei 9.492/97 — https://www.planalto.gov.br/ccivil_03/leis/l9492.htm
- STJ REsp 1.895.557 (sem lei local) — https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/19082021-Protesto-de-divida-pela-Fazenda-Publica-municipal-nao-depende-de-lei-local-autorizadora--decide-Primeira-Turma.aspx
- Leiaute Centralizado v4.3 (FEBRABAN/CRA) — https://www.protesto.com.br/cra/apresentantes/layouts/Layout_Centralizado_V43.pdf
- CRA21 — transmissão XML / WebService — http://manual.crabr.com.br/manual/integracao-por-webservice-cra21-api-3-2-2-2/
- Mapeamento de leiautes IEPTB/CRA (CNAB 240/400) — https://documentacao.senior.com.br/gestaoempresarialerp/5.10.2/integracoes/integracao-cra.htm
- Protesto e parcelamento — https://www.migalhas.com.br/depeso/433279/protesto-da-cda-e-o-parcelamento-da-divida-tributaria
- PGFN — protesto de CDA da União (modelo de referência) — https://www.gov.br/pgfn/pt-br/servicos/orientacoes-contribuintes/protesto-de-certidao-da-divida-ativa-da-uniao
- **CRA-RS / IEPTB-RS** (piloto Maximiliano de Almeida/RS) `[a confirmar — endpoint, leiaute, convênio do CRA do RS]`

---

## 5. Execução Fiscal (Lei 6.830/80) e Prescrição

### 5.1 Execução fiscal
- Ajuizada com base na CDA (título executivo extrajudicial). Já há `AjuizarExecucaoFiscal`
  no domínio existente — esta etapa do ERP **gera e exporta** a CDA/petição; o ajuizamento
  em si ocorre no PJe/eproc. `[a confirmar — integração de saída com PJe/eproc-RS é escopo?]`

### 5.2 Prescrição (CTN art. 174)
- Prazo: **5 anos** da constituição definitiva do crédito.
- **Interrupção** (art. 174, parág. único):
  - I — **despacho do juiz que ordena a citação** (redação pós **LC 118/2005**; antes era a
    citação pessoal). Retroage à **data do ajuizamento** (CPC art. 219 §1º / atual 240 §1º).
  - II — protesto judicial; III — ato que constitua em mora; IV — **reconhecimento do débito
    pelo devedor** (ex.: parcelamento/confissão).
- **Súmula 106/STJ**: demora na citação por culpa do mecanismo judiciário não prejudica o
  exequente. Pós-LC 118/05, demora na citação pessoal não desloca o marco (despacho).
- **Prescrição intercorrente** (LEF art. 40 + Súmula 314/STJ + REsp 1.340.553 repetitivo):
  não localizado devedor/bens → 1 ano de suspensão, depois corre o quinquênio intercorrente
  automaticamente. → Modelar **relógio de prescrição** por dívida (eventos que zeram/suspendem
  o prazo) é requisito de domínio. `[a confirmar — incluir agregado/serviço de prescrição]`

FONTES:
- CTN art. 174 — https://www.jusbrasil.com.br/topicos/10568330/artigo-174-da-lei-n-5172-de-25-de-outubro-de-1966 (confirmar no Planalto)
- Prescrição intercorrente (Súm. 314, art. 40 LEF) — https://barioniemacedo.adv.br/prescricao-intercorrente-no-direito-tributario/
- LEF na prática — https://juridico.ai/direito-tributario/lei-de-execucao-fiscal-6830/
- `[a confirmar — CTN art. 174 e LEF art. 40 direto do Planalto; REsp 1.340.553/RS (tema repetitivo) inteiro teor]`

---

## 6. Integrações de Arrecadação (PIX e CNAB)

### 6.1 PIX Cobrança (Bacen)
- Usar **PIX Cobrança com vencimento** (cobv) — suporta valor + juros/multa/desconto/abatimento
  por dia — adequado a DAM/dívida ativa. **QR Code dinâmico** (URL → payload no PSP).
- **DICT** mapeia a chave PIX → dados bancários do recebedor (ISPB/agência/conta).
- Conciliação via **webhook do PSP** (recebedor) ou consulta de cobrança.
- API municipal típica é a do **PSP/banco arrecadador** (ex.: BB "Arrecadação integrada ao PIX"),
  não a API Bacen diretamente. `[a confirmar — qual PSP/banco do município piloto]`

### 6.2 CNAB (retorno de arrecadação bancária)
- **CNAB 240** (FEBRABAN) — registro 240 bytes; arquivo = header + lotes (serviço/produto) +
  trailer. Numéricos à direita com zeros à esquerda; alfanuméricos à esquerda com brancos.
- Guias de tributo municipal seguem bloco de **código de barras de arrecadação (48 posições /
  4 blocos)** padrão FEBRABAN.
- Fluxo: ERP gera **remessa** (cobrança) → banco → **retorno** (baixa por pagamento) →
  conciliação automática contra `Lancamento`/`DividaAtiva`.
- Convênio (conta + tipo de serviço) é acordado banco↔ente. `[a confirmar — nº convênio,
  versão do leiaute e banco arrecadador do piloto]`

FONTES:
- FEBRABAN — Leiaute CNAB 240 v10.11 — https://cmsarquivos.febraban.org.br/Arquivos/documentos/PDF/Layout%20padrao%20CNAB240%20V%2010%2011%20-%2021_08_2023.pdf
- Manual de Padrões para Iniciação do Pix v2.9.0 (Bacen) — https://www.bcb.gov.br/content/estabilidadefinanceira/pix/Regulamento_Pix/II_ManualdePadroesparaIniciacaodoPix.pdf
- API PIX (Bacen, OpenAPI) — https://bacen.github.io/pix-api/ · repo https://github.com/bacen/pix-api
- API DICT — https://github.com/bacen/pix-dict-api
- BB — Arrecadação integrada ao PIX — https://www.bb.com.br/site/developers/bb-como-servico/api-arrecadacao-integrada-ao-pix/

---

## 7. Pendências consolidadas `[a confirmar — obter doc oficial]`

1. Lei 6.830/80 (art. 2º §5º, §8º; art. 3º; art. 40) e CTN art. 174 — texto consolidado do **Planalto** (substituir links Jusbrasil).
2. **REsp 1.340.553/RS** (repetitivo prescrição intercorrente) — inteiro teor / tese.
3. **CRA-RS / IEPTB-RS**: convênio, endpoint (CNAB 400/240 ou XML/WebService CRA21), posições de campo e códigos de ocorrência exatos.
4. Versionamento/substituição de CDA no domínio (art. 2º §8º LEF).
5. Agregado/serviço de **prescrição** (relógio de prazo com eventos de interrupção/suspensão).
6. **PSP/banco arrecadador** do município piloto: nº de convênio, versão do leiaute CNAB, credenciais PIX Cobrança.
7. Escopo de integração de **saída com PJe/eproc-RS** para execução fiscal (sim/não no M6).
8. Legislação **municipal-tipo** (Maximiliano de Almeida/RS): regras de juros/multa/correção, REFIS, limiar de ajuizamento/protesto — todos parametrizáveis por tenant (§16).

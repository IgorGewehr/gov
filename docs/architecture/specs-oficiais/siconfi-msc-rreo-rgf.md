# SICONFI (STN) — MSC, RREO, RGF e DCA

> Especificação oficial vigente (exercício **2026**) extraída das fontes do Tesouro Nacional / SICONFI.
> Base normativa central: **art. 48, §2º e art. 51 da LRF (LC 101/2000)**, regulamentados pela **Portaria STN nº 896/2017** e pela **Portaria STN nº 642, de 20/09/2019** (institui a MSC).
> Piloto Tensorroot.Gov: Maximiliano de Almeida/RS — o ente envia a MSC ao SICONFI; o TCE-RS recebe os dados via integração com a STN (acordo de cooperação) e/ou pela própria homologação no SICONFI.
> Última verificação das fontes: 2026-06-22.

---

## 1. O que é a MSC (Matriz de Saldos Contábeis)

Estrutura **padronizada** para recebimento de informações contábeis e fiscais dos entes da Federação, com três finalidades:
consolidação das contas nacionais; geração de estatísticas fiscais (acordos internacionais); e **elaboração automática das declarações** (Demonstrações Contábeis DCASP e Demonstrativos Fiscais da LRF — RREO, RGF, DCA).

- Produzida a partir do **PCASP Estendido** (Anexo III da IPC 00). Usa **apenas as contas de último nível** de detalhamento.
- A MSC **alimenta/gera os relatórios**: ao carregar a última MSC de um período, o SICONFI converte automaticamente os dados em **rascunho** do RREO/RGF/DCA (regras de mapeamento do MCASP + MDF). O ente revisa, ajusta (com nota explicativa) e **finaliza/homologa** a declaração.
- Padrão técnico de base: **XBRL** (eXtensible Business Reporting Language).

---

## 2. Estrutura / campos da MSC

Cada **linha** da MSC = uma combinação de **conta contábil** + **informações complementares**, com os detalhamentos de valor.

### 2.1 Conta Contábil
- Contas de último nível do **PCASP Estendido**. O ente só pode detalhar em níveis **posteriores** ao padronizado (vedado alterar os 6 primeiros níveis).
- **5º nível (subtítulo)** das contas de Natureza de Informação Patrimonial = identificação de saldos recíprocos para consolidação. Classificação:
  `1 = Consolidação · 2 = Intra OFSS · 3 = Inter OFSS–União · 4 = Inter OFSS–Estados · 5 = Inter OFSS–Municípios`.
- Contas fora do PCASP Estendido → **"De-Para" (Mapear Contas)** no SICONFI (uma vez por ano) ou no sistema do ente.

### 2.2 Informações Complementares (quadro resumo oficial)
| Nº | Código | Informação | Dígitos | Formato | Observação |
|----|--------|-----------|---------|---------|------------|
| 1 | **PO** | Poder ou Órgão | 5 | `XXXXX` | 2 dígitos = Poder, 3 = Órgão (art. 20 LRF). Associada a TODAS as contas; permite gerar RGF de todos os poderes a partir da MSC do Executivo e destacar o RPPS. |
| 2 | **FP** | Atributo do Superávit Financeiro | 1 | `X` | 1=Financeiro, 2=Permanente (Lei 4.320/64; apuração do superávit no BP). |
| 3 | **DC** | Dívida Consolidada | 1 | `X` | `1` = parcela que **não** compõe a DC (financiamentos/op. crédito < 12 meses). |
| 4 | **FR** | Fonte/Destinação de Recursos | 4 | `XXXX` | 1º dígito: exerc. atual(1)/anterior(2); 2º–4º: cód. fonte. Portaria STN/SOF nº 20/2021 + Portaria nº 710/2021 (obrigatório desde 2023). |
| 5 | **CO** | Cód. de Acompanhamento da Execução Orçamentária | 4 | `XXXX` | Complementa a Fonte de Recursos (Anexo II Portaria 710/2021). |
| 6 | **NR** | Natureza da Receita | 8 | `XXXXXXXX` | Portaria Interm. STN/SOF 163/2001; ementário Portaria SOF/ME 5.118/2021 e Portaria STN 831/2021. |
| 7 | **ND** | Natureza da Despesa | 8 | `XXXXXXXX` | `c.g.mm.ee.dd` (Portaria 163/2001); `dd` (subelemento) facultativo → "De-Para". |
| 8 | **FS** | Classificação Funcional (Função+Subfunção) | 5 | `XXXXX` | Função(2)+Subfunção(3), Portaria MOG nº 42/1999. |
| 9 | **AI** | Ano de Inscrição de Restos a Pagar | 4 | `XXXX` | Ano de inscrição em RP (ex.: quadro de RP do Demonstrativo da Saúde no RREO). |

> No **CSV de 2026**, o registro deve conter obrigatoriamente **6 conjuntos** de informações complementares (pares `ICx`/`TIPOx`), mesmo que nem todos sejam usados — porque há contas que exigem detalhamento por até 6 tipos.

### 2.3 Detalhamento dos valores (por linha)
- **Valor**: sem separador de milhar, separador decimal = ponto `.`; **valores negativos não são permitidos**.
- **Tipo_Valor** (taxonomia XBRL GL): `beginning_balance` (saldo inicial) · `period_change` (movimentação do período) · `ending_balance` (saldo final).
- **Natureza_Valor**: `D` (débito) ou `C` (crédito).

### 2.4 Cabeçalho / identificação
- **Cód. Siconfi** = código IBGE do ente + sufixo `EX` (executivo).
- **Período** = `YYYY-MM` (MSC agregada) · `YYYY-13` (MSC de encerramento, no CSV).

---

## 3. Tabelas de apoio (codificações normativas)
- **PCASP Estendido** — IPC 00 Anexo III (contas).
- **Fonte/Destinação de Recursos (FR)** — Portaria Conjunta STN/SOF nº 20/2021 + Portaria nº 710/2021 (rol válido p/ 2026: ver atualização nº 11/2025).
- **Código de Acompanhamento (CO)** — Anexo II Portaria nº 710/2021.
- **Natureza da Receita (NR)** — Portaria Interm. STN/SOF 163/2001; ementário Portaria SOF/ME 5.118/2021 e Portaria STN 831/2021.
- **Natureza da Despesa (ND)** — Portaria Interm. STN/SOF 163/2001.
- **Classificação Funcional (FS)** — Portaria MOG nº 42/1999.
- **Poder/Órgão (PO)** — classificação própria do SICONFI (art. 20 LRF).
- Todas consolidadas no **arquivo "Leiaute MSC 2026" (Anexo II da Portaria STN 642/2019)** — [obter arquivo oficial].

---

## 4. Tipos de MSC, formato e periodicidade
| Tipo | Periodicidade | Período | Gera | Observação |
|------|---------------|---------|------|------------|
| **MSC Agregada** | **Mensal** | mês a mês (`YYYY-MM`) | **RREO + RGF** | Agregada (sem exclusão de saldos recíprocos), separada por PO. Dezembro deve conter inscrições de RP (contas classe 6: 6.2.2.1.3.05/06/07, 6.3.1.7.1, 6.3.1.7.2, 6.3.2.7.0). |
| **MSC Encerramento** | **Anual** | mês 13 (`YYYY-13`) | **DCA** | Mesmo leiaute (Anexo II). Saldo inicial = saldo final da MSC Agregada de dezembro; contas de resultado zeradas após apuração. Decreto 10.540/2020 + NT SEI 11577/2019/ME. |

**Formatos de envio da MSC** (arquivo **zipado/compactado**):
1. **Arquivo CSV** — leiaute adaptado do XBRL GL; o SICONFI converte o CSV em instância XBRL GL.
2. **Instância XBRL GL** — taxonomia genérica do Consórcio XBRL Internacional (documento específico no site do SICONFI).

**Prazo de envio da MSC:** mensal, até o **último dia do mês seguinte** ao mês de referência (exercício corrente + 4 anteriores). MSC de encerramento do exercício: até o **último dia de março** do ano seguinte.

**Reenvio:** uma vez carregada, a MSC **não pode ser excluída**, mas pode ser **reenviada/substituída** — salvo proibição do respectivo Tribunal de Contas.

---

## 5. Relatórios fiscais — periodicidade, prazos e formato

| Relatório | Base legal | Periodicidade | Prazo de envio/publicação | Gerado a partir de |
|-----------|-----------|---------------|---------------------------|--------------------|
| **RREO** — Relatório Resumido da Execução Orçamentária | art. 165 §3º CF; arts. 52–53 LRF | **Bimestral** (municípios <50 mil hab. podem optar por semestral, art. 63 LRF) | até **30 dias após o encerramento de cada bimestre** (a partir do 1º bim. jan-fev) | **MSC Agregada** |
| **RGF** — Relatório de Gestão Fiscal | arts. 54–55 LRF | **Quadrimestral** (municípios <50 mil hab. podem optar por semestral, art. 63 LRF) | até **30 dias após o encerramento de cada quadrimestre** (a partir do 1º quad. jan-abr) | **MSC Agregada** |
| **DCA** — Declaração de Contas Anuais (Balanço Anual) | art. 51 LRF | **Anual** | conforme calendário STN do exercício seguinte (consolidação nacional) | **MSC Encerramento** |

- Assinaturas no SICONFI exigem **certificação digital** (relevante para o piloto: certificado A1/A3 do ente).
- Gerar **rascunho ≠ entregar**: é obrigatório **finalizar/homologar** a declaração no SICONFI. Os relatórios homologados devem refletir os mesmos demonstrativos enviados ao Tribunal de Contas; ajustes ao rascunho automático devem constar em **notas explicativas**.
- Planilhas auxiliares de edição no SICONFI são salvas em **`.XLS`**.

---

## 6. API SICONFI (dados abertos) — extração programática

- **Base:** `http://apidatalake.tesouro.gov.br/ords/siconfi/tt/` · **Docs/Swagger:** `http://apidatalake.tesouro.gov.br/docs/siconfi/`
- **REST, resposta JSON**, sem autenticação (dados públicos). **Paginação padrão: 5.000 itens/página.** Limite recomendado: **1 requisição por segundo**.
- Endpoints principais:
  - `/rreo` (params: `an_exercicio`, `nr_periodo`, `co_tipo_demonstrativo`, `no_anexo`, `co_esfera`, `id_ente`)
  - `/rreo-simplificado`
  - `/rgf` (8 params, inclui `co_poder`)
  - `/rgf-simplificado`
  - `/dca` (params: `an_exercicio`, `id_ente`, `no_anexo`)
  - `/msc_patrimonial`, `/msc_orcamentaria`, `/msc_controle` (todos os 6 params obrigatórios: `an_referencia`, `me_referencia`, `id_tv`, `id_tipo_matriz`, `classe_conta`, `id_ente`)
  - `/anexos-relatorios`, `/entes`, `/extrato_entregas` (params: `id_ente`, `an_referencia`)
- `id_ente` = **código IBGE** do município (Maximiliano de Almeida/RS = código IBGE de 7 dígitos — confirmar no endpoint `/entes`).
- A API é de **consulta** (download dos dados já homologados); o **envio/upload** da MSC e a homologação dos relatórios são feitos pela aplicação web do SICONFI (upload de arquivo zipado CSV/XBRL GL na área restrita do ente).

---

## 7. Fluxo de geração no SICONFI (resumo operacional)
1. Carregar **plano de contas** do ente (`.xls`, código+descrição) — só se houver contas fora do PCASP Estendido.
2. Carregar **informações complementares** do ente (`.xls`).
3. Fazer o **"De-Para"** (1×/ano) — no SICONFI ou no próprio sistema do ente (neste caso pula etapas 1-2).
4. **Carregar a MSC** mensal (CSV ou XBRL GL, zipado) com os filtros corretos (exercício, tipo de balancete, periodicidade, período).
5. O SICONFI **converte automaticamente** os dados em **rascunho** de RREO/RGF/DCA ao receber a última MSC do período.
6. Revisar rascunho, ajustar com **notas explicativas**, assinar com **certificado digital** e **finalizar** a declaração.

---

## 8. Fontes oficiais (URLs)
- MSC — Regras Gerais 2026 (Anexo I Portaria STN 642/2019): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- STN divulga layout da MSC: https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=5102
- MSC — Tesouro Transparente: https://www.tesourotransparente.gov.br/consultas/consultas-siconfi/matriz-de-saldos-contabeis-msc
- RGF — Regras Gerais e Instruções de Preenchimento 2026: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Regras_Gerais_e_Instrucoes_de_preenchimento_RGF_04032026.pdf
- RREO — Regras Gerais e Instruções de Preenchimento 2026: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Regras_Gerais_e_Instrucoes_de_preenchimento_RREO.pdf (URL inferida — confirmar nome exato)
- API SICONFI — docs: http://apidatalake.tesouro.gov.br/docs/siconfi/
- API SICONFI — Tesouro Transparente: https://www.tesourotransparente.gov.br/consultas/consultas-siconfi/siconfi-api-de-dados-abertos
- Catálogo de APIs gov (Conecta): https://www.gov.br/conecta/catalogo/apis/siconfi-extratos-das-declaracoes-contabeis
- Portal SICONFI: https://siconfi.tesouro.gov.br/

---

## 9. Pendências — [obter arquivo oficial]
- **[obter arquivo oficial]** Leiaute MSC 2026 — **Anexo II da Portaria STN nº 642/2019** (correlação conta×informação complementar; layout exato de colunas CSV/XBRL GL). Buscar em SICONFI > Publicações > MSC. **Documento mais crítico para implementar o gerador de MSC.**
- **[obter arquivo oficial]** Documento técnico da **Instância XBRL GL** (taxonomia, estrutura) — site do SICONFI.
- **[obter arquivo oficial]** **Portaria STN nº 642/2019** (texto integral) e suas atualizações para 2026.
- **[obter arquivo oficial]** Documento de **validações da MSC 2026** (a STN publicará documento específico — citado nas Regras Gerais, ainda não localizado).
- **[obter arquivo oficial]** **NT SEI nº 11577/2019/ME** (preenchimento da MSC de encerramento) e **Decreto nº 10.540/2020** (SIAFIC).
- **[obter arquivo oficial]** **MDF** (Manual de Demonstrativos Fiscais) e **MCASP** vigentes — regras de mapeamento MSC→RREO/RGF/DCA e leiautes de cada anexo do RREO/RGF.
- **[obter arquivo oficial]** Confirmar nome/URL exato do **PDF de Regras Gerais do RREO 2026** (URL acima inferida pelo padrão da do RGF).
- **[confirmar]** **código IBGE de Maximiliano de Almeida/RS** via endpoint `/entes` da API SICONFI.
- **[obter]** Swagger/contrato OpenAPI completo da API em `http://apidatalake.tesouro.gov.br/docs/siconfi/` (parâmetros e enums de `co_tipo_demonstrativo`/`no_anexo`).

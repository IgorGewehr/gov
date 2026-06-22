# MAPA DE PRESTAÇÃO DE CONTAS — Tensorroot.Gov

> **Para CADA módulo/dado: PARA ONDE vai** — ESTADO (TCE-RS) e FEDERAL/UNIÃO (STN/SICONFI, eSocial, DATASUS, INEP/FNDE, MDS, PNCP) — com **periodicidade** e **formato**.
> Derivado de `docs/diagnostico/REQUISITOS-GOVTECH.md`, `docs/architecture/contabilidade-pcasp-tce.md` e `docs/diagnostico/GAP-E-ROADMAP.md`, confirmado em fontes oficiais (jun/2026).
>
> **`[OFICIAL]`** = exige **leiaute/versão vigente** do exercício (CLAUDE.md §7/§16 — nunca hard-coded; validar antes de gerar remessa).
> **Princípio (spec contábil):** a **contabilidade (PCASP) é a FONTE**. Quase tudo no eixo fiscal nasce do `LancamentoContabil` → `Balancete` → **MSC**, e da **MSC** derivam RREO/RGF/DCA (SICONFI) e alimentam a remessa TCE-RS.
> **Multi-tenant:** Executivo e Legislativo do mesmo município são tenants DISTINTOS (CNPJs distintos) → cada um presta contas separadamente ao TCE-RS e ao SICONFI.

---

## 0. VISÃO DE ALTO NÍVEL — quem manda o quê para onde

```
 MÓDULO (origem do dado)              ESTADO (TCE-RS)                     FEDERAL/UNIÃO
 ─────────────────────────────────────────────────────────────────────────────────────────
 Financas (PCASP/MSC) ───────────►  SIAPC/PAD (mensal, txt larg.fixa)  ─► SICONFI: MSC→RREO/RGF/DCA (STN)
 RecursosHumanos (folha) ─────────►  Folha Res.1099 (mensal, TCE_4810)  ─► eSocial (S-1.3, mensal) + Ponto AFD/AEJ
 Tributos (receita/dív.ativa) ────►  alimenta SIAPC (receita) ──────────► (ADN passivo: ingestão NFS-e) + SICONFI
 Administracao (licitações) ──────►  licitações/contratos (SIAPC/LicitaCon)─► PNCP (eficácia do contrato)
 Patrimonio (bens/depreciação) ───►  via contabilidade→SIAPC ───────────► via MSC→SICONFI
 Saude ───────────────────────────►  (controle SUS/saúde→SIAPC) ────────► SIOPS, RNDS/SISAB/CNES/SIA-SIH/SI-PNI/BNAFAR
 Educacao ────────────────────────►  (controle MDE→SIAPC) ──────────────► SIOPE + Educacenso/INEP + PNAE/PNATE (FNDE)
 AssistenciaSocial ───────────────►  (controle SUAS→SIAPC) ─────────────► Rede SUAS/MDS: CadÚnico, RMA, Censo SUAS, SUASWeb
 Protocolo ───────────────────────►  documentos/processos sob demanda ──► e-SIC/LAI (não há remessa periódica federal)
 Legislativo ─────────────────────►  contas Câmara art.29-A (SIAPC) ────► (LexML/dados abertos — sem remessa obrigatória)
 Transparencia ───────────────────►  É O TRANSMISSOR (SIAPC/PAD) ────────► É O TRANSMISSOR (SICONFI/MSC) + Portal LAI
```

---

## 1. FINANCAS — Contabilidade PCASP, Orçamento, Ciclo da Despesa

### → ESTADO (TCE-RS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SIAPC/PAD** (e-Validador) | Execução orçamentária/financeira/contábil/patrimonial via RDI (saldos PCASP, empenho→liquidação→pagamento, restos a pagar) | **Mensal**, até **30 dias corridos** após o mês (desde jan/2019) | Texto **largura fixa**, 1 registro/linha, **CR/LF**, sem packed/binário; conteúdo acumulado 1º-jan→data ref.; **Código de Remessa** único; validado e autenticado pelo **PAD** (v25.x/2025 → 26.x/2026) | `[OFICIAL]` MT-ASCE Vol. IV (Elenco de Contas) e Vol. V (Resumo Leiaute Dados à Disposição) |
| **Contas Anuais/Ordinárias** | Balanço Geral + DCASP (7 demonstrações) + documentos | **Anual** | Conforme Res. TCE-RS 1134/2020 | `[OFICIAL]` Res. 1134/2020 (datas a confirmar por exercício) |
| **SICOE** (obras) | Acompanhamento de obras públicas | **Trimestral** (último dia útil do mês seguinte ao trimestre) `[a confirmar]` | Leiaute SICOE | `[OFICIAL]` |

### → FEDERAL/UNIÃO (STN/SICONFI)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SICONFI — MSC Agregada** | Matriz de Saldos Contábeis (conta PCASP + saldo + natureza da informação + indicador superávit F/P + info complementares) | **Mensal**, até o **último dia do mês seguinte** | XBRL/arquivo + API SICONFI | `[OFICIAL]` Portaria STN 642 (Regras Gerais MSC, anexo anual 2026) + Portaria STN 896/2017 |
| **SICONFI — MSC de Encerramento** | MSC referente a dezembro (encerramento do exercício) → rascunho da DCA | **Anual**, até **31/mar** do exercício seguinte | XBRL/arquivo + API | `[OFICIAL]` |
| **SICONFI — RREO** (Rel. Resumido Execução Orçamentária) | Derivado da MSC agregada | **Bimestral**, **30 dias** após o bimestre | Declaração SICONFI (gerada da MSC) | `[OFICIAL]` LRF art.52-53; Port. STN 896/2017 |
| **SICONFI — RGF** (Rel. Gestão Fiscal) | Derivado da MSC; limites pessoal/dívida | **Quadrimestral** (municípios; semestral facultativo p/ <50k hab.), **30 dias** após | Declaração SICONFI | `[OFICIAL]` |
| **SICONFI — DCA** (Decl. Contas Anuais) | Derivada da MSC de encerramento | **Anual** (~30/abr exercício seguinte) `[a confirmar data]` | Declaração SICONFI | `[OFICIAL]` |

> **Fundamento:** Lei 4.320/1964, LC 101/2000 (LRF) + LC 131/2009, MCASP 11ª ed. (vigência jan/2025), PCASP. Falha de envio SICONFI → impede transferências voluntárias (bloqueio CAUC).

---

## 2. RECURSOS HUMANOS — Folha, eSocial, Ponto

### → ESTADO (TCE-RS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **Remessa de Folha** (SIAPC/PAD) | Cadastro de servidores, vantagens/descontos, totalizadores, classes salariais e base legal | **Mensal**, até **30 dias corridos** após o mês (desde jan/2019) | Texto largura fixa, arquivo **`TCE_4810.TXT`**; valida no PAD | `[OFICIAL]` Res. TCE-RS **1099/2018** |

### → FEDERAL/UNIÃO
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **eSocial** (RFB/Caixa/INSS/MTE) | Eventos: S-1000 + tabelas; **S-1200** (celetista/comissionado/temporário RGPS), **S-1202** (estatutário/RPPS), **S-1207** + S-2400/2405/2410/2416 (benefícios RPPS); **S-1299** fecha a competência | **Mensal** — periódicos até **dia 15** do mês seguinte (antes do S-1299); admissão S-2200 até véspera do início; desligamento S-2299 até 10 dias `[a confirmar]` | XML assinado **A1 ICP-Brasil** contra XSD; WebService SOAP (Produção + Produção Restrita) | `[OFICIAL]` eSocial **S-1.3** |
| **Ponto eletrônico** (fiscalização MTE) | AFD (assinado) e AEJ — guarda/fiscalização (não há "envio" periódico; exibição sob demanda) | Contínuo / sob demanda | Arquivos **AFD** + **AEJ**; imutabilidade das marcações; REP-C/REP-A/REP-P | `[OFICIAL]` Portaria MTP **671/2021** (AFDT/ACJEF eliminados) |
| **CADPREV / DRPPS** (RPPS) | Apuração previdenciária (correlação com S-1202/S-1207) | Periódico `[a confirmar]` | Layout CADPREV | `[OFICIAL]` `[a confirmar]` |

> A folha é **reconciliável** com Finanças/Contabilidade → impacta a remessa SIAPC e a MSC.

---

## 3. TRIBUTOS — IPTU/ISS/ITBI/Taxas, Dívida Ativa, NFS-e

### → ESTADO (TCE-RS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SIAPC/PAD** (flag Receita) | Receita prevista→lançada→arrecadada→recolhida; dívida ativa inscrita/baixada (alimenta a contabilidade → MSC) | **Mensal** (junto da remessa SIAPC) | Texto largura fixa | `[OFICIAL]` MT-ASCE Vol. V (registros de receita) |

### → FEDERAL/UNIÃO
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **ADN / Receita Federal (NFS-e Nacional)** | **INGESTÃO PASSIVA** — o Worker `NfseSync` **baixa** XMLs de NFS-e dos CNPJs do tenant (não emitimos/assinamos) p/ painel fiscal e Dívida Ativa | **Diária** (download) | API JSON / DF-e XML assinado; mTLS por certificado ICP-Brasil; schemas NFSe XSD v1.01 (fev/2026) | `[OFICIAL]` obrigatória desde 01/01/2026 (ingestão, não emissão) |
| **SICONFI** (via Finanças/MSC) | Receita arrecadada compõe a MSC → RREO/RGF/DCA | conforme §1 | conforme §1 | `[OFICIAL]` |
| **Reforma Tributária (EC 132/LC 214)** | Coexistência ISS↔IBS; 2026 destaque-teste CBS 0,9%/IBS 0,1% (informativo); 2033 extinção ISS→IBS | conforme cronograma de transição | a definir por exercício | `[OFICIAL]` `[a confirmar leiautes]` |

> Demais destinos tributários **não-prestação-de-contas** (bancos FEBRABAN/PIX/CNAB, CRA/IEPTB protesto, cartórios ITBI, PGDAS-D/Simples) ficam fora deste mapa (são arrecadação/cobrança, não remessa a controle externo).

---

## 4. ADMINISTRACAO — Compras e Licitações (Lei 14.133/2021)

### → FEDERAL/UNIÃO
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **PNCP** (Portal Nacional de Contratações Públicas) | Editais, atas de registro de preços, contratos e aditivos, contratações diretas, **PCA** (Plano de Contratações Anual) | **Por evento** — **condição de eficácia** do contrato (art. 94): **até 10 dias úteis** (contratação direta) / ~20 dias úteis (licitação) da assinatura `[a confirmar literal art.94]` | API REST/JSON UTF-8 + **JWT (expira 1h)**; sem entrada manual | `[OFICIAL]` Lei 14.133/2021; valores atualizados Dec. 12.807/2025 (IPCA 2026) |

> **Regra de produto:** sem **nº de controle PNCP** confirmado → **bloquear execução financeira** (empenho/pagamento) em Finanças. SICAF (níveis I-IV) p/ fornecedores.

### → ESTADO (TCE-RS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SIAPC/PAD (LicitaCon)** | Dados de licitações, contratos e aditivos para acompanhamento do TCE-RS | **Por evento / mensal** `[a confirmar]` | Leiaute LicitaCon/SIAPC | `[OFICIAL]` `[a confirmar leiaute LicitaCon vigente]` |

---

## 5. PATRIMONIO — Bens, Depreciação, Frota, Almoxarifado

### → ESTADO + FEDERAL (sempre via contabilidade)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SIAPC/PAD** (via Finanças) | Bens patrimoniais, depreciação (MCASP), incorporação/baixa → contrapartida contábil (VPD/VPA) → balancete patrimonial | **Mensal** (compõe a remessa SIAPC) | Texto largura fixa (registros patrimoniais) | `[OFICIAL]` |
| **SICONFI** (via MSC) | Saldos patrimoniais compõem a MSC → Balanço Patrimonial/DCA | conforme §1 | conforme §1 | `[OFICIAL]` |

> Patrimônio **não tem remessa própria**: tudo flui pela contabilidade (gerar a contrapartida contábil em Finanças é o gap identificado no ROADMAP).

---

## 6. SAUDE — risco fiscal: dado clínico = receita

### → FEDERAL/UNIÃO (DATASUS / Ministério da Saúde / FNS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **e-SUS APS → SISAB** | Produção da Atenção Primária (fichas CDS/PEC) — base do cofinanciamento APS | **Contínua** + avaliação **quadrimestral** (15 indicadores) | LEDI/Thrift (e-SUS APS) | `[OFICIAL]` cofinanciamento Port. GM/MS **3.493/2024** |
| **CNES/SCNES** | Cadastro de estabelecimentos e profissionais | **Mensal** (~5º dia útil) | Sistema CNES | `[OFICIAL]` — falta 3 meses → suspensão de repasse (Port. 708/2007) |
| **SIA/SIH** (BPA/APAC/AIH) | Produção ambulatorial e hospitalar | **Mensal** (~5º dia útil) | Arquivos SIA/SIH (DATASUS) | `[OFICIAL]` Port. 708/2007 |
| **RNDS** (barramento nacional) | Eventos clínicos: imunização, exames, dispensação, sumário de alta | **Contínua / por evento** | **HL7 FHIR R4** via HTTPS + certificado ICP-Brasil (e-CNPJ) | `[OFICIAL]` FHIR-first |
| **SI-PNI** (imunização) | Evento de imunização | **Contínua** | FHIR via RNDS (integrado desde 01/06/2023) | `[OFICIAL]` |
| **BNAFAR / e-SUS AF** (farmácia) | Movimentação de assistência farmacêutica | **Mensal** | e-SUS AF (substitui Hórus — Port. GM/MS 11.585 de 16/06/2026, janela 180 dias) | `[OFICIAL]` |
| **SIOPS** (orçamento em saúde) | Receitas/despesas em saúde; mínimo constitucional (15% ASPS) | **Bimestral** (homologação) `[a confirmar]` | Sistema SIOPS / declaração | `[OFICIAL]` |

### → ESTADO (TCE-RS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SIAPC/PAD** (via Finanças) | Aplicação mínima em saúde (gasto função 10) conciliada com empenho/liquidação/pagamento | **Mensal** | Texto largura fixa | `[OFICIAL]` |

---

## 7. EDUCACAO — risco fiscal: descumprimento suspende repasses

### → FEDERAL/UNIÃO (INEP / FNDE)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **Educacenso / Censo Escolar (INEP)** | Matrícula, turmas, docentes, situação do aluno — fonte de verdade do FUNDEB/PNAE/PNATE | **Anual**, 2 etapas (1ª matrícula ~27/05-31/07/2026; 2ª situação ~01/02-12/03/2027) | Migrador INEP (layout) | `[OFICIAL]` `[a confirmar layout migrador]` |
| **SIOPE (FNDE)** | Receitas e despesas da educação; mínimo MDE (25% art.212 CF) + FUNDEB | **Bimestral**, **30 dias** após o bimestre (6º = **30/01**) | Sistema SIOPE; validação **SIOPE-MAVS** (Secretário + Presidente CACS/FUNDEB) | `[OFICIAL]` — não envio → suspensão de transferências voluntárias + pendência CAUC |
| **PNAE** (merenda) | Prestação de contas; mín. 30% agricultura familiar | **Anual** (SiGPC ~15/02, prorrogável 30/04; parecer CAE no Sigecon ~45 dias) | SiGPC / Sigecon (FNDE) | `[OFICIAL]` Lei 11.947/2009 |
| **PNATE** (transporte rural) | Prestação de contas do transporte escolar | **Mensal** no SiGPC + **consolidação anual** | SiGPC (FNDE); parecer CACS/FUNDEB | `[OFICIAL]` Lei 10.880/2004 |

### → ESTADO (TCE-RS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SIAPC/PAD** (via Finanças) | Aplicação mínima MDE/FUNDEB (gasto função 12) conciliada com PCASP/MSC | **Mensal** | Texto largura fixa | `[OFICIAL]` |

---

## 8. ASSISTENCIA SOCIAL — Rede SUAS (MDS)

### → FEDERAL/UNIÃO (MDS / Dataprev)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **CadÚnico** (novo sistema Dataprev) | Cadastro de famílias (chave = **CPF**; NIS permanece) | Atualização ~2 anos ou mudança `[a confirmar]` | Sistema CadÚnico / layout import-export | `[OFICIAL]` |
| **CECAD 2.0** | Consulta/extração do CadÚnico | Sob demanda | Portal CECAD | `[OFICIAL]` |
| **RMA** (Registro Mensal de Atendimentos) | Atendimentos CRAS/CREAS | **Mensal** | Portal MDS / layout | `[OFICIAL]` |
| **Censo SUAS** | Estrutura/serviços da rede socioassistencial | **Anual** (abertura ~16/out) | Portal MDS | `[OFICIAL]` — não preencher suspende cofinanciamento |
| **SUASWeb** | Plano de Ação + Demonstrativo Sintético (aprovação do Conselho) | **Anual** | Portal SUASWeb | `[OFICIAL]` |
| **SISC** (SCFV) | Serviço de Convivência e Fortalecimento de Vínculos | **Periódico** `[a confirmar]` | Portal SISC | `[OFICIAL]` |
| **CadSUAS** | Cadastro da rede (pré-requisito dos demais) | Sob demanda | Portal CadSUAS | `[OFICIAL]` |
| **Prontuário Eletrônico SUAS** | Acompanhamento familiar (nome + NIS) | Contínuo | Sistema MDS | `[OFICIAL]` |

> Maioria é **preenchimento manual** no portal MDS → priorizar **import/export por layout** + consulta CadÚnico/CECAD. **CPF/NIS** = chave do cidadão. `[a confirmar layouts município→MDS]`

### → ESTADO (TCE-RS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SIAPC/PAD** (via Finanças) | Despesa da função 08 (Assistência Social) conciliada com PCASP/MSC | **Mensal** | Texto largura fixa | `[OFICIAL]` |

---

## 9. PROTOCOLO — Processo Administrativo Eletrônico

| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **e-SIC / LAI** (federal — sistema próprio do ente) | Resposta a pedidos de acesso à informação | **Por demanda** (20 dias + 10 prorrogáveis) | Portal e-SIC do tenant | Lei 12.527/2011 |
| **TCE-RS / CONARQ** | Documentos/processos sob requisição; guarda conforme TTDD | **Sob demanda** (não há remessa periódica) | PDF/A + RDC-Arq; assinatura Lei 14.063/2020 (3 níveis) | `[OFICIAL]` e-ARQ Brasil v2 (2022); Port. AN/MGI 174/2024 |

> **Não há remessa periódica de prestação de contas** própria do Protocolo — é guarda, temporalidade e disponibilização sob demanda.

---

## 10. LEGISLATIVO (Câmara — tenant distinto)

### → ESTADO (TCE-RS)
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SIAPC/PAD** (Câmara) | Contas da Câmara: folha de vereadores/servidores, despesas, **limites art. 29-A CF** (gasto do Legislativo, duodécimo) | **Mensal** (SIAPC) + **Anual** (contas) | Texto largura fixa (a Câmara é tenant/CNPJ próprio, presta contas como ente) | `[OFICIAL]` `[a confirmar leiaute específico Legislativo]` |

### → FEDERAL/UNIÃO
| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **SICONFI** (Câmara) | MSC própria da Câmara (CNPJ próprio) → consolida com o ente | conforme §1 | conforme §1 | `[OFICIAL]` |
| **eSocial / PNCP** | Folha (S-1.3) e licitações próprias da Câmara | conforme §2 / §4 | conforme §2 / §4 | `[OFICIAL]` |
| **LexML / Dados Abertos** | Proposições e leis (URN persistente) — **transparência**, não remessa obrigatória | Contínuo | XML LexML / CSV-JSON | benchmark SAPL |

---

## 11. TRANSPARENCIA — o TRANSMISSOR + Portal LAI

> Transparencia **não origina dado próprio de prestação de contas**: é o **canal de saída** (consome a MSC e os dados dos demais módulos e transmite).

| Destino | O que vai | Periodicidade | Formato | Marca |
|---|---|---|---|---|
| **TCE-RS (SIAPC/PAD/e-Validador/SICOE)** | Remessas geradas a partir do Balancete/MSC dos demais módulos | Mensal/Trimestral/Anual (conforme tipo) | Texto largura fixa + assinatura A1 + transmissão SICOE | `[OFICIAL]` |
| **SICONFI (STN)** | Declarações MSC/RREO/RGF/DCA | Mensal/Bimestral/Quadrimestral/Anual | XBRL/arquivo + API SICONFI | `[OFICIAL]` |
| **Portal da Transparência (LAI)** | Execução orçamentária **em tempo real** (até 1º dia útil subsequente ao registro), dados abertos | **Tempo real** | Portal web + CSV/JSON; retenção mín. 5 anos | `[OFICIAL]` LC 131/2009, Dec. 7.185/2010 |

---

## 12. CALENDÁRIO CONSOLIDADO (visão rápida de prazos)

| Obrigação | Esfera | Periodicidade | Prazo |
|---|---|---|---|
| SIAPC/PAD (execução) | TCE-RS | Mensal | 30 dias corridos após o mês |
| Folha (Res. 1099) `TCE_4810` | TCE-RS | Mensal | 30 dias corridos após o mês |
| SICOE (obras) | TCE-RS | Trimestral | último dia útil do mês seguinte `[a confirmar]` |
| Contas Anuais (Res. 1134) | TCE-RS | Anual | conforme exercício `[a confirmar]` |
| MSC Agregada | SICONFI | Mensal | último dia do mês seguinte |
| MSC Encerramento | SICONFI | Anual | 31/mar do exercício seguinte |
| RREO | SICONFI | Bimestral | 30 dias após o bimestre |
| RGF | SICONFI | Quadrimestral (semestral facultativo <50k hab.) | 30 dias após |
| DCA | SICONFI | Anual | ~30/abr `[a confirmar]` |
| eSocial periódicos (S-1299) | Federal | Mensal | dia 15 do mês seguinte |
| SIOPE | FNDE | Bimestral | 30 dias após (6º bim. = 30/jan) |
| SIOPS | MS | Bimestral | `[a confirmar]` |
| PNAE (SiGPC) | FNDE | Anual | ~15/fev (prorrog. 30/abr) |
| PNATE (SiGPC) | FNDE | Mensal + anual | conforme SiGPC |
| Educacenso | INEP | Anual (2 etapas) | matrícula mai-jul; situação fev-mar ano+1 |
| CNES/SIA/SIH | DATASUS | Mensal | ~5º dia útil |
| RMA | MDS | Mensal | conforme portal |
| Censo SUAS | MDS | Anual | abertura ~16/out |
| PNCP (eficácia contrato) | Federal | Por evento | 10 dias úteis (contratação direta) / ~20 (licitação) |
| Transparência tempo real | Federal/LRF | Contínua | 1º dia útil após o registro |

---

## FONTES (oficiais — validar versão do exercício antes de gerar remessa)

- SICONFI/STN — Regras Gerais MSC 2026 (Anexo I Port. STN 642): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- SICONFI/STN — Regras Gerais e Instruções RGF 2026: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Regras_Gerais_e_Instrucoes_de_preenchimento_RGF_04032026.pdf
- Tesouro Transparente — Matriz de Saldos Contábeis (MSC): https://www.tesourotransparente.gov.br/consultas/consultas-siconfi/matriz-de-saldos-contabeis-msc
- SICONFI — Documentação (área pública): https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=12503
- TCE-RS — Perguntas Frequentes SIAPC: http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- TCE-RS — MT-ASCE Vol. V (Resumo Leiaute Dados à Disposição): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
- TCE-RS — Sistemas de Controle Externo: https://tcers.tc.br/sistemas-de-controle-externo/
- FNDE — SIOPE: https://www.gov.br/fnde/pt-br/assuntos/sistemas/siope
- Prazo SIOPE 30/jan/2026 (AAM): https://aam.org.br/prazo-para-envio-dos-dados-do-siope-termina-em-30-de-janeiro-de-2026/
- Ministério da Saúde — SIOPS (entrega de dados): https://www.gov.br/saude/pt-br/acesso-a-informacao/siops/entrega-de-dados
- TCE-SP — Lei 14.133/2021 comentada (art. 94 / PNCP eficácia): https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021
- PNCP: https://pncp.gov.br

> Demais URLs (Planalto, gov.br/nfse, eSocial, DATASUS/RNDS, INEP, MDS, CONARQ) constam em `docs/diagnostico/partes/govtech-*.md`. Itens `[a confirmar]` exigem validação na fonte oficial do exercício antes de implementar.

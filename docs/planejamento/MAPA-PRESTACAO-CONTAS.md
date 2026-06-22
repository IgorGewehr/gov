# MAPA DE PRESTAÇÃO DE CONTAS — Tensorroot.Gov

> **Tabela única:** para CADA módulo → **destino** (ESTADO = TCE-RS / FEDERAL = STN-SICONFI, eSocial, DATASUS, INEP/FNDE, MDS, PNCP) → **obrigação** → **periodicidade** → **formato** → **`[OFICIAL]`**.
> Derivado de `docs/planejamento/partes/prestacao-contas-mapa.md`, `docs/architecture/contabilidade-pcasp-tce.md`, `docs/diagnostico/REQUISITOS-GOVTECH.md` e `GAP-E-ROADMAP.md`. Fontes oficiais ao final.
>
> **`[OFICIAL]`** = exige leiaute/versão vigente do exercício (CLAUDE.md §7/§16 — nunca *hardcoded*; validar antes de gerar remessa).
> **Princípio (spec contábil):** a **contabilidade PCASP é a FONTE**. `LancamentoContabil` → `Balancete` → **MSC**; da MSC derivam RREO/RGF/DCA (SICONFI) e a remessa SIAPC/PAD (TCE-RS).
> **Multi-tenant:** Executivo e Legislativo do mesmo município são **tenants DISTINTOS** (CNPJs distintos) → cada um presta contas separadamente ao TCE-RS e ao SICONFI.
> `[a confirmar]` = data/leiaute a validar na fonte oficial do exercício antes de implementar.

---

## Tabela mestra: módulo → destino → obrigação → periodicidade → formato → [OFICIAL]

| # | Módulo | Destino | Esfera | Obrigação | Periodicidade | Formato | [OFICIAL] |
|---|---|---|---|---|---|---|---|
| 1 | **Financas** | SIAPC/PAD (e-Validador/RDI) | Estado (TCE-RS) | Execução orçamentária/financeira/contábil/patrimonial (saldos PCASP, empenho→liquidação→pagamento, restos a pagar) | Mensal, até 30 dias corridos após o mês | Texto largura fixa, 1 reg./linha, CR/LF, sem packed/binário; Código de Remessa único; valida no PAD | `[OFICIAL]` MT-ASCE Vol. IV/V; PAD v25.x→26.x |
| 1 | **Financas** | Contas Anuais/Ordinárias | Estado (TCE-RS) | Balanço Geral + DCASP (7 demonstrações) + documentos | Anual | Conforme Res. TCE-RS 1134/2020 | `[OFICIAL]` Res. 1134/2020 `[datas a confirmar]` |
| 1 | **Financas** | SICOE | Estado (TCE-RS) | Acompanhamento de obras públicas | Trimestral (últ. dia útil do mês seguinte) `[a confirmar]` | Leiaute SICOE | `[OFICIAL]` |
| 1 | **Financas** | SICONFI — MSC Agregada | Federal (STN) | Matriz de Saldos Contábeis (conta PCASP + saldo + natureza + indicadores) | Mensal, até último dia do mês seguinte | XBRL/arquivo + API SICONFI | `[OFICIAL]` Port. STN 642 + 896/2017 |
| 1 | **Financas** | SICONFI — MSC Encerramento | Federal (STN) | MSC de dezembro (encerramento) → rascunho DCA | Anual, até 31/mar do exercício seguinte | XBRL/arquivo + API | `[OFICIAL]` |
| 1 | **Financas** | SICONFI — RREO | Federal (STN) | Rel. Resumido Execução Orçamentária (derivado da MSC) | Bimestral, 30 dias após o bimestre | Declaração SICONFI | `[OFICIAL]` LRF art. 52-53 |
| 1 | **Financas** | SICONFI — RGF | Federal (STN) | Rel. Gestão Fiscal (limites pessoal/dívida) | Quadrimestral (semestral facult. <50k hab.), 30 dias após | Declaração SICONFI | `[OFICIAL]` |
| 1 | **Financas** | SICONFI — DCA | Federal (STN) | Declaração de Contas Anuais (da MSC de encerramento) | Anual (~30/abr) `[a confirmar]` | Declaração SICONFI (upload manual + e-CPF A3; geramos + reconciliamos via API de consulta — ADR-0009) | `[OFICIAL]` |
| 1 | **Financas** | **SADIPEM / CDP (Cadastro da Dívida Pública)** | Federal (STN) | **Toda a dívida do ente (passivo)** atualizada — ≠ Dívida Ativa a receber (Tributos). Não atualizar → **CAUC negativo → bloqueia transferências voluntárias e operações de crédito** | **Anual até ~30/jan** `[a confirmar]` | Sistema SADIPEM (alimentado pelo passivo PCASP) | `[OFICIAL]` SADIPEM/CDP |
| 1 | **Financas** | **Transferegov.br** (ex-SICONV) | Federal | **Prestação de contas de convênios/repasses federais** (PC final + parciais por convênio); PC não aprovada → inadimplência (LRF) → sem novos repasses | Por convênio (parciais + final) `[a confirmar]` | Portal Transferegov.br (vinculado à execução) | `[OFICIAL]` Transferegov |
| 1 | **Financas** | **Conformidade SIAFIC** (o próprio sistema) | Federal/Estado | **Autodeclaração SIAFIC** (XML nº 1 das contas anuais) + requisitos do Dec. 10.540/2020 (base única, trilha, vedação a 2º SIAFIC) que o **próprio ERP deve cumprir e comprovar** | Anual (contas) + contínuo (requisitos) | Autodeclaração XML + checklist de conformidade | `[OFICIAL]` Dec. 10.540/2020 |
| 2 | **RecursosHumanos** | Remessa de Folha (SIAPC/PAD) | Estado (TCE-RS) | Cadastro de servidores, vantagens/descontos, totalizadores, classes salariais, base legal | Mensal, até 30 dias corridos após o mês | Texto largura fixa, arquivo `TCE_4810.TXT`; valida no PAD | `[OFICIAL]` Res. TCE-RS 1099/2018 |
| 2 | **RecursosHumanos** | eSocial | Federal (RFB/Caixa/INSS/MTE) | Eventos S-1000/tabelas, S-1200/S-1202/S-1207, S-2200/S-2299, S-1299 (fecha competência) | Mensal — periódicos até dia 15 do mês seguinte | XML assinado A1 ICP-Brasil vs XSD; WebService SOAP | `[OFICIAL]` eSocial S-1.3 |
| 2 | **RecursosHumanos**/Tributos | **EFD-Reinf (R-2010/R-4020)** | Federal (RFB) | **Retenções do ente como tomador/retentor** (INSS 11%, IRRF/CSRF sobre as NFS-e ingeridas via ADN). **Substituiu a DIRF (extinta 2024)** e alimenta a DCTFWeb | Mensal — até dia 15 do mês seguinte | XML assinado A1; cruzamento retenção × NFS-e/ADN | `[OFICIAL]` EFD-Reinf S-1.5 · `[a confirmar]` regime de órgão público |
| 2 | **RecursosHumanos** | **DCTFWeb / MIT** | Federal (RFB) | **Confissão do débito** consolidando eSocial + EFD-Reinf → gera o **DAE** | Mensal (após eSocial+EFD-Reinf) `[a confirmar]` | DCTFWeb (consolida) → DAE | `[OFICIAL]` DCTFWeb · `[a confirmar]` |
| 2 | **RecursosHumanos** | Ponto eletrônico (fiscalização MTE) | Federal (MTE) | AFD (assinado) + AEJ — guarda/fiscalização (exibição sob demanda) | Contínuo / sob demanda | Arquivos AFD + AEJ; REP-C/REP-A/REP-P; marcações imutáveis | `[OFICIAL]` Port. MTP 671/2021 |
| 2 | **RecursosHumanos** | CADPREV / DRPPS (RPPS) | Federal | Apuração previdenciária (correlação S-1202/S-1207) — **só se o ente tiver RPPS próprio** | Periódico `[a confirmar]` | Layout CADPREV | `[OFICIAL]` · **`[a confirmar]` RPPS×RGPS do piloto** (se RGPS, não se aplica — coberto por eSocial/EFD-Reinf) |
| 3 | **Tributos** | SIAPC/PAD (flag Receita) | Estado (TCE-RS) | Receita prevista→lançada→arrecadada→recolhida; dívida ativa inscrita/baixada (alimenta MSC) | Mensal (junto da remessa SIAPC) | Texto largura fixa | `[OFICIAL]` MT-ASCE Vol. V |
| 3 | **Tributos** | ADN / Receita Federal (NFS-e Nacional) | Federal | **Ingestão passiva**: Worker `NfseSync` baixa XMLs NFS-e dos CNPJs do tenant (não emitimos/assinamos) | Diária (download) | API JSON / DF-e XML assinado; mTLS ICP-Brasil; XSD NFSe v1.01 | `[OFICIAL]` obrigatória desde 01/01/2026 |
| 3 | **Tributos** | SICONFI (via Finanças/MSC) | Federal (STN) | Receita arrecadada compõe a MSC → RREO/RGF/DCA | Conforme Finanças (§1) | Conforme Finanças | `[OFICIAL]` |
| 4 | **Administracao** | PNCP | Federal | Editais, atas RP, contratos/aditivos, contratações diretas, PCA | Por evento — **condição de eficácia** do contrato (art. 94): até 10 dias úteis (direta) / ~20 (licitação) `[a confirmar literal]` | API REST/JSON UTF-8 + JWT (expira 1h); sem entrada manual | `[OFICIAL]` Lei 14.133/2021; Dec. 12.807/2025 |
| 4 | **Administracao** | SIAPC/PAD (LicitaCon) | Estado (TCE-RS) | Licitações, contratos e aditivos para acompanhamento | Por evento / mensal `[a confirmar]` | Leiaute LicitaCon/SIAPC | `[OFICIAL]` `[a confirmar leiaute]` |
| 5 | **Patrimonio** | SIAPC/PAD (via Finanças) | Estado (TCE-RS) | Bens, depreciação (MCASP), incorporação/baixa → contrapartida contábil (VPD/VPA) → balancete patrimonial | Mensal (compõe remessa SIAPC) | Texto largura fixa (registros patrimoniais) | `[OFICIAL]` |
| 5 | **Patrimonio** | SICONFI (via MSC) | Federal (STN) | Saldos patrimoniais compõem a MSC → Balanço Patrimonial/DCA | Conforme Finanças (§1) | Conforme Finanças | `[OFICIAL]` |
| 6 | **Saude** | e-SUS APS → SISAB | Federal (MS) | Produção da Atenção Primária (fichas CDS/PEC) — base do cofinanciamento APS | Contínua + avaliação quadrimestral (15 indicadores) | LEDI/Thrift (e-SUS APS) | `[OFICIAL]` Port. GM/MS 3.493/2024 |
| 6 | **Saude** | CNES/SCNES | Federal (MS) | Cadastro de estabelecimentos e profissionais | Mensal (~5º dia útil) | Sistema CNES | `[OFICIAL]` Port. 708/2007 |
| 6 | **Saude** | SIA/SIH (BPA/APAC/AIH) | Federal (MS) | Produção ambulatorial e hospitalar | Mensal (~5º dia útil) | Arquivos SIA/SIH (DATASUS) | `[OFICIAL]` Port. 708/2007 |
| 6 | **Saude** | RNDS | Federal (MS) | Eventos clínicos: imunização, exames, dispensação, sumário de alta | Contínua / por evento | HL7 FHIR R4 via HTTPS + certificado ICP-Brasil (e-CNPJ) | `[OFICIAL]` |
| 6 | **Saude** | SI-PNI | Federal (MS) | Evento de imunização | Contínua | FHIR via RNDS | `[OFICIAL]` |
| 6 | **Saude** | BNAFAR / e-SUS AF | Federal (MS) | Movimentação de assistência farmacêutica | Mensal | e-SUS AF (substitui Hórus — Port. GM/MS 11.585 de 16/06/2026) | `[OFICIAL]` |
| 6 | **Saude** | SIOPS | Federal (MS) | Receitas/despesas em saúde; mínimo 15% ASPS | Bimestral (homologação) `[a confirmar]` | Sistema SIOPS / declaração | `[OFICIAL]` |
| 6 | **Saude** | SIAPC/PAD (via Finanças) | Estado (TCE-RS) | Aplicação mínima em saúde (função 10) conciliada com empenho/liquidação/pagamento | Mensal | Texto largura fixa | `[OFICIAL]` |
| 7 | **Educacao** | Educacenso / Censo Escolar (INEP) | Federal (INEP) | Matrícula, turmas, docentes, situação do aluno — fonte de verdade FUNDEB/PNAE/PNATE | Anual, 2 etapas (matrícula mai-jul; situação fev-mar ano+1) | Migrador INEP (layout) | `[OFICIAL]` `[a confirmar layout]` |
| 7 | **Educacao** | SIOPE (FNDE) | Federal (FNDE) | Receitas/despesas da educação; mínimo MDE 25% (art. 212 CF) + FUNDEB | Bimestral, 30 dias após (6º bim. = 30/jan) | Sistema SIOPE; validação SIOPE-MAVS | `[OFICIAL]` |
| 7 | **Educacao** | PNAE (SiGPC/Sigecon) | Federal (FNDE) | Prestação de contas merenda; mín. 30% agricultura familiar | Anual (~15/fev, prorrog. 30/abr) | SiGPC / Sigecon | `[OFICIAL]` Lei 11.947/2009 |
| 7 | **Educacao** | PNATE (SiGPC) | Federal (FNDE) | Prestação de contas transporte escolar rural | Mensal + consolidação anual | SiGPC; parecer CACS/FUNDEB | `[OFICIAL]` Lei 10.880/2004 |
| 7 | **Educacao** | SIAPC/PAD (via Finanças) | Estado (TCE-RS) | Aplicação mínima MDE/FUNDEB (função 12) conciliada com PCASP/MSC | Mensal | Texto largura fixa | `[OFICIAL]` |
| 8 | **AssistenciaSocial** | CadÚnico (Dataprev) | Federal (MDS) | Cadastro de famílias (chave = CPF; NIS permanece) | Atualização ~2 anos ou mudança `[a confirmar]` | Sistema CadÚnico / layout import-export | `[OFICIAL]` |
| 8 | **AssistenciaSocial** | RMA | Federal (MDS) | Registro Mensal de Atendimentos CRAS/CREAS | Mensal | Portal MDS / layout | `[OFICIAL]` |
| 8 | **AssistenciaSocial** | Censo SUAS | Federal (MDS) | Estrutura/serviços da rede socioassistencial | Anual (abertura ~16/out) | Portal MDS | `[OFICIAL]` |
| 8 | **AssistenciaSocial** | SUASWeb | Federal (MDS) | Plano de Ação + Demonstrativo Sintético (aprovação do Conselho) | Anual | Portal SUASWeb | `[OFICIAL]` |
| 8 | **AssistenciaSocial** | CECAD 2.0 / CadSUAS / SISC / Prontuário SUAS | Federal (MDS) | Consulta CadÚnico, cadastro da rede, SCFV, acompanhamento familiar | Sob demanda / periódico `[a confirmar]` | Portais MDS | `[OFICIAL]` |
| 8 | **AssistenciaSocial** | SIAPC/PAD (via Finanças) | Estado (TCE-RS) | Despesa da função 08 conciliada com PCASP/MSC | Mensal | Texto largura fixa | `[OFICIAL]` |
| 9 | **Protocolo** | e-SIC / LAI | Federal (próprio do ente) | Resposta a pedidos de acesso à informação | Por demanda (20 dias + 10 prorrog.) | Portal e-SIC do tenant | Lei 12.527/2011 |
| 9 | **Protocolo** | TCE-RS / CONARQ | Estado | Documentos/processos sob requisição; guarda conforme TTDD (sem remessa periódica) | Sob demanda | PDF/A + RDC-Arq; assinatura Lei 14.063/2020 | `[OFICIAL]` e-ARQ Brasil v2 |
| 10 | **Legislativo** | SIAPC/PAD (Câmara) | Estado (TCE-RS) | Contas da Câmara: folha, despesas, limites art. 29-A CF (duodécimo) — **tenant/CNPJ próprio** | Mensal (SIAPC) + Anual (contas) | Texto largura fixa | `[OFICIAL]` `[a confirmar leiaute Legislativo]` |
| 10 | **Legislativo** | SICONFI (Câmara) | Federal (STN) | MSC própria da Câmara (CNPJ próprio) → consolida com o ente | Conforme §1 | XBRL/arquivo + API | `[OFICIAL]` |
| 10 | **Legislativo** | eSocial / PNCP | Federal | Folha (S-1.3) e licitações próprias da Câmara | Conforme §2 / §4 | Conforme §2 / §4 | `[OFICIAL]` |
| 10 | **Legislativo** | LexML / Dados Abertos | Federal | Proposições e leis (URN persistente) — transparência, não remessa obrigatória | Contínuo | XML LexML / CSV-JSON | benchmark SAPL |
| 11 | **Transparencia** | TCE-RS (SIAPC/PAD/e-Validador) | Estado (TCE-RS) | **EMPACOTADOR/RECONCILIADOR** — gera+pré-valida+empacota as remessas do Balancete/MSC dos demais módulos. **Envio = ato humano** no PAD/e-Protocolo com **certificado pessoal** (ADR-0009; não há WS de upload) | Mensal/Trimestral/Anual | Texto largura fixa + ZIP+hash; protocolo do ato humano registrado | `[OFICIAL]` ADR-0009 |
| 11 | **Transparencia** | SICONFI (STN) | Federal (STN) | **EMPACOTADOR/RECONCILIADOR** — gera MSC/RREO/RGF/DCA e **reconcilia via API de consulta** (`/extrato_entregas`). **Envio = upload manual** com **e-CPF A3** do gestor (ADR-0009; SICONFI não tem API de upload) | Mensal/Bimestral/Quadrimestral/Anual | XBRL/CSV zipado + reconciliação via API de consulta | `[OFICIAL]` ADR-0009 |
| 11 | **Transparencia** | Portal da Transparência (LAI) | Federal/LRF | Execução orçamentária em tempo real + dados abertos | Tempo real (1º dia útil após registro) | Portal web + CSV/JSON; retenção mín. 5 anos | `[OFICIAL]` LC 131/2009, Dec. 7.185/2010 |

> **Identidade** (12º contexto técnico) não origina prestação de contas: provê AuthN/AuthZ, tenant e trilha de auditoria que sustentam as remessas.

---

## Calendário consolidado (prazos)

| Obrigação | Esfera | Periodicidade | Prazo |
|---|---|---|---|
| SIAPC/PAD (execução) | TCE-RS | Mensal | 30 dias corridos após o mês |
| Folha (Res. 1099) `TCE_4810` | TCE-RS | Mensal | 30 dias corridos após o mês |
| Contas Anuais (Res. 1134) | TCE-RS | Anual | conforme exercício `[a confirmar]` |
| SICOE (obras) | TCE-RS | Trimestral | últ. dia útil do mês seguinte `[a confirmar]` |
| MSC Agregada | SICONFI | Mensal | último dia do mês seguinte |
| MSC Encerramento | SICONFI | Anual | 31/mar do exercício seguinte |
| RREO | SICONFI | Bimestral | 30 dias após o bimestre |
| RGF | SICONFI | Quadrimestral (semestral facult. <50k hab.) | 30 dias após |
| DCA | SICONFI | Anual | ~30/abr `[a confirmar]` |
| eSocial periódicos (S-1299) | Federal | Mensal | dia 15 do mês seguinte |
| EFD-Reinf (R-2010/R-4020) | Federal (RFB) | Mensal | dia 15 do mês seguinte |
| DCTFWeb/MIT (→ DAE) | Federal (RFB) | Mensal | após eSocial+EFD-Reinf `[a confirmar]` |
| SADIPEM / CDP (dívida) | Federal (STN) | Anual | ~30/jan `[a confirmar]` |
| Transferegov.br (PC convênios) | Federal | Por convênio (parciais + final) | conforme convênio `[a confirmar]` |
| SIOPE | FNDE | Bimestral | 30 dias após (6º bim. = 30/jan) |
| SIOPS | MS | Bimestral | `[a confirmar]` |
| PNAE (SiGPC) | FNDE | Anual | ~15/fev (prorrog. 30/abr) |
| PNATE (SiGPC) | FNDE | Mensal + anual | conforme SiGPC |
| Educacenso | INEP | Anual (2 etapas) | matrícula mai-jul; situação fev-mar ano+1 |
| CNES/SIA/SIH | DATASUS | Mensal | ~5º dia útil |
| RMA | MDS | Mensal | conforme portal |
| Censo SUAS | MDS | Anual | abertura ~16/out |
| PNCP (eficácia contrato) | Federal | Por evento | 10 dias úteis (direta) / ~20 (licitação) |
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
- Ministério da Saúde — SIOPS (entrega de dados): https://www.gov.br/saude/pt-br/acesso-a-informacao/siops/entrega-de-dados
- TCE-SP — Lei 14.133/2021 comentada (art. 94 / PNCP eficácia): https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021
- PNCP: https://pncp.gov.br
- STN — SADIPEM (Sistema de Análise da Dívida Pública, Operações de Crédito e Garantias): https://www.tesourotransparente.gov.br/temas/divida/sadipem
- Transferegov.br (ex-SICONV — convênios e transferências da União): https://www.gov.br/transferegov/
- RFB — EFD-Reinf: https://www.gov.br/receitafederal/pt-br/assuntos/orientacao-tributaria/declaracoes-e-demonstrativos/efd-reinf
- RFB — DCTFWeb: https://www.gov.br/receitafederal/pt-br/assuntos/orientacao-tributaria/declaracoes-e-demonstrativos/dctfweb
- Planalto — Decreto 10.540/2020 (SIAFIC): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/decreto/d10540.htm

> Itens `[a confirmar]` exigem validação na fonte oficial do exercício antes de implementar (CLAUDE.md §16).

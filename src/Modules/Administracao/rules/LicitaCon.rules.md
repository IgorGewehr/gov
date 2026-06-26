---
modulo: Administracao
slice: RemessaLicitaCon
contexto: Administracao (remessa obrigatoria de licitacoes/contratos ao TCE-RS — e-Validador LicitaCon)
poder: Ambos
schema: administracao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "TCE-RS — LicitaCon, e-Validador, Manual e Leiaute 1.4 (14 arquivos CSV; inclui modalidade PDE — Dispensa Eletronica)"
  - "TCE-RS — Instrucao Normativa 13/2017 (prazos/obrigatoriedade de remessa das licitacoes e contratos)"
  - "Lei 14.133/2021 (NLLC) — base do ciclo de licitacao/contrato remetido"
  - "Dataset oficial de referencia da estrutura: https://dados.tce.rs.gov.br/dataset/licitacoes-consolidado-2025"
---

# RemessaLicitaCon — Regras-as-Code (Rules-as-Code)

> Geracao da **remessa LicitaCon 1.4** ao **TCE-RS** — os **14 arquivos CSV** que o **e-Validador**
> efetivamente valida (IN 13/2017). Funcao **PURA** (sem I/O): a partir do agregado `Licitacao` (lotes,
> propostas, habilitacoes) e dos parametros do orgao/tenant, produz os bytes dos 14 CSV. A **transmissao
> efetiva** ao Processo Eletronico do TCE-RS (empacotamento + envio com **credencial**) e **// TODO(M10)**.
> Este arquivo e **normativo e versionado**.

---

## 1. Linguagem Ubiqua

| Termo (identificador) | Definicao |
|---|---|
| Remessa (`RemessaLicitaCon`) | Conjunto dos 14 arquivos CSV do leiaute 1.4 de uma licitacao. |
| Arquivo (`ArquivoRemessaLicitaCon`) | Um CSV da remessa (nome oficial + bytes UTF-8/BOM). |
| Escritor (`LicitaConCsvEscritor`) | Serializador CSV do leiaute (virgula, UTF-8/BOM, CRLF, RFC 4180). |
| Parametros (`RemessaLicitaConParametros`) | Codigos do orgao/dominio do leiaute (CD_ORGAO, CD_TIPO_MODALIDADE, TP_OBJETO...) — da borda. |
| Gerador (`GeradorRemessaLicitaCon`) | Constroi os 14 CSV a partir da `Licitacao` + parametros. |

---

## 2. Formato do leiaute (verificado contra remessa REAL do TCE-RS)

- **Separador de campo:** virgula (`,`).
- **Codificacao:** UTF-8 **com BOM**.
- **Terminador de linha:** **CRLF**.
- **Cabecalho:** 1a linha com os nomes oficiais das colunas (prefixos `CD_/NR_/TP_/DT_/BL_/VL_/PC_/SG_/NM_/DS_`).
- **Datas:** `yyyy-MM-dd`. **Decimais:** ponto, 2 casas. **Booleanos:** `S`/`N`. **Nulos:** vazio.
- **Escape (RFC 4180):** celula com virgula/aspas/quebra vai entre aspas duplas; aspas internas duplicadas.
- **Os 14 arquivos** (ordem do leiaute): PESSOAS, MEMBRO_CONSORCIO, COMISSAO, MEMBRO_COMISSAO, **LICITACAO**,
  **LICITANTE**, DOTACAO_LICITACAO, EVENTO_LICITACAO, **LOTE**, ITEM, **PROPOSTA**, LOTE_PROPOSTA,
  ITEM_PROPOSTA, DOCUMENTO_LICITACAO.

> **Nota de estrutura:** as colunas de **LICITACAO (65)**, **LOTE**, **ITEM**, **PROPOSTA** e **LICITANTE**
> foram extraidas ao vivo de um arquivo real publicado pelo TCE-RS (dados.tce.rs.gov.br). LOTE/ITEM/LICITANTE
> **repetem** `TP_DOCUMENTO`/`NR_DOCUMENTO` (intencional — casa por posicao). Os demais arquivos saem com o
> cabecalho oficial e **sem linhas** ate o dominio modelar esses conceitos (comissao, dotacoes, itens
> granulares, documentos). // TODO(M10-validate): fechar o conjunto exato de colunas/dominios dos 14
> arquivos contra o PDF oficial do leiaute 1.4 no credenciamento do e-Validador.

---

## 3. Invariantes

- **I-1.** Cabecalho obrigatorio; cada linha deve ter exatamente a aridade do cabecalho (recusa se nao casar).
- **I-2.** A geracao e **tenant-scoped** (a `Licitacao` vem do repositorio com Global Query Filter).
- **I-3.** Determinismo: a mesma licitacao + parametros produz os mesmos bytes (idempotencia da remessa).
- **I-4.** Parametros do orgao/dominio vem da **borda** (sem numero magico no codigo — CLAUDE.md §16).
- **I-5.** Sem I/O no gerador; a transmissao real ao TCE-RS e // TODO(M10) (credencial).

---

## 4. Comandos / Consultas

| Operacao | Tipo | Efeito |
|---|---|---|
| `GerarRemessaLicitaCon` | Query (`RemessaLicitaCon`) | Carrega a licitacao e produz os 14 CSV. Endpoint devolve um ZIP. |

---

## 5. Seguranca, Tenant e Auditoria

- Tenant-scoped; sem vazamento entre entes (filtro global no repositorio).
- A remessa nao muta estado de dominio (consulta pura); a auditoria cobre a acao de geracao na borda.
- Permissao do endpoint: `administracao.gerenciar`.

---

## 6. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-26 | Versao inicial (L6) — gerador dos 14 CSV do leiaute LicitaCon 1.4, estrutura verificada ao vivo contra remessa real do TCE-RS; LICITACAO/LOTE/LICITANTE/PROPOSTA populados do agregado, demais arquivos com cabecalho oficial. Transmissao ao TCE-RS = // TODO(M10). |

<!-- manifest
queries: GerarRemessaLicitaCon
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->

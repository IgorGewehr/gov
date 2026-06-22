# Pesquisa M4 — Entrega ao SICONFI (STN/União): MSC, RREO, RGF, DCA

> Escopo desta nota: **COMO se entrega** ao SICONFI (canal, autenticação, formato, prazos)
> e a **relação com a MSC** que o módulo Transparencia gera. Não repete a estrutura/campos
> da MSC — isso está em `docs/architecture/specs-oficiais/siconfi-msc-rreo-rgf.md`.
> Regra de ouro: toda afirmação factual tem FONTE; o que não foi confirmado em doc oficial
> está marcado `[a confirmar — <documento>]`.
> Última verificação das fontes: 2026-06-22.

---

## 1. Achados — canal de entrega (o ponto mais load-bearing)

Há **dois canais distintos** no SICONFI, e eles **não são intercambiáveis**:

| Canal | Para quê serve | Direção | Autenticação |
|-------|----------------|---------|--------------|
| **Aplicação web SICONFI (área restrita do ente)** | **ENVIO/UPLOAD** da MSC e **homologação/assinatura** de RREO/RGF/DCA | ente → STN (entrega oficial) | **Certificado digital ICP-Brasil** (login + assinatura) |
| **API de Dados Abertos (`apidatalake.tesouro.gov.br`)** | **CONSULTA/DOWNLOAD** de declarações já homologadas | STN → público | **Nenhuma** (dados públicos, sem captcha) |

> **CONCLUSÃO CRÍTICA PARA O MÓDULO:** **não existe API pública de UPLOAD** da MSC nem de
> homologação dos relatórios. A entrega oficial é feita por **upload de arquivo (zipado) na
> área restrita web** do SICONFI, autenticada por certificado digital. O que o Tensorroot.Gov
> pode automatizar 100% é a **GERAÇÃO do arquivo** (CSV/XBRL GL) no formato exato; o **upload e
> a assinatura** são, hoje, etapa manual/assistida no portal. Confirmado pelas Regras Gerais da
> MSC 2026 e pelo conteúdo da área pública do SICONFI (caminho na área restrita:
> *Menu de Módulos > Declaração > Elaborar Declaração > Nova Declaração > Carregar Instância XBRL*).
> Fonte: Regras Gerais MSC 2026; SICONFI área pública (busca oficial).
> `[a confirmar — Regras Gerais MSC 2026, seção "Carga da MSC"]` para o caminho exato de menu da carga **CSV**.

### Formatos aceitos no upload da MSC
1. **CSV** (leiaute adaptado do XBRL GL) — o SICONFI converte o CSV em instância XBRL GL.
2. **Instância XBRL GL** (taxonomia do Consórcio XBRL Internacional).
- **Ambos devem ser enviados ZIPADOS/compactados.**
- Fonte: Regras Gerais MSC 2026 (https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf).

---

## 2. Autenticação — certificado digital

- **Tipo confirmado pela STN:** **e-CPF A3, padrão ICP-Brasil** (pessoa física — titular de
  Poder/Órgão, ex.: o Prefeito) para **identificação e assinatura** das declarações com validade
  jurídica. Armazenado em **smartcard/token** (A3 = mídia física), emitido por AC credenciada
  pela Receita Federal/ICP-Brasil. Validade 2–3 anos.
  - Fonte: SICONFI — "Problemas com certificado digital?" (https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=64).
- **IMPACTO NO PROJETO (vs. CLAUDE.md §6):** o §6 prevê certificados **A1 (.pfx)** no Azure Key Vault
  para integrações governamentais. O SICONFI, porém, documenta **e-CPF A3 em token** para a
  **assinatura final do gestor**. Ou seja: a assinatura de homologação no SICONFI tende a ser ato
  pessoal do gestor no portal (A3 em token), **não** automatizável por A1 no Key Vault.
  - `[a confirmar — doc oficial]` se o SICONFI aceita **e-CPF A1 (.pfx)** como alternativa ao A3 para
    login/assinatura (a página id=64 cita só A3; não confirma nem nega A1). Documento a obter:
    "Manual do Usuário SICONFI — Acesso e Certificação Digital".
  - `[a confirmar]` se a **carga da MSC** (upload do arquivo, distinto da assinatura) exige
    certificado de pessoa jurídica (e-CNPJ) ou apenas o login do operador.

---

## 3. O que cada declaração é, periodicidade e prazo de entrega

| Sigla | Declaração | Base legal | Periodicidade | Prazo de entrega/homologação | Gerada a partir de |
|-------|-----------|-----------|---------------|------------------------------|--------------------|
| **MSC** | Matriz de Saldos Contábeis | Portaria STN 642/2019; art. 48/51 LRF | **Mensal** (agregada) + **anual** (encerramento, mês 13) | mensal: até o **último dia do mês seguinte**; encerramento: até **último dia de março** do ano seguinte | (insumo — gerada pelo ente) |
| **RREO** | Relatório Resumido da Execução Orçamentária | art. 165 §3º CF; arts. 52–53 LRF | **Bimestral** (municípios <50 mil hab. podem optar por semestral — art. 63 LRF) | até **30 dias após o fim de cada bimestre** | MSC Agregada |
| **RGF** | Relatório de Gestão Fiscal | arts. 54–55 LRF | **Quadrimestral** (<50 mil hab. podem optar por semestral — art. 63 LRF) | até **30 dias após o fim de cada quadrimestre** | MSC Agregada |
| **DCA** | Declaração de Contas Anuais (Balanço Anual) | art. 51 LRF | **Anual** | conforme calendário STN do exercício seguinte | MSC Encerramento (mês 13) |

- Maximiliano de Almeida/RS tem **menos de 50 mil hab.** → pode optar por RREO/RGF **semestral**
  (art. 63 LRF). `[a confirmar]` qual periodicidade o município efetivamente exerce hoje.
- Prazos detalhados acima vêm das Regras Gerais MSC/RREO/RGF e da LRF. O **calendário exato da DCA
  2026/2027** deve ser obtido em Portaria STN específica do exercício — `[a confirmar — Portaria STN de calendário de entregas do exercício]`.
- Fontes: Regras Gerais MSC 2026; Regras Gerais RREO/RGF 2026 (SICONFI); LC 101/2000 (LRF).

---

## 4. Relação com a MSC que o módulo gera (fluxo de entrega)

A MSC **é o insumo único** que dispara a geração dos demonstrativos no SICONFI:

```
[Tensorroot.Gov — módulo Transparencia]
   gera MSC mensal (CSV ou XBRL GL, zipado)  ──upload manual/assistido──▶  [SICONFI web, área restrita]
                                                                                  │
                                            ao receber a ÚLTIMA MSC do período, o SICONFI
                                            CONVERTE automaticamente em RASCUNHO de RREO/RGF/DCA
                                                                                  │
                                            gestor revisa → ajusta com NOTA EXPLICATIVA →
                                            ASSINA (e-CPF A3) → FINALIZA/HOMOLOGA
                                                                                  │
                                            ──API de Dados Abertos──▶ Tensorroot.Gov pode
                                            BAIXAR de volta o que foi homologado (reconciliação)
```

- **Gerar rascunho ≠ entregar.** A entrega só se consuma com a **homologação assinada** no portal.
- O módulo Transparencia deve, portanto, implementar:
  1. **Gerador de MSC** (CSV/XBRL GL exato + zip) — núcleo automatizável.
  2. **Cliente da API de Dados Abertos** (consulta `/extrato_entregas`, `/rreo`, `/rgf`, `/dca`,
     `/msc_*`) para **reconciliar** o que foi homologado vs. o que o ERP calculou e para **auditar
     o status de entrega** (o `/extrato_entregas` retorna o que foi entregue/retificado).
  3. **Empacotamento + instruções de upload** (até existir API de envio, a entrega final é ação do gestor).

---

## 5. API de Dados Abertos — contrato confirmado (para reconciliação/auditoria)

- **Base:** `https://apidatalake.tesouro.gov.br/ords/siconfi/tt/` (**HTTPS** — corrige o `http://`
  registrado na spec antiga). **Versão 1.1.0.** **REST · resposta JSON · sem autenticação.**
- **Paginação:** 5.000 itens/página. **Rate limit recomendado:** **1 req/seg.** Licença Apache 2.0.
- Fonte: Swagger oficial `https://apidatalake.tesouro.gov.br/docs/siconfi/`.

| Endpoint | Método | Parâmetros obrigatórios | Opcionais |
|----------|--------|-------------------------|-----------|
| `/rreo` | GET | `an_exercicio`, `nr_periodo`, `co_tipo_demonstrativo`, `id_ente` | `no_anexo`, `co_esfera` |
| `/rgf` | GET | `an_exercicio`, `in_periodicidade` (`S`/`Q`), `nr_periodo`, `co_tipo_demonstrativo`, `co_poder`, `id_ente` | `no_anexo`, `co_esfera` |
| `/dca` | GET | `an_exercicio`, `id_ente` | `no_anexo` |
| `/msc_patrimonial` | GET | `id_ente`, `an_referencia`, `me_referencia`, `co_tipo_matriz` (`MSCC`/`MSCE`), `classe_conta`, `id_tv` | — |
| `/msc_orcamentaria` | GET | idem `/msc_patrimonial` | — |
| `/msc_controle` | GET | idem `/msc_patrimonial` | — |
| `/entes` | GET | (nenhum) — cadastro de todos os entes | — |
| `/extrato_entregas` | GET | `id_ente`, `an_referencia` | — |
| `/anexos-relatorios` | GET | (nenhum) — tabela de apoio de anexos por esfera | — |

- **Enums confirmados no Swagger:**
  - `co_esfera`: `M`=Municípios, `E`=Estados/DF, `U`=União, `C`=Consórcio.
  - `co_poder` (RGF): `E`=Executivo, `L`=Legislativo, `J`=Judiciário, `M`=Ministério Público, `D`=Defensoria.
  - `in_periodicidade` (RGF): `S`=semestral, `Q`=quadrimestral.
  - `co_tipo_matriz` (MSC): `MSCC` (consolidação/agregada) / `MSCE` (encerramento). `[a confirmar — significado exato dos códigos no Swagger]`.
  - `id_tv` (MSC): `beginning_balance` / `ending_balance` / `period_change`.
- `id_ente` = **código IBGE** do ente. `[a confirmar]` código IBGE de Maximiliano de Almeida/RS via `/entes`.
- **Correção vs. spec antiga:** o Swagger nomeia o parâmetro da matriz como **`co_tipo_matriz`**
  (não `id_tipo_matriz`) e o tipo de valor como **`id_tv`**. A spec `siconfi-msc-rreo-rgf.md` §6
  lista `id_tv`/`id_tipo_matriz` — **ajustar** para `co_tipo_matriz`.

---

## 6. Pendências `[a confirmar — obter doc oficial]`

- `[a confirmar]` **SICONFI aceita e-CPF A1 (.pfx)** para login/assinatura, ou exige A3 em token?
  → impacta diretamente CLAUDE.md §6 (A1 no Key Vault) e o grau de automação da homologação.
  Doc: "Manual do Usuário SICONFI — Acesso e Certificação Digital".
- `[a confirmar]` Existe **API/serviço de UPLOAD** (não-pública) da MSC para sistemas de ente
  (ex.: web service de carga em lote)? Hoje toda evidência aponta para upload **manual** no portal.
  Doc: Regras Gerais MSC 2026 + eventual manual de integração STN.
- `[a confirmar]` **Caminho de menu exato** da carga **CSV** na área restrita (o confirmado refere
  o caminho da **Instância XBRL**). Doc: Regras Gerais MSC 2026, seção de carga.
- `[a confirmar]` **Calendário oficial de entregas do exercício** (datas exatas de DCA, e do
  cronograma mensal/bimestral/quadrimestral 2026/2027). Doc: Portaria STN de calendário do exercício.
- `[a confirmar]` Periodicidade **efetivamente adotada** por Maximiliano de Almeida (bimestral/quadrimestral
  vs. opção semestral do art. 63 LRF).
- `[a confirmar]` Significado/enum exatos de `co_tipo_matriz` (`MSCC`/`MSCE`) e `co_tipo_demonstrativo`
  por relatório/anexo. Doc: Swagger `https://apidatalake.tesouro.gov.br/docs/siconfi/`.
- `[a confirmar]` Relação com o **TCE-RS**: se o TCE-RS recebe a MSC via integração/acordo com a STN
  (evita reenvio), ou se exige envio próprio pelo SIAPC/PAD. Doc: Instrução Normativa TCE-RS vigente.

---

## 7. Fontes oficiais (URLs)
- API SICONFI — Swagger oficial: https://apidatalake.tesouro.gov.br/docs/siconfi/
- API SICONFI — Tesouro Transparente: https://www.tesourotransparente.gov.br/consultas/consultas-siconfi/siconfi-api-de-dados-abertos
- Catálogo de APIs gov (Conecta): https://www.gov.br/conecta/catalogo/apis/siconfi-extratos-das-declaracoes-contabeis
- Regras Gerais MSC 2026: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- Certificado digital no SICONFI (e-CPF A3 ICP-Brasil): https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=64
- Taxonomia SICONFI (XBRL): https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=584
- Regras Gerais RREO (referência de leiaute): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2024_Regras_Gerais_e_Instrucoes_de_preenchimento_RREO.pdf
- Portal SICONFI: https://siconfi.tesouro.gov.br/
- LRF (LC 101/2000): http://www.planalto.gov.br/ccivil_03/leis/lcp/lcp101.htm

# Verificação adversarial — `pesquisa-siconfi-entrega.md`

> Papel: cético/auditor de fatos. Cada afirmação factual da nota de pesquisa foi checada
> contra **fonte oficial** (STN/SICONFI, Planalto/LRF). Classificação:
> **CONFIRMADO** (com URL oficial) · **PLAUSÍVEL-SEM-FONTE** · **INCERTO/CONTRADITÓRIO**.
> Data da verificação: 2026-06-22.
> Método: WebFetch do Swagger oficial; `pdftotext` sobre o PDF oficial das Regras Gerais MSC 2026
> (baixado do siconfi.tesouro.gov.br); WebFetch da página de certificado digital do SICONFI;
> texto do Art. 63 LRF.

---

## Placar

- **CONFIRMADO: 17**
- **PLAUSÍVEL-SEM-FONTE: 3**
- **INCERTO/CONTRADITÓRIO: 4** (3 já estavam marcados `[a confirmar]`; **1 é contradição nova
  encontrada nesta auditoria** — ver C-1)

---

## 1. Canal de entrega (§1 da nota)

| # | Afirmação | Veredito | Evidência |
|---|-----------|----------|-----------|
| 1.1 | Existem dois canais: app web SICONFI (envio) e API Dados Abertos (consulta) | **CONFIRMADO** | Swagger declara API só de consulta, sem autenticação; Regras Gerais MSC descrevem carga no Siconfi (área restrita). |
| 1.2 | **Não existe API pública de UPLOAD** da MSC nem de homologação | **CONFIRMADO** | Swagger (apidatalake) lista apenas GETs de consulta; nenhum POST de carga. Regras Gerais MSC: "CARREGAMENTO DA MSC … a instituição deve atentar para o correto preenchimento dos filtros no Siconfi" — descreve carga manual no portal. |
| 1.3 | O que dá para automatizar 100% é a GERAÇÃO do arquivo; upload/assinatura são manuais | **CONFIRMADO** (no escopo das fontes públicas) | Coerente com 1.2. Ver risco R-2 sobre web service não-público. |
| 1.4 | Formatos aceitos: **CSV** (adaptado do XBRL GL) e **instância XBRL GL** | **CONFIRMADO** | Regras Gerais MSC 2026: "dois formatos detalhados: arquivo CSV e a própria instância XBRL Global Ledger"; "O leiaute da MSC em arquivo CSV foi adaptado … com base no padrão XBRL GL". |
| 1.5 | O SICONFI converte o CSV em instância XBRL GL | **CONFIRMADO** | "Caso a instituição carregue um arquivo CSV, o sistema o transformará em uma instância XBRL GL". |
| 1.6 | Ambos devem ser enviados **zipados** | **CONFIRMADO** | "O arquivo a ser carregado no Siconfi, seja no formato CSV ou XBRL GL, deverá ser compactado (zipado)". |
| 1.7 | Caminho de menu exato da carga (Menu de Módulos > Declaração > …) | **INCERTO** (já marcado `[a confirmar]`) | O PDF de Regras Gerais 2026 descreve as ETAPAS (De-Para, Carregamento da MSC, Geração de relatórios) mas **não** o caminho literal de menu. Não implementar instruções de UI baseadas em texto não confirmado. |

## 2. Autenticação / certificado (§2)

| # | Afirmação | Veredito | Evidência |
|---|-----------|----------|-----------|
| 2.1 | SICONFI usa **e-CPF A3 ICP-Brasil** (token/smartcard) p/ assinatura do gestor | **CONFIRMADO** | Página id=64: "O certificado digital com especificação e-CPF A3, padrão ICP-Brasil permite a assinatura digital dos prefeitos e governadores no Siconfi". Mídia A3 = smartcard/token; validade 2–3 anos. |
| 2.2 | A página **não confirma nem nega A1 (.pfx)** | **CONFIRMADO** (como pendência) | A página id=64 cita só A3; não menciona A1. Logo: **não assumir** que A1 no Key Vault assina/homologa no SICONFI. |
| 2.3 | Conflito com CLAUDE.md §6 (A1 no Key Vault): a homologação no SICONFI tende a ser ato pessoal A3 | **PLAUSÍVEL-SEM-FONTE** | Inferência razoável, mas a fonte não afirma "intransferível/só A3 token". Manter como hipótese, não como requisito. |

## 3. Declarações, periodicidade, prazos (§3)

| # | Afirmação | Veredito | Evidência |
|---|-----------|----------|-----------|
| 3.1 | RREO é **bimestral**, prazo 30 dias após o bimestre | **CONFIRMADO** | LRF arts. 52–53. |
| 3.2 | RGF é **quadrimestral**, prazo 30 dias após o quadrimestre | **CONFIRMADO** | LRF arts. 54–55 (§2). |
| 3.3 | Municípios < 50 mil hab. podem optar por **semestral** (RREO e RGF) — art. 63 LRF | **CONFIRMADO** | Art. 63: "É facultado aos Municípios com população inferior a cinqüenta mil habitantes optar por: … II - divulgar semestralmente: b) o Relatório de Gestão Fiscal; c) os demonstrativos de que trata o art. 53 [RREO]". |
| 3.4 | MSC **mensal** (agregada) + **anual** (encerramento, "mês 13") | **CONFIRMADO** (com ressalva) | Regras Gerais MSC: "A MSC é dividida em dois tipos … a MSC agregada [periodicidade mensal] e a MSC encerramento [periodicidade anual]". **Ressalva:** o termo "mês 13" **não aparece** no PDF; o oficial é "MSC de encerramento". Ver C-2. |
| 3.5 | Encerramento: prazo até **último dia de março** do ano seguinte | **CONFIRMADO** | "O prazo para o envio da MSC de encerramento referente aos dados de 2024 será até o último dia do mês de março de 2026." (exemplo oficial). |
| 3.6 | MSC mensal: prazo "até o último dia do mês seguinte" | **PLAUSÍVEL-SEM-FONTE** | Não localizado verbatim no PDF de Regras Gerais 2026. Prática usual, mas **obter o calendário/portaria do exercício** antes de codar prazos. |
| 3.7 | DCA conforme calendário STN do exercício | **INCERTO** (já marcado `[a confirmar]`) | Calendário exato depende de Portaria STN do exercício — não fixar datas no código. |
| 3.8 | Maximiliano de Almeida < 50 mil hab. → pode optar semestral | **PLAUSÍVEL-SEM-FONTE** | População plausível (município pequeno/RS), mas **a opção efetivamente exercida** e o número populacional não foram confirmados aqui. Parametrizar por tenant (coerente com CLAUDE.md §7). |

## 4. Fluxo MSC → rascunho → homologação (§4)

| # | Afirmação | Veredito | Evidência |
|---|-----------|----------|-----------|
| 4.1 | Ao carregar a **última MSC do período**, o SICONFI converte automaticamente em **rascunho** de RREO/RGF/DCA | **CONFIRMADO** | "A conversão da MSC nos relatórios ocorrerá automaticamente quando do carregamento … da última MSC de um determinado período … o relatório convertido será disponibilizado em forma de rascunho na área restrita". |
| 4.2 | Gestor revisa, ajusta com **nota explicativa**, assina e homologa | **CONFIRMADO** | "as alterações sejam destacadas em notas explicativas"; "se o relatório for assinado e homologado…". |
| 4.3 | **Gerar rascunho ≠ entregar**; entrega só com homologação assinada | **CONFIRMADO** | Coerente com o texto de homologação/assinatura acima. |
| 4.4 | Reconciliação possível via API Dados Abertos (baixar o homologado) | **CONFIRMADO** | API expõe `/rreo`, `/rgf`, `/dca`, `/msc_*`, `/extrato_entregas` (consulta pública). |

## 5. API de Dados Abertos (§5)

| # | Afirmação | Veredito | Evidência |
|---|-----------|----------|-----------|
| 5.1 | Base `https://apidatalake.tesouro.gov.br/ords/siconfi/tt/`, **v1.1.0**, JSON, sem auth | **CONFIRMADO** | Swagger oficial. |
| 5.2 | Paginação 5.000/página; rate limit recomendado 1 req/s; licença Apache 2.0 | **CONFIRMADO** | Swagger oficial. |
| 5.3 | Tabela de endpoints + params obrigatórios/opcionais (rreo, rgf, dca, msc_*, entes, extrato_entregas, anexos-relatorios) | **CONFIRMADO** | Bate 1:1 com o Swagger. **Correção:** a nota lista `co_esfera` como opcional de `/rreo` e `/rgf` — confirmado opcional; e `co_poder` é **obrigatório** em `/rgf` — confirmado. |
| 5.4 | Parâmetro é **`co_tipo_matriz`** (não `id_tipo_matriz`); tipo de valor é **`id_tv`** | **CONFIRMADO** | Swagger: `co_tipo_matriz` (String) e `id_tv` (String) nos endpoints `msc_*`. Correção vs. spec antiga procede. |
| 5.5 | Enums `co_esfera` (M/E/U/C), `co_poder` (E/L/J/M/D), `in_periodicidade` (S/Q), `id_tv` (beginning_balance/ending_balance/period_change) | **CONFIRMADO** | Swagger oficial. |
| 5.6 | `co_tipo_matriz`: `MSCC`=agregada/consolidação, `MSCE`=encerramento | **CONFIRMADO** (significado) | Swagger expõe MSCC/MSCE; Regras Gerais MSC confirmam a semântica "agregada" vs "encerramento". A nota já marcava `[a confirmar]` o significado — agora **resolvido**: `MSCC`=agregada (mensal), `MSCE`=encerramento (anual). |
| 5.7 | `co_tipo_demonstrativo` por relatório/anexo | **INCERTO** (já marcado `[a confirmar]`) | Swagger não enumera valores de forma exaustiva ("varies by report"). Não hardcodar a lista; descobrir via API/tabela de anexos. |
| 5.8 | `id_ente` = código IBGE | **PLAUSÍVEL-SEM-FONTE** | Convenção SICONFI usual; confirmar o código de Maximiliano de Almeida via `/entes` antes de usar. |

---

## Contradição/achado NOVO desta auditoria

### C-1 (CONTRADITÓRIO — impacta modelagem de tenant) — **maior risco**
As Regras Gerais MSC 2026 afirmam, textualmente:
> "O envio da MSC será realizado **exclusivamente pelo Poder Executivo**, utilizando informações
> agregadas e não consolidadas. Por esse motivo, os demais poderes e órgãos deverão ser
> evidenciados na MSC utilizando a informação complementar 'Poder e Órgão'."

A nota de pesquisa (e o CLAUDE.md §4) tratam **Executivo e Legislativo como tenants distintos**.
Para o SICONFI/MSC, **só o Executivo envia a MSC**, consolidando o Legislativo via informação
complementar "Poder e Órgão" — **não** há envio separado de MSC pela Câmara. Isto **não invalida**
o modelo multi-tenant do ERP, mas **muda o desenho do gerador de MSC**: a MSC de um município é
**uma só**, originada no tenant Executivo, contendo dados do Legislativo. **Não implementar** um
fluxo "cada tenant envia sua própria MSC ao SICONFI".
(NB: RGF é emitido por cada Poder — art. 55 LRF —, mas isso é o relatório, não a carga da MSC.)

### C-2 (terminologia) — "mês 13"
O termo **"mês 13"** usado na nota **não consta** das Regras Gerais MSC 2026. O oficial é
**"MSC de encerramento"** (anual, prazo último dia de março). Usar a nomenclatura oficial no código
e nos contratos para evitar divergência com o leiaute.

---

## O que NÃO se deve implementar sem documento oficial vigente

1. **Caminho de menu / instruções de UI de carga** (1.7) — o texto literal de navegação não está
   confirmado no doc oficial; não embutir como "passo a passo" garantido.
2. **Qualquer cliente de UPLOAD/POST automático ao SICONFI** (R-2) — não há API pública; só gerar o
   arquivo. Antes de prometer automação de envio, **obter** confirmação da STN sobre web service de
   carga (hoje inexistente nas fontes públicas).
3. **Datas/prazos fixos no código** (3.6, 3.7) — o prazo mensal verbatim e o calendário da DCA
   dependem de Portaria STN do exercício. Parametrizar por tenant/exercício (CLAUDE.md §7).
4. **Assinatura/homologação via A1 no Key Vault** (2.2/2.3) — a fonte só documenta A3 em token para
   assinatura do gestor; não codar homologação automática por A1 sem doc que autorize A1.
5. **Lista hardcoded de `co_tipo_demonstrativo`** (5.7) — descobrir dinamicamente.
6. **Periodicidade (bimestral vs semestral) fixa para Maximiliano** (3.8) — parametrizar; opção do
   art. 63 é uma escolha do ente, não um default.

---

## Os 3 maiores riscos

1. **R-1 (modelagem) — MSC é enviada SÓ pelo Executivo, consolidando o Legislativo via "Poder e
   Órgão".** Tratar a Câmara como tenant que envia MSC própria ao SICONFI seria **incorreto** e
   geraria entrega inválida/duplicada. O gerador de MSC deve produzir **uma matriz por município**,
   no tenant Executivo, com o Legislativo evidenciado por informação complementar. (Fonte: Regras
   Gerais MSC 2026, "Observações Importantes".)

2. **R-2 (escopo de automação) — não existe API/serviço público de UPLOAD ou de homologação.** A
   entrega oficial é manual/assistida no portal, com assinatura A3 do gestor. Prometer "envio 100%
   automático ao SICONFI" é falso hoje. Escopo seguro: gerar CSV/XBRL GL zipado + cliente de
   consulta (`/extrato_entregas`, `/rreo`, etc.) para reconciliação. (Fontes: Swagger; Regras Gerais
   MSC.)

3. **R-3 (certificado) — assinatura de homologação parece exigir e-CPF A3 em token (ato pessoal do
   gestor), não A1 no Key Vault.** Conflito direto com a estratégia do CLAUDE.md §6 (A1/.pfx no Key
   Vault). É preciso o "Manual do Usuário SICONFI — Acesso e Certificação Digital" para saber se A1 é
   aceito; até lá, **não** projetar homologação desatendida por A1. (Fonte: SICONFI id=64.)

---

## Fontes oficiais consultadas nesta verificação
- Swagger API SICONFI: https://apidatalake.tesouro.gov.br/docs/siconfi/
- Regras Gerais MSC 2026 (Portaria STN 642): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- Certificado digital SICONFI (e-CPF A3): https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=64
- LRF — LC 101/2000 (arts. 52–55, 63): https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp101.htm (Art. 63 verbatim confirmado)

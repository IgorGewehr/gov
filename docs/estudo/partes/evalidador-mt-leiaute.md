# Estudo — Manual Técnico (MT) / Leiaute SIAPC-PAD do TCE-RS

> Objetivo: trocar nossos `TODO(MT-2026)` (campos/posições no gerador de remessa e no
> `EValidadorLocalSiapc`) pelas posições/tamanhos campo-a-campo oficiais, validando contra o
> **e-Validador / RVE (Recepção e Validação de Esquemas)** e o **PAD (Programa Autenticador de
> Dados)** oficiais — não mais contra a nossa simulação local.
> Convenção: toda afirmação tem **[FONTE]** ou **[a confirmar]**.

---

## 1. Resposta direta (TL;DR — 4 linhas)

1. **Versão vigente do leiaute / PAD:** Manual Técnico SIAPC/PAD com o **PAD versão 20.0.0.0**
   (ciclo 2025) e o **Volume V revisão 06, válido a partir de junho/2024 (rev. 26/03/2024)** —
   essa é a base posicional atualmente publicada. [FONTE: IGAM; MPC-RS MT-Vol-V rev.06] **[2026: a confirmar]**
2. **Onde obter:** Portal TCE-RS → *Sistemas de Controle Externo* → **SIAPC** → seção *Manuais*
   (Volumes I a V) e *Download do PAD*: `https://tcers.tc.br/sistemas-de-controle-externo/`. [FONTE: TCE-RS]
3. **Estrutura campo-a-campo:** os MTs trazem, por arquivo `.TXT`, tabelas com
   **Descrição do Campo / Tipo / Bytes / Colunas (posição inicial→final) / Observações** — exatamente
   o que precisamos para os campos posicionais exatos. [FONTE: MT Vol V rev.06, extração local]
4. **Mudança 2026 (já anunciada):** para o **encerramento de 2026** o TCE-RS passará a **confeccionar
   o Balanço Financeiro e a Demonstração dos Fluxos de Caixa (DFC)**; o leiaute detalhado desses
   demonstrativos para 2026 **ainda não foi localizado publicado** (acompanhar). [FONTE: TCE-RS, 26ª edição evento SIAPC] **[detalhe de campos: a confirmar]**

---

## 2. Versão vigente

| Item | Valor | Fonte |
|---|---|---|
| PAD (Programa Autenticador de Dados) | **versão 20.0.0.0** (ciclo 2025) | [FONTE: IGAM noticia 2951; TCE-RS notícia "versão de teste do PAD para 2025"] |
| MT Volume V (Informações Complementares — Lei 4.320/64) | **Revisão 06**, *"Válido a partir de junho de 2024"*, data rev. **26/03/2024**, identificador **MT-ASCE-0105** | [FONTE: PDF MT-ASCE-0105-06-MT-Volume-V, extração local] |
| MT Volume I / Volume II | Volume I *"atualizado para 2025"*; Volume II disponível no repositório (`MT_Vol_II_SiapcPAD_6404.pdf`) | [FONTE: TCE-RS portal; busca] **[revisão/data exata: a confirmar — servidor TCE bloqueia fetch direto (403)]** |
| Base normativa | Resolução TCE-RS nº **766/2007**, nº **883/2010**, nº **1.134/2020**; Instrução Normativa nº **25/2007** e nº **03/2011** | [FONTE: MT Vol V rev.06] |
| Periodicidade da remessa SIAPC/PAD | **Mensal**, entrega em até **30 dias corridos** após o fim do período de competência (regra a partir de jan/2019) | [FONTE: TCE-RS "Perguntas Frequentes" / busca] **[calendário 2026 oficial: a confirmar]** |

> Nota: o Vol V (Informações Complementares) é o que cobre Folha, Receita Pública, Diário Geral, etc.
> O **leiaute contábil/orçamentário do balancete** (PCASP/MSC, o núcleo da remessa) está distribuído
> nos **Volumes I/II** — precisamos baixar esses PDFs diretamente para mapear os registros do balancete. **[a confirmar]**

---

## 3. Onde obter (fontes oficiais)

- **Portal SIAPC (manuais + download PAD):** `https://tcers.tc.br/sistemas-de-controle-externo/` (seção SIAPC). [FONTE: TCE-RS]
- **Portal legado (índice SIAPC):** `http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc` [FONTE: TCE-RS]
- **MT Volume V (rev.06, jun/2024) — PDF:** `http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf`
  (espelho MPC-RS: `https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf`) [FONTE: TCE-RS / MPC-RS]
- **MT Volume II — PDF:** `https://tcers.tc.br/repo/SIAPC/MANUAL/MT_Vol_II_SiapcPAD_6404.pdf` [FONTE: busca TCE-RS]
- **Resumo de Leiaute (Vol V):** `http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf` [FONTE: TCE-RS]

> ⚠️ Operacional: o host `tcers.tc.br` retorna **HTTP 403** a fetch automatizado/CI.
> Para baixar os PDFs em pipeline, usar User-Agent de navegador ou download manual e versionar o
> PDF no repo (`docs/estudo/fontes/`). [FONTE: tentativas de WebFetch nesta sessão]

---

## 4. Estrutura campo-a-campo (formato a replicar nos nossos modelos posicionais)

Os MTs descrevem cada arquivo `.TXT` com colunas **Descrição do Campo | Tipo | Bytes | Colunas | Observações**.
Exemplo real extraído do **MT Vol V rev.06** (cabeçalho padrão dos arquivos de dados): [FONTE: extração local do PDF]

| Descrição do Campo | Tipo | Bytes | Colunas (pos.) | Observações |
|---|---|---|---|---|
| CNPJ do Setor de Governo (órgão/entidade) | Numérico | 14 | 1 a 14 | CNPJ junto ao Min. da Fazenda |
| Data Inicial da Informação | Data | 8 | 15 a 22 | 1º jan do exercício, `ddmmaaaa` |
| Data Final da Informação | Data | 8 | 23 a 30 | data final, `ddmmaaaa` |
| Data da Geração do Arquivo | Data | 8 | 31 a 38 | `ddmmaaaa` |
| Nome do Setor de Governo | Caractere | 80 | 39 a 118 | nome do órgão/entidade |
| Código da Remessa | Numérico | 12 | 119 a 130 | identificador exclusivo da remessa (análogo ao código de barras) |

> Exemplo de linha (Vol V): `99999999000199 01012001 31122001 31012002 Prefeitura...(até col.118) 000000000001`

Esse é exatamente o nível de detalhe que substitui os `TODO(MT-2026)`: para cada registro do nosso
gerador e do `EValidadorLocalSiapc`, ancorar **posição inicial, tamanho (bytes), tipo (Numérico/Data/Caractere)**
e regra de preenchimento conforme o volume correspondente.

---

## 5. Mudanças anunciadas para 2026

- **Balanço Financeiro** e **Demonstração dos Fluxos de Caixa (DFC)** passarão a ser **confeccionados pelo
  TCE-RS** para o **encerramento de 2026** (apresentado na 26ª edição do evento SIAPC, painel SIAPC/PAD
  dos auditores). [FONTE: TCE-RS — notícia 26ª edição evento SIAPC]
- Implicação para nós: o leiaute desses demonstrativos para 2026 pode **alterar/ampliar registros da
  remessa** (ou ser derivado pelo próprio Tribunal a partir dos dados já enviados). **Verificar se exige
  novos arquivos `.TXT` ou apenas qualifica dados existentes.** **[a confirmar — leiaute 2026 não localizado]**
- **Balanço Patrimonial**: citado como já presente no conjunto de demonstrações da remessa; sem mudança
  estrutural anunciada explicitamente para 2026. **[a confirmar]**

---

## 6. Próximos passos para fechar os TODO(MT-2026)

1. Baixar (com UA de navegador) e versionar **Vol I, Vol II e Vol V** em `docs/estudo/fontes/`. **[a fazer]**
2. Extrair, por arquivo `.TXT`, a tabela posicional e gerar um **mapa de campos** (JSON) que alimente
   tanto o gerador quanto o `EValidadorLocalSiapc`. **[a fazer]**
3. Confrontar nossa simulação local com o **PAD 20.0.0.0** oficial (rodar a remessa no autenticador real). **[a fazer]**
4. Monitorar publicação do **leiaute 2026 (Balanço Financeiro/DFC)** no portal SIAPC. **[a fazer]**

---

### Fontes
- TCE-RS — Sistemas de Controle Externo / SIAPC: https://tcers.tc.br/sistemas-de-controle-externo/
- TCE-RS (portal legado) — SIAPC: http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- MT Volume V (rev.06, jun/2024): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf
- MT Volume V (espelho MPC-RS): https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf
- MT Volume II: https://tcers.tc.br/repo/SIAPC/MANUAL/MT_Vol_II_SiapcPAD_6404.pdf
- Resumo de Leiaute (Vol V): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
- TCE-RS — PAD versão de teste 2025: https://tcers.tc.br/noticia/tce-disponibiliza-versao-de-teste-do-programa-autenticador-de-dados-para-2025/
- TCE-RS — 26ª edição evento SIAPC (mudanças 2026): https://tcers.tc.br/noticia/tce-rs-realiza-26a-edicao-do-evento-do-siapc-com-orientacoes-tecnicas-para-gestores-municipais/
- IGAM — versão 20.0.0.0 do SIAPC-PAD: https://www.igam.com.br/detalhe-noticia-2951
- TCE-RS — Perguntas Frequentes SIAPC: http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf

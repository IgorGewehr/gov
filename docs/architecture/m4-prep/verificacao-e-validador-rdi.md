# Verificação adversarial — pesquisa "e-Validador (RVE) e RDI" (M4 / Transparencia)

> **Papel:** cético / auditor de fatos. Tentei **refutar** cada afirmação factual de
> `pesquisa-e-validador-rdi.md` contra **fonte oficial** (TCE-RS, STN). Cada item recebe:
> **CONFIRMADO** (com URL/doc oficial) · **PLAUSÍVEL-SEM-FONTE** · **INCERTO/CONTRADITÓRIO**.
> Artefatos baixados e extraídos byte-a-byte com `pdftotext -layout` nesta sessão.
>
> **Data da verificação:** 2026-06-22. **Exercício-alvo:** 2026.

---

## Placar

- **CONFIRMADO (fonte oficial): 11**
- **PLAUSÍVEL-SEM-FONTE: 3**
- **INCERTO/CONTRADITÓRIO: 4**

### Os 3 MAIORES RISCOS (não implementar sem doc oficial vigente)

1. **IN vigente ERRADA na pesquisa.** A pesquisa aponta IN 13/2021 / 4/2021 / 6/2019 como
   candidatas a vigentes em 2026. **Refutado:** a norma vigente é a **IN nº 8/2025 (23/09/2025),
   que revoga a IN nº 5/2024** (a qual revogou a IN 18/2023, e assim sucessivamente). Toda a
   cadeia 6/2019→…→18/2023→5/2024→8/2025 supera as INs citadas. **Risco:** parametrizar críticas,
   percentuais (ex.: limite de pessoal passou a **81% em 2025**, alinhado à 15ª ed. do MDF/STN) e
   prazos com base em norma revogada. **Ação:** obter o texto integral da **IN 8/2025** + a
   **Tabela do PAD** e o **Manual do PAD do exercício 2026** antes de codar qualquer crítica.

2. **Leiautes/ZIP confirmados, porém em manual de 2010 — mudança real anunciada para 2026.**
   A estrutura do nome do ZIP e o cabeçalho/FINALIZADOR foram **confirmados byte-a-byte**, mas no
   **MT Vol V Versão 2.0 / Set/2010**. O TCE-RS anunciou (evento SIAPC, 27/11/2025) **Balanço
   Financeiro + Demonstração dos Fluxos de Caixa para o encerramento de 2026** — ou seja, **mudança
   de leiaute/novos arquivos**. **Risco:** congelar leiaute de 2010. **Ação:** baixar a versão
   vigente do MT (há mirror mais novo "MT-ASCE-0105-06" no MPC-RS) e os Manuais 2026 do portal SIAPC.

3. **Modelo de assinatura do RVE/RDI mal-atribuído na pesquisa.** A pesquisa diz "Remessa Regular =
   RVE (Responsável + Contabilista) = **2 assinaturas**" e trata "4 a 5 assinaturas" como exclusivo
   de meses de fechamento. **Contraditório com o FAQ oficial:** o quadro do FAQ mostra **todo mês**
   com **Remessa Regular + Remessa Complementar** somando **4 assinaturas** (Regular: Responsável +
   Contabilista; Complementar acrescenta RVE Responsável/Contabilista + **RDI: Responsável + Resp.
   Controle Interno + Resp. Folha de Pagamento**), subindo a **5** nos meses de fechamento
   (abr/jun/ago/dez, com RGF + MCI). **Risco:** gate de assinaturas incompleto no `EnviarRemessaTce`.
   **Ação:** modelar a matriz de assinaturas por mês/tipo de remessa direto do FAQ (quadro abaixo).

---

## 1. Nomenclatura das siglas

| # | Afirmação da pesquisa | Veredito | Fonte |
|---|---|---|---|
| 1.1 | **RVE = "Relatório de Validação e Encaminhamento"** (não "Envio") | **CONFIRMADO** | Cabeçalho literal em RVE real da CM de São Pedro do Sul, **PAD 20.0.0.5**: `Relatório de Validação e Encaminhamento - RVE`. |
| 1.2 | **RDI = "Relatório de Dados e Informações"** (não "Identificação") | **CONFIRMADO (duplo)** | (a) Título da **Seção 6 do MT Vol V** (oficial TCE-RS): `RELATÓRIO DE DADOS E INFORMAÇÕES - RDI`. (b) RDI real (Brochier/TCE portal): "Relatório de Dados e Informações - RDI". Refuta o enunciado "Relatório de Identificação". |
| 1.3 | RDI tem subtítulo fixo "Solicitação Formal" | **INCERTO/CONTRADITÓRIO** | O subtítulo **varia**: visto "RDI - Folha de Pagamento" (Brochier) e "RDI - Solicitação Formal" (na pesquisa). Não tratar como literal fixo. |

**Fonte 1.1:** https://camarasps.rs.gov.br/wp-content/uploads/jet-engine-forms/1/2025/08/RVE-PAD.pdf
**Fonte 1.2a:** http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf (Seção 6)
**Fonte 1.2b:** https://brochier.rs.gov.br/transparencia/view/316/relatorio-de-dados-e-informacoes-rdi-folha-de-pagamento

---

## 2. Pendências x Avisos (o que bloqueia a remessa)

| # | Afirmação | Veredito | Fonte |
|---|---|---|---|
| 2.1 | **Pendências/Erros bloqueiam a geração da remessa** | **CONFIRMADO** | Manual SICOE (TCE-RS), texto literal: *"Pendências são erros que impedem a geração da remessa."* |
| 2.2 | **Avisos NÃO bloqueiam** geração/envio | **CONFIRMADO** | Manual SICOE: *"Os alertas não impedem a geração e o envio da remessa…"* |
| 2.3 | "Relatório de Críticas de Validação" é o nome do relatório que lista Pendências e Avisos | **CONFIRMADO** | Manual SICOE: botão *"Imprimir: Gera 'Relatório de Críticas de Validação', contendo Pendências e Avisos."* |
| 2.4 | Colunas da tabela de críticas: `Nome do Arquivo, Cód. de Erro, Linha, Campo, Status, Descrição` | **CONFIRMADO** | RVE real CM SPS, seção `4. Avisos Exibidos na Verificação dos Arquivos Texto`: cabeçalho idêntico. |
| 2.5 | Status observado `AVISO` e existência de "justificados" | **CONFIRMADO com correção** | No RVE real o **status** é `AVISO`; as justificativas aparecem em **seção separada `4.2 Avisos Justificados pela Entidade`**, **não** como valor de status `JUSTIF.`. A pesquisa modelou `JUSTIF.` como valor de Status — **rever**: pode ser uma seção, não um enum. |
| 2.6 | `EMP_63` = licitação não cadastrada no Licitacon = **Aviso** | **CONFIRMADO** | RVE real CM SPS: `EMPENHO.TXT EMP_63 ... AVISO ... não cadastrada no Licitacon.` |
| 2.7 | `EMP_77` (Aviso) e `4810_55` (Aviso) vistos em relatórios reais | **PLAUSÍVEL-SEM-FONTE** | Não reencontrados nos PDFs reabertos nesta sessão; vieram dos artefatos citados pela pesquisa (PAD 18.x). Plausível, mas reconfirmar no exercício-alvo. |
| 2.8 | Códigos `EMP_73`/`EMP_86`/`GEN_02`/`BDP_52`/`VER_04`/etc. e **limiares 20% / 80%** classificados como Erro | **INCERTO/CONTRADITÓRIO** | Fonte é **wiki do fornecedor Thema**, NÃO o TCE. A própria pesquisa marca como "reconfirmar". **Não implementar limiares/classificação a partir do Thema.** Autoritativo = **Tabela do PAD + Manual do PAD do exercício**. |

**Fonte SICOE:** https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf (seções 3.5 Pendências, 3.6 Avisos, telas Validar/Imprimir). SICOE regulado pela **Resolução nº 1.074/2017**.
**Fonte RVE real:** https://camarasps.rs.gov.br/wp-content/uploads/jet-engine-forms/1/2025/08/RVE-PAD.pdf

---

## 3. Fluxo de geração/envio e assinaturas

| # | Afirmação | Veredito | Fonte |
|---|---|---|---|
| 3.1 | Dados cadastrais vêm do **SISCAD** (não se digita nas telas do PAD/MCI) | **CONFIRMADO** | FAQ Q1: *"Desde 2014 o SIAPC (PAD E MCI) está integrado com o Sistema de Cadastro Único – SISCAD… todas as informações cadastrais devem ser registradas no SISCAD."* |
| 3.2 | Assinatura digital no Processo Eletrônico com **certificado ICP-Brasil** conectado | **CONFIRMADO** | FAQ Q3: *"Portal > Jurisdicionado > Processo Eletrônico > Acesso ao Sistema (necessário estar com o certificado digital conectado)…"* |
| 3.3 | PAD **exige automaticamente** as assinaturas conforme o mês, buscando vínculos do SISCAD | **CONFIRMADO** | FAQ Q4: *"O PAD exige as assinaturas, de forma automática, dependendo do mês busca os vínculos que estão cadastrados no SISCAD…"* |
| 3.4 | Após assinar é preciso **enviar o e-protocolo** | **CONFIRMADO** | FAQ Q5: *"É necessário fazer o envio do e-protocolo para completar a entrega."* |
| 3.5 | Status de conferência: pendente / concluída / carregada | **CONFIRMADO** | FAQ Q6: *"…verifique se a remessa está pendente, concluída ou carregada."* |
| 3.6 | **RGF/MCI semestrais p/ municípios ≤ 50 mil hab** (caso Maximiliano de Almeida) | **CONFIRMADO** | FAQ Q11: *"…quadrimestral para municípios com mais de 50 mil habitantes, e semestral para municípios com até 50 mil habitantes."* |
| 3.7 | Folha de Pagamento mensal (até 30 dias) | **CONFIRMADO (+norma)** | FAQ Q9: previsto na **Resolução nº 1099/2018**; mensal, até 30 dias corridos. (A pesquisa não citava esta resolução.) |
| 3.8 | "Remessa Regular = 2 assinaturas (RVE: Responsável + Contabilista)" como total | **INCERTO/CONTRADITÓRIO** | Ver Risco #3. O FAQ não totaliza "2" em mês algum; o total mensal é **4** (Regular+Complementar) e **5** em fechamento. A composição RVE=Responsável+Contabilista está certa; o **enquadramento do total está errado**. |
| 3.9 | RDI assinado por Responsável + Resp. Controle Interno + Resp. Folha de Pagamento | **CONFIRMADO** | FAQ Q4 (quadro): bloco RDI lista exatamente `Responsável / Resp. Controle Interno / Resp. Folha de Pagamento`. |
| 3.10 | "Pré-validação é recomendação; validação do PAD é condição de geração" | **PLAUSÍVEL-SEM-FONTE** | Coerente com SICOE (Pendência impede geração) e com a arquitetura do fluxo, mas a frase "recomendação" é do fornecedor. A obrigatoriedade *de fato* (PAD é único caminho de geração) é uma inferência razoável, não citação normativa. |

### Matriz de assinaturas (extraída literal do FAQ Q4 — usar como verdade para o gate)

Todo mês (Executivo e Legislativo):
- **Remessa Regular → RVE:** Responsável + Contabilista.
- **Remessa Complementar → RVE** (Responsável + Contabilista) **+ RDI** (Responsável + Resp. Controle Interno + Resp. Folha de Pagamento).
- **Total mensal-base: 4 assinaturas.**
- **Meses de fechamento (abr / jun / ago / dez = entregas mai/jul/set/jan): 5 assinaturas**, somando **RGF** (Responsável + Resp. Adm. Financeira + Resp. Controle Interno) e **MCI** (Responsável + Resp. Controle Interno).
- Periodicidade RGF/MCI: **quadrimestral** (>50 mil hab) ou **semestral** (≤50 mil hab → Maximiliano de Almeida).

**Fonte:** http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf

---

## 4. Leiaute dos arquivos e nome da remessa (ZIP)

| # | Afirmação | Veredito | Fonte (MT Vol V) |
|---|---|---|---|
| 4.1 | Cabeçalho na 1ª linha com CNPJ + **Código da Remessa** (12 dígitos) | **CONFIRMADO** | MT Vol V §"Cabeçalho": *"Deverá constar obrigatoriamente na 1ª linha de todos os Arquivos, o CNPJ e o nome do Setor de Governo… Código da Remessa, Numérico 12, posições 119 a 130."* |
| 4.2 | Linha **FINALIZADOR** `FINALIZADOR0000000000` (qtde de registros, 10 bytes) | **CONFIRMADO** | MT Vol V §FINALIZADOR: *"…uma linha totalizadora contendo a palavra 'FINALIZADOR' … Tipo Numérico, 10 bytes: FINALIZADOR0000000000."* |
| 4.3 | Nome do ZIP estruturado `CNPJ.dataIni.dataFin.dataGer.Tipo.CodRemessa.zip` | **CONFIRMADO** | MT Vol V §5: campos CNPJ(14) `.` dataIni(ddmmaaaa) `.` dataFin `.` dataGer `.` Tipo(P/C/A/F/E/S/O) `.` CodRemessa(12) `.` ext(zip). Ex.: `99999999000199.01012007.31052007.15062007.P.000000000010.zip`. |
| 4.4 | Arquivos: TCE_4810 (Folha), TCE_4820 (Cad. Funcionários), TCE_4010 (Receita), EMPENHO, etc. | **CONFIRMADO** | MT Vol V sumário/seções: `TCE_4810.TXT - Folha de Pagamento`, `TCE_4820.TXT - Cadastro de Funcionários`, `TCE_4010.TXT – Receita Pública`, `TCE_4111.TXT – Livro Diário Geral`. |
| 4.5 | Esses leiautes valem para 2026 | **INCERTO/CONTRADITÓRIO** | MT Vol V verificado é **Versão 2.0 / Set/2010**. Há mudança anunciada (BF+DFC para encerramento 2026). **Não congelar como vigente.** Obter MT/leiaute 2026 do portal SIAPC. |

**Fonte:** http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
**Mirror mais recente (verificar versão):** https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf

---

## 5. Normativos (cadeia de INs) — correção importante

| # | Afirmação da pesquisa | Veredito | Fonte |
|---|---|---|---|
| 5.1 | "IN 13/2021 / 4/2021 / 6/2019 — confirmar vigente em 2026" | **INCERTO/CONTRADITÓRIO → corrigido** | **Vigente = IN nº 8/2025 (23/09/2025), revoga IN nº 5/2024.** Cadeia: 6/2019 → … → 18/2023 → 5/2024 → 8/2025. As INs da pesquisa estão revogadas. |
| 5.2 | (novo) Limite de despesa com pessoal | **CONFIRMADO (contexto)** | IN 8/2025: percentual permitido **81% para 2025** (ajuste pela **15ª ed. do MDF/STN**, 16/09/2025). **Parametrizar por exercício.** |
| 5.3 | (novo) Resolução-base do PAD | **PLAUSÍVEL-SEM-FONTE (verificar)** | Há Resolução nº **1134/2020** com a mesma ementa do PAD/RREO/RGF; SICOE pela **1074/2017**; folha pela **1099/2018**. Confirmar qual resolução-quadro está vigente para o PAD em 2026. |

**Fontes (secundárias, exigem confirmação no DOE/portal TCE):**
- IN 8/2025 (notícia técnica): https://inlegis.com.br/alerta-tce-rs-publica-instrucao-normativa-no-8-2025-com-novas-regras-para-relatorios-fiscais-dos-municipios/
- IN 5/2024 (revogada): https://atosoficiais.com.br/tcers/instrucao-normativa-n-5-2024-...
- Evento SIAPC 26ª ed. (BF+DFC 2026): https://tcers.tc.br/noticia/tce-rs-realiza-26a-edicao-do-evento-do-siapc-com-orientacoes-tecnicas-para-gestores-municipais/

> ⚠️ `atosoficiais.com.br` retorna **HTTP 403** a robôs — o texto integral das INs **não foi lido**
> nesta sessão; baixar do **Diário Oficial Eletrônico (DET)** / portal TCE-RS para byte-a-byte.

---

## 6. NÃO IMPLEMENTAR sem o documento oficial vigente

1. **Classificação Erro/Aviso e limiares (20%/80%/81%…) por código** → só a partir da **Tabela do PAD**
   + **Manual do PAD 2026**. **Nunca** a partir da wiki Thema (fornecedor).
2. **Lista fechada de valores de `Status`** → reavaliar o modelo: no artefato real `AVISO` é status e
   "justificados" é **seção** (`4.2`), não valor `JUSTIF.`. Confirmar enum no Manual do PAD 2026.
3. **Leiaute dos `.TXT`, cabeçalho/FINALIZADOR e nome do ZIP para 2026** → confirmados em manual de
   2010; **obter o MT/leiaute vigente** (mudança BF+DFC anunciada p/ encerramento 2026).
4. **Critérios de RREO/RGF, percentuais e prazos** → a partir da **IN 8/2025** (texto integral), não
   das INs citadas na pesquisa.
5. **Assinatura do ZIP por nós (A1/Key Vault) vs. assinatura no Processo Eletrônico do TCE** → o FAQ
   descreve a assinatura **no portal** com certificado do responsável; **não há confirmação** de que o
   SICOE aceite ZIP pré-assinado por nós. Premissa segura: **geramos o ZIP; o ente assina/protocola no
   Processo Eletrônico**. `[a confirmar — Manual SICOE §3.8 "Documentos a enviar na assinatura/envio"]`.

---

## 7. Fontes oficiais usadas nesta verificação (extraídas/lidas)

- **FAQ SIAPC** (assinaturas, fluxo, e-protocolo, RGF semestral, Res. 1099/2018) — **extraído byte-a-byte**:
  http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- **Manual SICOE** (Pendências/Avisos, Relatório de Críticas de Validação, Res. 1074/2017) — **extraído**:
  https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf
- **MT Vol V** (cabeçalho, FINALIZADOR, nome do ZIP, RDI, arquivos TXT) — **extraído**:
  http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
- **RVE real** CM São Pedro do Sul (PAD 20.0.0.5; colunas da tabela, EMP_63 AVISO, §4.2 justificados) — **extraído**:
  https://camarasps.rs.gov.br/wp-content/uploads/jet-engine-forms/1/2025/08/RVE-PAD.pdf
- **RDI real** Brochier ("Relatório de Dados e Informações - RDI - Folha de Pagamento"):
  https://brochier.rs.gov.br/transparencia/view/316/relatorio-de-dados-e-informacoes-rdi-folha-de-pagamento
- **IN 8/2025 / 5/2024 / evento SIAPC 2026** — fontes secundárias (confirmar no DET/portal TCE).

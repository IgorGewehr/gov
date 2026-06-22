# TCE-RS — e-Validador (RVE) e RDI: regras de validação, bloqueio de remessa e validação local

> M4 (Prestação de Contas TCE-RS). Pesquisa para o módulo **Transparencia** (RemessaTce, ArquivoRemessa,
> RegistroLeiaute, MatrizSaldos, EnviarRemessaTce — hoje STUB). CLAUDE.md §8/§16.
>
> **REGRA DE OURO:** nada de leiaute/protocolo inventado. Toda afirmação factual tem FONTE (URL) ou está
> marcada `[a confirmar — <doc oficial>]`. Esta pesquisa foi feita sobre **artefatos reais** (RVE e RDI em PDF
> de municípios do RS, baixados e extraídos byte-a-byte com `pdftotext`) + FAQ oficial do TCE-RS + wiki do
> fornecedor Thema. Onde o material real é de exercício antigo (PAD 18.x, 2018/2019), está sinalizado para
> reconfirmar no exercício-alvo (2026).

---

## 0. Conclusões diretas (TL;DR para implementação)

1. **Há DOIS "validadores" distintos** — não confundir:
   - **(a) Validação LOCAL no PAD/SIAPC** (programa do ente, instalado na máquina): lê os `.TXT`, roda as
     **críticas de validação** e, se passar, **gera a remessa (ZIP) + imprime o RVE/RDI**. É aqui que erros
     **bloqueiam**.
   - **(b) Validação no envio (SICOE / Processo Eletrônico do TCE)**: recebe o ZIP, reconfere e protocola.
2. **RVE** = *Relatório de Validação e Encaminhamento* (não "Envio"). É o comprovante/relatório-resumo
   gerado pelo PAD **junto com** a remessa; contém os dados contábeis sintéticos + a **lista de Avisos** da
   verificação dos arquivos texto. Assinado digitalmente (ICP-Brasil) por **Responsável + Contabilista**.
   — Corrigir o glossário do spec antigo (`tce-rs-siapc-pad.md §1`) que diz "Relatório de Validação e Envio".
3. **RDI** = *Relatório de Dados e Informações* (**não** "Identificação"). É o relatório da **Remessa
   Complementar** (Folha de Pagamento, Receita Pública etc.). Assinado por **Responsável + Resp. Controle
   Interno + Resp. Folha de Pagamento**. — Corrigir o spec antigo que expande RDI como "Relatório de Dados
   e *Informações*" no §1 mas como "Relatório de *Identificação*" no enunciado da tarefa: o nome oficial
   impresso no PDF é **"Relatório de Dados e Informações - RDI"**.
4. **Erros (Pendências) BLOQUEIAM a geração da remessa**; **Avisos NÃO bloqueiam** (a remessa é gerada e
   enviada com avisos, alguns exigindo justificativa). Confirmado por: (i) wiki Thema descreve códigos que
   "podem impedir o envio da remessa"; (ii) os RVE/RDI reais analisados foram gerados com sucesso e contêm
   **apenas seção de Avisos** (status `AVISO`/`JUSTIF.`), nenhuma seção de erro — coerente com "erro bloqueia,
   logo não chega a gerar relatório".
5. **A validação local é, na prática, obrigatória de fato**, porque é o próprio PAD que **gera a remessa** —
   não existe caminho de enviar um ZIP que não tenha passado pelo PAD. A wiki do fornecedor trata a *pré*-validação
   (rodar o relatório de validação antes) como **recomendação**; a validação do PAD em si é **condição de geração**.
   `[a confirmar — texto normativo explícito que classifique erro x aviso e obrigatoriedade: Manual do PAD do
   exercício 2026 + IN vigente]`.

---

## 1. Os relatórios oficiais (estrutura REAL extraída de PDF)

### 1.1 RVE — Relatório de Validação e Encaminhamento

Cabeçalho impresso (literal, RVE real de PM de Boa Vista do Cadeado, exercício 2018, PAD **18.0.0.5**):

```
ESTADO DO RIO GRANDE DO SUL — TRIBUNAL DE CONTAS DO ESTADO
SIAPC - Sistema de Informações para Auditoria e Prestação de Contas
Programa Autenticador de Dados - PAD Versão: 18.0.0.5
Relatório de Validação e Encaminhamento - RVE
PM DE BOA VISTA DO CADEADO   ORGÃO Nº: 88022   CNPJ: 04216132000106   01/01/2018 a 31/12/2018

Neste RVE estão incluídos:                       Sim   Não
   Modelos da LRF do Executivo
   Deverá ser consolidado para fins de LRF
```

Seções observadas no RVE real (nomes exatos):
- `1. Informações da Entidade` → `1.1 Dados Cadastrais`, `1.2 Prestadora de Serviços de Informática`,
  `1.3 Sistemas Informatizados`, `1.4 Participação com Consórcio Público`, `1.6/1.7 Entidades da Adm. Indireta`.
- `2. Informações Contábeis` (balancetes, receitas, despesas, `2.3.3 Disponibilidade Financeira` etc.).
- `3. Índices Constitucionais` (Saúde, Educação/MDE+FUNDEB, art. 29 CF — pessoal/Legislativo).
- **`4. Avisos Exibidos na Verificação dos Arquivos Texto`** → `4.1 Avisos Exibidos` (a tabela de críticas).

### 1.2 RDI — Relatório de Dados e Informações

Cabeçalho impresso (literal, RDI real):

```
Relatório de Dados e Informações - RDI - Solicitação Formal
Neste RDI estão incluídos:                       Sim   Não
   Folha de Pagamento
   Receita Pública
```

Seções observadas: `3. Resumos: Folha de Pagamento, Cadastro de Funcionários e Registros de Vantagens,
Descontos e Totalizadores` → `3.1 Resumo Mensal da Folha de Pagamento`; e
**`5. Avisos Exibidos na Verificação dos Arquivos Texto`** → `5.1 Avisos Exibidos`.

### 1.3 Tabela de críticas (formato REAL — colunas exatas)

Cabeçalho da tabela de críticas, **idêntico em RVE e RDI** (literal):

```
Nome do Arquivo        Cód. de Erro    Linha   Campo   Status      Descrição
EMPENHO.TXT            EMP_77          6       9       AVISO       Linha 6 - elemento da despesa fora dos padrões ... Portaria Interministerial STN/SOF nº 163...
TCE_4810.TXT           4810_55         0       0       AVISO       Linha: 16441 - O campo "Indicador de Incidência do RPPS" deve ser ... "S" ou ...
TCE_4820.TXT           4820_37         0       0       JUSTIF.     Percentuais dos campos Endereço, Cidade, UF e/ou CEP em branco e/ou zerados: 0,05%.
```

**Modelagem para `RegistroLeiaute`/`ArquivoRemessa`:** cada crítica tem
`{ NomeArquivo, CodigoErro (ex.: EMP_77, 4810_55), Linha, Campo, Status, Descricao }`.
**Valores de `Status` observados:** `AVISO`, `JUSTIF.` (justificativa). **`ERRO`/`PENDÊNCIA` não aparecem nos
PDFs reais** porque remessa com erro não é gerada → o RVE/RDI só existe quando não há erro bloqueante.
`[a confirmar — lista fechada de valores de Status (ERRO, PENDÊNCIA, AVISO, JUSTIF.) no Manual do PAD do exercício]`.

---

## 2. O que BLOQUEIA a remessa (Erro/Pendência) x o que só alerta (Aviso)

**Modelo confirmado (duas fontes convergentes):**
- **Pendências/Erros = bloqueiam a geração da remessa.** Resultado de busca oficial: *"Pendências são erros que
  impedem a geração da remessa; Avisos são alertas importantes sobre os dados fornecidos, identificados pelo
  validador."* (terminologia do **Validador / Relatório de Críticas de Validação**, TCE-RS / SICOE).
- **Avisos = não bloqueiam**; remessa é gerada e enviada. Alguns avisos têm status `JUSTIF.` (exigem
  justificativa do ente, mas não impedem).

**Exemplos REAIS de códigos de crítica e sua classificação** (wiki do fornecedor Thema — "Geração de Arquivos
SIAPC/PAD"; ⚠️ fonte de **fornecedor**, não TCE; cada código/limiar precisa reconfirmação no Manual do PAD do
exercício):

| Código | Descrição | Tipo (Thema) | Bloqueia? |
|---|---|---|---|
| `EMP_73` | Modalidade "NSA - Não se aplica" acima de 20% dos registros | **Erro** | Sim, se > 20% |
| `EMP_86` | Licitações cadastradas no Licitacon abaixo de 80% | **Erro** | Sim, se < 80% |
| `GEN_02` | "Complemento do Recurso Vinculado" não numérico | Erro | Sim |
| `BDP_52` | Dotação insuficiente para empenhos | Erro | Sim |
| `VER_04` | Cabeçalho diferente entre arquivos | Erro | Sim |
| `CTV_19/33/34` | Inconsistências em contas bancárias | Erro | Sim |
| `RET_30..35` | Meta de Arrecadação zerada | Erro | Sim |
| `EMP_77` | Elemento de despesa fora do padrão Portaria STN/SOF 163/2001 | Aviso | Não (visto em RVE real) |
| `4810_55` | Campo "Indicador de Incidência do RPPS" não preenchido S/N | Aviso | Não (visto em RDI real) |
| `4820_37` | % de Endereço/Cidade/UF/CEP em branco/zerados | Justificativa | Não |
| `CRE_10` | Percentuais de UF inválidos | Aviso | Não |
| `EMP_63` | Licitação não cadastrada no Licitacon | Aviso | Não |
| `LIQ_38` | Alto % de Nota Fiscal não informada | Aviso | Não |

> **Importante p/ o gerador:** os limiares (20%, 80%, 0%) e a própria classificação Erro/Aviso **mudam por
> exercício** e por versão do PAD. **Não hard-codar** (CLAUDE.md §7 — regras parametrizáveis por tenant). O
> conjunto autoritativo de códigos é a **Tabela do PAD** + Manual Técnico do exercício. `[a confirmar — Tabela
> do PAD 2026 + Manual do PAD 2026]`.

---

## 3. Há validação local obrigatória antes do envio?

**Sim, de fato — pela arquitetura do fluxo:** o **PAD é o único caminho** para produzir a remessa. Ele valida
e, **somente se não houver erro bloqueante, gera o ZIP + o RVE/RDI** que serão assinados e protocolados. Não há
como protocolar uma remessa que não tenha passado pela validação do PAD.

Fluxo confirmado (FAQ oficial TCE-RS):
1. Dados cadastrais vêm do **SISCAD** (não se digita nas telas do PAD/MCI).
2. Roda-se o PAD/MCI → valida → gera remessa + RVE/RDI.
3. **Envio pela internet** dos arquivos.
4. **Assinatura digital** (Portal > Jurisdicionado > Processo Eletrônico > Acesso ao Sistema, com **certificado
   ICP-Brasil** conectado). O PAD **exige automaticamente** as assinaturas conforme o mês (busca vínculos no
   SISCAD).
5. **Envio do e-protocolo** para completar a entrega.
6. Conferência em *Relatórios e Recibos de envio* (status: pendente / concluída / carregada).

**Pré-validação (rodar o "Relatório de Validação do SIAPC/PAD" ANTES de validar no PAD do TCE)** é tratada pela
wiki do fornecedor como **recomendação** ("antes de gerar e validar no programa do TCE/RS, façam a geração deste
relatório"), para corrigir inconsistências localmente. Não é um passo normativo separado — é a mesma engine de
críticas rodada antecipadamente.

**Assinaturas por tipo de remessa (FAQ oficial — quadro mensal):**
- **Remessa Regular:** RVE → *Responsável* + *Contabilista* (2 assinaturas).
- **Remessa Complementar:** RVE (*Responsável* + *Contabilista*) **+** RDI (*Responsável* + *Resp. Controle
  Interno* + *Resp. Folha de Pagamento*).
- Em meses de fechamento (abr/jun/ago/dez): somam-se **RGF** e **MCI** com suas próprias assinaturas
  (Responsável + Resp. Adm. Financeira + Resp. Controle Interno). Total varia de 4 a 5 assinaturas.
- Maximiliano de Almeida (≤ 50 mil hab.) → **RGF/MCI semestrais** (1º sem. na entrega de junho; 2º na de janeiro).

---

## 4. Implicações de implementação (módulo Transparencia)

- **`RegistroLeiaute`** deve modelar a tabela de críticas: `CodigoErro`, `NomeArquivo`, `Linha`, `Campo`,
  `Status` (enum `Erro|Aviso|Justificativa` — `[confirmar valores exatos]`), `Descricao`. Mapear `Status==Erro`
  → **bloqueia** `EnviarRemessaTce`.
- **`MatrizSaldos`/`ArquivoRemessa`**: o ZIP segue o nome estruturado do MT Vol. V já documentado em
  `specs-oficiais/tce-rs-siapc-pad.md §2.3`; cabeçalho/finalizador idem §2.2.
- **`EnviarRemessaTce`**: gate de domínio = "remessa só pode ser enviada se a validação não retornou nenhuma
  crítica com `Status=Erro`". Avisos/justificativas passam (eventualmente exigindo campo de justificativa).
- **Assinatura A1 (ICP-Brasil)** dos relatórios via **Azure Key Vault por tenant** (CLAUDE.md §6). O TCE assina
  no **Processo Eletrônico** com certificado dos responsáveis; avaliar se replicamos a assinatura ou se apenas
  geramos o ZIP e o ente assina no portal (provável: **só geramos**; assinatura/protocolo é no portal do TCE).
  `[a confirmar — se o SICOE aceita ZIP pré-assinado por nós ou se a assinatura é exclusivamente no portal]`.
- **Não reimplementar o PAD.** Decidir: (a) gerar `.TXT` e delegar validação ao PAD do ente, ou (b) embutir as
  críticas localmente (precisa da Tabela do PAD + Manual). Recomendado começar por (a) e evoluir as críticas de
  maior valor (as que bloqueiam) como pré-validação nossa.

---

## 5. FONTES (URLs verificadas)

**Primárias / oficiais TCE-RS:**
- FAQ SIAPC (assinaturas RVE/RDI, fluxo, e-protocolo) — **baixado e extraído**:
  http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- RDI real (estrutura, "Neste RDI estão incluídos", seção 5 Avisos) — portal TCE:
  https://portal.tce.rs.gov.br/pcdi2/ws/relatorio/visualizar-complementar/1311841/156
- Portal SIAPC (Manuais / Tabela do PAD):
  http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- MT Vol. V (arquivos à disposição do TCE):
  http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf
- Manual SICOE (Relatório de Críticas de Validação: Pendências x Avisos):
  https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf

**RVE/RDI reais (artefatos de jurisdicionados, extraídos byte-a-byte):**
- RVE PM Boa Vista do Cadeado (PAD 18.0.0.5) — **baixado e extraído**:
  https://mail.boavistadocadeado.rs.gov.br/uploads/relatoriocp/19756/rve_6b18.pdf
- RVE Cacequi 2026 (modelo atual "Neste RVE estão incluídos"):
  https://cacequi.rs.gov.br/transparencia/Contas%20P%C3%BAblicas%20-%20Relat%C3%B3rios%20Fiscais/RVE/2026/12_54943ffacee6c25a8f4b1f6f6265335f.pdf
- RVE-PAD modelo Câmara São Pedro do Sul 2025:
  https://camarasps.rs.gov.br/wp-content/uploads/jet-engine-forms/1/2025/08/RVE-PAD.pdf

**Normativos (RVE/critérios PAD):**
- IN TCE-RS nº 13/2021 (critérios dos relatórios do PAD; define que dados enviados correspondem ao RVe;
  RREO/RGF) — ⚠️ atosoficiais bloqueia fetch automatizado (HTTP 403):
  https://atosoficiais.com.br/tcers/instrucao-normativa-n-13-2021-...
- IN TCE-RS nº 4/2021 e nº 6/2019 (versões anteriores) — confirmar qual vigente em 2026.

**Fornecedor (NÃO oficial — usar só como pista; reconfirmar no TCE):**
- Wiki Thema "Geração de Arquivos SIAPC/PAD" (códigos de crítica Erro x Aviso, limiares 20%/80%):
  https://wiki.thema.inf.br/wiki/help/671626

---

## 6. Pendências `[a confirmar — obter doc oficial]`

1. **Nome oficial fechado do RDI** — impresso "Relatório de Dados e Informações - RDI - Solicitação Formal".
   Corrigir glossário (tarefa pediu "Relatório de Identificação" — **não confere** com o PDF real).
2. **Lista fechada de valores de `Status`** da tabela de críticas (ERRO/PENDÊNCIA/AVISO/JUSTIF.) — **Manual do
   PAD do exercício 2026**.
3. **Classificação Erro x Aviso + limiares por código** — autoritativo é **Tabela do PAD 2026** + Manual
   Técnico do exercício (a tabela Thema é referência indireta).
4. **IN vigente em 2026** para critérios do PAD/RREO/RGF (13/2021 vs. 4/2021 vs. posterior) — texto integral
   (atosoficiais bloqueia robôs; baixar do Diário Oficial / portal TCE).
5. **Manual SICOE** byte-a-byte: confirmar terminologia exata "Relatório de Críticas de Validação",
   "Pendências", e se a validação no envio (SICOE) repete as críticas do PAD ou só confere integridade do ZIP.
6. **Assinatura do ZIP**: confirmar se geramos ZIP e o ente assina/protocola no Processo Eletrônico do TCE
   (provável) ou se o SICOE aceita pré-assinatura A1 nossa.
7. **Versão do PAD 2026** e mudanças de leiaute anunciadas (Balanço Patrimonial + DFC — evento SIAPC nov/2025).

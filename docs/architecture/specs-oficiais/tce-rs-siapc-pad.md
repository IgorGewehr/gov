# TCE-RS — Prestação de Contas SIAPC/PAD (Especificações Oficiais)

> Piloto: Maximiliano de Almeida/RS. Fonte primária verificada: Manual Técnico do SIAPC **Volume V (MT-ASCE-0105, Revisão 6, mar/2024, válido a partir de jun/2024)**, 36 páginas, baixado do espelho MPC-RS. Trechos de campos/larguras abaixo foram transcritos **literalmente** do PDF oficial. Onde o leiaute completo do exercício corrente não foi extraído integralmente, há marca `[obter leiaute oficial do exercício]`.
>
> ATENÇÃO (CLAUDE.md §16): NÃO inventar campos. Antes de codificar o gerador de remessa, baixar o leiaute do exercício-alvo (a Revisão muda anualmente; verificar a versão do PAD do ano).

---

## 1. Visão geral dos sistemas / siglas

| Sigla | Significado | Papel |
|---|---|---|
| **SIAPC** | Sistema de Informações para Auditoria e Prestação de Contas | Sistema guarda-chuva da prestação de contas municipal (entes regidos pela Lei Federal 4.320/64). |
| **PAD** | Programa Autenticador de Dados | Programa cliente que lê os arquivos-texto gerados pelo sistema contábil do ente, valida (consistência lógica/contábil), autentica e gera a remessa. Versão **20.0.0.0** divulgada para 2020; **nova versão a cada exercício** (ex.: versão de teste do PAD 2025 divulgada). `[obter versão do PAD do exercício-alvo]` |
| **MCI** | Modelo de Controle Interno | Relatório quadrimestral/semestral de controle interno, gerado em conjunto com o PAD. |
| **e-Validador / RVE** | Relatório de Validação e Envio | Validação dos arquivos contra os leiautes pré-estabelecidos; correção de erros antes do envio. O RVE é o comprovante de validação. |
| **RDI** | Relatório de Dados e Informações | Anexo da remessa **Complementar** (inclui Folha de Pagamento, controle interno etc.); assinado por Responsável, Resp. Controle Interno e Resp. Folha de Pagamento. |
| **SICOE** | Sistema de Controle Externo | Aplicação/canal de transmissão dos dados informatizados ao TCE-RS (instalada na máquina do ente; gera/valida arquivos por certificado digital). |
| **SISCAD** | Sistema de Cadastro Único | Desde 2014 o SIAPC (PAD e MCI) é integrado ao SISCAD — todo dado cadastral de responsáveis vem do SISCAD; o PAD busca os vínculos automaticamente. |

Fluxo (alto nível): sistema contábil do ente → gera arquivos `.TXT` (leiaute SIAPC) → **PAD/SICOE** valida e gera ZIP da remessa → envio pela Internet → **assinatura digital** (certificado) dos responsáveis → **e-protocolo** → Processo Eletrônico do TCE-RS. Desde o 1º bimestre de 2015 o SIAPC está totalmente integrado ao Processo Eletrônico.

---

## 2. Estrutura dos arquivos de dados (leiaute) — REGRAS GERAIS (transcrição literal MT Vol. V)

- **Formato:** texto, padrão **ASCII – ISO 8859-1 (Latin-1)**, tabular, sequencial.
- Cada registro = uma linha; **todas as linhas com o mesmo tamanho**; término em **CR/LF (hexa 0D0A)**.
- Não se aceitam campos compactados (packed decimal), zonados, binários, ponto flutuante, nem outra codificação de texto.

### 2.1 Convenção da coluna "Tipo"
- **Caractere** — alinhado à esquerda, preenchido com espaços em branco à direita.
- **Memorando** — tipo Caractere com tamanho de 256 a 65.535 posições.
- **Numérico** — alinhado à direita, zeros à esquerda; sem caracteres especiais; 0 casas decimais salvo definição na grade.
- **Valor** — alinhado à direita, zeros à esquerda; em Reais **com centavos**, sem ponto/vírgula; sinal `+`(2B) / `-`(2D) à esquerda do número quando necessário. Ex.: `R$ -240,00` em campo de 13 posições → `-000000024000`.
- **Data** — 8 posições, formato **ddmmaaaa** (ex.: `06/01/2013` → `06012013`).
- Campo sem dado: branco (Caractere) ou zeros (Numérico/Valor/Data).

### 2.2 Estrutura do arquivo (Cabeçalho / Corpo / Finalizador)

**Cabeçalho (obrigatório na 1ª linha de TODOS os arquivos):**

| Campo | Tipo | Bytes | Colunas |
|---|---|---|---|
| CNPJ do Setor de Governo (órgão/entidade) | Numérico | 14 | 1–14 |
| Data Inicial da Informação (1º jan do exercício) | Data | 8 | 15–22 |
| Data Final da Informação | Data | 8 | 23–30 |
| Data da Geração do Arquivo | Data | 8 | 31–38 |
| Nome do Setor de Governo | Caractere | 80 | 39–118 |
| Código da Remessa (identificador exclusivo, gerado pelo ente) | Numérico | 12 | 119–130 |

**Corpo:** a partir da 2ª linha; cada linha = 1 registro; todas com o mesmo tamanho.

**Finalizador:** última linha = palavra `FINALIZADOR` + quantidade de registros (Numérico, 10 bytes). Ex.: `FINALIZADOR0000000000`.

### 2.3 Nome da remessa (arquivo ZIP)

Arquivos reunidos em **um único ZIP**, nome estruturado (60 bytes):
`CNPJ(14).DataIni(8).DataFim(8).DataGer(8).Tipo(1).CodRemessa(12).ext(3)`
- Tipo de Setor de Governo: `P`=Prefeitura, `C`=Câmara, `A`=Autarquia, `F`=Fundação, `E`=Empresa, `S`=Consórcio, `O`=Outros.
- Exemplo oficial: `99999999000199.01012007.31052007.15062007.P.000000000010.zip`

---

## 3. Arquivos do leiaute (Vol. V — Informações Complementares à disposição do TCE)

> O Vol. V cobre os arquivos **complementares**. Os arquivos da **prestação de contas contábil principal** (orçamento, balancetes, empenhos, receita/despesa orçamentária, balanços) estão nos demais volumes do Manual SIAPC. `[obter leiaute oficial do exercício]` — Manuais Vol. I a IV + Tabela do PAD (ver §7).

### 3.1 Folha de Pagamento (Resolução 1099/2018 — envio MENSAL)
Arquivos: cadastro de funcionários, lançamentos (vantagens/descontos), totalizadores, rubricas com base legal.

- **TCE_4810.TXT** — Folha de Pagamento (lançamentos). Campos transcritos (início):
  - Código do Tipo da Folha — Numérico, 1, col 1–1 (1=Normal, 2=13º, 3=Férias, 4=Rescisão, 5=Complementar/Suplementar, 6=Afastamento, 9=Outros)
  - Código do Registro do Funcionário — Alfanumérico, 12, col 2–13
  - Data de Competência — Data, 8, col 14–21
  - Data de Pagamento — Data, 8, col 22–29
  - Reservado uso futuro — Numérico, 3, col 30–32 (zeros)
  - Valor da Vantagem/Desconto/Totalizador — Valor, 17, col 33–49
  - Identificação da Operação — Caractere, 1, col 50–50 (V/D/T/O)
  - Indicador de Incidência do IRRF — Caractere, 1, col 51–51 (S/N/X)
  - Banco/Agência/Conta da entidade (Febraban) — col 52–81
  - Banco/Agência/Conta do funcionário (Febraban) — col 82–111
  - Observações — Caractere, 30, col 112–141
  - (continua: Código da Vantagem/Desconto, base legal...) `[obter leiaute oficial do exercício]`
- **TCE_4820.TXT** — Cadastro de Funcionários. Campos transcritos (início):
  - Data de Atualização — Data, 8, col 1–8
  - Código de registro do Funcionário — Alfanumérico, 12, col 9–20
  - CPF — Numérico, 14, col 21–34
  - Nome do Funcionário — Caractere, 70, col 35–104
  - Data Nascimento/Admissão/Demissão — Data, 8 cada, col 105–128
  - Código/Nome do Cargo — col 129–166; Código/Nome do Setor — col 167–204
  - Sexo (1=M) col 205; Situação col 209–210; Regime Jurídico (C=Celetista) col 211
  - Regime Previdenciário (02=RGPS) col 213–214; RG col 215–228; CBO col 229–234
  - NIT (PIS/PASEP/SI/SUS) col 235–245; Categoria do Trabalhador col 246–247
  - Endereço/Cidade/UF/CEP (EBCT) col 248–317 (continua) `[obter leiaute oficial do exercício]`
- **TCE_4960.TXT** — Tabela de Vantagens/Descontos e Totalizadores (rubricas + base legal).
- **PAGTO_POS.TXT** — Pagamento após Competência.
- **DEPENDENTE.TXT** — Cadastro de Dependentes.
- **PENSIONISTA.TXT** — Cadastro de Pensionistas.

### 3.2 Sistema de Receita Pública
- **TCE_4010.TXT** — Receita Pública.
- **TCE_4011.TXT** — Conteúdo do Código de Barras.

### 3.3 Arquivos suplementares
- **CADASTRO.TXT**
- **LEIAUTE.TXT** — usado com LEIAME.TXT quando há necessidade de modificar definições de campo (ex.: número de casas decimais em campo Numérico).
- **LEIAME.TXT** — descrição/observações da remessa (ex.: especificar Tipo "Outros").

> Larguras/posições completas de cada um dos arquivos acima: `[obter leiaute oficial do exercício]` — extrair do PDF MT Vol. V do exercício-alvo (transcrição parcial feita aqui apenas para 4810/4820).

---

## 4. Periodicidade, remessas e prazos

- **Periodicidade da informação:** acumulado de 1º de janeiro até o encerramento de cada **mês** (ou data definida em solicitação formal).
- **Prazo de entrega:** **mensal** (Folha e Livro Diário Geral: até **30 dias corridos** após o encerramento do período — Res. 1099/2018, a partir de jan/2019).
- **Forma de envio:** meio eletrônico (Internet) + assinatura digital + e-protocolo.

**Tipos de remessa e assinaturas (FAQ oficial):**
- **Remessa Regular** (todo mês): RVE + Responsável + Contabilista.
- **Remessa Complementar** (todo mês): RVE (Responsável+Contabilista) + **RDI** (Responsável + Resp. Controle Interno + Resp. Folha de Pagamento).
- **RGF** (Relatório de Gestão Fiscal) — junto com a entrega de **abril/junho/agosto/dezembro**: quadrimestral para municípios > 50 mil hab.; semestral para ≤ 50 mil hab. (Maximiliano de Almeida, pequeno porte → **semestral**: 1º semestre na entrega de junho, 2º na de janeiro).
- **MCI** — mesma cadência do RGF (1º/2º/3º quadrimestre ou 1º/2º semestre).

**Reenvio do PAD:** livre dentro do prazo; fora do prazo (não-RGF) com justificativa na tela do PAD; mês de RGF fora do prazo → contatar o Serviço de Acompanhamento de Gestão (SAG).

---

## 5. e-Validador / RVE / RDI (validação)

- O ente instala o SICOE, gera os arquivos conforme leiautes, configura **certificado digital** + diretórios, **valida** (corrige erros) e visualiza o **RVE (Relatório de Validação e Envio)**.
- O PAD executa verificações de consistência lógica e contábil **mensalmente** sobre os arquivos (que devem bater com os registros contábeis); telas interativas exigem uso do **plano de contas padrão** ou ajuste com justificativa.
- Tabela do PAD: define quais contas compõem cada relatório (ex.: despesa com pessoal). Critérios de geração de RREO/RGF na Instrução Normativa aplicável (ver §6).

---

## 6. Resoluções / Instruções Normativas vigentes

| Norma | Objeto |
|---|---|
| **Resolução TCE-RS nº 1.099/2018** | Envio mensal da **Folha de Pagamento** e do **Livro Diário Geral** (a partir de jan/2019, até 30 dias corridos). |
| **Resolução TCE-RS nº 766/2007, nº 883/2010, nº 1.134/2020** | Base normativa do Manual SIAPC Vol. V (informações complementares à disposição do TCE). |
| **Instrução Normativa TCE-RS nº 25/2007 e nº 03/2011** | Leiaute/forma das informações complementares (Vol. V). |
| **Resolução TCE-RS nº 1.074/2017** | **SICOE** — entrega, envio e disponibilização de dados informatizados ao TCE-RS. |
| **Instrução Normativa TCE-RS nº 08/2017** | Leiaute, periodicidade e forma dos dados informatizados (SICOE). |
| **Instrução Normativa TCE-RS nº 6/2019 / nº 4/2021** | Critérios de elaboração dos relatórios gerados pelo PAD; publicação de **RREO** e **RGF** (LRF / LC 101/2000). IN 4/2021 substitui a 6/2019 — **confirmar IN vigente no exercício-alvo.** `[obter norma vigente]` |

---

## 7. Fontes (URLs oficiais)

- Manual SIAPC Vol. V (verificado, transcrito): https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf — local: `/tmp/mtvolv.pdf` (espelho MPC-RS; obter o do exercício no portal TCE).
- Mesmo manual no domínio TCE-RS: http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf
- Resumo do leiaute (Vol. V): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
- Perguntas Frequentes SIAPC (verificado — periodicidade/assinaturas/Res.1099): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- Manual SICOE: https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf
- Resolução 1.074/2017 (SICOE): https://tcers.tc.br/repo/cex/sicoe/resolucao-1074-2017.pdf
- IN 08/2017 (SICOE): https://tcers.tc.br/repo/cex/sicoe/in-08-2017.pdf
- Portal Sistemas de Controle Externo: https://tcers.tc.br/sistemas-de-controle-externo/
- Portal SIAPC (Manuais/Tabela do PAD): http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- IN 4/2021 (RREO/RGF/PAD): https://atosoficiais.com.br/tcers/instrucao-normativa-n-4-2021-... (texto integral)

> Nota: o domínio `tcers.tc.br` / `portal.tce.rs.gov.br` retornou **403/404 a WebFetch automatizado**; os PDFs foram obtidos via download direto (curl). URLs validadas por busca; conteúdo de manual-sicoe.pdf / resolucao-1074 / in-08 ainda **não extraído byte-a-byte** → confirmar ao baixar.

---

## 8. Pendências `[obter leiaute oficial do exercício]`

1. **Leiaute completo do exercício-alvo (2026)** — baixar MT SIAPC Vol. I–V do exercício no portal TCE-RS; a Revisão muda anualmente. Para 2026 foi anunciada a criação do **Balanço Patrimonial e Demonstração dos Fluxos de Caixa** (evento SIAPC 26ª edição, nov/2025). `[obter MT Vol. I-V exercício 2026]`
2. **Versão do PAD do exercício-alvo** (sucessora da 20.0.0.0; versão de teste 2025 já divulgada). `[obter PAD do exercício]`
3. **Larguras/posições completas** dos arquivos 4810/4820/4960/PAGTO_POS/DEPENDENTE/PENSIONISTA/4010/4011 — só os iniciais foram transcritos. `[obter leiaute oficial do exercício]`
4. **Tabela do PAD** (contas → relatórios RREO/RGF) do exercício. `[obter Tabela do PAD]`
5. **Confirmar IN vigente** para RREO/RGF (IN 4/2021 vs. posterior) e Resolução base do Vol. V no exercício (1.134/2020 vs. posterior).
6. **manual-sicoe.pdf / Res. 1074/2017 / IN 08/2017** — baixar e transcrever leiaute de transmissão do SICOE (não extraído byte-a-byte).

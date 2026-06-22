# Pesquisa — Remessa de Folha de Pagamento ao TCE-RS (Resolução 1099/2018 + SIAPC/PAD)

> Preparação do M5 (RH completo). Regra de ouro CLAUDE.md §16: toda afirmação factual tem FONTE (URL) ou está marcada `[a confirmar]` com o documento de origem.
> Data da pesquisa: 2026-06-22. Município piloto: Maximiliano de Almeida/RS (regido pela Lei Federal nº 4.320/64 — confirma uso do Volume V _4320).

---

## 1. Base normativa e periodicidade

- **Resolução TCE-RS nº 1099/2018** (publicada em 23/11/2018): dispõe sobre prazos, documentos e informações entregues ao TCE/RS em formato eletrônico para exame dos processos de contas de governo e de gestão municipais. FONTE: <https://atosoficiais.com.br/tcers/instrucao-normativa-n-13-2018> (contexto); resumo em <https://inlegis.com.br/novidades/tce-rs-prorrogado-o-prazo-de-entrega-das-remessas-do-siapc-pad/>. `[a confirmar]` — obter texto integral oficial da Resolução 1099/2018 no Diário Oficial / portal TCE-RS.
- **Periodicidade: MENSAL.** A partir de janeiro/2019 a remessa da folha é enviada mensalmente, **em até 30 (trinta) dias corridos após o encerramento do período** a que corresponder. FONTE: FAQ SIAPC, pergunta nº 9 — <http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf>.
- O envio das informações da folha está **previsto na Resolução nº 1099/2018** (texto literal do FAQ, pergunta nº 9). FONTE: idem.
- Forma de entrega: **meio eletrônico (Internet)**; periodicidade das informações = acumulado de 1º de janeiro até o encerramento de cada mês. FONTE: Manual Técnico SIAPC Volume V (MT-ASCE-0105, Rev. 06, 26/03/2024), seção 1 "Principais Características" — <https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf> (mirror MPC-RS). Original TCE: <http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf>.

## 2. Relação com a remessa SIAPC/PAD

- Os arquivos de folha **NÃO são uma remessa isolada**: "deverão ser entregues acompanhados dos arquivos do Sistema SIAPC/PAD, sendo necessária a **geração conjunta**, obedecendo ao mesmo período das informações, tendo em vista os relacionamentos e fechamentos contábeis existentes entre os mesmos." FONTE: Manual Técnico Vol. V, seção 1 (mesma URL acima).
- A folha integra os **arquivos de informações complementares "à disposição do TCE"** (Volume V do Manual Técnico SIAPC), gerados junto com o PAD (Prestação Anual de Dados) e o MCI (Módulo de Contas Individuais). FONTE: FAQ SIAPC (perguntas 1, 3, 6) e Manual Vol. V.
- **Transmissão é MANUAL via aplicativo PAD** (gera/valida os arquivos e envia pela Internet); **não há API pública** documentada para folha. O reenvio é possível dentro do prazo sem justificativa; fora do prazo exige justificativa em tela do PAD (e contato com o SAG em meses de RGF). FONTE: FAQ SIAPC, perguntas 6 e 11 — mesma URL.
- Plano de Contas da Folha: o arquivo de rubricas (TCE_4960) referencia "Código da Conta – Plano de Contas da Folha de Pagamento" com **codificação TCE** — ponto de amarração contábil com o PCASP/SIAPC. FONTE: Manual Vol. V, seção 3.1.3.

## 3. Arquivos / leiaute (Manual Técnico SIAPC Volume V — MT-ASCE-0105 Rev. 06)

Três arquivos texto posicionais (largura fixa, sem delimitador) compõem o bloco "Folha de Pagamento" (seção 3.1):

| Arquivo | Conteúdo |
|---|---|
| **TCE_4810.TXT** | Folha de Pagamento — lançamentos (vantagens/descontos), totalizadores, dados bancários, incidências |
| **TCE_4820.TXT** | Cadastro de Funcionários |
| **TCE_4960.TXT** | Tabela de Vantagens/Descontos e Totalizadores (rubricas) + base legal |

FONTE: Manual Vol. V, sumário e seções 3.1.1–3.1.3.

### 3.1 TCE_4810.TXT — Folha de Pagamento (campos do corpo, layout posicional)

| # | Campo | Tipo | Bytes | Colunas | Observação |
|---|---|---|---|---|---|
| 1 | Código do Tipo da Folha de Pagamento | Numérico | 1 | 1 | 1-Normal, 2-13º, 3-Férias, 4-Rescisão, 5-Complementar/Suplementar, 6-Afastamento, 9-Outros |
| 2 | Código do Registro do Funcionário | Alfanumérico | 12 | 2-13 | Codificação própria (FK p/ TCE_4820) |
| 3 | Data de Competência da Folha | Data | 8 | 14-21 | ddmmaaaa |
| 4 | Data de Pagamento da Folha | Data | 8 | 22-29 | ddmmaaaa |
| 5 | Reservado uso futuro | Numérico | 3 | 30-32 | zeros |
| 6 | Valor da Vantagem/Desconto/Totalizador | Valor | 17 | 33-49 | formato valor |
| 7 | Identificação da Operação | Caractere | 1 | 50 | V-Vantagem, D-Desconto, T-Totalizador, O-Outros |
| 8 | Indicador de Incidência do IRRF | Caractere | 1 | 51 | S/N/X(NSA) |
| 9 | Cód. Banco depósito da Entidade | Numérico | 5 | 52-56 | Padrão Febraban |
| 10 | Cód. Agência depósito da Entidade | Numérico | 5 | 57-61 | Padrão Febraban |
| 11 | Cód. Conta-Corrente da Entidade | Numérico | 20 | 62-81 | com dígito verificador |
| 12 | Cód. Banco do Funcionário | Numérico | 5 | 82-86 | Febraban |
| 13 | Cód. Agência do Funcionário | Numérico | 5 | 87-91 | Febraban |
| 14 | Cód. Conta-Corrente do Funcionário | Numérico | 20 | 92-111 | com dígito verificador |
| 15 | Observações | Caractere | 30 | 112-141 | texto (ex.: rubrica sub judice / ordem judicial) |
| 16 | Código da Vantagem/Desconto/Totalizador | Numérico | 5 | 142-146 | codificação própria (FK p/ TCE_4960) |
| 17 | Indicador de Incidência da Saúde | Caractere | 1 | 147 | S/N/X(NSA) |
| 18 | Indicador de Incidência do RPPS | Caractere | 1 | 148 | S/N/X(NSA) |
| 19 | Indicador de Incidência do INSS | Caractere | 1 | 149 | S/N/X(NSA) |
| 20 | Percentual de Desconto do IRPF | Numérico | 4 | 150-153 | 4 dígitos sem separador decimal; 0000=NSA |
| 21 | Percentual de Desconto da Previdência | Numérico | 4 | 154-157 | idem |
| 22 | Percentual de Desconto da Saúde | Numérico | 4 | 158-161 | idem |
| 23 | Identificador de Desconto | Numérico | 1 | 162 | 0-NSA,1-IR,2-RPPS,3-INSS,4-Saúde,5-Outros |
| 24 | Identificador de Desconto da Saúde | Numérico | 1 | 163 | 0-NSA,1-IPE,3-Regime Próprio,4-Outros |
| 25 | Identificador da Folha de Pagamento | Numérico | 12 | 164-175 | codificação própria; ID único por folha gerada (ex.: 202000000001) |
| 26 | Pagamento após Competência | Caractere | 1 | 176 | S/N |
| 27 | Matrícula | Numérico | 14 | 177-190 | codificação própria; um sufixo por vínculo (ex.: ...00, ...01) |

FONTE: Manual Vol. V seção 3.1.1 (Rev. 06, aprovação Cesar Correa Becker, 26/03/2024). Identificadores "outros" (item 9 da operação) usam códigos especiais — ex.: `[TCERS902]` para "Jornada mensal de trabalho". Atenção a totalizadores: não somar rubrica marcada como totalizador junto às vantagens (risco de dobrar o total).

### 3.2 TCE_4820.TXT — Cadastro de Funcionários (campos iniciais)

Data de Atualização (8, ddmmaaaa) · Código de registro do Funcionário (12) · CPF (14) · Nome (70) · Data Nascimento (8) · Data Admissão (8) · Data Demissão (8) · Código do Cargo (8) · Nome do Cargo (30) · Código do Setor (8) · Nome do Setor (30) · Sexo (1; 1-M/2-F) · … · Tipo de Carga Horária (col. 351; "M – Mensal"). Situação do Funcionário inclui valor "03 – Pensionista". FONTE: Manual Vol. V seção 3.1.2. `[a confirmar]` — extrair a tabela completa de campos do TCE_4820 (este doc capturou apenas o início + colunas-chave).

### 3.3 TCE_4960.TXT — Tabela de Vantagens/Descontos e Totalizadores (rubricas)

Data da Atualização (8) · Reservado (3, zeros) · Nome Vantagem/Desconto/Totalizador (45) · **Base Legal (150 — resumo da base legal)** · Código da Vantagem/Desconto/Totalizador (5) · **Código da Conta – Plano de Contas da Folha (6 — codificação TCE)** · Indicador (S/N…) … FONTE: Manual Vol. V seção 3.1.3. `[a confirmar]` — extrair tabela completa de campos do TCE_4960.

## 4. Implicações para o M5 (mapeamento com o módulo atual)

Domínio existente (`src/Modules/RecursosHumanos/.../Domain/Folha/`): `FolhaDePagamento`, `EventoFolha`, `Rubrica`, `Competencia`, `BaseCalculo`, `LiquidoAPagar`, `Enums`. Mapeamento provável:
- `Rubrica` → TCE_4960 (precisa de campos **base legal** e **conta do Plano de Contas da Folha (cód. TCE)**, hoje provavelmente ausentes). `[a confirmar]` — inspecionar `Rubrica.cs`.
- `Servidor`/`Cargo` (módulo raso) → TCE_4820 (faltam: CPF, datas admissão/demissão/nascimento, setor, carga horária, situação incl. pensionista, código de registro).
- `EventoFolha` + dados bancários + incidências (IRRF/RPPS/INSS/Saúde) → TCE_4810. Hoje os enums de incidência S/N/X e percentuais (4 díg.) provavelmente não existem.
- Necessário um **gerador de arquivo posicional** (largura fixa, padrão Febraban p/ bancos, valores 17 bytes, datas ddmmaaaa) + **ID único de folha** e **matrícula com sufixo de vínculo**.
- A folha **não é transmitida pela aplicação**: gera os 3 TXT que o operador carrega no aplicativo PAD do TCE para envio conjunto com o SIAPC. Sem assinatura A1 nesta remessa (a A1 server-side é para eSocial). `[a confirmar]` — verificar se o PAD exige assinatura de responsáveis (FAQ menciona "Resp. Folha de Pagamento" no quadro de assinaturas automáticas via SISCAD, mas é assinatura no app, não no arquivo).

## 5. Pendências `[a confirmar]` (obter docs oficiais)

1. **Texto integral da Resolução TCE-RS nº 1099/2018** (e eventuais revisões vigentes em 2026) — confirmar prazo/anexos de folha. Fonte a obter: Diário Oficial TCE-RS / portal.
2. **Versão vigente do Manual Técnico SIAPC Volume V em 2026** — usamos Rev. 06 (26/03/2024) via mirror MPC-RS; confirmar se há revisão mais nova no portal TCE (<https://portalnovo.tce.rs.gov.br/sistemas-de-controle-externo/>).
3. **Tabelas completas TCE_4820 e TCE_4960** (campos restantes além dos capturados).
4. **Tabela de "Plano de Contas da Folha de Pagamento" (codificação TCE)** referenciada no TCE_4960 — obter lista de contas.
5. **IN nº 8/2025 do TCE-RS** (novas regras de relatórios fiscais) — verificar impacto em prazos/RGF que afetam reenvio de folha. Fonte: <https://inlegis.com.br/alerta-tce-rs-publica-instrucao-normativa-no-8-2025-com-novas-regras-para-relatorios-fiscais-dos-municipios/> + texto oficial.
6. Confirmar se o aplicativo **PAD** (versão anual; ex.: "versão para envio de janeiro") tem changelog de leiaute de folha por exercício. Fonte: <https://tcers.tc.br/>.

## Fontes (URLs)

- Manual Técnico SIAPC Vol. V (MT-ASCE-0105 Rev.06): <https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf> · original TCE: <http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf>
- FAQ SIAPC (Perguntas Frequentes): <http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf>
- Portal SIAPC TCE-RS: <https://portalnovo.tce.rs.gov.br/sistemas-de-controle-externo/>
- IN 13/2018 (RREO/RGF + remessas municipais): <https://atosoficiais.com.br/tcers/instrucao-normativa-n-13-2018>
- Resumo Resolução 1099/2018 / prorrogação de prazos: <https://inlegis.com.br/novidades/tce-rs-prorrogado-o-prazo-de-entrega-das-remessas-do-siapc-pad/>

# MCASP — Mecanismo de Débitos e Créditos e Roteiros de Lançamento (Eventos Contábeis)

> Espinha Contabilidade → MSC → Prestação de Contas (TCE-RS + União). Piloto: Maximiliano de Almeida/RS.
> Edição vigente: **MCASP 11ª edição (STN)**, em vigor a partir da execução da **LOA 2025**. PCASP é a Parte IV do MCASP.
> Princípio CONVENCOES-ENGENHARIA.md §8: não inventar. Onde o código relacional exato (par D/C oficial) não foi confirmado no documento-fonte, está marcado **[obter arquivo oficial]**.

---

## 1. Estrutura / Campos

### 1.1 Classes do PCASP por natureza da informação

| Classe | Natureza | Função |
|---|---|---|
| 1 Ativo / 2 Passivo / 3 VPD / 4 VPA | **Patrimonial** | Reflete o patrimônio e suas variações (regime de competência) |
| 5 Controle da **Aprovação** do Planejamento e Orçamento | **Orçamentária** | Previsão da receita, fixação da despesa, inscrição de RP autorizada |
| 6 Controle da **Execução** do Planejamento e Orçamento | **Orçamentária** | Arrecadação da receita, execução da despesa (empenho→liquidação→pagamento), execução de RP |
| 7 Controles Devedores / 8 Controles Credores | **Controle** | Atos potenciais, limites, responsabilidades, riscos fiscais |

**Mecanismo de débitos e créditos (regra estrutural do MCASP/PCASP):** método das **partidas dobradas**. Todo lançamento **debita e credita contas de mesma natureza de informação** (não se mistura orçamentária com patrimonial num mesmo par D/C). Um fato como "pagamento" gera **lançamentos simultâneos** em mais de uma natureza (orçamentária classe 6 + patrimonial classes 1/2) — cada um com seu próprio par equilibrado.

Saldo natural por classe: **5 = devedor**, **6 = credor** (espelho do orçamento aprovado vs. executado). Classes 1, 3 → devedoras; 2, 4 → credoras.

### 1.2 Contas-chave (códigos PCASP — confirmados na fonte oficial, máscara `X.X.X.X.X.XX.XX`)

**Classe 5 — Aprovação (Previsão da Receita / Fixação da Despesa):**
- `5.2.1.1.0.00.00` PREVISÃO INICIAL DA RECEITA
  - `5.2.1.1.1.00.00` PREVISÃO INICIAL DA RECEITA BRUTA
  - `5.2.1.1.2.00.00` (-) PREVISÃO DE DEDUÇÕES DA RECEITA (FUNDEB, transf. constitucionais, renúncia)
- `5.2.1.2.0.00.00` ALTERAÇÃO DA PREVISÃO DA RECEITA (`.1` adicional / `.9` (-) anulação)
- `5.2.2.1.1.00.00` DOTAÇÃO INICIAL (`.01` Crédito Inicial; `.02` Créditos Antecipados-LDO)
- `5.2.2.1.2.00.00` DOTAÇÃO ADICIONAL POR TIPO DE CRÉDITO (`.01` Suplementar; `.02` Especial; `.03` Extraordinário)
- `5.2.2.1.3.00.00` DOTAÇÃO ADICIONAL POR FONTE (superávit financeiro, excesso de arrecadação, op. crédito, etc.)
- `5.3.x` INSCRIÇÃO DE RESTOS A PAGAR (`5.3.1` RP não processados; subdivisões `5.3.1.1`–`5.3.1.7`)

**Classe 6 — Execução (Realização da Receita / Execução da Despesa):**
- `6.2.1.1.0.00.00` RECEITA A REALIZAR
- `6.2.1.2.0.00.00` RECEITA REALIZADA
- `6.2.1.3.0.00.00` (-) DEDUÇÕES DA RECEITA ORÇAMENTÁRIA (FUNDEB, transf. constitucionais, renúncia)
- `6.2.2.1.0.00.00` DISPONIBILIDADES DE CRÉDITO
  - `6.2.2.1.1.00.00` CRÉDITO DISPONÍVEL
  - `6.2.2.1.2.00.00` CRÉDITO INDISPONÍVEL
- `6.2.2.1.3.00.00` CRÉDITO UTILIZADO / DESPESA EMPENHADA (grupo das etapas da despesa):
  - `6.2.2.1.3.01.00` CRÉDITO EMPENHADO A LIQUIDAR
  - `6.2.2.1.3.02.00` CRÉDITO EMPENHADO EM LIQUIDAÇÃO
  - `6.2.2.1.3.03.00` CRÉDITO EMPENHADO LIQUIDADO A PAGAR
  - `6.2.2.1.3.04.00` CRÉDITO EMPENHADO LIQUIDADO PAGO
  - `6.2.2.1.3.05.00` EMPENHOS A LIQUIDAR INSCRITOS EM RESTOS A PAGAR NÃO PROCESSADOS
- `6.3.x` EXECUÇÃO DE RESTOS A PAGAR (`6.3.1` RP não processados: `.1` em liquidação… `.4` pagos… )

> **Atenção (PCASP Vol. IV):** as contas `Crédito Disponível` e `Crédito Empenhado a Liquidar` compartilham aparentemente a raiz `6.2.2.1.x.xx.xx`; **são contas distintas** e o sistema deve tratá-las como tais (não confundir pela semelhança de máscara).

---

## 2. Regras — Roteiros de Lançamento (eventos contábeis)

> Pares D/C de **execução orçamentária (classes 5/6)** transcritos do **PCASP Volume IV (STN)**. Para os fatos que geram também impacto **patrimonial/financeiro (classes 1/2/3)**, o par patrimonial está descrito; os **códigos analíticos exatos** dependem do detalhamento por natureza/credor e seguem o PCASP estendido → **[obter arquivo oficial]** (tabela relacional de lançamentos padronizados / "Lançamentos Contábeis Padronizados — LCP").

### 2.1 Previsão da receita orçamentária (abertura da LOA)
| | Conta | Título |
|---|---|---|
| **D** | `5.2.1.1.x.xx.xx` | Previsão Inicial da Receita |
| **C** | `6.2.1.1.x.xx.xx` | Receita Orçamentária a Realizar |

*(natureza orçamentária; alterações de previsão → `5.2.1.2` / efeito em `6.2.1.1`.)*

### 2.2 Dotação / Fixação da despesa orçamentária (abertura da LOA)
| | Conta | Título |
|---|---|---|
| **D** | `5.2.2.1.x.xx.xx` | Dotação Orçamentária Inicial |
| **C** | `6.2.2.1.x.xx.xx` | Crédito Orçamentário Disponível |

*(natureza orçamentária; créditos adicionais → `5.2.2.1.2`/`5.2.2.1.3` contra `6.2.2.1.1` Crédito Disponível.)*

### 2.3 Empenho da despesa
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `6.2.2.1.1.00.00` | Crédito Disponível | Orçamentária |
| **C** | `6.2.2.1.3.01.00` | Crédito Empenhado a Liquidar | Orçamentária |

*Empenho **não** gera lançamento patrimonial (não altera ativo/passivo). É reserva de dotação.* — **[confirmar par D/C exato no LCP oficial]**

### 2.4 Liquidação da despesa
Ocorre em até duas etapas (mecanismo "em liquidação"):

**(a) Fato gerador antes da liquidação formal — controle "em liquidação":**
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `6.2.2.1.3.01.00` | Crédito Empenhado a Liquidar | Orçamentária |
| **C** | `6.2.2.1.3.02.00` | Crédito Empenhado em Liquidação | Orçamentária |

**(b) Liquidação (verificação do direito do credor) — orçamentário:**
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `6.2.2.1.3.02.00` (ou `.01`) | Crédito Empenhado em Liquidação / a Liquidar | Orçamentária |
| **C** | `6.2.2.1.3.03.00` | Crédito Empenhado Liquidado a Pagar | Orçamentária |

**(c) Simultaneamente — patrimonial (reconhecimento do passivo / VPD ou ativo):**
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `3.x` (VPD, ex. `3.3.x`/serviços) **ou** `1.1.5`/`1.2.3` (Estoque/Imobilizado se aquisição de bem) | Variação Patrimonial Diminutiva / Ativo | Patrimonial |
| **C** | `2.1.3.x` | Fornecedores e Contas a Pagar de Curto Prazo (ou `2.1.1` Pessoal a Pagar) | Patrimonial |

*A liquidação é o **fato gerador da VPD por competência**.* — **[obter arquivo oficial: LCP por natureza de despesa]**

### 2.5 Pagamento da despesa
**(a) Orçamentário:**
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `6.2.2.1.3.03.00` | Crédito Empenhado Liquidado a Pagar | Orçamentária |
| **C** | `6.2.2.1.3.04.00` | Crédito Empenhado Liquidado Pago | Orçamentária |

**(b) Patrimonial/financeiro (baixa do passivo e da disponibilidade):**
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `2.1.3.x` (ou `2.1.1`) | Fornecedores / Contas a Pagar (baixa do passivo) | Patrimonial |
| **C** | `1.1.1.x` | Caixa e Equivalentes de Caixa | Patrimonial |

*Há ainda controle de disponibilidade por fonte (classes 7/8 — DDR).* — **[confirmar códigos exatos no LCP oficial]**

### 2.6 Arrecadação / Realização da receita orçamentária
**(a) Orçamentário:**
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `6.2.1.1.x.xx.xx` | Receita Orçamentária a Realizar | Orçamentária |
| **C** | `6.2.1.2.x.xx.xx` | Receita Orçamentária Realizada | Orçamentária |

**(b) Patrimonial (ingresso de caixa e VPA, quando fato gerador no recebimento):**
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `1.1.1.x` | Caixa e Equivalentes de Caixa | Patrimonial |
| **C** | `4.x` (VPA, ex. `4.1.1` Impostos / transferências) **ou** baixa de `1.1.2` se crédito já reconhecido | Variação Patrimonial Aumentativa / Ativo | Patrimonial |

*Para tributos lançados (regime de competência), a VPA é reconhecida no lançamento (`D 1.1.2.2 / C 4.x`) e a arrecadação baixa o crédito (`D 1.1.1 / C 1.1.2.2`).* — **[obter arquivo oficial: LCP de receita por natureza]**

### 2.7 Inscrição em Restos a Pagar (encerramento do exercício)
**Não processados** (empenhado, não liquidado):
| | Conta | Título | Natureza |
|---|---|---|---|
| **D** | `6.2.2.1.3.01.00` | Crédito Empenhado a Liquidar | Orçamentária |
| **C** | `6.2.2.1.3.05.00` | Empenhos a Liquidar Inscritos em Restos a Pagar Não Processados | Orçamentária |

Simultaneamente nas contas de inscrição/execução de RP: grupo `5.3` (Inscrição de RP) x `6.3` (Execução de RP). **Processados** (já liquidados, não pagos) permanecem como passivo patrimonial (`2.1.3`) e são controlados em `6.3`/RP processados. Roteiro completo de encerramento (transferência de saldos, RP processados x não processados) → **IPC 03 (STN)**. — **[obter arquivo oficial: IPC 03 — tabela de lançamentos de encerramento]**

---

## 3. Periodicidade / Prazos

- **Previsão/Fixação:** lançamento de abertura no início do exercício (publicação da LOA).
- **Execução (receita/despesa):** diária/contínua, conforme ocorrência dos fatos.
- **"Em liquidação":** reconhecido quando o fato gerador da obrigação ocorre entre empenho e liquidação (competência).
- **Inscrição em RP:** no **encerramento do exercício** (31/12), conforme IPC 03.
- **Prestação de contas / remessas:**
  - **MSC (Matriz de Saldos Contábeis)** → SICONFI/STN: remessa **mensal** (até o fim do mês subsequente) e específicas (RREO bimestral, RGF quadrimestral/semestral, DCA anual).
  - **TCE-RS (SIAPC/PAD)** → remessas conforme **IN TCE-RS nº 04/2021** (PAD/MCI, RREO e RGF eletrônicos). — **[obter arquivo oficial: IN nº 04/2021 + leiautes SIAPC/PAD vigentes para 2025/2026]**

---

## 4. Fontes (URLs oficiais / institucionais)

- **MCASP 11ª edição (STN)** — Tesouro Transparente: https://www.tesourotransparente.gov.br/publicacoes/manual-de-contabilidade-aplicada-ao-setor-publico-mcasp/2025/26
- **MCASP — página STN:** https://www.gov.br/tesouronacional/pt-br/contabilidade-e-custos/manuais/manual-de-contabilidade-aplicada-ao-setor-publico-mcasp-1
- **PCASP — Volume IV (mecanismo D/C, contas classes 5/6, lançamentos típicos)** (fonte usada para os pares D/C): https://legislacaofinanceira.fazenda.sp.gov.br/Federal/Volume_IV_PCASP_republicacao.pdf  *(republicação oficial estadual do anexo STN; baixar a versão atual do anexo PCASP da 11ª ed.)*
- **PCASP — execução da despesa 6.2.2 (códigos analíticos):** https://www.cosif.com.br/publica.asp?arquivo=pcasp-622  *(referência secundária para os códigos `6.2.2.1.3.0x`)*
- **IPC 03 — Encerramento de Contas Contábeis no PCASP (STN):** https://www.tcmgo.tc.br/portalgt/wp-content/uploads/2014/12/IPC03_EncerramentoContasContabeisPCASP.pdf
- **TCE-RS — SIAPC/PAD (portal):** http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- **TCE-RS — IN nº 04/2021 (RREO/RGF eletrônicos via PAD):** https://atosoficiais.com.br/tcers/instrucao-normativa-n-4-2021
- **SICONFI / Matriz de Saldos Contábeis (MSC) — STN:** https://www.gov.br/tesouronacional/pt-br/estados-e-municipios/contabilidade-aplicada-ao-setor-publico

---

## 5. Pendências — [obter arquivo oficial]

1. **[obter arquivo oficial]** PCASP **11ª edição** completo em formato pesquisável (anexo da STN), para travar a máscara `X.X.X.X.X.XX.XX` e os títulos exatos — `https://www.gov.br/tesouronacional/.../mcasp-1` (Parte IV).
2. **[obter arquivo oficial]** **Tabela de Lançamentos Contábeis Padronizados (LCP)** por natureza de despesa/receita — confirma os pares D/C patrimoniais (classes 1–4) ainda marcados como descritivos (itens 2.4c, 2.5b, 2.6b) e os pares orçamentários de empenho/liquidação/pagamento (itens 2.3–2.5a).
3. **[obter arquivo oficial]** **IPC 03 (STN)** em versão pesquisável — roteiros completos de encerramento e inscrição em RP processados x não processados (item 2.7).
4. **[obter arquivo oficial]** **TCE-RS — IN nº 04/2021 + Manual e leiautes SIAPC/PAD vigentes (2025/2026)** e tabela de correlação PCASP → arquivos PAD (prestação de contas estadual).
5. **[obter arquivo oficial]** **SICONFI — leiaute da MSC vigente** (estrutura de arquivo, contas-correntes/atributos obrigatórios) para a remessa mensal e DCA/RREO/RGF (prestação de contas à União).

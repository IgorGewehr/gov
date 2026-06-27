# PCASP — Plano de Contas Aplicado ao Setor Público (especificação oficial)

> Espec. para o módulo **Financas** (Contabilidade) do Tensorroot.Gov.
> Fonte primária: **MCASP / STN** e **PCASP** anual. Esta espec. NÃO inventa contas — onde o código exato
> depende do arquivo anual, está marcado `[obter arquivo oficial]`.
> Piloto: Maximiliano de Almeida/RS (TCE-RS adota o padrão STN sem desvios no plano de contas).

---

## 0. Edição vigente / norma aprovadora

| Item | Vigente | Norma | Observação |
|---|---|---|---|
| **PCASP (elenco anual)** | **PCASP 2026** | **Portaria STN/MF nº 3.133, de 18/12/2025** | Atualizado anualmente, publicado só na internet, uso obrigatório no exercício seguinte. PCASP Federação e PCASP Estendido agora num único arquivo Excel (filtrar coluna "L"). |
| **MCASP (manual)** | **11ª edição** | Portaria STN (válida a partir de jan/2025) | Volume IV = PCASP; Volume V = DCASP. 12ª edição ainda não publicada na data desta espec. |
| **MSC (Matriz de Saldos Contábeis)** | Regras Gerais 2026 | **Portaria STN nº 642** (Anexo I) | Leiaute de envio dos saldos PCASP ao SICONFI. |

> O **PCASP Estendido** (Anexo III da IPC 00) é de adoção **facultativa** — referência para desenvolvimento de
> sistemas. Para o Tensorroot.Gov, adotar o **PCASP Federação** como mínimo obrigatório e o **Estendido** como
> base de detalhamento dos níveis 6/7 quando o ente não tiver elenco próprio.

---

## 1. Estrutura de codificação (níveis + máscara)

O código é um conjunto ordenado de dígitos com detalhamento hierárquico progressivo. O elenco padronizado
nacionalmente é **fixo até o nível mínimo definido pela STN**; abaixo disso o ente pode (e deve, p/ TCE-RS)
detalhar.

**Máscara padrão de aplicação (estendido): `C.G.SG.T.ST.IT.SI` — 9 dígitos / 7 níveis.**

| Nível | Nome | Dígitos (posição) | Exemplo |
|---|---|---|---|
| 1º | **Classe** | 1 dígito | `1` Ativo |
| 2º | **Grupo** | 1 dígito | `1.1` Ativo Circulante |
| 3º | **Subgrupo** | 1 dígito | `1.1.1` Caixa e Equivalentes de Caixa |
| 4º | **Título** | 1 dígito | `1.1.1.1` Caixa e Equiv. em Moeda Nacional |
| 5º | **Subtítulo** | 2 dígitos | `1.1.1.1.01` |
| 6º | **Item** | 2 dígitos | `1.1.1.1.01.01` |
| 7º | **Subitem** | 2 dígitos | `1.1.1.1.01.01.01` |

> Nota: o MCASP define a estrutura de **7 níveis**; o nº de dígitos por nível abaixo do 4º é definido no
> elenco anual. O modelo de dados deve guardar o código como `string` segmentável + `nivel` (1–7) + ponteiro
> `conta_pai`, e validar que toda movimentação ocorre em **conta analítica** (folha), nunca sintética.

**Atributos obrigatórios de cada conta (campos do cadastro):**
- `codigo`, `titulo`, `funcao` (descrição do que registra)
- `funcionamento` (quando debita / quando credita)
- `natureza_saldo` — **Devedora**, **Credora** ou **Híbrida/Mista**
- `tipo` — Sintética (não recebe lançamento) / Analítica (recebe lançamento)
- `encerramento` — permanência ou não do saldo conforme natureza
- `indicador_superavit_financeiro` — `F` (Financeiro) / `P` (Permanente), só p/ contas de Ativo e Passivo (art. 105, Lei 4.320/1964)
- `natureza_informacao` — Patrimonial / Orçamentária / Controle (ver §3)

---

## 2. As 8 classes e a natureza do saldo

| Classe | Título | Natureza da informação | Natureza do saldo |
|---|---|---|---|
| **1** | **Ativo** | Patrimonial | **Devedora** |
| **2** | **Passivo e Patrimônio Líquido** | Patrimonial | **Credora** |
| **3** | **Variação Patrimonial Diminutiva (VPD)** | Patrimonial | **Devedora** |
| **4** | **Variação Patrimonial Aumentativa (VPA)** | Patrimonial | **Credora** |
| **5** | **Controles da Aprovação do Planejamento e Orçamento** | Orçamentária | **Devedora** |
| **6** | **Controles da Execução do Planejamento e Orçamento** | Orçamentária | **Credora** |
| **7** | **Controles Devedores** | Controle | **Devedora** |
| **8** | **Controles Credores** | Controle | **Credora** |

> Resultado patrimonial do período = **VPA (4) − VPD (3)**. Resultado da execução orçamentária é apurado
> no confronto das classes 5 e 6.

---

## 3. Natureza da informação (regra de partidas dobradas)

| Natureza | Classes | Finalidade |
|---|---|---|
| **Patrimonial** | 1, 2, 3, 4 | Posição patrimonial e suas variações (efeitos econômico-financeiros sobre o PL). |
| **Orçamentária** | 5, 6 | Planejamento, autorização e execução do orçamento (previsão/fixação × execução). |
| **Controle** | 7, 8 | Atos potenciais, riscos, garantias e demais controles relevantes para gestão/fiscalização. |

**Regra inegociável (MCASP):** método das **partidas dobradas**; débito e crédito de um mesmo lançamento devem
ocorrer entre contas da **mesma natureza de informação** (patrimonial com patrimonial, orçamentária com
orçamentária, controle com controle). O motor contábil do Tensorroot.Gov DEVE rejeitar lançamento que cruze
naturezas (validação de domínio = bug crítico se ausente).

---

## 4. Contas-chave do ciclo orçamentário (classes 5 e 6)

A execução da despesa (Lei 4.320, art. 58–65) segue a cadeia de transferência de saldo dentro da classe 6:

```
Crédito Orçamentário Disponível
  → Crédito Empenhado a Liquidar            (empenho)
  → Crédito Empenhado em Liquidação         (fato gerador ocorrido, ainda não conferido — facultativo)
  → Crédito Empenhado Liquidado a Pagar     (liquidação)
  → Crédito Empenhado Pago / Liquidado Pago (pagamento)
```

- O controle **"Em Liquidação"** existe para evidenciar despesas cujo fato gerador já ocorreu (ex.: fornecimento
  realizado) mas ainda sem conferência de objeto/credor/importância — evita que o empenhado a liquidar contamine
  o resultado. A passagem por "Em Liquidação" é **facultativa** (transferências simultâneas permitidas).

**Lado da receita (classe 5/6):** `Previsão Inicial da Receita Orçamentária` (5) → `Receita a Realizar` →
`Receita Realizada` (6). Lançamento de previsão da receita tem **natureza devedora** na classe 5.

> Os **códigos completos** de 5.x e 6.x (ex.: 5.2.x Fixação da Despesa, 6.2.x Execução da Receita,
> 6.3.x Execução da Despesa / crédito disponível/empenhado/liquidado/pago) são fixados no elenco anual.
> `[obter arquivo oficial]` — extrair de **PCASP 2026 (Excel, Portaria STN/MF 3.133/2025)**, abas/colunas das
> classes 5 e 6, para popular o seed do plano de contas.

---

## 5. Disponibilidades (classe 1)

| Código | Conta | Natureza saldo | Indicador |
|---|---|---|---|
| `1.1` | Ativo Circulante | Devedora | — |
| `1.1.1` | **Caixa e Equivalentes de Caixa** (disponibilidades) | Devedora | F |
| `1.1.1.1` | Caixa e Equivalentes de Caixa em Moeda Nacional | Devedora | F |
| `1.1.1.2` | Caixa e Equivalentes de Caixa em Moeda Estrangeira | Devedora | F |

`1.1.1` compreende o somatório dos valores de disponibilidade imediata. As disponibilidades sustentam o cálculo
do **superávit financeiro** (Ativo Financeiro − Passivo Financeiro, art. 105 Lei 4.320/1964), base da apuração
de fontes para abertura de créditos adicionais e dos Restos a Pagar.

---

## 6. VPD (classe 3) e VPA (classe 4)

- **Classe 3 – VPD** (saldo devedor): reduções do PL por despesas, perdas ou consumo de ativos.
- **Classe 4 – VPA** (saldo credor): aumentos do PL por receitas/transferências reconhecidas.
- Grupos detalhados (ex.: 3.1 Pessoal e Encargos, 3.3 Uso de Bens/Serviços/Consumo de Capital Fixo incl.
  **depreciação**, 4.1 Impostos/Taxas/Contribuições, 4.5 Transferências Recebidas) → `[obter arquivo oficial]`
  PCASP 2026 Excel, classes 3 e 4.

> Importante: a **VPD/VPA são patrimoniais** (competência) e independem do empenho/realização orçamentária —
> integram demonstrações sob regime de competência; orçamento sob regime misto (Lei 4.320).

---

## 7. Periodicidade / prazos (ganchos para o ciclo de prestação de contas)

| Evento | Periodicidade | Destino |
|---|---|---|
| Atualização do elenco PCASP | Anual (até dez/ano anterior) | STN — adotar no exercício seguinte |
| Geração de saldos PCASP → **MSC** | **Mensal** | SICONFI (Portaria STN 642) |
| DCASP (Balanços Orçamentário, Financeiro, Patrimonial, DVP) | Anual + bimestral/quadrimestral conforme RREO/RGF | SICONFI + TCE-RS |

> Prazos específicos do TCE-RS (remessas SIAPC/PAD, periodicidade mensal/anual) estão detalhados na spec
> [`tce-rs-siapc-pad.md`](./tce-rs-siapc-pad.md). Aqui registra-se apenas o vínculo PCASP → MSC → SICONFI/TCE-RS.

---

## 8. Decisões de implementação (Tensorroot.Gov)

1. **Seed do plano de contas** = importar PCASP Federação 2026 (mínimo obrigatório) + Estendido (níveis 6/7).
2. Conta modelada com todos os atributos do §1; movimentação só em conta **analítica**.
3. Validação de domínio: partida dobrada + **mesma natureza de informação** (§3) — invariante de agregado.
4. Cadeia de execução da despesa (§4) modelada como transição de estado com saldos espelhados na classe 6.
5. Indicador F/P (§1) obrigatório em Ativo/Passivo para apuração de superávit financeiro e Restos a Pagar.

---

## 9. Fontes (URLs oficiais)

- PCASP (STN): https://www.gov.br/tesouronacional/pt-br/contabilidade-e-custos/federacao/plano-de-contas-aplicado-ao-setor-publico-pcasp-1
- PCASP 2026 (sistema interativo / Excel): https://sisweb.tesouro.gov.br/apex/f?p=60021:1:::NO:::
- Síntese de Alterações do PCASP 2026: https://thot-arquivos.tesouro.gov.br/publicacao-anexo/27248
- MCASP (STN — Volume IV PCASP, Volume V DCASP): https://www.gov.br/tesouronacional/pt-br/contabilidade-e-custos/manuais/manual-de-contabilidade-aplicada-ao-setor-publico-mcasp-1
- MSC Regras Gerais 2026 (Portaria STN 642, Anexo I): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- MCASP Volume IV (texto base p/ estrutura/níveis): https://legislacaofinanceira.fazenda.sp.gov.br/Federal/Volume_IV_PCASP_republicacao.pdf

## 10. Pendências `[obter arquivo oficial]`

- [ ] **PCASP 2026 (Excel oficial)** — Portaria STN/MF 3.133/2025, via https://sisweb.tesouro.gov.br/apex/f?p=60021:1:::NO::: — extrair elenco completo classes 1–8 (códigos, natureza saldo, F/P, função) para o seed.
- [ ] **Síntese de Alterações PCASP 2026** — https://thot-arquivos.tesouro.gov.br/publicacao-anexo/27248 — confirmar contas novas/extintas vs. 2025.
- [ ] **MCASP 11ª ed., Volume IV (vigente)** — baixar do portal STN (link §9); a base textual usada nesta espec. é a 2ª edição (mirror SP) — validar máscara/dígitos exatos do nível 5–7 contra a 11ª ed.
- [ ] **Portaria STN 642 / Anexo I 2026 (MSC)** — confirmar de-para PCASP → contas-correntes da MSC p/ envio SICONFI.
- [ ] **TCE-RS** — leiaute SIAPC/PAD e periodicidade de remessa (ver `docs/architecture/specs-oficiais/tce-rs-siapc-pad.md`).

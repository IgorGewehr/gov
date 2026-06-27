# Revisão Tributária — para o Tributarista

> **O que esta página é.** Tudo que o tributarista precisa conferir nos **tributos municipais** e na
> **dívida ativa**: ITBI (Tema 1.113/STJ), ISS/DES-IF, IPTU/PGV, taxas/COSIP/contribuição de melhoria,
> CND/CPEN, decadência/prescrição, inscrição em dívida ativa/CDA, protesto e domicílio eletrônico.
>
> 🟢 = parâmetro/valor ajustável (alíquotas, prazos, faixas — quase tudo aqui é **lei municipal/CTM**,
> versionado por exercício) · 🟡 = lógica descrita no `.rules.md` (sinalize se divergir da norma).
>
> Base normativa: [`docs/normas/FONTES-NORMATIVAS.md`](../normas/FONTES-NORMATIVAS.md) (CTN Lei 5.172/66;
> LC 116/2003; Lei 6.830/80; Lei 9.492/97; Tema 1.113/STJ; Tema 1084/STF; Súmula 160/STJ).
>
> **Princípio do sistema:** nenhuma alíquota fica embutida no código. Alíquotas, faixas da PGV, prazos
> e isenções são **parâmetros versionados por exercício** (lei municipal/Código Tributário Municipal — CTM).

---

## A. ITBI — base de cálculo (o item mais sensível)

Arquivo de regra: [`src/Modules/Tributos/rules/Itbi.rules.md`](../../src/Modules/Tributos/rules/Itbi.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| **Base = VALOR DECLARADO** da transação (presunção de veracidade) | **Tema 1.113/STJ, REsp 1.937.821** (repetitivo) | 🟡 lógica (invariante central) | **Conferir que o sistema NÃO usa "valor de referência"/PGV como piso automático.** A base nasce com origem `Declarada`. |
| Elevação da base só por **arbitramento com contraditório** | **CTN art. 148** | 🟡 lógica (máquina de estados do arbitramento) | Confira o fluxo: `Instaurado → AguardandoContraditorio → EmAnalise → Concluido/Cancelado`. O valor só sobe ao fim desse processo. |
| Triagem por margem (declarado × referência) **só dispara revisão**, não altera o tributo | Tema 1.113/STJ | 🟢 parâmetro (margem de divergência %, por exercício) + 🟡 lógica | Confira que a margem apenas sinaliza para o fiscal — não eleva a base sozinha. |
| Alíquota geral e SFH; imunidade da 1ª aquisição SFH | CTN art. 35; **CF art. 156 §2º I** | 🟢 parâmetro (alíquota geral/SFH por exercício; isenções — CTM) | Confira alíquotas e a imunidade. |

> **Risco clássico a checar:** muitos sistemas "arbitram valor de referência prévio unilateral" — isso
> é vedado pelo Tema 1.113. Aqui o desenho separa valor declarado do arbitramento. Confira na prática.

---

## B. IPTU e Planta Genérica de Valores (PGV)

| Item | Arquivo / Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Motor de IPTU + guia (DAM), lançamento anual de ofício | [`DamIptu.rules.md`](../../src/Modules/Tributos/rules/DamIptu.rules.md) — **CTN art. 142/149, art. 33**; EC 29/2000; CF art. 156 §1 | 🟢 parâmetro (alíquota, nº de parcelas, prazos, isenção/desconto — CTM) + 🟡 lógica | Confira a fórmula: (terreno×VUT + construído×VUC×fatores) × fração ideal × alíquota − isenção − desconto; e que só calcula com PGV vigente cobrindo a zona fiscal. |
| **PGV: atualização de valor venal só por LEI** (decreto só corrige por índice) | [`Pgv.rules.md`](../../src/Modules/Tributos/rules/Pgv.rules.md) — **CTN art. 33; Súmula 160/STJ**; EC 29/2000; CF art. 182 §4 + Lei 10.257/2001 | 🟡 lógica (invariante) + 🟢 parâmetro (VUT/VUC por zona, fatores, alíquotas/faixas) | **Conferir:** a PGV/tabela só é editável enquanto **não vigente** (nova versão por exercício); majoração real exige lei, não decreto. Faixas de alíquota não se sobrepõem e cobrem de zero. |
| Cadastro imobiliário (BCI), origem do lançamento | [`Imovel.rules.md`](../../src/Modules/Tributos/rules/Imovel.rules.md) — **CTN art. 32-34**; Decreto 11.208/2022 (SINTER/CIB); IN RFB 2.275/2025 | 🟢 parâmetro (zonas fiscais, campos BCI) + 🟡 lógica | Confira inscrição municipal obrigatória; CIB/matrícula opcionais (CIB piloto jan/2027); zona fiscal é a chave da PGV. |

---

## C. ISS / NFS-e / DES-IF

| Item | Arquivo / Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Apuração do ISS a partir de NFS-e já ingeridas do ADN (próprio/retido/substituição) | [`Iss.rules.md`](../../src/Modules/Tributos/rules/Iss.rules.md) — **LC 116/2003**; CTM | 🟢 parâmetro (alíquota por item LC 116, hipóteses de retenção, substitutos — CTM) + 🟡 lógica | Confira que **nenhuma alíquota é hardcoded**; usa a tabela municipal vigente na competência. Confira a modalidade (próprio/retido/substituição). |
| NFS-e é **read model imutável** sincronizado do ADN (não emitimos nem assinamos) | [`NotaFiscalServico.rules.md`](../../src/Modules/Tributos/rules/NotaFiscalServico.rules.md) — NFS-e Nacional/ADN (ADR-0003); LC 116/2003 | 🟡 lógica | Confira a unicidade por chave de acesso e a dedup; o sistema é **passivo** (integração só de leitura). |
| **DES-IF** — ISSQN de instituições financeiras (modelo ABRASF) | [`Desif.rules.md`](../../src/Modules/Tributos/rules/Desif.rules.md) — **LC 116/2003**; Plano Contábil COSIF (BACEN); CTM | 🟢 parâmetro (alíquota por item, % de dedução, código de tributação Anexo 6 — CTM) + 🟡 lógica | Confira: deduções de receita reduzem a **base**; incentivos legais e depósitos judiciais (**CTN art. 151 II**) abatem o **imposto** (não a base). |

> **Atenção:** os leiautes de NFS-e (ADN) e DES-IF (ABRASF) estão **em transição** pela Reforma
> Tributária (CBS/IBS, NT 004 jun/2025). Confirme a versão vigente antes de produção. (FONTES E3/E7.)

---

## D. Lançamento, Decadência e Prescrição

| Item | Arquivo / Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Constituição do crédito (lançamento torna a obrigação líquida/certa/exigível) | [`Lancamento.rules.md`](../../src/Modules/Tributos/rules/Lancamento.rules.md) — **CTN art. 142, 139-141, 150, 201**; Lei 4.320/64 | 🟡 lógica (máquina de estados) | Confira: nasce `Aberto`; inscrição em dívida ativa exige `hoje > Vencimento` (**CTN art. 201**); vedado cancelar após pago. |
| **Decadência** (constituição) e **prescrição** (cobrança) | DividaAtiva.rules.md / Lancamento.rules.md — **CTN art. 173** (decadência) e **art. 174** (prescrição, 5 anos) | 🟡 lógica + 🟢 (multa/juros/correção — CTM) | **Conferir os prazos:** prescrição = data de inscrição + 5 anos; e que suspensão/parcelamento interrompem corretamente. |

---

## E. Dívida Ativa, CDA, Protesto e Execução Fiscal

Arquivo de regra: [`src/Modules/Tributos/rules/DividaAtiva.rules.md`](../../src/Modules/Tributos/rules/DividaAtiva.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Prescrição quinquenal; não corre se Quitada/Parcelada/Cancelada | **CTN art. 174** | 🟢 parâmetro (`AnosPrescricao = 5`) + 🟡 lógica | Confira o cálculo (inscrição + 5 anos) e os estados que suspendem. |
| CDA como título executivo (requisitos) | **Lei 6.830/80 (LEF), art. 2º §5º** | 🟡 lógica | Confira que a CDA só é emitida a partir de situação `Inscrita` e contém os requisitos da LEF. |
| **Protesto extrajudicial de CDA** (facultativo) | **Lei 9.492/97** (com Lei 12.767/2012) | 🟡 lógica | Confira que o protesto exige `CdaEmitida` e crédito exigível. |
| Parcelamento suspende exigibilidade e interrompe prescrição | CTN art. 151/174 | 🟡 lógica | Confira o efeito do parcelamento sobre a cobrança. |

---

## F. Certidões (CND / CPEN)

Arquivo de regra: [`src/Modules/Tributos/rules/Certidao.rules.md`](../../src/Modules/Tributos/rules/Certidao.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Tipo decidido pela situação fiscal (sem débito→CND; só suspenso→CPEN; exigível→Positiva) | **CTN art. 205/206** | 🟡 lógica | Confira a regra de decisão do tipo de certidão. |
| Validade, código de autenticação, sequencial por exercício | CTN; CTM | 🟢 parâmetro (validade — CTM) | Confira o prazo de validade parametrizado. |

---

## G. Taxas, COSIP, Alvarás e Contribuição de Melhoria

Arquivo de regra: [`src/Modules/Tributos/rules/TaxasCosipAlvaraMelhoria.rules.md`](../../src/Modules/Tributos/rules/TaxasCosipAlvaraMelhoria.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| **Taxa:** base é elemento físico (metragem/unidade), **nunca** capital/faturamento | **CTN art. 77-80; Súmula Vinculante 29/STF** (e SV 19 — taxa de lixo) | 🟡 lógica | Confira que a quantidade-base não usa capital/faturamento como base de imposto. |
| **COSIP** progressiva por faixa de consumo (kWh) por classe | **CF art. 149-A (EC 39/2002); STF RE 573.675/Tema 44** | 🟢 parâmetro (faixas de consumo, classes — CTM) + 🟡 lógica | Confira as faixas e a progressividade (válida pelo Tema 44). |
| **Contribuição de Melhoria:** edital prévio + impugnação ≥ 30 dias; **dois limites** (total ≤ custo; individual ≤ valorização) | **CTN arts. 81-82 + DL 195/1967** | 🟡 lógica (máquina de estados) | **Conferir os dois limites de rateio** e o prazo mínimo de impugnação (CTN art. 82). |
| **Alvará/TLL:** alvará é o ato de polícia; a TLL é o tributo, lançada à parte | **CTN art. 78** | 🟡 lógica | Confira a separação ato (alvará) × tributo (TLL). |

---

## H. Cadastro, Domicílio Eletrônico e S.I.M.

| Item | Arquivo / Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Cadastro de contribuinte PF/PJ (sujeito passivo) | [`Contribuinte.rules.md`](../../src/Modules/Tributos/rules/Contribuinte.rules.md) — **CTN art. 121-122**; LC 116/2003 | 🟡 lógica | Confira validação de CPF/CNPJ. |
| **Domicílio Eletrônico (DEC)** — ciência tácita por prazo | [`DomicilioEletronico.rules.md`](../../src/Modules/Tributos/rules/DomicilioEletronico.rules.md) — modelo e-CAC/DTE; CTM | 🟢 parâmetro (dias de ciência tácita, padrão **15**) | Confira o prazo de ciência tácita e o efeito de intimação pessoal. |
| **S.I.M.** — registro de inspeção municipal (origem animal/vegetal) | [`Sim.rules.md`](../../src/Modules/Tributos/rules/Sim.rules.md) — Lei 7.889/1989; Dec. 9.013/2017 (RIISPOA); SISBI/SUASA; CTN art. 77 | 🟢 parâmetro (máscara nº S.I.M., classificação — lei/decreto) + 🟡 lógica | Confira o ciclo de registro/suspensão/cassação e o lançamento da taxa correlata. |

---

## Pontos de atenção a SINALIZAR

- **ITBI:** não usar valor de referência/PGV como piso automático (Tema 1.113/STJ) — confira o desenho.
- **IPTU/PGV:** valor venal só por lei, salvo correção monetária (Súmula 160/STJ) — confira a trava de
  edição "só enquanto não vigente".
- **NFS-e/DES-IF:** leiautes em transição (Reforma Tributária) — confirmar versão vigente.
- **Código Tributário Municipal de Maximiliano de Almeida:** ainda não localizado (FONTES-NORMATIVAS,
  lacuna 5). As alíquotas/prazos/faixas devem espelhar o CTM local — peça o CTM para conferir os
  parâmetros 🟢 acima.

---

## Resumo para o tributarista

- **14 regras de negócio** (`.rules.md`) para revisar no módulo Tributos.
- **Quase tudo "ajustável" aqui é lei municipal/CTM** (alíquotas de IPTU/ISS/ITBI, faixas da PGV,
  faixas de COSIP, prazos, isenções) — versionado por exercício. Os únicos valores fixos no código são
  prazos legais federais (prescrição 5 anos, ciência DEC 15 dias) e os requisitos de CDA.
- **Prioridade de conferência (lógica 🟡):** ITBI (base = valor declarado, Tema 1.113), PGV
  (atualização só por lei, Súmula 160), DES-IF (dedução na base × abatimento no imposto), e os
  **dois limites** da contribuição de melhoria.
- **A remessa LicitaCon não é tributária**; a ingestão de NFS-e (ADN) é passiva e está em
  [`tce-rs-integracao.md`](tce-rs-integracao.md) / arquitetura.

# AUDITORIA ADVERSARIAL FINAL — W10.6

> Consolidação das três frentes de auditoria adversarial (FISCAL/CONTÁBIL, RH, TRIBUTOS/COMPRAS) sobre o código novo da Onda final. Cada regra fina foi conferida contra a fonte oficial ao vivo (IN RFB 1234/2012, FEBRABAN CNAB240 v10.x, Regras Gerais MSC 2026, Lei 15.270/2025, eSocial S-1.3, Lei 8.213/91, Lei 14.133/2021, CTN, Tema 1.113/STJ, ABRASF DES-IF).
>
> **⚙️ STATUS DE REMEDIAÇÃO (atualizado).** Os 4 P0 e os 7 P1 foram **RESOLVIDOS** em código (ver coluna *Status*). Dos P2: P2-1, P2-6, P2-7, P2-9 e P2-10 resolvidos; P2-3 (Transparência) e os demais permanecem como polimento aberto. O leiaute posicional do SIAPES (P1-7) foi reancorado na FONTE OFICIAL do TCE-RS — [`ImportDadosSIAPESConsist57.pdf`](https://www.tce.rs.gov.br/sistemas_controle/SIAPES/arquivos/pdf/ImportDadosSIAPESConsist57.pdf), Tabela 14 ("Formato Básico"), record de 1179 bytes; o `itensRemun` (P1-4) e o `indApurIR` foram reancorados no Manual eSocial S-1.3 (itensRemun NÃO tem `tpRubr`; `indApurIR=0` é a REGRA). Este documento deixou de gerar falso-alarme.

---

## 1. VEREDITO

O **miolo de cálculo está pronto para produção e para os validadores oficiais** — alíquotas IRRF/PJ (IN 1234) e IRRF 2026 com redutor (Lei 15.270, zera no R$5.000) corretas, ciclo contábil de retenção/consignação balanceado e idempotente, ITBI honrando o Tema 1.113, CND/CPEN fail-closed, Registro de Preços com os dois tetos certos, prazo PNCP art. 94 correto, certidão de tempo de serviço sem furos. **O risco de produção está na casca de transmissão, não no cálculo**: o gerador de CSV da MSC (`GeradorMscCsv`) não herda as validações duras do agregado de Finanças (fail-open → e-Validador SICONFI rejeita), o CNAB240 tem um off-by-one na contagem do Trailer de Lote (banco rejeita), e dois eventos eSocial (S-1202/RPPS e S-2200/admissão) reusam a estrutura errada do S-1200 (XSD rejeita toda folha de RPPS e toda admissão em municípios afetados). Há ainda um Integration Event de Dispensa publicado fora do Outbox (evento perdível). **Conclusão honesta (diagnóstico original):** o cálculo aguentava; a casca de saída NÃO aguentava sem os 4 P0. **Estado atual:** os 4 P0 e os 7 P1 estão **RESOLVIDOS** — a casca de saída (CSV MSC, XML eSocial S-1200/S-1202/S-2200, CNAB240, arquivo SIAPES) agora herda as validações duras e a estrutura/posições oficiais. Resta o habilitador transversal P2-5 (validação XSD no pipeline eSocial) como reforço anti-regressão.

**Contagem (original):** 4 P0 · 7 P1 · 11 P2 · ⇒ **Resolvidos: 4 P0 + 7 P1 + 5 P2 (P2-1/-6/-7/-9/-10).**

---

## 2. TABELA DE ACHADOS CONSOLIDADA

### P0 — Bloqueantes (rejeição garantida pelo validador/banco/órgão) — **TODOS RESOLVIDOS**

| # | Frente | Achado | Arquivo:linha | Norma | Status |
|---|--------|--------|---------------|-------|--------|
| P0-1 | FISCAL | **MSC fail-open**: o caminho que gera o artefato SICONFI (`GeradorMscCsv`) NÃO aplicava as 3 validações duras (PO obrigatório, balanço por classe, SI+mov=SF). | `GeradorMscCsv.cs`; `ValidadorMscCsv.cs` | ✅ **RESOLVIDO** — `GeradorMscCsv.Gerar` chama `ValidadorMscCsv.GarantirValida(declaracao.Matriz, ExtrairPoderOrgao)` antes de emitir. |
| P0-2 | FISCAL | **`GeradorMscCsv` não garantia PO como TIPO1/IC1**: emitia os pares na ordem do texto livre, sem exigir PO primeiro. | `GeradorMscCsv.cs:171-184` | ✅ **RESOLVIDO** — `OrdemSlotsIc = ["PO","FP","DC","FR","CO","NR","ND","FS","AI"]`; slot por CÓDIGO, PO garantido em IC1. |
| P0-3 | RH | **S-1202 (evtRmnRPPS) reusava estrutura do S-1200** (`ideEstabLot`/`codLotacao` em vez de `ideEstab`). | `GeradorEventosESocial.cs:205-280` | ✅ **RESOLVIDO** — `GerarS1200` roteia `rpps ? GerarS1202Rpps : GerarS1200Rgps`; o S-1202 usa `ideEstab` próprio. |
| P0-4 | RH | **S-2200: `infoRegimeTrab` aninhado DENTRO de `infoContrato`; faltava `tpRegTrab`**. | `GeradorEventosESocial.cs:143-174` | ✅ **RESOLVIDO** — `infoRegimeTrab` é IRMÃO de `infoContrato` (antes dele); `tpRegTrab` emitido (1=CLT/2=Estatutário). |

### P1 — Graves (cálculo/integridade errados; rejeição provável ou perda fiscal) — **TODOS RESOLVIDOS**

| # | Frente | Achado | Arquivo | Norma | Status |
|---|--------|--------|---------|-------|--------|
| P1-1 | FISCAL | **CNAB240 off-by-one no Trailer de Lote**: calculava `2N+3`; o correto é `1+2N+1`. | `Cnab240Writer.cs:53` | FEBRABAN CNAB240 v10.x, Trailer de Lote (reg.5), tipos 1+3+5 | ✅ **RESOLVIDO** — `quantidadeRegistrosLote = 1 + (Favorecidos.Count * 2) + 1`. |
| P1-2 | TRIBUTOS | **DES-IF: dedução de RECEITA abatida do IMPOSTO** (sub-arrecada ISS). `deducoesReceita` é dedução da BASE (Reg. 0440), não do imposto; município perdia `deducoesReceita×(1−alíquota)`. | `Domain/Desif/DeclaracaoDesif.cs` (`Entregar`) | ABRASF DES-IF Mód.2 (Reg.0430/0440); LC 116/2003 | ✅ **RESOLVIDO** — a dedução de receita reduz a BASE (ISS = `dedução × alíquota efetiva = devidoBruto÷receitaTributável`); só incentivos/depósitos abatem o imposto. Fail-closed contra imposto negativo. |
| P1-3 | TRIBUTOS | **Integration Event da Dispensa publicado FORA do Outbox** (evento perdível) via `IPublisher.Publish` pós-commit. | `Application/Dispensas/HomologarDispensa.cs` | CLAUDE.md §8/§10 (Integration Events via Outbox) | ✅ **RESOLVIDO** — `DispensaHomologadaIntegrationEvent` enfileirado via `IIntegrationEventWriter.Enfileirar` na mesma transação (Outbox). |
| P1-4 | RH | **S-1200 `itensRemun` somava proventos E descontos como `vrRubr` positivo**, sem distinguir natureza; `indApurIR` fixo `0`. | `GerarEventosPeriodicos.cs:51-66` | Manual eSocial S-1.3 (itensRemun; indApurIR) | ✅ **RESOLVIDO** — agrupa por `(Rubrica.Codigo, Tipo)`: provento e desconto JAMAIS colapsam num mesmo `vrRubr`. *(Anc. oficial: `itensRemun` NÃO tem campo `tpRubr` — ele vive no S-1010; o sinal vem do tpRubr da rubrica. `indApurIR=0` é a REGRA do MOS — IR apurado no eSocial; `1` é a exceção via EFD-Reinf, inaplicável aqui.)* |
| P1-5 | RH | **Banco de Horas: prescrição re-contava créditos já prescritos** (o job avança o limite mês a mês; originais nunca removidos → prescreve a mais). | `BancoDeHoras.cs` (`PrescreverCreditosAnterioresA`) | Portaria MTP 671/2021 (prescrição 6/12m) | ✅ **RESOLVIDO** — desconta o já-prescrito (`Σ lançamentos de Prescrição`) dos vencidos antes de calcular `aPrescrever`; prescreve só o vencido AINDA NÃO prescrito, limitado ao saldo. |
| P1-6 | RH | **SIAPES: regime jurídico derivado do regime PREVIDENCIÁRIO** (`RGPS→Celetista`). | `MapeamentoSiapes.cs:35-41`; `AdicionarAtoRemessa.cs:67` | SIAPES Tabela 5 (CD_REGIME_JURIDICO) | ✅ **RESOLVIDO** — `RegimePadraoDe(TipoCargo)`: Efetivo→Estatutário, Comissionado/Temporário→Administrativo; deriva do TIPO DO CARGO (atributo do vínculo). O caller passa `cargo.Tipo`, não `servidor.Regime`. Celetista só por override. |
| P1-7 | RH | **SIAPES linha de ato: campos posicionais intermediários omitidos** (05→07, 14→16, 16→21, 21→23, 23→25) — largura fixa, offsets deslocados → rejeição. | `LeiauteSiapes.cs` (`MontarLinhaAto`) | **SIAPES "Formato Básico", Tabela 14 ([`Consist57`](https://www.tce.rs.gov.br/sistemas_controle/SIAPES/arquivos/pdf/ImportDadosSIAPESConsist57.pdf))** | ✅ **RESOLVIDO** — emite TODOS os campos 01..25 nas posições EXATAS da Tabela 14 oficial (1-490): inclui 06 `CD_REGIME_JURIDICO_ANT`, 08 `CD_MUNICIPIO_MAE`, 09 `CD_ORGAO_ANT`, 15 `CONCURSADO`, 17 `ESPECIALIZACAO_PROF`, 18 `DS_CARGO_ANTERIOR`, 19 `ESPECIALIZACAO_ANT`, 20 `IDENTIFICADOR_PESSOA`, 22 `RG`, 24 `TITULO_ELEITOR` (padded). CD_REGIME_JURIDICO corrigido para N(2) por Tabela 14. |

### P2 — Médios (defensivo, coerência, semântica, escopo)

| # | Frente | Achado | Arquivo:linha | Norma | Fix |
|---|--------|--------|---------------|-------|-----|
| P2-1 | FISCAL | `MatrizSaldos.EstaBalanceada` (Transparência) usava `Equals` exato sem tolerância de centavos. | `MatrizSaldos.cs:58-59` | MCASP/partida dobrada | ✅ **RESOLVIDO** — `Math.Abs(TotalDebitos − TotalCreditos) <= ToleranciaFechamento` (`0.01m`). |
| P2-2 | FISCAL | Retenção IRRF/PJ modela só a parcela de IR (1,2%/2,4%/4,8%), não o agregado do DARF (IR+CSLL+COFINS+PIS, ex. 4,65% no cód. 6147). Líquido ao fornecedor sai maior que o devido se esperado o "cheio". Alíquotas de IR corretas — lacuna é de escopo. | `TabelaIrrfServicosCatalogo.cs:22-47`; `NaturezaRetencao.cs` | IN RFB 1234/2012 Anexo I | Modelar CSLL/COFINS/PIS como naturezas do mesmo DARF, ou documentar IR-only. |
| P2-3 | FISCAL | CSV injection: coluna `Valor` formatada `0.00` não passa por `Escapar`. Não é exploit hoje (decimal não-negativo); defensivo. | `GeradorMscCsv.cs:100,189` | OWASP CSV Injection (defensivo) | Sem ação obrigatória; padronizar formatação invariante. |
| P2-4 | RH | S-2210 (CAT): mapeamento aproximado (`dscLesao`/`lateralidade`/`agenteCausador`/`codCID` como leaf onde o leiaute espera grupos `parteAtingida`/`agenteCausador`); sem enforcement de prazo. | `GeradorEventosSst.cs:42-48`; `ComunicarAcidente.cs` | eSocial S-2210 | Modelar grupos corretos; alertar prazo de CAT. |
| P2-5 | RH | **Validação XSD ausente em todo o pipeline eSocial** — nenhum `XmlSchema`/`XmlReaderSettings` valida antes de assinar/transmitir. Mascara P0-3, P0-4, P1-4, P2-4. | módulo eSocial (sem validador) | ESOCIAL-SPEC §2.6 / MOS | Pipeline `XmlReaderSettings` com XSD S-1.3 pré-assinatura (fail-closed). |
| P2-6 | RH | `NumericoEsquerda` truncava via `[^tamanho..]` — overflow pegava os dígitos INFERIORES (órgão errado); negativos corrompiam. | `LeiauteSiapes.cs` (`NumericoEsquerda`) | coerência/leiaute | ✅ **RESOLVIDO** — fail-closed: `ThrowIfNegative` + `InvalidOperationException` no overflow (não trunca silenciosamente). |
| P2-7 | TRIBUTOS | CPEN não emitida para execução fiscal garantida por penhora — `EmExecucaoFiscal` caía no `default` → exigível → Positiva. | `CertidaoGiaRepositories.cs`; `DividaAtiva.cs` | CTN art. 206; Súmula 451-STJ | ✅ **RESOLVIDO** — `DividaAtiva.RegistrarGarantiaPenhora(data)` + flag `Garantida`; o cálculo da situação fiscal trata execução garantida não-prescrita como SUSPENSA → habilita CPEN. *(Requer migração EF: colunas `Garantida`/`DataGarantiaPenhora`.)* |
| P2-8 | TRIBUTOS | Habilitação da Dispensa é flag confiada do chamador (fail-open): `VencedorHabilitado` repassado direto ao domínio; handler não rechecagem regularidade. (Sanção impeditiva, essa sim, é rechecada fail-closed.) | `HomologarDispensa.cs:20,57` | Lei 14.133/2021 art. 63; IN SEGES/ME 67/2021 | Cross-check de regularidade no servidor ou exigir evidência registrada. |
| P2-9 | TRIBUTOS | Off-by-one de fuso na aferição de sanção: usava data UTC, não o fuso do tenant (UTC-3). | `RegistrarLanceDispensa.cs`; `HomologarDispensa.cs` | CLAUDE.md §16 (prazo por dia civil, fuso do tenant) | ✅ **RESOLVIDO** — usa `IDataHojeTenant.Hoje()` (os handlers de Dispensa recebem `IDataHojeTenant`, como `VarrerPrazosPncp`). |
| P2-10 | TRIBUTOS | ITBI complementar: fato gerador ancorado no mês do vencimento da guia (`new DateOnly(Exercicio, VencimentoComplementar.Month, 1)`) — quebra entre anos. | `ArbitramentoItbi.cs` | CTN art. 173, I; art. 116 | ✅ **RESOLVIDO** — fato gerador ancorado no EXERCÍCIO da transmissão (`new DateOnly(transmissao.Exercicio, 1, 1)`), não no vencimento da guia complementar. *(TODO/M10: persistir a data-dia real da transmissão na `TransmissaoImobiliaria` — hoje guarda só o exercício; decadência é year-anchored.)* |
| P2-11 | RH | Retenção IRRF/PJ — ver P2-2 (escopo CSLL/COFINS/PIS) [referência cruzada, contado em P2-2]. | — | — | — |

> Nota: P2-11 é a mesma lacuna de escopo de P2-2 sob a frente RH; não soma à contagem (a contagem real de P2 é 11, excluindo esta linha de referência cruzada, que renumera P2-2..P2-10 = 10 mais P2-1 = 11).

---

## 3. FILA DE FIX — ESTADO

### Bloco P0 — ✅ TODOS RESOLVIDOS

1. ✅ **P0-3 (S-1202/RPPS)** — `GerarS1202Rpps` com `ideEstab` próprio. `GeradorEventosESocial.cs`.
2. ✅ **P0-4 (S-2200/admissão)** — `infoRegimeTrab` irmão de `infoContrato` + `tpRegTrab`. `GeradorEventosESocial.cs`.
3. ✅ **P0-1 (MSC fail-open)** — `ValidadorMscCsv.GarantirValida` no `GeradorMscCsv`. `GeradorMscCsv.cs`.
4. ✅ **P0-2 (MSC PO em slot fixo)** — `OrdemSlotsIc`, PO em IC1. `GeradorMscCsv.cs`.

> Habilitador transversal ainda recomendado: **P2-5 (validação XSD no pipeline eSocial)** — reforço anti-regressão dos P0/P1 de RH (ABERTO).

### Bloco P1 — ✅ TODOS RESOLVIDOS

5. ✅ **P1-1 (CNAB240)** — `1+2N+1`. `Cnab240Writer.cs:53`.
6. ✅ **P1-2 (DES-IF dedução de receita)** — dedução reduz a BASE (alíquota efetiva). `DeclaracaoDesif.cs`.
7. ✅ **P1-3 (Dispensa fora do Outbox)** — `IIntegrationEventWriter.Enfileirar`. `HomologarDispensa.cs`.
8. ✅ **P1-4 (S-1200 itensRemun)** — agrupa por `(Codigo, Tipo)`; indApurIR=0 (regra MOS). `GerarEventosPeriodicos.cs`.
9. ✅ **P1-5 (Banco de Horas prescrição dupla)** — desconta o já-prescrito. `BancoDeHoras.cs`.
10. ✅ **P1-6 (SIAPES regime jurídico)** — deriva do `TipoCargo`. `MapeamentoSiapes.cs` + `AdicionarAtoRemessa.cs`.
11. ✅ **P1-7 (SIAPES posições)** — campos 01..25 nas posições da Tabela 14 oficial. `LeiauteSiapes.cs`.

### Bloco P2 (polimento)

- ✅ **P2-1** tolerância de centavos em `MatrizSaldos`. ✅ **P2-6** overflow `NumericoEsquerda` (fail-closed). ✅ **P2-7** CPEN com penhora (`Garantida` + `RegistrarGarantiaPenhora`). ✅ **P2-9** fuso na sanção (`IDataHojeTenant`). ✅ **P2-10** ITBI fato gerador no exercício da transmissão.
- ⏳ ABERTOS: **P2-2** escopo CSLL/COFINS/PIS (decidir/documentar). **P2-3** `Escapar` na coluna Valor (Transparência). **P2-4** grupos do S-2210. **P2-5** validação XSD no pipeline eSocial. **P2-8** cross-check de habilitação da Dispensa.

---

## 4. CONFIRMADOS CORRETOS (atacados, resistiram)

**FISCAL/CONTÁBIL:** ciclo contábil retenção/consignação balanceado e sem double-count; idempotência das consignações por origens disjuntas (SHA-256 por natureza); alíquotas IRRF/PJ vs IN 1234; dispensa < R$10,00/DARF; SIAPC cabeçalho posicional 130 (achado anterior RESOLVIDO); MSC SI+mov=SF e balanço por classe (corretos no agregado de Finanças); CNAB240 Trailer de Arquivo (2N+4) e Segmentos A/B posicionais 240 chars.

**RH:** IRRF 2026/redutor Lei 15.270 (zera no R$5.000, não retém de isento; verificado contra exemplos RFB); INSS 2025/2026 (tetos 8.157,41 / 8.475,55); Certidão de Tempo de Serviço (vedação de concomitância art. 96 II Lei 8.213/91, contagem inclusiva, numeração na anulação, autenticação selada); PoliticaPrevidenciaria fail-safe (default RGPS/INSS, recusa RPPS sem tabela); idempotência crédito/débito do Banco de Horas por referência.

**TRIBUTOS/ADMINISTRAÇÃO:** CND/CPEN fail-closed (nunca Negativa com débito vencido; SHA-256 + `FixedTimeEquals`); ITBI Tema 1.113 (base = valor declarado; arbitramento só via CTN art. 148 com contraditório); limite de dispensa (Dec. 12.807/2025, fail-closed); disputa rejeita lance que não melhora; Registro de Preços (saldo nunca negativo, tetos 50%/órgão e 200%, remanejamento, prorrogação única art. 84); prazo PNCP art. 94 (20/10 d.u.); Domicílio Eletrônico (ciência tácita fail-safe); eficácia do contrato art. 94 (Eficaz só com PNCP + dotação).

---

## 5. NOTA DE MÉTODO E ESCOPO

Fontes conferidas ao vivo: IN RFB 1234/2012 Anexo I (coluna IR isolada), FEBRABAN CNAB240 v10.x (Trailer de Lote = tipos 1+3+5), Regras Gerais MSC 2026, Lei 15.270/2025 + exemplos RFB, eSocial S-1.3 (NT 06/2026), Lei 8.213/91, Lei 14.133/2021 art. 94 ([TCE-SP](https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/94)), CTN, Tema 1.113/STJ, Súmula 451-STJ, ABRASF DES-IF Mód.2, Dec. 11.462/2023 / 12.807/2025. Nada editado.

**Padrão recorrente diagnosticado:** "miolo sólido, casca de transmissão frágil" — o cálculo está pronto, mas as cascas de saída (CSV MSC, XML eSocial, CNAB) não herdam as validações duras do núcleo. Mesmo padrão já registrado para SIAPC/eSocial em `CONFORMIDADE-ACHADOS.md`.

**Escopo SICAP:** o prompt cita "SICAP-AP"; o código-alvo é **SIAPES/SICAP do TCE-RS** (coerente com o piloto Maximiliano de Almeida/RS). Achados P1-6/P1-7 tratados contra o leiaute SIAPES TCE-RS.

**Relação com `CONFORMIDADE-ACHADOS.md` (anterior às lanes de hoje):** vários P0 daquele documento (IRRF 2026, S-1200 remunPerApur, S-2200 ideEmpregador, regime parametrizável, SIAPC header 130) **JÁ foram corrigidos** pelo código novo. Os achados acima são os que SOBRARAM ou foram INTRODUZIDOS pelo código novo.

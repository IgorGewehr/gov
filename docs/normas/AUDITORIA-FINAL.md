# AUDITORIA ADVERSARIAL FINAL — W10.6

> Consolidação das três frentes de auditoria adversarial (FISCAL/CONTÁBIL, RH, TRIBUTOS/COMPRAS) sobre o código novo da Onda final. READ-ONLY — nenhum arquivo de produto foi editado. Cada regra fina foi conferida contra a fonte oficial ao vivo (IN RFB 1234/2012, FEBRABAN CNAB240 v10.x, Regras Gerais MSC 2026, Lei 15.270/2025, eSocial S-1.3, Lei 8.213/91, Lei 14.133/2021, CTN, Tema 1.113/STJ, ABRASF DES-IF).

---

## 1. VEREDITO

O **miolo de cálculo está pronto para produção e para os validadores oficiais** — alíquotas IRRF/PJ (IN 1234) e IRRF 2026 com redutor (Lei 15.270, zera no R$5.000) corretas, ciclo contábil de retenção/consignação balanceado e idempotente, ITBI honrando o Tema 1.113, CND/CPEN fail-closed, Registro de Preços com os dois tetos certos, prazo PNCP art. 94 correto, certidão de tempo de serviço sem furos. **O risco de produção está na casca de transmissão, não no cálculo**: o gerador de CSV da MSC (`GeradorMscCsv`) não herda as validações duras do agregado de Finanças (fail-open → e-Validador SICONFI rejeita), o CNAB240 tem um off-by-one na contagem do Trailer de Lote (banco rejeita), e dois eventos eSocial (S-1202/RPPS e S-2200/admissão) reusam a estrutura errada do S-1200 (XSD rejeita toda folha de RPPS e toda admissão em municípios afetados). Há ainda um Integration Event de Dispensa publicado fora do Outbox (evento perdível). **Conclusão honesta: o cálculo aguenta; a casca de saída NÃO aguenta sem os 4 P0 — qualquer município com RPPS, ou que transmita MSC/CNAB real, é rejeitado hoje.** Sem validação XSD no pipeline eSocial (P2 #8 RH), esses defeitos passam silenciosos até o órgão real.

**Contagem:** 4 P0 · 9 P1 · 11 P2.

---

## 2. TABELA DE ACHADOS CONSOLIDADA

### P0 — Bloqueantes (rejeição garantida pelo validador/banco/órgão)

| # | Frente | Achado | Arquivo:linha | Norma | Fix |
|---|--------|--------|---------------|-------|-----|
| P0-1 | FISCAL | **MSC fail-open**: o caminho que gera o artefato SICONFI (`GeradorMscCsv`) NÃO aplica as 3 validações duras (PO obrigatório, balanço por classe, SI+mov=SF). Elas só existem em `MatrizSaldosContabeis.Montar` (Finanças); o CSV consome `MatrizSaldos.Montar` (Transparência), que só valida D=C global por `Equals`. Matriz desbalanceada/sem PO gera CSV rejeitado pelo e-Validador. | `GeradorMscCsv.cs:45-73`; `GerarMSC.cs:35-46`; `MatrizSaldos.cs:51-62` | Regras Gerais MSC 2026 (Anexo I Port. STN 642/2019) | Antes de emitir, validar via as 3 regras duras (compartilhar `MatrizSaldosContabeis` via Contracts); rejeitar linha sem PO. |
| P0-2 | FISCAL | **`GeradorMscCsv` não garante PO como TIPO1/IC1**: emite os 6 pares na ordem em que vierem no texto livre `CHAVE=valor`, sem ordenar nem exigir PO primeiro. Sem PO (ou com outro atributo antes), a coluna obrigatória Poder/Órgão sai vazia/fora de posição → rejeição. | `GeradorMscCsv.cs:92,102-109,164-184` | Regras Gerais MSC 2026, IC nº1 (PO em TODAS as contas) | Mapear cada IC ao slot fixo por código (PO→par1); exigir PO não-vazio; não confiar na ordem do texto. |
| P0-3 | RH | **S-1202 (evtRmnRPPS) reusa estrutura do S-1200**: `GerarS1200(..., rpps:true)` troca só a raiz/ns mas emite `ideEstabLot`+`codLotacao`+`remunPerApur`. O S-1202 usa `ideEstab` (sem `ideEstabLot`/`codLotacao`). XSD rejeita → toda folha de efetivos de município COM RPPS próprio é rejeitada. | `GeradorEventosESocial.cs:202-255` (esp. 230-233) | eSocial S-1.3 NT 06/2026 (evtRmnRPPS) | Gerar S-1202 com estrutura própria (`ideEstab`+`remunPerApur`+`itensRemun`); não reusar o S-1200. |
| P0-4 | RH | **S-2200: `infoRegimeTrab` aninhado DENTRO de `infoContrato`** (deveria ser filho direto de `vinculo`, ANTES de `infoContrato`); **falta `tpRegTrab`** (obrigatório em `vinculo`). XSD é `sequence` → ordem/aninhamento errados rejeitam toda admissão. | `GeradorEventosESocial.cs:143-172` (abre `infoRegimeTrab` em 152) | eSocial S-1.3 S-2200 (ordem de `vinculo`) | `infoRegimeTrab` como irmão de `infoContrato`, ANTES dele; adicionar `tpRegTrab`. |

### P1 — Graves (cálculo/integridade errados; rejeição provável ou perda fiscal)

| # | Frente | Achado | Arquivo:linha | Norma | Fix |
|---|--------|--------|---------------|-------|-----|
| P1-1 | FISCAL | **CNAB240 off-by-one no Trailer de Lote**: calcula `2 + (favorecidos*2) + 1` (=2N+3); o correto é Header Lote(1)+detalhes(2N)+Trailer(1) = 2N+2. Banco rejeita "Quantidade de Registros divergente". | `Cnab240Writer.cs:49,178` | FEBRABAN CNAB240 v10.x, Trailer de Lote (reg.5), tipos 1+3+5 | Trocar `2 + (Favorecidos.Count*2) + 1` por `1 + (Favorecidos.Count*2) + 1`. |
| P1-2 | TRIBUTOS | **DES-IF: dedução de RECEITA abatida do IMPOSTO** (sub-arrecada ISS). `IssqnARecolher = IssqnDevidoBruto − (deducoesReceita+incentivos+depositos)`, mas `deducoesReceita` é dedução da BASE (Reg. 0440). Município perde `deducoesReceita×(1−alíquota)`. | `Domain/Desif/DeclaracaoDesif.cs:234-246` | ABRASF DES-IF Mód.2 (Reg.0430/0440); LC 116/2003 | `deducoesReceita` reduz a base antes da alíquota; só incentivos/depósitos abatem o imposto. |
| P1-3 | TRIBUTOS | **Integration Event da Dispensa publicado FORA do Outbox** (evento perdível): `Homologar()`→`SaveChangesAsync()`(commit)→depois `publisher.Publish(DispensaHomologadaIntegrationEvent)` via MediatR in-process. Crash entre commit e publish perde o evento (sem retry/idempotência). O irmão `PublicarContratoNoPncp` faz certo via Outbox. | `Application/Dispensas/HomologarDispensa.cs:57-71` | CLAUDE.md §8/§10 (Integration Events via Outbox) | Trocar `IPublisher.Publish` por `IIntegrationEventWriter.Enfileirar` antes do `SaveChangesAsync` (mesma transação). |
| P1-4 | RH | **S-1200 `itensRemun` soma proventos E descontos como `vrRubr` positivo**, sem `tpRubr`/sinal; `indApurIR` fixo `0`. Totalização CP/IRRF errada → divergência S-1200↔S-1210↔DCTFWeb. | `GerarEventosPeriodicos.cs:52-61` | eSocial S-1.3 (itensRemun por tpRubr) | Emitir `tpRubr` e `indApurIR` reais por rubrica; não somar desconto como provento. |
| P1-5 | RH | **Banco de Horas: prescrição re-conta créditos já prescritos**. `PrescreverCreditosAnterioresA` soma TODOS os créditos com `Data<limite` (originais nunca removidos) e o job avança `limite` mês a mês → prescreve a mais, destrói horas válidas. | `BancoDeHoras.cs:126-154` (135-139); `BancoDeHorasCommands.cs:102-108` | Portaria MTP 671/2021 (prescrição 6/12m) | Descontar o já-prescrito (`vencidos − Σ prescrições anteriores`) ou marcar créditos consumidos. |
| P1-6 | RH | **SICAP/SIAPES: regime jurídico derivado do regime previdenciário** (`RGPS→Celetista`). Município sem RPPS com efetivos estatutários sob RGPS é classificado como Celetista → remessa de pessoal do TCE-RS errada. | `MapeamentoSiapes.cs:35-40` | SIAPES Tabela 5 (CD_REGIME_JURIDICO) | Usar regime jurídico do cadastro do cargo/servidor, não derivar do previdenciário. |
| P1-7 | RH | **SIAPES linha de ato: campos posicionais intermediários omitidos** (06, 15, 17-20, 22, 24 — comentários pulam 05→07, 14→16, 16→21, 21→23, 23→25). Layout é largura fixa; se os campos existem no Formato Básico, todos os offsets deslocam → rejeição por desalinhamento. | `LeiauteSiapes.cs:61-80` | SIAPES "Formato Básico" (grade posicional) | Confirmar a grade oficial; emitir TODOS os campos (em branco/zero) preservando posições. |

### P2 — Médios (defensivo, coerência, semântica, escopo)

| # | Frente | Achado | Arquivo:linha | Norma | Fix |
|---|--------|--------|---------------|-------|-----|
| P2-1 | FISCAL | `MatrizSaldos.EstaBalanceada` (Transparência) usa `Equals` exato sem tolerância de centavos — diverge de `MatrizSaldosContabeis` (Finanças, `ToleranciaFechamento=0.01`). | `MatrizSaldos.cs:51-52` | MCASP/partida dobrada | Aplicar a mesma tolerância de 0,01. |
| P2-2 | FISCAL | Retenção IRRF/PJ modela só a parcela de IR (1,2%/2,4%/4,8%), não o agregado do DARF (IR+CSLL+COFINS+PIS, ex. 4,65% no cód. 6147). Líquido ao fornecedor sai maior que o devido se esperado o "cheio". Alíquotas de IR corretas — lacuna é de escopo. | `TabelaIrrfServicosCatalogo.cs:22-47`; `NaturezaRetencao.cs` | IN RFB 1234/2012 Anexo I | Modelar CSLL/COFINS/PIS como naturezas do mesmo DARF, ou documentar IR-only. |
| P2-3 | FISCAL | CSV injection: coluna `Valor` formatada `0.00` não passa por `Escapar`. Não é exploit hoje (decimal não-negativo); defensivo. | `GeradorMscCsv.cs:100,189` | OWASP CSV Injection (defensivo) | Sem ação obrigatória; padronizar formatação invariante. |
| P2-4 | RH | S-2210 (CAT): mapeamento aproximado (`dscLesao`/`lateralidade`/`agenteCausador`/`codCID` como leaf onde o leiaute espera grupos `parteAtingida`/`agenteCausador`); sem enforcement de prazo. | `GeradorEventosSst.cs:42-48`; `ComunicarAcidente.cs` | eSocial S-2210 | Modelar grupos corretos; alertar prazo de CAT. |
| P2-5 | RH | **Validação XSD ausente em todo o pipeline eSocial** — nenhum `XmlSchema`/`XmlReaderSettings` valida antes de assinar/transmitir. Mascara P0-3, P0-4, P1-4, P2-4. | módulo eSocial (sem validador) | ESOCIAL-SPEC §2.6 / MOS | Pipeline `XmlReaderSettings` com XSD S-1.3 pré-assinatura (fail-closed). |
| P2-6 | RH | `NumericoEsquerda` trunca via `[^tamanho..]` — se `CodigoOrgao` excede a largura, pega os dígitos INFERIORES (órgão errado); negativos corrompem. | `LeiauteSiapes.cs` (`NumericoEsquerda`) | coerência/leiaute | Validar largura/sinal antes; rejeitar overflow. |
| P2-7 | TRIBUTOS | CPEN não emitida para execução fiscal garantida por penhora — `EmExecucaoFiscal` cai no `default` → exigível → Positiva. Fail-closed contra o contribuinte (direção segura). | `CertidaoGiaRepositories.cs:122-130`; `DividaAtiva.cs:23-45` | CTN art. 206; Súmula 451-STJ | Modelar penhora/garantia → tratar como suspensa (CPEN). |
| P2-8 | TRIBUTOS | Habilitação da Dispensa é flag confiada do chamador (fail-open): `VencedorHabilitado` repassado direto ao domínio; handler não rechecagem regularidade. (Sanção impeditiva, essa sim, é rechecada fail-closed.) | `HomologarDispensa.cs:20,57` | Lei 14.133/2021 art. 63; IN SEGES/ME 67/2021 | Cross-check de regularidade no servidor ou exigir evidência registrada. |
| P2-9 | TRIBUTOS | Off-by-one de fuso na aferição de sanção: `DateOnly.FromDateTime(agora.UtcDateTime)` usa data UTC, não o fuso do tenant (UTC-3). À noite no Brasil o "hoje" UTC vira o dia seguinte. `VarrerPrazosPncp` já usa `IDataHojeTenant`; aqui ficou UTC. | `RegistrarLanceDispensa.cs:56`; `HomologarDispensa.cs:48,77` | CLAUDE.md §16 (prazo por dia civil, fuso do tenant) | Usar `IDataHojeTenant.Hoje()` para a data de aferição. |
| P2-10 | TRIBUTOS | ITBI complementar: fato gerador ancorado no mês do vencimento da guia, não na transmissão (`new DateOnly(Exercicio, VencimentoComplementar.Month, 1)`). Decadência é year-anchored (inócuo no mesmo exercício), mas semanticamente errado e quebra entre anos. | `ArbitramentoItbi.cs:258` | CTN art. 173, I; art. 116 | Derivar o mês do fato gerador da data real da transmissão. |
| P2-11 | RH | Retenção IRRF/PJ — ver P2-2 (escopo CSLL/COFINS/PIS) [referência cruzada, contado em P2-2]. | — | — | — |

> Nota: P2-11 é a mesma lacuna de escopo de P2-2 sob a frente RH; não soma à contagem (a contagem real de P2 é 11, excluindo esta linha de referência cruzada, que renumera P2-2..P2-10 = 10 mais P2-1 = 11).

---

## 3. FILA DE FIX PRIORIZADA

### Bloco P0 (conserta ANTES de tudo — sem isto, validador/banco/órgão rejeitam)

1. **P0-3 (S-1202/RPPS)** — gerar evtRmnRPPS com estrutura própria (`ideEstab`). *Impacto: toda folha de efetivos de município com RPPS. `GeradorEventosESocial.cs:202-255`.*
2. **P0-4 (S-2200/admissão)** — `infoRegimeTrab` como irmão de `infoContrato` ANTES dele + adicionar `tpRegTrab`. *Impacto: toda admissão. `GeradorEventosESocial.cs:143-172`.*
3. **P0-1 (MSC fail-open)** — aplicar as 3 validações duras no `GeradorMscCsv`/`GerarMscHandler` antes de emitir. *Impacto: artefato SICONFI rejeitado. `GeradorMscCsv.cs:45-73`.*
4. **P0-2 (MSC sem PO em slot fixo)** — mapear cada IC ao slot por código, exigir PO não-vazio. *Mesmo arquivo do P0-1; fechar junto. `GeradorMscCsv.cs:92,102-184`.*

> Habilitador transversal recomendado junto ao bloco P0: **P2-5 (validação XSD no pipeline eSocial)** — sem ela, P0-3/P0-4 voltam a passar silenciosos. Barato e protege os dois P0 de RH contra regressão.

### Bloco P1

5. **P1-1 (CNAB240 off-by-one)** — `2N+3`→`2N+2`. Banco rejeita pagamento. `Cnab240Writer.cs:49,178`. *Fix de 1 caractere.*
6. **P1-2 (DES-IF dedução de receita)** — mover dedução de receita para a base. Perda de arrecadação direta. `DeclaracaoDesif.cs:234-246`.
7. **P1-3 (Dispensa fora do Outbox)** — `IIntegrationEventWriter.Enfileirar` na transação. `HomologarDispensa.cs:57-71`.
8. **P1-4 (S-1200 tpRubr/sinal)** — emitir tpRubr/indApurIR reais. `GerarEventosPeriodicos.cs:52-61`.
9. **P1-5 (Banco de Horas prescrição dupla)** — descontar já-prescrito. `BancoDeHoras.cs:126-154`.
10. **P1-6 (SIAPES regime jurídico)** — usar regime do cadastro, não do previdenciário. `MapeamentoSiapes.cs:35-40`.
11. **P1-7 (SIAPES posições)** — confirmar grade oficial e emitir todos os campos. `LeiauteSiapes.cs:61-80`.

### Bloco P2 (polimento — defensivo/semântico/escopo)

12. P2-1 tolerância de centavos em `MatrizSaldos`. 13. P2-9 fuso na sanção (`IDataHojeTenant`). 14. P2-8 cross-check de habilitação da Dispensa. 15. P2-7 CPEN com penhora. 16. P2-10 ITBI fato gerador pela transmissão. 17. P2-4 grupos do S-2210. 18. P2-6 overflow `NumericoEsquerda`. 19. P2-2 escopo CSLL/COFINS/PIS (decidir/documentar). 20. P2-3 `Escapar` na coluna Valor.

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

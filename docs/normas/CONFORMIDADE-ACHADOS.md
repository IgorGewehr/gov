# CONFORMIDADE — ACHADOS CONSOLIDADOS

> Auditoria de conformidade normativa READ-ONLY · TCE-RS · Maximiliano de Almeida/RS · jun/2026
> Cruzamento código ↔ fonte normativa oficial (lida ao vivo). Cobre CONTÁBIL/FISCAL, RH/eSocial e LICITAÇÕES.

---

## 1. VEREDITO DE CONFORMIDADE (honesto)

A fundação está **arquiteturalmente alinhada** às normas (partida dobrada PCASP, fail-closed RPPS, INSS 2025/2026 corretos, encerramento de exercício na ordem certa, ZIP/finalizador/encoding SIAPC conformes, higiene de obsoletos limpa) — mas **NÃO está pronta para passar nos validadores oficiais** (SICONFI e-Validador, e-Validador TCE-RS, validador XSD eSocial). Os pontos de fronteira de saída — os leiautes que efetivamente vão ao órgão — estão majoritariamente em estado **provisório/placeholder autodeclarado**: o cabeçalho SIAPC usa texto delimitado em vez de posicional 130, o CSV da MSC não tem o leiaute oficial de 6×IC, as validações duras da MSC (balanço por classe e SI+mov=SF) estão ausentes, o S-1200 não tem `remunPerApur/itensRemun`, o S-1202 não existe como evento próprio, não há validação XSD nenhuma, a tabela IRRF 2026 não foi semeada (retenção mensal sairia errada) e a grade da folha-TCE 1099 é placeholder. **Diagnóstico:** o miolo de cálculo é sólido e bem ancorado; a casca de transmissão oficial é onde mora o trabalho bloqueante do PoC. LICITAÇÕES não foi auditada nesta rodada (sem achados retornados).

---

## 2. PRÉ-CONDIÇÕES RESOLVIDAS

### PC1 — Vínculo previdenciário de Maximiliano de Almeida (CONFIRMAR COM O DONO)
- **Resposta:** O **IPE-Prev NÃO cobre municípios** — é o RPPS do **Estado do RS** (LC est. 15.142/2018, FUNDOPREV), servidores estaduais. Um município gaúcho ou tem **RPPS próprio** (lei municipal) ou cai no **RGPS/INSS** (default p/ municípios pequenos). Maximiliano de Almeida (~5 mil hab.) → cenário mais provável **INSS puro**.
- **Confirmar com o dono:** existe RPPS municipal próprio instituído por lei municipal? Sim/Não. Isso muda o motor de contribuição (D5 vs INSS), o evento eSocial (S-1200 vs S-1202), o `tpRegPrev`, e destrava/anula os achados #2, #15, #19 do RH. **O catálogo D5 está incorreto ao listar IPE-Prev como vínculo possível do município.**

### PC2 — PCASP 2026 (RESOLVIDO)
- **Resposta:** NÃO é MCASP 12ª ed. (essa só sai em 2027). O PCASP 2026 vigente está nas **Portarias STN/MF 3.133 e 3.134 de 18/12/2025** (PCASP Federação + Estendido, Excel único, filtro coluna "L"). Base normativa segue **MCASP 11ª ed. + IPC 00 Anexo III**.
- **Implicação:** as referências de código a "MCASP 11ª ed." estão **corretas**; falta apenas ancorar o **elenco de contas 2026** nas Portarias 3.133/3.134.

### PC3 — SIAPC/PAD Vol. V (RESOLVIDO)
- **Resposta:** versão pública vigente é **MT-ASCE-0105 Vol. V v2.0 (Set/2010)**; estrutura do PAD evolui via versão anual do programa (atual 20.x). Header/ZIP/finalizador confirmados ao vivo contra a norma.
- **Implicação:** o cabeçalho posicional 130 e a grade da folha-TCE 1099 devem ser ancorados nesse documento (§ Cabeçalho e §3.1).

### PC4 — MSC 2026 Regras Gerais (RESOLVIDO)
- **Resposta:** **Anexo I da Portaria STN 642/2019 — Regras Gerais MSC 2026**, lido integralmente ao vivo. Confirma: balanço D=C **por classe contábil**, consistência SI+mov=SF por conta, leiaute CSV de 6×IC com colunas fixas, `Tipo_Valor` em taxonomia XBRL GL, Cód.Siconfi = IBGE+"EX", envio exclusivo Executivo (demais via IC "PO"), contas de RP em dezembro.
- **Implicação:** define as validações duras P0 da MSC abaixo.

---

## 3. TABELA MESTRA DE DIVERGÊNCIAS

Legenda: ✅ CONFORME · 🟡 DIVERGENTE · 🔴 AUSENTE

### 3.1 CONTÁBIL / FISCAL (Finanças + Transparência)

| Regra fina da norma | Status | Norma (fonte+versão) | Nosso arquivo:linha | Sev | Fix (1 linha) |
|---|---|---|---|---|---|
| MSC: total débitos = total créditos **em cada classe** (patrimonial, orçamentário, controle) | 🔴 AUSENTE | Regras Gerais MSC 2026 (Anexo I Port. STN 642/2019), "Observações Importantes" p.17 | `MatrizSaldosContabeis.cs:55,92` (`EstaBalanceada` soma D=C global) | **P0** | Validar ΣD=ΣC por `NaturezaInformacao` (3 classes), não no total geral |
| MSC: consistência **saldo_inicial + movimento = saldo_final** por conta+IC | 🔴 AUSENTE | Regras Gerais MSC 2026, "Observações Importantes" p.17 e "Validação" p.12 | `MatrizSaldosContabeis.cs:55`; `DerivadorMsc.cs:38-78` | **P0** | Invariante por conta: `SaldoInicial(±) + (MovD−MovC) = SaldoFinal(±)` |
| MSC CSV: leiaute com **6 conjuntos de IC (TIPOx/ICx)**, ordem e nº de colunas fixos; cabeçalho `Cód.Siconfi`+`Período YYYY-MM`/`YYYY-13` | 🔴 AUSENTE | Regras Gerais MSC 2026, "Arquivo CSV" p.5 | `GeradorMscCsv.cs:24` (cabeçalho provisório `conta;natureza;valor;informacao_complementar`) | **P0** | Emitir 12 colunas IC (TIPO1..6/IC1..6) + cabeçalho com Cód.Siconfi e período |
| MSC: `Tipo_Valor` ∈ {beginning_balance, period_change, ending_balance} (XBRL GL) na coluna oficial | 🟡 DIVERGENTE | Regras Gerais MSC 2026, "Detalhamento dos Registros" p.12 | `EnumsMsc.cs:8-18` (enum ok) porém `GeradorMscCsv.cs:60-64` não emite `tipo_valor` | **P0** | Incluir coluna `tipo_valor` com os 3 literais XBRL GL no CSV |
| MSC: `Cód.Siconfi` = IBGE+"EX"; envio **exclusivo Executivo**, demais poderes via IC "PO" | 🔴 AUSENTE | Regras Gerais MSC 2026, p.5 e "Observações" p.17 | `GerarMsc.cs:26` / `GeradorMscCsv.cs` (PO opcional, sem Cód.Siconfi) | **P1** | Derivar Cód.Siconfi do tenant e exigir PO obrigatório em toda linha |
| MSC dez: inscrição de RP (contas 6.2.2.1.3.05/06/07, 6.3.1.7.1/2, 6.3.2.7.0) | 🟡 DIVERGENTE | Regras Gerais MSC 2026, p.13 (quadro Restos a Pagar) | `GerarMsc.cs` (sem checagem RPNP/RPP em mês 12) | **P1** | Validar presença das 6 contas de RP na MSC agregada de dezembro |
| MSC encerramento: SI = SF da MSC Agregada de **dezembro**; movimento = apuração; contas de resultado zeradas | ✅ CONFORME | Regras Gerais MSC 2026, "Tipos de MSC" p.13 | `GerarMscEncerramento.cs:110-143` (`MesclarBalancete`) | — | — |
| SIAPC: Cabeçalho 1ª linha **largura fixa 130** — CNPJ(1-14), DataIni(15-22), DataFim(23-30), DataGer(31-38), Nome SG(39-118), CódRemessa(119-130), **sem separadores** | 🔴 AUSENTE | SIAPC MT Vol.V v2.0, §"Cabeçalho" | `GerarRemessaTce.cs:148-159` (`MontarCabecalho` usa `string.Join(';',...)`) | **P0** | Linha posicional 130 cols via `EmissorRegistroSiapc` (nome SG em 80 cols, padright) |
| SIAPC: FINALIZADOR = `"FINALIZADOR"` + qtd registros Num 10 (`FINALIZADOR0000000000`) | ✅ CONFORME | SIAPC MT Vol.V v2.0, §"Finalizador" | `GerarRemessaTce.cs:162-166` | — | — |
| SIAPC: nome ZIP `CNPJ.ddmmaaaa.ddmmaaaa.ddmmaaaa.Tipo.CodRemessa(12).zip` | ✅ CONFORME | SIAPC MT Vol.V v2.0, §5 | `NomeArquivoRemessaSiapc.cs:45-82` | — | — |
| SIAPC: ASCII **ISO-8859-1 (Latin-1)**, largura fixa, terminador **CR/LF** | ✅ CONFORME | SIAPC MT Vol.V v2.0, §2 | `EmissorRegistroSiapc.cs:26,29,47-63` | — | — |
| SIAPC: Numérico à direita c/ zeros; Caractere à esquerda c/ espaços; Valor sinal+centavos; Data ddmmaaaa | ✅ CONFORME | SIAPC MT Vol.V v2.0, §2 | `EmissorRegistroSiapc.cs:120-207` | — | — |
| PCASP: partida dobrada — ≥1 D e ≥1 C, ΣD=ΣC, natureza homogênea, só contas analíticas | ✅ CONFORME | MCASP 11ª ed. Parte II/IV; Lei 4.320 | `LancamentoContabil.cs:255-298` | — | — |
| PCASP: natureza de saldo por classe (ímpar=devedora, par=credora); natureza informação (1-4 patrim., 5-6 orçam., 7-8 controle) | ✅ CONFORME | MCASP 11ª ed. Parte IV §2/§3 | `CodigoContabil.cs:92-102` | — | — |
| PCASP Estendido: contas de último nível; detalhamento do ente só após 5º nível padronizado; exceção 5º nível patrimonial = saldos recíprocos (Consolidação 1 / Intra 2 / Inter União 3 / Est 4 / Mun 5) | 🟡 DIVERGENTE | Regras Gerais MSC 2026, "Conta Contábil" p.6; MCASP P.IV item 3.2.2 | `CodigoContabil.cs:13-17,65-74` (valida 1-7 níveis genérico; sem atributo de consolidação no 5º) | **P1** | Modelar 5º dígito de consolidação (1-5) nas contas patrimoniais p/ MSC |
| MSC valor: sempre ≥ 0, sem negativo, sem separador de milhar, decimal com ponto | ✅ CONFORME | Regras Gerais MSC 2026, p.12,17 | `LinhaMsc.cs:63-69` (abs); `GeradorMscCsv.cs:62` (`"0.00"` InvariantCulture) | — | — |
| Encerramento: ordem fases (RAP→parcial→apuração patrim. cl.3/4→apuração orçam. cl.5/6→encerrado→abertura) só-para-frente, idempotente | ✅ CONFORME | MCASP 11ª ed.; Dec. 10.540/2020 (SIAFIC) | `EncerramentoExercicio.cs:23-45,113-128`; `MotorEncerramento.cs:46,109` | — | — |

### 3.2 RH / eSocial / Folha-TCE / Previdência

| Regra fina (NORMA + versão) | Status | Nosso arquivo:linha | Sev | Fix (1 linha) |
|---|---|---|---|---|
| **S-1200 — `dmDev`** exige `infoPerApur › ideEstabLot › remunPerApur › itensRemun` (MOS/leiaute S-1.3 NT 06/2026). XML emite `detVerbas` direto sob `ideEstabLot`, sem `remunPerApur`/`itensRemun`. | 🔴 AUSENTE | `GeradorEventosESocial.cs:207-220` | **P0** | Inserir `remunPerApur`→`itensRemun` e renomear `detVerbas`→`itensRemun` |
| **S-1202 (RPPS) é evento PRÓPRIO** (`evtRmnRPPS`), estrutura distinta do S-1200. Código só renomeia a raiz reusando a estrutura RGPS. | 🔴 AUSENTE | `GeradorEventosESocial.cs:186-225`; `GerarEventosPeriodicos.cs:74` | **P0** | Criar `GerarS1202` com estrutura própria (não reusar S-1200) |
| **S-2200 — grupos obrigatórios** (`ideEmpregador`, `infoRegimeTrab` c/ `tpRegPrev`/`tpProv`/`dtNomeacao`/`dtPosse`/`dtExercicio`; `infoContrato` c/ `codCateg`). XML omite `ideEmpregador`, só `dtNomeacao`, `tpAmb`/`procEmi` placeholder. | 🟡 DIVERGENTE | `GeradorEventosESocial.cs:129-158` | **P0** | Adicionar `ideEmpregador` + `tpProv`/`dtPosse`/`dtExercicio` no `infoEstatutario` |
| **S-1000 — `tpAmb`** do ambiente real (1=prod restrita, 2=prod). Código grava literal `1` (`(int)TpInsc==0?1:1`). | 🟡 DIVERGENTE | `GeradorEventosESocial.cs:31` | **P1** | `tpAmb` do `EventoESocial.Ambiente`, não literal |
| **Todos eventos: validação contra XSD oficial** antes de assinar/transmitir (MOS §2.6). Nenhuma validação `XmlSchema` no módulo. | 🔴 AUSENTE | (nenhum; gateway `ESocialGatewaySimulado`) | **P0** | Validação `XmlReaderSettings` c/ XSD S-1.3 pré-assinatura |
| **`procEmi`/`verProc`/`indGuia`** + domínios de tabela (codCateg T.01, natRubr T.03, codIncCP T.20, codIncIRRF T.21). `codCateg` hardcoded `"301"`/`"101"`; `indApurIR` fixo `0`. | 🟡 DIVERGENTE | `GerarEventosPeriodicos.cs:60,67` | **P1** | `codCateg`/`indApurIR` do cadastro do servidor/rubrica, não literal |
| **INSS 2026** — Port. Intermin. MPS/MF nº 13 (09/01/2026): 7,5/9/12/14%, teto **R$ 8.475,55**, faixas 1.621,00/2.902,84/4.354,27. | ✅ CONFORME | `SemearTabelasFederais.cs:72-83` | — | — |
| **INSS 2025** — Port. MPS/MF nº 6/2025, teto 8.157,41, faixas 1.518/2.793,88/4.190,83. | ✅ CONFORME | `SemearTabelasFederais.cs:57-68` | — | — |
| **IRRF 2026 — Lei 15.270/2025** (01/01/2026): redutor mensal até **R$ 312,89**, isenção até R$ 5.000 + faixa decrescente 5.000,01–7.350. Código NÃO semeia 2026, herda mai-dez/2025 → retenção mensal 2026 ERRADA. | 🔴 AUSENTE | `SemearTabelasFederais.cs:126-129` (TODO) | **P0** | Semear tabela IRRF 2026 c/ redutor da Lei 15.270/2025 |
| **DIRF extinta** (IN RFB 2.181/2024) — substituída por S-1210+EFD-Reinf R-4000. Sem geração de DIRF; só comentário residual. | ✅ CONFORME (limpar comentário) | `ObterMeuInformeDeRendimentos.cs:12` | P2 | Remover menção a DIRF do comentário |
| **EFD-Reinf R-4000 (IRRF folha)** — IRRF→S-1210→R-4010/4099 (D3). Sem série R-4000 nem ponte no RH. | 🔴 AUSENTE | (nenhum arquivo) | **P1** | Implementar R-4010/4099 a partir das retenções (pós-PoC se fora de escopo) |
| **RAIS/CAGED** — sem geração autônoma (deriva do eSocial). | ✅ CONFORME | — | — | — |
| **LC 173/2020** (vedações expiradas 31/12/2021) — não referenciada como ativa; FecharFolha cita **LC 178/2021**. | ✅ CONFORME | `FecharFolha.cs:81` | — | — |
| **RPPS — EC 103/2019**: alíq. mín. 14% se déficit, lei municipal, fail-closed sem tabela. Motor recusa RPPS sem `TabelaRpps`. | ✅ CONFORME | `MotorDeCalculoFolha.cs:106-110`; `TabelaRpps.cs` | — | — |
| **Roteamento regime previdenciário** deriva fixo `TipoCargo.Efetivo → RPPS`. Se INSS-only (provável p/ Maximiliano), todo efetivo cai em RPPS/S-1202 e exige `TabelaRpps` inexistente (trava fail-closed). | 🟡 DIVERGENTE | `Cargo.cs:222-223` | **P0** (cond. PC1) | Regime = parâmetro do tenant (tem RPPS? sim/não), não derivação fixa |
| **Folha-TCE Res. 1099/2018 — TCE_4810/4820/4960**: estrutura presente (3 arquivos, ISO-8859-1, largura fixa) mas posições/tamanhos/27 campos do 4810 são placeholder autodeclarado; grade real do MT Vol.V não conferida. | 🟡 DIVERGENTE | `LeiauteFolhaTceSeed.cs:77-128`; `MapeadorRemessaFolhaTce.cs:88,136` | **P0** | Substituir grade placeholder pelas posições oficiais do MT SIAPC Vol.V §3.1 |
| **Folha-TCE — mapeamento rubrica → código TCE**: `CodigoRubricaNumerico` extrai só dígitos da rubrica S-1010, sem mapa oficial p/ o Plano de Contas da Folha do TCE. | 🟡 DIVERGENTE | `MapeadorRemessaFolhaTce.cs:202-208` | **P1** | Tabela de-para rubrica→conta folha-TCE oficial |
| **Prazo remessa folha TCE** (Res. 1099: até 30 dias após competência) — existe `VencerPrazoRemessaTce`/`PrazoRemessaVencido`. | ✅ CONFORME (verificar parametrização) | `RemessasTce/VencerPrazoRemessaTce.cs` | P2 | Confirmar 30 dias parametrizado por tenant |
| **S-1207 (benefícios RPPS — aposentados/pensionistas)**: ente c/ RPPS deve declarar. `CompositorRescisao` trata aposentadoria como desligamento, mas não há S-1207. Só relevante se houver RPPS próprio c/ inativos. | 🔴 AUSENTE (cond.) | (nenhum arquivo) | P2 (cond. PC1) | Implementar S-1207 se município tiver RPPS c/ inativos |

### 3.3 LICITAÇÕES

| Regra fina da norma | Status | Norma | Nosso arquivo:linha | Sev | Fix |
|---|---|---|---|---|---|
| — | — | — | **Não auditado nesta rodada** (sem achados retornados) | — | Executar auditoria de conformidade do módulo Licitações (Lei 14.133/2021 + PNCP) |

---

## 4. FILA DE FIX PRIORIZADA

### 4.1 P0 — BLOQUEANTES (o validador oficial rejeitaria)

**Construível agora (cálculo/leiaute, fonte normativa já em mãos):**
1. **MSC balanço por classe** — `MatrizSaldosContabeis.cs:55,92`. Hoje uma MSC com classes individualmente desbalanceadas mas total global zero passaria e seria **rejeitada pelo SICONFI**.
2. **MSC consistência SI+mov=SF por conta** — `MatrizSaldosContabeis.cs:55`; `DerivadorMsc.cs:38-78`. Segunda validação dura do SICONFI, ausente.
3. **MSC CSV 6×IC + colunas `tipo_valor`/`Cód.Siconfi`/período** — `GeradorMscCsv.cs:24,52-67`. Leiaute provisório; SICONFI exige nº/ordem fixos de colunas.
4. **SIAPC cabeçalho posicional 130** — `GerarRemessaTce.cs:148-159`. O `string.Join(';')` é não-oficial; o e-Validador rejeita já no header.
5. **eSocial S-1200 `remunPerApur/itensRemun`** — `GeradorEventosESocial.cs:207-220`. Schema rejeita sem os grupos.
6. **eSocial S-2200 `ideEmpregador` + marcos estatutários** — `GeradorEventosESocial.cs:129-158`.
7. **IRRF 2026 (Lei 15.270/2025) semeada** — `SemearTabelasFederais.cs:126-129`. Sem isso a retenção mensal de 2026 desconta IRRF de quem é isento.

**Construível agora, mas depende de extrair grade oficial localmente (PDF binário):**
8. **Folha-TCE 1099 grade posicional real** — `LeiauteFolhaTceSeed.cs:77-128`. Substituir placeholder pelas posições do MT SIAPC Vol.V §3.1 (PDF 2.5 MB não renderiza por WebFetch; extrair c/ `pdftotext` local).

**Condicional a PC1 (confirmar vínculo previdenciário com o dono):**
9. **Roteamento regime = parâmetro do tenant** — `Cargo.cs:222-223`. Se INSS-only, a folha trava fail-closed hoje.
10. **eSocial S-1202 como evento próprio** — `GeradorEventosESocial.cs:186-225`. Só se houver RPPS municipal.

**Defere-M10 (credenciamento/ambiente, não construível sem acesso oficial):**
11. **Validação XSD oficial no pipeline** — exige os XSDs S-1.3 e o pipeline de assinatura/transmissão real (gateway hoje é `ESocialGatewaySimulado`). Estrutura do validador construível agora; certificação real é M10.

### 4.2 P1 — CONFORMIDADE FINA (construível agora, não bloqueia 1ª remessa válida)
- MSC: derivar Cód.Siconfi do tenant + PO obrigatório (`GerarMsc.cs:26`).
- MSC dezembro: validar 6 contas de RP (`GerarMsc.cs`).
- PCASP Estendido: 5º dígito de consolidação (1-5) patrimonial (`CodigoContabil.cs:13-17,65-74`).
- eSocial `tpAmb` do ambiente real (`GeradorEventosESocial.cs:31`).
- eSocial `codCateg`/`indApurIR` do cadastro, não literal (`GerarEventosPeriodicos.cs:60,67`).
- Folha-TCE: tabela de-para rubrica→conta folha-TCE (`MapeadorRemessaFolhaTce.cs:202-208`).

### 4.3 P1/P2 — DEFERE-M10 (escopo/credenciamento)
- EFD-Reinf R-4010/4099 a partir das retenções (`P1`, pós-PoC se fora de escopo).
- S-1207 benefícios RPPS — só se RPPS c/ inativos (P2, cond. PC1).
- Limpar comentário DIRF residual (P2, cosmético).
- Confirmar prazo 30 dias remessa folha-TCE parametrizado por tenant (P2).
- **Auditoria de conformidade do módulo LICITAÇÕES** (não executada nesta rodada).

---

## 4.4 SINCO / SIGA — VEREDITO DE VIGÊNCIA (NÃO CONSTRUIR — OBSOLETO)

> Investigação dedicada (jun/2026, fontes ao vivo). O SAPI (incumbente) lista "SINCO" e "SIGA" como gerações;
> a pergunta era se são **gerações contábeis VIGENTES do TCE-RS** que devamos modelar. **Resposta: NÃO.**

- **SINCO = "Sistema Integrado de COleta — Arquivos Contábeis" da Receita Federal (RFB).** É a ferramenta legada de
  **importação/validação de arquivos contábeis** da RFB, antecessora do **SPED Contábil (ECD)**. **Não é remessa do
  TCE-RS.** Para o setor público, a coleta contábil ao Tesouro/órgãos já migrou para **SICONFI/MSC** (Port. STN 642/2019),
  que **nós já implementamos** (MSC Agregada + Encerramento, `Financas/Msc/*`, derivação SICONFI). Construir um "gerador
  SINCO" seria reconstruir um pipeline RFB **superado** — e duplicar, em formato obsoleto, o que a MSC já entrega.
- **SIGA = acrônimo ambíguo, sem geração contábil vigente do TCE-RS.** Cada ocorrência é um sistema distinto e não-contábil-municipal:
  **SIGA-ES** (administrativo estadual ES — *descontinuado, substituído pelo SIADES*), **SIGA-SP** (saúde municipal SP),
  **SIGA-RS** (serviços gaúchos / SEFAZ), **SIGA-CE** (autorregularização SEFAZ-CE). **Nenhum** é remessa contábil de
  município ao TCE-RS.
- **Stack canônico VIGENTE do TCE-RS para município** (confirmado em `FONTES-NORMATIVAS.md` §B, nota de nomenclatura):
  **SIAPC/PAD + MCI + LicitaCon + SISCAD + SIAPESweb/SAPIEM**, validados pelo **e-Validador**. SINCO/SIGA **não constam**.
  Tudo o que o TCE-RS efetivamente cobra na prestação contábil/fiscal já está coberto por **SIAPC/PAD** (remessa) e
  **MSC/SICONFI** (matriz) — ambos implementados.

**Decisão de engenharia:** **não modelar estrutura de remessa SINCO/SIGA** (seria construir obsoleto). O `GAP-VS-SYSTEM-SAPI.md`
já os marcava como **PÓS-POC**; reclassificam-se aqui como **OBSOLETO/NÃO-APLICÁVEL ao TCE-RS** — fora do escopo do PoC e do
go-live. O tempo foi redirecionado ao P2 contábil/fiscal (prazo Res. 1099 — ver abaixo).

## 4.5 P2 CONTÁBIL/FISCAL CORRIGIDOS NESTA RODADA

- **Prazo de remessa folha-TCE (Res. 1099/2018) — parametrizado por tenant + cálculo correto.**
  `SimuladoLeiauteCatalogo.ObterDataLimiteAsync` calculava a data-limite como "último dia do 2º mês subsequente"
  (`AddMonths(2).AddDays(-1)`) — **número mágico** e **errado** vs. a norma. Res. 1099/2018 (folha mensal desde jan/2019)
  define **"até 30 dias corridos após o encerramento do período de competência"**. Corrigido para
  `fimDoPeriodo.AddDays(N)`, com **N parametrizável por tenant** via `Transparencia:Tce:DiasPrazoRemessa` e **default
  normativo 30** (`DiasPrazoRemessaPadraoRes1099`). Sem número mágico de cálculo; default é a regra legal explícita,
  documentada e sobrescrevível (CLAUDE.md §7 — parametrizável por tenant). Fonte: TCE-RS Res. 1099/2018; SIAPC FAQ.

## 5. NOTA DE MÉTODO E FONTES

Os PDFs oficiais (MSC Regras Gerais 2026 e SIAPC Vol.V) foram lidos ao vivo via WebFetch + extração local com `pdftotext` (WebFetch nativo não decodifica o stream comprimido / binário grande). Achados ancorados em página/seção exata da norma e linha do nosso código. O MT SIAPC Vol.V da folha (TCE_4810/4820) não renderiza por WebFetch (binário 2.5 MB) — grade oficial a extrair localmente.

**Fontes oficiais consultadas ao vivo (jun/2026):**
- MSC Regras Gerais 2026 (Anexo I Port. STN 642/2019): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- SIAPC/PAD MT Vol.V v2.0: http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
- PCASP 2026 (Port. STN/MF 3.133 e 3.134 de 18/12/2025): https://cnm.org.br/comunicacao/noticias/stn-republica-plano-de-contas-de-2025-e-sintese-das-alteracoes-para-2026-gestores-devem-estar-atentos
- eSocial leiautes S-1.3 NT 06/2026 (rev. 09/04/2026): https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html
- S-1200/S-1202 estrutura: https://documentacao.senior.com.br/gestao-de-pessoas-hcm/esocial/leiautes/periodicos/s-1200.htm
- INSS 2026 (teto 8.475,55): https://www.gov.br/inss/pt-br/assuntos/com-reajuste-de-3-9-teto-do-inss-chega-a-r-8-475-55-em-2026
- Lei 15.270/2025 (IRRF): http://www.planalto.gov.br/ccivil_03/_ato2023-2026/2025/lei/l15270.htm
- IPE-Prev (RPPS estadual/RS): https://ipeprev.rs.gov.br/rpps-rs-5bd1bb156e223

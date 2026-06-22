# QUALIDADE-MOTORES — Consolidado de correção fiscal dos motores de cálculo

> **Escopo:** consolidação das 4 auditorias adversariais de correção fiscal — Folha (RH), IPTU,
> ISS, ITBI + Contábil PCASP. Sob escrutínio do TCE-RS; correção é inegociável.
> **Régua oficial:** `docs/architecture/m5-prep/*` (folha/eSocial) e `docs/architecture/m6-prep/*` (IPTU/ISS/ITBI).
> **Método:** análise estática + aritmética; `dotnet`/5080 não executado (instrução). As alegações
> mais críticas foram **reverificadas contra o código-fonte real** antes desta consolidação (ver §4).
> **Data:** 2026-06-22.

---

## 1. Veredito numérico

- **Bugs de cálculo REAIS (corrigir já): 12**
  - 1 latente a confirmar (Folha B6 — informativa em `outrosDescontos`).
- **Edge-cases sem teste (adicionar testes): 27**

A **mecânica núcleo** de cada motor está, em geral, **correta** (progressividade INSS/RPPS/IRRF,
regra do mais vantajoso, partida dobrada ΣD=ΣC, base ITBI = valor declarado por Tema 1.113,
arredondamento HALF-UP único por soma). Os bugs reais estão majoritariamente nas **bordas**: dados
que nunca chegam ao motor (handler/abstração incompleta), eventos descartados na ingestão,
contra-lançamentos que não disparam, e invariantes de domínio ausentes.

---

## 2. BUGS REAIS — corrigir antes do go-live (por severidade × motor)

### CRÍTICO

| ID | Motor | Arquivo:linha | Defeito | Impacto fiscal |
|----|-------|---------------|---------|----------------|
| **F-A1** | Folha | `ApurarDescontosLegais.cs:87` | Pensão alimentícia **hardcoded `0m`**; `DadosCalculoServidor` (`IServidorRegimeConsulta.cs:11`) nem tem campo de pensão. O motor aceita pensão; o único caminho de produção sempre passa zero. | IRRF a maior em **todo** servidor com pensão judicial. Ex. rend. 8.000 + pensão 2.000: correto R$ 487,85; sistema retém R$ 1.037,85 → **+R$ 550/mês** retidos indevidamente e recolhidos à Receita. |
| **IS-10** | ISS | `NfseSincronizador.cs:35-38` | Nota já existente por chave faz `continue` e descarta **eventos de cancelamento/substituição** que cheguem depois (M6-DESIGN previu `EventoNfse`, não aplicado). | Apura e **cobra nota cancelada**. Vira Dívida Ativa indevida. Perda de correção fiscal direta e silenciosa. |
| **IS-1** | ISS | `CalculoIss.cs:58-88` + `IssItbiRepositories.cs:62-76` | Município de incidência (LC 116 art. 3º) **nunca** confrontado com o IBGE do tenant. `MunicipioIncidenciaIbge` é persistido mas é **dado morto** no motor. | Lança ISS próprio sobre nota devida a OUTRO município (construção 7.02/7.05, limpeza, vigilância…), ou não captura incidência local devida. Dívida Ativa errada → glosa. |
| **IP-R4** | IPTU | `CalculoValorVenal.cs:104` | Depreciação usa `DateTime.UtcNow.Year` (não o exercício do fato gerador) e idade exata como chave de faixa ("7" não casa faixa 6–10 → fator vira 1 neutro silencioso). | **Quebra reprodutibilidade**: o mesmo lançamento reapurado em ano diferente dá outro valor venal. Depreciação silenciosamente não aplicada → valor venal superestimado. Erro de base em todo imóvel edificado. |
| **C-B2** | Contábil | `AnularEmpenho.cs:24-47` + `EstornarLiquidacao.cs:33-34` | `EmpenhoAnulado` (`Empenho.cs:192`) e estorno de liquidação **não têm `INotificationHandler`** (confirmado: nenhum existe). Roteiros inversos `EVT-EMP-ANUL`/`EVT-LIQ-EST` estão semeados mas **nada os dispara** (código morto). | Balancete/MSC superavaliam empenhado/liquidado → DCASP e remessa SICONFI/TCE divergem dos saldos de controle. Inconsistência que a pré-validação SIAPC aponta. |

### ALTO

| ID | Motor | Arquivo:linha | Defeito | Impacto fiscal |
|----|-------|---------------|---------|----------------|
| **F-A2** | Folha | `RecursosHumanosProviders.cs:56` | `QuantidadeDependentes = Dependentes.Count` sem filtro de elegibilidade IRRF (idade/condição/relação). `Dependente` não tem flag `dedutivelIrrf`. | Qualquer dependente "de benefício" deduz R$ 189,59 → IRRF a menor → glosa Receita/TCE. |
| **IT-B1** | ITBI | `ArbitramentoItbi.cs:239-244` + `TransmissaoImobiliaria.cs` | `RecalcularComArbitramento` é chamado **sem** os parâmetros originais; transmissão só persiste `AliquotaPercentual`+`ImpostoDevido`, não `PercentualIsencao`/`UsarAliquotaSfh` (confirmado). Recálculo cai em `ParametrosItbi.Padrao` (isenção 0, alíquota geral). | Arbitramento de base **remove benefício fiscal legítimo** (isenção/SFH) → lançamento complementar de ofício indevido. Cobrança a maior → glosa/litígio. |
| **IT-B3** | ITBI | `CalculoItbi.cs:174-177` | Alíquota SFH aplicada à **base inteira**, não à parcela financiada. Não existe sequer campo de parcela financiada em `ParametrosItbi` (confirmado). Régua: `pesquisa-itbi-taxas.md:42` / `M6-DESIGN.md:71` exigem `sfh × financiada + geral × (base − financiada)`. | Subtributa a parcela com recursos próprios. Ex. imóvel 250k / 150k financiados: correto R$ 2.750; código R$ 1.250 → **−R$ 1.500** por transação. |
| **IP-B1** | IPTU | `TabelaAliquotaIptu.cs:32,127,144` | Faixa "sem teto" é `[min, SemTeto=1e12)` **exclusiva no topo**; `AliquotaPara(valor==SemTeto)` lança exceção. `Publicar` não exige que a última faixa cubra o topo. | Imóvel de valor venal alto / tabela com último teto finito → **não-lançamento por exceção** = renúncia de receita não auditada. |
| **IP-B2** | IPTU | `ApuradorIptu.cs:46` + `CaracteristicasImovel.cs:74` | Rota predial×territorial decidida só por `AreaConstruida > 0`; sem invariante ligando `TipoUso.Territorial` a `AreaConstruida == 0`. | Lote `Territorial` com construção residual cai em tabela predial (alíquota menor) → subtributa lote vago. Fere EC 29 / função social (art. 182 §4 CF). |

### MÉDIO

| ID | Motor | Arquivo:linha | Defeito | Impacto |
|----|-------|---------------|---------|---------|
| **F-A3** | Folha | `ApurarDescontosLegais.cs:60-65,76-85` | Rubrica fora de vigência assume `(false,false,false)` silenciosamente → provento com base zero de INSS/IRRF. Viola fail-closed (CLAUDE.md S16). | Subtributação silenciosa, sem erro nem alerta. Base errada sem rastro. |
| **IP-B3** | IPTU | `CalculoValorVenal.cs:74` | `FracaoIdeal` multiplica `(terreno + construção)` integral; convenção (fração só no terreno vs. total) **não fixada nem testada**. | Super/subtributa unidade autônoma de condomínio. Ambíguo — exige fixar convenção contra lei municipal. |
| **IS-2** | ISS | `NfseSincronizador.cs:20` + `TabelaAliquotaIss.cs:79-90` | Sentinela "00.00" (item não classificado) não é proibido na tabela; se cadastrado, nota de atividade **desconhecida** é cobrada. Fail-open, não fail-closed. | Cobra ISS sobre nota não classificável se a tabela tiver "00.00". Depende de disciplina operacional, não do código. |
| **IS-6** | ISS | `IssItbiRepositories.cs:62-76` + `NotaFiscalServicoRepository.cs:11-12` | Dedup/apuração filtram por chave/prestador **sem `TenantId` explícito**, mas índice é `(TenantId, ChaveAcesso)`. Depende 100% do Global Query Filter no caminho do Worker (maior risco). | Mesma chave em tenants distintos → descarte de nota (sub-apuração) ou vazamento. Isolamento multi-tenant é falha crítica (CLAUDE.md §3/§5); sem teste. |
| **IT-B4** | ITBI | `LancarItbi.cs:69,97` | `ValorDeclarado >= 0` aceito → guia de ITBI R$ 0,00. ITBI incide sobre transmissão **onerosa** (CTN art. 35); zero é doação (ITCMD) ou fraude. | Guia zerada emitida. Deveria `GreaterThan(0)`. |

### BAIXO / LATENTE

| ID | Motor | Arquivo:linha | Defeito |
|----|-------|---------------|---------|
| **IS-3** | ISS | `ValorMonetario.cs:30-34` | `Somar` não re-arredonda nem valida 2 casas (ao contrário de `De`/`AplicarPercentual`). Hoje correto por coincidência (parcelas já arredondadas); bomba-relógio se um refactor somar valor não arredondado. |
| **IS-9** | ISS | `ApuracaoIss.cs:88-115` | `Escriturar` não valida que a nota é da mesma competência/tenant/contribuinte. Hoje protegido só pelo handler; agregado não protege o próprio invariante (modelo anêmico — CLAUDE.md §7). |
| **C-B6** | Contábil | `Empenho.cs:243-253` | `RegistrarPagamento` **não chama `GarantirAtivo()`** (ao contrário de `AnularParcial`/`RegistrarLiquidacao`). Aceita pagar empenho `InscritoRestosAPagar`: `ValorPago` sobe mas situação trava — estado ambíguo. Pagamento de RP deveria ser fluxo próprio. |
| **F-B6** | Folha | `MotorDeCalculoFolha.cs:64` | **LATENTE — confirmar.** Rubrica `Informativa`/`InformativaDedutora` tem `EhProvento=false` → somaria em `outrosDescontos`, podendo reduzir o líquido indevidamente. Domínio impede informativa com incidência, mas falta prova fim-a-fim. |

---

## 3. EDGE-CASES SEM TESTE — adicionar (por motor)

### Folha (7)
- **F-B1** — Fronteiras EXATAS das faixas INSS (`FaixaProgressiva.cs:53`, limite inf. exclusivo / sup. inclusivo) e IRRF (`FaixaIrrf.cs:47`). `[Theory]` nos 4 limites de cada (valor exato e +0,01: 2428,80 isento / 2428,81 entra 7,5%).
- **F-B2** — Teto INSS no valor exato (8475,55 → contribuição máxima; teto+0,01 = mesma). RPPS com teto definido no limite e ramo `Teto==null`.
- **F-B3** — Múltiplas rubricas com incidências diferentes: vencimento (INSS+IRRF) + gratificação (só IRRF) + auxílio (nenhuma) + adicional (só INSS) → `BaseInss ≠ BaseRpps ≠ BaseIrrf ≠ TotalProventos`.
- **F-B4** — Dependentes onde a **base completa** vence a simplificada: queda monotônica do IRRF por dependente; caso N grande que zera o IRRF.
- **F-B5** — Arredondamento HALF-UP em meio-centavo (X,XX5): blindar contra banker's rounding e contra "arredonda por faixa" vs. "arredonda na soma" (`FaixaProgressiva.cs:46-49`).
- **F-B7** — `Verbas = []` → `ResultadoCalculoServidor` todo zero sem exceção; e servidor só com desconto manual (líquido clampado a 0).
- **F-B8** — Ponto de virada do mais vantajoso (`TabelaIrrf.cs:148 Math.Min`): rend. onde `INSS+deps+pensão == descontoSimplificado` (empate e ±0,01); provar que o INSS entra na base completa.

### IPTU (4)
- **IP-R4t** — Depreciação deve usar o **exercício** (não o relógio): reapuração dá o mesmo número; idade no limite de faixa; chave de depreciação ausente deveria falhar (não virar 1 silencioso).
- **IP-R5** — Fator de padrão/uso ausente vira 1 neutro (≠ `ObterZona` que lança): testar e idealmente falhar/registrar na memória de cálculo.
- **IP-R6** — Isenção 100% → devido 0; cota única (desconto à vista) num caso conhecido; ordem isenção→desconto com arredondamento exposto (ex. bruto 1.999, isenção 33%, desconto 7%).
- **IP-R7/R8** — Overflow decimal / valor venal próximo de `SemTeto` (deve casar faixa — ver IP-B1) → erro de domínio claro, não `OverflowException`; áreas zero (terreno 0 + construção 0) → invariante "imóvel tem ao menos terreno".

### ISS (5)
- **IS-4** — Precedência substituição > retenção-da-lei: item com `substituicao=true, retencaoObrigatoria=true` → Substituição.
- **IS-5** — `Substituir()` exclui da base (espelho de cancelada); idempotência de `Cancelar`/`Substituir`; transição cruzada cancelada↔substituída (decidir/barrar).
- **IS-7** — Seleção de tabela: 2 tabelas (202401/202501) apurar 202503 → usa 202501; competência anterior a qualquer tabela falha explicitamente; **desempate determinístico** quando há colisão de `VigenciaInicioAaaaMm` (hoje `OrderByDescending(...).First()` é não-determinístico).
- **IS-8** — Faixa 2%–5% (CF art. 156 §3º / LC 157): doc promete, validação aceita [0,100]. Alinhar doc×código; testar limites (permitir 0 para isenção, alertar fora de 2%–5%).
- **Geral ISS** — Zero testes para `NfseSincronizador`, isolamento de tenant no ISS, e agregação monetária de muitas notas (somar 333 × R$ 0,01 → total exato).

### ITBI + Contábil (6)
- **IT-B5** — Imunidades CF art. 156 §2º I (integralização de capital / fusão-cisão + preponderância 2 anos antes / 3 depois). **Dívida de feature**, não só teste — não modelável hoje.
- **IT-T5** — Triagem no limite exato (`declarado == limiteInferior`, hoje `<` estrito não dispara alerta).
- **C-T1** — Propagação ao balancete de estorno/anulação (depende de C-B2 corrigido).
- **C-T2** — `ResolverPara` só aceita `ValorDoFato`: roteiro com base derivada (% retenção INSS/IRRF na liquidação) arredonda sem desbalancear ΣD=ΣC (hoje lançaria `PartidaDobradaDesbalanceadaException`).
- **C-T3** — `AnularTotal` sobre empenho parcialmente liquidado: falha com mensagem clara (hoje `InvalidOperationException` genérica).
- **C-T4** — Restos a Pagar: inscrição + pagamento no exercício n+1; distinção RP processado / não-processado (`EVT-RP-INSC`).
- **C-B2'** (sub-risco de C-B2) — Idempotência da anulação: `MotorContabil` deduplica por `origemReferenciaId+evento.Id`; se o handler usar `EmpenhoId`, a 2ª anulação parcial é silenciosamente ignorada. Chave precisa incluir o id do ato. Teste: `Duas_anulacoes_parciais_geram_dois_lancamentos`.

---

## 4. Verificação contra o código (esta consolidação)

Reverifiquei os 5 críticos + 4 altos diretamente no fonte:
- **F-A1** ✔ `DadosCalculoServidor(RegimePrevidenciario Regime, int QuantidadeDependentes)` — sem campo de pensão; `ApurarDescontosLegais.cs:87` passa `0m`.
- **IS-10** ✔ `NfseSincronizador.cs:35-38` faz `continue` na dedup, sem aplicar transição de situação.
- **IP-R4** ✔ `CalculoValorVenal.cs:104` usa `DateTime.UtcNow.Year`; `FaixaIdade` retorna idade exata como string.
- **C-B2** ✔ Nenhum `INotificationHandler<EmpenhoAnulado>` nem `<LiquidacaoEstornada>` existe; `Empenho.cs:192` levanta o evento sem consumidor.
- **IT-B1** ✔ `RecalcularComArbitramento` chamado só com base/alíquotas; `TransmissaoImobiliaria` persiste apenas `AliquotaPercentual`+`ImpostoDevido`.
- **IT-B3** ✔ `ParametrosItbi(decimal PercentualIsencao, bool UsarAliquotaSfh)` — sem campo de parcela financiada; `CalculoItbi.cs:174` aplica à base inteira.

---

## 5. Plano priorizado de execução

**Bloco 1 — CRÍTICOS (antes de qualquer folha/lançamento real):**
1. **F-A1** Pensão alimentícia — adicionar `PensaoAlimenticia` a `DadosCalculoServidor` + `ServidorRegimeConsulta`; propagar ao `InsumosCalculoServidor`. Teste de integração em `ApurarDescontosLegaisTests`.
2. **IS-10** Eventos de cancelamento/substituição — separar "documento novo" de "evento sobre documento existente" no `NfseSincronizador`; aplicar `Cancelar()`/`Substituir()` idempotente na nota persistida. Teste do sincronizador (hoje inexistente).
3. **C-B2** Contra-lançamento de anulação/estorno — criar `ContabilizarAnulacaoEmpenhoHandler` (`INotificationHandler<EmpenhoAnulado>`) + levantar/consumir `LiquidacaoEstornada`; chave de idempotência pelo id do ato (C-B2'). Testes de balancete revertendo ao saldo pré-empenho.
4. **IP-R4** Depreciação — passar o **exercício** do fato gerador ao motor (remover `DateTime.UtcNow.Year`); semântica de FAIXA na chave de idade; fator de depreciação ausente deve falhar. Teste de reapuração reproduzível.
5. **IS-1** Município de incidência — propagar IBGE do tenant até `CalculadoraIss.Apurar` (ou filtrar a query) com regra por subitem do art. 3º.

**Bloco 2 — ALTOS:**
6. **IT-B1** persistir `ParametrosItbi` em `TransmissaoImobiliaria` e repassá-los no recálculo arbitrado.
7. **IT-B3** introduzir `ValorFinanciadoSfh` e aplicar SFH só na parcela financiada.
8. **IP-B1** `Publicar` deve exigir que a última faixa cubra o topo; `AliquotaPara(SemTeto)` deve casar a última faixa.
9. **IP-B2** invariante `TipoUso.Territorial ⇒ AreaConstruida == 0` em `CaracteristicasImovel.Criar`.
10. **F-A2** modelar elegibilidade IRRF do dependente e filtrar a contagem.

**Bloco 3 — MÉDIOS/BAIXOS:** F-A3, IP-B3, IS-2, IS-6, IT-B4, IS-3, IS-9, C-B6, F-B6 (confirmar).

**Bloco 4 — Testes (27):** §3, começando pelos de fronteira exata (F-B1, F-B2, IT-T5) e isolamento de tenant (IS-6/geral), que são exatamente os que o TCE-RS reconfere centavo a centavo.

**Conformidade (não-bug):** IRRF 2026 não semeada (`SemearTabelasFederais.cs:126`) — folha de 2026 herda a tabela de 2025; adicionar guarda que alerte quando a competência não tem tabela própria do ano (Lei 15.270/2025).

# Auditoria de Backend — Núcleo Fiscal/Administrativo

**Escopo:** Tributos, Financas, Administracao, RecursosHumanos, Patrimonio
**Método:** leitura empírica do código real (`src/Modules/*`), contagem de arquivos, entidades, handlers, endpoints e testes. Confrontado com o escopo legal do `CLAUDE.md` §4.
**Data:** 2026-06-22

---

## 0. Resumo quantitativo (medido, não estimado)

| Módulo | LoC (sem migr.) | LoC Domain | LoC App | Agregados/Entidades ricas | Handlers (cmd+qry) | Endpoints HTTP | Testes (Fact/Theory) |
|---|---|---|---|---|---|---|---|
| **Tributos** | 1.771 | 734 | 432 | 4 | 6 | **5** | ~5 |
| **Financas** | 3.272 | 1.688 | 1.231 | 6 | 22 | **4** | ~3 |
| **Administracao** | 5.021 | 2.127 | 2.016 | 3 raiz + 6 filhas | 33 | 17 | ~68 |
| **RecursosHumanos** | 3.681 | 1.486 | 1.383 | 3 raiz + filhas | 22 | 22 | ~60 |
| **Patrimonio** | 5.565 | 2.539 | 1.952 | 3 raiz + 8 filhas | 34 | 33 | ~71 |

**Achado transversal nº1 — assimetria invertida:** o `CLAUDE.md` declara Tributos como "padrão de referência a replicar" (§15, Fases 3-4). Empiricamente é o módulo **mais raso** em superfície de API (5 endpoints, 6 handlers) e em testes (~5). Os módulos posteriores (Patrimonio, Administracao, RH) são muito mais profundos. A "referência" foi superada pelos módulos que deveriam imitá-la.

**Achado transversal nº2 — gap endpoints×handlers em Financas:** 22 handlers existem, mas só **4 endpoints** estão mapeados (`POST empenhos`, `liquidar`, `pagar`, `GET empenho`). Dotações, Liquidações (consulta/estorno), Pagamentos (ordem/consulta), Restos a Pagar e Encerramento de Exercício têm caso de uso implementado mas **não expostos via HTTP**. O backend de Financas existe; a API está pela metade.

**Achado transversal nº3 — domínio rico, não anêmico (conforme):** todos os 5 módulos seguem o padrão do `CLAUDE.md` §7: construtor privado + factory estático, `private set`, invariantes no domínio, Value Objects (`ValorMonetario`, `Competencia`, `Placa`, `Renavam`, `Odometro`, `PontoPedido`, `ClassificacaoOrcamentaria`), `RaiseDomainEvent`, exceções de domínio. **Nenhum modelo anêmico detectado** nas raízes de agregado auditadas.

---

## 1. Tributos

### O que existe (real)
- **Agregados/Entidades:** `Contribuinte` (PF/PJ via factory), `Lancamento` (IPTU/ISS/ITBI como enum `TipoTributo`), `DividaAtiva`, `NotaFiscalServico`.
- **VOs:** `Competencia`, `ValorMonetario`.
- **`DividaAtiva` (rico):** `Inscrever` → `EmitirCda` → `Protestar` → `AjuizarExecucaoFiscal` → `FirmarParcelamento` → `Quitar`; constante `AnosPrescricao = 5` (CTN art. 174) + `EstaPrescrita(hoje)`. Máquina de estados completa (7 situações).
- **`Lancamento` (rico):** `Lancar`, `RegistrarPagamento`, `InscreverEmDividaAtiva(hoje)`, `Cancelar`.
- **Casos de uso (6 handlers):** `CadastrarContribuintePessoaFisica`, `LancarCredito`, `InscreverEmDividaAtiva`, `EmitirCda`, `QuitarDivida`, `ObterDividasAtivasDoContribuinte`.
- **Endpoints (5):** cadastrar PF, lançar, inscrever DA, emitir CDA, listar DA do contribuinte.
- **Integração NFS-e/ADN:** `AdnNfseGateway` (HTTP real + `AddStandardResilienceHandler`/Polly, ACL conforme §8) e `SimuladoNfseGateway`, alternados por `Nfse:Provider` em config. `NfseSincronizador` deduplica por chave. Cross-module **real**: publica `ReceitaArrecadadaIntegrationEvent` consumido por Financas.

### Profundidade vs escopo legal (§4: IPTU, ISS, ITBI, Taxas, Alvarás, NFS-e/ADN, Dívida Ativa/CDA/cobrança)
- ✅ **Dívida Ativa / CDA / prescrição / execução fiscal / parcelamento** — bem coberto e o mais maduro do módulo.
- ✅ **Ingestão NFS-e/ADN passiva** — arquitetura conforme §8 (não emite/assina).
- ⚠️ **IPTU/ISS/ITBI** — existem apenas como **valores de enum** `TipoTributo`. Não há **motor de cálculo** por tributo (base de cálculo, alíquota, planta de valores IPTU, cálculo ITBI sobre transmissão), nem cadastro imobiliário/econômico.
- ❌ **Taxas e Alvarás** — ausentes (nem enum, nem agregado, nem alvará/licença).
- ❌ **Lançamento em lote / carnê / cota única vs parcelado** — ausente.

### Real × stub
- **Real:** todo o domínio, handlers, persistência (EF config + repos + migration), gateway ADN.
- **Stub controlado:** `SimuladoNfseGateway` (explicitamente dev/demo, alternável por config — aceitável).

### Lacunas
- API mínima (5 endpoints): sem consulta de lançamentos, sem listagem geral de dívida, sem protesto/execução expostos.
- Sem motor de apuração tributária. Sem Taxas/Alvarás. Cobertura de teste ínfima (~5) para um módulo que processa crédito tributário.

---

## 2. Financas

### O que existe (real)
- **Agregados (6):** `DotacaoOrcamentaria`, `Empenho`, `Liquidacao`, `OrdemDePagamento`, `RestoAPagar`, `ReceitaArrecadada`.
- **VOs:** `ValorMonetario`, `ClassificacaoOrcamentaria`, `ContaBancaria`, `Credor`, `DocumentoComprobatorio`. Exceções de domínio dedicadas (`SaldoExceptions`).
- **`Empenho` (muito rico):** controle de saldos `SaldoEmpenhado/SaldoALiquidar/SaldoAPagar`, `Emitir`, `AnularParcial/Total`, `RegistrarLiquidacao`, `EstornarLiquidacao`, `RegistrarPagamento`, `InscreverEmRestosAPagar`, máquina de 8 situações coerente com Lei 4.320/64 (art. 58-63). Invariantes fortes (não anula com liquidação, não excede saldo).
- **`RestoAPagar` (rico):** Processado/Não-Processado (art. 36), `Inscrever/Liquidar/RegistrarPagamento/Cancelar`.
- **`DotacaoOrcamentaria`:** `Reforcar`, `AnularCredito`, `ReservarEmpenho`, `LiberarEmpenho`, `Bloquear`, `Encerrar`.
- **Casos de uso (22 handlers):** dotações (criar/consultar/movimentar/anular), empenhar/anular/obter, liquidar/estornar/consultar, pagar/ordem/consultar, RAP (consultar/movimentar), `EncerrarExercicio`, e `RegistrarReceitaArrecadadaHandler` (consumo cross-module **real** do evento de Tributos).

### Profundidade vs escopo legal (§4: Orçamento PPA/LDO/LOA, Empenho→Liquidação→Pagamento Lei 4.320, Contabilidade PCASP/MCASP, Restos a Pagar)
- ✅ **Ciclo Empenho→Liquidação→Pagamento (Lei 4.320)** — núcleo forte e correto.
- ✅ **Restos a Pagar** — bem modelado (Processado/Não-Processado, cancelamento).
- ✅ **Execução de receita** — `ReceitaArrecadada` alimentada por evento.
- ❌ **Orçamento PPA / LDO / LOA** — **ausente como agregado**. Há `DotacaoOrcamentaria` (peça de execução), mas não há peças de planejamento orçamentário (programas, metas, anexos da LOA).
- ❌ **Contabilidade PCASP/MCASP** — **ausente**. Não há plano de contas, lançamento contábil por partidas dobradas, nem escrituração. Esta é **a maior lacuna do núcleo contábil** — exatamente a preocupação central do dono (contabilidade + prestação de contas ao TCE).

### Real × stub / TODO
- **Real:** todo o domínio acima, EF (porém só configs de `Empenho` e `ReceitaArrecadada` — Dotacao/Liquidacao/OrdemPagamento/RestoAPagar **sem `IEntityTypeConfiguration` dedicada**, dependem de convenção).
- **TODOs explícitos (não-resolvidos, marcados `revisao-contabil`):**
  - `RestoAPagar.Cancelar` — regra de cancelamento por prazo (Decreto 93.872/86 / TCE-RS) parametrizável **não implementada**.
  - `EncerrarExercicio.cs:27` — apuração de superávit financeiro / RAP por fonte sinalizada como sensível e pendente.
  - `ClassificacaoOrcamentaria.cs:61` — validação dos dígitos da funcional-programática e tabela de fontes pendente.
  - `PagamentoEfetuadoIntegrationEvent` — campos do layout SIAPC/PAD do TCE-RS a confirmar.

### Lacunas (críticas para TCE)
- **Sem PCASP/MCASP** (escrituração contábil) — bloqueia geração fiel de SIAPC/PAD e MSC/SICONFI.
- **Sem PPA/LDO/LOA**.
- **Apenas 4 endpoints** para 22 handlers — API drasticamente subexposta.
- **Cobertura de teste ~3** — desproporcional ao risco (módulo do dinheiro público).
- Configurações EF incompletas para 4 dos 6 agregados.

---

## 3. Administracao

### O que existe (real)
- **Agregados:** `Licitacao` (com filhas `Lote`, `Proposta`, `Habilitacao`, `Recurso`), `Contrato` (com filhas `Aditivo`, `Apostilamento`, `Garantia`), `Fornecedor` (com `Sancao`).
- **`Licitacao` (425 LoC, muito rico):** `Abrir`, `AdicionarLote`, `RegistrarProposta`, `PublicarEditalPncp`, `JulgarPropostas`, `HabilitarLicitante`, `Homologar`, `DeclararFracassada/Deserta`, `Revogar`, `Anular`, `ValorAdjudicado`.
- **`Contrato` (464 LoC, muito rico):** `Celebrar`, `PublicarContratoPncp`, `ConfirmarDotacao(EmpenhoRef)`, `BloquearEficaciaPorDotacaoIndisponivel`, `IniciarExecucao`, `CelebrarAditivo`, `MarcarAditivoPublicado`, `Apostilar`, `PrestarGarantia`, `Encerrar`, `Rescindir`.
- **`Fornecedor`:** SICAF, sanções (`AplicarSancao`/`Reabilitar`), impedimento.
- **Casos de uso (33 handlers):** ciclo completo de licitação, contrato (com aditivo/apostila/garantia/rescisão/encerramento), fornecedor (cadastro/sanção/SICAF/reabilitação).
- **Endpoints (17):** boa cobertura.
- **Integração cross-module rica e real:** publica `ContratoAssinadoIntegrationEvent` (consumido por Patrimonio), `LicitacaoHomologadaIntegrationEvent`, `FornecedorSancionadoIntegrationEvent`; consome `EmpenhoEmitidoIntegrationEvent` (confirma dotação) e `DotacaoIndisponivelIntegrationEvent` (bloqueia eficácia). Saga orçamento↔contrato **implementada**.

### Profundidade vs escopo legal (§4: Compras, Licitações Lei 14.133/2021, Fornecedores, Contratos, Aditivos, PNCP)
- ✅ **Licitações Lei 14.133** — modalidades, julgamento, habilitação, recurso, homologação, fracasso/deserção/revogação/anulação.
- ✅ **Contratos + Aditivos + Apostilamento + Garantia** — completo.
- ✅ **Fornecedores + SICAF + sanções**.
- ⚠️ **PNCP** — existe `PublicarEditalNoPncp`/`PublicarContratoNoPncp` mas a publicação real é apenas estado interno + evento; **não há cliente HTTP do PNCP** (sem gateway de integração efetiva).
- ⚠️ **Receita Federal (CNPJ)** — `SimuladoReceitaCnpjGateway` (sempre retorna ativo); produção resiliente declarada como "será" (comentário), **não implementada**.
- ❌ **Compras / Catálogo / Requisição de compra / Pesquisa de preços** — não há agregado de "Compra" autônomo além da licitação.

### Real × stub
- **Real:** todo o domínio, handlers, EF (configs de Contrato/Fornecedor/Licitacao), migration, sagas cross-module.
- **Stub:** `SimuladoReceitaCnpjGateway` (sempre ativo); integração PNCP é evento interno, sem cliente HTTP.

### Lacunas
- Integração PNCP e Receita são placeholders (sem chamada externa real).
- Sem etapa de planejamento de compras / pesquisa de preços.

---

## 4. RecursosHumanos

### O que existe (real)
- **Agregados:** `Servidor` (com `Dependente`), `Cargo` (+ `PlanoDeCargos`), `FolhaDePagamento` (+ `EventoFolha`).
- **VOs:** `Competencia`, `Lotacao`, `Vencimento`, `BaseCalculo`, `Rubrica`, `LiquidoAPagar`.
- **`Servidor` (rico):** `Admitir`, `AdicionarDependente`, `RegistrarPosse`, `IniciarExercicio`, `ConcederEstabilidade`, `RegistrarAfastamento`, `RetornarDeAfastamento`, `Desligar` — ciclo estatutário completo.
- **`FolhaDePagamento` (rico):** `Abrir`, `AdicionarEvento`/`RemoverEvento`, `Calcular`, `Fechar`, `EfetuarPagamento`. `Calcular` aplica **abate-teto** (CF art. 37, XI) idempotente, totaliza proventos/descontos, garante líquido não-negativo.
- **Casos de uso (22 handlers):** cargos (criar/prover/vagar/extinguir/alterar vencimento), servidores (admitir/posse/exercício/estabilidade/afastamento/desligar/projetar), folha (abrir/evento/calcular/fechar/pagar/contracheque).
- **Endpoints (22):** boa cobertura.
- **Parametrização por tenant:** `IParametrosFolhaProvider` + `ParametrosFolha` (teto, rubricas) — conforme §7 (nada hardcoded). `IRubricaS1010Consulta` valida vigência de rubrica.

### Profundidade vs escopo legal (§4: Folha, Cargos públicos, Ponto Port. MTP 671/2021, eSocial)
- ✅ **Cargos públicos / plano de cargos / provimento / vacância**.
- ✅ **Servidor estatutário** (posse, exercício, estabilidade, afastamento, desligamento).
- ⚠️ **Folha** — estrutura e abate-teto presentes, mas o **cálculo de encargos (INSS/RPPS, IRRF, FGTS)** não é computado no domínio: depende de eventos/rubricas externos (S-1010). Não há motor de cálculo previdenciário/tributário próprio.
- ❌ **eSocial** — **ausente como integração real**. Há comentários referenciando S-1010/S-1200/S-1210/S-1299/DCTFWeb e menção a "via Outbox", mas **nenhuma geração de evento XML, nenhum lote, nenhum cliente de transmissão** (grep retornou zero implementações). Apenas a consulta `IRubricaS1010Consulta`.
- ❌ **Ponto (Port. MTP 671/2021)** — **totalmente ausente** (grep por Ponto/Frequencia/MarcacaoPonto/REP = zero).

### Real × stub
- **Real:** domínio, handlers, EF (3 configs + providers), migration, parametrização por tenant.
- **Ausente (não stub — simplesmente não existe):** eSocial (eventos/transmissão) e Ponto.

### Lacunas (relevantes para folha pública)
- Sem motor de cálculo de INSS/RPPS/IRRF (delegado a rubricas externas).
- eSocial e Ponto inexistentes — dois dos quatro itens de escopo do §4 não foram iniciados.

---

## 5. Patrimonio

### O que existe (real)
- **Agregados (3 raiz):** `BemPatrimonial` (+ `HistoricoDepreciacao`, `Reavaliacao`, `Impairment`, `MovimentacaoPatrimonial`), `ItemEstoque` (+ `Lote`, `MovimentoEstoque`, `Requisicao`), `Veiculo` (+ `Abastecimento`, `Licenciamento`, `ManutencaoOS`, `Motorista`, `Multa`).
- **VOs (ricos):** `NumeroTombamento`, `Depreciacao`, `Placa`, `Renavam`, `Odometro`, `Horimetro`, `PontoPedido`, `SaldoAlmoxarifado`, `ValorMonetario`.
- **`BemPatrimonial` (362 LoC, muito rico, MCASP real):** `Incorporar`, `ColocarEmCondicoesDeUso`, `Tombar`, `Depreciar(competencia)` com regra I-2/I-3/I-4 (terreno não deprecia, piso no residual, parcela mensal), `Reavaliar(laudo)`, `RegistrarImpairment(valor recuperável, laudo)`, `Transferir`, `Ceder`, `Baixar`, `Alienar` (leilão/avaliação prévia). Histórico de depreciação persistido.
- **`ItemEstoque`:** método de custeio **PEPS/Médio** real, ponto de pedido, valor realizável líquido, curva ABC.
- **`Veiculo`/Frota:** abastecimento, licenciamento, manutenção (OS), motorista, multas.
- **Casos de uso (34 handlers)** e **endpoints (33)** — a cobertura de API mais completa dos 5 módulos.
- **Integração cross-module real:** consome `ContratoAssinadoIntegrationEvent` (Administracao) para incorporar bem; publica `BemIncorporado/Depreciado/Reavaliado/Baixado` e `PontoPedidoAtingido`.

### Profundidade vs escopo legal (§4: Bens, Tombamento, Depreciação MCASP, Almoxarifado, Frota)
- ✅ **Bens + Tombamento + ciclo de vida** (incorporação→baixa/alienação).
- ✅ **Depreciação MCASP** — implementação real (não stub): cota mensal, residual, condições de uso, reavaliação e impairment com laudo.
- ✅ **Almoxarifado** — PEPS/Médio, ponto de pedido, curva ABC, requisição.
- ✅ **Frota** — completa.
- ⚠️ Único TODO menor: comentário sobre método de custeio I-3/I-4/I-5 (já implementado em essência).

### Real × stub
- **Real:** praticamente tudo. Este é, empiricamente, **o módulo mais profundo e completo** do conjunto (5.565 LoC, 34 handlers, 33 endpoints, ~71 testes) — apesar de o `CLAUDE.md` ainda listar Tributos como referência.

### Lacunas
- Mínimas. Falta a contrapartida contábil em Financas (a depreciação gera evento, mas não há razão contábil PCASP para recebê-lo).

---

## 6. Síntese — prioridades para o dono (foco contabilidade + TCE)

| # | Lacuna | Módulo | Severidade | Impacto TCE |
|---|---|---|---|---|
| 1 | **PCASP/MCASP (escrituração contábil por partidas dobradas) inexistente** | Financas | 🔴 Crítica | Sem isto não há SIAPC/PAD nem MSC fiéis |
| 2 | **PPA/LDO/LOA (planejamento orçamentário) inexistente** | Financas | 🔴 Crítica | Prestação de contas incompleta |
| 3 | **eSocial (eventos/transmissão) inexistente** | RH | 🟠 Alta | Obrigação acessória federal |
| 4 | **Ponto (MTP 671/2021) inexistente** | RH | 🟡 Média | Escopo §4 não iniciado |
| 5 | **API Financas subexposta (4 de 22 handlers)** | Financas | 🟠 Alta | Funcionalidade existe, inacessível |
| 6 | **Tributos: sem motor IPTU/ISS/ITBI, sem Taxas/Alvarás** | Tributos | 🟠 Alta | Apuração de receita própria limitada |
| 7 | **PNCP e Receita/CNPJ são placeholders** | Administracao | 🟡 Média | Transparência Lei 14.133 |
| 8 | **Cobertura de teste ínfima em Tributos (~5) e Financas (~3)** | Tributos/Financas | 🟠 Alta | Módulos do dinheiro público sem rede |

**Conclusão honesta:** o backend é **real, rico e não-anêmico** — não há "fachada". Patrimonio, Administracao e RecursosHumanos estão maduros. Porém o **coração da preocupação do dono (contabilidade pública e prestação de contas ao TCE) é justamente o mais incompleto**: falta a escrituração contábil PCASP/MCASP e o planejamento orçamentário (PPA/LDO/LOA) em Financas, e o módulo Tributos (declarado "referência") é na prática o mais raso. Os marcadores `TODO(revisao-contabil)` confirmam que o próprio time sinalizou os pontos contábeis sensíveis como pendentes.

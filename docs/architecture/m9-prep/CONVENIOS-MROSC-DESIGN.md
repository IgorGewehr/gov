# W9.6 — CONVÊNIOS + MROSC — DESIGN DE DOMÍNIO

> **Arquitetura de domínio** (convênios públicos). Módulo **NOVO** `Tensorroot.Gov.Modules.Convenios`
> (o maior do M9). Autoridade-base: `M9-BREAKDOWN.md` §W9.6. **READ-ONLY** (sem build/run).
> Disciplina CLAUDE.md: domínio rico, multi-tenant, cross-module **só via `*.Contracts`**,
> integrações atrás de **ACL+Polly+Outbox**, auditoria imutável, **prazos parametrizáveis por tenant**
> (nunca hardcoded — §7/§16). Data: 2026-06-23.

---

## 0. Decisão estrutural: DOIS fluxos SEPARADOS, mesmo módulo

O módulo `Convenios` hospeda **dois bounded-context-mates distintos**, com **state machines e prazos
próprios** — NÃO compartilham agregado nem máquina de estados:

| | (A) Convênios federais **RECEBIDOS** | (B) Parcerias-saída **OSC (MROSC)** |
|---|---|---|
| **Direção do dinheiro** | **Entra** (município é convenente/recebedor) | **Sai** (município repassa à OSC) |
| **Norma** | Dec. 11.531/2023 + Transferegov.br | Lei 13.019/2014 (MROSC) + Dec. 8.726/2016 |
| **Partícipe externo** | União/órgão concedente | OSC (Organização da Sociedade Civil) |
| **Seleção** | proposta/plano de trabalho aprovado pelo concedente | chamamento público **OU** dispensa/inexigibilidade |
| **Instrumento** | Convênio / Termo (plano de trabalho) | Termo de Colaboração / Fomento / Acordo de Cooperação |
| **Prazos de PC** | análise **60/180 d**; saneamento **45 d** | PC OSC **90+30 d**; análise **150 d**; saneamento **45 d** |
| **Vínculo orçamentário** | **receita** de convênio + **empenho da contrapartida** | **empenho→liquidação→pagamento** de cada repasse |
| **Gatilho de bloqueio** | inadimplência → não libera nova parcela | inadimplência (LRF/Lei 13.019 art. 48) → **bloqueia novos repasses** |

Raiz de namespace: `Tensorroot.Gov.Modules.Convenios.{Domain,Application,Infrastructure,Contracts}`.
Schema EF: `convenios`. DbContext: `ConveniosDbContext` (Outbox + interceptors de tenant/auditoria base).

---

## 1. Reuso confirmado no código atual (não duplicar)

- **Agregado + Outbox + DomainEvents**: padrão de `Administracao.Domain/Contratos/Contrato` (partial
  class, `AggregateRoot<TId>` + `IMustHaveTenant`, ctor privado, factory, `RaiseDomainEvent`, strong-id
  `readonly record struct`). Replicar fielmente.
- **State machine por transições**: padrão `Transparencia.Application/Esic/TransicoesPedidoSic` —
  um `Command`/`Handler`/`Validator` por transição; a **invariante de transição vive no agregado**
  (método `IniciarAtendimento()`, `Responder(...)`), o handler só carrega→transiciona→`SaveChanges`.
- **Calendário de dias úteis**: **`ICalendarioDiasUteis`** (porta de domínio) já existe em
  `Transparencia.Domain/Esic/ICalendarioDiasUteis.cs` — `SomarDiasUteis(DateOnly inicio, int diasUteis)`,
  determinístico, feriados por tenant. **W9.1 vai promovê-lo a serviço transversal compartilhado**
  (provável `SharedKernel`/`BuildingBlocks`); **este módulo CONSOME, não recria.** O prazo é sempre
  **CALCULADO** pelo agregado via essa porta, nunca digitado.
- **`TimeProvider`** (relógio) injetado como em `ResponderPedidoSicHandler` — para "data de hoje" nas
  transições; mantém o agregado determinístico/testável.
- **Execução orçamentária M2/M3 (Finanças)** via Contracts — eventos já existentes que vamos **consumir**:
  `DespesaEmpenhadaIntegrationEvent` (com `FuncaoSubfuncao`/`FonteRecurso`/`NaturezaDespesa`),
  `DespesaLiquidadaIntegrationEvent`, `PagamentoEfetuadoIntegrationEvent`. Para o lado (A) precisamos
  **emitir** ao Finanças um pedido de empenho da contrapartida e o reconhecimento de **receita de
  convênio** — ver §5 (contratos novos).
- **ACL+Polly**: padrão de gateway externo (W9.1 `IPncpGateway`) para o **`ITransferegovGateway`**
  (defere transmissão real → M10).
- **Inbox idempotente** (W9.7) + `EventId`: consumo de eventos de Finanças deduplicado por `EventId`
  por handler+tenant (padrão `ReceberContratoAssinadoHandler`).

---

# FLUXO (A) — CONVÊNIOS FEDERAIS RECEBIDOS

> Dec. 11.531/2023 + Transferegov.br. Município = **convenente/recebedor**. O dinheiro **ENTRA**.

## A.1 Resumo do fluxo

```
Proposta + Plano de Trabalho ──► Celebração ──► Execução físico-financeira
   (cadastro/import DTPAR)        (assinatura)    (liberação de parcelas + contrapartida + rendimentos)
                                                          │
                                       ┌──────────────────┴───────────────────┐
                                       ▼                                       ▼
                            PC parcial (contínua,                    PC final (após vigência)
                            por parcela/etapa)                              │
                                       └──────────────► Análise (60 d parcial / 180 d final) ──► Aprovada
                                                              │                                    │
                                                     pendências?                            (ou Aprovada-c/-ressalva)
                                                              ▼
                                                  Saneamento (45 d) ──► reanálise ──► Aprovada
                                                              │
                                                       não saneou ──► Rejeitada ──► TCE/Devolução (inadimplência)
```

## A.2 Agregado-raiz `ConvenioRecebido`

`Convenios.Domain/Recebidos/ConvenioRecebido.cs` — `AggregateRoot<ConvenioRecebidoId>`, `IMustHaveTenant`.

**Entidades/VOs do agregado:**
- `ConvenioRecebidoId` (strong-id).
- `OrgaoConcedente` (VO): `Cnpj`, `Nome`, `Esfera` (União/Estado), `NumeroConvenioTransferegov` (string,
  nullable até celebração), `SistemaOrigem` (enum: Transferegov, SICONV-legado, Outro).
- `PlanoDeTrabalho` (entidade filha): metas, etapas/fases (`EtapaPlanoTrabalho`: descrição, valor,
  início/fim previstos físico-financeiro), cronograma de desembolso (`ParcelaPrevista`).
- `Repasse` (entidade filha — liberações recebidas): `numeroOrdem`, `valor`, `dataPrevista`,
  `dataLiberada?`, `Situacao` (Prevista/Liberada/Bloqueada).
- `Contrapartida` (VO): `Modalidade` (Financeira/BensServicos), `ValorPactuado`, `PercentualMinimo`
  (parâmetro por tenant — **regra dos 20% NÃO é constante**), `ValorEmpenhado` (espelho do empenho).
- `RendimentoAplicacaoFinanceira` (entidade filha): saldos em conta vinculada rendem e **integram o
  objeto** (Dec. 11.531 art. — devolução/uso). `valor`, `data`, `aplicado?`.
- `PrestacaoContasConvenio` (entidade filha — ver A.4): parciais (N) + final (1).
- `Vigencia` (VO): `inicio`, `fim`, `prorrogacoes` (lista, com `prorrogacaoDeOficio?`).

**Situação (state machine A — `SituacaoConvenioRecebido`):**
`EmProposta(1) → Celebrado(2) → EmExecucao(3) → EmPrestacaoContas(4) → EmAnalise(5) → Aprovado(6, terminal) | AprovadoComRessalva(7, terminal) | Rejeitado(8, terminal) → Inadimplente(9, terminal-bloqueio)`.

**Transições (cada uma = Command/Handler/Validator, invariante no agregado):**
`RegistrarProposta` · `AprovarPlanoTrabalho` (concedente) · `Celebrar` · `RegistrarLiberacaoParcela` ·
`RegistrarContrapartidaEmpenhada` (reage a evento Finanças) · `RegistrarRendimento` ·
`AbrirPrestacaoParcial` · `EncerrarVigencia→AbrirPrestacaoFinal` · `SubmeterPrestacao` ·
`IniciarAnalise` · `RegistrarDiligencia/Pendencia` · `AbrirSaneamento` · `ConcluirAnalise`
(Aprovado/ComRessalva/Rejeitado) · `DeclararInadimplencia`.

## A.3 Invariantes (A) — numeradas

1. **(A-INV-1) Plano antes da celebração.** `Celebrar()` exige `PlanoDeTrabalho` aprovado e `Vigencia`
   definida; senão `InvalidConvenioStateException`.
2. **(A-INV-2) Contrapartida mínima.** `ValorPactuado` da contrapartida ≥ `PercentualMinimo × valorRepasseTotal`,
   onde `PercentualMinimo` é **VO parametrizável por tenant** (norma-fonte: Portaria Conjunta 33/2023 —
   **nunca hardcoded**; default carregado de config com `NormaFonte`).
3. **(A-INV-3) Soma das parcelas = valor global.** `Σ ParcelaPrevista.valor + Σ Contrapartida = ValorGlobal`
   do plano (tolerância de centavos definida em VO).
4. **(A-INV-4) Liberação exige etapa anterior comprovada.** `RegistrarLiberacaoParcela(n)` só se a parcela
   `n-1` tiver PC parcial **submetida** (ou regra de liberação por etapa do tenant); senão bloqueia.
5. **(A-INV-5) Empenho de contrapartida obrigatório antes de executar.** Não há `EmExecucao` sem
   `Contrapartida.ValorEmpenhado ≥ ValorPactuado` (reconhecido via evento `DespesaEmpenhadaIntegrationEvent`
   de Finanças, vinculado por `ConvenioRecebidoId`).
6. **(A-INV-6) Rendimento segue o objeto.** `RendimentoAplicacaoFinanceira` não pode ser sacado fora do
   objeto; ao concluir, saldo+rendimento não aplicado **deve** entrar na PC final como devolução.
7. **(A-INV-7) PC final só após vigência.** `AbrirPrestacaoFinal()` exige `Vigencia.fim ≤ hoje` (relógio
   `TimeProvider`) ou vigência encerrada por transição explícita.
8. **(A-INV-8) Prazo de análise calculado, não digitado.** `PrazoAnalise` derivado de `ICalendarioDiasUteis`
   a partir de `dataSubmissao` (parcial: `PrazoAnaliseParcial`; final: `PrazoAnaliseFinal`), em **dias**
   conforme VO do tenant (60/180 — ver A.5). Vencido sem decisão → evento `PrazoAnaliseConvenioVencido`.
9. **(A-INV-9) Saneamento limitado.** `AbrirSaneamento()` concede `PrazoSaneamento` (45 d, VO tenant) **uma
   única vez por PC** (ou N conforme parâmetro); expirado sem sanar → habilita `Rejeitar`.
10. **(A-INV-10) Inadimplência bloqueia liberação.** Em `Inadimplente`, qualquer `RegistrarLiberacaoParcela`
    lança exceção — espelha o gatilho LRF (consulta a inadimplência também publica evento p/ Finanças/PainelGestor).
11. **(A-INV-11) Terminalidade.** De estados terminais (`Aprovado/ComRessalva/Rejeitado`) não há transição,
    exceto reabertura auditada por `Rejeitado → EmSaneamento` se norma do tenant permitir recurso.
12. **(A-INV-12) Imutável e tenant-scoped.** Toda mutação gera trilha (AuditInterceptor); `TenantId` carimbado;
    nenhuma operação cross-tenant (Global Query Filter).

## A.4 PC (A): parcial contínua + final

`PrestacaoContasConvenio` (entidade filha; o agregado tem N parciais + 1 final):
- `Tipo` (Parcial/Final), `competencia/etapaRef`, `dataSubmissao?`, `Situacao`
  (`Pendente → Submetida → EmAnalise → EmSaneamento → Aprovada/Rejeitada`).
- Componentes: `relacaoPagamentos` (reconciliada com `PagamentoEfetuadoIntegrationEvent`),
  `execucaoFisica` (% por meta), `rendimentos`, `devolucaoSaldo` (na final), `conciliacaoBancaria`.
- **Parcial é contínua**: aberta a cada etapa/parcela; sua aprovação destrava `A-INV-4`.
- **Final**: fecha objeto; dispara cálculo de saldo a devolver + atualização **Selic a partir do 30º dia**
  (parâmetro/tabela por tenant — **não modelar Selic/"30º dia" como constante**; VO `IndiceDevolucao`).

## A.5 Prazos como VOs parametrizáveis por tenant (A)

VO `PrazoConvenioRecebido` (record) carregado de `ConveniosParametrosTenant` (config/tabela por tenant),
cada um com `Quantidade`, `Unidade` (DiasCorridos/DiasUteis), `NormaFonte` (string citável):

| VO | Default (norma-fonte) | Cálculo |
|---|---|---|
| `PrazoAnalisePcParcial` | 60 dias (Dec. 11.531/2023) | `ICalendarioDiasUteis.SomarDiasUteis(dataSubmissao, n)` quando DiasUteis |
| `PrazoAnalisePcFinal` | 180 dias | idem |
| `PrazoSaneamento` | 45 dias | idem, a partir da notificação de pendência |
| `PercentualContrapartidaMinimo` | 20% (Portaria Conjunta 33/2023) | razão sobre valor global |
| `IndiceDevolucaoSelic` | Selic a partir do 30º dia | tabela mensal por exercício, VO `IndiceDevolucao` |

Nenhum literal numérico no domínio: todos resolvidos via `IConveniosParametros` (porta) → tenant.

## A.6 Eventos de integração (A) — Contracts

**Publicados** (`Convenios.Contracts`):
- `ConvenioRecebidoCelebradoIntegrationEvent` (TenantId, ConvenioId, OrgaoConcedente, ValorGlobal,
  ValorContrapartida, VigenciaInicio/Fim) → Finanças (reconhece **receita de convênio** + reserva da
  contrapartida), Transparência, PainelGestor.
- `ContrapartidaConvenioAEmpenharIntegrationEvent` (ConvenioId, ValorPactuado, ClassificacaoSugerida)
  → Finanças (gera empenho da contrapartida).
- `PrestacaoContasConvenioSubmetidaIntegrationEvent` / `...AnaliseConcluidaIntegrationEvent`
  (resultado Aprovado/ComRessalva/Rejeitado) → Transparência (LAI), PainelGestor.
- `PrazoAnaliseConvenioVencidoIntegrationEvent` → PainelGestor (alerta).
- `ConvenioInadimplenteIntegrationEvent` (ConvenioId, motivo) → Finanças/PainelGestor (gatilho LRF).

**Consumidos** (de `Financas.Contracts`, via Inbox idempotente por `EventId`):
- `DespesaEmpenhadaIntegrationEvent` → `RegistrarContrapartidaEmpenhada` (satisfaz A-INV-5).
- `PagamentoEfetuadoIntegrationEvent` → reconcilia `relacaoPagamentos` da PC.
- (novo no Finanças, defere-se a W2 do M9 ou stub) `ReceitaConvenioReconhecidaIntegrationEvent`.

## A.7 O que difere no M10 (A)

- **Transmissão real ao Transferegov.br** (PC oficial): integração restrita a **órgãos integrados** →
  começar por **leitura de dados abertos DTPAR** (cadastro/situação) no M9; submissão real → M10 (creds/cert).
- `ITransferegovGateway` (ACL+Polly) testável contra **WireMock** no M9; produção (base real) defere M10.

---

# FLUXO (B) — PARCERIAS-SAÍDA OSC (MROSC)

> Lei 13.019/2014 + Dec. 8.726/2016. Município = **administração pública parceira**. O dinheiro **SAI**.

## B.1 Resumo do fluxo

```
Seleção ──► Instrumento ──► Plano de Trabalho ──► Repasses ──► PC da OSC ──► Análise (150 d) ──► Aprovada
 │            (Termo)         (metas/cronograma)   (parcelas)    (90+30 d)         │
 ├─ Chamamento público                                                    pendências?
 └─ Dispensa/Inexigibilidade (art. 30/31)                                         ▼
                                                                   Saneamento (45 d) ──► reanálise
                                                                                          │
                                              inadimplência (art.48/LRF) ──► BLOQUEIA NOVOS REPASSES
```

## B.2 Agregado-raiz `ParceriaOsc`

`Convenios.Domain/Mrosc/ParceriaOsc.cs` — `AggregateRoot<ParceriaOscId>`, `IMustHaveTenant`.

**Entidades/VOs do agregado:**
- `ParceriaOscId` (strong-id).
- `Osc` (VO): `Cnpj`, `RazaoSocial`, `naturezaJuridica`, **`ExperienciaPrevia`/`CapacidadeTecnica`**
  (requisitos de habilitação art. 33-34), `certidoesRegularidade` (lista com validade).
- `TipoInstrumento` (enum): `TermoColaboracao(1)` (proposta da Adm., art. 16),
  `TermoFomento(2)` (proposta da OSC, art. 17), `AcordoCooperacao(3)` (sem repasse $, art. 2º VIII-A).
- `FormaSelecao` (VO): `Chamamento(ProcessoId, edital)` **OU** `Dispensa(art.30, hipótese)` **OU**
  `Inexigibilidade(art.31, justificativa)` — mutuamente exclusivas, com fundamentação obrigatória.
- `PlanoDeTrabalhoOsc` (entidade filha): metas, indicadores, `cronogramaDesembolso`
  (`ParcelaRepasse`: valor, dataPrevista, condicionantes).
- `Repasse` (entidade filha — saídas): `numeroOrdem`, `valor`, `Situacao`
  (Prevista/Liberada/**Bloqueada**), vínculo a `empenhoId/liquidacaoId/pagamentoId` (espelho Finanças).
- `ContaVinculada` (VO) + `RendimentoAplicacao` (como em A.6).
- `PrestacaoContasOsc` (entidade filha — ver B.4).
- `Vigencia` (VO), `comissaoMonitoramentoAvaliacao` (ref), `gestorParceria` (servidor designado, art. 35 §4).

**Situação (state machine B — `SituacaoParceriaOsc`):**
`EmSelecao(1) → Celebrada(2) → EmExecucao(3) → EmPrestacaoContas(4) → EmAnalise(5) → Aprovada(6) | AprovadaComRessalva(7) | Rejeitada(8) → Inadimplente(9, bloqueio repasses)`.

> **Estado distinto de (A)**: a seleção pode ser `Chamamento` (com etapa de julgamento/recurso) ou
> `Dispensa/Inexigibilidade` (justificada, sem certame) — modelado em `FormaSelecao`, não como sub-estados.

**Transições:** `IniciarSelecao` · `PublicarChamamento`/`RegistrarDispensa`/`RegistrarInexigibilidade` ·
`AprovarPlanoTrabalho` · `Celebrar` · `LiberarRepasse` · `RegistrarMonitoramento` (visita/relatório
técnico art. 59) · `AbrirPrestacaoOsc` · `SubmeterPrestacao` · `IniciarAnalise` · `RegistrarPendencia` ·
`AbrirSaneamento` · `ConcluirAnalise` · `DeclararInadimplencia` · `BloquearRepasses`.

## B.3 Invariantes (B) — numeradas

1. **(B-INV-1) Seleção fundamentada.** `Celebrar()` exige `FormaSelecao` definida: se `Chamamento`,
   exige edital homologado; se `Dispensa/Inexigibilidade`, exige fundamento legal (art. 30/31) +
   justificativa textual; senão `InvalidParceriaStateException`.
2. **(B-INV-2) Acordo de Cooperação não repassa.** Se `TipoInstrumento = AcordoCooperacao`, o agregado
   **proíbe** `ParcelaRepasse`/`LiberarRepasse` (não há transferência de recursos — art. 2º VIII-A).
3. **(B-INV-3) Habilitação da OSC.** `Celebrar()` exige `Osc.certidoesRegularidade` válidas na data e
   requisitos art. 33-34 atendidos (capacidade técnica/experiência); certidão vencida bloqueia.
4. **(B-INV-4) Plano antes do repasse.** `LiberarRepasse(n)` exige `PlanoDeTrabalhoOsc` aprovado e
   `Vigencia` vigente; parcela `n` só após condicionantes da `n-1` (ex.: PC parcial aprovada).
5. **(B-INV-5) Repasse espelha execução orçamentária.** Um `Repasse.Liberada` só se houver
   `empenho→liquidação→pagamento` correspondentes (reconhecidos via eventos de Finanças por `ParceriaOscId`).
6. **(B-INV-6) PC da OSC tem prazo 90+30.** A OSC entrega a PC em `PrazoEntregaPcOsc` (90 d, VO tenant)
   após o fim da vigência, **prorrogável uma vez por `PrazoProrrogacaoPcOsc`** (30 d). Calculado por
   `ICalendarioDiasUteis`/`TimeProvider`; não entrega → `DeclararInadimplencia`.
7. **(B-INV-7) Análise em 150 d.** `PrazoAnalisePcOsc` (150 d, VO tenant) a partir do recebimento;
   vencido sem decisão → `PrazoAnaliseParceriaVencido` (alerta), não aprova por decurso.
8. **(B-INV-8) Saneamento 45 d.** `AbrirSaneamento()` concede `PrazoSaneamento` (45 d, VO tenant) uma vez;
   expirado → habilita `Rejeitar`.
9. **(B-INV-9) Inadimplência BLOQUEIA novos repasses (gatilho LRF).** `DeclararInadimplencia()` (PC não
   entregue/rejeitada, irregularidade, ou impedimento art. 39) → estado `Inadimplente` **e** marca todas
   as `ParcelaRepasse` futuras como `Bloqueada`; qualquer `LiberarRepasse` lança exceção. Publica
   `ParceriaOscInadimplenteIntegrationEvent` → Finanças/PainelGestor (espelha LRF art. 48).
10. **(B-INV-10) Comissão e gestor designados.** `Celebrar()` exige `gestorParceria` (art. 35 §4) e
    `comissaoMonitoramentoAvaliacao` referenciadas — pré-requisito de monitoramento.
11. **(B-INV-11) Terminalidade + auditoria + tenant.** Como A-INV-11/12 (estados terminais imutáveis,
    trilha imutável, `TenantId` carimbado, Global Query Filter).
12. **(B-INV-12) Devolução com Selic.** Saldo/rendimento não aplicado e glosas na PC final → devolução
    atualizada por `IndiceDevolucao` (VO tenant, Selic — **não constante**).

## B.4 PC (B): PC da OSC

`PrestacaoContasOsc` (entidade filha):
- `Situacao`: `Pendente → Recebida → EmAnalise → EmSaneamento → Aprovada/AprovadaComRessalva/Rejeitada`.
- Componentes (art. 64-72 + Dec. 8.726 art. 56-67): `relatorioExecucaoObjeto`,
  `relatorioExecucaoFinanceira` (quando exigido pela faixa/valor), `conciliacaoBancaria`,
  `comprovantesDespesa`, `glosas` (valores impugnados), `parecerTecnicoMonitoramento`.
- Prazos: entrega `90+30` (B-INV-6); análise `150` (B-INV-7); saneamento `45` (B-INV-8).

## B.5 Prazos como VOs parametrizáveis por tenant (B)

VO `PrazoParceriaOsc` (mesma estrutura de A.5: `Quantidade`, `Unidade`, `NormaFonte`), via `IConveniosParametros`:

| VO | Default (norma-fonte) | Cálculo |
|---|---|---|
| `PrazoEntregaPcOsc` | 90 dias (Lei 13.019 art. 69) | `SomarDiasUteis(fimVigencia, n)` |
| `PrazoProrrogacaoPcOsc` | 30 dias | +30 sobre o anterior, uma vez |
| `PrazoAnalisePcOsc` | 150 dias (art. 71) | `SomarDiasUteis(recebimento, n)` |
| `PrazoSaneamentoOsc` | 45 dias | a partir da notificação |
| `IndiceDevolucaoSelic` | Selic | tabela mensal/exercício (VO) |

## B.6 Eventos de integração (B) — Contracts

**Publicados** (`Convenios.Contracts`):
- `ParceriaOscCelebradaIntegrationEvent` (TenantId, ParceriaId, Osc, TipoInstrumento, ValorGlobal,
  Vigencia) → Finanças (reserva/empenho), Transparência, PainelGestor.
- `RepasseOscAEmpenharIntegrationEvent` (ParceriaId, ParcelaId, Valor, Classificacao) → Finanças
  (empenho→liquidação→pagamento do repasse).
- `PrestacaoContasOscAnaliseConcluidaIntegrationEvent` → Transparência/PainelGestor.
- `PrazoAnaliseParceriaVencidoIntegrationEvent` → PainelGestor.
- **`ParceriaOscInadimplenteIntegrationEvent`** (ParceriaId, motivo) → Finanças/PainelGestor
  (**gatilho LRF — bloqueia novos repasses**).

**Consumidos** (de `Financas.Contracts`, Inbox idempotente por `EventId`):
- `DespesaEmpenhadaIntegrationEvent` / `DespesaLiquidadaIntegrationEvent` / `PagamentoEfetuadoIntegrationEvent`
  → preenchem o espelho `empenhoId/liquidacaoId/pagamentoId` do `Repasse` (satisfaz B-INV-5).

## B.7 O que difere no M10 (B)

- MROSC tem **menos dependência de integração externa** que (A): chamamento, termos e PC são **internos**
  (não há "Transferegov de saída" obrigatório). M10 limita-se a: publicação oficial do chamamento/PC em
  **portal de transparência real** (já coberto por Transparência) e eventual **assinatura qualificada**
  do termo (A1/Key Vault, M2) em produção. **Nada de credencial bloqueia o domínio no M9.**

---

## 2. Diferenças-chave entre (A) e (B) (resumo de contraste)

| Aspecto | (A) Recebidos | (B) MROSC |
|---|---|---|
| Estado de seleção | `EmProposta` (aprovação do concedente) | `EmSelecao` (Chamamento **ou** Dispensa/Inexig.) |
| Direção orçamentária | **receita** + empenho da **contrapartida** | empenho→liquidação→**pagamento** do repasse |
| Prazo análise | 60 (parcial) / 180 (final) | 150 |
| Prazo PC do partícipe | — (PC é nossa, ao concedente) | **90+30** (OSC presta a nós) |
| Saneamento | 45 d | 45 d |
| Gatilho inadimplência | não libera nova parcela recebida | **bloqueia novos repasses (LRF art. 48)** |
| Integração externa M10 | **Transferegov.br** (alto) | portal/assinatura (baixo) |

---

## 3. Sequência de implementação (sub-workflows)

> Backend **edit-only + verify** (sem `dotnet`/`npm`; respeitar :5080). Frontend **`npx`** isolado.
> Cada sub-WF entrega spec BDD `.md` antes do código (CLAUDE.md §1/§12) + `rules/*.rules.md`.

**Pré-req transversal (vem de W9.1):** `ICalendarioDiasUteis` promovido a serviço compartilhado +
`IConveniosParametros` (porta de prazos/percentuais/Selic por tenant, com `NormaFonte`).

### Backend (edit-only)
1. **W9.6-B1 — Scaffolding do módulo.** Projetos `Convenios.{Domain,Application,Infrastructure,Contracts}`
   + `IModule` (DI/endpoints/`ConveniosDbContext` schema `convenios` + Outbox/interceptors) + registro no
   ApiHost (ativação por tenant). *Verify:* NetArchTest (isolamento) + provisionamento de schema.
2. **W9.6-B2 — VOs e parâmetros compartilhados.** `PrazoConvenioRecebido`, `PrazoParceriaOsc`,
   `IndiceDevolucao`, `OrgaoConcedente`, `Osc`, `FormaSelecao`, `Contrapartida`, `Vigencia` +
   `IConveniosParametros` (porta) + impl Infra (config/tabela por tenant). *Verify:* testes de VO/parâmetro.
3. **W9.6-B3 — Agregado (A) `ConvenioRecebido` + state machine + invariantes A-INV-1..12.** Entidades
   filhas (PlanoDeTrabalho/Repasse/Contrapartida/Rendimento/PrestacaoContasConvenio) + DomainEvents.
   *Verify:* testes de invariante/transição (prazos via calendário fake determinístico).
4. **W9.6-B4 — PC (A) parcial+final + cálculo de devolução/Selic + análise/saneamento.** *Verify:* testes
   de prazo (60/180/45) com `ICalendarioDiasUteis` fake + Selic 30º dia.
5. **W9.6-B5 — Agregado (B) `ParceriaOsc` + state machine + invariantes B-INV-1..12.** FormaSelecao
   (chamamento/dispensa/inexig.), tipos de termo, repasses, gestor/comissão. *Verify:* invariantes/transições.
6. **W9.6-B6 — PC (B) OSC 90+30 + análise 150 + saneamento 45 + gatilho inadimplência/bloqueio.**
   *Verify:* B-INV-6/7/8/9 (bloqueio de repasses).
7. **W9.6-B7 — Application handlers + endpoints (Commands/Queries) dos dois fluxos.** Pipeline
   Validation→Logging→UoW→Idempotency. *Verify:* handler tests.
8. **W9.6-B8 — Integração Contracts (publicar) + Inbox (consumir Finanças).** Eventos §A.6/§B.6 +
   handlers ACL idempotentes por `EventId`. *Verify:* Outbox dispatch + Inbox dedupe (reusar
   `OutboxDispatchIsolationTests`).
9. **W9.6-B9 — ACL `ITransferegovGateway` (A) atrás de Polly + WireMock** (leitura DTPAR; transmissão real
   defere M10). *Verify:* contrato testado contra WireMock (harness W9.8).
10. **W9.6-B10 — Migrations + `EntityConfiguration`s (Fluent) + `rules/*.rules.md`** dos dois agregados.
    *Verify:* migration aplica em schema isolado + WORM/hash-chain herdados.

### Frontend (`npx`, isolado de :5080)
11. **W9.6-F1 — Telas (A):** lista/ficha de convênio recebido, plano de trabalho, parcelas/contrapartida,
    PC parcial/final, painel de prazos. gov.br DS + WCAG 2.1 AA + `<Can>` gating + `.a11y.test.tsx`.
12. **W9.6-F2 — Telas (B):** chamamento/seleção, termo (colaboração/fomento/cooperação), repasses,
    PC da OSC, alertas de inadimplência/bloqueio. Mesmo padrão.
13. **W9.6-F3 — E2E + axe** dos dois fluxos (estende harness W9.8).

---

## 4. Resumo executivo

- **Dois agregados-raiz, dois fluxos, duas máquinas de estados, prazos próprios** — `ConvenioRecebido`
  (entra, Dec. 11.531/Transferegov, análise 60/180 + saneamento 45, vínculo a **receita+contrapartida**)
  e `ParceriaOsc` (sai, MROSC Lei 13.019, PC OSC 90+30 + análise 150 + saneamento 45, vínculo a
  **empenho→liquidação→pagamento**, **inadimplência bloqueia novos repasses**).
- **Entidades-chave:** `ConvenioRecebido`/`ParceriaOsc` (raízes); filhas `PlanoDeTrabalho(Osc)`,
  `Repasse`, `Contrapartida`, `RendimentoAplicacao`, `PrestacaoContas(Convenio/Osc)`; VOs
  `OrgaoConcedente`, `Osc`, `FormaSelecao`, `Vigencia`, `IndiceDevolucao`, `Prazo*` (parametrizáveis por
  tenant com `NormaFonte`), porta `IConveniosParametros`, ACL `ITransferegovGateway`.
- **Reuso:** padrão agregado+Outbox de `Contrato`; state machine de `PedidoSic`; **`ICalendarioDiasUteis`**
  (de W9.1, compartilhado) + `TimeProvider` para todos os prazos (calculados, nunca digitados); execução
  orçamentária M2/M3 via `Financas.Contracts` (consumo) + novos eventos de saída; ACL+Polly+Inbox.
- **Nada hardcoded:** 20%, Selic/30º dia, 60/180/90+30/150/45 — **todos VO por tenant** com norma-fonte.
- **Fronteira M10:** transmissão real ao **Transferegov.br** (A — começar por leitura DTPAR no M9); MROSC
  (B) é praticamente todo M9 (só portal/assinatura real tocam produção).
- **Sequência:** B1→B2→B3→B4→B5→B6→B7→B8→B9→B10 (backend edit-only+verify) → F1→F2→F3 (frontend `npx`).

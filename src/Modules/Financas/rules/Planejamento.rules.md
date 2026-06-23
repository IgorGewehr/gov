# Planejamento Orçamentário (PPA / LDO / LOA + Créditos Adicionais) — Rules-as-Code

Fecha o GAP do módulo Finanças: além da execução (Dotação→Empenho→Liquidação→Pagamento), o
planejamento orçamentário com a cadeia de compatibilidade **LOA ⊆ LDO ⊆ PPA** provada e o vínculo
em que a `DotacaoOrcamentaria` **nasce da LOA** (dotação inicial = despesa fixada). Spec de
arquitetura em `docs/architecture/ppa-ldo-loa/DESIGN.md` + `pesquisa-*.md`.
Fontes legais: CF/88 arts. 165/166/167 + ADCT 35; Lei 4.320/1964 (arts. 2-15, 40-46); LC 101/2000 (LRF) arts. 4-5.

## Linguagem ubíqua

- **PlanoPlurianual (PPA)** — plano de 4 anos: `Programa → AcaoPpa → MetaAcao` (física/financeira, regionalização). Agregado único.
- **LeiDiretrizes (LDO)** — o elo anual: `PrioridadeLdo` (seleção de ações do PPA), `MetaFiscal` (triênio LRF), `AnexoLdo` (AMF/ARF).
- **LeiOrcamentariaAnual (LOA)** — estima receita (`ReceitaPrevista` por natureza) e fixa despesa (`ItemDespesaFixada` ≡ QDD).
- **CreditoAdicional** — ato que ALTERA a LOA: Suplementar / Especial / Extraordinário (Lei 4.320 arts. 40-46).

## Invariantes (domínio)

1. PPA: `AnoFim = AnoInicio + 3`; meta com `Ano ∈ [AnoInicio, AnoFim]`; só PPA Vigente é referenciável.
2. LDO: PPA vigente cobre o exercício; `PrioridadeLdo` só de ação vigente no PPA (CF 165 §2º); meta fiscal cobre exercício + 2 (LRF art. 4º §1º).
3. LOA ⊆ LDO ⊆ PPA: todo `ItemDespesaFixada.AcaoPpaId` existe/vigente no PPA **e** priorizado na LDO (CF 167, I/§1º).
4. Equilíbrio: Σ `ReceitaPrevista` ≥ Σ `ItemDespesaFixada` (Lei 4.320 art. 2º) — validado em `Aprovar`.
5. Exercício: LOA == LDO == dentro do quadriênio do PPA.
6. Vínculo: ao entrar em execução, cada item sem dotação levanta `ItemLoaEntrouEmExecucao` → 1 `DotacaoOrcamentaria` (idempotente por item).
7. Crédito: suplementar exige dotação alvo; soma de suplementações por decreto ≤ `LimiteSuplementacaoPercentual` (art. 7º / CF 167, V); especial/extraordinário criam dotação nova; extraordinário dispensa autorizador/fonte (CF 167 §3º).

## Máquinas de estado

- PPA: `Elaboracao → EmTramitacao → Vigente → (Encerrado | Revisado)`.
- LDO: `Elaboracao → EmTramitacao → Vigente → Encerrada`.
- LOA: `ProjetoLei → EmTramitacao → Aprovada → EmExecucao → Encerrada`.
- Crédito: `Registrado → Aberto`.

## RBAC

- Consultas → `financas.ver`.
- Mutações de planejamento (PPA/LDO/LOA + créditos) → `financas.planejar` (segregado do `financas.gerenciar` da execução — segregação de funções, controle interno/TCE).

## Cenários (BDD)

- **Dado** um PPA vigente com ação A e uma LDO vigente que prioriza A, **quando** a LOA fixa despesa na ação A com receita ≥ despesa, **então** `AprovarLoa` sucede.
- **Dado** uma LOA cuja ação fixada não foi priorizada na LDO, **quando** `AprovarLoa`, **então** lança `IncompatibilidadeOrcamentariaException` listando o item infrator.
- **Dado** uma LOA aprovada, **quando** `ColocarLoaEmExecucao`, **então** cada item gera uma dotação com `ValorDotadoInicial` = despesa fixada e origem rastreável (LOA/item/ação).
- **Dado** uma LOA em execução, **quando** `AbrirCreditoAdicional` suplementar por anulação, **então** anula a dotação origem e reforça a dotação alvo (roteiro contábil EVT-DOT), auditado.

<!-- manifest
commands: CriarPpa, AdicionarPrograma, AdicionarAcao, DefinirMeta, TramitarPpa, VigorarPpa, CriarLdo, PriorizarAcao, DefinirMetaFiscal, AnexarLdo, TramitarLdo, VigorarLdo, CriarLoa, PreverReceita, FixarDespesa, TramitarLoa, AprovarLoa, ColocarLoaEmExecucao, EncerrarLoa, AbrirCreditoAdicional
queries: ConsultarPpa, ConsultarLdo, ConsultarLoa, ListarItensQdd, ConsultarCompatibilidade
domainEvents: PpaVigente, LdoVigente, LoaAprovada, ItemLoaEntrouEmExecucao, CreditoAdicionalAberto
integrationEventsPublished: 
integrationEventsConsumed: 
-->

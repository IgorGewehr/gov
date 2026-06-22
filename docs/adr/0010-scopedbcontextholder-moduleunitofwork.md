# ADR-0010 — ScopeDbContextHolder + ModuleUnitOfWork (vários DbContexts num escopo)

- **Status:** Aceito (corrige bug que descartava gravações)
- **Data:** 2026-06-22
- **Código:** `BuildingBlocks.Infrastructure/{ScopeDbContextHolder,ModuleUnitOfWork,ModuleDbContext}.cs`

## Contexto

A arquitetura tem **um `DbContext` por módulo** (ADR-0001/0005), e um pipeline MediatR com um
behavior de **Transaction/UnitOfWork** que chama `IUnitOfWork.SaveChanges` ao fim do command.
Numa única requisição, mais de um `ModuleDbContext` pode ser construído no mesmo escopo de DI
(ex.: um handler de um módulo que reage a evento e toca o contexto de outro contexto registrado).

Como **todos os módulos registram `IUnitOfWork`** no mesmo contêiner, a resolução por DI fazia
**a última registração vencer** — o `SaveChanges` do behavior atingia um `DbContext` que **não
era** o que recebeu as mutações. Resultado: **gravações silenciosamente descartadas** — bug
crítico num sistema que move dinheiro público.

## Decisão

Introduzir um **holder por escopo** que rastreia o `DbContext` de módulo ativo:

- `ScopeDbContextHolder` (scoped) guarda o `ModuleDbContext` **atual** do escopo. É populado
  **pelo próprio contexto ao ser construído** (`Definir(contexto)`), não por convenção de DI.
- `ModuleUnitOfWork` (a `IUnitOfWork` compartilhada) faz `SaveChanges` no **contexto correto**
  apontado pelo holder, eliminando a ambiguidade de "qual `IUnitOfWork` foi resolvida".
- Mantém um `DbContext` por módulo e o behavior único de UnitOfWork — sem fragmentar o pipeline.

## Alternativas consideradas

- **Registrar `IUnitOfWork` com chave por módulo (keyed services) e resolver pelo módulo do
  handler:** exigiria o behavior saber, em tempo de pipeline, qual módulo/contexto é o alvo —
  acoplamento e fragilidade. Rejeitado.
- **Um único `DbContext` global:** quebra o isolamento por módulo (ADR-0001) e o schema por
  módulo. Rejeitado.
- **`SaveChanges` em todos os contextos resolvidos no escopo:** dispararia gravações e
  interceptors em contextos não tocados; mais I/O e risco de efeitos colaterais. Rejeitado em
  favor de rastrear o contexto **efetivamente** ativo.

## Consequências

- ➕ Corrige o bug de **gravação perdida** — o `SaveChanges` sempre atinge o contexto que recebeu
  as mutações; preserva isolamento por módulo + um contexto por módulo.
- ➕ O contrato é simples e auto-descritivo (o contexto se anuncia ao holder ao nascer).
- ➖ **Estado scoped implícito:** depende de o `ModuleDbContext` chamar `Definir(...)` no ctor —
  um contexto novo que esqueça isso reintroduz a ambiguidade. Mitigar pela base `ModuleDbContext`
  fazer o registro e por teste de arquitetura/integração.
- ➖ Caso **patológico** de dois módulos mutados no **mesmo** escopo: o holder aponta para um
  "atual" — fluxos que exijam gravar dois contextos no mesmo command devem ser explícitos
  (transação distribuída/Outbox), não confiar no holder. Documentar como limite.
- 🔗 Trabalha junto com o **Outbox** (ADR-0011): mutação de estado + integration events na **mesma**
  unidade de trabalho.

# ADR-0012 — Fábrica Rules-as-Code (*.rules.md + manifesto) + Fitness Functions (NetArchTest)

- **Status:** Aceito
- **Data:** 2026-06-22
- **Código:** `src/Modules/*/rules/*.rules.md`, `tests/Tensorroot.Gov.ArchitectureTests/`

## Contexto

O domínio é **rico e regulado** (Lei 4.320, PCASP/MCASP, Lei 14.133, eSocial, prazos legais),
sob princípio **Spec-Driven/BDD-first**: nenhuma feature sem `Given/When/Then` aprovado **antes**
do código (CLAUDE.md §1/§12). Sem mecanismo de *enforcement*, duas coisas erodem com o tempo:

1. As **regras de negócio** viram conhecimento tácito disperso no código, divergindo das fontes
   oficiais e das specs.
2. As **fronteiras arquiteturais** (isolamento de módulo, `Domain` não depende de
   `Infrastructure`, cross-module só por `*.Contracts`) degradam silenciosamente sob pressão de
   prazo ("só dessa vez").

## Decisão

Combinar **Rules-as-Code** (regras como artefato versionado) com **Fitness Functions** (regras de
arquitetura como teste que falha o build):

- **Rules-as-Code:** cada agregado/contexto tem um `*.rules.md` (manifesto): linguagem ubíqua,
  invariantes numeradas e cenários BDD, **referenciando a spec oficial** (ex.:
  `Financas/rules/Contabilidade.rules.md` → `docs/architecture/specs-oficiais/pcasp-*.md`). É a
  **fonte** que dirige a implementação do domínio; layouts/códigos não confirmados ficam marcados
  (`// TODO(validar-leiaute-oficial)` / `[a confirmar]`), nunca inventados (§16).
- **Fitness Functions (NetArchTest):** testes de arquitetura bloqueiam, no build, (a)
  `Domain → Infrastructure`; (b) módulo → interno de outro módulo; (c) referência cross-module
  fora de `*.Contracts`. Acoplamento proibido **quebra o CI**, não depende de revisão humana.

## Alternativas consideradas

- **Regras só em código + documentação separada (wiki/Confluence):** a doc diverge do código sem
  aviso. O `.rules.md` co-localizado no módulo, sob revisão de PR, reduz a deriva. Preferido.
- **Motor de regras em runtime (ex. RulesEngine/DSL executável):** tiraria invariantes do domínio
  rico (anti-modelo-anêmico, §7) e adicionaria indireção. Aqui "Rules-as-Code" é **spec que guia o
  domínio**, não interpretador em produção. (As regras *parametrizáveis por tenant* — eventos
  contábeis, prazos — são dados configuráveis, não DSL.)
- **Revisão humana das fronteiras (só code review):** não escala e falha sob pressão. NetArchTest
  torna a regra **executável**. Preferido.

## Consequências

- ➕ Regras de negócio **rastreáveis à fonte oficial** e revisadas como código; specs BDD viram
  testes; o domínio nasce dirigido pela regra, não o contrário.
- ➕ Fronteiras arquiteturais **garantidas pelo build** — o isolamento de módulo deixa de ser
  disciplina e vira invariante mecânica (sustenta ADR-0001).
- ➖ **Disciplina de manutenção:** um `*.rules.md` desatualizado é pior que nenhum — exige que o PR
  atualize a regra junto com o código (item de checklist).
- ➖ NetArchTest cobre **dependências de tipo/assembly**, não semântica — não pega "vazou regra de
  negócio para a camada errada" se as referências forem legais. Complementar com testes de domínio.
- 🔗 Os `*.rules.md` alimentam tanto a implementação quanto a documentação viva do bounded context
  (README do módulo).

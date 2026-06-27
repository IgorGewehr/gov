# ADR-0001 — Monolito Modular + Clean Architecture + DDD Tático

- **Status:** Aceito
- **Data:** 2026-06-20

## Contexto

O Tensorroot.Gov cobre 11 domínios públicos heterogêneos (Compras, Finanças, Tributos,
RH, Patrimônio, Protocolo, Saúde, Educação, Assistência Social, Legislativo, Transparência).
Os clientes são prefeituras e câmaras, muitas de pequeno porte, com necessidade de
**deploy simples** e **baixo custo operacional**. Há forte exigência de **transações consistentes**
(dinheiro público) e **isolamento de domínio**. Microserviços trariam complexidade operacional,
latência e consistência eventual desnecessárias neste estágio.

## Decisão

Adotar **Monolito Modular** com **Clean Architecture** e **DDD Tático**:

- Cada Bounded Context é dividido em **Domain → Application → Infrastructure**, com um projeto
  **Contracts** público como **única** superfície cross-module.
- Comunicação intra-processo via **MediatR** (Domain Events in-process) e **Integration Events**
  via **Outbox Pattern** (consistência transacional com o estado).
- Acoplamento bloqueado por **fitness functions** (NetArchTest).
- Um único deployable (`ApiHost`) + Workers de processo (ex.: `NfseSync`).

## Consequências

- ➕ Simplicidade operacional, **latência zero** entre módulos, transações locais ACID.
- ➕ Caminho de evolução: um módulo pode ser **extraído para microserviço** futuramente, pois já
  só se comunica via `Contracts` + eventos.
- ➖ Exige **disciplina arquitetural** — mitigada por testes de arquitetura automatizados.
- ➖ Escala vertical do processo; aceitável para o perfil de carga municipal.

---
modulo: RecursosHumanos
agregado: Relatorios (read-side — sem agregado novo)
contexto: RecursosHumanos (relatorios gerenciais da folha — projecoes de leitura)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["LC 101/2000 (LRF) art. 19/20 (limites de despesa com pessoal)", "CF/1988 art. 169 (despesa com pessoal)", "CF/1988 art. 37, XI (teto remuneratorio)", "EC 103/2019 (regimes RPPS/RGPS)", "eSocial — S-1020 (lotacao tributaria)", "TCE-RS SIAPC/PAD (demonstrativo de pessoal — remessa formatada no modulo Transparencia/M4)"]
---

# Relatorios gerenciais da folha — Regras-as-Code (Rules-as-Code)

> Suite **read-side** (sub-onda 3a — ONDA3-DESIGN §4.1) sobre os agregados ja existentes
> (`FolhaDePagamento`/`EventoFolha`/`Servidor`/`Cargo`): **folha por secretaria/UO e por fonte**,
> **evolucao mensal da despesa de pessoal**, **mapa de cargos** (ocupados x vagos) e **demonstrativo de
> pessoal para o TCE**. NAO cria dominio novo — sao projecoes (queries) tenant-scoped, somente leitura.
> Este arquivo e normativo e versionado; o codigo (queries/handlers + read provider + endpoints) e
> consequencia dele.

---

## 1. Linguagem ubiqua

- **Fonte:** regime previdenciario (`RegimePrevidenciario`) — **RPPS** (efetivo/regime proprio do ente)
  vs **RGPS** (INSS); eixo que separa a despesa previdenciaria patronal (EC 103/2019).
- **Secretaria/UO:** unidade organizacional = `Lotacao.DenominacaoUnidade` do `Cargo` do servidor
  (mapeada a S-1020). Servidor sem cargo resolvido cai em `(Sem lotacao)` — nunca descartado.
- **Despesa de pessoal:** proventos (remuneracao bruta) da **folha Mensal** (13o/ferias/rescisao tem
  folha propria e nao compoem a serie mensal — design §1.1).
- **Demonstrativo TCE:** visao gerencial (totais por fonte e por UO + contribuicao previdenciaria do
  segurado). A **remessa formatada** SIAPC/PAD e do modulo Transparencia/M4.

## 2. Regras (read-side)

- **R1.** Todas as consultas sao **tenant-scoped** (Global Query Filter) e **read-only** (sem mutacao).
- **R2.** As projecoes que dependem de competencia/eventos (propriedade convertida + colecao owned) sao
  agregadas em memoria sobre o conjunto ja filtrado por tenant (mesmo padrao do `TabelasLegaisProvider`).
- **R3.** Folha por secretaria/UO e demonstrativo TCE projetam a **folha Mensal** da competencia; quando
  inexistente, retornam **nulo** (404 no endpoint) — jamais um zero silencioso.
- **R4.** Os codigos de rubrica previdenciaria (INSS/RPPS e variantes do 13o) usados no demonstrativo
  TCE vem da **configuracao do tenant** (`ParametrosFolha`) — nunca hardcoded (CLAUDE.md §7).
- **R5.** Liquido por grupo respeita o **piso zero** (irredutibilidade — espelha `LiquidoAPagar`).

## 3. Endpoints (`/api/recursoshumanos/relatorios`) — todos `recursoshumanos.ver`

- `GET /folha-por-secretaria?ano=&mes=` — folha por secretaria/UO e por fonte.
- `GET /evolucao-despesa?anoDe=&mesDe=&anoAte=&mesAte=` — serie mensal da despesa de pessoal (LRF).
- `GET /mapa-cargos` — vagas autorizadas x ocupadas x livres, por cargo e totais por tipo.
- `GET /demonstrativo-tce?ano=&mes=` — demonstrativo de pessoal (fonte/UO + contribuicao previdenciaria).

---

## Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versão inicial — relatorios gerenciais da folha (sub-onda 3a, ONDA3-DESIGN §4.1): folha por secretaria/UO e fonte, evolucao da despesa de pessoal, mapa de cargos, demonstrativo TCE. Read-side, sem dominio novo. |

<!-- manifest
commands: 
queries: ObterFolhaPorSecretaria, ObterEvolucaoDespesa, ObterMapaCargos, ObterDemonstrativoTce
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->

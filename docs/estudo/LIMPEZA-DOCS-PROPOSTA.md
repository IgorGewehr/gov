# Limpeza de Documentação — Proposta (revisão 2026-06-25)

> Revisão feita durante o run autônomo, a pedido do dono ("checar docs, ver obsoletos que já podemos excluir").
> **Resultado: NADA foi deletado.** Os candidatos óbvios estão referenciados por docs preservados — apagar agora quebraria links. A limpeza fica como um **passo coordenado para o fim do plano** (ver memória `limpeza-final-producao` + [[definicao-pronto-m10]] W10.5). Este doc é o roteiro desse passo.

Inventário: **194 arquivos .md, ~3,1 MB** em `docs/`.

## ✅ PRESERVAR (produto — nunca deletar)
- `docs/adr/` (22 ADRs) — decisões com trade-offs.
- `docs/architecture/specs-oficiais/` + os **`*-DESIGN.md` / `*-BREAKDOWN.md` / `*-SPEC.md`** (15) — design vivo, ainda consultado nas ondas/M9.
- `docs/RUNBOOK.md`, `docs/MAPA-TOPICO-CODIGO.md`, `docs/roadmap_team.html`, `docs/progresso/` (painel), `docs/design-system/`.
- Planos **ativos**: `estudo/completude-modulos/PLANO-PROFUNDIDADE.md`, `estudo/robustez-fundacao/PLANO-ROBUSTEZ.md`, `estudo/qualidade-rh/RH-PLANO.md`, `architecture/m9-prep/M9-BREAKDOWN.md`, `architecture/profundidade/ONDA*-DESIGN.md` — em uso AGORA.

## 🗑️ CANDIDATOS A REMOVER — **no fim, de forma coordenada** (NÃO agora)
| Alvo | Por que obsoleto | ⚠️ Entrelaçamento (resolver junto) |
|---|---|---|
| `docs/diagnostico/ESTADO-ATUAL.md`, `GAP-E-ROADMAP.md` | Fotos PRÉ-M0 (marcadas histórico); estado real = `progresso.json` | **Referenciados** por `roadmap_team.html`, `progresso.json`, `planejamento/partes/*`, `prova-de-conceito/pesquisa-requisitos-legislativo.md` → atualizar/remover esses links |
| `docs/diagnostico/partes/auditoria-*.md` | Auditorias empíricas pré-M0 (datadas 2026-06-22) | idem (sintetizadas no ESTADO-ATUAL) |
| `docs/architecture/*-prep/pesquisa-*.md` + `verificacao-*.md` (~50) | Pesquisa intermediária já consolidada nos `*-DESIGN.md` | **Citados** pelos próprios `*-DESIGN.md` → remover as citações ou manter como apêndice |
| `estudo/` auditorias one-off (`qualidade-*/auditoria-*`, `seguranca/ataque-*`+`verificacao-*`, `AUDITORIA-*`, `DIAGNOSTICO-VS-REAL`, `DIRECIONAMENTO`, `MAPA-SISTEMICO`, `dto-*`) | Achados já corrigidos no código + commits | baixo; conferir se algum vira base de doc final |
| `docs/diagnostico/REQUISITOS-GOVTECH.md` + `partes/govtech-*.md` | Requisitos pré-M0 | **manter por ora** — pode virar base do doc de requisitos final |

## Recomendação
1. Fazer a limpeza **junto com o W10.5 (limpeza de produção)**, no fim — quando os planos ativos (Onda/M9) já tiverem virado código e os audits estiverem todos fechados.
2. Nesse passo: deletar os alvos acima **E** atualizar/remover as referências (links em `roadmap_team.html`, `progresso.json`, planejamento) na MESMA passada → zero link quebrado.
3. Confirmar com o dono a lista final antes de apagar (regra da memória).

**Estimativa de redução:** ~70-90 arquivos .md (fotos pré-M0 + pesquisa/verificação + audits one-off) → docs/ enxuto, só produto + ADRs + specs + DESIGNs finais.

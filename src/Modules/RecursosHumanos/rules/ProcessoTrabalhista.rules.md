---
modulo: RecursosHumanos
agregado: ProcessoTrabalhista
contexto: RecursosHumanos (contencioso trabalhista e provisão para riscos)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["NBC TSP / MCASP (provisões, passivos e ativos contingentes — reconhecimento por probabilidade de perda)", "CLT (reclamações trabalhistas)", "Lei 4.320/1964 (execução da despesa decorrente de condenação/acordo)", "LRF — LC 101/2000 art. 50, II (registro de obrigações)"]
---

<!-- manifest
commands: CadastrarProcessoTrabalhista, ReavaliarPrognostico, RegistrarAcordo, RegistrarCondenacao, RegistrarImprocedencia, ArquivarProcessoTrabalhista
queries: BuscarProcessosTrabalhistas, ObterProcessoTrabalhista, ObterDemonstrativoProvisao
domainEvents: ProcessoTrabalhistaCadastrado, ProvisaoProcessoAtualizada, ProcessoTrabalhistaEncerrado
integrationEventsPublished:
integrationEventsConsumed:
-->

# Processo Trabalhista (Provisão) — Regras-as-Code

> **Processo Trabalhista** acompanha o contencioso (reclamações trabalhistas) contra o ente e mantém a
> **provisão contábil** correspondente, dimensionada pelo **prognóstico de perda** (provável / possível /
> remota). O valor provisionado alimenta o demonstrativo de provisões e é **reavaliado** ao longo do
> ciclo de vida do processo; o desfecho (acordo, condenação, improcedência) **encerra** o processo e
> consolida o valor efetivo. Os critérios de reconhecimento seguem a **NBC TSP / MCASP** e são
> **parametrizáveis por tenant** — sem número mágico. Este arquivo é **normativo e versionado**.
>
> ⚠️ `// TODO(validar-oficial)`: as **faixas de prognóstico** e a política de provisionamento (provável =
> 100%, possível = divulgação, remota = nada) devem ser confirmadas contra a NBC TSP vigente e a política
> contábil do tenant antes do go-live (M10).

---

## 1. Linguagem Ubíqua

| Termo (identificador-no-código) | Definição |
|---|---|
| Processo (`ProcessoTrabalhista`) | Reclamação trabalhista contra o ente. Raiz de agregado. |
| Prognóstico (`PrognosticoPerda`) | Probabilidade de perda: Provável / Possível / Remota. |
| Provisão (`ValorProvisionado`) | Valor reconhecido no passivo conforme o prognóstico. |
| Acordo (`RegistrarAcordo`) | Desfecho consensual; encerra com valor pactuado. |
| Condenação (`RegistrarCondenacao`) | Desfecho desfavorável; encerra com valor da sentença. |
| Improcedência (`RegistrarImprocedencia`) | Desfecho favorável; encerra com valor efetivo zero. |

---

## 2. Invariantes (Rules-as-Code)

- **I-1 — Provisão segue o prognóstico.** O valor provisionado é função do prognóstico vigente; mudança de
  prognóstico **reavalia** a provisão e emite `ProvisaoProcessoAtualizada`.
- **I-2 — Reavaliação só em processo ativo.** Não se reavalia prognóstico de processo já encerrado.
- **I-3 — Desfecho é terminal.** Acordo, condenação ou improcedência **encerram** o processo
  (`ProcessoTrabalhistaEncerrado`); novo desfecho sobre processo encerrado falha.
- **I-4 — Valor efetivo no encerramento.** O encerramento consolida o **valor efetivo** (acordado/condenado
  ou zero na improcedência), distinto da provisão estimada.
- **I-5 — Histórico imutável.** Cadastro, reavaliações e desfecho compõem trilha auditável para o TCE.

---

## 3. Cenários BDD (resumo)

1. **Cadastrar processo** → nasce com prognóstico inicial e provisão; emite `ProcessoTrabalhistaCadastrado`.
2. **Reavaliar prognóstico** → ajusta a provisão; emite `ProvisaoProcessoAtualizada`; em processo encerrado **falha** (I-2).
3. **Registrar acordo** → encerra com valor pactuado; emite `ProcessoTrabalhistaEncerrado`.
4. **Registrar condenação** → encerra com valor da sentença.
5. **Registrar improcedência** → encerra com valor efetivo zero.
6. **Arquivar** → processo encerrado é arquivado; novo desfecho **falha** (I-3).

> Cobertura em `tests/Tensorroot.Gov.Modules.RecursosHumanos.Tests/`.

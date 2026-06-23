# ADR-0021 — Reprodutibilidade do cálculo fiscal: derivar do EXERCÍCIO do fato gerador, nunca do relógio [OFICIAL]

- **Status:** Aceito — princípio **[OFICIAL]** para todos os motores fiscais
- **Data:** 2026-06-22
- **Código:** `Modules/Tributos/.../Domain/Calculo/CalculoValorVenal.cs`,
  `Domain/Pgv/PlantaValores.cs`,
  `Application/Iptu/{ApuradorIptu,CalcularIptu,LancarIptuAnual}.cs`,
  `Application/Itbi/ApuradorItbi.cs`, `Modules/Tributos/rules/Pgv.rules.md`

## Contexto

Cálculos fiscais (IPTU, ITBI, taxas, depreciação) viram **lançamentos** que o TCE pode **reapurar
anos depois**. Se qualquer valor derivado do tempo — **idade** do imóvel/pessoa, índices/correção,
prazos, depreciação — for computado a partir do **relógio do sistema** (`DateTime.Now/UtcNow/Today`),
a **reapuração do mesmo lançamento em outro ano daria um número diferente**: o cálculo deixa de ser
auditável e reproduzível. Para um sistema sob escrutínio do TCE, **reprodutibilidade é inegociável**.

## Decisão

Tornar os motores fiscais **determinísticos e reproduzíveis**, ancorados no **exercício do fato
gerador**:

- **O exercício é entrada explícita**, não lido do relógio. Apuradores e comandos recebem `exercicio`
  como parâmetro (`ApuradorIptu.ApurarAsync(..., int exercicio, ...)`, `CalcularIptuQuery`,
  `LancarIptuAnualCommand`, `ApuradorItbi.ApurarAsync(..., int exercicio, ...)`).
- **Parâmetros versionados por vigência.** A PGV e as tabelas de alíquota são **versionadas por
  exercício** (lei municipal); o motor lê **sempre** a versão vigente no exercício do fato gerador
  (`plantas.ObterVigenteAsync(exercicio)`), **nunca** defaults numéricos no código (CLAUDE.md §7/§16:
  "nenhum número fiscal vive no código").
- **Valores derivados do tempo saem do exercício.** A **idade** para depreciação é
  `Math.Max(0, planta.Exercicio - anoConstrucao)` — **não** `DateTime.UtcNow - dataConstrucao`. Assim
  a reapuração do mesmo lançamento em outro ano produz **sempre** o mesmo valor.
- **Relógio fora do domínio de cálculo.** Onde o tempo é inevitável (auditoria, Outbox, sessão), usa-se
  a abstração `TimeProvider` na **infraestrutura**; os **motores de domínio não injetam relógio** —
  recebem o exercício/data do fato gerador.

## Alternativas consideradas

- **Calcular idade/índices a partir de `DateTime.Now`:** simples, mas **quebra a reprodutibilidade** —
  reapuração tardia diverge. Rejeitado (é exatamente o anti-padrão que o princípio proíbe).
- **Injetar `IClock`/`IRelogio` nos motores fiscais:** ainda atrela o cálculo a um "agora" mockável;
  o correto é a base ser o **exercício do fato gerador**, não um relógio (mesmo fake). Rejeitado para
  o domínio de cálculo.
- **Hardcodar tabelas/índices no motor:** viola "nenhum número fiscal no código" e impede
  versionamento por exercício/lei municipal. Rejeitado — parâmetros versionados por vigência.

## Consequências

- ➕ **Reapuração estável:** o mesmo lançamento recalculado em qualquer ano dá o mesmo resultado —
  cálculo **auditável e defensável** perante o TCE.
- ➕ Casa com a parametrização por tenant/exercício (CLAUDE.md §7) e com o ITBI (ADR-0018), cuja
  alíquota também é lida por exercício.
- ➕ Determinismo facilita **teste por casos conhecidos** (entradas iguais → saídas iguais) e prova de
  invariantes.
- ➖ **Disciplina contínua:** todo motor novo deve receber o exercício/data do fato gerador e **evitar**
  `DateTime.Now/UtcNow/Today` no cálculo; hoje isso é convenção forte (comentários, rules), **sem**
  fitness-function dedicada que falhe o build ao ver `DateTime.Now` no domínio de cálculo — risco de
  regressão a cobrir por revisão/teste.
- ➖ Exige o subsistema de **parâmetros versionados por vigência** populado por exercício (PGV,
  alíquotas) — *fail-closed*: sem versão vigente, o cálculo **lança** em vez de assumir default.
- 🔗 Princípio transversal aos motores de **Tributos** (e à depreciação MCASP no Patrimônio); base
  obrigatória para qualquer cálculo fiscal futuro.

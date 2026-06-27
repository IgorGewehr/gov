# ADR-0020 — Trilha de ACESSO LGPD (behavior `ISensivelLgpd`/`TrilhaAcessoSensivelBehavior`) + redação de PII por padrão (`CampoSensivelLgpdAttribute`)

- **Status:** Aceito
- **Data:** 2026-06-22
- **Código:** `BuildingBlocks.Application/Messaging/ISensivelLgpd.cs`,
  `BuildingBlocks.Application/Behaviors/TrilhaAcessoSensivelBehavior.cs`,
  `BuildingBlocks.Application/Abstractions/IRegistroAcessoSensivel.cs`,
  `BuildingBlocks.Infrastructure/Auditing/{RegistroAcessoSensivel,PoliticaRedacaoAuditoria,AuditSaveChangesInterceptor}.cs`,
  `SharedKernel/CampoSensivelLgpdAttribute.cs`

## Contexto

CONVENCOES-ENGENHARIA.md §6 (LGPD) exige, para dados sensíveis (**Saúde, Assistência Social, menores na Educação**),
**base legal explícita**, **minimização** e **trilha de acesso** — *quem leu o quê, quando, por quê*.
A `AuditTrail` (ADR-0016) registra **mutações**; faltavam dois mecanismos: (1) **trilha de leitura**
(a LGPD pede *accountability* de **acesso**, não só de escrita) e (2) garantir que a própria trilha
(e logs) **não vaze** a PII que registra. Sem isso, o sistema gravaria CPF/CNS/NIS e dados clínicos
em claro na auditoria — ferindo a minimização que a LGPD impõe.

## Decisão

Duas peças complementares, *deny-by-default*:

- **Trilha de acesso (leitura).** Queries que tocam PII implementam o marcador **`ISensivelLgpd`**
  (`EntidadeSensivel`, `EntidadeId?`, `BaseLegal`). O **`TrilhaAcessoSensivelBehavior`** (pipeline
  MediatR, **após** Logging/Validation e **após** o handler retornar) sela o acesso via
  `IRegistroAcessoSensivel.RegistrarAsync` → grava na **`AuditTrail`** com `Action = "Read"`, usuário
  (claim `sub`, não forjável), IP, tenant, entidade/id e **base legal** estruturada, **encadeado na
  mesma hash-chain** (ADR-0016). A selagem é **parte da operação**: se não for possível registrar, a
  leitura **aborta** (accountability *deny-by-default*) — a exceção não é engolida.
- **Redação de PII por padrão.** `CampoSensivelLgpdAttribute` (SharedKernel) marca propriedades
  sensíveis. `PoliticaRedacaoAuditoria.DeveRedigir` redige (`[REDACTED]`) ao gravar a trilha quando:
  (a) a coluna é cifrada (material de cofre), **(b)** a propriedade tem o atributo, **ou (c)** o nome
  bate uma **convenção de segurança** (CPF, NIS, CNS, PIS, PASEP…) — rede de proteção mesmo sem
  anotação. A máscara é recursiva no JSON antes/depois (apanha PII aninhada). **Por padrão = redigido**
  salvo material explicitamente seguro.

## Alternativas consideradas

- **Logar acesso só por código no handler de cada query sensível:** disperso e fácil de esquecer; um
  handler novo vaza acesso. O **behavior** centraliza e o marcador torna a intenção explícita.
  Rejeitada a dispersão.
- **Tabela de acesso separada (fora da `AuditTrail`):** perderia a imutabilidade/hash-chain já
  provada. Reusar a `AuditTrail` com `Action="Read"` dá *append-only* e verificação grátis. Preferido.
- **Redação opt-in (logar em claro salvo marcação explícita):** inverte o risco — um campo esquecido
  vaza. Escolhido **deny-by-default** com convenção de nomes como salvaguarda.
- **Só não registrar PII na trilha de mutação (sem trilha de leitura):** não cumpre a *accountability*
  de **acesso** que a LGPD pede. Rejeitado.

## Consequências

- ➕ *Accountability* de leitura sensível (quem/o quê/quando/**base legal**) **imutável e verificável**
  (mesma hash-chain do ADR-0016) — pronta para ANPD/TCE.
- ➕ **Minimização real:** PII redigida por padrão na trilha e nos logs; três camadas (cifra +
  atributo + convenção) reduzem o risco de vazamento por esquecimento.
- ➕ Marcador + behavior tornam a regra **explícita e barata** para queries não sensíveis (sem
  `ISensivelLgpd`, custo zero).
- ➖ **Acoplamento à disciplina do marcador:** uma query sensível que **esqueça** `ISensivelLgpd` não
  gera trilha de acesso — mitigar por revisão/teste por módulo (há testes LG-1/LG-2 para selagem,
  não-forjabilidade e redação).
- ➖ A redação por **convenção de nome** pode redigir demais (falso positivo) ou de menos se um campo
  PII tiver nome atípico — daí o atributo explícito como complemento.
- ➖ Selar o acesso por leitura adiciona uma escrita por query sensível (custo + a leitura passa a
  depender do sucesso da gravação da trilha).
- 🔗 Construído sobre o ADR-0016 (imutabilidade) e o ADR-0007 (`NivelSensibilidade`/ABAC do recurso).

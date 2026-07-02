# Caça a bugs (iteração 1) — resultado e residual honesto

> Revisão adversarial multi-agente (46 agentes) sobre 16 módulos + Platform + BuildingBlocks:
> **27 achados → 16 confirmados pelo cético**. Cada um foi **re-verificado no código** antes de agir
> (o cético erra — 1 falso-positivo pego). Resultado abaixo.

## Corrigidos (11) — todos com teste e suíte verde

| # | Módulo | Correção |
|---|--------|----------|
| #6 | PainelGestor | RCL do mesmo mês era sobrescrita (`>=`→`>`) — corrompia o denominador LRF |
| #15 | Finanças | `LiberarEmpenho` truncava saldo a zero (fail-open → fail-closed) |
| #8 | Educação | nota sem validação `[0,10]` no domínio |
| #14 | Tributos | busca fiscal por documento sem filtro explícito de tenant |
| #10 | Protocolo | reativar processo nunca tramitado emitia setor `Guid.Empty` |
| #11 | Protocolo | arquivamento descartava motivo/data (Lei 9.784) → persiste + migration |
| #7 | Legislativo | Ordem do Dia aceitava proposição inexistente |
| #2 | Platform | índice de login para tenant inexistente (órfão → login falha silenciosa) |
| #3 | Platform | FKs de integridade (TenantModules/UsuariosTenantIndex → Tenants) + migration |
| #12 | Patrimônio | `MotivoBaixa` era `int` com `IsInEnum()` fantasma → enum real |
| #16 | Administração | aditivo de reequilíbrio não reduzia valor → `ReequilibrioReducao` (art. 124-126) |

## Falso-positivo verificado (1)

- **#1 `ValueObjectJsonConverter` `return null!`** — **dead code**: `JsonConverter<T>` com `HandleNull`
  default não é chamado pelo System.Text.Json em token null (o STJ atribui null direto). Trocar por
  `throw` não teria efeito. Descartado.

## Não-issue verificado (1)

- **#9 Saúde `MarcarAgendamento`** — a `OcuparVaga` antes da checagem de data futura deixa a vaga
  Ocupada **em memória**, mas o `SaveChanges` não é alcançado e o agregado é **descartado**. Nenhum
  estado incorreto é persistido. Não é bug.

## Deferidos com nota (3) — reais, mas fix de escopo/risco maior

- **#5 (S1, ALTA) — escalação lateral de papéis em `CriarUsuario`** — falta a prova I4. Fix tem blast
  radius (endpoint 403 + testes sem concedente) e deve extrair a prova I4 para serviço compartilhado.
  Detalhe em `SEGURANCA-PENDENCIAS.md`.
- **#4 (S2, ALTA) — race na rotação de certificado A1** — falta índice único filtrado
  `(TenantId, Status='Ativo')`. Detalhe em `SEGURANCA-PENDENCIAS.md`.
- **#13 (MÉDIA) — pensões alimentícias sem rastreio por beneficiário** — RE-VERIFICADO: **não é erro de
  cálculo** (a `FolhaDePagamento` adiciona eventos separados e soma corretamente o total). É
  limitação de **rastreabilidade**: múltiplas pensões usam a mesma rubrica, então o **repasse
  individual** e o **eSocial S-1200 (ideBenef)** não conseguem distinguir beneficiários. O fix (referência
  ao beneficiário em `EventoFolha`, threaded até o eSocial) pertence ao trabalho de transporte eSocial
  (cred-gated) — `src/Modules/RecursosHumanos/.../Folha/ApurarDescontosLegais.cs:128`.

## Princípio

Nenhuma correção quebrou build/testes; nada de domínio ou contrato oficial foi fabricado; cada achado
foi lido no código antes de qualquer mudança.

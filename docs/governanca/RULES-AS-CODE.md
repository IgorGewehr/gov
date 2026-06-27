# Governança Rules-as-Code — A Especificação é a Fonte da Verdade

> **Princípio:** o código é **derivado de um arquivo de regras** (`*.rules.md`) por **módulo**
> e por **agregado/entidade**. A especificação `.rules.md` é a **fonte da verdade**: dela saem
> domínio, casos de uso, persistência, endpoints e **testes**, e o código é **auditado contra as
> regras** (Spec-Code Consistency Check). O `.rules.md` é **normativo e versionado** — alterações
> de comportamento começam por ele.

---

## 1. Por que

Um ERP GovTech de missão crítica com **dezenas de módulos** (muitos são mini-sistemas) só é
sustentável se cada comportamento tiver um **contrato rígido, legível e executável**. Esse
contrato:

- elimina ambiguidade para o gerador de código;
- permite **auditoria automática** (código ⇄ regras) sem revisão humana linha a linha;
- transforma "corrigir bug / mudar regra / adicionar feature" em **editar um `.md`**;
- mantém a rastreabilidade legal (cada regra cita a lei/norma).

## 2. Fluxo de trabalho

```
Engenheiro edita  src/Modules/<M>/rules/<Entidade>.rules.md   (ponto de partida)
        │
        ▼
Pipeline (determinístico):
  1. Geração   → Domain + Application + Infrastructure conforme as regras
  2. Testes    → 1 teste por invariante, 1 por transição de estado, 1 por cenário BDD
  3. Auditoria → verifica código ⇄ regras (cobertura total) + segurança + tenant
  4. Correção  → conserta divergências (mexe no CÓDIGO, nunca nas regras)
  5. Checagem  → roda build + testes + fitness functions + spec-code consistency
        │
        ▼
PR  →  CI (NetArchTest + spec-code consistency + auditoria de CVEs)  →  merge
```

Regra de ouro: **bug/ajuste/nova regra ⇒ muda o `.rules.md`**. O código é consequência.

## 3. Template normativo do `*.rules.md`

Toda entidade/agregado segue **exatamente** esta estrutura (campos obrigatórios):

```markdown
---
modulo: <Modulo>            # ex.: Tributos
agregado: <Entidade>        # ex.: DividaAtiva
contexto: <Bounded Context>
poder: Executivo|Legislativo|Ambos
schema: <schema-ef>         # ex.: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: [ "CTN art. 174", "Lei 6.830/80" ]
---

## 1. Linguagem Ubíqua
Tabela `Termo (identificador-no-código sem acento) — definição`. Os identificadores são VINCULANTES.

## 2. Modelo
- Identidade: <Id forte> (record struct Guid)
- Propriedades: nome : Tipo (VO/enum/primitivo) — descrição
- Value Objects e enums (com valores)

## 3. Invariantes
Lista NUMERADA. Cada item I-n vira `[Fact] Invariante_n_*`.

## 4. Máquina de Estados
Tabela: Estado origem → comando → Estado destino | guarda | evento emitido.

## 5. Comandos (escrita)
Para cada comando: entrada (DTO), pré-condições, efeito, pós-condições, exceções, evento.

## 6. Consultas (leitura)
Nome, entrada, projeção (DTO), filtros (sempre tenant-scoped).

## 7. Eventos
- Domínio: Nome(payload)
- Integração (publica/consome via *.Contracts): Nome(payload)

## 8. Validações (FluentValidation)
Campo → regra → mensagem.

## 9. Persistência
Tabela, colunas, conversores (VO/Id), índices (incl. únicos), schema.

## 10. Segurança, Tenant e Auditoria
IMustHaveTenant, RBAC (policies/roles), LGPD (dados sensíveis), trilha de auditoria.

## 11. Integrações Governamentais
Sistema, layout/versão, prazo legal, resiliência (Polly), ACL, idempotência.

## 12. Cenários BDD
Given/When/Then. Cada cenário vira teste de integração.

## 13. Casos de Borda
Enumerados (cada um vira teste).

## 14. Changelog
| versao | data | mudança |
```

## 4. Enforcement (o que torna as regras VINCULANTES)

1. **Fitness Functions (NetArchTest)** — isolamento de camadas e de módulos.
2. **Spec-Code Consistency Check** — teste/analisador que, lendo o front-matter
   e as seções do `.rules.md`, exige que:
   - todo **Comando** tenha um `*Command` + `*Handler` + `*Validator`;
   - toda **Consulta** tenha `*Query` + `*Handler`;
   - todo **Evento** exista no assembly correto (domínio vs `*.Contracts`);
   - toda **Invariante** tenha um teste por convenção de nome;
   - todo **Cenário BDD** tenha um teste correspondente;
   - o **schema** e os **índices** batam com a configuração EF.
3. **Cobertura obrigatória** das invariantes e cenários (gate de CI).
4. **Auditoria de CVEs** (NuGetAudit, já ligada) e **revisão de segurança** automatizada.

## 5. Responsabilidades por etapa (separação de papéis no pipeline)

| Etapa | Lê | Produz | Nunca faz |
|---|---|---|---|
| **Geração** | `.rules.md` | Domain/App/Infra | inventar regra fora do `.md` |
| **Testes** | `.rules.md` | testes (invariantes/estados/BDD) | testar além do especificado |
| **Auditoria** | código + `.rules.md` | relatório de divergência | corrigir |
| **Correção** | relatório | correção de **código** | editar `.rules.md` |
| **Integração** | `.rules.md` §11 | gateway/worker/ACL + Polly | acoplar a outro módulo |
| **Revisão de Segurança** | diff | parecer (tenant/LGPD/SQLi/segredos) | aprovar sem checar |

## 6. Exemplo concreto (retrofit do código já existente)

`DividaAtiva` (módulo Tributos), hoje já implementado, expresso como regras:

```markdown
---
modulo: Tributos
agregado: DividaAtiva
poder: Executivo
schema: tributos
versao_regras: 1.0.0
fontes_legais: ["CTN art. 174 (prescrição 5 anos)", "Lei 6.830/80", "Lei 9.492/97"]
---
## 3. Invariantes
I-1. Prazo prescricional = data de inscrição + 5 anos (CTN art. 174).
I-2. CDA só é emitida a partir da situação `Inscrita`.
I-3. Protesto exige `CdaEmitida`.
I-4. Execução fiscal exige `CdaEmitida` ou `Protestada`.
I-5. Parcelamento (REFIS) suspende exigibilidade e interrompe prescrição.
I-6. Dívida `Quitada`/`Cancelada` não admite novas transições.
## 4. Máquina de Estados
| Origem | Comando | Destino | Guarda | Evento |
| Inscrita | EmitirCda | CdaEmitida | nº CDA informado | CdaEmitida |
| CdaEmitida | Protestar | Protestada | exigível | — |
| CdaEmitida\|Protestada | AjuizarExecucaoFiscal | EmExecucaoFiscal | exigível | — |
| (exigível) | FirmarParcelamento | Parcelada | — | ParcelamentoFirmado |
| (≠ encerrada) | Quitar | Quitada | — | DividaQuitada |
## 7. Eventos
- Domínio: DividaAtivaInscrita, CdaEmitida, ParcelamentoFirmado, DividaQuitada
- Integração (publica): Tributos.Contracts.ReceitaArrecadadaIntegrationEvent
## 12. Cenários BDD
- Dado dívida `Inscrita`, Quando EmitirCda("CDA-..."), Então situação=`CdaEmitida` + evento.
- Dado dívida `Quitada`, Quando Protestar, Então erro "não exigível".
```

O código (`DividaAtiva.cs`, handlers, EF config, testes) **satisfaz** essas regras —
o modelo é fiel ao `.rules.md`. **Todo módulo nasce do `.rules.md`.**

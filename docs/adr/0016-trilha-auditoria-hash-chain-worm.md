# ADR-0016 — Trilha de auditoria imutável por HASH-CHAIN + WORM (init-only não basta para o TCE)

- **Status:** Aceito
- **Data:** 2026-06-22
- **Código:** `BuildingBlocks.Infrastructure/Auditing/{AuditHashChain,AuditTrail,AuditSaveChangesInterceptor,IVerificadorTrilhaAuditoria,VerificadorTrilhaAuditoria,VerificacaoTrilhaResultado}.cs`,
  `BuildingBlocks.Infrastructure/Multitenancy/SchemaProvisioner.cs`,
  `Modules/Cofre/.../Migrations/20260622190000_AuditTrailHashChain.cs`,
  `ApiHost/Admin/AdminEndpoints.cs`

## Contexto

O `AuditSaveChangesInterceptor` (CONVENCOES-ENGENHARIA.md §6) grava a `AuditTrail` (antes/depois em JSON, quem, IP,
quando) com propriedades **`init`-only** — o que impede mutação **pela aplicação**, mas **não** pelo
banco. Um operador com acesso ao banco **dedicado** do tenant (ADR-0005), ou uma SQL injection
futura, poderia **reescrever ou apagar** a trilha. Para um ERP que processa dinheiro público sob
escrutínio do **Tribunal de Contas**, a trilha **é prova** levada ao TCE: **imutabilidade
verificável** é requisito, não *nice-to-have*. `init`-only é só intenção do código de aplicação.

## Decisão

Tornar a trilha **inviolável e auto-verificável** em três camadas complementares:

- **Hash-chain por tenant (camada criptográfica).** Cada linha é **selada**:
  `HashAtual = SHA-256(HashAnterior ‖ conteúdo canônico)`, com `HashGenesis = "GENESIS"` como âncora
  e uma `Sequencia` monotônica **por tenant**. O conteúdo canônico cobre Id, TenantId, Sequencia,
  entidade/ação, valores antes/depois, usuário, IP e timestamp (normalizado a milissegundos para
  *round-trip* estável entre SQLite/SqlServer). O selo é feito no interceptor, lendo o último selo
  do tenant e encadeando a nova linha.
- **WORM no banco (camada de banco — só SqlServer/produção).** Triggers `INSTEAD OF UPDATE` e
  `INSTEAD OF DELETE` na tabela `AuditTrail` **lançam** (THROW 51001/51002) qualquer tentativa de
  alterar/remover linhas. Criados pela migration e **garantidos de forma idempotente** em runtime
  pelo `SchemaProvisioner` (CREATE TRIGGER IF NOT EXISTS por schema). No SQLite (dev) a detecção fica
  a cargo da cadeia de hash + verificador.
- **Verificador (camada de prova).** `IVerificadorTrilhaAuditoria` re-caminha a cadeia do genesis e
  detecta **lacuna de sequência** (linha removida/inserida), **elo quebrado** (`HashAnterior` não
  bate) e **conteúdo adulterado** (`HashAtual` recomputado diverge), apontando a 1ª divergência.
  Exposto em endpoint admin dedicado (`/api/admin/auditoria/verificacao`: 200 íntegra / 409
  adulterada). A detecção é **determinística e não depende** de o banco impor WORM — complementa o
  trigger.

## Alternativas consideradas

- **Manter só `init`-only:** não protege contra UPDATE/DELETE direto no banco; trilha do TCE
  reescrevível. Rejeitado.
- **Apenas trigger WORM (sem hash-chain):** protege contra edição casual, mas não dá **prova
  portável** (vale só onde o trigger existe; SQLite/dev fica descoberto) nem detecta adulteração de
  um *backup* restaurado. Rejeitado isolado.
- **Tabela temporal / Ledger nativo do SQL Server:** poderoso, mas amarra ao fornecedor e ao tier de
  banco, e não cobre o SQLite de dev. A cadeia de hash é portável e independente do banco.
- **Append-only só por convenção/permissão de banco:** disciplina operacional, não invariante
  provável. Rejeitado.

## Consequências

- ➕ Trilha **à prova de adulteração e remoção**, com **prova determinística** (verificador) que não
  depende do banco — exatamente o que o controle externo (TCE) exige.
- ➕ A mesma infraestrutura sela a **trilha de acesso LGPD** (ADR-0020: ação `Read`), unificando
  imutabilidade de mutação e de leitura sensível.
- ➕ Camadas independentes (app `init`-only + banco WORM + cripto hash-chain): falha de uma não
  derruba a garantia.
- ➖ **Custo de escrita:** cada INSERT lê o último selo do tenant e calcula SHA-256; serialização por
  tenant na cadeia. Aceitável para a criticidade.
- ➖ **Linhas legadas** (pré-cadeia) ficam com `Sequencia=0`/hashes vazios e são **ignoradas** pelo
  verificador — a garantia vale do *go-live* da cadeia em diante.
- ➖ WORM **SqlServer-específico**; em SQLite (dev) a imutabilidade real depende só da cadeia. A
  provisão idempotente em runtime evita esquecer o trigger ao criar schema de novo tenant.
- 🔗 Reaproveita o ADR-0005 (banco por tenant: o trigger é criado por schema/tenant) e habilita a
  remessa da trilha ao TCE como artefato confiável (ADR-0009).

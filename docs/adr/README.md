# Architecture Decision Records (ADR)

Registro imutável das decisões arquiteturais relevantes do Tensorroot.Gov.
Formato leve (MADR). Uma decisão por arquivo; nunca editar uma decisão "Aceita" —
crie uma nova que a **substitua** (`Superseded by`).

| ADR | Título | Status | Data |
|---|---|---|---|
| [0001](0001-monolito-modular-clean-architecture.md) | Monolito Modular + Clean Architecture + DDD | Aceito | 2026-06-20 |
| [0002](0002-multitenancy-ativacao-modular.md) | Multi-tenancy e ativação modular por tenant | Aceito | 2026-06-20 |
| [0003](0003-nfse-nacional-adn-integracao-passiva.md) | NFS-e Nacional (ADN): integração passiva via Worker | Aceito | 2026-06-20 |
| [0004](0004-frontend-react-govbr-ds.md) | Frontend React + gov.br Design System | Aceito | 2026-06-20 |
| [0005](0005-banco-dedicado-por-tenant.md) | Banco DEDICADO por tenant (não schema-por-módulo) | Aceito | 2026-06-22 |
| [0006](0006-jwt-self-issued-hs256.md) | Autenticação JWT self-issued (HS256), sem IdP externo | Aceito | 2026-06-22 |
| [0007](0007-autorizacao-rbac-abac-uo-i4.md) | Autorização RBAC dinâmico + ABAC organizacional (UO) + I4 | Aceito | 2026-06-22 |
| [0008](0008-a1-envelope-encryption.md) | Certificado A1 por envelope encryption (banco cifrado + KEK no Key Vault) | Aceito | 2026-06-22 |
| [0009](0009-transmissao-tce-siconfi-manual.md) | Transmissão TCE-RS/SICONFI MANUAL (gera artefatos + reconcilia) | Aceito | 2026-06-22 |
| [0010](0010-scopedbcontextholder-moduleunitofwork.md) | ScopeDbContextHolder + ModuleUnitOfWork | Aceito | 2026-06-22 |
| [0011](0011-outbox-domain-integration-events.md) | Outbox Pattern para integration events | Aceito | 2026-06-22 |
| [0012](0012-rules-as-code-fitness-functions.md) | Rules-as-Code (*.rules.md) + Fitness Functions (NetArchTest) | Aceito | 2026-06-22 |
| [0013](0013-contabilidade-pcasp-lancamento-automatico.md) | Contabilidade PCASP — lançamento automático via domain events | Aceito | 2026-06-22 |
| [0014](0014-stack-frontend-react-vite-tanstack.md) | Stack frontend: React + Vite + TanStack Query + registry + `<Can>` | Aceito | 2026-06-22 |
| [0015](0015-despacho-outbox-isolado-por-mensagem.md) | Despacho do Outbox isolado por mensagem (escopo de DI por mensagem) + reuso na assinatura A1 | Aceito | 2026-06-22 |
| [0016](0016-trilha-auditoria-hash-chain-worm.md) | Trilha de auditoria imutável por HASH-CHAIN + WORM (init-only não basta para o TCE) | Aceito | 2026-06-22 |
| [0017](0017-ordem-interceptors-tenant-audit-outbox.md) | Ordem dos interceptors Tenant→Audit→Outbox unificada por helper (corrige W0.2) | Aceito | 2026-06-22 |
| [0018](0018-itbi-base-valor-declarado-tema-1113-arbitramento-148.md) | ITBI: base = valor declarado (Tema 1.113/STJ) + arbitramento (CTN 148) como processo com contraditório | Aceito | 2026-06-22 |
| [0019](0019-autorizacao-i4-composicao-papeis-guarda-cross-tenant.md) | Enforcement I4 na composição/atribuição de papéis + guarda cross-tenant (permissão de plataforma fora de `Permissoes.Todas`) | Aceito | 2026-06-22 |
| [0020](0020-trilha-acesso-lgpd-redacao-pii-por-padrao.md) | Trilha de ACESSO LGPD (`ISensivelLgpd`/behavior) + redação de PII por padrão (`CampoSensivelLgpdAttribute`) | Aceito | 2026-06-22 |
| [0021](0021-reprodutibilidade-calculo-fiscal-exercicio-fato-gerador.md) | Reprodutibilidade do cálculo fiscal: derivar do EXERCÍCIO do fato gerador, nunca do relógio [OFICIAL] | Aceito | 2026-06-22 |

> **Nota:** o ADR-0005 aprofunda e firma o modelo *database-per-tenant* esboçado no ADR-0002.
> A decisão **NFS-e passiva** é o ADR-0003 (já existente). Onde estas ADRs divergem da `CLAUDE.md`
> (ex.: §9 "schema isolado" lido como shared-DB → ADR-0005; §6 "segredos só no Key Vault" → ADR-0008),
> a leitura da constituição passa a ser feita **sob a ADR**.

# Avaliação de Engenharia — Tensorroot.Gov (gatilho M5/M6)

**Avaliador:** arquiteto/auditor externo, independente. **Escopo:** engenharia ponta a ponta de um ERP GovTech multi-tenant (.NET 8) que processa dinheiro público sob escrutínio do TCE. **Ênfase:** auditoria (trilha imutável), persistência e isolamento multi-tenant.
**Base:** código real em `src/`, testes em `tests/`, `docs/progresso/progresso.json`. Diagnóstico pré-M0 ignorado por instrução. **Não** foram executados build/test nem subida de porta — análise estática + evidências de runtime registradas.

**Veredito resumido:** a fundação de engenharia é **sólida e madura para o estágio** (M5/M6 fundacionais). Outbox, motores de cálculo, autorização e isolamento de leitura estão bem construídos e cobertos por testes. Há **um risco ALTO real (ordem dos interceptors) que o próprio backlog admite estar latente (W0.2)** e **lacunas de imutabilidade da trilha** que precisam ser fechadas antes de tocar dinheiro público em produção.

---

## 1. Riscos por severidade

### ALTO

**A1 — Ordem dos interceptors: Auditoria roda ANTES do Tenant (TenantId espúrio na trilha em inserções).**
`src/Modules/Financas/.../FinancasModule.cs:51-54` (e idêntico nos 13 módulos) registra `AddInterceptors(Audit, Tenant, Outbox)`. O EF Core executa `SavingChanges` **na ordem de registro**, então `AuditSaveChangesInterceptor` roda primeiro. Em `AuditSaveChangesInterceptor.cs:100` o `TenantId` da trilha é lido de `entry.Entity.TenantId` **antes** de o `TenantSaveChangesInterceptor` carimbá-lo (`TenantSaveChangesInterceptor.cs:47-48`). Para entidades cujo factory já seta `TenantId` (padrão observado em Finanças, ex. `Empenho.cs:86`, `LancamentoContabil.cs:81`) o valor sai correto — mas **qualquer entidade que dependa do carimbo do interceptor terá `TenantId=Guid.Empty` na trilha**, quebrando o filtro/segregação por tenant da auditoria. O backlog reconhece o problema: progresso.json marca `W0.2 "Ordem dos interceptors (Tenant→Audit) — latente"` como `pending`. **Correção:** inverter para `Tenant → Audit → Outbox` em todos os módulos (a ordem é replicada 13×, então um helper único de registro evitaria a divergência) e adicionar teste que prove `AuditTrail.TenantId == tenant` para entidade sem TenantId pré-setado.

**A2 — Trilha de auditoria SEM imutabilidade real no banco (apenas convenção em memória).**
`AuditTrail.cs` usa props `init`-only (impede mutação via app), mas **não há proteção no nível de banco** contra `UPDATE`/`DELETE`: nenhum trigger, nenhuma tabela append-only/temporal, nenhum WORM (`grep` por trigger/WORM/append-only em `src/` = vazio, exceto comentários). Um operador com acesso ao banco do tenant (ou SQL injection futura) pode reescrever/apagar a trilha que o TCE considera prova. Para um ERP que "processa dinheiro público sob escrutínio do Tribunal de Contas", **imutabilidade verificável é requisito, não nice-to-have**. CLAUDE.md §6 promete trilha "imutável"; hoje isso é só intenção do código de aplicação. **Correção:** triggers `INSTEAD OF UPDATE/DELETE` (ou SQL Server temporal/ledger tables) na `AuditTrail`, e hash-chain/selo por linha para detecção de adulteração. (W9.7 "WORM" está em M9 — mas a trilha já grava em M0; o gap existe agora.)

### MÉDIO

**M1 — Visualizador de auditoria não agrega trilha multi-schema (perda de visibilidade em produção SqlServer).**
`AuditoriaReadDbContext.cs:8-13` mapeia `AuditTrail` sem schema e o próprio comentário admite: em SqlServer (multi-schema por módulo, o modo de produção) cada módulo grava sua `AuditTrail` no **seu** schema e este contexto só enxerga o default — "agregação entre schemas é uma evolução futura". Resultado: em produção o admin **não vê a trilha da maioria dos módulos** por um único ponto. Funciona em SQLite (dev) porque tudo cai num arquivo. **Correção:** contexto/consulta por módulo ou tabela de auditoria unificada por tenant.

**M2 — Trilha de ACESSO (LGPD) não é sistêmica — depende de cada módulo lembrar de registrar.**
O `AuditSaveChangesInterceptor` cobre **mutações** (Added/Modified/Deleted), não **leituras**. A trilha de acesso a dado sensível (CLAUDE.md §6: "quem leu o quê, quando, por quê") existe só onde foi codada à mão (`AssistenciaSocial/.../RegistrarAcessoProntuario.cs`, `Saude/.../ObterHistoricoClinicoDoPaciente.cs`). Não há mecanismo transversal: Saúde, Educação (menores) e Assistência podem expor PII sem trilha se um handler esquecer. **Correção:** abstração de leitura auditada (behavior/decorator) para queries marcadas como sensíveis, com cobertura por teste de arquitetura.

**M3 — Segredo JWT único compartilhado por todos os tenants (raio de explosão).**
`Program.cs:76-95` valida HS256 com um único `Jwt:Secret`; `EmissorToken.cs:76` assina com o mesmo. O `tenant_id` é vinculado server-side na emissão (`EmissorToken.cs:59`) — **bom**, não é forjável pelo cliente. Mas vazamento do segredo único permite forjar token de **qualquer** tenant. Para isolamento "extremo" (CLAUDE.md §5), considerar chave por tenant ou assinatura assimétrica (RS256) com rotação. Tratar como decisão arquitetural a documentar, não bug aberto.

### BAIXO / OBSERVAÇÕES

- **B1 — `IpAddress`/`UserId` da trilha nulos em mutações fora de requisição** (jobs/seed/drain do Outbox): `CurrentUser.cs` lê do HttpContext, que é nulo em background. A trilha registra a mutação mas sem ator/IP. Aceitável se rotulado como "sistema", mas hoje fica silenciosamente nulo — registrar um ator-sistema explícito.
- **B2 — Provisionamento não é transacional entre catálogo e bancos dos módulos** (`TenantProvisioner.cs:30-51`): se a migração de um módulo falhar no meio, o tenant fica meio-provisionado (catálogo gravado, alguns bancos sem schema). Falta compensação/idempotência de re-execução.
- **B3 — `FaixaIdade` usa `DateTime.UtcNow.Year`** (`CalculoValorVenal.cs:104`) dentro do motor: introduz dependência de relógio num cálculo que se quer determinístico/reproduzível para o exercício. Deveria derivar a idade do **exercício** apurado, não do "hoje".

---

## 2. O que está sólido (engenharia de referência)

- **Outbox (dead-letter/backoff): muito bem feito.** `OutboxPublisher.cs` seleciona elegíveis por `ProcessedOnUtc==null && DeadLetteredOnUtc==null && NextAttemptUtc<=agora` (`:49-57`), backoff exponencial com teto (30s→15min, `:113-121`), poison message sai da cabeça do lote sem bloquear, dead-letter após `MaxAttempts=5`. Resiliência por-mensagem (uma falha não derruba o lote) e idempotência at-least-once nos handlers. Índice de drenagem correto (`ModelBuilderExtensions.cs:89`). Coberto por `OutboxPoisonTests`, `OutboxDispatchIsolationTests`, `OutboxPublisherTests`.
- **Isolamento multi-tenant de leitura:** Global Query Filter por `TenantId` aplicado por reflexão na base (`ModelBuilderExtensions.cs:45-49`), combinado por AND com o filtro de UO. Banco-por-tenant real (`TenantConnectionResolver.cs`) com cache TTL+invalidação. Carimbo + bloqueio de gravação cross-tenant (`TenantSaveChangesInterceptor.cs:50-55`). Provado em `IsolamentoTenantTests` (inclui o caminho de autenticação que ignora o filtro mas exige tenant explícito).
- **Drenagem do Outbox isolada por escopo/tenant** (`OutboxBackgroundService.cs` + `ScopedOutboxMessageDispatcher`): cada mensagem despachada em escopo de DI próprio com `TenantOverride`, evitando colisão de `ModuleDbContext` (guarda H5). Raciocínio de layering correto e documentado.
- **Motores de cálculo (folha e IPTU/ISS/ITBI): determinísticos, parametrizados, fail-closed, auditáveis.** `MotorDeCalculoFolha.cs` — INSS/RPPS/IRRF só por tabelas por tenant/competência, **fail-closed** se faltar tabela do regime (`:73-88`), arredondamento explícito `AwayFromZero`. `CalculadoraValorVenal.cs`/`CalculoIptu.cs` — fórmula sem defaults numéricos, **memória de cálculo** (cada fator/parcela exposto para o TCE), validação de faixas de isenção/desconto (`:68-76`). Nenhuma alíquota hardcoded. O bug de "teto=0 zerando líquido" citado no progresso **não está presente** no código atual.
- **Autorização: deny-by-default + I4 provado no domínio.** RBAC por claim `perm` sem fallback permissivo (`PermissionAuthorization.cs:36-39`), políticas materializadas sob demanda. Delegação I4 ("não delega o que não tem") é verificada no domínio (`AtribuirPapelAoUsuario.cs:79-99`, `AutorizacaoDeConcessao`), não só na UI. Separação operador-de-plataforma × admin-de-tenant garantida por construção no catálogo (`Program.cs:209-223`).

---

## 3. Recomendações priorizadas

1. **[ALTO] Inverter a ordem dos interceptors para Tenant→Audit→Outbox** nos 13 módulos (extrair helper único de registro) e blindar com teste que prove `AuditTrail.TenantId` correto para entidade sem TenantId pré-setado. Fecha W0.2 (latente assumido).
2. **[ALTO] Imutabilidade real da trilha:** triggers `INSTEAD OF UPDATE/DELETE` + hash-chain/selo por linha (ou ledger/temporal no SqlServer). Antecipar parte de W9.7 — a trilha já grava desde M0.
3. **[MÉDIO] Visualizador de auditoria multi-schema** (M1): consolidar a trilha por tenant em produção SqlServer, senão a maioria dos módulos fica invisível ao auditor.
4. **[MÉDIO] Trilha de acesso LGPD transversal** (M2): behavior/decorator para leituras sensíveis (Saúde/Educação/Assistência) + teste de arquitetura que falhe se um endpoint sensível não auditar leitura.
5. **[MÉDIO] Ator-sistema explícito na trilha de jobs** (B1) e **provisionamento idempotente/compensável** (B2).
6. **[BAIXO] Derivar idade do imóvel pelo exercício**, não por `DateTime.UtcNow` (B3); documentar decisão de chave JWT por-tenant vs. única (M3).

**Conclusão:** nada aqui impede seguir a engenharia de M5/M6; mas **A1 e A2 são pré-requisitos de prontidão para produção tocando dinheiro público** e devem entrar antes do go-live (M10), idealmente já no próximo M1.x de robustez. A base é confiável e os pontos críticos são localizados e corrigíveis.

# Pendências de segurança (backlog priorizado)

> Itens de segurança confirmados mas **deliberadamente não corrigidos numa leva rápida** por terem
> blast radius alto ou dependerem de credencial/infra. Documentados para tratamento dedicado.

## S1 — Escalação lateral de papéis na criação de usuário (ALTA) — CONFIRMADO

**Onde:** `src/Modules/Identidade/.../Application/Usuarios/CriarUsuario.cs` (handler).

**Problema:** `CriarUsuarioHandler` aceita `PapeisIds` iniciais e chama `Usuario.Criar(...)` com esses
papéis **sem validar a regra I4 ("não delega o que não tem")**. Diferentemente de
`DefinirPapeisDoUsuarioHandler` (que chama `ProvarCoberturaI4GlobalAsync`) e `AtribuirPapelHandler`, o
`CriarUsuario` só verifica que os papéis **existem**, não que o **concedente tem escopo para concedê-los**.
Um administrador com apenas `identidade.usuarios.gerenciar` pode criar um usuário já com papéis de
Finanças/Saúde que ele próprio não possui.

**Por que não foi corrigido numa leva:** o fix correto exige (a) injetar `ICurrentUser` + reusar a prova
I4 (`AutorizacaoDeConcessao.Verificar` + escopo efetivo do concedente), (b) o endpoint `POST /usuarios`
tratar `ConcessaoNaoAutorizadaException` → 403, e (c) atualizar os testes existentes de criação-com-papéis
que **não montam concedente** (hoje passariam a falhar na prova I4). O **seed do admin é seguro** (usa
`Usuario.Criar` direto — `IdentidadeModule.cs:218` — sem passar pelo handler).

**Fix recomendado:** extrair a prova I4 (`ProvarCoberturaI4Global`) para um serviço compartilhado
`Application/Autorizacao/` e chamá-lo em **CriarUsuario** e **DefinirPapeis**; adicionar o `catch` no
endpoint; cobrir com teste (concedente sem escopo → 403; com escopo → 201). Esforço: M.

## S2 — Race na rotação de certificado A1 (ALTA) — ✅ RESOLVIDO (2026-07-01)

**Onde:** `CertificadoA1CofreConfiguration.cs` + migration `CertificadoA1UnicoAtivoS2`.

**Era:** invariante "um único certificado Ativo por tenant" garantida só no fluxo (sem constraint no
banco) — duas rotações concorrentes podiam gravar 2 Ativos.

**Correção aplicada:** índice ÚNICO FILTRADO `(TenantId, Status)` com filtro `[Status] = 'Ativo'`
(portável Sqlite/SqlServer, mesmo padrão do AuditTrail) — o `SaveChanges` concorrente que criaria um
2º Ativo agora falha com violação de constraint. Verificado que **não quebra a rotação** (o EF ordena
o UPDATE do anterior→Substituído antes do INSERT do novo→Ativo; os 31 testes do Cofre seguem verdes).

## Deferidos da Onda 3 (já registrados)

- **R10** — JWT RS256 + refresh + revogação por `jti`: chaves RS256 são cred-gated (Key Vault) e o
  refresh/revogação mexe no fluxo de auth inteiro. Esforço G.
- **R8** — `IModelCacheKeyFactory` no filtro de tenant: defesa-em-profundidade sobre isolamento que
  **já funciona** (database-per-tenant, provado em E2E). Risco de regressão > ganho; tratar com cuidado.
- **R12** — `AllowedHosts` em produção: depende do domínio de produção real.
- **P10** — Azure SQL privado (bicep): IaC não-validável sem Azure; exige desenho de VNet/Private Endpoint.

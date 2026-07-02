# Pendências de segurança (backlog priorizado)

> Itens de segurança confirmados mas **deliberadamente não corrigidos numa leva rápida** por terem
> blast radius alto ou dependerem de credencial/infra. Documentados para tratamento dedicado.

## S1 — Escalação lateral de papéis na criação de usuário (ALTA) — ✅ RESOLVIDO (2026-07-01)

**Onde:** `src/Modules/Identidade/.../Application/Usuarios/CriarUsuario.cs` (handler).

**Problema:** `CriarUsuarioHandler` aceita `PapeisIds` iniciais e chama `Usuario.Criar(...)` com esses
papéis **sem validar a regra I4 ("não delega o que não tem")**. Diferentemente de
`DefinirPapeisDoUsuarioHandler` (que chama `ProvarCoberturaI4GlobalAsync`) e `AtribuirPapelHandler`, o
`CriarUsuario` só verifica que os papéis **existem**, não que o **concedente tem escopo para concedê-los**.
Um administrador com apenas `identidade.usuarios.gerenciar` pode criar um usuário já com papéis de
Finanças/Saúde que ele próprio não possui.

**✅ RESOLVIDO (2026-07-01):** o `CriarUsuarioHandler` agora injeta `ICurrentUser` + `TimeProvider` e
executa a MESMA prova I4 global do `DefinirPapeisDoUsuarioHandler` (`ProvarCoberturaI4GlobalAsync` +
`AutorizacaoDeConcessao.Verificar` sobre o escopo efetivo do concedente) para os papéis INICIAIS; o
endpoint `POST /usuarios` passou a mapear `ConcessaoNaoAutorizadaException` → 403. Blast radius foi
zero (nenhum teste construía `CriarUsuarioCommand`; o seed usa `Usuario.Criar` direto). Coberto por 2
testes (`AutorizacaoAdminUsuarioTests`: admin escopado nega; admin pleno concede). Suíte completa verde.

> Follow-up opcional (dívida técnica leve): a prova I4 hoje está replicada em `CriarUsuario` e
> `DefinirPapeis` — extrair para um serviço compartilhado `Application/Autorizacao/` numa próxima passada.

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

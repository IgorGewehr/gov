---
modulo: Identidade
agregado: Usuario
contexto: Identidade (RBAC+ABAC — principais autenticaveis e atribuicoes de papel com escopo de UO)
poder: Ambos
schema: identidade
ativavel_por_tenant: false
versao_regras: 1.0.0
fontes_legais: ["LGPD (Lei 13.709/2018) — minimizacao e trilha de acesso", "CLAUDE.md §5 (multi-tenancy) e §6 (AuthN/AuthZ, negar por padrao)"]
---

# Usuario — Regras-as-Code (Rules-as-Code)

> Principal autenticavel do tenant. Nasce com a senha ja transformada em **hash** (o dominio nunca ve a
> senha em claro) e carrega um conjunto de **atribuicoes de papel COM ESCOPO de Unidade Organizacional
> (UO)** — cada uma um par (papel, alcance de UOs) — que determinam suas permissoes efetivas (RBAC+ABAC).
> Apenas usuarios **ativos** podem autenticar. Este arquivo e **normativo e versionado**; o codigo
> (agregado `Usuario`, handlers, validators, EF config, testes) e consequencia dele.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Usuario (`Usuario`) | Principal autenticavel do tenant. Raiz de agregado. |
| Identidade do Usuario (`UsuarioId`) | `readonly record struct UsuarioId(Guid Value)`; fabrica `UsuarioId.New()`. |
| Email (`Email`) | VO de e-mail de login (validado); unico por tenant. |
| Senha-hash (`SenhaHash`) | Hash da senha (algoritmo + sal embutidos pela porta de hash). Nunca a senha em claro. |
| Atribuicao de Papel (`AtribuicaoDePapel`) | Par (papel, UO raiz, alcance de subunidades, vigencia, origem). Fonte de verdade do RBAC+ABAC do sujeito. |
| Papeis (`Papeis`) | Visao PLANA e DERIVADA (apenas os `PapelId` distintos, sem escopo) — compatibilidade. |
| Escopo (`UnidadeOrganizacionalId` + `IncluiSubunidades`) | UO raiz da atribuicao e se alcanca os descendentes. |
| Raiz Pendente (`UnidadeOrganizacionalId.RaizPendente`) | Sentinela de escopo global legado, re-ancorada na UO raiz real (migracao MODELO §10.2). |
| Vigencia (`Vigencia`) | Janela temporal da atribuicao (aberta ou com fim). |
| Origem (`OrigemAtribuicao`) | Procedencia da atribuicao (direta/delegada + concedente). |
| Tenant (`TenantId`) | Ente publico dono do registro. |

---

## 2. Modelo

- **Identidade:** `UsuarioId` — `readonly record struct UsuarioId(Guid Value)`; `UsuarioId.New()`.
- **Raiz de agregado:** `Usuario : AggregateRoot<UsuarioId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parametros para o EF; um completo). Nasce valido via factory `Criar(...)`.

### Propriedades

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant dono do registro. | `private set` |
| `Nome` | `string` | Nome de exibicao (max. `ComprimentoMaximoNome` = 200). | `private set` |
| `Email` | `Email` (VO) | E-mail de login (unico por tenant). | `private set` |
| `SenhaHash` | `string` | Hash da senha (nunca a senha em claro). | `private set` |
| `Ativo` | `bool` | Habilitado a autenticar. | `private set` |
| `Papeis` | `IReadOnlySet<PapelId>` | Visao plana DERIVADA das atribuicoes. | colecao encapsulada |
| `Atribuicoes` | `IReadOnlyCollection<AtribuicaoDePapel>` | Atribuicoes com escopo de UO (fonte de verdade). | colecao encapsulada |

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** `Criar` exige `email` nao nulo e `senhaHash` nao vazio; `Nome` nao vazio e ≤ 200; nasce `Ativo` e emite `UsuarioCriado(id, tenant)`.
- **I-2.** O dominio **nunca** recebe a senha em claro: `SenhaHash` ja vem calculado da porta de hash (em `Criar` e `TrocarSenha`).
- **I-3.** `Editar(nome, email)` valida nome/e-mail e emite `UsuarioEditado`.
- **I-4.** `Ativar`/`Desativar` sao **idempotentes**; `Desativar` emite `UsuarioDesligado(id, tenant)` (evento sensivel) e impede autenticacao.
- **I-5.** `AtribuirPapel` e **idempotente por escopo** (mesmo papel + mesma UO + mesmo alcance nao duplica); emite `PapelAtribuido`. A regra "nao delega o que nao tem" e o escopo do concedente sao validados na aplicacao/handler (I4 organizacional).
- **I-6.** `RevogarPapel` remove todas as atribuicoes do papel na UO dada; o papel so sai da visao plana quando nao restar nenhuma atribuicao dele; emite `PapelRevogado`.
- **I-7.** `DefinirPapeis` (ponte de compatibilidade MODELO §10.2) substitui integralmente as atribuicoes a partir de lista plana, ancorando-as na `RaizPendente` com `IncluiSubunidades=true`; emite `PapeisDoUsuarioDefinidos`.
- **I-8.** `ReancorarAtribuicoesPendentesNaRaiz(raizId)` e **idempotente**: re-ancora as atribuicoes pendentes na UO raiz real (preservando alcance global), sem apagar/duplicar; emite `AtribuicoesReancoradasNaRaiz` quando ha mudanca.
- **I-9.** `TrocarSenha(novoHash)` exige hash nao vazio e emite `SenhaTrocada` (evento sensivel — apenas o fato, nunca o hash).

---

## 4. Comandos (escrita)

### 4.1 CriarUsuario
- **Command:** `CriarUsuarioCommand(string Nome, string Email, string Senha, IReadOnlyCollection<Guid>? PapeisIds = null) : ICommand<Guid>`.
- **Efeito:** calcula hash; `Usuario.Criar(tenant, nome, email, hash, papeis)`; persiste. Retorna `UsuarioId.Value`.
- **Evento de dominio:** `UsuarioCriado` (+ `PapeisDoUsuarioDefinidos` se papeis iniciais).

### 4.2 EditarUsuario
- **Command:** `EditarUsuarioCommand(Guid UsuarioId, string Nome, string Email) : ICommand`.
- **Efeito:** `usuario.Editar(nome, email)`. **Evento:** `UsuarioEditado`.

### 4.3 AtivarUsuario / DesativarUsuario
- **Commands:** `AtivarUsuarioCommand(Guid UsuarioId) : ICommand`; `DesativarUsuarioCommand(Guid UsuarioId) : ICommand`.
- **Efeito:** `usuario.Ativar()` / `usuario.Desativar()` (idempotentes). **Eventos:** `UsuarioAtivado` / `UsuarioDesligado`.

### 4.4 AlterarSenha
- **Command:** `AlterarSenhaCommand(Guid UsuarioId, string NovaSenha) : ICommand`.
- **Efeito:** calcula hash; `usuario.TrocarSenha(hash)`. **Evento:** `SenhaTrocada`.

### 4.5 DefinirPapeisDoUsuario
- **Command:** `DefinirPapeisDoUsuarioCommand(Guid UsuarioId, IReadOnlyCollection<Guid> PapeisIds) : ICommand`.
- **Efeito:** `usuario.DefinirPapeis(papeisIds)`. **Evento:** `PapeisDoUsuarioDefinidos`.

### 4.6 AtribuirPapelAoUsuario
- **Command:** `AtribuirPapelAoUsuarioCommand(Guid UsuarioId, Guid PapelId, Guid UnidadeId, bool IncluiSubunidades, DateTimeOffset? VigenciaInicio, DateTimeOffset? VigenciaFim = null) : ICommand`.
- **Efeito:** valida escopo/delegacao do concedente (I4); `usuario.AtribuirPapel(...)` (idempotente por escopo). **Evento:** `PapelAtribuido`.

### 4.7 RevogarAtribuicao
- **Command:** `RevogarAtribuicaoCommand(Guid UsuarioId, Guid PapelId, Guid UnidadeId) : ICommand`.
- **Efeito:** `usuario.RevogarPapel(papelId, unidadeId)`. **Evento:** `PapelRevogado`.

---

## 5. Consultas (leitura)

### 5.1 ListarUsuarios
- **Query:** `ListarUsuariosQuery : IQuery<IReadOnlyList<UsuarioResumo>>`. Tenant-scoped via Global Query Filter.

### 5.2 ObterUsuario
- **Query:** `ObterUsuarioQuery(Guid UsuarioId) : IQuery<UsuarioDetalhe>`. Tenant-scoped.

### 5.3 ObterPermissoesEfetivasDoUsuario
- **Query:** `ObterPermissoesEfetivasDoUsuarioQuery(Guid UsuarioId) : IQuery<IReadOnlyList<string>>`. Resolve as permissoes efetivas (papeis × escopo). Tenant-scoped.

---

## 6. Eventos

### Dominio (in-process, MediatR; assembly `...Identidade.Domain`)

| Evento | Payload | Emitido por |
|---|---|---|
| `UsuarioCriado` | `(UsuarioId, Guid TenantId)` | `Usuario.Criar` |
| `UsuarioEditado` | `(UsuarioId)` | `Usuario.Editar` |
| `UsuarioAtivado` | `(UsuarioId)` | `Usuario.Ativar` |
| `UsuarioDesligado` | `(UsuarioId, Guid TenantId)` | `Usuario.Desativar` |
| `PapeisDoUsuarioDefinidos` | `(UsuarioId)` | `Usuario.DefinirPapeis` |
| `PapelAtribuido` | `(UsuarioId, PapelId, UnidadeOrganizacionalId, bool IncluiSubunidades)` | `Usuario.AtribuirPapel` |
| `PapelRevogado` | `(UsuarioId, PapelId, UnidadeOrganizacionalId)` | `Usuario.RevogarPapel` |
| `SenhaTrocada` | `(UsuarioId)` | `Usuario.TrocarSenha` |
| `AtribuicoesReancoradasNaRaiz` | `(UsuarioId, UnidadeOrganizacionalId RaizId)` | `Usuario.ReancorarAtribuicoesPendentesNaRaiz` |

> Identidade **nao publica nem consome Integration Events** (nao ha Outbox de contrato neste agregado).

---

## 7. Seguranca, Tenant e Auditoria

- **Tenant:** `Usuario` implementa `IMustHaveTenant`; `TenantId` carimbado na insercao; **Global Query Filter** por `TenantId`. Gravacao cross-tenant lanca excecao.
- **AuthZ (negar por padrao):** gerir usuarios/papeis/atribuicoes exige permissao administrativa de Identidade; `AtribuirPapel` respeita o escopo do concedente (I4 — nao delega o que nao tem).
- **Auditoria imutavel:** `AuditSaveChangesInterceptor` grava trilha (antes/depois, usuario, IP, timestamp) em toda mutacao. `UsuarioDesligado`, `PapelAtribuido`, `PapelRevogado`, `SenhaTrocada` e `AtribuicoesReancoradasNaRaiz` sao **sensiveis**.
- **LGPD:** senha **nunca** em claro no dominio; `SenhaTrocada` registra apenas o fato. Trilha de acesso minimiza dados pessoais.
- **Anti-SQLi:** acesso via EF parametrizado.

---

## 8. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versao inicial — derivada do agregado `Usuario` e do RBAC+ABAC do modulo Identidade (P0 governanca: SpecCodeConsistency). |

<!-- manifest
commands: CriarUsuario, EditarUsuario, AtivarUsuario, DesativarUsuario, AlterarSenha, DefinirPapeisDoUsuario, AtribuirPapelAoUsuario, RevogarAtribuicao
queries: ListarUsuarios, ObterUsuario, ObterPermissoesEfetivasDoUsuario
domainEvents: UsuarioCriado, UsuarioEditado, UsuarioAtivado, UsuarioDesligado, PapeisDoUsuarioDefinidos, PapelAtribuido, PapelRevogado, SenhaTrocada, AtribuicoesReancoradasNaRaiz
integrationEventsPublished: 
integrationEventsConsumed: 
-->

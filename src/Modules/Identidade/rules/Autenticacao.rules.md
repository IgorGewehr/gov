---
modulo: Identidade
agregado: Autenticacao
contexto: Identidade (AuthN — login por e-mail/senha e emissao de token)
poder: Ambos
schema: identidade
ativavel_por_tenant: false
versao_regras: 1.0.0
fontes_legais: ["CLAUDE.md §6 (AuthN JWT Bearer; negar por padrao)", "OWASP ASVS — credenciais e enumeracao de contas"]
---

# Autenticacao — Regras-as-Code (Rules-as-Code)

> Caso de uso de **autenticacao** (AuthN): valida e-mail/senha dentro de um tenant e emite o token de
> acesso (JWT) com as **permissoes efetivas** embutidas. **Seguranca critica**: mensagem de falha
> uniforme (nao revela se foi e-mail inexistente, senha incorreta ou conta inativa) e verificacao de
> hash mesmo quando o usuario nao existe (mitiga enumeracao por timing). Este arquivo e **normativo e
> versionado**.

---

## 1. Linguagem Ubiqua

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Autenticar (`AutenticarCommand`) | Caso de uso de login (e-mail + senha + tenant). |
| Resultado da Autenticacao (`ResultadoAutenticacao`) | DTO de retorno: usuario, tenant, nome, e-mail, permissoes efetivas e token. |
| Token Emitido (`TokenEmitido`) | Token de acesso (JWT) emitido pelo `IEmissorToken`. |
| Permissoes Efetivas (`PermissoesEfetivas`) | Uniao das permissoes dos papeis, embutida no token. |
| Senha-hasher (`ISenhaHasher`) | Porta de verificacao/derivacao de hash de senha. |

---

## 2. Comando (escrita)

### 2.1 Autenticar
- **Command:** `AutenticarCommand(Guid TenantId, string Email, string Senha, string? TenantNome = null) : ICommand<ResultadoAutenticacao>`.
- **Dependencias do handler:** `IUsuarioRepository`, `IPapelRepository`, `IUnidadeRepository`, `ISenhaHasher`, `IEmissorToken`, `TimeProvider`.
- **Pre-condicoes:** `TenantId`, `Email` e `Senha` nao vazios (validador).
- **Efeito:** resolve o usuario por `(TenantId, Email)`; verifica o hash da senha; exige usuario **ativo**; calcula permissoes efetivas (papeis × escopo); emite token via `IEmissorToken`.
- **Pos-condicoes:** retorna `ResultadoAutenticacao` (token + permissoes efetivas).
- **Falha:** `AutenticacaoFalhouException` com mensagem **generica** ("Credenciais invalidas.") para e-mail inexistente, senha incorreta **ou** conta inativa. Verificacao de hash executada mesmo sem usuario (timing estavel).

> Nao emite eventos de dominio nem de integracao.

---

## 3. Validacao (FluentValidation)

### AutenticarValidator (`AbstractValidator<AutenticarCommand>`)

| Campo | Regra |
|---|---|
| `TenantId` | `NotEmpty()` |
| `Email` | `NotEmpty()` |
| `Senha` | `NotEmpty()` |

---

## 4. Seguranca, Tenant e Auditoria

- **Tenant:** a autenticacao e sempre escopada a um `TenantId`; usuarios resolvidos sob o Global Query Filter.
- **AuthN:** JWT Bearer; o token carrega `tenant_name` (quando informado) e as permissoes efetivas.
- **Anti-enumeracao:** mensagem de falha uniforme + verificacao de hash com `SenhaFalsaParaTimingEstavel` quando o usuario nao existe.
- **LGPD/Seguranca:** senha em claro nunca persistida nem logada; somente verificada e descartada.

---

## 5. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versao inicial — derivada do caso de uso `Autenticar` do modulo Identidade (P0 governanca: SpecCodeConsistency). |

<!-- manifest
commands: Autenticar
queries: 
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->

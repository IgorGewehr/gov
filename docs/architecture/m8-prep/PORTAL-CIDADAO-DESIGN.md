# Portal do Cidadão ("Minha Cidade") — DESIGN pronto-para-implementar (M8 · Frente A)

> **Arquiteto do M8 / Frente A.** Design técnico do **Portal do Cidadão** do Tensorroot.Gov. O cidadão é um **ator EXTERNO** (não é servidor, não pertence ao RBAC organizacional do §6): tem **autenticação própria** e enxerga **somente o que é dele**, com isolamento dado-próprio **à prova de bala reusando o padrão do Minha Folha** (RH).
>
> **Autoridade:** `docs/architecture/m8-prep/M8-DESIGN.md` (Frente A) + código dos módulos. Este doc **detalha e operacionaliza** a Frente A do M8-DESIGN no nível de implementação, alinhado às regras do `CLAUDE.md` (Clean Arch/DDD §2; `IMustHaveTenant` §5; auditoria/trilha de acesso §6; cross-module só via `*.Contracts` §2; deny-by-default §6; nada hardcoded §7; gov.br DS + WCAG §13).
>
> **Achado-chave (ancora tudo):** o autosservico do servidor já existe e é o **molde exato** a replicar. Ler antes de codar:
> - `…/RecursosHumanos.Domain/Servidores/VinculoServidorUsuario.cs` — agregado-ancora `(TenantId, UsuarioId)→ServidorId`, `IMustHaveTenant`, par único nos dois sentidos.
> - `…/RecursosHumanos.Application/Internal/ResolvedorServidorDoUsuarioAutenticado.cs` — lê o `sub` do `ICurrentUser`, resolve o servidor PELO VÍNCULO, **nunca aceita id do cliente**; sem vínculo → sela negativa LGPD + lança `UsuarioSemVinculoServidorException`.
> - `…/RecursosHumanos.Application/MinhaFolha/ObterMeuContracheque.cs` — handler obtém o id pela ancora e filtra só o dado-próprio; `ISensivelLgpd` (trilha de acesso).
> - `…/Tributos.Domain/Contribuintes/Contribuinte.cs` — **`Contribuinte` é `IMustHaveTenant` + `Documento` (CPF/CNPJ sem máscara)**: é o "Servidor" do lado tributário — o alvo natural do vínculo do cidadão.
> - `…/Identidade.Application/Autenticacao/Autenticar.cs` + `…/Identidade.Domain/Usuarios/Usuario.cs` — login email+senha interno (servidor/RBAC); **não tem CPF nem tipo externo**.

---

## 0. Descoberta arquitetural crítica (ler antes de tudo)

1. **`Contribuinte` é a "pessoa-própria" do cidadão.** `Tributos.Domain/Contribuintes/Contribuinte` já é `IMustHaveTenant`, chaveado por `Documento` (CPF/CNPJ em dígitos) + `TipoPessoa`. É o equivalente exato do `Servidor` no Minha Folha → o vínculo do cidadão será `(TenantId, UsuarioCidadaoId) → ContribuinteId`. **CONFIANÇA: ALTA** (código lido).

2. **Os `*.Contracts` de Tributos/Protocolo/Transparencia hoje só têm Integration Events — não há DTO/Query de leitura cidadã.** Tributos.Contracts = `ReceitaArrecadada…`, `PosicaoDividaAtiva…`; Protocolo.Contracts = `ProcessoAutuado/Tramitado/Arquivado`, `DocumentoAssinado`; Transparencia.Contracts = remessas/MSC/prazo. **Falta o lado de leitura por CPF/processo.** Cada serviço do portal precisa de **uma query nova no módulo-fonte** (não duplicar lógica no Cidadao). **CONFIANÇA: ALTA** (lista de Contracts verificada).

3. **`Identidade` é do servidor, não do cidadão.** `Usuario` = email+senha+papéis RBAC, `IMustHaveTenant`; `AutenticarCommand(TenantId, Email, Senha)` emite JWT com `sub`+`tenant`+permissões. Não há tipo "externo", nem login por CPF, nem cadastro self-service. **Decisão:** o cidadão **NÃO** reusa `Usuario` interno (poluiria o RBAC e o universo de contas administrativas). Cria-se um **principal externo próprio** (`CidadaoConta`), mas **reusando a mesma infra de JWT/hash/`ICurrentUser`** para que o resolvedor dado-próprio funcione idêntico ao Minha Folha. **CONFIANÇA: MÉDIA** (decisão de produto; alternativa "flag externo no Usuario" descartada por contaminar RBAC).

4. **O Portal é fachada de LEITURA + disparo de Commands existentes.** Zero regra de tributo/protocolo no módulo `Cidadao`. Tudo que ele faz: autenticar o cidadão, resolver a pessoa-própria pelo `sub`, e delegar ao módulo-fonte (via Contracts) passando **a pessoa resolvida server-side**. **CONFIANÇA: ALTA** (princípio M8-DESIGN A.3 + §2).

---

## 1. AUTENTICAÇÃO do cidadão (ator externo)

### 1.1 Princípio
O cidadão é **externo**: nunca entra no RBAC organizacional (UO/papéis/§6). Autentica em um **realm próprio** e recebe um JWT cujo `sub` é a **conta-cidadão**, não um `Usuario` interno. O isolamento por tenant continua valendo: a conta-cidadão é `IMustHaveTenant` (o cidadão do município X não vê dado do município Y).

### 1.2 Novo módulo `Cidadao` (Bounded Context, schema `cidadao`)
Agregado-raiz **`CidadaoConta`** (`IMustHaveTenant`), molde no `Usuario` mas enxuto e externo:

| Campo | Tipo | Observação |
|---|---|---|
| `Id` | `CidadaoContaId` (Guid forte) | vira o `sub` do JWT do cidadão |
| `TenantId` | `Guid` | `IMustHaveTenant` — isolamento por município |
| `Documento` | `Cpf`/`Cnpj` (VO do SharedKernel) | identidade civil; **par único `(TenantId, Documento)`** |
| `Nome` | `string` | |
| `Email` / `Telefone` | VO | contato/recuperação |
| `SenhaHash` | `string` | **reusa `ISenhaHasher` do Identidade** (mesma porta de hash) |
| `Origem` | enum `{CadastroLocal, GovBr}` | proveniência da identidade |
| `SeloGovBr` | enum? `{Bronze,Prata,Ouro}` | nulo no cadastro local; vem do `id_token` no fluxo gov.br |
| `Ativo` / `EmailConfirmado` | `bool` | deny-by-default: inativo não autentica |

Factory privada + `Criar…`, invariantes no nascimento (CPF/CNPJ válido pelo VO; senha já em hash — domínio nunca vê senha em claro, como no `Usuario`). Trilha de auditoria imutável herdada (interceptors §6).

### 1.3 Login local por CPF/CNPJ + senha (entregável agora)
- `RegistrarCidadaoCommand(Documento, Nome, Email, Senha)` → valida VO, hash via `ISenhaHasher`, cria `CidadaoConta` com `Origem=CadastroLocal`, dispara confirmação de e-mail. **Anti-enumeração:** mesma técnica do `AutenticarHandler` (resposta uniforme, hash sintético quando não existe).
- `AutenticarCidadaoCommand(TenantId, Documento, Senha)` → handler espelha o `AutenticarHandler` (mensagem genérica `Credenciais invalidas`, verificação de hash mesmo sem conta, checa `Ativo`). Emite JWT por um **`IEmissorTokenCidadao`** (nova porta, ou reuso de `IEmissorToken` com claim `tipo=cidadao` e **sem** permissões RBAC).
- **Claims do JWT do cidadão:** `sub` = `CidadaoContaId`; `tenant` = `TenantId`; `tipo=cidadao`; `selo` (se gov.br). **Sem** as permissões organizacionais — o cidadão não tem papel no RBAC.

### 1.4 Gancho gov.br / OIDC (// TODO-creds — bloqueador externo)
- ApiHost como **Relying Party** OIDC (authorization code) tendo o **gov.br como IdP**; valida `id_token` contra JWKS do gov.br; lê **selo** (bronze/prata/ouro) e o **CPF**; faz *match*/provisionamento de `CidadaoConta` por `(TenantId, Documento)` com `Origem=GovBr`. **Atrás de ACL + Polly + Outbox** (§8/§11), como toda integração governamental.
- **`// TODO-creds`:** client_id/secret, endpoints, JWKS e **status de adesão do município à Rede gov.br** (bloqueador de cronograma — M8-DESIGN A.1/E.1). Encapsular em `IProvedorIdentidadeGovBr` (porta) + `IOptions<GovBrOptions>`; **stub** no go-live se o ente não aderiu. Gating por selo (consulta=anônimo/bronze; protocolo/dado pessoal=prata; ato forte=ouro) é **parametrizável por tenant** (`IOptions`, nunca hardcoded — §7).

### 1.5 Isolamento dado-próprio à prova de bala (REUSO do padrão Minha Folha)
**Porta-âncora (nova, em `Cidadao.Application.Abstractions`):**
```
IResolvedorPessoaDoCidadaoAutenticado
  Task<ContribuinteRef> ResolverContribuinteAtualAsync(CancellationToken ct);
```
Implementação **idêntica** ao `ResolvedorServidorDoUsuarioAutenticado`:
1. lê `sub` do `ICurrentUser` (já existe; serve tanto servidor quanto cidadão);
2. resolve `(TenantId, sub)→Documento/Contribuinte` por um repositório-âncora (`IVinculoCidadaoContribuinteRepository` ou direto do `CidadaoConta.Documento`);
3. **se não há vínculo/conta → sela NEGATIVA na trilha LGPD (`IRegistroAcessoSensivel.RegistrarNegacaoAsync`) e lança `CidadaoSemVinculoException`** (deny-by-default);
4. **NUNCA recebe id do cliente** — o `Documento`/`ContribuinteId` é SEMPRE derivado do principal.

**Regra de ouro:** todo handler de "meus dados" começa por `ResolverContribuinteAtualAsync(ct)` e só consulta dados dessa pessoa. O cliente jamais envia CPF/inscrição/processoId arbitrário; quando envia um id (ex.: `processoId`), o handler **revalida a titularidade server-side** contra a pessoa resolvida (defesa contra IDOR).

**Cross-module:** o `Cidadao` resolve a pessoa, mas a **ligação `sub`→`Documento`** mora no próprio `CidadaoConta` (o `Documento` é dado civil do cidadão, não interno de Tributos). Para falar com Tributos/Protocolo, o `Cidadao` chama **apenas Contracts** passando o `Documento`/`ContribuinteId` já resolvido — nunca lê entidade interna de outro módulo (§2). O módulo-fonte ainda aplica seu próprio `IMustHaveTenant`.

---

## 2. FUNCIONALIDADES (consumindo o que já existe via Contracts)

> Para cada serviço: **reusa** (o que já há) · **falta no backend** (query/Contract/endpoint a criar). Padrão de todos: handler no **módulo-fonte**, exposto via **`*.Contracts` (DTO + interface de query)**, chamado pelo `Cidadao` com a **pessoa resolvida server-side**. Endpoints do cidadão sob grupo `/cidadao/*` com policy própria (ver §3).

### 2.1 Meus débitos / lançamentos (Tributos)
- **Reusa:** agregados `Lancamento` (tipo/situação/valor, vínculo `ContribuinteId`), `Contribuinte` (chave por `Documento`). Estado `SituacaoLancamento` (Aberto/Pago/InscritoEmDividaAtiva) já modelado.
- **Falta no backend:** `Tributos.Contracts.IConsultaTributariaCidadao` com `ObterMeusLancamentosAsync(ContribuinteRef, filtro)` → `MeuLancamentoDto[]` (tributo, competência, vencimento, valor atualizado, situação). Read-only; filtra por `ContribuinteId` resolvido. **Esforço: M** (query + DTO no Contract + mapeamento; encargos/atualização monetária já existem no domínio de Dívida).

### 2.2 2ª via de DAM / boleto
- **Reusa:** agregado `Dam` (cota única OU N parcelas, vencimentos, `ContribuinteId`, `ValorTotal`) — já existe.
- **Falta no backend:** (a) query `ObterMeusDamsAsync` / `ObterDamPorIdAsync(damId)` com **revalidação de titularidade** (o `Dam.ContribuinteId` tem de bater com a pessoa resolvida — anti-IDOR); (b) **renderização do documento** (PDF/linha digitável). Se não há gerador de PDF/CIP no Tributos, é **trabalho novo** (template DAM + cálculo de linha digitável/QR Pix). **Esforço: M–G** (query M; geração de boleto/linha digitável/Pix G se inexistente — *confirmar no código de Arrecadacao antes de estimar*).

### 2.3 Situação da minha dívida ativa
- **Reusa:** `DividaAtiva` (situações Inscrita/CdaEmitida/Protestada/EmExecucaoFiscal/Parcelada/Quitada/Cancelada), `CertidaoDividaAtiva`, regra de encargos/prescrição já no domínio; evento `PosicaoDividaAtivaIntegrationEvent` existe.
- **Falta no backend:** query `ObterMinhaDividaAtivaAsync(ContribuinteRef)` → posição consolidada por CDA (valor originário + encargos atualizados + situação + flag de parcelamento). **Adesão a parcelamento** = disparo de **Command já existente no Tributos** (não criar no Cidadao) com a pessoa resolvida; gating **prata** (gov.br) por ser ato com efeito. **Esforço: M** (query) · **adesão a parcelamento: depende de o Command existir** — *confirmar*; senão G.

### 2.4 Meus processos / protocolos (Protocolo)
- **Reusa:** `Processo` (NUP, classificação, **`NivelDeAcesso`**, situação, movimentações, despachos), eventos `ProcessoAutuado/Tramitado/Arquivado`. Já há `OrigemModulo/OrigemId` (liga processo a origem) e `NivelAcesso` (respeitar: processo sigiloso não vai para o portal mesmo sendo do interessado, salvo regra).
- **Falta no backend:** **(crítico)** o `Processo` **não tem campo de interessado/CPF** no domínio (grep não achou `Cpf/Documento` em `Processo.cs`). Para "meus processos" é preciso um **`InteressadoDocumento`/`InteressadoCidadaoId` no `Processo`** (campo novo + migração) **ou** uma tabela de associação `ProcessoInteressado`. Depois: query `Protocolo.Contracts.IConsultaProcessoCidadao.ObterMeusProcessosAsync(ContribuinteRef)` + `ObterMeuProcessoAsync(processoId)` com **revalidação de titularidade e de `NivelAcesso`**. **Abrir processo** = Command de autuação já existente, passando o interessado resolvido (gating **prata**). **Esforço: G** (modelagem de interessado + migração + queries + respeitar nível de acesso).

### 2.5 Acesso à transparência (LAI) — público anônimo
- **Reusa:** módulo `Transparencia` (M8-DESIGN C.1/C.2 estende-o para publicidade LAI + dados abertos). É **público/anônimo** — **não passa pelo resolvedor dado-próprio** (não pode ter barreira de login; M8-DESIGN C.2/C.3).
- **No portal:** o cidadão logado vê os mesmos dados abertos + atalhos; e-SIC (transparência passiva) usa identificação básica, **login gov.br opcional/auxiliar** (M8-DESIGN C.3 — registrar no decision journal "e-SIC sem login obrigatório"). **Esforço:** front M; backend já coberto pela Frente C do M8-DESIGN (fora do escopo desta frente A, mas o portal consome).

> **Resumo do que falta no backend (todos pequenos/médios, exceto interessado de processo e geração de boleto):** uma **interface de query + DTO em cada `*.Contracts`** (Tributos: lançamentos, DAM, dívida; Protocolo: meus processos) + **campo interessado no `Processo`** (migração) + **gerador de DAM/linha digitável** se inexistente. Nenhuma lógica de negócio nova — só **leitura por pessoa** e revalidação de titularidade.

---

## 3. SEGURANÇA / LGPD

- **Deny-by-default (§6):** endpoints `/cidadao/*` exigem JWT de cidadão (`tipo=cidadao`) por policy própria (`PortalCidadaoPolicy`), distinta das permissões RBAC. Sem conta/sem vínculo → `CidadaoSemVinculoException` (403) + negativa selada na trilha. Serviços públicos (transparência/dados abertos) são a **única** rota anônima.
- **Dado-próprio à prova de bala:** a pessoa é SEMPRE resolvida do `sub` (§1.5); ids vindos do cliente (`damId`, `processoId`) são **revalidados contra a pessoa resolvida** (anti-IDOR). `IMustHaveTenant` em `CidadaoConta` + Global Query Filter garantem que o cidadão de um município não cruze para outro.
- **Trilha de acesso LGPD (§6):** toda query "meus dados" marca `ISensivelLgpd` (entidade + `EntidadeId=null` porque o id é server-side) com **base legal `ExercicioDeDireitos`** (titular acessando os próprios dados — art. 18 LGPD) e/ou `ObrigacaoLegal` (fornecer 2ª via/CDA). O `TrilhaAcessoSensivelBehavior` sela acesso e negativa na trilha imutável — **idêntico ao Minha Folha** (`BasesLegaisAutosservico`). Criar `BasesLegaisPortalCidadao` (FrozenSet) análogo.
- **Minimização:** o portal expõe só o estritamente necessário (valor/situação/2ª via); não devolve dados de terceiros nem o cadastro tributário completo. Transparência ativa segue o **LGPD gate** da Frente C (mascaramento antes de publicar) — o portal nunca publica dado pessoal de outrem.
- **Anti-enumeração** no login/cadastro (reuso da técnica do `AutenticarHandler`). Confirmação de e-mail e (recomendado) 2FA para atos `prata`. Segredos gov.br só no Key Vault (§5).

---

## 4. Sequência de sub-workflows (priorizada por dependência e risco)

> Racional: entregar **dado-próprio com login local** cedo (sem depender da adesão gov.br); isolar o **bloqueador externo (gov.br RP)** no fim; reusar ao máximo o molde Minha Folha. Honestidade de esforço marcada por item.

**Backend**
1. **`B1` Fundação `Cidadao` + identidade externa.** Módulo `Cidadao` (`IModule`, schema `cidadao`, DbContext+Outbox+interceptors); `CidadaoConta` (reusa `ISenhaHasher`); `Registrar`/`AutenticarCidadao` (espelham Identidade); emissor de JWT do cidadão (claim `tipo=cidadao`, sem RBAC); **resolvedor dado-próprio `IResolvedorPessoaDoCidadaoAutenticado`** (cópia fiel do RH) + trilha de negativa. **Não depende de gov.br.** **Esforço: M.**
2. **`B2` Queries de leitura nos módulos-fonte (Contracts).** Tributos: `IConsultaTributariaCidadao` (lançamentos, DAM, dívida ativa). Cada uma read-only, por `ContribuinteRef` resolvido, revalidando titularidade. **Esforço: M** (sem boleto).
3. **`B3` Geração de 2ª via DAM** (PDF + linha digitável/QR Pix) — **só se não existir** no Arrecadacao. **Esforço: M–G** (*confirmar antes*).
4. **`B4` Interessado no `Processo` + query "meus processos".** Campo/associação `Interessado` no `Processo` (migração) + `IConsultaProcessoCidadao` respeitando `NivelDeAcesso`; abertura/acompanhamento via Commands existentes. **Esforço: G** (migração de domínio).
5. **`B5` Adesão a parcelamento via portal** — reuso de Command existente do Tributos (gating prata). **Esforço: M se o Command existe; senão G.**
6. **`B6` gov.br RP / OIDC** — `IProvedorIdentidadeGovBr` + validação JWKS + selo + provisionamento `Origem=GovBr` + gating parametrizável. **DEPENDE de `// TODO-creds` + adesão do município (bloqueador externo).** Até lá, login local cobre todo o portal. **Esforço: G + bloqueio externo.**

**Frontend** (gov.br DS + eMAG + WCAG 2.1 AA — §13; molde nas telas `MinhaFolha*` do `src/Web/src/modules/recursoshumanos`)
7. **`F1` Shell "Minha Cidade"** — área pública/cidadão separada do app administrativo (rota + auth-context próprio do cidadão; reusa `src/Web/src/auth`), tela de login/cadastro por CPF, acessibilidade desde o nascimento (VLibras, axe-core já no projeto). **Esforço: M.**
8. **`F2` Meus débitos + 2ª via DAM** (consome B2/B3). **Esforço: M.**
9. **`F3` Minha dívida ativa (+ parcelamento)** (B2/B5). **Esforço: M.**
10. **`F4` Meus processos / protocolos** (B4). **Esforço: M.**
11. **`F5` Atalhos de transparência/LAI/dados abertos** (Frente C). **Esforço: P–M.**
12. **`F6` Botão gov.br + selo** no login (B6) — atrás de feature flag até as creds. **Esforço: M.**

---

## 5. Pendências (`// TODO-creds` e decisões — §16)
- **Bloqueador externo:** adesão do município à Rede gov.br + client_id/secret/JWKS/endpoints (B6/F6). Sem isso, **login local cobre 100% do portal** no go-live.
- **Confirmar no código antes de estimar fino:** existe gerador de PDF/linha digitável/Pix em `Tributos…Arrecadacao` (afeta B3)? existe Command de adesão a parcelamento (afeta B5)? `Processo` realmente não guarda interessado (afeta B4 — grep indica que não).
- **Decisão a registrar (decision journal):** (a) cidadão = principal externo próprio (`CidadaoConta`), não `Usuario` RBAC; (b) e-SIC/transparência sem login obrigatório; (c) tabela de gating selo×serviço como `IOptions` por tenant.

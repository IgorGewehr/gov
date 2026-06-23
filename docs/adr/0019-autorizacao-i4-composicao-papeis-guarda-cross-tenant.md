# ADR-0019 — Enforcement I4 na composição/atribuição de papéis + guarda cross-tenant (permissão de plataforma fora de `Permissoes.Todas`)

- **Status:** Aceito (fecha o escalonamento por papéis e o cross-tenant A→B — achados AA-1/AA-2 e XT-1)
- **Data:** 2026-06-22
- **Código:** `Modules/Identidade/.../Domain/Usuarios/{AutorizacaoDeConcessao,AutorizacaoDeComposicaoDePapel,ConcessaoNaoAutorizadaException}.cs`,
  `Application/Papeis/{CriarPapelHandler,DefinirPermissoesDoPapelHandler}.cs`,
  `Application/Usuarios/{AtribuirPapelAoUsuarioHandler,DefinirPapeisDoUsuarioHandler}.cs`,
  `Domain/Permissoes/Permissoes.cs`, `ApiHost/Admin/AdminEndpoints.cs`

## Contexto

O ADR-0007 fixou o **modelo** (RBAC dinâmico + ABAC organizacional + regra-mãe **I4** "não delega o
que não tem"). Faltava **fechar o enforcement** de duas vias concretas de escalonamento:

1. **Escalonamento por papéis (AA-1/AA-2):** ao **compor** um papel (criar/definir permissões) ou
   **atribuí-lo** a um usuário, nada impedia um concedente de empacotar/conceder permissões que ele
   **próprio não possui** — burlando o I4 se a checagem ficasse só na UI.
2. **Cross-tenant A→B (XT-1):** permissões de **plataforma** (provisionar tenants; licenciar módulos
   gravando na tabela de controle `TenantModule` de um tenant arbitrário da rota) são ações do
   **operador da plataforma**, não do administrador de um tenant. Se essas permissões integrassem o
   catálogo de tenant, o papel "Administrador" de **A** poderia operar **B**.

## Decisão

Provar o I4 **no domínio** (não na UI) e tirar a permissão de plataforma do catálogo de tenant:

- **I4 na composição.** `CriarPapelHandler` e `DefinirPermissoesDoPapelHandler` chamam
  `AutorizacaoDeComposicaoDePapel.Verificar`: para **cada** permissão do papel, o compositor deve
  possuí-la cobrindo o **tenant inteiro**; senão `ConcessaoNaoAutorizadaException` (HTTP 403 com
  motivo claro, *deny-by-default*, nunca silencioso).
- **I4 na atribuição.** `AtribuirPapelAoUsuarioHandler` e `DefinirPapeisDoUsuarioHandler` chamam
  `AutorizacaoDeConcessao.Verificar`, que exige (a) poder administrativo (`identidade.usuarios.gerenciar`),
  (b) administrar a UO alvo (e subárvore), (c) **possuir cada permissão do papel no escopo alvo**
  (`EscopoEfetivo.CobreEscopo`). Reprova → 403 com motivo.
- **Permissão de plataforma fora de `Permissoes.Todas`.** O catálogo canônico `Permissoes.Todas`
  **exclui deliberadamente** `PlataformaTenantsProvisionar` e `PlataformaModulosConfigurar` — o papel
  "Administrador" de um tenant **nunca** as recebe (deny-by-construction).
- **Guarda cross-tenant defesa-em-profundidade.** Os endpoints `/admin` exigem a permissão de
  plataforma **e** aplicam `DeveNegarOperacaoCrossTenant`: se há tenant resolvido no contexto
  (principal de tenant) e o tenant da **rota** é outro, **403 auditado**. Operador de plataforma sem
  tenant ligado (onboarding) passa.

## Alternativas consideradas

- **Checar I4 só na UI/Application sem provar no domínio:** burlável por chamada direta à API;
  contraria "provado no domínio" do ADR-0007. Rejeitado.
- **Manter as permissões de plataforma dentro do catálogo e confiar só no endpoint:** uma única
  brecha de atribuição vazaria poder de plataforma a um tenant. Excluí-las de `Permissoes.Todas`
  fecha por construção. Rejeitado manter no catálogo.
- **Super-admin global de cliente:** viola a separação de poderes por tenant (ADR-0007). Rejeitado —
  operador de plataforma é papel distinto, sem tenant ligado.
- **Confiar apenas no Global Query Filter para barrar A→B:** o filtro protege **leitura** de dados de
  negócio, mas a tabela de controle (`TenantModule`) é por rota; precisa de guarda explícita +
  exclusão do catálogo. Rejeitado isolado.

## Consequências

- ➕ Escalonamento por papéis **fechado no domínio**: ninguém compõe/concede além das próprias
  permissões efetivas; reprovas retornam **403 com motivo** (auditável, não silencioso).
- ➕ Poder de **plataforma é inalcançável** por papel de tenant (fora do catálogo) **e** barrado por
  guarda cross-tenant auditada — defesa em profundidade sobre a separação de poderes (ADR-0007).
- ➕ Casa com o isolamento multi-tenant (CLAUDE.md §5) e com a auditoria (a tentativa cross-tenant é
  registrada com IP/sub).
- ➖ **Custo de verificação** a cada composição/atribuição (resolver escopo efetivo do concedente +
  cobertura por permissão); mitigado por cache de escopo (ADR-0007).
- ➖ Migração: a ponte legada (`DefinirPapeisDoUsuario`, escopo global) **também** passa a exigir
  cobertura I4 — operações que antes "passavam" podem reprovar; previsto e coberto por teste.
- 🔗 Aprofunda o enforcement do ADR-0007; complementa o isolamento de banco por tenant (ADR-0005).

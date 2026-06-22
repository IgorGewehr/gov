# ADR-0006 — Autenticação por JWT self-issued (HS256), sem IdP externo

- **Status:** Aceito
- **Data:** 2026-06-22

## Contexto

O sistema precisa autenticar servidores municipais e carregar, por requisição, o
`ITenantContext` e as permissões efetivas (deny-by-default na borda). Há a opção de
delegar a autenticação a um *Identity Provider* externo (gov.br, Azure AD/Entra,
Keycloak) via OIDC, ou de o próprio `ApiHost` emitir e validar os tokens.

Restrições do contexto: piloto em município pequeno (Maximiliano de Almeida/RS), sem
infraestrutura de IdP existente; necessidade de **controle total** sobre o conteúdo do
token (claims de tenant, permissões); simplicidade operacional; e a regra de que o
JWT carrega o tenant que dirige o `ITenantConnectionResolver` (ADR-0005).

## Decisão

O `ApiHost` é o **emissor e validador** dos próprios JWTs (*self-issued*):

- Algoritmo **HS256 (HMAC-SHA256)** com **segredo simétrico** — `EmissorToken` usa
  `SymmetricSecurityKey` + `SigningCredentials(HmacSha256)`, `JwtSecurityToken` com
  `Issuer`/`Audience` próprios (`Modules/Identidade/.../Seguranca/EmissorToken.cs`).
- O segredo (`Jwt:Secret`) vem de **configuração**; em PRODUÇÃO, **do Azure Key Vault** —
  nunca do repositório; emissão bloqueada se ausente.
- Claims: `sub`, `tenant_id`/`tenant_name` e uma claim `perm` por permissão efetiva
  (catálogo canônico). Enforcement por política `perm:<escopo>`, deny-by-default.
- **Sem IdP externo, sem fluxo OIDC** nesta fase.

## Alternativas consideradas

- **OIDC com gov.br/Entra/Keycloak (RS256, chaves assimétricas via JWKS):** padrão para
  federação e SSO; chave pública distribuível; rotação por JWKS. Rejeitado **por ora**:
  adiciona dependência operacional e de credenciamento que o piloto não justifica, e o
  emissor e o validador são o **mesmo** serviço (assimetria traz pouco ganho hoje).
- **RS256 self-issued (par de chaves próprio):** evita segredo compartilhado, melhor se
  múltiplos validadores surgirem. Considerado caminho de evolução, não necessário agora.
- **Sessão server-side / cookies opacos:** não casa com o frontend SPA + API-first (ADR-0004)
  nem com Workers que resolvem tenant por iteração.

## Consequências

- ➕ Simplicidade e **controle total** do token (claims de tenant/permissão sob medida);
  zero dependência externa de autenticação; onboarding de ente sem integração com IdP.
- ➕ Mesmo serviço emite e valida → HS256 é adequado e leve.
- ➖ **Segredo simétrico** é ponto único: quem o tem forja tokens. Mitigado por custódia no
  Key Vault, deny-by-default e (futuro) rotação. **Não** distribuir o segredo a terceiros.
- ➖ **Sem SSO/federação** com gov.br — divergência consciente de roadmaps "govtech" que
  assumem login gov.br. É um caminho de evolução (migrar para RS256 + OIDC) que a separação
  emissor/validador deixa aberto sem reescrita do enforcement.
- 🔗 A resolução de escopo organizacional (UO) tende a sair do token para server-side
  (ver ADR-0007), o que mantém o JWT enxuto e permite revogação imediata (I10).

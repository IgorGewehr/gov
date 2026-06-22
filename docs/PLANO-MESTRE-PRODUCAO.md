# Plano-Mestre de Produção — Tensorroot.Gov

> **Onde estamos:** 11/11 Bounded Contexts com código completo, **547 testes verdes**, build 0/0,
> 11 migrations, todos descobertos em runtime. Infra sólida: database-per-tenant, Outbox
> (interceptor+publisher+coordinator), auditoria imutável, gating por licença, multi-tenant.
> Frontend gov.br em construção (fundação + 6 módulos).
>
> **O que este plano cobre:** o *último quilômetro* entre "verde e testado" e "uma prefeitura
> usa em produção e presta contas ao TCE". Hoje as **integrações governamentais são simuladas
> (ACL stub)** e a **autorização é rasa** (JWT + "qualquer autenticado"). É isso que falta.

## Princípios do plano
- **Prioridade = valor para o piloto** (Maximiliano de Almeida/RS) + venda. O que destrava uso real vem primeiro.
- **A fábrica Rules-as-Code acelera:** onde marcado 🏭, edita-se o `.rules.md`/spec e gera-se/audita-se o código.
- **Esforço:** S (dias) · M (~1 sprint) · L (1-2 sprints) · XL (vários).
- Sprints com letras (A–J) para não colidir com o roadmap de módulos (1–10) já concluído.

---

## Sprints

### A — Identidade & Acesso (AuthN/AuthZ/RBAC/LGPD) 🏭 parcial · **L** · gate
**Por quê:** sem login real e papéis, ninguém usa nem demonstra. Gate de tudo que é user-facing.
- **IdP escolhido: Keycloak (OIDC self-host)** — emite o JWT; nosso RBAC (`Usuario→Departamento→Role→Permission`) vive no sistema. **gov.br Login** fica reservado para portais do CIDADÃO (ex.: consulta de dívida ativa).
- Módulo **Identidade**: `Usuario → Departamento → Role → Permission`; emissão/validação de JWT com claims (`tenant_id`, roles).
- **Autorização por política/operação**: trocar `RequireAuthorization()` por policies (`Tributos.DividaAtiva.Inscrever`, etc.).
- **LGPD**: trilha de **acesso de leitura** a dados sensíveis (Saúde, Assistência, menores) + base legal por operação.
- **Dependências:** nenhuma. **Habilita:** frontend real, demo, todos os Tiers seguintes.

### B — Wiring cross-module + Outbox/Inbox idempotente 🏭 parcial · **M**
**Por quê:** hoje eventos cross-module são *placeholders ACL locais*; os módulos não conversam de verdade.
- Consolidar eventos no `*.Contracts` da **origem** (ex.: `ContratoAssinado` em `Administracao.Contracts`); consumidores referenciam a origem (sem violar fitness).
- **Inbox** (dedup por `EventId`) + dispatch transacional; fechar o ciclo Outbox→Inbox.
- Testes **e2e de fluxo**: Tributos receita→Finanças; Finanças empenho→Administração eficácia; Legislativo sanção→Executivo.
- **Dependências:** nenhuma (mais fácil após A).

### C — Base de Integração Governamental (A1/Key Vault + ACL+Polly + RemessaLog) · **M** · fundação Tier 1
**Por quê:** infra compartilhada por TODAS as remessas reais.
- `IAssinadorXml` (XMLDSig com **A1 .pfx do Key Vault**, por tenant).
- **Gateway resiliente** base (Polly: timeout/retry/circuit-breaker) + padrão ACL com mapa de erros.
- **`RemessaLog`** (idempotência + auditoria de envios) + agendamento por **Worker** (padrão `NfseSync`).
- **Dependências:** A (Key Vault/tenant).

### D — Remessa TCE-RS (SIAPC/PAD) 🏭 · **L** · ⭐ killer do piloto
**Por quê:** sem isso a prefeitura **não presta contas**. Maior diferencial.
- Layout **SIAPC/PAD** versionado por exercício (IOptions, nunca hardcoded); geração a partir das projeções de Finanças/Tributos/RH/Patrimônio.
- Assinatura A1, transmissão via `ITceGateway`, retorno (aceito/rejeitado + inconsistências) → painel de pendências.
- **Dependências:** C, B. *(decisão pendente: confirmar versão do leiaute TCE-RS vigente)*

### E — eSocial (RH) 🏭 · **L**
- Eventos S-1000/S-2200/S-1200/S-1210, XSD vigente, assinatura A1, lotes, controle de recibo/ordem, ACL+Polly.
- **Dependências:** C.

### F — NFS-e/ADN real + CNAB retorno + Transparência/SICONFI 🏭 parcial · **L**
- **AdnNfseGateway real** (Worker já existe; hoje simulado), dedup por chave de acesso.
- **CNAB retorno** (baixa) + Segmento B/PIX + layouts por banco (BB/Caixa/Sicredi) sobre a base 240 já concreta.
- **Transparência/SICONFI**: export dados abertos (CSV/JSON) + **MSC** (Matriz de Saldos Contábeis).
- **Dependências:** C, B (Transparência consome eventos).

### G — Deploy Azure + Provisionamento real por tenant · **L** · operar
- Bicep: **Azure SQL** (DB dedicado por tenant / elastic pool), **Key Vault**, **Container Apps** (ApiHost + Workers), Log Analytics. *(decisão: pool elástico vs instâncias)*
- CI/CD de deploy; provisionamento real cria **Azure SQL** do tenant + migra + grava conexão no Key Vault.
- **Dependências:** A, C.

### H — Observabilidade & Resiliência de produção · **M**
- Exporter **OTLP/App Insights sem o CVE** (NU1902), dashboards, alertas, health por dependência.
- Outbox **poison/retry**, rate limit **por tenant**.
- **Dependências:** G.

### I — Maturidade de API + Profundidade de domínio (onda 2) 🏭 · **XL**
- API: paginação, filtros, **ProblemDetails**, versionamento, polish OpenAPI.
- Domínio onda 2 (fábrica): Finanças **PPA/LDO/LOA + PCASP + restos a pagar**; Saúde **farmácia/HÓRUS + vacina/SI-PNI + telessaúde**; Educação **merenda/PNAE + transporte/PNATE**; Administração **atas/registro de preços**; etc.
- **Dependências:** nenhuma rígida (vários runs da fábrica).

### J — Hardening final + Go-live do piloto · **L**
- Testes **e2e cross-module**, contrato de API, **carga/performance** (memória/`Span`), **segurança** (OWASP/pentest).
- **Seed/dados de referência** (leiautes TCE por exercício, CBO/CID/procedimentos SUS), runbooks.
- **Go-live** Maximiliano de Almeida/RS.
- **Dependências:** A–H.

---

## Track paralelo — Frontend
Em andamento (fundação + Saúde/Educação/Legislativo/RH/Finanças/Transparência). Faltarão depois: telas de **Administração, Patrimônio, Protocolo, Assistência** (mesmo padrão, baratas) + integração com o **AuthN do Sprint A** (login/guarda real) + cliente tipado gerado do **OpenAPI**.

## Caminho crítico mínimo para um PILOTO demonstrável
> **A (auth) → C (base integração) → D (remessa TCE) → G (deploy)** + frontend.
> Resultado: o usuário **loga com papéis**, opera o núcleo e **presta contas ao TCE**, no ar na Azure. É o menor conjunto que vira piloto vendável.

## Decisões
- ✅ **IdP (Sprint A):** **Keycloak (OIDC self-host)** — emite o JWT; RBAC no sistema.
- ✅ **Ordem:** **LARGURA** — todo o Tier 1 (**A→B→C→D→E→F**) antes do deploy, depois **G→H→I→J**.

**Sequência aprovada:** A · B · C · D · E · F · G · H · I · J.

Pendentes (decidir no respectivo sprint, não bloqueiam agora):
- **Azure SQL (Sprint G):** DB dedicado por tenant em **elastic pool** (custo menor) vs instâncias separadas (isolamento máximo).
- **TCE-RS (Sprint D):** confirmar a **versão do leiaute SIAPC/PAD** vigente para o exercício do piloto.

## Como a fábrica acelera
Sprints D, E, F, I e parte de A/B nascem de **specs `.rules.md`/integração** → geração + auditoria automáticas + verificação (build/test/consistency/fitness), como nos 9 módulos. Os Tiers 2 (deploy/observabilidade) são mais "à mão" (infra/Bicep/pipelines).

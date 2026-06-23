---
modulo: RecursosHumanos
agregado: VinculoServidorUsuario
contexto: RecursosHumanos (autosservico do servidor — "Minha Folha")
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["LGPD Lei 13.709/2018 art. 5 (dado pessoal)", "LGPD art. 7, VI e art. 18 (acesso do titular aos proprios dados)", "LGPD art. 7, II (obrigacao legal — fornecer contracheque/informe)", "LGPD art. 37 (registro/trilha de acesso)", "CF/1988 art. 5, XXXIII (acesso a informacao propria)"]
---

# Minha Folha — Autosservico do Servidor (Rules-as-Code)

> SELF-SERVICE do servidor: um `Usuario` (modulo Identidade) vinculado a um `ServidorId` (modulo
> RecursosHumanos) consulta SOMENTE os SEUS dados pessoais — contracheque, espelho de ponto, ferias
> e informe de rendimentos. Cross-module APENAS por chave opaca (o `sub`/subject do JWT); o RH NAO
> referencia o tipo interno de `Usuario`.

## Vinculo usuario ↔ servidor (a ancora)

- O agregado `VinculoServidorUsuario` (TenantId, UsuarioId, ServidorId) registra que um usuario
  autenticado E um servidor. Unico por tenant nos dois sentidos (um usuario ↔ um servidor).
- O vinculo e criado pela GESTAO do RH (`autosservico.proprio` NAO o cria) — gated por
  `recursoshumanos.gerenciar`.

## ABAC dado-proprio — A PROVA DE BALA

- **G**: dado um servidor autenticado **W**: quando consulta um endpoint de "Minha Folha"
  **T**: SO retorna dados do PROPRIO servidor, resolvido do usuario autenticado (`ICurrentUser.sub`)
  pelo vinculo — NUNCA de um `servidorId` informado pelo cliente (os endpoints nao expoem esse
  parametro: nao ha id a forjar).
- **G**: dado um servidor A **W**: quando tenta ver dados do servidor B
  **T**: e impossivel — A so enxerga as proprias verbas (filtro por servidor-proprio).
- **G**: dado um usuario SEM vinculo (ou sem `sub`) **W**: quando consulta "Minha Folha"
  **T**: acesso NEGADO (deny-by-default) e a tentativa e selada na trilha de acesso LGPD (negativa).
- **G**: dado um vinculo do tenant B **W**: quando consultado no contexto do tenant A
  **T**: invisivel (Global Query Filter) — negado.

## LGPD (trilha de acesso — LG-2)

- Toda query de "Minha Folha" implementa `ISensivelLgpd`: o pipeline sela
  `{Tenant, UserId, Ip, Entidade, BaseLegal, Ts}` apos a leitura. Bases legais aplicaveis:
  exercicio de direitos do titular (art. 7, VI / art. 18) e obrigacao legal do empregador
  (art. 7, II) — qualquer outra e rejeitada e auditada.

## Permissao

- `autosservico.proprio`: verbo do papel "Servidor". CONHECIDO/atribuivel, porem FORA de `Todas`
  (o admin de tenant nao o recebe; quem ve "tudo" usa `recursoshumanos.ver`).

<!-- manifest
commands: VincularUsuarioAoServidor
queries: ObterMeuContracheque, ObterMeuEspelhoDePonto, ObterMinhasFerias, ObterMeuInformeDeRendimentos
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->

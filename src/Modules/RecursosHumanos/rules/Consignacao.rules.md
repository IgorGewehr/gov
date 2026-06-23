# Consignação + Margem Consignável — Rules-as-Code (Recursos Humanos)

Consignações em folha (Lei 14.131/2021) com controle da **margem consignável** parametrizável por tenant.
Reusa a espinha de `RegraAfastamento`/gancho-de-folha: o usuário escolhe a rubrica; o efeito (balde de
margem, prioridade no corte) vem da parametrização (`RubricaConsignavel`), nunca hardcoded. O
`MotorDeCalculoFolha` permanece puro; o `LancadorDescontosConsignados` lança o desconto consignado na
folha respeitando a margem disponível da competência (corte por prioridade) — mesma porta do
`AjustadorProventoPorAfastamento` (design RH §2.5).

## Agregados
- **Consignataria** — cadastro mestre (banco/entidade habilitada). `Cadastrar`/`Suspender`/`Reativar`. Único `(TenantId, Cnpj)`.
- **RubricaConsignavel** — parametrização por tenant: `Categoria` (prioridade), `GrupoMargem` (balde), `ContaParaMargem` (compõe a base).
- **ContratoConsignacao** — averbação servidor×consignatária×rubrica. `Averbar`/`Suspender`/`Reativar`/`RegistrarParcelaPaga`/`Cancelar`.
- **ParametrosMargemVigente** — percentuais por vigência (default legal 35% geral + 5% cartão consignado + 5% cartão benefício).

## Margem (3 baldes independentes — reserva legal)
- Geral 35% · Cartão consignado 5% · Cartão benefício 5% (parametrizáveis por tenant/vigência).
- `Disponivel(grupo) = Limite(grupo) − Comprometido(grupo)`, nunca negativa; o cartão não invade o geral.
- Base de cálculo = remuneração consignável apurada da folha (proventos com `ContaParaMargem`), nunca digitada.

## Invariantes
- Averbar só se `ValorParcela <= Disponivel(grupoDaRubrica)`; consignatária Ativa; valor>0; qtd>0.
- Reativar re-checa a margem (a base pode ter caído por afastamento).
- `RegistrarParcelaPaga` quita ao atingir `QuantidadeParcelas` (idempotente); cancelar libera margem.
- Efeito na folha: soma dos averbados acima da margem → corte por prioridade (obrigatória→facultativa→benefício); jamais estoura a margem.

## Endpoints
- `POST /api/recursoshumanos/consignatarias` — CadastrarConsignatariaCommand `[rh.consignacao.gerenciar]`
- `GET /api/recursoshumanos/consignatarias` — ListarConsignatariasQuery `[rh.consignacao.ver]`
- `POST /api/recursoshumanos/consignatarias/{id}/suspensao` — SuspenderConsignatariaCommand `[rh.consignacao.gerenciar]`
- `POST /api/recursoshumanos/consignatarias/{id}/reativacao` — ReativarConsignatariaCommand `[rh.consignacao.gerenciar]`
- `POST /api/recursoshumanos/rubricas-consignaveis` — DefinirRubricaConsignavelCommand `[rh.consignacao.gerenciar]`
- `GET /api/recursoshumanos/servidores/{id}/margem` — ConsultarMargemQuery `[rh.consignacao.ver]`
- `GET /api/recursoshumanos/servidores/{id}/consignacoes` — ListarConsignacoesDoServidorQuery `[rh.consignacao.ver]`
- `POST /api/recursoshumanos/consignacoes` — AverbarConsignacaoCommand `[rh.consignacao.averbar]`
- `POST /api/recursoshumanos/consignacoes/{id}/suspensao` — SuspenderConsignacaoCommand `[rh.consignacao.gerenciar]`
- `POST /api/recursoshumanos/consignacoes/{id}/reativacao` — ReativarConsignacaoCommand `[rh.consignacao.gerenciar]`
- `POST /api/recursoshumanos/consignacoes/{id}/cancelamento` — CancelarConsignacaoCommand `[rh.consignacao.gerenciar]`
- `POST /api/recursoshumanos/folhas/{id}/consignados` — LancarConsignadosNaFolhaCommand `[recursoshumanos.gerenciar]`

<!-- manifest
commands: CadastrarConsignataria, SuspenderConsignataria, ReativarConsignataria, DefinirRubricaConsignavel, AverbarConsignacao, SuspenderConsignacao, ReativarConsignacao, CancelarConsignacao, LancarConsignadosNaFolha
queries: ConsultarMargem, ListarConsignacoesDoServidor, ListarConsignatarias
domainEvents: ConsignacaoAverbada, ConsignacaoSuspensa, ConsignacaoQuitada, ConsignacaoCancelada
integrationEventsPublished: 
integrationEventsConsumed: 
-->

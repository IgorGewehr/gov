# Estabelecimento — Rules-as-Code (Saúde)

Agregado-mestre da **Unidade/Estabelecimento de saúde (CNES)** (UBS/UPA/Hospital/CAPS...). Substitui o
`SimuladoEstabelecimentoRepository` (só checa `Guid.Empty`) por cadastro real local consultado pelo
`EstabelecimentoRepository`. Pré-requisito da Agenda (Onda 2) e da exibição de nome/CNES no atendimento.

## Invariantes
- CNES válido (7 dígitos); `(TenantId, Cnes)` único; Nome obrigatório.
- Estabelecimento inativo bloqueia vínculo de novos atendimentos (validado no repo de consulta).

## Endpoints
- `POST /api/saude/estabelecimentos` — CadastrarEstabelecimentoCommand `[saude.gerenciar]`
- `PUT /api/saude/estabelecimentos/{id}` — AtualizarEstabelecimentoCommand `[saude.gerenciar]`
- `POST /api/saude/estabelecimentos/{id}/inativacao` — InativarEstabelecimentoCommand `[saude.gerenciar]`
- `POST /api/saude/estabelecimentos/{id}/reativacao` — ReativarEstabelecimentoCommand `[saude.gerenciar]`
- `GET /api/saude/estabelecimentos` — BuscarEstabelecimentosQuery (paginado) `[saude.ver]`
- `GET /api/saude/estabelecimentos/{id}` — ObterEstabelecimentoPorIdQuery `[saude.ver]`

<!-- manifest
commands: CadastrarEstabelecimento, AtualizarEstabelecimento, InativarEstabelecimento, ReativarEstabelecimento
queries: BuscarEstabelecimentos, ObterEstabelecimentoPorId
domainEvents: EstabelecimentoCadastrado, EstabelecimentoAtualizado, EstabelecimentoInativado, EstabelecimentoReativado
integrationEventsPublished: 
integrationEventsConsumed: 
-->

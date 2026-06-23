# Profissional — Rules-as-Code (Saúde)

Agregado-mestre do **Profissional de saúde** (CPF/CNS/registro de conselho) com vínculos CNES
(`VinculoProfissional`: estabelecimento + CBO + período). Habilita teleconsulta (CRM ativo) e a
validação real de `ProfissionalAtivo`/`ProfissionalComCrmAtivo` no atendimento.

## Invariantes
- CPF válido; `(TenantId, Cpf)` único.
- Um vínculo ativo por `(profissional, estabelecimento, CBO)` simultâneo.
- Teleconsulta exige `TemCrmAtivo` (registro CRM + situação Ativo).

## Endpoints
- `POST /api/saude/profissionais` — CadastrarProfissionalCommand `[saude.gerenciar]`
- `POST /api/saude/profissionais/{id}/vinculos` — VincularProfissionalCommand `[saude.gerenciar]`
- `POST /api/saude/profissionais/{id}/vinculos/encerramento` — EncerrarVinculoProfissionalCommand `[saude.gerenciar]`
- `POST /api/saude/profissionais/{id}/inativacao` — InativarProfissionalCommand `[saude.gerenciar]`
- `GET /api/saude/profissionais` — BuscarProfissionaisQuery (paginado) `[saude.ver]`
- `GET /api/saude/profissionais/{id}` — ObterProfissionalPorIdQuery `[saude.ver]`

<!-- manifest
commands: CadastrarProfissional, VincularProfissional, EncerrarVinculoProfissional, InativarProfissional
queries: BuscarProfissionais, ObterProfissionalPorId
domainEvents: ProfissionalCadastrado, VinculoProfissionalAberto, VinculoProfissionalEncerrado, ProfissionalInativado
integrationEventsPublished: 
integrationEventsConsumed: 
-->

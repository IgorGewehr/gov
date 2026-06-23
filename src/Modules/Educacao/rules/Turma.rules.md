# Turma — Rules-as-Code (Educação)

Agregado-mestre da **Turma** (escola + ano letivo + etapa + série + turno + vagas). Mantém o
contador desnormalizado `Matriculados` (atualizado pela Matrícula na mesma UoW) e elimina o stub
`TurmaRepository.PossuiVagaAsync` (`id != Guid.Empty`) por contagem real `matriculados < vagas`.

## Invariantes
- I-T1: `Vagas > 0`; `Etapa`/`Turno` válidos.
- I-T2: `Matriculados <= Vagas` sempre.
- I-T3: Enturmação só em `Aberta`; `AjustarVagas` nunca abaixo de `Matriculados`.
- I-T4: Unicidade `(TenantId, EscolaId, AnoLetivo, Serie, Turno)`.
- I-T5: `Encerrar` exige `Matriculados == 0` (ou encerramento de ano letivo).

## Endpoints
- `POST /api/educacao/turmas` — CriarTurmaCommand `[educacao.gerenciar]`
- `POST /api/educacao/turmas/{id}/abertura` — AbrirTurmaCommand `[educacao.gerenciar]`
- `POST /api/educacao/turmas/{id}/encerramento` — EncerrarTurmaCommand `[educacao.gerenciar]`
- `PUT /api/educacao/turmas/{id}/vagas` — AjustarVagasCommand `[educacao.gerenciar]`
- `GET /api/educacao/turmas` — BuscarTurmasQuery (paginado, c/ VagasDisponiveis) `[educacao.ver]`
- `GET /api/educacao/turmas/{id}` — ObterTurmaQuery `[educacao.ver]`

<!-- manifest
commands: CriarTurma, AbrirTurma, EncerrarTurma, AjustarVagas
queries: BuscarTurmas, ObterTurma
domainEvents: TurmaCriada, TurmaAberta, TurmaEncerrada
integrationEventsPublished: 
integrationEventsConsumed: 
-->

# Aluno — Rules-as-Code (Educação)

Agregado-mestre do **Aluno** (Onda 1). Substitui o `AlunoId` "solto" da Matrícula por um
agregado rico (`Aluno` + `Responsavel`), com dados civis, endereço e situação. Base dos
*pickers* da tela de matrícula e do diário por turma.

## Invariantes
- I-A1: Nome obrigatório; `DataNascimento` não-futura.
- I-A2: Menor de idade ⇒ ≥1 `Responsavel` no cadastro (fail-closed no factory).
- I-A3: `NomeMae` obrigatório (exigência EducaCenso).
- I-A4: Unicidade por tenant — `(TenantId, Cpf)` quando informado; `(TenantId, CodigoInepAluno)` quando informado.
- I-A5: `Inativar`/`Transferido` é terminal — bloqueia novas alterações.

## LGPD
`DadosCivis`, `Cpf`, `Responsavel.Cpf` marcados `[CampoSensivelLgpd]` (menor — art. 14).

## Endpoints
- `POST /api/educacao/alunos` — CadastrarAlunoCommand `[educacao.gerenciar]`
- `PUT /api/educacao/alunos/{id}` — AtualizarAlunoCommand `[educacao.gerenciar]`
- `POST /api/educacao/alunos/{id}/responsaveis` — AdicionarResponsavelCommand `[educacao.gerenciar]`
- `POST /api/educacao/alunos/{id}/inativacao` — InativarAlunoCommand `[educacao.gerenciar]`
- `GET /api/educacao/alunos` — BuscarAlunosQuery (paginado) `[educacao.ver]`
- `GET /api/educacao/alunos/{id}` — ObterAlunoQuery `[educacao.ver]`

<!-- manifest
commands: CadastrarAluno, AtualizarAluno, AdicionarResponsavel, InativarAluno
queries: BuscarAlunos, ObterAluno
domainEvents: AlunoCadastrado, DadosAlunoAtualizados, ResponsavelAdicionado, AlunoInativado
integrationEventsPublished: AlunoCadastradoIntegrationEvent
integrationEventsConsumed: 
-->

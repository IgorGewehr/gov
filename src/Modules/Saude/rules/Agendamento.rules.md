# Agendamento — Rules-as-Code (Saúde · Agendamento)

Marcação de uma consulta/exame de um **Paciente** com um **Profissional** num **Estabelecimento**, numa
**Vaga** da AgendaProfissional. Reusa Paciente/Profissional/Estabelecimento da Onda 1 por Id. A consulta
realizada faz ponte para o `Atendimento`/PEP existente. Dado pessoal sensível (LGPD art. 11) — a busca
gera trilha de acesso (LG-3).

## Invariantes
- **Sem overbooking:** uma vaga `Livre` só admite uma marcação (a transição que ocupa a vaga exige `Livre`).
- Marcar exige Paciente/Profissional/Estabelecimento **existentes e Ativos** (Onda 1) e data no futuro.
- Sem duplo-agendamento do paciente no mesmo instante (conflito rejeitado).
- Cancelamento/falta **libera** a vaga e dispara convocação da fila de espera (prioridade + FIFO).
- Falta só registrável na data do atendimento ou depois; `Realizar` associa opcionalmente o Atendimento (PEP).

## Cenários (BDD)
- **Dado** uma vaga livre, **quando** o paciente é marcado, **então** a vaga fica `Ocupada` e o agendamento nasce `Marcado`; **quando** uma segunda marcação tenta a mesma vaga, **então** falha (anti-overbooking).
- **Dado** um agendamento ativo, **quando** cancelado/marcado como falta, **então** a vaga é liberada e o próximo da fila (maior prioridade, mais antigo) é convocado.
- **Dado** estabelecimento inativo, **quando** se tenta marcar, **então** a marcação é rejeitada.

## Endpoints
- `POST /api/saude/agendamentos` — MarcarAgendamentoCommand `[saude.agenda.marcar]`
- `POST /api/saude/agendamentos/{id}/confirmacao` — ConfirmarAgendamentoCommand `[saude.agenda.marcar]`
- `POST /api/saude/agendamentos/{id}/cancelamento` — CancelarAgendamentoCommand `[saude.agenda.marcar]`
- `POST /api/saude/agendamentos/{id}/falta` — RegistrarFaltaCommand `[saude.agenda.marcar]`
- `POST /api/saude/agendamentos/{id}/realizacao` — RealizarAgendamentoCommand `[saude.agenda.marcar]`
- `GET /api/saude/agendamentos` — BuscarAgendamentosQuery (paginado, sensível LGPD) `[saude.agenda.ver]`

<!-- manifest
commands: MarcarAgendamento, ConfirmarAgendamento, CancelarAgendamento, RegistrarFalta, RealizarAgendamento
queries: BuscarAgendamentos
domainEvents: AgendamentoMarcado, AgendamentoConfirmado, AgendamentoCancelado, FaltaRegistrada, AgendamentoRealizado
integrationEventsPublished: 
integrationEventsConsumed: 
-->

# FilaEspera — Rules-as-Code (Saúde · Agendamento)

Entrada na fila de espera quando não há vaga: o **Paciente** aguarda por um **Profissional** específico ou
por uma **especialidade (CBO)**, num **Estabelecimento**. Ao liberar vaga (cancelamento/falta), a
orquestração convoca por prioridade e ordem de chegada (FIFO dentro da prioridade). Dado pessoal sensível
(LGPD art. 11) — a listagem gera trilha de acesso.

## Invariantes
- Exige ao menos um alvo: profissional OU especialidade (CBO 6 dígitos); paciente Ativo.
- Convocação: somente entradas `Aguardando`, ordenadas por prioridade (desc) e `DataEntrada` (FIFO).
- Estados terminais `Atendido`/`Removido` não voltam a `Aguardando`; remoção exige motivo.

## Cenários (BDD)
- **Dado** uma vaga liberada com fila não vazia, **quando** se processa a liberação, **então** o próximo (maior prioridade, mais antigo) é `Convocado`.
- **Dado** uma entrada `Aguardando`, **quando** removida com motivo, **então** fica `Removido` (terminal).

## Endpoints
- `POST /api/saude/fila-espera` — EntrarNaFilaDeEsperaCommand `[saude.agenda.marcar]`
- `POST /api/saude/fila-espera/{id}/convocacao` — ConvocarDaFilaDeEsperaCommand `[saude.agenda.marcar]`
- `POST /api/saude/fila-espera/{id}/remocao` — RemoverDaFilaDeEsperaCommand `[saude.agenda.marcar]`
- `GET /api/saude/fila-espera` — BuscarFilaDeEsperaQuery (paginado, sensível LGPD) `[saude.agenda.ver]`

<!-- manifest
commands: EntrarNaFilaDeEspera, ConvocarDaFilaDeEspera, RemoverDaFilaDeEspera
queries: BuscarFilaDeEspera
domainEvents: PacienteIncluidoNaFilaDeEspera, PacienteConvocadoDaFilaDeEspera, EntradaFilaDeEsperaRemovida
integrationEventsPublished: 
integrationEventsConsumed: 
-->

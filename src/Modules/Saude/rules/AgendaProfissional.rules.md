# AgendaProfissional — Rules-as-Code (Saúde · Agendamento)

Grade de disponibilidade de um **Profissional** num **Estabelecimento (UBS)** para uma data, expandida em
**Vagas (slots)** de duração fixa. Pré-requisito da marcação (Onda 2). Reusa Profissional/Estabelecimento
da Onda 1 por Id. A vaga é entidade *owned* — toda transição de slot ocorre por métodos do agregado
(fronteira de consistência da concorrência / anti-overbooking).

## Invariantes
- Janela válida (`HoraFim > HoraInicio`); duração de slot `>= 5` min; capacidade `>= 1`; expansão limitada (teto de vagas).
- Profissional e Estabelecimento devem existir e estar **Ativos** (reuso Onda 1) para abrir agenda.
- Publicar expande as vagas (idempotente) e torna a grade marcável; bloquear/reabrir afeta apenas vagas livres/bloqueadas (vaga ocupada exige cancelamento do agendamento).

## Cenários (BDD)
- **Dado** uma agenda em rascunho, **quando** publicada, **então** as vagas são geradas (`HoraInicio..HoraFim` / duração × capacidade) e a grade fica `Aberta`.
- **Dado** uma agenda aberta com vaga livre, **quando** o dia é bloqueado, **então** as vagas livres ficam `Bloqueada` e a grade `Bloqueada`; **quando** reaberto, voltam a `Livre`.

## Endpoints
- `POST /api/saude/agendas` — AbrirAgendaCommand `[saude.agenda.gerenciar]`
- `POST /api/saude/agendas/{id}/publicacao` — PublicarAgendaCommand `[saude.agenda.gerenciar]`
- `POST /api/saude/agendas/{id}/bloqueio` — BloquearDiaAgendaCommand `[saude.agenda.gerenciar]`
- `POST /api/saude/agendas/{id}/reabertura` — ReabrirDiaAgendaCommand `[saude.agenda.gerenciar]`
- `GET /api/saude/agendas/{id}` — ObterAgendaPorIdQuery `[saude.agenda.ver]`
- `GET /api/saude/agendas/vagas` — BuscarVagasLivresQuery `[saude.agenda.ver]`

<!-- manifest
commands: AbrirAgenda, PublicarAgenda, BloquearDiaAgenda, ReabrirDiaAgenda
queries: ObterAgendaPorId, BuscarVagasLivres
domainEvents: AgendaProfissionalAberta, AgendaProfissionalPublicada, DiaAgendaBloqueado, DiaAgendaReaberto
integrationEventsPublished: 
integrationEventsConsumed: 
-->

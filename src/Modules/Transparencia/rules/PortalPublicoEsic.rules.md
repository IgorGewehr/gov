# Regras — Portal Público + e-SIC + Dados Abertos (Onda 2)

> Rules-as-Code da superfície de PROFUNDIDADE backend da Transparência: portal público anônimo
> (transparência ativa materializada de Integration Events já publicados), e-SIC (LAI, transparência
> passiva) e dados abertos (CSV). Marco legal: Lei 12.527/2011 (LAI, arts. 8-16), LC 131/2009 (tempo
> real), Dec. 7.724/2012 (transparência ativa), LGPD (arts. 6, 11, 23).

## Invariantes

- **Superfície pública é read-only e anônima** (`/publico/transparencia/{slug}`), fora de `/api/...`;
  resolve o tenant pelo slug (sem JWT), fixa o `TenantOverride` e o Global Query Filter por TenantId volta
  a valer (anti-vazamento cross-tenant). Módulo não licenciado ⇒ 404 (não revela existência).
- **Read models idempotentes** por chave de origem (`OrigemEventoId`): reprocessar um evento não duplica.
- **e-SIC**: protocolo único por (tenant, ano); prazo legal **20 dias úteis CALCULADO** (nunca digitado),
  prorrogação **única, justificada, antes do vencimento** (+10 dias úteis); resposta/indeferimento sempre
  fundamentados; máquina de estados `Aberto→EmAtendimento→(Respondido|Indeferido)→[RecursoAberto→RecursoRespondido]→Encerrado`.
- **LGPD**: CPF mascarado na origem; folha nominal pública sem CPF/matrícula; PII do solicitante e-SIC
  jamais exposta na consulta pública do protocolo; detalhe interno (com PII) gera trilha de acesso LG-3.

## Cenários (BDD)

**Cenário 1 — Portal público anônimo isolado por tenant**
Dado um cidadão anônimo acessando `/publico/transparencia/{slug}`
Quando o slug é resolvido para um tenant com o módulo licenciado
Então só os dados daquele tenant são retornados (TenantOverride + Global Query Filter), sem exigir login.

**Cenário 2 — e-SIC: prazo legal calculado**
Dado um pedido aberto em uma data
Quando o pedido é protocolado
Então o `PrazoResposta` = +20 dias **úteis** (calculado pelo calendário do tenant), e **PedidoSicAberto** é emitido.

**Cenário 3 — Prorrogação única e antes do vencimento**
Dado um pedido ainda no prazo
Quando o órgão prorroga com justificativa
Então `ProrrogadoAte` = +10 dias úteis e nova prorrogação é rejeitada (LAI art. 11 §2º).

**Cenário 4 — Projeção idempotente**
Dado um `DespesaEmpenhadaIntegrationEvent` já projetado
Quando o mesmo evento é reprocessado
Então a linha de `PublicacaoDespesa` é atualizada, não duplicada (idempotência por `OrigemEventoId`).

**Cenário 5 — LGPD na folha nominal pública**
Dada a folha nominal pública
Quando o cidadão a consulta
Então nome/cargo/lotação/remuneração são exibidos, mas **nunca** CPF ou matrícula (não existem no read model).

<!-- manifest
commands: AbrirPedidoSic, IniciarAtendimentoSic, ResponderPedidoSic, ProrrogarPedidoSic, IndeferirPedidoSic, InterporRecursoSic, DecidirRecursoSic, ConfigurarPortalPublico
queries: ConsultarStatusPedidoSic, ListarPedidosSic, ObterPedidoSicInterno, ConsultarDespesasPublicas, ConsultarReceitasPublicas, ConsultarContratosPublicos, ConsultarFolhaPublica, ConsultarResumoFiscalPublico, ObterCatalogoDadosAbertos
domainEvents: PedidoSicAberto, PedidoSicEmAtendimento, PedidoSicProrrogado, PedidoSicRespondido, PedidoSicIndeferido, PedidoSicRecursoInterposto, PedidoSicRecursoDecidido, PedidoSicEncerrado
integrationEventsPublished:
integrationEventsConsumed: DespesaEmpenhadaIntegrationEvent, DespesaLiquidadaIntegrationEvent, PagamentoEfetuadoIntegrationEvent, ReceitaCorrenteLiquidaApuradaIntegrationEvent, ContratoAssinadoIntegrationEvent, ContratoPublicadoPncpIntegrationEvent, LicitacaoHomologadaIntegrationEvent, FolhaResumoRemessaTceIntegrationEvent
-->

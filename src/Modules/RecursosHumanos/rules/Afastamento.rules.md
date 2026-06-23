# Afastamento — Rules-as-Code (Recursos Humanos)

Agregado **Afastamento** TIPADO (maternidade, doença/INSS, prêmio, sem vencimento, etc.) com efeito
determinístico na folha. Substitui o afastamento genérico (`Servidor.RegistrarAfastamento` sem tipo
nem efeito). O `PercentualRemuneracao` e os flags (`SuspendeProventos`, `ContaTempo`) vêm da tabela
de regras do tipo (`IRegraAfastamentoProvider`/`ParametrosAfastamento`), parametrizada por tenant —
nunca hardcoded. O `MotorDeCalculoFolha` permanece puro; o `AjustadorProventoPorAfastamento` ajusta
as verbas (vencimento × percentual / proporcionalidade por dias) ANTES de entrar no motor.

> Os comandos `RegistrarAfastamento` (agora com `Tipo`) e o evento `AfastamentoRegistrado` (agora com
> tipo) seguem declarados no manifesto de `Servidor.rules.md`.

## Tipos e efeito na folha (parametrizável por tenant/vigência)
- LicencaMaternidade — ente integral; 120d (180d se adesão Empresa Cidadã). Conta tempo. S-2230.
- LicencaPaternidade — integral; 5d (20d se adesão). Conta tempo.
- DoencaAte15Dias — ente integral (15 primeiros dias).
- DoencaINSS (>15d) — suspende provento do ente a partir do 16º dia (RGPS).
- AcidenteTrabalho — análogo doença, com estabilidade.
- LicencaPremio — integral (90d por quinquênio, se lei municipal).
- LicencaSemVencimento — suspende 100%; NÃO conta tempo.
- Cessao com/sem ônus; MandatoEletivo (art. 38 CF).

## Invariantes
- Tipo válido; `Inicio <= FimPrevisto` quando informado.
- `PercentualRemuneracao` ∈ [0,100], derivado da regra do tipo (não digitado livre).
- `Encerrar` só em afastamento `Vigente`; calcula dias efetivos.
- `ContaTempo=false` desconta do cômputo de estabilidade/aposentadoria.

## Endpoints
- `POST /api/rh/servidores/{id}/afastamentos` — RegistrarAfastamentoCommand (com Tipo) `[rh.gerenciar]`
- `POST /api/rh/afastamentos/{id}/encerramento` — EncerrarAfastamentoCommand `[rh.gerenciar]`
- `POST /api/rh/afastamentos/{id}/cancelamento` — CancelarAfastamentoCommand `[rh.gerenciar]`
- `GET /api/rh/servidores/{id}/afastamentos` — ListarAfastamentosDoServidorQuery `[rh.ver]`
- `GET /api/rh/afastamentos` — BuscarAfastamentosQuery (paginado) `[rh.ver]`

<!-- manifest
commands: EncerrarAfastamento, CancelarAfastamento
queries: ListarAfastamentosDoServidor, BuscarAfastamentos
domainEvents: AfastamentoEncerrado
integrationEventsPublished: 
integrationEventsConsumed: 
-->

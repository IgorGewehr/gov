# eSocial — Rules-as-Code (RecursosHumanos)

> Bounded Context: **RecursosHumanos** · Agregado: **EventoESocial** · Piloto: Maximiliano de Almeida/RS (Grupo 4, RPPS).
> Autoridade: `docs/architecture/m5-prep/ESOCIAL-SPEC.md` + `M5-DESIGN.md`.
> **REGRA CRÍTICA [OFICIAL] §16:** leiaute/XSD do eSocial é OFICIAL. A estrutura dos eventos é FIEL ao
> conceito do leiaute S-1.3 (NT 06/2026); campos/XSD EXATOS estão marcados `// TODO(validar-oficial)` no
> código e o XML deve ser validado contra o XSD travado antes de assinar. A TRANSMISSÃO REAL (Produção
> Restrita/SOAP 1.2/mTLS) depende de credenciais do dono → `IESocialGateway` atrás de ACL com impl.
> **SIMULADA**; a real marcada `// TODO(prod: creds homologação)`.

<!-- manifest
commands: GerarS1000, GerarS1005, GerarS1010, GerarS2200, GerarS2299, GerarRemuneracaoFolha, GerarPagamentosFolha, GerarFechamentoFolha, AssinarEventoESocial, TransmitirEventosAssinados, ConsultarRetornosESocial
queries: ListarEventosESocial
domainEvents: EventoESocialGerado, EventoESocialAssinado, EventoESocialTransmitido, EventoESocialProcessado, EventoESocialRejeitado
integrationEventsPublished:
integrationEventsConsumed:
-->

## Linguagem ubíqua

- **EventoESocial** — raiz de agregado que carrega o XML de UM evento do leiaute e a máquina de estados.
- **ChaveIdempotencia** — `(tipoEvento, idNegocio, competencia)`; garante UM evento por chave de negócio.
- **Lote** — `envioLoteEventos` com ≤ 50 eventos E ≤ 5 MB (limites CONFIRMADOS no MOS v1.15).
- **Gateway** — porta de transmissão (ACL); impl. SIMULADA por ora, SOAP 1.2/mTLS real pendente de credenciais.

## Máquina de estados (ESOCIAL-SPEC §4.4)

```
Gerado ──assinar──▶ Assinado ──empacotar+enviar──▶ Transmitido(protocolo)
   │                                                      │
   │                                            consultar lote (polling)
   ▼                                                      ▼
RejeitadoLocal (XSD inválido)              Processado(nrRecibo)  /  Rejeitado(erros)
```

## Geração — origem no domínio

| Evento | Comando | Origem |
|---|---|---|
| S-1000 / S-1005 / S-1010 | GerarS1000 / GerarS1005 / GerarS1010 | Config do tenant (empregador/EFR) · catálogo de `RubricaFolha` |
| S-2200 / S-2299 | GerarS2200 / GerarS2299 | Agregado `Servidor` (admissão/desligamento) |
| S-1200 (RGPS) / S-1202 (RPPS) | GerarRemuneracaoFolha | `FolhaDePagamento` FECHADA + `EventoFolha` (roteador por regime) |
| S-1210 | GerarPagamentosFolha | `FolhaDePagamento` PAGA |
| S-1299 | GerarFechamentoFolha | Orquestração (folha Fechada/Paga) |

## Cenários (Given/When/Then)

### Geração idempotente
- **Given** uma folha fechada com 1 servidor RPPS **When** GerarRemuneracaoFolha é executado 2x **Then**
  existe exatamente 1 evento S-1202 (mesma chave de negócio não duplica).
- **Given** o ente sem CNPJ configurado **When** qualquer geração é solicitada **Then** a geração é
  recusada (fail-closed — CLAUDE.md §16).

### Roteador remuneração
- **Given** servidor RPPS **When** gera remuneração **Then** evento = S-1202 e `tpRegPrev=2`.
- **Given** servidor RGPS **When** gera remuneração **Then** evento = S-1200 e `tpRegPrev=1`.

### Assinatura (reusa Cofre)
- **Given** evento Gerado **When** AssinarEventoESocial **Then** estado = Assinado (XML-DSig A1, perfil
  eSocial confirmado no MOS v1.15 §6.7; `IAssinaturaEmEscopoDedicado` — guarda H5).
- Re-assinar um evento Assinado é no-op (idempotente).

### Lote + transmissão
- **Given** 120 eventos assinados **When** TransmitirEventosAssinados **Then** são montados 3 lotes
  (50 + 50 + 20), cada um ≤ 50 eventos e ≤ 5 MB.
- **Given** um lote > 50 eventos ou > 5 MB **Then** o lote é recusado ANTES de gastar cota (erros 613/612).

### Retorno
- **Given** eventos Transmitidos **When** ConsultarRetornosESocial e o lote foi processado **Then** cada
  evento aceito recebe `nrRecibo` e vai a Processado; rejeitados vão a Rejeitado (re-gerável).

### Inspeção / auditoria
- **Given** eventos eSocial do tenant **When** ListarEventosESocial **Then** retorna estado, XML gerado
  (fiel ao leiaute), protocolo e `nrRecibo` por evento — apenas do tenant atual (Global Query Filter).

## Pendências `// TODO(validar-oficial)` / `// TODO(prod)`

- Pacote XSD da versão travada (S-1.3 NT 06/2026) + Tabelas 03/20/21/23/categorias/tpRegPrev (Anexo I).
- `codCateg`/`tpRegPrev` explícitos no `Servidor` (hoje derivados do regime).
- Gateway SOAP 1.2 + mTLS + Polly em Produção Restrita (creds do ente) — substitui o `ESocialGatewaySimulado`.

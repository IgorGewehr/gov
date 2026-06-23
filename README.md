# Tensorroot.Gov

**ERP GovTech SaaS multi-tenant** para **Prefeituras (Poder Executivo)** e **Câmaras de Vereadores
(Poder Legislativo)** do Brasil. Sistema de **missão crítica** — processa dinheiro público e dados
sensíveis sob escrutínio do **Tribunal de Contas (foco TCE-RS e padrão nacional)**.

> Piloto de produção: **Município de Maximiliano de Almeida/RS**.

---

## Visão Geral

- **Arquitetura:** Monolito Modular + Clean Architecture + DDD Tático ([ADR-0001](docs/adr/0001-monolito-modular-clean-architecture.md)).
- **Multi-tenant** com **ativação modular por tenant** — cada ente licencia só os módulos que usa ([ADR-0002](docs/adr/0002-multitenancy-ativacao-modular.md)).
- **Backend:** .NET 8 · C# 12 · EF Core 8 · MediatR · Serilog · OpenTelemetry · Polly.
- **Frontend:** React + **gov.br Design System** ([ADR-0004](docs/adr/0004-frontend-react-govbr-ds.md)).
- **Regras de engenharia:** ver [`CLAUDE.md`](CLAUDE.md) (constituição estrita).

## Estado atual (provado em runtime)

| Marco | Entrega | Status |
|---|---|---|
| **M0** | Build + bugs críticos | ✅ |
| **M1** | Autorização organizacional (UO + escopo + ABAC) | ✅ |
| **M2** | Contabilidade PCASP (lançamento automático, balancete ΣD=ΣC) | ✅ |
| **M3** | Demonstrações DCASP + MSC | ✅ |
| **M4** | Prestação de contas TCE-RS/SICONFI (remessa SIAPC + protocolo) | ✅ |
| — | Cofre A1 (envelope encryption) · Outbox resiliente · Legislativo demonstrável | ✅ |
| — | Planejamento PPA/LDO/LOA · Encerramento de exercício (resultado + RAP) | ✅ |
| **M5** | RH: folha mensal + 13º/férias/rescisão · ponto (software + coletor REP) · eSocial estrutural · remessa folha TCE | ✅ |
| **M6** | Tributos: IPTU · ISS (NFS-e passiva) · ITBI (Tema 1.113) · taxas/COSIP/alvarás · dívida ativa/CDA/protesto | ✅ |
| — | Segurança: 6 críticos + 5 altos do red-team fechados · auditoria hash-chain imutável | ✅ |
| **M7–M10** | Saúde/Educação/Assistência · Cidadão/Gestor/BI · Suprimentos/QA · **Prontidão PoC/Go-live** | 🔜 |

**Build 0 erros / 0 avisos** (warnings=errors) · **~1.067 testes backend + 192 frontend** · fitness/arquitetura verdes. Acompanhe em [`docs/progresso/`](docs/progresso/).

> **Honestidade "simulado vs oficial":** NFS-e/ADN é integração real; TCE/SICONFI geram artefato correto com validação **local** (validação no PAD **oficial** pendente do leiaute MT 2026); demais integrações chegam nos marcos M5–M10. Detalhe verificado em [`docs/estudo/DIAGNOSTICO-VS-REAL.md`](docs/estudo/DIAGNOSTICO-VS-REAL.md).

### Para o time
- 🚀 **Rodar/testar/depurar:** [`docs/RUNBOOK.md`](docs/RUNBOOK.md) · login demo `admin@tensorroot.gov` / `Mudar@123`.
- 📐 **Decisões com trade-offs:** [`docs/adr/`](docs/adr/) · **onde está cada coisa no código:** [`docs/MAPA-TOPICO-CODIGO.md`](docs/MAPA-TOPICO-CODIGO.md).
- 📚 **Estudo guiado (.NET + GovTech):** abra [`docs/roadmap_team.html`](docs/roadmap_team.html) no navegador.

## Bounded Contexts (11)

| Módulo | Poder | Escopo | Spec |
|---|---|---|---|
| Administracao | Ambos | Compras, Licitações (Lei 14.133), Contratos, Aditivos | [README](src/Modules/Administracao/README.md) |
| Financas | Ambos | Orçamento, Empenho→Liquidação→Pagamento, Contabilidade | [README](src/Modules/Financas/README.md) |
| Tributos | Executivo | IPTU/ISS/Taxas, **NFS-e via ADN (passiva)**, Dívida Ativa | [README](src/Modules/Tributos/README.md) |
| RecursosHumanos | Ambos | Folha, Cargos, Ponto, eSocial | [README](src/Modules/RecursosHumanos/README.md) |
| Patrimonio | Ambos | Bens, Depreciação, Almoxarifado, Frota | [README](src/Modules/Patrimonio/README.md) |
| Protocolo | Ambos | Processo eletrônico, assinatura (Lei 14.063), GED | [README](src/Modules/Protocolo/README.md) |
| Saude | Executivo | UBS, PEP, Regulação, Farmácia, Imunização, Telessaúde | [README](src/Modules/Saude/README.md) |
| Educacao | Executivo | Escolas, Matrículas, Diário, BNCC, Merenda, Transporte | [README](src/Modules/Educacao/README.md) |
| AssistenciaSocial | Executivo | SUAS, CRAS/CREAS, CadÚnico, Benefícios | [README](src/Modules/AssistenciaSocial/README.md) |
| Legislativo | Legislativo | Sessões, Proposições, Comissões, Votação | [README](src/Modules/Legislativo/README.md) |
| Transparencia | Ambos | Dados abertos (LAI), remessas TCE-RS, SICONFI/MSC | [README](src/Modules/Transparencia/README.md) |

## Estrutura

```
src/
  SharedKernel/                 primitivos de domínio + Value Objects (Cnpj, Cpf)
  BuildingBlocks/               behaviors MediatR + Outbox/Auditoria/Tenant (infra)
  Modules/<Modulo>/             Domain · Application · Infrastructure · Contracts
  ApiHost/                      Composition Root (web) + ativação modular por tenant
  Workers/NfseSync/             sincronização diária NFS-e do ADN (integração passiva)
tests/
  ArchitectureTests/            fitness functions (NetArchTest)
docs/
  adr/                          decisões arquiteturais
  architecture/                 diagramas e visões
  design-system/                Design System Constitution (UI/UX gov.br DS)
```

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 8 (C# 12) |
| Dados | EF Core 8 · Azure SQL |
| Mensageria | MediatR (in-process) + Outbox |
| Resiliência | Polly (retry + circuit breaker) |
| Observabilidade | Serilog + OpenTelemetry |
| Segurança | JWT Bearer · RBAC · Azure Key Vault (certificados A1) |
| Frontend | React + gov.br Design System |
| Cloud | Azure App Service · Azure SQL · Blob Storage |

## Build

> O alvo é **.NET 8** (`net8.0`). O `global.json` usa `rollForward: latestMajor`, então o SDK .NET 10
> compila a solução; para **executar** o runtime net8 localmente, instale o runtime .NET 8.

```bash
dotnet restore
dotnet build -c Release
dotnet test            # testes de arquitetura (Fase 6)
```

## Roadmap (marcos M0–M10)

Execução por **marcos** (ver progresso real em [`docs/progresso/`](docs/progresso/) e plano em [`docs/planejamento/PLANO-MESTRE.md`](docs/planejamento/PLANO-MESTRE.md)):

- **M0–M4 ✅** — build, autorização, espinha fiscal (PCASP → DCASP/MSC → prestação TCE-RS) + PPA/LDO/LOA + encerramento de exercício.
- **M5 ✅** RH (folha mensal + ciclo anual + ponto/coletor + eSocial + remessa folha) · **M6 ✅** Tributos (IPTU/ISS/ITBI/taxas/dívida) · **M7** Saúde/Educação/Assistência · **M8** Cidadão/Gestor/BI · **M9** Suprimentos + compliance + QA.
- **M10** — **Prontidão para PoC + Go-live** (capstone): validação no oficial, avaliações isentas, deploy/infra, segurança/LGPD final. "Pronto" = validado no oficial + deployável no município, não só código.

## Compliance

LGPD · LAI (Lei 12.527/2011) · LRF (LC 101/2000) · Lei 14.133/2021 · prestação de contas TCE-RS (SIAPC/PAD).
Auditoria imutável de toda mutação de estado. Segredos exclusivamente no Azure Key Vault.

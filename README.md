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
- **Convenções de engenharia:** ver [`docs/CONVENCOES-ENGENHARIA.md`](docs/CONVENCOES-ENGENHARIA.md) (regras estritas).

## O que o sistema faz

ERP integrado para os dois poderes municipais, com a espinha fiscal completa e os módulos setoriais:

- **Espinha fiscal:** autorização organizacional (UO + escopo + ABAC) · contabilidade PCASP (lançamento
  automático, balancete ΣD=ΣC) · demonstrações DCASP + MSC · prestação de contas TCE-RS/SICONFI
  (remessa SIAPC + protocolo) · planejamento PPA/LDO/LOA · encerramento de exercício (resultado + RAP).
- **RH:** folha mensal + 13º/férias/rescisão · ponto (software + coletor REP) · eSocial estrutural ·
  remessa de folha ao TCE.
- **Tributos:** IPTU · ISS (NFS-e passiva) · ITBI (Tema 1.113) · taxas/COSIP/alvarás ·
  dívida ativa/CDA/protesto.
- **Setoriais e suporte:** Saúde · Educação · Assistência Social · Legislativo · Patrimônio ·
  Protocolo · Transparência (LAI / dados abertos).
- **Plataforma:** Cofre A1 (envelope encryption) · Outbox resiliente · auditoria hash-chain imutável.

Build **0 erros / 0 avisos** (warnings=errors) · suíte de testes backend + frontend · fitness functions
de arquitetura verdes.

> **Honestidade "simulado vs oficial":** NFS-e/ADN é integração **real**; as remessas TCE-RS/SICONFI
> geram o artefato correto com validação **local** (a validação no PAD **oficial** depende do leiaute MT
> do exercício e das credenciais do município — ver [`docs/go-live/`](docs/go-live/)). As demais
> integrações governamentais que dependem de credencial/canal de produção estão marcadas no código e
> documentadas em [`docs/REVISAO-HUMANA/`](docs/REVISAO-HUMANA/).

### Por onde começar
- 🚀 **Rodar/testar/depurar:** [`docs/RUNBOOK.md`](docs/RUNBOOK.md) · login demo `admin@tensorroot.gov` / `Mudar@123`.
- 📐 **Decisões com trade-offs:** [`docs/adr/`](docs/adr/).
- ✅ **Pontos para revisão humana (contador/jurídico):** [`docs/REVISAO-HUMANA/`](docs/REVISAO-HUMANA/).
- 🌐 **Operação de go-live (credenciais, validação oficial):** [`docs/go-live/`](docs/go-live/).
- 📏 **Convenções de engenharia:** [`docs/CONVENCOES-ENGENHARIA.md`](docs/CONVENCOES-ENGENHARIA.md).

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
  CONVENCOES-ENGENHARIA.md      regras estritas de engenharia
  adr/                          decisões arquiteturais
  architecture/                 designs por tópico + specs oficiais
  design-system/                Design System Constitution (UI/UX gov.br DS)
  governanca/                   Rules-as-Code
  go-live/                      credenciais e validação oficial para entrar em produção
  normas/                       fontes normativas
  REVISAO-HUMANA/               pontos para conferência de contador/jurídico
  RUNBOOK.md                    guia operacional para rodar/testar/depurar
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
> Detalhes e troubleshooting no [`RUNBOOK`](docs/RUNBOOK.md).

```bash
dotnet restore
dotnet build -c Release
dotnet test            # inclui as fitness functions de arquitetura
```

## Compliance

LGPD · LAI (Lei 12.527/2011) · LRF (LC 101/2000) · Lei 14.133/2021 · prestação de contas TCE-RS (SIAPC/PAD).
Auditoria imutável de toda mutação de estado. Segredos exclusivamente no Azure Key Vault.

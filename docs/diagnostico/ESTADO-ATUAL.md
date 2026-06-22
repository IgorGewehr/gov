> ⚠️ **FOTO PRÉ-M0 (histórico).** Estado real e provado: ver `docs/progresso/progresso.json` e `docs/estudo/DIAGNOSTICO-VS-REAL.md`.

# Tensorroot.Gov — ESTADO ATUAL (Onde estamos EXATAMENTE)

> Síntese das auditorias empíricas de `docs/diagnostico/partes/auditoria-*.md` (todas datadas 2026-06-22,
> com leitura direta do código, contagem de arquivos/handlers/endpoints/testes e build executado).
> Honesto sobre **real × stub × ausente**. Foco do dono: **Contabilidade e Prestação de Contas ao TCE-RS**.

---

## Resumo executivo (10 linhas)

1. **O backend NÃO COMPILA hoje** (2 erros em `EmpenhoRepository`, Finanças) — CI vermelho, e os 575 testes não rodam. Bloqueador total nº 0.
2. O **domínio é real, rico e não-anêmico** em todos os 12 módulos (11 BCs + Identidade): factories, invariantes, máquinas de estado, VOs, domain events — não há fachada no núcleo de negócio.
3. **Patrimônio, Administração e RH são os módulos mais maduros**; **Tributos (declarado "referência") é na prática o mais raso** (5 endpoints, ~5 testes).
4. **A maior preocupação do dono é a mais incompleta:** Contabilidade **PCASP/MCASP não existe (zero linhas)**; sem partidas dobradas, sem plano de contas, sem balancete.
5. **PPA/LDO/LOA (planejamento orçamentário) ausente**; existe só o ciclo de execução da despesa (Empenho→Liquidação→Pagamento) e Restos a Pagar.
6. **Prestação de contas ao TCE é fachada:** a máquina de estados da Remessa/Declaração funciona, mas opera sobre dados fixos; o **leiaute oficial SIAPC/PAD e a MSC do SICONFI não são gerados**.
7. **De ~19 integrações governamentais, só 1 é real** (NFS-e/ADN com HTTP+Polly). Todo o resto é `Simulado*` ou porta-sem-cliente.
8. **Assinatura A1, SOAP e Azure Key Vault inexistentes em runtime** — pré-requisitos de TCE, eSocial e Protocolo; pacotes e Bicep prontos, mas integração não fiada.
9. **Plataforma sólida** (database-per-tenant, query filter, Outbox, JWT/RBAC negar-por-padrão, BCrypt) com falhas pontuais críticas: ordem de interceptors zera `TenantId` na auditoria de INSERTs, `/admin/tenants` sem RBAC, auditoria não-imutável de fato.
10. **Frontend cobre 100% dos endpoints existentes** (58 rotas, todos os 12 módulos), mas é limitado pelo backend; zero testes de acessibilidade apesar de WCAG AA obrigatório.

---

## Tabela por módulo (Backend × Frontend)

| Módulo | Backend: Entidades/Agregados | Casos de uso (handlers) | Endpoints | Testes | Profundidade backend | Frontend (rotas / cobertura endpoints) |
|---|---|---|---:|---:|---|---|
| **Tributos** | Contribuinte, Lancamento, DividaAtiva, NotaFiscalServico | 6 | 5 | ~5 | 🟡 **Raso em superfície.** DívidaAtiva/CDA/prescrição maduros; **sem motor IPTU/ISS/ITBI, sem Taxas/Alvarás** | 1 rota (Dívida Ativa + modais) / 100% dos 5 |
| **Financas** | Dotacao, Empenho, Liquidacao, OrdemPagamento, RestoAPagar, ReceitaArrecadada | 22 | **4** | ~3 | 🟠 Ciclo Lei 4.320 forte; **18 de 22 handlers não expostos via HTTP**; **PCASP e PPA/LDO/LOA ausentes** | 2 rotas (Empenho, sem LISTA) / 100% dos 4 |
| **Administracao** | Licitacao (+Lote/Proposta/Habilitacao/Recurso), Contrato (+Aditivo/Apostila/Garantia), Fornecedor (+Sancao) | 33 | 17 | ~68 | ✅ **Maduro.** Lei 14.133 completa; saga orçamento↔contrato real. PNCP/Receita são placeholders | 7 rotas / 100% dos 17 |
| **RecursosHumanos** | Servidor (+Dependente), Cargo (+PlanoCargos), FolhaPagamento (+EventoFolha) | 22 | 22 | ~60 | ✅ Maduro. Ciclo estatutário + folha com abate-teto. **eSocial e Ponto inexistentes**; sem motor INSS/RPPS/IRRF | 6 rotas / 100% dos 22 |
| **Patrimonio** | BemPatrimonial, ItemEstoque, Veiculo (+ ~20 entidades filhas) | 34 | 33 | ~71 | ✅ **O mais completo.** Depreciação MCASP real, PEPS/Médio, frota. Falta contrapartida contábil em Finanças | 6 rotas / 100% dos 33 |
| **Saude** | Paciente, Atendimento (SOAP/CID/CIAP), SolicitacaoRegulacao | 27 | 25 | 54 | ✅ Domínio rico. **SI-PNI, HÓRUS, telessaúde ausentes**; toda integração (SISREG/RNDS/SISAB/CADSUS/CNES/A1) é `Simulado*` | 5 rotas / 100% dos 25 |
| **Educacao** | Escola (INEP), Matricula, DiarioClasse | 19 | 19 | 53 | ✅ Domínio rico. **PNAE (merenda) e PNATE (transporte) ausentes**; EducaCenso/INEP só armazena local | 5 rotas / 100% dos 19 |
| **AssistenciaSocial** | Familia, Beneficio, ProntuarioSuas (com trilha LGPD) | 15 | 15 | 63 | ✅ Domínio rico, parametrização por vigência. CadÚnico/MDS só leitura simulada | 7 rotas / 100% dos 15 |
| **Protocolo** | Processo (NUP/CONARQ), Documento (assinatura/hash) | 17 | 12 | 46 | ✅ Domínio rico. NUP real (Dec. 8.539). **Carimbo de tempo é relógio local** (ACT ICP ausente); GED de binário ausente. Race condition no sequencial NUP | 4 rotas / 100% dos 12 |
| **Legislativo** | Sessao, Votacao (maioria/modalidade), Proposicao | 33 | 31 | 69 | ✅ **Maior cobertura de casos de uso.** Sessões/votação/painel completos. Sem integração externa (correto) | 6 rotas / 100% dos 31 |
| **Transparencia** | RemessaTce, DeclaracaoFiscal (MatrizSaldos balanceada) | 14 | 13 | 45 | 🔴 **Andaime de processo real, conteúdo fictício.** Estados/hash/prazo OK; **leiaute TCE-RS e MSC SICONFI NÃO gerados**; consome evento que ninguém publica | 4 rotas / 100% dos 13 |
| **Identidade / Admin** | Usuario, Papel, Permissão, AuditTrail, Tenant/Modulo | — | 14+ | ~5 (isolamento) | ✅ JWT/RBAC/BCrypt maduros. Endpoint `/admin/tenants` sem RBAC (escalonamento) | 5 rotas / 100% |

Totais frontend: **58 rotas, 28 ListPages, 25 DetailPages, 39 testes**. Stack React 18 + Vite 5 + TS 5 + TanStack Query 5. Nenhuma página stub em produção.

---

## Contabilidade e Prestação de Contas ao TCE (o coração da preocupação do dono)

**Estado: a parte mais incompleta de todo o sistema.**

- **PCASP/MCASP — INEXISTENTE (contagem = 0).** Busca por `PlanoDeContas|ContaContabil|LancamentoContabil|PartidaContabil|EventoContabil|Balancete` em `src/Modules/Financas` retorna **0 arquivos**. Não há plano de contas, **não há partida dobrada em lugar nenhum**, sem roteiro fato→partidas, sem balancete, sem lançamento contábil automático a partir dos domain events. É a FONTE de tudo e está 100% ausente.
- **PPA/LDO/LOA — ausente** como agregado. Existe só `DotacaoOrcamentaria` (execução), não o planejamento (programas, metas, anexos LOA).
- **MSC — read model órfão.** `MatrizSaldos` calcula ΣD=ΣC, mas é alimentado por dados externos, não por lançamentos. O consumidor `ReceberMSCGeradaHandler` escuta `MSCGeradaIntegrationEvent` — **e ninguém publica esse evento (0 publishers)**. Pipeline contabilidade→MSC nunca dispara.
- **Leiaute SIAPC/PAD — genérico/fake.** `GerarRemessaTceHandler.MontarPacote` gera `.txt` com linhas `Tipo|Conteudo` de **3 itens fixos hardcoded** (`SimuladoPublicacaoTransparenciaRepository`). Sem posição/tamanho/formato campo-a-campo. **Não passaria no e-Validador real.**
- **e-Validador (RDI) — STUB** (`SimuladoEValidadorTce` aprova qualquer pacote com ≥1 arquivo).
- **Transmissão (SIAPC/PAD e SICONFI) — STUB total.** `TransmitirAsync → Task.CompletedTask`; protocolo SICONFI determinístico fake. Sem SOAP, sem HTTP, sem Polly.
- **Assinatura A1 — INEXISTENTE.** Zero `SignedXml`/`X509Certificate2`/Key Vault no `src`. Toda menção é comentário XML-doc.
- ✅ **O que está real:** máquina de estados de Remessa e Declaração (45 testes de fluxo), hash de integridade, alerta de prazo LRF, Outbox e ACL de entrada bem separada. Andaime correto, vazio de substância.

**Conclusão honesta:** **nada nesta cadeia geraria uma prestação de contas válida ao TCE-RS hoje.** Fases 2.2 (PCASP) e 2.3 (TCE real) não foram iniciadas. TODOs `revisao-contabil` em `EncerrarExercicio.cs` e `ClassificacaoOrcamentaria.cs` confirmam que o time sinalizou os pontos sensíveis como pendentes.

---

## Integrações Governamentais

**De ~19 integrações previstas (§8), apenas 1 é real de produção.**

| Estado | Integrações |
|---|---|
| ✅ **REAL** | **NFS-e/ADN** (`AdnNfseGateway`, HTTP+Polly, único ponto com `AddStandardResilienceHandler`; worker `NfseSync` maduro). **CNAB 240** (gerador de arquivo real — não é transmissão online) |
| 🔴 **STUB `Simulado*`** | SIAPC/PAD, e-Validador, catálogo de leiaute, calendário fiscal, SICONFI/MSC, Receita/CNPJ, RNDS, SISAB, SISREG, CADSUS, CNES, Assinatura ICP (Saúde), CadÚnico/MDS |
| 🟡 **PARCIAL/LOCAL** | eSocial S-1010 (valida rubrica contra config local, não fala com eSocial); Carimbo de tempo Protocolo (relógio local, sem ACT ICP) |
| ❌ **AUSENTE (sem porta)** | eSocial (envio de eventos S-1000/1200/2200…), PNCP (só grava nº + emite evento, sem cliente HTTP), EducaCenso/INEP (só armazena local) |

**Transversais críticos (medidos = 0 ocorrências):** assinatura A1 (`SignedXml`/`X509Certificate2`), SOAP (`System.ServiceModel`), Azure Key Vault em runtime (`AddAzureKeyVault`/`SecretClient`/`DefaultAzureCredential`). Ressalva no único real: ADN sobe `HttpClient` sem autenticação configurada (URL default `adn.invalido.local`).

Ordem necessária para o TCE funcionar de fato: **Key Vault + A1 → cliente SIAPC/PAD + leiaute oficial + e-Validador → SICONFI/MSC → eSocial.**

---

## Plataforma e Segurança

| Capacidade | Estado |
|---|---|
| Database-per-tenant + Global Query Filter + TenantInterceptor | ✅ Real e sólido (resolver com cache, carimbo + exceção cross-tenant) |
| Outbox (transacional + drain por tenant/módulo a cada 30s) | 🟡 Real, mas **sem retry/back-off/dead-letter** (evento "veneno" reprocessa para sempre); sem idempotência no consumidor |
| JWT (HS256, claim `perm`) + RBAC negar-por-padrão + BCrypt(12) + anti-timing | ✅ Real e maduro |
| Auditoria de mutações + visualizador admin | 🟡 **Bug crítico TCE:** ordem `Audit,Tenant,Outbox` faz o audit ler `TenantId` ANTES do carimbo → **INSERTs gravam `TenantId=Guid.Empty`**, somem da trilha do tenant. AuditTrail **não é imutável de fato** (sem WORM/append-only/hash-chain). **Sem trilha de leitura LGPD** |
| Provisionamento de tenant + gating de licença (403 módulo não-licenciado) | 🟡 Real, mas **`/admin/tenants` só `.RequireAuthorization()` sem RBAC** → qualquer token provisiona tenants (escalonamento) |
| Pipeline MediatR | 🟡 Só **Validation + Logging**; faltam **Transaction/UnitOfWork e Idempotency** behaviors (§10) |
| Certificado A1 / Key Vault em runtime | 🔴 Stub — Bicep + pacotes prontos, integração **não fiada** no `Program.cs` |
| Credenciais default | ⚠️ `admin@tensorroot.gov / Mudar@123` versionadas em código e `appsettings.Development.json` |

---

## QA e Infraestrutura

- 🔴 **Build Release FALHA** — `EmpenhoRepository` não implementa 2 membros de `IEmpenhoRepository` (`ListarPorDotacaoAsync`, `ListarComSaldoAbertoPorExercicioAsync`), já consumidos por `ObterEmpenho` e `EncerrarExercicio`. **Bloqueador nº 0.** Como o CI roda `build` antes de `test --no-build`, **os 575 testes não executam hoje**.
- **Testes:** 51 arquivos, **575 `[Fact]`/`[Theory]`** (verdes só no histórico anterior ao bug), quase todos de domínio/handler in-memory. Apenas 8 `[InlineData]`. **Sem testes HTTP E2E** sobre `WebApplicationFactory`; **sem cobertura medida**; isolamento cross-tenant testado **só na Identidade**.
- ✅ **Fitness Functions reais** (NetArchTest: Domain↛Infra/EF/AspNet, isolamento de módulo, SharedKernel limpo), **Maintainability** (nenhum `.cs` > 500 linhas) e **Spec-Code Consistency** (`*.rules.md` × código) — mas não rodam hoje (build quebrado).
- **CI/CD:** `ci.yml` e `deploy.yml` (OIDC sem segredo) estruturalmente corretos, mas **CI vermelho** por #0. Riscos: **deploy automático em todo push para `main`** sem gate; **SQL firewall `AllowAllAzureIps` (0.0.0.0)** + `publicNetworkAccess: Enabled`; SKUs de dev (B1/S0); Bicep **não cria o Worker NfseSync**.
- **Observabilidade:** Serilog + OpenTelemetry **fiados, mas sem exportador** (telemetria coletada e descartada — "FASE 5") e **sem enriquecer logs com `TenantId`/`CorrelationId`** (§11 exige); `/health` vazio sem checks de SQL/KV.
- **Frontend QA:** `axe-core` instalado mas **0 testes de acessibilidade** (WCAG 2.1 AA / eMAG sem rede de proteção); adesão ao gov.br DS parcial (tokens de cor + skip-link, poucos componentes `br-*`); gating `<Can>` desigual (Tributos 1, Finanças 2 vs 4-6 nos demais); 5 modais de ação >600 linhas.
- **Divergência dev/prod:** SQLite compartilha 1 arquivo por tenant entre módulos (vs schema isolado em SqlServer); `EnsureCreatedAsync`/SQLite vs `MigrateAsync`/SqlServer.

---

## Prioridades consolidadas (ordem de risco para o dono)

1. 🔴 **Corrigir `EmpenhoRepository`** (2 métodos) — destrava build, CI e os 575 testes. Bloqueador absoluto.
2. 🔴 **Implementar Contabilidade PCASP/MCASP** (plano de contas, partida dobrada, evento contábil, balancete, lançamento automático) — a FONTE de toda prestação de contas; hoje 100% ausente.
3. 🔴 **Corrigir ordem dos interceptors** (Tenant antes de Audit/Outbox) — senão a trilha do TCE perde os INSERTs.
4. 🔴 **Fiar Key Vault + assinatura A1 real** (X509 + XMLDSig) e **leiaute oficial SIAPC/PAD + e-Validador + transmissão SICOE/SICONFI**.
5. 🟠 **Expor a API de Finanças** (18 handlers órfãos) + PPA/LDO/LOA; **RBAC no `/admin/tenants`**; restringir SQL firewall; gate no deploy de prod.
6. 🟠 **eSocial** (eventos/transmissão) e **motor de cálculo de folha** (INSS/RPPS/IRRF); motor de apuração tributária (IPTU/ISS/ITBI/Taxas) em Tributos.
7. 🟡 Imutabilidade real da AuditTrail + trilha de leitura LGPD; exportador OTel + enrichers; retry/dead-letter no Outbox; behaviors Transaction/Idempotency; testes de acessibilidade.

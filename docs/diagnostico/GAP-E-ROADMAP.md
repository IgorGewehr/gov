> ⚠️ **FOTO PRÉ-M0 (histórico).** Estado real e provado: ver `docs/progresso/progresso.json` e `docs/estudo/DIAGNOSTICO-VS-REAL.md`.

# Tensorroot.Gov — GAP & ROADMAP (Onde estamos → Onde precisamos chegar → O que falta → Sequência)

> Cruzamento de `ESTADO-ATUAL.md` (auditoria empírica do código) × `REQUISITOS-GOVTECH.md` (norma de produção).
> Foco do dono: **CONTABILIDADE** e **PRESTAÇÃO DE CONTAS AO TCE-RS**. Piloto: Maximiliano de Almeida/RS.
> Data: 2026-06-22.
>
> **Legenda de prioridade:** 🔴 bloqueador / fonte da verdade · 🟠 alto valor fiscal · 🟡 robustez/compliance · 🟢 evolução.
> **[OFICIAL]** = depende de fonte/leiaute oficial (validar versão do exercício ANTES de implementar — CLAUDE.md §7/§16). Nunca hard-coded.
>
> **Regra de ouro herdada:** nada de plano de contas próprio, roteiro de lançamento caseiro ou formato de remessa improvisado. Em dúvida sobre regra fiscal/legal: parar e pesquisar fonte oficial.

---

## 0. BLOQUEADOR ABSOLUTO (antes de qualquer marco)

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🔴 Backend **não compila** (`EmpenhoRepository` não implementa `ListarPorDotacaoAsync` e `ListarComSaldoAbertoPorExercicioAsync`); CI vermelho; 575 testes não rodam. | Build verde, CI rodando, fitness functions ativas. | 1. Implementar os 2 métodos faltantes de `IEmpenhoRepository`. 2. Confirmar `dotnet build -c Release` verde. 3. Confirmar os 575 testes + NetArchTest verdes. 4. Trocar deploy automático em `main` por gate manual. |

---

## EIXO 1 — NÚCLEO FISCAL: CONTABILIDADE → TCE (a maior preocupação do dono)

### 1.1 Contabilidade PCASP/MCASP — **a FONTE de tudo, hoje 100% ausente (0 linhas)**

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🔴 Zero `PlanoDeContas/ContaContabil/LancamentoContabil/PartidaContabil/Balancete`. Sem partida dobrada. `EncerrarExercicio.cs` e `ClassificacaoOrcamentaria.cs` com TODO `revisao-contabil`. | Motor contábil determinístico: PCASP oficial carregado, partidas dobradas sempre fechadas (3 enfoques: orçamentário+patrimonial+controle), roteiros MCASP por evento, balancete, 7 demonstrações DCASP. | 1. **[OFICIAL]** Carregar **PCASP** (8 classes, atributos natureza/indicador F-P) versionável por exercício — STN/MCASP 11ª ed. 2. Agregado `LancamentoContabil` + `PartidaContabil` com invariante ΣD=ΣC. 3. **[OFICIAL]** Roteiros de contabilização MCASP por evento (empenho, liquidação, pagamento, anulações, RP processados/não). 4. Subscrever domain events de Finanças/Patrimônio → gerar lançamento automático. 5. Balancete e **[OFICIAL]** 7 demonstrações DCASP (Bal. Orçamentário/Financeiro/Patrimonial, DVP, DMPL, DFC, Notas). |

### 1.2 PPA/LDO/LOA — planejamento orçamentário ausente

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🟠 Existe só `DotacaoOrcamentaria` (execução), não o planejamento. | Ciclo de planejamento (programas, metas, anexos LOA) ligado à execução. | 1. Agregados PPA/LDO/LOA (programas, ações, metas). 2. **[OFICIAL]** Anexos da Lei 4.320 (Portaria STN 438/2012). 3. Vínculo LOA→Dotação→Empenho (vedar despesa sem dotação/empenho — art. 60). |

### 1.3 Matriz de Saldos Contábeis (MSC) — read model órfão

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🔴 `MatrizSaldos` calcula ΣD=ΣC mas é alimentada por dados externos. `ReceberMSCGeradaHandler` escuta evento que **ninguém publica** (0 publishers). Pipeline nunca dispara. | MSC derivada dos lançamentos PCASP: fonte da verdade que gera RREO/RGF/DCA. | 1. Publicar `MSCGeradaIntegrationEvent` a partir do motor contábil (1.1). 2. **[OFICIAL]** Regras Gerais MSC do exercício (Portaria STN 642/896) — conta PCASP, saldo, natureza, indicador F/P, correlações. 3. MSC Agregada (mensal) e de Encerramento (anual). |

### 1.4 Remessa TCE-RS (SIAPC/PAD/e-Validador) — fachada sobre dados fixos

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🔴 `GerarRemessaTceHandler` gera `.txt` com 3 linhas hard-coded. e-Validador é stub (aprova qualquer pacote). Transmissão `Task.CompletedTask`. Máquina de estados/hash/prazo OK. | Remessa largura-fixa real que passa no PAD/e-Validador e é transmitida. | 1. **[OFICIAL]** Leiaute SIAPC campo-a-campo (largura fixa, 1 reg/linha, CR/LF, sem binário; MT Vol. IV-V; PAD v25/26.x). 2. Geração com Código de Remessa único, conteúdo acumulado 1º-jan→data ref. 3. Validador RDI real. 4. Cliente de transmissão com Polly + ACL. |

### 1.5 SICONFI/STN (RREO, RGF, DCA)

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🔴 Transmissão SICONFI determinística fake. | RREO (bimestral), RGF (quadrimestral), DCA (anual) derivados da MSC, enviados via API SICONFI. | 1. **[OFICIAL]** Leiautes RREO/RGF/DCA (Portaria STN 896/2017) derivados da MSC. 2. Carga XBRL/arquivo + cliente API SICONFI. 3. Calendário fiscal versionável por exercício. |

### 1.6 Transversais de assinatura/segredo (pré-requisito de 1.4/1.5 e RH)

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🔴 Zero `SignedXml`/`X509Certificate2`/Key Vault em runtime (Bicep+pacotes prontos, não fiados). Ordem de interceptors zera `TenantId` em INSERTs. `/admin/tenants` sem RBAC. | A1 ICP-Brasil server-side (XAdES/PAdES) por tenant via Key Vault; trilha íntegra; admin protegido. | 1. Fiar **Azure Key Vault** no `Program.cs` (cert A1 por tenant). 2. Assinatura A1 real (X509 + XMLDSig). 3. **Corrigir ordem dos interceptors** (Tenant antes de Audit/Outbox). 4. RBAC no `/admin/tenants`. 5. Restringir SQL firewall; remover credenciais default versionadas. |

### 1.7 Expor API de Finanças

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🟠 22 handlers, só **4 expostos** via HTTP (18 órfãos). Frontend: 2 rotas, sem lista de empenho. | Ciclo da despesa completo navegável fim-a-fim. | 1. Expor os 18 handlers órfãos. 2. ListPage de Empenho/Liquidação/Pagamento/Restos a Pagar. 3. Gating `<Can>` consistente. |

---

## EIXO 2 — RECURSOS HUMANOS (alimenta remessa de folha TCE-RS)

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🟠 Domínio maduro (folha com abate-teto). **eSocial e Ponto inexistentes**; eSocial S-1010 só valida config local; sem motor INSS/RPPS/IRRF. | eSocial S-1.3 completo + ponto Port. 671/2021 + folha reconciliável → remessa TCE-RS (Res. 1099/2018). | 1. Motor de cálculo INSS/RPPS/IRRF (parametrizável por exercício). 2. **[OFICIAL]** Eventos eSocial S-1000/S-1200/S-1202/S-1207/S-1299 (XSD S-1.3), XML assinado A1, WebService SOAP + ambientes. 3. **[OFICIAL]** Ponto: AFD/AEJ (Port. MTP 671/2021), imutabilidade. 4. **[OFICIAL]** Remessa folha TCE-RS (Res. 1099/2018, leiaute `TCE_4810`). |

---

## EIXO 3 — TRIBUTOS (receita alimenta relatórios fiscais)

| Onde estamos | Onde precisamos chegar | O que falta |
|---|---|---|
| 🟠 Raso: 5 endpoints. DívidaAtiva/CDA/prescrição maduros; **sem motor IPTU/ISS/ITBI, sem Taxas/Alvarás**. NFS-e/ADN é a única integração real. | Motor de apuração tributária + cobrança + coexistência reforma. | 1. Motor IPTU (cadastro imobiliário/CIB, PGV, valor venal, lançamento em lote, carnê). 2. ISS (cadastro mobiliário, LC 116, retenção/Simples) e ITBI (Tema 1.124). 3. Taxas/COSIP/Alvarás/Contrib. Melhoria. 4. **[OFICIAL]** DAM código de barras FEBRABAN + PIX; retorno CNAB 240/400; SELIC/juros/multa. 5. **[OFICIAL]** Protesto CRA/IEPTB (Lei 9.492). 6. **[OFICIAL]** Coexistência ISS↔IBS (EC 132/LC 214 — teste 2026). |

---

## EIXO 4 — DOMÍNIOS COM RISCO FISCAL DE REPASSE (Saúde, Educação, Assistência)

| Domínio | Onde estamos | O que falta |
|---|---|---|
| **Saúde** 🟠 | Domínio rico; toda integração `Simulado*` (SISREG/RNDS/SISAB/CADSUS/CNES). | 1. **[OFICIAL]** RNDS (FHIR R4 + e-CNPJ) FHIR-first. 2. **[OFICIAL]** e-SUS APS→SISAB (cofinanciamento Port. 3.493/2024). 3. **[OFICIAL]** CNES/SIA/SIHD mensal (suspensão por 3 meses — Port. 708/2007). 4. **[OFICIAL]** SI-PNI via RNDS; farmácia e-SUS AF/BNAFAR (Port. 11.585/2026). Painel de cofinanciamento (venda). |
| **Educação** 🟠 | Domínio rico; **PNAE/PNATE ausentes**; EducaCenso só local. | 1. **[OFICIAL]** Educacenso/INEP (layout migrador). 2. **[OFICIAL]** SIOPE bimestral conciliado com PCASP/MSC. 3. **[OFICIAL]** PNAE (SiGPC, 30% agric. familiar) e PNATE (SiGPC mensal). |
| **Assistência** 🟡 | Domínio rico (trilha LGPD); CadÚnico/MDS só leitura simulada. | 1. **[OFICIAL]** Import/export por layout MDS (CadÚnico/CECAD, RMA, Censo SUAS, SUASWeb). 2. CPF/NIS como chave. |

---

## EIXO 5 — DOMÍNIOS MADUROS (fechar lacunas pontuais)

| Domínio | Onde estamos | O que falta |
|---|---|---|
| **Administração** ✅🟡 | Lei 14.133 completa; PNCP/Receita são placeholders. | 1. **[OFICIAL]** Cliente PNCP real (REST/JSON, JWT 1h) — **bloquear execução financeira sem nº de controle PNCP** (condição de eficácia). 2. PCA divulgado no PNCP. |
| **Patrimônio** ✅🟡 | O mais completo (depreciação MCASP, frota). | 1. Contrapartida contábil em Finanças (ligar a 1.1). |
| **Protocolo** ✅🟡 | NUP real; carimbo de tempo é relógio local; GED de binário ausente; race no sequencial NUP. | 1. **[OFICIAL]** Carimbo de tempo ACT ICP-Brasil. 2. Assinatura Lei 14.063 (3 níveis). 3. GED/SIGAD e-ARQ + TTDD CONARQ + PDF/A. 4. Corrigir race condition no NUP. |
| **Legislativo** ✅🟢 | Maior cobertura; sem integração externa (correto). | 1. **[OFICIAL]** Export LexML/PDF-A + dados abertos. 2. **[OFICIAL]** Prestação de contas art. 29-A ao TCE-RS. |

---

## EIXO 6 — PLATAFORMA, SEGURANÇA, QA (transversal)

| Onde estamos | O que falta |
|---|---|
| 🟡 Outbox sem retry/dead-letter/idempotência; pipeline só Validation+Logging; AuditTrail não imutável de fato; sem trilha de leitura LGPD; OTel sem exportador; `/health` vazio; 0 testes a11y; SQLite difere de SqlServer. | 1. Retry/back-off/dead-letter + idempotência no Outbox. 2. Behaviors Transaction/UnitOfWork + Idempotency (§10). 3. AuditTrail imutável real (WORM/hash-chain) + trilha de leitura LGPD. 4. Exportador OTel + enrichers `TenantId`/`CorrelationId`; health checks SQL/KV. 5. Testes E2E HTTP (`WebApplicationFactory`) + acessibilidade (axe-core/WCAG AA). 6. Convergir dev/prod (schema isolado, MigrateAsync). |

---

## ROADMAP POR MARCOS (resumo)

- **M0 — Destravar (dias).** Corrigir `EmpenhoRepository` → build/CI/575 testes verdes; gate no deploy de prod. [Eixo 0]
- **M1 — Fundação contábil + segurança.** PCASP oficial + partida dobrada + roteiros MCASP + balancete; corrigir ordem de interceptors; Key Vault + A1 real; RBAC `/admin/tenants`; expor API de Finanças. [1.1, 1.6, 1.7] **[OFICIAL: PCASP/MCASP]**
- **M2 — Demonstrações + MSC.** 7 demonstrações DCASP; MSC derivada dos lançamentos (publisher real); PPA/LDO/LOA. [1.2, 1.3] **[OFICIAL: Regras MSC]**
- **M3 — Prestação de contas ao TCE-RS.** Leiaute SIAPC/PAD largura-fixa + e-Validador real + transmissão; SICONFI RREO/RGF/DCA. **Meta do dono atingida.** [1.4, 1.5] **[OFICIAL: SIAPC/PAD, SICONFI]**
- **M4 — RH/eSocial + remessa de folha.** Motor INSS/RPPS/IRRF; eSocial S-1.3; ponto AFD/AEJ; remessa folha TCE-RS (Res. 1099). [Eixo 2] **[OFICIAL]**
- **M5 — Tributos.** Motor IPTU/ISS/ITBI/Taxas; DAM/CNAB/PIX; protesto; coexistência ISS↔IBS. [Eixo 3] **[OFICIAL]**
- **M6 — Risco de repasse.** Saúde (RNDS/SISAB/CNES), Educação (Educacenso/SIOPE/PNAE/PNATE), Assistência (layouts MDS). [Eixo 4] **[OFICIAL]**
- **M7 — Compliance & robustez.** PNCP bloqueante; Protocolo (A1/carimbo/GED); Legislativo (LexML/29-A); Outbox/behaviors/AuditTrail WORM/LGPD/OTel/a11y. [Eixos 5-6] **[OFICIAL: PNCP, CONARQ]**

> Sequência guiada pela preocupação do dono: **M0→M1→M2→M3 entregam o eixo Contabilidade→TCE de ponta a ponta** antes de qualquer outro domínio.

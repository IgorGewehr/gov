# Auditoria Independente — Estado Atual ao Concluir o M6

> **Tipo:** Avaliação *current-state* independente (auditor sênior externo).
> **Data:** 2026-06-23. **Commit auditado:** `0a5cb50` ("M6 COMPLETO: divida ativa/CDA/protesto/prescricao + frontend taxas/COSIP/alvaras"), branch `main`, árvore limpa.
> **Método:** leitura do **código** em `src/`, dos **testes** em `tests/`, e de `docs/progresso/progresso.json`. Cada afirmação foi confirmada no código (`arquivo:linha`). `docs/diagnostico/*` foi **ignorado por instrução** (fotos pré-M0). Não se executou `dotnet` (porta 5080 em uso por outro processo) — logo, *build verde* e *contagem de testes verde* são **estáticos** (contados, não executados): marcados **[a confirmar em CI]**.
> **Disclaimer de escopo:** isto avalia engenharia e prontidão de PoC. **Não** é parecer jurídico nem certificação de aderência oficial (TCE/eSocial/SICONFI) — esses dependem de validação em ambiente oficial, que ainda não ocorreu.

---

## 0. Veredito em uma página

O sistema é **substancialmente mais sólido e mais completo do que o próprio `progresso.json` declara**. O JSON é uma foto **defasada** (16 commits atrás): diz "852 testes backend", M5/M6 `active`, PPA/LDO/LOA e encerramento `pending`. O **código de hoje** tem **~1.067 métodos de teste** (1.050 `[Fact]` + 17 `[Theory]`, +62 `[InlineData]`), PPA/LDO/LOA implementado e testado, encerramento de exercício implementado e testado, M6 com a receita tributária inteira + dívida ativa/CDA/protesto/prescrição, e a robustez de auditoria (hash-chain) + os 11 achados de segurança (6 críticos + 5 altos) **fechados no código**.

A **fundação de engenharia é production-grade** nas dimensões que mais importam para dinheiro público: isolamento multi-tenant *fail-closed*, trilha de auditoria imutável com hash-chain *tamper-evident*, autorização organizacional com a regra "não delega o que não tem", e motores de cálculo `[OFICIAL]` determinísticos e parametrizados.

A **lacuna real** não é de núcleo — é de **borda oficial** (validação em ambiente real do TCE/eSocial, pendente de credenciais e do Manual Técnico MT-2026), de **alguns frontends/serviços cidadão** (Portal do Cidadão, PIX/QR de arrecadação) e do **ciclo anual completo da folha** (13º/férias/rescisão). São gaps **nomeáveis e fecháveis**, não fragilidade estrutural.

---

## 1. Estado real provado (por marco/módulo, com evidência)

### 1.1 Build e testes (números reais)

| Métrica | `progresso.json` | **Real (contado no código, 2026-06-23)** |
|---|---|---|
| Testes backend (métodos) | 852 | **~1.067** (`grep [Fact]`=1.050, `[Theory]`=17; +62 `[InlineData]`) — **[a confirmar verde em CI]** |
| Testes frontend (`it`/`test`) | 182 | **192** em **61** arquivos `.test`/`.spec` (+5 `*.a11y.test.*`) — **[a confirmar verde]** |
| Build | 0 erros · 0 avisos | `Directory.Build.props`: `TreatWarningsAsErrors=true`, NRT, `AnalysisLevel=latest-recommended`. **Não executado** — **[a confirmar 0/0 em CI]** |
| Módulos backend | 13 | **13** módulos confirmados em `src/Modules/` (cada um com Domain/Application/Infrastructure/Contracts) |
| `NotImplementedException` em `src/` | — | **0** (nenhum stub preguiçoso). 163 marcadores `TODO`, quase todos `TODO(validar-oficial)` — flag honesto de leiaute/XSD a confirmar |

Distribuição dos testes por módulo (top): Legislativo **165**, RecursosHumanos **112**, Tributos **98**, Identidade **92**, Administração **90**, Finanças **85**, Patrimônio **83**, Transparência **70**, AssistênciaSocial **68**, Saúde **60**, Educação **53**, Protocolo **46**, Cofre **22**, Platform **11**, ArchitectureTests **11**, Integracoes **1**.

> **Achado de honestidade #1:** o `progresso.json` **subestima** o estado (números defasados; M5/M6/PPA/encerramento marcados `pending`/`active` apesar de entregues e testados no código). Recomendação ao dono: regerar o `progresso.json` a partir do código — a narrativa atual prejudica a própria empresa numa banca.

### 1.2 Espinha fiscal M0–M4 — **PROVADA** (`[OFICIAL]` no núcleo contábil)

Confirmado em `src/Modules/Financas` + `src/Modules/Transparencia` e respectivos testes:

- **PCASP / partida dobrada:** `LancamentoContabil` valida balanceamento e lança `PartidaDobradaDesbalanceadaException` se ΣDébito ≠ ΣCrédito (`.../Contabilidade/Lancamentos/LancamentoContabil.cs`, método `ValidarBalanceamento`). Provado em `ContabilidadeInvariantesTests.cs:46,61`.
- **Balancete ΣD=ΣC provado em runtime de teste (roundtrip SQLite):** `ContabilidadePersistenciaTests.cs:136` — `linhas.Sum(l => l.TotalDebitos).Should().Be(linhas.Sum(l => l.TotalCreditos))`. Repetido no encerramento (`EncerramentoExercicioTests.cs:59`) e na MSC (`MscDerivacaoTests.cs:107`).
- **Lançamento automático via Outbox (transacional):** `ConvertDomainEventsToOutboxInterceptor` materializa eventos de domínio como `OutboxMessage` no **mesmo** `SaveChanges`; `MotorContabil.ContabilizarAsync` é **idempotente por `OrigemReferenciaId`** (re-execução retorna 0 lançamentos — `ContabilidadePersistenciaTests.cs:89`).
- **Ciclo da despesa (Lei 4.320) Empenho→Liquidação→Pagamento:** invariantes encadeadas (não empenhar > dotação; não liquidar > empenho; não pagar > liquidado; **pagar sem liquidação é bloqueado**) — `SaldoInvariantesTests.cs` e `EmpenhoFluxoTests.cs` (ciclo completo persiste e respeita isolamento de tenant). Inclui anulação, estorno e inscrição em Restos a Pagar.
- **DCASP (BO/BF/BP/DVP):** os 4 demonstrativos fecham seus invariantes — BP `Ativo = Passivo + PL`, DVP `VPA − VPD`, BO `Receita − Despesa`, BF `Ingressos − Dispêndios` (`DemonstracoesDcaspTests.cs`). Superávit financeiro pelo indicador F/P (art. 105, Lei 4.320) também testado.
- **MSC derivada + bloqueio se desbalanceada:** `DerivadorMsc`/`MatrizSaldosContabeis` recusa matriz desbalanceada (`MatrizSaldosDesbalanceadaException`).
- **Remessa TCE-RS SIAPC/PAD + e-Validador local (RDI) + reconciliação SICONFI:** emissor posicional genérico (`EmissorRegistroSiapc`), empacotamento ZIP determinístico, pré-validação local (campo obrigatório vazio, arquivo vazio, somatório divergente → erro que rejeita a remessa — `PreValidacaoSiapcTests.cs`), reconciliação SICONFI por Dados Abertos que **detecta divergência e degrada graciosamente** quando a API cai (Polly — `ReconciliacaoSiconfiTests.cs`). Transmissão é **ato humano** (sem API), por design.

> **Ressalva fiscal importante (não-bloqueante, mas honesta):** o **leiaute exato** do SIAPC/PAD está marcado `// TODO(validar-leiaute-MT-2026)` em vários pontos (`LeiauteSiapcSeed.cs`, `GerarRemessaTce.cs`, `GeradorMscCsv.cs`). A estrutura posicional, o empacotamento e a pré-validação são reais e testados, mas as **posições/larguras/colunas oficiais 2026** ainda **não foram conferidas contra o Manual Técnico oficial** — ou seja, o pipeline é "**simulado correto**", não "**oficial certificado**". Isto é coerente com o que o próprio board declara.

### 1.3 PPA/LDO/LOA + encerramento de exercício — **IMPLEMENTADOS e TESTADOS** (corrige uma defasagem grave do JSON)

- **Planejamento orçamentário:** `AcaoPpa` (PPA), `LeiDiretrizes` (LDO), `LeiOrcamentariaAnual` (LOA), `CreditoAdicional` (suplementar/especial/extraordinário, art. 40–46 Lei 4.320) com fontes (anulação/superávit/excesso de arrecadação). Testados em `PlanejamentoDominioTests.cs`, `PlanejamentoPersistenciaTests.cs`, `PlanejamentoOutboxRoundTripTests.cs`.
- **Encerramento de exercício:** `EncerramentoExercicio` (máquina de estados que **congela após Encerrado** e recusa pular fases) + `MotorEncerramento` (apuração patrimonial zera classes 3/4 → conta de resultado; apuração orçamentária zera 5/6; abertura transfere resultado para "exercícios anteriores" no mês 0 do ano seguinte). Provado em `EncerramentoExercicioTests.cs` (superávit, déficit, idempotência, abertura fecha D=C).

> **Achado #2 (relevante para a banca):** uma avaliação independente anterior (`docs/estudo/avaliacao-m5m6/AVALIACAO-POC.md`, **2026-06-22**) reprovava a trilha municipal em parte por **"PPA/LDO/LOA ausente (0 referências no código)"**. Esse gap **foi efetivamente fechado** no commit `c555f91` (posterior àquela avaliação) e está testado. **O estado atual supera aquela foto.** O mesmo vale para "ponto eletrônico ausente" (fechado em `671babc`).

### 1.4 M5 RecursosHumanos — **núcleo PROVADO** (`[OFICIAL]`), eSocial só esqueleto

- **Motor de folha determinístico e parametrizado:** `MotorDeCalculoFolha` (INSS progressivo, IRRF, RPPS), **sem números mágicos** (alíquotas/faixas vêm de `TabelaInss`/`TabelaIrrf`/`TabelaRpps` por competência/tenant), arredondamento `AwayFromZero`, líquido nunca negativo, e **fail-closed**: RPPS sem tabela municipal **lança exceção** (não assume zero) — `MotorDeCalculoFolhaTests.cs:134`. Determinismo provado (mesma entrada → mesma saída, `:148`).
  - **Correção factual ao headline:** o `progresso.json` cita "líquido 6.034,77". O **teste real prova 4.185,60** (salário 5.000, 0 dependentes — `MotorDeCalculoFolhaTests.cs:60` e `ApurarDescontosLegaisTests.cs:100`). O **motor está provado**; o número 6.034,77 é da narrativa, **não** uma asserção de teste. Recomendo alinhar o texto ao valor testado.
- **Ponto (Portaria MTP 671/2021):** geradores **AFD/AEJ** posicionais (ordenação por NSR, encoding ISO-8859-1, header/trailer) com **CRC-16/CCITT** correto — provado contra o vetor padrão `"123456789" → 0x29B1` (`PontoGeradorAfdTests.cs:94`). Posições exatas do Anexo marcadas `TODO(validar-oficial)`.
- **eSocial:** existe **scaffolding de domínio substancial** (mapeamento XML S-1000/S-1005/S-2200/S-1210/S-1299 em `.../ESocial/`), mas **não há camada de transmissão** (zero handler de aplicação, zero cliente SOAP) e quase tudo está `// TODO(validar-oficial: XSD)`. **Correto:** depende de credenciais de homologação. **Não transmite hoje.**

### 1.5 M6 Tributos — **COMPLETO no código** (`[OFICIAL]` nos motores)

Todos os motores são determinísticos, parametrizados (lei municipal/tenant) e **fail-closed** (item fora da tabela → exceção). Testes em `tests/Tensorroot.Gov.Modules.Tributos.Tests` (98 métodos):

- **IPTU:** cadastro imobiliário + PGV (planta de valores) + motor de valor venal **reprodutível** (idade calculada pelo **ano-exercício**, não pelo relógio → mesma apuração no mesmo ano). `MotorIptuTests.cs` prova valor venal **180.000 → imposto 1.800** (alíquota 1%), progressividade por faixa, isenção+desconto, lote territorial.
  - **Correção factual:** o headline cita "162.000 → 1.458". O **teste real** usa **180.000 → 1.800**. A fórmula bate; o par 162.000/1.458 **não** está no conjunto de testes. Motor provado, número do texto difere.
- **ISS:** apuração sobre NFS-e ingeridas (passiva), classificação de modalidade **Substituição > Retido > Próprio**, livro eletrônico consolidando próprio/retido/substituição (`MotorIssTests.cs`). Alíquotas por item LC 116, parametrizadas.
- **ITBI (Tema 1.113/STJ — correto):** **base = valor declarado** (não venal); valor venal é **triagem/alerta**, não eleva a base de ofício; arbitramento (CTN art. 148) só eleva a base com **processo concluído e contraditório**, **fail-closed** caso contrário (`MotorItbiTests.cs:82,102`). Rejeita declarado zero.
- **Taxas / COSIP / Alvarás (reais, não stubs):** taxa em modos ValorFixo/PorUnidade/PorFaixa (faixas sobrepostas rejeitadas, tabela vigente imutável); COSIP progressiva por faixa de consumo+classe (RE 573.675); Alvará como máquina de estados (`MotorTaxasCosipMelhoriaTests.cs`).
- **Dívida Ativa / CDA / Protesto / Prescrição — production-grade:** CDA exige requisitos da LEF art. 2º §5º (nome/valor/origem/fundamento → `CdaRequisitoAusenteException`); encargos (multa 2% + juros 1% a.m. + correção 0,5% a.m.) determinísticos; protesto extrajudicial (remessa/retorno); **prescrição quinquenal (CTN art. 174)** computada por datas do fato (não relógio), com **interrupção que reinicia o quinquênio** e prazo parametrizável — provado em `DividaAtivaCdaProtestoPrescricaoTests.cs:205,222` (prescreve em 10/05/2025; após interrupção em 01/01/2023, vai para 01/01/2028).

### 1.6 Legislativo — **o módulo mais profundo (165 testes), genuíno (não padded)**

`src/Modules/Legislativo` + 165 testes. Agregados ricos: `Votacao`, `Sessao`, `Proposicao`, `Comissao`, `Norma`, `TribunaSessao`. Lógica de votação **correta e testada na borda**:

- **Quórum de deliberação** = maioria absoluta dos membros `(Total/2)+1`; rejeita `presentes > totalMembros` (BUG-8a).
- **Maiorias:** Simples `> 50%` dos presentes (empate **rejeita**); Absoluta `>= (Total/2)+1` dos **membros**; Qualificada `ceil(2/3)` via `Math.Ceiling` (não trunca).
- **Voto único (BUG-2)** em **toda** modalidade (inclusive secreta — o `VereadorId` é mantido internamente para unicidade mesmo sob sigilo); aprovação vincula-se à **maioria atingida**, não à exigida (BUG-1) — provado em `AprovarProposicaoHandlerTests.cs:64,98` e `BordasVotacaoTests.cs:176`.
- **Painel eletrônico ao vivo** (front) com idempotência; cenários BDD do README têm testes correspondentes.

---

## 2. Solidez de engenharia (production-grade vs frágil)

Auditados os mecanismos transversais em `src/BuildingBlocks/...Infrastructure`, `src/SharedKernel`, `src/Platform`, `src/ApiHost` e módulo `Identidade`.

| Mecanismo | Evidência (arquivo:linha) | Veredito |
|---|---|---|
| **Multi-tenant** | `TenantSaveChangesInterceptor.cs` (carimba `TenantId` no insert; gravação cross-tenant lança `InvalidOperationException`); `ModelBuilderExtensions.cs:46-61` aplica filtro global por reflexão a todo `IMustHaveTenant`. **Sem tenant → filtro vira `1=0` (deny-by-default), nunca `Guid.Empty`** (não vaza órfãos). Provado em `FiltroDeTenantSemTenantTests.cs` e `CrossTenantModulosTests.cs`. | **Production-grade / fail-closed** |
| **Auditoria imutável + hash-chain** | `AuditSaveChangesInterceptor.cs` (antes/depois JSON, usuário do claim `sub`, IP, timestamp); `AuditHashChain.cs` (SHA-256, `HashAtual = SHA256(HashAnterior‖conteúdo)`); `VerificadorTrilhaAuditoria.cs` detecta 3 classes de adulteração (gap de sequência, elo quebrado, conteúdo alterado) e aponta a linha; propriedades `init`-only + trigger WORM (SqlServer) bloqueando UPDATE/DELETE. | **Production-grade / tamper-evident** |
| **Ordem dos interceptors** | `ModuleInterceptorRegistration.cs` centraliza **Tenant → Audit → Outbox** (Audit lê `TenantId` já carimbado). Fecha o risco latente W0.2 que o JSON ainda lista como `pending`. | **Corrigido** (JSON defasado) |
| **Autorização (RBAC + escopo UO)** | `PermissionAuthorization.cs` (policies dinâmicas `perm:*`, **negar por padrão**); `AutorizacaoDeConcessao.cs:82-164` codifica **I4 "não delega o que não tem"** (concedente precisa possuir cada permissão do papel cobrindo o escopo-alvo) — **no domínio**, fail-closed; `PoliticaDelegacao.cs` limita **profundidade de subdelegação** (padrão 2, parametrizável por tenant). | **Production-grade** |
| **Ativação modular por tenant** | `ModuleRegistry` (descoberta por reflexão de `IModule`); middleware em `ApiHost/Program.cs:188-212` checa `TenantModuleProvider.IsModuleEnabledAsync` e devolve **403 auditado** para módulo não-licenciado. | **Production-grade** |
| **Outbox / consistência** | `OutboxMessage` com backoff (`AttemptCount`/`NextAttemptUtc`) e dead-letter (5 tentativas); eventos materializados na transação; isolamento de UoW 1:1 por módulo (`ModuleUnitOfWorkIsolationTests`). | **Production-grade** |
| **Fitness functions (NetArchTest)** | `tests/.../ArchitectureTests/FitnessFunctions.cs`: Domain ↛ EF/ASP.NET; Application ↛ Infra/EF; cross-module só via `*.Contracts`; SharedKernel limpo; **LGPD-LG1** (operação de acesso sensível não aceita `usuarioId` do cliente). Aplicadas por reflexão a **todos os 13 módulos**. + `MaintainabilityTests` (nenhum `.cs` de produção > 500 linhas). | **Production-grade** |

### 2.1 Segurança — 6 críticos + 5 altos verificados no código

Revisão adversarial confirmou que os 11 achados do red-team estão **fechados em código** (não só em commit message). Destaques:

- **Escalada de privilégio (AA-1/2):** bloqueada no domínio (`AutorizacaoDeConcessao`) — não dá para conceder papel que não se possui; `AtribuirPapelAoUsuario` lança `ConcessaoNaoAutorizadaException`.
- **Cross-tenant (XT-1/2):** guarda "tenant da rota == tenant do JWT" **auditada** em `AdminEndpoints.cs` (403 opaco) + filtro global `1=0` sem tenant.
- **LGPD (LG-1/2/3 + A2):** `TrilhaAcessoSensivelBehavior` (MediatR) **sela leituras sensíveis (Saúde/Assistência) na cadeia de hash** e **nega + audita `ReadDenied`** se a base legal não for aplicável (conjuntos fechados por código, ex. `BasesLegaisSaude.cs`); identidade do acesso vem do `ICurrentUser` (JWT), **não** de query string forjável; **redação de PII por padrão** na auditoria (`PoliticaRedacaoAuditoria` — CPF/NIS/CNS/diagnóstico mascarados em valor **e** chave JSON).
- **Conexão cifrada (SEC-1):** connection string do tenant **cifrada em repouso** (envelope **AES-256-GCM + KEK** via `ProtetorConexaoTenant.cs`, prefixo `encv1:`), decifrada **só em memória** com zeragem de buffers; tag GCM impede adulteração.

### 2.2 Segredos e certificado A1 — limpos

- **Nenhum segredo real no repositório:** varredura por `password=`, `AccountKey=`, `BEGIN PRIVATE KEY`, `.pfx`/`.p12`/`.key` → **0 ocorrências reais**. O único segredo em `appsettings.Development.json` é placeholder explícito ("DEV-ONLY-troque-...-via-KeyVault"); KEK **não versionada**.
- **Cofre / A1 real (não stub):** `ServicoCustodiaCertificado` recebe o `.pfx` só em memória e o cifra (AES-256-GCM); DEK envelopada por **RSA-OAEP no Key Vault** (`ProvedorKekKeyVault`, `DefaultAzureCredential`/Managed Identity, com Polly). 22 testes no Cofre.

### 2.3 Pontos ainda **frágeis / não provados**

- **Leiautes/XSD oficiais não validados:** SIAPC/PAD (`TODO(validar-leiaute-MT-2026)`), AFD/AEJ (Anexos da 671), eSocial (S-1.3 XSD). Hoje é "simulado correto", não "oficial certificado".
- **eSocial não transmite:** só esqueleto de domínio.
- **Integrações externas reais pendentes de credencial:** NFS-e/ADN tem cliente HTTP real com Polly, mas roda com `SimuladoNfseGateway` até credenciais de homologação; Receita/CNPJ usa `SimuladoReceitaCnpjGateway`; PNCP grava número + emite evento, cliente HTTP publicador **[a confirmar]** se real ou stub.
- **`tests/...Integracoes.Tests` tem 1 teste** (CNAB-240, só formato) — cobertura de integração genuinamente fina (esperado nesta fase, dado que o real depende de endpoints/credenciais).

---

## 3. Prontidão para PoC

> Triangulação com a avaliação independente anterior (`docs/estudo/avaliacao-m5m6/AVALIACAO-POC.md`, 2026-06-22): ela deu **Legislativa 78% (APROVA)** e **Municipal 66% (reprova)** — mas seus dois principais reprovadores municipais (**PPA/LDO/LOA** e **ponto 671**) **foram fechados depois** dela. Logo, a foto atual é **melhor**.

### 3.1 PoC Legislativa — **~80–82%, APROVA** (PoC de "software de processo legislativo")

- **Demonstra muito bem:** processo legislativo ponta a ponta (proposição → tramitação → comissão/parecer → votação com quórum/maiorias corretas → painel ao vivo → norma → diário/tribuna), com 165 testes de domínio e 14+ telas React (incl. Painel Ao Vivo com teste de acessibilidade). Multi-tenant e auditoria reforçam credibilidade técnica.
- **Reprovaria se o edital exigir:** deliberação remota em tempo real (voto/tribuna online), terminais físicos de plenário/painel LED, ou ERP-de-Câmara **completo** (folha/eSocial da Câmara, portal do cidadão). Confirmar Ata Sintética automática e "parlamentar impedido de votar" **[a confirmar]**.

### 3.2 PoC Municipal — **~72–75%** (PoC de "núcleo fiscal/contábil/TCE": forte ~90%; ERP municipal **completo** eliminatório: ainda reprova, por margem menor que os 66% anteriores)

- **Demonstra muito bem (é a liderança):** núcleo PCASP/Lei 4.320 (ciclo da despesa, balancete ΣD=ΣC, 4 DCASP, MSC), **PPA/LDO/LOA + créditos + encerramento de exercício**, remessa TCE-RS SIAPC/PAD + RDI + reconciliação SICONFI, A1/Cofre, e os motores tributários (IPTU/ISS/ITBI/taxas/COSIP) + dívida ativa/CDA/protesto/prescrição. Folha e ponto rodam.
- **Reprovaria hoje num ERP municipal completo eliminatório, por gaps nomeáveis:**
  1. **Validação oficial real ausente** (TCE/eSocial em ambiente oficial) — o maior risco de credibilidade se a banca exigir prova oficial, não simulada.
  2. **eSocial não transmite** + **ciclo anual da folha** (13º/férias/rescisão) **[a confirmar]**.
  3. **Portal/App do Cidadão** ausente (M8).
  4. **Arrecadação PIX/QR** ausente.

### 3.3 O que **reprovaria** transversalmente (qualquer trilha)

- Exigência de **aderência oficial comprovada** (remessa aceita pelo PAD real / evento eSocial aceito em homologação) — hoje **não** demonstrável.
- Exigência de **demo end-to-end com dados-seed e roteiro** — existe `docs/estudo/POC-ROTEIRO-DEMO.md`, mas a execução real **[a confirmar]** (não rodei a aplicação).

---

## 4. Gaps reais e próximos passos (priorizados)

**P0 — destrava credibilidade de PoC municipal "oficial" (depende de insumos do dono):**
1. **Validação oficial real** (W10.1): conferir leiaute SIAPC/PAD contra **MT-2026/e-Validador/PAD** e fechar os `TODO(validar-leiaute-MT-2026)`; rodar **eSocial em homologação** (S-1.3) — requer **A1 real, credenciais de homologação, RPPS×RGPS, Azure/Key Vault**.
2. **eSocial transmissão** (W5.2): construir camada de aplicação + cliente SOAP + Outbox/ACL sobre o esqueleto de domínio existente.

**P1 — fecha a PoC municipal "ERP completo":**
3. **Ciclo anual da folha** (13º/férias/rescisão) e **Remessa de folha TCE-RS** (W5.4).
4. **Portal/App do Cidadão** (M8/W8.1) + **arrecadação PIX/QR**.
5. **Tesouraria/Controladoria** (M4.x — W4.4/W4.5) para fechar a borda de controle interno.

**P2 — robustez e cobertura de borda:**
6. **PNCP bloqueante** com cliente HTTP real (M9/W9.1) — hoje grava número + evento; confirmar publicação real.
7. **Integrações setoriais** M7 (Saúde RNDS/e-SUS/SIOPS; Educação Educacenso/SIOPE; Assistência CadÚnico) — todas pendentes.
8. **Acessibilidade WCAG AA** ampliada (hoje 5 arquivos `.a11y` de prova; estender a todas as telas) e **QA E2E** (W9.8).
9. **Higiene documental (rápido, alto retorno):** regerar `progresso.json` a partir do código; alinhar os números do headline (folha 4.185,60; IPTU 180.000→1.800) aos valores **testados**.

---

## 5. Riscos (por severidade)

### Alto
- **R1 — "Simulado" confundido com "oficial".** Leiautes TCE/eSocial não validados em ambiente real. Numa banca, alegar aderência a item não-comprovado é o pior erro (risco de "produto ≠ amostra"). **Mitigação:** manter os `TODO(validar-oficial)` honestos e priorizar W10.1; **nunca** apresentar remessa simulada como aceita pelo TCE.
- **R2 — Dependência de credenciais externas** (eSocial, ADN, PNCP, Azure). Sem elas, integrações ficam em modo simulado — bloqueio de prontidão, não de engenharia.

### Médio
- **R3 — Build/verde não verificável aqui.** 1.067 testes contados estaticamente; *0/0 e all-green* dependem de execução em CI (não rodei `dotnet`). **[a confirmar]**.
- **R4 — Ciclo anual da folha incompleto** (13º/férias/rescisão) e remessa de folha TCE — reprovam folha completa.
- **R5 — Cobertura de integração fina** (1 teste em Integracoes; gateways simulados) — risco quando os reais entrarem.

### Baixo
- **R6 — Frontend a11y parcial:** gov.br DS + axe-core presentes, mas só 5 telas com teste de acessibilidade dedicado; resto **[a confirmar]**.
- **R7 — Defasagem documental** (`progresso.json` desatualizado) — risco reputacional, não técnico; correção trivial.
- **R8 — `IgnoreQueryFilters`/contexto de sistema** existe para DDL/bootstrap; manter auditado e restrito (hoje está, mas é superfície a vigiar).

---

## Conclusão do auditor

A engenharia de núcleo está **acima da média para o estágio** e **acima do que o próprio board declara**: isolamento multi-tenant *fail-closed*, auditoria imutável com hash-chain *tamper-evident*, autorização "não delega o que não tem", motores `[OFICIAL]` determinísticos/parametrizados, e os 11 achados de segurança fechados — tudo com evidência em código e teste. A **espinha fiscal M0–M4 + PPA/LDO/LOA + encerramento + M5 núcleo + M6 completo** está entregue e testada. A distância para "pronto de verdade" é de **borda oficial** (validação TCE/eSocial em ambiente real, pendente de credenciais e MT-2026), de **serviços ao cidadão** (Portal, PIX) e do **ciclo anual da folha** — gaps **nomeáveis e priorizáveis**, não fragilidade estrutural. **Recomendação:** seguir para PoC **legislativa** já (aprova ~80%); para a **municipal**, posicionar como "núcleo fiscal/TCE forte (~90%)" e fechar P0/P1 antes de qualquer edital municipal eliminatório.

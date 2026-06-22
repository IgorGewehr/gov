# Diagnóstico (foto pré-M0) × Estado REAL hoje — confronto honesto, item-a-item

> Este documento confronta as **afirmações dos docs de DIAGNÓSTICO** (`ESTADO-ATUAL.md` /
> `GAP-E-ROADMAP.md`, datados 2026-06-22 — **fotos PRÉ-M0**) com o **estado REAL e provado em
> runtime hoje**, verificado por leitura direta do código + build + execução dos testes.
> Regra: cada linha tem **EVIDÊNCIA** (arquivo / teste / marco). Sem complacência: onde a crítica
> **ainda procede** (maturidade das integrações), está dito com franqueza.
>
> Verificação executada nesta sessão (não assumida):
> - `dotnet build Tensorroot.Gov.sln -c Release` → **Compilação com êxito. 0 Aviso(s), 0 Erro(s)** (25 s).
> - `dotnet test Tensorroot.Gov.sln -c Release` → **705 testes, Com falha: 0**, em 16 assemblies
>   (Financas 55, Patrimonio 71, Administracao 71, Transparencia 70, Legislativo 69, AssistenciaSocial 63,
>   Identidade 59, Saude 54, Educacao 53, Protocolo 46, Cofre 16, RH 60, Tributos 5, Platform 5,
>   ArchitectureTests 7, Integracoes 1). Contagem estática: **697 `[Fact]/[Theory]` em 67 arquivos**.
>   (Nota de ambiente: a máquina só tem o runtime .NET 10; os testes têm como alvo net8.0, então rodam
>   com `DOTNET_ROLL_FORWARD=Major` — limitação do host, não do código.)

---

## 1. As 3 preocupações da IA externa — veredito

| # | Afirmação da IA externa (lendo a FOTO pré-M0) | Veredito | Síntese |
|---|---|---|---|
| 1 | "Não compila — bug no `EmpenhoRepository`, 575 testes travados" | ✅ **RESOLVIDA (foto velha)** | Os 2 métodos faltantes existem e estão implementados; build **0/0**; **705 testes verdes**. Era o bloqueador M0, fechado. |
| 2 | "Contabilidade PCASP→TCE 100% ausente" | ✅ **RESOLVIDA (foto velha)** | Existe motor contábil PCASP real: partida dobrada com invariante ΣD=ΣC, roteiros MCASP, lançamento automático via Outbox, balancete, demonstrações DCASP, MSC e remessa SIAPC posicional. Entregue em M2–M4 e provado em runtime. |
| 3 | "De ~19 integrações só 1 é real (NFS-e/ADN), resto simulado" | 🟡 **PARCIAL / REAL-EM-ABERTO** | A crítica **ainda procede em parte**: várias integrações seguem `Simulado*` por serem marcos M5–M9 ainda não iniciados. Mas a régua mudou: o que existe não é mais "andaime vazio" — é **artefato correto (remessa posicional, assinatura A1, MSC, validador local) com a *validação contra o oficial* pendente** do PAD/SISCAD. Detalhado na seção "Maturidade das integrações". |

---

## 2. Tabela honesta — AFIRMAÇÃO ANTIGA → ESTADO REAL → EVIDÊNCIA

| Afirmação antiga (DIAGNÓSTICO pré-M0) | Estado REAL hoje | Evidência |
|---|---|---|
| "O backend **NÃO COMPILA** (2 erros em `EmpenhoRepository`); 575 testes não rodam. Bloqueador nº 0." | **Falso hoje.** Build Release **0 erros / 0 avisos**. | `dotnet build -c Release` nesta sessão; `EmpenhoRepository.cs:23` (`ListarPorDotacaoAsync`) e `:31` (`ListarComSaldoAbertoPorExercicioAsync`) implementados; marco **M0 done** (progresso.json). |
| "575 `[Fact]/[Theory]` verdes só no histórico." | **705 testes verdes** hoje (0 falhas) em 16 assemblies; 697 `[Fact]/[Theory]` estáticos. | `dotnet test -c Release` nesta sessão; progresso.json: "Testes backend 694 ✓" + "frontend 181 ✓". |
| "**PCASP/MCASP INEXISTENTE (0 linhas).** Sem partida dobrada, sem plano de contas, sem balancete." | **Falso.** Existe agregado `LancamentoContabil` com partida dobrada (`PartidaContabil`), plano de contas (`ContaContabil`/`CodigoContabil`), roteiros (`EventoContabil`/`LinhaRoteiro`), `MotorContabil`, balancete e demonstrações DCASP. | `Financas.Domain/Contabilidade/{PlanoDeContas,Lancamentos,EventosContabeis,Msc}/*.cs`; `Application/Contabilidade/{Handlers,Demonstracoes,Motor}/*.cs`. Marcos **M2/M3 done**. |
| "**Não há partida dobrada em lugar nenhum.**" | **Falso.** `LancamentoContabil.Registrar` valida ≥1 débito + ≥1 crédito, natureza homogênea e **ΣD=ΣC** (`PartidaDobradaDesbalanceadaException`); imutável, corrigido por estorno. | `LancamentoContabil.cs:145-281` (`ValidarBalanceamento`, `ValidarNaturezaHomogenea`, `ValidarComposicao`). |
| "Sem **balancete**; **lançamento contábil automático a partir dos domain events** ausente." | **Falso.** `ContabilizarEmpenhoHandler`/`...Liquidacao`/`...Pagamento`/`...Receita` consomem domain events via Outbox e geram lançamento pelo `MotorContabil`; `ProjetarBalanceteHandler` mantém o read model. | `Application/Contabilidade/Handlers/Contabilizar*.cs`, `ProjetarBalanceteHandler.cs`; progresso.json M2: "despesa → lançamentos automáticos → balancete fecha ΣD=ΣC". |
| "**MSC — read model órfão**; `MSCGeradaIntegrationEvent` sem publisher (0 publishers); pipeline nunca dispara." | **Resolvido.** MSC derivada dos lançamentos com publisher real; ponte Finanças→Transparência (Declaração MSC) fechada em runtime. | progresso.json **M3** W3.3 "MSC derivada dos lançamentos + publisher (provado)"; `Financas.Contracts/MSCGeradaIntegrationEvent.cs`, `Transparencia.Application/Integracoes/ReceberMSCGeradaHandler.cs`. |
| "**Leiaute SIAPC/PAD genérico/fake** — `.txt` com 3 linhas `Tipo\|Conteudo` hard-coded." | **Falso (hoje é posicional de verdade).** Emissor de largura fixa, **Latin-1 + CR/LF**, alinhamento/preenchimento/sinal por campo, via `Span<char>`. **Ressalva honesta:** a **grade exata de campos de 2026** ainda é `TODO(validar-leiaute-MT-2026)` — depende do MT/PAD oficial. | `Transparencia.Domain/RemessasTce/Leiautes/EmissorRegistroSiapc.cs` (`SerializarLinha`, posicional), `LeiauteSiapc.cs`, `RegistroLeiauteDef.cs`; progresso.json **M4** W4.1 "Remessa SIAPC/PAD posicional + ZIP (provado)". |
| "**e-Validador (RDI) — STUB** (`SimuladoEValidadorTce` aprova qualquer pacote)." | **Parcial-resolvido.** Wired hoje o `EValidadorLocalSiapc` (validador local real do leiaute), não o stub. **Mas é SIMULAÇÃO fiel do MT, não o PAD oficial** — "0 erros" real só vem do RVE/RDI do PAD. | `TransparenciaModule.cs:67` `AddScoped<IEValidadorTce, EValidadorLocalSiapc>`; `MotorPreValidacaoSiapc.cs`; `docs/estudo/VALIDACAO-REAL-TCE.md` (o plano honesto). |
| "**Transmissão SIAPC/PAD e SICONFI — STUB total** (`Task.CompletedTask`)." | **Por design, é ato humano.** A transmissão ao TCE/SICONFI é ato do gestor/contador com certificado (não há API pública/CLI do PAD — ver VALIDACAO-REAL-TCE.md). Reconciliação SICONFI via **API Dados Abertos real (HTTP+Polly)**, config-gated. | progresso.json M4: "transmissão = ato humano, sem API"; `TransparenciaModule.cs:82` `ConsultaSiconfiHttp` + `AddStandardResilienceHandler` (gate `Transparencia:Siconfi:Provider=DadosAbertos`). |
| "**Assinatura A1 — INEXISTENTE.** Zero `SignedXml`/`X509Certificate2`/Key Vault no `src`." | **Falso.** Módulo **Cofre** com custódia de certificado A1 (envelope encryption), `IServicoAssinaturaDigital` e `AssinadorXmlDsig` real (`SignedXml` + `X509Certificate2` + `ComputeSignature`, resolução de `#Id` p/ eSocial). | `Modules/Cofre/...Infrastructure/{ServicoAssinaturaDigital,ServicoCustodiaCertificado,AssinadorXmlDsig}.cs`; progresso.json **M2** W2.1 "Cofre + assinatura A1 (envelope encryption) — PROVADO seguro"; 16 testes Cofre verdes. |
| "**PPA/LDO/LOA ausente.**" | **Ainda em aberto (honesto).** Existe `DotacaoOrcamentaria` (execução); planejamento PPA/LDO/LOA está **pendente** como M3.x. | progresso.json M3 W3.1 "Planejamento PPA/LDO/LOA — M3.x" (pending). |
| "De **~19 integrações, só 1 real** (NFS-e/ADN); resto `Simulado*`." | **Parcialmente ainda verdadeiro.** NFS-e/ADN real (HTTP+Polly). Acresceram: SICONFI Dados Abertos (consulta real), Cofre/A1 (assinatura real), remessa SIAPC posicional. Demais (eSocial, RNDS, CadÚnico, Receita/CNPJ, etc.) seguem `Simulado*` por serem M5–M9 **não iniciados**. | `Tributos.../Nfse/AdnNfseGateway.cs` + `TributosModule.cs:66`; `Saude/.../SimuladoGateways.cs`, `AssistenciaSocial/.../SimuladoCadUnicoGateway.cs`, `Administracao/.../SimuladoReceitaCnpjGateway.cs`. |
| "eSocial S-1010 valida só config local; **envio de eventos ausente**." | **Ainda em aberto.** eSocial completo (S-1000/1200/2200…, SOAP, Produção Restrita) é **M5 inteiro pendente**. A infra de assinatura (Cofre/XMLDSig) que ele exige já existe. | progresso.json **M5** W5.2 "eSocial S-1.3 (eventos + SOAP)" (pending). |

---

## 3. Veredito por preocupação (detalhado, sem complacência)

### Preocupação 1 — "não compila / 575 testes travados" → ✅ **RESOLVIDA (foto velha)**
Era real **na foto pré-M0**. Hoje o `EmpenhoRepository` implementa os dois métodos, o build é
**0/0** e **705 testes** passam. Não procede mais.

### Preocupação 2 — "Contabilidade PCASP→TCE 100% ausente" → ✅ **RESOLVIDA (foto velha)**
Era real na foto pré-M0 (0 linhas). Hoje existe a **espinha fiscal completa e provada em runtime**:
plano de contas PCASP → partida dobrada (ΣD=ΣC) → roteiros MCASP → lançamento automático por evento
(Outbox) → balancete → DCASP → MSC com publisher → remessa SIAPC posicional. Marcos M2–M4 `done`.
**Ressalva honesta:** a *exatidão* do leiaute 2026 e das colunas da MSC depende de validação contra
fonte oficial (TODO `validar-leiaute-MT-2026`) — ver Preocupação 3 e VALIDACAO-REAL-TCE.md.

### Preocupação 3 — "só 1 integração real, resto simulado" → 🟡 **PARCIAL / REAL-EM-ABERTO**
**A crítica ainda procede em parte — e é correto dizer.** Não porque o sistema seja fachada, mas
porque integrações estão em **maturidades diferentes**:
- **Real de produção:** NFS-e/ADN (HTTP+Polly).
- **Artefato correto + validação contra o oficial PENDENTE:** remessa SIAPC/PAD (posicional, mas grade
  exata de 2026 = TODO), e-Validador (local fiel ao MT, mas não é o PAD oficial), assinatura A1 (Cofre
  real, mas sem certificado real homologado), SICONFI (reconciliação real via Dados Abertos; transmissão
  da declaração = ato humano). **Honestidade:** o "0 erros" só é verdade quando vier do **RVE/RDI do PAD
  oficial** rodando sobre a nossa remessa — hoje é nossa simulação fiel. (VALIDACAO-REAL-TCE.md.)
- **Marco futuro não iniciado (corretamente `Simulado*`):** eSocial, RNDS/SISAB/CNES/SISREG/CADSUS,
  CadÚnico/MDS, Receita/CNPJ, PNCP, Educacenso/SIOPE — são M5–M9 pendentes.

**O que falta, por bloco aberto:**
- **SIAPC/PAD:** obter PAD vigente + MT Vol I/II/V; codificar grade posicional EXATA (trocar TODOs);
  rodar no PAD oficial (GUI Windows, ato humano); congelar golden file. Sem API/CLI → sem gate de CI.
- **SICONFI:** transmissão da declaração (hoje ato humano); leiautes RREO/RGF/DCA derivados da MSC com
  atributos oficiais confirmados.
- **A1/Cofre:** certificado ICP-Brasil A1 real do município no Key Vault por tenant (cripto pronta).
- **eSocial (M5):** S-1.3 (XSD), SOAP + Produção Restrita, motor INSS/RPPS/IRRF, remessa folha TCE-RS.
- **Demais (M6–M9):** clientes reais de RNDS, CadÚnico/MDS, Receita/CNPJ, PNCP, Educacenso/SIOPE.

---

## 4. Maturidade das integrações (resumo)

| Integração | Estado real hoje | Simulado / Oficial | Marco |
|---|---|---|---|
| **NFS-e / ADN (Receita)** | ✅ Real (HTTP + Polly + worker `NfseSync`) | **Oficial** (ingestão passiva) | Entregue |
| **CNAB 240 (arquivo)** | ✅ Geração de arquivo real | Oficial (geração; não é transmissão online) | Entregue |
| **Assinatura A1 / Cofre (Key Vault, XMLDSig)** | ✅ Real (`SignedXml`+`X509`, envelope encryption) | Cripto **oficial**; falta **certificado real homologado** por tenant | M2 (W2.1) done |
| **Remessa TCE-RS SIAPC/PAD** | 🟡 Artefato **posicional real** (Latin-1/CR-LF/largura fixa) | **Simulado fiel ao MT**; grade exata 2026 = `TODO(validar-leiaute-MT-2026)` | M4 (W4.1) done; validação oficial em aberto |
| **e-Validador (RDI) TCE-RS** | 🟡 `EValidadorLocalSiapc` (validação local real) | **Simulado** (réplica do MT); oficial = PAD GUI, ato humano | M4 (W4.2) done; PAD oficial em aberto |
| **SICONFI — reconciliação** | ✅ Consulta real Dados Abertos (HTTP+Polly), config-gated | **Oficial** (consulta); default `Simulado` | M4 (W4.3) done |
| **SICONFI — transmissão da declaração** | 🟠 Ato humano (operador registra protocolo; sem POST) | Simulado por design (sem API de envio fiada) | M4 done (transmissão = ato humano) |
| **MSC (Matriz de Saldos)** | ✅ Derivada dos lançamentos + publisher real | Oficial (estrutura); colunas exatas 2026 a confirmar | M3 (W3.3) done |
| **eSocial (S-1000/1200/2200…, SOAP)** | ❌ Ausente (infra de assinatura pronta) | A fazer — **oficial** (XSD S-1.3, Produção Restrita) | **M5 pendente** |
| **RNDS / SISAB / CNES / SISREG / CADSUS (Saúde)** | 🔴 `Simulado*` | A fazer — oficial | **M7 pendente** |
| **CadÚnico / MDS (Assistência)** | 🔴 `Simulado*` (leitura) | A fazer — oficial | **M7 pendente** |
| **Receita / CNPJ (Administração)** | 🔴 `Simulado*` (placeholder) | A fazer — oficial | M5–M9 |
| **PNCP (Administração)** | ❌ Sem cliente HTTP (grava nº + evento) | A fazer — oficial (eficácia do contrato) | **M9 (W9.1) pendente** |
| **Educacenso/INEP, SIOPE, PNAE/PNATE (Educação)** | 🔴 Local / ausente | A fazer — oficial | **M7 pendente** |
| **Carimbo de tempo ACT ICP (Protocolo)** | 🟡 Relógio local (sem ACT) | A fazer — oficial | **M9 (W9.4) pendente** |

**Régua honesta (VALIDACAO-REAL-TCE.md):** para o SIAPC/PAD **não existe gate oficial automatizável**
(PAD é desktop Java GUI, sem CLI/XSD/web service público). O CI valida contra a **réplica fiel
versionada do MT + golden files**; o **oficial entra como homologação manual** (RVE/RDI no PAD, com
certificado, no Windows). Já o **eSocial** tem homologação automatizável (XSD + Produção Restrita).

---

## 5. Conclusão

Das 3 preocupações da IA externa: **2 estão RESOLVIDAS** (eram fotos pré-M0 — build/testes e
contabilidade PCASP→TCE) e **1 é PARCIAL/REAL-EM-ABERTO** (maturidade das integrações). A crítica de
maturidade **ainda procede** e está aqui sem maquiagem: o núcleo fiscal (Contabilidade → MSC → remessa
SIAPC → DCASP) existe, é rico e provado em runtime, mas a **validação contra o leiaute/PAD oficial do
exercício 2026 e a maioria das integrações de domínio (eSocial, Saúde, Assistência, Educação, PNCP)
seguem pendentes** nos marcos M5–M9. O que mudou desde a foto pré-M0 não é só "compila": é a espinha
fiscal inteira saindo de 0 linhas para artefato correto — faltando, com franqueza, o carimbo do oficial.

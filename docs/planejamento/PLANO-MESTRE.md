# PLANO-MESTRE — Tensorroot.Gov (paridade Betha + production-ready)

> **Backlog sequenciado de WORKFLOWS.** Cada item é uma unidade de trabalho que será executada depois,
> de ponta a ponta (backend e/ou frontend). Ordenado por marcos (M0..M9) seguindo a prioridade do dono:
> **Contabilidade → MSC → TCE-RS/SICONFI primeiro**, modelo de autorização organizacional cedo (é base de tudo),
> depois aprofundamento de módulos e camadas faltantes (cidadão, Portal do Gestor/BI, sub-módulos).
>
> **Fontes do plano (referenciadas, NÃO copiadas — evita divergência, cf. AUDITORIA M-5/K1):**
> diagnóstico em `docs/diagnostico/{ESTADO-ATUAL,GAP-E-ROADMAP}.md` (fotos pré-M0 — consultar lá, não reproduzir aqui);
> `docs/planejamento/{BENCHMARK-BETHA,partes/betha-parity,MODELO-AUTORIZACAO-ORGANIZACIONAL,MAPA-PRESTACAO-CONTAS}.md`;
> `docs/architecture/contabilidade-pcasp-tce.md`; ADRs `docs/adr/0005,0009,0010,0011,0013`; `CLAUDE.md`.
> **Status de execução é o board** (`docs/progresso/progresso.json` + tasks M0–M9) — este plano apenas o espelha.
>
> **Legenda de prioridade:** 🔴 bloqueador/fonte da verdade · 🟠 alto valor fiscal · 🟡 robustez/compliance · 🟢 evolução.
> **Legenda de STATUS:** `[ENTREGUE]` (board=completed, provado por ADR) · `[EM CURSO]` (in-progress) · `[PENDENTE]` (pending/backlog).
> **`[OFICIAL]`** = depende de leiaute/versão oficial do exercício (CLAUDE.md §7/§16 — validar fonte ANTES de codar; nunca *hardcoded*).
> **`[a confirmar]`** = depende de fonte oficial ou da realidade do piloto (CLAUDE.md §16).
> **Tipo:** `backend` · `frontend` · `fullstack`.
> Data: 2026-06-22. Piloto: Maximiliano de Almeida/RS (TCE-RS).

---

## Resumo dos marcos

> **STATUS espelha o board** (`docs/progresso/progresso.json`). M0–M4 estão `completed` e provados
> por ADRs — ver bloco "ENTREGUE" abaixo. Os workflows de M0–M4 ficam marcados ✅ apenas como histórico;
> **não re-implementar** (cf. AUDITORIA K1 — pegar este plano e "corrigir" build verde ou recodar
> contabilidade entregue é o retrabalho que esta coluna existe para evitar).

| Marco | Tema | Resultado de negócio | STATUS |
|---|---|---|---|
| **M0** | Destravar build + bugs críticos de plataforma | CI verde, 575 testes rodando, segurança de borda fechada | ✅ **ENTREGUE** |
| **M1** | Autorização organizacional (UO/ABAC/SoD) — base de tudo | Escopo por unidade + segregação de funções (exigência TCE) | ✅ **ENTREGUE** (ADR-0007) |
| **M2** | Fundação contábil PCASP + segurança fiscal (A1/Key Vault) | Partida dobrada real + assinatura A1 — a FONTE de tudo | ✅ **ENTREGUE** (ADR-0008/0013) |
| **M3** | Demonstrações DCASP + MSC + Planejamento PPA/LDO/LOA | MSC derivada dos lançamentos; ciclo orçamentário completo | ✅ **ENTREGUE** (ADR-0011/0013) |
| **M4** | Prestação de contas TCE-RS + SICONFI | **Meta nº1 do dono atingida** (artefatos válidos + reconciliação) | ✅ **ENTREGUE** (ADR-0009) |
| **M5** | RH completo: motor de folha + eSocial + Ponto + remessa folha + EFD-Reinf/DCTFWeb | Folha calculada e prestada (S-1.3, AFD/AEJ, Res. 1099) + retenções federais | ⏳ PENDENTE |
| **M6** | Tributos: motores de apuração + cobrança + sub-módulos de receita | Receita própria (IPTU/ISS/ITBI/Taxas) + cadastro imobiliário | ⏳ PENDENTE |
| **M7** | Domínios com risco de repasse (Saúde, Educação, Assistência) | Integrações federais reais (RNDS/SISAB/SIOPE/PNAE/PNATE/MDS) | ⏳ PENDENTE |
| **M8** | Camada do cidadão + Portal do Gestor + BI | Diferencial Betha (Minha Cidade, painéis executivos) | ⏳ PENDENTE |
| **M9** | Suprimentos avançados + compliance final + robustez de plataforma | PNCP bloqueante, GED/CONARQ, Frotas/Obras/Almox, QA/a11y/Outbox, SADIPEM, Transferegov, SIAFIC | ⏳ PENDENTE |

---

## ENTREGUE — M0→M4 (board=completed; consultar ADRs, não re-executar)

> **Bloco de histórico** (cf. AUDITORIA §2.1 / K1 / M-5). M0–M4 já estão prontos e provados; o
> diagnóstico que os originou (ESTADO-ATUAL/GAP, "0 linhas", "18 handlers órfãos", "destravar build")
> é **foto pré-M0** — não é mais backlog. Mantemos os workflows abaixo apenas como mapa do que foi
> feito e onde está documentado. **O `[OFICIAL]` de cada um permanece**: os leiautes exatos seguem
> sob `// TODO(validar-leiaute-oficial)` até a fonte do exercício, mas a engenharia está entregue.

| Marco | Workflows entregues | Prova / referência |
|---|---|---|
| **M0** | W0.1–W0.4 (build, ordem de interceptors, RBAC `/admin/tenants`, gate de deploy) | board=completed; CI verde |
| **M1** | W1.1–W1.6 (UO/árvore, AtribuicaoDePapel ABAC, filtro de UO, SoD, delegação I4, sensibilidade LGPD) | **ADR-0007** (RBAC+ABAC+UO+I4) |
| **M2** | W2.1–W2.5 (Key Vault/A1, API de Finanças, PCASP partida dobrada, lançamento automático, balancete) | **ADR-0008** (A1), **ADR-0013** (PCASP), **ADR-0010** (UnitOfWork) |
| **M3** | W3.1–W3.3 (PPA/LDO/LOA, 7 DCASP, MSC + `MSCGeradaIntegrationEvent`) | **ADR-0011** (Outbox domain/integration), **ADR-0013** |
| **M4** | W4.1–W4.5 (remessa SIAPC/PAD, e-Validador+empacotamento, SICONFI carga+consulta, tesouraria, controle interno) | **ADR-0009** (transmissão manual; ver reescrita abaixo) |

> A espinha M0→M4 confirma a **meta nº1 do dono**: artefatos de prestação corretos gerados,
> pré-validados, empacotados e reconciliados via API de consulta — o **envio é ato humano**
> (ADR-0009). Os workflows M0–M4 detalhados abaixo permanecem marcados ✅ e **não devem ser
> re-planejados**; o foco vivo do plano é M5→M9.

---

## M0 — DESTRAVAR (bloqueador absoluto, dias) — ✅ ENTREGUE (board=completed)

> Objetivo: build verde, CI rodando, 575 testes + fitness functions ativos, borda segura. Pré-requisito de tudo.
> **Histórico** — não re-executar; o "destravar build" abaixo é foto pré-M0 (AUDITORIA K1).

### W0.1 — Corrigir `EmpenhoRepository` (destravar build) 🔴
- **tipo:** backend · **área:** Financas/Infrastructure
- **objetivo:** restaurar compilação Release e a execução dos 575 testes.
- **escopo:** implementar `ListarPorDotacaoAsync` e `ListarComSaldoAbertoPorExercicioAsync` de `IEmpenhoRepository`; rodar `dotnet build -c Release` verde; confirmar 575 `[Fact]/[Theory]` + NetArchTest verdes.
- **dependências:** nenhuma.
- **marco:** M0 · **prioridade:** 🔴
- **critério-de-pronto:** CI verde; testes e fitness functions executando.

### W0.2 — Corrigir ordem dos interceptors (Tenant antes de Audit/Outbox) 🔴
- **tipo:** backend · **área:** BuildingBlocks.Infrastructure
- **objetivo:** parar de gravar `TenantId=Guid.Empty` em INSERTs (audit lê antes do carimbo) — senão a trilha do TCE perde inserts.
- **escopo:** reordenar pipeline de interceptors para `Tenant → Audit → Outbox`; teste de regressão provando `TenantId` carimbado antes da auditoria; cobrir em isolamento cross-tenant.
- **dependências:** W0.1.
- **marco:** M0 · **prioridade:** 🔴
- **critério-de-pronto:** teste verde garante INSERT auditado com `TenantId` correto.

### W0.3 — RBAC no `/admin/tenants` + hardening de segredos 🔴
- **tipo:** backend · **área:** Identidade/ApiHost
- **objetivo:** fechar escalonamento de privilégio (qualquer token provisiona tenants) e remover credenciais default versionadas.
- **escopo:** política RBAC dedicada de Admin de Plataforma em `/admin/tenants`; remover `admin@tensorroot.gov / Mudar@123` do código e `appsettings.Development.json`; seed via segredo de ambiente; teste negando token comum.
- **dependências:** W0.1.
- **marco:** M0 · **prioridade:** 🔴
- **critério-de-pronto:** token sem papel de plataforma recebe 403 auditado em `/admin/tenants`.

### W0.4 — Gate de deploy de produção + restringir SQL firewall 🟡
- **tipo:** backend · **área:** Infra/CI-CD/Bicep
- **objetivo:** remover deploy automático em todo push para `main` e fechar superfície de rede.
- **escopo:** gate manual de aprovação no `deploy.yml`; remover `AllowAllAzureIps`/`publicNetworkAccess: Enabled`; Bicep passa a criar o Worker `NfseSync`.
- **dependências:** W0.1.
- **marco:** M0 · **prioridade:** 🟡
- **critério-de-pronto:** deploy de prod só após aprovação; firewall restrito provisionado.

---

## M1 — AUTORIZAÇÃO ORGANIZACIONAL (base de tudo, antes do núcleo fiscal) — ✅ ENTREGUE (board=completed, ADR-0007)

> Pré-requisito de qualidade do TCE: escopo por UO/UG e Segregação de Funções (SoD) são exigências do controle externo.
> **Histórico** — modelo RBAC+ABAC+UO+I4 entregue; ver ADR-0007. Não re-implementar.

### W1.1 — Unidade Organizacional (UO) + árvore + seed 🔴
- **tipo:** fullstack · **área:** Identidade
- **objetivo:** introduzir a dimensão organizacional (Secretarias/Departamentos/Setores) base do escopo de autorização e da ponte com a Unidade Gestora contábil.
- **escopo:** agregado `UnidadeOrganizacional` (árvore auto-FK, `Codigo` único/tenant, `Tipo`, `UnidadeGestoraContabilId?`, `Ativa`, invariante de subárvore conexa); migration `unidades_organizacionais`; CRUD HTTP; tela React **Estrutura Organizacional** (árvore, ativar/desativar, vínculo UG) no gov.br DS. `[a confirmar]` aderência do `Tipo`/UG ao cadastro UG do TCE-RS.
- **dependências:** W0.1, W0.3.
- **marco:** M1 · **prioridade:** 🔴 · `[OFICIAL]` (granularidade UG do TCE-RS)
- **critério-de-pronto:** árvore de UOs criável/editável; seed do piloto parametrizável (nunca hardcoded).

### W1.2 — AtribuicaoDePapel com escopo (ABAC do sujeito) + migração de dados 🔴
- **tipo:** fullstack · **área:** Identidade
- **objetivo:** substituir o RBAC plano (`Usuario.HashSet<PapelId>`) por atribuição com escopo de UO, vigência e origem.
- **escopo:** entidade `AtribuicaoDePapel` (`UnidadeId`, `IncluiSubunidades`, `Vigencia`, `Origem`); `CalculadoraPermissoesEfetivas` passa a devolver `(permissão → conjunto de UOs)`; migration `atribuicoes_papel` + **migração de dados** (cada par atual vira atribuição na UO raiz com `IncluiSubunidades=true`, `Origem=Direta` — preserva comportamento global no go-live); tela **Usuário → Atribuições** (papel + UO + subunidades + vigência).
- **dependências:** W1.1.
- **marco:** M1 · **prioridade:** 🔴
- **critério-de-pronto:** permissões efetivas escopadas por UO; isolamento por tenant intacto; migração idempotente testada.

### W1.3 — Filtro de leitura por UO + guards de command 🔴
- **tipo:** backend · **área:** SharedKernel/BuildingBlocks + módulos fiscais
- **objetivo:** restringir leituras e mutações ao escopo de UO do sujeito (análogo ao Global Query Filter de tenant).
- **escopo:** marcador `IMustHaveUnidade { Guid UnidadeId }` em SharedKernel; Global Query Filter de UO por reflexão; guards de command-side (`unidade(recurso) ∈ escopoUO(sujeito, permissão)`); aplicar `UnidadeId` nas entidades fiscais prioritárias (Finanças/Patrimônio/RH) via migrations por módulo.
- **dependências:** W1.2.
- **marco:** M1 · **prioridade:** 🔴
- **critério-de-pronto:** "empenho só da Secretaria de Saúde" funciona; testes de escopo verdes.

### W1.4 — Verbos finos SoD + catálogo de permissões estendido 🟠
- **tipo:** backend · **área:** Identidade
- **objetivo:** habilitar Segregação de Funções nos atos com efeito legal/fiscal.
- **escopo:** acrescentar ao `FrozenSet` (compatível, nada removido) `financas.empenho.assinar`, `financas.liquidacao.atestar`, `financas.pagamento.ordenar`, `financas.exercicio.encerrar`, `transparencia.remessa.transmitir`, `protocolo.documento.assinar`; regra I9 (permissões conflitantes não coexistem no mesmo sujeito/UO). `[a confirmar]` matriz oficial SoD do TCE-RS.
- **dependências:** W1.2.
- **marco:** M1 · **prioridade:** 🟠 · `[OFICIAL]` (matriz SoD TCE-RS)
- **critério-de-pronto:** quem liquida não pode pagar no mesmo escopo; testado.

### W1.5 — Delegação de administração com escopo (D1–D6) 🟠
- **tipo:** fullstack · **área:** Identidade
- **objetivo:** materializar "não delega o que não tem" (regra-mãe I4).
- **escopo:** agregado `ConcessaoDelegacao` (`UnidadeEscopo`, `PermissoesDelegaveis`, `Vigencia`, `PodeSubdelegar=false`); algoritmo D1–D6 provado no domínio; tela **Delegações** (UI mostra só permissões que o concedente possui). `[a confirmar]` política de subdelegação no setor público.
- **dependências:** W1.4.
- **marco:** M1 · **prioridade:** 🟠
- **critério-de-pronto:** secretário cria usuários só na própria secretaria, sem conceder o que não tem; provas no domínio.

### W1.6 — Sensibilidade do dado (ABAC do recurso) + trilha de leitura LGPD 🟠
- **tipo:** fullstack · **área:** SharedKernel + módulos sensíveis + Identidade
- **objetivo:** clearance ≥ sensibilidade + registro de quem leu dado `SensivelLGPD`.
- **escopo:** enum `NivelSensibilidade` (`Publico<Interno<Restrito<SensivelLGPD`) em SharedKernel; coluna nas entidades sensíveis (Saúde/Assistência/Educação/Tributos sigilo fiscal); verbos `*.dados.sensiveis.acessar`; store append-only de trilha de leitura; banner + justificativa obrigatória na UI. `[a confirmar]` registro da base legal LGPD e papel do DPO.
- **dependências:** W1.3.
- **marco:** M1 · **prioridade:** 🟠
- **critério-de-pronto:** acesso a recurso sensível exige clearance e gera trilha de leitura imutável.

---

## M2 — FUNDAÇÃO CONTÁBIL PCASP + SEGURANÇA FISCAL (a FONTE de tudo) — ✅ ENTREGUE (board=completed, ADR-0008/0010/0013)

> A maior preocupação do dono começa aqui: partida dobrada real + assinatura A1 (pré-requisito de TCE/eSocial).
> **Histórico** — PCASP síncrono/transacional (ΣD=ΣC) e A1 entregues; ver ADR-0013/0008. "0 linhas"/"18 handlers órfãos" é foto pré-M2.

### W2.1 — Azure Key Vault em runtime + assinatura A1 ICP-Brasil 🔴
- **tipo:** backend · **área:** BuildingBlocks/ApiHost (transversal)
- **objetivo:** fiar o Key Vault e a assinatura digital A1 (X509 + XMLDSig/XAdES) por tenant — pré-requisito de TCE, SICONFI, eSocial e Protocolo.
- **escopo:** `AddAzureKeyVault`/`SecretClient`/`DefaultAzureCredential` no `Program.cs`; cofre de certificado A1 por tenant (cifrado, ver `docs/architecture/certificado-a1.md`); serviço de assinatura `SignedXml`/`X509Certificate2`; health check de KV; ACL e Polly. `[a confirmar]` perfil de assinatura por destino (XAdES para SICONFI, etc.).
- **dependências:** W0.1, W0.3.
- **marco:** M2 · **prioridade:** 🔴 · `[OFICIAL]` (perfis de assinatura)
- **critério-de-pronto:** XML assinado verificável; cert resolvido do KV por tenant; sem segredo no repo.

### W2.2 — Expor API de Finanças (18 handlers órfãos) + frontend do ciclo da despesa 🟠
- **tipo:** fullstack · **área:** Financas
- **objetivo:** tornar o ciclo Lei 4.320 (Dotação→Empenho→Liquidação→Pagamento→Restos a Pagar) navegável fim-a-fim.
- **escopo:** expor os 18 handlers órfãos via HTTP; ListPages de Empenho/Liquidação/Pagamento/Restos a Pagar/Receita; gating `<Can>` consistente com verbos SoD (W1.4); escopo de UO (W1.3).
- **dependências:** W0.1, W1.3, W1.4.
- **marco:** M2 · **prioridade:** 🟠
- **critério-de-pronto:** operador percorre o ciclo da despesa inteiro na UI, respeitando UO e SoD.

### W2.3 — Plano de Contas PCASP + agregados contábeis (partida dobrada) 🔴
- **tipo:** backend · **área:** Financas (Contabilidade)
- **objetivo:** criar o motor contábil — hoje 0 linhas.
- **escopo:** **[OFICIAL]** carregar PCASP (8 classes, natureza da informação P/O/C, natureza do saldo, nível, conta-mãe) versionável por exercício (STN/MCASP 11ª ed.); agregados `PlanoDeContas`/`ContaContabil`, `LancamentoContabil` + `PartidaContabil` com invariante forte **ΣD=ΣC**, imutável após registro (estorno por novo lançamento); `EventoContabil` (roteiro fato→partidas parametrizável por tenant, vigência por exercício). Marcar códigos exatos com `// TODO(validar-leiaute-oficial)` até fonte confirmada.
- **dependências:** W0.1, W2.2.
- **marco:** M2 · **prioridade:** 🔴 · `[OFICIAL]` (PCASP/MCASP)
- **critério-de-pronto:** lançamento manual fecha em partida dobrada; plano de contas versionado por exercício.

### W2.4 — Lançamento contábil automático (subscrição dos domain events) 🔴
- **tipo:** backend · **área:** Financas + Patrimonio (contrapartida)
- **objetivo:** gerar lançamentos automaticamente a partir dos eventos do ciclo orçamentário e patrimonial.
- **escopo:** handlers que escutam `DotacaoAprovada`, `EmpenhoEmitido`, `DespesaLiquidada`, `PagamentoEfetuado`, `ReceitaArrecadada`, virada de Restos a Pagar → resolvem `EventoContabil` vigente → emitem `LancamentoContabil` na MESMA unidade de trabalho/Outbox; **[OFICIAL]** roteiros MCASP (3 enfoques: orçamentário+patrimonial+controle); contrapartida contábil de Patrimônio (VPD/VPA, depreciação).
- **dependências:** W2.3.
- **marco:** M2 · **prioridade:** 🔴 · `[OFICIAL]` (roteiros MCASP)
- **critério-de-pronto:** empenhar/liquidar/pagar/arrecadar gera lançamento correto e balanceado, sem digitação manual.

### W2.5 — Balancete (read model) 🟠
- **tipo:** fullstack · **área:** Financas
- **objetivo:** consolidar saldos por conta/período como base das demonstrações e da MSC.
- **escopo:** `Balancete` (saldo anterior, débitos, créditos, saldo atual) derivado dos lançamentos; conferência D=C global; ListPage + detalhe no gov.br DS.
- **dependências:** W2.4.
- **marco:** M2 · **prioridade:** 🟠
- **critério-de-pronto:** balancete fecha (ΣD=ΣC) e reflete os lançamentos automáticos.

---

## M3 — DEMONSTRAÇÕES DCASP + MSC + PLANEJAMENTO ORÇAMENTÁRIO — ✅ ENTREGUE (board=completed, ADR-0011/0013)

> **Histórico** — DCASP + MSC derivada dos lançamentos + `MSCGeradaIntegrationEvent` entregues; ver ADR-0011/0013. Não re-implementar.

### W3.1 — Planejamento PPA/LDO/LOA + vínculo com a execução 🟠
- **tipo:** fullstack · **área:** Financas
- **objetivo:** preencher o ciclo de planejamento (hoje só existe a execução) e travar despesa sem dotação.
- **escopo:** agregados PPA/LDO/LOA (programas, ações, metas); **[OFICIAL]** anexos da Lei 4.320 (Portaria STN 438/2012); vínculo LOA→Dotação→Empenho (vedar despesa sem dotação/empenho — art. 60); telas de planejamento e anexos.
- **dependências:** W2.2.
- **marco:** M3 · **prioridade:** 🟠 · `[OFICIAL]` (anexos Lei 4.320)
- **status:** ✅ ENTREGUE (board=completed)
- **critério-de-pronto:** LOA aprovada gera dotações; empenho sem dotação é bloqueado.

### W3.2 — 7 Demonstrações DCASP 🟠
- **tipo:** fullstack · **área:** Financas
- **objetivo:** emitir as demonstrações contábeis a partir do balancete.
- **escopo:** **[OFICIAL]** Balanço Orçamentário, Financeiro, Patrimonial, DVP, DMPL, DFC e Notas Explicativas derivados do balancete (MCASP); telas de visualização/exportação.
- **dependências:** W2.5.
- **marco:** M3 · **prioridade:** 🟠 · `[OFICIAL]` (DCASP)
- **status:** ✅ ENTREGUE (board=completed)
- **critério-de-pronto:** as 7 demonstrações batem com o balancete do período.

### W3.3 — Matriz de Saldos Contábeis (MSC) derivada dos lançamentos 🔴
- **tipo:** backend · **área:** Financas → Transparencia (Contracts)
- **objetivo:** transformar o read model órfão da MSC em fonte da verdade alimentada pelo PCASP.
- **escopo:** gerar MSC do balancete (conta PCASP, saldo, Tipo de Valor inicial/movimento D-C/final, natureza, indicador F/P, informações complementares); **publicar de verdade** `MSCGeradaIntegrationEvent` (corrige os 0 publishers); MSC Agregada (mensal) e de Encerramento (anual); **[OFICIAL]** Regras Gerais MSC (Portaria STN 642/896 do exercício).
- **dependências:** W2.4, W2.5.
- **marco:** M3 · **prioridade:** 🔴 · `[OFICIAL]` (Regras MSC)
- **status:** ✅ ENTREGUE (board=completed)
- **critério-de-pronto:** `ReceberMSCGeradaHandler` em Transparencia recebe MSC real; pipeline contábil→MSC dispara.

### W3.4 — Encerramento de exercício contábil (apuração de resultado + transposição de saldos) 🔴
- **tipo:** backend · **área:** Financas (Contabilidade)
- **objetivo:** fechar o exercício corretamente ANTES da prestação anual — sem isto a MSC de dezembro e a DCA anual nascem incorretas (falha que só aparece em jan/mar e trava a parte mais escrutinada pelo TCE). Cf. AUDITORIA **K3**.
- **escopo:** **[OFICIAL]** lançamentos de encerramento das contas de resultado (apuração de superávit/déficit do exercício, classe 6 vs receita/despesa), transposição/transferência de saldos do exercício para o seguinte, abertura do exercício seguinte (saldos iniciais) e encerramento dos restos a pagar; verbo SoD `financas.exercicio.encerrar` (já no catálogo W1.4); idempotência por exercício (encerrar é irreversível salvo estorno por novo lançamento). Consolida o que estava pulverizado em W2.4/W3.3/W4.5.
- **dependências:** W2.4, W3.3.
- **marco:** M3 · **prioridade:** 🔴 · `[OFICIAL]` (MCASP — roteiros de encerramento do exercício)
- **status:** ⏳ PENDENTE (lacuna apontada pela AUDITORIA — bloqueia a prestação anual)
- **critério-de-pronto:** encerrar o exercício gera os lançamentos de apuração de resultado balanceados (ΣD=ΣC), transpõe saldos e abre o exercício seguinte; a MSC de Encerramento (W3.3) e a DCA anual (W4.3) consomem o resultado correto.

---

## M4 — PRESTAÇÃO DE CONTAS TCE-RS + SICONFI (meta nº1 do dono) — ✅ ENTREGUE (board=completed, ADR-0009)

> **Histórico** — meta nº1 atingida: artefatos gerados+validados+empacotados+reconciliados; **envio é ato humano** (ADR-0009). Não re-implementar; a reescrita abaixo (W4.2/W4.3) já reflete o ADR-0009.

### W4.1 — Remessa SIAPC/PAD largura-fixa (leiaute oficial) 🔴
- **tipo:** fullstack · **área:** Transparencia/Financas
- **objetivo:** substituir o `.txt` de 3 linhas hardcoded por remessa real campo-a-campo.
- **escopo:** **[OFICIAL]** leiaute SIAPC campo-a-campo (largura fixa, 1 reg/linha, CR/LF, sem binário; MT-ASCE Vol. IV/V; PAD v25/26.x) gerado da MSC/balancete; Código de Remessa único; conteúdo acumulado 1º-jan→data ref; uso de `Span<T>` para fatiar sem alocação; tela de geração/acompanhamento.
- **dependências:** W3.3, W2.1.
- **marco:** M4 · **prioridade:** 🔴 · `[OFICIAL]` (SIAPC/PAD)
- **status:** ✅ ENTREGUE (board=completed)
- **critério-de-pronto:** arquivo gerado com posições/tamanhos corretos do exercício vigente.

### W4.2 — e-Validador (RDI) real + empacotamento + registro de protocolo do ato humano 🔴
- **tipo:** backend · **área:** Transparencia
- **objetivo:** **gerar, pré-validar e empacotar** a remessa de verdade (hoje stub aprova qualquer coisa) e **registrar o protocolo do envio feito pelo servidor**. Conforme **ADR-0009**: **o TCE-RS não tem web service de upload** — a transmissão é ato humano no PAD desktop / e-Protocolo com **certificado pessoal ICP-Brasil** do responsável (não o A1 institucional). Não construímos cliente de envio.
- **escopo:** implementar as críticas do **RDI vigente** que bloqueiam (substituir `SimuladoEValidadorTce` por pré-validação local real); **empacotar** (ZIP nomeado + hash + artefato de auditoria); registrar como artefato de auditoria o **recibo/protocolo** que o servidor cola após o envio manual; idempotência por `RemessaTceId`. Remove o `EnviarRemessaTce` (POST) e o "cliente de transmissão SOAP/REST" (premissa refutada). A assinatura A1 (W2.1) cobre só o que assinamos server-side, **não** o RVE/RDI no e-Protocolo (esse é certificado pessoal).
- **dependências:** W4.1, W2.1.
- **marco:** M4 · **prioridade:** 🔴 · `[OFICIAL]` (RDI/PAD vigente — MT 2026)
- **status:** ✅ ENTREGUE (board=completed; ver ADR-0009)
- **critério-de-pronto:** remessa passa na pré-validação RDI local, é empacotada (ZIP+hash) e o protocolo do ato humano fica registrado como trilha de auditoria. **Não há "um clique e transmitiu"** (ADR-0009).

### W4.3 — SICONFI: RREO, RGF, DCA + MSC (gerar + empacotar + reconciliar) 🔴
- **tipo:** fullstack · **área:** Transparencia/Financas
- **objetivo:** **gerar e reconciliar** a prestação à União derivada da MSC. Conforme **ADR-0009**: o **SICONFI só tem API de CONSULTA** (Dados Abertos), **não de upload** — a MSC/declaração é **upload manual** no portal, homologado com **e-CPF A3 em token** do gestor. Não construímos cliente de transmissão.
- **escopo:** **[OFICIAL]** gerar os artefatos RREO (bimestral), RGF (quadrimestral), DCA (anual) e MSC Agregada/Encerramento (Regras Gerais MSC + Port. STN 642/896 do exercício) derivados da MSC; **empacotar** a MSC (CSV/XBRL-GL zipada + hash); **reconciliar** o que foi homologado via a **API de consulta** (`/extrato_entregas`) do SICONFI, atrás de Polly + ACL, idempotente, via Outbox; calendário fiscal versionável por exercício; telas de geração/acompanhamento de prazo (LRF) e do **protocolo do upload manual**. Remove "cliente API SICONFI" de envio (premissa refutada — só consulta).
- **dependências:** W3.3, W2.1, W3.4 (DCA/MSC de Encerramento consomem o exercício encerrado).
- **marco:** M4 · **prioridade:** 🔴 · `[OFICIAL]` (Regras MSC/Instruções RREO/RGF do exercício)
- **status:** ✅ ENTREGUE (board=completed; ver ADR-0009 — geração+reconciliação. O encerramento W3.4 que alimenta a DCA é a parte pendente)
- **critério-de-pronto:** RREO/RGF/DCA/MSC **gerados corretos**, empacotados e **reconciliados pela API de consulta**; o protocolo do upload manual fica registrado. Envio = ato humano com A3 pessoal (ADR-0009).

### W4.4 — Tesouraria / Controle de Caixa / Conciliação bancária 🟠
- **tipo:** fullstack · **área:** Financas
- **objetivo:** fechar o lado financeiro (caixa/banco) que alimenta o pagamento e a conciliação.
- **escopo:** contas bancárias, movimento de caixa, conciliação CNAB 240 (retorno) com a execução; telas de tesouraria.
- **dependências:** W2.4.
- **marco:** M4 · **prioridade:** 🟠
- **status:** ✅ ENTREGUE (board=completed)
- **critério-de-pronto:** extrato bancário concilia com pagamentos/arrecadação.

### W4.5 — Controladoria / Controle Interno + papéis especiais 🟡
- **tipo:** fullstack · **área:** Financas/Identidade
- **objetivo:** atender o controle interno (CF 74) com leitura ampla intra-tenant sem mutar nem ver sensível individual.
- **escopo:** papel Controlador (leitura ampla intra-tenant); painéis de controle interno; pareceres; trilha de auditoria; `[a confirmar]` exigências da Res. TCE-RS para contas anuais (Res. 1134/2020).
- **dependências:** W4.2, W1.6.
- **marco:** M4 · **prioridade:** 🟡 · `[OFICIAL]` (Res. 1134/2020)
- **status:** ✅ ENTREGUE (board=completed)
- **critério-de-pronto:** controlador acessa visão consolidada de leitura, auditada, sem violar LGPD.

---

## M5 — RECURSOS HUMANOS COMPLETO (alimenta remessa de folha TCE-RS) — ⏳ PENDENTE (foco vivo)

> **Bloco de trabalho ativo.** Inclui agora os destinos fiscais federais que faltavam (AUDITORIA K4/H9):
> **W5.6 EFD-Reinf+DCTFWeb** (retenções cruzando NFS-e/ADN) e **W5.7 CADPREV/RPPS** `[a confirmar]` (só se o piloto tiver RPPS).

### W5.1 — Motor de cálculo de folha (INSS/RPPS/IRRF/consignados) 🔴
- **tipo:** fullstack · **área:** RecursosHumanos
- **objetivo:** calcular a folha (hoje há estrutura e abate-teto, sem motor).
- **escopo:** motor de cálculo parametrizável por exercício (faixas INSS/IRRF, RPPS, consignados); reconciliação da folha; contrapartida contábil (W2.4); telas de processamento/fechamento.
- **dependências:** W2.4, W1.3.
- **marco:** M5 · **prioridade:** 🔴 · `[OFICIAL]` (tabelas INSS/IRRF/RPPS do exercício)
- **critério-de-pronto:** folha fecha com líquido correto e gera lançamento contábil.

### W5.2 — eSocial S-1.3 (eventos + transmissão SOAP) 🔴
- **tipo:** backend · **área:** RecursosHumanos
- **objetivo:** transmitir eventos ao eSocial (hoje só valida S-1010 local).
- **escopo:** **[OFICIAL]** eventos S-1000/tabelas, S-1200/S-1202/S-1207, S-2200/S-2299, S-1299 (XSD S-1.3); XML assinado A1 (W2.1); WebService SOAP + ambientes (produção/restrita); Polly + ACL + Outbox idempotente.
- **dependências:** W5.1, W2.1.
- **marco:** M5 · **prioridade:** 🔴 · `[OFICIAL]` (eSocial S-1.3)
- **critério-de-pronto:** competência fechada (S-1299) aceita no ambiente restrito.

### W5.3 — Ponto eletrônico (AFD/AEJ, Port. MTP 671/2021) 🟠
- **tipo:** fullstack · **área:** RecursosHumanos
- **objetivo:** registro de ponto com imutabilidade e arquivos fiscais.
- **escopo:** **[OFICIAL]** AFD (assinado) + AEJ; marcações imutáveis; REP-C/REP-A/REP-P; geração de arquivos sob demanda; telas de espelho de ponto.
- **dependências:** W5.1.
- **marco:** M5 · **prioridade:** 🟠 · `[OFICIAL]` (Port. 671/2021)
- **critério-de-pronto:** AFD/AEJ gerados conforme leiaute; marcações não editáveis.

### W5.4 — Remessa de folha TCE-RS (Res. 1099/2018, `TCE_4810`) — gerar + validar + empacotar 🟠
- **tipo:** backend · **área:** RecursosHumanos/Transparencia
- **objetivo:** **gerar, pré-validar e empacotar** a folha para prestação ao TCE-RS. Conforme **ADR-0009**, o **envio é ato humano no PAD/e-Protocolo com certificado pessoal** — reusa de W4.2 o **empacotamento + pré-validação local + registro de protocolo**, **não** uma "transmissão" (que não existe).
- **escopo:** **[OFICIAL]** leiaute `TCE_4810.TXT` largura fixa (cadastro de servidores, vantagens/descontos, totalizadores, classes salariais, base legal); pré-validação no PAD; empacotamento (ZIP+hash) e registro do protocolo do ato humano (reusa o pipeline local de W4.2 — empacotar/validar, não enviar).
- **dependências:** W5.1, W4.2.
- **marco:** M5 · **prioridade:** 🟠 · `[OFICIAL]` (Res. 1099/2018)
- **status:** ⏳ PENDENTE
- **critério-de-pronto:** remessa de folha gerada, pré-validada no PAD e empacotada; protocolo do envio manual registrado.

### W5.5 — Minha Folha (self-service do servidor) 🟢
- **tipo:** fullstack · **área:** RecursosHumanos
- **objetivo:** portal do servidor (contracheque, informe de rendimentos).
- **escopo:** rotas autenticadas do servidor; download de contracheque/IRRF; gov.br DS + a11y.
- **dependências:** W5.1.
- **marco:** M5 · **prioridade:** 🟢
- **status:** ⏳ PENDENTE
- **critério-de-pronto:** servidor consulta e baixa o próprio contracheque.

### W5.6 — EFD-Reinf (R-2010/R-4020) + DCTFWeb 🔴
- **tipo:** backend · **área:** RecursosHumanos/Tributos (cruza retenção × ingestão NFS-e/ADN)
- **objetivo:** escriturar as **retenções** em que o ente é tomador/retentor (INSS 11%, IRRF/CSRF sobre as NFS-e já ingeridas via ADN) e **confessar o débito** na DCTFWeb. A EFD-Reinf **substituiu a DIRF (extinta 2024)** e alimenta a DCTFWeb. Cf. AUDITORIA **K4**. Lacuna que gera recolhimento irregular e multa.
- **escopo:** **[OFICIAL]** eventos EFD-Reinf **R-2010** (retenção INSS sobre serviços tomados) e **R-4020** (retenções IRRF/CSRF — PIS/COFINS/CSLL), cruzando a retenção apurada com a **ingestão NFS-e/ADN** (Worker `NfseSync`) e com a folha (W5.1); fechamento que consolida eSocial + EFD-Reinf na **DCTFWeb/MIT** (gera o DAE); XML assinado A1 (W2.1); pré-validação local + reconciliação. `[a confirmar]` **regime do órgão público** (administração direta — quais eventos R-* efetivamente se aplicam) e periodicidade exata da DCTFWeb.
- **dependências:** W5.1, W2.1, ingestão NFS-e/ADN (Tributos).
- **marco:** M5 · **prioridade:** 🔴 · `[OFICIAL]` (EFD-Reinf S-1.5 / leiaute DCTFWeb vigente) · `[a confirmar]` (regime de órgão público)
- **status:** ⏳ PENDENTE (lacuna AUDITORIA — mensal dia 15; multa R$ 500–1.500/mês)
- **critério-de-pronto:** R-2010/R-4020 gerados cruzando retenção × NFS-e; DCTFWeb consolida eSocial+EFD-Reinf e gera o DAE.

### W5.7 — CADPREV / DRPPS (RPPS) 🟠
- **tipo:** fullstack · **área:** RecursosHumanos
- **objetivo:** apuração previdenciária do **RPPS** (correlação S-1202/S-1207), se o piloto tiver RPPS próprio. Cf. AUDITORIA **H9**.
- **escopo:** **[OFICIAL]** layout CADPREV/DRPPS; correlação com os eventos eSocial S-1202/S-1207; demonstrativos previdenciários. **[a confirmar] — depende da realidade do piloto: Maximiliano de Almeida é RPPS ou RGPS.** Se for RGPS (sem regime próprio), este workflow **sai do escopo** (vira excesso) e a contribuição previdenciária já é coberta por eSocial/EFD-Reinf.
- **dependências:** W5.2.
- **marco:** M5 · **prioridade:** 🟠 · `[OFICIAL]` (CADPREV) · `[a confirmar]` (RPPS×RGPS do piloto — só implementar se RPPS)
- **status:** ⏳ PENDENTE / `[a confirmar]` aplicabilidade
- **critério-de-pronto:** apuração previdenciária RPPS gerada no layout CADPREV — **condicionado** a o piloto ter RPPS.

---

## M6 — TRIBUTOS (receita própria + sub-módulos de receita)

### W6.1 — Cadastro Imobiliário (CIB/PGV) — base do IPTU 🔴
- **tipo:** fullstack · **área:** Tributos
- **objetivo:** criar a base cadastral que hoje inexiste e bloqueia o IPTU.
- **escopo:** **[OFICIAL]** cadastro imobiliário (CIB), Planta Genérica de Valores (PGV), logradouros, fração ideal; sensibilidade (W1.6); telas de cadastro/consulta.
- **dependências:** W1.3, W1.6.
- **marco:** M6 · **prioridade:** 🔴 · `[OFICIAL]` (CIB)
- **critério-de-pronto:** imóvel cadastrado com valor venal calculável pela PGV.

### W6.2 — Motor IPTU (apuração + lançamento em lote + carnê) 🔴
- **tipo:** fullstack · **área:** Tributos
- **objetivo:** apurar e lançar o IPTU.
- **escopo:** cálculo de valor venal → lançamento em lote → carnê; **[OFICIAL]** DAM com código de barras FEBRABAN + PIX; SELIC/juros/multa parametrizáveis.
- **dependências:** W6.1.
- **marco:** M6 · **prioridade:** 🔴 · `[OFICIAL]` (FEBRABAN/SELIC)
- **critério-de-pronto:** carnê emitido com guia válida para imóvel cadastrado.

### W6.3 — Motor ISS / eNota / Livro Eletrônico + ITBI 🟠
- **tipo:** fullstack · **área:** Tributos
- **objetivo:** apurar ISS (cadastro mobiliário, LC 116, retenção/Simples) e ITBI.
- **escopo:** **[OFICIAL]** cadastro mobiliário, apuração ISS, livro eletrônico (ingestão NFS-e/ADN já existe); ITBI (Tema 1.124 STF); guias DAM.
- **dependências:** W6.2.
- **marco:** M6 · **prioridade:** 🟠 · `[OFICIAL]` (LC 116, Tema 1.124)
- **critério-de-pronto:** ISS apurado a partir das NFS-e ingeridas; ITBI lançável.

### W6.4 — Taxas / COSIP / Alvarás / Contribuição de Melhoria 🟠
- **tipo:** fullstack · **área:** Tributos
- **objetivo:** demais receitas próprias + emissão de alvarás.
- **escopo:** motores de Taxas/COSIP/Contribuição de Melhoria; sub-módulo Alvarás (emissão/licenciamento de funcionamento); guias DAM.
- **dependências:** W6.2.
- **marco:** M6 · **prioridade:** 🟠
- **critério-de-pronto:** taxa lançada e alvará emitido com guia.

### W6.5 — Procuradoria: protesto + execução fiscal + coexistência ISS↔IBS 🟠
- **tipo:** fullstack · **área:** Tributos
- **objetivo:** cobrança da dívida e preparação para a reforma tributária.
- **escopo:** **[OFICIAL]** protesto CRA/IEPTB (Lei 9.492); retorno CNAB 240/400; **[OFICIAL]** coexistência ISS↔IBS (EC 132/LC 214 — teste 2026); sigilo fiscal (sensibilidade W1.6).
- **dependências:** W6.3, W1.6.
- **marco:** M6 · **prioridade:** 🟠 · `[OFICIAL]` (Lei 9.492, EC 132/LC 214)
- **critério-de-pronto:** dívida ativa protestável; remessa CNAB de protesto gerada.

### W6.6 — Sub-módulos de receita: Meio Ambiente, Planejamento Urbano, Cemitério 🟢
- **tipo:** fullstack · **área:** Tributos
- **objetivo:** paridade Betha em receitas setoriais.
- **escopo:** licenciamento ambiental; planejamento urbano; gestão de cemitério (jazigos/sepultamentos) — cada um backend+frontend, ligado a guias/taxas.
- **dependências:** W6.4.
- **marco:** M6 · **prioridade:** 🟢 · `[OFICIAL]` (licenciamento ambiental)
- **critério-de-pronto:** cada sub-módulo navegável com cobrança associada.

---

## M7 — DOMÍNIOS COM RISCO DE REPASSE (Saúde, Educação, Assistência)

### W7.1 — Saúde: RNDS (FHIR R4) + SI-PNI 🟠
- **tipo:** backend · **área:** Saude
- **objetivo:** eventos clínicos reais (hoje tudo `Simulado*`).
- **escopo:** **[OFICIAL]** RNDS FHIR-first (FHIR R4 + e-CNPJ ICP-Brasil); SI-PNI (imunização via RNDS); ACL + Polly + Outbox.
- **dependências:** W2.1.
- **marco:** M7 · **prioridade:** 🟠 · `[OFICIAL]` (RNDS/FHIR)
- **critério-de-pronto:** evento de imunização enviado e confirmado na RNDS (homologação).

### W7.2 — Saúde: e-SUS APS→SISAB + CNES + SIA/SIH + SIOPS 🟠
- **tipo:** fullstack · **área:** Saude
- **objetivo:** cofinanciamento e produção (risco de suspensão de repasse).
- **escopo:** **[OFICIAL]** e-SUS APS→SISAB (LEDI, Port. 3.493/2024); CNES mensal (Port. 708/2007); SIA/SIH (BPA/APAC/AIH); SIOPS (mínimo 15% ASPS); painel de cofinanciamento.
- **dependências:** W7.1, W4.3.
- **marco:** M7 · **prioridade:** 🟠 · `[OFICIAL]`
- **critério-de-pronto:** produção exportada nos formatos DATASUS; painel de indicadores.

### W7.3 — Saúde: Farmácia (e-SUS AF/BNAFAR) + Vigilância Sanitária 🟢
- **tipo:** fullstack · **área:** Saude
- **objetivo:** sub-módulos faltantes de paridade Betha.
- **escopo:** **[OFICIAL]** assistência farmacêutica e-SUS AF/BNAFAR (Port. 11.585/2026, substitui HÓRUS); vigilância sanitária; dispensação.
- **dependências:** W7.1.
- **marco:** M7 · **prioridade:** 🟢 · `[OFICIAL]`
- **critério-de-pronto:** movimentação de farmácia exportada ao BNAFAR.

### W7.4 — Educação: Educacenso/INEP + SIOPE 🟠
- **tipo:** fullstack · **área:** Educacao
- **objetivo:** fonte de verdade FUNDEB e aplicação mínima MDE (25%).
- **escopo:** **[OFICIAL]** Educacenso/INEP (layout migrador, 2 etapas); SIOPE bimestral conciliado com PCASP/MSC.
- **dependências:** W3.3.
- **marco:** M7 · **prioridade:** 🟠 · `[OFICIAL]`
- **critério-de-pronto:** Censo gerado no layout; SIOPE concilia com a contabilidade.

### W7.5 — Educação: PNAE (merenda) + PNATE (transporte) 🟠
- **tipo:** fullstack · **área:** Educacao
- **objetivo:** prestação de contas FNDE (sub-módulos ausentes).
- **escopo:** **[OFICIAL]** PNAE (SiGPC, 30% agricultura familiar) e PNATE (SiGPC mensal + anual); telas de gestão e prestação.
- **dependências:** W7.4.
- **marco:** M7 · **prioridade:** 🟠 · `[OFICIAL]`
- **critério-de-pronto:** prestação PNAE/PNATE gerada no SiGPC.

### W7.6 — Educação: portais Professores / Pais e Alunos 🟢
- **tipo:** fullstack · **área:** Educacao
- **objetivo:** paridade Betha (camada de relacionamento escolar).
- **escopo:** portal de professores (diário, frequência) e de pais/alunos; gov.br DS + a11y.
- **dependências:** W7.4.
- **marco:** M7 · **prioridade:** 🟢
- **critério-de-pronto:** professor lança diário; responsável consulta boletim.

### W7.7 — Assistência: integração CadÚnico/MDS (RMA, Censo SUAS, SUASWeb) 🟡
- **tipo:** fullstack · **área:** AssistenciaSocial
- **objetivo:** import/export real por layout MDS (hoje só leitura simulada).
- **escopo:** **[OFICIAL]** CadÚnico/CECAD (CPF chave, NIS), RMA mensal, Censo SUAS, SUASWeb; sensibilidade LGPD (W1.6).
- **dependências:** W1.6.
- **marco:** M7 · **prioridade:** 🟡 · `[OFICIAL]`
- **critério-de-pronto:** RMA exportada no layout MDS; consulta CadÚnico real.

---

## M8 — CAMADA DO CIDADÃO + PORTAL DO GESTOR + BI (diferencial Betha)

### W8.1 — Portal do Cidadão / Minha Cidade (web) 🟠
- **tipo:** fullstack · **área:** novo módulo Cidadão (Tributos/Protocolo/Transparencia via Contracts)
- **objetivo:** camada voltada ao cidadão, hoje inexistente.
- **escopo:** serviços online (2ª via de guias, agendamento, abertura de protocolo, consulta de processo); autenticação cidadão (gov.br/CPF); gov.br DS + WCAG AA.
- **dependências:** W6.2, W0.1.
- **marco:** M8 · **prioridade:** 🟠
- **critério-de-pronto:** cidadão emite 2ª via e abre protocolo sem ir à prefeitura.

### W8.2 — Portal do Gestor (painel executivo) 🟠
- **tipo:** fullstack · **área:** novo módulo Gestão (read models via Contracts)
- **objetivo:** visão executiva (prefeito/secretários) — diferencial Betha em toda linha.
- **escopo:** painéis de execução orçamentária, saúde/educação (índices constitucionais), RH, arrecadação; escopo por UO (W1.3); alertas de prazo (LRF/repasse).
- **dependências:** W3.3, W4.3, W5.1, W6.2.
- **marco:** M8 · **prioridade:** 🟠
- **critério-de-pronto:** gestor vê índices constitucionais e execução em tempo quase real.

### W8.3 — BI por área (Contábil, Saúde, Educação, Administrativo) 🟢
- **tipo:** fullstack · **área:** Gestão
- **objetivo:** análises e relatórios por área.
- **escopo:** read models analíticos + dashboards por área; exportação CSV/JSON; dados abertos.
- **dependências:** W8.2.
- **marco:** M8 · **prioridade:** 🟢
- **critério-de-pronto:** dashboards por área com drill-down e exportação.

### W8.4 — Transparência LAI completa (tempo real + dados abertos) 🟡
- **tipo:** fullstack · **área:** Transparencia
- **objetivo:** publicar execução em tempo real (LC 131/2009).
- **escopo:** **[OFICIAL]** portal público de transparência (execução em tempo real, dados abertos CSV/JSON, retenção 5 anos); e-SIC.
- **dependências:** W4.2.
- **marco:** M8 · **prioridade:** 🟡 · `[OFICIAL]` (LC 131/2009)
- **critério-de-pronto:** registro aparece no portal no 1º dia útil; e-SIC funcional.

---

## M9 — SUPRIMENTOS AVANÇADOS + COMPLIANCE FINAL + ROBUSTEZ DE PLATAFORMA — ⏳ PENDENTE

> Inclui agora os destinos/obrigações que faltavam (AUDITORIA H10/K5/M-1): **W9.6** ligado ao
> **Transferegov.br**, **W9.9 SADIPEM/CDP** (passivo PCASP → STN, anual) e **W9.10 conformidade SIAFIC**
> (o próprio sistema como SIAFIC do ente). W9.7 reescrito para **banco dedicado por tenant** (ADR-0005).

### W9.1 — PNCP bloqueante (condição de eficácia) 🟠
- **tipo:** fullstack · **área:** Administracao
- **objetivo:** transmissão real ao PNCP (hoje só grava nº + evento).
- **escopo:** **[OFICIAL]** cliente PNCP REST/JSON UTF-8 + JWT (expira 1h); **bloquear execução financeira sem nº de controle PNCP** (art. 94); PCA divulgado no PNCP; Polly + ACL + Outbox.
- **dependências:** W2.1, W2.2.
- **marco:** M9 · **prioridade:** 🟠 · `[OFICIAL]` (Lei 14.133, Dec. 12.807/2025)
- **critério-de-pronto:** contrato sem nº PNCP não empenha; transmissão confirmada.

### W9.2 — Almoxarifado dedicado + Monitor DF-e 🟢
- **tipo:** fullstack · **área:** Patrimonio/Administracao
- **objetivo:** fluxo de estoque dedicado e captura de NF-e de fornecedores.
- **escopo:** módulo/fluxo de almoxarifado (requisições, PEPS/Médio já existem); **[OFICIAL]** Monitor DF-e (captura NF-e por chave de acesso).
- **dependências:** W2.2.
- **marco:** M9 · **prioridade:** 🟢 · `[OFICIAL]` (DF-e)
- **critério-de-pronto:** requisição de estoque baixa saldo; NF-e capturada e conciliada.

### W9.3 — Frotas completa + Obras/ObrasPro + SICOE 🟢
- **tipo:** fullstack · **área:** Patrimonio/Financas
- **objetivo:** paridade Betha em frota e obras.
- **escopo:** ciclo de frota (abastecimento, manutenção, multas, CNH); Obras/ObrasPro (medições, fiscalização, cronograma físico-financeiro); **[OFICIAL]** remessa SICOE (obras) ao TCE-RS.
- **dependências:** W4.2.
- **marco:** M9 · **prioridade:** 🟢 · `[OFICIAL]` (SICOE)
- **critério-de-pronto:** medição de obra gera remessa SICOE; ciclo de frota completo.

### W9.4 — Protocolo: carimbo de tempo ACT ICP + GED/CONARQ + assinatura Lei 14.063 🟡
- **tipo:** fullstack · **área:** Protocolo
- **objetivo:** fechar lacunas de compliance documental.
- **escopo:** **[OFICIAL]** carimbo de tempo ACT ICP-Brasil (substitui relógio local); assinatura Lei 14.063/2020 (3 níveis); GED/SIGAD e-ARQ + TTDD CONARQ + PDF/A; corrigir race condition no sequencial NUP.
- **dependências:** W2.1.
- **marco:** M9 · **prioridade:** 🟡 · `[OFICIAL]` (ICP-Brasil, e-ARQ, CONARQ)
- **critério-de-pronto:** documento com carimbo ACT real; NUP sem race; temporalidade aplicada.

### W9.5 — Legislativo: LexML + prestação art. 29-A 🟢
- **tipo:** fullstack · **área:** Legislativo
- **objetivo:** transparência e prestação de contas da Câmara.
- **escopo:** **[OFICIAL]** export LexML/PDF-A + dados abertos; prestação art. 29-A (duodécimo) ao TCE-RS (tenant próprio).
- **dependências:** W4.2.
- **marco:** M9 · **prioridade:** 🟢 · `[OFICIAL]` (LexML, art. 29-A)
- **critério-de-pronto:** proposições exportadas em LexML; contas da Câmara prestadas.

### W9.6 — Convênios + Terceiro Setor + prestação de contas Transferegov.br 🟠
- **tipo:** fullstack · **área:** Financas
- **objetivo:** paridade Betha (gestão de convênios e repasses ao terceiro setor) **+ prestação de contas de convênios/repasses federais no Transferegov.br** (antigo SICONV). Cf. AUDITORIA **H10**: convênio sem PC aprovada → inadimplência (LRF) → **sem novos repasses**. O W9.6 não pode ficar só "paridade Betha" desligado do destino federal.
- **escopo:** agregados de convênio (plano de trabalho, prestação de contas, repasse) e terceiro setor (MROSC); **[OFICIAL]** prestação de contas no **Transferegov.br** — PC final + parciais **por convênio**, vinculadas à execução orçamentária; acompanhamento de prazos e situação de adimplência. `[a confirmar]` layout/integração Transferegov vigente.
- **dependências:** W2.4.
- **marco:** M9 · **prioridade:** 🟠 · `[OFICIAL]` (MROSC, Transferegov.br)
- **status:** ⏳ PENDENTE
- **critério-de-pronto:** convênio com PC (final + parciais) vinculada à execução e prestada/reconciliada no Transferegov.br.

### W9.9 — SADIPEM / CDP (Cadastro da Dívida Pública → STN) 🔴
- **tipo:** fullstack · **área:** Financas
- **objetivo:** atualização **anual obrigatória** de **toda a dívida do ente** no SADIPEM/CDP. Cf. AUDITORIA **K5**. ≠ Dívida Ativa do M6 (que é a *receber*); aqui é o **passivo** do ente. Não atualizar → **CAUC negativo → bloqueia transferências voluntárias e operações de crédito** (>4.000 municípios já bloqueados por isso).
- **escopo:** **[OFICIAL]** CDP alimentado pelo **passivo PCASP** (saldos de dívida da contabilidade); geração do demonstrativo da dívida; **destino STN** (SADIPEM); vincular ao painel de pendências fiscais/CAUC. `[a confirmar]` layout/calendário SADIPEM vigente.
- **dependências:** W2.4 (passivo PCASP), W3.3 (MSC).
- **marco:** M9 · **prioridade:** 🔴 · `[OFICIAL]` (SADIPEM/CDP — STN)
- **status:** ⏳ PENDENTE (lacuna AUDITORIA — anual até ~30/jan; risco CAUC)
- **critério-de-pronto:** CDP gerado do passivo PCASP e prestado ao SADIPEM; pendência reflete no painel CAUC.

### W9.10 — Conformidade SIAFIC (Dec. 10.540/2020) — autodeclaração e requisitos do próprio sistema 🟡
- **tipo:** fullstack · **área:** Financas/Transparencia (transversal)
- **objetivo:** **nomear e comprovar** que o nosso ERP **É o SIAFIC** do ente. Cf. AUDITORIA **M-1**. Há autodeclaração (XML nº 1 nas contas anuais) e requisitos técnicos do Dec. 10.540/2020 que o **próprio sistema deve cumprir e comprovar**.
- **escopo:** **[OFICIAL]** requisitos SIAFIC — **base de dados única** (a contabilidade PCASP como fonte), **trilha de auditoria** (já temos AuditTrail/WORM), **vedação a 2º SIAFIC** (registro contábil único por ente), padronização e transparência; gerar a **autodeclaração SIAFIC** (XML nº 1) das contas anuais; checklist de conformidade auto-comprovável. `[a confirmar]` formato exato da autodeclaração no exercício.
- **dependências:** W2.4, W3.3, W9.7 (AuditTrail WORM).
- **marco:** M9 · **prioridade:** 🟡 · `[OFICIAL]` (Dec. 10.540/2020)
- **status:** ⏳ PENDENTE
- **critério-de-pronto:** autodeclaração SIAFIC gerada; requisitos do decreto comprovados pelo próprio sistema (base única, trilha, sem 2º SIAFIC).

### W9.7 — Robustez de plataforma (Outbox/behaviors/AuditTrail WORM/OTel + provisionamento banco-por-tenant) 🟡
- **tipo:** backend · **área:** BuildingBlocks/Infra (transversal)
- **objetivo:** fechar dívidas de plataforma do diagnóstico (Eixo 6) e o **custo operacional do banco DEDICADO por tenant** (ADR-0005), não "schema isolado". Cf. AUDITORIA **H6**: o modelo é **database-per-tenant**; o problema real é provisionamento + fan-out de migrations **por banco**, não isolamento de schema (que persiste só como separação por módulo *dentro* do banco do tenant).
- **escopo:** Outbox com `AttemptCount`+`NextAttemptUtc` (backoff) + **dead-letter** + idempotência no consumidor (AUDITORIA H1); despacho **paralelo** (concorrência limitada) pulando tenants sem pendências (H4); behaviors `Transaction/UnitOfWork` e `Idempotency` (CLAUDE.md §10); `ModuleUnitOfWork` que **falha-alto** no 2º `Definir` divergente + teste de arquitetura multi-contexto (H5/ADR-0010); AuditTrail imutável real (WORM/hash-chain); exportador OTel + enrichers `TenantId`/`CorrelationId`; `/health` com checks SQL/KV; **convergir dev/prod via `SchemaProvisioner` + migrations por módulo *por banco* (banco dedicado por tenant — ADR-0005), `MigrateAsync` em lote com observabilidade por tenant**; runbook de provisionamento/rotação de connection string + invalidação do `TenantConnectionCache` (K6).
- **dependências:** W0.2, W2.1.
- **marco:** M9 · **prioridade:** 🟡
- **status:** ⏳ PENDENTE
- **critério-de-pronto:** evento "veneno" vai para dead-letter (não reprocessa eterno); migrations aplicadas por **banco dedicado** com paridade dev/prod; rotação de segredo invalida o cache de conexão; trilha provada imutável; telemetria exportada.

### W9.8 — QA state-of-the-art: E2E HTTP + acessibilidade WCAG AA 🟡
- **tipo:** fullstack · **área:** tests (transversal)
- **objetivo:** rede de proteção que o diagnóstico aponta como ausente.
- **escopo:** testes E2E HTTP sobre `WebApplicationFactory` (incl. isolamento cross-tenant em todos os módulos); cobertura medida; testes de acessibilidade (axe-core/WCAG 2.1 AA/eMAG) em todas as rotas; gating `<Can>` consistente; quebrar modais >600 linhas.
- **dependências:** todos os marcos anteriores (incremental, mas consolidado aqui).
- **marco:** M9 · **prioridade:** 🟡
- **critério-de-pronto:** E2E cobre os 11 módulos; 0 violações axe-core críticas; cobertura medida no CI.

---

## Notas de sequenciamento

1. **M0 destrava** — nada roda antes dele (build/CI/testes/segurança de borda).
2. **M1 vem antes do núcleo fiscal** porque escopo por UO/UG e SoD são **exigências do TCE** e base de todo enforcement; adiá-lo geraria retrabalho em Finanças/RH/Tributos.
3. **M2→M3→M4 são a espinha dorsal da preocupação do dono** (Contabilidade → MSC → TCE-RS/SICONFI) e devem ser entregues na ordem, pois cada um é insumo do seguinte (PCASP→balancete→MSC→remessas).
4. **A1/Key Vault (W2.1)** é pré-requisito compartilhado de TCE, SICONFI, eSocial, RNDS, PNCP e Protocolo — por isso entra cedo (M2) e é dependência transversal.
5. **M5–M7** aprofundam módulos com risco fiscal/repasse, cada workflow **backend+frontend end-to-end**.
6. **M8** entrega os diferenciais de produto Betha (cidadão, Portal do Gestor, BI).
7. **M9** fecha suprimentos avançados, compliance documental e a robustez de plataforma/QA.
8. **Todo item `[OFICIAL]`** exige validar o leiaute/versão do exercício na fonte oficial ANTES de codar (CLAUDE.md §16); o que não estiver confirmado fica `// TODO(validar-leiaute-oficial)`.

**Arquivo:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/planejamento/PLANO-MESTRE.md`

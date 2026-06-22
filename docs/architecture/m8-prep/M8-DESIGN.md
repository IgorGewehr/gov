# M8 — Cidadão + Gestor/BI + Transparência LAI · DESIGN pronto-para-implementar

> **Arquiteto do M8.** Design técnico da camada **cidadão-facing** e **gestor/BI** do Tensorroot.Gov, consumindo os módulos já implementados (Tributos, Financas, Protocolo, Transparencia-fiscal, RH, Saúde/Educação/Assistência). Três frentes: **Portal do Cidadão** ("Minha Cidade") · **Portal do Gestor + BI** · **Transparência LAI completa** (ativa + dados abertos + e-SIC + Ouvidoria).
>
> **Regras inegociáveis aplicadas:** Clean Arch + DDD (CLAUDE.md §2); `IMustHaveTenant` em toda raiz (§5); auditoria imutável (§4/§6); domínio rico, VO `record`/`sealed`, factory privada (§7); MediatR CQRS + Outbox (§10); cross-module **só via `*.Contracts`** (§2); ativação modular por tenant via `IModule` (§4); **nada hardcoded — prazos/limites legais parametrizáveis por tenant+vigência** (§7/§16); front-end **gov.br DS + eMAG + WCAG 2.1 AA** sem exceção (§13); LGPD gate antes de publicar (§6).
>
> Legenda de confiança: **ALTA** = base legal confirmada nas verificações `verificacao-*.md`; **MÉDIA** = padrão correto, detalhe numérico/checklist depende de doc oficial; **BAIXA** = bloqueado por contrato/adesão/checklist não obtido (não implementar fiel sem o doc).

---

## 0. Descoberta arquitetural crítica (ler antes de tudo)

O módulo **`Transparencia` que JÁ existe é fiscal-remessa** (agregados `RemessaTce`, `DeclaracaoFiscal`, `MatrizSaldos`; integrações SIAPC/PAD, SICONFI/MSC, eValidador) — ele NÃO faz transparência LAI cidadã. **CONFIANÇA: ALTA** (inspeção do código: `src/Modules/Transparencia/...Domain/{RemessasTce,DeclaracoesFiscais}`).

Decisão: o M8 **estende** `Transparencia` com um novo Bounded Context interno de **publicidade LAI** (sub-aggregates `PublicacaoTransparencia`, `PedidoEsic`) e cria **dois novos módulos** ativáveis por tenant:
- **`Cidadao`** (Portal do Cidadão: Carta de Serviços, Ouvidoria Lei 13.460, identidade gov.br). **CONFIANÇA: MÉDIA** (decisão de produto; bounded context próprio porque tem ciclo de vida e linguagem ubíqua distintos do fiscal).
- **`PainelGestor`** (BI/indicadores/alertas) — pode nascer como **read-only module** (só queries + read models materializados), sem agregados de escrita próprios. **CONFIANÇA: MÉDIA.**

Regra de ouro de reuso (§2): nenhum dos novos módulos lê entidade interna de outro — **só consome Integration Events via Outbox** dos módulos-fonte, materializando read models locais. Eventos já disponíveis hoje: `Tributos.ReceitaArrecadadaIntegrationEvent`; `Financas.{DespesaEmpenhada,PagamentoEfetuado,MSCGerada}IntegrationEvent`; `Protocolo.{ProcessoAutuado,ProcessoTramitado,ProcessoArquivado,DocumentoAssinado}IntegrationEvent`; `Transparencia.{RemessaEnviadaTce,MscEnviadaSiconfi,...}IntegrationEvent`. **Eventos a CRIAR nos módulos-fonte** (pré-requisito): despesa por `liquidação`, `contrato/licitação` (Administracao), `servidor/folha` publicável (RH), `meta de arrecadação` — ver §7. **CONFIANÇA: ALTA** (a lista de eventos existentes foi verificada nos `*.Contracts`).

---

# FRENTE A — Portal do Cidadão ("Minha Cidade")

## A.1 Identidade do cidadão — RP gov.br (OIDC) com gating por selo

- Novo subsistema de **identidade externa** (NÃO confundir com o JWT/RBAC de servidores do §6 do CLAUDE.md). O cidadão autentica via **gov.br como Identity Provider** (OpenID Connect Core 1.0 sobre OAuth 2.0, fluxo *authorization code*); o ApiHost é **Relying Party (RP)**. Lê o **selo (bronze/prata/ouro)** do `id_token` (JWT) e aplica **autorização por nível** por serviço (gating). **CONFIANÇA: ALTA** (OIDC + 3 selos confirmados — `verificacao-cidadao.md` 3.0–3.3).
- **Gating recomendado** (parametrizável por tenant, NÃO hardcoded): consulta pública / 2ª via por inscrição → **bronze ou anônimo**; protocolo de processo, ouvidoria identificada, dados pessoais, assinatura → **prata**; ato com efeito jurídico forte → **ouro**. **CONFIANÇA: MÉDIA** (é decisão de produto, não norma — `verificacao-cidadao.md` 3.6; tabela de nível-por-serviço vira `IOptions`/parâmetro de tenant).
- **ACL + Outbox + Polly** na chamada ao gov.br (§8/§11); `id_token` validado contra JWKS do gov.br; mapear `sub`/CPF→cidadão local sem duplicar dado sensível.
- **Fallback login local por CPF:** só como **exceção auditada**, nunca primário (§6). **CONFIANÇA: MÉDIA** (`[a confirmar política do ente]`).
- **RISCO BLOQUEANTE (cronograma):** ser RP gov.br exige **adesão formal do ente à Rede Nacional de Governo Digital** + pedido por produto + ciclo homologação→produção, e o agente solicitante precisa de conta prata/ouro. Se Maximiliano de Almeida/RS não aderiu, o portal não autentica via gov.br no go-live. **CONFIANÇA: ALTA** (`verificacao-cidadao.md` risco 1).
- **Docs a obter:** roteiro técnico acesso.gov.br (iniciar integração); catálogo API Login Único (Conecta); **status de adesão do município-piloto** (pendência §6.2 da pesquisa) — obter AGORA. **CONFIANÇA do contrato técnico: ALTA**; **do status de adesão: BAIXA (pendente)**.

## A.2 Carta de Serviços — catálogo estruturado (NÃO página estática)

- Agregado novo **`ServicoPublico`** (módulo `Cidadao`, schema `cidadao`, `IMustHaveTenant`): VOs `Requisitos`, `DocumentosNecessarios`, `Prazo`, `FormaPrestacao`, `LocaisAcesso`, `TempoEspera`, `CanalReclamacao`, `CondicoesAcessibilidade`, `PrioridadeAtendimento`, `NivelGovbrMinimo`. **CONFIANÇA: ALTA** (conteúdo obrigatório confirmado — `verificacao-cidadao.md` 2.1; base: Lei 13.460 art. 7º nacional + Decreto 9.094 como modelo).
- Cada `ServicoPublico` **liga-se a uma ação no portal** (2ª via tributo → Tributos; abrir processo → Protocolo; manifestar → Ouvidoria) e **carrega a pesquisa de satisfação acoplada** (§A.4). Modelar como dados, não HTML, para alimentar portal + avaliação + agendamento.
- **Não reexigir documento já em poder do órgão** (Lei 13.460 art. 5º): ao montar um serviço, consultar via Contracts se o doc já existe (cadastro tributário/protocolo). **CONFIANÇA: MÉDIA** (princípio confirmado; implementação depende dos read models).
- **Lei municipal a parametrizar:** o rol de serviços, prazos e regulamento local da Lei 13.460 / adoção do Decreto 9.094 (`[a confirmar]`). **Docs a obter:** decreto municipal regulamentador (pendência §6.1).

## A.3 Serviços online (consome módulos existentes — reuso puro)

| Serviço no portal | Módulo-fonte | Como reusa (Contracts/read model) | Confiança |
|---|---|---|---|
| 2ª via DAM (IPTU/ISS/ITBI/taxas), consulta de débitos | **Tributos** | read model alimentado por `ReceitaArrecadadaIntegrationEvent` + query de débitos por inscrição/CPF | ALTA |
| Consulta dívida ativa / parcelamento | **Tributos** (DividaAtiva/CDA já existem) | query read-only; ação de adesão a parcelamento via Command no Tributos | ALTA |
| Abrir/acompanhar processo administrativo | **Protocolo** | `ProcessoAutuado/Tramitado/Arquivado` → read model "meus processos"; abertura via Command no Protocolo (gating prata) | ALTA |
| Assinar documento (cidadão) | **Protocolo** (Lei 14.063, A1 em Key Vault) | reusa assinatura existente; gating **prata/ouro** | MÉDIA |
| Agendamento de atendimento | **novo** (`Cidadao`) | `[a confirmar se já existe módulo Agendamento]` — senão criar entidade `Agendamento` | BAIXA |

> Princípio: o Portal do Cidadão é **fachada de leitura + disparo de Commands** dos módulos existentes. Nada de lógica de tributo/protocolo duplicada aqui.

## A.4 Ouvidoria (Lei 13.460) — motor de SLA PRÓPRIO (≠ e-SIC)

- **Achado material:** Ouvidoria e e-SIC são **dois fluxos distintos com prazos diferentes** — não confundir (`verificacao-transparencia-lai.md` risco 2). **CONFIANÇA: ALTA.**
- Agregado **`Manifestacao`** (módulo `Cidadao`, `IMustHaveTenant`): tipos **reclamação, denúncia, sugestão, elogio, solicitação de providência** (art. 10); identificada **ou anônima** (sigilo de identidade quando solicitado). **CONFIANÇA: ALTA** (`verificacao-cidadao.md` 1.3).
- **Motor de SLA:** prazo conclusivo **30 dias + prorrogação 1× de +30** (teto 60), justificativa expressa (art. 16). Prazo interno da ouvidoria a agente público **20 + 20**. **Parametrizável por tenant** (nunca hardcoded — §7). **CONFIANÇA: ALTA** (`verificacao-cidadao.md` 1.4).
- Cada manifestação → **protocolo + rastreio público** por número + contador de prazo. Eventos `ManifestacaoRegistrada`/`ManifestacaoRespondida`/`PrazoManifestacaoVencendo` (Outbox) alimentam o Painel do Gestor.
- **Docs a obter:** texto literal arts. 10, 16, 18–21, 23 da Lei 13.460 direto no Planalto (fetch caiu — pendência §6.6). **CONFIANÇA: ALTA** no número 30+30; literal `[a confirmar]`.

## A.5 Avaliação / pesquisa de satisfação (art. 23) — OBRIGATÓRIO, não opcional de BI

- **RISCO:** entregar portal sem o motor de avaliação = descumprimento legal no dia 1 (`verificacao-cidadao.md` risco 3). **CONFIANÇA: ALTA.**
- Entidade **`AvaliacaoServico`** acoplada a cada `ServicoPublico`: 5 aspectos do art. 23 (satisfação, qualidade do atendimento, cumprimento de prazos, quantidade de manifestações, melhorias). **Pesquisa ≥ anual** + **publicação integral** dos resultados (com ranking de reclamações) na Transparência ativa — automatizada. **CONFIANÇA: ALTA** (`verificacao-cidadao.md` 1.5).
- Reusa o pipeline de publicação LAI (§C.1) para publicar o resultado.

## A.6 Acessibilidade (transversal a A e ao app móvel)

- Front nasce **eMAG 3.1 + WCAG 2.1 AA** (já há `@govbr-ds/core`, `eslint-plugin-jsx-a11y`, `axe-core` no `src/Web`). **VLibras + símbolo de acessibilidade em destaque** (LBI art. 63 §1º). **CONFIANÇA: ALTA** (`verificacao-cidadao.md` 4.2: vínculo legal municipal é LBI art.63→WCAG, eMAG é padrão federal recomendado).
- **Acessibilidade é QA bloqueante**, inclui o app móvel "Minha Cidade" (frequentemente esquecido — `verificacao-cidadao.md` risco 2). **CONFIANÇA: ALTA.**
- `[a confirmar]` AA vs AAA em itens específicos (pendência §6.4).

---

# FRENTE B — Portal do Gestor + BI

## B.1 Arquitetura: módulo read-only sobre read models materializados

- `PainelGestor` consome **Integration Events** dos módulos-fonte (Outbox) e materializa **data marts por área** (orçamento, arrecadação, pessoal, saúde, educação) — não consulta entidades de outro módulo (§2). **CONFIANÇA: ALTA** (princípio); `[a confirmar tecnologia de BI do stack — componente próprio vs embedding]` (pendência gestor §130).
- **Motor de regras de limites** parametrizável (§7 — nunca hardcoded) → status semáforo verde/amarelo/vermelho + alertas.
- **Motor de calendário fiscal** → contagem regressiva + notificações via Outbox.

## B.2 Indicadores de gestão (catálogo, base legal de cálculo)

| Indicador | Limite | Fonte de dados (Contracts) | Confiança |
|---|---|---|---|
| **Educação MDE** | **25%** receita impostos+transf. (CF art. 212) | Financas (receita/despesa por função) | ALTA |
| **FUNDEB remuneração** | **60%** em remuneração (Novo FUNDEB) | Financas + RH | ALTA |
| **Saúde ASPS** | **15%** (municípios) — CF art. 198 §2º / LC 141/2012 | Financas | MÉDIA `[confirmar 15% em LC 141 art. 7º]` |
| **Despesa com pessoal (LRF)** | legal **54%** RCL · prudencial **51,3%** (95%) · alerta **48,6%** (90%) | RH + Financas | ALTA (atenção LC 178/2021 mudou apuração) |
| **Execução orçamentária** | % executado vs LOA (empenhado/liquidado/pago) | Financas | ALTA |
| **Arrecadação** | realizado vs meta, por tributo/situação | **Tributos** (`ReceitaArrecadadaIntegrationEvent`) | ALTA |

- **Princípio de produto:** o gestor não calcula — o sistema calcula sobre os dados e mostra semáforo + tendência. Cor = status (não decoração); 5–7 KPIs por tela; drill-down headline→secretaria→conta. **CONFIANÇA: ALTA** (boas práticas BI — pesquisa gestor §4).
- **Reconciliação obrigatória:** indicadores ↔ anexos RREO/RGF ↔ SIOPE/SIOPS (consistência com o que é declarado pelo módulo Transparencia-fiscal). **CONFIANÇA: ALTA** (princípio); **MÉDIA** no escopo SIOPE/SIOPS.
- **Dimensões por secretaria:** adotar/adaptar as 7 do **IEG-M** (i-Plan, i-Fiscal, i-Educ, i-Saúde, i-Amb, i-Cidade, i-GovTI) como benchmark. **CONFIANÇA: MÉDIA** (modelo de referência, não obrigação).
- **Parâmetros (lei/norma a versionar por tenant):** 25%/60%/15%/54%/51,3%/48,6% — todos em `ParametroIndicador` versionado por vigência. **Docs a obter:** LC 141/2012 art. 7º; LC 101/2000 arts. 51/73 (penalidades); manual IEG-M; regras de cômputo LC 178/2021.

## B.3 Calendário fiscal embutido (alertas de prazo)

| Relatório | Prazo | Exceção < 50 mil hab. | Confiança |
|---|---|---|---|
| **RREO** | até **30 dias** após fim do bimestre | semestral (parcial) | ALTA |
| **RGF** | até **30 dias** após fim do quadrimestre | semestral | ALTA |
| DCA / MSC (Siconfi), prestação anual TCE-RS, SIOPE/SIOPS | — | — | MÉDIA `[datas exatas a confirmar]` |

- Reusa o módulo **Transparencia-fiscal** (já tem `ICalendarioFiscal`, `Bimestre`, `Quadrimestre`, eventos `PrazoRemessaVencido`) — o Painel **assina** esses eventos e exibe contagem regressiva. **CONFIANÇA: ALTA** (verificado: `Transparencia.Application/Abstractions/ICalendarioFiscal.cs` e VOs existem).
- **Penalidades LRF** (LC 101 arts. 51 §2º / 73: suspensão de transferências voluntárias, vedação de op. crédito) → exibir como consequência do alerta. **CONFIANÇA: MÉDIA** `[confirmar texto Planalto]`.

## B.4 Painel de conformidade EBT (diferencial de produto)

- Autoavaliação dos quesitos EBT 360 da CGU (ativa 50% / passiva 50%) — alimentado por C.1/C.4. **CONFIANÇA: ALTA** (EBT 50/50 confirmado — `verificacao-transparencia-lai.md` 5.1).

---

# FRENTE C — Transparência LAI completa

## C.1 Transparência ATIVA — publicação proativa

- Sub-aggregate **`PublicacaoTransparencia`** (estende módulo `Transparencia`, schema `transparencia`): rol mínimo LAI art. 8º §1º (estrutura org., repasses/transferências, despesas, licitações/contratos, programas/obras, FAQ) + rol prático EBT (receitas, servidores, diárias, obras, convênios). **CONFIANÇA: ALTA** (`verificacao-transparencia-lai.md` 1.3, 5.2). Já existe `IPublicacaoTransparenciaRepository` no módulo — **estender**, não criar do zero. **CONFIANÇA: ALTA** (verificado).
- **Fonte de dados (eventos Contracts):** despesa/receita ← Financas (`DespesaEmpenhada`, `PagamentoEfetuado`); arrecadação ← Tributos; licitações/contratos ← **Administracao** (`[evento a CRIAR]`); servidores/diárias ← **RH** (`[evento a CRIAR, com gate LGPD]`). **CONFIANÇA: ALTA** (princípio "consumir, não duplicar" — pesquisa §6).
- **Transparência fiscal em tempo real (LC 131 art. 48-A + SIAFIC):** despesa com nº processo, bem/serviço, **credor**, dados da licitação; receita (lançamento+recebimento). Cadência **D+1 útil** ao registro contábil. **CONFIANÇA: ALTA** (`verificacao-transparencia-lai.md` 2.1–2.3).
- **Feature flag por porte:** municípios ≤ 10.000 hab. dispensados da divulgação na internet do rol art. 8º (§4º) — **MAS tempo-real fiscal (art. 73-B LRF) sempre ON**. **CONFIANÇA: ALTA** (`verificacao-transparencia-lai.md` 1.5; correção: §4º remete a art. 73-B da LC 101, não genericamente à LC 131).
- **Docs a obter:** Decreto 10.540/2020 (SIAFIC) **vigente** — alterado pelo Decreto 11.644/2023, manter watch de versão (§16); checklist específico **TCE-RS** (projeto é RS — cada TCE tem índice próprio, `[a confirmar]`). **CONFIANÇA do SIAFIC base: ALTA; campos mínimos: MÉDIA (drift normativo).**

## C.2 Dados abertos (LAI art. 8º §3º)

- Toda publicação expõe: **download CSV/ODS** (formato aberto não proprietário) + **API REST/JSON** (acesso automatizado por sistemas externos) + **pesquisa de conteúdo** + carimbo de **última atualização** + autenticidade/integridade. **CONFIANÇA: ALTA** (`verificacao-transparencia-lai.md` 3.1).
- API de dados abertos é endpoint **público anônimo** (sem login) — não pode ter barreira. **CONFIANÇA: ALTA.**

## C.3 e-SIC — Transparência PASSIVA (motor de SLA da LAI, ≠ Ouvidoria)

- Sub-aggregate **`PedidoEsic`** (módulo `Transparencia`, `IMustHaveTenant`). **Motor de SLA LAI** (distinto do 30+30 da Ouvidoria):

| Etapa | Prazo | Confiança |
|---|---|---|
| Resposta padrão | **20 dias** | ALTA |
| Prorrogação | **+10 dias** com justificativa expressa, ciente o requerente | ALTA |
| Recurso à autoridade superior | **10 dias** p/ interpor · **5 dias** p/ decidir (arts. 15–16) | ALTA |

  Todos **parametrizáveis por tenant** (§7). **CONFIANÇA: ALTA** (`verificacao-transparencia-lai.md` 4.1–4.3 — elevou de `[a confirmar]` a CONFIRMADO).
- **Vedado exigir motivos** do pedido (art. 10 §3º); só identificação básica; rastreio por protocolo; e-SIC eletrônico **+ SIC físico**. **CONFIANÇA: ALTA** (4.4–4.6).
- **RISCO CRÍTICO — login gov.br NÃO pode ser barreira no e-SIC.** Orientação CGU: o SIC não pode condicionar o direito de acesso ao login gov.br. O caminho mínimo de protocolo aceita **identificação básica sem cadastro complexo**; login gov.br é **opcional/auxiliar** (rastreio, autopreenchimento). Exigi-lo configura "ponto que dificulta o pedido" que zera EBT 360. **Decisão de arquitetura a registrar no decision journal antes de codar.** **CONFIANÇA: ALTA** (`verificacao-transparencia-lai.md` risco 1).

## C.4 LGPD gate (transversal — antes de QUALQUER publicação)

- Pipeline de **anonimização/mascaramento** antes de publicar (C.1/C.2): folha publica **nome, cargo, lotação, remuneração bruta** — **nunca** CPF/conta/endereço; beneficiários de Saúde/Educação/Assistência **não nominais**. **CONFIANÇA: ALTA** (`verificacao-transparencia-lai.md` 6.1–6.2; STF RE 652777). Regras de anonimização **por dataset** `[a confirmar/detalhar]`.

---

## D. Ordem de implementação (sequenciada por dependência e risco)

0. **Pré-flight (bloqueadores, fazer AGORA, em paralelo ao dev):** (a) confirmar status de adesão do município ao Login Único gov.br (A.1); (b) reler textos primários no Planalto — Lei 13.460 arts. 10/16/23, LAI arts. 8º/11/15–17/23/31, Decreto 10.540 vigente (§16); (c) obter checklist TCE-RS; (d) registrar no decision journal a decisão "e-SIC sem login obrigatório".
1. **Fundação de parâmetros legais versionados** (prazos LAI 20+10, Ouvidoria 30+30, limites 25/60/15/54%) — nada hardcoded. **+ Eventos de Integração faltantes** nos módulos-fonte (Administracao: contratos/licitações; RH: servidores publicáveis).
2. **Transparência ATIVA + Dados abertos (C.1/C.2) + LGPD gate (C.4)** — estende o módulo `Transparencia` existente; maior valor de conformidade, menor dependência de identidade. Destrava o Painel EBT (B.4).
3. **e-SIC (C.3)** — motor SLA LAI + rastreio público (sem login obrigatório).
4. **Painel do Gestor + BI (B.1–B.4)** — read models sobre os eventos; calendário fiscal reusa Transparencia-fiscal; indicadores + alertas.
5. **Portal do Cidadão — Carta de Serviços (A.2) + Ouvidoria (A.4) + Avaliação (A.5)** — motor SLA 30+30; obrigatório por Lei 13.460.
6. **Identidade gov.br RP (A.1) + gating + serviços online (A.3)** — por último porque é o bloqueador externo; até lá, serviços públicos/consultas anônimas e e-SIC já funcionam sem ele.
7. **App móvel "Minha Cidade"** + hardening de acessibilidade (A.6) como QA bloqueante de todo o front.

> Racional da ordem: entrega valor de **conformidade legal** cedo (transparência ativa + e-SIC) sem depender da adesão gov.br (risco externo); o que é **bloqueante de cronograma** (RP gov.br) fica isolado no fim; tudo o que é cidadão-facing herda o gate de acessibilidade.

---

## E. Pendências consolidadas (`[a confirmar]` — §16)

1. Status de adesão do município ao Login Único gov.br (**bloqueante**) — A.1.
2. Texto literal Planalto: Lei 13.460 (10/16/23), LAI (8º/11/15–17/23/31), Decreto 10.540 vigente + 11.644/2023.
3. Checklist/índice de transparência **do TCE-RS** (projeto é RS).
4. Decreto municipal regulamentador da Lei 13.460 e da LAI; adoção do Decreto 9.094 como modelo de Carta.
5. Existência de módulo Agendamento; nível gov.br mínimo por serviço (política do ente); fallback CPF.
6. WCAG AA vs AAA por item; inclusão VLibras.
7. LC 141/2012 art. 7º (15% saúde); LC 101 arts. 51/73 (penalidades); LC 178/2021 (cômputo pessoal); escopo SIOPE/SIOPS; tecnologia de BI do stack; datas Siconfi/TCE-RS.
8. Eventos de Integração a criar (Administracao contratos/licitações; RH servidores) — pré-requisito de C.1.

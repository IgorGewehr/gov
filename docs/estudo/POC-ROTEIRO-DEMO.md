# Roteiro de Demonstração — Prova de Conceito (Tensorroot.Gov)

> **Objetivo:** roteiro **passo-a-passo** para uma PoC / prova de aderência pensada para **impressionar
> uma comissão de recebimento** (banca técnica nomeada, Lei 14.133/2021 art. 17 §3º / 41 II). Cobre as
> **duas trilhas que já funcionam fim-a-fim no código**: **Legislativa (Câmara)** e **Municipal (Executivo)**.
> **Régua:** incorpora os critérios de "o que reprova / o que anula" e a prontidão honesta da PoC
> (consolidados aqui); o estado real do projeto vive em `docs/progresso/progresso.json`.
> **Regra de ouro da PoC (3 editais RS/SC/MG):** tudo demonstrado **tem de existir na build** (nativo/
> parametrizável). Vedado "codar/ajustar durante a apresentação" (Riqueza §14.6.9; SEFAZ-MS §11.5.2.5; Mata §7.16).
> **Honestidade:** este roteiro só lista o que está **provado em código** (endpoints + handlers + seed +
> ~700 testes verdes). O que ainda não roda fim-a-fim está em §4 "O que NÃO mostrar ainda".
> Data: 2026-06-22.

---

## 0. Preparação do ambiente (antes da banca entrar)

> A API roda em `http://localhost:5080` (NÃO subir aqui — a porta é usada por outro processo; este
> roteiro é o guião, a execução é do operador). Frontend React (gov.br DS) em `http://localhost:5173`
> com proxy `/api → 5080`. Provider DEV = SQLite, **database-per-tenant** (1 arquivo `.db` por ente).
> Detalhe operacional completo em `docs/RUNBOOK.md`.

### 0.1 Dois tenants distintos (o isolamento multi-tenant é item técnico "atende/não atende")
- **Executivo** — provisionado **automaticamente** no startup em Development: *Prefeitura de Maximiliano
  de Almeida/RS*, CNPJ `11.222.333/0001-81`, **Poder Executivo**, **todos os módulos licenciados** +
  admin semeado (`Program.cs`, bloco "Bootstrap de DEV").
- **Legislativo** — **CNPJ distinto** (Câmara é tenant separado, CLAUDE.md §4). Provisionar uma vez
  (ação de **operador de plataforma**, exige `plataforma.tenants.provisionar` — que o admin de tenant
  **não** tem, por design):
  ```
  POST /admin/tenants
  { "cnpj":"00.000.000/0001-00", "nome":"Câmara Municipal de Exemplo/RS",
    "poder":"Legislativo", "connectionString":null,
    "modulos":["Identidade","Financas","Legislativo","Transparencia"] }
  ```
  Crie depois o admin da Câmara (índice central email→tenant garante e-mail único global).

### 0.2 Login (mesma mecânica nos dois tenants) — **diferencial técnico citável**
- `POST /api/identidade/login` (anônimo) com `{ "email": "...", "senha": "..." }` → resposta
  `{ accessToken, expiraEm }`. Admin demo do Executivo: `admin@tensorroot.gov` / `Mudar@123`.
- **O tenant NÃO vai em header** — está embutido na claim `tenant_id` do JWT (auto-emitido, HS256).
  Basta enviar `Authorization: Bearer <accessToken>`. Para a banca: **um único endpoint de login serve
  os dois Poderes**, sem vazamento de escopo — o índice central resolve o tenant pelo e-mail.
- **Para mostrar isolamento ao vivo:** logue na Câmara, tente `GET /api/tributos/...` → **403 auditado**
  ("Módulo não licenciado para este tenant", gating em `Program.cs`). Prova de ativação modular por tenant.

---

## TRILHA LEGISLATIVA (Câmara) — *a mais pronta; comece por ela*

> **Por que primeiro:** é a maior cobertura fim-a-fim do sistema e a de **maior prontidão** (AUTOAVALIACAO:
> ~70-75%, provável aprovação numa PoC de "processo legislativo"). O núcleo (proposições, tramitação,
> sessão, quórum, votação nominal, **painel ao vivo**, ata, normas, diário, tribuna) está de pé.
> Todos os endpoints abaixo: `src/Modules/Legislativo/.../Infrastructure/LegislativoEndpoints.cs`.

### 1.1 Pré-requisitos / seed (idempotente, 1 clique)
- **Estar logado no tenant Legislativo** (Câmara).
- Rodar o seed de demonstração — **cria todo o cenário de uma vez**:
  ```
  POST /api/legislativo/demonstracao/seed        (perm: legislativo.demo.semear)
  ```
  Cria (handler `SemearDemonstracaoLegislativa`, idempotente por tenant — não duplica se já semeado):
  - **Legislatura 2025-2028 + Mesa Diretora + 9 vereadores NOMEADOS** (catálogo `VereadoresCatalogo`):
    Ana Paula (PSDB, **Presidente**), Bruno Carvalho (MDB, **Vice**), Carla Menezes (PT, **1ª Secretária**),
    Diego Fernandes (PP, **2º Secretário**), Eliane Tavares (PL), Fabio Ramos (PDT), Gabriela Nunes (PSB),
    Henrique Barros (REPUBLICANOS), Isabela Moreira (PSD).
  - **1 proposição em tramitação** — PLO-0001/2026 "denominação de logradouro público", **já com pareceres
    CCJ + Finanças favoráveis** e **incluída na Ordem do Dia** (pronta para deliberar).
  - **1 sessão ordinária ABERTA** com **8 dos 9 presentes** (1 ausente de propósito, para o painel mostrar ausência).
  - **1 votação nominal ABERTA** com votos parciais já lançados (alguns Sim/Não/Abstenção, **deixando membros
    por votar** — é isto que faz o painel "encher ao vivo" na frente da banca).
  - Resposta: `{ Vereadores, ProposicaoId, SessaoId, VotacaoId, JaSemeado }` — anote `SessaoId` e `VotacaoId`.

### 1.2 Roteiro de cliques (sequência numerada)

1. **Mostrar os vereadores** → `GET /api/legislativo/vereadores` — a bancada plural com nome parlamentar,
   partido e cargo na Mesa. (Telão: "esta é a composição da Casa".)
2. **Abrir a sessão / ver presenças e quórum** → `GET /api/legislativo/sessoes/{SessaoId}` e
   `GET /api/legislativo/sessoes/{SessaoId}/presencas`. Mostrar 8 presentes / 1 ausente; quórum de instalação atingido.
   - (Se quiser dramatizar: registrar a presença do faltante ao vivo →
     `POST /api/legislativo/sessoes/{SessaoId}/presencas` `{ "vereadorId": "..." }`, depois
     `POST .../quorum` para reapurar — recomposição de quórum é requisito recorrente, Vitória §3.44.)
3. **Abrir o PAINEL AO VIVO da votação** → `GET /api/legislativo/votacoes/{VotacaoId}/painel`.
   **(este é o momento central — ver §1.3.)**
4. **Registrar votos ao vivo** dos que faltam → para cada membro:
   ```
   POST /api/legislativo/votacoes/{VotacaoId}/votos
   { "votoId":"<novo-guid>", "vereadorId":"...", "sentido": 1 }   // 1=Sim, 2=Não, 3=Abstenção
   ```
   A cada voto, **recarregar o painel** (passo 3) — o placar muda, os ausentes diminuem, a apuração parcial vira.
5. **Encerrar a votação** → `POST /api/legislativo/votacoes/{VotacaoId}/encerramento` → retorna
   `{ resultado: "Aprovado" | "Rejeitado" }` (apuração oficial pela maioria exigida).
6. **Deliberar a proposição com base na votação** →
   `POST /api/legislativo/proposicoes/{ProposicaoId}/aprovacao` `{ "votacaoId": "..." }`
   (a proposição passa a APROVADA, amarrada ao resultado da votação — rastreabilidade).
7. **Gerar a ATA da sessão** → `GET /api/legislativo/sessoes/{SessaoId}/ata` — ata **estruturada e automática**
   (presenças nominais, Ordem do Dia com ementas, votações com placar + resultado + **nomes por sentido**).
   "Ata Sintética automática ao fim da sessão" é requisito **literal** de edital (Vitória §2.1.8).
8. **Normas Jurídicas (busca)** → `GET /api/legislativo/normas?termo=...&tipo=&ano=` — base de leis/normas
   consultável com filtros (termo, tipo, número, ano, situação) + paginação. (Cadastrar uma norma:
   `POST /api/legislativo/normas`; vincular à proposição de origem: `POST .../proposicao-origem`.)
9. **Diário Oficial eletrônico** → `GET /api/legislativo/diario/publico?ano=2026` (consulta cidadã).
   Para o fluxo de produção: abrir edição (`POST /diario/edicoes`) → adicionar matéria
   (`POST /diario/edicoes/{id}/materias`) → **publicar** (`POST .../publicacao`). Diário Oficial é
   requisito recorrente (Matão).
10. **Tribuna / cronômetro** → abrir a tribuna da sessão (`POST /api/legislativo/sessoes/{SessaoId}/tribuna`
    `{ "tempoPadraoSegundos": 300 }`), inscrever orador (`POST .../tribuna/inscricoes`), e **controlar o tempo
    ao vivo**: início → pausa → retomada → encerramento da fala
    (`POST .../tribuna/inscricoes/{id}/inicio|pausa|retomada|encerramento`). Consultar:
    `GET .../tribuna`. Cronômetro de tribuna + inscrição de oradores é recorrente (Vitória, Curitiba).

### 1.3 O "MOMENTO UAU" da trilha — o **PAINEL AO VIVO**
`GET /api/legislativo/votacoes/{VotacaoId}/painel` devolve o registro `PainelVotacao`
(`Application/Votacoes/ObterPainelDaVotacao.cs`) — **exatamente o que um painel eletrônico de plenário
consome**, calculado em tempo real:
- **Placar agregado:** `Sim`, `Nao`, `Abstencao`, `TotalVotos`.
- **Quórum:** `TotalMembros`, `Presentes`, `Ausentes` (= quem ainda não votou), `QuorumMinimo`
  (maioria absoluta), `QuorumAtingido` (bool).
- **Apuração em tempo real:** `ResultadoParcial` (preview projetado a cada voto, espelhando a regra de
  `Votacao.Apurar` — maioria simples/absoluta/qualificada) e `ResultadoApurado` (oficial, após encerrar).
- **Lista NOMINAL com nomes:** `Votos[]` = cada `LinhaPainel(VereadorId, NomeParlamentar, Sentido, RegistradoEm)`
  — o painel mostra **nome do parlamentar e como votou**, ordenado pela hora do voto.
- **Segurança embutida:** em votação **secreta** a lista nominal é omitida (invariante I-12), mas o agregado
  permanece visível — mostrar isto à banca prova maturidade de domínio.

**A cena:** projetar o painel, registrar os votos que faltam um a um, e a comissão vê o placar subir, os
ausentes caírem a zero e a apuração virar de "Rejeitado" para "Aprovado" **com os nomes na tela** — e, em
seguida, a **ata sair pronta** com tudo aquilo consolidado. É o pico da PoC legislativa.

---

## TRILHA MUNICIPAL (Executivo) — *os motores provados*

> **Honestidade (AUTOAVALIACAO):** o **núcleo fiscal** (PCASP → DCASP/MSC → prestação TCE-RS) é o ponto
> forte e está entregue. Os **motores de Folha e Tributos rodam e calculam** (handlers + tabelas legais +
> ~700 testes verdes), embora **não haja seed de demonstração** desses módulos — a massa é montada com os
> próprios comandos paramétricos (o que, em si, prova "nativo/parametrizável por lei municipal"). O que
> **ainda não fecha** (eSocial, validação oficial TCE, PPA/LDO/LOA dedicado, Cidadão) está em §4.
> **Estar logado no tenant Executivo** (Prefeitura). Endpoints: `RecursosHumanosEndpoints.cs`,
> `Iptu/IssItbi/TributosEndpoints.cs`, `FinancasEndpoints.cs`, `TransparenciaEndpoints.cs`.

### 2.A — FOLHA (motor de cálculo da folha de pagamento)
Pré-requisito (1 vez): semear as **tabelas legais federais** →
`POST /api/recursoshumanos/tabelas-legais/semear-federais` (INSS e IRRF oficiais 2025/2026;
`SemearTabelasFederais.cs`). RPPS é **fail-closed**: exige tabela por **lei municipal**
(`POST /tabelas-legais/rpps`) — prova de parametrização por lei.

Cliques:
1. **Cadastrar servidor (vínculo RGPS/RPPS)** → `POST /api/recursoshumanos/servidores`
   `{ cpf, matricula, dadosPessoais{ nome, dataNascimento }, cargoId, regime, dataNomeacao }`
   (`regime`: 1=RPPS, 2=RGPS; invariante: regime casa com o regime do cargo).
2. **Abrir a folha da competência** → `POST /api/recursoshumanos/folhas` `{ ano, mes }` (1 folha/competência/tenant).
3. **Lançar proventos (rubricas)** → `POST /api/recursoshumanos/folhas/{folhaId}/eventos`
   `{ servidorId, rubrica, tipo, baseCalculo, valor }` (`tipo`: 1=Provento, 2=Desconto).
4. **Apurar descontos legais** → `POST /api/recursoshumanos/folhas/{folhaId}/apuracao-legal` — o
   **`MotorDeCalculoFolha`** aplica **INSS progressivo por faixas** (RGPS), **RPPS** (tabela municipal),
   **IRRF** (após previdência + dependentes, regra da maior vantagem).
5. **Calcular a folha (com abate-teto)** → `POST /api/recursoshumanos/folhas/{folhaId}/calculo`
   (aplica teto remuneratório parametrizável; gera rubrica "Abate-Teto" se estourar).
6. **Emitir o CONTRACHEQUE** → `GET /api/recursoshumanos/folhas/{folhaId}/servidores/{servidorId}/contracheque`.
   - **O momento de prova:** `ContrachequeDto` retorna `Linhas[]` (rubrica, tipo, valor), `TotalProventos`,
     `TotalDescontos` e **`LiquidoAPagar`** — com a identidade **`LiquidoAPagar = TotalProventos − TotalDescontos`**
     (calculado em `ObterContrachequeDoServidor.cs`). Mostrar a conta fechando na tela é o "uau" da folha.
7. (Opcional) Fechar e pagar: `POST .../fechamento` → `POST .../pagamento` `{ dataPagamento }`.

### 2.B — PONTO (Portaria MTP 671/2021)
1. **Definir jornada** → `POST /api/recursoshumanos/ponto/jornadas`.
2. **Registrar marcações** → `POST /api/recursoshumanos/ponto/marcacoes`
   `{ servidorId, dataHora, sentido, origem }` (`sentido`: 1=Entrada, 2=Saída) → retorna `{ nsr }`
   (NSR sequencial sem lacunas; marcação **imutável**, sem reescrita/exclusão — exigência da 671).
3. **Apurar a jornada** → `POST /api/recursoshumanos/ponto/apuracoes` `{ servidorId, ano, mes }` → computa
   `MinutosTrabalhados`, `MinutosExtras`, `MinutosFalta`, saldo de banco de horas. Fechar: `POST .../apuracoes/{id}/fechamento`.
4. **Exportar o AFD (Arquivo Fonte de Dados)** → `GET /api/recursoshumanos/ponto/afd?inicio=AAAA-MM-DD&fim=AAAA-MM-DD&assinar=true`
   — arquivo **posicional Portaria 671** (ISO-8859-1), com **assinatura CAdES destacada (.p7s)** opcional via Cofre A1.
   (Também há o **AEJ**, Anexo VI: `GET .../ponto/aej?ano=&mes=&assinar=true`.) Gerar o AFD assinado na frente
   da banca prova conformidade legal do ponto eletrônico.

### 2.C — IPTU (com **memória de cálculo** — o "uau" tributário)
Pré-requisitos (paramétricos por **lei municipal** — mostrar que nada é hardcoded):
- **PGV (Planta Genérica de Valores)** → `POST /api/tributos/pgv` (zonas com VUT/VUC + fatores padrão/
  depreciação/uso, com `fundamentoLegal`).
- **Tabela de alíquotas IPTU** → `POST /api/tributos/iptu/aliquotas` (faixas progressivas por valor venal,
  `edificado` true/false, `fundamentoLegal`).

Cliques:
1. **Cadastrar imóvel (cadastro imobiliário)** → `POST /api/tributos/imoveis`
   `{ proprietarioId, inscricaoMunicipal, zonaFiscal, areaTerreno, areaConstruida, tipoUso, padraoConstrutivo, ... }`.
   (Cadastrar o contribuinte antes: `POST /api/tributos/contribuintes/pessoa-fisica`.)
2. **APURAR o IPTU com memória de cálculo** → `GET /api/tributos/imoveis/{imovelId}/iptu/{exercicio}`
   `?percentualIsencao=&percentualDesconto=` → `ResultadoIptu` com **toda a memória**:
   `ValorVenal`, `ValorTerreno`, `ValorConstrucao`, `AliquotaPercentual`, `ImpostoBruto`, `ValorIsencao`,
   `ValorDesconto`, `ImpostoDevido`. A `MemoriaValorVenal` detalha cada fator (AreaTerreno × VUT) +
   (AreaConstruida × VUC × FatorPadrao × FatorDepreciacao × FatorUso) × FracaoIdeal. **Mostrar a matemática
   item a item, batendo com a lei municipal, é audit-ready** — é o diferencial.
3. **Emitir a DAM / guia** → `POST /api/tributos/imoveis/{imovelId}/iptu/lancar`
   `{ exercicio, primeiroVencimento, numeroParcelas, ... }` → `{ LancamentoId, DamId, ImpostoDevido }`
   (DAM com parcelas/cota única; resíduo de arredondamento absorvido na última parcela).

### 2.D — ISS (apurado das NFS-e ingeridas do ADN)
> Modelo de arquitetura: **ingestão PASSIVA** — não emitimos NFS-e; o Worker baixa os XML do **Ambiente
> Nacional (ADN/Receita)** (CLAUDE.md §8). Ver o risco de aderência em §4.
1. (Setup) **Tabela de alíquotas ISS** por item LC 116 → `POST /api/tributos/iss/aliquotas`.
2. **Sincronizar NFS-e do ADN** → `POST /api/tributos/iss/nfse/sincronizar` `{ prestadores:[CNPJ...], desde }`
   → `{ importadas }` (dedup por chave de acesso).
3. **Apurar o ISS mensal** → `POST /api/tributos/iss/contribuintes/{contribuinteId}/apurar`
   `{ ano, mes, vencimentoIssProprio }` → `ResultadoApuracaoIss` com `IssProprio`, `IssRetido`,
   `IssSubstituicao`, `QuantidadeNotas`. Cada nota tem `MemoriaIssNota` (base = valor do serviço, alíquota,
   modalidade Próprio/Retido/Substituição classificada por LC 116 + lei municipal).

### 2.E — ITBI (sobre o **valor declarado** — Tema 1.113/STJ)
1. (Setup) **Alíquota ITBI** → `POST /api/itbi/aliquotas` (geral + SFH financiado, `fundamentoLegal`).
2. **Pré-visualizar** → `GET /api/itbi/imoveis/{imovelId}/preview?exercicio=&valorDeclarado=` →
   `ResultadoItbi`: **`BaseCalculo = ValorDeclarado`** (presunção de boa-fé), `ValorVenalReferencia`
   **só triagem** (`HaDivergenciaReferencia` sinaliza, **nunca eleva a base de ofício**), `AliquotaPercentual`,
   `ImpostoDevido`.
3. **Lançar a transmissão** → `POST /api/itbi/lancar`
   `{ imovelId, transmitenteId, adquirenteId, exercicio, valorDeclarado, vencimento, ... }` →
   `{ TransmissaoId, LancamentoId, DamId, BaseCalculo, ImpostoDevido }` (DAM contra o adquirente, CTN art. 42).
   - Para a banca: a base é o **declarado**; só um **arbitramento formal com contraditório** (CTN art. 148,
     permissão fina `tributos.itbi.arbitrar`, fluxo `POST /api/itbi/arbitramento/...`) muda a base. Prova de
     conformidade com a jurisprudência vinculante.

### 2.F — ESPINHA FISCAL: despesa → contabilidade PCASP automática → balancete → MSC → remessa TCE
> **O núcleo mais escrutinado da PoC municipal** (módulo contábil é o mais cobrado e eliminatório nos 3 editais).
> Endpoints: `FinancasEndpoints.cs` (despesa/contabilidade) + `TransparenciaEndpoints.cs` (TCE/SICONFI).

Pré-requisitos (1 vez): **semear o plano de contas PCASP** → `POST /api/financas/contabilidade/plano-de-contas/semear`;
criar a **dotação** (LOA) → `POST /api/financas/dotacoes` `{ exercicio, orgao, unidade, funcionalProgramatica, categoriaEconomica, fonteRecurso, valorDotado }`.

Cliques (ciclo Lei 4.320 + contabilização **automática**):
1. **Empenhar** → `POST /api/financas/empenhos` `{ numero, dotacaoId, tipoEmpenho, exercicio, dataEmpenho, credorNome, credorTipo, credorDocumento, valor }`.
2. **Liquidar** → `POST /api/financas/liquidacoes` `{ empenhoId, valor, dataLiquidacao, tipoDocumento, numeroDocumento, chaveNfse, ... }`.
3. **Pagar** → `POST /api/financas/ordens-pagamento` (monta a OP) → `POST /api/financas/ordens-pagamento/{ordemId}/efetuar`.
4. **A contabilização é AUTOMÁTICA** (e isto é o que impressiona): cada estágio dispara um **Domain Event**
   (`EmpenhoEmitido`, `DespesaLiquidada`, `PagamentoEfetuado`) que viaja pelo **Outbox** (transacional) até
   handlers (`ContabilizarEmpenho/Liquidacao/PagamentoHandler`) que chamam o **`MotorContabil`**; este resolve
   o **roteiro contábil** (`RoteirosCatalogo`) do evento e aplica a **partida dobrada PCASP** (orçamentária +
   patrimonial). **O operador NUNCA digita um lançamento contábil** — ele cai do ato de gestão. (Mostrar o
   empenho e, em seguida, o lançamento já no balancete é a prova.)
5. **Balancete (ΣDébito = ΣCrédito)** → `GET /api/financas/contabilidade/balancete?exercicio=2026&mes=6`
   → `LinhaBalanceteDto[]` (`CodigoConta`, `TotalDebitos`, `TotalCreditos`, `SaldoAtual`). A soma de débitos
   bate com a de créditos — e o sistema **recusa gerar MSC desbalanceada** (`MatrizSaldosDesbalanceadaException`).
   (Razão por conta: `GET .../contas/{contaId}/razao`.)
6. **Demonstrativos DCASP** → Balanço Orçamentário / Financeiro / Patrimonial / DVP em
   `GET /api/financas/contabilidade/demonstracoes/balanco-orcamentario|balanco-financeiro|balanco-patrimonial|variacoes-patrimoniais?exercicio=&mes=`.
7. **Gerar a MSC (Matriz de Saldos Contábeis)** → `POST /api/financas/contabilidade/msc/gerar` `{ exercicio, mes }`
   → `{ EventId, QuantidadeLinhas }` (publica `MSCGeradaIntegrationEvent`).
8. **Remessa TCE-RS (SIAPC/PAD)** → `POST /api/transparencia/remessas-tce`
   `{ exercicio, tipoPeriodo, numeroPeriodo, leiauteCodigo:"SIAPC", leiauteVersao:"2026" }` — gera os arquivos
   **posicionais largura-fixa** (ISO-8859-1) + **hash SHA-256**. Depois: **pré-validar** (RDI / e-Validador) →
   `POST /api/transparencia/remessas-tce/{id}/validacao` (transita Gerada→Validada ou Rejeitada; críticas em
   `GET .../criticas`); **empacotar** → `POST .../empacotamento` (ZIP pronto p/ transmissão); **registrar protocolo**
   (segregação de função, `transparencia.remessa.transmitir`) → `POST .../protocolo`.
9. **Reconciliar com o SICONFI** → `POST /api/transparencia/declaracoes-fiscais/{id}/reconciliacao`
   (consulta a API de Dados Abertos da STN e compara com a matriz local; **degrada graciosamente** com 503 se
   a API externa cair, em vez de estourar 500 — resiliência Polly visível).

---

## 3. O que DESTACAR para a comissão (diferenciais citáveis, não marketing)

1. **Trilha de auditoria à prova de adulteração.** Toda mutação de estado grava trilha **imutável**
   (`AuditSaveChangesInterceptor`): `EntityName`, `EntityId`, `Action`, `UserId`, `IpAddress`, `TimestampUtc`,
   **OldValues/NewValues (antes/depois)**. Demonstrável ao vivo: `GET /api/admin/auditoria?entidade=...`
   (perm `admin.auditoria.ver`, leitura de banco dedicado **somente-leitura**). Atende o item recorrente
   "log de transações I/A/E/C por usuário" (Carbonita 48/49/71) — e é exatamente o que o Tribunal de Contas exige.
2. **Isolamento multi-tenant extremo.** `IMustHaveTenant` + **Global Query Filter** por `TenantId` em todo
   DbContext + `SaveChangesInterceptor` que **lança exceção em gravação cross-tenant** + **database-per-tenant**.
   Executivo e Legislativo são **CNPJs/bancos distintos**. Prova ao vivo: 403 ao tocar módulo não licenciado.
   (Pode virar item técnico "atende/não atende".)
3. **Motores parametrizáveis por lei municipal (nada hardcoded).** IPTU (PGV + faixas de alíquota com
   `fundamentoLegal`), ISS (tabela LC 116), ITBI (alíquota geral/SFH), RPPS **fail-closed** até a lei entrar,
   regras fiscais/prazos por tenant (CLAUDE.md §7). Ataca diretamente o requisito "nativo/parametrizável,
   sem codar na hora" (SEFAZ-MS §11.5.2.5; Riqueza §14.6.9) — **onde reprova quem só customiza**.
4. **Web nativo, n-camadas, SEM emulador.** SPA **React (gov.br Design System / eMAG / WCAG 2.1 AA)** + API
   **.NET 8**. Cláusula **literal e repetida** nos editais (Riqueza §1.b "vedado desktop emulado"; Mata §7.9).
   **Diferencial direto e citável na ata da banca.**
5. **Espinha fiscal completa e automática** (PCASP via Outbox → balancete ΣD=ΣC → DCASP → MSC → remessa
   SIAPC/PAD + RDI + reconciliação SICONFI). É o módulo **mais cobrado e eliminatório** — e o nosso ponto forte.
6. **Resiliência e conformidade de design:** Outbox transacional, Polly (retry/circuit breaker) com degradação
   graciosa, JWT/RBAC deny-by-default, A1 em Azure Key Vault (envelope encryption), ~700 testes + fitness
   functions (NetArchTest) verdes, build 0 erro/0 aviso.

---

## 4. O que NÃO mostrar ainda (gaps — ser honesto sobre o que não está pronto)

> Regra: **não demonstrar o que não roda fim-a-fim** (item não apresentado = ausente; e demonstrar tela
> diferente do ofertado reprova — Acórdão 2611/2016-TCU). Quando o requisito for de **integração com sistema
> de terceiros**, lembrar que editais o **dispensam da PoC** (Riqueza §14.6.6 — vira obrigação contratual).

- **eSocial / EFD-Reinf — NÃO demonstrar transmissão.** Falta credenciais/ambiente; só há validação local de
  evento. Bloco obrigatório em alguns editais (Carbonita 549-565) → tratar como obrigação de implantação.
- **Validação OFICIAL no TCE-RS — pendente.** O sistema **gera** a remessa SIAPC/PAD correta com **hash** e
  **pré-validação local (RDI)**, mas a validação no **PAD oficial** depende do **leiaute MT 2026** e do
  **certificado A1** do ente. Demonstrar a **geração + pré-validação + empacotamento + protocolo**, e ser
  explícito: "validação no oficial ocorre na implantação". (Mesma honestidade do README e `VALIDACAO-REAL-TCE.md`.)
- **PPA / LDO / LOA — em construção.** Hoje o orçamento é representado por **Dotações** (`/api/financas/dotacoes`),
  suficiente para vedar despesa sem dotação e rodar o ciclo, mas **não há módulo de planejamento dedicado** com
  histórico/anexos. Se o edital cobrar PPA/LDO/LOA com legislação de autorização, sinalizar como evolução.
- **Tributos / Folha — sem seed de demonstração.** Os motores **calculam e estão testados**, mas a massa é
  montada com os comandos paramétricos na hora (não há "1 clique" como no Legislativo). Ensaiar a sequência
  antes para não titubear na frente da banca (5-7 min/requisito).
- **Risco NFS-e (ingestão passiva × emissão/escrituração ativa).** Nosso modelo **ingere** NFS-e do ADN; alguns
  editais (MG/RS) presumem **emissão/escrituração ABRASF ativa**. **Confirmar no edital-alvo** antes de prometer
  aderência; se exigir emissão, é ajuste de escopo (decisão de arquitetura, CLAUDE.md §8).
- **Portal do Cidadão / autoatendimento / app — ausente.** Camada cidadã (2ª via de guia, protocolo, contracheque
  online) ainda não existe; recorrente em Mata/RS. Não incluir no caminho crítico da demo.
- **PNCP — sem cliente HTTP.** Grava número/evento, mas não transmite ao PNCP; dispensável na PoC (Riqueza §14.6.6).
- **Saúde / Educação / Assistência Social — scaffold.** Bounded contexts existem na solução, mas **não rodam
  fim-a-fim**. **Não abrir** numa PoC, salvo se o objeto for especificamente um desses (não é o caso destas duas trilhas).
- **Deliberação remota / terminais físicos de plenário** (Legislativo) — diferencial/condicional ao edital, não
  mínimo universal; não prometer integração com hardware de painel LED.

> **Resumo de prontidão (AUTOAVALIACAO):** Legislativa **~70-75%** (provável aprovação numa PoC de processo
> legislativo); Municipal **~60-65%** num ERP-completo (núcleo fiscal forte; Folha/Tributos calculam mas a
> banca cobra módulos completos + Cidadão). **Mostre fundo o que está provado, seja transparente no resto.**

# ONDA 2 — Superfície de alto valor PoC (Backend Design, implementação-pronto)

> **Autoridade:** `docs/estudo/completude-modulos/PLANO-PROFUNDIDADE.md` (Onda 2).
> **Status:** spec de implementação. Read-only sobre o código atual; nada foi executado (`dotnet`/`npm`),
> a `:5080` da fundação P0 não foi tocada.
> **Escopo:** três frentes — (1) **Transparência** (portal público + e-SIC + dados abertos + consulta em tempo real);
> (2) **RH — Consignações + Margem consignável**; (3) **Saúde — Agendamento**.
> **Pré-requisitos atendidos:** Onda 0 (navegabilidade) e Onda 1 (Aluno/Turma/Matrícula, Paciente/UBS/Profissional,
> Afastamentos tipados, Inventário) já concluídas. Reuso máximo dos padrões provados.

## 0. Padrões de fundação a reusar (não reinventar)

Verificados no código atual; toda entrega abaixo os herda **sem exceção**:

- **Agregado rico:** `sealed class X : AggregateRoot<XId>, IMustHaveTenant`, ctor privado + factory `Criar/Abrir/Cadastrar`,
  `RaiseDomainEvent(...)` na criação e nas transições. Ex.: `Estabelecimento`, `Afastamento`, `Atendimento`.
- **ID forte:** `readonly record struct XId(Guid Value)` com `New()`/`ToString()`.
- **Efeito determinístico parametrizado por tenant:** o usuário escolhe o *tipo*; o efeito vem de uma
  **Regra vigente** (snapshot no agregado). Padrão `RegraAfastamento` + `IRegraAfastamentoProvider.ObterVigenteAsync`
  — **espinha dorsal da margem consignável** (§2).
- **Gancho na folha sem sujar o motor:** `AjustadorProventoPorAfastamento` ajusta valores ANTES do lançamento;
  `MotorDeCalculoFolha` permanece puro. A consignação entra pela MESMA porta (§2.5).
- **CQRS MediatR:** `ICommand<T>`/`ICommandHandler`, `IQuery<T>`/`IQueryHandler`, `AbstractValidator<T>`,
  `IUnitOfWork.SaveChangesAsync`, `ITenantContext.TenantId`. Pipeline: Validation→Logging→Transaction→Idempotency.
- **Endpoints Minimal API:** `internal static class XEndpoints.Map(IEndpointRouteBuilder)`, `MapGroup("/api/<modulo>")`,
  `.RequirePermission("modulo.acao")`. Mutação retorna `{ id }`/`NoContent`; download via `Results.File`.
- **Módulo:** `IModule` (`AddModule`/`MapEndpoints`/`MigrarBancoAsync`/`DrenarOutboxAsync`); DbContext por módulo,
  schema isolado, `AddModuleSaveChangesInterceptors` (Tenant→Audit→Outbox), `SchemaProvisioner.AplicarAsync`.
- **Read model alimentado por Integration Events (Outbox):** padrão I-13. O portal público (§1) é exatamente isto —
  materializar eventos JÁ publicados (`DespesaEmpenhada/Liquidada`, `PagamentoEfetuado`, `ReceitaArrecadada`,
  `ContratoAssinado`, `LicitacaoHomologada`, `FolhaFechada`, etc.).
- **LGPD:** `[CampoSensivelLgpd]` redige PII na trilha; trilha de **acesso** a dado sensível (quem leu o quê).
- **Cross-module só por `*.Contracts`** (Integration Events + DTOs). Nunca tocar interno de outro módulo.

---

## 1. TRANSPARÊNCIA — Portal público + e-SIC + Dados abertos + Consulta em tempo real

**Lacuna:** ~0% da função pública hoje. O backend de prestação de contas (TCE/SICONFI/núcleo fiscal) está maduro,
mas **não há uma única rota pública**. A matéria-prima já trafega pelos Integration Events internos
(`SimuladoPublicacaoTransparenciaRepository` é o stub que provará o read model real — I-13).

### 1.1 Decisão arquitetural-chave: a superfície PÚBLICA (anônima)

Hoje todo endpoint é `.RequirePermission(...)` (deny-by-default) e o gating de licença em `Program.cs` só age
`if (tenantContext.HasTenant)` — e `TenantContext` só resolve tenant pela claim `tenant_id` do JWT. **Cidadão anônimo
não tem JWT.** Portanto o portal público precisa de **resolução de tenant SEM token** + rota fora do RBAC + rate-limit dedicado.

**Plano (mínimo, sem derrubar a :5080):**
- Novo grupo `/publico/transparencia/{slugEnte}` — **fora** de `/api/...`, logo o gating de licença atual (que casa `/api/<modulo>`) **não o intercepta**. Tag `Transparencia.Publico`.
- **Resolução de tenant por slug público:** a Plataforma já tem catálogo de tenants (CNPJ/Nome). Adicionar `SlugPublico`
  (único, ex.: `maximiliano-de-almeida`) ao registro do tenant e um `ITenantPublicoResolver.ResolverPorSlugAsync(slug)`
  → `TenantId`. O endpoint público usa `TenantOverride` (já existe, usado por jobs) para fixar o tenant do escopo da
  requisição — assim o **Global Query Filter por TenantId continua valendo** e não há vazamento.
- **Sem `.RequirePermission`** nos endpoints públicos; em vez disso `.AllowAnonymous()`.
- **Rate-limit dedicado, mais agressivo** que o global (100/min): policy `"publico"` por IP, ex. 30/min + janela deslizante,
  via `options.AddPolicy("publico", ...)` no `AddRateLimiter` e `.RequireRateLimiting("publico")` no grupo.
- **Licença:** o portal só responde se o tenant tiver o módulo Transparência licenciado — checar `ITenantModuleProvider.IsModuleEnabledAsync(tenantId, "Transparencia")` no resolver; senão 404 (não revelar existência).

### 1.2 Entidades e read models

**a) Read models de Transparência Ativa (materializados de eventos — schema `transparencia`):**

- `PublicacaoDespesa` (read model): `Id, TenantId, Exercicio, Fase{Empenhada|Liquidada|Paga}, NumeroEmpenho,
  CredorNomeOuRazao, CredorDocMascarado, Funcao, Subfuncao, UnidadeOrcamentaria, FonteRecurso, Valor, Data, OrigemEventoId`.
  Alimentado por `DespesaEmpenhadaIntegrationEvent`, `DespesaLiquidadaIntegrationEvent`, `PagamentoEfetuadoIntegrationEvent` (Financas).
- `PublicacaoReceita` (read model): `Id, TenantId, Exercicio, RubricaReceita, FonteRecurso, Valor, Data`.
  Alimentado por `ReceitaArrecadadaIntegrationEvent` (Tributos), `ReceitaCorrenteLiquidaApuradaIntegrationEvent` (Financas).
- `PublicacaoContrato` (read model): `Id, TenantId, NumeroContrato, Fornecedor, Objeto, Valor, Vigencia, Modalidade,
  LinkPncp?`. Alimentado por `ContratoAssinadoIntegrationEvent`, `ContratoPublicadoPncpIntegrationEvent`,
  `LicitacaoHomologadaIntegrationEvent` (Administracao).
- `PublicacaoFolhaNominal` (read model): `Id, TenantId, Competencia, ServidorNome, CargoDescricao, Lotacao,
  RemuneracaoBruta, Descontos, Liquido`. Alimentado por `FolhaFechadaIntegrationEvent` (RH). **CPF/matrícula NUNCA expostos** (§1.6).
- `PublicacaoDiaria` e `PublicacaoRepasse`: estruturas análogas (diárias de RH/Administração; repasses a OSC/fundos).
  Se ainda não houver evento dedicado, derivar de `PublicacaoDespesa` filtrando por elemento de despesa (diárias 339014; repasses 335043/444042). **Honesto:** diárias/repasses são P2 — entregar como filtro derivado primeiro; evento dedicado fica como refino.

> **Invariante dos read models:** são **append/upsert por chave de origem** (`OrigemEventoId`), idempotentes, **somente leitura**
> na superfície pública. Não têm comportamento de domínio — são projeções. Tenant-scoped pelo Global Query Filter.

**b) e-SIC — agregado de domínio rico (transparência passiva, LAI Lei 12.527/2011):**

`PedidoInformacaoSic : AggregateRoot<PedidoInformacaoSicId>, IMustHaveTenant`

Campos: `Protocolo (VO: ano+sequencial por tenant), Solicitante (VO: nome, doc opcional mascarável, email/contato — base
legal própria), Descricao, FormaResposta{Email|RetiradaPresencial|...}, DataAbertura, PrazoResposta (=DataAbertura+20 dias úteis),
ProrrogadoAte? (+10 dias úteis, com justificativa), Situacao{Aberto|EmAtendimento|Respondido|Indeferido|RecursoAberto|RecursoRespondido|Encerrado},
Resposta? (texto + anexos), DataResposta?, Recurso? (VO: instancia 1ª/2ª, fundamento, dataInterposicao, decisao, dataDecisao)`.

Transições (factory + métodos, cada um `RaiseDomainEvent`):
- `Abrir(tenant, solicitante, descricao, formaResposta, dataAbertura, calendarioDiasUteis)` → calcula `PrazoResposta` (20 dias úteis); `Situacao=Aberto`. Protocolo gerado por sequencial do tenant/ano.
- `IniciarAtendimento()` → `EmAtendimento`.
- `Prorrogar(motivo, calendario)` → soma +10 dias úteis a `ProrrogadoAte`; **invariante:** só uma prorrogação, só antes do vencimento, motivo obrigatório (LAI art. 11 §2º).
- `Responder(resposta, dataResposta)` → `Respondido`; **invariante:** dentro do prazo (vencido marca em atraso, mas não bloqueia — registra para indicador).
- `Indeferir(fundamentoLegal)` → `Indeferido` (fundamento obrigatório — LAI art. 11 §1º).
- `InterporRecurso(...)` / `DecidirRecurso(...)` → ciclo de recurso (1ª/2ª instância).
- `Encerrar()`.

> **Invariantes-chave:** prazo legal **calculado, nunca digitado** (parametrizável por tenant via `ICalendarioDiasUteisProvider`,
> mesma filosofia de `IRegraAfastamentoProvider`); prorrogação única e justificada; resposta/indeferimento sempre fundamentados;
> protocolo único por (tenant, ano).

### 1.3 Endpoints

**PÚBLICOS (anônimos, read-only, rate-limit `publico`, grupo `/publico/transparencia/{slug}`):**

| Verbo | Rota | Retorno |
|---|---|---|
| GET | `/despesas?exercicio&fase&funcao&fonte&credor&pagina&tamanho` | página de `PublicacaoDespesa` |
| GET | `/receitas?exercicio&rubrica&fonte&pagina&tamanho` | página de `PublicacaoReceita` |
| GET | `/contratos?ano&fornecedor&pagina&tamanho` | página de `PublicacaoContrato` |
| GET | `/folha?competencia&lotacao&pagina&tamanho` | folha nominal **sem PII** |
| GET | `/diarias?exercicio&pagina` · `/repasses?exercicio&pagina` | derivados |
| GET | `/resumo-fiscal?exercicio` | receita arrecadada × despesa por fase (consulta em tempo real) |
| GET | `/minimos?exercicio` | reusa `ApurarMinimosQuery` (núcleo fiscal já existe) — semáforo Saúde/Educação |
| POST | `/esic` | abre pedido LAI → `{ protocolo }` (anônimo permitido; LAI veda exigir motivação) |
| GET | `/esic/{protocolo}` | consulta status público do pedido (sem expor PII do solicitante a terceiros — ver §1.6) |
| POST | `/esic/{protocolo}/recurso` | interpõe recurso |
| GET | `/dados-abertos` | catálogo (dicionário) de datasets |
| GET | `/dados-abertos/{dataset}.csv?exercicio` | download CSV (folha, contratos, licitações, diárias, repasses, despesas, receitas) |

> **Paginação obrigatória** em todas as listas (reusar o padrão de `BuscarPacientes`/`BuscarServidores`).
> CSV gerado em stream (`Results.File`, `text/csv`), padrão do `GeradorMscCsv`. `Span<T>` para lotes grandes.

**INTERNOS (autenticados, `.RequirePermission("transparencia.*")`, grupo `/api/transparencia`):**

| Verbo | Rota | Permissão |
|---|---|---|
| GET | `/esic` (lista/filtra pedidos do tenant) | `transparencia.esic.ver` |
| GET | `/esic/{id}` (detalhe interno, com PII) | `transparencia.esic.ver` |
| POST | `/esic/{id}/atendimento` · `/resposta` · `/prorrogacao` · `/indeferimento` · `/recurso/decisao` | `transparencia.esic.responder` |
| POST | `/dados-abertos/datasets/{dataset}/regenerar` (rematerializa) | `transparencia.gerenciar` |
| GET | `/publicacoes/conferencia?exercicio` (espelho interno dos read models p/ auditoria) | `transparencia.ver` |

### 1.4 Persistência / migração

- Schema `transparencia` (já existe). Migração `Onda2_PortalPublicoESic`:
  tabelas `transparencia.publicacao_despesa`, `publicacao_receita`, `publicacao_contrato`, `publicacao_folha_nominal`,
  `pedido_informacao_sic` (+ owned `recurso`/`resposta`/`solicitante`), `dataset_dados_abertos` (catálogo/dicionário).
- Índices: read models por `(TenantId, Exercicio, Funcao/Competencia)` para paginação rápida; `pedido_informacao_sic`
  índice único `(TenantId, Protocolo)`.
- **Plataforma:** migração separada adicionando `SlugPublico` único ao tenant.
- Configurations via Fluent API (sem data annotations no domínio), padrão das `Configurations/*.cs`.

### 1.5 Handlers (consumidores de eventos — projeção)

`NotificationHandler<DespesaEmpenhadaIntegrationEvent>` → upsert `PublicacaoDespesa`. Idem para Liquidada/Pago/Receita/Contrato/Folha.
Cada handler é **idempotente** por `OrigemEventoId` (a Onda 2 não muda o Outbox; só adiciona consumidores). `DrenarOutboxAsync`
do módulo já publica/consome — só registrar os novos handlers no `AddMediatR` do `TransparenciaModule`.

### 1.6 LGPD — o que é público vs. minimizado

- **Público por lei (transparência ativa, Dec. 7.724/2012):** nome do servidor, cargo, lotação, remuneração bruta/descontos/líquido;
  fornecedor (razão social/nome empresarial), objeto e valor de contratos; despesas/receitas por fase.
- **Minimizado/mascarado:** **CPF** sempre mascarado (`***.456.789-**`) em qualquer superfície pública (read model já grava mascarado;
  `[CampoSensivelLgpd]` na origem). **Matrícula** não exposta. Endereço/conta bancária do credor: nunca.
- **e-SIC:** o **status público** (`GET /esic/{protocolo}`) revela situação/datas/resposta, **mas não os dados pessoais do solicitante**
  a terceiros (LAI protege o requerente). Os dados do solicitante (nome/doc/contato) ficam visíveis só na superfície **interna** autenticada.
- **Trilha de acesso a dado sensível:** acesso interno ao detalhe do pedido (com PII) registra "quem leu, quando, por quê" (padrão LG-3).
- **CSV de dados abertos:** mesma regra de mascaramento da tela; nada de PII além do permitido pela transparência ativa.

### 1.7 Testes-chave

- Portal anônimo retorna **só** o tenant do slug (anti-vazamento cross-tenant com `TenantOverride`).
- e-SIC: `PrazoResposta` = +20 dias **úteis**; prorrogação única e só antes do vencimento; resposta após prazo marca "em atraso".
- Read model idempotente: reprocessar o mesmo `OrigemEventoId` não duplica linha.
- LGPD: folha nominal pública nunca contém CPF/matrícula; CPF mascarado no CSV.
- Rate-limit `publico` rejeita com 429 acima do limite; rota pública não exige JWT; módulo não-licenciado → 404.

### 1.8 Sub-workflows (ordem) e esforço

1. **Infra pública** (slug do tenant, `ITenantPublicoResolver`, grupo `/publico`, rate-limit, AllowAnonymous). **M**
2. **Read models + handlers de projeção** (despesa/receita/contrato/folha) + consulta em tempo real (resumo-fiscal). **M/G**
3. **Endpoints públicos de consulta + paginação + reuso do `/minimos`.** **M**
4. **e-SIC** (agregado + prazos + transições + endpoints públicos/internos). **M/G**
5. **Dados abertos** (catálogo/dicionário + export CSV por dataset). **M**

**Esforço total da frente Transparência: G.**

---

## 2. RH — Consignações + Margem consignável (Lei 14.131/2021)

**Lacuna:** não existe consignação nem margem hoje; é uso diário de todo RH municipal. **Reusa a espinha** de
`RegraAfastamento`/`Afastamento`/`AjustadorProventoPorAfastamento`: tipo escolhido pelo usuário, efeito derivado de regra
parametrizada, gancho determinístico na folha sem sujar o motor.

### 2.1 Entidades

**a) `Consignataria : AggregateRoot<ConsignatariaId>, IMustHaveTenant`** (cadastro mestre — banco/entidade habilitada):
`Cnpj, RazaoSocial, TipoConsignataria{InstituicaoFinanceira|EntidadeClassista|Seguradora|Outra}, Situacao{Ativa|Suspensa}`.
Factory `Cadastrar`, métodos `Suspender/Reativar`. Unicidade `(TenantId, Cnpj)`.

**b) `RubricaConsignavel` (parametrização por tenant — análoga a `RegraAfastamento`):**
`Codigo, Descricao, Categoria{Obrigatoria|Facultativa|Beneficio}, ContaParaMargem(bool), GrupoMargem{Geral|CartaoConsignado|CartaoBeneficio}`.
Define quais rubricas entram em **qual** dos três "baldes" de margem.

**c) `ContratoConsignacao : AggregateRoot<ContratoConsignacaoId>, IMustHaveTenant`** (agregado rico):
`ServidorId, ConsignatariaId, RubricaConsignavel (snapshot do grupo/categoria), NumeroContratoExterno, ValorParcela,
QuantidadeParcelas, ParcelasPagas, DataAverbacao, Situacao{Averbada|Suspensa|Quitada|Cancelada}, GrupoMargem (snapshot)`.

Transições (cada uma `RaiseDomainEvent`):
- `Averbar(tenant, servidorId, consignatariaId, rubrica, valorParcela, qtdParcelas, dataAverbacao, margemDisponivel)` —
  **invariante:** `valorParcela <= margemDisponivel` no grupo; consignatária Ativa; valor>0; qtd>0. Nasce `Averbada`.
- `Suspender(motivo)` / `Reativar(margemDisponivel)` (reativar re-checa a margem).
- `RegistrarParcelaPaga()` — incrementa `ParcelasPagas`; ao atingir `QuantidadeParcelas` → `Quitada` (idempotente).
- `Cancelar(motivo)` — libera margem.

**d) `MargemConsignavel` (VO de cálculo, domínio puro — análogo a `EfeitoFolhaAfastamento`):**
recebe `BaseDeCalculo` (remuneração-base consignável da competência) e os percentuais **parametrizáveis por tenant**
(default legal Lei 14.131/2021): **35% geral + 5% cartão de crédito consignado + 5% cartão benefício = 45% total**.
Expõe `LimiteGrupo(grupo)`, `Comprometido(grupo)`, `Disponivel(grupo) = Limite - Comprometido` (nunca negativo).

### 2.2 Invariantes da margem

- Três baldes independentes: **Geral 35%**, **Cartão consignado 5%**, **Cartão benefício 5%** — cada averbação consome
  o balde do seu `GrupoMargem`. Reserva legal: o cartão (5%+5%) **não** invade o geral e vice-versa.
- `Disponivel(grupo) >= ValorParcela` é **pré-condição de averbação** — checada com a base da competência corrente.
- Percentuais **parametrizáveis por tenant/vigência** (CLAUDE.md S7/S16 — nunca hardcoded), via
  `IParametrosMargemProvider.ObterVigenteAsync(competencia)` (mesmo padrão de `IParametrosFolhaProvider`/`IRegraAfastamentoProvider`).
- Base de cálculo da margem = remuneração consignável (proventos com `RubricaConsignavel.ContaParaMargem` — tipicamente
  vencimento + permanentes; exclui eventuais), apurada da folha — **não digitada**.

### 2.3 Endpoints (`/api/recursoshumanos`, autenticados)

| Verbo | Rota | Permissão |
|---|---|---|
| POST/GET | `/consignatarias` · `/consignatarias/{id}` | `rh.consignacao.gerenciar` / `.ver` |
| POST | `/consignatarias/{id}/suspensao` · `/reativacao` | `rh.consignacao.gerenciar` |
| GET | `/servidores/{id}/margem?competencia` | `rh.consignacao.ver` (3 baldes: limite/comprometido/disponível) |
| GET | `/servidores/{id}/consignacoes` | `rh.consignacao.ver` |
| POST | `/consignacoes` (averbar) | `rh.consignacao.averbar` |
| POST | `/consignacoes/{id}/suspensao` · `/reativacao` · `/cancelamento` | `rh.consignacao.gerenciar` |
| POST | `/rubricas-consignaveis` (parametrização) | `rh.consignacao.gerenciar` |

### 2.4 Persistência / migração

- Schema `recursoshumanos`, migração `Onda2_Consignacoes`: `consignataria`, `rubrica_consignavel`, `contrato_consignacao`,
  `parametros_margem` (percentuais por vigência). Índices: `(TenantId, ServidorId, Situacao)` em `contrato_consignacao`
  (consulta de margem rápida); único `(TenantId, Cnpj)` em `consignataria`.

### 2.5 Gancho na folha (o ponto crítico)

Criar `LancadorDescontosConsignados` (análogo a `AjustadorProventoPorAfastamento`): no fechamento/cálculo da folha,
para cada servidor com `ContratoConsignacao` **Averbada** vigente na competência, lança um **Desconto** por contrato,
**respeitando a margem disponível apurada da MESMA competência**. Se a soma dos averbados exceder a margem
(ex.: base caiu por afastamento — §1 RH), aplica **corte por prioridade** (obrigatórias→facultativas→benefício) e
registra a glosa para auditoria. O `MotorDeCalculoFolha` permanece **puro** (recebe verbas já resolvidas), como hoje.
**Ordem na consolidação:** a margem incide sobre a remuneração **após** ajuste de afastamento (§ RH Onda 1) e **antes**
do líquido — encaixa na sequência de `ConsolidarDescontosLegaisMensais` (legais primeiro; consignado sobre o que sobra).

### 2.6 Eventos / cross-module

- Domain events internos: `ConsignacaoAverbada`, `ConsignacaoSuspensa`, `ConsignacaoQuitada`.
- Integration Event (Contracts) **opcional**: `ConsignacaoAverbadaIntegrationEvent` — só se outro módulo precisar.
  Não vaza para Transparência (consignação individual é dado do vínculo; folha nominal pública não detalha consignados).

### 2.7 Testes-chave

- Averbar acima da margem do grupo → rejeita; dentro → aceita e reduz `Disponivel`.
- Três baldes independentes (cartão não invade geral).
- Base cai (afastamento) → margem recalcula → corte por prioridade no lançamento, sem líquido negativo.
- Percentuais parametrizados por tenant respeitados (não hardcoded).
- Quitação ao atingir `QuantidadeParcelas`; cancelamento libera margem.

### 2.8 Sub-workflows (ordem) e esforço

1. `Consignataria` + `RubricaConsignavel` (cadastros mestres + endpoints). **M**
2. VO `MargemConsignavel` + `IParametrosMargemProvider` + endpoint de consulta de margem. **M**
3. `ContratoConsignacao` (agregado + averbar/suspender/cancelar/quitar + endpoints). **M**
4. `LancadorDescontosConsignados` (gancho na folha + corte por prioridade) + testes de folha. **M/G**

**Esforço total da frente RH-Consignações: G.**

---

## 3. SAÚDE — Agendamento de consultas/exames

**Lacuna:** não há agenda. **Reusa Onda 1** (`Paciente`/`PacienteId`, `Estabelecimento`/`EstabelecimentoId`,
`Profissional`/`ProfissionalId` — todos já são agregados/IDs fortes locais) e o tipo de `Competencia`/`Atendimento`.
A consulta marcada **vira** `Atendimento` (PEP/SOAP já existente) quando o paciente comparece — ponte natural.

### 3.1 Entidades

**a) `AgendaProfissional : AggregateRoot<AgendaProfissionalId>, IMustHaveTenant`** (grade de disponibilidade):
`ProfissionalId, EstabelecimentoId, TipoAtendimento{Consulta|Exame}, DiaSemana ou Data, HoraInicio, HoraFim,
DuracaoSlotMinutos, QuantidadeVagas (capacidade), Situacao{Aberta|Bloqueada}`.
Gera os **slots/vagas** (`Vaga`: `DataHora, Situacao{Livre|Reservada|Ocupada|Bloqueada}`) por expansão da grade.
Métodos: `Publicar`, `BloquearDia(data, motivo)`, `ReabrirDia(data)`.

**b) `Agendamento : AggregateRoot<AgendamentoId>, IMustHaveTenant`** (a marcação — agregado rico):
`PacienteId, ProfissionalId, EstabelecimentoId, DataHora, TipoAtendimento, VagaId,
Situacao{Marcado|Confirmado|Cancelado|Falta|Realizado}, Prioridade, DataMarcacao, MotivoCancelamento?`.

Transições (cada uma `RaiseDomainEvent`):
- `Marcar(tenant, pacienteId, profissionalId, estabelecimentoId, vaga, tipo)` — **invariantes:** vaga `Livre`;
  estabelecimento/profissional **ativos** (`Estabelecimento.EstaAtivo`); sem duplo-agendamento do paciente no mesmo slot;
  ocupa a vaga. Nasce `Marcado`.
- `Confirmar()` — `Marcado→Confirmado`.
- `Cancelar(motivo, origem)` — libera a vaga; `→Cancelado` (motivo obrigatório).
- `RegistrarFalta()` — `Confirmado/Marcado→Falta` na data; libera/consome conforme política.
- `Realizar(atendimentoId?)` — `→Realizado`; gancho para criar/associar `Atendimento` (PEP).

**c) `FilaEspera` (quando não há vaga):** `PacienteId, ProfissionalId|Especialidade, EstabelecimentoId, Prioridade,
DataEntrada, Situacao{Aguardando|Convocado|Atendido|Removido}`. Ao liberar vaga, convoca por prioridade+ordem (FIFO dentro da prioridade).

### 3.2 Invariantes

- **Sem overbooking:** uma `Vaga Livre` só admite uma marcação (concorrência tratada na transição que ocupa a vaga).
- Cancelamento/falta **libera** a vaga (reentra no pool / dispara convocação da fila).
- Agendamento sempre referencia **Paciente/Profissional/Estabelecimento existentes e ativos** (Onda 1) — por ID, sem navegação cross-aggregate.
- Datas no futuro para marcar; falta só registrável na data ou depois.

### 3.3 Endpoints (`/api/saude`, autenticados; `.RequirePermission("saude.agenda.*")`)

| Verbo | Rota | Permissão |
|---|---|---|
| POST/GET | `/agendas` · `/agendas/{id}` (cadastra/consulta grade) | `saude.agenda.gerenciar` / `.ver` |
| POST | `/agendas/{id}/publicacao` · `/bloqueio` · `/reabertura` | `saude.agenda.gerenciar` |
| GET | `/agendas/vagas?profissional&estabelecimento&de&ate&tipo` | `saude.agenda.ver` (vagas livres) |
| POST | `/agendamentos` (marcar) | `saude.agenda.marcar` |
| POST | `/agendamentos/{id}/confirmacao` · `/cancelamento` · `/falta` · `/realizacao` | `saude.agenda.marcar` |
| GET | `/agendamentos?paciente&profissional&data&situacao&pagina` | `saude.agenda.ver` |
| POST/GET | `/fila-espera` · `/fila-espera/{id}/convocacao` | `saude.agenda.marcar` |

### 3.4 Persistência / migração

- Schema `saude`, migração `Onda2_Agendamento`: `agenda_profissional` (+ owned `vaga`), `agendamento`, `fila_espera`.
- Índices: `vaga` por `(TenantId, ProfissionalId, DataHora, Situacao)` (busca de vaga livre); `agendamento` por
  `(TenantId, PacienteId, DataHora)` e `(TenantId, ProfissionalId, DataHora)`.

### 3.5 Eventos / cross-module

- Domain events: `AgendamentoMarcado/Confirmado/Cancelado`, `FaltaRegistrada`. Internos ao módulo.
- Ao `Realizar` com `Atendimento`, reusa o fluxo PEP/SOAP existente (mesmos IDs fortes). Sem novo Contract necessário.
- Indicador de absenteísmo (faltas) pode alimentar BI/Painel do Gestor via evento futuro (P3).

### 3.6 LGPD

- Agenda trata dado sensível (saúde, LGPD art. 11). Trilha de **acesso** a quem consultou agendamentos de um paciente (LG-3).
- Nenhuma exposição pública (a frente pública é só Transparência §1).

### 3.7 Testes-chave

- Sem overbooking: 2ª marcação na mesma vaga falha.
- Cancelar/Falta libera a vaga e convoca a fila por prioridade.
- Marcar exige Paciente/Profissional/Estabelecimento ativos (Estabelecimento inativo → rejeita).
- `Realizar` cria/associa `Atendimento` com os mesmos IDs.
- Isolamento tenant (Global Query Filter) nas vagas/agendamentos.

### 3.8 Sub-workflows (ordem) e esforço

1. `AgendaProfissional` + expansão de `Vaga` + endpoints de grade/vagas. **M/G**
2. `Agendamento` (marcar/confirmar/cancelar/falta) + endpoints + invariante anti-overbooking. **M**
3. `FilaEspera` + convocação por prioridade. **M**
4. Gancho `Realizar`→`Atendimento` (ponte com PEP existente). **P/M**

**Esforço total da frente Saúde-Agendamento: G** (depende de UBS/Profissional da Onda 1 — já entregue).

---

## RESUMO POR MÓDULO

**1) Transparência (G).** Maior salto de percepção da PoC: cria a **superfície pública anônima** (grupo `/publico`,
resolução de tenant por slug + `TenantOverride`, rate-limit dedicado) materializando read models de eventos **já publicados**
(despesa/receita/contrato/folha) + consulta em tempo real + e-SIC (agregado LAI com prazo 20+10 dias úteis, recurso) +
dados abertos (CSV). LGPD: transparência ativa publica nome/cargo/remuneração; **CPF/matrícula mascarados/omitidos**.

**2) RH — Consignações + Margem (G).** Reusa a espinha de `RegraAfastamento`/gancho-de-folha: `Consignataria` +
`RubricaConsignavel` + `ContratoConsignacao` + VO `MargemConsignavel` (35% + 5% + 5%, parametrizável por tenant).
Averbação/suspensão validam a margem; `LancadorDescontosConsignados` lança o desconto na folha respeitando a margem
da competência (corte por prioridade), mantendo o `MotorDeCalculoFolha` puro.

**3) Saúde — Agendamento (G).** Reusa Paciente/UBS/Profissional (Onda 1): `AgendaProfissional` (grade→vagas),
`Agendamento` (marcar/confirmar/cancelar/falta/realizar, anti-overbooking) e `FilaEspera` (convocação por prioridade).
A consulta realizada faz ponte para o `Atendimento`/PEP existente. Dado sensível com trilha de acesso (LGPD).

## ORDEM RECOMENDADA

1. **Transparência** — maior risco reputacional da PoC (o nome promete o que o módulo não entrega) e **não depende de
   credencial** (matéria-prima já flui por eventos). Começar pela **infra pública** (item §1.8.1), que é o bloqueador.
2. **RH — Consignações** — uso diário de todo RH; reaproveita máximo a espinha de afastamentos já provada; risco isolado
   (tudo intra-módulo, gancho na folha controlado).
3. **Saúde — Agendamento** — alto valor operacional, mas é o que mais depende de pré-requisitos (Onda 1) e tem o ciclo
   de concorrência (vaga) mais delicado; fazer por último, com a fundação dos outros dois já estável.

**Leitura honesta:** as três frentes são **G**. Nenhuma exige credencial oficial — são read models, agregados e fluxos
internos. O item de **maior esforço de fundação nova** é a superfície pública anônima da Transparência (tenant sem JWT +
rota fora do gating + rate-limit), que não tem precedente no código atual; as demais reusam padrões já consolidados.

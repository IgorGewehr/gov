---
modulo: AssistenciaSocial
agregado: ProntuarioSuas
contexto: AssistenciaSocial (SUAS — acompanhamento familiar sigiloso PAIF/PAEFI)
poder: Executivo
schema: assistenciasocial
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF arts. 203-204", "Lei 8.742/1993 (LOAS, c/ Lei 12.435/2011)", "Tipificacao Nacional dos Servicos Socioassistenciais (Res. CNAS 109/2009)", "NOB-SUAS/2012 (Res. CNAS 33/2012)", "LGPD art. 11 (dado sensivel)"]
---

# ProntuarioSuas — Regras-as-Code (Rules-as-Code)

> Documento **sigiloso** do acompanhamento familiar no SUAS (PAIF em CRAS / PAEFI em CREAS):
> registra atendimentos, plano de acompanhamento e violações de direitos. Sujeito a **sigilo
> profissional** e a uma **trilha de acesso imutável** (quem leu, quando, por quê), exigível pelo
> controle social/Tribunal de Contas. **PAIF só em CRAS; PAEFI só em CREAS.** Marco: CF arts.
> 203-204; Lei 8.742/1993 (LOAS, c/ Lei 12.435/2011); Tipificação Nacional (Res. CNAS 109/2009);
> NOB-SUAS/2012; LGPD art. 11. Este arquivo é **normativo e versionado**; o código (domínio,
> handlers, validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Prontuário SUAS (`ProntuarioSuas`) | Registro **sigiloso** do acompanhamento familiar; raiz de agregado. Implementa `IMustHaveTenant`. |
| Abertura (`Abrir` / `Aberto`) | Ato de abrir o prontuário ao iniciar o acompanhamento de uma família. |
| Registro de Acompanhamento (`RegistroAcompanhamento`) | Entidade-filha: anotação de um atendimento/evolução do acompanhamento. |
| Plano de Acompanhamento Familiar (`PlanoAcompanhamentoFamiliar`) | Entidade-filha: plano com objetivos/compromissos pactuados com a família. |
| Violação de Direito (`ViolacaoDireito`) | Entidade-filha: registro de direito violado (dado sensível, p.ex. criança/adolescente). |
| Atendimento (`RegistrarAtendimento` / `AtendimentoRegistrado`) | Registro de um atendimento PAIF/PAEFI/SCFV no prontuário. |
| Serviço (`Servico` : `TipoServico`) | `Paif`, `Paefi` ou `Scfv` — vínculo com a oferta da unidade. |
| Encerramento (`EncerrarAcompanhamento` / `Encerrado`) | Conclusão do acompanhamento, com motivo. |
| Unidade de Atendimento (`UnidadeAtendimentoId`) | CRAS/CREAS/CentroPOP responsável (PAIF só em CRAS; PAEFI só em CREAS). |
| Sigilo (`Sigiloso`) | Atributo do prontuário: conteúdo protegido por sigilo profissional. |
| Trilha de Acesso (`AcessoProntuario` / `RegistrarAcessoProntuario`) | Registro **imutável** de cada leitura do prontuário (quem/quando/por quê). |
| Motivo de Acesso (`MotivoAcesso`) | Justificativa obrigatória da leitura do conteúdo sigiloso. |
| Família (`FamiliaId`) | Família acompanhada (vínculo com o agregado `Familia`). |
| Tenant (`TenantId`) | Ente público (Prefeitura) dono do registro — município. |
| Situação (`Situacao` : `SituacaoProntuario`) | Estado atual do prontuário no ciclo de acompanhamento. |

---

## 2. Modelo

- **Identidade:** `ProntuarioSuasId` — `readonly record struct ProntuarioSuasId(Guid Value)`; fábrica `ProntuarioSuasId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `ProntuarioSuas : AggregateRoot<ProntuarioSuasId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `Abrir(...)`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (município) dono do registro. | `private set` |
| `FamiliaId` | `Guid` | Família acompanhada. | `private set` |
| `UnidadeAtendimentoId` | `Guid` | CRAS/CREAS responsável (define serviços permitidos). | `private set` |
| `Situacao` | `SituacaoProntuario` | Situação atual. | `private set` |
| `DataAbertura` | `DateOnly` | Data de abertura do acompanhamento. | `private set` |
| `MotivoEncerramento` | `string?` | Motivo do encerramento (preenchido em `Encerrado`). | `private set` |
| `DataEncerramento` | `DateOnly?` | Data do encerramento. | `private set` |
| `Registros` | `IReadOnlyCollection<RegistroAcompanhamento>` | Atendimentos/evoluções (entidades-filhas). | coleção encapsulada |
| `Plano` | `PlanoAcompanhamentoFamiliar?` | Plano de acompanhamento (entidade-filha). | encapsulado |
| `Violacoes` | `IReadOnlyCollection<ViolacaoDireito>` | Violações de direitos registradas (sensível). | coleção encapsulada |

> **Sigilo:** o conteúdo (registros, plano, violações) é **sigiloso**; o acesso é registrado na trilha (ver §10) e **nunca** trafega para outros tenants/módulos (README §6, §8 ⭐).

### Value Objects / entidades-filhas

- `RegistroAcompanhamento` — `RegistroAcompanhamentoId`, `Servico` (`TipoServico`), `DataAtendimento` (`DateOnly`), `Descricao` (texto sigiloso), `ProfissionalId` (autor).
- `PlanoAcompanhamentoFamiliar` — `PlanoId`, `Objetivos` (texto), `DataPactuacao` (`DateOnly`), `Compromissos` (lista).
- `ViolacaoDireito` — `ViolacaoId`, `TipoViolacao` (enum), `EnvolveCriancaAdolescente` (`bool` — dado sensível reforçado), `DataIdentificacao` (`DateOnly`).
- `AcessoProntuario` — registro **imutável** da trilha: `AcessoId`, `UsuarioId`, `MotivoAcesso` (`string`), `DataHoraAcessoUtc` (`DateTime`). **Append-only** (sem update/delete).

### Enum `TipoServico`

| Valor | Numérico | Descrição |
|---|---|---|
| `Paif` | 1 | Proteção e Atendimento Integral à Família (**só em CRAS**). |
| `Paefi` | 2 | Proteção e Atendimento Especializado a Famílias e Indivíduos (**só em CREAS**). |
| `Scfv` | 3 | Serviço de Convivência e Fortalecimento de Vínculos. |

### Enum `SituacaoProntuario`

| Valor | Numérico | Descrição |
|---|---|---|
| `Aberto` | 1 | Prontuário aberto; acompanhamento em curso (estado inicial). |
| `Encerrado` | 2 | Acompanhamento encerrado, com motivo (terminal). |

> **Conjuntos de referência usados nas guardas:**
> - **Ativo** = { `Aberto` } — única situação que admite `RegistrarAtendimento`, `DefinirPlano`, `RegistrarViolacao`, `EncerrarAcompanhamento`.
> - **Encerrado** = { `Encerrado` } — terminal; não admite novos atendimentos nem novo encerramento.
> - **Compatibilidade serviço↔unidade:** `Paif` exige unidade **CRAS**; `Paefi` exige unidade **CREAS** (README §5).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** `FamiliaId` e `UnidadeAtendimentoId` são obrigatórios na abertura (`ArgumentNullException`); o prontuário sempre se vincula a uma família referenciada e a uma unidade.
- **I-2.** Na abertura, a situação inicial é `Aberto`, `DataAbertura` = data informada (sem evento de domínio na abertura nesta versão).
- **I-3.** `RegistrarAtendimento` só é permitido com situação `Aberto`; prontuário `Encerrado` não admite novos atendimentos (`InvalidOperationException`).
- **I-4.** **PAIF só em CRAS; PAEFI só em CREAS:** registrar `Servico` incompatível com o **tipo da unidade** do prontuário é rejeitado por invariante de domínio (`InvalidOperationException`) — README §5, §8 (Cenário "Oferta incompatível").
- **I-5.** Ao registrar atendimento válido, adiciona `RegistroAcompanhamento` e emite `AtendimentoRegistrado(prontuarioId, unidadeId, servico, dataAtendimento)`.
- **I-6.** `EncerrarAcompanhamento` exige situação `Aberto` e `motivoEncerramento` não nulo/vazio; passa a `Encerrado`, grava `DataEncerramento` e emite `AcompanhamentoEncerrado(prontuarioId, motivoEncerramento)`.
- **I-7.** **Sigilo:** toda **leitura** do conteúdo do prontuário exige `MotivoAcesso` e é **registrada na trilha** (`AcessoProntuario`), de forma **imutável** (append-only) — README §6, §8 ⭐.
- **I-8.** A trilha de acesso (`AcessoProntuario`) **nunca** é atualizada nem removida (somente inserção); é exigível pelo controle social/Tribunal de Contas (README §6).
- **I-9.** O conteúdo sigiloso (registros/plano/violações) **não trafega** para outros tenants nem para módulos não autorizados; Integration Events expõem apenas identificadores e metadados (README §6, §7).
- **I-10.** `ViolacaoDireito` que envolva criança/adolescente é **dado sensível reforçado** (art. 11 LGPD); seu registro segue minimização e finalidade, e o acesso é sempre auditado (I-7).
- **I-11.** Prontuário `Encerrado` não admite novo encerramento (idempotência negativa) nem novo plano/violação/atendimento (I-3); estado terminal.
- **I-12.** `DefinirPlano` e `RegistrarViolacao` só ocorrem com situação `Aberto`.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Abrir` | `Aberto` | `FamiliaId` e `UnidadeAtendimentoId` informados | — |
| `Aberto` | `RegistrarAtendimento` | `Aberto` (mantém) | `Servico` compatível com o tipo da unidade (I-4) | `AtendimentoRegistrado` |
| `Aberto` | `DefinirPlano` | `Aberto` (mantém) | situação == `Aberto` | — |
| `Aberto` | `RegistrarViolacao` | `Aberto` (mantém) | situação == `Aberto` | — |
| `Aberto` | `EncerrarAcompanhamento` | `Encerrado` | `motivoEncerramento` não vazio | `AcompanhamentoEncerrado` |
| qualquer | `RegistrarAcessoProntuario` (leitura) | (mantém) | `MotivoAcesso` não vazio (append-only) | — (trilha imutável) |

> Observações:
> - `RegistrarAtendimento`, `DefinirPlano` e `RegistrarViolacao` **não** alteram a situação (permanece `Aberto`); apenas mutam o conteúdo.
> - `RegistrarAcessoProntuario` é um efeito colateral **obrigatório** de qualquer leitura de conteúdo sigiloso (I-7); registra na trilha sem alterar o estado de acompanhamento.
> - `Encerrado` é terminal: nenhum comando de conteúdo é aceito após o encerramento (I-3, I-11).

---

## 5. Comandos (escrita)

### 5.1 AbrirProntuario

- **Command:** `AbrirProntuarioCommand(Guid FamiliaId, Guid UnidadeAtendimentoId) : ICommand<Guid>`.
- **Entrada (DTO):** `FamiliaId`, `UnidadeAtendimentoId` (CRAS/CREAS).
- **Dependências do handler:** `IFamiliaRepository`, `IUnidadeAtendimentoRepository`, `IProntuarioSuasRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Família existe (tenant-scoped), senão `InvalidOperationException("Família não encontrada.")`.
  - Unidade existe, senão `InvalidOperationException("Unidade de atendimento não encontrada.")`.
- **Efeito:** `hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)`; cria via `ProntuarioSuas.Abrir(tenant.TenantId, familiaId, unidadeId, hoje)`; `prontuarios.Adicionar(prontuario)`; `SaveChangesAsync`.
- **Pós-condições:** novo `ProntuarioSuas` em situação `Aberto`; retorna `prontuario.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (família/unidade inexistente).
- **Evento de domínio:** — (sem evento na abertura nesta versão).

### 5.2 RegistrarAtendimento

- **Command:** `RegistrarAtendimentoCommand(Guid ProntuarioId, TipoServico Servico, DateOnly DataAtendimento, string Descricao, Guid ProfissionalId) : ICommand`.
- **Entrada (DTO):** `ProntuarioId`, `Servico` (`Paif`/`Paefi`/`Scfv`), `DataAtendimento`, `Descricao` (sigilosa), `ProfissionalId`.
- **Dependências do handler:** `IProntuarioSuasRepository`, `IUnidadeAtendimentoRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pré-condições:**
  - `request` não nulo.
  - Prontuário existe (`ObterPorIdAsync`, tenant-scoped), senão `InvalidOperationException("Prontuário não encontrado.")`.
  - Situação `Aberto` (I-3); `Servico` compatível com o tipo da unidade do prontuário (I-4 — PAIF↔CRAS, PAEFI↔CREAS).
- **Efeito:** `prontuario.RegistrarAtendimento(servico, dataAtendimento, descricao, profissionalId, tipoUnidade)`; `SaveChangesAsync`; publica `AtendimentoRegistradoIntegrationEvent(...)` via Outbox.
- **Pós-condições:** novo `RegistroAcompanhamento`; `AtendimentoRegistrado` (domínio) + Integration Event publicados (sem conteúdo sigiloso no payload).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, encerrado, ou serviço incompatível com a unidade).
- **Evento de domínio:** `AtendimentoRegistrado(Id, unidadeId, servico, dataAtendimento)`.
- **Evento de integração (publica):** `AtendimentoRegistradoIntegrationEvent`.

### 5.3 EncerrarAcompanhamento

- **Command:** `EncerrarAcompanhamentoCommand(Guid ProntuarioId, string MotivoEncerramento) : ICommand`.
- **Entrada (DTO):** `ProntuarioId`, `MotivoEncerramento`.
- **Dependências do handler:** `IProntuarioSuasRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Prontuário existe, senão `InvalidOperationException("Prontuário não encontrado.")`.
  - Situação `Aberto` e `MotivoEncerramento` não vazio (I-6).
- **Efeito:** `prontuario.EncerrarAcompanhamento(request.MotivoEncerramento, hoje)`; `SaveChangesAsync`; publica `AcompanhamentoEncerradoIntegrationEvent(...)` via Outbox.
- **Pós-condições:** situação `Encerrado`; `DataEncerramento`/`MotivoEncerramento` gravados; `AcompanhamentoEncerrado` (domínio) + Integration Event publicados.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (motivo vazio); `InvalidOperationException` (não encontrado ou já encerrado).
- **Evento de domínio:** `AcompanhamentoEncerrado(Id, motivoEncerramento)`.
- **Evento de integração (publica):** `AcompanhamentoEncerradoIntegrationEvent`.

### 5.4 RegistrarAcessoProntuario

- **Command:** `RegistrarAcessoProntuarioCommand(Guid ProntuarioId, Guid UsuarioId, string MotivoAcesso) : ICommand`.
- **Entrada (DTO):** `ProntuarioId`, `UsuarioId`, `MotivoAcesso` (justificativa obrigatória).
- **Dependências do handler:** `IProntuarioSuasRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Prontuário existe (tenant-scoped), senão `InvalidOperationException("Prontuário não encontrado.")`.
  - `MotivoAcesso` não vazio (I-7).
- **Efeito:** `prontuario.RegistrarAcesso(usuarioId, motivoAcesso, agoraUtc)` — **append-only** na trilha (`AcessoProntuario`); `SaveChangesAsync`.
- **Pós-condições:** novo `AcessoProntuario` imutável; nenhuma mudança na situação de acompanhamento; conteúdo **não** sai do tenant.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (motivo vazio); `InvalidOperationException` (não encontrado).
- **Evento de domínio:** — (trilha imutável; sem Integration Event para preservar sigilo — I-9).

> **Comandos de domínio existentes no agregado sem handler dedicado nesta versão:** `DefinirPlano(...)` e `RegistrarViolacao(...)` (métodos da raiz; exigem situação `Aberto` — I-12 — e seguem o mesmo regime de sigilo/auditoria; expostos para futura orquestração).

---

## 6. Consultas (leitura)

> **Toda leitura de conteúdo sigiloso DEVE registrar a trilha de acesso (I-7).** As consultas
> abaixo retornam metadados/resumo; o acesso ao **conteúdo** dispara `RegistrarAcessoProntuario`.

### 6.1 ObterProntuarioDaFamilia

- **Query:** `ObterProntuarioDaFamiliaQuery(Guid FamiliaId, Guid UsuarioId, string MotivoAcesso) : IQuery<ProntuarioDetalhe>`.
- **Entrada:** `FamiliaId`, `UsuarioId`, `MotivoAcesso` (obrigatório — leitura sigilosa).
- **Handler:** `ObterProntuarioDaFamiliaHandler(IProntuarioSuasRepository prontuarios, IPublisher publisher)`; localiza o prontuário (tenant-scoped); **antes de projetar o conteúdo**, registra o acesso na trilha (I-7) via comando/serviço de auditoria.
- **Projeção (DTO):** `ProntuarioDetalhe(Guid Id, Guid FamiliaId, Guid UnidadeAtendimentoId, string Situacao, DateOnly DataAbertura, IReadOnlyList<RegistroResumo> Registros, bool PossuiViolacaoCriancaAdolescente)` — conteúdo sigiloso, **somente para usuários autorizados e auditados**.
- **Filtros:** por `FamiliaId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`; conteúdo **nunca** cruza tenants (I-9).
- **Pré-condições:** `request` não nulo; `MotivoAcesso` não vazio (sem motivo ⇒ acesso negado).

### 6.2 ObterTrilhaAcessoProntuario

- **Query:** `ObterTrilhaAcessoProntuarioQuery(Guid ProntuarioId) : IQuery<IReadOnlyList<AcessoResumo>>`.
- **Entrada:** `ProntuarioId`.
- **Handler:** `ObterTrilhaAcessoProntuarioHandler(IProntuarioSuasRepository prontuarios)`; lista a trilha imutável (quem/quando/por quê) para o controle social/Tribunal de Contas.
- **Projeção (DTO):** `AcessoResumo(Guid AcessoId, Guid UsuarioId, string MotivoAcesso, DateTime DataHoraAcessoUtc)`.
- **Filtros:** por `ProntuarioId`; tenant-scoped.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`). Consultar a trilha **não** expõe conteúdo sigiloso do acompanhamento.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...AssistenciaSocial.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `AtendimentoRegistrado` | `(ProntuarioSuasId, Guid UnidadeId, TipoServico Servico, DateOnly DataAtendimento)` | `ProntuarioSuas.RegistrarAtendimento` |
| `AcompanhamentoEncerrado` | `(ProntuarioSuasId, string MotivoEncerramento)` | `ProntuarioSuas.EncerrarAcompanhamento` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...AssistenciaSocial.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `AtendimentoRegistradoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ProntuarioId, Guid UnidadeId, string Servico, DateOnly DataAtendimento)` | `RegistrarAtendimentoHandler` |
| `AcompanhamentoEncerradoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ProntuarioId, string MotivoEncerramento)` | `EncerrarAcompanhamentoHandler` |

> Payloads expõem apenas **identificadores e metadados** (serviço, datas) — **nunca** o conteúdo sigiloso (descrições de atendimento, violações de direitos, dados de crianças/adolescentes) — README §6, §7, §8 ⭐ (I-9). A trilha de acesso (`AcessoProntuario`) **não** gera Integration Event.

### Integração (consome)

- Nenhum nesta versão (o prontuário não consome Integration Events de outros módulos).

---

## 8. Validações (FluentValidation)

### AbrirProntuarioValidator (`AbstractValidator<AbrirProntuarioCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `FamiliaId` | `NotEmpty()` | "Família é obrigatória." |
| `UnidadeAtendimentoId` | `NotEmpty()` | "Unidade de atendimento é obrigatória." |

### RegistrarAtendimentoValidator (`AbstractValidator<RegistrarAtendimentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProntuarioId` | `NotEmpty()` | "Identificador do prontuário é obrigatório." |
| `Servico` | `IsInEnum()` | "Serviço (PAIF/PAEFI/SCFV) inválido." |
| `DataAtendimento` | `NotEmpty()` | "Data do atendimento é obrigatória." |
| `Descricao` | `NotEmpty()` + `MaximumLength(4000)` | "Descrição do atendimento é obrigatória." |
| `ProfissionalId` | `NotEmpty()` | "Profissional responsável é obrigatório." |

### EncerrarAcompanhamentoValidator (`AbstractValidator<EncerrarAcompanhamentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProntuarioId` | `NotEmpty()` | "Identificador do prontuário é obrigatório." |
| `MotivoEncerramento` | `NotEmpty()` + `MaximumLength(400)` | "Motivo do encerramento é obrigatório." |

### RegistrarAcessoProntuarioValidator (`AbstractValidator<RegistrarAcessoProntuarioCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProntuarioId` | `NotEmpty()` | "Identificador do prontuário é obrigatório." |
| `UsuarioId` | `NotEmpty()` | "Usuário do acesso é obrigatório." |
| `MotivoAcesso` | `NotEmpty()` + `MaximumLength(400)` | "Motivo de acesso ao prontuário é obrigatório." |

> `ObterTrilhaAcessoProntuarioQuery` não possui validador FluentValidation (proteção por `ArgumentNullException.ThrowIfNull`). `ObterProntuarioDaFamiliaQuery` exige `MotivoAcesso` não vazio (validador dedicado), pois é leitura sigilosa.

---

## 9. Persistência (EF Core 8)

- **Schema:** `assistenciasocial` (isolado por módulo). **DbContext:** o do módulo AssistenciaSocial. **Migrations:** por módulo.
- **Tabela:** `ProntuarioSuas` (raiz) + `RegistroAcompanhamento`, `PlanoAcompanhamentoFamiliar`, `ViolacaoDireito`, `AcessoProntuario` (entidades-filhas).

| Coluna (ProntuarioSuas) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `ProntuarioSuasId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `FamiliaId` | `Guid` | família acompanhada. |
| `UnidadeAtendimentoId` | `Guid` | CRAS/CREAS responsável. |
| `Situacao` | `int` | enum `SituacaoProntuario`. |
| `DataAbertura` | `date` | `DateOnly`. |
| `MotivoEncerramento` | `nvarchar(400)?` | preenchido quando `Encerrado`. |
| `DataEncerramento` | `date?` | `DateOnly?`. |

| Coluna (RegistroAcompanhamento) | Tipo lógico | Observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `RegistroAcompanhamentoId`. |
| `ProntuarioId` | `Guid` (FK) | pertence ao prontuário (cascade). |
| `Servico` | `int` | enum `TipoServico`. |
| `DataAtendimento` | `date` | `DateOnly`. |
| `Descricao` | `nvarchar(4000)` | **conteúdo sigiloso** (LGPD art. 11). |
| `ProfissionalId` | `Guid` | autor do registro. |

| Coluna (ViolacaoDireito) | Tipo lógico | Observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `ViolacaoId`. |
| `ProntuarioId` | `Guid` (FK) | pertence ao prontuário. |
| `TipoViolacao` | `int` | enum. |
| `EnvolveCriancaAdolescente` | `bit` | **dado sensível reforçado** (art. 11 LGPD). |
| `DataIdentificacao` | `date` | `DateOnly`. |

| Coluna (AcessoProntuario) | Tipo lógico | Observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `AcessoId`. |
| `ProntuarioId` | `Guid` (FK) | trilha do prontuário. |
| `UsuarioId` | `Guid` | quem acessou. |
| `MotivoAcesso` | `nvarchar(400)` | por quê acessou. |
| `DataHoraAcessoUtc` | `datetime2` | quando acessou. **Append-only** (sem update/delete). |

- **Índices:**
  - PK em `Id` (todas as tabelas).
  - Índice **único** em `(TenantId, FamiliaId)` (um prontuário ativo por família por tenant — unicidade de acompanhamento).
  - Índice em `RegistroAcompanhamento(ProntuarioId)`, `ViolacaoDireito(ProntuarioId)`, `AcessoProntuario(ProntuarioId, DataHoraAcessoUtc)`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto AssistenciaSocial para `AtendimentoRegistradoIntegrationEvent` e `AcompanhamentoEncerradoIntegrationEvent`.
- **Imutabilidade da trilha:** `AcessoProntuario` é **append-only**; o mapeamento/infra impede update/delete (auditoria exigível pelo TCE-RS).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `ProntuarioSuas` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Abertura/atendimento/encerramento: papéis da **equipe técnica de referência** (ex.: `AssistenciaSocial.Prontuario.Acompanhar`).
  - Leitura do conteúdo sigiloso: papel restrito (ex.: `AssistenciaSocial.Prontuario.Ler`) **e sempre auditada** (I-7).
  - Leitura da trilha de acesso: papel de **controle/auditoria** (ex.: `AssistenciaSocial.Prontuario.Auditar`).
  - Módulo AssistenciaSocial é **ativável por tenant** (Executivo); requisição a tenant sem licença → 404/403 auditado.
- **Sigilo profissional & LGPD — dado sensível (art. 11):** o prontuário contém vulnerabilidade social, saúde, deficiência e **crianças/adolescentes**. Base legal: **execução de política pública** (art. 11, II, "b" / art. 23) — **não** consentimento. Princípios de **minimização** e finalidade. **Trilha de acesso ao prontuário** (quem leu, quando, por quê) **imutável**, exigível pelo controle social/Tribunal de Contas (README §6, §8 ⭐).
- **Isolamento de conteúdo:** o conteúdo sigiloso **NÃO trafega entre tenants** nem para módulos não autorizados; Integration Events expõem apenas metadados (I-9).
- **Auditoria imutável:** além da trilha de acesso dedicada (`AcessoProntuario`), o `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em `Abrir`, `RegistrarAtendimento`, `DefinirPlano`, `RegistrarViolacao`, `EncerrarAcompanhamento`.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Prontuário Eletrônico Simplificado (MDS) — interoperabilidade:** o acompanhamento familiar pode interoperar com o Prontuário Eletrônico Simplificado do MDS (README §4). `HttpClient` tipado + **Polly** (retry + circuit breaker + timeout) atrás de **ACL**; idempotente; **sem** envio de conteúdo sigiloso identificável além do estritamente necessário e autorizado.
- **RMA (consolidação de atendimentos):** os `AtendimentoRegistrado` deste prontuário alimentam o **Registro Mensal de Atendimentos** (agregado `RegistroMensal`/RMA) por unidade/competência, para exportação ao SAGI/MDS — consumido intra-módulo a partir de `AtendimentoRegistradoIntegrationEvent` (dados agregados, não conteúdo).
- **Saída — cross-module via Contracts:** `AtendimentoRegistradoIntegrationEvent`/`AcompanhamentoEncerradoIntegrationEvent` (Outbox) disponíveis a consumidores autorizados, **somente com metadados** (sem conteúdo sigiloso — I-9).
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Abertura de prontuário**
- **Dado** uma família referenciada e uma `UnidadeAtendimento` existentes no tenant
- **Quando** executo `AbrirProntuarioCommand(familiaId, unidadeId)`
- **Então** é criado um `ProntuarioSuas` em situação `Aberto` com `DataAbertura` = hoje (I-1, I-2).

**Cenário 2 — Registro de atendimento PAIF em CRAS** (README §8)
- **Dado** um prontuário `Aberto` vinculado a uma unidade **CRAS**
- **Quando** executo `RegistrarAtendimentoCommand(prontuarioId, Paif, hoje, descricao, profissionalId)`
- **Então** um `RegistroAcompanhamento` é adicionado, `AtendimentoRegistrado` (domínio) + `AtendimentoRegistradoIntegrationEvent` (só metadados) são publicados (I-4, I-5).

**Cenário 3 — Oferta de serviço incompatível com a unidade** (README §8)
- **Dado** um prontuário vinculado a uma unidade **CRAS**
- **Quando** se tenta registrar atendimento **PAEFI** (exclusivo de CREAS)
- **Então** a operação é rejeitada por invariante de domínio (`InvalidOperationException`) e nenhum evento é publicado (I-4).

**Cenário 4 — Atendimento sobre prontuário encerrado**
- **Dado** um prontuário `Encerrado`
- **Quando** executo `RegistrarAtendimentoCommand`
- **Então** ocorre `InvalidOperationException` (prontuário encerrado não admite novos atendimentos — I-3).

**Cenário 5 — Encerramento do acompanhamento**
- **Dado** um prontuário `Aberto`
- **Quando** executo `EncerrarAcompanhamentoCommand(prontuarioId, "objetivos alcançados")`
- **Então** a situação passa a `Encerrado`, `DataEncerramento` é gravada e `AcompanhamentoEncerrado` + `AcompanhamentoEncerradoIntegrationEvent` são publicados (I-6).

**Cenário 6 — Encerramento sem motivo**
- **Dado** um prontuário `Aberto`
- **Quando** executo `EncerrarAcompanhamentoCommand(prontuarioId, "")`
- **Então** a validação/`ArgumentException` rejeita (motivo obrigatório — I-6).

**⭐ Cenário 7 — Acesso sigiloso ao prontuário** (README §8 ⭐)
- **Dado** um `ProntuarioSuas` com dados sensíveis de violação de direitos de criança
- **Quando** um profissional autorizado o acessa informando `MotivoAcesso`
- **Então** o acesso é permitido **somente sob autorização** e **registrado na trilha** (quem/quando/por quê), de forma imutável, **sem trafegar o conteúdo a outros tenants** (I-7, I-8, I-9).

**Cenário 8 — Acesso sem motivo é negado**
- **Dado** um prontuário sigiloso
- **Quando** se tenta ler o conteúdo (`ObterProntuarioDaFamiliaQuery`) **sem** `MotivoAcesso`
- **Então** o acesso é negado (validação exige motivo) e nada é projetado (I-7).

**Cenário 9 — Trilha de acesso é imutável**
- **Dado** um prontuário com acessos registrados
- **Quando** se tenta alterar/remover um `AcessoProntuario`
- **Então** a operação é impedida (append-only); a trilha permanece íntegra para o controle social/TCE-RS (I-8).

**Cenário 10 — Conteúdo sigiloso não cruza tenants**
- **Dado** um prontuário no tenant A
- **Quando** executo `ObterProntuarioDaFamiliaQuery` no contexto do tenant B
- **Então** o conteúdo não é retornado (Global Query Filter por `TenantId`); nenhum Integration Event carrega conteúdo sigiloso (I-9).

**Cenário 11 — Consulta da trilha de acesso**
- **Dado** um prontuário com diversos acessos auditados no tenant A
- **Quando** o controle executa `ObterTrilhaAcessoProntuarioQuery(prontuarioId)` no tenant A
- **Então** retorna a lista imutável de `AcessoResumo` (usuário, motivo, data/hora) — sem expor o conteúdo do acompanhamento.

---

## 13. Casos de Borda

- **B-1.** `FamiliaId`/`UnidadeAtendimentoId` vazios em `Abrir` ⇒ validação `NotEmpty`/`ArgumentNullException` (I-1).
- **B-2.** Registrar `Paif` em unidade **CREAS** ⇒ `InvalidOperationException` (I-4); idem `Paefi` em **CRAS**.
- **B-3.** `Scfv` é admissível conforme a oferta da unidade; incompatibilidade segue a mesma invariante de oferta (I-4).
- **B-4.** Encerrar prontuário já `Encerrado` ⇒ `InvalidOperationException` (estado terminal — I-11).
- **B-5.** `RegistrarAtendimento`/`DefinirPlano`/`RegistrarViolacao` após encerramento ⇒ rejeitado (I-3, I-12).
- **B-6.** Leitura de conteúdo **sem** `MotivoAcesso` ⇒ negada (I-7); nenhum conteúdo projetado.
- **B-7.** Tentativa de `update`/`delete` em `AcessoProntuario` ⇒ impedida (append-only — I-8).
- **B-8.** Integration Event com descrição de atendimento/violação ⇒ **proibido**; payload restrito a metadados (I-9; README §7).
- **B-9.** `ViolacaoDireito` com `EnvolveCriancaAdolescente = true` ⇒ dado sensível reforçado; acesso sempre auditado e minimizado (I-10).
- **B-10.** Abrir segundo prontuário para a mesma família no mesmo tenant ⇒ impedido pelo índice único `(TenantId, FamiliaId)` (unicidade de acompanhamento ativo).
- **B-11.** `Descricao` acima de 4000 caracteres ⇒ rejeitada pelo validator (`MaximumLength(4000)`).
- **B-12.** `AtendimentoRegistradoIntegrationEvent`/`AcompanhamentoEncerradoIntegrationEvent` devem ser idempotentes no consumidor por `EventId` (reentrega via Outbox).
- **B-13.** Acesso de profissional **sem** papel restrito de leitura ⇒ negado por RBAC (negar por padrão), e a tentativa é auditada.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo AssistenciaSocial (§§1-9). Acompanhamento familiar sigiloso PAIF/PAEFI; invariante PAIF↔CRAS / PAEFI↔CREAS; trilha de acesso imutável (quem/quando/por quê); isolamento de conteúdo sigiloso por tenant (sem conteúdo no barramento). |

<!-- manifest
commands: AbrirProntuario, RegistrarAtendimento, EncerrarAcompanhamento, RegistrarAcessoProntuario
queries: ObterProntuarioDaFamilia, ObterTrilhaAcessoProntuario
domainEvents: AtendimentoRegistrado, AcompanhamentoEncerrado
integrationEventsPublished: AtendimentoRegistradoIntegrationEvent, AcompanhamentoEncerradoIntegrationEvent
integrationEventsConsumed: 
-->

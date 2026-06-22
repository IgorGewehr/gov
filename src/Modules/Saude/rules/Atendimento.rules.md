---
modulo: Saude
agregado: Atendimento
contexto: Saude (Atenção à Saúde municipal — SUS; PEP/e-SUS APS, regulação, farmácia e imunização)
poder: Executivo
schema: saude
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF arts. 196-200", "Lei 8.080/1990", "PNAB Portaria GM/MS 2.436/2017", "Portaria 1.412/2013 (SISAB/e-SUS APS)", "Portaria 1.434/2020 (RNDS)", "Resolucao CFM 1.821/2007", "Lei 13.787/2018 (prontuario eletronico, guarda 20 anos)", "Lei 14.510/2022 (telessaude)", "LGPD art. 11, II, f (tutela da saude)"]
---

# Atendimento — Regras-as-Code (Rules-as-Code)

> Registro clínico de um encontro assistencial (consulta/atendimento na APS) de um `Paciente`
> em um `Estabelecimento`, com evolução **SOAP**, prescrições e solicitações de exame. É
> **append-only**: evolução assinada não se edita — só **adendo datado/reassinado**. A eliminação
> do papel exige **NGS2 + ICP-Brasil**, com **guarda mínima de 20 anos** (Lei 13.787/2018). Após
> assinado, gera Bundle **FHIR R4** para a **RNDS** e produção para o **SISAB**. Trata dado pessoal
> **sensível** (LGPD art. 11). Este arquivo é **normativo e versionado**; o código (`Atendimento.cs`,
> handlers, validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Atendimento (`Atendimento`) | Encontro assistencial registrado no PEP. Raiz de agregado. |
| Evolução SOAP (`EvolucaoSOAP`) | Nota clínica estruturada (Subjetivo, Objetivo, Avaliação, Plano). Entidade. |
| Prescrição (`Prescricao`) | Item prescrito no atendimento (medicamento/conduta). Entidade. |
| Solicitação de Exame (`SolicitacaoExame`) | Pedido de exame/procedimento vinculado ao atendimento. Entidade. |
| CID (`Cid` : `Cid`) | Classificação Internacional de Doenças (CID-10) — diagnóstico clínico. VO. |
| CIAP (`Ciap` : `Ciap`) | Classificação Internacional de Atenção Primária (CIAP-2). VO. |
| Assinatura Digital (`AssinaturaDigital` : VO) | Assinatura ICP-Brasil (NGS2) da evolução/atendimento. |
| Registrar Atendimento (`RegistrarAtendimento`) | Ato de abrir o registro clínico do encontro. |
| Adicionar Evolução (`AdicionarEvolucaoSOAP`) | Inclusão de uma nota SOAP no atendimento. |
| Adendo (`AdicionarAdendo`) | Nota datada e reassinada que **complementa** uma evolução já assinada (não a edita). |
| Assinar (`Assinar` / `AssinarAtendimento`) | Assinatura digital NGS2 (ICP-Brasil) que torna o registro imutável. |
| Compartilhar na RNDS (`CompartilharNaRNDS`) | Envio do RES como Bundle FHIR R4 via mTLS + ICP-Brasil. |
| Lançar no SISAB (`LancarNoSISAB`) | Envio da produção (CDS/e-SUS APS) na competência. |
| NGS1 / NGS2 (`NivelGarantia` : `NivelGarantia`) | Níveis de garantia de segurança do prontuário (CFM 1.821/2007). |
| Competência (`Competencia` : `Competencia`) | Mês de referência (AAAA-MM) da produção SISAB. |
| Telessaúde / Teleconsulta (`Modalidade` : `ModalidadeAtendimento`) | Atendimento presencial ou remoto regulado (Lei 14.510/2022). |
| Situação (`Situacao` : `SituacaoAtendimento`) | Estado atual do atendimento no ciclo clínico. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Secretaria de Saúde) dono do registro. |

---

## 2. Modelo

- **Identidade:** `AtendimentoId` — `readonly record struct AtendimentoId(Guid Value)`; fábrica `AtendimentoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Atendimento : AggregateRoot<AtendimentoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `Registrar(...)`.
- **Referências cross-aggregate por Id** (sem navegação): `PacienteId`, `EstabelecimentoId`, `ProfissionalId` (regra: "Atendimento referencia Paciente/Estabelecimento por Id").

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `PacienteId` | `PacienteId` (VO/Id) | Paciente atendido (referência por Id). | `private set` |
| `EstabelecimentoId` | `EstabelecimentoId` (VO/Id) | Estabelecimento (CNES) do atendimento. | `private set` |
| `ProfissionalId` | `ProfissionalId` (VO/Id) | Profissional responsável (CBO ativo na competência). | `private set` |
| `DataHora` | `DateTimeOffset` | Data/hora do atendimento. | `private set` |
| `Competencia` | `Competencia` (VO) | Competência (AAAA-MM) para o SISAB. | `private set` |
| `Modalidade` | `ModalidadeAtendimento` | Presencial ou Teleconsulta. | `private set` |
| `Diagnostico` | `Cid?` / `Ciap?` | Diagnóstico (CID-10 clínico e/ou CIAP-2 na APS). | `private set` |
| `NivelGarantia` | `NivelGarantia` | NGS1/NGS2 (NGS2 exigido para eliminar papel). | `private set` |
| `Assinatura` | `AssinaturaDigital?` | Assinatura ICP-Brasil (nula até assinar). | `private set` |
| `Situacao` | `SituacaoAtendimento` | Situação atual. | `private set` |
| `Evolucoes` | `IReadOnlyCollection<EvolucaoSOAP>` | Notas SOAP/adendos (append-only). | coleção encapsulada |
| `Prescricoes` | `IReadOnlyCollection<Prescricao>` | Prescrições do atendimento. | coleção encapsulada |
| `SolicitacoesExame` | `IReadOnlyCollection<SolicitacaoExame>` | Solicitações de exame. | coleção encapsulada |

### Value Objects (referenciados)

- `Cid` — `readonly record struct` com `Codigo : string` (CID-10). Formato validado.
- `Ciap` — `readonly record struct` com `Codigo : string` (CIAP-2).
- `AssinaturaDigital` — owned/VO: `CertificadoIcpBrasil` (identificação), `Hash`, `Carimbo : DateTimeOffset`, `Nivel : NivelGarantia` (NGS2).
- `Competencia` — `readonly record struct` com `Ano : int`, `Mes : int`; `ToString()` => `AAAA-MM`.

### Entidades do agregado

- `EvolucaoSOAP` — `Id`, `Subjetivo`, `Objetivo`, `Avaliacao`, `Plano`, `DataHora`, `Assinada : bool`, ` ehAdendo : bool`, `EvolucaoReferenciadaId?` (adendo aponta para a evolução complementada).
- `Prescricao` — `Id`, `Item`, `Posologia`, `DataHora`.
- `SolicitacaoExame` — `Id`, `Procedimento`, `Justificativa`, `DataHora`.

### Enum `SituacaoAtendimento`

| Valor | Numérico | Descrição |
|---|---|---|
| `EmAndamento` | 1 | Registro aberto, ainda editável (estado inicial). |
| `Assinado` | 2 | Assinado em ICP-Brasil — **imutável** (somente adendo). |
| `Compartilhado` | 3 | RES enviado e aceito na RNDS. |
| `Cancelado` | 4 | Cancelado antes da assinatura — terminal. |

### Enum `NivelGarantia`

| Valor | Numérico | Descrição |
|---|---|---|
| `NGS1` | 1 | Nível de garantia 1 (não elimina papel). |
| `NGS2` | 2 | Nível de garantia 2 (ICP-Brasil) — habilita eliminação do papel. |

### Enum `ModalidadeAtendimento`

| Valor | Numérico | Descrição |
|---|---|---|
| `Presencial` | 1 | Atendimento presencial. |
| `Teleconsulta` | 2 | Atendimento remoto regulado (Lei 14.510/2022). |

> **Conjuntos de referência usados nas guardas:**
> - **Editável** = { `EmAndamento` } — única situação que admite edição/inclusão direta de evolução.
> - **Imutável** = { `Assinado`, `Compartilhado` } — só admite **adendo** datado/reassinado.
> - **Encerrado** = { `Cancelado` } (e, para fins de cancelamento, `Compartilhado` não pode ser cancelado).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** O atendimento exige **CNS válido** do paciente (paciente com `CnsConfirmado == true`) **e** CNES e profissional/CBO ativos na competência; sem isso, `Registrar` falha e nenhum evento é publicado. (Regra crítica / Cenário 1)
- **I-2.** Registro clínico é **append-only**: uma `EvolucaoSOAP` com `Assinada == true` **não** pode ser editada; alterações só por **adendo datado/reassinado** (`AdicionarAdendo`). Tentar editar lança `InvalidOperationException`. (Regra crítica / Cenário 2)
- **I-3.** A assinatura (`Assinar`) exige situação `EmAndamento`, ao menos uma `EvolucaoSOAP`, e `AssinaturaDigital` ICP-Brasil válida; ao assinar, passa a `Assinado` e a evolução corrente é marcada `Assinada = true`.
- **I-4.** A **eliminação do papel** só é válida com `NivelGarantia == NGS2` + ICP-Brasil; **guarda mínima de 20 anos** do registro (Lei 13.787/2018) — o registro nunca é apagado fisicamente antes desse prazo.
- **I-5.** `CompartilharNaRNDS` exige situação `Assinado` em **NGS2**; gera Bundle FHIR R4 e, ao sucesso, passa a `Compartilhado` e publica `RESCompartilhadoNaRNDS`. (Cenário 3)
- **I-6.** `LancarNoSISAB` exige situação `Assinado`/`Compartilhado` e `Competencia` definida; envia a produção na competência (Portaria 1.412/2013) e emite `PEPLancadoNoSISAB`.
- **I-7.** Atendimento `Assinado`/`Compartilhado` **não** admite adicionar/remover `Prescricao`/`SolicitacaoExame` nem nova `EvolucaoSOAP` direta — apenas adendo (I-2).
- **I-8.** `AdicionarEvolucaoSOAP` (direta) exige situação `EmAndamento`; cada campo SOAP relevante não pode estar todo vazio (ao menos um preenchido).
- **I-9.** `Modalidade == Teleconsulta` exige profissional com **CRM ativo** e direito a atendimento presencial (Lei 14.510/2022); validado na orquestração/ACL.
- **I-10.** O `Cancelar` só é permitido na situação `EmAndamento` (atendimento **não assinado**); atendimento `Assinado`/`Compartilhado` **não** se cancela (correção apenas por adendo). 
- **I-11.** Na criação, situação inicial é `EmAndamento` e é emitido o evento `AtendimentoRegistrado(id, pacienteId, estabelecimentoId)`.
- **I-12.** Estados `Compartilhado` e `Cancelado` não admitem novas transições de ciclo (terminais para o fluxo de cobrança clínica; `Compartilhado` ainda admite `LancarNoSISAB`).
- **I-13.** Dado clínico é **sensível** (LGPD art. 11): toda leitura registra **trilha de acesso ao prontuário** (quem, quando, qual CNS) e exige claim `saude.atender`.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Registrar` | `EmAndamento` | CNS confirmado && CNES/CBO ativos na competência | `AtendimentoRegistrado` |
| `EmAndamento` | `AdicionarEvolucaoSOAP` | `EmAndamento` | situação == `EmAndamento`; SOAP não vazio | — |
| `EmAndamento` | `AdicionarPrescricao` | `EmAndamento` | situação == `EmAndamento` | — |
| `EmAndamento` | `AdicionarSolicitacaoExame` | `EmAndamento` | situação == `EmAndamento` | — |
| `EmAndamento` | `Assinar` | `Assinado` | ≥1 evolução; assinatura ICP-Brasil válida | `AtendimentoAssinado` |
| `EmAndamento` | `Cancelar` | `Cancelado` | situação == `EmAndamento` (não assinado) | `AtendimentoCancelado` |
| `Assinado` \| `Compartilhado` | `AdicionarAdendo` | (mantém) | evolução referenciada assinada; adendo reassinado | `AdendoRegistrado` |
| `Assinado` | `CompartilharNaRNDS` | `Compartilhado` | NGS2; Bundle FHIR R4 aceito | `RESCompartilhadoNaRNDS` |
| `Assinado` \| `Compartilhado` | `LancarNoSISAB` | (mantém) | competência definida | `PEPLancadoNoSISAB` |

> Observações:
> - `AdicionarEvolucaoSOAP`/`AdicionarPrescricao`/`AdicionarSolicitacaoExame` chamam `GarantirEmAndamento()` antes do efeito.
> - `AdicionarAdendo` chama `GarantirAssinado()` (situação ∈ {`Assinado`,`Compartilhado`}) e exige reassinatura do adendo; **não** altera a evolução original (append-only — I-2).
> - `CompartilharNaRNDS` exige `NivelGarantia == NGS2` (I-4/I-5).
> - `LancarNoSISAB` não altera a `Situacao` (efeito de produção), apenas emite o evento de domínio.

---

## 5. Comandos (escrita)

### 5.1 RegistrarAtendimento

- **Command:** `RegistrarAtendimentoCommand(Guid PacienteId, Guid EstabelecimentoId, Guid ProfissionalId, DateTimeOffset DataHora, ModalidadeAtendimento Modalidade) : ICommand<Guid>`.
- **Entrada (DTO):** `PacienteId`, `EstabelecimentoId`, `ProfissionalId`, `DataHora`, `Modalidade`.
- **Dependências do handler:** `IPacienteRepository`, `IEstabelecimentoRepository`, `IAtendimentoRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Paciente existe e tem `CnsConfirmado == true`, senão `InvalidOperationException("Paciente sem CNS válido/confirmado.")` (I-1).
  - Estabelecimento (CNES) existe e está ativo; profissional/CBO ativo na competência, senão `InvalidOperationException` (I-1).
  - Se `Modalidade == Teleconsulta`: profissional com CRM ativo (I-9).
- **Efeito:** calcula `Competencia` da `DataHora`; cria via `Atendimento.Registrar(tenant.TenantId, pacienteId, estabelecimentoId, profissionalId, dataHora, competencia, modalidade)`; `atendimentos.Adicionar(atendimento)`; `SaveChangesAsync`.
- **Pós-condições:** novo `Atendimento` em situação `EmAndamento`; retorna `atendimento.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (CNS inválido, CNES/CBO inativo, CRM inativo em teleconsulta).
- **Evento de domínio:** `AtendimentoRegistrado(id, pacienteId, estabelecimentoId)` (emitido no construtor via factory).

### 5.2 AdicionarEvolucaoSOAP

- **Command:** `AdicionarEvolucaoSOAPCommand(Guid AtendimentoId, string Subjetivo, string Objetivo, string Avaliacao, string Plano, string? Cid, string? Ciap) : ICommand`.
- **Entrada (DTO):** `AtendimentoId`, campos SOAP, `Cid?`/`Ciap?`.
- **Dependências do handler:** `IAtendimentoRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo; atendimento existe (senão `InvalidOperationException("Atendimento não encontrado.")`).
  - Situação == `EmAndamento` (I-8).
- **Efeito:** `atendimento.AdicionarEvolucaoSOAP(...)`; `SaveChangesAsync`.
- **Pós-condições:** nova `EvolucaoSOAP` (`Assinada = false`) na coleção `Evolucoes`.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (SOAP totalmente vazio); `InvalidOperationException` (não encontrado ou não `EmAndamento`).
- **Evento de domínio:** — (nenhum específico nesta versão).

### 5.3 AssinarAtendimento

- **Command:** `AssinarAtendimentoCommand(Guid AtendimentoId, string CertificadoIcpBrasil, string Hash) : ICommand`.
- **Entrada (DTO):** `AtendimentoId`, dados da assinatura ICP-Brasil (NGS2).
- **Dependências do handler:** `IAtendimentoRepository`, `IAssinaturaIcpBrasilService`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo; atendimento existe.
  - Situação == `EmAndamento`; ≥ 1 `EvolucaoSOAP`; assinatura ICP-Brasil válida (NGS2) (I-3/I-4).
- **Efeito:** valida certificado; `atendimento.Assinar(assinaturaDigital)`; `SaveChangesAsync`; (Outbox publica `AtendimentoAssinado` Integration Event).
- **Pós-condições:** situação `Assinado`; evolução corrente `Assinada = true`; registro torna-se **imutável** (somente adendo).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, sem evolução, situação ≠ `EmAndamento` ou assinatura inválida).
- **Evento de domínio:** `AtendimentoAssinado(Id)`.
- **Evento de integração (publica):** `AtendimentoAssinadoIntegrationEvent`.

### 5.4 AdicionarAdendo

- **Command:** `AdicionarAdendoCommand(Guid AtendimentoId, Guid EvolucaoReferenciadaId, string Texto, string CertificadoIcpBrasil, string Hash) : ICommand`.
- **Entrada (DTO):** `AtendimentoId`, `EvolucaoReferenciadaId`, `Texto`, assinatura ICP-Brasil.
- **Dependências do handler:** `IAtendimentoRepository`, `IAssinaturaIcpBrasilService`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo; atendimento existe.
  - Situação ∈ {`Assinado`,`Compartilhado`}; `EvolucaoReferenciadaId` aponta para evolução assinada existente (I-2).
- **Efeito:** `atendimento.AdicionarAdendo(evolucaoReferenciadaId, texto, assinaturaDigital, agora)`; `SaveChangesAsync`.
- **Pós-condições:** nova `EvolucaoSOAP` (`ehAdendo = true`, `Assinada = true`) referenciando a original; a evolução original permanece **inalterada**.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (texto vazio); `InvalidOperationException` (não encontrado, situação não assinada, evolução referenciada inexistente/não assinada).
- **Evento de domínio:** `AdendoRegistrado(Id, evolucaoReferenciadaId)`.

### 5.5 CompartilharAtendimentoNaRNDS

- **Command:** `CompartilharAtendimentoNaRNDSCommand(Guid AtendimentoId) : ICommand`.
- **Entrada (DTO):** `AtendimentoId`.
- **Dependências do handler:** `IAtendimentoRepository`, `IRndsGateway` (FHIR R4, mTLS+ICP-Brasil), `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo; atendimento existe.
  - Situação == `Assinado` em **NGS2** (I-5).
- **Efeito:** monta Bundle FHIR R4; `IRndsGateway.EnviarBundleAsync(...)` (mTLS); ao sucesso `atendimento.CompartilharNaRNDS(protocoloRnds)`; `SaveChangesAsync`; (Outbox publica `RESCompartilhadoNaRNDS`).
- **Pós-condições:** situação `Compartilhado`; protocolo RNDS registrado.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, não assinado/NGS1, ou rejeição da RNDS após retries).
- **Evento de domínio:** `RESCompartilhadoNaRNDS(Id)`.
- **Evento de integração (publica):** `RESCompartilhadoNaRNDSIntegrationEvent`.

### 5.6 LancarAtendimentoNoSISAB

- **Command:** `LancarAtendimentoNoSISABCommand(Guid AtendimentoId) : ICommand`.
- **Entrada (DTO):** `AtendimentoId`.
- **Dependências do handler:** `IAtendimentoRepository`, `ISisabGateway` (CDS/e-SUS APS), `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo; atendimento existe.
  - Situação ∈ {`Assinado`,`Compartilhado`}; `Competencia` definida (I-6).
- **Efeito:** envia produção da `Competencia` ao SISAB (`ISisabGateway.EnviarProducaoAsync`); `atendimento.LancarNoSISAB(competencia)`; `SaveChangesAsync`.
- **Pós-condições:** produção lançada na competência (situação inalterada).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou não assinado).
- **Evento de domínio:** `PEPLancadoNoSISAB(Id, competencia)`.

### 5.7 CancelarAtendimento

- **Command:** `CancelarAtendimentoCommand(Guid AtendimentoId, string Motivo) : ICommand`.
- **Entrada (DTO):** `AtendimentoId`, `Motivo`.
- **Dependências do handler:** `IAtendimentoRepository`, `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo; atendimento existe.
  - Situação == `EmAndamento` (não assinado) (I-10).
- **Efeito:** `atendimento.Cancelar(request.Motivo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Cancelado`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou já assinado/compartilhado).
- **Evento de domínio:** `AtendimentoCancelado(Id, motivo)`.

> **Comandos de domínio expostos na raiz (orquestrados conforme caso de uso):** `AdicionarPrescricao()`, `AdicionarSolicitacaoExame()` — efeito apenas em situação `EmAndamento`.

---

## 6. Consultas (leitura)

### 6.1 ObterAtendimentoPorId

- **Query:** `ObterAtendimentoPorIdQuery(Guid AtendimentoId) : IQuery<AtendimentoDetalhe?>`.
- **Entrada:** `AtendimentoId`.
- **Handler:** `ObterAtendimentoPorIdHandler(IAtendimentoRepository atendimentos)`; chama `atendimentos.ObterPorIdAsync(new AtendimentoId(request.AtendimentoId), ct)`.
- **Projeção (DTO):** `AtendimentoDetalhe(Guid Id, Guid PacienteId, Guid EstabelecimentoId, Guid ProfissionalId, DateTimeOffset DataHora, string Competencia, string Modalidade, string Situacao, string NivelGarantia, bool Assinado, IReadOnlyList<EvolucaoDto> Evolucoes, IReadOnlyList<PrescricaoDto> Prescricoes, IReadOnlyList<SolicitacaoExameDto> Exames)`.
- **Filtros:** por `AtendimentoId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo.
- **Segurança:** **dado sensível** (LGPD art. 11); claim `saude.atender`; **trilha de acesso ao prontuário** obrigatória.

### 6.2 ListarAtendimentosDoPaciente

- **Query:** `ListarAtendimentosDoPacienteQuery(Guid PacienteId, DateOnly? De, DateOnly? Ate) : IQuery<IReadOnlyList<AtendimentoResumo>>`.
- **Entrada:** `PacienteId`, intervalo opcional.
- **Handler:** `ListarAtendimentosDoPacienteHandler(IAtendimentoRepository atendimentos)`; chama `atendimentos.ListarPorPacienteAsync(new PacienteId(request.PacienteId), request.De, request.Ate, ct)`.
- **Projeção (DTO):** `AtendimentoResumo(Guid Id, DateTimeOffset DataHora, string Modalidade, string Situacao, string? Cid, string? Ciap)`.
- **Filtros:** por `PacienteId` + intervalo; **sempre tenant-scoped**.
- **Pré-condições:** `request` não nulo.
- **Segurança:** claim `saude.atender`; **trilha de acesso** registra quem, quando e qual CNS.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Saude.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `AtendimentoRegistrado` | `(AtendimentoId, PacienteId, EstabelecimentoId)` | `Atendimento.Registrar` (construtor) |
| `AtendimentoAssinado` | `(AtendimentoId)` | `Atendimento.Assinar` |
| `AdendoRegistrado` | `(AtendimentoId, Guid evolucaoReferenciadaId)` | `Atendimento.AdicionarAdendo` |
| `RESCompartilhadoNaRNDS` | `(AtendimentoId)` | `Atendimento.CompartilharNaRNDS` |
| `PEPLancadoNoSISAB` | `(AtendimentoId, Competencia)` | `Atendimento.LancarNoSISAB` |
| `AtendimentoCancelado` | `(AtendimentoId, string motivo)` | `Atendimento.Cancelar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Saude.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `AtendimentoAssinadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid AtendimentoId, Guid PacienteId)` | `AssinarAtendimentoHandler` |
| `RESCompartilhadoNaRNDSIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid AtendimentoId, string ProtocoloRnds)` | `CompartilharAtendimentoNaRNDSHandler` (analítica/**Transparencia** agregada/anonimizada) |

> O README indica que a publicação para **Transparencia** usa **dados agregados/anonimizados**; os eventos de integração carregam apenas identificadores e nunca conteúdo clínico bruto.

### Integração (consome)

- `FornecedorHabilitado` (**Administracao**) — para credenciar fornecedor de insumos de saúde (consumo no contexto do módulo; não muda o estado do agregado `Atendimento`).
- `BemIncorporado` (**Patrimonio**) — para vincular equipamento à UBS/estabelecimento (consumo no contexto do módulo).

> Estes consumos pertencem ao **bounded context Saude** (não ao agregado `Atendimento` em si), mas são listados no manifesto do módulo via este agregado por serem os Integration Events consumidos declarados no README (§7).

---

## 8. Validações (FluentValidation)

### RegistrarAtendimentoValidator (`AbstractValidator<RegistrarAtendimentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `PacienteId` | `NotEmpty()` | "Paciente é obrigatório." |
| `EstabelecimentoId` | `NotEmpty()` | "Estabelecimento (CNES) é obrigatório." |
| `ProfissionalId` | `NotEmpty()` | "Profissional é obrigatório." |
| `DataHora` | `NotEmpty()` | "Data/hora do atendimento é obrigatória." |
| `Modalidade` | `IsInEnum()` | "Modalidade inválida." |

### AdicionarEvolucaoSOAPValidator (`AbstractValidator<AdicionarEvolucaoSOAPCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `AtendimentoId` | `NotEmpty()` | "Atendimento é obrigatório." |
| (SOAP) | `Must(AoMenosUmCampoPreenchido)` | "Informe ao menos um campo SOAP." |
| `Cid` | `Must(SerCidValido).When(cid != null)` | "CID-10 inválido." |
| `Ciap` | `Must(SerCiapValido).When(ciap != null)` | "CIAP-2 inválido." |

### AssinarAtendimentoValidator (`AbstractValidator<AssinarAtendimentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `AtendimentoId` | `NotEmpty()` | "Atendimento é obrigatório." |
| `CertificadoIcpBrasil` | `NotEmpty()` | "Certificado ICP-Brasil (NGS2) é obrigatório." |
| `Hash` | `NotEmpty()` | "Hash da assinatura é obrigatório." |

### AdicionarAdendoValidator (`AbstractValidator<AdicionarAdendoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `AtendimentoId` | `NotEmpty()` | "Atendimento é obrigatório." |
| `EvolucaoReferenciadaId` | `NotEmpty()` | "Evolução referenciada é obrigatória." |
| `Texto` | `NotEmpty()` + `MaximumLength(4000)` | "Texto do adendo é obrigatório (máx. 4000)." |

> `CompartilharAtendimentoNaRNDSCommand`, `LancarAtendimentoNoSISABCommand` e `CancelarAtendimentoCommand` validam ao menos `AtendimentoId` `NotEmpty`; demais proteções por invariantes de domínio e checagem de existência/estado no handler. Queries não possuem validador além de `ArgumentNullException`.

---

## 9. Persistência (EF Core 8)

- **Schema:** `saude` (isolado por módulo). **DbContext:** o do módulo Saude. **Migrations:** por módulo.
- **Tabela:** `Atendimento` (raiz) + filhas `EvolucaoSOAP`, `Prescricao`, `SolicitacaoExame`.

| Coluna (Atendimento) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `AtendimentoId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo interceptor; alvo do Global Query Filter. |
| `PacienteId` | `Guid` | conversor de `PacienteId` ↔ `Guid` (referência por Id). |
| `EstabelecimentoId` | `Guid` | conversor de `EstabelecimentoId` ↔ `Guid`. |
| `ProfissionalId` | `Guid` | conversor de `ProfissionalId` ↔ `Guid`. |
| `DataHora` | `datetimeoffset` | `DateTimeOffset`. |
| `Competencia` | `nvarchar(7)` ou `(int Ano,int Mes)` | owned/VO `Competencia` (AAAA-MM). |
| `Modalidade` | `int` | enum `ModalidadeAtendimento`. |
| `NivelGarantia` | `int` | enum `NivelGarantia` (NGS1/NGS2). |
| `Assinatura_*` | (owned) | owned type `AssinaturaDigital` (certificado, hash, carimbo, nivel) — nulo até assinar. |
| `Situacao` | `int` | enum `SituacaoAtendimento`. |

- **Tabelas filhas:**
  - `EvolucaoSOAP(Id, AtendimentoId FK, Subjetivo, Objetivo, Avaliacao, Plano, DataHora, Assinada, EhAdendo, EvolucaoReferenciadaId?)`.
  - `Prescricao(Id, AtendimentoId FK, Item, Posologia, DataHora)`.
  - `SolicitacaoExame(Id, AtendimentoId FK, Procedimento, Justificativa, DataHora)`.
- **Append-only:** evolução assinada nunca recebe `UPDATE` em conteúdo; adendos são **novas linhas** em `EvolucaoSOAP` (`EhAdendo = true`).
- **Índices:**
  - PK em `Id`.
  - Índice em `(TenantId, PacienteId, DataHora)` para `ListarPorPacienteAsync`.
  - Índice em `(TenantId, Competencia)` para envio SISAB por competência.
  - FKs filhas com índice por `AtendimentoId`.
- **Conversores (VO/Id):** Fluent API; owned types para `Competencia`/`AssinaturaDigital`.
- **Guarda legal:** retenção mínima de **20 anos** (Lei 13.787/2018) — política de retenção impede expurgo físico antes do prazo (I-4).
- **Outbox:** tabela Outbox do contexto Saude para `AtendimentoAssinadoIntegrationEvent` e `RESCompartilhadoNaRNDSIntegrationEvent` (consistência transacional).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Atendimento` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId`; **nunca aceito do cliente**. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Registrar/evoluir/assinar/adendar/cancelar: claim **`saude.atender`** (profissional autorizado).
  - Compartilhar na RNDS / lançar no SISAB: claim `saude.atender` + papel de operação de interoperabilidade.
  - Leitura do prontuário: claim `saude.atender`/`saude.ler`.
  - Módulo Saude **ativável por tenant** (Executivo); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** dado pessoal **sensível** (art. 11). Base legal: **tutela da saúde** (art. 11, II, "f"), com minimização. **Trilha de acesso ao prontuário** registra quem leu, quando e qual CNS, além do *audit trail* imutável. Integration Events para Transparencia carregam apenas dados **agregados/anonimizados**.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Registrar`, `AdicionarEvolucaoSOAP`, `Assinar`, `AdicionarAdendo`, `CompartilharNaRNDS`, `LancarNoSISAB`, `Cancelar`) — destinada ao Tribunal de Contas (TCE-RS). A assinatura NGS2 (ICP-Brasil) reforça o não-repúdio.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **RNDS (Portaria 1.434/2020 — saída):** barramento **HL7 FHIR R4** (Bundles), **mTLS + ICP-Brasil**. `CompartilharNaRNDS` exige NGS2; idempotente por identificador do Bundle, com **timeout, retry e circuit breaker (Polly)** e **Anti-Corruption Layer**; evento via **Outbox** (`RESCompartilhadoNaRNDSIntegrationEvent`).
- **e-SUS APS / SISAB (Portaria 1.412/2013 — saída):** envio da produção (CDS) na **competência**; `LancarNoSISAB` emite `PEPLancadoNoSISAB`. Idempotente por `(AtendimentoId, Competencia)`; ACL + Polly.
- **CADSUS (entrada de pré-condição):** o CNS do paciente é validado no cadastro (agregado Paciente); o atendimento apenas exige `CnsConfirmado == true` (I-1).
- **CNES (master data):** validação de estabelecimento/profissional ativos na competência via ACL (síncrono ou cache).
- **ICP-Brasil (assinatura):** serviço de assinatura/validação NGS2 (`IAssinaturaIcpBrasilService`) — certificado A1/A3; certificados por tenant no **Azure Key Vault**.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; ACL por integração.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Atendimento sem CNS válido**
- **Dado** um `Paciente` sem CNS confirmado no CADSUS
- **Quando** registro um `Atendimento` (`RegistrarAtendimentoCommand`)
- **Então** a operação é rejeitada (`InvalidOperationException`) e **nenhum evento é publicado** (I-1).

**Cenário 2 — Evolução assinada é imutável**
- **Dada** uma `EvolucaoSOAP` já assinada (ICP-Brasil)
- **Quando** tento editar o texto da evolução
- **Então** a edição é bloqueada (`InvalidOperationException`) e só é permitido um **adendo datado/reassinado** (`AdicionarAdendoCommand`) (I-2).

**Cenário 3 — Compartilhamento na RNDS**
- **Dado** um `Atendimento` assinado em **NGS2**
- **Quando** o caso de uso `CompartilharAtendimentoNaRNDSCommand` é executado (evento `AtendimentoAssinado` processado)
- **Então** um Bundle **FHIR R4** é enviado via **mTLS**, a situação passa a `Compartilhado` e `RESCompartilhadoNaRNDS` é publicado (I-5).

**Cenário 4 — Lançamento de produção no SISAB**
- **Dado** um `Atendimento` `Assinado` com `Competencia` definida
- **Quando** executo `LancarAtendimentoNoSISABCommand`
- **Então** a produção é enviada na competência e `PEPLancadoNoSISAB` é emitido (I-6).

**Cenário 5 — Assinatura exige evolução**
- **Dado** um `Atendimento` `EmAndamento` **sem** nenhuma `EvolucaoSOAP`
- **Quando** executo `AssinarAtendimentoCommand`
- **Então** ocorre `InvalidOperationException` (assinatura requer ao menos uma evolução) (I-3).

**Cenário 6 — Adendo após assinatura**
- **Dado** um `Atendimento` `Assinado`
- **Quando** executo `AdicionarAdendoCommand(atendimentoId, evolucaoAssinadaId, "Correção...")`
- **Então** uma nova `EvolucaoSOAP` (`EhAdendo = true`, `Assinada = true`) é criada, a evolução original permanece inalterada e `AdendoRegistrado` é emitido (I-2).

**Cenário 7 — Cancelamento só antes da assinatura**
- **Dado** um `Atendimento` `EmAndamento`
- **Quando** executo `CancelarAtendimentoCommand(atendimentoId, "Erro de digitação")`
- **Então** situação = `Cancelado` e `AtendimentoCancelado` é emitido; se o atendimento estivesse `Assinado`/`Compartilhado`, o cancelamento seria rejeitado (I-10).

**Cenário 8 — Teleconsulta exige CRM ativo**
- **Dado** uma solicitação de `Atendimento` com `Modalidade = Teleconsulta` e profissional sem CRM ativo
- **Quando** registro o atendimento
- **Então** a operação é rejeitada (`InvalidOperationException`) (I-9 / Lei 14.510/2022).

**Cenário 9 — Compartilhar exige NGS2**
- **Dado** um `Atendimento` `Assinado` em **NGS1**
- **Quando** executo `CompartilharAtendimentoNaRNDSCommand`
- **Então** ocorre `InvalidOperationException` (RNDS exige NGS2) (I-4/I-5).

**Cenário 10 — Consulta tenant-scoped do prontuário**
- **Dado** atendimentos de um paciente no tenant A e atendimentos do tenant B
- **Quando** executo `ListarAtendimentosDoPacienteQuery(pacienteId)` no contexto do tenant A
- **Então** retornam **apenas** os atendimentos do tenant A e a leitura gera **trilha de acesso ao prontuário**.

---

## 13. Casos de Borda

- **B-1.** Paciente inexistente em `Registrar` ⇒ `InvalidOperationException("Paciente sem CNS válido/confirmado.")` (I-1).
- **B-2.** Profissional/CBO inativo na competência ⇒ registro rejeitado (I-1).
- **B-3.** `AdicionarEvolucaoSOAP` com todos os campos SOAP vazios ⇒ `ArgumentException` (I-8).
- **B-4.** `Assinar` sem evolução ⇒ `InvalidOperationException` (I-3).
- **B-5.** Editar evolução assinada ⇒ bloqueado (I-2); apenas adendo.
- **B-6.** Adendo referenciando evolução **não assinada** ou inexistente ⇒ `InvalidOperationException`.
- **B-7.** Adicionar `Prescricao`/`SolicitacaoExame` em atendimento `Assinado` ⇒ `InvalidOperationException` (I-7).
- **B-8.** `CompartilharNaRNDS` em atendimento `EmAndamento` (não assinado) ⇒ `InvalidOperationException`.
- **B-9.** `CompartilharNaRNDS` em NGS1 ⇒ `InvalidOperationException` (I-4/I-5).
- **B-10.** RNDS retorna erro após retries (Polly) ⇒ situação permanece `Assinado`; evento de integração **não** é publicado (Outbox só após sucesso/idempotência conforme política).
- **B-11.** `LancarNoSISAB` sem `Competencia` ⇒ `InvalidOperationException` (I-6).
- **B-12.** Reenvio do mesmo atendimento à RNDS/SISAB ⇒ idempotente por identificador (`AtendimentoId`/Bundle/competência); não duplica.
- **B-13.** Cancelar atendimento `Assinado`/`Compartilhado` ⇒ `InvalidOperationException` (I-10); correção apenas por adendo.
- **B-14.** Tentativa de expurgo físico antes de 20 anos ⇒ bloqueada pela política de retenção (I-4 / Lei 13.787/2018).
- **B-15.** Leitura do prontuário sem claim `saude.atender` ⇒ negado por padrão (403 auditado) (I-13).
- **B-16.** `AtendimentoAssinadoIntegrationEvent` deve ser idempotente no consumidor por `EventId` (reentrega via Outbox).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo Saude (mapa de domínio: Atendimento raiz; entidades EvolucaoSOAP/Prescricao/SolicitacaoExame; VOs CID/CIAP/AssinaturaDigital; eventos AtendimentoRegistrado/AtendimentoAssinado/PEPLancadoNoSISAB/RESCompartilhadoNaRNDS; regras append-only, NGS2/ICP-Brasil, guarda 20 anos, RNDS FHIR R4, SISAB, teleconsulta). |

<!-- manifest
commands: RegistrarAtendimento, AdicionarEvolucaoSOAP, AssinarAtendimento, AdicionarAdendo, CompartilharAtendimentoNaRNDS, LancarAtendimentoNoSISAB, CancelarAtendimento
queries: ObterAtendimentoPorId, ListarAtendimentosDoPaciente
domainEvents: AtendimentoRegistrado, AtendimentoAssinado, AdendoRegistrado, RESCompartilhadoNaRNDS, PEPLancadoNoSISAB, AtendimentoCancelado
integrationEventsPublished: AtendimentoAssinadoIntegrationEvent, RESCompartilhadoNaRNDSIntegrationEvent
integrationEventsConsumed: FornecedorHabilitado, BemIncorporado
-->

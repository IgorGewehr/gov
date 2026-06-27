# ONDA 1 — Cadastros-mestres que destravam o resto (BACKEND DESIGN, implementação-pronto)

> **Status:** Ondas 0–3 concluídas. Esta spec de design (Onda 1) é auto-suficiente e permanece como referência do que foi implementado.
> **Escopo desta spec:** entidades-mestre que faltam e que hoje são "GUID digitado" —
> Educação (Aluno/Turma/Matrícula real), Saúde (Paciente 1ª classe/Unidade/Profissional),
> RH (Afastamentos tipados com efeito na folha), Patrimônio (Inventário Lei 4.320 art. 96).
> **Não cobre frontend** (apenas anota o que cada item destrava no SPA).
>
> **Disciplina de engenharia (CLAUDE.md):** domínio rico (factory + invariantes no ctor),
> multi-tenant (`IMustHaveTenant` + Global Query Filter herdado de `ModuleDbContext`),
> auditoria/Outbox herdados da base, cross-module **só via `*.Contracts`** (Integration Events),
> `warnings = errors`, NRT estrito. **DbContext ctor inalterado** (assinatura
> `(DbContextOptions<T>, ITenantContext, ScopeDbContextHolder?)`) — só acrescentar `DbSet`s e
> `ApplyConfigurationsFromAssembly` já cobre as novas `IEntityTypeConfiguration`.
>
> **Padrões provados reaproveitados (referência viva no repo):**
> - ID forte: `readonly record struct XId(Guid Value) { static New(); }` (ex.: `BemPatrimonialId`).
> - Agregado: `sealed class X : AggregateRoot<XId>, IMustHaveTenant`, ctor privado + factory estática,
>   coleções-filhas como `List<>` privada exposta por `IReadOnlyCollection<>` (ex.: `BemPatrimonial`, `Paciente`).
> - EF config: `HasConversion(id => id.Value, v => new XId(v)).ValueGeneratedNever()`, VOs como
>   `ComplexProperty`/owned, `HasIndex(... ).IsUnique()` sempre **prefixado por TenantId** (ex.: `EscolaConfiguration`).
> - Busca paginada: `ResultadoPaginado<T>` + `Paginacao.Sanear` + `BuscaTexto` (já existe em Saúde/Patrimônio — Onda 0).
> - Endpoints: Minimal API em `MapGroup("/api/<modulo>")` + `.RequirePermission("<modulo>.ver|gerenciar")`.
> - Migração: `SchemaProvisioner.AplicarAsync` no `MigrarBancoAsync` do módulo (nada novo a fazer além de gerar a migration).

---

## 0. Resumo executivo (3 linhas por módulo) + esforço

- **Educação (G):** criar agregados **Aluno** (com Responsáveis, LGPD-menor) e **Turma** (escola+série+turno+ano letivo+vagas), e ligar a **Matrícula** já existente a entidades reais — substituindo o `TurmaRepository.PossuiVagaAsync` stub (`id != Guid.Empty`) por contagem real `matriculados < vagas` e o GUID digitado por *pickers*. Destrava: tela de matrícula utilizável, diário por turma, FUNDEB com matrícula ponderada real. **É o bloqueador nº1 do projeto.**
- **Saúde (M):** promover **Paciente** (já é agregado rico) ao 1º clique e materializar **Unidade/Estabelecimento (CNES)** e **Profissional (CBO/CNES)** como agregados locais, trocando o `SimuladoEstabelecimentoRepository` (que só checa `Guid.Empty`) por consulta ao cadastro real. Destrava: agenda (Onda 2), atendimento referenciando entidades reais com nome/CNES, lista de UBS/equipe.
- **RH (M):** transformar o afastamento genérico (`RaiseDomainEvent(AfastamentoRegistrado(...))` sem tipo nem efeito) em **agregado `Afastamento` TIPADO** (maternidade 180d, doença/INSS, prêmio, sem vencimento, etc.) com **efeito determinístico na folha** via insumo do `MotorDeCalculoFolha` (suspende/reduz proventos, conta ou não tempo). Destrava: folha correta, S-2230, tela de afastamentos. **Corrige risco de folha errada hoje.**
- **Patrimônio (G):** criar o agregado **Inventario** (Lei 4.320 art. 96): levantamento anual/por setor, comissão designada, coleta físico item-a-item, conciliação **físico × contábil**, registro de **divergências** (sobra/falta/divergência de localização) e encerramento que alimenta movimentações/baixas. Não existe hoje (0 ocorrências). Destrava: "patrimônio de verdade", cobrança recorrente do TCE.

**Ordem recomendada das ondas de build (ver §6):** 1) RH-Afastamentos → 2) Educação-Aluno → 3) Educação-Turma+ligação Matrícula → 4) Saúde-Unidade/Profissional → 5) Patrimônio-Inventário.

---

## 1. EDUCAÇÃO — Aluno, Turma e Matrícula real

### Contexto atual (o que já existe)
- `Escola` (agregado rico, `CodigoInep`, endereço, infra) — **reaproveitar como âncora**.
- `Matricula` (agregado rico, máquina de estados `Ativa→Transferida/Concluída/Abandono`) — **mas referencia `AlunoId`/`TurmaId` como `readonly record struct` "soltos"** (`Matriculas/Identificadores.cs`), sem agregado por trás.
- `MatricularAlunoHandler` chama `turmas.PossuiVagaAsync` → hoje implementado em `TurmaRepository` como `Task.FromResult(turmaId.Value != Guid.Empty)` (**stub explícito, smoking gun**).
- Endpoint `/api/educacao/matriculas` recebe `AlunoId`/`TurmaId`/`EscolaId` como **GUID cru no corpo**.

### 1.1 Agregado **Aluno** (novo — `Domain/Alunos/`)

**Identidade:** `readonly record struct AlunoId(Guid Value)` com `New()`. **Substituir** o `AlunoId` solto em `Matriculas/Identificadores.cs` por um *type alias*/`using` apontando ao novo, ou manter o de matrícula como referência cross-aggregate por Id (preferível: o `Matriculas/AlunoId` vira `using AlunoId = ...Domain.Alunos.AlunoId;` para não quebrar a `Matricula`).

```
sealed class Aluno : AggregateRoot<AlunoId>, IMustHaveTenant
  Guid TenantId
  Cpf? Cpf                      // opcional (recém-nascido pode não ter) — SharedKernel.ValueObjects.Cpf
  string CodigoInepAluno?       // ID único do aluno no Censo/INEP (12 díg.) — preenchido pós-EducaCenso
  DadosCivis DadosCivis         // owned: Nome, NomeSocial, DataNascimento, Sexo, NomeMae (obrigatório p/ menor), NomePai?
  Endereco Endereco             // reusar ValueObjects/Endereco (já existe no módulo)
  SituacaoAluno Situacao        // Ativo | Transferido | Inativo (terminal)
  IReadOnlyCollection<Responsavel> Responsaveis   // List privada; >=1 obrigatório se menor de idade
  // factory:
  static Aluno Cadastrar(tenantId, DadosCivis, Endereco, Cpf?, hoje)  // RaiseDomainEvent(AlunoCadastrado)
  void AdicionarResponsavel(Responsavel)          // valida vínculo único por (parentesco+cpf)
  void AtualizarDados(DadosCivis, Endereco)
  void VincularCodigoInep(string)                 // idempotente; só se vazio
  void Inativar(string motivo)                    // terminal
```

`Responsavel` (Entity-filha de `Aluno`, `Entity<ResponsavelId>`): `Nome`, `Cpf?`, `Parentesco` (enum Mae/Pai/Avo/Tutor/Outro), `Telefone`, `bool ResponsavelFinanceiro`, `bool AutorizadoBuscar`. **LGPD art. 14 (melhor interesse do menor):** marcar `DadosCivis`/`Cpf`/`Responsavel.Cpf` com `[CampoSensivelLgpd]` (atributo já usado em `Paciente.Cns`/`Servidor.Cpf`).

**Invariantes (Aluno):**
- I-A1: Nome obrigatório; DataNascimento não-futura (validar no ctor de `DadosCivis`, padrão `Identificacao` de Saúde).
- I-A2: Menor de idade (`hoje - DataNascimento < 18a`) ⇒ **≥1 Responsavel** ao cadastrar (fail-closed no factory).
- I-A3: `NomeMae` obrigatório (exigência EducaCenso).
- I-A4: Unicidade por tenant: `(TenantId, Cpf)` único **quando Cpf informado**; `(TenantId, CodigoInepAluno)` único quando informado. (Índice filtrado.)
- I-A5: `Inativar`/`Transferido` é terminal — bloqueia novas alterações (padrão `GarantirAtivo` de `Paciente`).

### 1.2 Agregado **Turma** (novo — `Domain/Turmas/`)

```
readonly record struct TurmaId(Guid Value)        // substitui o "solto" de Matriculas
sealed class Turma : AggregateRoot<TurmaId>, IMustHaveTenant
  Guid TenantId
  EscolaId EscolaId             // FK lógica ao agregado Escola (mesmo módulo) — referência por Id
  int AnoLetivo                 // ex.: 2026
  Etapa Etapa                   // enum: EducacaoInfantil, Fundamental1, Fundamental2, EJA, ...
  string Serie                  // "1º ano", "Pré II" (mapeamento BNCC fica P3)
  Turno Turno                   // Matutino | Vespertino | Noturno | Integral
  int Vagas                     // capacidade (>0)
  int Matriculados              // contador desnormalizado mantido pelo domínio (ver §1.3)
  SituacaoTurma Situacao        // Planejada | Aberta | Encerrada
  int VagasDisponiveis => Vagas - Matriculados
  bool PossuiVaga => Situacao == Aberta && VagasDisponiveis > 0
  static Turma Criar(tenantId, EscolaId, anoLetivo, Etapa, serie, Turno, vagas)  // nasce Planejada
  void Abrir()                  // Planejada -> Aberta (habilita enturmação)
  void Encerrar()               // -> Encerrada (só se sem matrícula ativa, ou ao fim do ano)
  void AjustarVagas(int)        // >= Matriculados (nunca abaixo do já enturmado)
  internal void IncrementarMatriculados()  // chamado na matrícula (ver §1.3)
  internal void DecrementarMatriculados()  // chamado no encerramento/transferência
```

**Invariantes (Turma):**
- I-T1: `Vagas > 0`; `Etapa`/`Turno` válidos (`Enum.IsDefined`).
- I-T2: `Matriculados <= Vagas` **sempre** (I-11 do README de Educação, hoje só stub).
- I-T3: Enturmação só em `Aberta`; `AjustarVagas` nunca abaixo de `Matriculados`.
- I-T4: Unicidade `(TenantId, EscolaId, AnoLetivo, Serie, Turno)` — não duplicar turma idêntica.
- I-T5: `Encerrar` exige `Matriculados == 0` **ou** flag de encerramento de ano letivo (parametrizável).

### 1.3 Ligação Matrícula ↔ Aluno/Turma (alterar o existente)

**Decisão de consistência:** Aluno, Turma e Matrícula são **agregados distintos** (cada um com sua transação). O contador `Turma.Matriculados` cruza fronteira de agregado, então:

- **Opção adotada (simples, consistente, sem evento cross-aggregate):** `MatricularAlunoHandler` carrega a `Turma` no mesmo `DbContext`/transação, chama `turma.PossuiVaga`, cria a `Matricula`, chama `turma.IncrementarMatriculados()` e salva **tudo no mesmo `SaveChangesAsync`** (mesma UoW, mesmo tenant). Como é o mesmo módulo/contexto, isto **não viola** isolamento (a regra cross-module vale entre Bounded Contexts, não dentro do mesmo). Igual ao padrão em que `Matricula` e `Turma` coexistem no `EducacaoDbContext`.
- `TurmaRepository.PossuiVagaAsync` **deixa de ser stub**: passa a `context.Turmas.Where(t => t.Id == id).Select(t => t.PossuiVaga)` (ou carrega a entidade). Remover `Task.FromResult(turmaId.Value != Guid.Empty)`.
- `EncerrarMatricula`/`TransferirAluno`/`RegistrarAbandono` ⇒ `turma.DecrementarMatriculados()` na mesma transação.
- `MatricularAlunoHandler` passa a validar que `Aluno` existe e está `Ativo` e que `EscolaId` da turma == `EscolaId` informado (coerência aluno-turma-escola).

### 1.4 Endpoints (novos + alterados)

```
# Aluno
POST   /api/educacao/alunos                         CadastrarAlunoCommand            -> { id }       [educacao.gerenciar]
PUT    /api/educacao/alunos/{alunoId:guid}          AtualizarAlunoCommand            -> 204          [educacao.gerenciar]
POST   /api/educacao/alunos/{alunoId:guid}/responsaveis  AdicionarResponsavelCommand -> 201          [educacao.gerenciar]
POST   /api/educacao/alunos/{alunoId:guid}/inativacao    InativarAlunoCommand        -> 204          [educacao.gerenciar]
GET    /api/educacao/alunos                          BuscarAlunosQuery (termo/nome/cpf/nasc, paginado) [educacao.ver]
GET    /api/educacao/alunos/{alunoId:guid}           ObterAlunoQuery (ficha + responsáveis + matrículas) [educacao.ver]
# Turma
POST   /api/educacao/turmas                          CriarTurmaCommand                -> { id }       [educacao.gerenciar]
POST   /api/educacao/turmas/{turmaId:guid}/abertura   AbrirTurmaCommand               -> 204          [educacao.gerenciar]
POST   /api/educacao/turmas/{turmaId:guid}/encerramento EncerrarTurmaCommand          -> 204          [educacao.gerenciar]
PUT    /api/educacao/turmas/{turmaId:guid}/vagas      AjustarVagasCommand             -> 204          [educacao.gerenciar]
GET    /api/educacao/turmas                          BuscarTurmasQuery (escola/ano/turno/etapa, paginado, c/ VagasDisponiveis) [educacao.ver]
GET    /api/educacao/turmas/{turmaId:guid}           ObterTurmaQuery (+ lista de matriculados) [educacao.ver]
# Matrícula (existente — agora com pickers reais; sem mudança de rota)
POST   /api/educacao/matriculas  (corpo já tem AlunoId/TurmaId/EscolaId — agora referenciam entidades reais)
```

### 1.5 Persistência / migração
- `EducacaoDbContext`: acrescentar `DbSet<Aluno> Alunos` e `DbSet<Turma> Turmas` (ctor **inalterado**).
- `AlunoConfiguration` / `TurmaConfiguration` (padrão `EscolaConfiguration`): ID `HasConversion`, `DadosCivis`/`Endereco` como `ComplexProperty`/owned, `Responsaveis` como `OwnsMany` (tabela `AlunoResponsaveis`), índices únicos filtrados (`HasIndex(...).IsUnique().HasFilter(...)` no SQL Server; em SQLite usar índice + guarda no domínio). `Turma.Matriculados`/`VagasDisponiveis`/`PossuiVaga` ⇒ `Ignore` nas calculadas, persistir só `Matriculados`.
- Migration: `dotnet ef migrations add CadastroAlunoTurma` (schema `educacao`); `SchemaProvisioner.AplicarAsync` já cuida do apply no `MigrarBancoAsync`.

### 1.6 Testes-chave
- Aluno: menor sem responsável ⇒ rejeita (I-A2); CPF duplicado no tenant ⇒ rejeita (I-A4); data nasc. futura ⇒ rejeita.
- Turma: `AjustarVagas` abaixo de `Matriculados` ⇒ rejeita (I-T3); enturmar em turma cheia ⇒ `PossuiVaga=false` (I-T2); encerrar turma com matrícula ativa ⇒ rejeita (I-T5).
- Matrícula: enturmar incrementa `Matriculados`; encerrar/transferir decrementa; matricular em escola ≠ escola da turma ⇒ rejeita; **regressão do stub**: `PossuiVagaAsync` agora reflete contagem real, não `Guid.Empty`.
- Isolamento: aluno/turma de tenant B invisíveis ao tenant A (Global Query Filter).

### 1.7 Destrava no frontend
Tela de matrícula com *picker* de aluno (busca por nome/CPF) e de turma (com vagas disponíveis) — fim do GUID digitado; lista navegável de alunos e turmas; base para diário por turma e boletim (Onda 3) e matrícula ponderada FUNDEB real.

---

## 2. SAÚDE — Paciente 1ª classe, Unidade/Estabelecimento e Profissional

### Contexto atual
- `Paciente` **já é agregado rico** (CNS, `Identificacao` com CPF opcional, condições/alergias, confirmação CADSUS) e já tem busca paginada (`BuscarPacientes`, Onda 0). **Não precisa recriar** — apenas garantir que está no 1º clique (frontend) e que `Atendimento`/`SolicitacaoRegulacao` referenciam o `PacienteId` real (já fazem por Id).
- `Atendimento` referencia `EstabelecimentoId`/`ProfissionalId` como `readonly record struct` soltos (`Atendimento/ReferenciasExternas.cs`).
- `SimuladoEstabelecimentoRepository` valida só `Guid.Empty` (master data CNES inexistente localmente).

### 2.1 Agregado **Estabelecimento** (UBS/unidade — novo, `Domain/Estabelecimentos/`)

```
readonly record struct EstabelecimentoId(Guid Value)   // unificar com o "solto" de Atendimento via using-alias
sealed class Estabelecimento : AggregateRoot<EstabelecimentoId>, IMustHaveTenant
  Guid TenantId
  CodigoCnes Cnes              // VO: 7 dígitos, validado (novo VO no módulo, padrão CodigoInep)
  string Nome                  // "UBS Central"
  TipoEstabelecimento Tipo     // UBS, UPA, Hospital, CAPS, Farmácia, ...
  Endereco Endereco            // reusar Pacientes/Endereco ou ValueObject próprio
  SituacaoEstabelecimento Situacao  // Ativo | Inativo
  static Estabelecimento Cadastrar(tenantId, CodigoCnes, nome, Tipo, Endereco)  // RaiseDomainEvent(EstabelecimentoCadastrado)
  void Inativar(); void Reativar(); void AtualizarDados(...)
```

**Invariantes:** Cnes válido (7 díg.); `(TenantId, Cnes)` único; Nome obrigatório; inativo bloqueia vínculo de novos atendimentos (validado no repo de consulta).

### 2.2 Agregado **Profissional** (novo, `Domain/Profissionais/`)

```
readonly record struct ProfissionalId(Guid Value)      // unificar com o "solto" de Atendimento
sealed class Profissional : AggregateRoot<ProfissionalId>, IMustHaveTenant
  Guid TenantId
  Cpf Cpf                      // SharedKernel
  string Nome
  string? Cns                  // CNS do profissional (opcional)
  RegistroConselho? Registro   // VO: tipo (CRM/COREN/CRO/...) + UF + número (habilita teleconsulta — I-9)
  IReadOnlyCollection<VinculoCnes> Vinculos   // List privada: (EstabelecimentoId, CBO, dataInicio, dataFim?)
  SituacaoProfissional Situacao  // Ativo | Inativo
  static Profissional Cadastrar(tenantId, Cpf, nome, RegistroConselho?)
  void Vincular(EstabelecimentoId, Cbo cbo, DateOnly inicio)   // I-1: vínculo CBO ativo
  void EncerrarVinculo(EstabelecimentoId, DateOnly fim)
  bool TemVinculoAtivo(EstabelecimentoId, DateOnly hoje)
  bool TemCrmAtivo => Registro is { Tipo: Crm } && Situacao == Ativo
```

`VinculoCnes` (Entity-filha): `EstabelecimentoId`, `Cbo` (VO código CBO), `DataInicio`, `DataFim?`. **Invariantes:** Cpf válido; `(TenantId, Cpf)` único; um vínculo ativo por `(profissional, estabelecimento, CBO)` simultâneo.

### 2.3 Religação do `IEstabelecimentoRepository` (substituir o simulado)
A interface `IEstabelecimentoRepository` (Application) **permanece** — só a implementação muda: criar `EstabelecimentoRepository` (Infrastructure) que consulta os **agregados reais locais**:
- `EstabelecimentoAtivoAsync` ⇒ `Estabelecimentos.AnyAsync(e => e.Id == id && e.Situacao == Ativo)`.
- `ProfissionalAtivoAsync` ⇒ `Profissionais` com vínculo CBO ativo na competência.
- `ProfissionalComCrmAtivoAsync` ⇒ `prof.TemCrmAtivo`.

Manter `SimuladoEstabelecimentoRepository` apenas como *fallback* de teste; trocar o registro em `SaudeModule` para o real (`// TODO(prod: CNES oficial quando houver credencial)` — a operação local roda sem cred).

### 2.4 Endpoints
```
POST /api/saude/estabelecimentos               CadastrarEstabelecimentoCommand  -> {id}   [saude.gerenciar]
PUT  /api/saude/estabelecimentos/{id}          AtualizarEstabelecimentoCommand  -> 204    [saude.gerenciar]
POST /api/saude/estabelecimentos/{id}/inativacao                                  -> 204   [saude.gerenciar]
GET  /api/saude/estabelecimentos               BuscarEstabelecimentosQuery (nome/cnes/tipo, paginado) [saude.ver]
GET  /api/saude/estabelecimentos/{id}                                                      [saude.ver]
POST /api/saude/profissionais                  CadastrarProfissionalCommand     -> {id}   [saude.gerenciar]
POST /api/saude/profissionais/{id}/vinculos    VincularProfissionalCommand      -> 201    [saude.gerenciar]
POST /api/saude/profissionais/{id}/vinculos/encerramento                          -> 204   [saude.gerenciar]
GET  /api/saude/profissionais                  BuscarProfissionaisQuery (nome/cpf/cbo/estab, paginado) [saude.ver]
GET  /api/saude/profissionais/{id}                                                          [saude.ver]
```

### 2.5 Persistência / migração / testes
- `SaudeDbContext`: `DbSet<Estabelecimento>`, `DbSet<Profissional>` (ctor inalterado). Configs padrão `PacienteConfiguration` (conversores de ID, owned `Endereco`, `OwnsMany` vínculos). Migration `CadastroEstabelecimentoProfissional` (schema `saude`).
- **Testes:** atendimento com estabelecimento inativo ⇒ rejeita (I-1); teleconsulta sem CRM ⇒ rejeita (I-9); profissional sem vínculo CBO ativo na competência ⇒ rejeita; unicidade CNES/CPF por tenant; isolamento por tenant.

### 2.6 Destrava no frontend
Atendimento/regulação exibindo nome real da UBS e do profissional (em vez de GUID); lista de UBS e equipe; **pré-requisito da Agenda (Onda 2)** que precisa de unidade+profissional+CBO reais.

---

## 3. RH — Afastamentos/Licenças TIPADOS com efeito na folha

### Contexto atual (o problema)
- `Servidor.RegistrarAfastamento(inicio, fim, motivo)` apenas muda `Situacao→Afastado` e emite `AfastamentoRegistrado` — **sem tipo, sem prazo legal, sem efeito na folha**.
- `MotorDeCalculoFolha.Calcular` recebe `InsumosCalculoServidor` (verbas+regime+dependentes+pensão) e **não conhece afastamento** — a folha de um servidor afastado é calculada como se ativo (**risco de folha errada / pagamento indevido hoje**).

### 3.1 Agregado **Afastamento** (novo, `Domain/Afastamentos/`)
Modelar como **agregado próprio** (ciclo de vida + histórico), referenciando `ServidorId` por Id (mesmo módulo). O `Servidor.Situacao=Afastado` continua como espelho de "tem afastamento vigente".

```
readonly record struct AfastamentoId(Guid Value)
sealed class Afastamento : AggregateRoot<AfastamentoId>, IMustHaveTenant
  Guid TenantId
  ServidorId ServidorId
  TipoAfastamento Tipo         // enum tipado (§3.2)
  DateOnly Inicio
  DateOnly? FimPrevisto
  DateOnly? FimEfetivo
  string? Documento            // atestado/portaria/laudo (referência)
  decimal? PercentualRemuneracao   // 0..100; preenchido a partir das REGRAS DO TIPO (não digitado livre)
  SituacaoAfastamento Situacao // Vigente | Encerrado | Cancelado
  static Afastamento Abrir(tenantId, ServidorId, TipoAfastamento, inicio, fimPrevisto, documento, RegrasAfastamento)
  void Encerrar(DateOnly fimEfetivo)      // calcula dias; emite AfastamentoEncerrado
  void Cancelar(string motivo)
  // projeções p/ folha:
  bool VigenteEm(DateOnly competenciaInicio, DateOnly competenciaFim)
  EfeitoFolhaAfastamento EfeitoNa(Competencia)  // ver §3.3
```

### 3.2 Os TIPOS e as regras de cada um (parametrizável por tenant — nunca hardcoded, CLAUDE.md S7/S16)
`TipoAfastamento` (enum) + tabela de regras `RegrasAfastamento` (VO/seed por tenant, padrão `SeedRegrasMde`/tabelas legais). Cada tipo define: **quem paga**, **% de provento mantido pelo ente**, **conta tempo p/ estabilidade/aposentadoria?**, **gera evento eSocial?**, **duração legal padrão**.

| Tipo | Pagamento / efeito na folha | Conta tempo? | Duração padrão | eSocial |
|---|---|---|---|---|
| **LicencaMaternidade** | Ente paga integral; **120d** (CF art. 7 XVIII) — **180d** se o ente aderiu ao Empresa Cidadã/lei municipal (parametrizável). RGPS: salário-maternidade (reembolso INSS). | Sim | 120/180d | S-2230 |
| **LicencaPaternidade** | Integral, ente. 5d (20d se programa adesão). | Sim | 5/20d | S-2230 |
| **DoencaAte15Dias** | Ente paga integral (primeiros 15d — RGPS). | Sim | ≤15d | S-2230 |
| **DoencaINSS (>15d / auxílio)** | **Suspende provento do ente** a partir do 16º dia (RGPS — benefício pago pelo INSS). RPPS: pode manter por lei. **Reduz/zera proventos do ente.** | Sim | indeterminado | S-2230 |
| **AcidenteTrabalho** | Análogo doença, com estabilidade acidentária; ente 15d, depois INSS/RPPS. | Sim | indeterminado | S-2230 |
| **LicencaPremio** | Integral, ente (3 meses por quinquênio, se lei municipal). | Sim | 90d | S-2230 |
| **LicencaSemVencimento (interesse particular)** | **Suspende 100% dos proventos**; **NÃO conta tempo**. | **Não** | até 2 anos | S-2230 |
| **CessaoComOnus / SemOnus** | Com ônus: ente paga. Sem ônus: suspende provento do ente. | Sim | conforme ato | S-2231 |
| **MandatoEletivo** | Conforme opção remuneratória (art. 38 CF). | Sim | mandato | S-2230 |

> **Regra-mestra:** o `PercentualRemuneracao` e os flags (`SuspendeProventos`, `ContaTempo`) **vêm da tabela de regras do tipo**, versionada por tenant/vigência — o usuário escolhe o **tipo**, não digita o efeito. Maternidade default 120d; 180d só se o tenant tiver o parâmetro de adesão ligado.

### 3.3 O gancho no motor de folha (efeito determinístico)
**Onde plugar:** no **handler que monta `InsumosCalculoServidor`** (montagem das verbas do servidor para a competência — `AdicionarEvento`/apuração que alimenta a `FolhaDePagamento`). Antes de lançar proventos:

1. Consultar afastamentos **vigentes na competência** do servidor (`IAfastamentoRepository.ListarVigentesAsync(servidorId, competencia)`).
2. Calcular `EfeitoFolhaAfastamento` = pior/aplicável: `(SuspendeProventos, PercentualRemuneracao, DiasAfastadosNaCompetencia)`.
3. **Aplicar ao provento-base** (vencimento) ao criar a `VerbaCalculo`:
   - `SuspendeProventos=true` (sem vencimento / INSS pós-15d) ⇒ vencimento do mês **= 0** (ou proporcional aos dias trabalhados).
   - `PercentualRemuneracao < 100` ⇒ vencimento × percentual.
   - Proporcionalidade por dias (ex.: licença iniciada no meio do mês) usando `DiasAfastadosNaCompetencia`.
4. O `MotorDeCalculoFolha` **não muda** — ele continua puro; recebe as verbas já ajustadas. (Mantém determinismo e o "não há segundo motor".)
5. **Contagem de tempo:** afastamentos com `ContaTempo=false` (licença sem vencimento) **descontam** do cômputo de `ConcederEstabilidade`/aposentadoria — adicionar `Servidor.DiasNaoComputaveis`/consulta ao calcular elegibilidade (ajuste em `ConcederEstabilidade` para subtrair períodos não-computáveis).

**Servidor (alteração mínima):** `RegistrarAfastamento` passa a **delegar** a criação do agregado `Afastamento` (o handler cria o `Afastamento` e, se vigente, espelha `Servidor.Situacao=Afastado`); `RetornarDeAfastamento` encerra o `Afastamento` vigente. Manter o evento `AfastamentoRegistrado` (agora com `Tipo`) para o S-2230.

### 3.4 Endpoints
```
POST /api/rh/servidores/{id}/afastamentos              RegistrarAfastamentoCommand (agora com Tipo)  -> {id}  [rh.gerenciar]
POST /api/rh/afastamentos/{afastamentoId}/encerramento EncerrarAfastamentoCommand                     -> 204   [rh.gerenciar]
POST /api/rh/afastamentos/{afastamentoId}/cancelamento CancelarAfastamentoCommand                     -> 204   [rh.gerenciar]
GET  /api/rh/servidores/{id}/afastamentos              ListarAfastamentosDoServidorQuery                       [rh.ver]
GET  /api/rh/afastamentos                              BuscarAfastamentosQuery (tipo/vigência/competência, paginado) [rh.ver]
```
> Estender o `RegistrarAfastamentoCommand` existente com `TipoAfastamento Tipo` (hoje só `Inicio/Fim/Motivo`).

### 3.5 Persistência / migração / testes
- `RecursosHumanosDbContext`: `DbSet<Afastamento>` + tabela de `RegrasAfastamento` (parametrização por tenant). Config padrão `ServidorConfiguration`. Migration `AfastamentosTipados` (schema `rh`).
- **Testes-chave (críticos — folha):**
  - Licença sem vencimento na competência ⇒ vencimento = 0 na folha (proventos zerados, descontos legais coerentes).
  - Doença >15d (RGPS) ⇒ ente paga só os 15 primeiros dias; resto suspenso.
  - Maternidade 120d vs 180d (tenant com adesão) ⇒ percentual/duração corretos por parâmetro.
  - Afastamento parcial no mês ⇒ proporcionalidade por dias.
  - `ContaTempo=false` ⇒ atrasa elegibilidade à estabilidade.
  - Determinismo: mesmas entradas+regras ⇒ mesmo resultado (motor puro).
  - Emite S-2230 com o tipo correto.

### 3.6 Destrava no frontend
Tela de afastamentos tipada (dropdown de tipo, prazos sugeridos, documento), folha que reflete o afastamento sem ajuste manual, e habilita o evento eSocial S-2230.

---

## 4. PATRIMÔNIO — Inventário (Lei 4.320 art. 96)

### Contexto atual
- Domínio rico em `BemPatrimonial` (incorporação→tombamento→depreciação→baixa), `MovimentacaoPatrimonial` (localização/responsável), `Veiculo`, `ItemEstoque`. Busca paginada (Onda 0).
- **Inventário não existe** (0 ocorrências) — embora o README cite a Lei 4.320 art. 96 como regra crítica.

### 4.1 Agregado **Inventario** (novo, `Domain/Inventarios/`)
Levantamento periódico (anual obrigatório, ou por setor) que **concilia o saldo físico × contábil** dos bens.

```
readonly record struct InventarioId(Guid Value)
sealed class Inventario : AggregateRoot<InventarioId>, IMustHaveTenant
  Guid TenantId
  int Exercicio                        // ano-base do levantamento
  TipoInventario Tipo                  // Anual | PorSetor | Eventual | Transferencia
  string? Setor                        // localização/UO escopo (null = geral)
  ComissaoInventario Comissao          // VO/owned: membros (Guid responsável + nome) + portaria designação
  SituacaoInventario Situacao          // EmAbertura | EmContagem | EmConciliacao | Encerrado | Cancelado
  DateOnly DataAbertura
  DateOnly? DataEncerramento
  IReadOnlyCollection<ItemInventario> Itens   // List privada (snapshot + contagem)
  IReadOnlyCollection<DivergenciaInventario> Divergencias  // derivado/persistido na conciliação
  static Inventario Abrir(tenantId, exercicio, Tipo, setor, ComissaoInventario, dataAbertura)
  void CarregarSnapshotContabil(IEnumerable<SnapshotBem>)   // congela físico esperado (do acervo)
  void RegistrarContagem(BemPatrimonialId, situacaoEncontrada, localizacaoEncontrada, observacao)  // físico real
  void RegistrarBemNaoCadastrado(descricao, localizacao, valorEstimado)   // "sobra" (achado sem tombo)
  IReadOnlyCollection<DivergenciaInventario> Conciliar()    // físico × contábil -> divergências
  void Encerrar(DateOnly data)         // exige conciliação feita; emite InventarioEncerrado
  void Cancelar(string motivo)
```

- `ItemInventario` (Entity-filha): `BemPatrimonialId`, `NumeroTombamento`, `DescricaoSnapshot`, `LocalizacaoEsperada`, `ValorContabilSnapshot`, `SituacaoEncontrada` (Localizado | NaoLocalizado | LocalizadoOutroSetor | Inservivel), `LocalizacaoEncontrada?`, `bool Contado`.
- `DivergenciaInventario` (Entity-filha): `Tipo` (Falta | Sobra | DivergenciaLocalizacao | DivergenciaEstado | DivergenciaValor), `BemPatrimonialId?`, `Descricao`, `Recomendacao` (baixa/transferência/incorporação/reavaliação).
- `ComissaoInventario` (VO owned): `Portaria`, `List<MembroComissao>` (≥3 membros — exigência usual de comissão).

### 4.2 Fluxo (máquina de estados)
1. **Abrir** (`EmAbertura`): define exercício, tipo, setor, comissão/portaria.
2. **CarregarSnapshotContabil** → `EmContagem`: congela a lista de bens esperados (do acervo, filtrada por setor) — *snapshot* imutável dentro do inventário (não navega ao `BemPatrimonial` ao vivo, evita drift).
3. **RegistrarContagem** item-a-item (físico real) + `RegistrarBemNaoCadastrado` (sobras).
4. **Conciliar** → `EmConciliacao`: cruza snapshot × contagem e gera `Divergencias`:
   - bem no snapshot e não contado ⇒ **Falta**;
   - bem contado em setor ≠ esperado ⇒ **DivergenciaLocalizacao**;
   - achado sem tombo ⇒ **Sobra**;
   - estado/valor divergente ⇒ **DivergenciaEstado/Valor**.
5. **Encerrar** (`Encerrado`): exige conciliação; emite `InventarioEncerrado` (Integration Event via Outbox) com as recomendações.

### 4.3 Conciliação com os bens (efeito downstream — via Contracts/eventos, não acoplamento)
O inventário **não muta o `BemPatrimonial` diretamente** (agregados distintos). Ao encerrar, **publica recomendações** que o operador efetiva por comandos existentes:
- **Falta** confirmada ⇒ operador chama `Baixar` (com laudo/autorização) no bem — fluxo de baixa já existe.
- **DivergenciaLocalizacao** ⇒ `Transferir` (movimentação) no bem.
- **Sobra** ⇒ `Incorporar` novo bem.
- **DivergenciaValor** ⇒ `Reavaliar`/`RegistrarImpairment`.

Manter o inventário como **fonte de auditoria** do levantamento (snapshot congelado + divergências + recomendações), preservando a trilha para o TCE. As efetivações são auditadas nos próprios agregados de bem. (Opcional Onda 2+: automatizar a efetivação via handler que consome o `InventarioEncerrado`.)

### 4.4 Endpoints
```
POST /api/patrimonio/inventarios                          AbrirInventarioCommand        -> {id}   [patrimonio.gerenciar]
POST /api/patrimonio/inventarios/{id}/snapshot            CarregarSnapshotCommand       -> 204     [patrimonio.gerenciar]
POST /api/patrimonio/inventarios/{id}/contagens           RegistrarContagemCommand      -> 201     [patrimonio.gerenciar]
POST /api/patrimonio/inventarios/{id}/sobras              RegistrarBemNaoCadastradoCommand -> 201   [patrimonio.gerenciar]
POST /api/patrimonio/inventarios/{id}/conciliacao         ConciliarInventarioCommand    -> 200 (divergências) [patrimonio.gerenciar]
POST /api/patrimonio/inventarios/{id}/encerramento        EncerrarInventarioCommand     -> 204     [patrimonio.gerenciar]
GET  /api/patrimonio/inventarios                          BuscarInventariosQuery (exercício/setor/situação, paginado) [patrimonio.ver]
GET  /api/patrimonio/inventarios/{id}                     ObterInventarioQuery (itens + divergências) [patrimonio.ver]
GET  /api/patrimonio/inventarios/{id}/divergencias        ListarDivergenciasQuery                  [patrimonio.ver]
```

### 4.5 Persistência / migração / testes
- `PatrimonioDbContext`: `DbSet<Inventario>`; `OwnsMany` para `Itens`/`Divergencias`, `ComplexProperty` para `ComissaoInventario`. Migration `Inventario` (schema `patrimonio`).
- **Testes-chave:** snapshot congela e não muda se o acervo mudar depois; bem não contado ⇒ Falta; contado em outro setor ⇒ DivergenciaLocalizacao; achado sem tombo ⇒ Sobra; encerrar sem conciliar ⇒ rejeita; comissão < 3 membros ⇒ rejeita; `InventarioEncerrado` publicado no Outbox; isolamento por tenant.

### 4.6 Destrava no frontend
Tela de inventário (abrir levantamento, app/lista de contagem, painel de conciliação físico×contábil com divergências e recomendações, relatório de encerramento) — o diferencial de "patrimônio de verdade" cobrado recorrentemente pelo TCE.

---

## 5. Matriz de esforço e dependências

| Módulo | Item | Esforço | Novo agregado? | Toca o quê de existente | Cred oficial? |
|---|---|---|---|---|---|
| Educação | Aluno | G | Sim (Aluno + Responsavel) | `Matriculas/AlunoId` (alias) | Não |
| Educação | Turma + ligação Matrícula | M/G | Sim (Turma) | `TurmaRepository` (remove stub), `MatricularAlunoHandler`, encerrar/transferir | Não |
| Saúde | Estabelecimento + Profissional | M | Sim (2) | `IEstabelecimentoRepository` impl real, `Atendimento` ids (alias) | Não (CNES = Onda 4) |
| RH | Afastamentos tipados + efeito folha | M | Sim (Afastamento + RegrasAfastamento) | montagem de `InsumosCalculoServidor`, `Servidor.RegistrarAfastamento`, `ConcederEstabilidade` | Não (habilita S-2230) |
| Patrimônio | Inventário | G | Sim (Inventario + itens/divergências) | consome comandos de baixa/transf./incorporação já existentes (via recomendação) | Não |

---

## 6. ORDEM RECOMENDADA das ondas de build (sub-workflows)

Critério: **menor risco × destrava mais downstream × independência entre frentes** (permite paralelizar por módulo). Sequência sugerida para 1 dev; frentes A/B/C/D/E são independentes entre módulos e podem rodar em paralelo se houver braço.

1. **RH — Afastamentos tipados (M).** *Primeiro porque corrige um BUG ativo de folha* (servidor afastado hoje é pago como ativo) e é contido (1 agregado + 1 gancho no insumo da folha, motor intacto). Maior risco fiscal eliminado cedo. Habilita S-2230.
2. **Educação — Aluno (G).** Bloqueador nº1 do projeto. Sem dependência de outros itens. Entrega já visível (lista/busca/ficha de aluno).
3. **Educação — Turma + religação da Matrícula (M/G).** Depende de Aluno (matrícula liga os dois) e remove o stub `PossuiVagaAsync`. Fecha a tela de matrícula — o item de maior percepção de "destravou".
4. **Saúde — Estabelecimento + Profissional (M).** Independente; troca o `SimuladoEstabelecimentoRepository`. Pré-requisito da Agenda (Onda 2) — fazer antes de iniciar a Onda 2 de Saúde.
5. **Patrimônio — Inventário (G).** Maior agregado, mas isolado (consome comandos de bem já prontos via recomendação, sem reescrever bens). Fazer por último na Onda 1; alto valor de PoC/TCE.

**Regra de cada sub-workflow (idêntica, replicável):** Domain (agregado+invariantes+eventos) → EF Config + DbSet (ctor inalterado) → migration (`SchemaProvisioner` aplica) → Repository → Application (commands/queries/validators + `ResultadoPaginado` nas buscas) → registro no `Module` (DI) → endpoints `RequirePermission` → testes (invariantes + isolamento tenant + regressão do stub). BDD `Given/When/Then` **antes** do código (CLAUDE.md §1).

---

## 7. Notas de conformidade (todas as frentes)
- **Multi-tenant:** todo agregado novo é `IMustHaveTenant`; índices únicos **sempre** prefixados por `TenantId`; nunca desabilitar o Global Query Filter.
- **Auditoria/Outbox:** herdados de `ModuleDbContext` — Integration Events (`AlunoCadastrado`, `EstabelecimentoCadastrado`, `AfastamentoRegistrado` com tipo, `InventarioEncerrado`) só em `*.Contracts`, publicados via Outbox.
- **LGPD:** Aluno (menor — art. 14), Responsavel.Cpf, Profissional.Cpf, Paciente.Cns ⇒ `[CampoSensivelLgpd]` (mascaramento na trilha).
- **Parametrização legal:** prazos/percentuais de afastamento, vagas, duração de inventário ⇒ tabela por tenant/vigência, **nunca hardcoded** (CLAUDE.md S7/S16).
- **Sem cred oficial em nenhuma frente da Onda 1** — operação 100% local; troca por gateways reais (CNES/INEP/eSocial) é Onda 4/M10.

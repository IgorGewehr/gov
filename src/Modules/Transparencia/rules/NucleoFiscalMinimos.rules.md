---
modulo: Transparencia
agregado: NucleoFiscalMinimos
contexto: Transparencia (núcleo fiscal compartilhado M7.0 — mínimos constitucionais Saúde/Educação)
poder: Executivo
schema: transparencia
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "LC 141/2012 (regulamenta a EC 29) — mínimo de 15% em ASPS (Ações e Serviços Públicos de Saúde)"
  - "CF art. 212 — mínimo de 25% da receita de impostos e transferências em MDE (Manutenção e Desenvolvimento do Ensino)"
  - "Portaria MOG 42/1999 — classificação funcional (função 10 Saúde, função 12 Educação)"
  - "Portaria STN 710/2021 — fonte/destinação de recurso (FR) do PCASP"
  - "Portaria STN 642/2019 — Matriz de Saldos Contábeis (atributos FR/ND/FS por conta)"
---

# Núcleo Fiscal dos Mínimos Constitucionais (M7.0) — Regras-as-Code

> **Fonte da verdade.** Núcleo fiscal compartilhado dos setores que respondem a mínimos
> constitucionais (Saúde 15% ASPS · Educação 25% MDE). Um só esqueleto: a apuração é "a mesma coisa
> com nomes diferentes" — muda o percentual e a função. **Nada é hardcoded**: percentuais, funções e
> regras de classificação são **parâmetros versionados por tenant+vigência** (CLAUDE.md §7/§16).
>
> Mora no módulo **Transparencia** (eixo de prestação fiscal): reaproveita Outbox, audit trail
> imutável, isolamento por tenant e consome a contabilidade de **Finanças** **somente via Contracts**
> (Integration Events). Cross-module nunca toca o interno de outro módulo.

---

## 1. Linguagem Ubíqua

| Termo (identificador-no-código) | Definição |
|---|---|
| **Setor de Mínimo** (`SetorMinimo`) | Setor sujeito a mínimo constitucional, ancorado na função de governo (`Saude`=10, `Educacao`=12, `Nenhum`=0). |
| **Código Funcional** (`CodigoFuncional`) | Função/subfunção (MOG 42/1999). VO que extrai a função (2 dígitos) da funcional-programática ou do FS. |
| **Fonte de Recurso Vinculado** (`FonteRecursoVinculado`) | Regra, por tenant+vigência, que carimba `(função, fonte) → setor` e se a despesa **computa** no mínimo. Raiz de agregado. |
| **Classificador de Fonte** (`ClassificadorFonteRecurso`) | Serviço de domínio que escolhe a regra mais específica (fonte vence função; vigência mais recente desempata). |
| **Linha de Execução Fiscal** (`LinhaExecucaoFiscal`) | Read model: receita-base ou despesa setorial já decomposta por função/fonte, projetada da contabilidade. |
| **Indicador de Mínimo** (`IndicadorMinimo`) | Resultado da apuração: base, aplicado, % aplicado, % mínimo (limite) e situação (`Atingido`/`NaoAtingido`). VO. |
| **Apurador de Mínimo** (`ApuradorMinimo`) | Serviço de domínio que cruza receita-base × despesa computável × % vigente → `IndicadorMinimo`. Puro/reprodutível. |
| **Calendário Federal** (`CalendarioFederal`) | Prazo federal/estadual parametrizável por tenant+exercício (SIOPS/SIOPE/RMA/AgilizaSUAS). Raiz de agregado. |
| **Parecer de Conselho** (`ParecerConselho`) | Registro imutável do parecer de CMS/CME/CACS-FUNDEB/CMAS sobre as contas/repasses. Raiz de agregado. |
| **Execução Setorial** (`IExecucaoSetorialReadModel` / `ExecucaoSetorial`) | Porta que entrega receita-base + despesas computáveis por setor, desacoplando a via A2 (deriva da MSC) da A1 (contrato carimbado). |
| **Parâmetro de Mínimo** (`ParametroMinimo` / `IParametroMinimoProvider`) | Percentual mínimo vigente por setor (default legal 15%/25%, sobreponível por tenant). |

---

## 2. Modelo

- **`FonteRecursoVinculado` : `AggregateRoot<FonteRecursoVinculadoId>, IMustHaveTenant`** — `(Funcao, FonteRecurso?, Setor, ComputaNoMinimo, VigenciaInicio)`. Fábrica `Criar(...)`. Método `Casa(codigo, fonte)` e `Especificidade()`.
- **`CalendarioFederal` : `AggregateRoot<CalendarioFederalId>, IMustHaveTenant`** — `(Chave, Exercicio, Periodo?, DataLimite, Descricao?)`. Fábrica `Registrar(...)`; `EstaVencido(referencia)` (sem relógio).
- **`ParecerConselho` : `AggregateRoot<ParecerConselhoId>, IMustHaveTenant`** — `(Conselho, Setor, Exercicio, DataParecer, NumeroResolucao, Resultado, Observacao?)`. Fábrica `Registrar(...)`.
- **`LinhaExecucaoFiscal` : `Entity<LinhaExecucaoFiscalId>, IMustHaveTenant`** (read model) — `(Exercicio, Tipo, Funcao?, FonteRecurso?, Valor, OrigemHash)`. Fábricas `ReceitaBase(...)` e `Despesa(...)`.
- **`IndicadorMinimo` : `ValueObject`** — `(Setor, ReceitaBase, Aplicado, PercentualAplicado, PercentualMinimo, Situacao)`. Fábrica `Apurar(...)`; divisão segura (base 0 → 0%).

### 2.1 Enums

- **`SetorMinimo`:** `Nenhum`=0, `Saude`=10, `Educacao`=12.
- **`SituacaoMinimo`:** `Atingido`=1, `NaoAtingido`=2.
- **`TipoLinhaExecucao`:** `ReceitaBaseImpostosTransferencias`=1, `DespesaSetorial`=2.
- **`TipoConselho`:** `Cms`=1, `Cme`=2, `CacsFundeb`=3, `Cmas`=4.
- **`ResultadoParecer`:** `Aprovado`=1, `AprovadoComRessalva`=2, `Rejeitado`=3.

---

## 3. Invariantes

- **I-1.** Toda entidade do núcleo é `IMustHaveTenant`; `TenantId` carimbado na inserção e imutável (isolamento multi-tenant).
- **I-2.** Percentual mínimo é **parametrizável por tenant+vigência**; o default legal (Saúde 15% LC 141; Educação 25% CF 212) é só o piso de partida — nunca hardcoded em regra.
- **I-3.** Despesa só computa no mínimo se uma `FonteRecursoVinculado` vigente a classifica no setor **e** marca `ComputaNoMinimo` (LC 141 art. 4º: saneamento/inativos/merenda/limpeza NÃO computam em Saúde).
- **I-4.** A regra mais **específica** (refina por fonte) vence a geral da função; no empate, a de **vigência mais recente**.
- **I-5.** `IndicadorMinimo.Apurar` não divide por zero: receita-base 0 ⇒ percentual aplicado 0% ⇒ `NaoAtingido`.
- **I-6.** A apuração é **reprodutível** (pura, sem `DateTime.Now`): mesma entrada ⇒ mesmo `IndicadorMinimo`. A âncora temporal é o **exercício** (31/12 como referência de vigência).
- **I-7.** A projeção `LinhaExecucaoFiscal` é **idempotente** por `(TenantId, OrigemHash)`: a mesma origem não duplica a linha (I-13 do padrão).
- **I-8.** `Situacao = Atingido` sse `PercentualAplicado >= PercentualMinimo` (comparação sobre o % já arredondado a 4 casas, evitando falso negativo por dízima).
- **I-9.** O núcleo **não recalcula contabilidade**: a execução é **derivada** de Finanças (via A2: MSC; via A1: contrato carimbado) — Finanças continua a fonte da verdade.

---

## 4. Fonte de dados de execução (M7.0.0 — decisão A1×A2)

- **Porta:** `IExecucaoSetorialReadModel.ObterExecucaoAsync(exercicio)` → `ExecucaoSetorial(receitaBase, despesasPorSetor)`.
- **Via A2 (implementada):** `ExecucaoSetorialReadModel` lê `LinhaExecucaoFiscal` do exercício e classifica cada despesa por função/fonte contra `FonteRecursoVinculado` vigente. A `MSCGeradaIntegrationEvent` de Finanças já carrega FR/ND/FS por conta — fonte natural da projeção.
- **Via A1 (futura):** `DespesaEmpenhadaIntegrationEvent` foi **enriquecido** (campos opcionais `FuncaoSubfuncao`/`FonteRecurso`/`NaturezaDespesa`/`Competencia`, retrocompatíveis). Quando Finanças os preencher, troca-se **apenas** a implementação da porta — domínio e apuradores intactos.

---

## 5. Comandos (escrita)

### 5.1 `RegistrarExecucaoFiscal` — Projetar execução decomposta no read model

- **Comando (DTO):** `RegistrarExecucaoFiscalCommand(int Exercicio, IReadOnlyList<LinhaExecucaoEntrada> Linhas) : ICommand<int>`.
- **Dependências:** `ILinhaExecucaoFiscalRepository`, `ITenantContext`, `IUnitOfWork`.
- **Efeito:** para cada entrada não projetada (idempotência por `OrigemHash`), cria `LinhaExecucaoFiscal.ReceitaBase(...)` ou `.Despesa(...)`; retorna o nº de linhas projetadas.
- **Pós-condições:** linhas carimbadas com `TenantId`; reprocessamento não duplica (I-7).

### 5.2 `RegistrarRegraClassificacao` — Versionar regra setorial (função[, fonte] → setor)

- **Comando (DTO):** `RegistrarRegraClassificacaoCommand(string Funcao, SetorMinimo Setor, DateOnly VigenciaInicio, string? FonteRecurso = null, bool ComputaNoMinimo = true) : ICommand<Guid>`.
- **Dependências:** `IFonteRecursoVinculadoRepository`, `ITenantContext`, `IUnitOfWork`.
- **Efeito:** cria `FonteRecursoVinculado.Criar(...)` (regra de classificação setorial do tenant, versionada por `VigenciaInicio`); retorna o `Id`.
- **Pós-condições:** regra carimbada com `TenantId`; o percentual/classificação nunca é hardcoded (parametrizável por tenant+vigência, §16).

---

## 6. Consultas (leitura)

### 6.1 `ApurarMinimos` — Apurar Saúde 15% e Educação 25% do exercício

- **Query:** `ApurarMinimosQuery(int Exercicio) : IQuery<IReadOnlyList<ApuracaoMinimoResultado>>`.
- **Handler:** `ApurarMinimosHandler(IExecucaoSetorialReadModel, IParametroMinimoProvider)`.
- **Projeção (DTO):** `ApuracaoMinimoResultado(SetorMinimo Setor, decimal ReceitaBase, decimal Aplicado, decimal PercentualAplicado, decimal PercentualMinimo, SituacaoMinimo Situacao)`.
- **Efeito:** obtém execução classificada + percentuais vigentes e delega ao `ApuradorMinimo.ApurarTodos`. Read-only, tenant-scoped, reprodutível.

---

## 7. Persistência (EF Core 8)

- **DbContext:** `TransparenciaDbContext` (schema `transparencia`); **ctor inalterado** — apenas novos `DbSet`.
- **Tabelas:** `FontesRecursoVinculado`, `CalendariosFederais`, `PareceresConselho`, `LinhasExecucaoFiscal`, `ParametrosFiscaisVigentes`.
- **Configurações:** `FiscalConfigurations.cs` (Fluent API; enums como string; `decimal(18,2)`/`(18,6)`).
- **Índices:** único `(TenantId, OrigemHash)` em `LinhasExecucaoFiscal`; único `(TenantId, Chave, VigenciaInicio)` em `ParametrosFiscaisVigentes`.
- **Migração:** `NucleoFiscalM7` (SqlServer); no SQLite (dev/teste) o schema vem do modelo (`EnsureCreated`/`GenerateCreateScript`).

---

## 8. Segurança, Tenant e Auditoria

- **`IMustHaveTenant`** em todas as raízes/entidades; **Global Query Filter** por `TenantId`; gravação cross-tenant lança exceção.
- **Auditoria imutável** (hash-chain) e **Outbox** herdados da base — `ParecerConselho` é evidência de prestação para o TCE-RS.
- **Cross-module só via Contracts** (Integration Events de Finanças); o núcleo não referencia o interno de Finanças.

---

## 9. Cenários BDD

**Cenário 1 — Classificação por função.** Despesa de função 10 → Saúde; função 12 → Educação; função sem regra → não vinculada.
**Cenário 2 — Saúde 15% atingido.** Receita 1.000.000 e aplicado 150.000 (15%) → `Atingido`.
**Cenário 3 — Saúde 15% não atingido.** Aplicado 140.000 (14%) → `NaoAtingido`.
**Cenário 4 — Educação 25% atingido e não atingido.** 260.000 (26%) → `Atingido`; 240.000 (24%) → `NaoAtingido`.
**Cenário 5 — Reprodutibilidade.** Mesma entrada ⇒ mesmo indicador (sem relógio).
**Cenário 6 — Isolamento de tenant.** Execução do tenant A não é vista pelo tenant B.
**Cenário 7 — Fonte não computável.** Despesa de fonte marcada `ComputaNoMinimo=false` não entra no mínimo.
**Cenário 8 — Percentual parametrizado.** Tenant com 18% em saúde sobrepõe o default legal de 15%.

---

## 10. Casos de Borda

- **CB-1.** `CodigoFuncional.DeTexto("10.301.0002.2010")` extrai função "10" e subfunção "301".
- **CB-2.** `IndicadorMinimo.Apurar` com receita-base 0 → 0%, `NaoAtingido` (sem divisão por zero).
- **CB-3.** Regra específica por fonte vence a geral da função (I-4).
- **CB-4.** Projeção repetida do mesmo `OrigemHash` não duplica linha (I-7).
- **CB-5.** `ApuradorMinimo.Apurar(Nenhum, ...)` → `ArgumentException` (exige setor de mínimo).

---

## 11. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versão inicial — núcleo fiscal M7.0 (M7.0.0 fonte de dados + classificador; M7.0.1 calendário; M7.0.2 parecer de conselho; M7.0.3 ApuradorMinimo Saúde 15%/Educação 25%). Percentuais/funções/contas parametrizáveis e versionados; apuração reprodutível; cross-module só via Contracts de Finanças. |

<!-- manifest
commands: RegistrarExecucaoFiscal, RegistrarRegraClassificacao
queries: ApurarMinimos
domainEvents:
integrationEventsPublished: MinimoConstitucionalApuradoIntegrationEvent
integrationEventsConsumed:
-->

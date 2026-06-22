---
modulo: Patrimonio
agregado: Veiculo
contexto: Patrimonio (Frota — abastecimento, manutencao, multas, licenciamento, motorista/CNH)
poder: Ambos
schema: patrimonio
ativavel_por_tenant: true
versao_regras: 2.0.0
fontes_legais: ["Lei 9.503/1997 - CTB (licenciamento, IPVA, multas, CNH)", "MCASP/STN (Procedimentos Contabeis Patrimoniais)", "NBC TSP 07 (Ativo Imobilizado)", "Lei 4.320/1964 (controle patrimonial)", "Lei 14.133/2021 art. 31/76 (alienacao/leilao)"]
---

# Veiculo — Regras-as-Code (Rules-as-Code)

> Veículo da frota pública. **É um agregado próprio** (`AggregateRoot<VeiculoId>`) que **se vincula**
> a um `BemPatrimonial` por meio de uma referência `BemPatrimonialId` — o registro contábil/patrimonial
> do veículo. A gestão patrimonial (incorporação contábil, depreciação, reavaliação, impairment e
> baixa/alienação) é **responsabilidade do `BemPatrimonial` vinculado** (vide `BemPatrimonial.rules.md`);
> o `Veiculo` cuida **apenas da operação de frota** conforme o **CTB (Lei 9.503/1997)**: `Placa`/`Renavam`,
> abastecimento (cota por veículo), manutenção (ordem de serviço), multas, licenciamento/IPVA e
> motorista (CNH). Controla `Odometro` (km) e `Horimetro` (horas de uso). Este arquivo é **normativo e
> versionado**; o código (`Veiculo.cs`, handlers, validators, EF config, testes) é consequência dele.
>
> **Decisão de modelagem (DDD):** agregados **não herdam** entre si. `Veiculo` **compõe-se** com
> `BemPatrimonial` por referência de identidade (`BemPatrimonialId`), em vez de `Veiculo : BemPatrimonial`.
> Cada agregado tem seu próprio ciclo de consistência transacional.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Veículo (`Veiculo`) | Bem da frota; agregado próprio (`AggregateRoot<VeiculoId>`). Raiz de agregado da operação de frota. |
| Bem Patrimonial vinculado (`BemPatrimonialId` : referência) | Identidade do `BemPatrimonial` que representa o registro contábil/patrimonial do veículo (módulo Patrimônio). |
| Placa (`Placa` : VO) | Identificação oficial do veículo (CTB). |
| RENAVAM (`Renavam` : VO) | Registro Nacional de Veículos Automotores (CTB). |
| Odômetro (`Odometro` : VO) | Quilometragem acumulada do veículo. |
| Horímetro (`Horimetro` : VO) | Horas de uso acumuladas (máquinas/equipamentos). |
| Abastecimento (`RegistrarAbastecimento` / `Abastecimento` : entidade) | Registro de saída de combustível, sob cota por veículo. |
| Cota de Combustível (`CotaCombustivel`) | Limite de consumo por veículo (gestor de combustível). |
| Ordem de Serviço (`AbrirOrdemServico` / `ManutencaoOS` : entidade) | Registro de manutenção do veículo/equipamento. |
| Manutenção (`ConcluirManutencao` / `ManutencaoConcluida`) | Encerramento da ordem de serviço de manutenção. |
| Multa (`RegistrarMulta` / `Multa` : entidade) | Infração de trânsito atribuída ao veículo/condutor (CTB). |
| Licenciamento (`RegistrarLicenciamento` / `Licenciamento` : entidade) | Licenciamento anual e IPVA (CTB). |
| Motorista (`Motorista` : entidade) | Condutor habilitado vinculado; controla CNH. |
| CNH (`Cnh` / `ValidadeCnh`) | Carteira Nacional de Habilitação do motorista (CTB). |
| Tenant (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro. |
| Situação (`Situacao` : `SituacaoBemPatrimonial`) | Estado do veículo no ciclo de vida operacional/patrimonial (próprio do agregado `Veiculo`). |

---

## 2. Modelo

- **Identidade:** `VeiculoId` — `readonly record struct VeiculoId(Guid Value)`; fábrica `VeiculoId.New()`. (Identidade **própria** e distinta de `BemPatrimonialId`: `Veiculo` **não** é-um `BemPatrimonial`.)
- **Raiz de agregado:** `sealed class Veiculo : AggregateRoot<VeiculoId>, IMustHaveTenant` — agregado **independente**, sem herança de `BemPatrimonial`.
- **Vínculo patrimonial:** o `Veiculo` referencia o seu registro contábil/patrimonial por `BemPatrimonialId` (referência de identidade, **não** navegação para outro agregado). Depreciação, tombamento contábil, baixa e alienação ocorrem no `BemPatrimonial` vinculado (vide `BemPatrimonial.rules.md`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `IncorporarVeiculo(...)`.

### Propriedades (raiz `Veiculo`)

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Ente público dono do registro (`IMustHaveTenant`). | `private set` |
| `Descricao` | `string` | Descrição do veículo. | `private set` |
| `NumeroTombamento` | `string?` | Número de tombamento operacional do veículo (nulo antes de `Tombar`). | `private set` |
| `ValorInicial` | `ValorMonetario` | Valor de incorporação operacional registrado no veículo. | `private set` |
| `ValorResidual` | `ValorMonetario` | Valor residual de referência. | `private set` |
| `ValorContabil` | `ValorMonetario` | Valor contábil de referência espelhado no veículo. | `private set` |
| `VidaUtilMeses` | `int` | Vida útil estimada, em meses. | `private set` |
| `DataIncorporacao` | `DateOnly` | Data de incorporação ao acervo. | `private set` |
| `Origem` | `string` | Origem do ingresso (aquisição, doação, produção própria). | `private set` |
| `EmCondicoesDeUso` | `bool` | Indica se o veículo está em condições de uso. | `private set` |
| `Situacao` | `SituacaoBemPatrimonial` | Estado do veículo no ciclo de vida (próprio do agregado). | `private set` |
| `Placa` | `Placa` (VO) | Placa do veículo. | `private set` |
| `Renavam` | `Renavam` (VO) | RENAVAM. | `private set` |
| `Odometro` | `Odometro` (VO) | Quilometragem atual (monotônica não decrescente). | `private set` |
| `Horimetro` | `Horimetro` (VO) | Horas de uso atuais (monotônica não decrescente). | `private set` |
| `MotoristaAtualId` | `Guid?` | Motorista atualmente designado (nulo se sem condutor). | `private set` |
| `Abastecimentos` | `IReadOnlyCollection<Abastecimento>` | Histórico de abastecimentos. | coleção controlada |
| `OrdensServico` | `IReadOnlyCollection<ManutencaoOS>` | Ordens de serviço de manutenção. | coleção controlada |
| `Multas` | `IReadOnlyCollection<Multa>` | Multas registradas. | coleção controlada |
| `Licenciamentos` | `IReadOnlyCollection<Licenciamento>` | Licenciamentos/IPVA por exercício. | coleção controlada |
| `Motoristas` | `IReadOnlyCollection<Motorista>` | Condutores vinculados (CNH). | coleção controlada |
| `AtivoNoAcervo` | `bool` (derivada) | `true` quando `Situacao` é `Tombado` ou `Cedido` (apto a operações de frota, I-5). | get-only |

> **Sobre a gestão contábil/patrimonial:** os aspectos de **depreciação linear, reavaliação, impairment,
> transferência, cessão e baixa/alienação** **NÃO** vivem no `Veiculo`. Eles são responsabilidade do
> `BemPatrimonial` **vinculado** (referenciado por `BemPatrimonialId`) — vide `BemPatrimonial.rules.md`.
> Os campos `ValorInicial`/`ValorResidual`/`ValorContabil`/`VidaUtilMeses`/`NumeroTombamento` no `Veiculo`
> são dados operacionais/de referência da frota; a fonte de verdade contábil é o `BemPatrimonial`.

### Value Objects (referenciados)

- `Placa` — `record struct` com placa validada (formato Mercosul/antigo).
- `Renavam` — `record struct` com dígito verificador validado.
- `Odometro` — `record struct` com `Valor` (`int` km); `Avancar(int)` exige valor ≥ atual (monotônico não decrescente).
- `Horimetro` — `record struct` com `Valor` (`decimal` horas); `Avancar(decimal)` exige valor ≥ atual.
- `ValorMonetario` — para valores de abastecimento, manutenção, multa, IPVA.

### Entidades (do agregado `Veiculo`)

- `Abastecimento` — data, litros, valor, odômetro/horímetro no abastecimento, motorista.
- `ManutencaoOS` — abertura, descrição, custo estimado/realizado, conclusão, odômetro.
- `Multa` — data infração, código CTB, valor, condutor (motorista), situação (`Pendente`/`EmRecurso`/`Paga`).
- `Licenciamento` — exercício, valor IPVA, valor taxa, data, situação (`Regular`/`Pendente`).
- `Motorista` — nome, `Cnh`, categoria, `ValidadeCnh`, vínculo ao veículo.

### Enums (frota)

#### `SituacaoOrdemServico`

| Valor | Numérico | Descrição |
|---|---|---|
| `Aberta` | 1 | Ordem de serviço aberta. |
| `Concluida` | 2 | Manutenção concluída. |
| `Cancelada` | 3 | Ordem cancelada. |

#### `SituacaoMulta`

| Valor | Numérico | Descrição |
|---|---|---|
| `Pendente` | 1 | Multa pendente de pagamento. |
| `EmRecurso` | 2 | Multa em recurso (defesa/JARI). |
| `Paga` | 3 | Multa paga. |

#### `SituacaoLicenciamento`

| Valor | Numérico | Descrição |
|---|---|---|
| `Pendente` | 1 | Licenciamento pendente no exercício. |
| `Regular` | 2 | Licenciamento regular no exercício. |

> A **situação do veículo** (`Situacao`) usa o enum `SituacaoBemPatrimonial` (`EmIncorporacao`, `Tombado`,
> `Cedido`, `Baixada`, `Alienada`) como **estado próprio** do agregado `Veiculo` — não é herdado de
> `BemPatrimonial`. Operações de frota só são permitidas para veículo **ativo no acervo** (`Tombado`/`Cedido`).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** O `Veiculo` é um **agregado próprio** vinculado a um `BemPatrimonial` por `BemPatrimonialId`. As invariantes contábeis/patrimoniais (depreciação linear, residual como piso, baixa com laudo, alienação com avaliação prévia) pertencem ao `BemPatrimonial` vinculado e **não** são responsabilidade do `Veiculo` — vide `BemPatrimonial.rules.md`.
- **I-2.** Na incorporação, `Placa` e `Renavam` são obrigatórios e válidos (formato/dígito); `Renavam` é único por tenant.
- **I-3.** O **odômetro** é **monotônico não decrescente**: toda atualização (abastecimento/manutenção) exige valor ≥ `Odometro` atual via `Odometro.Avancar(...)`; valor inferior é rejeitado.
- **I-4.** O **horímetro** é **monotônico não decrescente**: toda atualização exige valor ≥ `Horimetro` atual via `Horimetro.Avancar(...)`.
- **I-5.** Operações de frota (abastecer, abrir/concluir OS, registrar multa/licenciamento, designar motorista) só são permitidas para veículo **ativo no acervo** (`Situacao` ∈ {`Tombado`, `Cedido`}, i.e. `AtivoNoAcervo`); veículo encerrado (`Baixada`/`Alienada`) ou `EmIncorporacao` as rejeita (`GarantirAtivoNoAcervo`).
- **I-6.** O **abastecimento** respeita a **cota por veículo** (`cotaLitros`); abastecimento que ultrapasse a cota vigente é bloqueado (ou marcado para autorização, conforme política do gestor de combustível).
- **I-7.** A **conclusão de manutenção** (`ConcluirManutencao`) exige OS em situação `Aberta`; conclui para `Concluida` e emite `ManutencaoConcluida`.
- **I-8.** O **registro de multa** exige código de infração (CTB) e valor; ao registrar, emite `MultaRegistrada`.
- **I-9.** A **designação de motorista** exige `Cnh` válida e **não vencida** (`ValidadeCnh ≥ hoje`); motorista com CNH vencida não pode ser designado.
- **I-10.** O **abastecimento** registra `Odometro`/`Horimetro` no ato; o registro atualiza o odômetro/horímetro do veículo respeitando I-3/I-4 e emite `AbastecimentoRegistrado`.
- **I-11.** O **licenciamento** é registrado por **exercício** (ano); não se admite mais de um licenciamento `Regular` para o mesmo exercício/veículo.
- **I-12.** A **baixa** e a **alienação** do veículo ocorrem no `BemPatrimonial` **vinculado** (exigem, respectivamente, laudo + autorização e avaliação prévia/leilão — Lei 14.133 art. 31/76), com lançamento contábil simultâneo a Finanças. O `Veiculo` apenas **reflete** o encerramento na sua `Situacao` (passando a `Baixada`/`Alienada`), o que bloqueia novas operações de frota (I-5).

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

> A coluna "Origem/Destino" abaixo refere-se à **situação própria do veículo** (`Situacao : SituacaoBemPatrimonial`).
> As operações de frota **não alteram** a situação; alteram coleções/medições. As transições de encerramento
> (`Baixada`/`Alienada`) são **espelhadas** a partir da baixa/alienação do `BemPatrimonial` vinculado (I-12).

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `IncorporarVeiculo` | `EmIncorporacao` | placa/renavam válidos; descrição/origem não vazias; vida útil > 0; residual ≤ inicial | — |
| `EmIncorporacao` | `Tombar` | `Tombado` | número de tombo não vazio; situação == `EmIncorporacao` | — |
| `Tombado` \| `Cedido` | `RegistrarAbastecimento` | (inalterada) | ativo no acervo; dentro da cota; odômetro/horímetro ≥ atual | `AbastecimentoRegistrado` |
| `Tombado` \| `Cedido` | `AbrirOrdemServico` | (inalterada) | ativo no acervo; odômetro ≥ atual | — |
| OS `Aberta` | `ConcluirManutencao` | (inalterada; OS `Concluida`) | OS em `Aberta` | `ManutencaoConcluida` |
| `Tombado` \| `Cedido` | `RegistrarMulta` | (inalterada) | ativo no acervo; código CTB + valor | `MultaRegistrada` |
| `Tombado` \| `Cedido` | `RegistrarLicenciamento` | (inalterada) | ativo no acervo; exercício não duplicado (Regular) | — |
| `Tombado` \| `Cedido` | `DesignarMotorista` | (inalterada) | ativo no acervo; CNH válida e não vencida | — |
| `Tombado` \| `Cedido` | (encerramento espelhado do `BemPatrimonial`) | `Baixada` | baixa do bem vinculado (laudo + autorização) | — (evento no bem vinculado) |
| `Tombado` \| `Cedido` | (encerramento espelhado do `BemPatrimonial`) | `Alienada` | alienação do bem vinculado (avaliação prévia + leilão) | — (evento no bem vinculado) |

> Observações:
> - As operações de frota chamam a guarda de "ativo no acervo" (`GarantirAtivoNoAcervo`, I-5) **antes** das verificações específicas.
> - `RegistrarAbastecimento` valida cota (I-6) e atualiza `Odometro`/`Horimetro` via `Avancar` (I-3/I-4/I-10).
> - A **depreciação, reavaliação, impairment, baixa e alienação contábil** são comandos do `BemPatrimonial` vinculado (vide `BemPatrimonial.rules.md`), **não** do `Veiculo`.

---

## 5. Comandos (escrita)

### 5.1 IncorporarVeiculo

- **Command:** `IncorporarVeiculoCommand(string Descricao, decimal ValorInicial, decimal ValorResidual, int VidaUtilMeses, DateOnly DataIncorporacao, string Origem, string Placa, string Renavam, int OdometroInicial, decimal HorimetroInicial) : ICommand<Guid>`.
- **Entrada (DTO):** dados operacionais/patrimoniais de referência + `Placa`, `Renavam`, medições iniciais.
- **Dependências do handler:** `IVeiculoRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; `Placa`/`Renavam` válidos e `Renavam` único por tenant (I-2); descrição/origem não vazias; `VidaUtilMeses` > 0; `ValorResidual` ≤ `ValorInicial`.
- **Efeito:** cria via `Veiculo.IncorporarVeiculo(tenant.TenantId, ...)`; `veiculos.Adicionar(veiculo)`; `SaveChangesAsync`. (O registro contábil correspondente é criado/vinculado no `BemPatrimonial` — vide nota abaixo.)
- **Pós-condições:** veículo em `EmIncorporacao`; retorna `veiculo.Id.Value`; publica `BemIncorporadoIntegrationEvent` (variação patrimonial aumentativa) a partir do bem vinculado.
- **Exceções:** `ArgumentNullException`; `ArgumentException` (descrição/origem/placa/renavam inválidos); `ArgumentOutOfRangeException` (vida útil / residual); `InvalidOperationException` (renavam duplicado).
- **Evento de domínio:** — (nesta versão, `IncorporarVeiculo` não emite evento de domínio na raiz `Veiculo`; a incorporação contábil/`BemIncorporado` ocorre no `BemPatrimonial` vinculado).
- **Evento de integração (publica):** `BemIncorporadoIntegrationEvent` (do `BemPatrimonial` vinculado).

### 5.2 RegistrarAbastecimento

- **Command:** `RegistrarAbastecimentoCommand(Guid VeiculoId, DateOnly Data, decimal Litros, decimal Valor, int Odometro, decimal Horimetro, Guid? MotoristaId) : ICommand`.
- **Pré-condições:** `request` não nulo; veículo existe; ativo no acervo (I-5); dentro da **cota** vigente (I-6); `Odometro` ≥ atual (I-3) e `Horimetro` ≥ atual (I-4).
- **Efeito:** `veiculo.RegistrarAbastecimento(data, litros, valor, odometro, horimetro, cotaLitros, motoristaId)`; grava `Abastecimento`; atualiza `Odometro`/`Horimetro`; `SaveChangesAsync`.
- **Pós-condições:** novo `Abastecimento`; medições atualizadas; emite `AbastecimentoRegistrado`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado / cota excedida / odômetro retroativo).
- **Evento de domínio:** `AbastecimentoRegistrado(VeiculoId, litros, valor, data)`.

### 5.3 AbrirOrdemServico

- **Command:** `AbrirOrdemServicoCommand(Guid VeiculoId, string Descricao, decimal CustoEstimado, int Odometro) : ICommand<Guid>`.
- **Pré-condições:** `request` não nulo; veículo existe; ativo no acervo (I-5); `Odometro` ≥ atual (I-3).
- **Efeito:** `veiculo.AbrirOrdemServico(descricao, custoEstimado, odometro)`; grava `ManutencaoOS` em `Aberta`; `SaveChangesAsync`.
- **Pós-condições:** nova OS em `Aberta`; retorna `osId` (`ManutencaoOsId.Value`).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado).
- **Evento de domínio:** — (abertura interna; sem evento nesta versão).

### 5.4 ConcluirManutencao

- **Command:** `ConcluirManutencaoCommand(Guid VeiculoId, Guid OrdemServicoId, decimal CustoRealizado, DateOnly DataConclusao) : ICommand`.
- **Pré-condições:** `request` não nulo; veículo e OS existem; OS em situação `Aberta` (I-7).
- **Efeito:** `veiculo.ConcluirManutencao(ordemServicoId, custoRealizado, dataConclusao)`; OS → `Concluida`; `SaveChangesAsync`.
- **Pós-condições:** OS `Concluida`; emite `ManutencaoConcluida`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / OS ≠ `Aberta`).
- **Evento de domínio:** `ManutencaoConcluida(VeiculoId, ordemServicoId, custoRealizado)`.

### 5.5 RegistrarMulta

- **Command:** `RegistrarMultaCommand(Guid VeiculoId, string CodigoInfracaoCtb, decimal Valor, DateOnly DataInfracao, Guid? MotoristaId) : ICommand`.
- **Pré-condições:** `request` não nulo; veículo existe; ativo no acervo (I-5); código CTB e valor informados (I-8).
- **Efeito:** `veiculo.RegistrarMulta(codigoInfracaoCtb, valor, dataInfracao, motoristaId)`; grava `Multa` em `Pendente`; `SaveChangesAsync`.
- **Pós-condições:** nova `Multa`; emite `MultaRegistrada`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado).
- **Evento de domínio:** `MultaRegistrada(VeiculoId, codigoInfracaoCtb, valor, dataInfracao)`.

### 5.6 RegistrarLicenciamento

- **Command:** `RegistrarLicenciamentoCommand(Guid VeiculoId, int Exercicio, decimal ValorIpva, decimal ValorTaxa, DateOnly Data) : ICommand`.
- **Pré-condições:** `request` não nulo; veículo existe; ativo no acervo (I-5); `Exercicio` sem licenciamento `Regular` para o veículo (I-11).
- **Efeito:** `veiculo.RegistrarLicenciamento(exercicio, valorIpva, valorTaxa, data)`; grava `Licenciamento` `Regular`; `SaveChangesAsync`.
- **Pós-condições:** novo `Licenciamento` regular para o exercício.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado / exercício duplicado).
- **Evento de domínio:** — (sem evento publicado nesta versão).

### 5.7 DesignarMotorista

- **Command:** `DesignarMotoristaCommand(Guid VeiculoId, string Nome, string Cnh, string CategoriaCnh, DateOnly ValidadeCnh) : ICommand`.
- **Pré-condições:** `request` não nulo; veículo existe; ativo no acervo (I-5); CNH válida e **não vencida** `ValidadeCnh ≥ hoje` (I-9).
- **Efeito:** `veiculo.DesignarMotorista(nome, cnh, categoriaCnh, validadeCnh, hoje)`; vincula `Motorista`; define `MotoristaAtualId`; `SaveChangesAsync`.
- **Pós-condições:** motorista vinculado/atual.
- **Exceções:** `ArgumentException` (nome/CNH vazios); `InvalidOperationException` (não encontrado / encerrado / CNH vencida).
- **Evento de domínio:** — (sem evento publicado nesta versão).

> **Aspectos contábeis/patrimoniais** (incorporação contábil, tombamento contábil, depreciação,
> reavaliação, impairment, transferência, cessão, baixa e alienação) **não** são comandos do `Veiculo`:
> eles vivem no `BemPatrimonial` **vinculado** (referenciado por `BemPatrimonialId`) e estão descritos em
> `BemPatrimonial.rules.md`. O `Veiculo` apenas reflete o encerramento na sua `Situacao` (I-12).

---

## 6. Consultas (leitura)

### 6.1 ObterVeiculo

- **Query:** `ObterVeiculoQuery(Guid VeiculoId) : IQuery<VeiculoDetalhe>`.
- **Handler:** `ObterVeiculoHandler(IVeiculoRepository veiculos)`.
- **Projeção (DTO):** `VeiculoDetalhe(Guid Id, string? NumeroTombamento, string Placa, string Renavam, int Odometro, decimal Horimetro, decimal ValorContabil, string Situacao, Guid? MotoristaAtualId)`.
- **Filtros:** por `Id`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarAbastecimentosDoVeiculo

- **Query:** `ListarAbastecimentosDoVeiculoQuery(Guid VeiculoId, DateOnly De, DateOnly Ate) : IQuery<IReadOnlyList<AbastecimentoResumo>>`.
- **Projeção (DTO):** `AbastecimentoResumo(Guid Id, DateOnly Data, decimal Litros, decimal Valor, int Odometro)`.
- **Filtros:** por `VeiculoId` e período; tenant-scoped.

### 6.3 ListarMultasPendentes

- **Query:** `ListarMultasPendentesQuery() : IQuery<IReadOnlyList<MultaResumo>>`.
- **Projeção (DTO):** `MultaResumo(Guid Id, Guid VeiculoId, string Placa, string CodigoInfracaoCtb, decimal Valor, DateOnly DataInfracao)`.
- **Filtros:** multas em `Pendente`/`EmRecurso`; tenant-scoped.

### 6.4 ListarLicenciamentosPendentes

- **Query:** `ListarLicenciamentosPendentesQuery(int Exercicio) : IQuery<IReadOnlyList<LicenciamentoResumo>>`.
- **Projeção (DTO):** `LicenciamentoResumo(Guid VeiculoId, string Placa, int Exercicio, decimal ValorIpva, string Situacao)`.
- **Filtros:** veículos sem licenciamento `Regular` no exercício; tenant-scoped.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Patrimonio.Domain.Events`)

Eventos **emitidos pela raiz `Veiculo`** (frota):

| Evento | Payload | Emitido por |
|---|---|---|
| `AbastecimentoRegistrado` | `(VeiculoId, decimal Litros, decimal Valor, DateOnly Data)` | `Veiculo.RegistrarAbastecimento` |
| `ManutencaoConcluida` | `(VeiculoId, ManutencaoOsId OrdemServicoId, decimal CustoRealizado)` | `Veiculo.ConcluirManutencao` |
| `MultaRegistrada` | `(VeiculoId, string CodigoInfracaoCtb, decimal Valor, DateOnly DataInfracao)` | `Veiculo.RegistrarMulta` |

> Os eventos patrimoniais (`BemIncorporado`, `BemTombado`, `BemDepreciado`, `BemBaixado`, `BemReavaliado`)
> são emitidos pelo agregado `BemPatrimonial` **vinculado** (vide `BemPatrimonial.rules.md`), **não** pelo
> `Veiculo`.

### Integração (publica via `*.Contracts` + Outbox; assembly `...Patrimonio.Contracts`)

Publicados pelo lado **patrimonial** (a partir do `BemPatrimonial` vinculado ao veículo):

| Evento | Payload | Publicado por | Destino |
|---|---|---|---|
| `BemIncorporadoIntegrationEvent` | `{ TombamentoId, ValorInicial, Origem }` | `IncorporarVeiculoHandler` (via bem vinculado) | Finanças (variação patrimonial aumentativa) |
| `BemDepreciadoIntegrationEvent` | `{ TombamentoId, ValorDepreciado, Competencia }` | `DepreciarBemHandler` (`BemPatrimonial` vinculado) | Finanças |
| `BemBaixadoIntegrationEvent` | `{ TombamentoId, MotivoBaixa, ValorContabil }` | `BaixarBemHandler` / `AlienarBemHandler` (`BemPatrimonial` vinculado) | Finanças |
| `BemReavaliadoIntegrationEvent` | `{ TombamentoId, NovoValorJusto }` | `ReavaliarBemHandler` / `RegistrarImpairmentHandler` (`BemPatrimonial` vinculado) | Finanças |

### Integração (consome)

| Evento | Payload | Origem | Efeito |
|---|---|---|---|
| `ContratoAssinadoIntegrationEvent` | `{ ContratoId, ... }` | Administracao | Aquisição de veículo via licitação dispara o tombamento (entrada do bem vinculado). |

---

## 8. Validações (FluentValidation)

### IncorporarVeiculoValidator (`AbstractValidator<IncorporarVeiculoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Descricao` | `NotEmpty()` + `MaximumLength(200)` | Descrição obrigatória, máx. 200 caracteres. |
| `ValorInicial` | `GreaterThan(0)` | Valor inicial deve ser positivo. |
| `ValorResidual` | `GreaterThanOrEqualTo(0)` + `LessThanOrEqualTo(x => x.ValorInicial)` | Valor residual não pode exceder o inicial. |
| `VidaUtilMeses` | `GreaterThan(0)` | Vida útil deve ser positiva. |
| `Placa` | `NotEmpty()` + `Matches(<regex placa>)` | Placa inválida. |
| `Renavam` | `NotEmpty()` + `Must(RenavamValido)` | RENAVAM inválido. |

### RegistrarAbastecimentoValidator (`AbstractValidator<RegistrarAbastecimentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `VeiculoId` | `NotEmpty()` | Identificador obrigatório. |
| `Litros` | `GreaterThan(0)` | Litros deve ser positivo. |
| `Valor` | `GreaterThan(0)` | Valor deve ser positivo. |
| `Odometro` | `GreaterThanOrEqualTo(0)` | Odômetro inválido. |

### RegistrarMultaValidator (`AbstractValidator<RegistrarMultaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `VeiculoId` | `NotEmpty()` | Identificador obrigatório. |
| `CodigoInfracaoCtb` | `NotEmpty()` | Código de infração (CTB) obrigatório. |
| `Valor` | `GreaterThan(0)` | Valor da multa deve ser positivo. |

### DesignarMotoristaValidator (`AbstractValidator<DesignarMotoristaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `VeiculoId` | `NotEmpty()` | Identificador obrigatório. |
| `Nome` | `NotEmpty()` + `MaximumLength(150)` | Nome do motorista obrigatório. |
| `Cnh` | `NotEmpty()` | CNH obrigatória. |
| `ValidadeCnh` | `Must(d => d >= hoje)` | CNH vencida não pode ser designada (I-9). |

---

## 9. Persistência (EF Core 8)

- **Schema:** `patrimonio` (isolado por módulo). **DbContext:** o do módulo Patrimonio. **Migrations:** por módulo.
- **Mapeamento por composição:** `Veiculo` é uma **tabela própria** (`Veiculo`), **sem herança** de `BemPatrimonial` (sem TPH/TPT entre eles). O vínculo é uma coluna `BemPatrimonialId` (`Guid`) que referencia a identidade do `BemPatrimonial` correspondente — **referência de identidade entre agregados**, mapeada como valor (sem FK navegável de domínio para outro agregado, conforme regra de agregados). Tabelas filhas de frota: `Abastecimento`, `ManutencaoOS`, `Multa`, `Licenciamento`, `Motorista`.

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `VeiculoId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo interceptor; alvo do Global Query Filter. |
| `BemPatrimonialId` | `Guid` | referência ao `BemPatrimonial` vinculado (registro contábil/patrimonial). |
| `Descricao` | `nvarchar(200)` | descrição do veículo. |
| `NumeroTombamento` | `nvarchar` | tombamento operacional (nulo antes de `Tombar`). |
| `Placa` | `nvarchar(8)` | conversor de `Placa`. |
| `Renavam` | `nvarchar(11)` | conversor de `Renavam`. |
| `Odometro` | `int` | conversor de `Odometro`. |
| `Horimetro` | `decimal` | conversor de `Horimetro`. |
| `MotoristaAtualId` | `Guid?` | motorista designado. |
| `ValorInicial` / `ValorResidual` / `ValorContabil` | `decimal` | conversores de `ValorMonetario` (referência operacional; fonte contábil é o `BemPatrimonial`). |
| `VidaUtilMeses` | `int` | vida útil de referência. |
| `DataIncorporacao` | `date` | data de incorporação. |
| `Origem` | `nvarchar` | origem do ingresso. |
| `EmCondicoesDeUso` | `bit` | condições de uso. |
| `Situacao` | `int`/`nvarchar` | `SituacaoBemPatrimonial` (estado próprio do veículo). |

- **Índices:**
  - PK em `Id`.
  - **Único** em `(TenantId, Renavam)` (I-2) e em `(TenantId, Placa)`.
  - Índice em `(TenantId, BemPatrimonialId)` (lookup do bem vinculado).
  - **Único** em `(VeiculoId, Exercicio)` em `Licenciamento` para situação `Regular` (I-11).
  - Índice em `(TenantId, MotoristaAtualId)` e em `Multa(TenantId, Situacao)`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto Patrimonio para os Integration Events do `BemPatrimonial` vinculado (`BemIncorporado/Depreciado/Baixado/Reavaliado`).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Veiculo` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId`. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição. O `BemPatrimonial` vinculado pertence ao **mesmo tenant**.
- **RBAC (policy-based, negar por padrão; segregação de funções):**
  - Gestão de frota (abastecer, OS, multa, licenciamento, motorista): papéis da **Frota** (ex.: `Patrimonio.Frota.Gerir`).
  - Baixa/alienação do bem vinculado: papel com **autorização** (ex.: `Patrimonio.Bens.Baixar`), segregado de quem opera a frota (comando do `BemPatrimonial`).
  - Consultas: papel de **leitura de frota** (ex.: `Patrimonio.Frota.Ler`).
  - Módulo Patrimonio é **ativável por tenant** (Ambos); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** `Motorista` (nome, **CNH**, validade) é **dado pessoal**; base legal de gestão de frota, minimização nas projeções e **trilha de acesso**. Multas vinculam condutor — acesso auditado.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em **toda** mutação de frota (`RegistrarAbastecimento`, `AbrirOrdemServico`, `ConcluirManutencao`, `RegistrarMulta`, `RegistrarLicenciamento`, `DesignarMotorista`, `Tombar`) — destinada ao Tribunal de Contas (TCE-RS). As operações contábeis são auditadas no `BemPatrimonial` vinculado.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Saída — Finanças (intra-aplicação, via Contracts):** incorporação, depreciação e baixa/alienação do **bem vinculado** publicam Integration Events (Outbox) para variação patrimonial/lançamento contábil em Finanças. Idempotente por `EventId`. **Nunca** há chamada direta.
- **Entrada — Administracao (intra-aplicação):** aquisição de veículo via licitação consome `ContratoAssinadoIntegrationEvent`/recebimento, disparando o tombamento. ACL + idempotência.
- **Gestor de combustível (externo):** cota por veículo e validação de abastecimento integradas ao gestor de combustível. ACL + Polly (timeout, retry, circuit breaker) + idempotência.
- **DETRAN/CTB (externo, previsto):** consulta/baixa de multas, licenciamento e IPVA (Lei 9.503/1997) — **confirmar layout/versão atual** antes de integrar. ACL + idempotência.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Depreciação é responsabilidade do bem vinculado**
- **Dado** um veículo incorporado e tombado, vinculado a um `BemPatrimonial`
- **Quando** o processamento mensal de depreciação é executado sobre o `BemPatrimonial` vinculado
- **Então** o `BemPatrimonial` é depreciado linearmente, sem reduzir o valor contábil abaixo do residual, e o `Veiculo` **não** expõe comando de depreciação próprio (I-1; vide `BemPatrimonial.rules.md`).

**Cenário 2 — Odômetro não pode retroceder**
- **Dado** um veículo com odômetro 50.000 km
- **Quando** registro um abastecimento informando odômetro 49.000 km
- **Então** a operação é rejeitada por odômetro retroativo (I-3).

**Cenário 3 — Abastecimento dentro/fora da cota**
- **Dado** um veículo com cota de combustível definida
- **Quando** registro um abastecimento que ultrapassa a cota vigente
- **Então** o abastecimento é bloqueado (ou marcado para autorização) conforme política do gestor (I-6).

**Cenário 4 — Abastecimento válido**
- **Dado** um veículo ativo, dentro da cota, com odômetro ≥ atual
- **Quando** executo `RegistrarAbastecimentoCommand`
- **Então** o `Abastecimento` é gravado, o odômetro é atualizado e `AbastecimentoRegistrado` é emitido (I-10).

**Cenário 5 — Conclusão de manutenção**
- **Dado** uma OS em situação `Aberta`
- **Quando** executo `ConcluirManutencaoCommand(custoRealizado)`
- **Então** a OS passa a `Concluida` e o evento `ManutencaoConcluida` é emitido (I-7).

**Cenário 6 — Conclusão de OS já concluída**
- **Dado** uma OS em situação `Concluida`
- **Quando** executo `ConcluirManutencaoCommand`
- **Então** ocorre `InvalidOperationException` (OS não está `Aberta`).

**Cenário 7 — Registro de multa**
- **Dado** um veículo ativo no acervo
- **Quando** executo `RegistrarMultaCommand(codigoCtb, valor)`
- **Então** a `Multa` é registrada e `MultaRegistrada` é emitido (I-8).

**Cenário 8 — Motorista com CNH vencida**
- **Dado** um motorista cuja `ValidadeCnh` já passou
- **Quando** executo `DesignarMotoristaCommand`
- **Então** a designação é rejeitada por CNH vencida (I-9).

**Cenário 9 — Operação de frota sobre veículo encerrado**
- **Dado** um veículo `Baixada` (ou `Alienada`) — encerramento espelhado do bem vinculado
- **Quando** executo qualquer operação de frota (abastecer/OS/multa/licenciamento/motorista)
- **Então** a operação é rejeitada por veículo encerrado (I-5).

**Cenário 10 — Licenciamento duplicado por exercício**
- **Dado** um veículo já licenciado (`Regular`) para o exercício corrente
- **Quando** executo novo `RegistrarLicenciamentoCommand` para o mesmo exercício
- **Então** a operação é rejeitada por exercício duplicado (I-11).

**Cenário 11 — RENAVAM único por tenant**
- **Dado** um veículo já incorporado com determinado RENAVAM no tenant
- **Quando** incorporo outro veículo com o mesmo RENAVAM
- **Então** a operação é rejeitada (índice único `(TenantId, Renavam)`) (I-2).

**Cenário 12 — Baixa ocorre no bem vinculado**
- **Dado** um veículo `Tombado` cujo `BemPatrimonial` vinculado não tem laudo anexado
- **Quando** executo `BaixarBemCommand` sobre o `BemPatrimonial` vinculado
- **Então** a baixa é rejeitada e nenhum lançamento contábil é emitido; o `Veiculo` permanece `Tombado` (I-12; vide `BemPatrimonial.rules.md`).

**Cenário 13 — Consulta tenant-scoped**
- **Dado** veículos do tenant A e do tenant B
- **Quando** executo `ObterVeiculoQuery` no contexto do tenant A
- **Então** somente veículos do tenant A são retornados.

---

## 13. Casos de Borda

- **B-1.** Placa/RENAVAM inválidos na incorporação ⇒ rejeitado (I-2 / validator).
- **B-2.** RENAVAM duplicado no tenant ⇒ violação do índice único `(TenantId, Renavam)`.
- **B-3.** Abastecimento com odômetro igual ao atual ⇒ permitido (monotônico **não decrescente**, I-3).
- **B-4.** Abastecimento com horímetro inferior ao atual ⇒ rejeitado (I-4).
- **B-5.** Abastecimento exatamente no limite da cota ⇒ permitido; acima ⇒ bloqueado/autorização (I-6).
- **B-6.** Concluir OS `Cancelada` ⇒ falha (não está `Aberta`).
- **B-7.** Designar motorista com `ValidadeCnh == hoje` ⇒ permitido (válida hoje, I-9).
- **B-8.** Designar motorista com `ValidadeCnh == hoje − 1` ⇒ rejeitado (vencida).
- **B-9.** Operações de frota sobre veículo `EmIncorporacao` (ainda sem tombo) ⇒ rejeitadas (exige ativo no acervo, I-5).
- **B-10.** Multa sem código CTB ou valor ⇒ rejeitada (I-8 / validator).
- **B-11.** Licenciamento de exercício já `Regular` ⇒ rejeitado (I-11).
- **B-12.** Depreciar/baixar/alienar segue exatamente as bordas do `BemPatrimonial` **vinculado** (residual como piso, laudo, avaliação prévia); o `Veiculo` não as executa, apenas reflete o encerramento na `Situacao` (I-1/I-12).
- **B-13.** Integration Events do bem vinculado devem ser idempotentes no consumidor (Finanças) por `EventId`.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 2.0.0 | 2026-06-21 | **Refatoração para COMPOSIÇÃO (DDD):** `Veiculo` deixa de herdar (`é-um`) `BemPatrimonial` e passa a ser **agregado próprio** (`AggregateRoot<VeiculoId>`) **vinculado** a um `BemPatrimonial` por `BemPatrimonialId`. Depreciação/tombamento contábil/reavaliação/impairment/baixa/alienação passam a ser responsabilidade do `BemPatrimonial` vinculado; o `Veiculo` cuida só da operação de frota e reflete o encerramento na sua `Situacao`. Removidas as seções de comandos/propriedades "herdados". Manifesto inalterado. |
| 1.0.0 | 2026-06-21 | Versão inicial — regras derivadas do README do módulo Patrimonio (frota: veículo é-um `BemPatrimonial`; abastecimento sob cota, manutenção/OS, multas, licenciamento/IPVA, motorista/CNH; CTB Lei 9.503/1997 + MCASP/NBC TSP 07). |

<!-- manifest
commands: IncorporarVeiculo, RegistrarAbastecimento, AbrirOrdemServico, ConcluirManutencao, RegistrarMulta, RegistrarLicenciamento, DesignarMotorista
queries: ObterVeiculo, ListarAbastecimentosDoVeiculo, ListarMultasPendentes, ListarLicenciamentosPendentes
domainEvents: AbastecimentoRegistrado, ManutencaoConcluida, MultaRegistrada
integrationEventsPublished: BemIncorporadoIntegrationEvent, BemDepreciadoIntegrationEvent, BemBaixadoIntegrationEvent, BemReavaliadoIntegrationEvent
integrationEventsConsumed: ContratoAssinadoIntegrationEvent
-->

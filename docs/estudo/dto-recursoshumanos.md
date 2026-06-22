# Reconciliação DTO — RecursosHumanos (front `recursoshumanos` × back `RecursosHumanos`)

> Comparação CÓDIGO×CÓDIGO (sem subir o backend). Fontes:
> - Front: `src/Web/src/modules/recursoshumanos/*.api.ts`
> - Back: `src/Modules/RecursosHumanos/.../Infrastructure/RecursosHumanosEndpoints.cs` + Commands/Queries/DTOs em `...Application/**` + enums em `...Domain/**/Enums.cs`
> - Infra de serialização: `src/ApiHost/Program.cs` + `src/Web/src/api/http.ts`

## Premissas de serialização (decidem tudo)

1. **camelCase (default ASP.NET `JsonSerializerDefaults.Web`)** — não há `PropertyNamingPolicy` sobrescrito. Logo `LiquidoAPagar` → `liquidoAPagar`, `NomeServidor` → `nomeServidor`, etc. Os `.api.ts` já usam camelCase. OK.
2. **`JsonStringEnumConverter` global** (`Program.cs:135-136`), registrado SEM `disallowIntegerValues`. Portanto, na LEITURA (request) o backend aceita enum **por número OU por nome**; na ESCRITA (response) emite enum **por nome (string)**.
   - Consequência A: os campos enum de entrada que o front envia como `number` (1,2,3...) **bindam** — não quebram.
   - Consequência B: os campos enum de saída chegam como **string**; todos os DTOs de leitura do front tipam esses campos como `string` (`situacao`, `regime`, `tipo`, `natureza`). OK.
3. **Ordinais dos enums** conferem com o mapeamento hardcoded no front: todos os enums do Domain são numerados explicitamente **a partir de 1** (`Rpps=1/Rgps=2`, `Efetivo=1/Comissionado=2/Temporario=3`, `Provento=1/Desconto=2`, `Natureza 1..4`, `Estatutario=1/Celetista=2`, `Entrada=1/Saida=2`, `RepC=1/RepA=2/RepP=3`). Os comentários `1=...,2=...` do front estão corretos.
4. **`http.ts` não desempacota envelope**: retorna o JSON cru. Endpoints de criação devolvem `{ id }` e o front tipa `CriacaoResponse { id }`. Marcação devolve `{ nsr }` e o front tipa `MarcacaoRegistrada { nsr }`. OK.
5. **`post(path, body, options)`**: a assinatura passa o input como `body` (2º arg). Conferido em todos os `*.api.ts`. OK.

---

## Tabela endpoint → campo front → campo back → status → correção

### Servidores (`servidor.api.ts`)

| Endpoint | Campo front | Campo back | Status | Correção |
|---|---|---|---|---|
| POST `/servidores` | `cpf,matricula,dadosPessoais{nome,dataNascimento},cargoId,regime(number),dataNomeacao` | `AdmitirServidorCommand(Cpf,Matricula,DadosPessoais(Nome,DataNascimento),CargoId,Regime:RegimePrevidenciario,DataNomeacao)` | ok | — (regime number bind via converter) |
| GET `/servidores/ativos` | `ServidorResumo{id,cpf,matricula,nomeServidor,cargoId,regime,situacao,dataNomeacao,dataExercicio}` | `ServidorResumo(Id,Cpf,Matricula,NomeServidor,CargoId,Regime:string,Situacao:string,DataNomeacao,DataExercicio?)` | ok | — |
| GET `/servidores/por-matricula/{matricula}` | `ServidorResumo \| null` | `ServidorResumo?` | ok* | back devolve `Results.Ok(null)` → corpo vazio → `http.ts` retorna `undefined` (não `null`). UI trata ausência por falsy; sem quebra. |
| POST `/servidores/{id}/posse` | `{dataPosse}` | `PossePayload(DataPosse)` | ok | — |
| POST `/servidores/{id}/exercicio` | `{dataExercicio}` | `ExercicioPayload(DataExercicio)` | ok | — |
| POST `/servidores/{id}/estabilidade` | (sem body) | (sem body) | ok | — |
| POST `/servidores/{id}/afastamento` | `{inicio,fim?,motivo}` | `AfastamentoPayload(Inicio,Fim?,Motivo)` | ok | — |
| POST `/servidores/{id}/desligamento` | `{dataDesligamento,motivo}` | `DesligamentoPayload(DataDesligamento,Motivo)` | ok | — |

### Cargos (`cargo.api.ts`)

| Endpoint | Campo front | Campo back | Status | Correção |
|---|---|---|---|---|
| POST `/cargos` | `denominacao,tipo(number),vencimento,lotacao{inscricaoEstabelecimento,denominacaoUnidade,codigoLotacaoTributaria?},quantidadeVagas,leiCriacao,planoDeCargosId?` | `CriarCargoCommand(Denominacao,Tipo:TipoCargo,Vencimento,LotacaoDto(InscricaoEstabelecimento,DenominacaoUnidade,CodigoLotacaoTributaria?),QuantidadeVagas,LeiCriacao,PlanoDeCargosId?)` | ok | — |
| GET `/cargos/com-vagas?tipo=` | `CargoResumo{id,denominacao,tipo,vagasDisponiveis}` + query `tipo` (string) | `CargoResumo(Id,Denominacao,Tipo:string,VagasDisponiveis)` ; query `TipoCargo? tipo` | ok | — (query enum por nome aceita) |
| GET `/cargos/{id}` | `CargoDetalhe{id,denominacao,tipo,vencimento,lotacao,regime,quantidadeVagas,vagasOcupadas,situacao,leiCriacao}` | `CargoDetalhe(Id,Denominacao,Tipo,Vencimento,Lotacao:string,Regime,QuantidadeVagas,VagasOcupadas,Situacao,LeiCriacao)` | ok | — |
| POST `/cargos/{id}/provimento` | (sem body) | (sem body) | ok | — |
| POST `/cargos/{id}/vacancia` | (sem body) | (sem body) | ok | — |
| POST `/cargos/{id}/vencimento` | `{novoVencimento}` | `AlterarVencimentoPayload(NovoVencimento)` | ok | — |
| POST `/cargos/{id}/extincao` | `{leiExtincao}` | `ExtinguirCargoPayload(LeiExtincao)` | ok | — |

### Folha (`folha.api.ts`)

| Endpoint | Campo front | Campo back | Status | Correção |
|---|---|---|---|---|
| POST `/folhas` | `{ano,mes}` | `AbrirFolhaCommand(Ano,Mes)` | ok | — |
| GET `/folhas/por-competencia?ano&mes` | `FolhaResumo{id,competencia,situacao,totalProventos,totalDescontos,totalLiquido,dataFechamento}` | `FolhaResumo(Id,Competencia:string,Situacao:string,TotalProventos,TotalDescontos,TotalLiquido,DataFechamento?)` | ok | — |
| POST `/folhas/{id}/eventos` | `{servidorId,rubrica,tipo(number),baseCalculo,valor}` | `EventoPayload(ServidorId,Rubrica,Tipo:TipoEvento,BaseCalculo,Valor)` | ok | — |
| POST `/folhas/{id}/apuracao-legal` | (sem body) | (sem body) | ok | — |
| POST `/folhas/{id}/calculo` | (sem body) | (sem body) | ok | — |
| POST `/folhas/{id}/fechamento` | (sem body) | (sem body) | ok | — |
| POST `/folhas/{id}/pagamento` | `{dataPagamento}` | `PagamentoPayload(DataPagamento)` | ok | — |
| GET `/folhas/{id}/servidores/{sid}/contracheque` | `Contracheque{servidorId,competencia,linhas[{rubrica,tipo,valor}],totalProventos,totalDescontos,liquidoAPagar}` | `ContrachequeDto(ServidorId,Competencia,Linhas[LinhaContracheque(Rubrica,Tipo,Valor)],TotalProventos,TotalDescontos,LiquidoAPagar)` | ok | — (`LiquidoAPagar`→`liquidoAPagar` por camelCase) |

### Rubricas (`rubrica.api.ts`)

| Endpoint | Campo front | Campo back | Status | Correção |
|---|---|---|---|---|
| POST `/rubricas` | `{codigo,descricao,natureza(number),anoVigencia,mesVigencia,incideInss,incideRpps,incideIrrf,incideFgts,valorFixo?,percentual?}` | `CriarRubricaCommand(Codigo,Descricao,Natureza:int,AnoVigencia,MesVigencia,IncideInss,IncideRpps,IncideIrrf,IncideFgts,ValorFixo?,Percentual?)` | ok | — (`Natureza` é `int` no back; nº casa direto) |
| GET `/rubricas/vigentes?ano&mes` | `RubricaResumo{id,codigo,descricao,natureza,incideInss,incideRpps,incideIrrf,incideFgts}` | `RubricaDto(Id,Codigo,Descricao,Natureza:string,IncideInss,IncideRpps,IncideIrrf,IncideFgts)` | ok | — (front nomeia o tipo `RubricaResumo`, mas os CAMPOS batem 1:1; nome do interface TS é irrelevante para o bind) |

### Tabelas Legais (`tabelaLegal.api.ts`)

| Endpoint | Campo front | Campo back | Status | Correção |
|---|---|---|---|---|
| POST `/tabelas-legais/semear-federais` | (sem body) | (sem body) | ok | — |
| POST `/tabelas-legais/rpps` | `{anoVigencia,mesVigencia,faixas[{limiteInferior,limiteSuperior,aliquota}],teto?,baseLegal}` | `CriarTabelaRppsCommand(AnoVigencia,MesVigencia,Faixas[FaixaProgressivaDto(LimiteInferior,LimiteSuperior,Aliquota)],Teto?,BaseLegal)` | ok | — (aliquota em fração decimal 0..1 em ambos) |

### Ponto (`ponto.api.ts`)

| Endpoint | Campo front | Campo back | Status | Correção |
|---|---|---|---|---|
| POST `/ponto/jornadas` | `{servidorId,cargaDiariaMinutos,intervaloMinutos,regime(number),vigenciaInicio,toleranciaMinutos}` | `DefinirJornadaCommand(ServidorId,CargaDiariaMinutos,IntervaloMinutos,Regime:int,VigenciaInicio,ToleranciaMinutos)` | ok | — |
| POST `/ponto/marcacoes` | `{servidorId,dataHora,sentido(number),origem?(number)}` → `{nsr}` | `RegistrarMarcacaoCommand(ServidorId,DataHora:DateTimeOffset,Sentido:int,Origem?:int)` → `{nsr:long}` | ok | — |
| POST `/ponto/apuracoes` | `{servidorId,ano,mes}` → `ApuracaoResultado{id, +minutos*? opcionais}` | `ApurarJornadaCommand(ServidorId,Ano,Mes)` → `{ id }` **apenas** | ok* | back devolve só `{ id }`; campos de espelho do front são OPCIONAIS e a UI (`EspelhoPonto.tsx`) guarda cada um com `!== undefined`. Sem quebra; ver "Risco" abaixo. |
| POST `/ponto/apuracoes/{id}/fechamento` | (sem body, 204) | (204) | ok | — |
| GET `/ponto/afd?inicio&fim&assinar?` | download binário (fetch direto) | `Results.File(...)` | ok | — (front usa `fetch` direto, não o http client JSON) |
| GET `/ponto/aej?ano&mes&assinar?` | download binário (fetch direto) | `Results.File(...)` | ok | — |

---

## Veredito

**Divergências reais que QUEBRAM o bind da UI: 0.**

Os `.api.ts` deste módulo foram escritos **contra o contrato real** (`RecursosHumanosEndpoints.cs`), não inferidos do padrão — os comentários citam o endpoint e o `{ id }`/`{ nsr }` corretos. As três condições que normalmente causam falha de bind estão cobertas:

- **Nomes de campo**: 100% camelCase, batem 1:1 (inclusive `liquidoAPagar`, `nomeServidor`, `vagasDisponiveis`).
- **Enums por inteiro**: salvos por `JsonStringEnumConverter` (aceita número na leitura) + ordinais do Domain começando em 1, coerentes com os comentários do front. Resposta de enum vem como string e o front tipa string.
- **Envelopes**: `{ id }` / `{ nsr }` corretamente tipados; `http.ts` não desempacota nada que precise.

### Itens de atenção (não quebram bind, mas pesam no demo)

1. **`POST /ponto/apuracoes` não devolve o espelho** (só `{ id }`). A `PontoServidorPage`/`EspelhoPonto` foram desenhadas para mostrar minutos trabalhados/extras/falta e banco de horas, mas **não há GET de apuração nem campos de espelho na resposta** — o operador só vê "apurada (id ...)". No demo o "espelho do ponto" fica vazio até gerar o AEJ (download). **Impacto de PoC alto** (a tela parece incompleta), embora tecnicamente não seja erro de DTO. Correção sugerida: NÃO mexer no `.api.ts`; é lacuna de endpoint — enriquecer a resposta de `ApurarJornadaCommand` (ou criar `GET /ponto/apuracoes/{id}`) no backend para popular os campos opcionais que o front já prevê.
2. **`por-matricula` e `contracheque` retornam `Results.Ok(null)`** → corpo vazio → `http.ts` entrega `undefined` (não `null`). As telas tratam por falsy, então não quebra; mas se alguma tela fizer comparação estrita `=== null`, falharia. Verificar usos em `ServidorDetailPage`/`ContrachequeModal` (atualmente OK).

### As que mais pesam no demo (prioridade)

1. **Espelho de ponto vazio** (item 1) — é o que um avaliador de PoC mais notaria na trilha M5 de ponto. Resolver no backend, não no front.
2. Nenhuma outra: o restante do módulo (servidores, cargos, folha, rubricas, tabelas) binda integralmente.

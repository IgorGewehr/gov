# Reconciliação de DTOs — Módulo Legislativo (front `legislativo` × back `Legislativo`)

Comparação **estática de código** (sem subir API). Front em
`src/Web/src/modules/legislativo/*.api.ts` × back em
`src/Modules/Legislativo/.../Application/**/*.cs` + `Infrastructure/LegislativoEndpoints.cs`.

Premissas de serialização (de `src/ApiHost/Program.cs`):
- **camelCase** (default do Minimal API/System.Text.Json).
- **`JsonStringEnumConverter` global** → enums saem como **string (nome)** na resposta. Logo os
  campos de saída do tipo enum chegam como `"Aberta"`, `"Lei"`, etc. (o front trata como `string`, OK).
- Entradas com enum tipado como `int` na command → front manda `int` numérico, OK.
- `DateOnly` serializa como `"yyyy-MM-dd"`; `DateTimeOffset` como ISO-8601. Front tipa ambos como `string`, OK.

Legenda STATUS: **ok** | **divergente** (quebra bind/parse) | **ausente** (campo existe de um lado só).

---

## 1. Vereadores (`vereadores.api.ts` × `Vereadores/*`)

### GET `/vereadores` e `/vereadores/{id}` → `VereadorResumo` (back)
| Campo front (`VereadorResumo`/`VereadorDetalhe`) | Campo back (`VereadorResumo`) | STATUS | Correção |
|---|---|---|---|
| `id` | `Id` | ok | — |
| `nomeParlamentar` | `NomeParlamentar` | ok | — |
| `partido` | `Partido` | ok | — |
| `legislatura: string` | `legislaturaInicio: int` + `legislaturaFim: int` | **divergente** | back não tem `legislatura`. Em `vereadores.api.ts` trocar `legislatura: string` por `legislaturaInicio: number; legislaturaFim: number` e derivar o label na UI (`${ini}–${fim}`). |
| `cargoMesa` | `CargoMesa` (string) | ok | — |
| `situacao` | `Situacao` (string) | ok | — |
| `nomeCivil` (só em `VereadorDetalhe`) | `NomeCivil` (já vem no Resumo) | ok | — |

> Obs.: o endpoint `/vereadores/{id}` devolve **`VereadorResumo`** (não um "detalhe" com campos extras). O front declara `VereadorDetalhe extends VereadorResumo { nomeCivil }`, mas `nomeCivil` já está no Resumo — funciona, porém o "extends" é redundante.

### POST `/vereadores` → `CadastrarVereadorCommand` (back)
| Campo front (`VereadorInput`) | Campo back (`CadastrarVereadorCommand`) | STATUS | Correção |
|---|---|---|---|
| `nomeCivil` | `NomeCivil` | ok | — |
| `nomeParlamentar` | `NomeParlamentar` | ok | — |
| `partido` | `Partido` | ok | — |
| `legislatura: string` | `legislaturaInicio: int` + `legislaturaFim: int` | **divergente** | back ignora `legislatura` e fica **sem `LegislaturaInicio/Fim`** (obrigatórios) → 400/validação. Separar em dois campos numéricos no `VereadorInput` + form. |
| `cargoMesa: number` | `CargoMesa: int` | ok | — |
| `situacao: number` | — (não existe no cadastro) | **ausente** | back **não** aceita `situacao` no cadastro. Campo extra é ignorado (sem quebra), mas o form pede algo que não persiste. Remover `situacao` do input de cadastro. |

### PUT `/vereadores/{id}` → `EditarVereadorPayload` (back)
| Campo front (`VereadorInput`) | Campo back (`EditarVereadorPayload`) | STATUS | Correção |
|---|---|---|---|
| `nomeCivil` / `nomeParlamentar` / `partido` | idem | ok | — |
| `cargoMesa: number` | `CargoMesa: int` | ok | — |
| `situacao: number` | `Situacao: int` | ok (edição aceita) | — |
| `legislatura` | — | **ausente** | back não usa no editar; campo extra ignorado. OK funcionalmente. |

---

## 2. Comissões (`comissoes.api.ts` × `Comissoes/*`)

### GET `/comissoes` → `ComissaoResumo`
| Campo front | Campo back (`ComissaoResumo`) | STATUS | Correção |
|---|---|---|---|
| `id` / `nome` / `tipo` | idem | ok | — |
| `totalMembros` | `TotalMembros` | ok | — |
| — | `Situacao` (extra do back) | ausente (no front) | sem quebra; UI poderia exibir. |

### GET `/comissoes/{id}` → `ComissaoDetalhe`
| Campo front (`ComissaoDetalhe`) | Campo back (`ComissaoDetalhe`) | STATUS | Correção |
|---|---|---|---|
| `id` / `nome` / `tipo` | idem | ok | — |
| `finalidade: string` | **não existe** (back tem `Situacao`, `PresidenteVereadorId`) | **divergente** | back não devolve `finalidade`. UI mostrará `undefined`. Remover `finalidade` de `ComissaoDetalhe` ou mapear de outra fonte. |
| `membros[].vereadorId` | `Membros[].VereadorId` | ok | — |
| `membros[].vereadorNome` | **não existe** (back: `Papel`, `Cargo`) | **divergente** | back NÃO envia o nome do vereador no membro (só `VereadorId`, `Papel`, `Cargo`). Lista de membros aparece sem nome. Ajustar `MembroComissaoResumo` para `{ vereadorId; papel; cargo }` e resolver nome via `useVereadores()` no client, OU pedir join no back. |
| `membros[].cargo` | `Membros[].Cargo` (string) | ok | — |
| — | `Membros[].Papel` (extra) | ausente (no front) | back distingue Papel × Cargo; front só lê `cargo`. |

### POST `/comissoes` → `CriarComissaoCommand`
| Campo front (`ComissaoInput`) | Campo back (`CriarComissaoCommand`) | STATUS | Correção |
|---|---|---|---|
| `nome` | `Nome` | ok | — |
| `tipo: number` | `Tipo: int` | ok | — |
| `finalidade: string` | **não existe** | **ausente** | back só aceita `Nome` + `Tipo`. `finalidade` é ignorado. Remover do input/form (ou o back precisa passar a aceitar). |

### POST `/comissoes/{id}/membros` → `DesignarMembroPayload`
| Campo front (`MembroInput`) | Campo back (`DesignarMembroPayload`) | STATUS | Correção |
|---|---|---|---|
| `vereadorId` | `VereadorId` | ok | — |
| `cargo: number` | `Cargo: int` | ok | — |
| — | `Papel: int` (**obrigatório** no back) | **divergente (ausente)** | back exige `Papel` (não-nullable). Sem ele → 400. Adicionar `papel: number` ao `MembroInput` e ao form. **Pesa para o demo** (designar membro quebra). |

---

## 3. Normas (`normas.api.ts` × `Normas/*`)

### GET `/normas` (paginado) → `PaginaNormas`
| Campo front (`PaginaResultado<T>`) | Campo back (`PaginaNormas`) | STATUS | Correção |
|---|---|---|---|
| `itens` | `Itens` | ok | — |
| `total` | `Total` | ok | — |
| `pagina` | `Pagina` | ok | — |
| `tamanhoPagina` | `Tamanho` | **divergente** | nome do campo difere (`tamanhoPagina` × `tamanho`). Em `legislativo.shared.ts` renomear `tamanhoPagina` → `tamanho`. (Só quebra se a UI ler `tamanhoPagina`; lista em si bina por `itens`.) |

### `NormaResumo` (itens)
| Campo front (`NormaResumo`) | Campo back (`NormaResumo`) | STATUS | Correção |
|---|---|---|---|
| `id` / `tipo` / `ano` / `ementa` / `situacao` | idem | ok | — |
| `numero: string` | `Numero: int` | **divergente (tipo)** | back manda número; front tipa string. JSON number→string causa render estranho/`.padStart` quebra. Trocar para `numero: number` em `NormaResumo`. |
| `dataPublicacao` | `DataPromulgacao` | **divergente** | back não tem `dataPublicacao`; o campo de data é `dataPromulgacao`. Renomear no front (`NormaResumo` e `NormaDetalhe`). UI mostra data vazia hoje. |

### GET `/normas/{id}` → `NormaDetalhe`
| Campo front (`NormaDetalhe`) | Campo back (`NormaDetalhe`) | STATUS | Correção |
|---|---|---|---|
| herdados de `NormaResumo` | (ver acima: `numero`, `dataPublicacao`) | **divergente** | mesmas correções de `numero`/`dataPromulgacao`. |
| `textoIntegral` | `TextoArticulado` | **divergente** | nome diverge. Renomear `textoIntegral` → `textoArticulado`. Texto da norma fica vazio na UI. |
| `proposicaoId` | `ProposicaoOrigemId` | **divergente** | renomear `proposicaoId` → `proposicaoOrigemId`. |
| `revogaNormas[]` / `alteraNormas[]` | **não existem** | **divergente** | back devolve `historico: EventoVigenciaDto[]` (`{ tipo, data, normaReferenciaId, observacao }`), não duas listas separadas. Substituir por `historico` e derivar revoga/altera por `tipo`. |
| — | `dataRevogacao` (extra) | ausente (no front) | adicionar se a UI precisar. |

### POST `/normas` → `CadastrarNormaCommand`
| Campo front (`NormaInput`) | Campo back (`CadastrarNormaCommand`) | STATUS | Correção |
|---|---|---|---|
| `tipo: number` | `Tipo: int` | ok | — |
| `numero: string` | `Numero: int` | **divergente (tipo)** | back espera int; mandar string pode falhar a desserialização → 400. Trocar para `numero: number`. |
| `ano: number` | `Ano: int` | ok | — |
| `ementa` | `Ementa` | ok | — |
| `textoIntegral` | `TextoArticulado` | **divergente** | renomear → `textoArticulado` (senão texto não persiste). |
| `dataPublicacao` | `DataPromulgacao` | **divergente** | renomear → `dataPromulgacao` (senão data não persiste). **Pesa** (cadastro de norma). |
| — | `ProposicaoOrigemId` (opcional) | ausente | OK omitir. |

### POST `/normas/{id}/revogacao` → `RevogarNormaPayload`
| Campo front (`NormaAcaoInput`) | Campo back (`RevogarNormaPayload`) | STATUS | Correção |
|---|---|---|---|
| `normaAfetadaId` | `NormaRevogadoraId` | **divergente** | nomes e semântica divergem. Endpoint é `POST /normas/{normaId}/revogacao` e o back grava que **{normaId} foi revogada** por `NormaRevogadoraId`. Front manda `normaAfetadaId` → back recebe `null` em `NormaRevogadoraId`. Além disso o back **exige `DataRevogacao: DateOnly` (não-nullable)** que o front NÃO envia. |
| `justificativa` | — | **ausente** | back não tem `justificativa`. |
| — | `DataRevogacao` (**obrigatório**) | **ausente no front** | adicionar `dataRevogacao` ao input. **Quebra** sem ela. |

> Ação sugerida em `normas.api.ts`: `NormaAcaoInput` para revogação = `{ dataRevogacao: string; normaRevogadoraId?: string }`.

### POST `/normas/{id}/alteracao` → `RegistrarAlteracaoNormaPayload`
| Campo front (`NormaAcaoInput`) | Campo back (`RegistrarAlteracaoNormaPayload`) | STATUS | Correção |
|---|---|---|---|
| `normaAfetadaId` | `NormaAlteradoraId` (**obrigatório**) | **divergente** | renomear / remapear. |
| `justificativa` | — | ausente | back ignora. |
| — | `DataReferencia: DateOnly` (**obrigatório**) | **ausente no front** | adicionar `dataReferencia`. **Quebra** sem ela. |

---

## 4. Diário Oficial (`diario.api.ts` × `DiarioOficial/*`)

### GET `/diario/edicoes` — **envelope divergente**
| Front | Back | STATUS | Correção |
|---|---|---|---|
| `listarEdicoes()` espera `EdicaoResumo[]` (array puro) | back devolve `PaginaEdicoes { itens, total, pagina, tamanho }` | **divergente** | a chamada retorna um objeto, não um array → `.map` na lista quebra (`edicoes.map is not a function`). Ajustar `listarEdicoes` para `http.get<PaginaResultado<EdicaoResumo>>` e usar `.itens`. **Pesa muito no demo** (tela de edições não renderiza). |

### `EdicaoResumo`
| Campo front | Campo back | STATUS | Correção |
|---|---|---|---|
| `id` / `numero` / `situacao` | idem | ok | — |
| `totalMaterias` | `TotalMaterias` | ok | — |
| `dataReferencia: string` | **não existe** (back tem `ano: int`, `dataPublicacao`) | **divergente** | back não tem `dataReferencia`. Trocar por `ano: number` (+ `dataPublicacao?: string`). |

### POST `/diario/edicoes` → `AbrirEdicaoDiarioCommand`
| Campo front (`EdicaoInput`) | Campo back (`AbrirEdicaoDiarioCommand`) | STATUS | Correção |
|---|---|---|---|
| `numero: number` | — (back **gera** o número sequencial) | **divergente (extra)** | back só aceita `{ ano }`; `numero` é calculado. Enviar `numero` é ignorado. |
| `dataReferencia: string` | — | **divergente (extra)** | ignorado. |
| — | `Ano: int` (**obrigatório**, 1900–2100) | **ausente no front** | sem `ano` → 400/validação. Trocar `EdicaoInput` para `{ ano: number }`. **Pesa** (abrir edição quebra). |

### GET `/diario/edicoes/{id}` → `EdicaoDetalhe`
| Campo front (`EdicaoDetalhe`) | Campo back (`EdicaoDetalhe`) | STATUS | Correção |
|---|---|---|---|
| `id` / `numero` / `situacao` | idem | ok | — |
| `dataReferencia: string` | **não existe** (back: `ano`, `dataPublicacao`) | **divergente** | trocar por `ano` (+ usar `dataPublicacao`). |
| `publicadaEm: string\|null` | `DataPublicacao` | **divergente** | renomear `publicadaEm` → `dataPublicacao`. |
| `materias[].id/ordem/tipo/titulo` | idem | ok | — |
| `materias[].conteudo: string` | `Conteudo: string?` | ok (pode vir null) | tornar `conteudo: string \| null`. |
| — | `materias[].referenciaId` (extra) | ausente (front) | opcional. |
| — | `edicaoOriginalId` (extra) | ausente (front) | opcional. |

### POST `/diario/edicoes/{id}/materias` → `AdicionarMateriaPayload`
| Campo front (`MateriaInput`) | Campo back (`AdicionarMateriaPayload`) | STATUS | Correção |
|---|---|---|---|
| `tipo: number` | `TipoMateria: int` | **divergente** | nome diverge (`tipo` × `tipoMateria`). Back recebe `tipoMateria = 0/undefined`. Renomear `tipo` → `tipoMateria` no `MateriaInput`. **Pesa** (adicionar matéria). |
| `titulo` | `Titulo` | ok | — |
| `conteudo: string` | `Conteudo: string?` | ok | — |
| — | `ReferenciaId` (opcional) | ausente | OK omitir. |

---

## 5. Tribuna (`tribuna.api.ts` × `Tribuna/*`)

### GET `/sessoes/{id}/tribuna` → `TribunaDto`
| Campo front (`TribunaPainel`) | Campo back (`TribunaDto`) | STATUS | Correção |
|---|---|---|---|
| `sessaoId` | `SessaoId` | ok | — |
| `situacaoSessao: string` | **não existe** | **divergente** | back não devolve `situacaoSessao` (tem `tribunaId`, `tempoPadraoSegundos`, `oradorAtualId`). O `refetchInterval` do polling depende de `situacaoSessao === 'Aberta'` → **nunca faz polling**, painel "congela". Buscar a situação via `useSessao` ou expor `situacaoSessao` no back. **Pesa no demo ao vivo.** |
| `inscricoes[]` | `Inscricoes[]` | ok (estrutura) | ver campos abaixo |

### `InscricaoResumo` (item) × `InscricaoDto` (back)
| Campo front | Campo back | STATUS | Correção |
|---|---|---|---|
| `id` | `InscricaoId` | **divergente** | back manda `inscricaoId`, não `id`. Os comandos de cronômetro usam `inscricao.id` → vira `undefined`, URL `/inscricoes/undefined/inicio`. Renomear front `id` → `inscricaoId` (ou ler `inscricaoId`). **Pesa** (controlar fala quebra). |
| `ordem` | `Ordem` | ok | — |
| `vereadorId` | `VereadorId` | ok | — |
| `vereadorNome: string` | **não existe** | **divergente** | back não envia nome do orador. Lista da tribuna sem nome. Resolver via `useVereadores()` ou join no back. |
| `situacao` | `Situacao` | ok | — |
| `tempoConcedidoSegundos: number` | `TempoConcedidoSegundos: double` | ok | — |
| `tempoConsumidoSegundos: number` | `TempoUtilizadoSegundos: double?` | **divergente** | nome diverge (`consumido` × `Utilizado`) e back é nullable. Cronômetro mostra `undefined`/0. Renomear → `tempoUtilizadoSegundos` (e tratar null). |
| `iniciadoEm: string\|null` | `IniciadoEm: DateTimeOffset?` | ok | — |
| — | `fase` (string, extra) | ausente (front) | back distingue fase do orador. |
| — | `pausada: bool` (extra) | ausente (front) | útil p/ UI do cronômetro. |
| — | `excedenteSegundos: double?` (extra) | ausente (front) | opcional. |

### POST `/sessoes/{id}/tribuna/inscricoes` → `InscreverOradorPayload`
| Campo front (`InscricaoInput`) | Campo back (`InscreverOradorPayload`) | STATUS | Correção |
|---|---|---|---|
| `vereadorId` | `VereadorId` | ok | — |
| `tempoConcedidoSegundos: number` | `TempoConcedidoSegundos: int?` | ok | — |
| — | `TribunaId: Guid` (**obrigatório**) | **ausente no front** | back precisa do `tribunaId` (id da tribuna, não da sessão). Front só manda `vereadorId`+tempo → 400. Buscar `tribunaId` de `useTribuna()` e incluir no payload. **Pesa** (inscrever orador quebra). |
| — | `Fase: int` (**obrigatório**) | **ausente no front** | sem `fase` → 400. Adicionar `fase: number` ao input. **Pesa.** |

### POST `/sessoes/{id}/tribuna/inscricoes/{insId}/{verbo}` → `TribunaControlePayload`
| Front | Back (`TribunaControlePayload`) | STATUS | Correção |
|---|---|---|---|
| `comandar()` faz POST **sem body** | back exige `{ TribunaId: Guid }` no corpo | **divergente** | back lê `payload.TribunaId`; sem body → 400/`tribunaId` vazio. Passar `{ tribunaId }` no corpo dos comandos início/pausa/retomada/encerramento. **Pesa** (controle do cronômetro quebra). |
| verbo `inicio` | rota `/inicio` | ok | — |

> Front não implementa `cancelamento` da inscrição (back tem). Ausência, sem quebra.

---

## 6. Proposições (`proposicao.api.ts` × `Proposicoes/*`) — majoritariamente OK

### GET `/proposicoes?situacao` → `ProposicaoResumo`
| Campo front | Campo back | STATUS |
|---|---|---|
| `id`/`tipo`/`ementa`/`situacao` | idem | ok |
| `dataApresentacao: string` | `DataApresentacao: DateOnly` | ok |

### GET `/proposicoes/{id}` → `ProposicaoDetalhe`
| Campo front | Campo back | STATUS |
|---|---|---|
| `id/tipo/ementa/autoria/regime/protocolo/dataApresentacao/situacao/numeroAutografo` | idem | ok |
| `tramitacoes[]` (`id/fase/comissao/parecerFavoravel/data`) | `Tramitacoes[]` (`Id/Fase/Comissao/ParecerFavoravel/Data`) | ok |

### POST `/proposicoes` → `ApresentarProposicaoCommand`
| Campo front (`ApresentarProposicaoInput`) | Back | STATUS |
|---|---|---|
| `tipo`/`ementa`/`autoria`/`regime` | `Tipo`/`Ementa`/`Autoria`/`Regime` | ok |

Emendas (`texto`/`autoria`), Pareceres (`comissao`/`favoravel`), Deliberação (`votacaoId`),
Autógrafo (`numeroAutografo`): **todos ok** (batem com os payloads do `LegislativoEndpoints`).
Resposta de criação `{ id }` bate com `CriacaoResponse`. **Bloco sem divergências.**

---

## 7. Sessões (`sessao.api.ts` × `Sessoes/*`) — OK

### `SessaoResumo` (GET `/sessoes/agendadas`)
| Campo front | Campo back | STATUS |
|---|---|---|
| `id`/`tipo`/`situacao` | idem | ok |
| `dataHora: string` | `DataHora: DateTimeOffset` | ok |

> `useSessoesAgendadas` chama `/sessoes/agendadas` que retorna `SessaoResumo[]` (array puro). OK.

### `SessaoDetalhe` (GET `/sessoes/{id}`)
| Campo front | Campo back | STATUS |
|---|---|---|
| `id/tipo/dataHora/totalMembros/quorumInstalacao/presentes/situacao` | idem | ok |
| `ordemDoDia[]` (`proposicaoId/ordem`) | `OrdemDoDia[]` (`ProposicaoId/Ordem`) | ok |

`AgendarSessaoInput` (`tipo/dataHora/totalMembros`) × `AgendarSessaoCommand` → ok.
`RegistrarPresencaInput` (`vereadorId`), `IncluirNaOrdemDoDiaInput` (`proposicaoId`) → ok.
Quórum: front lê `{ quorumAtingido }` e back devolve `new { quorumAtingido = ... }` → ok.
`PresencaResumo` (`vereadorId/registradaEm`) → ok (back `RegistradaEm`).
`AtaSessao` (`sessaoId/conteudo/geradaEm`): ver §8.

---

## 8. Votações (`votacao.api.ts` × `Votacoes/*`)

### GET `/votacoes/{id}` → `VotacaoDetalhe`
| Campo front | Campo back (`VotacaoDetalhe`) | STATUS | Correção |
|---|---|---|---|
| `id/sessaoId/proposicaoId/tipo/maioriaExigida/totalMembros/presentes/turno/situacao/resultado` | idem | ok | — |
| `votosSim`/`votosNao`/`abstencoes` | `VotosSim`/`VotosNao`/`Abstencoes` | ok | — |
| `maioriaExigida: string` | `MaioriaExigida: string` | ok (enum→string) | — |
| `turno: number` | `Turno: int` | ok | — |

### POST `/votacoes` → `IniciarVotacaoCommand`
| Campo front (`IniciarVotacaoInput`) | Back | STATUS |
|---|---|---|
| `sessaoId/proposicaoId/tipo/maioriaExigida/totalMembros/presentes/turno` | idem (`MaioriaExigida: int`) | ok — front manda `maioriaExigida: number`, back recebe int ✔ |

### GET `/votacoes?sessaoId` → `VotacaoResumo`
| Campo front (`VotacaoResumo`) | Campo back (`VotacaoResumo`) | STATUS | Correção |
|---|---|---|---|
| `id`/`proposicaoId`/`tipo`/`situacao`/`resultado` | idem | ok | — |
| — | `sessaoId`/`maioriaExigida`/`turno`/`totalVotos` (extras do back) | ausente (front) | sem quebra (campos a mais ignorados). |

### GET `/votacoes/{id}/placar` → `PlacarVotacao`
| Campo front | Campo back | STATUS |
|---|---|---|
| `votacaoId/sim/nao/abstencao/totalVotos/situacao` | idem | ok |

### GET `/votacoes/{id}/painel` → `PainelVotacao`
| Campo front (`PainelVotacao`) | Campo back (`PainelVotacao`) | STATUS | Correção |
|---|---|---|---|
| `votacaoId/sim/nao/abstencao/totalVotos/presentes/situacao` | idem | ok | — |
| `ausentes` | `Ausentes` | ok | — |
| `quorumMinimo` | `QuorumMinimo` | ok | — |
| `quorumAtingido` | `QuorumAtingido` | ok | — |
| `resultadoParcial: string` | `ResultadoParcial: string` | ok | — |
| `votos[]` (`vereadorId/nomeVereador/sentido`) | `Votos[]` (`VereadorId/NomeParlamentar/Sentido/RegistradoEm`) | **divergente** | back manda `nomeParlamentar`, front lê `nomeVereador` → nome em branco no painel ao vivo. Renomear `nomeVereador` → `nomeParlamentar` em `VotoNominalPainel`. **Pesa no demo (painel eletrônico).** |
| — | `totalMembros`/`resultadoApurado` (extras do back) | ausente (front) | opcional. |

> `usePainelVotacao` faz polling enquanto `situacao === 'Aberta'`. Back devolve enum como string `"Aberta"` → ok.

### GET `/votacoes/{id}/votos-nominais` → `VotoResumo`
| Campo front | Campo back | STATUS |
|---|---|---|
| `vereadorId/sentido/registradoEm` | `VereadorId/Sentido/RegistradoEm` | ok |

### POST `/votacoes/{id}/votos` → `RegistrarVotoPayload`
| Campo front (`RegistrarVotoInput`) | Back (`RegistrarVotoPayload`) | STATUS |
|---|---|---|
| `votoId/vereadorId/sentido` | `VotoId/VereadorId/Sentido(int)` | ok |

Encerramento: front lê `{ resultado }`, back devolve `new { resultado = ... }` → ok.

---

## Resumo executivo

**Total de divergências reais que afetam o bind/parse da UI: 27** (excluindo
campos meramente "extras no back" que o front ignora sem quebrar). Distribuição:
Vereadores 3 · Comissões 3 · Normas 9 · Diário 6 · Tribuna 6 · Votações 1 ·
(Proposições e Sessões: 0).

### As que mais pesam no demo de PoC (quebra visível / tela não funciona)
1. **Diário – lista de edições**: front espera array, back manda envelope paginado `{ itens, ... }` → a tela de edições **não renderiza** (`.map` em objeto). *(diario.api.ts: `listarEdicoes`)*
2. **Diário – abrir edição**: back exige `{ ano }`; front manda `{ numero, dataReferencia }` → **400**, não cria edição. *(diario.api.ts: `EdicaoInput`)*
3. **Diário – adicionar matéria**: campo `tipo` × `tipoMateria` → matéria entra com tipo errado/zero. *(diario.api.ts: `MateriaInput`)*
4. **Tribuna – controle do cronômetro**: comandos início/pausa/retomada/encerramento vão **sem body**, back exige `{ tribunaId }`; e o id da inscrição é lido como `id` mas o back manda `inscricaoId` → URL com `undefined`. Painel ao vivo da tribuna **não controla**. *(tribuna.api.ts: `comandar`, `InscricaoResumo.id`)*
5. **Tribuna – inscrever orador / polling**: payload sem `tribunaId`+`fase` (400) e `situacaoSessao` inexistente trava o auto-refresh. *(tribuna.api.ts: `InscricaoInput`, `TribunaPainel`)*
6. **Votação – painel eletrônico**: `nomeVereador` × `nomeParlamentar` → **nomes em branco no painel ao vivo** (a tela "estrela" do legislativo). *(votacao.api.ts: `VotoNominalPainel`)*
7. **Comissões – designar membro**: falta `papel` (obrigatório) → **400**. *(comissoes.api.ts: `MembroInput`)*
8. **Vereadores – cadastro**: `legislatura: string` único × `legislaturaInicio/Fim` obrigatórios → **400** ao cadastrar. *(vereadores.api.ts: `VereadorInput`)*
9. **Normas – cadastro/edição**: `textoIntegral`×`textoArticulado`, `dataPublicacao`×`dataPromulgacao`, `numero` string×int, e revogação/alteração com campos de data obrigatórios ausentes → cadastro/ações de norma **não persistem ou dão 400**. *(normas.api.ts)*

> Bom sinal: **Proposições e Sessões estão alinhadas** (0 divergências) — o fluxo central
> "protocolar → pautar → abrir sessão → votar" funciona; os pontos críticos restantes são
> Tribuna, Diário, Comissões, Normas e o **nome no painel de votação**.

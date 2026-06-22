# Reconciliação DTO — Módulo Tributos (front × back)

Comparação CÓDIGO×CÓDIGO entre os `*.api.ts` do frontend
(`src/Web/src/modules/tributos/`) e os endpoints + Commands/Queries reais do backend
(`src/Modules/Tributos/.../Infrastructure/*Endpoints.cs` + handlers em `.Application`).
**Nada foi executado** (sem dotnet/5080). Os DTOs do front foram INFERIDOS do "padrão" e
divergem do contrato real em vários pontos que QUEBRAM o bind/serialização.

## Fatos de borda (serialização) que mudam o veredito

1. **Enums = STRING no JSON.** `ApiHost/Program.cs:135-136` registra
   `JsonStringEnumConverter` global via `ConfigureHttpJsonOptions`. Logo:
   - **Resposta:** enums saem como **nome** (`"Iptu"`, `"Residencial"`, `"Inscrita"`). As
     projeções do front que tipam esses campos como string (`SituacaoDividaAtiva`,
     `UsoImovel`, etc.) estão certas.
   - **Request:** o `JsonStringEnumConverter` padrão (não-strict) **também aceita inteiro**
     na desserialização. Por isso enviar `tipoTributo: 1` ou `uso: 1` ainda BINDA quando o
     alvo é de fato um `enum`. O problema é quando o front manda int para um campo que **não
     é enum** (ver PadraoConstrutivo) ou quando o **valor inteiro está fora do range** do
     enum por ordem divergente.
2. **camelCase / case-insensitive.** STJ desserializa case-insensitive por padrão; o http
   client (`api/http.ts`) faz `JSON.stringify(body)` sem transformar chaves. Logo nomes em
   camelCase do front bindam nos records PascalCase — DESDE QUE o nome bata. Onde o **nome do
   campo difere**, o valor chega `null`/default e o comando falha (validação ou
   `NullReferenceException`/erro de invariante).

---

## tributos/api.ts × TributosEndpoints.cs

| Endpoint | Campo front | Campo back | Status | Correção (em api.ts) |
|---|---|---|---|---|
| POST /contribuintes/pessoa-fisica | cpf, nome, inscricaoMunicipal | Cpf, Nome, InscricaoMunicipal | ok | — |
| POST /lancamentos | tipoTributo: number | TipoTributo (enum) | ok (int aceito) | — (ver nota enum) |
| POST /lancamentos | ano, mes, valorPrincipal, vencimento | Ano, Mes, ValorPrincipal, Vencimento (DateOnly) | ok | garantir `vencimento` = `"yyyy-MM-dd"` (DateOnly) |
| POST /dividas/{id}/cda | numeroCda | NumeroCda | ok | — |
| GET /contribuintes/{id}/dividas-ativas | DividaAtivaResumo.* | DividaAtivaResumo (back) | ok | — (campos batem 1:1) |
| (enum SituacaoDividaAtiva) | ordem: Inscrita,CdaEmitida,Protestada,**Parcelada**,Quitada,Cancelada,Prescrita | **1**Inscrita,2CdaEmitida,3Protestada,**4 EmExecucaoFiscal**,5Parcelada,6Quitada,7Cancelada | divergente (só importa se usar int) | resposta é string → tipo `string`; **adicionar `'EmExecucaoFiscal'` e remover `'Prescrita'`** do union p/ não quebrar `switch`/labels |
| TipoTributo (TIPO_TRIBUTO_VALOR) | falta Cosip/ContribuicaoMelhoria | 5=ContribuicaoMelhoria, 6=Cosip | divergente (incompleto) | inofensivo p/ bind; completar o map se UI listar tipos |

**Observação chave:** o módulo "padrão-ouro" (`api.ts`) é o ÚNICO realmente alinhado. As
divergências de enum aqui só pesam se a UI **enviar/interpretar inteiros**; como a resposta é
string e o request também aceita string, o risco no demo é baixo — exceto o union de
`SituacaoDividaAtiva` (label/exibição inconsistente).

---

## tributos/iptu.api.ts × IptuEndpoints.cs (+ CadastrarImovel / LancarIptuAnual / Pgv / Aliquotas)

### POST /imoveis — `CadastrarImovelInput` × `CadastrarImovelCommand` — **QUEBRA TOTAL**

| Campo front | Campo back | Status | Correção |
|---|---|---|---|
| contribuinteId | **ProprietarioId** | **divergente (nome)** | renomear p/ `proprietarioId` |
| inscricaoImobiliaria | **InscricaoMunicipal** | **divergente (nome)** | renomear p/ `inscricaoMunicipal` |
| logradouro | Logradouro | ok | — |
| numero | Numero (opcional) | ok | — |
| bairro | Bairro | ok | — |
| zona | **ZonaFiscal** | **divergente (nome)** | renomear p/ `zonaFiscal` |
| uso: number | **TipoUso** (enum TipoUsoImovel) | **divergente (nome + valor)** | renomear p/ `tipoUso`; ver enum abaixo |
| padrao: number | **PadraoConstrutivo: string** | **divergente (nome + TIPO)** | renomear p/ `padraoConstrutivo` e enviar **string** ("Normal"), NÃO int |
| anoConstrucao | AnoConstrucao (opcional) | ok | — |
| areaTerreno | AreaTerreno | ok | — |
| areaConstruida | AreaConstruida | ok | — |
| — (ausente) | **SetorQuadraLote** (obrigatório, NotEmpty) | **ausente** | adicionar `setorQuadraLote: string` (req. validador) |
| — (ausente) | CibCodigo? | ausente (opcional) | opcional — adicionar se UI tiver |
| — (ausente) | MatriculaRgi? | ausente (opcional) | opcional |
| — (ausente) | Cep? | ausente (opcional) | opcional |
| — (ausente) | FaceQuadra? | ausente (opcional) | opcional |
| — (ausente) | FracaoIdeal (default 1) | ausente (default) | opcional |

> **Enum TipoUsoImovel (back):** 1 Residencial, 2 Comercial, 3 Industrial, 4 Servicos,
> **5 Misto, 6 Territorial**. O front `USO_IMOVEL_VALOR` tem `Territorial: 5` (deveria ser 6)
> e **não tem Misto** → mandar `5` grava **Misto** em vez de Territorial; "Territorial=5"
> seleciona o uso errado. Como enum aceita string, o mais seguro é enviar **`tipoUso:
> "Territorial"`** (nome). Correção mínima: alinhar o map (`Misto:5, Territorial:6`).

> **PadraoConstrutivo NÃO é enum no back — é `string`** (validado `NotEmpty/MaxLength(30)`).
> Enviar `padrao: 2` (int) faz a request falhar a desserialização (string esperada) ou gravar
> "2". Front deve mandar o texto do padrão (`"Normal"`).

### GET /contribuintes/{id}/imoveis — `ImovelResumo` (resposta) — **divergente**

| Campo front | Campo back (ImovelResumo) | Status |
|---|---|---|
| id | Id | ok |
| contribuinteId | **(ausente no back)** | **extra no front → undefined** |
| inscricaoImobiliaria | **InscricaoMunicipal** | **divergente (nome)** |
| logradouro | Logradouro | ok |
| numero | **(ausente)** | extra → undefined |
| bairro | **(ausente)** | extra → undefined |
| zona | **ZonaFiscal** | **divergente (nome)** |
| uso | **TipoUso** (string) | **divergente (nome)** |
| padrao | **(ausente)** | extra → undefined |
| anoConstrucao | **(ausente)** | extra → undefined |
| areaTerreno | AreaTerreno | ok |
| areaConstruida | AreaConstruida | ok |
| — | **CibCodigo?** | ausente no front |
| — | **Ativo: bool** | ausente no front |

> A tabela de imóveis (`ImovelListPage.tsx`) lê `inscricaoImobiliaria` → vem `undefined`
> (back manda `inscricaoMunicipal`). Coluna em branco no demo. Corrigir a interface
> `ImovelResumo` p/ `inscricaoMunicipal`, `zonaFiscal`, `tipoUso`, e ajustar os acessores.

### GET /imoveis/{id}/iptu/{exercicio} — `ApuracaoIptu` (resposta) — **divergente**

Back retorna `ResultadoIptu`. Mapa front→back:

| Campo front | Campo back | Status |
|---|---|---|
| imovelId | ImovelId | ok |
| exercicio | Exercicio | ok |
| valorTerreno | ValorTerreno | ok |
| valorConstrucao | ValorConstrucao | ok |
| valorVenal | ValorVenal | ok |
| aliquotaPercentual | AliquotaPercentual | ok |
| impostoBruto | ImpostoBruto | ok |
| impostoDevido | ImpostoDevido | ok |
| descontoCotaUnica | **(ausente)** — back tem **ValorIsencao** | **divergente/ausente** |
| valorCotaUnica | **(ausente)** | **ausente no back** |
| memoria: linha[] | **(ausente)** — back é achatado, sem `memoria` | **ausente no back** |
| — | **ValorDesconto** | ausente no front |

> O front espera `valorCotaUnica`, `descontoCotaUnica` e um array `memoria[]` que o back
> **não retorna**. `ApurarIptuPage` que renderizar memória de cálculo mostra vazio/`map` de
> undefined (risco de crash se `.map` em undefined). Corrigir `ApuracaoIptu` para espelhar
> `ResultadoIptu` (`valorIsencao`, `valorDesconto`, sem `memoria`).

### POST /imoveis/{id}/iptu/lancar — `LancarIptuInput` × `LancarIptuPayload` — **divergente**

| Campo front | Campo back (LancarIptuPayload) | Status | Correção |
|---|---|---|---|
| exercicio | Exercicio | ok | — |
| quantidadeParcelas | **NumeroParcelas** (default 1) | **divergente (nome)** | renomear p/ `numeroParcelas` |
| vencimentoPrimeiraParcela | **PrimeiroVencimento** | **divergente (nome)** | renomear p/ `primeiroVencimento` |
| — | **PercentualIsencao** (default 0) | ausente (default) | opcional |
| — | **PercentualDesconto** (default 0) | ausente (default) | opcional |

> Os dois campos obrigatórios da UI (`numeroParcelas`, `primeiroVencimento`) chegam `null`
> no back → cai no default 1 parcela e **`PrimeiroVencimento` (DateOnly não-anulável) =
> 0001-01-01** ou erro de bind. O lançamento sai errado no demo. Resposta `LancamentoIptuResultado`
> do front (`valorTotal`, `parcelas[]`) **não bate** com `ResultadoLancamentoIptu`
> (`lancamentoId`, `damId`, `impostoDevido`) — sem `parcelas`. Corrigir a interface.

### POST /pgv — `PublicarPgvInput` × `ConfigurarPlantaValoresCommand` — **divergente**

| Campo front | Campo back | Status | Correção |
|---|---|---|---|
| exercicio | Exercicio | ok | — |
| — (ausente) | **FundamentoLegal** (NotEmpty) | **ausente** | adicionar `fundamentoLegal: string` |
| zonas[].zona | Zonas[].**ZonaFiscal** | **divergente (nome)** | `zonaFiscal` |
| zonas[].vut | Zonas[].**ValorM2Terreno** | **divergente (nome)** | `valorM2Terreno` |
| zonas[].vuc | Zonas[].**ValorM2Construcao** | **divergente (nome)** | `valorM2Construcao` |
| fatores[].chave | Fatores[].Chave | ok | — |
| fatores[].descricao | **(ausente)** — back tem **Tipo** (enum TipoFatorPgv) | **divergente/ausente** | trocar `descricao` por `tipo` |
| fatores[].fator | Fatores[].**Multiplicador** | **divergente (nome)** | `multiplicador` |

> Sem `fundamentoLegal` o validador rejeita (NotEmpty) → **400 garantido**. Zonas com `zona/vut/vuc`
> não bindam (back quer `zonaFiscal/valorM2Terreno/valorM2Construcao`) → zonas vazias → "Defina
> ao menos uma zona". PGV não publica no demo.

### POST /iptu/aliquotas — `PublicarAliquotasInput` × `ConfigurarTabelaAliquotaIptuCommand` — **QUEBRA TOTAL**

O front modela uma estrutura COMPLETAMENTE diferente (regime, predial×territorial num só
payload, desconto, parcelas). O back é **uma tabela por chamada** (Edificado bool) com faixas
`(ValorVenalMinimo, ValorVenalMaximo, AliquotaPercentual)`.

| Campo front | Campo back | Status |
|---|---|---|
| exercicio | Exercicio | ok |
| regime: number | **(ausente)** | **extra** |
| aliquotaPredial / aliquotaTerritorial | **(ausente)** | **extra** |
| faixasPredial / faixasTerritorial | **Faixas** (uma lista; `Edificado` define qual) | **divergente** |
| faixa.ateValorVenal | **ValorVenalMinimo + ValorVenalMaximo** | **divergente** (back tem min E max) |
| faixa.aliquota | AliquotaPercentual | divergente (nome) |
| descontoCotaUnica / quantidadeParcelas | **(ausente)** | **extra** |
| — | **Edificado: bool** (NotEmpty implícito) | **ausente** |
| — | **FundamentoLegal** (NotEmpty) | **ausente** |

> Reescrever `PublicarAliquotasInput` para `{ exercicio, edificado, fundamentoLegal,
> faixas: { valorVenalMinimo, valorVenalMaximo, aliquotaPercentual }[], publicar? }` e chamar
> **duas vezes** (predial e territorial). Como está, **400** (sem FundamentoLegal/Faixas válidas).

---

## tributos/iss.api.ts × IssItbiEndpoints.cs

### POST /iss/aliquotas — `ConfigurarAliquotasIssInput` × `ConfigurarTabelaAliquotaIssCommand` — **divergente**

| Campo front | Campo back | Status | Correção |
|---|---|---|---|
| exercicio | **VigenciaInicioAaaaMm** (AAAAMM, ≥190001) | **divergente (nome + semântica)** | renomear p/ `vigenciaInicioAaaaMm` e enviar AAAAMM (ex.: 202601), não o ano |
| — (ausente) | **FundamentoLegal** (NotEmpty) | **ausente** | adicionar `fundamentoLegal` |
| itens[].itemListaServico | ItemListaServico | ok | — |
| itens[].descricao | **(ausente)** | **extra** | remover (ou inócuo, ignorado) |
| itens[].aliquota | **AliquotaPercentual** | **divergente (nome)** | `aliquotaPercentual` |
| itens[].retencao | **RetencaoObrigatoria** | **divergente (nome)** | `retencaoObrigatoria` |
| itens[].substituicao | **SubstituicaoTributaria** | **divergente (nome)** | `substituicaoTributaria` |

> `exercicio` (ex.: 2026) cai em `VigenciaInicioAaaaMm` → 2026 < 190001 → **400** no validador.
> Mesmo corrigindo o nome, a UI precisa montar AAAAMM. `aliquota/retencao/substituicao` não
> bindam → itens com alíquota 0 e sem retenção.

### POST /iss/contribuintes/{id}/apurar — `ApurarIssInput` × `ApurarIssPayload` — **divergente**

| Campo front | Campo back (ApurarIssPayload) | Status | Correção |
|---|---|---|---|
| competencia: string | **Ano: int + Mes: int** | **divergente (estrutura)** | trocar por `{ ano, mes, vencimentoIssProprio }` |
| — | **VencimentoIssProprio: DateOnly** (NotEmpty) | **ausente** | adicionar |

> O back NÃO tem `competencia` — quer `ano`, `mes` e `vencimentoIssProprio`. Enviar
> `{ competencia }` → `Ano=0, Mes=0` → `Competencia.De(0,0)` lança / validador (Mes 1..12) **400**.
> Resposta `ApuracaoIss` do front (`competencia, quantidadeNotas, totais{}, livro[]`...) NÃO
> bate com `ResultadoApuracaoIss` (`apuracaoId, lancamentoId, quantidadeNotas, issProprio,
> issRetido, issSubstituicao`). Sem `competencia`/`livro`/`totais` na resposta → o
> `setQueryData(issKeys.apuracao(id, resultado.competencia))` usa `undefined` na key e a tela
> de livro fica vazia. Corrigir interface de resposta.

### POST /iss/nfse/sincronizar — `SincronizarNfseInput` × `SincronizarNfsePayload` — **QUEBRA TOTAL**

| Campo front | Campo back (SincronizarNfsePayload) | Status |
|---|---|---|
| contribuinteId: string | **Prestadores: string[]** (lista de CNPJs) | **divergente** |
| competencia: string | **Desde: DateOnly** | **divergente** |

> O back quer `{ prestadores: string[], desde: "yyyy-MM-dd" }`; resposta é `{ importadas: int }`,
> não `{ notasRecebidas, notasNovas, ... }`. Reescrever input e o tipo de resposta
> (`SincronizacaoNfseResultado` → `{ importadas: number }`).

---

## tributos/itbi.api.ts × IssItbiEndpoints.cs

### POST /itbi/aliquotas — `ConfigurarAliquotasItbiInput` × `ConfigurarAliquotaItbiCommand` — **divergente**

| Campo front | Campo back | Status | Correção |
|---|---|---|---|
| exercicio | Exercicio | ok | — |
| aliquotaGeral | **AliquotaGeralPercentual** | **divergente (nome)** | `aliquotaGeralPercentual` |
| aliquotaSfh | **AliquotaSfhFinanciadaPercentual** | **divergente (nome)** | `aliquotaSfhFinanciadaPercentual` |
| — | **FundamentoLegal** (NotEmpty) | **ausente** | adicionar `fundamentoLegal` |

> Sem `fundamentoLegal` → **400**. Alíquotas não bindam (nomes) → gravam 0.

### GET /itbi/imoveis/{id}/preview — `PreviewItbi` (resposta) × `ResultadoItbi` — **divergente**

Query string `?exercicio&valorDeclarado` — OK (back lê `int exercicio, decimal valorDeclarado`).

| Campo front | Campo back (ResultadoItbi) | Status |
|---|---|---|
| imovelId, exercicio | ImovelId, Exercicio | ok |
| valorVenalReferencia | ValorVenalReferencia | ok |
| valorDeclarado | ValorDeclarado | ok |
| baseCalculo | BaseCalculo | ok |
| sfh: bool | **(ausente)** — back tem **BaseFoiValorVenal** | **divergente/ausente** |
| aliquotaPercentual | AliquotaPercentual | ok |
| impostoDevido | ImpostoDevido | ok |
| memoria[] | **(ausente)** | **ausente no back** |
| — | **BaseFoiValorVenal, ImpostoBruto, ValorIsencao** | ausentes no front |

> `sfh` e `memoria[]` vêm `undefined`. Se a tela renderiza `memoria.map(...)` → risco de crash.
> Corrigir `PreviewItbi` p/ `baseFoiValorVenal, impostoBruto, valorIsencao` e remover `memoria/sfh`.

### POST /itbi/lancar — `LancarItbiInput` × `LancarItbiCommand` — **QUEBRA TOTAL**

| Campo front | Campo back | Status | Correção |
|---|---|---|---|
| imovelId | ImovelId | ok | — |
| exercicio | Exercicio | ok | — |
| valorDeclarado | ValorDeclarado | ok | — |
| sfh: bool | **UsarAliquotaSfh** | **divergente (nome)** | `usarAliquotaSfh` |
| transmitente: string | **TransmitenteId: Guid** | **divergente (nome/tipo)** | `transmitenteId` (Guid) |
| adquirente: string | **AdquirenteId: Guid** | **divergente (nome/tipo)** | `adquirenteId` (Guid) |
| — | **Vencimento: DateOnly** (NotEmpty) | **ausente** | adicionar `vencimento` |
| — | PercentualIsencao (default 0) | ausente (default) | opcional |

> `transmitente/adquirente` (front parece mandar nome textual) → o back quer **GUIDs** dos
> contribuintes; `vencimento` obrigatório ausente → bind/validação falha. Lançamento de ITBI
> não funciona no demo. Resposta `LancamentoItbiResultado` do front (`guiaNumero, vencimento`)
> ≠ `ResultadoLancamentoItbi` (`transmissaoId, lancamentoId, damId, baseCalculo,
> baseFoiValorVenal, impostoDevido`) — sem `guiaNumero`. Corrigir interface de resposta.

---

## Veredito

**Endpoints com divergência real: 13 de 14** (único 100% alinhado: o "padrão-ouro" `api.ts`,
e mesmo ele tem o union `SituacaoDividaAtiva` desalinhado p/ exibição). Praticamente todos os
`*.api.ts` inferidos de IPTU/ISS/ITBI quebram.

### As que mais pesam no demo (ordem de impacto)

1. **POST /imoveis (CadastrarImovel)** — 4 campos com nome errado (`proprietarioId`,
   `inscricaoMunicipal`, `zonaFiscal`, `tipoUso`), `setorQuadraLote` **obrigatório ausente**,
   e `padrao` enviado como **int** num campo que é **string**. Cadastrar imóvel — porta de
   entrada do M6 — falha. **Sem imóvel, não há IPTU nem ITBI no demo.**
2. **POST /itbi/lancar** — `transmitenteId/adquirenteId` (Guid) com nome/tipo errados +
   `vencimento` obrigatório ausente. Lançar ITBI quebra.
3. **POST /iptu/aliquotas** — payload com estrutura inteiramente diferente (regime/predial×
   territorial) vs. `{ edificado, fundamentoLegal, faixas[] }`. **400**; sem alíquota não há
   apuração de IPTU.
4. **POST /pgv** — `fundamentoLegal` ausente (NotEmpty → 400) + zonas com `zona/vut/vuc` que
   não bindam. Sem PGV não há valor venal → IPTU/ITBI = 0.
5. **POST /iss/contribuintes/{id}/apurar** e **POST /iss/aliquotas** — `competencia` em vez de
   `ano/mes/vencimentoIssProprio`, e `exercicio` em vez de `vigenciaInicioAaaaMm` + nomes de
   item errados. ISS inteiro quebra (400).
6. **Respostas divergentes (GET imóveis, ApuracaoIptu, ApuracaoIss, PreviewItbi)** — campos
   `undefined` em tabelas/telas de memória de cálculo; `memoria.map(...)` sobre `undefined`
   pode **crashar** a página no demo.

Prioridade de correção: **#1 e #4/#3 primeiro** (destravam todo o fluxo IPTU/ITBI), depois
ISS (#5) e os tipos de resposta (#6).

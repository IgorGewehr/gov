# W9.4 — PROTOCOLO avançado: Carimbo ACT RFC 3161 + Temporalidade CONARQ + Fix do NUP

> **Autoridade base:** `docs/architecture/m9-prep/M9-BREAKDOWN.md` (W9.4).
> **Disciplina CLAUDE.md:** domínio rico, integrações atrás de **ACL + Polly**, imutabilidade/WORM/hash-chain
> **já existem** na fundação — este design **reusa**, não recria. Multi-tenant por padrão; tudo
> parametrizável por tenant; nenhuma regra/prazo *hardcoded*.
> **Estado atual ancorado no código** (lido em 2026-06-23), não nas fotos pré-M0:
> `Documento`/`Processo`/`Movimentacao`/`Despacho`, VOs `CarimboDeTempo`/`Assinatura`/`Nup`/`Classificacao`/`Prazo`,
> `CarimboDeTempoLocalService` (relógio local), `NupSequencialGenerator` (race condition), `UltimoSeloReader`
> (padrão atômico UPDLOCK/HOLDLOCK do hash-chain a espelhar).
> **Versões fixadas:** DOC-ICP-12 **v2.1** (Res. CG ICP-Brasil 188/2021), RFC **3161** + ESSCertIDv2 (RFC 5816),
> e-ARQ Brasil **v2** (Res. CONARQ 50/2022), Res. CONARQ **37/2012** (diretrizes de gestão arquivística SIGAD),
> Portaria AN/MGI **174/2024**, Decreto **8.539/2015** (NUP), Lei **14.063/2020** + Decreto **10.543/2020**.
> **READ-ONLY:** este documento é design. Nenhum `dotnet`/`npm` rodado; a :5080 não foi tocada.

---

## 0. O que já existe (não relistar como pendente) e o que muda

| Peça atual | Estado | O que W9.4 faz |
|---|---|---|
| VO `CarimboDeTempo(InstanteUtc, Autoridade)` | Modelado, fino | **Estende** para portar o TST real (token + hash carimbado + serial + policy + algoritmo) sem quebrar o existente |
| `ICarimboDeTempoService.GerarAsync()` | Porta sem TSQ/hash de entrada | **Substituída** por `ICarimbadorDeTempo` (recebe o hash a carimbar); a antiga vira *adapter* legado |
| `CarimboDeTempoLocalService` (relógio local) | Placeholder declarado | **Vira `CarimbadorDeTempoLocalService` (fallback/dev)** — sem mudança de comportamento, só de contrato/posição |
| `Classificacao` (string codigo) | VO fino | Ganha o **Plano de Classificação** configurável por tenant como agregado de referência |
| `Prazo` (art. 66) | Calcula prazo de decisão | Reusado como base; **motor de temporalidade** adiciona TTD (guarda/destinação) |
| `NupSequencialGenerator` (COUNT+1) | **Race condition** | **Corrigido** com padrão atômico do hash-chain (UPDLOCK/HOLDLOCK ou sequência por tenant) |
| WORM `INSTEAD OF UPDATE/DELETE`, hash-chain (`UltimoSeloReader`), Outbox resiliente, Key Vault/A1 (M2) | ✅ fundação | **Reusados** — destinação/eliminação e carimbo amarram-se a hash + assinatura + WORM existentes |

**Fronteira M9 × M10 (W9.4):** no M9 entregam-se **a porta `ICarimbadorDeTempo` + ACL RFC 3161 + persistência do TST + motor de temporalidade completo + fix do NUP**, tudo testável contra **WireMock/stub**. O **carimbo ACT real** (ACT credenciada contratada + custo por carimbo = decisão comercial) e a assinatura qualificada em produção ficam no **M10**. O `CarimbadorDeTempoLocalService` permanece como fallback/dev.

---

## PEÇA 1 — CARIMBO DE TEMPO RFC 3161 REAL (ACL + Polly contra ACT ICP-Brasil)

### 1.1 Protocolo (o que a ACL traduz)
RFC 3161 (TSP — *Time-Stamp Protocol*): o cliente monta um **TSQ** (`TimeStampReq`) contendo o
`MessageImprint` = `{ AlgorithmIdentifier (SHA-256), hashedMessage = SHA-256(conteúdo) }`, um `nonce`
aleatório (anti-replay) e `certReq=true` (exige o certificado da ACT no token). A ACT responde com um
**TST** (`TimeStampResp`): um `PKIStatusInfo` + um `TimeStampToken` (CMS `SignedData` cujo `eContent` é o
`TSTInfo` — contém `genTime`, `serialNumber`, `policy`, o `MessageImprint` ecoado e o `nonce`).

O cliente **valida** antes de aceitar: (a) `PKIStatus` = `granted`/`grantedWithMods`; senão lê
`PKIFailureInfo` (badAlg, badRequest, badDataFormat, timeNotAvailable, unacceptedPolicy,
unacceptedExtension, addInfoNotAvailable, systemFailure); (b) o `MessageImprint` do TSTInfo **bate** com
o hash enviado; (c) o `nonce` ecoado é o enviado; (d) a cadeia/assinatura CMS do token encadeia até uma
**AC do tempo credenciada ICP-Brasil** (DOC-ICP-12 v2.1) e o EKU `id-kp-timeStamping` está presente;
(e) `genTime` dentro da janela tolerada. Falha em qualquer item ⇒ **não persiste** e propaga erro de domínio.

### 1.2 Portas e ACL (camadas)

**Application — `Tensorroot.Gov.Modules.Protocolo.Application/Abstractions/ICarimbadorDeTempo.cs`** (nova porta, substitui `ICarimboDeTempoService`):

```csharp
/// <summary>
/// Autoridade de Carimbo do Tempo (ACT) credenciada ICP-Brasil (RFC 3161 / DOC-ICP-12 v2.1).
/// Recebe o HASH do documento (nunca o conteúdo — privacidade: a ACT não vê o arquivo) e devolve o
/// TST validado vinculado a esse hash. I/O idempotente (nonce), resiliente (Polly) e atrás de ACL.
/// </summary>
public interface ICarimbadorDeTempo
{
    /// <param name="hashDocumento">SHA-256 (hex) do conteúdo a carimbar — o MessageImprint do TSQ.</param>
    Task<CarimboDeTempo> CarimbarAsync(Hash hashDocumento, CancellationToken cancellationToken);
}
```

**Domain — `CarimboDeTempo` (VO) estendido** (sem quebrar o `De(instante, autoridade)` atual; adiciona a *factory* `DeToken`):

```csharp
public sealed class CarimboDeTempo : ValueObject
{
    // existentes: InstanteUtc, Autoridade
    public string? TokenBase64 { get; }      // TST (CMS) DER em Base64 — a prova oponível ao TCE
    public string? SerialToken { get; }       // serialNumber do TSTInfo
    public string? PoliticaCarimbo { get; }   // policy OID (TSA policy ICP-Brasil)
    public string AlgoritmoHash { get; }      // "SHA-256"
    public string HashCarimbado { get; }      // o hash que a ACT atestou (== Hash do documento)
    public OrigemCarimbo Origem { get; }       // Act | Local (fallback/dev)

    public static CarimboDeTempo DeToken(
        DateTime genTimeUtc, string autoridade, string tokenBase64, string serialToken,
        string politica, string hashCarimbado); // valida não-vazios + comprimentos

    // De(instante, autoridade) mantido → Origem = Local (fallback)
}
public enum OrigemCarimbo { Local = 1, Act = 2 }
```

**Infrastructure — ACL `Tensorroot.Gov.Modules.Protocolo.Infrastructure/Protocolo/Carimbo/`:**

- `CarimbadorDeTempoAct : ICarimbadorDeTempo` — **adapter de produção**. Monta o TSQ
  (`BouncyCastle.Cryptography` — `TimeStampRequestGenerator`/`TimeStampResponse`), faz `POST` ao
  endpoint da ACT com `Content-Type: application/timestamp-query`, lê `application/timestamp-reply`,
  **valida** (status/PKIFailureInfo/imprint/nonce/cadeia/EKU/genTime) e mapeia para `CarimboDeTempo.DeToken`.
  Recebe um `HttpClient` nomeado cuja resiliência (`AddStandardResilienceHandler` — retry idempotente,
  circuit breaker, timeout) é configurada no `ProtocoloModule` — **mesmo padrão do `AdnNfseGateway`**.
- `CarimbadorDeTempoLocalService : ICarimbadorDeTempo` — **fallback/dev** (o atual
  `CarimboDeTempoLocalService` renomeado): usa `TimeProvider`, `Origem = Local`, sem token. Em produção
  **só entra se o tenant não tiver ACT configurada** (e o documento de criticidade **Alta/qualificada**
  deve **recusar** carimbo local — invariante abaixo).
- `OpcoesAct` (`IOptions`, por tenant): `Endpoint`, `PoliticaEsperada` (OID), `EmissoresConfiaveis`
  (thumbprints das AC do tempo ICP-Brasil), `ToleranciaGenTime`, `TimeoutSegundos`. Endpoint/credencial
  da ACT em **Key Vault** (M2), nunca no repositório.
- `ExcecaoCarimboDeTempo : DomainException` — carrega o `PKIFailureInfo` mapeado (erro explícito, CLAUDE.md §8).

### 1.3 Persistência do TST (vinculado ao hash)
O `DocumentoConfiguration` já mapeia `CarimboTempo` via `OwnsOne`. **Estender o mapeamento** (não nova tabela):
adicionar colunas `CarimboTokenBase64` (`nvarchar(max)`/blob — o CMS DER é grande), `CarimboSerial`,
`CarimboPolitica`, `CarimboAlgoritmo`, `CarimboHashCarimbado`, `CarimboOrigem`. **Invariante de vínculo:**
`HashCarimbado == Documento.Hash.Valor` — checado na `Assinar`. O TST fica sob **WORM/hash-chain**
existentes (a linha do `Documento` é imutável após juntada — I-4). Migration nova
`AddTstCarimboDeTempo` (apenas `AddColumn`, nullable — *additive*, sem quebrar dados existentes).

### 1.4 Onde a porta é chamada
`AssinarDocumentoHandler` (Application) já recebe um `CarimboDeTempo` e chama `Documento.Assinar(...)`.
Trocar a dependência `ICarimboDeTempoService` → `ICarimbadorDeTempo.CarimbarAsync(documento.Hash, ct)`.
O domínio (`Documento.Assinar`) ganha a invariante: **criticidade Alta ⇒ exige `Origem == Act`**
(carimbo local não satisfaz ato qualificado ICP-Brasil). `Documento.VerificarIntegridade` ganha par:
`VerificarCarimbo()` revalida `HashCarimbado == Hash` (oponibilidade ao TCE).

### 1.5 Teste (M9, sem credencial)
WireMock servindo `application/timestamp-reply` pré-gravados (tokens de stub gerados com BouncyCastle e
um par de chaves de teste): casos `granted`, `rejection` com cada `PKIFailureInfo`, imprint divergente,
nonce divergente, cadeia não-confiável, `genTime` fora da janela, timeout/circuit-breaker (Polly).
Cobertura: a invariante "Alta exige Act" e o vínculo hash↔token.

---

## PEÇA 2 — MOTOR DE TEMPORALIDADE / DESTINAÇÃO (e-ARQ v2 / CONARQ)

> Realiza o que hoje é só **enum/conceito**. Plano de Classificação configurável + metadados arquivísticos
> + TTD com cálculo CORRENTE → INTERMEDIÁRIA → DESTINAÇÃO, **amarrado a hash + assinatura + WORM**,
> **parametrizável por tenant** (cada município tem sua TTD aprovada). Bounded context: Protocolo (GED).

### 2.1 Agregados / entidades / VOs (Domain — `Protocolo.Domain/Arquivistica/`)

**`PlanoDeClassificacao` (AggregateRoot, `IMustHaveTenant`)** — o plano do tenant (código × assunto).
Itens `ClasseDocumental` (entidade interna, árvore): `CodigoClassificacao` (ex.: `040`, `040.1`),
`Assunto`, `CodigoPai?`, `AtividadeMeioOuFim` (CONARQ código de classificação das atividades-meio é
nacional; atividades-fim são do município/RS). Substitui a `string` solta — a VO `Classificacao` do
`Processo` passa a **referenciar** um `CodigoClassificacao` válido no plano do tenant (validado no handler).

**`TabelaTemporalidade` (AggregateRoot, `IMustHaveTenant`)** — a TTD do tenant. Itens
`RegraTemporalidade` (entidade interna), uma por `CodigoClassificacao`:

```csharp
public sealed class RegraTemporalidade
{
    public string CodigoClassificacao { get; }
    public int PrazoGuardaCorrenteAnos { get; }      // fase corrente
    public int PrazoGuardaIntermediariaAnos { get; } // fase intermediária
    public Destinacao DestinacaoFinal { get; }       // Eliminacao | GuardaPermanente
    public EventoContagem EventoContagem { get; }     // a partir de quê conta (ver 2.3)
    public string? Observacao { get; }                // norma-fonte (auditabilidade)
}
public enum Destinacao { Eliminacao = 1, GuardaPermanente = 2 }
public enum EventoContagem { DataAutuacao = 1, DataArquivamento = 2, AprovacaoContas = 3, /* ... por tenant */ }
```

**`MetadadosArquivisticos` (VO, e-ARQ v2)** — embarcado no `Processo`/`Documento`: `ProdutorOrgao`,
`TipoDocumental`, `DataProducao`, `RestricaoAcesso` (reusa `NivelDeAcesso`), `SuporteOriginal`,
`FundoSerie`. Mínimo e-ARQ para o SIGAD; campos obrigatórios são **parâmetro do tenant** (decisão de
produto pendente no M10 — qual subconjunto e-ARQ v2 é mandatório).

**`DestinacaoProcesso` (AggregateRoot, `IMustHaveTenant`)** — a **decisão calculada e registrada** de
destinação de um processo arquivado (a "ficha de destinação"). Estados:
`AguardandoPrazo → ApताToEliminar/AptoGuardaPermanente → EliminacaoAutorizada → Eliminado`
(ou `Permanente`, terminal). Carrega `ProcessoId`, `CodigoClassificacao`, datas calculadas das três
fases, `Destinacao`, e — na eliminação — `TermoEliminacaoHash` + `EditalEliminacaoRef` (Res. CONARQ
40/2014 exige edital + termo) **assinados e carimbados** (reusa Peça 1) sob WORM.

### 2.2 Serviço de domínio / porta (cálculo)

**`Tensorroot.Gov.Modules.Protocolo.Application/Abstractions/IMotorTemporalidade.cs`**:

```csharp
public interface IMotorTemporalidade
{
    /// Resolve a regra do plano/TTD do tenant para a classe e devolve as datas das 3 fases.
    PlanoDeDestinacao Calcular(string codigoClassificacao, DateOnly eventoBase, ITabelaTemporalidadeTenant ttd);
}
public sealed record PlanoDeDestinacao(
    DateOnly FimGuardaCorrente, DateOnly FimGuardaIntermediaria,
    Destinacao Destinacao, DateOnly? DataAptidaoEliminacao);
```

O cálculo é **determinístico e local** (sem I/O): soma de anos sobre `eventoBase` conforme a
`RegraTemporalidade` do tenant. A **fase corrente** termina em `eventoBase + corrente`; a
**intermediária** em `+ corrente + intermediária`; a **aptidão à eliminação** = fim da intermediária
(se `Destinacao = Eliminacao`). O cálculo de prazos em dias úteis/feriados, quando aplicável, reusa o
`ICalendarioDiasUteis` transversal promovido em **W9.1** (não duplicar).

### 2.3 Gatilho e fluxo
1. `Processo.Arquivar` (já existe) passa a emitir, além de `ProcessoArquivado`, o gancho para criar a
   `DestinacaoProcesso` (via handler do Outbox, transacional). O `eventoBase` vem do `EventoContagem`
   da regra (data de arquivamento por padrão; ou aprovação de contas etc.).
2. Um job/varredura (reusa o padrão de drenagem do Outbox; **não** novo scheduler ad-hoc) promove
   `AguardandoPrazo → Apto*` quando a data corrente ≥ `DataAptidaoEliminacao`.
3. Eliminação exige **ato humano autorizado** (RBAC) + edital + termo assinado/carimbado (Peça 1) —
   nunca automática.

### 2.4 Invariantes (domínio rico — protegem o irreversível)
- **I-T1 (não eliminar antes do prazo):** `AutorizarEliminacao` lança se `hoje < DataAptidaoEliminacao`.
- **I-T2 (guarda permanente nunca elimina):** `Destinacao = GuardaPermanente` ⇒ `AutorizarEliminacao`
  e `MarcarEliminado` **sempre** lançam; transição para `Eliminado` é inalcançável.
- **I-T3 (eliminação só de arquivado):** exige `Processo.Situacao == Arquivado`.
- **I-T4 (rastreabilidade do irreversível):** `MarcarEliminado` exige `TermoEliminacaoHash` presente,
  **assinado + carimbado** (Peça 1) e gravado sob WORM — o termo é a prova oponível ao TCE.
- **I-T5 (classe deve existir no plano do tenant):** autuação rejeita `CodigoClassificacao` ausente do
  `PlanoDeClassificacao` ativo do tenant (FK lógica multi-tenant).
- **I-T6 (parâmetro, nunca constante):** prazos/destinação vêm **sempre** da TTD do tenant; zero
  `hardcode` (CLAUDE.md §7) — cada regra carrega `Observacao` = norma-fonte para o TCE.
- **I-T7 (imutabilidade da decisão):** uma `DestinacaoProcesso` em estado terminal (`Eliminado`/`Permanente`)
  não retrocede (append-only, sob hash-chain/WORM).

### 2.5 Persistência (Infrastructure)
DbContext do Protocolo já isolado (schema `protocolo`). Novas tabelas: `PlanosClassificacao` +
`ClassesDocumentais` (OwnsMany/árvore), `TabelasTemporalidade` + `RegrasTemporalidade`,
`DestinacoesProcesso`. Global Query Filter por tenant herdado da base. Migration `AddTemporalidadeConarq`.
A eliminação **não** deleta a linha do `Processo` (WORM) — marca `DestinacaoProcesso = Eliminado` e
registra o termo; o conteúdo físico (blob) é purgado fora do banco com a referência preservada.

---

## PEÇA 3 — FIX DO RACE CONDITION DO SEQUENCIAL NUP

### 3.1 O bug atual
`NupSequencialGenerator.GerarAsync` faz `COUNT` dos processos do ano e usa `count + 1`. Sob duas
autuações **concorrentes** do mesmo tenant, ambas leem o mesmo `count` ⇒ geram o **mesmo NUP**. Hoje só
o **índice único `(TenantId, Nup)`** (visto em `ProcessoConfiguration`) barra a colisão — mas o
**segundo** `SaveChanges` **explode** (`DbUpdateException`), derrubando a autuação em vez de alocar o
próximo número. Falha intermitente sob carga — exatamente o que CLAUDE.md proíbe ("nunca trava em produção").

### 3.2 A correção — mesmo padrão atômico do hash-chain
Espelhar o `UltimoSeloReader` (UPDLOCK/HOLDLOCK por tenant) que já resolve a corrida idêntica na trilha:

- **Nova tabela `SequenciasNup` (`protocolo` schema):** `(TenantId, Ano, UltimoSequencial)`,
  PK `(TenantId, Ano)`. Uma linha-contador por tenant×exercício — a "cabeça" da sequência.
- **`NupSequencialGenerator` reescrito:** em **SqlServer (produção)**, lê a linha do contador sob
  `UPDLOCK, HOLDLOCK` na **conexão/transação atual** do `ProtocoloDbContext` (idêntico ao
  `LerComLockAsync`), incrementa `UltimoSequencial`, persiste no **mesmo `SaveChanges`** da autuação
  (transacional — o lock segura até o commit, serializando concorrentes do mesmo tenant×ano). Em
  **SQLite/testes** (escrita serializada) usa LINQ `Max(UltimoSequencial)+1`; o índice único
  `(TenantId, Nup)` permanece como **backstop** em qualquer provider.
- O dígito verificador (módulo 11, Decreto 8.539/2015) e o formato `nnnnnn/aaaa-dd` ficam **iguais** —
  muda só **como** o sequencial é alocado (contador atômico, não `COUNT`).
- **Reset anual:** sem linha para `(TenantId, AnoCorrente)` ⇒ cria com `UltimoSequencial = 1` (o `UPDLOCK`
  + PK composta serializa o "primeiro do ano"; o `INSERT` concorrente perde para o índice único e relê).

### 3.3 Por que não usar `SEQUENCE` do SqlServer
`CREATE SEQUENCE` é global por banco; aqui o requisito é **sequencial por tenant×ano com reset anual** e
o app roda **banco-dedicado por tenant** (ADR-0005) — uma `SEQUENCE` por tenant×ano seria DDL dinâmico
frágil. O contador em tabela + UPDLOCK reusa **infra já provada** (hash-chain) e mantém a auditoria.

---

## ENTIDADES / PORTAS-CHAVE (resumo)

| Tipo | Nome | Camada | Origem |
|---|---|---|---|
| Porta | `ICarimbadorDeTempo` | Application | **nova** (substitui `ICarimboDeTempoService`) |
| Adapter | `CarimbadorDeTempoAct` (RFC 3161/BouncyCastle) | Infrastructure | **novo** |
| Adapter | `CarimbadorDeTempoLocalService` (fallback/dev) | Infrastructure | renomeia `CarimboDeTempoLocalService` |
| VO | `CarimboDeTempo` + `OrigemCarimbo` + `DeToken` | Domain | **estende** existente (TST/serial/policy/hashCarimbado) |
| Exceção | `ExcecaoCarimboDeTempo` (PKIFailureInfo) | Infrastructure/Domain | **nova** |
| Options | `OpcoesAct` (por tenant, Key Vault) | Infrastructure | **nova** |
| Agregado | `PlanoDeClassificacao` + `ClasseDocumental` | Domain | **novo** |
| Agregado | `TabelaTemporalidade` + `RegraTemporalidade` | Domain | **novo** |
| Agregado | `DestinacaoProcesso` | Domain | **novo** |
| VO | `MetadadosArquivisticos` (e-ARQ v2) | Domain | **novo** |
| Porta/Serviço | `IMotorTemporalidade` + `PlanoDeDestinacao` | Application | **novo** (cálculo local determinístico) |
| Tabela | `SequenciasNup` (TenantId, Ano, UltimoSequencial) | Infrastructure | **nova** |
| Reescrita | `NupSequencialGenerator` (UPDLOCK/HOLDLOCK) | Infrastructure | espelha `UltimoSeloReader` |
| Reuso | WORM, hash-chain, Outbox, Key Vault/A1, `ICalendarioDiasUteis` (W9.1), `Documento`/`Hash`/`Assinatura` | fundação | **sem recriar** |

---

## SEQUÊNCIA DE IMPLEMENTAÇÃO (backend edit-only + verify; frontend npx)

> Todo o backend é **edit-only + verify** (sem `dotnet run`/build manual fora do pipeline; não derrubar a :5080).
> Frontend (telas de plano/TTD/destinação) via `npx` quando chegar a vez. Specs BDD `.md` **antes** do código (CLAUDE.md §1).

1. **Fix do NUP (Peça 3) — primeiro, P, zero dependência.** Spec BDD da concorrência → tabela
   `SequenciasNup` + migration `AddSequenciaNup` → reescrita do `NupSequencialGenerator` (UPDLOCK/HOLDLOCK
   espelhando `UltimoSeloReader`) → testes de concorrência (xUnit, duas autuações paralelas mesmo
   tenant×ano ⇒ NUPs distintos; SQLite backstop) → **verify** (build + testes verdes).

2. **Carimbo RFC 3161 (Peça 1) — porta + ACL + persistência.** Spec BDD (granted/PKIFailureInfo/imprint/
   nonce) → estende VO `CarimboDeTempo` (+`DeToken`/`OrigemCarimbo`) e `ExcecaoCarimboDeTempo` → nova porta
   `ICarimbadorDeTempo`; renomeia local → `CarimbadorDeTempoLocalService` → `CarimbadorDeTempoAct`
   (BouncyCastle + `HttpClient` resiliente registrado no `ProtocoloModule`, padrão `AdnNfseGateway`) →
   estende `DocumentoConfiguration` (colunas TST) + migration `AddTstCarimboDeTempo` (additive) →
   invariante "Alta exige Act" em `Documento.Assinar` + vínculo hash↔token → troca a dependência no
   `AssinarDocumentoHandler` → testes WireMock/stub → **verify**.

3. **Motor de temporalidade (Peça 2) — o maior.** Spec BDD das invariantes I-T1..I-T7 → agregados
   `PlanoDeClassificacao`/`TabelaTemporalidade` + `DestinacaoProcesso` + VO `MetadadosArquivisticos` →
   `IMotorTemporalidade` (cálculo local) → configs EF + migration `AddTemporalidadeConarq` → handlers
   (cadastrar plano/TTD por tenant; calcular destinação no arquivamento via Outbox; autorizar/registrar
   eliminação com termo assinado+carimbado) → reuso do `ICalendarioDiasUteis` (W9.1) → testes de
   invariante (não eliminar antes do prazo; permanente nunca elimina; classe inexistente recusada) → **verify**.

4. **Frontend (npx) — por último.** Telas gov.br/WCAG 2.1 AA: CRUD do Plano de Classificação e da TTD
   por tenant; ficha de `DestinacaoProcesso` (fases/datas/destinação); ação auditada de autorizar
   eliminação; exibição do TST (serial/policy/autoridade) no documento assinado. Provas `.a11y.test.tsx`.

5. **Integração com W9.8 (rede de proteção):** o harness WireMock do W9.8 hospeda os endpoints da ACT
   (timestamp-reply) reusados aqui; cobertura cross-tenant do Protocolo (plano/TTD/sequência NUP por tenant).

**Defere M10 (não bloqueia M9):** ACT credenciada real contratada + custo por carimbo (decisão comercial);
endpoint/credencial de produção da ACT no Key Vault; subconjunto e-ARQ v2 obrigatório por tenant; TTD de
atividades-fim RS/município (carga de dados, não código).

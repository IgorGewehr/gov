# DESIGN — Coletor de Ponto (hardware REP → nosso domínio AFD/jornada)

> **Tensorroot.Gov / módulo RecursosHumanos.** O lado *software* do ponto já existe (marcação
> imutável `MarcacaoPonto` com NSR, `GeradorAfd`/`GeradorAej` posicionais Port. 671, `Crc16Ccitt`,
> apuração de jornada). Este documento projeta o lado **hardware**: como o sistema **coleta o AFD
> dos equipamentos físicos REP** e **ingere as marcações** no nosso domínio, de forma idempotente,
> multi-tenant e auditável.
>
> Base de pesquisa: `pesquisa-rep-afd.md` e `pesquisa-fabricantes-sdk.md` (mesma pasta).
> `[a confirmar]` = depende de SDK/doc proprietário do fabricante ou do texto integral do Anexo da 671.

---

## 1. Princípio arquitetural (decorrente da pesquisa)

Existe **um padrão de DADOS (AFD posicional ASCII da Port. 671)**, mas **NÃO existe um padrão de
PROTOCOLO** de coleta em tempo real — cada fabricante tem o seu (REST, SDK/DLL TCP, USB/pendrive).
Logo:

> **O AFD é a *língua franca* de ingestão. Todo driver de fabricante, qualquer que seja o transporte
> (arquivo, TCP/SDK, REST/cloud), CONVERGE para entregar o mesmo AFD posicional 671, que um único
> pipeline de ingestão consome.** Isso isola o proprietário (`[a confirmar]`) na borda e mantém o
> núcleo estável.

Espelha a topologia já aprovada do `NfseSync` (CLAUDE.md §2/§8): **Worker** dedicado, **atrás de
ACL**, integração **idempotente, resiliente (Polly) e multi-tenant por iteração explícita**.

```
[REP físico] --(arquivo USB | TCP/SDK | REST/cloud)--> [IColetorRep do fabricante]
        --> AFD posicional 671 (bytes) --> [PipelineIngestaoAfd]
        --> parse + validação NSR/CRC/assinatura --> [IngestarMarcacoesAfdCommand]
        --> MarcacaoPonto.Registrar (append-only, idempotente) --> domínio já existente
```

---

## 2. Onde mora cada peça (respeitando isolamento de módulo, CLAUDE.md §2)

| Peça | Camada/projeto | Papel |
|---|---|---|
| `IColetorRep` + DTO `LoteAfdColetado` | `RecursosHumanos.Application/Ponto/Coleta` (abstração) | Contrato por-fabricante: "me devolva o AFD desde o NSR X". |
| Drivers `ColetorControlId`, `ColetorTopdata`, `ColetorHenry`, `ColetorDimep`, `ColetorMadis`, `ColetorArquivoAfd` | `RecursosHumanos.Infrastructure/Ponto/Coleta/<Fabricante>` | Implementações; falam REST/SDK/USB e normalizam para AFD. **ACL por fabricante.** |
| `IParserAfd` + `RegistroAfdParseado` | `RecursosHumanos.Domain/Ponto` (puro, sem I/O) | Inverso do `GeradorAfd`: lê o posicional 671, valida NSR/CRC. |
| `IngestarMarcacoesAfdCommand` + handler | `RecursosHumanos.Application/Ponto/Coleta` | Ingestão idempotente; usa `IMarcacaoPontoRepository` já existente. |
| `RepConfigurado` (entidade) + `IRepRepository` | `RecursosHumanos.Domain/Ponto` + Infra | Cadastro multi-tenant do parque de REPs (marca, transporte, endereço/credencial-ref, último NSR coletado). |
| `Tensorroot.Gov.Workers.PontoColetor` | `src/Workers/` (novo, irmão do `NfseSync`) | Orquestra a coleta agendada por tenant/REP, online-first com reenvio. |

**Regra de dependência:** os drivers ficam em **Infrastructure** (têm I/O e SDK proprietário). O
**Domain** só conhece `IParserAfd` (puro). A **Application** orquestra via `IColetorRep` e o command
de ingestão. Credenciais dos REPs → **Azure Key Vault**, nunca no banco/repo (CLAUDE.md §6).

---

## 3. Abstração `IColetorRep` (driver por fabricante)

```csharp
// Application/Ponto/Coleta — contrato estável; implementações na Infrastructure.
public interface IColetorRep
{
    MarcaRep Marca { get; }                 // ControlId, Topdata, Henry, Dimep, Madis, ArquivoAfd...
    ModoColeta Modos { get; }               // [Flags] Arquivo | TcpSdk | RestCloud

    // Coleta INCREMENTAL: pede o AFD a partir do último NSR já ingerido (quando o REP suporta
    // filtro por NSR — Henry/Control iD suportam; senão devolve o AFD completo e a ingestão deduplica).
    Task<LoteAfdColetado> ColetarAsync(RepConexao conexao, long ultimoNsrConhecido, CancellationToken ct);
}

public sealed record LoteAfdColetado(
    byte[] ConteudoAfd,            // AFD posicional 671 (ISO-8859-1) — a língua franca
    byte[]? AssinaturaCades,       // .p7s detached, quando o REP/fabricante fornece (REP-P/A)
    MarcaRep Marca,
    string IdentificacaoEquipamento); // nº de série / fabricante, p/ trilha
```

- **Online-first (Port. 671):** quando o fabricante oferece **cloud/push** (Control iD **iDCloud** via
  `get_afd.fcgi`, Dimep **REST API**), o driver usa esse caminho — sem agente local. Quando não há
  cloud (Topdata/Henry/Madis on-premise), a coleta é **TCP/SDK na rede do cliente** (exige host
  on-premise) **ou** **importação de arquivo** (USB/pendrive) como fallback sempre-disponível.
- **`ColetorArquivoAfd`** é o driver universal: recebe um AFD já exportado (upload pela UI ou
  pendrive coletado na porta fiscal) e entra no mesmo pipeline. Funciona para **qualquer** REP-C,
  inclusive marcas sem SDK (Tellijack `[a confirmar]`).
- **Resiliência:** todo driver de rede usa **Polly** (timeout + retry + circuit breaker) e mapeia
  erros do fabricante para exceções de domínio (Anti-Corruption Layer, CLAUDE.md §8).

---

## 4. `IParserAfd` — o inverso do nosso `GeradorAfd`

O `GeradorAfd.cs` já monta o AFD posicional (tipo na 1ª posição, NSR, data, hora, CPF, CRC). O parser
faz o caminho inverso, **reusando `CampoPosicional` e `Crc16Ccitt` já existentes**:

```csharp
// Domain/Ponto — serviço PURO (sem I/O), simétrico a GeradorAfd.
public interface IParserAfd
{
    ResultadoParseAfd Parse(ReadOnlySpan<byte> afd); // Span<byte> p/ fatiar sem alocar (CLAUDE.md §7)
}

public sealed record RegistroAfdParseado(long Nsr, Cpf Cpf, DateTimeOffset DataHora, TipoRep Origem);
public sealed record ResultadoParseAfd(
    CabecalhoAfd Cabecalho,
    IReadOnlyList<RegistroAfdParseado> Marcacoes,
    bool CrcOk, bool NsrContinuo, long? PrimeiroNsr, long? UltimoNsr);
```

**Validações de integridade no parse (críticas, alimentam o PTRP):**
1. **Continuidade de NSR** — sequência crescente e **sem lacunas** dentro do REP. Lacuna = sinal de
   supressão → marca o lote como suspeito (auditável), não descarta silenciosamente.
2. **CRC-16** por registro/arquivo (já temos `Crc16Ccitt`). `[a confirmar]` abrangência exata na 671.
3. **Assinatura CAdES `.p7s`** do AFD do REP físico (quando presente) antes de tratar.

> **CORREÇÃO de domínio identificada (apontar no PR):** o leiaute 671 usa **tipo `3` = marcação de
> REP-C/REP-A** e **tipo `7` = marcação de REP-P** (CPF nas posições **035–046**, 12 chars). O enum
> atual `TipoRegistroAfd` (Domain/Ponto/Enums.cs) mapeia `Marcacao = 3` genérico e não tem o `7`. O
> parser **e** o `GeradorAfd` precisam distinguir 3 vs 7 conforme `TipoRep`. `[a confirmar]` posições
> integrais no Anexo oficial — os `// TODO(validar-oficial)` já existentes cobrem isso.

---

## 5. Ingestão idempotente no domínio (o coração)

O AFD do equipamento traz **o NSR do EQUIPAMENTO**. Nosso domínio também tem um NSR (gerado por
`RegistrarMarcacaoHandler` para marcações nascidas no nosso REP-P). **São espaços de numeração
distintos** — não se pode misturar. Decisão:

- A marcação de equipamento físico é **origem `RepC`/`RepA`** e carrega a **chave natural
  `(TenantId, RepId, NsrEquipamento)`** como **idempotency key**. Reingestão do mesmo AFD (re-upload,
  re-coleta após falha) **não duplica**: o handler ignora pares já vistos.
- Reusa a entidade `MarcacaoPonto` (append-only, imutável) e o `IMarcacaoPontoRepository` existentes.
  Adiciona-se ao agregado os campos `RepId` + `NsrEquipamento` (o `Nsr` interno do nosso REP-P segue
  para marcações próprias). Migration por módulo (CLAUDE.md §9).

```csharp
public sealed record IngestarMarcacoesAfdCommand(Guid RepId, byte[] ConteudoAfd, byte[]? AssinaturaCades)
    : ICommand<ResultadoIngestao>;

// Handler (pseudo):
//  1. parser.Parse(afd) -> valida CRC/NSR/assinatura (fail-closed se inválido — CLAUDE.md §16)
//  2. já-vistos = repo.NsrsEquipamentoExistentes(RepId, [nsrs do lote])  // dedup em lote
//  3. para cada registro novo: resolve ServidorId por CPF (consulta no tenant);
//     MarcacaoPonto.RegistrarDeEquipamento(tenant, servidorId, cpf, repId, nsrEquip, dataHora, sentido, origem)
//  4. repo.Adicionar(...) ; unitOfWork.SaveChanges()  // transacional + Outbox + auditoria
//  5. rep.AvancarUltimoNsrColetado(maxNsr)            // próxima coleta é incremental
//  6. retorna ResultadoIngestao(novas, duplicadas, suspeitas)
```

- **Sentido (entrada/saída):** o AFD 671 **não** carrega sentido — só a batida. Mantém-se o pareamento
  já feito na apuração (`TratamentoJornada`), que ordena por data/hora e alterna E/S. `[a confirmar]`
  se algum fabricante expõe sentido fora do AFD (não usar — fora do padrão).
- **CPF → ServidorId:** resolvido via consulta no tenant (reusa padrão `IServidorPontoConsulta`).
  CPF sem servidor correspondente → registro **pendente de vínculo** (auditável), não descartado.
- **Multi-tenancy:** `RepId` pertence a um tenant; o `TenantInterceptor`/Global Query Filter garante
  isolamento. Gravação cross-tenant lança exceção (CLAUDE.md §5).

---

## 6. O Worker `PontoColetor` (irmão do `NfseSync`)

```
src/Workers/Tensorroot.Gov.Workers.PontoColetor/
  Program.cs              // Composition Root do worker
  PontoColetorWorker.cs   // BackgroundService: loop por intervalo
  WorkerComponents.cs     // WorkerTenantContext, SistemaCurrentUser ("sistema:ponto-coletor"), config
```

- **Padrão idêntico ao `NfseSync`:** `BackgroundService`; itera tenants/REPs configurados; para cada
  REP cria `scope`, define `WorkerTenantContext.Definir(tenantId)`, resolve o `IColetorRep` da marca,
  coleta incremental a partir de `rep.UltimoNsrColetado`, e despacha `IngestarMarcacoesAfdCommand`.
- **Boundary de resiliência:** falha de um REP/tenant **não** interrompe os demais (mesmo
  `SuppressMessage CA1031` do `NfseSync`), com log estruturado por `TenantId`/`CorrelationId`.
- **Identidade de sistema** na trilha de auditoria: `sistema:ponto-coletor`.
- **Online-first + reenvio offline (Port. 671):** REPs cloud (Control iD/Dimep) podem empurrar via
  endpoint REST nosso (ApiHost, atrás de ACL) em tempo quase-real; o Worker faz a **varredura de
  reconciliação** periódica (pega o que o push perdeu) — o dedup por NSR torna os dois caminhos
  convergentes e seguros. `[a confirmar]` formato exato do push de cada fabricante.

---

## 7. Como casa com o que já existe

| Já existe | Como o coletor se conecta |
|---|---|
| `MarcacaoPonto` (append-only, imutável, NSR) | Reusada; ganha `RepId`+`NsrEquipamento` p/ idempotência de equipamento. |
| `IMarcacaoPontoRepository` | Reusado para `Adicionar` + nova consulta `NsrsEquipamentoExistentes` (dedup em lote). |
| `GeradorAfd` / `CampoPosicional` / `Crc16Ccitt` | Reusados pelo `IParserAfd` (caminho inverso, mesmas larguras/CRC). |
| `GerarAfdHandler` (gera+assina AFD do nosso REP-P) | Inalterado. O coletor é a **entrada**; o gerador é a **saída**. |
| `TratamentoJornada` / `ApuracaoPonto` / `GerarAej` | Inalterados — consomem as marcações ingeridas como qualquer outra. AEJ continua assinado por nós. |
| `ParametrosPonto` (por tenant) | Estendido com `RecursosHumanos:Ponto:Coletor` (intervalo, modo, marcas habilitadas). |
| Cofre A1 / `IAssinaturaEmEscopoDedicado` | Reusado para **validar** o `.p7s` do AFD recebido e assinar nosso AEJ. |

Papel regulatório (pesquisa §5): atuamos como **PTRP** consumindo AFD de REP-C de terceiros, e/ou
como **REP-P** (coletores próprios). O coletor habilita o cenário **PTRP** sem mudar o núcleo.

---

## 8. `[a confirmar]` (SDKs proprietários e Anexo oficial)

- **SDKs/protocolos por-fabricante** (endpoints, portas, autenticação, filtro por NSR):
  **Control iD** (REST `get_afd.fcgi`/iDCloud — a mais aberta), **Dimep** (REST/Protocolo VIII),
  **Topdata** (SDK Inner REP, DLL TCP, Windows/.NET), **Henry** (TCP/Serial/USB, SDK), **Madis**
  (pendrive AFD + SDK), **Tellijack** (sem doc pública — só `ColetorArquivoAfd`). Obter doc/contrato
  de cada um antes de implementar o driver.
- **Posições/larguras integrais** do leiaute AFD 671 (tipos 3 vs 7, CPF 035–046, CRC) — Anexo oficial
  (DOU). Os `// TODO(validar-oficial)` já presentes no Domain cobrem o ajuste.
- **Push/webhook** de cada cloud (formato do payload Control iD/Dimep) para o endpoint de ingestão.
- **Certificado ICP-Brasil** (A1/A3) para validar `.p7s` do AFD e assinar o AEJ — reusar Key Vault/M2.
- **Templates biométricos** são proprietários e **não-portáveis** — fora do escopo da coleta de AFD;
  re-enrollment ao trocar de marca é decisão operacional, não de software.

---

## RESUMO (12 linhas)

1. **Padrão de dados (AFD 671) existe; padrão de protocolo não** → drivers por-fabricante na borda.
2. **AFD é a língua franca**: todo transporte (USB/arquivo, TCP/SDK, REST/cloud) converge para AFD.
3. Abstração **`IColetorRep`** (marca + modos) na Application; **drivers na Infrastructure** (ACL).
4. **`ColetorArquivoAfd`** é o driver universal (upload/pendrive) — funciona em qualquer REP-C.
5. **`IParserAfd`** (Domain, puro) é o inverso do `GeradorAfd` — reusa `CampoPosicional`/`Crc16Ccitt`.
6. Parser **valida NSR contínuo, CRC-16 e assinatura CAdES** antes de ingerir (fail-closed).
7. **Ingestão idempotente** por chave natural `(TenantId, RepId, NsrEquipamento)` — re-coleta não duplica.
8. Reusa **`MarcacaoPonto`** (append-only) + `IMarcacaoPontoRepository`; coleta incremental por NSR.
9. **Worker `PontoColetor`** espelha o `NfseSync`: ACL, Polly, multi-tenant por iteração, online-first.
10. Apuração/AEJ/jornada existentes **inalterados** — consomem as marcações ingeridas normalmente.
11. **Achado:** AFD usa tipo `3` (REP-C/A) e `7` (REP-P); o enum atual não distingue — corrigir.
12. **`[a confirmar]`:** SDKs proprietários (Control iD/Dimep/Topdata/Henry/Madis), posições do Anexo, push.

---

## CAMINHO (próximos passos de implementação)

1. **Domain:** `IParserAfd` + `ResultadoParseAfd` (espelho de `GeradorAfd`); corrigir `TipoRegistroAfd`
   (3 vs 7) e `GeradorAfd` para distinguir REP-C/A vs REP-P. Specs BDD antes (CLAUDE.md §1/§12).
2. **Domain/Infra:** entidade `RepConfigurado` (+`RepId`/`NsrEquipamento` em `MarcacaoPonto`),
   `IRepRepository`, migration `PontoColeta`. Credenciais → Key Vault.
3. **Application:** `IColetorRep`/`LoteAfdColetado`; `IngestarMarcacoesAfdCommand`+handler idempotente;
   `NsrsEquipamentoExistentes` no repositório; estender `ParametrosPonto` (`:Coletor`).
4. **Application:** `ImportarAfdCommand` (upload pela UI) reusando o mesmo pipeline de ingestão.
5. **Infrastructure:** driver `ColetorArquivoAfd` (universal) primeiro; depois `ColetorControlId`
   (REST/iDCloud — melhor documentado) como segundo, com Polly/ACL. Demais marcas atrás de `[a confirmar]`.
6. **Worker:** `Tensorroot.Gov.Workers.PontoColetor` clonando a topologia do `NfseSync`.
7. **ApiHost:** endpoint de **push** (atrás de ACL) para REPs cloud, despachando o mesmo command.
8. **Tests:** ArchTests (Domain sem I/O; driver só em Infra); ingestão idempotente; NSR-gap; CRC; tenant.
9. **Confirmar** os `[a confirmar]` (SDKs + Anexo 671) com fabricantes/DOU antes do uso fiscal.

---

## FONTES

- MTE — Leiaute do AFD (PDF oficial): https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/leiaute-do-arquivo-fonte-de-dados-afd.pdf
- TOTVS — Leitura do novo layout AFD (tipo 3 REP-C/A, tipo 7 REP-P; CPF 035–046): https://tdn.totvs.com/display/public/ConSeg/Portaria+671+-+Leitura+do+Novo+Layout+AFD
- Alterdata — PIS→CPF no AFD 671 (CPF 12 chars, posição 035–046): https://ajuda.alterdata.com.br/ponbase/extincao-do-pis-e-nova-formatacao-de-afd
- Control iD — iDCloud / modo Push (push.idsecure.com.br): https://www.controlid.com.br/docs/access-api-pt/modo-push/idcloud/
- Control iD — API (get_afd, load_objects): https://www.controlid.com.br/docs/access-api-pt/
- (demais fontes por-fabricante: ver `pesquisa-fabricantes-sdk.md` e `pesquisa-rep-afd.md`)

*Design: 2026-06-22. Validar `[a confirmar]` (SDKs proprietários + Anexo integral 671) antes de implementar driver/uso fiscal.*

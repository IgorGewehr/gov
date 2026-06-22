# Auditoria — Integrações Governamentais (Tensorroot.Gov)

> **Método:** leitura empírica do código-fonte (`src/`, excluindo `obj/`/`bin/`). Cada veredito abaixo
> foi verificado lendo a implementação concreta, o registro no DI (`*Module.cs`) e procurando por
> os marcadores de uma integração **real**: `HttpClient`/`AddHttpClient`, Polly
> (`AddStandardResilienceHandler`/`AddResilienceHandler`), SOAP (`System.ServiceModel`/`ChannelFactory`),
> assinatura digital (`SignedXml`/`X509Certificate2`) e Azure Key Vault.

## Resumo executivo

De **todas** as integrações governamentais previstas na Constituição (§8), **apenas UMA tem cliente real
de produção**: **NFS-e / ADN** (`AdnNfseGateway`, HTTP + Polly). Todas as demais são **stubs `Simulado*`**
ou **fluxos de domínio sem transmissão externa** (a porta/ACL existe, mas a implementação concreta apenas
retorna dados determinísticos ou `Task.CompletedTask`).

Achados transversais críticos (medidos no código):

- **Assinatura digital A1 (XML/ICP-Brasil): INEXISTENTE.** Zero ocorrências de `SignedXml`,
  `X509Certificate2`, `RSACryptoServiceProvider` em todo o `src/`. A Constituição (§6, §8) exige A1 por
  tenant para **eSocial, remessas TCE-RS e Protocolo** — nada disso assina nada hoje.
- **SOAP: INEXISTENTE.** Zero ocorrências de `System.ServiceModel`/`ChannelFactory`/`BasicHttpBinding`.
  SICONFI e eventos eSocial são, na prática, SOAP/REST — não há cliente.
- **Azure Key Vault: NÃO INTEGRADO.** O único uso de "segredo" é `Jwt:Secret` lido de
  `IConfiguration` em `ApiHost/Program.cs` (comentário diz "Key Vault em produção", mas não há
  `AddAzureKeyVault` nem `SecretClient` no código).
- **Polly real: 1 ocorrência.** `AddStandardResilienceHandler` só aparece no registro do `AdnNfseGateway`
  (`TributosModule.cs`). Todos os outros gateways são registrados como `AddSingleton/AddScoped<Simulado*>`,
  sem resiliência (porque não há I/O).
- **`AddHttpClient` real: 1 ocorrência de negócio** (NFS-e/ADN). As outras duas em `Program.cs` são
  `AddHttpClientInstrumentation` (OpenTelemetry), não clientes.

---

## Tabela: REAL × STUB/PENDENTE

| Integração | Porta / ACL (interface) | Implementação concreta | Veredito | Cliente real (HTTP/SOAP) | Polly | Assinatura A1 |
|---|---|---|---|---|---|---|
| **NFS-e / ADN** | `INfseNacionalGateway` | `AdnNfseGateway` (+ `SimuladoNfseGateway`) | **REAL** (passiva, leitura) | ✅ `HttpClient` + JSON | ✅ `AddStandardResilienceHandler` | n/a (não assinamos NFS-e — ADR-0003) |
| **TCE-RS — SIAPC/PAD (transmissão)** | `ISiapcPadGateway` | `SimuladoSiapcPadGateway` → `Task.CompletedTask` | **STUB** | ❌ | ❌ | ❌ (exigido) |
| **TCE-RS — e-Validador (RDI)** | `IEValidadorTce` | `SimuladoEValidadorTce` (validação fake em memória) | **STUB** | ❌ | ❌ | n/a |
| **TCE-RS — catálogo de leiaute** | `ILeiauteCatalogo` | `SimuladoLeiauteCatalogo` | **STUB** | ❌ | ❌ | n/a |
| **TCE-RS — calendário fiscal** | `ICalendarioFiscal` | `SimuladoCalendarioFiscal` | **STUB** | ❌ | ❌ | n/a |
| **SICONFI / MSC (STN)** | `ISiconfiGateway` | `SimuladoSiconfiGateway` (protocolo determinístico) | **STUB** | ❌ | ❌ | ❌ |
| **eSocial (envio de eventos)** | — (não existe porta) | — | **AUSENTE** | ❌ | ❌ | ❌ |
| **eSocial — S-1010 (rubricas)** | `IRubricaS1010Consulta` | `RubricaS1010Consulta` (lê tabela em `IConfiguration` do tenant) | **PARCIAL/LOCAL** (não fala com o eSocial; valida contra catálogo local) | ❌ | ❌ | n/a |
| **PNCP (Administração)** | — (não há gateway) | Handlers só gravam nº do PNCP + emitem evento | **PORTA-SEM-IMPLEMENTAÇÃO** | ❌ | ❌ | ❌ |
| **Receita Federal — CNPJ** | `IReceitaCnpjGateway` | `SimuladoReceitaCnpjGateway` (sempre "ativo") | **STUB** | ❌ | ❌ | n/a |
| **RNDS (e-SUS/FHIR)** | `IRndsGateway` | `SimuladoRndsGateway` (`RNDS-SIM-…`) | **STUB** | ❌ | ❌ | ❌ (mTLS/ICP) |
| **e-SUS APS / SISAB** | `ISisabGateway` | `SimuladoSisabGateway` → `Task.CompletedTask` | **STUB** | ❌ | ❌ | n/a |
| **SISREG (regulação)** | `ISisregGateway` | `SimuladoSisregGateway` (`SISREG-SIM-…`) | **STUB** | ❌ | ❌ | n/a |
| **CADSUS (PIX/PDQ)** | `ICadsusGateway` | `SimuladoCadsusGateway` (valida só formato do CNS) | **STUB** | ❌ | ❌ | n/a |
| **CNES (estabelecimento/profissional)** | `IEstabelecimentoRepository` | `SimuladoEstabelecimentoRepository` (`!= Guid.Empty`) | **STUB** | ❌ | ❌ | n/a |
| **Assinatura ICP-Brasil (Saúde/PEP)** | `IAssinaturaIcpBrasilService` | `SimuladoAssinaturaIcpBrasilService` | **STUB** | ❌ | ❌ | ❌ |
| **CadÚnico / MDS (Assist. Social)** | `ICadUnicoGateway` / `ICadUnicoReadModel` | `SimuladoCadUnicoGateway` (resumo determinístico) | **STUB** | ❌ | ❌ | n/a |
| **Educacenso / INEP (Educação)** | — (não há porta) | Só armazena `CodigoInep` + campos de censo localmente | **AUSENTE** (sem transmissão) | ❌ | ❌ | n/a |
| **Carimbo de tempo / ACT (Protocolo)** | `ICarimboDeTempoService` | `CarimboDeTempoLocalService` (relógio do sistema) | **STUB/LOCAL** | ❌ | ❌ | ❌ (ACT ICP) |
| **CNAB 240 (remessa bancária FEBRABAN)** | `IGeradorRemessaCnab` | `Cnab240Gerador` | **REAL (gerador)** — não é transmissão | n/a (gera arquivo) | n/a | n/a |

---

## O que existe (e a profundidade real)

### 1. NFS-e / ADN — a ÚNICA integração de produção (REAL)
- `src/Modules/Tributos/.../Nfse/AdnNfseGateway.cs`: `HttpClient` real, consulta
  `nfse/emitidas?cnpj=…&desde=…` via `GetFromJsonAsync<NfseDocumento[]>`. ACL limpa (traduz para `NfseDocumento`).
- Registro em `TributosModule.cs`: `AddHttpClient<INfseNacionalGateway, AdnNfseGateway>(…).AddStandardResilienceHandler()`
  — **único ponto com Polly real** do sistema. Selecionável por config `Nfse:Provider` (`Adn` vs `Simulado`).
- `NfseSincronizador` faz **deduplicação por chave de acesso** (`ExistePorChaveAsync`) e persiste como read model
  fiscal, com `IUnitOfWork`/tenant — sólido.
- `Workers/NfseSync`: `BackgroundService` que itera tenants (`Nfse:Tenants`), define tenant por escopo
  (`WorkerTenantContext`), identidade de sistema na auditoria (`SistemaCurrentUser`), captura falha por
  tenant sem derrubar os demais. **Maduro.**
- **Lacuna:** a URL default é `https://adn.invalido.local/`; autenticação/credencial do ADN (certificado/token
  da Receita) **não** é configurada — o `HttpClient` sobe sem auth. O endpoint real do ADN não é JSON simples.

### 2. CNAB 240 — gerador real (mas é geração de arquivo, não "integração" online)
- `src/Integracoes/.../Cnab/Cnab240Gerador.cs`: gera registros de 240 posições (Header Arquivo/Lote,
  Segmento A, Trailers) com largura fixa, centavos, dígitos — implementação concreta e determinística.
- **Ressalva:** é a única classe do projeto `Integracoes`. Não há transmissão ao banco (só o arquivo);
  layouts específicos por banco exigem ajuste (declarado no próprio XML doc).

### 3. Domínio TCE-RS / SICONFI — rico no agregado, vazio na borda
- O **domínio** é substancial: `RemessaTce` (ciclo Gerada→Validada→Enviada), `HashIntegridade`,
  `Leiaute`/`RegistroLeiaute`/`ArquivoRemessa`, `DeclaracaoFiscal`/`MatrizSaldos`/`LinhaContabil`,
  `ResultadoValidacao`. `GerarRemessaTceHandler` monta pacote e calcula hash com `Span`/`Memory`.
- **Mas** `EnviarRemessaTceHandler` chama `ISiapcPadGateway.TransmitirAsync` → `Task.CompletedTask`, e
  `TransmitirDeclaracaoFiscal` chama `ISiconfiGateway` → protocolo fake. **Não há assinatura A1, nem SOAP/HTTP,
  nem Polly.** O pacote é montado como `.txt` simples (`{tipo}|{conteudo}`), não no leiaute oficial do TCE-RS.

### 4. eSocial — só validação local de rubrica
- `RubricaS1010Consulta` (RH) verifica vigência de rubrica contra uma lista em `IConfiguration`
  (`RecursosHumanos:RubricasS1010`); **se vazia, aceita qualquer rubrica não vazia**. Não fala com o eSocial.
- **Não existe** geração/transmissão de eventos eSocial (S-1000/S-1010/S-1200/S-2200 etc.).

---

## Lacunas (prioridade alta → o que o dono mais teme: Contabilidade e Prestação de Contas ao TCE)

1. **TCE-RS (SIAPC/PAD) — transmissão real ausente.** É o coração da prestação de contas. Hoje o sistema
   "envia" para o vazio (`Task.CompletedTask`). Falta: cliente real, leiaute oficial (não `.txt` ad-hoc),
   execução do **e-Validador** real, e **assinatura A1** do pacote.
2. **SICONFI / MSC — transmissão real ausente.** Protocolo é forjado localmente. Falta cliente SOAP/REST + A1.
3. **Assinatura digital A1 e Azure Key Vault — ausentes em todo o sistema.** Pré-requisito de TCE, eSocial e
   Protocolo. Nenhum `SignedXml`/`X509Certificate2`/`SecretClient` no `src/`.
4. **eSocial — ausente** (sem geração/envio de eventos; só validação local de S-1010).
5. **PNCP — porta sem cliente.** Publicação de edital/contrato (Lei 14.133, condição de eficácia) apenas grava
   o número informado pelo caller e emite evento; comentários citam "handler do Outbox (cliente PNCP resiliente)"
   que **não existe**.
6. **Saúde (RNDS/SISREG/SISAB/CADSUS/CNES) e Assist. Social (CadÚnico) e Educação (Educacenso/INEP)** — todas
   stubs/ausentes. Para Saúde, falta inclusive mTLS/ICP-Brasil do RNDS.
7. **NFS-e/ADN (a real)** ainda precisa de **autenticação do ADN** e validação do contrato de payload real.

> **Conclusão:** o **domínio contábil/fiscal está maduro**, mas a **borda de integração com o governo está
> 1 em ~19 implementada** (só NFS-e/ADN). Para "prestação de contas ao TCE" funcionar de fato, faltam, em ordem:
> Key Vault + assinatura A1 → cliente SIAPC/PAD + leiaute oficial + e-Validador → SICONFI/MSC → eSocial.

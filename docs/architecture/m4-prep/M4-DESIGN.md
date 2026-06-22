# M4-DESIGN — Prestação de Contas REAL (TCE-RS SIAPC/PAD + SICONFI) + Assinatura A1

> **Arquiteto do M4.** Design PRONTO-PARA-IMPLEMENTAR consolidando as 5 pesquisas + 4 verificações
> adversariais de `m4-prep/` e o estado real do código (`src/Modules/Transparencia`).
> Constituição: `CLAUDE.md` §6 (segurança/A1/Key Vault), §7 (sem hardcode de prazo/regra fiscal),
> §8 (integração gov: Polly + ACL + Outbox, idempotente), §10 (CQRS/Outbox), §16 (não inventar).
> **REGRA DE OURO:** nenhum leiaute/protocolo inventado. Cada ponto leva **CONFIANÇA** (da verificação)
> e a **LISTA DE DOCS OFICIAIS A OBTER** antes de codar.
> Piloto: Maximiliano de Almeida/RS (Prefeitura, Executivo, ≤50 mil hab → RGF/MCI **semestral**).
> Data: 2026-06-22.

---

## RESUMO EXECUTIVO (15 linhas)

1. **O ERP gera artefatos; NÃO transmite por API.** Verificação CONFIRMOU: o TCE-RS não tem web service público de envio — a transmissão é feita no **PAD desktop** operado por servidor humano + assinatura no **Processo Eletrônico** (e-Protocolo). O SICONFI também só tem API de **consulta**, não de upload. Logo o `EnviarRemessaTce` que faz `POST` é uma **premissa errada** e deve ser substituído por "empacotar + registrar protocolo + reconciliar".
2. O **valor automatizável** do M4 é: (a) gerar os `.TXT` posicionais do SIAPC no leiaute exato; (b) montar o **ZIP nomeado** (60 bytes); (c) gerar a **MSC CSV/XBRL-GL zipada** do SICONFI; (d) pré-validar localmente (críticas que bloqueiam); (e) **reconciliar** via API de Dados Abertos do SICONFI; (f) registrar recibo/protocolo como artefato de auditoria.
3. **Assinatura:** o RVE/RDI do TCE e a homologação do SICONFI são assinados **no portal pelo responsável** com certificado **pessoal** (TCE: ICP-Brasil; SICONFI: e-CPF **A3 em token**). O **A1 institucional no Key Vault** serve para o que **assinamos server-side** (eSocial M5, e — se confirmado — pacotes/anexos), **não** para homologar no SICONFI por A3.
4. Mesmo assim o **W2.1 (Serviço de Assinatura A1)** é necessário e transversal: carrega o `.pfx` cifrado do Key Vault em memória, assina (XML-DSig p/ eSocial; CMS/PKCS#7 p/ binário), **nunca exporta a chave**, é auditado e protegido por RBAC `documentos.assinar` + `admin.certificado.gerenciar`.
5. **Tudo externo** (API SICONFI consulta, futura transmissão, Key Vault) atrás de **ACL + Polly + Outbox** (já é o padrão das portas existentes).
6. **Ordem:** W2.1 (Assinatura A1) → M4.1 (leiaute SIAPC campo-a-campo) → M4.2 (e-Validador/RDI local) → M4.3 (empacotamento + protocolo) → M4.4 (SICONFI: MSC + reconciliação) — **mas só após obter os docs oficiais vigentes** (MT SIAPC 2026, Tabela do PAD 2026, IN 8/2025, Regras Gerais MSC 2026, Manual SICONFI de certificação).
7. **Bloqueador factual nº1:** o leiaute conferido é de **2010** e há **mudança anunciada para 2026** (Balanço Patrimonial/Financeiro + DFC). **Não congelar** nenhuma grade de campo sem o MT 2026.

**Caminho do arquivo:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/architecture/m4-prep/M4-DESIGN.md`

---

## 0. Estado real do código (baseline confirmado por leitura)

| Artefato | Estado | Implicação M4 |
|---|---|---|
| `RemessaTce` (agregado) | Ciclo `Gerada→Validada→Enviada→Homologada/Rejeitada`, hash SHA-256, alerta de prazo idempotente | **Mantém-se.** Renomear/ressignificar transição "Enviada" (ver §4). |
| `RegistroLeiaute` | 2 campos livres (`Tipo`, `Conteudo` string) | **Gap maior.** Falta grade posicional campo-a-campo (§2). |
| `Leiaute` (VO) | `Codigo`+`Versao` string livre | Falta catálogo versionado por exercício (§2.3). |
| `ResultadoValidacao`/`OcorrenciaValidacao` (RDI) | `Severidade` (Erro/Aviso), `PossuiErro` bloqueia | **Sólido.** Ajustar enum p/ `Justificado` como **seção**, não status (verificação 2.5). |
| `EnviarRemessaTce` handler | STUB: chama `siapcPad.TransmitirAsync` (POST fake), `ConsolidarConteudo` assume **1 arquivo** | **Reescrever** (§4). Premissa de POST é falsa. |
| `GerarRemessaTce.MontarPacote` | Gera `"{Tipo}|{Conteudo}"` em **UTF-8**, nome `*.txt` | **Reescrever** p/ ASCII ISO-8859-1 posicional + ZIP (§2). |
| `ISiapcPadGateway` | Porta `TransmitirAsync` (simulada `Task.CompletedTask`) | **Ressignificar** p/ empacotar+protocolar, não transmitir (§4). |
| `ISiconfiGateway` | `TransmitirAsync` retorna protocolo (simulada) | Dividir: **gerar MSC** (automatizável) vs **consultar** API Dados Abertos (§5). |
| `MatrizSaldos`/`LinhaContabil`/`DeclaracaoFiscal` (M3) | Existe; balanceia débito/crédito | **Insumo** do gerador MSC (§5). |
| Key Vault | Pacotes `Azure.Security.KeyVault.{Secrets,Certificates}` 4.7.0 + `System.Security.Cryptography.Xml` 8.0.1 referenciados; **nenhum serviço real ainda** | W2.1 implementa o serviço (§3). |
| RBAC | `Permissoes.cs` já tem `admin.certificado.gerenciar`, `documentos.assinar`, `transparencia.remessa.transmitir` | **Reusar** (§3.4); nada a criar no catálogo. |
| Assinatura existente | `IAssinaturaIcpBrasilService` (Saude) e `AssinarDocumento` (Protocolo) — **simulados** | W2.1 fornece a impl real reutilizável. |

---

## 1. Visão geral do fluxo (o que automatizamos vs. o que é ato humano no portal)

```
                       ┌──────────────────────── Tensorroot.Gov (AUTOMATIZÁVEL) ────────────────────────┐
Financas (PCASP/MCASP) │  GerarRemessaTce  →  .TXT posicionais (ISO-8859-1) + ZIP nomeado               │
Patrimonio/RH (M3/M5)  │  ValidarRemessaTce → pré-validação local (críticas que BLOQUEIAM) → RDI        │
                       │  EmpacotarRemessaTce → ZIP + hash + artefato de auditoria                      │
                       │  GerarMSC → CSV/XBRL-GL zipado (SICONFI)                                       │
                       └───────────────────────────────────────┬──────────────────────────────────────┘
                                                                │  (download do artefato)
                  ┌─────────────────────────────────────────────┴─────── ATO HUMANO (portal) ──────────┐
 TCE-RS:          │  servidor abre o PAD desktop → carrega TXT → valida → gera RVE/RDI → transmite →     │
                  │  assina RVE/RDI no e-Protocolo (cert. PESSOAL ICP-Brasil) → envia e-protocolo        │
 SICONFI:         │  gestor faz upload da MSC zipada → SICONFI converte em rascunho RREO/RGF/DCA →        │
                  │  assina/homologa (e-CPF A3 em token)                                                 │
                  └─────────────────────────────────────────────┬───────────────────────────────────────┘
                                                                │  (recibo/protocolo → de volta ao ERP)
                       ┌─────────────────────────────────────────┴──── RECONCILIAÇÃO (AUTOMATIZÁVEL) ────┐
                       │  Cliente API Dados Abertos SICONFI (consulta /extrato_entregas, /rreo, /msc_*)   │
                       │  Registro manual/assistido do protocolo do TCE → RemessaTce.RegistrarProtocolo   │
                       └─────────────────────────────────────────────────────────────────────────────────┘
```

> **CONFIANÇA: ALTA** de que não há API de upload (CONFIRMADO nas 3 verificações: transmissão SIAPC §A3/Risco 1; SICONFI Risco R-2). **Não implementar cliente HTTP de envio** ao TCE nem ao SICONFI.

---

## 2. Geração da remessa SIAPC/PAD — onde plugar o leiaute campo-a-campo

### 2.1 Forma do arquivo (CONFIRMADO — formato; INCERTO — campos 2026)

- **Codificação:** texto **ASCII ISO-8859-1 (Latin-1)**, largura fixa, terminador **CR/LF (0x0D0A)**. Hoje geramos UTF-8 com `\n` → **corrigir**. **CONFIANÇA: ALTA** (MT Vol. V §2, verificação e-validador 4.x e assinatura §3).
- **Tipos de campo:** Caractere (esq., espaços à dir.), Numérico (dir., zeros à esq.), **Valor** (centavos, sinal `+`=2B/`-`=2D à esq.), **Data** `ddmmaaaa`. **CONFIANÇA: ALTA** (formato), porém campos por arquivo **INCERTOS** p/ 2026.
- **Cabeçalho (1ª linha):** CNPJ + nome do Setor de Governo + datas + **Código da Remessa** (Numérico 12, posições 119–130). **CONFIRMADO** que o Código da Remessa é **gerado pelo PRÓPRIO ENTE** (não pelo PAD; verificação assinatura §3 corrige a especulação anterior). **CONFIANÇA: ALTA.**
- **Finalizador:** linha `FINALIZADOR` + qtd de registros (Numérico 10): `FINALIZADOR0000000000`. **CONFIANÇA: ALTA** (verificação e-validador 4.2).
- **Nome do ZIP (estruturado):** `CNPJ(14).DataIni(ddmmaaaa).DataFim.DataGer.Tipo(1).CodRemessa(12).zip`; Tipo = `P`(Prefeitura)/`C`(Câmara)/`A`/`F`/`E`/`S`/`O`. Ex.: `99999999000199.01012007.31052007.15062007.P.000000000010.zip`. **CONFIANÇA: ALTA** (verificação 4.3), mas re-confirmar no MT 2026.

### 2.2 Onde plugar o leiaute (modelo de domínio proposto)

O design separa **definição** (grade versionada, dirigida por dados) de **emissão** (writer posicional).

```
Domain/RemessasTce/Leiaute/
  CampoLeiaute (VO)       { Nome, PosicaoInicial, Tamanho, Tipo(Caractere|Numerico|Valor|Data),
                            Obrigatorio, Preenchimento(EsqEspaco|DirZero|SinalValor) }
  RegistroLeiauteDef (VO) { CodigoRegistro, NomeArquivo (ex. "EMPENHO.TXT","TCE_4810.TXT"),
                            IReadOnlyList<CampoLeiaute> }
  LeiauteSiapc (agregado de referência, por exercício/versão)
                          { Codigo="SIAPC", Versao="2026", IReadOnlyList<RegistroLeiauteDef>,
                            TipoSetorGoverno, regras de cabeçalho/finalizador }
```

- **`ILeiauteCatalogo`** (porta já existente) deixa de ser stub: passa a **resolver a grade real do exercício** a partir de configuração versionada (JSON/seed na Infrastructure), nunca hardcode em C# (§7). `ObterDataLimiteAsync` lê prazos parametrizáveis por tenant/exercício.
- **`EmissorRegistroSiapc`** (serviço de domínio/Application): recebe `RegistroLeiauteDef` + valores e escreve a **linha posicional** usando `Span<char>`/`Span<byte>` (§7 — `Span<T>` para remessas pesadas, sem alocação). Aplica padding/sinal/encoding ISO-8859-1.
- **`MontarPacoteSiapc`** (substitui `MontarPacote`): gera **vários `.TXT`** (um por `RegistroLeiauteDef.NomeArquivo`), cabeçalho+corpo+finalizador cada, e empacota em **ZIP** com o nome estruturado. `ConsolidarConteudo` (no envio) deixa de assumir **1 arquivo** → consolida o ZIP inteiro para o hash.

### 2.3 De onde vêm os dados (mapeamento por arquivo — a obter)

Cada `RegistroLeiauteDef` é alimentado por uma query de leitura cross-module **via Contracts** (nunca acesso interno a outro módulo — §2):
- Orçamento/empenho/liquidação/pagamento/balancetes → `Financas.Contracts`.
- Folha (TCE_4810) / cadastro de funcionários (TCE_4820) → `RecursosHumanos.Contracts` (M5).
- Bens/patrimônio → `Patrimonio.Contracts`.
- Licitações/contratos → `Administracao.Contracts`.

> **LISTA DE DOCS A OBTER (bloqueia §2):**
> 1. **MT SIAPC Vol. I–V do exercício 2026** (grade campo-a-campo de TODOS os `.TXT`, incl. novos Balanço Patrimonial/Financeiro + DFC). — portal TCE-RS / `mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf` (verificar versão).
> 2. **Tabela do PAD 2026** (mapeamento contas→RREO/RGF e códigos de crítica).
> 3. **Versão do PAD 2026** (sucessora da 25.0.0.0) e formato exato do ZIP/cabeçalho do exercício.
> 4. **IN TCE-RS nº 8/2025 (23/09/2025)** — norma **vigente** (revoga a 5/2024; cadeia 6/2019→18/2023→5/2024→8/2025). Define critérios RREO/RGF e percentuais (ex.: limite de pessoal **81%** em 2025). **Não usar** IN 13/2021 / 4/2021 (revogadas).

---

## 3. Assinatura A1 (W2.1) — Serviço de Assinatura transversal

### 3.1 Posição factual (o que o A1 server-side cobre e o que NÃO cobre)

- **NÃO cobre** a assinatura final do TCE-RS (RVE/RDI são assinados no e-Protocolo com cert. **pessoal** do responsável, ato interativo) nem a homologação do SICONFI (**e-CPF A3 em token** do gestor). **CONFIANÇA: ALTA** de que o A1 do Key Vault **não** substitui esses atos (verificação SICONFI Risco R-3; gap §2.3; transmissão A7/A17).
- **COBRE** as assinaturas que o sistema produz **server-side**: eSocial (XML-DSig Enveloped, M5) e — **se** confirmado — assinatura CMS/PKCS#7 de anexos/pacotes. **CONFIANÇA: ALTA** para eSocial (manual oficial conferido linha a linha); **INCERTO** se o TCE/SICONFI aceitam pacote pré-assinado por A1 → `[a confirmar]`.
- **Conclusão de design:** W2.1 é construído **agora** porque é transversal (Protocolo, Saude, eSocial/M5) e porque o M4 precisa ao menos do **carregamento seguro do A1** e do **hash/CMS de anexos**. A homologação no portal **fica como ato humano documentado**, não automatizada.

### 3.2 Serviço de Assinatura — contrato e ciclo de vida da chave

Porta nova em `BuildingBlocks.Application` (transversal, reutilizável por todos os módulos):

```
IServicoAssinaturaDigital
  Task<ResultadoAssinatura> AssinarXmlAsync(XmlDocument doc, OpcoesAssinaturaXml opcoes, CancellationToken ct);   // eSocial
  Task<byte[]> AssinarCmsAsync(ReadOnlyMemory<byte> conteudo, OpcoesAssinaturaCms opcoes, CancellationToken ct);   // binário/.TXT (se exigido)
  Task<X509Certificate2Info> ObterInfoCertificadoAsync(CancellationToken ct);                                      // validade/titular, sem expor chave
```

**Como o A1 cifrado no banco do tenant é carregado, assina e NUNCA vaza** (CLAUDE.md §5/§6):

1. **Armazenamento:** o `.pfx` é segredo do **Azure Key Vault por tenant** (CLAUDE.md §6 — "certificado NUNCA no repo; segredos só no Key Vault"). A senha do `.pfx` também é segredo no Key Vault. **Nada de `.pfx` em disco em produção.**
   - *Nota de divergência com o enunciado:* o enunciado fala em "A1 cifrado no banco do tenant". O CLAUDE.md §6 manda **Key Vault**. **Decisão de design:** fonte canônica = **Key Vault**; se o produto exigir custódia no banco, o material deve ser **envelope-encrypted** com chave do Key Vault (envelope encryption), nunca em claro. `[a confirmar com o dono: Key Vault vs banco cifrado]`.
2. **Carga em memória:** `X509CertificateLoader.LoadPkcs12(bytes, senha, KeyStorageFlags.EphemeralKeySet)` (o ctor `new X509Certificate2(bytes, senha)` está **obsoleto** no .NET 9 — SYSLIB0057; mas o projeto é **net8** → confirmar API exata no TFM). `EphemeralKeySet` evita gravar a chave no keystore do SO/container. **CONFIANÇA: ALTA** (verificação assinatura §5).
3. **Uso:** `cert.GetRSAPrivateKey()` (não `cert.PrivateKey`, obsoleto) só durante a assinatura.
4. **Descarte:** `using`/`Dispose` determinístico do `X509Certificate2` e da chave logo após assinar; o objeto vive o mínimo possível. A chave **nunca** é serializada, logada, retornada por API, nem persistida.
5. **Validação de cadeia:** `X509Chain` com a **cadeia AC-Raiz ICP-Brasil embarcada** (containers Linux não trazem as raízes ICP-Brasil no trust store) + revogação CRL/OCSP + uso de chave (digitalSignature/nonRepudiation). **CONFIANÇA: ALTA** (risco operacional confirmado).
6. **Resiliência/observabilidade:** acesso ao Key Vault atrás de **Polly** (timeout/retry/circuit breaker) e **OpenTelemetry** com `TenantId`/`CorrelationId`. Toda assinatura gera **trilha de auditoria imutável** (quem, quando, qual certificado/titular, qual artefato — §6).

### 3.3 Parâmetros de assinatura por destino (CONFIRMADO p/ eSocial)

| Destino | Formato | Algoritmo | Canonicalização | KeyInfo |
|---|---|---|---|---|
| **eSocial** (M5) | XML-DSig **Enveloped** | RSA-SHA256 (`xmldsig-more#rsa-sha256`), digest `xmlenc#sha256` | **C14N inclusiva** `REC-xml-c14n-20010315` (**NÃO** exc-c14n — pegadinha do default .NET) | **EndCertOnly** (só `<X509Certificate>`) |
| **TCE/.TXT** (se exigido) | CMS/PKCS#7 (CAdES) | RSA-SHA256 | n/a | conforme perfil |

**CONFIANÇA: ALTA** (manual eSocial v1.15 §6.7 conferido). Atenção: a regra "remover `xmlns:xsi`/`xmlns:xsd` do root" é **folclore de implementação** (não está no manual) — validar empiricamente no Verificador ITI/Produção Restrita, **não** codar como requisito documentado.

### 3.4 RBAC (reusar catálogo existente — nada a criar)

- **`admin.certificado.gerenciar`** → cadastrar/rotacionar o A1 do tenant no Key Vault (endpoint admin; auditado).
- **`documentos.assinar`** / **`protocolo.documento.assinar`** → invocar o serviço de assinatura em atos com efeito legal.
- **`transparencia.remessa.transmitir`** → gate de **Segregação de Funções (SoD)** para empacotar/registrar a remessa.
- Política materializada por `PermissionPolicyProvider` (`perm:<escopo>`), deny-by-default. **CONFIANÇA: ALTA** (lido em `Permissoes.cs` + `PermissionAuthorization.cs`).

> **LISTA DE DOCS A OBTER (§3):** (a) Manual do PAD/Processo Eletrônico TCE-RS 2026 — confirma **se** há assinatura programática aceita ou só ato interativo; (b) Manual do Usuário SICONFI — Acesso e Certificação Digital — confirma se A1 é aceito além de A3; (c) Cadeia AC-Raiz ICP-Brasil vigente (`repositorio.iti.gov.br`); (d) MOS eSocial S-1.3 (cruzar com o Manual do Desenvolvedor v1.15) — só relevante no M5.

---

## 4. Transmissão (SICOE/PAD) + entrega SICONFI — Polly + ACL + Outbox

### 4.1 TCE-RS: ressignificar `ISiapcPadGateway` e o estado "Enviada"

- **NÃO** há `POST`. A porta deixa de "transmitir" e passa a **`EmpacotarRemessaSiapc`** + **`RegistrarProtocoloTce`**:
  - `EmpacotarAsync(remessa)` → produz o ZIP nomeado (assinado por A1 **só se confirmado** que o TCE aceita) e o disponibiliza para download pelo servidor que opera o PAD. Idempotente por `RemessaTceId`.
  - `RegistrarProtocoloAsync(remessa, protocolo, dataRecibo, status)` → grava o recibo retornado pelo e-Protocolo (`pendente|concluída|carregada`) como **artefato de auditoria** (§6). Entrada manual/assistida — o servidor cola o protocolo do portal.
- O estado **`Enviada`** do agregado passa a significar **"empacotada e disponibilizada / protocolo registrado"**, não "transmitida por HTTP". O comentário do handler e a doc da porta devem refletir isso. **CONFIANÇA: ALTA** (Risco 1 das verificações).
- **Matriz de assinaturas como gate** (modelar a partir do FAQ — verdade verificada): todo mês = **4 assinaturas** (Regular RVE: Responsável+Contabilista; Complementar RVE + RDI: Responsável+Controle Interno+Folha); meses de fechamento (abr/jun/ago/dez = entregas mai/jul/set/jan) = **5** (somam RGF+MCI). Maximiliano = **semestral**: 1º sem. entregue em **JULHO** (competência junho), 2º em **JANEIRO** (competência dezembro). **CORREÇÃO:** o "junho/janeiro" anterior estava com off-by-one. **CONFIANÇA: ALTA** (verificação transmissão C1 + e-validador §3). Prazos **parametrizáveis** (§7) — não hardcode.

### 4.2 SICONFI: gerar MSC + reconciliar (não transmitir)

- **`GerarMSC`** (novo handler): a partir de `MatrizSaldos`/`DeclaracaoFiscal` (M3), emite **CSV adaptado do XBRL-GL** ou instância XBRL-GL, **zipado**. **CONFIANÇA: ALTA** (Regras Gerais MSC 2026 conferidas).
- **MSC é UMA por município, enviada SÓ pelo Executivo**, consolidando o Legislativo via informação complementar "Poder e Órgão". **NÃO** implementar "cada tenant envia sua MSC". **CONFIANÇA: ALTA** (verificação SICONFI C-1 — maior risco de modelagem). Usar nomenclatura oficial **"MSC de encerramento"** (não "mês 13").
- **`ISiconfiGateway` divide-se em duas portas:**
  - `IGeradorMsc` (produz o arquivo — automatizável 100%).
  - `IConsultaSiconfi` (cliente da **API de Dados Abertos**, **só consulta**, `https://apidatalake.tesouro.gov.br/ords/siconfi/tt/`, JSON, sem auth, **rate limit 1 req/s**, paginação 5.000). Endpoints: `/extrato_entregas`, `/rreo`, `/rgf`, `/dca`, `/msc_patrimonial|orcamentaria|controle`, `/entes`. Params confirmados: `co_tipo_matriz`(MSCC/MSCE), `id_tv`(beginning/ending/period_change), `co_esfera`(M/E/U/C), `co_poder`(E/L/J/M/D), `in_periodicidade`(S/Q). `id_ente` = código IBGE. **CONFIANÇA: ALTA** (Swagger conferido). Usar para **reconciliar** o homologado vs. o calculado e **auditar** status de entrega.
- A homologação assinada (e-CPF A3) permanece **ato humano** no portal. **CONFIANÇA: ALTA** (R-2/R-3).

### 4.3 Padrões obrigatórios (CLAUDE.md §8/§10/§11)

- **ACL:** todo acesso externo (Key Vault, API SICONFI) atrás de porta/Anti-Corruption Layer (já é o padrão).
- **Polly:** timeout + retry + circuit breaker em Key Vault e no cliente HTTP do SICONFI (consulta).
- **Outbox:** `RemessaEnviadaTceIntegrationEvent`, `MscEnviadaSiconfiIntegrationEvent`, `PrazoRemessaVencidoIntegrationEvent` (já existem) publicados **transacionalmente** via Outbox. **Idempotência** por `RemessaTceId`/`DeclaracaoFiscalId`.
- **Auditoria:** protocolo/recibo, hash do pacote, certificado usado e cada assinatura → trilha imutável (§6).

> **LISTA DE DOCS A OBTER (§4):**
> 1. **Manual SICOE / Res. 1.074/2017 / IN 08/2017** byte-a-byte — confirmar (definitivamente) ausência de API e o formato do recibo. (Relevante p/ indireta; piloto é PAD.)
> 2. **eValidador_NovaAutenticacao.pdf** — confirma fluxo interativo (QR-code/TCE-login) e ausência de endpoint.
> 3. **Ofício Circular SIAPC 2026** (calendário/prazos da remessa regular; o TCE prorroga prazos com frequência).
> 4. **Regras Gerais MSC 2026** (caminho de carga; confirmação "só Executivo envia").
> 5. **Manual do Usuário SICONFI — Acesso e Certificação Digital** (A1 vs A3; quem assina a carga).
> 6. **Portaria STN de calendário de entregas** do exercício (datas DCA/RREO/RGF).

---

## 5. Ordem de implementação (M4 + W2.1)

> **Pré-condição global:** obter os docs oficiais vigentes das listas acima **antes** de codar cada bloco. Sem o MT 2026 / Tabela do PAD 2026 / IN 8/2025, **não** escrever grade de campos nem críticas (CLAUDE.md §16). Specs BDD `Given/When/Then` antes do código (§1).

| # | Bloco | Entrega | Depende de | Doc oficial bloqueador |
|---|---|---|---|---|
| **W2.1** | **Serviço de Assinatura A1** | `IServicoAssinaturaDigital` em BuildingBlocks: carga segura do `.pfx` do Key Vault, XML-DSig (eSocial), CMS opcional, validação de cadeia ICP-Brasil, RBAC `admin.certificado.gerenciar`/`documentos.assinar`, auditoria, Polly | Key Vault (pacotes já presentes) | Cadeia AC-Raiz ICP-Brasil; (eSocial: MOS S-1.3 só no M5) |
| **M4.1** | **Leiaute SIAPC campo-a-campo** | `CampoLeiaute`/`RegistroLeiauteDef`/`LeiauteSiapc`, `EmissorRegistroSiapc` (ISO-8859-1, posicional, `Span<T>`), `ILeiauteCatalogo` real versionado por exercício; reescrever `MontarPacote`→`MontarPacoteSiapc` (multi-arquivo + cabeçalho/finalizador) | — | **MT SIAPC 2026 (Vol. I–V); Tabela do PAD 2026** |
| **M4.2** | **e-Validador / RDI (pré-validação local)** | Motor de críticas que **bloqueiam** (Erro) vs avisos; alinhar `OcorrenciaValidacao`/`ResultadoValidacao` (Justificado = seção, não status); mapear `Status=Erro`→bloqueia `EnviarRemessaTce` | M4.1 | **Tabela do PAD 2026; Manual do PAD 2026; IN 8/2025** (percentuais/limiares — nunca da wiki Thema) |
| **M4.3** | **Empacotamento + protocolo TCE** | ZIP nomeado (60 bytes); ressignificar `ISiapcPadGateway`→`EmpacotarRemessaSiapc`+`RegistrarProtocoloTce`; estado "Enviada" = empacotada/protocolada; matriz de assinaturas como gate; prazos parametrizáveis | M4.1, M4.2, W2.1 | **Manual SICOE; Ofício Circular SIAPC 2026** |
| **M4.4** | **SICONFI: MSC + reconciliação** | `IGeradorMsc` (CSV/XBRL-GL zipado, 1/município, só Executivo); `IConsultaSiconfi` (API Dados Abertos, Polly, 1 req/s) p/ reconciliar `/extrato_entregas` etc.; homologação = ato humano | M3 (MatrizSaldos) | **Regras Gerais MSC 2026; Manual Certificação SICONFI; Portaria STN calendário** |

**Sequência recomendada:** **W2.1 → M4.1 → M4.2 → M4.3 → M4.4.** W2.1 primeiro por ser transversal (desbloqueia Protocolo/Saude/eSocial) e por validar o ciclo de vida seguro do A1 cedo. M4.4 pode rodar em paralelo a M4.3 (insumos distintos: SICONFI usa MatrizSaldos do M3, TCE usa o leiaute do M4.1).

---

## 6. Riscos de implementação travados na verificação (NÃO codar sem doc vigente)

1. **Leiaute de 2010 ≠ 2026** (BP/BF + DFC novos). → grade **dirigida por dados versionados**, nunca constante.
2. **Premissa de API de upload (TCE e SICONFI) é FALSA.** → não escrever cliente HTTP de envio; só gerar artefato + reconciliar.
3. **A1 do Key Vault NÃO homologa no SICONFI (A3 token) nem assina o RVE (cert. pessoal).** → não automatizar homologação por A1.
4. **Percentuais/críticas/prazos** vêm de **IN 8/2025 + Tabela do PAD 2026**, parametrizáveis por tenant/exercício (§7) — nunca da wiki de fornecedor (Thema) nem hardcoded.
5. **MSC é uma por município (só Executivo).** → não modelar envio por tenant Câmara.
6. **Canonicalização eSocial = C14N inclusiva** (não o default exc-c14n do .NET) — testar contra Verificador ITI/Produção Restrita (M5).
7. **Off-by-one do RGF semestral corrigido:** 1º sem. entrega **julho**, 2º **janeiro**.

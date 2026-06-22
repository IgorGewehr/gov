# Red-Team — Injeção, Exposição de Segredos e Superfície de Upload

> Análise adversarial **estática** do código em `src/`. Foco: injeção (SQL/EF/deserialização),
> exposição de segredos (logs, respostas de erro, appsettings, KEK/Cofre) e superfície de upload
> (.pfx do Cofre, ingestão NFS-e/ADN). Itens marcados **[a confirmar]** exigem runtime para prova final.
> Data: 2026-06-22. Escopo: ERP GovTech multi-tenant .NET8 (dinheiro público + dados sensíveis, TCE).

## Sumário

**11 achados** — 1 ALTA, 5 MÉDIA, 4 BAIXA, 1 INFO.

Boa notícia para o red-team defensivo: o **núcleo criptográfico do Cofre é sólido**
(AES-256-GCM com nonce aleatório por cifragem, AAD = TenantId||Thumbprint amarrando o blob ao tenant,
`EphemeralKeySet`, `CryptographicOperations.ZeroMemory` no `finally`, KEK nunca hardcoded, mensagens de
erro de cripto que não vazam material) e o **XML signer está protegido contra XXE**
(`XmlResolver = null` + `DtdProcessing.Prohibit`). Não foi encontrado **SQL concatenado com entrada do
usuário** — o único SQL bruto é DDL com literais controlados pelo código. Os achados abaixo são de
**configuração/operação, hardening e robustez de borda**, não de uma SQLi clássica explorável hoje.

---

## TOP 3 (piores)

### 1. [ALTA] Connection string do tenant (com credenciais de banco em PROD) persistida em CLARO no catálogo da plataforma
- **Arquivos:**
  - `src/Platform/Tensorroot.Gov.Platform/Tenancy/Tenancy.cs:51` (`public string? ConnectionString { get; private set; }`)
  - `src/Platform/Tensorroot.Gov.Platform/Persistence/PlatformDbContext.cs:35`
    (`builder.Property(tenant => tenant.ConnectionString).HasMaxLength(500);` — **sem `HasConversion` de cifra**)
  - Origem do dado: `src/ApiHost/Program.cs:221-230` → `ProvisionarTenantRequest.ConnectionString`
    → `TenantProvisioner.ProvisionarAsync` (`src/ApiHost/Provisioning/TenantProvisioner.cs:39-41`)
- **Cenário de exploração:** em produção (`Database:Provider = SqlServer`) a coluna `Tenants.ConnectionString`
  contém usuário/senha do banco DEDICADO de cada município. Como é gravada em texto puro no banco de
  CONTROLE, qualquer um com leitura desse banco (dump/backup vazado, read-replica, suporte, SQLi em
  outro ponto, insider) obtém as credenciais de **todos os bancos de tenant** — pivot direto para folha,
  CPF, dados de saúde/assistência de todos os municípios. Viola CLAUDE.md §6 ("segredos só no Key Vault").
- **Correção:** não persistir credenciais no catálogo. Guardar apenas uma **referência** a um secret do
  Key Vault (ex.: nome do segredo) e resolver a connection string em runtime via Key Vault; ou, no
  mínimo, aplicar `HasConversion` com cifra de coluna (envelope, igual ao Cofre) + AAD por TenantId.
  Garantir também que dumps/telemetria nunca serializem a entidade `Tenant` com esse campo.

### 2. [MÉDIA] Ausência de handler global de exceção → vazamento de stack trace / detalhes internos nas respostas de erro
- **Arquivo:** `src/ApiHost/Program.cs` (pipeline montado nas linhas ~174-238: há `UseSerilogRequestLogging`,
  `UseRateLimiter`, `UseAuthentication/Authorization`, mas **não há** `UseExceptionHandler`,
  `AddProblemDetails` nem `UseDeveloperExceptionPage` controlado por ambiente).
- **Cenário de exploração:** qualquer exceção não tratada num handler MediatR/endpoint (ex.: `throw new
  InvalidOperationException("Usuario nao encontrado.")` em `AlterarSenha.cs:38`; o `InvalidOperationException`
  de tenant não resolvido em `TenantConnectionResolver.cs:79`; falha de EF/SQL) sobe sem tratamento. No
  Kestrel sem `UseExceptionHandler`, a resposta padrão é 500 com corpo de stack trace quando o detalhe não
  é suprimido — expõe namespaces, caminho de arquivos, versões e a estrutura interna a um atacante. Em
  GovTech sob TCE isso é munição de reconhecimento e potencial vazamento de mensagens com dados. **[a confirmar]** o
  corpo exato depende do ambiente em runtime.
- **Correção:** adicionar `app.UseExceptionHandler` + `builder.Services.AddProblemDetails()` que devolve
  `ProblemDetails` genérico (sem stack/detalhe) em PROD, logando o detalhe só no servidor com CorrelationId.

### 3. [MÉDIA] Endpoint de provisionamento aceita `ConnectionString` arbitrária do corpo da requisição (injeção de parâmetros de conexão / SSRF de banco)
- **Arquivos:** `src/ApiHost/Program.cs:221-230` (`MapPost("/admin/tenants", ...)` desserializa
  `ProvisionarTenantRequest` direto do body); `src/ApiHost/Provisioning/TenantProvisioner.cs:39-49`
  repassa a string para `module.MigrarBancoAsync(conexao, ...)` que abre conexão real.
- **Cenário de exploração:** um operador de plataforma (ou token com `plataforma.tenants.provisionar`
  comprometido) define uma connection string controlada apontando para host/porta arbitrários, habilitando
  parâmetros perigosos do provider (ex.: em SqlServer, `Server=atacante;...`, ou flags que alteram trust/
  encryption), efetivamente fazendo o servidor abrir conexão de saída para um destino escolhido (SSRF de
  banco) e migrar schema lá. Mesmo sendo endpoint privilegiado, há ganho de privilégio lateral e
  exfiltração. O campo não é validado/allow-listed.
- **Correção:** não aceitar connection string crua do cliente. Derivar a conexão de um template
  server-side + identificador de tenant, ou validar contra allow-list de hosts e proibir keywords
  sensíveis do provider. Combinar com o achado #1 (referência ao Key Vault em vez de string crua).

---

## Demais achados

### 4. [MÉDIA] `appsettings.Development.json` versionado com segredo JWT de DEV e senha do admin padrão
- **Arquivo:** `src/ApiHost/appsettings.Development.json:12` (`"Secret": "DEV-ONLY-troque-..."`) e
  `:30-33` (`Identidade:Admin` = `admin@tensorroot.gov` / `Mudar@123`). O `.gitignore`
  (`/.gitignore:26-27`) ignora `secrets.json` e `appsettings.*.Local.json`, mas **NÃO**
  `appsettings.Development.json` — logo ele está no repositório.
- **Cenário de exploração:** o seed de DEV (`Program.cs:264-279`) provisiona o tenant demo e o admin
  `admin@tensorroot.gov / Mudar@123`. Se um ambiente não-prod (homologação acessível) rodar com esse
  appsettings, qualquer um forja JWTs válidos (segredo conhecido) ou loga como admin. Credencial conhecida
  em repo é antipadrão mesmo rotulada "DEV".
- **Correção:** mover o secret/senha de DEV para `secrets.json`/variável de ambiente (já ignorados);
  manter no `appsettings.Development.json` apenas placeholders. Garantir que homologação use Key Vault.

### 5. [MÉDIA] HS256 sem validação de comprimento mínimo do segredo (chave fraca aceita)
- **Arquivos:** `src/ApiHost/Program.cs:83-102` (lê `Jwt:Secret` e cria `SymmetricSecurityKey` sem checar
  tamanho); `src/Modules/Identidade/.../Seguranca/EmissorToken.cs:45-77` (mesma ausência na emissão).
- **Cenário de exploração:** HS256 exige ≥256 bits de entropia para resistir a brute-force/colisão. O
  código aceita qualquer string não-vazia como chave. Um segredo curto/fraco em PROD (erro de config)
  permite recuperar a chave offline e **forjar tokens com qualquer claim `perm`** — bypass total do RBAC
  multi-tenant. O sistema não falha rápido nesse cenário.
- **Correção:** validar `Encoding.UTF8.GetBytes(secret).Length >= 32` no startup e na emissão; abortar se menor.

### 6. [MÉDIA] Validação de cadeia ICP-Brasil é no-op (somente KeyUsage) — aceita certificado A1 não confiável / autoassinado no upload
- **Arquivo:** `src/Modules/Cofre/.../Cripto/ValidacaoCadeiaIcpBrasil.cs` (método `Validar` só checa
  KeyUsage; a verificação de cadeia/revogação está marcada como `TODO(validar-oficial)` e desligada).
  Pior: `PermiteAssinatura` retorna `true` quando **não há** extensão KeyUsage (`return !temKeyUsage;`).
- **Cenário de exploração:** no upload do `.pfx` (`CofreEndpoints.cs:34`), um certificado autoassinado ou
  fora da ICP-Brasil — desde que abra com a senha e tenha (ou omita) KeyUsage — é aceito, cifrado e
  custodiado como A1 ativo do tenant. Documentos/remessas eSocial/TCE passam a ser assinados com cert
  não confiável; risco de repúdio e rejeição pelo órgão, e de um operador malicioso plantar um cert
  próprio. **[a confirmar]** comportamento exato depende do conteúdo do `.pfx` em runtime.
- **Correção:** implementar a validação estrita já descrita no TODO (CustomRootTrust com AC-Raiz
  ICP-Brasil embarcadas + `RevocationMode=Online`) antes do go-live; e exigir KeyUsage presente e de
  assinatura (não aceitar ausência como permissão).

### 7. [BAIXA] `Type.GetType(mensagem.Type)` na desserialização do Outbox (resolução de tipo por string)
- **Arquivo:** `src/BuildingBlocks/.../Outbox/OutboxPublisher.cs:64-66`
  (`Type.GetType(mensagem.Type)` seguido de `JsonSerializer.Deserialize(content, tipo)`).
- **Cenário de exploração:** hoje a coluna `Type` é escrita pelo próprio sistema ao enfileirar o evento,
  então **não é entrada direta do usuário** (risco baixo). Porém, se um atacante conseguir escrever na
  tabela Outbox (SQLi futura, insider, bug de tenant), pode apontar `Type` para um tipo arbitrário
  carregável e instanciá-lo via desserialização — vetor de type-confusion. System.Text.Json sem
  `TypeNameHandling` limita o dano, mas a resolução dinâmica de tipo é superfície desnecessária.
- **Correção:** resolver o tipo por um **mapa allow-list** (nome lógico → `Type` conhecido) em vez de
  `Type.GetType` aberto; rejeitar tipos fora do conjunto de Integration/Domain events registrados.

### 8. [BAIXA] DDL idempotente do AuditTrail monta nome de trigger por interpolação de `Schema` (defesa em profundidade)
- **Arquivo:** `src/BuildingBlocks/.../Multitenancy/SchemaProvisioner.cs:66-92` (interpola `{schema}` em
  `CREATE TRIGGER [{schema}].[...]` via `ExecuteSqlRawAsync`) e `:139`
  (`ALTER TABLE ... ADD COLUMN "{coluna}" {definicao}`).
- **Cenário de exploração:** `schema` vem de `(contexto as ModuleDbContext)?.Schema` — **literal controlado
  pelo código do módulo**, não por entrada do usuário; idem `coluna`/`definicao` (constantes). Logo **não é
  injetável hoje**. Fica registrado como dívida: a montagem por string é frágil a uma futura refatoração
  que torne o schema configurável por dado externo.
- **Correção:** se o schema vier a ser dinâmico, validar contra `^[a-z][a-z0-9_]*$` / usar `QUOTENAME`;
  manter os nomes como constantes fechadas.

### 9. [BAIXA] Rate limiting não protege o login por conta (brute-force de senha distribuído)
- **Arquivos:** `src/ApiHost/Program.cs:114-127` (limiter global particionado por
  `User.Identity?.Name ?? RemoteIpAddress ?? "anonymous"`, 100 req/min); login em
  `src/Modules/Identidade/.../IdentidadeEndpoints.cs:29-63` é `AllowAnonymous` (sem `Name`, particiona por IP).
- **Cenário de exploração:** o login anônimo cai na partição por IP. Um atacante com pool de IPs (botnet)
  distribui 100 tentativas/min **por IP** contra uma conta-alvo, contornando o limite. Embora a mensagem
  "Credenciais invalidas." seja constante (bom: não há enumeração de usuário) e BCrypt(12) seja lento, não
  há trava por conta, backoff exponencial nem CAPTCHA. **[a confirmar]** efetividade real depende do volume.
- **Correção:** limiter dedicado por e-mail-alvo no `/login` (contagem de falhas por conta com
  bloqueio temporário/backoff), além do limite por IP.

### 10. [BAIXA] BCrypt trunca em 72 bytes — senhas até 256 chars aceitas, mas só os primeiros 72 bytes contam
- **Arquivos:** `src/Modules/Identidade/.../Usuarios/AlterarSenha.cs:21` (`MaximumLength(256)`);
  hashing em `src/Modules/Identidade/.../Seguranca/SenhaHasher.cs` (BCrypt puro, sem pré-hash).
- **Cenário de exploração:** caracteres além do 72º byte são silenciosamente ignorados pelo BCrypt. Duas
  senhas longas que compartilham o mesmo prefixo de 72 bytes colidem — reduz a entropia efetiva de
  passphrases longas e dá falsa sensação de força. Impacto limitado (≥73 bytes é incomum).
- **Correção:** pré-hash SHA-256 (Base64) antes do BCrypt, ou documentar/limitar o input a ≤72 bytes.

### 11. [INFO] Mensagens de exceção de domínio retornadas ao cliente (`excecao.Message`) — verificar conteúdo
- **Arquivos:** `src/Modules/Cofre/.../CofreEndpoints.cs:69,85,114` (`erro = excecao.Message` de
  `CertificadoInvalidoException`); `src/Modules/Identidade/.../IdentidadeEndpoints.cs:126,142`
  (`erro = excecao.Message` de `ConcessaoNaoAutorizadaException`).
- **Cenário de exploração:** as mensagens hoje inspecionadas são genéricas e cuidadosas (a de cripto
  explicitamente **não** vaza material — `CofreCripto.cs:93-94`). Risco INFO: é um padrão de "ecoar
  `Message` da exceção" que, se uma exceção futura embutir dado sensível (CPF, caminho, valor), passa a
  vazar pela borda sem filtro. Combinado com o achado #2, reforça a necessidade de saneamento central.
- **Correção:** padronizar respostas de erro via `ProblemDetails` com mensagens curadas; nunca repassar
  `Message` de exceções não-curadas (só de exceções de domínio explicitamente seguras).

---

## Pontos verificados como SEGUROS (não-achados, para registro)

- **SQLi:** nenhum `FromSqlRaw/ExecuteSqlRaw` com interpolação de entrada do usuário. Filtros do
  visualizador de auditoria (`AdminEndpoints.cs:100-114`) usam LINQ parametrizado por EF; paginação com
  `Math.Clamp` (cap 200). Gateway ADN (`AdnNfseGateway.cs:19-21`) usa `Uri.EscapeDataString` no CNPJ e JSON.
- **XXE:** `AssinadorXmlDsig.cs:37-39` cria `XmlReader` com `XmlResolver = null` e `DtdProcessing.Prohibit`.
- **Cofre/KEK:** AES-256-GCM com nonce aleatório por operação (`AesGcmEnvelope.cs:28`), AAD por
  TenantId||Thumbprint (`CofreCripto.cs:181-182`), `EphemeralKeySet`, zeragem de memória, KEK exigida via
  env var (`ProvedorKekConfig.cs:26-37`, nunca hardcoded). Endpoints do Cofre nunca retornam .pfx/senha/chave.
- **Upload .pfx:** valida `HasFormContentType`, exige campos, limita a 256 KB (`CofreEndpoints.cs:24,54`),
  processa só em memória (`MemoryStream`), nunca grava em disco.
- **Login:** mensagem de falha constante e indistinguível (sem enumeração de conta/tenant).

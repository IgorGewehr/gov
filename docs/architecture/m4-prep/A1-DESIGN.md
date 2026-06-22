# A1-DESIGN — Cofre + Assinatura do certificado A1 (ICP-Brasil)

> M4-prep · Tensorroot.Gov · Detalhamento do **W2.1** (Serviço de Assinatura A1) do `M4-DESIGN.md`.
> Constituição: `CLAUDE.md` §5 (multi-tenancy/isolamento físico), §6 (segurança/A1/Key Vault/auditoria), §7 (sem hardcode), §8 (integrações gov), §11 (Polly/OTel), §16 (pesquisar fonte; não inventar).
> REGRA DE OURO: toda afirmação factual tem **FONTE (URL/resolução)** ou está marcada `[a confirmar — <doc oficial>]`.
> Fontes verificadas reusadas: `m4-prep/verificacao-assinatura-a1-icp.md` (placar 14 CONFIRMADO) e `m4-prep/pesquisa-assinatura-a1-icp.md`.
> Data: 2026-06-22.

---

## 0. Resumo executivo (o que este doc decide)

Este design detalha o **cofre de certificados A1** e o **serviço de assinatura** transversal (`IServicoAssinaturaDigital`), reconciliando o pedido do dono ("A1 cifrado no **banco dedicado** do tenant") com a Constituição §6 ("segredos só no **Key Vault**") via **envelope encryption**: o `.pfx` e sua senha ficam cifrados em **AES-256-GCM no banco dedicado do tenant** (§5: database-per-tenant já existe — `ITenantConnectionResolver`), e a **chave mestra (DEK-wrapping key) vive no Azure Key Vault** (em DEV/local, uma chave protegida fora do repo). Assim nenhum material em claro toca repo, disco ou banco. O serviço **carrega o A1 só-em-memória** (`EphemeralKeySet`), assina **XML-DSig Enveloped/RSA-SHA256/C14N inclusiva** (eSocial — CONFIRMADO no manual) ou **CMS/PKCS#7 (CAdES)** para binários/.TXT (se exigido), **nunca exporta a chave privada**, valida a **cadeia ICP-Brasil** e **registra trilha de auditoria imutável de cada uso** (quem assinou o quê, quando, com qual certificado/titular, IP). Acesso é **RBAC negar-por-padrão** (`admin.certificado.gerenciar` para custódia; `documentos.assinar` para assinar) e todo I/O externo fica atrás de **ACL + Polly + OTel**.

**Caminho deste arquivo:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/architecture/m4-prep/A1-DESIGN.md`

---

## 1. Reconciliação "banco dedicado cifrado" × Constituição §6 (Key Vault)

O enunciado pede **custódia no banco dedicado do tenant**; a §6 manda **Key Vault**. O `M4-DESIGN.md` §3.2 já antecipou a divergência. **Resolução de design (envelope encryption / DEK-wrapping):**

- **Camada de dados (no banco DEDICADO do tenant):** guarda o `.pfx` e a senha **cifrados** com uma **DEK (Data Encryption Key)** AES-256 por registro; a DEK por sua vez é **embrulhada (wrapped)** pela **chave mestra do Key Vault**. Nada em claro é persistido.
- **Camada de raiz de confiança (Key Vault):** guarda **apenas a chave mestra** (KEK — Key Encryption Key). O Key Vault nunca vê o `.pfx`; só faz wrap/unwrap da DEK (operação `wrapKey/unwrapKey`, a chave nunca sai do HSM). Isso é **fiel à §6** ("segredos só no Key Vault") porque o **segredo que dá acesso** (a KEK) está no Key Vault; o blob no banco é inútil sem ela.
- **Por que no banco dedicado e não como secret direto no Key Vault?** Isolamento físico por tenant (§5: cada ente tem banco próprio, exigência de auditoria/edital) + custo/limite de secrets do Key Vault para muitos tenants + co-localização do material com a trilha de auditoria do tenant. **Trade-off explícito:** mais código de cripto nosso (risco de erro) vs. menos dependência de quota do Key Vault. **`[a confirmar com o dono]`** que esta é a custódia desejada (vs. secret puro no Key Vault).
- **Modo DEV/local:** a KEK vem de um provedor local (ex.: chave em variável de ambiente/arquivo protegido fora do repo, ou Key Vault de dev). **Nunca** chave hardcoded no código (§6). O **algoritmo de envelope é idêntico** em DEV e PROD; só muda a origem da KEK.

> Fonte do conceito de envelope encryption + AES-GCM como AEAD: `System.Security.Cryptography.AesGcm` (.NET 8, cross-platform). `[a confirmar — disponibilidade/known-answer no TFM net8 em container]`.

---

## 2. Modelo do cofre (entidade + tabela no banco dedicado do tenant)

Entidade no módulo de custódia (sugestão: `Identidade`/`Plataforma` Infrastructure, transversal — **não** dentro de Transparencia, pois é usado por Protocolo, Saude, eSocial/M5). Implementa `IMustHaveTenant` (§5).

```
CertificadoA1Cofre : IMustHaveTenant
  Guid     Id
  Guid     TenantId                 // carimbado pelo TenantSaveChangesInterceptor; cross-tenant lança (§5)
  string   Titular                  // CN/Razão Social do e-CNPJ (NÃO sigiloso)
  string   CnpjTitular              // VO Cnpj (SharedKernel)
  string   Thumbprint               // SHA-256 do cert (identificação, não sigiloso)
  DateTime NotBeforeUtc / NotAfterUtc
  string   Serie                    // "A1"
  // --- material cifrado (envelope) ---
  byte[]   PfxCipher                // .pfx cifrado AES-256-GCM
  byte[]   PfxNonce  (12 bytes)     // nonce GCM único por cifragem
  byte[]   PfxTag    (16 bytes)     // tag de autenticação GCM
  byte[]   SenhaCipher / SenhaNonce / SenhaTag   // senha do .pfx, mesmo esquema
  byte[]   DekWrapped               // DEK embrulhada pela KEK do Key Vault
  string   KekKeyId                 // identificador/versão da chave mestra no Key Vault (para rotação)
  // --- ciclo de vida ---
  CertificadoStatus Status          // Ativo | Revogado | Expirado | Substituido
  Guid?    CertificadoAnteriorId    // cadeia de rotação
  // auditoria de mutação via AuditSaveChangesInterceptor (antes/depois/quem/quando/IP) — §6
```

**Regras de modelagem:**
- Construtor **privado + factory** `Cadastrar(...)` que valida invariantes (cert válido, NotAfter futuro, série A, CNPJ confere com o tenant). Entidade nasce válida (§7).
- **Apenas UM certificado `Ativo` por (TenantId, finalidade)** por vez (invariante de domínio). Rotação cria novo e marca o anterior `Substituido`.
- **Nunca** expor `PfxCipher`/`SenhaCipher`/`DekWrapped` em DTO/endpoint/log. `ObterInfoCertificadoAsync` devolve só metadados (titular, validade, thumbprint).
- **Global Query Filter por TenantId** aplicado (§5) — nenhuma consulta cross-tenant.

---

## 3. Fluxo de cifragem (cadastro/rotação) e decifragem (assinatura)

### 3.1 Cadastrar / rotacionar (RBAC `admin.certificado.gerenciar`, auditado)
1. Recebe `.pfx` + senha **só em memória** (upload TLS; nunca grava em disco).
2. Carrega para validar: `X509CertificateLoader.LoadPkcs12(pfxBytes, senha, KeyStorageFlags.EphemeralKeySet)` — ctor `new X509Certificate2(bytes,senha)` está **obsoleto (SYSLIB0057)**; net8 → confirmar API exata no TFM. **FONTE:** SYSLIB0057 (CONFIRMADO, verificação §5).
3. Valida: série A, `NotAfter` futuro, CNPJ = tenant, e **cadeia ICP-Brasil** (`X509Chain` com AC-Raiz/intermediárias embarcadas — §5 deste doc).
4. **Gera DEK aleatória** (`RandomNumberGenerator`, 32 bytes). Cifra `.pfx` e senha com **AES-256-GCM** (nonce de 12 bytes **único por cifragem**, tag de 16 bytes).
5. **Wrap da DEK** pela KEK do Key Vault (`KeyClient.WrapKey`, RSA-OAEP — a KEK nunca sai do HSM). Persiste `DekWrapped` + `KekKeyId`.
6. **Zera** (`CryptographicOperations.ZeroMemory`) DEK, senha e bytes do `.pfx` em claro assim que possível.
7. Persiste a entidade no **banco dedicado do tenant** → `AuditSaveChangesInterceptor` grava trilha imutável (quem cadastrou, quando, IP, thumbprint).

### 3.2 Assinar (RBAC `documentos.assinar`, auditado a cada uso)
1. Carrega `CertificadoA1Cofre` `Ativo` do tenant.
2. **Unwrap da DEK** via Key Vault (`KeyClient.UnwrapKey`). 3. Decifra senha e `.pfx` (AES-GCM; a **tag GCM autentica** — adulteração no banco → falha de integridade, não decifra).
4. `X509CertificateLoader.LoadPkcs12(pfx, senha, EphemeralKeySet)` → cert **só em memória**.
5. `cert.GetRSAPrivateKey()` (não `cert.PrivateKey`, obsoleto) **somente durante a operação**.
6. Assina (XML-DSig ou CMS — §4). **Nunca** chama `cert.Export(...)` com chave privada; a chave **não é exportável** e não é persistida fora do GCM.
7. `finally`: `cert.Dispose()` + `CryptographicOperations.ZeroMemory` na DEK/senha/pfx em claro. `EphemeralKeySet` garante que a chave **não vai para o keystore do SO/container**.
8. Grava **trilha de auditoria de uso** (§6 deste doc).

---

## 4. Parâmetros de assinatura por destino

### 4.1 eSocial — XML-DSig Enveloped (**CONFIRMADO linha a linha**, M5)
| Parâmetro | Valor | Fonte |
|---|---|---|
| Padrão | XML Digital Signature, **Enveloped** | Manual Desenvolvedor eSocial v1.15 §6.7.1 — CONFIRMADO |
| Assinatura | RSA-SHA256 `xmldsig-more#rsa-sha256` | §6.7.5 — CONFIRMADO |
| Digest | SHA-256 `xmlenc#sha256` | §6.7.6 — CONFIRMADO |
| Canonicalização | **C14N INCLUSIVA** `REC-xml-c14n-20010315` (**NÃO** exc-c14n) | §6.7.8.2 — CONFIRMADO |
| Transforms | `enveloped-signature` + C14N | §6.7.8 — CONFIRMADO |
| KeyInfo | **EndCertOnly** (só `<X509Certificate>`) | §6.7.3 — CONFIRMADO |
| Certificado | série A (A1/A3); tipo S rejeitado | §6.7.3.1 / §6.8.4 — CONFIRMADO |

FONTE: `manualorientacaodesenvolvedoresocialv1-15.pdf` (gov.br/esocial). **`[a confirmar — cruzar com MOS S-1.3 vigente; só no M5]`** que nenhuma NT da S-1.3 alterou a assinatura.

Implementação: `System.Security.Cryptography.Xml.SignedXml`, `CanonicalizationMethod = REC-xml-c14n-20010315` (**não** `XmlDsigExcC14NTransform` — pegadinha do default .NET, risco ALTO), subclasse `SignedXmlWithId` se houver `Id`. Validar contra **Verificador de Conformidade ITI** + Produção Restrita.

### 4.2 Binário / .TXT (TCE-RS) — CMS/PKCS#7 (CAdES) **só se exigido**
`System.Security.Cryptography.Pkcs.SignedCms` + `CmsSigner(cert)`. **Não usar XML-DSig** em arquivo não-XML. **INCERTO** se o TCE-RS exige assinatura criptográfica do `.TXT` — a evidência conferida aponta que a autenticidade vem do **PAD + ato no e-Protocolo (cert. PESSOAL)**, não de assinatura embutida no arquivo. **`[a confirmar — Manual PAD/Processo Eletrônico TCE-RS vigente]`** antes de habilitar CMS. **FONTE:** verificação §3 (o ResumoLeiaute Vol. V não menciona assinatura ICP no arquivo).

---

## 5. Contrato do serviço + validação de cadeia ICP-Brasil

```
IServicoAssinaturaDigital   // BuildingBlocks (transversal) — atrás de ACL + Polly + OTel
  Task<ResultadoAssinatura> AssinarXmlAsync(XmlDocument doc, OpcoesAssinaturaXml o, CancellationToken ct);
  Task<byte[]>              AssinarCmsAsync(ReadOnlyMemory<byte> conteudo, OpcoesAssinaturaCms o, CancellationToken ct);
  Task<CertificadoInfo>     ObterInfoCertificadoAsync(CancellationToken ct);  // metadados, SEM chave
```
- **Cadeia ICP-Brasil:** validar com `X509Chain` + AC-Raiz/intermediárias **embarcadas** (containers Linux **não** trazem ICP-Brasil no trust do SO). Validar **revogação (CRL/OCSP)** e `KeyUsage` (digitalSignature/nonRepudiation). **`[a confirmar — cadeia AC-Raiz vigente em repositorio.iti.gov.br]`**. **FONTE:** verificação §4 (PLAUSÍVEL-SEM-FONTE, fato operacional).
- Carimbo de tempo (RFC 3161, DOC-ICP-11) **não** exigido pelo eSocial; TCE/SICONFI `[a confirmar]`. `Rfc3161TimestampRequest` — `[a confirmar — paridade Linux net8]`.

---

## 6. RBAC e Auditoria de cada uso

**RBAC (negar-por-padrão, §6) — permissões JÁ existentes no catálogo** (`Modules/Identidade/.../Permissoes.cs`):
- `admin.certificado.gerenciar` → cadastrar/rotacionar/revogar A1 no cofre (endpoint admin).
- `documentos.assinar` / `protocolo.documento.assinar` → invocar a assinatura em ato com efeito legal.
- `financas.empenho.assinar`, `transparencia.remessa.transmitir` → atos específicos. **Reusar; nada novo no catálogo.**

**Auditoria de USO (além do `AuditSaveChangesInterceptor` de mutação):** cada `AssinarXmlAsync/AssinarCmsAsync` grava registro imutável (modelar como `AuditTrail` existente ou tabela dedicada `AssinaturaAuditLog`):
`{ TenantId, UserId (quem), TimestampUtc (quando), Thumbprint+Titular do cert, finalidade/destino (eSocial/TCE/Protocolo), hash SHA-256 do artefato assinado (o quê), CorrelationId, IpAddress, Resultado (Sucesso/Falha+motivo) }`.
- Grava **sucesso E falha** (tentativa de assinar = evento auditável).
- **Nunca** logar `.pfx`, senha, DEK ou chave privada (nem em exceção/stacktrace).
- Trilha **imutável** (§6), destinada ao Tribunal de Contas.

---

## 7. Riscos de segurança (e mitigação)

1. **Cripto caseira mal feita (ALTO):** AES-GCM exige **nonce único por cifragem** — reuso de nonce com a mesma DEK quebra a confidencialidade. Mitigação: nonce aleatório de 12 bytes por operação; nunca reusar; testes known-answer.
2. **Vazamento de KEK / Key Vault mal configurado (ALTO):** se a KEK vaza, todo o cofre cai. Mitigação: KEK só no Key Vault/HSM (operações wrap/unwrap, a chave **não sai**); RBAC do Key Vault mínimo; rotação de KEK (`KekKeyId` versionado).
3. **Chave privada persistida no keystore do SO/container (ALTO):** sem `EphemeralKeySet` o A1 fica no disco do container. Mitigação: **sempre** `EphemeralKeySet`; nunca `.pfx` em disco; `Dispose` + `ZeroMemory`.
4. **Canonicalização errada no eSocial (ALTO):** default .NET é exc-c14n; manual exige **C14N inclusiva** → "Assinatura inválida". Mitigação: fixar `REC-xml-c14n-20010315`; validar no Verificador ITI.
5. **Cadeia ICP-Brasil ausente em Linux (ALTO em PROD):** `X509Chain` falha sem as raízes. Mitigação: embarcar cadeia vigente + CRL/OCSP.
6. **Material em log/exceção/DTO (ALTO):** `.pfx`/senha/DEK em log = vazamento. Mitigação: scrubbing; nunca serializar campos cipher; revisão de logging.
7. **Adulteração do blob no banco (MÉDIO):** mitigado pela **tag GCM** (AEAD) — alteração → decifra falha. Acrescentar AAD = `TenantId||Thumbprint` para amarrar o blob ao tenant.
8. **Cross-tenant / IDOR (ALTO, §5):** assinar com cert de outro tenant. Mitigação: Global Query Filter + `TenantSaveChangesInterceptor`; cert resolvido sempre via `ITenantContext`/`ITenantConnectionResolver`, nunca por id cru do request.
9. **Abuso de privilégio (MÉDIO):** quem tem `documentos.assinar` pode assinar em massa. Mitigação: auditoria de uso (§6) + alerta de volume anômalo; RBAC mínimo.
10. **Confiar A1 server-side para atos que exigem cert PESSOAL (MÉDIO, retrabalho):** RVE/RDI do TCE e homologação SICONFI são assinados com **e-CPF/A3 do responsável**, não pelo A1 institucional. Mitigação: A1 cobre só o que assinamos server-side (eSocial; CMS se confirmado); homologação no portal = **ato humano documentado**.
11. **Versão de leiaute defasada (ALTO):** eSocial Manual≠MOS S-1.3; TCE-RS leiaute 2010. Mitigação: contrato de versão + teste de contrato por destino antes de produção.

---

## 8. O que validar (checklist de aceite)

- [ ] **Round-trip de cifra:** cadastrar → decifrar → assinar produz assinatura válida; blob no banco nunca em claro; nonce único por operação (teste).
- [ ] **`EphemeralKeySet`:** após assinar, nenhuma chave no keystore do SO/container (verificar em container Linux net8).
- [ ] **AAD/tag GCM:** adulterar 1 byte do `PfxCipher` ou trocar `TenantId` → decifra **falha** (não assina).
- [ ] **Isolamento de tenant:** tenant A não assina com cert de B (Global Query Filter + interceptor); tentativa auditada.
- [ ] **eSocial:** XML assinado passa no **Verificador de Conformidade ITI** e na **Produção Restrita**; C14N inclusiva (não exc-c14n); KeyInfo EndCertOnly.
- [ ] **Cadeia ICP-Brasil** valida em container Linux (raízes embarcadas) + revogação CRL/OCSP.
- [ ] **Auditoria de uso** grava sucesso E falha, com hash do artefato, titular, quem/quando/IP; **sem** material sensível em log.
- [ ] **RBAC:** sem `documentos.assinar` → 403 auditado; sem `admin.certificado.gerenciar` → não cadastra/rotaciona.
- [ ] **Polly/OTel:** Key Vault e qualquer HTTP atrás de timeout/retry/circuit breaker, com `TenantId`/`CorrelationId`.
- [ ] **Rotação:** novo cert `Ativo`, anterior `Substituido`; só um `Ativo` por finalidade; KEK rotacionável via `KekKeyId`.
- [ ] **Decisão de custódia confirmada com o dono** (banco cifrado por envelope vs secret puro no Key Vault).

---

## 9. Pendências `[a confirmar — obter doc oficial]`

- [ ] Custódia: **banco dedicado cifrado (envelope)** vs **secret no Key Vault** — decisão do dono.
- [ ] **Cadeia AC-Raiz ICP-Brasil vigente** (`repositorio.iti.gov.br`) para embarcar.
- [ ] **Manual PAD/Processo Eletrônico TCE-RS 2026** — se há assinatura programática (CMS) aceita do `.TXT`/pacote ou só ato interativo.
- [ ] **Manual do Usuário SICONFI — Acesso e Certificação Digital** — se A1 é aceito além de e-CPF A3.
- [ ] **MOS eSocial S-1.3** — cruzar com Manual do Desenvolvedor v1.15 (só no M5).
- [ ] Paridade Linux/net8 de `AesGcm`, `SignedCms`/`CmsSigner`, `Rfc3161Timestamp*`; API exata `X509CertificateLoader` no TFM.
- [ ] Necessidade de **carimbo de tempo** (RFC 3161/DOC-ICP-11) por destino.

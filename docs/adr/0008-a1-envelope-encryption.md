# ADR-0008 — Certificado A1 por envelope encryption (cifrado no banco do tenant + KEK no Key Vault)

- **Status:** Aceito
- **Data:** 2026-06-22
- **Fonte:** `docs/architecture/m4-prep/A1-DESIGN.md`

## Contexto

O certificado **A1 (.pfx, ICP-Brasil)** é necessário para assinar server-side (eSocial,
e — se confirmado — pacotes/anexos TCE), por tenant. Há uma tensão entre dois requisitos da
constituição/dono:

- **`CLAUDE.md` §6:** "segredos só no **Azure Key Vault**".
- **Pedido do dono / ADR-0005:** material do tenant **co-localizado no banco DEDICADO** do
  ente (isolamento físico, custódia junto da trilha de auditoria do tenant).

Guardar N certificados como *secrets* diretos no Key Vault colide com quotas/limites e
dispersa o material do tenant para fora do seu banco; guardar o `.pfx` em claro no banco
violaria §6. É preciso conciliar isolamento físico com "o segredo só no Key Vault".

## Decisão

**Envelope encryption (DEK-wrapping):**

- **No banco dedicado do tenant:** o `.pfx` e a senha ficam cifrados com **AES-256-GCM** (AEAD;
  nonce de 12 bytes **único por cifragem**, tag de 16 bytes), por uma **DEK** (Data Encryption
  Key) aleatória por registro. A DEK é persistida **embrulhada** (`DekWrapped`) + `KekKeyId`.
- **No Key Vault:** vive **apenas a chave mestra (KEK)**; o Key Vault faz `wrapKey`/`unwrapKey`
  (RSA-OAEP) — a KEK **nunca sai do HSM** e o Key Vault **nunca vê o `.pfx`**. Isso é fiel à §6:
  o *segredo que dá acesso* (a KEK) está no Key Vault; o blob no banco é inútil sem ela.
- **Uso (assinatura):** unwrap da DEK → decifra senha/`.pfx` → carrega
  `X509CertificateLoader.LoadPkcs12(..., EphemeralKeySet)` **só em memória** → assina →
  `Dispose` + `CryptographicOperations.ZeroMemory`. A chave privada **nunca** é exportada nem
  persistida fora do GCM, nem vai para o keystore do SO/container.
- **DEV/local:** o algoritmo de envelope é **idêntico**; só muda a origem da KEK (chave protegida
  fora do repo / Key Vault de dev). Nunca chave hardcoded.
- **Auditoria:** cada uso (sucesso E falha) grava trilha imutável (quem, quando, thumbprint/titular,
  destino, hash do artefato, IP). RBAC deny-by-default (`admin.certificado.gerenciar` para custódia,
  `documentos.assinar` para assinar).

## Alternativas consideradas

- **Secret puro no Key Vault (um `.pfx` por tenant como secret):** mais simples, 100% aderente à
  leitura literal da §6, mas quebra a co-localização no banco do tenant, sofre com quota/limite de
  secrets para muitos entes e separa o material da trilha de auditoria do ente. Marcado
  `[a confirmar com o dono]` como alternativa — não é o caminho escolhido.
- **`.pfx` em claro no banco / no disco do container:** viola §6 e deixa a chave privada exposta.
  Rejeitado.
- **Cifra simétrica com chave única da aplicação:** ponto único de falha; comprometer a app
  compromete todos os certificados. Rejeitado em favor de KEK por tenant no HSM.

## Consequências

- ➕ **Isolamento físico** do A1 por tenant + raiz de confiança no HSM; o blob vazado do banco é
  inútil sem a KEK; a tag GCM autentica (adulteração → decifra falha, com AAD = `TenantId||Thumbprint`).
- ➕ Reconcilia §6 (Key Vault) com a custódia no banco dedicado (ADR-0005) sem violar nenhuma.
- ➖ **Mais código de criptografia nosso** = mais superfície de erro (reuso de nonce, AAD, zeroização).
  Mitigado por testes known-answer, *round-trip*, e checklist de aceite do `A1-DESIGN.md`.
- ➖ Dependência operacional do Key Vault para wrap/unwrap em toda assinatura — atrás de Polly/ACL/OTel.
- ➖ **Divergência consciente** da leitura literal de §6: o segredo *de acesso* está no Key Vault,
  mas o *blob cifrado* mora no banco. Esta ADR documenta a reconciliação.
- 🔗 Limite de escopo: o A1 cobre só o que assinamos **server-side**. Atos que exigem certificado
  **pessoal** (e-CPF/A3) — homologação SICONFI, RVE/RDI no e-Protocolo TCE — são **ato humano**
  no portal, não cobertos pelo A1 institucional (ver ADR-0010).

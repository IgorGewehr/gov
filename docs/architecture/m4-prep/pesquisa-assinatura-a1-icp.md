# Pesquisa — Assinatura digital com certificado A1 ICP-Brasil (.pfx) em .NET

> M4-prep · Tensorroot.Gov · Constituição §8 (integrações gov) / §16 (pesquisar fonte oficial, não inventar).
> REGRA DE OURO: toda afirmação factual tem FONTE (URL) ou está marcada `[a confirmar — <doc oficial>]`.
> Data da pesquisa: 2026-06-22.

---

## 0. TL;DR para engenharia

- **Não existe um único "padrão de assinatura" para todas as integrações.** O formato depende do destino:
  - **eSocial** → **XML-DSig (XML Digital Signature), perfil Enveloped**, RSA-SHA256, **canonicalização C14N padrão (não exclusiva)**. CONFIRMADO em fonte oficial (manual gov.br).
  - **TCE-RS SIAPC/PAD** → os arquivos de dados **NÃO são XML**: são **texto posicional ASCII (ISO 8859-1 / Latin-1), arquivos `.TXT`**. A autenticidade é garantida pelo **Programa Autenticador de Dados (PAD)** + **Código da Remessa** e pelo **parecer/manifestação conclusiva do Controle Interno assinado com e-CNPJ ICP-Brasil no Processo Eletrônico**, e **não** por XML-DSig dos arquivos de dados. CONFIRMADO (leiaute oficial TCE-RS) — porém o **mecanismo exato de assinatura/autenticação do pacote** (CMS/PKCS#7? hash CRC? assinatura do upload?) `[a confirmar — MT-ASCE-0105 Volume V (atual) + manual do PAD/Processo Eletrônico TCE-RS]`.
- **Decisão de design provável:** módulo Transparencia precisa de **dois mecanismos**: (a) gerador de **texto posicional + autenticação PAD** para TCE-RS; (b) **assinador XML-DSig Enveloped C14N/RSA-SHA256** para eSocial (M5) e possivelmente SICONFI. Tratar como **estratégias distintas atrás da ACL**, não como um único "assinador".
- **.NET:** `System.Security.Cryptography.Xml.SignedXml` + `X509Certificate2` cobrem XML-DSig. Atenção a 3 armadilhas conhecidas: **canonicalização (C14N vs exc-C14N)**, **namespaces espúrios (`xmlns:xsi`/`xmlns:xsd`) que invalidam a assinatura**, e **resolução de elementos por `Id`** (necessita subclasse de `SignedXml`).

---

## 1. XML-DSig vs CMS/PKCS#7 — quando usar cada um

| Critério | XML-DSig (XML Digital Signature) | CMS / PKCS#7 (CAdES) |
|---|---|---|
| Objeto assinado | Documento/elemento **XML** | Qualquer **binário/arquivo** (inclui .txt, .pdf) |
| Perfis | Enveloped, Enveloping, Detached | Attached / Detached |
| Norma base W3C/IETF | W3C XML-Signature Syntax and Processing | RFC 5652 (CMS) / ETSI CAdES |
| Padrão ICP-Brasil | **XAdES** (perfil avançado do XML-DSig) | **CAdES** (padrão adotado p/ maioria dos documentos) e **PAdES** (PDF) |
| Uso no projeto | **eSocial** (Enveloped) | Candidato p/ assinar **arquivos .TXT do TCE-RS** e PDFs `[a confirmar]` |

- ICP-Brasil define formatos/perfis de assinatura (CAdES, XAdES, PAdES) no **DOC-ICP-15** e complementares. FONTE: gov.br/iti DOC-ICP-15.
  → Para **arquivos não-XML** (caso dos `.TXT` do TCE-RS), o caminho técnico natural é **CMS/PKCS#7 (CAdES)**, NÃO XML-DSig. Mas **só usar se o TCE-RS exigir assinatura criptográfica do arquivo** — o leiaute sugere que a autenticidade vem do PAD + parecer, não de uma assinatura embutida no .TXT. `[a confirmar — manual do PAD/Processo Eletrônico TCE-RS]`.

---

## 2. Requisito CONFIRMADO — eSocial (XML-DSig Enveloped)

Trechos extraídos do **Manual de Orientação do Desenvolvedor do eSocial v1.15 (abr/2025), seção 6.7 "Padrão de assinatura digital"** (FONTE oficial, extração local do PDF gov.br):

- **Padrão:** "XML Digital Signature, utilizando o formato **Enveloped**".
- **Função criptográfica assimétrica:** RSA — `http://www.w3.org/2001/04/xmldsig-more#rsa-sha256`.
- **Hash / DigestMethod:** `http://www.w3.org/2001/04/xmlenc#sha256`.
- **CanonicalizationMethod:** `http://www.w3.org/TR/2001/REC-xml-c14n-20010315` → **C14N "inclusiva" (padrão), NÃO a Exclusive C14N** (`exc-c14n`). Esta é a pegadinha mais comum em .NET (default de exemplos é `XmlDsigExcC14NTransform`).
- **Transformações exigidas (2):**
  1. `http://www.w3.org/2000/09/xmldsig#enveloped-signature`
  2. `http://www.w3.org/TR/2001/REC-xml-c14n-20010315` (C14N)
- **Cadeia de certificação:** **EndCertOnly** — "Incluir na assinatura apenas o certificado do" signatário; KeyInfo/X509Data deve conter **apenas a tag `<X509Certificate>`** (não a cadeia inteira).
- **Tipo de certificado:** série **A** (assinatura) X.509 ICP-Brasil; A1 é aceito.
- **Declaração de namespace:** "A declaração do namespace da assinatura digital deverá ser realizada na própria tag" — root deve conter **apenas o namespace do eSocial** (remover `xmlns:xsi`/`xmlns:xsd`, senão "Assinatura do evento inválida").

FONTE: <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-15.pdf> (seção 6.7/6.8) · Doc técnica: <https://www.gov.br/esocial/pt-br/documentacao-tecnica> · Orientações: <http://portal.esocial.gov.br/manuais/orientacoes-assinatura-digital-e-procuracao-eletronica>
> Nota de versão: a versão de leiaute vigente é **S-1.3** (Portaria Conjunta RFB/MPS/MTE nº 13/2024). Confirmar a versão do manual do desenvolvedor alinhada ao S-1.3 antes do M5. `[a confirmar — versão vigente do Manual do Desenvolvedor no M5]`.

---

## 3. Requisito — TCE-RS SIAPC/PAD (texto posicional, NÃO XML)

Trechos do **leiaute oficial TCE-RS (ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0 / MT-ASCE-0105 Vol. V)** (extração local do PDF):

- "Os Arquivos deverão ser gerados em **modo texto, no padrão ASCII – ISO 8859-1 (Latin-1)**" — **vedados** binários/zonados/float. → **arquivos `.TXT` posicionais**, não XML.
- Geração **conjunta de TODOS os arquivos** do sistema SIAPC/PAD; cada cabeçalho leva **Código da Remessa** (número exclusivo, "uso semelhante ao Código de Barras"), CNPJ e nome do órgão, periodicidade e datas.
- Entrega: o controle externo é **eletrônico via Processo Eletrônico** desde 2015; remessas mensais até 30 dias após o período (a partir de 2019). FONTE busca oficial TCE-RS.
- **Assinatura digital ICP-Brasil:** exigida para o **Controle Interno realizar a operação de "parecer conclusivo / manifestação conclusiva"** que disponibiliza a remessa ao TCE — i.e., assina-se o **ato no Processo Eletrônico**, não necessariamente cada arquivo `.TXT`.

FONTES:
- Leiaute Vol. V (resumo): <http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf>
- Manual Vol. V completo: <https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf>
- Arquivos à disposição (4320): <http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf>
- FAQ SIAPC: <http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf>
- IN TCE-RS nº 6/2019 e nº 13/2021 (RREO/RGF); Resolução 1134/2020 (contas anuais — formato eletrônico).
- Portal sistemas controle externo: <https://portalnovo.tce.rs.gov.br/sistemas-de-controle-externo/>

**Pendências TCE-RS (bloqueiam implementação real do M4):**
- `[a confirmar — MT-ASCE-0105 Volume V vigente]` o **leiaute posicional campo-a-campo** de cada arquivo .TXT (BAL_VER.TXT, BAL_REC.TXT, UNIORCAM.TXT etc.).
- `[a confirmar — manual do PAD]` como o **PAD autentica** o pacote: há **hash/CRC/assinatura** embutida? O "Código da Remessa" é gerado por algoritmo do PAD?
- `[a confirmar — manual Processo Eletrônico TCE-RS]` se há **API/webservice** de envio ou apenas **upload manual**; e se a assinatura do parecer é **CAdES/PKCS#7** sobre o pacote.
- `[a confirmar]` se o TCE-RS exige **assinatura criptográfica do arquivo de remessa** em si (se sim → CMS/PKCS#7/CAdES, não XML-DSig).

---

## 4. Implementação em .NET

### 4.1 Carregar o A1 (.pfx) — `X509Certificate2`
- Carregar de **stream/bytes vindos do Azure Key Vault** (Constituição §5: certificado NUNCA no repo; segredos só no Key Vault). NÃO ler .pfx do disco em produção.
- Em .NET moderno usar `X509CertificateLoader.LoadPkcs12(...)` (substitui o construtor `new X509Certificate2(bytes, senha)` obsoleto em .NET 9+). `[a confirmar — API alvo conforme TFM do projeto]`.
- Obter a chave: `cert.GetRSAPrivateKey()` (não usar `cert.PrivateKey`, obsoleto). Flags: em Linux/containers o A1 vem como `EphemeralKeySet`/`Exportable` conforme necessidade; em Windows pode exigir `MachineKeySet`.

### 4.2 Assinar XML (eSocial) — `System.Security.Cryptography.Xml.SignedXml`
Pacote NuGet: `System.Security.Cryptography.Xml`. Configuração ALINHADA ao eSocial (§2):

```csharp
var signedXml = new SignedXml(doc.DocumentElement!)
{
    SigningKey = cert.GetRSAPrivateKey()
};
signedXml.SignedInfo.SignatureMethod      = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
// eSocial exige C14N INCLUSIVA (não exc-c14n):
signedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

var reference = new Reference { Uri = "" };               // ou "#<Id>" p/ elemento específico
reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
reference.AddTransform(new XmlDsigC14NTransform());        // C14N, NÃO XmlDsigExcC14NTransform
signedXml.AddReference(reference);

var keyInfo = new KeyInfo();
keyInfo.AddClause(new KeyInfoX509Data(cert));              // EndCertOnly: só o cert do signatário
signedXml.KeyInfo = keyInfo;

signedXml.ComputeSignature();
doc.DocumentElement!.AppendChild(doc.ImportNode(signedXml.GetXml(), true));
```

- **rsa-sha256 não vem registrado por padrão** em runtimes antigos → pode ser necessário `CryptoConfig.AddAlgorithm(...)` / `RSAPKCS1SHA256SignatureDescription`. Em .NET (Core) atual a URI já é reconhecida. `[a confirmar — comportamento no TFM do projeto]`.
- **Elementos com `Id`** (URI `#id`): `SignedXml` por padrão não resolve atributos `Id` arbitrários → subclassar e sobrescrever `GetIdElement`:
```csharp
public sealed class SignedXmlWithId : SignedXml {
    public SignedXmlWithId(XmlDocument d) : base(d) { }
    public override XmlElement? GetIdElement(XmlDocument doc, string id) =>
        base.GetIdElement(doc, id) ?? doc.SelectSingleNode($"//*[@Id='{id}']") as XmlElement;
}
```

### 4.3 Assinar arquivo não-XML (TCE-RS .TXT, se exigido) — CMS/PKCS#7
- `System.Security.Cryptography.Pkcs.SignedCms` com `CmsSigner(cert)` → produz **PKCS#7/CAdES** (attached ou detached). É o caminho correto para `.TXT`, **não** XML-DSig.
- **Só implementar após confirmar** que o TCE-RS exige assinatura do arquivo (ver §3 pendências). Hoje a evidência aponta para autenticação via PAD + parecer no Processo Eletrônico.

### 4.4 Validação de cadeia ICP-Brasil — `X509Chain`
- Validar com `X509Chain` carregando as **ACs intermediárias e a AC-Raiz ICP-Brasil** (cadeia v1..v10/v11) no trust store ou via `chain.ChainPolicy.ExtraStore`.
- Em Linux/container as raízes ICP-Brasil **não estão no trust do SO** → embarcar/instalar a cadeia. `[a confirmar — cadeia AC-Raiz vigente em <repositorio.iti.gov.br>]`.
- Validar **revogação (CRL/OCSP)** e o **uso de chave** (digitalSignature/nonRepudiation). FONTE leiaute certificados RFB: <http://icp-brasil.certisign.com.br/repositorio/ac-certisign-rfb/pdf/Leiaute-dos-certificados-digitais-RFB-v4.3.pdf>

---

## 5. Carimbo de tempo (timestamp)
- **Carimbo do Tempo ICP-Brasil** = selo de **ACT credenciada**, protocolo **RFC 3161** (TSQ/TSR), vinculando o documento a data/hora oficial (Observatório Nacional). Regido por **DOC-ICP-11/12/13/14**; formatos avançados (CAdES/XAdES/PAdES com timestamp) no **DOC-ICP-15**.
- **eSocial:** o manual NÃO exige carimbo de tempo embutido na assinatura do evento (a tempestividade é do recibo/processamento). `[a confirmar — ausência de requisito de TS no Manual do Desenvolvedor S-1.3]`.
- **TCE-RS:** `[a confirmar]` se exige carimbo de tempo no parecer/remessa.
- Implementação .NET: `Rfc3161TimestampRequest` / `Rfc3161TimestampToken` (namespace `System.Security.Cryptography.Pkcs`) para anexar timestamp a um `SignedCms`. Requer **endpoint de ACT contratada** (custo/segredo no Key Vault).

FONTES: DOC-ICP-12 <https://www.gov.br/iti/pt-br/assuntos/legislacao/documentos-principais/doc-icp-12versao-1-3.pdf> · DOC-ICP-15-01 <https://www.gov.br/iti/pt-br/central-de-conteudo/doc-icp-15-01-v-1-0-pdf> · Validador ITI <https://validar.iti.gov.br/> · Repositório ITI <https://repositorio.iti.gov.br/>

---

## 6. RISCOS (algoritmos, canonicalização, operação)

1. **Canonicalização errada (risco ALTO):** usar `exc-c14n` onde o eSocial exige **C14N inclusiva** → "Assinatura inválida". Default de muitos exemplos .NET é o errado. Testar contra o **Validador ITI** e ambiente de Produção Restrita.
2. **Namespaces espúrios (risco ALTO):** `xmlns:xsi`/`xmlns:xsd` no root quebram a assinatura do eSocial. Serializar o XML **sem** esses namespaces.
3. **Algoritmo legado SHA-1 (risco):** garantir **RSA-SHA256** ponta a ponta; SHA-1 está obsoleto e é rejeitado.
4. **rsa-sha256 não registrado no runtime (risco):** em runtimes antigos exige registro via `CryptoConfig`. Validar no TFM alvo.
5. **Resolução de `Id` / múltiplas assinaturas (risco):** `SignedXml` sem `GetIdElement` customizado assina o elemento errado; aceitar **só 1** `<Signature>` na validação (CVE-style XML signature wrapping).
6. **Cadeia ICP-Brasil ausente em Linux/container (risco ALTO em produção):** sem as raízes ICP-Brasil instaladas a validação `X509Chain` falha; embarcar cadeia + CRL/OCSP.
7. **Chave privada A1 em container (risco):** flags `KeyStorageFlags` incorretas → "Keyset does not exist". Carregar do **Key Vault** com flags adequadas; nunca persistir .pfx em disco (§5).
8. **Confundir XML-DSig com o fluxo TCE-RS (risco de retrabalho):** TCE-RS é texto posicional + PAD; assinar XML aqui é caminho errado. Confirmar leiaute/protocolo antes de codar (§16).
9. **Versão de leiaute defasada (risco):** eSocial S-1.3 e leiaute TCE-RS evoluem; fixar versão e ter teste de contrato.

---

## 7. Pendências consolidadas `[a confirmar — obter doc oficial]`
- [ ] Leiaute posicional campo-a-campo TCE-RS — **MT-ASCE-0105 Volume V (versão vigente)**.
- [ ] Mecanismo de autenticação/assinatura do PAD e do **Processo Eletrônico TCE-RS** (hash? CAdES? upload? webservice/API?).
- [ ] Se TCE-RS exige **assinatura criptográfica do arquivo de remessa** (→ define CMS vs nada).
- [ ] Versão do **Manual do Desenvolvedor eSocial** alinhada a **S-1.3** e eventual requisito de carimbo de tempo.
- [ ] **Cadeia AC-Raiz ICP-Brasil vigente** (v parente) p/ embarcar em container — repositório ITI.
- [ ] Política/perfil de assinatura aplicável (**XAdES/CAdES** + nível com/sem TS) por destino.

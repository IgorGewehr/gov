# Verificação adversarial — Assinatura digital A1 ICP-Brasil (.pfx) em .NET

> M4-prep · Tensorroot.Gov · Constituição §8/§16.
> Papel: CÉTICO/AUDITOR DE FATOS. Cada afirmação de `pesquisa-assinatura-a1-icp.md` foi checada
> contra fonte oficial e classificada: **CONFIRMADO** (URL oficial) · **PLAUSÍVEL-SEM-FONTE** · **INCERTO/CONTRADITÓRIO**.
> Data da verificação: 2026-06-22.

---

## Placar

- **CONFIRMADO: 14** · **PLAUSÍVEL-SEM-FONTE: 4** · **INCERTO/CONTRADITÓRIO: 5**

A pesquisa é, no geral, **honesta e bem marcada** (usa `[a confirmar]` nos pontos certos). Os
problemas estão concentrados em **TCE-RS**, onde a pesquisa faz uma afirmação factual sobre o
mecanismo de autenticação que a fonte citada NÃO sustenta, e em um detalhe da regra de namespace
do eSocial que foi superinterpretado.

---

## 1. eSocial — XML-DSig (verificado contra o PDF oficial v1.15, seção 6.7/6.8)

Fonte primária extraída e lida localmente (pdftotext) do PDF oficial gov.br:
<https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-15.pdf>

| # | Afirmação da pesquisa | Classificação | Evidência (trecho literal do manual) |
|---|---|---|---|
| 1 | Padrão = XML Digital Signature, formato **Enveloped** | **CONFIRMADO** | §6.7.1: "Padrão de assinatura: XML Digital Signature, utilizando o formato Enveloped" |
| 2 | Assimétrica **RSA-SHA256** `xmldsig-more#rsa-sha256` | **CONFIRMADO** | §6.7.5: "Função criptográfica assimétrica: RSA (http://www.w3.org/2001/04/xmldsig-more#rsa-sha256)" |
| 3 | DigestMethod **SHA-256** `xmlenc#sha256` | **CONFIRMADO** | §6.7.6: "Função de message digest: SHA-256. (http://www.w3.org/2001/04/xmlenc#sha256)" |
| 4 | CanonicalizationMethod = **C14N inclusiva** `REC-xml-c14n-20010315`, **não** exc-c14n | **CONFIRMADO** | §6.7.8.2 + exemplo XML: `<CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315"/>`. Nenhuma menção a exc-c14n. A advertência da pesquisa (default .NET é exc-c14n → erro) é correta. |
| 5 | Duas transformações: Enveloped + C14N | **CONFIRMADO** | §6.7.8.1 `#enveloped-signature` e §6.7.8.2 C14N |
| 6 | Cadeia **EndCertOnly**, apenas tag `<X509Certificate>` | **CONFIRMADO** | §6.7.3: "Cadeia de certificação: EndCertOnly (Incluir na assinatura apenas o certificado do usuário final)" + "o arquivo XML assinado deve conter apenas a tag X509Certificate" |
| 7 | Certificado série **A** (A1/A3 aceitos), tipo S rejeitado | **CONFIRMADO** | §6.7.3.1 "Tipo do certificado: A1 ou A3"; §6.8.4 "aceitar certificados somente do tipo A (não serão aceitos certificados do tipo S)" |
| 8 | Namespace da assinatura declarado na própria tag `<Signature>` | **CONFIRMADO** | §6.x: "A declaração do namespace da assinatura digital deverá ser realizada na própria tag `<Signature>`" + "O uso de declaração namespace diferente do padrão estabelecido é vetado" |
| 9 | **"Remover `xmlns:xsi`/`xmlns:xsd` do root senão 'Assinatura do evento inválida'"** | **INCERTO/CONTRADITÓRIO** | O manual NÃO contém essa frase. O exemplo de SOAP no §6.4 do PRÓPRIO manual **usa** `xmlns:xsi` e `xmlns:xsd` — porque pertencem ao envelope SOAP, não ao evento. A regra real do manual é genérica ("namespace diferente do padrão é vetado"). A pegadinha de namespaces espúrios é **conhecida na prática** (fóruns de implementação), mas **não é citação literal do manual**. Rebaixar de "CONFIRMADO" para folclore de implementação a validar contra Produção Restrita. |

**Veredito eSocial:** o núcleo técnico (itens 1–8) é **fiel ao documento oficial**, conferido linha a
linha. Só o item 9 foi superinterpretado.

---

## 2. eSocial — versão de leiaute / manual

| Afirmação | Classificação | Evidência |
|---|---|---|
| Leiaute vigente **S-1.3**, Portaria Conjunta RFB/MPS/MTE nº **13/2024** | **CONFIRMADO** | Notas Técnicas S-1.3 nº 03,04,05/2025 publicadas no portal eSocial; S-1.3 aprovado pela Portaria Conjunta 13/2024. NT oficial: <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/nota-tecnica-s-1-3-04-2025-rev.pdf> |
| A pesquisa cita o **"Manual do Desenvolvedor v1.15 (abr/2025)"** como vigente p/ assinatura | **PLAUSÍVEL-SEM-FONTE** (ressalva importante) | O Manual do **Desenvolvedor** (v1.15) é doc DISTINTO do **MOS S-1.3** (Manual de Orientação do eSocial, que acompanha o leiaute). A seção 6.7 do Manual do Desenvolvedor é historicamente estável (RSA-SHA256 desde a migração SHA1→SHA256, registrada no changelog do próprio v1.15, linhas 45-47). Risco: confirmar que nenhuma NT da S-1.3 alterou a especificação de assinatura antes do M5. `[a confirmar — seção de assinatura no MOS S-1.3 vigente]` |

---

## 3. TCE-RS SIAPC/PAD — texto posicional (verificado contra ResumoLeiaute Vol. V V2.0)

Fonte primária extraída e lida localmente (pdftotext):
<http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf>
(documento datado **Set/2010, Versão 2.0** — ver alerta de vigência abaixo).

| Afirmação | Classificação | Evidência |
|---|---|---|
| Arquivos em **modo texto ASCII ISO 8859-1 (Latin-1)**, vedados binários/zonados/float | **CONFIRMADO** | §2: "Os Arquivos deverão ser gerados em modo texto, no padrão ASCII – ISO 8859-1 (Latin-1), em formato tabular, sequencial... não se aceitando campos compactados (paket decimal), zonados, binários, ponto flutuante (float point)". Linhas terminadas em CR/LF (0D0A). |
| Arquivos são **`.TXT` posicionais, NÃO XML** | **CONFIRMADO** | Idem §2 (tabular/sequencial, registro por linha). |
| Existe **"Código da Remessa"** no cabeçalho, número exclusivo, "uso semelhante ao Código de Barras" | **CONFIRMADO** | Campo "Código da Remessa", Numérico 12, posição 119–130: "obrigatório em todos os Cabeçalhos... número exclusivo de cada remessa, de uso semelhante ao Código de Barras". |
| **"O Código da Remessa pode ser gerado por algoritmo do PAD"** (especulação `[a confirmar]` da pesquisa) | **CONTRADITÓRIO** | O documento diz o OPOSTO: "Deverá ser gerado **pelo próprio Órgão/Entidade**, e será utilizado como identificador exclusivo da remessa". **NÃO é hash/algoritmo do PAD** — é um identificador atribuído pelo ente. Corrigir a pendência da pesquisa: essa parte já está respondida pela fonte. |
| **"Autenticidade garantida por PAD + parecer/manifestação conclusiva do Controle Interno assinado com e-CNPJ ICP-Brasil no Processo Eletrônico"** | **INCERTO/CONTRADITÓRIO** | O **ResumoLeiaute Vol. V** (a fonte citada na §3 da pesquisa) **NÃO menciona** assinatura digital ICP-Brasil, XML-DSig, CMS, hash, CRC, nem "parecer conclusivo via Processo Eletrônico". A única ocorrência de "assinatura" (§6.d) é **campo para aposição de assinaturas do responsável/contabilista/Controle Interno em relatórios** — não assinatura criptográfica. A afirmação da pesquisa é uma **inferência plausível**, mas está apresentada como se a fonte a sustentasse. Não implementar sobre ela. |
| Entrega via **Processo Eletrônico desde 2015**, remessas mensais até 30 dias (a partir de 2019) | **PLAUSÍVEL-SEM-FONTE** | Não verificável no ResumoLeiaute (Set/2010). Coerente com a evolução do TCE-RS, mas exige IN/Resolução vigente como fonte. `[a confirmar — IN TCE-RS nº 6/2019, nº 13/2021, Resolução 1134/2020]` |

**ALERTA DE VIGÊNCIA (risco alto):** o ResumoLeiaute conferido é de **Set/2010 (V2.0)**. A própria
pesquisa pede `[a confirmar — MT-ASCE-0105 Volume V vigente]`. **Toda a estrutura campo-a-campo
(BAL_VER.TXT, BAL_REC.TXT, UNIORCAM.TXT etc.) deve ser obtida da versão vigente antes de codar** —
o doc de 2010 serve para confirmar o FORMATO (ASCII posicional) mas NÃO os campos atuais.

---

## 4. ICP-Brasil — formatos, cadeia e carimbo de tempo

| Afirmação | Classificação | Evidência |
|---|---|---|
| ICP-Brasil define CAdES/XAdES/PAdES no **DOC-ICP-15** (+ complementares) | **CONFIRMADO** | DOC-ICP-15: "Todos os formatos e perfis... definidos no conjunto DOC-ICP-15". CAdES=ext. CMS; XAdES=ext. XML-DSig; PAdES=ext. PDF. <https://www.gov.br/iti/pt-br/central-de-conteudo/doc-icp-15-v-1-0-pdf> |
| Para **arquivos não-XML** o caminho é **CMS/PKCS#7 (CAdES)**, não XML-DSig | **CONFIRMADO** (técnico) | Decorre direto da definição: CAdES assina binário/arquivo qualquer; XAdES é só p/ XML. Coerente com DOC-ICP-15. |
| Carimbo de tempo = ACT credenciada, **RFC 3161**; modelo regido por **DOC-ICP-11**; requisitos de ACT em **DOC-ICP-12** | **CONFIRMADO** (com correção menor) | DOC-ICP-15: "ICP-Brasil define no documento **DOC-ICP-11** o modelo de carimbo do tempo". DOC-ICP-12 = requisitos das DPC das ACTs. A pesquisa escreveu "DOC-ICP-11/12/13/14" genérico — o **modelo** está especificamente no **DOC-ICP-11**; citar DOC-ICP-11 como principal. |
| Validar cadeia AC-Raiz ICP-Brasil; em Linux/container as raízes **não estão no trust do SO** | **PLAUSÍVEL-SEM-FONTE** | Verdadeiro na prática (containers Linux não trazem AC-Raiz ICP-Brasil no store padrão), mas é fato operacional, não citação. Embarcar cadeia + CRL/OCSP. `[a confirmar — cadeia vigente em repositorio.iti.gov.br]` |

---

## 5. Implementação .NET (APIs)

| Afirmação | Classificação | Evidência |
|---|---|---|
| `X509CertificateLoader.LoadPkcs12(...)` substitui `new X509Certificate2(bytes, senha)` (obsoleto .NET 9+) | **CONFIRMADO** | SYSLIB0057: ctors de byte[]/string de X509Certificate2 obsoletos a partir do .NET 9; usar X509CertificateLoader. <https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0057> |
| `SignedXml` precisa de subclasse sobrescrevendo `GetIdElement` p/ resolver `Id` arbitrário | **CONFIRMADO** | Microsoft Learn: `GetIdElement` é `virtual`, projetado para override quando se usa atributo diferente de `Id`. <https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-security-cryptography-xml-signedxml> |
| `SignedCms`/`CmsSigner` → PKCS#7/CAdES; `Rfc3161TimestampRequest`/`Token` p/ timestamp | **CONFIRMADO** (com ressalva de plataforma) | Existem em `System.Security.Cryptography.Pkcs`. Ressalva: parte da doc lista essas APIs sob `windowsdesktop`; **confirmar disponibilidade cross-platform no TFM net8 do projeto** antes de assumir paridade Linux. `[a confirmar — Rfc3161* no net8 Linux]` |
| `cert.GetRSAPrivateKey()` em vez de `cert.PrivateKey`; flags KeyStorageFlags em container | **PLAUSÍVEL-SEM-FONTE** | Boas práticas corretas e amplamente documentadas; não conferidas contra página específica nesta verificação. |
| rsa-sha256 pode exigir `CryptoConfig.AddAlgorithm` em runtimes antigos | **PLAUSÍVEL-SEM-FONTE** | Verdadeiro p/ .NET Framework antigo; em .NET 8 a URI é reconhecida. A pesquisa já marca `[a confirmar — TFM]`. OK. |

---

## 6. O que NÃO se deve implementar sem o documento oficial vigente

1. **NÃO** implementar qualquer geração de arquivo TCE-RS campo-a-campo com base no ResumoLeiaute de **2010**. Obter **MT-ASCE-0105 Vol. V vigente** (+ tabelas BAL_VER/BAL_REC/UNIORCAM atuais).
2. **NÃO** assumir que o TCE-RS exige assinatura ICP-Brasil/CAdES sobre o `.TXT` nem que a autenticação é "PAD + parecer no Processo Eletrônico" — **nenhuma fonte oficial conferida sustenta isso**. Obter o **manual do PAD e do Processo Eletrônico TCE-RS vigente** antes de escolher CMS vs upload simples.
3. **NÃO** codar a regra "remover `xmlns:xsi`/`xmlns:xsd` do root" como requisito do manual eSocial — ela não está no texto. Validar empiricamente contra **Produção Restrita** + **Verificador de Conformidade ITI** (<https://app-verificador.iti.gov.br/>).
4. **NÃO** fixar a especificação de assinatura do eSocial só pelo Manual do Desenvolvedor v1.15 sem cruzar com o **MOS S-1.3** vigente (doc distinto).
5. **NÃO** assumir paridade Linux das APIs `Rfc3161Timestamp*`/`SignedCms` sem testar no TFM net8 em container.

---

## 7. Os 3 maiores riscos

1. **TCE-RS apresentado com falsa ancoragem (risco de retrabalho ALTO).** A §3 da pesquisa afirma o mecanismo de autenticação (PAD + parecer ICP-Brasil no Processo Eletrônico) como se viesse do leiaute citado, mas o leiaute **não diz isso** e o "Código da Remessa" é gerado **pelo ente**, não pelo PAD. Construir o M4 sobre essa premissa não verificada pode exigir reimplementação do fluxo de entrega. Mitigação: obter manual do PAD/Processo Eletrônico vigente ANTES de desenhar a ACL de envio.

2. **Versão de leiaute defasada — TCE-RS 2010 e eSocial Manual≠MOS (risco ALTO).** O único leiaute TCE-RS conferível é de Set/2010; os campos reais podem ter mudado. No eSocial, a fonte é o Manual do Desenvolvedor, não o MOS S-1.3 vigente. Codar contra versão errada = remessa rejeitada em produção. Mitigação: contrato de versão + teste de contrato por destino.

3. **Canonicalização/namespace do eSocial (risco ALTO, mas bem mapeado).** C14N inclusiva (não exc-c14n) está CONFIRMADO no manual; o erro de namespace espúrio é real na prática mas NÃO é citação literal. Risco de "Assinatura inválida" se o default .NET (exc-c14n) for usado. Mitigação: testes contra Verificador ITI e Produção Restrita; tratar namespace como hipótese a validar, não como regra documentada.

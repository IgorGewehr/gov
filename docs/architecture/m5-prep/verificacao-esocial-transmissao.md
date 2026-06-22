# Verificação (auditoria de fatos) — eSocial: Transmissão de Eventos (M5 RH)

> Papel: **cético / auditor de fatos**. Cada afirmação do doc `pesquisa-esocial-transmissao.md` foi
> checada contra fonte oficial vigente (gov.br/eSocial). Onde possível, extraí o texto direto do PDF
> oficial baixado localmente (MOS Desenvolvedor **v1.10**, via `pdftotext`).
> Classificação: **CONFIRMADO** (URL/citação) · **PLAUSÍVEL-SEM-FONTE** · **INCERTO/REFUTADO**.
> Data da verificação: 2026-06-22.

## Nota metodológica importante (versão das fontes)

- A pesquisa cita o MOS Desenvolvedor **v1.0 / v1.9 / v1.10**. A versão **mais recente publicada é
  v1.15 (abril/2025)**. As URIs de assinatura, SOAP e estrutura de lote conferidas abaixo vêm da
  **v1.10**; são estáveis há várias versões, mas **antes de codar o assinador/cliente WS, confirmar
  na v1.15** se houve alteração de binding/URLs.
  FONTE (lista de manuais): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais> ·
  v1.15: <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-15.pdf>
- Os **schemas dos eventos (S-1200, S-2200 etc.)** NÃO estão no MOS Desenvolvedor; estão nos
  **Leiautes S-1.3**, cuja versão vigente é consolidada até **NT 06/2026 (rev. 09/04/2026)** — e
  não no "MOS S-1.3 cons. até NO S-1.3.07/2026" citado na pesquisa. São documentos distintos.
  FONTE (leiautes vigentes): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html>

---

## §1 Ambientes e URLs

| Afirmação da pesquisa | Veredito | Evidência |
|---|---|---|
| Dois ambientes: Produção e Produção Restrita | **CONFIRMADO** | Página oficial de Produção Restrita. |
| Produção Restrita sem efeito jurídico, máx. 1.000 vínculos/empregador, só testes funcionais | **CONFIRMADO** | "tests without any legal effect" + "limits 1,000 employment relationships per employer". <https://www.gov.br/esocial/pt-br/acesso-ao-sistema/ambiente-de-producao-restrita> |
| Novas URLs de produção (notícia de migração); restrita não mudou | **CONFIRMADO** | Notícia oficial; uso obrigatório a partir de 08/jan. <https://www.gov.br/esocial/pt-br/noticias/divulgadas-novas-url-para-transmissao-dos-dados-de-producao-do-esocial> |
| URL Prod enviar lote `webservices.envio.esocial.gov.br/.../WsEnviarLoteEventos.svc` | **CONFIRMADO** | Idem notícia + MOS v1.10 linha 1107. |
| URL Prod consultar `webservices.consulta.esocial.gov.br/.../WsConsultarLoteEventos.svc` | **CONFIRMADO** | Idem notícia + MOS v1.10 linhas 1634-1636. |
| URLs de Produção Restrita (envio/consulta) | **CONFIRMADO** | MOS v1.10 linhas 1639-1645 + notícia. |
| BX/Download e consulta de identificadores `[a confirmar]` | **CONFIRMADO (resolvido)** | URLs reais extraídas do MOS v1.10 — ver §5 abaixo. A pesquisa marcou como pendente; **agora confirmado**. |

---

## §2 Protocolo Web Service (SOAP)

| Afirmação | Veredito | Evidência |
|---|---|---|
| SOAP 1.1, troca XML padrão Document/Literal, sem REST (.svc/WCF) | **CONFIRMADO** | MOS v1.10 l.664-665: "realizada no padrão SOAP versão 1.1, com troca de mensagens XML no padrão Style/Encoding: Document/Literal"; quadro-resumo l.893/897. |
| Acesso WS sem interface de navegador | **CONFIRMADO** | Notícia de URLs: "there is no web page with visual interface". |
| Binding exato (transport vs message security) sob S-1.3 `[a confirmar]` | **PLAUSÍVEL-SEM-FONTE** | TLS no transporte confirmado (l.17 "protocolo de segurança da camada de transporte"; l.653-655 mTLS cliente/servidor). O detalhe de WS-Security não foi extraído — **confirmar na v1.15**. |

> ⚠️ Discrepância menor: o exemplo de envelope SOAP no PDF usa namespace `http://www.w3.org/2003/05/soap-envelope` (que é SOAP **1.2**), mas o texto normativo afirma **SOAP 1.1** e o changelog (l.20) registra "Alteração da versão do SOAP de 1.2 para 1.1". **Vale SOAP 1.1** (texto normativo > exemplo). Confirmar WSDL real.

---

## §3 Lote vs individual

| Afirmação | Veredito | Evidência |
|---|---|---|
| Envio sempre em lote (`enviarLoteEventos`), mesmo 1 evento; processamento assíncrono | **CONFIRMADO** | MOS v1.10 l.399-424: lotes encapsulam conjunto de eventos; modelo assíncrono via dois WS. |
| Validação 2 níveis: N1 na recepção (certificado conexão + estrutura) → protocolo; N2 assíncrono por evento → recibo | **CONFIRMADO** | MOS v1.10 l.435-449: "Validação Nível 1 ... no momento da recepção ... certificado da conexão"; "Validação Nível 2 ... segundo momento ... modelo assíncrono". |
| Elemento raiz `eSocial/envioLoteEventos` `[a confirmar]` | **REFUTADO (corrigir)** | O raiz é `<eSocial xmlns="http://www.esocial.gov.br/schema/lote/eventos/envio/vx_x_x">` (MOS v1.10 l.600). **Não** é `envioLoteEventos`. Nome de método WS: `EnviarLoteEventos` (l.1093). Schema do lote: `loteEventos-vx_x_x.xsd` (l.1004). |
| Limite máx. **50 eventos/lote** `[a confirmar]` | **REFUTADO / não existe** | **Não há limite por contagem de eventos** no MOS v1.10. O limite real é de **tamanho**: "O tamanho limite da mensagem SOAP é **750 kbytes**" (l.1569-1571; erro 612 "A solicitação ultrapassou o tamanho limite"). **NÃO implementar "50/lote" — implementar corte por 750 KB.** Confirmar 750 KB na v1.15. |

---

## §4 Assinatura digital (XML-DSig) — **núcleo do M5/A1**

Todos os valores que a pesquisa marcou `[a confirmar]` foram **extraídos do MOS v1.10** (bloco
SignedInfo l.793-820). **CONFIRMADOS:**

| Item | Valor oficial (MOS v1.10) | Veredito |
|---|---|---|
| Padrão | XML Digital Signature, **Enveloped** (`xmldsig-core`) | **CONFIRMADO** (l.742-743) |
| CanonicalizationMethod | `http://www.w3.org/TR/2001/REC-xml-c14n-20010315` | **CONFIRMADO** (l.797) |
| SignatureMethod | `http://www.w3.org/2001/04/xmldsig-more#rsa-sha256` | **CONFIRMADO** (l.799-800) |
| DigestMethod | `http://www.w3.org/2001/04/xmlenc#sha256` | **CONFIRMADO** (l.808) |
| Transforms | enveloped-signature (`#enveloped-signature`) + C14N (`REC-xml-c14n-20010315`) | **CONFIRMADO** (l.803-805) |
| **Reference URI** | **`URI=""`** (assinatura sobre o documento inteiro) | **CONFIRMADO / CORRIGE a pesquisa** (l.801) |
| KeyInfo | `<X509Data><X509Certificate>` apenas (sem rep. individualizada extra) | **CONFIRMADO** (l.784-820) |
| Codificação | Base64 | **CONFIRMADO** (l.770) |

> ⚠️ **Correção importante para o assinador:** a pesquisa (§4) supôs "Reference URI apontando para o
> `Id` do evento (`evtX` com atributo Id)". O **MOS v1.10 usa `Reference URI=""`** (enveloped sobre o
> documento todo), **não** uma referência ao `Id`. O atributo `Id` do evento existe e é usado como
> **identificador de negócio** (consulta/download — §5), **não** como alvo da Reference da assinatura.
> Implementar com `URI=""` + transform enveloped + C14N. **Reconfirmar na v1.15 / no XSD vigente.**

| Afirmação (certificado) | Veredito | Evidência |
|---|---|---|
| ICP-Brasil **série A, tipo A1 ou A3** | **CONFIRMADO** | MOS v1.10 l.694-708: "emitido por AC credenciada ICP-Brasil ... pertencer à série A ... tipo A1 ou A3". |
| Dois usos: certificado de **conexão** (transporte) e de **assinatura** do XML | **CONFIRMADO** | MOS v1.10 l.718-725: "certificados digitais serão exigidos em dois momentos distintos". Podem ser o mesmo A1 (não há vedação a usá-los iguais; restrição a certificados distintos: **não localizada** → PLAUSÍVEL-SEM-FONTE). |
| Obrigatório assinar todos os eventos (inclusão/alteração/retificação/exclusão) | **CONFIRMADO (com ressalva)** | Manual Web Geral: usuários de **certificado** assinam todas as edições; via **login gov.br** (ouro/prata) a assinatura é automática. Para o M5 (A1 server-side) → assinar tudo. |
| EFR obrigatório para ente público; estab. com raiz CNPJ diferente no mesmo EFR | **CONFIRMADO** | Manual Web Geral: "É obrigatório o preenchimento do campo do CNPJ do Ente Federado Responsável - EFR" para naturezas jurídicas **101-5, 104-0, 107-4, 116-3, 134-1**; coerência com S-1000. |

---

## §5 Protocolo, recibos e consulta/download (BX)

| Afirmação | Veredito | Evidência |
|---|---|---|
| Protocolo retornado **síncrono** no `enviarLoteEventos` (após N1) | **CONFIRMADO** | MOS v1.10 l.415-416, 491: "retorna ... o Protocolo de Envio ... confirmação de recepção". |
| Recibo por evento após N2; recuperado via `consultarLoteEventos` pelo protocolo | **CONFIRMADO** | MOS v1.10 l.502, 512-517. |
| Solução **eSocial BX (Download)** via WS; consulta por **nº recibo** ou **nº identificador (Id)** | **CONFIRMADO (URLs resolvidas)** | WS Download `https://webservices.download.esocial.gov.br/servicos/empregador/dwlcirurgico/WsSolicitarDownloadEventos.svc` (l.2860-2862); métodos `SolicitarDownloadEventosPorId` e `SolicitarDownloadEventosPorNrRecibo` (l.290-291). Consulta de identificadores `WsConsultarIdentificadoresEventos.svc` (mesmo host `dwlcirurgico`, l.2171-2173); tag `consultaIdentificadoresEvts`. |
| Atributo `Id` do evento usado na consulta/download | **CONFIRMADO** | MOS v1.10 l.2806, 2880, 2995: "atributo Id que fica na tag do evento". |

> ⚠️ **Limite operacional do BX (não na pesquisa):** soma de acessos a `ConsultarIdentificadores` +
> `Download` **não pode superar 10 por dia** e consulta com data limite até 1h antes da atual
> (MOS v1.10 l.2160-2170). **Projetar o polling/retry respeitando esse teto** — não fazer polling agressivo.

---

## §6 eSocial vs TCE-RS

| Afirmação | Veredito | Evidência |
|---|---|---|
| eSocial = API real (WS SOAP, assinatura A1, protocolo/recibo, consulta programática) | **CONFIRMADO** | §1-§5 acima. |
| TCE-RS = SEM API; geração de arquivos/leiaute + upload (SIAPC/PAD) | **INCERTO (não verificado aqui)** | Fora do escopo desta auditoria (foco eSocial). A pesquisa já marca `[a confirmar — leiaute TCE-RS/SIAPC-PAD]`. **Manter como pendência** e validar contra doc TCE-RS vigente antes do pipeline TCE. |
| Pipelines distintos (WS online vs exportador offline) | **PLAUSÍVEL** | Decorre do acima; consequência arquitetural razoável, não um fato a citar. |

---

## Placar

- **CONFIRMADO (com URL/citação oficial): 24** afirmações (incl. todas as URIs de assinatura, SOAP 1.1,
  validação 2 níveis, A1/A3 série A, EFR ente público, protocolo/recibo, URLs prod/restrita/BX).
- **REFUTADO / a corrigir na pesquisa: 3** (raiz `envioLoteEventos`; "50 eventos/lote"; Reference URI ao `Id`).
- **PLAUSÍVEL-SEM-FONTE: 3** (binding WS-Security exato; vedação a certificados distintos; pipelines distintos).
- **INCERTO/fora de escopo: 1** (TCE-RS sem API — não verificado aqui).

## NÃO implementar sem o doc oficial vigente (v1.15 / Leiautes S-1.3 NT 06/2026 / XSD)

1. **Schemas dos eventos de RH** (S-1200, S-1210, S-2200, S-2299, S-1000 etc.): obter dos **Leiautes
   S-1.3 vigentes (NT 06/2026)** — NÃO do MOS Desenvolvedor, NÃO inventar campos.
2. **Binding/WSDL e WS-Security** sob S-1.3: confirmar na **MOS Desenvolvedor v1.15** antes do cliente SOAP.
3. **Corte de lote por tamanho (750 KB)** — confirmar o valor na v1.15; **não** usar "50 eventos".

## 3 maiores riscos

1. **Versão das fontes desatualizada.** A pesquisa fixa URIs/estrutura na v1.10 e cita um "MOS S-1.3"
   inexistente como fonte de leiaute. Risco de codar contra spec antiga. **Mitigação:** travar tudo
   contra **MOS Desenvolvedor v1.15** + **Leiautes S-1.3 NT 06/2026** (XSD oficiais) antes do código.
2. **Assinatura errada quebra todo o envio (erro 612/rejeição do lote).** Dois erros concretos na
   pesquisa: `Reference URI` ao `Id` (correto é `URI=""`) e a confusão `Id`-na-assinatura. Como o A1
   server-side é o núcleo do M5, **um C14N/transform/URI fora do padrão rejeita 100% dos lotes.**
   **Mitigação:** validar o `<Signature>` gerado contra o exemplo do MOS e testar em **Produção Restrita**.
3. **Limite real do lote (750 KB) + teto de 10 consultas/dia no BX.** Implementar "50 eventos/lote"
   (mito) ou polling agressivo no Download gera rejeição/bloqueio. **Mitigação:** particionar lote por
   **750 KB** e desenhar fila de consulta respeitando **≤10 acessos/dia** ao BX, com backoff.

## Fontes (oficiais)
- Notícia URLs produção: <https://www.gov.br/esocial/pt-br/noticias/divulgadas-novas-url-para-transmissao-dos-dados-de-producao-do-esocial>
- Produção Restrita: <https://www.gov.br/esocial/pt-br/acesso-ao-sistema/ambiente-de-producao-restrita>
- MOS Desenvolvedor v1.10 (citado, texto extraído): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-10.pdf>
- MOS Desenvolvedor v1.15 (mais recente — usar): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-15.pdf>
- Lista de manuais: <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais>
- Leiautes S-1.3 vigentes (NT 06/2026): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html>
- Leiautes S-1.3 (índice/portaria): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-v-1.3/index.html>
- Manual Web Geral (assinatura/EFR/certificado): <https://www.gov.br/esocial/pt-br/empresas/manual-web-geral>

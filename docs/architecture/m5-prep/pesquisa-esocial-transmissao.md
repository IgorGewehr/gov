# Pesquisa — eSocial: Transmissão de Eventos (M5 RH)

> Escopo: ambiente, web service, lote vs individual, assinatura digital dos eventos (XML-DSig / A1 do ente), recibos/protocolo, consulta de eventos. E como isso difere do TCE-RS (que NÃO tem API).
> Regra de ouro (CLAUDE.md §16): toda afirmação factual tem FONTE (URL) ou está marcada `[a confirmar — <doc oficial>]`.
> Data da pesquisa: 2026-06-22. Versão de leiaute vigente do eSocial: **S-1.3** (MOS S-1.3 consolidado até NO S-1.3.07/2026).

---

## 1. Ambientes

O eSocial possui **dois ambientes** de webservice, com URLs distintas:

| Ambiente | Finalidade | Efeito jurídico |
|---|---|---|
| **Produção** | Transmissão oficial/legal dos eventos | Sim |
| **Produção Restrita** | Testes funcionais (sem carga/performance) | **Nenhum** efeito jurídico; máx. 1.000 vínculos/empregador |

- Produção Restrita: "realização de testes pelas empresas, sem qualquer efeito jurídico", limitada a testes funcionais. FONTE: <https://www.gov.br/esocial/pt-br/acesso-ao-sistema/ambiente-de-producao-restrita>
- As URLs de produção foram alteradas (notícia oficial de migração); as de produção restrita NÃO mudaram. FONTE: <https://www.gov.br/esocial/pt-br/noticias/divulgadas-novas-url-para-transmissao-dos-dados-de-producao-do-esocial>

### URLs dos Web Services (SVC = WCF/SOAP)

**Produção:**
- Enviar lote: `https://webservices.envio.esocial.gov.br/servicos/empregador/enviarloteeventos/WsEnviarLoteEventos.svc`
- Consultar lote: `https://webservices.consulta.esocial.gov.br/servicos/empregador/consultarloteeventos/WsConsultarLoteEventos.svc`

**Produção Restrita:**
- Enviar lote: `https://webservices.producaorestrita.esocial.gov.br/servicos/empregador/enviarloteeventos/WsEnviarLoteEventos.svc`
- Consultar lote: `https://webservices.producaorestrita.esocial.gov.br/servicos/empregador/consultarloteeventos/WsConsultarLoteEventos.svc`

FONTES: notícia de URLs de produção (acima) e página de Produção Restrita (acima).

> `[a confirmar — MOS Desenvolvedor v1.x]` URLs/serviços de **download de eventos (eSocial BX)** e **consulta de identificadores** (`WsConsultarIdentificadoresEventos` / `WsDownloadEventos`). Existência confirmada conceitualmente (recuperação de eventos transmitidos e recibos via webservice), mas as URLs exatas não foram extraídas — PDFs do MOS Desenvolvedor não parseáveis via fetch. FONTE conceitual (solução BX): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-10.pdf>

---

## 2. Web Service — protocolo (SOAP, não REST)

- Comunicação entre os webservices do eSocial e as aplicações dos contribuintes usa **SOAP 1.1**, troca de mensagens XML no padrão **Document/Literal**. **Não há API REST** — é SOAP/WCF (`.svc`). FONTE: <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-9-1.pdf> (busca no MOS Desenvolvedor)
- Acesso via WS não tem interface de navegador; o ente deve usar seu próprio sistema/cliente WS. FONTE: página Produção Restrita (§1).
- Implicação M5: cliente SOAP server-side com mTLS (certificado de conexão A1 do ente) + WS-Security/transporte. `[a confirmar — MOS Desenvolvedor]` binding exato (transport security x message security) e versão WSDL atual sob S-1.3.

---

## 3. Lote vs individual

- O envio é **sempre em lote** (`enviarLoteEventos`), mesmo que o lote contenha um único evento. Processamento **assíncrono** a partir do leiaute S-1.0. FONTE (busca MOS): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/mos-s-1-0-final-publicada-revisada.pdf>
- **Validação em 2 níveis**: Nível 1 na recepção do lote (verifica certificado de conexão + estrutura do lote) → retorna **protocolo**; Nível 2 (assíncrono) extrai e valida cada evento individualmente → gera **recibo** por evento. Eventos com erro estrutural são rejeitados sem prejudicar os demais do mesmo lote. FONTE (busca MOS S-1.0, acima).
- Elemento raiz do lote: `eSocial/envioLoteEventos` `[a confirmar — XSD/MOS Desenvolvedor: nome exato e namespace]`.

> `[a confirmar — MOS Desenvolvedor v1.x §lote]` **Limite máximo de eventos por lote** (valor frequentemente citado como 50 no modelo assíncrono) — NÃO confirmado em fonte oficial extraída. Obter número exato e tamanho máx. do lote no MOS Desenvolvedor vigente.

---

## 4. Assinatura digital dos eventos (XML-DSig)

- **Obrigatório** assinar **todos** os eventos transmitidos ao ambiente nacional (inclusão, alteração, retificação, exclusão). FONTE (busca MOS / Manual Web): <https://www.gov.br/esocial/pt-br/empresas/manual-web-geral>
- Certificado **ICP-Brasil série "A", tipo A1 ou A3**. FONTE (busca MOS Desenvolvedor v1.0): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manual-orientacao-desenvolvedore-social-versao1.0>
- **Para o ente público (Tensorroot.Gov): usar A1 e-CNPJ do ente**, server-side (envelope encryption já em construção). O preenchimento do **EFR (Ente Federado Responsável)** é obrigatório para naturezas jurídicas de ente público; estabelecimento com raiz CNPJ diferente deve pertencer ao mesmo EFR. FONTE (busca Manual Web Geral / MOS, acima).
- Há **dois usos do certificado**: (a) **certificado de conexão** (autenticação do canal/transporte ao WS) e (b) **certificado de assinatura do XML** (cada evento). Podem ser o mesmo A1 do ente. `[a confirmar — MOS Desenvolvedor]` se há restrição quanto a usar certificados distintos.

### Especificação técnica XML-DSig

`[a confirmar — MOS Desenvolvedor v1.x §Assinatura Digital]` — valores padrão do eSocial (NÃO confirmados em fonte extraída; PDF não parseável). Os valores tipicamente exigidos pelo eSocial, a CONFIRMAR no doc oficial:
- Assinatura **enveloped** sobre o elemento do evento (`evtX` com atributo `Id`).
- Canonicalization: `http://www.w3.org/TR/2001/REC-xml-c14n-20010315` (C14N).
- SignatureMethod: `http://www.w3.org/2001/04/xmldsig-more#rsa-sha256`.
- DigestMethod: `http://www.w3.org/2001/04/xmlenc#sha256`.
- Transforms: enveloped-signature + C14N.
- Reference URI apontando para o `Id` do evento.

> AÇÃO: baixar o MOS Desenvolvedor (PDF) localmente e extrair §assinatura para fixar URIs exatas antes de implementar o assinador.

---

## 5. Protocolo, recibos e consulta de eventos

- **Protocolo**: retornado de forma **síncrona** na resposta do `enviarLoteEventos` (após validação nível 1). Identifica o lote para consulta posterior. FONTE (busca MOS S-1.0, §validação 2 níveis).
- **Recibo de entrega**: gerado por evento após processamento (nível 2 assíncrono); recuperado via `consultarLoteEventos` informando o protocolo. FONTE (idem).
- **Consulta**: `consultarLoteEventos` retorna o resultado do processamento (status do lote + recibos/erros por evento). Para recuperar eventos já transmitidos e recibos, há a solução **eSocial BX (Download)** via webservice; consulta por **número do recibo** ou, na ausência, **número do identificador**. FONTE: MOS Desenvolvedor v1.10 (BX), acima.
- Fluxo M5: `enviarLoteEventos` → guarda protocolo → polling em `consultarLoteEventos` até processado → persistir recibo por evento (necessário para retificações/exclusões e prova de entrega).

---

## 6. Diferença para o TCE-RS (prestação de contas)

- **eSocial = API real (webservice SOAP)**: transmissão máquina-a-máquina, assinatura XML-DSig com A1, protocolo/recibo automáticos, consulta programática. Integração contínua e automatizável.
- **TCE-RS = SEM API**: a prestação de contas (folha/SIAPC-PAD etc.) é por **geração de arquivos/leiaute + upload manual** no sistema do Tribunal — não há webservice de transmissão automatizada. Ver `docs/architecture/specs-oficiais` e `INTEGRACOES-PRESTACAO-DE-CONTAS.md` deste repo. `[a confirmar — leiaute TCE-RS folha / SIAPC-PAD vigente]`.
- Consequência arquitetural M5: o módulo de **transmissão eSocial** é um cliente WS online (fila assíncrona, retry, persistência de protocolo/recibo); o de **TCE-RS** é um **gerador/exportador de arquivos** validável offline. São pipelines distintos — não reusar o transporte.

---

## 7. Pendências consolidadas [a confirmar — obter doc oficial]

1. URIs exatas de C14N/SignatureMethod/DigestMethod e estrutura do `<Signature>` — **MOS Desenvolvedor eSocial v1.x**, §Assinatura Digital.
2. Limite máx. de eventos por lote e tamanho máx. do lote — **MOS Desenvolvedor**, §Envio em Lotes.
3. URLs/WSDL dos serviços de **download (BX)** e **consulta de identificadores** — **MOS Desenvolvedor**.
4. Binding SOAP exato (transport vs message security; WS-Security) e versão WSDL sob **S-1.3** — **MOS Desenvolvedor** vigente.
5. Nomes/namespaces exatos dos elementos `envioLoteEventos`/`evtX` e schema dos eventos de RH a transmitir (S-1200 folha, S-2200/S-2299 admissão/desligamento etc.) — **XSD oficiais + MOS S-1.3**.
6. Leiaute TCE-RS de folha (SIAPC-PAD) vigente — **TCE-RS**.

## Fontes
- URLs de produção: <https://www.gov.br/esocial/pt-br/noticias/divulgadas-novas-url-para-transmissao-dos-dados-de-producao-do-esocial>
- Produção Restrita: <https://www.gov.br/esocial/pt-br/acesso-ao-sistema/ambiente-de-producao-restrita>
- MOS Desenvolvedor v1.0 / v1.9 / v1.10 (PDFs, não parseados via fetch — baixar p/ extração): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manual-orientacao-desenvolvedore-social-versao1.0> · <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-9-1.pdf> · <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-10.pdf>
- MOS leiaute S-1.0 (validação 2 níveis): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/mos-s-1-0-final-publicada-revisada.pdf>
- MOS S-1.3 (vigente): <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/mos-s-1-3-consolidada-ate-a-no-s-1-3-07-2026.pdf>
- Manual Web Geral (assinatura/EFR/certificado): <https://www.gov.br/esocial/pt-br/empresas/manual-web-geral>

# Pesquisa — PNCP (Portal Nacional de Contratações Públicas)

> M9 · Suprimentos avançados + compliance. Insumo para a integração de SAÍDA do módulo
> **Administracao** (compras/licitações/contratos) com o PNCP, sob a Lei 14.133/2021 (NLLC).
> Regra CLAUDE.md §16: toda afirmação tem **FONTE** ou marca **[a confirmar]**.

Data da pesquisa: 2026-06-22 · Status: pesquisa (não-código).

---

## 1. Por que é URGENTE para quem já empenha (a tese)

O fluxo de contrato do módulo Administracao já dispara empenho em Financas
(`ContratoAssinadoIntegrationEvent` → `EmpenhoEmitidoIntegrationEvent`). **Mas empenhar
não basta para o contrato produzir efeitos.** A Lei 14.133/2021, **art. 94**, é taxativa:

> "A divulgação no Portal Nacional de Contratações Públicas (PNCP) é **condição
> indispensável para a eficácia do contrato e de seus aditamentos**" — art. 94, caput.

Consequência prática: um contrato assinado, empenhado e até pago, **mas não publicado no
PNCP no prazo, é ineficaz** — e, no caso de contrato de urgência publicado fora do prazo,
**nulo** (§1). Para um ERP que já move dinheiro público, integrar o PNCP não é "feature de
transparência": é o **gate jurídico de eficácia** sem o qual a execução contratual e o
pagamento ficam expostos a glosa do Tribunal de Contas. Por isso a invariante I-7 do
`Contrato.rules.md` (eficácia exige PNCP) precisa de uma integração real por trás, não só
de um flag de domínio.

FONTE: Lei 14.133/2021, art. 94 — https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/94

---

## 2. Art. 94 — o quê, e em que PRAZO (bloqueante)

Texto do **art. 94** (condição de eficácia; prazos contados **da data da assinatura**, em
**dias úteis**):

| Item | Regra | Prazo |
|---|---|---|
| Caput | Divulgação no PNCP = condição indispensável de eficácia do contrato **e de seus aditamentos** | — |
| Inciso I | Quando decorrente de **licitação** | **20 dias úteis** |
| Inciso II | Quando **contratação direta** (dispensa/inexigibilidade) | **10 dias úteis** |
| §1º | Contrato de **urgência**: eficaz desde a assinatura, mas deve ser publicado nos prazos I/II — **sob pena de nulidade** | I/II |
| §2º | Contratação **artística**: divulgar custos detalhados (cachê, transporte, hospedagem, infraestrutura) | — |
| §3º | **Obras**: divulgar quantitativos e preços unitários/totais em até **25 dias úteis** após a assinatura; quantitativos executados e preços praticados em até **45 dias úteis** após a conclusão | 25 / 45 d.u. |

FONTE: art. 94 — https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/94

> ⚠️ **CORREÇÃO a aplicar no domínio (achado principal).** O `Contrato.rules.md` atual
> atribui a "condição de eficácia" ao **art. 174** (ver linhas 17, 28-32, 58, 94, 144, 148,
> 406 do arquivo). Isso está **impreciso**: a condição de eficácia + os prazos são do
> **art. 94**. O **art. 174** define o portal (finalidades e rol do que se divulga). O M9
> deve corrigir as `fontes_legais` e a redação das invariantes I-7/I-11 para citar
> **art. 94** (eficácia/prazos) e manter art. 174 só como base do portal. Além disso, o
> domínio **não modela prazo** hoje: não há controle de "20/10 dias úteis a contar da
> assinatura", nem alerta de prazo vencido — lacuna a fechar no M9 (ver §6).

---

## 3. Art. 174 — o portal e o ROL do que se publica

O **art. 174** institui o PNCP como sítio eletrônico oficial de **divulgação centralizada e
obrigatória** dos atos da NLLC e de **realização facultativa** das contratações. Gerido pelo
Comitê Gestor da Rede Nacional de Contratações Públicas (CGRNCP).

Rol de divulgação **obrigatória** no PNCP (art. 174, §2º — o que o módulo terá de empurrar):

- **Planos de contratação anuais (PCA)** e catálogo eletrônico de padronização;
- **Editais** de licitação e respectivos anexos (inteiro teor);
- **Atas de registro de preços (ARP)**;
- **Contratos** e **termos aditivos**;
- **Notas fiscais eletrônicas**, quando for o caso.

FONTES:
- art. 174 — https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/174
- art. 174 (rol) — https://www.jusbrasil.com.br/topicos/386671224/artigo-174-da-lei-n-14133-de-01-de-abril-de-2021
- Portal PNCP — https://www.gov.br/pncp/pt-br/pncp

> **Mapeamento PNCP × Administracao:** `Licitacao` → publicar **edital** (art. 54 publicidade
> do edital + art. 174); `Licitacao` com SRP → **ata de registro de preços**; `Contrato` →
> **contrato**; `Aditivo` → **termo aditivo**; planejamento de compras → **PCA**. O M9 amplia
> a integração para além do `Contrato` (hoje o único evento PNCP existente). [a confirmar:
> se PCA/edital/ata entram no escopo do M9 ou ficam para módulo de Planejamento de Compras]

---

## 4. API / Integração do PNCP (técnico)

Protocolo **REST/HTTP 1.1**, payloads **JSON**. As APIs de **consulta são públicas**; as de
**manutenção** (inserir/retificar/excluir) exigem **autenticação/autorização** e
**credenciamento** da plataforma junto ao Ministério da Gestão e Inovação (MGI), que fornece
login/senha por órgão/entidade representado.

**Ambientes (URL base dos serviços de manutenção):**

| Ambiente | Portal | Base da API |
|---|---|---|
| Treino/Homologação | https://treina.pncp.gov.br | `https://treina.pncp.gov.br/api/pncp` |
| Produção | https://pncp.gov.br | `https://pncp.gov.br/api/pncp` |

**Autenticação:** API de login (usuário/senha) → retorna **JWT** no header HTTP
`Authorization` após o prefixo `Bearer`; token com **validade de ~1 hora**, reutilizável
enquanto válido.

**Documentação técnica viva (Swagger):**
- Manutenção (produção): https://pncp.gov.br/api/pncp/swagger-ui/index.html
- Consulta (produção): https://pncp.gov.br/api/consulta/swagger-ui/index.html
- Treino/homologação: https://treina.pncp.gov.br/api/pncp/swagger-ui/index.html

**Manual de Integração — versão CORRENTE: 2.5 (publicado 11/06/2026).** Versões anteriores
(2.3.5/2.3.6/2.2.x) ficam no histórico; confirmar contratos de payload sempre contra a 2.5 +
Swagger antes de codar.

**Endpoints de manutenção (famílias — estrutura estável entre versões):**
- Inserir **órgão/entidade**;
- Inserir **unidade administrativa** (quem compra e contrata);
- Inserir **compra/edital** (módulo compra/licitação/edital);
- **Ata de registro de preços**;
- **Contrato** (inserir contrato) e **termo de contrato/aditivo**;
- **Arquivos/documentos** anexos.
[a confirmar: paths e schemas exatos de cada endpoint na v2.5 — extrair do Swagger/PDF]

FONTES:
- Manuais (índice; v2.5 11/06/2026) — https://www.gov.br/pncp/pt-br/integre-se-ao-pncp/manual-de-integracao
- Manuais (acesso à informação) — https://www.gov.br/pncp/pt-br/acesso-a-informacao/manuais
- Swagger manutenção — https://pncp.gov.br/api/pncp/swagger-ui/index.html
- Swagger consulta — https://pncp.gov.br/api/consulta/swagger-ui/index.html
- Decreto 11.462/2023 (regulamenta o PNCP) — https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2023/decreto/d11462.htm

---

## 5. Obrigatoriedade / regime de transição (municípios pequenos)

- A obrigatoriedade plena da NLLC (revogação da Lei 8.666) foi prorrogada para **29/12/2023**
  (MP 1.167/2023 e conversão).
- **Art. 176:** municípios com **até 20.000 habitantes** têm **6 anos** da publicação da Lei
  (01/04/2021) — ou seja, **até ~01/04/2027** — para cumprir as exigências de divulgação em
  sítio eletrônico/PNCP. Enquanto não adotarem o PNCP, devem **publicar em diário oficial**
  (admitido extrato) e disponibilizar versão física dos documentos.
- **Impacto no piloto (Maximiliano de Almeida/RS):** município pequeno — durante a transição
  é admissível diário oficial, mas o destino é PNCP. O ERP deve **nascer já integrado ao PNCP**
  (não depender do regime transitório), pois o prazo expira em 2027 e a eficácia do art. 94 já
  vale para quem optou pela NLLC. [a confirmar: porte populacional exato do piloto]

FONTES:
- art. 176 — https://www.jusbrasil.com.br/topicos/386671146/artigo-176-da-lei-n-14133-de-01-de-abril-de-2021
- Prorrogação (MGI) — https://www.gov.br/gestao/pt-br/assuntos/noticias/2023/marco/prazo-para-adequacao-de-estados-e-municipios-a-nova-lei-de-licitacoes-sera-prorrogado

---

## 6. Relação com o módulo Administracao — o que o M9 precisa fazer

Já existe (base):
- `ContratoPublicadoPncpIntegrationEvent` (Contracts) e comando `PublicarContratoNoPncp`;
- Invariante I-7 (eficácia exige `PublicadoNoPncp`); §11 das rules prevê cliente REST/JSON
  resiliente (Polly) via handler do Outbox, idempotente, atrás de ACL.
  Arquivos:
  - `/Users/igorgewehr/Development/Tensorroot.Gov/src/Modules/Administracao/rules/Contrato.rules.md`
  - `/Users/igorgewehr/Development/Tensorroot.Gov/src/Modules/Administracao/Tensorroot.Gov.Modules.Administracao.Contracts/ContratoPublicadoPncpIntegrationEvent.cs`

Lacunas a fechar no M9:
1. **Cliente PNCP real** (ACL): login/JWT (renovação a cada ~1h), credenciamento por
   CNPJ/órgão, mapeamento de payloads v2.5, Polly (retry/circuit breaker/timeout), idempotência
   por chave da contratação. Certificado/credenciais **só no Azure Key Vault** (CLAUDE.md §5).
2. **Controle de PRAZO do art. 94** (hoje inexistente): persistir `DataAssinatura`, calcular
   deadline em **dias úteis** (20 licitação / 10 direta / 25-45 obras), e gerar **alerta de
   prazo a vencer / vencido** — risco de ineficácia/nulidade. Feriados em dias úteis:
   calendário nacional + municipal por tenant (parametrizável, CLAUDE.md §7).
3. **Corrigir base legal** nas rules: eficácia/prazos = **art. 94**; portal/rol = **art. 174**.
4. **Ampliar escopo de publicação** além do contrato: edital, ata de registro de preços (SRP),
   termo aditivo, e (se no escopo M9) PCA — conforme rol do art. 174 §2.
5. **Ordem de pré-requisitos PNCP:** inserir contrato exige órgão+unidade já cadastrados no
   PNCP; modelar o bootstrap de órgão/unidade por tenant. [a confirmar fluxo exato no Swagger]

---

## 7. Pendências [a confirmar] — consolidado

- Paths e JSON-schemas exatos dos endpoints de manutenção na **v2.5** (extrair do Swagger/PDF).
- Sequência de pré-cadastro (órgão → unidade → compra → contrato) e chaves de idempotência.
- Escopo M9: PCA e edital/ata entram aqui ou em módulo de Planejamento de Compras?
- Regras de **retificação/exclusão** no PNCP (e como refletir aditivo/apostilamento/rescisão).
- Calendário de feriados (nacional + municipal do tenant) para a contagem de dias úteis.
- Porte populacional do piloto (regime transitório do art. 176).
- Texto **literal integral** do art. 94 direto do Planalto (fetch falhou nesta sessão;
  validado por fonte secundária TCE-SP). URL: https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm

---

## 8. Fontes (consolidado)

- Lei 14.133/2021, art. 94 (TCE-SP comentada) — https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/94
- Lei 14.133/2021, art. 174 (TCE-SP comentada) — https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/174
- Lei 14.133/2021, art. 174 (rol — Jusbrasil) — https://www.jusbrasil.com.br/topicos/386671224/artigo-174-da-lei-n-14133-de-01-de-abril-de-2021
- Lei 14.133/2021, art. 176 (Jusbrasil) — https://www.jusbrasil.com.br/topicos/386671146/artigo-176-da-lei-n-14133-de-01-de-abril-de-2021
- Lei 14.133/2021 (texto integral, Planalto) — https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm
- Decreto 11.462/2023 (Planalto) — https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2023/decreto/d11462.htm
- PNCP — Manual de Integração (índice, v2.5 11/06/2026) — https://www.gov.br/pncp/pt-br/integre-se-ao-pncp/manual-de-integracao
- PNCP — Swagger manutenção — https://pncp.gov.br/api/pncp/swagger-ui/index.html
- PNCP — Swagger consulta — https://pncp.gov.br/api/consulta/swagger-ui/index.html
- PNCP — Portal gov.br — https://www.gov.br/pncp/pt-br/pncp
- Prorrogação NLLC (MGI) — https://www.gov.br/gestao/pt-br/assuntos/noticias/2023/marco/prazo-para-adequacao-de-estados-e-municipios-a-nova-lei-de-licitacoes-sera-prorrogado

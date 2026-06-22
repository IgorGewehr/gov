# Verificação — pesquisa-pncp.md (auditoria cética de fontes)

> Auditoria das afirmações de `pesquisa-pncp.md` contra fontes oficiais.
> Classificação por afirmação: **CONFIRMADO** (fonte oficial/primária bate literalmente) ·
> **PLAUSÍVEL** (fonte secundária consistente, sem primária literal nesta sessão) ·
> **INCERTO** (não confirmado, impreciso ou contraditório).
> Regra CLAUDE.md §16: FONTE ou [a confirmar].

Data da verificação: 2026-06-22 · Auditor: agente CÉTICO.
Limitação: `planalto.gov.br` recusou conexão direta (WebFetch) nesta sessão — texto de lei
validado via **TCE-SP (legislação comentada, transcrição literal)** + buscas. Marcado
**CONFIRMADO** apenas quando a transcrição literal foi obtida.

---

## Placar

- **CONFIRMADO: 14**
- **PLAUSÍVEL: 4**
- **INCERTO/IMPRECISO: 3**

---

## 1. Afirmações jurídicas (Lei 14.133/2021)

| # | Afirmação em pesquisa-pncp.md | Classe | Evidência |
|---|---|---|---|
| 1 | Art. 94 caput: divulgação no PNCP é **condição indispensável para a eficácia do contrato e de seus aditamentos** | **CONFIRMADO** | Transcrição literal TCE-SP art. 94: "...é condição indispensável para a eficácia do contrato e de seus aditamentos e deverá ocorrer nos seguintes prazos, contados da data de sua assinatura" |
| 2 | Art. 94 I: licitação → **20 dias úteis** (da assinatura) | **CONFIRMADO** | TCE-SP: "I - 20 (vinte) dias úteis, no caso de licitação" |
| 3 | Art. 94 II: contratação direta → **10 dias úteis** | **CONFIRMADO** | TCE-SP: "II - 10 (dez) dias úteis, no caso de contratação direta" |
| 4 | Art. 94 §1: contrato de **urgência** eficaz desde a assinatura, mas publica nos prazos I/II **sob pena de nulidade** | **CONFIRMADO** | TCE-SP §1º: eficácia imediata, publicação nos prazos, sob pena de nulidade |
| 5 | Art. 94 §2: contratação **artística** divulga custos (cachê, transporte, hospedagem, infraestrutura, logística) | **CONFIRMADO** | TCE-SP §2º transcrito: "custos do cachê do artista, dos músicos ou da banda... transporte... hospedagem... infraestrutura... logística do evento e das demais despesas específicas" |
| 6 | Art. 94 §3: **obras** → quantitativos/preços contratados em **25 d.u. após assinatura**; quantitativos executados/preços praticados em **45 d.u. após a conclusão** | **CONFIRMADO** | TCE-SP §3º transcrito: "em até 25 (vinte e cinco) dias úteis após a assinatura... e em até 45 (quarenta e cinco) dias úteis após a conclusão do contrato" |
| 7 | **Achado principal**: a condição de eficácia + prazos é do **art. 94**, não do art. 174; rules.md atribui ao art. 174 (impreciso) | **CONFIRMADO** | Confirma-se: art. 94 traz eficácia e prazos; art. 174 institui o portal e o rol. A correção proposta nas rules está correta. |
| 8 | Art. 174: institui o PNCP — (I) divulgação centralizada e obrigatória; (II) realização facultativa das contratações pelos Poderes Executivo/Legislativo/Judiciário | **CONFIRMADO** | TCE-SP art. 174 caput, incisos I e II transcritos |
| 9 | Rol de divulgação obrigatória (art. 174 **§2º**): PCA, catálogo de padronização, editais e anexos, ata de registro de preços, contratos e termos aditivos, **NFe quando for o caso** | **CONFIRMADO** | TCE-SP §2º: lista inclui "planos de contratação anuais", "catálogos eletrônicos de padronização", "editais... e respectivos anexos", "atas de registro de preços", "contratos e termos aditivos", "notas fiscais eletrônicas, quando for o caso". OBS: o §2º também inclui itens não citados na pesquisa (ex.: editais de credenciamento/pré-qualificação, avisos de contratação direta) — rol é mais amplo. |
| 10 | PNCP gerido pelo **Comitê Gestor da Rede Nacional de Contratações Públicas (CGRNCP)**, presidido por representante indicado pelo Presidente da República | **CONFIRMADO** | TCE-SP art. 174 §1º; reforçado pelo Decreto 10.764/2021 |
| 11 | Art. 176: municípios **até 20.000 hab.** têm **6 anos** da publicação da Lei (01/04/2021) → ~01/04/2027 | **CONFIRMADO** | Busca + TCE-SP/Jusbrasil art. 176: prazo de 6 anos para arts. 7º/8º, pregão eletrônico (§2º art. 17) e regras de divulgação em sítio eletrônico |
| 12 | Durante a transição, município sem PNCP **publica em diário oficial** (admitido extrato) + versão física dos documentos | **CONFIRMADO** | Art. 176, parágrafo único, I e II: publicar no diário oficial (extrato admitido) e disponibilizar versão física sem custo além de reprodução |
| 13 | Prorrogação da obrigatoriedade plena da NLLC (revogação 8.666) para **29/12/2023** | **PLAUSÍVEL** | Consistente com MP 1.167/2023 e com o Decreto 11.462/2023 (transição cita 8.666/10.520/12.462 até 29/12/2023). Texto literal da MP não buscado nesta sessão. |
| 14 | Substituição da publicação na imprensa oficial pelo PNCP (efeito do art. 94) | **CONFIRMADO** | Busca art. 94: a divulgação no PNCP substitui a publicação resumida na imprensa oficial (cf. art. 61 § único da 8.666) como condição de eficácia |

---

## 2. Afirmações técnicas (API PNCP)

| # | Afirmação | Classe | Evidência |
|---|---|---|---|
| 15 | APIs de **consulta públicas**; APIs de **manutenção** exigem autenticação/autorização e credenciamento | **CONFIRMADO** | Manual de Integração PNCP (índice gov.br) e Swagger; busca confirma |
| 16 | Base de manutenção: treino `https://treina.pncp.gov.br/api/pncp` · produção `https://pncp.gov.br/api/pncp` | **CONFIRMADO** | Swagger oficial ativo em ambos os hosts (`/api/pncp/swagger-ui/index.html`); base `${BASE_URL}` = host + `/api/pncp` |
| 17 | Login retorna **JWT** no header `Authorization` após `Bearer`; validade **~1 hora**, reutilizável enquanto válido | **CONFIRMADO** | Manual: token expira em 1h; login `POST .../v1/usuarios/login` devolve JWT no header Authorization/Bearer |
| 18 | Endpoint de login `POST /v1/usuarios/login` | **PLAUSÍVEL** | Busca aponta `POST https://pncp.gov.br/api/pncp/v1/usuarios/login`; confirmar path exato no Swagger v2.5 antes de codar |
| 19 | **Manual de Integração — versão corrente 2.5**, publicado **Jun/2026** | **CONFIRMADO** | Resultado oficial gov.br: "VERSÃO 2.5 Jun/2026" no display-file do manual atual. (A data exata "11/06/2026" afirmada na pesquisa é **PLAUSÍVEL** — a fonte confirma "Jun/2026", não o dia 11.) |
| 20 | Famílias de endpoints de manutenção: órgão, unidade administrativa, compra/edital, ata de registro de preços, contrato, termo aditivo, arquivos | **PLAUSÍVEL** | Estrutura consistente com manuais anteriores e Swagger; **paths/schemas exatos da v2.5 NÃO extraídos** (PDF 433 p./7,2 MB não parseável nesta sessão). Mantém-se [a confirmar] como na pesquisa. |

---

## 3. Afirmações INCERTAS / IMPRECISAS (corrigir)

| # | Afirmação | Classe | Problema |
|---|---|---|---|
| 21 | **"Decreto 11.462/2023 (regulamenta o PNCP)"** — citado em §4 e §8 como FONTE do PNCP | **INCERTO / IMPRECISO** | O Decreto **11.462/2023 regulamenta o Sistema de Registro de Preços (arts. 82–86 da Lei 14.133)** — atas de registro de preços no âmbito federal —, **não o PNCP**. Quem disciplina o **CGRNCP / gestão do PNCP** é o **Decreto 10.764/2021** (art. 174 §1). Corrigir a citação: usar **Decreto 10.764/2021** para "gestão do PNCP/Comitê Gestor" e reservar **11.462/2023** para o tema **SRP/ata de registro de preços**. |
| 22 | "Obrigatoriedade plena prorrogada para 29/12/2023 (MP 1.167/2023 e conversão)" | **PLAUSÍVEL→a confirmar** | Data 29/12/2023 confirmada por fonte normativa (Decreto 11.462/2023 cita esse marco). Mas o instrumento "MP 1.167/2023 e conversão" não foi verificado em fonte primária — confirmar número da MP/lei de conversão antes de citar como FONTE. |
| 23 | "Porte populacional do piloto (Maximiliano de Almeida/RS)" e enquadramento no art. 176 | **INCERTO** | Já marcado [a confirmar] na pesquisa; permanece. Maximiliano de Almeida/RS é município pequeno (provavelmente < 20.000 hab., logo no regime do art. 176), mas o número exato não foi verificado. Impacta a decisão de "nascer já integrado ao PNCP". |

Observação adicional (não-erro, refinamento): a pesquisa cita o rol em alguns pontos como
"art. 174 §2º" e o WebFetch de uma fonte secundária mencionou "§3º". A transcrição literal
do **TCE-SP confirma o rol no §2º** — a pesquisa está correta. A divergência veio de fonte
secundária e foi descartada.

---

## 4. Três riscos para o M9

1. **Risco jurídico de base legal/regulamentar errada (alto).** Citar "Decreto 11.462/2023
   regula o PNCP" é falso: ele regula o SRP. Se essa premissa entrar nas `fontes_legais` do
   domínio ou em remessa ao TCE, vira achado de auditoria. **Mitigação:** trocar por Decreto
   10.764/2021 (gestão/PNCP) e usar 11.462/2023 só no contexto de ata de registro de preços.
   Manter o achado já correto da pesquisa: eficácia/prazos = art. 94; portal/rol = art. 174.

2. **Risco de contrato de API não verificado (alto, bloqueante de implementação).** Os
   paths/JSON-schemas dos endpoints de manutenção da **v2.5** não foram extraídos (PDF não
   parseável; Swagger não navegado endpoint-a-endpoint nesta sessão). Construir o cliente ACL
   (login/JWT, inserir órgão→unidade→compra→contrato→aditivo→arquivos, retificação/exclusão,
   chaves de idempotência) sobre suposição = retrabalho. **Mitigação:** antes de codar, extrair
   contratos do Swagger de produção/treino e do PDF v2.5; validar em `treina.pncp.gov.br`.

3. **Risco de prazo legal não modelado (alto, é o gate de eficácia).** O domínio hoje não
   persiste `DataAssinatura` nem calcula deadline em **dias úteis** (20/10/25/45), e não há
   alerta de prazo a vencer/vencido. Perder o prazo do art. 94 = contrato **ineficaz** (e, em
   urgência, **nulo**) → glosa do TCE. A contagem em dias úteis depende de **calendário de
   feriados nacional + municipal por tenant** (parametrizável, §7 CLAUDE.md), que também não
   existe. **Mitigação:** modelar relógio de prazo + calendário de feriados como pré-requisito
   da invariante I-7, não como item secundário.

---

## 5. Fontes da verificação

- Lei 14.133/2021 art. 94 (transcrição literal) — https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/94
- Lei 14.133/2021 art. 174 (transcrição literal, rol §2º, gestor §1º) — https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/174
- Lei 14.133/2021 art. 176 (comentado) — https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/176
- Decreto 10.764/2021 (CGRNCP / gestão do PNCP) — https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/decreto/d10764.htm
- Decreto 11.462/2023 (Sistema de Registro de Preços, arts. 82–86) — https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2023/decreto/d11462.htm
- Manual de Integração PNCP v2.5 Jun/2026 (oficial) — https://www.gov.br/pncp/pt-br/pncp/manuais/manual-de-integracao-pncp/@@display-file/file
- Swagger manutenção (produção) — https://pncp.gov.br/api/pncp/swagger-ui/index.html
- Swagger manutenção (treino) — https://treina.pncp.gov.br/api/pncp/swagger-ui/index.html
- Swagger consulta — https://pncp.gov.br/api/consulta/swagger-ui/index.html

> Pendência herdada (não fechada nesta sessão): texto literal do art. 94/174/176 direto do
> **Planalto** (host recusou conexão); paths/schemas exatos da API v2.5; número/conversão da
> MP de prorrogação; porte populacional do piloto.

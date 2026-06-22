# M9 — Verificação adversarial: Convênios (Transferegov/D11531) + MROSC (L13019)

> Auditoria cética do documento `pesquisa-convenios-mrosc.md`. Cada afirmação foi
> reconferida contra fonte oficial (Planalto/Câmara mirror, gov.br/transferegov, TCU).
> Classificação: **CONFIRMADO** (fonte primária bate) · **PLAUSÍVEL** (fonte secundária
> coerente, sem texto primário) · **INCERTO** (não verificado / divergência / fonte frágil).
>
> Nota metodológica: o site do Planalto retornou erro de socket em todas as tentativas
> (mesmo problema relatado na pesquisa original). Usei mirrors oficiais — Câmara dos
> Deputados (legin), gov.br/transferegov, jurishand, modeloinicial — e portal TCU.
> Onde só havia fonte secundária, classifiquei como PLAUSÍVEL, não CONFIRMADO.

Data da verificação: 2026-06-22.

---

## Quadro de classificação por afirmação

### A) TRANSFEREGOV / Decreto 11.531/2023 (convênios federais recebidos)

| # | Afirmação na pesquisa | Classificação | Evidência / correção |
|---|---|---|---|
| A1 | Transferegov.br é plataforma única para transferências da União a entes, consórcios e entidades privadas s/ fins lucrativos | **CONFIRMADO** | gov.br/transferegov; D11271/2022 institui a plataforma e o Sigpar. |
| A2 | Convênios/contratos de repasse regidos pelo **D11.531/2023** (substituiu sistemática anterior) | **CONFIRMADO** | Câmara legin + gov.br/transferegov/legislacao/decretos. Decreto de 16/05/2023. |
| A3 | Operacionalização do Transferegov disciplinada pelo **D11.271/2022** | **CONFIRMADO** | gov.br/transferegov e ABCR: D11.271 de 05/12/2022 instituiu o Transferegov.br + Sigpar; operação a partir de 31/01/2023. (Pendência nº2 da pesquisa RESOLVIDA — não é "[a confirmar]".) |
| A4 | Prestação de contas **CONTÍNUA**, iniciada concomitantemente à liberação da 1ª parcela | **CONFIRMADO** | contas.cnt.br cita texto: "a prestação de contas é contínua... iniciada concomitantemente à liberação da primeira parcela dos recursos financeiros". |
| A5 | Prazo da Administração ANALISAR PC: **60 dias** (informatizado) ou **180 dias** (convencional), prorrogável uma vez por igual período | **CONFIRMADO** | **Art. 21 do D11.531/2023**: 60 dias (procedimento informatizado) / 180 dias (análise convencional); prorrogação única por igual período; prazo do inc. I conta da atribuição da nota de risco no Transferegov.br. Saneamento de impropriedades: até 45 dias. |
| A6 | 1º desembolso (salvo parcela única) **não pode exceder 20%** do valor total | **INCERTO / FONTE DESATUALIZADA** | A regra dos 20% é da **Portaria Interministerial 424/2016, REVOGADA pela Portaria Conjunta MGI/MF/CGU nº 33/2023**. Não foi possível localizar a regra dos 20% no texto do D11.531/2023 nem reconfirmá-la na cartilha TCU 2025. Tratar como **[a confirmar na Portaria Conjunta 33/2023]** — NÃO modelar como constante hardcoded. |
| A7 | Prazo do MUNICÍPIO apresentar PC final ~30 dias (faixa 30–60) após término | **INCERTO** | A própria pesquisa marcou "[a confirmar]". Busca trouxe "PC final em 60 dias" e "devolução de saldo em 30 dias" (fontes secundárias divergentes). Pendência nº1 PERMANECE — exige leitura do artigo específico do D11.531/2023. |
| A8 | Inadimplência: saldo corrigido pela **Selic a partir do 30º dia**; impedimento de novos recursos | **PLAUSÍVEL (gatilho incerto)** | Correção pela Selic em débitos de convênio é praxe (fonte secundária confirma Selic + multa de mora 0,33%/dia limitada a 20%). O gatilho exato "30º dia" não foi confirmado em fonte primária. Impedimento de novos recursos por instrumento sem execução >180 dias era regra da PI 424 (revogada). |
| A9 | Resultado: aprovação / aprovação com ressalvas / rejeição | **PLAUSÍVEL** | Padrão da legislação de convênios; sem citação ao artigo do D11.531. |
| A10 | Existência de **API pública** do Transferegov (marcado "[a confirmar]") | **CONFIRMADO — existe** | gov.br/transferegov/sobre/apis-integracao + Swagger `docs.api.transferegov.gestao.gov.br` (ex.: módulo fundo-a-fundo, PostgREST) + dados.gov.br "Transferências e parcerias da União". Maioria das APIs é restrita a órgãos públicos integrados; **dados abertos** (DTPAR) são livres. Pendência nº4 RESOLVIDA: API existe; escopo de escrita exige convênio de integração. |

### B) MROSC — Lei 13.019/2014 (parcerias de saída com OSCs)

| # | Afirmação na pesquisa | Classificação | Evidência / correção |
|---|---|---|---|
| B1 | **Termo de Colaboração** = plano proposto pela ADMINISTRAÇÃO | **CONFIRMADO** | **Art. 16**, caput (Câmara legin): plano de trabalho proposto pela administração pública; § único admite proposta de conselhos de políticas públicas. |
| B2 | **Termo de Fomento** = plano proposto pela OSC | **CONFIRMADO** | **Art. 17**, caput: plano proposto pelas organizações da sociedade civil. |
| B3 | Diferença Colaboração×Fomento = quem propõe | **CONFIRMADO** | Decorre direto dos arts. 16 e 17. |
| B4 | **Acordo de Cooperação** = sem transferência de recursos | **PLAUSÍVEL** | Coerente com a Lei (art. 2º, VIII-A def.); não reli o inciso primário nesta rodada. Mantém classificação da pesquisa. |
| B5 | Regra: termo de colab./fomento depende de **chamamento público** (art. 24) | **CONFIRMADO** | **Art. 24**: administração deverá realizar chamamento; edital com programação orçamentária, objeto, datas, critérios, valor, exigências (3 anos de existência, experiência, capacidade técnica). |
| B6 | **Dispensa (art. 30)**: urgência; guerra/calamidade/perturbação; proteção a ameaçados; (pesquisa cita "180 dias" e credenciamento educação/saúde/assistência) | **CONFIRMADO (parcial — ATENÇÃO ao "180 dias")** | **Art. 30** confirmado quanto a: I urgência (limitada ao prazo do instrumento original); II guerra/grave perturbação (assist. social/saúde/educação); III proteção a pessoas ameaçadas. **O número "180 dias" do art. 30 NÃO foi confirmado** no texto reconferido — a redação fala em "limitada ao prazo original", não a 180 dias fixos. Tratar "180 dias" como **INCERTO**. |
| B7 | **Inexigibilidade (art. 31)**: inviabilidade de competição (objeto singular / entidade específica / acordo internacional) | **CONFIRMADO** | **Art. 31**: inexigível quando inviável a competição por natureza singular do objeto ou exclusividade. (Hipótese "acordo internacional" não reconfirmada literalmente — minor.) |
| B8 | Ausência de chamamento exige **justificativa detalhada** publicada (art. 32) | **CONFIRMADO** | **Art. 32**: ausência de processo seletivo detalhadamente justificada; extrato publicado (prazo p/ impugnação ~5 dias); justificativa indevida → revogação e abertura de chamamento. |
| B9 | **Plano de Trabalho (art. 22)**: realidade/objeto, metas, receitas/despesas, parâmetros de aferição | **CONFIRMADO** | **Art. 22** (incisos reconferidos): I diagnóstico/realidade; II metas quantitativas e mensuráveis; III prazo; IV indicadores/aferição; V compatibilidade de custos; VI plano de aplicação; VII estimativa de encargos; VIII valores + cronograma de desembolso; IX modo de prestação de contas; X prazos de análise. (Pendência nº3 da pesquisa RESOLVIDA.) |
| B10 | OSC apresenta **PC final em até 90 dias**, prorrogável +30 (art. 69 §1º) | **CONFIRMADO** | **Art. 69**: até 90 dias do término (ou ao fim de cada exercício se vigência > 1 ano); prorrogável por até 30 dias justificadamente. |
| B11 | Administração pode fixar prazo diverso de 90 dias conforme complexidade/vulto | **PLAUSÍVEL** | Coerente com art. 69; não localizei o dispositivo literal que autoriza prazo diverso nesta rodada. |
| B12 | Administração ANALISA PC final: até **150 dias** do recebimento do Relatório Final, prorrogável por igual período | **CONFIRMADO** | **Art. 71**: até 150 dias contados do recebimento (ou cumprimento de diligência), prorrogável justificadamente por igual período. **Achado adicional**: **art. 70** — saneamento de irregularidades pela OSC em 45 dias por notificação, prorrogável uma vez, dentro do prazo de análise. Convém modelar esse subestado. |
| B13 | PC contínua (relatórios parciais + final) | **PLAUSÍVEL** | Coerente com os manuais MROSC citados; sem artigo único. |
| B14 | Resultado: aprovação / ressalvas / rejeição | **CONFIRMADO (implícito)** | Decorre dos arts. 71–72 e manuais; medidas compensatórias na rejeição. |
| B15 | Município mantém lista de parcerias + edital em sítio oficial e plataforma (arts. 10/11) | **PLAUSÍVEL** | Arts. 10/11 impõem transparência; **prazos exatos não reconferidos** — pesquisa já marcava "[a confirmar]". Mantém INCERTO quanto a prazos. |
| B16 | Parcerias federais com OSCs operam pela **Plataforma MROSC dentro do Transferegov.br** | **CONFIRMADO** | gov.br/transferegov: Sigpar (Sistema de Gestão de Parcerias da União) instituído pelo D11.271/2022 para parcerias; Plataforma MROSC integrada ao Transferegov.br. |

---

## Placar

- **CONFIRMADO**: 14 (A1, A2, A3, A4, A5, A10, B1, B2, B3, B5, B7, B8, B9, B10, B12, B16, B6-núcleo) — núcleo jurídico do MROSC e do D11.531 sólido.
- **PLAUSÍVEL** (usar, mas sem citar como certeza primária): A8, A9, B4, B11, B13, B15.
- **INCERTO / corrigir antes de modelar**: **A6 (20% — base revogada)**, **A7 (prazo PC do convenente)**, **B6 "180 dias" no art. 30**, **B15 prazos de transparência**.

Resumo: a maioria das afirmações estruturantes está **CONFIRMADA**. Os pontos frágeis são
todos de **número/prazo específico** — exatamente onde a §16 exige cuidado — e dois deles
(A6, B6) decorrem de **citar regra de norma revogada ou número não textual**.

---

## 3 RISCOS para o modelo M9 (Convênios)

1. **Hardcode de prazos/percentuais de norma REVOGADA ou não confirmada (risco crítico).**
   A regra dos 20% (A6) vem da PI 424/2016, **revogada pela Portaria Conjunta MGI/MF/CGU
   nº 33/2023**, e o gatilho Selic "30º dia" (A8) não tem base primária. Se modelados como
   constantes, violam §7 (sem números mágicos) e §16 (parar e pesquisar). **Mitigação**:
   todos os prazos/percentuais como parâmetros por tenant/configuração versionada, com a
   norma-fonte registrada; bloquear PR que introduza esses números hardcoded.

2. **Confusão entre os dois regimes e suas máquinas de estado/prazos.** Convênio federal
   (D11.531: análise 60/180d, art. 21) e MROSC (L13019: PC 90+30d art. 69; análise 150d
   art. 71; saneamento 45d art. 70) têm prazos e gatilhos DISTINTOS. Reaproveitar a mesma
   state machine "para facilitar" produziria alertas legais errados (multa/Selic/inscrição
   em dívida ativa indevidas). **Mitigação**: dois agregados/fluxos separados, prazos
   parametrizados por regime, e o subestado de saneamento (45d MROSC) explicitamente modelado.

3. **Dependência de integração Transferegov assumida como aberta.** A API existe, porém a
   maioria dos endpoints é **restrita a órgãos integrados** (exige convênio de integração);
   só os **dados abertos DTPAR** são livres. Planejar M9 contando com escrita/sincronização
   bidirecional sem esse acordo geraria retrabalho. **Mitigação**: na fase de integração,
   começar pela leitura de dados abertos (idempotente, atrás de ACL — §8), e tratar a
   integração transacional como item dependente de habilitação institucional [a confirmar].

---

## Pendências remanescentes (fonte primária a obter quando Planalto voltar)

1. **A6/A7** — texto do D11.531/2023 sobre prazo de PC do convenente e eventual % de 1º
   desembolso; checar também a **Portaria Conjunta MGI/MF/CGU nº 33/2023** (substituta da PI 424).
2. **B6** — confirmar se o art. 30 da L13019 fixa "180 dias" ou apenas "prazo do instrumento original".
3. **B15** — prazos exatos de publicidade dos arts. 10/11 da L13019.
4. **A8** — gatilho exato (30º dia?) da correção Selic em convênio federal sob o D11.531.

## Fontes consultadas nesta verificação
- L13019/2014 (Câmara legin): https://www2.camara.leg.br/legin/fed/lei/2014/lei-13019-31-julho-2014-779123-publicacaooriginal-144670-pl.html
- L13019 compilado (Planalto, ref.): https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2014/lei/l13019compilado.htm
- D11.531/2023 (gov.br/transferegov): https://www.gov.br/transferegov/pt-br/legislacao/decretos/decreto-no-11-531-de-16-de-maio-de-2023
- D11.531/2023 art. 21 (jusbrasil/anvisalegis): https://anvisalegis.datalegis.net/action/ActionDatalegis.php?acao=detalharAto&tipo=DEC&numeroAto=00011531&seqAto=000&valorAno=2023
- D11.531/2023 (síntese — PC contínua): https://www.contas.cnt.br/decreto-11-531-2023-convenios-e-contratos-de-repasse-relativos-as-transferencias-de-recursos-da-uniao/
- D11.271/2022 (gov.br/transferegov): https://www.gov.br/transferegov/pt-br/legislacao/decretos/decreto-no-11-271-de-5-de-dezembro-de-2022
- APIs Transferegov (gov.br): https://www.gov.br/transferegov/pt-br/sobre/apis-integracao
- Swagger API Transferegov: https://docs.api.transferegov.gestao.gov.br/fundoafundo/
- Dados abertos (dados.gov.br): https://dados.gov.br/dados/conjuntos-dados/transferencias-e-parcerias-da-uniao
- PI 424/2016 (revogada pela Portaria Conjunta MGI/MF/CGU nº 33/2023): https://www.gov.br/transferegov/pt-br/legislacao/portarias/portaria-interministerial-no-424-de-30-de-dezembro-de-2016
- TCU — Cartilha Transferências Voluntárias 2025 (8ª ed.): https://portal.tcu.gov.br/data/files/1D/52/DC/19/B69A591078006549E18818A8/Transferencias%20Voluntarias%20da%20Uniao%20-%20Oitava%20Edicao.pdf

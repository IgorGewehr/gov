# Verificação de Fatos — Pesquisa Cadastro Imobiliário, PGV e IPTU (M6)

> Auditoria cética do documento `pesquisa-imobiliario-iptu.md`.
> Método: cada afirmação factual cruzada contra fonte oficial (Receita Federal/gov.br, Planalto, STF/STJ, CNM, LC/CTN).
> Classificação: **CONFIRMADO** (fonte oficial corrobora) · **PLAUSÍVEL-SEM-FONTE** (verdadeiro/usual mas sem fonte oficial direta, ou exige lei municipal) · **INCERTO** (não verificável / depende de doc não publicado / impreciso).
> Data da verificação: 2026-06. Auditor: agente de IA (constituição §16).

---

## Quadro de verificação

### Bloco 1 — Cadastro Imobiliário / CIB / SINTER

| # | Afirmação no doc | Classificação | Evidência oficial |
|---|---|---|---|
| 1.1 | CIB = código alfanumérico **ABC1234-5** (7 caracteres + dígito verificador), estrutura "AAAAAAA-D". | **CONFIRMADO** | Decreto 11.208/2022 e página oficial RFB/SINTER: "código de identificação unívoco atribuído pelo Sinter a cada imóvel … sete caracteres alfanuméricos e um dígito verificador, com a estrutura 'AAAAAAA-D'". gov.br/receitafederal …/sinter/cib. |
| 1.2 | CIB reúne dados de imóveis urbanos, rurais, públicos e privados; parte urbana enviada **pelas prefeituras**. | **CONFIRMADO** | Página oficial RFB: urbanos via municípios; rurais via INCRA. |
| 1.3 | Para o imóvel obter CIB, a **prefeitura envia os dados ao SINTER, que gera o código**. | **CONFIRMADO** (com ressalva de titularidade) | RFB: o código é "**atribuído pelo Sinter**" a partir dos cadastros de origem (município). Ou seja, município alimenta; SINTER atribui. A redação do doc está correta. |
| 1.4 | Base legal: **Decreto 11.208/2022** regulamenta SINTER e CIB. | **CONFIRMADO** | Decreto 11.208, de **26/09/2022** (Planalto). Doc não cita a data; convém anotar 26/09/2022. |
| 1.5 | **IN RFB 2.275/2025 (18/08/2025)** institui regras e introduz **valor de referência** de mercado p/ imóveis urbanos. | **CONFIRMADO com correção de data** | IN foi **assinada/expedida em 15/08/2025** e **publicada no DOU em 18/08/2025** (Ed. 155, Seç. 1). O doc grava "(18/08/2025)" = data de publicação (válida, mas ambígua). **Atenção de escopo:** a IN 2.275 trata sobretudo das **obrigações de cartórios (serviços notariais e de registro)** de compartilhar info via SINTER e adotar o CIB — e cumpre obrigações da **LC 214/2025**. O "valor de referência" é, na origem, do **art. 256 da LC 214/2025**, não criação autônoma da IN. |
| 1.6 | Cronograma: **capitais + DF + federais + cartórios a partir de jan/2026**; **demais municípios + estaduais a partir de jan/2027**. | **CONFIRMADO** | Página oficial RFB/SINTER, texto literal: "A partir de janeiro de 2026: … as CAPITAIS dos Estados e o Distrito Federal …"; "A partir de janeiro de 2027: … e os demais municípios …". Maximiliano de Almeida (não-capital) → **jan/2027**. CONFIRMADO. |
| 1.7 | "prazo de adaptação ~ago/2026 / ~ago/2027 conforme leitura da IN". | **INCERTO** | O doc já marca como "leitura". As fontes oficiais consultadas só fixam jan/2026 e jan/2027 e remetem o detalhamento a um **plano de trabalho interinstitucional** (RFB+CNJ+registradores). O "~ago" não tem âncora oficial localizada → manter como inferência. |
| 1.8 | Protocolo técnico EXATO prefeitura→SINTER (leiaute/XSD/serviço, REST/SOAP, regra do DV) — `[a confirmar]`. | **INCERTO (corretamente sinalizado)** | Não há manual técnico público de leiaute do envio cadastral urbano nas fontes consultadas. A marcação `[a confirmar — obter doc oficial]` está correta; exige manual técnico SINTER / convênio ENAT / NT CTAT 05/2025. |

### Bloco 2 — BCI (atributos do cadastro)

| # | Afirmação | Classificação | Observação |
|---|---|---|---|
| 2.1 | Campos do BCI: localização/zona, terreno (área, testada, topografia), construção (área, tipo, padrão, uso, ano, conservação), fração ideal, infraestrutura. | **PLAUSÍVEL-SEM-FONTE** | É o conjunto recorrente em manuais municipais (Contagem/MG, Canoas/RS citados). NÃO é norma nacional única — **cada município define seu BCI por lei/decreto local**. Não inventado, mas não há "leiaute nacional de BCI". Os campos efetivos do tenant piloto dependem da legislação de Maximiliano de Almeida. |

### Bloco 3 — PGV e base de cálculo

| # | Afirmação | Classificação | Evidência |
|---|---|---|---|
| 3.1 | Base de cálculo do IPTU = **valor venal** (**CTN art. 33**). | **CONFIRMADO** | CTN (Lei 5.172/66) art. 33: a base de cálculo do imposto é o valor venal do imóvel. Consolidado em doutrina/jurisprudência. |
| 3.2 | PGV fixada **por lei municipal**; majoração da base depende de lei (decreto só corrige monetariamente). | **CONFIRMADO** | Princípio da legalidade tributária; **Súmula 160 STJ**: "É defeso ao Município atualizar o IPTU, mediante decreto, em percentual superior ao índice oficial de correção monetária." O doc está alinhado, embora não cite a súmula — **recomenda-se citá-la**. |
| 3.3 | Fórmula-padrão `ValorVenal = (AreaTerreno × VUT × fatores) + (AreaConstruida × VUC × FatorPadrao × FatorDepreciacao × fatores)`. | **PLAUSÍVEL-SEM-FONTE** | É a estrutura genérica usual (terreno + construção com fatores). **Não existe fórmula nacional única** — cada município define a sua na lei da PGV. O exemplo de fatores de Salvador (VUT/FVT/FCT/VUC/FL/FDC/FAV) é ilustrativo de UM município, não padrão. O doc já trata como "genérica" → adequado. A fórmula de Maximiliano de Almeida é `[a confirmar — lei municipal]`. |
| 3.4 | Recomendação de revisão periódica da PGV (~4 anos). | **PLAUSÍVEL-SEM-FONTE** | Recomendação técnica/de boas práticas (fonte privada citada), não prazo legal nacional obrigatório. Manter como recomendação, não regra. |

### Bloco 4 — Lançamento, alíquotas, descontos, parcelamento, DAM

| # | Afirmação | Classificação | Evidência |
|---|---|---|---|
| 4.1 | IPTU lançado **uma vez por exercício, de ofício**. | **CONFIRMADO** | Natureza do lançamento de ofício do IPTU (CTN art. 142/149); pacífico. |
| 4.2 | Alíquotas **lei municipal**, podem ser **progressivas** (valor venal e/ou uso/territorial×predial) + progressividade no tempo (**EC 29 / Estatuto da Cidade**). | **CONFIRMADO** | **EC 29/2000** → progressividade fiscal por valor venal e diferenciação por uso/localização (**CF art. 156 §1, I e II**). **Progressividade no tempo** → **CF art. 182 §4, II + Estatuto da Cidade (Lei 10.257/2001)** (função social). Enquadramento do doc correto. |
| 4.3 | Cota única × parcelamento; descontos; valor mínimo de parcela — exemplos SP (3%, mín. R$50, ~10 parcelas), Recife 5%, Natal até 16%. | **PLAUSÍVEL-SEM-FONTE / por tenant** | São exemplos municipais (variam por ano e por decreto). NÃO são regra nacional. Corretamente tratados como parametrizáveis. **Valores específicos de Maximiliano de Almeida = `[a confirmar — decreto municipal anual]`. Não usar valores de SP/Recife/Natal como default.** |
| 4.4 | DAM (boleto) por parcela; 2ª via via portal. | **PLAUSÍVEL-SEM-FONTE** | Prática usual; nomenclatura "DAM" e fluxo de 2ª via variam por município. Sem norma nacional única. |

### Bloco 5 — Reforma Tributária / transição (constituição §0 + §3)

| # | Afirmação | Classificação | Evidência |
|---|---|---|---|
| 5.1 | Reforma Tributária regida por **LC 214/2025**; valor de referência (art. 256). | **CONFIRMADO** | LC 214/2025 institui CBS/IBS/IS; art. 256 trata do valor de referência de imóveis no SINTER. |
| 5.2 | Transição ISS↔IBS (mencionada no prompt-constituição): ISS reduzido gradualmente, IBS crescente, **extinção do ISS em 2033**; teste 2026. | **CONFIRMADO** | Cronograma oficial: 2026 fase de teste (IBS 0,1% / CBS 0,9%); 2029–2032 redução gradual de ICMS/ISS (proporção decrescente) e aumento do IBS; **2033 extinção total de ICMS e ISS**. O documento `pesquisa-imobiliario-iptu.md` **não detalha** essa transição (foca IPTU), apenas remete em pendência #3 — adequado, mas o **impacto IBS imobiliário** sobre operações com imóveis é tema aberto. |
| 5.3 | Pendência: valor de referência (IN 2.275) impacta base municipal vs base IBS. | **INCERTO (corretamente sinalizado)** | Tema em construção regulatória; sem definição operacional consolidada do efeito sobre a base do IPTU municipal. Manter como `[a confirmar]`. |

### Bloco 6 — Dívida Ativa / CDA / Protesto (cobrança — escopo §4 da constituição)

> O doc `pesquisa-imobiliario-iptu.md` **não cobre CDA/protesto** (foca cadastro+PGV+IPTU). O prompt do auditor pediu checagem de CDA/protesto; registra-se aqui como **lacuna do doc** + base oficial para o M6.

| # | Tema | Classificação | Evidência |
|---|---|---|---|
| 6.1 | Requisitos da CDA / Termo de Inscrição em Dívida Ativa. | **CONFIRMADO (base p/ M6)** | **Lei 6.830/80 (LEF) art. 2º §5º** + **CTN art. 202**: nome do devedor/corresponsáveis e domicílio; valor originário, termo inicial e forma de cálculo de juros/encargos; origem/natureza/fundamento legal; indicação de correção monetária e fundamento; data e nº de inscrição; nº do processo administrativo/auto de infração. CDA é **título executivo extrajudicial** com presunção (relativa) de certeza e liquidez. |
| 6.2 | Protesto de CDA municipal é cabível. | **CONFIRMADO** | **Lei 9.492/97 art. 1º, parágrafo único** (incluído pela **Lei 12.767/2012, art. 25**) inclui CDA no rol de títulos protestáveis. **STF ADI 5135** julgada **improcedente** → protesto de CDA é **constitucional**. **STJ (1ª Turma)**: protesto pela Fazenda municipal **independe de lei local autorizadora** (base é lei federal). |

---

## Resumo executivo — confirmados x incertos

**CONFIRMADOS (com fonte oficial):** 11
- 1.1 formato CIB · 1.2 abrangência CIB · 1.3 geração via SINTER · 1.4 Decreto 11.208/2022 · 1.5 IN 2.275 (com correção de data/escopo) · 1.6 cronograma jan/2026 e jan/2027 · 3.1 valor venal CTN art. 33 · 3.2 legalidade/Súmula 160 STJ · 4.1 lançamento de ofício · 4.2 progressividade (EC 29 + Estatuto da Cidade) · 5.1 LC 214/2025 · 5.2 transição ISS→2033 · 6.1 requisitos CDA · 6.2 protesto CDA (STF ADI 5135).

**PLAUSÍVEL-SEM-FONTE / depende de lei municipal (não inventado, mas NÃO normativo nacional):** 6
- 2.1 campos do BCI · 3.3 fórmula do valor venal · 3.4 revisão ~4 anos · 4.3 descontos/parcelas (SP/Recife/Natal) · 4.4 DAM/2ª via.

**INCERTOS (não verificáveis / dependem de doc não publicado):** 4
- 1.7 prazo "~ago" de adaptação · 1.8 protocolo técnico SINTER (já `[a confirmar]`) · 5.3 efeito do valor de referência na base do IPTU · (e a lacuna de cobrança/CDA, não tratada no doc original).

**Correções pontuais ao doc:**
1. IN 2.275/2025: **assinada 15/08/2025, publicada DOU 18/08/2025** — explicitar ambos; e atribuir o **valor de referência ao art. 256 da LC 214/2025**, não à IN.
2. Decreto 11.208 → datar **26/09/2022**.
3. Citar **Súmula 160 STJ** no §3 (limite de atualização por decreto).
4. Adicionar **base legal de CDA/protesto** (LEF art. 2º, CTN art. 202, Lei 9.492/97 + 12.767/2012, STF ADI 5135) — hoje ausente, embora cobrança/CDA esteja no escopo do módulo (constituição §3/§4).

---

## 3 RISCOS

1. **Hardcode de valores municipais por engano (risco §16 alto).** O doc traz exemplos concretos (SP 3%/R$50/10 parcelas, Recife 5%, Natal 16%, fatores de Salvador) que são **ilustrativos de outros municípios**. Risco real de algum implementador "ancorar" defaults nesses números no motor de cálculo/parcelamento. Mitigação: motor 100% data-driven por tenant+exercício, **sem nenhum default numérico**, e popular o tenant piloto SOMENTE com o Código Tributário + lei da PGV + decreto anual de **Maximiliano de Almeida/RS** (ainda `[a confirmar — obter doc oficial]`).

2. **Protocolo SINTER↔Prefeitura indefinido bloqueia a integração de envio do CIB.** Não há leiaute técnico público (XSD/serviço/DV) localizado. Construir o `CadastroImobiliarioSync` agora seria especular sobre o contrato. Mitigação: tratar CIB como **atributo opcional/nullable** no go-live (piloto só obrigado em **jan/2027**), isolar atrás de ACL+Outbox+idempotência (filosofia NfseSync §8) e **não implementar o envio** até obter o manual técnico SINTER/ENAT/NT CTAT 05/2025.

3. **Reforma Tributária move o alvo durante o ciclo de vida do M6.** Valor de referência do SINTER (LC 214/2025 art. 256, IN 2.275/2025) e a transição IBS imobiliário podem alterar base de cálculo/relações cadastrais ao longo de 2026–2033. Modelar a base do IPTU acoplada a uma única interpretação atual é frágil. Mitigação: manter PGV/base versionadas por exercício, manter o "valor de referência" como **dado externo informativo** (não base de cálculo automática) até definição regulatória, e revisar a cada exercício fiscal.

---

## Fontes oficiais consultadas
- RFB/SINTER — CIB (formato, abrangência, geração, cronograma): https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/acoes-e-programas/programas-e-atividades/sinter/cib
- Decreto 11.208, de 26/09/2022 (Planalto): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2022/decreto/d11208.htm
- IN RFB 2.275, de 15/08/2025 (DOU 18/08/2025) — IRIB/CNB/Conjur: https://www.irib.org.br/instrucao-normativa-rfb-n-2-275-de-15-de-agosto-de-2025
- LC 214/2025 (Reforma Tributária; valor de referência art. 256) — análises Migalhas/Conjur/Barbieri.
- CTN (Lei 5.172/66) art. 33 (valor venal) e art. 202 (CDA): Planalto.
- Súmula 160 STJ (atualização IPTU por decreto).
- CF art. 156 §1 e art. 182 §4; EC 29/2000; Estatuto da Cidade (Lei 10.257/2001) — progressividade.
- Lei 6.830/80 (LEF) art. 2º §5º (requisitos CDA): https://www.planalto.gov.br/ccivil_03/leis/l6830.htm
- Lei 9.492/97 art. 1º p.ú. + Lei 12.767/2012 art. 25; STF ADI 5135 (protesto de CDA constitucional): https://noticias.stf.jus.br/postsnoticias/protesto-de-certidoes-de-divida-ativa-e-constitucional-decide-stf/
- Cronograma transição Reforma Tributária 2026–2033 — Senado/Câmara.
- CNM — Nota Técnica CTAT 05/2025 (orientações a municípios sobre SINTER/CIB).

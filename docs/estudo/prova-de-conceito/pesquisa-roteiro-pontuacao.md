# Prova de Conceito (POC) em licitações de software de gestão pública — roteiro de demonstração, pontuação/avaliação e armadilhas

> Estudo para preparar a Tensorroot.Gov para provas de conceito/aderência (Lei 14.133/2021).
> Regra de citação: toda afirmação tem FONTE (URL/edital/acórdão) ou está marcada **[a confirmar]**.
> Data da pesquisa: 2026-06-22.

---

## 1. Como os editais estruturam o ROTEIRO de demonstração

### 1.1 Formato dominante: planilha/quadro de requisitos com coluna de aferição

O formato real recorrente é um **quadro ("Amostra do objeto" / "Roteiro de Requisitos Funcionais")** com colunas:
`# | CÓD | CATEGORIA/MÓDULO | REQUISITO FUNCIONAL | (NATIVO) | ATENDE | NÃO ATENDE`.

Dois modelos de coluna de aferição aparecem na prática:

- **Binário "Atende (S/N)"** — usado no Relatório de POC do Pregão Eletrônico 90056/2024 do IFB (sistema de bibliotecas), com 45+ requisitos agrupados por categoria (Características Gerais, Terminal de Consulta, Integração, Processamento Técnico, Aquisição, Relatórios, Inventário, Circulação, Migração). Cada linha recebeu "Sim".
  FONTE: https://ifb.edu.br/attachments/article/39100/Relat%C3%B3rio%20da%20Prova%20de%20Conceito%20-%20Preg%C3%A3o%20Eletr%C3%B4nico%20N%C2%B0%2090056_2024.pdf
- **Binário "ATENDE / NÃO ATENDE" + coluna "NATIVO"** — Roteiro da POC da SEFAZ-MS (Pregão Eletrônico 0022/2022, sistema de cobrança/gestão tributária), 26 requisitos funcionais codificados (RF.1.01 … RF.14.03) agrupados em 14 categorias. Coluna "NATIVO" marcada com "X" para indicar que o requisito deve ser demonstrado de forma nativa.
  FONTE: https://www.sefaz.ms.gov.br/wp-content/uploads/2023/05/Convocacao-e-Roteiro-da-POC.pdf

A terceira opção ("atende parcialmente") é **comum em planilhas de habilitação técnica de propostas**, mas no momento da POC propriamente dita a tendência dos bons editais é o **julgamento binário sem gradação** (ver 2.1). O TCU/doutrina alerta que "atende parcialmente" sem critério objetivo gera subjetividade impugnável.
FONTE: https://www.conjur.com.br/2024-jan-15/prova-de-conceito-em-licitacoes-do-teorico-ao-factivel/

### 1.2 Exigência de demonstração NATIVA (sem customização)

Cláusula crítica e recorrente (SEFAZ-MS, itens 11.5.2.4 a 11.5.2.6):
> "Todos os requisitos constantes no roteiro devem ter sua correta implementação comprovada, e devem estar disponíveis de maneira nativa nos componentes que integram a solução. (...) o atendimento de qualquer requisito do roteiro não deve depender da necessidade de customização por meio de linguagem de programação e/ou alteração de estrutura de base de dados, sendo admitida apenas a parametrização de funcionalidades disponíveis na versão original do produto ofertado."
FONTE: SEFAZ-MS Roteiro POC (acima).

> **Implicação para a Tensorroot.Gov:** o que for demonstrado tem de existir na build, não pode ser "vamos codar na implantação". Parametrização (config) é aceita; programação na hora, não.

### 1.3 Ordem de apresentação

O licitante normalmente pode escolher a ordem: "O Roteiro de apresentação dos Requisitos funcionais não necessita seguir a ordem apresentada, sendo que a Licitante poderá verificar a melhor forma de demonstração dos itens." (SEFAZ-MS).
FONTE: SEFAZ-MS Roteiro POC.

---

## 2. PONTUAÇÃO / critério de aprovação

### 2.1 Julgamento binário por item, sem notas/pesos

SEFAZ-MS, item 11.5.2.6:
> "Qualquer item do roteiro (...) será aprovado ou reprovado integralmente, não havendo notas/pesos, aprovação com ressalvas ou qualquer outro tipo de gradação."
FONTE: SEFAZ-MS Roteiro POC.

### 2.2 Distinção requisitos TÉCNICOS (100%) vs. FUNCIONAIS (tolerância)

Modelo SEFAZ-MS (referência forte de como separar):
- **Requisitos técnicos:** "100% dos requisitos técnicos avaliados devem ser atendidos integralmente." (item 11.5.2.8).
- **Requisitos funcionais:** tolerância de **até 5%** — "Aceita-se que sejam necessários ajustes pontuais em até 5% dos requisitos funcionais presentes no roteiro (...) contanto que já tenham sido implementados parcialmente na solução e que possam passar a ser atendidos plenamente por meio de customizações realizadas durante a implantação da solução sem implicação de qualquer custo adicional." (item 11.5.2.7).
- **Desclassificação:** "A proposta da licitante será desclassificada caso o limite percentual mencionado em 11.5.2.7 seja excedido para os requisitos funcionais ou se qualquer item do roteiro referente a requisito técnico seja reprovado." (item 11.5.2.9).
FONTE: SEFAZ-MS Roteiro POC.

### 2.3 Modelos de "obrigatórios + desejáveis"

Há editais municipais que adotam **100% dos obrigatórios + X% dos desejáveis** (ex.: padrão citado de "100% dos itens obrigatórios e 80% dos desejáveis"). Esse percentual de desejáveis varia por edital. **[a confirmar com edital nominado]** — o número 80% apareceu em síntese de busca mas precisa de edital fonte específico antes de tratar como benchmark; busca direta não retornou o edital-fonte.
FONTE (síntese, sem edital nominado): resultados de busca PNCP/portais; **[a confirmar]**.

### 2.4 Polêmica sobre o percentual mínimo

Há tensão real entre dois entendimentos:
- **100% é defensável** porque qualquer percentual abaixo de 100% sem critério objetivo de quais itens podem faltar vira subjetivismo (vedado). A proposta deve estar "de acordo com as especificações do edital".
- **Órgãos de controle (TCE-PR, TCE-RS) já questionaram exigências entre 80% e 100%** como potencialmente excessivas/direcionadoras e suspenderam licitações por falta de critérios objetivos.
FONTE: https://gestgov.discourse.group/t/ausencia-de-percentual-minimo-de-aceitabilidade-de-sistema-em-prova-de-conceito/25339
> Conclusão prática: o que protege o edital não é o número em si, mas **listar objetivamente quais itens são obrigatórios (100%) e quais admitem tolerância**, com o percentual e a regra de cálculo explícitos. O modelo SEFAZ-MS (técnicos 100% + funcionais ≤5% de ajuste) é o mais robusto que encontrei.

---

## 3. TEMPO por requisito e por sessão

- **Prazo total da POC (convocação→execução):** modelos usam **até 15 dias** distribuídos entre entrega, preparação e execução (IFB, item 7.21).
  FONTE: IFB Relatório POC.
- **Convocação:** comumente **2 dias úteis** após a convocação do vencedor provisório para iniciar a execução presencial **[a confirmar com edital nominado para o "2 dias úteis"]** (apareceu em síntese; SEFAZ-MS convocou com ~10 dias de antecedência, de 25/05 para 05/06/2023).
  FONTE convocação SEFAZ-MS: SEFAZ-MS Roteiro POC.
- **Duração da sessão de demonstração (exemplo real medido):** IFB Pregão 90056/2024 — **início 12/09/2024 às 10h, término às 14h40** ≈ **4h40 para ~45 requisitos** ≈ ~6 min/requisito em média.
  FONTE: IFB Relatório POC.
- **Tempo formal por requisito:** raramente fixado por requisito; quando há, é tempo total de sessão. **[a confirmar]** se algum edital municipal de ERP fixa minutos por item.

> ATENÇÃO TCU: prazo exíguo (ex.: 48h) para preparar amostra **restringe competição** — Acórdão 6638/2015. Logo, prazos curtos protegem incumbentes; o desafiante deve exigir prazo razoável via impugnação.
> FONTE: https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/

---

## 4. AMBIENTE de demonstração e dados de teste

- **Presencial na sede do órgão** é o padrão dominante (SEFAZ-MS: COTIN, Campo Grande/MS, data e horário fixados em convocação publicada no DOE).
  FONTE: SEFAZ-MS Roteiro POC.
- **Remoto/gravado é aceito**: IFB realizou a POC por **Conferência Web RNP, gravada**.
  FONTE: IFB Relatório POC.
- **Dados/ambiente:** o licitante apresenta a própria solução; em sistemas estruturantes pode haver exigência de carga de bases reais/extrações (SEFAZ-MS RF.1.01 testa "carga e gerenciamento de bases cadastrais por meio de extrações e integrações com os sistemas estruturantes"). **Dados fictícios/de teste** são a norma para a demonstração, mas o uso de **dados/massa fornecidos pelo órgão** aparece quando o requisito é migração/integração. **[a confirmar]** quanto a editais municipais de ERP que entregam massa de dados padronizada ao licitante.
- **Acompanhamento dos demais licitantes:** deve ser viabilizado mediante registro formal junto ao pregoeiro (IFB, item 7.23). Negar acompanhamento é ilegal (TCU — ver 6.5).
  FONTE: IFB Relatório POC; TCU.

---

## 5. RESULTADO, RECURSOS e impugnações

### 5.1 Fluxo de resultado (modelo IFB, itens 7.24–7.32)
- Equipe técnica designada elabora **relatório** dizendo se a solução do 1º colocado **está ou não de acordo** (7.24–7.25).
- Conforme → licitante **declarado vencedor**; não conforme → **desclassificado** (7.26).
- **Aprovação com ressalva possível**: se possui todas as funcionalidades mas apresentou falha durante o teste, "o licitante terá prazo de **3 dias úteis** (...) para proceder aos ajustes necessários (...) para aferição da correção" (7.27–7.28). Novo relatório aponta conformidade ou desclassifica (7.29).
- POC **rejeitada, não realizada ou fora das condições do TR** → proposta não aceita (7.30).
- Desclassificado o 1º → convoca o próximo, sucessivamente (7.31).
- Resultados divulgados por mensagem no sistema (7.32).
FONTE: IFB Relatório POC.

> Nota: o modelo IFB ADMITE ressalva+correção em 3 dias; o modelo SEFAZ-MS NÃO admite ressalva (binário puro). **A regra muda por edital — ler sempre a cláusula específica.**

### 5.2 Recursos/impugnações disponíveis ao fornecedor
- **Impugnação do edital** (antes da abertura) por critérios indefinidos/subjetivos, prazo exíguo, ou POC facultativa.
- **Recurso administrativo** contra desclassificação fundamentado em critério não especificado no edital.
- **Representação/denúncia ao TCU** (art. 5º, Lei 14.133) ou ao TCE-RS por violação de princípios.
FONTE: TCU Licitações e Contratos (5.4.1.2).

---

## 6. ARMADILHAS que reprovam fornecedores ou anulam a POC

### 6.1 Que reprovam O FORNECEDOR (cuidado nosso na prova)
1. **Depender de customização/código na hora** — requisito tem de ser nativo/parametrizável (SEFAZ-MS 11.5.2.5–11.5.2.6).
2. **Estourar a tolerância de funcionais** (>5% no modelo SEFAZ-MS) ou **falhar 1 requisito técnico** → desclassificação imediata (11.5.2.9).
3. **Falha durante o teste** — em editais binários puros, reprova; em editais com ressalva, gera prazo de 3 dias (IFB 7.27).
4. **Não comparecer / não realizar nas condições do TR** → proposta não aceita (IFB 7.30).
5. **Demonstrar tela/módulo diferente do ofertado na proposta** — TCU veda aceitar produto diferente da amostra (Acórdão 2611/2016).
FONTES: SEFAZ-MS; IFB; TCU.

### 6.2 Que ANULAM/viciam a POC (a favor do desafiante, base p/ impugnação)
- **POC facultativa** ou **sem especificar os pontos avaliados** → ilegal: Acórdãos 3355/2024 e 2992/2016.
- **Critérios vagos** ("todos os testes necessários") → fere o julgamento objetivo: Acórdão 529/2018.
- **Exigir POC de vários licitantes** (não só do 1º provisório) → fere isonomia/eleva custos: Acórdãos 2640/2019, 2096/2015.
- **Sem data/horário/local definidos** → afronta publicidade: Acórdão 2796/2013.
- **Não divulgar resultado** ou **não permitir acompanhamento** dos demais → Acórdãos 2401/2019, 1823/2017.
- **Prazo exíguo** (ex.: 48h) → restringe competição: Acórdão 6638/2015.
- **Dispensar amostra prevista no edital** → vedado: Acórdão 1948/2019.
FONTE: https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/

### 6.3 Base legal e jurisprudencial geral
- Lei 14.133/2021, art. 41, parágrafo único (amostra/POC só do licitante provisoriamente classificado em 1º) e art. 17 (fase de julgamento).
- IN SEGES/ME 73/2022 (referenciada pela doutrina como norma de amostras/POC).
- Acórdão TCU 1.984 (definição de POC como comprovação pelo 1º colocado).
- Acórdão TCU 387/2024 (permite inversão de fases com justificativa).
FONTES: TCU (acima); https://justen.com.br/artigo_pdf_2/a-prova-de-conceito-poc-a-luz-da-eficiencia-e-da-racionalidade-administrativa/

---

## 7. Composição da COMISSÃO avaliadora (exemplos reais)
- **SEFAZ-MS:** Comissão de Avaliação da POC com **6 servidores** nominados por matrícula (técnicos de TI/tributação).
  FONTE: SEFAZ-MS Roteiro POC.
- **IFB:** "Equipe de Apoio" + "Equipe Técnica" (bibliotecárias + analista de TI + coordenador), relatório assinado eletronicamente por 6 membros.
  FONTE: IFB Relatório POC.

---

## 8. Implicações diretas para a Tensorroot.Gov

1. **Tudo demonstrado tem de ser nativo na build** — não prometer "codamos na implantação". Mapear cada requisito de edital para tela/funcionalidade existente antes de aceitar a POC.
2. **Separar mentalmente "técnico" (100%, zero falha) de "funcional" (tolerância ~5% só se já parcialmente implementado)** — é onde se ganha ou se perde.
3. **Ensaiar a sessão cronometrada** (~5–7 min/requisito; sessão de ~4–5h para ~45 itens) e poder **escolher a ordem** de demonstração.
4. **Multi-tenant Executivo/Legislativo:** confirmar que a massa de dados de teste cobre os dois tenants distintos sem vazamento de escopo (ABAC/UO) — isso pode virar requisito técnico de "atende/não atende".
5. **Preparar dossiê de impugnação preventiva** contra editais com armadilhas pró-incumbente (prazo 48h, critério vago, POC facultativa) usando os acórdãos do item 6.2.

---

## 9. Pendências [a confirmar]
- [ ] Edital MUNICIPAL nominado de ERP de gestão pública (PNCP) que fixe explicitamente "100% obrigatórios + 80% desejáveis" — número 80% ainda sem edital-fonte.
- [ ] Algum edital que fixe TEMPO MÁXIMO POR REQUISITO (minutos/item).
- [ ] Editais que entreguem MASSA DE DADOS padronizada ao licitante para a POC (vs. licitante traz a própria base).
- [ ] Texto integral da IN SEGES/ME 73/2022 sobre amostras/POC (citada por terceiros, não lida na fonte).
- [ ] Edital de CÂMARA MUNICIPAL (tenant Legislativo) com roteiro de POC — todos os exemplos achados são Executivo/órgão federal.
- [ ] Anexo VIII CISMEL (critérios de avaliação POC com pontuação) — PDF não decodificado nesta rodada: https://cismel.pr.gov.br/wp-content/uploads/2024/01/3-ANEXO-VIII-Criterios-Para-Avaliacao-da-Prova-de-Conceito-POC-VIDEOMONITORAMENTO.pdf

---

## Fontes principais (URLs)
- TCU — Amostra e prova de conceito (acórdãos, regras): https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/
- SEFAZ-MS — Convocação e Roteiro da POC (Pregão 0022/2022), modelo de planilha + critério 100% técnico / ≤5% funcional: https://www.sefaz.ms.gov.br/wp-content/uploads/2023/05/Convocacao-e-Roteiro-da-POC.pdf
- IFB — Relatório da POC (Pregão 90056/2024), execução real, tempos e fluxo de resultado: https://ifb.edu.br/attachments/article/39100/Relat%C3%B3rio%20da%20Prova%20de%20Conceito%20-%20Preg%C3%A3o%20Eletr%C3%B4nico%20N%C2%B0%2090056_2024.pdf
- ConJur — Prova de conceito do teórico ao factível: https://www.conjur.com.br/2024-jan-15/prova-de-conceito-em-licitacoes-do-teorico-ao-factivel/
- GestGov/NELCA — Ausência de percentual mínimo de aceitabilidade na POC: https://gestgov.discourse.group/t/ausencia-de-percentual-minimo-de-aceitabilidade-de-sistema-em-prova-de-conceito/25339
- Justen Filho — A POC à luz da eficiência: https://justen.com.br/artigo_pdf_2/a-prova-de-conceito-poc-a-luz-da-eficiencia-e-da-racionalidade-administrativa/
- Pato Branco/PR — Edital software gestão de saúde pública (amostra): https://patobranco.pr.gov.br/wp-content/uploads/2023/04/41-SOFTWARE-GESTAO-DE-SAUDE-PUBLICA.pdf
- CISMEL/PR — Anexo VIII critérios de avaliação POC [a confirmar]: https://cismel.pr.gov.br/wp-content/uploads/2024/01/3-ANEXO-VIII-Criterios-Para-Avaliacao-da-Prova-de-Conceito-POC-VIDEOMONITORAMENTO.pdf

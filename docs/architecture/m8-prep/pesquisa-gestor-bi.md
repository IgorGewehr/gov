# M8 — Pesquisa: Portal do Gestor (painel executivo) + BI por área

Status: pesquisa de produto. Foco menos normativo, mais produto. Regra §16: nada inventado — cada achado tem FONTE ou marcação **[a confirmar]**.
Data: 2026-06-22.

---

## 1. Objetivo do produto

Painel executivo (Portal do Gestor) que consome os dados dos módulos já existentes do ERP (tributos, protocolo, finanças/contabilidade, RH, saúde/educação/assistência) e entrega:
- Visão executiva única ("estou no rumo certo?") para o Prefeito/Secretários.
- Dashboards por secretaria (drill-down).
- Indicadores de gestão: execução orçamentária, arrecadação, mínimos constitucionais, RH/pessoal.
- Alertas de prazos e de limites (mínimos constitucionais, despesa com pessoal, publicação de relatórios fiscais).

Princípio de produto: o gestor não deve "calcular" — o sistema já calcula sobre os dados dos módulos e mostra status semáforo (verde/amarelo/vermelho) + tendência.

---

## 2. Indicadores de gestão — catálogo (com base legal de cálculo)

### 2.1 Mínimos constitucionais (os 2 indicadores mais críticos do painel)

| Indicador | Mínimo | Base de cálculo | Fundamento |
|---|---|---|---|
| **Educação (MDE)** | **25%** da receita de impostos + transferências | Receita resultante de impostos próprios + transferências | CF art. 212; municípios devem aplicar no mínimo 25% |
| **FUNDEB — remuneração** | **60%** do FUNDEB em remuneração de profissionais da educação básica | Recursos do FUNDEB recebidos | Exigência constitucional (Novo FUNDEB) |
| **Saúde (ASPS)** | **15%** da receita de impostos + transferências (municípios) | CF art. 198 §2º; regulamentado pela LC 141/2012 (EC 29) | **[a confirmar o número exato 15% na fonte primária — CONASS/LC 141]** |

FONTES:
- Mínimos educação (25%, CF art. 212) e papel do FUNDEB (60% remuneração): https://todospelaeducacao.org.br/noticias/municipios-devem-gastar-no-minimo-25-dos-seus-orcamentos-com-educacao/ e https://apet.org.br/artigos/o-fundeb-e-os-minimos-constitucionais-da-educacao/
- Base de cálculo / fiscalização por TCs e MP: MEC, conforme citado em https://todospelaeducacao.org.br/noticias/municipios-devem-gastar-no-minimo-25-dos-seus-orcamentos-com-educacao/
- Saúde — CF art. 198 §2º e art. 212 educação (limites constitucionais): https://www.gov.br/cgu/pt-br/assuntos/auditoria-e-fiscalizacao/avaliacao-da-gestao-dos-administradores/prestacao-de-contas-do-presidente-da-republica/arquivos/2009/34.pdf
- Aplicação de recursos em saúde (CONASS): https://www.conass.org.br/guiainformacao/aplicacao-de-recursos-em-acoes-e-dos-publicos-de-saude/

Sistemas de origem dos dados (para integração/benchmark): **SIOPE** (educação) e **SIOPS** (saúde) — os indicadores do painel devem reconciliar com o que será declarado nesses sistemas. **[a confirmar nomes/escopo SIOPE/SIOPS na fonte oficial FNDE/MS]**

### 2.2 Despesa com pessoal (LRF) — alertas escalonados

| Faixa | % da RCL (Executivo municipal) | Ação no painel |
|---|---|---|
| Limite legal Executivo | **54%** da RCL (teto global do município = 60%; 6% Legislativo) | linha de referência |
| Limite prudencial | **95% de 54% = 51,3%** da RCL | alerta AMARELO — bloqueia aumento/contratação |
| Limite de alerta (TC) | **90%** do limite legal | alerta — Tribunal de Contas notifica o gestor |

Consequência de ultrapassar o prudencial: vedado conceder vantagens, aumentos, reajustes, contratar pessoal (salvo decisão judicial/legal/contratual).

FONTES:
- Teto 60% RCL (54% Executivo / 6% Legislativo), prudencial 95% e alerta 90%: https://www.politize.com.br/limites-da-despesa-com-pessoal/
- Alteração de regras de cômputo pela LC 178/2021 (atenção a novas regras de apuração): https://cnm.org.br/comunicacao/noticias/atencao-novas-regras-e-prazos-para-o-computo-do-limite-de-gastos-de-pessoal-da-lrf
- TCs alertam municípios acima dos limites e por não atingimento de metas de arrecadação (caso de uso real do alerta): https://www.tcesc.tc.br/tcesc-alerta-municipios-com-relacao-despesas-com-pessoal-acima-dos-limites-e-nao-atingimento-de

### 2.3 Execução orçamentária

- KPI base: **% do orçamento executado vs. planejado** (LOA) — indica capacidade de planejamento e execução. FONTE: https://aprova.com.br/blog/indicadores-de-gestao-municipal
- Decompor por: empenhado / liquidado / pago; por função e subfunção; por secretaria/UO.
- Reconciliar com os anexos do RREO (ver §3) — Anexo 2 = execução por função/subfunção; Anexo 3 = Receita Corrente Líquida.

### 2.4 Arrecadação (consome módulo Tributos)

- KPI: arrecadação realizada vs. meta (a meta de arrecadação é objeto de alerta dos TCs quando não atingida — FONTE TCE-SC acima).
- Decompor por tributo (IPTU, ISS, ITBI, taxas), por situação (a vencer/vencido/dívida ativa), e evolução mensal.

### 2.5 Índice-síntese de gestão — IEG-M (benchmark e modelo de dimensões)

Modelo de referência pronto para o "Portal do Gestor por área": o **IEG-M** (Índice de Efetividade da Gestão Municipal, TCESP, criado 2015) avalia 7 dimensões — alinhar os dashboards por secretaria a elas:
- **i-Plan** (planejamento), **i-Fiscal** (gestão fiscal), **i-Educ** (educação), **i-Saúde** (saúde), **i-Amb** (meio ambiente), **i-Cidade** (proteção ao cidadão/Defesa Civil), **i-Gov TI** (governança de TI).

Útil também como benchmarking: o **IEGM Brasil** (IRB) reúne dados de múltiplos estados → permite comparar o município com pares.

FONTES:
- Dimensões e definição IEG-M: https://www.iegm.tce.sp.gov.br/ e https://om30.com.br/ieg-m-e-desempenho-municipal-o-que-os-indicadores-revelam-sobre-a-sua-gestao/
- Manual oficial IEG-M (metodologia de cálculo por dimensão — usar para parametrizar os KPIs): https://www.tce.sp.gov.br/publicacoes/manual-ieg-m-2026
- IEGM Brasil (benchmark multiestados): https://www.iegm.irbcontas.org.br/
- Guia de indicadores para gestão pública (catálogo amplo de indicadores temáticos): https://www.cidadessustentaveis.org.br/arquivos/Publicacoes/Guia_de_Indicadores_para_a_Gestao_Publica.pdf

---

## 3. Alertas de prazos — calendário fiscal embutido

O painel deve ter um "calendário de obrigações" com contagem regressiva e status. Prazos oficiais confirmados:

| Relatório | Periodicidade | Prazo de publicação | Exceção municípios pequenos |
|---|---|---|---|
| **RREO** (Relatório Resumido da Execução Orçamentária) | Bimestral | **até 30 dias após o fim do bimestre** | Municípios < 50 mil hab. podem optar por divulgação semestral de parte dos demonstrativos |
| **RGF** (Relatório de Gestão Fiscal) | Quadrimestral | **até 30 dias após o fim do quadrimestre** | Municípios < 50 mil hab. podem optar por periodicidade semestral |

Anexos a monitorar:
- RREO: Anexo 1 (Balanço Orçamentário), Anexo 2 (Execução por Função/Subfunção), Anexo 3 (RCL), Anexo 4 (Receitas/Despesas Previdenciárias).
- RGF: Anexo 1 (Despesa com Pessoal), Anexo 2 (Dívida Consolidada Líquida), Anexo 3 (Garantias), Anexo 4 (Operações de Crédito), Anexo 6 (Demonstrativo Simplificado do RGF).

FONTES:
- Prazos RREO (CF art. 165 §3º) e RGF, e exceção < 50 mil hab.: https://www.gov.br/sri/pt-br/backup-secretaria-de-governo/portalfederativo/guiainicio/prefeito/trilhas-100-dias-de-governo/rgf-e-rreo
- RREO União / definição e periodicidade (Tesouro Transparente): https://www.tesourotransparente.gov.br/temas/contabilidade-e-custos/relatorio-resumido-da-execucao-orcamentaria-rreo-uniao
- Estrutura dos anexos RREO/RGF e calendário quadrimestral: https://www.estrategiaconcursos.com.br/blog/entenda-o-relatorio-resumido-de-execucao-orcamentaria-e-o-relatorio-de-gestao-fiscal/
- Demonstrativos fiscais oficiais (STN): http://www.tesouro.fazenda.gov.br/demonstrativos-fiscais

Outros prazos a embutir no calendário **[a confirmar datas exatas em fonte oficial Siconfi/TCE-RS]**:
- DCA / Matriz de Saldos Contábeis (MSC) — Siconfi.
- Prestação de contas anual ao TCE-RS.
- Declarações SIOPE/SIOPS (educação/saúde).
- Penalidades por não publicação dos relatórios LRF — **[a confirmar: LC 101/2000 art. 51 §2º e art. 73 / suspensão de transferências voluntárias e vedação de operações de crédito]**.

---

## 4. Boas práticas de BI / design do painel executivo

Aplicar sobre os dados dos módulos (não sobre planilhas manuais):

1. **Regra dos 5 segundos** — a mensagem principal ("estamos no rumo?") deve ser captada em até 5s. FONTE: https://www.datacamp.com/tutorial/dashboard-design-tutorial
2. **5–7 KPIs primários por tela**; o resto vai para drill-down/visões secundárias (evita sobrecarga). FONTE: https://www.clearpointstrategy.com/blog/kpi-dashboard-best-practices
3. **Progressive disclosure / drill-down**: headline → secretaria → iniciativa/conta. Executivo vê o agregado; secretário/analista clica e abre o detalhe. FONTE: https://www.thoughtspot.com/data-trends/dashboard-design-examples-best-practices
4. **Cor = status, não decoração**: verde/amarelo/vermelho consistentes (on-track / em risco / fora da meta) — casa perfeitamente com os limites legais (prudencial, mínimos). FONTE: https://www.clearpointstrategy.com/blog/kpi-dashboard-best-practices
5. **Top-rail layout**: navegação + filtros + KPIs no topo; gráficos no corpo. Ideal quando a 1ª pergunta é "estamos no rumo?". FONTE: https://www.datacamp.com/tutorial/dashboard-design-tutorial
6. **Contexto em cada gráfico**: descrição + tendência histórica + benchmark regional (IEGM Brasil) — número vira insight, não dado cru. FONTE: https://envisio.com/blog/8-local-government-public-dashboard-examples/
7. **Balanced Scorecard** para mapear KPIs operacionais a objetivos estratégicos por secretaria (cada métrica "explica por que importa"). FONTE: https://www.thoughtspot.com/data-trends/dashboard-design-examples-best-practices
8. **Atualização consistente e automática** a partir dos sistemas integrados (reduz erro humano, garante consistência). FONTE: https://aprova.com.br/blog/indicadores-de-gestao-municipal e exemplo Orçamentômetro (dados atualizados diariamente): https://orcamentometro.agenciamural.org.br/

---

## 5. Arquitetura de dados sugerida (a validar com módulos existentes)

- **Camada semântica / data marts** por área (orçamento, arrecadação, pessoal, saúde, educação), alimentada pelos módulos do ERP. **[a confirmar tecnologia de BI a adotar no stack do projeto]**
- **Motor de regras de limites** parametrizável (25% MDE, 60% FUNDEB, 15% saúde, 54%/51,3%/90% pessoal) → gera status semáforo + alertas.
- **Motor de calendário fiscal** (RREO 30d/bimestre, RGF 30d/quadrimestre, Siconfi, TCE-RS, SIOPE/SIOPS) → contagem regressiva + notificações.
- Reconciliação obrigatória: indicadores do painel ↔ anexos RREO/RGF ↔ declarações SIOPE/SIOPS (consistência com o que é oficialmente declarado).

---

## 6. Pendências / a confirmar antes do dev

- [ ] Saúde: confirmar **15%** municípios em fonte primária (LC 141/2012, art. 7º) e base de cálculo exata.
- [ ] SIOPE/SIOPS: escopo, periodicidade e formato de integração (FNDE / Ministério da Saúde).
- [ ] Penalidades por descumprimento de prazos LRF (LC 101/2000, arts. 51 e 73) — fonte primária Planalto.
- [ ] Regras de cômputo de despesa com pessoal pós **LC 178/2021** (mudou apuração) — impacta o cálculo do indicador.
- [ ] Tecnologia de BI/visualização do stack Tensorroot.Gov (componente próprio vs. embedding).
- [ ] Calendário exato de prestação de contas anual ao TCE-RS e prazos Siconfi (DCA/MSC).
- [ ] Definir conjunto final de dimensões por secretaria (adotar/adaptar as 7 do IEG-M).

---

## Fontes (consolidado)

- CF art. 212 / 25% educação: https://todospelaeducacao.org.br/noticias/municipios-devem-gastar-no-minimo-25-dos-seus-orcamentos-com-educacao/
- FUNDEB / mínimos educação: https://apet.org.br/artigos/o-fundeb-e-os-minimos-constitucionais-da-educacao/
- Limites constitucionais saúde/educação (CGU): https://www.gov.br/cgu/pt-br/assuntos/auditoria-e-fiscalizacao/avaliacao-da-gestao-dos-administradores/prestacao-de-contas-do-presidente-da-republica/arquivos/2009/34.pdf
- Aplicação de recursos em saúde (CONASS): https://www.conass.org.br/guiainformacao/aplicacao-de-recursos-em-acoes-e-dos-publicos-de-saude/
- Despesa com pessoal LRF (limites/prudencial/alerta): https://www.politize.com.br/limites-da-despesa-com-pessoal/
- Novas regras LC 178/2021: https://cnm.org.br/comunicacao/noticias/atencao-novas-regras-e-prazos-para-o-computo-do-limite-de-gastos-de-pessoal-da-lrf
- Alerta TC despesa pessoal + meta arrecadação: https://www.tcesc.tc.br/tcesc-alerta-municipios-com-relacao-despesas-com-pessoal-acima-dos-limites-e-nao-atingimento-de
- RREO/RGF prazos (SRI/gov.br): https://www.gov.br/sri/pt-br/backup-secretaria-de-governo/portalfederativo/guiainicio/prefeito/trilhas-100-dias-de-governo/rgf-e-rreo
- RREO (Tesouro Transparente): https://www.tesourotransparente.gov.br/temas/contabilidade-e-custos/relatorio-resumido-da-execucao-orcamentaria-rreo-uniao
- Demonstrativos fiscais (STN): http://www.tesouro.fazenda.gov.br/demonstrativos-fiscais
- IEG-M (TCESP): https://www.iegm.tce.sp.gov.br/ — Manual: https://www.tce.sp.gov.br/publicacoes/manual-ieg-m-2026
- IEGM Brasil (IRB, benchmark): https://www.iegm.irbcontas.org.br/
- Indicadores de gestão municipal: https://aprova.com.br/blog/indicadores-de-gestao-municipal
- Guia de indicadores gestão pública: https://www.cidadessustentaveis.org.br/arquivos/Publicacoes/Guia_de_Indicadores_para_a_Gestao_Publica.pdf
- BI/dashboard best practices: https://www.datacamp.com/tutorial/dashboard-design-tutorial · https://www.clearpointstrategy.com/blog/kpi-dashboard-best-practices · https://www.thoughtspot.com/data-trends/dashboard-design-examples-best-practices
- Dashboards de governo local: https://envisio.com/blog/8-local-government-public-dashboard-examples/
- Orçamentômetro (exemplo de execução orçamentária pública): https://orcamentometro.agenciamural.org.br/

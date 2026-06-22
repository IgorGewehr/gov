# M9 — Pesquisa: Convênios e Terceiro Setor (Transferegov.br + MROSC)

Escopo: o que o **município** envia/registra e quando, em duas frentes:
(A) prestação de contas de convênios/transferências federais via Transferegov.br;
(B) parcerias com OSCs sob o MROSC (Lei 13.019/2014 — termo de fomento/colaboração).

Regra §16: cada afirmação tem FONTE oficial ou marca [a confirmar].

---

## A) TRANSFEREGOV.br — Convênios e transferências da União

### Base legal
- Plataforma única para operacionalizar transferências de recursos do Orçamento Fiscal e da Seguridade Social da União a entes (incl. municípios), consórcios e entidades privadas sem fins lucrativos. [FONTE: portal.transferegov.sistema.gov.br]
- Convênios e contratos de repasse regidos pelo **Decreto nº 11.531/2023** (substituiu a sistemática anterior do SICONV). [FONTE: planalto — D11531; cnt.br]
- Operacionalização via Transferegov.br disciplinada pelo **Decreto nº 11.271/2022**. [FONTE: orzil.org — citação ao Decreto 11.271/2022; texto integral [a confirmar] no Planalto]

### O que o município registra/envia e quando (ciclo)
1. **Proposta/Plano de trabalho** — cadastro na plataforma (celebração). [FONTE: Transferegov; igam.com.br]
2. **Execução física e financeira** — atos de execução, acompanhamento e fiscalização são registrados no Transferegov.br pelos convenentes (município), concedentes, mandatária e prestadores. [FONTE: D11531/2023, art. sobre execução — cnt.br]
3. **Prestação de contas CONTÍNUA** — inicia-se **concomitantemente à liberação da 1ª parcela** dos recursos (não é só ao final). [FONTE: D11531/2023 — cnt.br]
   - Inclui acompanhamento parcial ao longo da vigência + **prestação de contas final** ao término.
4. **Liberação de parcelas** — salvo parcela única, o 1º desembolso não pode exceder **20% do valor total** do instrumento. [FONTE: TCU — Cartilha Transferências Voluntárias 2025, 8ª ed.; consultoriasquadra]

### Prazos (convênios federais)
- **Prazo do município apresentar prestação de contas final**: ~30 dias após o término do instrumento (faixa 30–60 dias citada por consultorias). [a confirmar — número exato e gatilho no D11531/2023]
- **Prazo da Administração (concedente/mandatária) ANALISAR**: **60 dias** (procedimento informatizado) ou **180 dias** (análise convencional). [FONTE: D11531/2023 — cnt.br]
- Resultado da análise: **aprovação / aprovação com ressalvas / rejeição**, com impactos financeiros. [FONTE: consultoriasquadra]
- Inadimplência: saldo remanescente sofre correção pela **Selic** a partir do 30º dia após o fim do prazo; impedimento de receber novos recursos. [FONTE: consultoriasquadra]

---

## B) MROSC — Lei 13.019/2014 (parcerias com OSCs)

### Instrumentos (o que o município celebra)
- **Termo de Colaboração** — quando a parceria é proposta/dirigida pela ADMINISTRAÇÃO (política pública predefinida pelo poder público). [FONTE: planalto — L13019; comunitas.org.br]
- **Termo de Fomento** — quando o plano de trabalho é proposto pela OSC (protagonismo da sociedade civil). [FONTE: L13019; comunitas.org.br]
- **Acordo de Cooperação** — parceria **sem transferência de recursos financeiros**. [FONTE: L13019; mpce.mp.br]
- Diferença-chave Colaboração vs Fomento = **quem propõe** o objeto/plano de trabalho. [FONTE: comunitas.org.br]

### Chamamento público (regra) e exceções
- **Regra**: celebração de termo de colaboração/fomento depende de **chamamento público** prévio. [FONTE: L13019 art. 24; desenvolvimentosocial.sp.gov.br]
- **Dispensa (art. 30)**: urgência/paralisação até 180 dias; guerra/calamidade/grave perturbação da ordem; programas de proteção a pessoas ameaçadas; atividades de educação/saúde/assistência social por OSCs previamente credenciadas. [FONTE: L13019 art. 30]
- **Inexigibilidade (art. 31)**: inviabilidade de competição por objeto singular ou metas atingíveis só por entidade específica; objeto decorrente de acordo internacional. [FONTE: L13019 art. 31]
- Acordo de cooperação: em regra **sem chamamento** (salvo comodato/doação de bens). [FONTE: L13019]
- Ausência de chamamento (dispensa/inexigibilidade) exige **justificativa detalhada** publicada. [FONTE: L13019 art. 32 — jusbrasil; fundesporte.ms.gov.br]

### Plano de Trabalho (art. 22)
- Obrigatório nos termos de colaboração/fomento; deve conter descrição da realidade/objeto, metas, previsão de receitas/despesas, parâmetros de aferição de resultados. [FONTE: L13019 art. 22 — texto integral dos incisos [a confirmar]]

### Prestação de contas MROSC — o que e quando
- **OSC apresenta prestação de contas final**: até **90 dias** do término da parceria, **prorrogável por +30 dias**. [FONTE: L13019 art. 69 §1º — abong.org.br; metalicitacoes]
- A Administração pode fixar prazo diferente de 90 dias conforme complexidade/vulto (art. 69 §1º). [FONTE: abong.org.br]
- **Administração (município) ANALISA a prestação de contas final**: prazo fixado no instrumento, **até 150 dias** contados do recebimento do Relatório Final, **prorrogável por igual período** justificadamente. [FONTE: L13019 art. 71 — abong.org.br]
- Prestação de contas é **contínua** (relatórios parciais durante a vigência + final). [FONTE: L13019 — manuais MROSC]
- Resultado da análise: aprovação / aprovação com ressalvas / rejeição. [FONTE: manuais MROSC SP — transparenciacultura.sp.gov.br]

### O que o MUNICÍPIO publica/registra (transparência — arts. 10/11)
- Manter **lista das parcerias** celebradas e edital de chamamento em sítio oficial e na plataforma eletrônica. [FONTE: L13019 arts. 10–11 — texto exato dos prazos [a confirmar]]
- Parcerias federais com OSCs operam pela **Plataforma MROSC dentro do Transferegov.br**. [FONTE: gov.br/transferegov — Plataforma MROSC; orzil.org]

---

## Implicação para o módulo Tensorroot.Gov (M9 — Convênios)
- Modelar **dois fluxos distintos**: (A) convênios/transferências federais recebidos (Transferegov/D11531) e (B) parcerias-saída com OSCs (MROSC/L13019).
- Entidades comuns: Plano de Trabalho, Cronograma de desembolso, Execução físico-financeira, Prestação de contas (parcial + final), Análise/Parecer.
- Máquina de estados com **prazos** (alertas): vigência → execução → PC parcial → término → PC final → análise (60/180d convênio; 150d MROSC) → aprovação/ressalva/rejeição.
- Integração futura: API/dados abertos Transferegov [a confirmar disponibilidade de API pública].

## Pendências (a confirmar em fontes primárias)
1. Prazo exato do MUNICÍPIO apresentar PC final em convênio federal (D11531/2023, artigo específico).
2. Texto integral do Decreto 11.271/2022 (operacionalização Transferegov).
3. Incisos completos do art. 22 (plano de trabalho) e arts. 10/11 (prazos de transparência) da L13019 — WebFetch ao Planalto falhou (socket), reusar.
4. Existência/escopo de API pública do Transferegov.br para integração.

## Fontes
- Lei 13.019/2014 (MROSC): https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2014/lei/l13019.htm
- Decreto 11.531/2023: https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2023/decreto/d11531.htm
- Portal Transferegov.br: https://portal.transferegov.sistema.gov.br/
- Plataforma MROSC (gov.br/transferegov): https://www.gov.br/transferegov/pt-br/noticias/eventos/fntu/x-fntu/apresentacoes/evento-099-plataforma-mrosc-defesa-e-fortalecimento-das-osc.pdf
- TCU — Cartilha Transferências Voluntárias da União 2025 (8ª ed.): https://portal.tcu.gov.br/data/files/1D/52/DC/19/B69A591078006549E18818A8/Transferencias%20Voluntarias%20da%20Uniao%20-%20Oitava%20Edicao.pdf
- ABONG — Prestação de Contas L13019 (art. 69/71): https://abong.org.br/wp-content/uploads/2021/09/4.-Prestacao-de-Contas.pdf
- Decreto 11.531/2023 (síntese prazos): https://www.contas.cnt.br/decreto-11-531-2023-convenios-e-contratos-de-repasse-relativos-as-transferencias-de-recursos-da-uniao/
- Manual MROSC PC (SP/Cultura): https://www.transparenciacultura.sp.gov.br/wp-content/uploads/2022/09/Manual_para_Prestacao_de_Contas_OSCs_MROSC_versao_ago_2022.pdf

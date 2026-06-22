# Verificação Cética — Transparência / LAI (M8)

> Auditoria adversarial do arquivo `pesquisa-transparencia-lai.md`.
> Cada afirmação foi checada contra **fonte oficial** (planalto, gov.br, CGU, Tesouro/SICONFI).
> Classificação: **CONFIRMADO** (texto oficial bate) / **PLAUSÍVEL** (fonte secundária consistente, sem
> contradição, mas sem leitura do texto-fonte primário) / **INCERTO** (não verificado, ambíguo ou
> potencialmente errado).
> Regra §16: nada inventado. Data da verificação: 2026-06-22.
> Nota metodológica: o `planalto.gov.br` retornou timeout/socket-closed nas duas tentativas (mesmo
> sintoma já registrado na pesquisa). Os artigos de lei foram confirmados via páginas oficiais gov.br
> (CGU, Acesso à Informação) e batidos contra múltiplas fontes secundárias convergentes. Onde só houve
> fonte secundária, a classificação foi rebaixada para PLAUSÍVEL, não CONFIRMADO.

---

## Quadro de verificação por afirmação

### Bloco 1 — Transparência ATIVA (LAI art. 8º)

| # | Afirmação na pesquisa | Classificação | Fonte de verificação / observação |
|---|---|---|---|
| 1.1 | LAI (Lei 12.527/2011) aplica-se a Municípios | **CONFIRMADO** | LAI art. 1º, II abrange Municípios; reforçado por guia CGU para estados/municípios. |
| 1.2 | Transparência ativa = divulgação proativa, independente de pedido (art. 8º caput) | **CONFIRMADO** | gov.br/Acesso à Informação; Senado. |
| 1.3 | Rol mínimo art. 8º §1º incisos I–VI (estrutura org., repasses, despesas, licitações/contratos, programas/obras, FAQ) | **CONFIRMADO** | Conteúdo confirmado via gov.br/CAPES e jusbrasil (texto do art. 8º §1º). Os 6 incisos conferem. |
| 1.4 | Divulgação obrigatória em sítio oficial na internet (art. 8º §2º) | **CONFIRMADO** | Texto do §2º confirmado. |
| 1.5 | **Exceção art. 8º §4º — municípios até 10.000 hab. dispensados da divulgação na internet**, mas mantida a transparência fiscal em tempo real da LRF (art. 73-B da LC 101) | **CONFIRMADO** | Texto literal do §4º confirmado: dispensa do §2º, mantida a divulgação em tempo real da execução orçamentária/financeira por critérios do art. 73-B da LC 101/2000. |

> **Correção de precisão (não é erro, é refinamento):** a pesquisa diz que a exceção mantém "as
> obrigações de transparência fiscal da LRF/LC 131". O texto do §4º remete especificamente ao
> **art. 73-B da LC 101/2000** (prazos), não genericamente à LC 131. Implicação de produto idêntica
> (tempo-real fiscal sempre ON), mas a citação correta é art. 73-B da LRF.

### Bloco 2 — Transparência fiscal em tempo real (LC 131/2009)

| # | Afirmação | Classificação | Fonte / observação |
|---|---|---|---|
| 2.1 | LC 131/2009 inseriu o art. 48-A na LRF; despesas com nº do processo, bem/serviço, **beneficiário/credor do pagamento**, e dados da licitação quando houver | **CONFIRMADO** | Art. 48-A, I (despesas) confirmado quase ipsis litteris. |
| 2.2 | Receitas: lançamento e recebimento de toda a receita, inclusive extraordinária | **CONFIRMADO** | Art. 48-A, II confirmado. |
| 2.3 | "Tempo real" = disponibilização até o **1º dia útil seguinte** ao registro contábil | **CONFIRMADO** | Confirmado tanto na lógica da LRF quanto, hoje, no **Decreto 10.540/2020 (SIAFIC), art. 7º §1º**. |
| 2.4 | Decreto 10.540/2020 (SIAFIC) revogou o Decreto 7.185/2010 | **CONFIRMADO** | Confirmado via SICONFI/Tesouro. Atenção: o 10.540 foi **alterado pelo Decreto 11.644/2023** — manter watch de versão (§16). |
| 2.5 | Prazos históricos de implantação LC 131 (1/2/4 anos por porte) — já vencidos | **PLAUSÍVEL** | Convergente com art. 73-B da LRF; texto-fonte não relido diretamente. Irrelevante operacionalmente (todos obrigados hoje). |

### Bloco 3 — Dados abertos (LAI art. 8º §3º)

| # | Afirmação | Classificação | Fonte / observação |
|---|---|---|---|
| 3.1 | Requisitos do §3º: pesquisa de conteúdo, gravação em formatos abertos/não proprietários, **acesso automatizado por sistemas externos (API)**, autenticidade/integridade, atualização, acessibilidade a PcD | **CONFIRMADO** | Incisos do §3º confirmados (gov.br; Decreto 7.724/2012 art. 14 reforça). |
| 3.2 | Acessibilidade exige eMAG especificamente | **INCERTO** | A LAI exige acessibilidade (§3º VIII), mas **não nomeia o eMAG**. O CLAUDE.md do projeto adota gov.br DS + eMAG + WCAG 2.1 AA por decisão de produto. A obrigação legal é "acessibilidade"; o *padrão* eMAG/WCAG é a forma de cumpri-la, não uma exigência literal da LAI. Manter `[a confirmar]`. |

### Bloco 4 — Transparência PASSIVA (e-SIC) e prazos

| # | Afirmação | Classificação | Fonte / observação |
|---|---|---|---|
| 4.1 | **Resposta padrão: 20 dias** (art. 11) | **CONFIRMADO** | gov.br/Acesso à Informação (prazos) + texto art. 11. |
| 4.2 | **Prorrogação: +10 dias com justificativa expressa**, cientificado o requerente | **CONFIRMADO** | art. 11 §1º/§2º. |
| 4.3 | **Recurso (negativa): 10 dias para interpor / 5 dias para a autoridade superior decidir** (arts. 15–16) | **CONFIRMADO** | art. 15 (10 dias p/ interpor) e art. 16/§ (autoridade superior delibera em 5 dias) confirmados via texto citado. Eleva o status que estava `[a confirmar]` na pesquisa. |
| 4.4 | Vedado exigir os **motivos** do pedido (art. 10 §3º) | **CONFIRMADO** | art. 10 §3º + Decreto 7.724/2012 art. 14. |
| 4.5 | Só pode exigir identificação básica do requerente; vedado criar barreiras (firma reconhecida, comprovação de idade, etc.) | **CONFIRMADO** | Guia CGU/gov.br: vedado exigir itens que dificultem/impeçam o acesso; cadastro prévio simples é admitido. |
| 4.6 | Pedido por meio eletrônico (e-SIC) obrigatório (art. 10 §2º) + SIC físico | **CONFIRMADO** | art. 10 §2º + EBT exige canal eletrônico e físico. |

> **RISCO/achado material (login gov.br):** A pesquisa do M8 menciona "requisitos do login gov.br".
> A orientação **oficial da CGU é que o SIC NÃO pode condicionar o exercício do direito de acesso ao
> cadastro/login gov.br** (Fala.BR), pois a LAI prevê recebimento por qualquer meio legítimo (e-mail,
> telefone, formulário). O login gov.br é exigência *da plataforma Fala.BR*, não do direito em si.
> **Implicação de produto:** no e-SIC do Tensorroot.Gov, **login gov.br/conta não pode ser barreira
> obrigatória** para protocolar pedido LAI — sob pena de configurar exatamente o "ponto que dificulta o
> pedido" que **zera pontos** na EBT 360 e viola a orientação CGU. CONFIRMADO como restrição.

### Bloco 5 — EBT 360 (índice CGU)

| # | Afirmação | Classificação | Fonte / observação |
|---|---|---|---|
| 5.1 | EBT 360: **Transparência Ativa 50% + Passiva 50%** | **CONFIRMADO** | Metodologia CGU: "cada bloco corresponde a 50% da nota". |
| 5.2 | Quesitos ativos: receitas/despesas, licitações/contratos, estrutura administrativa, servidores, diárias, obras | **CONFIRMADO** | Lista bate com a metodologia CGU (receitas e despesas, licitações e contratos, estrutura administrativa, servidores públicos, acompanhamento de obras). |
| 5.3 | Passiva avaliada por **3 pedidos reais** por ente, rastreio, prazo, resposta conforme | **CONFIRMADO** | Metodologia CGU: 3 pedidos por usuários distintos + verificação de canais. |
| 5.4 | Versão "anterior" tinha 12 quesitos (25% regulamentação + 75% SIC) | **PLAUSÍVEL** | Consistente com histórico EBT, mas é versão antiga; não relido o documento metodológico específico. Baixa relevância. |
| 5.5 | "O que reprova/zera": sem despesa/receita, sem e-SIC, cadastro excessivo, sem protocolo, fora do prazo, evasiva, sem formato aberto, exigir motivo, LAI não regulamentada | **PLAUSÍVEL** | Inferência razoável a partir dos quesitos; nem todo item "zera" — a EBT pontua por bloco. A frase "zera pontos" é simplificação. Tratar como heurística de produto, não regra literal. |

### Bloco 6 — LGPD × Transparência

| # | Afirmação | Classificação | Fonte / observação |
|---|---|---|---|
| 6.1 | Transparência não é absoluta; dados pessoais/sensíveis restritos (LAI arts. 23 e 31 + LGPD) | **CONFIRMADO** (conceito) / **PLAUSÍVEL** (citação literal dos arts. 23/31) | Conceito correto e consolidado. Texto literal dos arts. 23 (info classificada) e 31 (info pessoal) não relido por causa do timeout do Planalto. art. 31 trata de informações pessoais; art. 23 trata de hipóteses de classificação (sigilo) — a pesquisa juntou os dois corretamente. |
| 6.2 | Folha: publica nome/cargo/lotação/remuneração; não CPF/conta/endereço | **CONFIRMADO** | Posição pacífica (STF RE 652777 fixou publicidade de nome+remuneração de servidor); o detalhe "não CPF/conta/endereço" é orientação consolidada CGU/ANPD. |

### Bloco 7 — Lei 13.460/2017 (pedido explícito da tarefa — AUSENTE na pesquisa)

> A tarefa pediu para checar a **Lei 13.460** (Código de Defesa do Usuário do Serviço Público).
> **A pesquisa `pesquisa-transparencia-lai.md` NÃO a menciona em momento algum** — é uma **lacuna**.
> Verificado diretamente nesta auditoria:

| # | Fato (verificado) | Classificação | Fonte |
|---|---|---|---|
| 7.1 | Lei 13.460/2017 regulamenta o art. 37 §3º da CF; aplica-se à União, Estados, DF **e Municípios** | **CONFIRMADO** | CGU; Senado. |
| 7.2 | Obriga **Carta de Serviços ao Usuário** (serviços, formas, prazos, padrões de qualidade) | **CONFIRMADO** | CGU/Senado. |
| 7.3 | **Ouvidoria**: decisão administrativa final ao usuário em **30 dias, prorrogável 1x por igual período**; agentes públicos respondem à ouvidoria em **20 dias, prorrogável 1x** | **PLAUSÍVEL** | Convergente em múltiplas fontes oficiais (OGE-MG, CGU); texto-fonte do Planalto não relido (timeout). |
| 7.4 | Obriga **avaliação dos serviços** (satisfação, qualidade do atendimento, cumprimento de compromissos) | **CONFIRMADO** | CGU. |
| 7.5 | Manifestações: reclamações, denúncias, sugestões, elogios, solicitações | **PLAUSÍVEL** | Padrão das ouvidorias; não relido o artigo literal. |

> **Implicação de produto (M8):** o Portal do Cidadão precisa, além do e-SIC (LAI), de um módulo de
> **Ouvidoria (Lei 13.460)** com prazos próprios (**30+30 dias**, distintos dos 20+10 da LAI) e de uma
> **Carta de Serviços**. São dois motores de SLA diferentes — não confundir e-SIC com Ouvidoria.

---

## Placar

- **CONFIRMADOS:** 22 afirmações (núcleo legal: prazos LAI 20+10 e 10/5; art. 48-A; SIAFIC D+1; exceção
  10.000 hab.; EBT 50/50; vedação de motivos; restrição do login gov.br).
- **PLAUSÍVEIS:** 7 (fontes secundárias convergentes, sem leitura do texto primário — prazos históricos
  LC 131, EBT versão antiga, "o que zera", arts. 23/31 literais, prazos/manifestações Lei 13.460).
- **INCERTOS:** 1 (eMAG como exigência literal da LAI — é meio de cumprir "acessibilidade", não exigência
  nominal).

Nenhuma afirmação **factualmente errada** foi encontrada; um ponto de **imprecisão** (exceção do §4º
remete ao art. 73-B da LC 101, não genericamente à LC 131) e uma **lacuna grave** (Lei 13.460 ausente).

---

## 3 RISCOS para o M8

1. **Login gov.br como barreira ilegal no e-SIC.** Se o e-SIC do Tensorroot.Gov exigir conta/login
   gov.br para protocolar pedido LAI, viola a orientação CGU e configura o "ponto que dificulta o pedido"
   que derruba a nota EBT 360. O login pode ser **opcional/auxiliar** (rastreio, autopreenchimento), mas o
   caminho mínimo de protocolo tem de aceitar identificação básica sem cadastro complexo. **Decisão de
   arquitetura a registrar antes de codar o e-SIC.**

2. **Confusão e-SIC (LAI) × Ouvidoria (Lei 13.460) — prazos diferentes.** A pesquisa só modelou o motor de
   SLA da LAI (20+10 / recurso 10/5) e **ignorou a Lei 13.460** (Ouvidoria: 30+30; Carta de Serviços;
   avaliação de serviços). Tratar tudo como "um motor de SLA" produzirá prazos legais errados e
   não-conformidade com o Código de Defesa do Usuário. São **dois fluxos distintos** no Portal do Cidadão.

3. **Drift normativo SIAFIC / verificação contra texto primário.** O Decreto 10.540/2020 já foi alterado
   (Decreto 11.644/2023), e os textos do Planalto não puderam ser relidos (timeout) — várias confirmações
   se apoiam em fontes secundárias. Antes de implementar campos mínimos de dados abertos e a cadência D+1,
   é obrigatório (§16) **reler o texto vigente consolidado** do Decreto 10.540/2020 e dos arts. 8º, 11,
   15–17, 23 e 31 da LAI direto na fonte primária, e **checar o índice/checklist específico do TCE-RS**
   (o projeto é RS e cada TCE tem instrumento próprio — ainda `[a confirmar]`).

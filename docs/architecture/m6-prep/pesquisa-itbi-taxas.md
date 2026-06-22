# Pesquisa — M6 Tributos: ITBI, Taxas, COSIP, Alvarás e Contribuição de Melhoria

> Preparação do M6 (Tributos — espécies além de IPTU/ISS já existentes). Regra de ouro CLAUDE.md §16: toda afirmação factual tem FONTE (URL) ou está marcada `[a confirmar — obter doc oficial]`.
> Regra CLAUDE.md (§16 / spec): **alíquotas, PGV, faixas e regras NUNCA são hardcoded** — são **parametrizáveis por tenant** via **lei municipal** (Código Tributário Municipal — CTM, e leis específicas de cada obra/serviço).
> Lembrete (§8): **NFS-e é PASSIVA** (ingerida do ADN, não emitida) — fora do escopo desta pesquisa, mas o ISS lançado/auto pode usar a NFS-e ingerida como base; aqui tratamos das demais espécies.
> Domínio existente (`Tributos.Domain/Lancamentos/Lancamento.cs`): `enum TipoTributo` já contempla `Iptu(1)`, `Iss(2)`, `Itbi(3)`, `Taxa(4)`, `ContribuicaoMelhoria(5)`, `Cosip(6)`. Esta pesquisa subsidia os **motores de cálculo / parametrização** dessas espécies.
> Data da pesquisa: 2026-06-22. Município piloto: Maximiliano de Almeida/RS.

---

## 0. Mapa das espécies tributárias municipais (CF/88 art. 145 e 149-A; CF art. 156)

| Espécie | Competência | Fundamento | Quem regula localmente |
|---|---|---|---|
| **ITBI** | Imposto (CF art. 156, II) | CTN arts. 35–42 | CTM (alíquota, prazo, isenções) |
| **Taxas** (poder de polícia / serviço) | Taxa (CF art. 145, II) | CTN arts. 77–80 | CTM (fato gerador, base, tabela de valores) |
| **Contribuição de Melhoria** | Contribuição (CF art. 145, III) | CTN arts. 81–82 + **DL 195/1967** | Lei específica **por obra** + CTM |
| **COSIP/CIP** | Contribuição *sui generis* (CF art. 149-A, EC 39/2002) | Lei municipal própria | Lei municipal específica de iluminação |
| **Alvará (licença)** | **NÃO é tributo** — é o **ato administrativo** de licença; o tributo correlato é a **Taxa de Licença de Localização/Funcionamento (TLL)** cobrada pelo exercício do poder de polícia | CTN arts. 77–78 | CTM |

> **Achado central de modelagem:** "Alvará" é o **ato de polícia**; o **fato gerador tributário** é a **Taxa de Licença** (poder de polícia) que custeia a fiscalização. Modelar como `TipoTributo.Taxa` com subtipo "Licença de Localização/Funcionamento" e amarração a um cadastro de Alvará (estabelecimento). FONTE: CTN art. 78 (poder de polícia) — <https://www.jusbrasil.com.br/topicos/10581742/artigo-78-da-lei-n-5172-de-25-de-outubro-de-1966>; análise TLL — <https://www.jusbrasil.com.br/artigos/taxa-de-licenca-e-localizacao-tll-historia-funcionamento-legalidade-e-o-que-cai-em-concursos/5387209605>.

---

## 1. ITBI — Imposto sobre Transmissão de Bens Imóveis *inter vivos*

### 1.1 Fato gerador
- **Transmissão *inter vivos*, a qualquer título, por ato oneroso**, da propriedade ou do domínio útil de bens imóveis, e de direitos reais sobre imóveis (exceto garantia), bem como a cessão de direitos à sua aquisição. Base: **CTN art. 35** (combinado com CF art. 156, II). FONTE: <https://modeloinicial.com.br/lei/CTN/codigo-tributario-nacional/art-77> (portal CTN); jurisprudência consolidada STJ/STF abaixo.
- **Momento do fato gerador = REGISTRO da transmissão no Cartório de Registro de Imóveis.** A mera escritura ou o compromisso de compra e venda **não** constituem o fato gerador. FONTE (STF, entendimento consolidado): <https://www.notariado.org.br/novas-regras-para-itcmd-e-itbi-entram-em-vigor-e-podem-deixar-herancas-doacoes-e-compras-de-imoveis-mais-caras/>; <https://www.conjur.com.br/2026-jan-27/reforma-tributaria-fim-das-discussoes-sobre-base-de-calculo-do-itbi/>.
  - **Reforma tributária (EC 132/2023 / LC 227/2026):** foi **vetado** dispositivo que permitiria recolher ITBI já na escritura (antes do registro), exatamente para não criar insegurança sobre o momento do fato gerador → **mantém-se: exigível só após o registro.** FONTE: <https://www.ozai.com.br/mudancas-itcmd-itbi-2026/>; <https://www.notariado.org.br/novas-regras-para-itcmd-e-itbi-entram-em-vigor-e-podem-deixar-herancas-doacoes-e-compras-de-imoveis-mais-caras/>. `[a confirmar — obter texto literal da LC 227/2026 e da EC 132/2023 art. 156 no Planalto; o fetch do Planalto falhou nesta sessão]`.

### 1.2 Base de cálculo — **Tema 1.113/STJ (REsp 1.937.821, repetitivo, 2022)** — CRÍTICO para o motor
- **Base = "valor venal do imóvel em condições normais de mercado"** (CTN art. 38). FONTE (STJ): <https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/09032022-Base-de-calculo-do-ITBI-e-o-valor-do-imovel-transmitido-em-condicoes-normais-de-mercado--define-Primeira-Secao.aspx>.
- **Três teses fixadas (transcrição do resumo oficial STJ):**
  1. A base de cálculo do ITBI é o valor do imóvel em condições normais de mercado, **não vinculada à base de cálculo do IPTU**, que **nem sequer pode ser utilizada como piso de tributação**.
  2. O **valor da transação declarado pelo contribuinte goza da presunção** de ser condizente com o valor de mercado, que **só pode ser afastada pelo Fisco mediante processo administrativo próprio (CTN art. 148)**.
  3. O município **NÃO pode arbitrar previamente** a base de cálculo do ITBI com respaldo em **valor de referência por ele estabelecido de forma unilateral**.
  FONTE: STJ (mesma URL acima); reforço IRIB <https://www.irib.org.br/noticias/detalhes/stj-define-base-de-calculo-para-cobranca-de-itbi> e Registro de Imóveis <https://www.registrodeimoveis.org.br/stj-itbi-calculo>.
- **Implicação de engenharia (parametrização):** o motor de ITBI **não pode** ter "Planta de Valores / valor venal de referência" como base impositiva automática de ofício. Deve: (a) tomar o **valor declarado** como base presumida; (b) permitir, como parâmetro do tenant, um **valor de referência apenas indicativo/alerta** (não impositivo); (c) suportar **abertura de processo administrativo (art. 148 CTN)** para arbitramento quando o Fisco questionar. Tudo configurável por lei municipal.

### 1.3 Alíquota e contribuinte
- **Alíquota:** proporcional, fixada por lei municipal (não há teto federal específico; usual 2%–3%). **Parametrizável por tenant.** Pode haver alíquota reduzida para imóveis financiados pelo SFH sobre a parcela financiada. `[a confirmar — obter alíquota e regras do CTM de Maximiliano de Almeida/RS]`.
- **Contribuinte:** definido em lei municipal — qualquer das partes da operação (CTN art. 42); na prática o **adquirente**. FONTE: CTN art. 42 (portal CTN, mesma base acima).
- **Imunidades (CF art. 156, §2º, I):** não incide sobre transmissão de bens/direitos incorporados ao patrimônio de PJ em realização de capital, nem sobre fusão/incorporação/cisão/extinção, **salvo** se a atividade preponderante do adquirente for compra/venda/locação de imóveis. → **regra de exceção a modelar** no motor. `[a confirmar — texto literal CF art. 156 §2º no Planalto]`.

### 1.4 Guia de recolhimento (DAM/guia ITBI)
- Emissão de **guia avulsa** (não é lançamento periódico): gerada por transação de transmissão, com base no valor declarado, alíquota do tenant, e validação prévia ao registro cartorial. Integração futura com cartórios `[a confirmar — verificar se há convênio/ARISP-tipo no RS]`.

---

## 2. Taxas (poder de polícia e de serviço)

### 2.1 Fundamento e fato gerador (CTN arts. 77–80)
- **CTN art. 77:** taxas têm como fato gerador **o exercício regular do poder de polícia** OU **a utilização, efetiva ou potencial, de serviço público específico e divisível**, prestado ao contribuinte ou posto à sua disposição.
- **CTN art. 78:** define **poder de polícia** (atividade da Adm. Pública que, limitando/disciplinando direito/interesse/liberdade, regula a prática de ato em razão de interesse público — segurança, higiene, ordem, costumes, disciplina do mercado, atividades econômicas dependentes de concessão/autorização, tranquilidade pública, propriedade/direitos).
- **CTN art. 79:** serviço público **específico** (destacável em unidades autônomas) e **divisível** (suscetível de utilização separada por usuário); **potencial** quando de uso compulsório posto à disposição por atividade administrativa em efetivo funcionamento.
- **CTN art. 80 (vedação):** a taxa **NÃO pode ter base de cálculo ou fato gerador idênticos aos de imposto, nem ser calculada em função do capital das empresas.**
FONTE (CTN arts. 77–79): <https://modeloinicial.com.br/lei/CTN/codigo-tributario-nacional/art-77> e <https://www.jusbrasil.com.br/topicos/10581742/artigo-78-da-lei-n-5172-de-25-de-outubro-de-1966>; CF art. 145, II: <https://portal.stf.jus.br/constituicao-supremo/artigo.asp?abrirBase=CF&abrirArtigo=145>.

### 2.2 Jurisprudência vinculante (limites da base de cálculo)
- **Súmula Vinculante 29/STF:** é **constitucional** adotar, no cálculo do valor de taxa, **um ou mais elementos da base de cálculo de imposto**, desde que **não haja identidade integral** entre uma base e outra. → permite usar área/metragem (elemento também usado no IPTU) sem copiar a base inteira.
- **Súmula Vinculante 19/STF:** taxa cobrada exclusivamente por serviços de **coleta, remoção e tratamento/destinação de lixo** de imóveis **não viola** o art. 145, II, CF (é específica e divisível).
FONTE: <https://www.estrategiaconcursos.com.br/blog/as-taxas-e-a-jurisprudencia-do-supremo-tribunal-federal/>; STF SV (base 26).

### 2.3 Implicação de engenharia
- Modelar **Taxa** como `TipoTributo.Taxa` com **subtipo** (poder de polícia vs. serviço) e **tabela de valores parametrizável por tenant** (valores fixos por faixa/UF/atividade/metragem). **Nunca** calcular sobre o capital da empresa nem replicar base de imposto integralmente (CTN art. 80 + SV 29). Tabelas (ex.: lixo, licença sanitária, vigilância) vêm do **CTM do tenant**. `[a confirmar — obter tabelas de taxas do CTM de Maximiliano de Almeida/RS]`.

---

## 3. Alvarás e Taxa de Licença de Localização/Funcionamento (TLL)

- O **Alvará** é o **ato administrativo de licença** (instrumento do poder de polícia); o tributo é a **Taxa de Licença de Localização e Funcionamento (TLL)** + taxas de renovação/fiscalização anual, devidas por PF/PJ que exerçam atividade passível de fiscalização municipal. FONTE: <https://www.jusbrasil.com.br/artigos/taxa-de-licenca-e-localizacao-tll-historia-funcionamento-legalidade-e-o-que-cai-em-concursos/5387209605>; <https://www.contabilizei.com.br/contabilizei-responde/o-alvara-de-funcionamento-e-imposto-ou-taxa/>.
- **Renovação anual** mediante pagamento da taxa correspondente (prática típica RS — ex. Passo Fundo). FONTE: <https://www.pmpf.rs.gov.br/secretaria-de-desenvolvimento-economico/servicos/renovacao-do-alvara-de-localizacao-e-funcionamento/>.
- **Modelagem:** cadastro de **Alvará/Estabelecimento** (ato de polícia, com vigência/renovação) **+** geração de **Lançamento `TipoTributo.Taxa` subtipo Licença** a partir da **tabela parametrizável** (por atividade/CNAE/área/risco). Espécies de alvará: localização e funcionamento, sanitário, ambiental, obras (Habite-se). `[a confirmar — obter tabela e classes de risco do CTM de Maximiliano de Almeida/RS]`.

---

## 4. COSIP / CIP — Contribuição para Custeio da Iluminação Pública

### 4.1 Fundamento e natureza (CF art. 149-A, EC 39/2002; STF RE 573.675 — Tema 44 RG)
- **CF art. 149-A** (incluído pela EC 39/2002): os Municípios e o DF poderão instituir contribuição, **na forma das respectivas leis**, para o **custeio do serviço de iluminação pública**, observado o art. 150, I e III; **faculta-se a cobrança na fatura de energia elétrica**. FONTE: <https://noticias.stf.jus.br/postsnoticias/supremo-mantem-cobranca-proporcional-de-contribuicao-de-iluminacao-publica-no-estado/>; <https://haradaadvogados.com.br/contribuicao-social-para-custeio-do-servico-de-iluminacao-publica/>.
- **STF RE 573.675 (constitucionalidade — Tema 44):** firmou que (resumo):
  1. Lei que restringe os contribuintes da COSIP aos **consumidores de energia elétrica** do município **não ofende a isonomia** (impossibilidade de identificar/tributar todos os beneficiários).
  2. A **progressividade da alíquota** (rateio do custo entre consumidores) **não fere a capacidade contributiva**.
  3. COSIP é tributo **sui generis**: não se confunde com imposto (receita afetada a fim específico) nem com taxa (não exige serviço individualizado/divisível).
  4. Atende razoabilidade e proporcionalidade.
  FONTE: <https://www.conjur.com.br/2010-out-07/entendimento-stf-cosip-influenciar-leis-municipais/>; <https://www.jusbrasil.com.br/artigos/breve-analise-da-natureza-juridica-da-contribuicao-para-o-custeio-do-servico-de-iluminacao-publica-cosip/881809632>.

### 4.2 Implicação de engenharia
- Base/alíquota = **definidas por lei municipal própria**, tipicamente **faixas de consumo (kWh) na fatura de energia** (cobrança feita pela distribuidora) ou valor fixo por classe de consumidor. **100% parametrizável por tenant** (tabela de faixas). Como a cobrança costuma ser **na fatura da distribuidora**, modelar tanto (a) repasse/conciliação com a concessionária quanto (b) lançamento próprio para casos não faturados pela distribuidora. `[a confirmar — obter lei de COSIP/CIP de Maximiliano de Almeida/RS e modelo de convênio com a distribuidora (RGE/CEEE na região)]`.

---

## 5. Contribuição de Melhoria (CTN arts. 81–82 + DL 195/1967)

### 5.1 Fato gerador, base e limites
- **Fato gerador:** **valorização imobiliária decorrente de obra pública** (não a obra em si). **CTN art. 81 / DL 195/67 art. 1º.**
- **Limites (CTN art. 81):** **limite total = despesa realizada** com a obra; **limite individual = acréscimo de valor** que da obra resultar para cada imóvel beneficiado. **Rateio proporcional à valorização de cada imóvel.**
- FONTE: <https://www.vademecumprevidenciario.com.br/legislacao/art/dcl_00001951967-1> (DL 195/67 art. 1º); CTN arts. 81–82 (portal CTN, base acima); análise <https://ambitojuridico.com.br/cadernos/direito-tributario/a-contribuicao-de-melhoria-e-sua-utilizacao-por-administradores-publicos-no-brasil/>.

### 5.2 Requisitos procedimentais obrigatórios (CTN art. 82) — CRÍTICO para o fluxo
A lei (específica por obra) **deve** observar requisitos mínimos:
1. **Publicação prévia** de: memorial descritivo do projeto; orçamento do custo da obra; **parcela do custo a ser financiada** pela contribuição; **delimitação da zona beneficiada**; **fator de absorção do benefício** da valorização.
2. **Prazo não inferior a 30 dias** para **impugnação** pelos interessados.
3. **Regulamentação do processo administrativo** de instrução e julgamento da impugnação.
FONTE: CTN art. 82 (mesma base); reforço <https://www.conteudojuridico.com.br/consulta/artigos/54251/a-contribuio-de-melhoria-e-a-discusso-sob-prisma-legal-e-jurisprudencial-acerca-da-publicao-prvia-do-edital>.

### 5.3 Implicação de engenharia
- Modelar como **agregado "Obra/Edital de Contribuição de Melhoria"** (com edital, custo, zona, fator de absorção, prazo de impugnação) → gera **Lançamentos `TipoTributo.ContribuicaoMelhoria`** por imóvel, com **rateio proporcional à valorização individual** (respeitando os dois limites). Workflow de **impugnação (≥30 dias)** é parte do fluxo, não opcional. Todos os valores/fatores **parametrizáveis por obra/tenant**.

---

## 6. Síntese para modelagem (motores + parametrização)

| Espécie | `TipoTributo` | Base de cálculo (parametrizável) | Gatilho/lançamento | Regra inegociável |
|---|---|---|---|---|
| ITBI | `Itbi(3)` | valor declarado (presumido) — **não** valor venal unilateral | guia avulsa por transação; exigível no **registro** | Tema 1.113/STJ; arbitramento só via art. 148 CTN |
| Taxa (polícia/serviço) | `Taxa(4)` | tabela do CTM (faixa/área/atividade) | lançamento por fato/anual | CTN art. 80 + SV 29 (sem base de imposto integral; sem capital) |
| Alvará/Licença | `Taxa(4)` (subtipo Licença) | tabela TLL do CTM | cadastro de Alvará + renovação anual | é taxa de poder de polícia, não imposto |
| COSIP | `Cosip(6)` | faixas de consumo/classe (lei municipal) | fatura distribuidora ou lançamento próprio | tributo *sui generis* (RE 573.675); afetada à iluminação |
| Contrib. Melhoria | `ContribuicaoMelhoria(5)` | valorização individual; limites total/indiv. | edital por obra → rateio | CTN art. 82 (edital prévio + impugnação ≥30d) |

---

## 7. Pendências `[a confirmar — obter doc oficial]`
1. **CTM de Maximiliano de Almeida/RS** (texto integral): alíquotas/contribuinte ITBI; tabelas de Taxas e TLL; lei de COSIP/CIP; classes de risco de alvará. — fonte primária ausente nesta sessão.
2. **Texto literal Planalto** dos artigos do CTN (35, 38, 42, 77–82) e CF art. 156 §2º / 149-A — o fetch ao planalto.gov.br falhou (socket fechado); revalidar via <https://www.planalto.gov.br/ccivil_03/leis/l5172compilado.htm> e <https://www.planalto.gov.br/ccivil_03/constituicao/constituicao.htm>.
3. **EC 132/2023 + LC 227/2026** — texto literal sobre ITBI (momento do fato gerador / veto à antecipação na escritura) e eventuais impactos na competência municipal residual após IBS.
4. **COSIP — convênio de cobrança na fatura** com a distribuidora atuante na região (RGE/CEEE-tipo) e modelo de repasse/conciliação.
5. **Integração ITBI ↔ Cartório de Registro de Imóveis** (existência de convênio/serviço eletrônico no RS) para validação da guia antes do registro.
6. **STF RE 573.675 / Tema 44** e **STJ Tema 1.113 (REsp 1.937.821)** — obter inteiro teor/acórdão para citação literal das teses (atualmente via resumos oficiais STJ + secundárias).

# GovTech — Contabilidade Pública e Prestação de Contas (PCASP/MCASP, Lei 4.320, TCE-RS, SICONFI)

> Pesquisa para o módulo **Financas/Contabilidade** do Tensorroot.Gov. Piloto: Maximiliano de Almeida/RS (jurisdição TCE-RS).
> Maiores preocupações do dono: **Contabilidade** e **Prestação de Contas ao TCE**.
> Data da pesquisa: 2026-06. Documentos técnicos do TCE-RS e SICONFI são **versionados anualmente** — sempre validar a versão do exercício vigente antes de gerar remessa.
>
> AVISO METODOLÓGICO: vários PDFs oficiais (manuais técnicos do TCE-RS, Regras Gerais MSC) não puderam ser extraídos integralmente por WebFetch (vêm comprimidos/binários). Os fatos abaixo vêm de fontes oficiais e snippets confiáveis; pontos não confirmados na íntegra estão marcados **[a confirmar em fonte oficial]** com a URL exata a baixar manualmente.

---

## 1. FUNDAMENTO LEGAL — o que NÃO se pode inventar

A contabilidade pública municipal brasileira é **fortemente normatizada**. O sistema NÃO pode improvisar estrutura de contas, estágios de despesa, demonstrativos ou leiautes. Tudo é prescrito por:

| Norma | Objeto |
|---|---|
| **Lei nº 4.320/1964** | Normas gerais de Direito Financeiro; orçamento, estágios da despesa/receita, exercício financeiro, restos a pagar, demonstrações contábeis (arts. 101-105) |
| **LC nº 101/2000 (LRF)** | Responsabilidade fiscal; obriga RREO, RGF, limites de despesa de pessoal/dívida, consolidação nacional |
| **MCASP (11ª edição, vigência jan/2025)** | Manual de Contabilidade Aplicada ao Setor Público — STN. Padrão obrigatório para União, Estados, DF e Municípios |
| **PCASP** | Plano de Contas Aplicado ao Setor Público — relação padronizada de contas + atributos |
| **Portaria STN nº 896/2017** | Institui a MSC e os leiautes RREO, RGF, DCA, MSC no SICONFI |
| **Portaria STN nº 642 (Regras Gerais MSC)** | Regras anuais da Matriz de Saldos Contábeis [a confirmar nº/ano vigente] |
| **Portaria STN nº 438/2012** | Atualizou as estruturas dos anexos da Lei 4.320 às novas normas |
| **Resolução TCE-RS nº 1134/2020** | Prazos, documentos e informações em formato eletrônico para exame das contas anuais e ordinárias municipais |
| **Resolução TCE-RS nº 1099/2018** | Envio mensal de dados de folha de pagamento ao TCE-RS |
| **Resolução TCE-RS nº 1074/2017** | SICOE (jurisdição/obras) [a confirmar — citada em busca] |
| **NBC TSP** (CFC) | Normas Brasileiras de Contabilidade Aplicadas ao Setor Público (convergência IPSAS) |

**Regra de ouro do produto:** o motor contábil é um **gerador determinístico** a partir dos leiautes oficiais. Nada de plano de contas "próprio", nada de demonstrativo "customizado". O que o sistema produz tem de bater 1:1 com e-Validador (TCE-RS) e SICONFI (STN).

---

## 2. PCASP / MCASP — estrutura contábil

### 2.1 As 8 classes (1º nível do código contábil)

| Classe | Natureza | Conteúdo |
|---|---|---|
| **1 — Ativo** | Patrimonial | Bens e direitos |
| **2 — Passivo e Patrimônio Líquido** | Patrimonial | Obrigações + PL |
| **3 — Variações Patrimoniais Diminutivas (VPD)** | Patrimonial (resultado) | Reduções do PL: despesas, perdas, consumo de ativos |
| **4 — Variações Patrimoniais Aumentativas (VPA)** | Patrimonial (resultado) | Aumentos do PL: receitas tributárias, transferências, ganhos |
| **5 — Controles da aprovação do planejamento e orçamento** | Orçamentária | Previsão receita, fixação despesa, dotação |
| **6 — Controles da execução do planejamento e orçamento** | Orçamentária | Execução: empenho, liquidação, pagamento (lado orçamentário) |
| **7 — Controles devedores** | Controle | Atos potenciais, garantias, contratos |
| **8 — Controles credores** | Controle | Contrapartida da classe 7 |

### 2.2 Três naturezas de informação contábil
- **PATRIMONIAL** (classes 1-4) — fatos financeiros e não financeiros do patrimônio.
- **ORÇAMENTÁRIA** (classes 5-6) — planejamento e execução do orçamento.
- **CONTROLE** (classes 7-8) — atos que podem modificar o patrimônio.

### 2.3 Regras inegociáveis
- **Método das partidas dobradas**: todo lançamento gera débito e crédito, em pelo menos duas contas; **equação contábil sempre fechada**.
- **Atributos das contas** (atributo conceitual): cada conta carrega atributo de natureza da informação, indicador de superávit financeiro (F/P), etc. — **definidos no PCASP, não inventáveis**.
- **Estrutura de codificação** do PCASP estendida (a STN define níveis 1-5 padrão; entes podem detalhar abaixo respeitando integridade). O ente NÃO altera os níveis padronizados.
- **Regras de integridade do PCASP** (MCASP Parte IV/seção 3.5) — validações obrigatórias entre contas.

### 2.4 DCASP — Demonstrações Contábeis obrigatórias (MCASP Parte V)
Conjunto que o sistema **deve gerar automaticamente**:
1. **Balanço Orçamentário** — previsto x realizado (receita/despesa).
2. **Balanço Financeiro** — entradas/saídas (obrigatório por art. 101 da Lei 4.320, embora não previsto nas NBC TSP).
3. **Balanço Patrimonial** — ativo, passivo, PL.
4. **Demonstração das Variações Patrimoniais (DVP)**.
5. **Demonstração das Mutações do Patrimônio Líquido (DMPL)**.
6. **Demonstração dos Fluxos de Caixa (DFC)**.
7. **Notas Explicativas**.
> Estruturas baseadas em NBC TSP 11/12/13 + Lei 4.320 + LRF. Anexos da Lei 4.320 atualizados pela Portaria STN 438/2012.

---

## 3. CICLO DA DESPESA (Lei 4.320) — empenho / liquidação / pagamento

Três estágios **obrigatórios e sequenciais** (arts. 58-65 da Lei 4.320):

1. **Empenho** — ato de autoridade competente que cria obrigação de pagamento. **É vedada despesa sem prévio empenho** (art. 60). Tipos: ordinário, estimativo, global. Gera **Nota de Empenho**. Lançamento orçamentário (classe 6) reduzindo o saldo da dotação.
2. **Liquidação** — verificação do direito adquirido pelo credor com base em títulos/documentos (nota fiscal, contrato, medição). Confirma "o quê, quanto e a quem pagar". É aqui que tipicamente nasce a **VPD patrimonial** (classe 3) e a obrigação (passivo, classe 2).
3. **Pagamento** — só após regular liquidação; ordem de pagamento; saída financeira (crédito no Caixa/Banco — classe 1).

**Restos a Pagar** (art. 36): despesas empenhadas e não pagas até 31/12, distinguindo:
- **Processados** — já liquidados (direito do credor verificado).
- **Não processados** — empenhados mas não liquidados.

> O sistema deve registrar **lançamentos contábeis simultâneos** nos três enfoques (orçamentário, patrimonial e de controle) a cada estágio — esse é o cerne do "motor PCASP". Cada evento (empenho, liquidação, pagamento, anulação, RP) tem **roteiro de contabilização padronizado no MCASP**; não se inventa partida.

### Lado da receita (Lei 4.320, arts. 51-57)
Estágios: **Previsão → Lançamento → Arrecadação → Recolhimento**. Reconhecimento patrimonial (VPA, classe 4) frequentemente pelo regime de competência, conforme PCP do MCASP.

---

## 4. TCE-RS — SIAPC/PAD, e-Validador, SICOE

O TCE-RS recebe a prestação de contas via **remessas eletrônicas** geradas a partir de leiautes técnicos versionados por exercício.

### 4.1 SIAPC — Sistema de Informações para Auditoria e Prestação de Contas
- Recebe dados de **execução orçamentária, financeira, contábil e patrimonial** dos municípios/câmaras.
- Estruturado em **RDI — Relatório de Dados e Informações** (a remessa que o jurisdicionado entrega; pode conter Folha de Pagamento, Receita Pública, etc., marcados Sim/Não no cabeçalho do RDI).
- A definição prévia dos leiautes está no **Manual Técnico do SIAPC** (Volumes I-V). Volume V trata dos "Arquivos à Disposição do TCE" (dados Lei 4.320).

### 4.2 PAD — Programa Autenticador de Dados (papel do "e-Validador")
- **PAD** é o aplicativo que **valida e autentica** os arquivos antes do envio. Versão **25.0.0.0 para 2025** (versionamento anual — terá 26.x para 2026).
- Função: conferir aderência ao leiaute, consistência e gerar a remessa autenticada (assinatura/hash). **Sem passar no validador, a remessa não é aceita.**
- Cada **cabeçalho** dos arquivos exige **Código da Remessa** — número exclusivo por remessa, obrigatório em todos os arquivos.

### 4.3 Formato dos arquivos (CRÍTICO — não inventar)
Conforme manual técnico SIAPC:
- Arquivos em **formato tabular/sequencial**, **um registro por linha**, **todas as linhas do mesmo tamanho** (registros de **largura fixa**).
- Cada registro termina com **CR/LF** (carriage return + line feed).
- **NÃO se aceita**: campos packed (packed decimal), zoned, binário, ponto flutuante, nem outras codificações de texto fora da especificada.
- **Periodicidade acumulada**: o conteúdo acumula de 1º de janeiro até a data de referência do período.

### 4.4 Periodicidades e prazos (TCE-RS)
| Remessa | Periodicidade | Prazo |
|---|---|---|
| Dados de execução / RDI mensal | Mensal | até **30 dias corridos** após o fim do mês de referência [base Res. 1099/2018 p/ folha; **demais conjuntos a confirmar no Manual Vol. V vigente**] |
| **Folha de Pagamento** | Mensal | até **30 dias corridos** após o encerramento do mês (Res. TCE-RS nº 1099/2018) |
| **SICOE** (obras/jurisdição) | **Trimestral** (acumulado) | até o **último dia útil do mês seguinte** ao trimestre (Res. TCE-RS nº 1074/2017) [a confirmar no Manual SICOE] |
| **Contas Anuais / Ordinárias** | Anual | conforme **Resolução TCE-RS nº 1134/2020** (prazos do exercício) [a confirmar datas exatas do exercício] |

> **Atenção produto:** prazos exatos e quais arquivos compõem cada RDI mudam por resolução/exercício. O sistema deve ter os prazos como **dados configuráveis por exercício**, não hard-coded.

### 4.5 Legislação SIAPC/PAD
Resoluções e Instruções Normativas que tratam de entrega/envio/disponibilização: Portal TCE-RS > Jurisdicionados > Sistemas de Controle Externo > SIAPC > Legislação.

---

## 5. SICONFI (STN) — consolidação nacional

O SICONFI (Sistema de Informações Contábeis e Fiscais do Setor Público Brasileiro) recebe os relatórios fiscais e a matriz contábil de todos os entes.

### 5.1 MSC — Matriz de Saldos Contábeis
- Estrutura de dados da STN para transmitir saldos contábeis e fiscais.
- **Dois tipos:**
  - **MSC Agregada** — **mensal**; saldos das contas contábeis + informações complementares de todos os órgãos/poderes do ente. **Gera automaticamente RREO e RGF.**
  - **MSC de Encerramento** — **anual**; **gera a DCA.**
- **Prazo:** envio **mensal, até o último dia do mês seguinte** ao mês de referência (relativo ao exercício corrente e aos 4 anteriores).
- **Conteúdo dos registros:** conta contábil PCASP, valor/saldo, natureza da informação, indicador de superávit financeiro, e **informações complementares** (correlação com naturezas de receita/despesa, função/subfunção, etc.) — definidas nas **Regras Gerais da MSC (Portaria STN 642 / anexo do exercício)**.
- **Formato:** padrão SICONFI (esquema próprio; carga via XBRL/arquivo estruturado e API) [a confirmar formato exato no anexo de Regras Gerais MSC do exercício].

### 5.2 Relatórios fiscais (LRF) gerados/derivados
| Relatório | Base | Periodicidade | Prazo |
|---|---|---|---|
| **RREO** — Relatório Resumido da Execução Orçamentária | art. 52 LRF | **Bimestral** | até **30 dias após o fim de cada bimestre** |
| **RGF** — Relatório de Gestão Fiscal | art. 54-55 LRF | **Quadrimestral** (municípios; semestral facultativo p/ <50 mil hab.) | até **30 dias após o fim de cada quadrimestre** |
| **DCA** — Declaração de Contas Anuais | Portaria 896/2017 | **Anual** | tipicamente até **30 de abril** do exercício seguinte [confirmar data do exercício na Portaria/cronograma SICONFI] |
| **MSC** | Portaria 896/2017 | Mensal (agregada) / Anual (encerramento) | último dia do mês seguinte |

> No fluxo moderno, **carregadas as MSC dos meses de um quadrimestre, o SICONFI gera o rascunho do RGF** automaticamente; o mesmo vale para RREO (bimestre) e DCA (encerramento). Logo, **a MSC correta é a fonte da verdade** — produzir MSC fiel ao PCASP é o que destrava todo o resto.

### 5.3 Leiautes e instruções
Instruções e guias de preenchimento (RREO, RGF, DCA, MSC) na área pública do SICONFI (versionados por exercício). Portaria STN nº 896/2017 institui os formatos.

---

## 6. O QUE O SISTEMA DE PRODUÇÃO PRECISA TER (checklist milimétrico)

**Plano de contas**
- [ ] PCASP oficial carregado (8 classes, níveis padronizados STN), com atributos (natureza da informação, indicador F/P) e regras de integridade. Versionável por exercício.

**Motor contábil (partidas dobradas)**
- [ ] Roteiros de contabilização do MCASP por evento (empenho, liquidação, pagamento, anulações, RP processados/não processados, receita prevista/arrecadada). Lançamento simultâneo orçamentário + patrimonial + controle.
- [ ] Validação de equação contábil sempre fechada; bloqueio de despesa sem empenho prévio.

**Execução orçamentária**
- [ ] Empenho (ordinário/estimativo/global) → Liquidação (com documentos) → Pagamento → Restos a Pagar (com inscrição/baixa em 31/12).

**Demonstrações (DCASP)**
- [ ] Geração automática dos 7 demonstrativos + notas explicativas, conforme MCASP Parte V.

**Remessas TCE-RS**
- [ ] Geração de arquivos **largura fixa, 1 registro/linha, CR/LF, sem packed/binário**, conforme Manual Técnico SIAPC do exercício.
- [ ] **Código de Remessa** único em todos os cabeçalhos.
- [ ] Compatibilidade total com o **PAD/e-Validador** do exercício (validar antes de mostrar "pronto para envio").
- [ ] RDI mensal (com flags Folha/Receita/etc.); Folha mensal (Res. 1099/2018); SICOE trimestral; Contas Anuais (Res. 1134/2020).
- [ ] Prazos como **configuração por exercício** (não hard-coded).

**Remessas SICONFI**
- [ ] Geração da **MSC Agregada (mensal)** e **MSC de Encerramento (anual)** a partir do PCASP, com informações complementares (Regras Gerais MSC do exercício).
- [ ] RREO (bimestral), RGF (quadrimestral), DCA (anual) — preferencialmente derivados da MSC, batendo com o rascunho gerado pelo SICONFI.
- [ ] Integração via API SICONFI / carga de arquivo no formato vigente.

**Governança**
- [ ] Trilha de auditoria de todo lançamento (quem, quando, evento, documento de origem). Imutabilidade após fechamento de período.
- [ ] Multi-tenant por ente (Prefeitura e Câmara são unidades gestoras/poderes distintos que consolidam na MSC do ente).

---

## 7. O QUE NÃO SE PODE INVENTAR (resumo de riscos)
- **Plano de contas próprio** — proibido; é o PCASP da STN.
- **Roteiros de lançamento "caseiros"** — devem seguir o MCASP; partida inventada gera MSC inválida e reprovação no SICONFI/TCE.
- **Formato de arquivo de remessa** — fixo pelo Manual SIAPC (largura fixa/CR-LF) e pelas Regras Gerais MSC; qualquer desvio é rejeitado pelo PAD/SICONFI.
- **Prazos** — definidos por LRF + resoluções TCE-RS por exercício; não são opinião do produto.
- **Demonstrativos** — estrutura/anexos definidos por Lei 4.320 (Portaria 438/2012) e MCASP Parte V.
- **Versão do leiaute** — TCE-RS e SICONFI versionam **anualmente**; usar versão errada = remessa inválida.

---

## 8. FONTES (URLs)

**STN / MCASP / PCASP**
- MCASP 11ª edição (PDF): https://cnm.org.br/storage/noticias/2024/Links/MCASP%20-%2011%C2%AA%20Edi%C3%A7%C3%A3o.pdf
- PCASP — Tesouro Nacional: https://www.gov.br/tesouronacional/pt-br/contabilidade-e-custos/federacao/plano-de-contas-aplicado-ao-setor-publico-pcasp-1
- MCASP — Tesouro Nacional: https://www.gov.br/tesouronacional/pt-br/contabilidade-e-custos/manuais/manual-de-contabilidade-aplicada-ao-setor-publico-mcasp-1
- MCASP Parte IV — PCASP (PDF): https://tcenet.tce.go.gov.br/Downloads/Arquivos/002980/MCASP-Parte_IV_-_PCASP.pdf
- MCASP Parte V — DCASP (PDF): https://cnm.org.br/cms/images/stories/Links/06072016_MCASP_7_Parte_V_DCASP.pdf
- Regras de Integridade do PCASP: https://contas.cnt.br/mcasp/3-5-regras-de-integridade-do-pcasp/

**Lei 4.320 / despesa**
- Lei 4.320/1964 (Planalto): https://www.planalto.gov.br/ccivil_03/leis/l4320.htm
- Execução da despesa pública (CGU): https://portaldatransparencia.gov.br/entenda-a-gestao-publica/execucao-despesa-publica

**TCE-RS — SIAPC/PAD/SICOE**
- Sistemas de controle externo (portal atual): https://tcers.tc.br/sistemas-de-controle-externo/
- SIAPC (portal jurisdicionados): http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- Manual Técnico SIAPC Vol. V — Arquivos à Disposição (Lei 4.320) (PDF): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf
- Resumo Leiaute Dados à Disposição SIAPC Vol. V v2.0 (PDF): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
- Perguntas Frequentes SIAPC (PDF): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- MT-ASCE-0105 Volume V (MPC-RS) (PDF): https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf
- Manual SICOE (PDF): https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf
- Notícia PAD versão 25.0.0.0 (2025): https://tcers.tc.br/noticia/tce-disponibiliza-versao-de-teste-do-programa-autenticador-de-dados-para-2025/
- Resolução TCE-RS nº 1134/2020 (contas anuais): https://atosoficiais.com.br/tcers/resolucao-n-1134-2020-dispoe-sobre-prazos-documentos-e-informacoes-que-deverao-ser-entregues-ao-tribunal-de-contas-do-estado-do-rio-grande-do-sul-em-formato-eletronico-para-exame-das-contas-anuais-e-ordinarias-da-esfera-municipal-nos-termos-previstos-nos-artigos-71-paragrafo-unico-e-82-do-regimento-interno-aprovado-pela-resolucao-n-1028-de-4-de-marco-de-2015
- Exemplo de RDI (capa, flags Folha/Receita): https://www.camaraboavistadocadeado.rs.gov.br/public/admin/globalarq/conta-publica/arquivo/46f6950d31e6f48dfc7dc9a6cc829eef.pdf

**SICONFI — MSC / RREO / RGF / DCA**
- MSC (Tesouro Transparente): https://www.tesourotransparente.gov.br/consultas/consultas-siconfi/matriz-de-saldos-contabeis-msc
- Regras Gerais MSC 2026 (Anexo I Portaria STN 642) (PDF): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- Regras Gerais / Instruções RGF 2026 (PDF): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Regras_Gerais_e_Instrucoes_de_preenchimento_RGF_04032026.pdf
- Instruções e Guias de Preenchimento (SICONFI): https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=42
- Relatórios Contábeis e Fiscais (Tesouro Transparente): https://www.tesourotransparente.gov.br/temas/contabilidade-e-custos/relatorios-contabeis-e-fiscais-de-estados-df-e-municipios
- API SICONFI — Extratos das declarações (gov.br/conecta): https://www.gov.br/conecta/catalogo/apis/siconfi-extratos-das-declaracoes-contabeis
- Cartilha MSC (ABIPEM) (PDF): https://www.abipem.org.br/wp-content/uploads/2019/05/Cartilha_Matriz_de_Saldos_Contabeis.pdf

> **Próximo passo recomendado:** baixar manualmente os PDFs marcados [a confirmar] (Manual Técnico SIAPC Vol. I-V do exercício 2026, PAD 26.x, Regras Gerais MSC 2026, Res. 1134/2020 e cronograma SICONFI 2026) para extrair os leiautes campo-a-campo — esses não abriram via fetch automático por serem binários/comprimidos.

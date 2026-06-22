# Verificação Cética — Requisitos de Sistema de Gestão Legislativa

> Auditoria de fatos do documento `pesquisa-requisitos-legislativo.md` contra **editais/TRs reais** (PDFs baixados e extraídos com `pdftotext`, não resumos de IA).
> Data: 2026-06-22. Classificação por afirmação: **CONFIRMADO** (com edital/fonte verificada) / **PLAUSÍVEL-SEM-FONTE** / **INCERTO**.
> Método: extração local do texto dos PDFs (E1 Matão, E2 Vitória, E3 Curitiba) + novas buscas no PNCP/portais de Câmaras. Os números de linha citados (`l.NNN`) referem-se ao texto extraído de cada PDF, conferíveis.

---

## 0. Veredito rápido

- **A pesquisa original é, em geral, HONESTA e bem-fundamentada.** A maioria das afirmações com FONTE foi **confirmada na íntegra** ao extrair o texto real dos PDFs E2 (Vitória) e E3 (Curitiba). Os marcadores `[a confirmar]` estavam corretamente colocados.
- **Correções factuais necessárias (2):** valor do contrato de Matão; e o status de "LexML" deve ser rebaixado de citado-no-ecossistema para **raramente exigido em texto de edital**.
- **Achados NOVOS que reforçam a tese (importantes para PoC):** existe edital de Câmara que **junta contabilidade/prestação de contas + folha** no mesmo objeto (CMBelacruz/CE) e existe **prova de conceito real** de Câmara com escopo ERP integrado (Salto/SP) — exatamente o cenário-alvo do Tensorroot.Gov.

---

## 1. Fontes verificadas (texto real extraído, não resumo)

| ID | Órgão | O que foi efetivamente verificado | Status da fonte |
|----|-------|-----------------------------------|-----------------|
| **E1** | Câmara de Matão/SP — PE 06/2024 | Objeto e módulos confirmados via página oficial; **PDF do Memorial não extraído** (permaneceu inacessível) | Página CONFIRMADA; Memorial INCERTO (itens numerados) |
| **E2** | Câmara de Vitória/ES — TR Votação | **PDF de 33 págs. extraído integralmente** (1.603 linhas). Quase todas as citações conferidas. | CONFIRMADA (texto integral) |
| **E3** | Câmara de Curitiba/PR — ETP PA 00264/2024 | **PDF extraído integralmente** (1.866 linhas). Citações de deliberação remota, SPL, votação simbólica conferidas. | CONFIRMADA (texto integral) |
| **REF** | SAPL/Interlegis | Módulos confirmados na página oficial. **SAPL não menciona LexML.** | CONFIRMADA |
| **NOVO-A** | Câmara de Belacruz/CE — minuta de TR | TR de Câmara que inclui prestação de contas SICONFI/STN, SIOPE, SIOPS, depreciação, folha de pagamento, transparência | CONFIRMADA (PDF extraído, 1.830 linhas) |
| **NOVO-B** | Câmara de Salto/SP — Convocação para Prova de Conceito (PE 2024) | **PoC real** com escopo: folha, compras, contratos, almoxarifado, patrimônio, processo legislativo, controle interno, portal transparência, e-SIC, ouvidoria | CONFIRMADA (página oficial) |

---

## 2. Verificação item a item (checklist da pesquisa)

### Bloco 1 — Processo Legislativo
| Afirmação da pesquisa | Veredito | Evidência verificada |
|---|---|---|
| Proposições por tipo (PL, requerimento, indicação, moção, emenda) | **PLAUSÍVEL-SEM-FONTE** (tipos exatos) / baseline CONFIRMADO | SAPL: "Elaboração e tramitação de proposições" confirmado. Os **tipos nominais** não aparecem textualmente em E1/E2/E3 — corretamente marcados `[a confirmar item exato]`. |
| Protocolo web | **CONFIRMADO** | E1 objeto: "protocolo web" (página oficial Matão). |
| Tramitação / consulta de matérias em tramitação | **CONFIRMADO** | E2: "consultas de proposições...em tramitação" (l.92); E3: SPL faz "consulta a dados de sessões plenárias (como ordem do dia...)" (l.646-647). |
| Comissões e membros efetivos/suplentes | **CONFIRMADO (literal)** | E2 l.329: *"3.13 Cadastrar comissões e seus membros, efetivos e suplentes"*. Base única plenário+comissões: E2 l.256-257. |
| Pareceres / reuniões de comissão | **CONFIRMADO** | E3 l.649: SPL faz "lançamento de atas e pautas de reuniões das comissões, informação de instruções". |
| Emendas | **PLAUSÍVEL-SEM-FONTE** | Só baseline SAPL; sem item textual em edital. Marcação correta. |
| Assinatura/certificado digital | **CONFIRMADO** | E1: "certificados digitais" (confirmado na página oficial). |

### Bloco 2 — Sessões
| Afirmação | Veredito | Evidência |
|---|---|---|
| Gerenciamento de sessão (todas as etapas) | **CONFIRMADO** | E2 l.76: "Solução de Gerenciamento da Sessão". |
| Pauta editável / ordem do dia | **CONFIRMADO** | E2 l.981: "ordem do dia das sessões plenárias contendo os seguintes campos". |
| Geração automática da Ordem do Dia (via tramitação) | **CONFIRMADO** | E2 l.91: "para a geração automática da Ordem do Dia". |
| Registro de presença + recomposição de quórum | **CONFIRMADO (literal)** | E2 l.512-513: *"3.44 Realizar recomposições de quórum, com cancelamento do quórum anterior e novo registro de presença"*. |
| Geração automática da Ata Sintética | **CONFIRMADO (literal)** | E2 l.149: *"2.1.8 Geração e emissão automática da Ata Sintética"*; l.926-927: ata = resumo da sessão plenária **e reuniões de Comissões**. |
| Tipos de sessão ordinária/extraordinária | **CONFIRMADO** | E3 l.64-66: "preparatórias, ordinárias, extraordinárias, solenes e especiais". (Mais tipos do que a pesquisa citou.) |
| Cronômetro | **CONFIRMADO (literal)** | E2 l.462-463: "3.36 Controlar os cronômetros"; E3 l.80: "cronômetro digital auxiliar". |
| Oradores / tribuna / apartes | **CONFIRMADO** | E2 l.730/882: orador, aparteante; E3 l.75: "gerenciamento de oradores, pela inscrição de oradores". |

### Bloco 3 — Votação
| Afirmação | Veredito | Evidência |
|---|---|---|
| Parâmetros e controle de execução (abrir/fechar/cancelar) | **CONFIRMADO** | E2: controle de votação e terminais (l.111, l.123). |
| Votação nominal remota | **CONFIRMADO (literal)** | E3 l.222-223: "registro de presença, votação nominal, pedido de palavra, inscrição para o uso da tribuna...de forma totalmente remota e on-line". |
| Votação simbólica — `[a confirmar; em Curitiba a simbólica NÃO é informatizada]` | **CONFIRMADO (a ressalva estava certa)** | E3 l.501: processo "informatizado, **com exceção das votações simbólicas**". Ou seja, a pesquisa acertou: simbólica raramente é informatizada. |
| Parlamentares impedidos de votar | **CONFIRMADO (literal)** | E2 l.509: "impedidos de votar". |
| Painel apregoador (totalizadores, multimídia, TV Câmara) | **CONFIRMADO** | E2 l.126/130/730: painel apregoador, conteúdo da TV Câmara, orador/aparteante. |
| Terminais físicos + hot-swap | **CONFIRMADO (literal)** | E2 l.820-822: "terminais...poderão ser trocados hot swap, mesmo durante uma votação". |
| Deliberação remota (módulo) | **CONFIRMADO (literal)** | E3 l.219: "(iv) módulo para deliberação remota"; item de locação l.1333/1536. |

### Bloco 4 — Transparência
| Afirmação | Veredito | Evidência |
|---|---|---|
| Diário Oficial eletrônico | **CONFIRMADO** | E1: "diário oficial". |
| Transmissão das sessões (TV Câmara/internet) | **CONFIRMADO** | E2 l.764-768: transmissão da TV Câmara; E3 l.1601: "via transmissão pela internet". |
| Relatórios (presenças, votações, pauta) | **CONFIRMADO** | E2 l.519: "Presenças por reunião"; relatórios de votação. |
| Acompanhamento pelo cidadão em tempo real | **CONFIRMADO** | REF SAPL "Acompanhamento da produção legislativa pelos cidadãos". |
| LAI 12.527 / LC 131 exigência textual | **INCERTO** (continua `[a confirmar]`) | Não localizado item textual nos PDFs extraídos. Marcação correta. |

### Bloco 5 — Normas Jurídicas / LexML
| Afirmação | Veredito | Evidência |
|---|---|---|
| Base de leis consultável | **CONFIRMADO** | REF SAPL: "Manutenção da base de leis"; E3 l.646: SPL faz "consulta à legislação". |
| **LexML como padrão exigido** | **REBAIXAR → PLAUSÍVEL-SEM-FONTE / raro** | LexML é padrão nacional real (URN única por norma — lexml.gov.br), mas **não aparece textualmente** em E1/E2/E3 nem na página do SAPL. O mercado usa "consolidação/compilação" (ex.: leismunicipais.com.br) sem citar LexML. **Não tratar como obrigatório de facto.** |
| Compilação/consolidação de normas | **PLAUSÍVEL-SEM-FONTE** | SAPL 3.1 cita "compilação de textos articulados" (REF). Conceito comum, item textual `[a confirmar]`. |

### Bloco 6/7 — Contabilidade + TCE + Folha + eSocial da Câmara
| Afirmação | Veredito | Evidência |
|---|---|---|
| Câmara é tenant/UO próprio com contabilidade e prestação ao TCE | **CONFIRMADO (reforçado por achado novo)** | **NOVO-A (CMBelacruz/CE)**: TR de Câmara inclui prestação de contas a **SICONFI/STN, SIOPE/FNDE, SIOPS/MS**, Matriz de Saldos Contábeis, depreciação/amortização, extratos bancários eletrônicos, e "prestação de contas...da Câmara Municipal" (l.90, 194, 207, 212). |
| Importação/contabilização de folha | **CONFIRMADO** | NOVO-A: "notas de pagamento referentes às folhas de pagamento" (l.174); + TR Cerro Negro citado na pesquisa. |
| eSocial em Câmara | **PLAUSÍVEL-SEM-FONTE** (item textual de Câmara) | Plausível e comum no mercado; **NOVO-A não cita eSocial nominalmente** no trecho extraído. Continua `[a confirmar item]`. |
| Folha + subsídios de vereadores | **CONFIRMADO (escopo)** | **NOVO-B (Salto/SP, PoC)**: módulo "gestão de pessoal e folha de pagamento" entre os demonstrados na prova de conceito. |

---

## 3. Correções factuais ao documento original

1. **Valor do contrato de Matão (E1).** A pesquisa registra "R$ 52.823,90 máx.". O **contrato efetivamente assinado** com a SOFTCAM foi **R$ 49.950,00** (06/12/2024), com aditivo de R$ 43.961,40 para prorrogação de 12 meses (03/12/2025). Ajustar (o valor original pode ter sido o teto/estimado, mas o número adjudicado é outro). Fonte: página oficial Matão.
2. **LexML.** Rebaixar de "citado no ecossistema → provável requisito" para **raramente exigido em texto de edital**. Não usar como gap prioritário de PoC.
3. **Tipos de sessão.** E3 lista mais tipos do que a pesquisa ("preparatórias, ordinárias, extraordinárias, solenes e especiais"). Detalhe menor, mas amplia o cadastro de tipos de sessão.

---

## 4. Requisitos OBRIGATÓRIOS DE FACTO (recorrentes em múltiplas fontes)

Aparecem em **2+ editais/baseline** verificados — são os que **toda PoC de Câmara provavelmente cobra**:

1. **Sessão plenária: gerenciar pauta + ordem do dia + presença/quórum + ata** (E2, E3, SAPL, NOVO-B). **NÚCLEO.**
2. **Votação eletrônica nominal, com abrir/fechar/cancelar e resultado em painel** (E1, E2, E3). **NÚCLEO.**
3. **Painel eletrônico/apregoador** exibindo presentes/ausentes/orador/resultado (E2, E3, mercado: Plenus, aPlenário, SIVECAM). **NÚCLEO (mas frequentemente acoplado a hardware/terminais — ver risco).**
4. **Recomposição de quórum com cancelamento e novo registro** (E2; padrão de regimento). **Recorrente.**
5. **Cronômetro de tribuna/apartes + inscrição de oradores** (E2, E3). **Recorrente.**
6. **Comissões: cadastro de membros (efetivos/suplentes) + atas/pautas de reunião** (E2, E3, SAPL). **Recorrente.**
7. **Proposições + tramitação + protocolo web** (E1, E3, SAPL). **NÚCLEO do processo legislativo.**
8. **Base de leis/normas consultável + Diário Oficial eletrônico** (E1, E3, SAPL). **Recorrente.**
9. **Portal do cidadão / transparência + transmissão das sessões** (E2, E3, SAPL, NOVO-A/B). **Recorrente.**
10. **Assinatura/certificado digital nas peças** (E1; Lei 14.063/2020). **Recorrente.**
11. **Para Câmaras que compram ERP completo: contabilidade + prestação de contas (SICONFI/TCE) + folha** no mesmo objeto/PoC (NOVO-A, NOVO-B). **Recorrente no segmento "sistema integrado de Câmara".**

### Requisitos RAROS / ESPECÍFICOS (não tratar como obrigatórios de facto)
- **Deliberação remota** (voto/tribuna online): forte em E3 (Curitiba), mas é demanda de Câmaras grandes pós-pandemia; **não universal**. PLAUSÍVEL como diferencial, não como obrigatório mínimo.
- **Terminais físicos + hot-swap + painel LED multimídia** (E2 Vitória): isso é **licitação de hardware+software de plenário**, escopo diferente de "software de processo legislativo". Cuidado: nem toda PoC exige integração com terminais físicos.
- **Votação simbólica informatizada:** **explicitamente NÃO informatizada** em Curitiba (E3 l.501). Não é gap.
- **LexML verbatim:** raro.
- **eSocial nominal em TR de Câmara:** plausível, não confirmado textualmente nas fontes verificadas.

---

## 5. Impacto nos GAPS do Tensorroot.Gov (revisão dos gaps da pesquisa)

| Gap apontado pela pesquisa | Veredito do auditor |
|---|---|
| 1. Comissões + pareceres + reuniões | **VÁLIDO e recorrente** (E2/E3/SAPL). Priorizar. |
| 2. Normas Jurídicas + Diário Oficial | **VÁLIDO** (recorrente). **Mas remover LexML do caminho crítico.** |
| 3. Deliberação remota | **REBAIXAR a diferencial**, não mínimo. Não bloqueia a maioria das PoCs. |
| 4. Ata Sintética automática | **VÁLIDO** (E2 literal). Priorizar. |
| 5. Integração com painel/terminais físicos | **VÁLIDO porém condicional ao tipo de edital.** Se o alvo é "software de processo legislativo" (Matão/Salto), o painel pode ser por vídeo/web; integração com hardware de terceiros é exigência de editais de plenário (Vitória/Curitiba). Decidir por edital-alvo. |
| 6. Contabilidade+TCE+folha como tenant Câmara | **VÁLIDO e CONFIRMADO por edital real (NOVO-A) e PoC real (NOVO-B).** Forte vantagem nossa (PCASP/TCE-RS/SICONFI já provados). |

---

## 6. Pendências remanescentes (honestas)

- **Memorial Descritivo de Matão (E1):** continua não extraído — itens numerados de proposições/normas seguem `[a confirmar]`.
- **TCE-RS especificamente:** as fontes de prestação de contas verificadas são SICONFI/STN/SIOPE/SIOPS (federais, em TR de Câmara CE) e TCE-SC/SP. Falta um **TR de Câmara gaúcha** exigindo **SIAPC/PAD do TCE-RS** nominalmente — buscar no PNCP/Licitacon-RS. `[a confirmar]`.
- **eSocial em TR de Câmara:** localizar item textual.
- **LexML:** confirmado que **não** é exigência textual comum; encerrar a busca como "raro".

---

## 7. Fontes (URLs verificadas)

- E1 Matão/SP — PE 06/2024: https://www.camaramatao.sp.gov.br/portal/editais/0/1/79/
- E2 Vitória/ES — TR Votação (PDF extraído): https://www.cmv.es.gov.br/uploads/licitacao/2962-termo-de-referencia-e-justificativa-1748288531.pdf
- E3 Curitiba/PR — ETP PA 00264/2024 (PDF extraído): https://mid-transparencia.curitiba.pr.gov.br/contratos/licitacoes/2024/CMC_2024_PE_19_221519_59219.pdf
- REF SAPL/Interlegis: https://www12.senado.leg.br/interlegis/produtos/sapl
- NOVO-A CMBelacruz/CE — minuta de TR (PDF extraído): https://www.cmbelacruz.ce.gov.br/arquivos_download/licitacao/22/114
- NOVO-B Salto/SP — Convocação Prova de Conceito: https://www.camarasalto.sp.gov.br/noticias/3831-convocacao-para-prova-de-conceito
- Pirauba/MG — TR Câmara (não extraído, 403): https://pirauba.mg.leg.br/arquivo/68daeb2067e40.pdf `[a confirmar]`
- LexML Brasil (padrão nacional): https://www.lexml.gov.br/

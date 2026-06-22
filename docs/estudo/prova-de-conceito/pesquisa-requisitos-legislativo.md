# Pesquisa de Requisitos — Sistema de Gestão Legislativa (Câmara de Vereadores)

> Insumo para provas de conceito/aderência (Lei 14.133/2021). Toda afirmação com FONTE (URL/edital) ou marcada `[a confirmar]`.
> Data: 2026-06-22.

## Editais/TRs reais consultados (FONTES)

| # | Órgão | Documento | Objeto | URL |
|---|-------|-----------|--------|-----|
| E1 | Câmara Municipal de Matão/SP | Pregão Eletrônico nº 06/2024 — TR + Memorial Descritivo | Software de processo legislativo eletrônico/digital web: protocolo web, votação eletrônica, diário oficial, certificados digitais, implantação, migração, suporte, treinamento. Adjudicado à SOFTCAM (R$ 52.823,90 máx.) | https://www.camaramatao.sp.gov.br/portal/editais/0/1/79/ |
| E2 | Câmara Municipal de Vitória/ES | Termo de Referência — Sistema Eletrônico de Votação (locação) | Painel apregoador multimídia LED + terminais de votação/presença + software de gerenciamento de sessão integrado ao Sistema de Tramitação de Processos Legislativos; geração automática de Ata Sintética | https://www.cmv.es.gov.br/uploads/licitacao/2962-termo-de-referencia-e-justificativa-1748288531.pdf |
| E3 | Câmara Municipal de Curitiba/PR | ETP — PA 00264/2024 (Software Plenário) | Software de apuração, frequência e votação, presencial e **remoto** (deliberação remota), painel eletrônico, integração com o "SPL" (Sistema de Proposições Legislativas interno) | https://mid-transparencia.curitiba.pr.gov.br/contratos/licitacoes/2024/CMC_2024_PE_19_221519_59219.pdf |
| REF | Interlegis/Senado | SAPL — Sistema de Apoio ao Processo Legislativo (padrão de fato gratuito) | Elaboração/tramitação de proposições, sessões plenárias, base de leis, mesa diretora, comissões, votações, acompanhamento cidadão. Usado como baseline funcional por muitos editais municipais | https://www12.senado.leg.br/interlegis/produtos/sapl |

Observação: os TRs E2/E3 cobrem o **plenário/votação**; o processo legislativo completo (proposições/comissões/normas) aparece em E1 (Memorial Descritivo) e no baseline SAPL/SAGL. O Memorial de Matão não pôde ser baixado (download bloqueado no ambiente) — `[a confirmar]` os números de item exatos via https://www.camaramatao.sp.gov.br/portal/download/licitacoes-anexos/hRoAx/.

---

## CHECKLIST DE REQUISITOS (por bloco)

### 1. Processo Legislativo (proposições / tramitação / comissões / pareceres)
- [ ] Cadastro de **proposições/matérias** por tipo: projeto de lei, requerimento, indicação, moção, emenda — `[a confirmar item exato]` (baseline SAPL "elaboração e tramitação de proposições"; E1 objeto "processo legislativo eletrônico"). FONTES: REF; E1.
- [ ] **Protocolo web** de documentos/proposições (E1 "protocolo web"). FONTE: E1.
- [ ] **Tramitação** com etapas/fluxo e consulta de matérias em tramitação — "consultas de proposições e projetos em tramitação" (E2 §92-93); painel exibe "tramitação, resultado das votações com data e número da sessão" (E2 l.983). FONTES: E2; REF.
- [ ] Cadastro de **comissões e seus membros, efetivos e suplentes** — "3.13 Cadastrar comissões e seus membros, efetivos e suplentes" (E2). Sessões/votação aplicáveis a plenário **e comissões**, base única (E2 §256-257). FONTE: E2.
- [ ] **Pareceres** / instruções e reuniões de comissões — SPL de Curitiba faz "lançamento de atas e pautas de reuniões das comissões, informação de instruções" (E3 l.649). FONTE: E3. `[a confirmar exigência explícita de parecer em E1]`.
- [ ] **Emendas** a proposições — `[a confirmar item exato]` (baseline SAPL). FONTE: REF.
- [ ] **Assinatura/certificado digital** nas peças (E1 "certificados digitais"). FONTE: E1.

### 2. Sessões (pauta / ordem do dia / ata / presença)
- [ ] **Gerenciamento de sessão**: edição e acompanhamento de todas as etapas (E2 §141, item 6 "Solução de Gerenciamento da Sessão"). FONTE: E2.
- [ ] **Pauta** cadastrada por fase com restrições; editável a qualquer momento da reunião; itens: votações, oradores, convidados (E2 §368-369, 407-411). FONTE: E2.
- [ ] **Geração automática da Ordem do Dia** a partir da integração com a tramitação (E2 §90-91). FONTE: E2.
- [ ] **Registro de presença** por reunião; recomposição de quórum com cancelamento e novo registro (E2 §511-513); controle de apuração/frequência presencial e remoto (E3 §74). FONTES: E2; E3.
- [ ] **Geração e emissão automática da Ata Sintética** ao final das sessões (E2 §149-150, item 2.1.8). FONTE: E2.
- [ ] Tipos de sessão: ordinária, extraordinária (E3 §68). FONTE: E3.
- [ ] **Cronômetro** digital para discursos/apartes/tribuna (E3 §80,99). FONTE: E3.
- [ ] Inscrição de **oradores** (incluir/atribuir fala), tribuna, apartes, questão de ordem, pela ordem, grande expediente (E2 §539-540; E3 §76-77). FONTES: E2; E3.

### 3. Votação (painel eletrônico / nominal / simbólica)
- [ ] **Cadastro de parâmetros de votação** e de votações para plenário (E2 §385-388). FONTE: E2.
- [ ] **Controle de execução**: abrir, fechar, cancelar votação; adicionar/remover votação da pauta rapidamente (E2 §474-479). FONTE: E2.
- [ ] **Votação nominal** registrada eletronicamente, inclusive remota (E3 §223). FONTE: E3.
- [ ] **Votação simbólica** — `[a confirmar suporte no software; em Curitiba a simbólica permanece NÃO informatizada]` (E3 §501). FONTE: E3.
- [ ] Identificar **parlamentares impedidos de votar** durante a votação (E2 §508-509). FONTE: E2.
- [ ] **Painel apregoador/eletrônico** exibindo: votação aberta, sessão aberta, resultado de votação, orador, aparteante, totalizadores (presentes/ausentes/licenciados em cor configurável), mensagens, conteúdo multimídia/TV Câmara (E2 §427, 686-687, 714-715, 729-737). FONTE: E2.
- [ ] **Terminais de votação e presença** físicos, identificação por parlamentar, hot-swap durante votação (E2 §771-821). FONTE: E2.
- [ ] **Deliberação remota** (módulo): presença, votação nominal, pedido de palavra, inscrição na tribuna, on-line e em tempo real (E3 §219-224); debate e declaração de voto remotos (E3 §54). FONTE: E3.
- [ ] Encaminhamento/justificativa de votação; justificativas de votações em relatório (E2 §522; E3 §77). FONTES: E2; E3.

### 4. Transparência legislativa / Portal do Cidadão
- [ ] Acesso público a Mesa, Comissões, Parlamentares, Ordem do Dia, Sessões, Proposições e **Normas Jurídicas** (baseline SAPL/Portal Modelo). FONTE: REF.
- [ ] Acompanhamento, pelo cidadão, das proposições lidas/votadas em tempo real (E3 §90-91; E2 §224). FONTES: E2; E3.
- [ ] **Diário Oficial** eletrônico (E1 "diário oficial"). FONTE: E1.
- [ ] **Transmissão** das sessões (TV Câmara/internet) integrada ao painel (E2 §764-767). FONTE: E2.
- [ ] **Relatórios**: presenças por reunião, votações com data/nº da sessão, resultado das votações, pauta da reunião (E2 §516-525, 957, 983). FONTE: E2.
- [ ] Aderência à Lei de Acesso à Informação (LAI 12.527/2011) e LC 131/2009 — `[a confirmar exigência textual nos editais]`.

### 5. Normas Jurídicas / LexML / Legislação
- [ ] **Base de leis/normas** mantida e consultável (baseline SAPL "manutenção da base de leis"; SPL Curitiba "consulta à legislação" E3 §646-647). FONTES: REF; E3.
- [ ] Consulta com padrão **LexML** (busca de normas) — citado no ecossistema Interlegis/Portal Modelo. FONTE: REF (https://www12.senado.leg.br/interlegis/produtos/sapl). `[a confirmar exigência explícita de LexML em edital municipal]`.
- [ ] Compilação/consolidação de normas e vínculo norma↔proposição de origem — `[a confirmar item exato]` (baseline SAGL). FONTE: REF.

### 6. Contabilidade própria da Câmara + Prestação ao TCE
- [ ] A Câmara é **tenant/UO próprio**: contabilidade PCASP independente do Executivo; prestação de contas anual ao TCE (ex.: IN TC 20/2015 TCE-SC, envio até 28/02). FONTE: TCE-SC https://www.tcesc.tc.br/sites/default/files/guia%20ONLINE_0.pdf. `[confirmar regra equivalente TCE-RS para nosso alvo]`.
- [ ] Integração/exportação para o **sistema do TCE** (ex.: AUDESP TCE-SP; SIAPC/PAD TCE-RS) e SICONFI — `[a confirmar layout específico exigido por edital de Câmara]`. FONTE TCE-SP AUDESP: https://www.tce.sp.gov.br/audesp/documentacao.
- [ ] Importação de arquivos de **contabilização de folha** (TR municipal Cerro Negro/SC menciona import de folha → contabilidade). FONTE: https://cerronegro.sc.gov.br/uploads/sites/409/2024/06/Termo-de-Referencia-RETIFICADO.pdf.

### 7. eSocial / Folha de Pagamento da Câmara
- [ ] **Folha de pagamento** dos servidores e subsídios dos vereadores — `[a confirmar item exato em edital de Câmara]`.
- [ ] **eSocial**: processamento, validação, transmissão e monitoramento de eventos periódicos (Câmaras contratam esse serviço — confirmado em pesquisa de mercado). FONTE: busca PNCP/TCE (resumo). `[a confirmar TR específico de Câmara com itens]`.

---

## Aderência do nosso sistema (do diagnóstico ESTADO-ATUAL.md)
- Módulo **Legislativo** já cobre Sessão, Votação (maioria/modalidade) e Proposição com painel — "maior cobertura de casos de uso", porém **sem integração externa** e sem detalhamento de comissões/pareceres/normas. FONTE interna: `docs/diagnostico/ESTADO-ATUAL.md` (linha 37).
- **GAPS prováveis vs. editais** a tratar antes de PoC:
  1. Comissões + pareceres + reuniões de comissão (E2/E3) — `[validar implementação]`.
  2. Base de **Normas Jurídicas/LexML** e Diário Oficial eletrônico (E1/REF).
  3. **Deliberação remota** (presença/voto/tribuna on-line) (E3).
  4. **Ata Sintética automática** ao fim da sessão (E2 item 2.1.8).
  5. Integração com **painel/terminais físicos** (protocolo de troca: pauta/ordem do dia ⇄ votos/faltas) (E2/E3).
  6. **Contabilidade + prestação ao TCE + folha/eSocial** como tenant da Câmara — já temos PCASP/TCE-RS/SICONFI provados; validar recorte "Câmara".

## Pendências [a confirmar]
- Baixar Memorial Descritivo de Matão (E1) para itens numerados de proposições/tramitação/normas: https://www.camaramatao.sp.gov.br/portal/download/licitacoes-anexos/hRoAx/ (download bloqueado no ambiente).
- Buscar edital de Câmara com **contabilidade + folha + eSocial + processo legislativo no mesmo objeto** (PNCP) para checklist unificado.
- Confirmar exigência textual de **LexML** e de **votação simbólica informatizada** em algum edital municipal.
- Confirmar regra de prestação de contas de Câmara no **TCE-RS** (alvo declarado), não só TCE-SC/SP.

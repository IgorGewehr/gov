# Módulo Legislativo
> Gestão do processo legislativo municipal — proposições, sessões, votações e controle externo da Câmara. · Poder: Legislativo · Schema EF Core: `legislativo` · Ativável por tenant.

## 1. Propósito & Marco Legal
Suporta o ciclo completo do processo legislativo da Câmara Municipal: protocolo e tramitação de proposições, sessões plenárias, deliberação por votação e formalização de atos (autógrafo → sanção/veto → promulgação). Garante conformidade e trilha de prova jurídica.

Marco legal: **CF/88 art. 29** (Lei Orgânica e iniciativa), **art. 29-A** (nº de vereadores por faixa populacional; limite de despesa do Legislativo de 7% a 3,5% da receita tributária + transferências), **art. 30** (competências municipais), **art. 31** (controle externo com auxílio do TCE). **LRF (LC 101/2000) art. 20, III, "a"**: despesa total com pessoal do Legislativo ≤ 6% da RCL. **Lei Orgânica Municipal (LOM)** — alterável só por 2/3 em dois turnos; **Regimento Interno** aprovado por Resolução. Processo legislativo em simetria com **CF arts. 59–69**.

## 2. Linguagem Ubíqua
- **Proposicao**: matéria submetida à apreciação do Plenário.
- **ProjetoDeLeiOrdinaria (PLO)** / **ProjetoDeLeiComplementar (PLC)**: normas de iniciativa legislativa.
- **EmendaALOM**: proposta de alteração da Lei Orgânica (rito qualificado).
- **ProjetoDeDecretoLegislativo (PDL)**: matéria de competência exclusiva da Câmara.
- **ProjetoDeResolucao (PR)**: regula assuntos internos (ex.: Regimento).
- **Requerimento / Indicacao / Mocao**: instrumentos de manifestação parlamentar.
- **Emenda / Substitutivo**: modificações a proposição em curso.
- **Parecer**: manifestação de Comissão sobre a matéria.
- **OrdemDoDia / Expediente**: partes da sessão (deliberação / comunicações).
- **Autografo**: texto final aprovado, enviado ao Executivo.
- **Sancao / Veto / Promulgacao**: atos de aperfeiçoamento da lei.
- **Quorum**: número mínimo para instalar sessão ou deliberar.
- **MesaDiretora / Comissao**: órgãos de direção e instrução.
- **SessaoOrdinaria / SessaoExtraordinaria**: tipos de reunião plenária.
- **VotacaoSimbolica / VotacaoNominal / VotacaoSecreta**: modalidades de apuração.

## 3. Mapa de Domínio
**Agregados (raiz) — todos `IMustHaveTenant`:**
- **Proposicao** — VOs `TipoProposicao`, `Ementa`, `Autoria`, `RegimeTramitacao`; entidades `Emenda`, `Substitutivo`, `Tramitacao` (fases).
- **Sessao** — VOs `TipoSessao`, `DataHora`, `OrdemDoDia`, `QuorumInstalacao`; entidade `Presenca`.
- **Votacao** — VOs `TipoVotacao`, `MaioriaExigida`, `Resultado`; entidade `Voto`.
- **Comissao** — entidades `Membro`, `Relatoria`, `Parecer`.
- **MesaDiretora**; **Vereador/Mandato**.

**Eventos de Domínio:** `ProposicaoApresentada`, `ProposicaoDistribuida`, `EmendaApresentada`, `ParecerEmitido`, `SessaoAberta`, `QuorumVerificado`, `VotacaoIniciada`, `VotoRegistrado`, `VotacaoEncerrada`, `ProposicaoAprovada`, `ProposicaoRejeitada`, `ProposicaoArquivada`, `AutografoEnviado`.

## 4. Integrações & Padrões Técnicos
- **Painel Eletrônico de Votação**: captura presença, abre/fecha votação e exibe placar nominal em tempo real (WebSocket/event stream). Idempotência por `votoId`.
- **SAPL/Interlegis**: interoperação e importação de tramitação.
- **Publicação de atos**: Diário Oficial do município / LeisMunicipais; PDF assinado do autógrafo via **ICP-Brasil**.
- **Transmissão + ata eletrônica**; **APIs abertas (LAI)**.
- Persistência EF Core 8 (schema `legislativo`); efeitos colaterais e Integration Events via **Outbox**; comandos/consultas via **MediatR**.

## 5. Regras de Negócio Críticas
- **Quórum de instalação** = maioria absoluta (> 50% dos membros).
- **Maiorias de deliberação**: simples (> 50% dos presentes) p/ lei ordinária; absoluta (> 50% dos membros) p/ LC, derrubada de veto e Regimento; qualificada **2/3 em dois turnos** p/ Emenda à LOM.
- **Pareceres da CCJ (constitucionalidade)** e de **Finanças/Orçamento** obrigatórios — ausência vicia a tramitação.
- **Sanção/veto** do prefeito em **15 dias úteis**; silêncio = sanção tácita. Veto derrubável por **maioria absoluta**.
- **Arquivamento** ao fim da legislatura, salvo exceções regimentais.
- **Trilha imutável** de votos, presenças e pareceres (prova jurídica + LAI).

## 6. Multi-Tenancy, Segurança & Auditoria
A **Câmara é tenant distinto** do Executivo: isolamento físico-lógico por `TenantId` (global query filter EF Core); todo agregado implementa `IMustHaveTenant`. Acesso por papéis (Vereador, Mesa, Secretaria Legislativa, Comissão). Auditoria append-only e imutável sobre votos/presenças/pareceres/tramitação, atendendo prova jurídica e LAI. Ativação do módulo por tenant via feature flag.

## 7. Contratos Públicos (Integration Events)
Cross-module **somente** via Integration Events (sem referência direta entre BCs):
- **Publica**: `AutografoEnviado` (→ Executivo, para sanção/veto/promulgação).
- **Consome**: `Sancao`, `Veto` (← Executivo), atualizando a `Tramitacao` da `Proposicao`.
- Publica eventos de transparência (sessões, resultados) para o portal LAI/APIs abertas.

## 8. Cenários BDD
**Cenário 1 — Instalação de sessão**
- **Dado** uma `SessaoOrdinaria` agendada com 11 membros, **Quando** comparecerem 6 vereadores, **Então** o `QuorumInstalacao` (maioria absoluta) é atingido e `SessaoAberta` é emitido.

**Cenário 2 — Aprovação de lei ordinária**
- **Dado** um PLO em `OrdemDoDia` com pareceres da CCJ e de Finanças favoráveis, **Quando** a `VotacaoSimbolica` registrar > 50% dos presentes a favor, **Então** `ProposicaoAprovada` é emitido.

**Cenário 3 — Vício por ausência de parecer**
- **Dado** um PLC sem `Parecer` da CCJ, **Quando** for incluído em `OrdemDoDia`, **Então** a inclusão é rejeitada por vício de tramitação.

**Cenário 4 — Autógrafo ao Executivo**
- **Dado** uma `Proposicao` aprovada, **Quando** o `Autografo` for gerado e assinado (ICP-Brasil), **Então** `AutografoEnviado` é publicado ao Executivo via Outbox.

**Cenário 5 — Derrubada de veto**
- **Dado** um `Veto` recebido do Executivo, **Quando** a `VotacaoNominal` alcançar maioria absoluta pela rejeição do veto, **Então** a matéria segue à promulgação.

**Cenário 6 — Idempotência do painel**
- **Dado** uma `Votacao` aberta no painel eletrônico, **Quando** o mesmo `votoId` chegar duas vezes, **Então** apenas um `VotoRegistrado` é persistido.

## 9. Fontes
- CF/88 — https://www.planalto.gov.br/ccivil_03/constituicao/constituicao.htm
- LRF (LC 101/2000) — https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp101.htm
- Tramitação (referência) — https://www2.camara.leg.br/
- SAPL/Interlegis — https://www.interlegis.leg.br/

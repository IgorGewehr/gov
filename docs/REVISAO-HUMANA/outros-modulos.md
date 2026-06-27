# Demais Módulos — Lista de Regras por Área

> **O que esta página é.** A lista enxuta das regras dos módulos **sociais, legislativo, patrimônio,
> protocolo e identidade** — para quem precisa de uma visão geral ou é especialista de uma dessas
> áreas. Aqui o foco é **localizar** o `.rules.md` e saber a norma principal que ele segue (sem o
> aprofundamento das páginas contábil/folha/tributos/licitações).
>
> ⭐ marca as regras com **norma legal/fiscal "dura"** (que um especialista deve conferir com cuidado).
> As demais são operacionais (fluxo de cadastro/agenda), de menor risco normativo.
>
> Pasta dos arquivos: `src/Modules/<Módulo>/rules/`.

---

## Patrimônio (7 regras)

| Regra | Norma principal | Conferir? |
|---|---|---|
| ⭐ [`BemPatrimonial.rules.md`](../../src/Modules/Patrimonio/rules/BemPatrimonial.rules.md) | **Depreciação MCASP/SNC** (Portaria STN 828/2014; método linear; vida útil por classe) | **Sim** — confira vida útil por classe e a fórmula de depreciação (norma contábil dura). |
| ⭐ [`Obra.rules.md`](../../src/Modules/Patrimonio/rules/Obra.rules.md) | **PNCP art. 94 §3** (prazos de execução por porte); Lei 14.133/2021 | **Sim** — confira os prazos de obra e a remessa SICOE ao TCE-RS (`TODO(M10)`). |
| [`Inventario.rules.md`](../../src/Modules/Patrimonio/rules/Inventario.rules.md) | Levantamento/reconciliação de bens | Operacional. |
| [`ItemEstoque.rules.md`](../../src/Modules/Patrimonio/rules/ItemEstoque.rules.md) | Almoxarifado FEFO; baixa por requisição | Operacional. |
| [`PedidoRequisicao.rules.md`](../../src/Modules/Patrimonio/rules/PedidoRequisicao.rules.md) | Requisição de material | Operacional. |
| [`Veiculo.rules.md`](../../src/Modules/Patrimonio/rules/Veiculo.rules.md) / [`FrotaPneusApolices.rules.md`](../../src/Modules/Patrimonio/rules/FrotaPneusApolices.rules.md) | Frota: veículos, pneus, apólices | Operacional. |

> A depreciação (BemPatrimonial) alimenta a contabilidade — interessa também ao **contador**.

---

## Legislativo (10 regras)

| Regra | Norma principal | Conferir? |
|---|---|---|
| ⭐ [`LimiteCamaraArt29A.rules.md`](../../src/Modules/Legislativo/rules/LimiteCamaraArt29A.rules.md) | **CF art. 29-A (EC 25/2000, EC 58/2009, EC 109/2021)** — faixas populacionais × % de repasse; subteto da folha 70%; inativos no teto a partir de 2025 | **Sim** — norma fiscal dura. Parâmetros em [`ParametrosArt29A.cs`](../../src/Modules/Legislativo/Tensorroot.Gov.Modules.Legislativo.Domain/LimiteCamara/ParametrosArt29A.cs) 🟢 (faixas, subteto 70%, limiar do semáforo, exercício-corte 2025). |
| [`Sessao.rules.md`](../../src/Modules/Legislativo/rules/Sessao.rules.md) / [`Votacao.rules.md`](../../src/Modules/Legislativo/rules/Votacao.rules.md) | Plenária, quóruns, painel eletrônico | Operacional (quórum tem regra jurídica leve). |
| [`Proposicao.rules.md`](../../src/Modules/Legislativo/rules/Proposicao.rules.md) / [`Norma.rules.md`](../../src/Modules/Legislativo/rules/Norma.rules.md) | Projetos/normas municipais; ciclo de sanção | Operacional. |
| [`Comissao.rules.md`](../../src/Modules/Legislativo/rules/Comissao.rules.md) · [`Vereador.rules.md`](../../src/Modules/Legislativo/rules/Vereador.rules.md) · [`Tribuna.rules.md`](../../src/Modules/Legislativo/rules/Tribuna.rules.md) · [`DiarioOficial.rules.md`](../../src/Modules/Legislativo/rules/DiarioOficial.rules.md) · [`LexmlExport.rules.md`](../../src/Modules/Legislativo/rules/LexmlExport.rules.md) | Colegiados, mandato, tribuna, diário, exportação LexML | Operacional (LexML tem `TODO(M10)` de transmissão). |

> O limite da Câmara (Art. 29-A) interessa também ao **contador** e ao **painel do gestor**.

---

## Protocolo (5 regras)

| Regra | Norma principal | Conferir? |
|---|---|---|
| ⭐ [`Temporalidade.rules.md`](../../src/Modules/Protocolo/rules/Temporalidade.rules.md) | **CONARQ** (tabela de retenção/eliminação documental) | **Sim** — confira os prazos de guarda/eliminação contra a tabela de temporalidade do município. |
| ⭐ [`CarimboRfc3161.rules.md`](../../src/Modules/Protocolo/rules/CarimboRfc3161.rules.md) | **RFC 3161** (carimbo de tempo) + ICP-Brasil A1/A3 | **Sim** — padrão técnico de carimbo de tempo; integração com ACT é `TODO(M10)`. |
| [`SequenciaNup.rules.md`](../../src/Modules/Protocolo/rules/SequenciaNup.rules.md) | CONARQ (NUP + dígito verificador); **Lei 14.063/2020** (assinatura) | Confira a numeração NUP. |
| [`Processo.rules.md`](../../src/Modules/Protocolo/rules/Processo.rules.md) / [`Documento.rules.md`](../../src/Modules/Protocolo/rules/Documento.rules.md) | Processo administrativo eletrônico; GED; assinatura (Lei 14.063/2020) | Operacional. |

---

## Saúde (12 regras)

| Regra | Norma principal | Conferir? |
|---|---|---|
| ⭐ [`FiscalSaude.rules.md`](../../src/Modules/Saude/rules/FiscalSaude.rules.md) | **LC 141/2012** (mínimo 15% ASPS); Portaria 3.992/2017 (blocos FMS); SIOPS | **Sim** — interessa ao **contador** (mínimo da saúde). |
| ⭐ [`Atendimento.rules.md`](../../src/Modules/Saude/rules/Atendimento.rules.md) | **Lei 13.787/2018** (guarda 20 anos do prontuário); NGS2 ICP-Brasil; RNDS FHIR R4 | **Sim** — guarda legal do prontuário + dado sensível LGPD. |
| ⭐ [`Vigilancia.rules.md`](../../src/Modules/Saude/rules/Vigilancia.rules.md) / [`FiscalSaude`] | **Lei 6.437/1977**; RDC Anvisa 153/2017; Lei 13.874/2019 (dispensa baixo risco); SINAVISA (`TODO(M10)`) | **Sim** — auto de infração sanitária; AutoVisa pode gerar dívida ativa. |
| [`Imunizacao.rules.md`](../../src/Modules/Saude/rules/Imunizacao.rules.md) | **SI-PNI**; calendário; FEFO (`TODO(M10)` transmissão) | Confira o calendário vacinal. |
| [`Farmacia.rules.md`](../../src/Modules/Saude/rules/Farmacia.rules.md) | REMUME; estoque/lote/FEFO; **SNGPC/HÓRUS** (`TODO(M10)`) | Confira REMUME/escrituração. |
| [`Paciente.rules.md`](../../src/Modules/Saude/rules/Paciente.rules.md) / [`Profissional.rules.md`](../../src/Modules/Saude/rules/Profissional.rules.md) | CNS/CADSUS; CNES/CBO; CRM ativo p/ teleconsulta (Lei 14.510/2022); LGPD | Dado sensível — confira trilha de acesso. |
| [`SolicitacaoRegulacao.rules.md`](../../src/Modules/Saude/rules/SolicitacaoRegulacao.rules.md) | SISREG; cota + prioridade | Operacional. |
| [`Agendamento`/`AgendaProfissional`/`Estabelecimento`/`FilaEspera`](../../src/Modules/Saude/rules/) | Agenda, CNES, fila FIFO | Operacional. |

---

## Educação (8 regras)

| Regra | Norma principal | Conferir? |
|---|---|---|
| ⭐ [`Fiscal.rules.md`](../../src/Modules/Educacao/rules/Fiscal.rules.md) | **CF art. 212** (mínimo 25% MDE); **EC 108/2020** (piso 70% magistério FUNDEB) | **Sim** — interessa ao **contador** (mínimo da educação/FUNDEB). |
| ⭐ [`DiarioClasse.rules.md`](../../src/Modules/Educacao/rules/DiarioClasse.rules.md) | **LDB Lei 9.394/1996** (atual. Lei 14.945/2024): frequência ≥75%, 200 dias, 800h/1.000h | **Sim** — regras de aprovação/reprovação por frequência. |
| ⭐ [`Escola.rules.md`](../../src/Modules/Educacao/rules/Escola.rules.md) | Censo/EducaCenso (INEP); **FUNDEB (EC 108/2020 + Lei 14.113/2020)** | Confira código INEP e base FUNDEB. |
| [`Merenda.rules.md`](../../src/Modules/Educacao/rules/Merenda.rules.md) | **PNAE — Lei 11.947/2009** | Confira cardápio/distribuição. |
| [`Transporte.rules.md`](../../src/Modules/Educacao/rules/Transporte.rules.md) | **PNATE — Lei 10.880/2004** | Confira rotas/veículos. |
| [`Aluno`/`Matricula`/`Turma`](../../src/Modules/Educacao/rules/) | EducaCenso; matrícula; enturmação | Operacional (Matrícula alimenta FUNDEB ponderado). |

---

## Assistência Social (5 regras)

| Regra | Norma principal | Conferir? |
|---|---|---|
| ⭐ [`Beneficio.rules.md`](../../src/Modules/AssistenciaSocial/rules/Beneficio.rules.md) | **CF 203-204; Lei 8.742/1993 (LOAS); Lei 14.601/2023** — BPC (renda < ¼ SM), PBF, eventuais | **Sim** — critérios de elegibilidade (versionados por vigência). |
| ⭐ [`FiscalAssistencia.rules.md`](../../src/Modules/AssistenciaSocial/rules/FiscalAssistencia.rules.md) | **FMAS** execução por bloco/piso (Portaria MDS 1.043/2024); RMA | **Sim** — interessa ao **contador** (fundo de assistência). |
| ⭐ [`ProntuarioSuas.rules.md`](../../src/Modules/AssistenciaSocial/rules/ProntuarioSuas.rules.md) | **Sigilo PAIF/PAEFI** (dado sensível LGPD art. 11); NOB-SUAS/2012; Tipificação CNAS 109/2009 | **Sim** — trilha de acesso imutável a dado sensível. |
| [`Familia.rules.md`](../../src/Modules/AssistenciaSocial/rules/Familia.rules.md) | **CadÚnico** (read-model federal, nunca sobrescreve); PNAS/2004 | Confira que o CadÚnico não é alterado localmente. |
| [`Onda3dAssistencia.rules.md`](../../src/Modules/AssistenciaSocial/rules/Onda3dAssistencia.rules.md) | **PBF** condicionalidades (advertência→bloqueio→suspensão); Censo SUAS; IGD (`TODO(M10)`) | Confira o efeito gradativo das condicionalidades. |

---

## Identidade (4 regras) e Painel do Gestor (1 regra)

| Regra | Norma principal | Conferir? |
|---|---|---|
| [`Autenticacao.rules.md`](../../src/Modules/Identidade/rules/Autenticacao.rules.md) · [`Usuario`](../../src/Modules/Identidade/rules/Usuario.rules.md) · [`Papel`](../../src/Modules/Identidade/rules/Papel.rules.md) · [`UnidadeOrganizacional`](../../src/Modules/Identidade/rules/UnidadeOrganizacional.rules.md) | JWT; RBAC (Usuário→Departamento→Papel); negar por padrão | Operacional/segurança — confira a segregação de funções. |
| [`PainelGestor.rules.md`](../../src/Modules/PainelGestor/rules/PainelGestor.rules.md) | Dashboard de KPIs; analítica agregada (LAI); LRF (limites de pessoal/dívida) | Consome os limites fiscais (LRF, Art. 29-A) — confira a coerência com Finanças/RH. |

---

## Resumo

- **53 regras de negócio** (`.rules.md`) nestes módulos (Saúde 12, Educação 8, Assistência 5,
  Legislativo 10, Patrimônio 7, Protocolo 5, Identidade 4, Painel 1).
- **Regras com norma dura (⭐)** que valem conferência cuidadosa: Art. 29-A (Câmara), Obra (PNCP),
  BemPatrimonial (depreciação MCASP), Temporalidade + RFC 3161 (CONARQ/carimbo), os **fiscais
  setoriais** (saúde 15%, educação 25%, assistência FMAS — que também aparecem em
  [`contabil.md`](contabil.md)), e os prontuários sensíveis (saúde/assistência, LGPD).
- **Parâmetros ajustáveis 🟢 nestes módulos:** `ParametrosArt29A` (faixas/subteto da Câmara) e os
  parâmetros de Obra (prazos PNCP, em `ObraOptions`). O resto é majoritariamente operacional.

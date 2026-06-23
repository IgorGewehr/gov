# ONDA 3 — Largura Operacional Restante (DESIGN implementação-pronto)

> **Autoridade:** `docs/estudo/completude-modulos/PLANO-PROFUNDIDADE.md` (Onda 3, itens 12-16).
> **Tese:** fechar o "impecável em tudo" — a largura operacional do dia a dia que ainda falta nos 5 setores, **reusando agressivamente** as entidades das Ondas 0-2.
> **Regra de ouro do design:** operação local sem nenhuma credencial oficial (CLAUDE.md §8). Tudo que depende de RNDS/SI-PNI/HÓRUS/FNDE/INEP/eSocial/MDS/TCE fica **marcado e deferido para M10**.
> **Restrições de arquitetura (CLAUDE.md):** domínio rico, multi-tenant (`IMustHaveTenant` + Global Query Filter), auditoria imutável, LGPD (dado sensível em Saúde/Assistência/menor), cross-module **só via `*.Contracts`** (Integration Events), 1 DbContext/schema por módulo, CQRS via MediatR, identificadores sem acento.

---

## 0. Estado atual verificado (o que JÁ existe — não reconstruir)

Leitura do código antes de desenhar. Muita coisa de "Onda 3" no plano original **já foi entregue** nas ondas anteriores ou nasceu rica desde a Fase 3/4. O design abaixo só projeta o **delta real**.

| Já existe (reuso direto) | Onde |
|---|---|
| `Turma` (vagas, situação, contador matriculados), `Matricula`, `Aluno`, `Escola` | `Educacao.Domain/{Turmas,Matriculas,Alunos,Escolas}` |
| `DiarioClasse` **1-1 por matrícula** (frequência %, notas por componente, aulas/dias letivos, `ApurarResultado`) + endpoints `/diarios/*` | `Educacao.Domain/DiarioClasse` |
| `Paciente` (CNS, condições, alergias, LGPD), `Estabelecimento` (CNES), `Profissional` (CBO, vínculos), `Agendamento` | `Saude.Domain/{Pacientes,Estabelecimentos,Profissionais,Agendamento}` |
| `ItemEstoque` (saldo, lotes PEPS/médio, `RegistrarEntrada`, `AtenderRequisicao`, ponto de pedido) + `Requisicao` (filha, Abrir→Atender) | `Patrimonio.Domain/Estoque` |
| `Veiculo` **já rico**: abastecimento (com cota), `ManutencaoOS`, `Multa`, `Licenciamento`, `Motorista` (CNH), depreciação + endpoints `/veiculos/*` | `Patrimonio.Domain/Frota` |
| `Servidor`, `Cargo`, `PlanoDeCargos`, `Afastamento` (tipado, efeito folha), `FolhaDePagamento`, `EventoFolha`, `Rubrica`, `Consignacao` | `RecursosHumanos.Domain/*` |
| `Familia` (NIS, CadÚnico, renda per capita, território/CRAS), `Beneficio`, `FMAS`, `RMA` (RegistroMensalAtendimento) | `AssistenciaSocial.Domain/*` |

> **Consequência:** Onda 3 NÃO cria Turma/Aluno/Paciente/UBS/ItemEstoque/Veiculo/Servidor/Familia. Ela adiciona **read models de turma/boletim, novos agregados de operação (Farmácia, Imunização, Vigilância, Merenda, Transporte, Requisição self-service, Progressão), painéis agregados (Frota, RH) e o ciclo PBF/Censo/IGD** — todos pendurados nas entidades acima por **Id (FK lógica)**.

---

## 1. EDUCAÇÃO

### 1.1 Diário por Turma + Boletim/Histórico (M)
- **Resumo (2 linhas):** hoje o diário é 1-1 por matrícula (lançar é micro). Falta a **visão de turma** (diário coletivo: chamada e notas da turma inteira em uma tela) e o **Boletim/Histórico** (read model por aluno agregando os diários). Nenhuma entidade nova de domínio — é orquestração + projeção.
- **Reusa:** `Turma`, `Matricula` (ativas da turma), `DiarioClasse` (1-1), `Aluno`.
- **Entidades novas:** nenhuma raiz nova. Opcional `ComponenteCurricular` como catálogo (hoje é só `ComponenteCurricularId`); fica P3/M10. Read models: `DiarioTurmaView`, `BoletimAlunoView`, `HistoricoEscolarView`.
- **Backend novo:** `RegistrarFrequenciaTurmaCommand` (chamada em lote — itera matrículas ativas → `DiarioClasse.RegistrarFrequencia`), `LancarNotasTurmaCommand` (grade nota×aluno×componente×período). Queries: `ObterDiarioDaTurma(turmaId, data)`, `ObterBoletim(matriculaId|alunoId, anoLetivo)`, `ObterHistoricoEscolar(alunoId)`.
- **Endpoints (em `/api/educacao`):**
  - `GET /turmas/{turmaId}/diario?data=` — grade de chamada (alunos ativos × presença)
  - `POST /turmas/{turmaId}/diario/frequencias` — lançamento de frequência em lote
  - `POST /turmas/{turmaId}/diario/notas` — lançamento de notas em lote
  - `GET /matriculas/{matriculaId}/boletim?anoLetivo=` — boletim (notas/médias/faltas/% freq./resultado)
  - `GET /alunos/{alunoId}/historico-escolar` — histórico longitudinal
- **Esforço:** **M** (orquestração de lote + 3 read models; domínio reusado).
- **Cred oficial:** não. EducaCenso/INEP = M10.

### 1.2 Merenda (PNAE — cardápio/estoque) (M)
- **Resumo (2 linhas):** novo agregado de planejamento nutricional (cardápio semanal por faixa etária/escola) + consumo do estoque de gêneros. **Reusa o motor de estoque existente** (PEPS/lote/validade) para os gêneros alimentícios, sem reimplementar baixa.
- **Reusa:** `ItemEstoque` (gêneros = itens de almoxarifado com unidade kg/L/un; entrada/saída e validade já prontos), `Escola`, `Turma` (nº de comensais = matriculados).
- **Entidades novas:** `Cardapio` (raiz: escola, semana, refeições) → `ItemCardapio` (gênero `ItemEstoqueId`, quantidade per capita), `DistribuicaoMerenda` (raiz: escola, data, nº comensais, baixa efetiva de gêneros). Catálogos: `FaixaEtariaPnae`, `TipoRefeicao` (enums).
- **Backend novo:** `Cargo`-style aggregates novos; `RegistrarDistribuicaoMerendaCommand` → calcula consumo (per capita × comensais) e chama `ItemEstoque.AtenderRequisicao` por gênero (despesa reconhecida no consumo, já implementado). Eventos: `MerendaDistribuida`.
- **Endpoints (`/api/educacao/merenda`):** `POST /cardapios`, `GET /cardapios?escolaId=&semana=`, `POST /distribuicoes`, `GET /distribuicoes?escolaId=&periodo=`, `GET /consumo?escolaId=&periodo=` (relatório).
- **Esforço:** **M** (2 agregados novos finos + reuso total do estoque).
- **Cred oficial:** operação não. Prestação de contas **PNAE/FNDE = M10**.

### 1.3 Transporte (PNATE — rotas/alunos) (M)
- **Resumo (2 linhas):** novo agregado `RotaTransporte` (itinerário, turno, veículo) com alunos vinculados; **reusa `Veiculo`** da Frota como veículo da rota e `Aluno`/`Matricula` para a lista de transportados. Frequência de transporte opcional.
- **Reusa:** `Veiculo` (`VeiculoId` — frota), `Aluno`/`Matricula`, `Escola`.
- **Entidades novas:** `RotaTransporte` (raiz: nome, turno, `VeiculoId`, km, pontos) → `AlunoTransportado` (filha: `AlunoId`, ponto de embarque). Enum `ModalidadeTransporte` (próprio/terceirizado).
- **Backend novo:** `CriarRotaCommand`, `VincularAlunoRotaCommand`, `RegistrarFrequenciaTransporteCommand` (opcional). Query: `ObterRotasPorEscola`, `ListarAlunosDaRota`.
- **Endpoints (`/api/educacao/transporte`):** `POST /rotas`, `GET /rotas`, `POST /rotas/{rotaId}/alunos`, `GET /rotas/{rotaId}/alunos`.
- **Esforço:** **M**.
- **Cred oficial:** não. Prestação **PNATE/FNDE = M10**.

---

## 2. SAÚDE

### 2.1 Farmácia / Dispensação (G)
- **Resumo (2 linhas):** novo submódulo: estoque de medicamentos por lote/validade **+ dispensação ao paciente** (vincula `Paciente`, baixa o lote, registra quem retirou). O motor de saldo/lote/PEPS **existe em Patrimônio** mas é de outro módulo — Saúde precisa de seu **próprio** `EstoqueMedicamento` (isolamento de BC; medicamento tem regras próprias: controlado/Portaria 344, dose, prescrição).
- **Reusa:** `Paciente` (`PacienteId`, LGPD sensível), `Estabelecimento` (UBS/farmácia = `EstabelecimentoId`), `Profissional` (prescritor), `Prescricao`/`Atendimento` (vínculo opcional à receita já registrada no PEP). **Padrão de saldo/lote** copiado de `ItemEstoque` (não referenciado — replicado no BC Saúde).
- **Entidades novas:** `Medicamento` (catálogo: princípio ativo, apresentação, controlado?), `EstoqueMedicamento` (raiz por estabelecimento+medicamento: saldo, lotes c/ validade, entrada/saída), `LoteMedicamento` (filha), `Dispensacao` (raiz: `PacienteId`, `EstabelecimentoId`, itens dispensados, `PrescricaoId?`, profissional). Enums `TipoControleSngpc`.
- **Backend novo:** `CadastrarMedicamentoCommand`, `RegistrarEntradaMedicamentoCommand` (lote+validade), `DispensarMedicamentoCommand` (valida saldo/validade/controlado → baixa FEFO first-expired-first-out → `MedicamentoDispensado`), alerta de validade/ruptura. Queries: lista de medicamentos, posição de estoque, histórico de dispensação por paciente (trilha LGPD).
- **Endpoints (`/api/saude/farmacia`):** `POST /medicamentos`, `GET /medicamentos`, `POST /estoque/{estabId}/{medId}/entradas`, `GET /estoque?estabId=`, `POST /dispensacoes`, `GET /pacientes/{pacienteId}/dispensacoes`, `GET /alertas/validade`.
- **Esforço:** **G** (novo motor de estoque + dispensação + LGPD na trilha).
- **Cred oficial:** operação local **não**. Integração **HÓRUS / SNGPC = M10**.

### 2.2 Imunização (carteira de vacina / aprazamento) (M/G)
- **Resumo (2 linhas):** carteira de vacinação do `Paciente` com calendário PNI (aprazamento da próxima dose). Reusa Paciente; aplicação opcionalmente baixa do estoque de imunobiológicos (reusa o `EstoqueMedicamento` de 2.1 se modelado genérico).
- **Reusa:** `Paciente` (`PacienteId`), `Estabelecimento`, `Profissional` (aplicador), e o estoque de 2.1 para o imunobiológico (baixa de lote).
- **Entidades novas:** `Imunobiologico` (catálogo: vacina, doses do esquema), `CarteiraVacinacao` (raiz por `PacienteId`) → `DoseAplicada` (filha: imunobiológico, dose nº, lote, data, próxima dose aprazada). `CalendarioVacinal` (seed PNI por faixa etária — parametrizável).
- **Backend novo:** `RegistrarDoseCommand` (aplica → calcula aprazamento da próxima → `DoseAplicada`; opcional baixa de estoque), `ObterCarteira(pacienteId)`, `ListarAprazamentosVencidos` (busca ativa). Evento `DoseAplicadaRegistrada`.
- **Endpoints (`/api/saude/imunizacao`):** `POST /imunobiologicos`, `GET /pacientes/{pacienteId}/carteira`, `POST /pacientes/{pacienteId}/doses`, `GET /aprazamentos/vencidos?estabId=`.
- **Esforço:** **M/G** (carteira + motor de aprazamento; G se acoplar baixa de estoque).
- **Cred oficial:** registro local **não**. **SI-PNI / RNDS = M10**.

### 2.3 Vigilância Sanitária (inspeções/autos) (M)
- **Resumo (2 linhas):** novo agregado de fiscalização sanitária: estabelecimentos fiscalizáveis, inspeções (roteiro/checklist), e autos de infração/intimação. Independente do PEP.
- **Reusa:** `Profissional` (fiscal/responsável técnico, opcional), padrão de processo administrativo do módulo Protocolo via Contracts (opcional, para o auto virar processo).
- **Entidades novas:** `EstabelecimentoFiscalizavel` (raiz: CNPJ/CPF, ramo, alvará sanitário), `Inspecao` (raiz: estabelecimento, roteiro, itens conformes/não-conformes, resultado), `AutoInfracao` (raiz: inspeção, tipo, prazo defesa, valor). Enums `TipoAuto`, `ResultadoInspecao`.
- **Backend novo:** `CadastrarEstabelecimentoFiscalizavelCommand`, `AbrirInspecaoCommand`, `RegistrarItemInspecaoCommand`, `LavrarAutoCommand`. Queries: agenda de inspeções, autos por prazo.
- **Endpoints (`/api/saude/vigilancia`):** `POST /estabelecimentos`, `GET /estabelecimentos`, `POST /inspecoes`, `POST /inspecoes/{id}/autos`, `GET /autos?status=`.
- **Esforço:** **M**.
- **Cred oficial:** não (operação local; SINAVISA/e-SUS VS = M10).

---

## 3. PATRIMÔNIO

### 3.1 Requisição de Almoxarifado self-service (M)
- **Resumo (2 linhas):** hoje `Requisicao` é uma filha do `ItemEstoque` aberta e atendida na mesma chamada (`AtenderRequisicao`) — não há **fluxo self-service** (solicitar → aprovar → atender, multi-item, por setor). Onda 3 adiciona o agregado de **pedido** que orquestra a baixa nos itens já existentes.
- **Reusa:** `ItemEstoque.AtenderRequisicao` (baixa de saldo + valoração + despesa já implementada), VOs de saldo.
- **Entidades novas:** `PedidoRequisicao` (raiz: setor solicitante, situação Solicitado→Aprovado→Atendido→Cancelado) → `ItemPedido` (filha: `ItemEstoqueId`, qtd solicitada, qtd atendida). Enum `SituacaoPedido`.
- **Backend novo:** `AbrirPedidoCommand`, `AprovarPedidoCommand` (autorização — RBAC do setor), `AtenderPedidoCommand` (itera itens → `ItemEstoque.AtenderRequisicao` por item, na mesma transação; baixa parcial permitida). Eventos: `PedidoAprovado`, `PedidoAtendido`. Queries: fila de pedidos pendentes, pedidos por setor.
- **Endpoints (`/api/patrimonio/requisicoes`):** `POST /` (abrir), `GET /?status=&setor=`, `POST /{pedidoId}/aprovacao`, `POST /{pedidoId}/atendimento`, `POST /{pedidoId}/cancelamento`.
- **Esforço:** **M** (orquestra entidade de estoque pronta).
- **Cred oficial:** não.

### 3.2 Painel de Frota (abastecimento/manutenção/multas) (M)
- **Resumo (2 linhas):** `Veiculo` **já registra** abastecimento, manutenção, multas e licenciamento. Falta o **painel agregado de gestão** — read models de custo/consumo por veículo, km/L, multas e CNH/licenciamento a vencer. É projeção + UI, **zero entidade nova de domínio**.
- **Reusa:** `Veiculo` (todas as coleções já existentes: `Abastecimentos`, `OrdensServico`, `Multas`, `Licenciamentos`, `Motoristas`) + endpoints já mapeados (`/veiculos/multas-pendentes`, `/veiculos/licenciamentos-pendentes`).
- **Entidades novas:** nenhuma. Read models: `PainelFrotaView` (KPIs da frota), `CustoVeiculoView` (custo/consumo por veículo/período), `CnhVencendoView`.
- **Backend novo:** Queries: `ObterPainelFrota(periodo)` (gasto total combustível+manutenção+multas, consumo médio km/L), `ObterCustoPorVeiculo(veiculoId, periodo)`, `ListarCnhVencendo(dias)`, `ListarManutencoesAbertas`. Sem novos commands.
- **Endpoints (`/api/patrimonio/frota`):** `GET /painel?periodo=`, `GET /veiculos/{veiculoId}/custos?periodo=`, `GET /cnh-vencendo?dias=`, `GET /manutencoes/abertas`.
- **Esforço:** **M** (puramente read models + dashboard).
- **Cred oficial:** não. **Multas RENAINF/DETRAN-RS, IPVA/licenciamento online = M10** (depende de convênio).

---

## 4. RECURSOS HUMANOS

### 4.1 Relatórios gerenciais (M)
- **Resumo (2 linhas):** suite de read models gerenciais sobre `FolhaDePagamento`/`EventoFolha`/`Servidor`/`Cargo` já existentes: folha por secretaria/fonte, evolução mensal da despesa de pessoal, mapa de cargos (vagas × ocupação) e demonstrativo para TCE. Projeção + UI, sem domínio novo.
- **Reusa:** `FolhaDePagamento`, `EventoFolha`, `Rubrica`, `Servidor` (lotação), `Cargo` (vagas/ocupadas), `Lotacao`/`Vencimento`.
- **Entidades novas:** nenhuma. Read models: `FolhaPorSecretariaView`, `EvolucaoDespesaPessoalView`, `MapaCargosView`, `DemonstrativoTceView`.
- **Backend novo:** Queries: `ObterFolhaPorSecretaria(competencia)`, `ObterEvolucaoDespesa(de, ate)` (série mensal — base p/ limite LRF 54%/RCL), `ObterMapaCargos()`, `ObterDemonstrativoTce(competencia)`. Sem commands.
- **Endpoints (`/api/recursoshumanos/relatorios`):** `GET /folha-por-secretaria?competencia=`, `GET /evolucao-despesa?de=&ate=`, `GET /mapa-cargos`, `GET /demonstrativo-tce?competencia=`.
- **Esforço:** **M** (4 read models sobre dados existentes).
- **Cred oficial:** não. Remessa formatada **TCE-RS** já é do módulo Transparência/M4.

### 4.2 Progressão / Promoção funcional (G)
- **Resumo (2 linhas):** novo agregado de carreira: progressão horizontal (referência/nível por triênio/avaliação) e promoção vertical (classe) com **efeito no vencimento**. `Cargo` hoje tem só `Vencimento` único — sem matriz nível×classe; precisa de estrutura de carreira nova + histórico por servidor.
- **Reusa:** `Servidor` (tempo de efetivo exercício já modelado para estabilidade — CF art. 41), `Cargo`/`PlanoDeCargos`, `Vencimento`. Habilita **eSocial S-2206** (alteração contratual) via Outbox — deferido para quando A1 estiver no Key Vault.
- **Entidades novas:** `TabelaCarreira` (raiz no `PlanoDeCargos`: matriz nível×classe → percentual/valor), `EnquadramentoServidor` (raiz: `ServidorId`, nível/classe atual, data) → `EventoProgressao` (filha: tipo horizontal/vertical, motivo triênio/mérito, vencimento anterior/novo). Enum `TipoProgressao`.
- **Backend novo:** `DefinirTabelaCarreiraCommand`, `EnquadrarServidorCommand` (enquadramento inicial), `ProgredirServidorCommand` (valida interstício/triênio → recalcula vencimento → `ServidorProgredido`; publica intenção S-2206 via Outbox). Queries: elegíveis a progressão (interstício cumprido), histórico de carreira do servidor.
- **Endpoints (`/api/recursoshumanos/carreira`):** `POST /tabelas`, `POST /servidores/{servidorId}/enquadramento`, `POST /servidores/{servidorId}/progressao`, `GET /elegiveis?data=`, `GET /servidores/{servidorId}/historico-carreira`.
- **Esforço:** **G** (matriz de carreira + máquina de progressão + efeito na folha).
- **Cred oficial:** operação **não**. Transmissão **eSocial S-2206 = M10** (certificado A1).

---

## 5. ASSISTÊNCIA SOCIAL

> Base já forte: `Familia` (NIS, CadÚnico, renda per capita, território/CRAS), `Beneficio`, `FMAS`, `RMA`. Onda 3 adiciona o **acompanhamento PBF e os consolidados de gestão (Censo SUAS / IGD)** — construindo o que é viável sem integração oficial e marcando o que depende de creds.

### 5.1 PBF — Acompanhamento de condicionalidades (M)
- **Resumo (2 linhas):** acompanhamento **local** das condicionalidades do Programa Bolsa Família (frequência escolar, agenda de saúde/vacinação, pré-natal) por família, com registro de cumprimento/descumprimento e efeito gerencial (não altera benefício federal — a base MDS é autoritativa, I-9 já respeitado em `Familia`).
- **Reusa:** `Familia` (`FamiliaId`, NIS, membros). Cruzamento opcional **via Contracts** com Educação (frequência ≥ 85%) e Saúde (carteira de vacina/2.2) — leitura por Integration Events, nunca chamada direta.
- **Entidades novas:** `AcompanhamentoCondicionalidade` (raiz: `FamiliaId`, período/vigência) → `RegistroCondicionalidade` (filha: tipo Educação/Saúde, membro, status cumprida/descumprida/justificada, motivo). Enum `TipoCondicionalidade`, `StatusCondicionalidade`.
- **Backend novo:** `AbrirAcompanhamentoCommand`, `RegistrarCondicionalidadeCommand` (manual e/ou alimentado por evento Educação/Saúde), `JustificarDescumprimentoCommand`. Queries: famílias em descumprimento, agenda de acompanhamento.
- **Endpoints (`/api/assistenciasocial/pbf`):** `POST /acompanhamentos`, `GET /acompanhamentos?status=`, `POST /acompanhamentos/{id}/condicionalidades`, `POST /condicionalidades/{id}/justificativa`, `GET /descumprimentos?periodo=`.
- **Esforço:** **M**.
- **Cred oficial:** acompanhamento local **não**. Sincronização oficial **CECAD/SICON/Sistema Condicionalidades (MDS) = M10**.

### 5.2 Censo SUAS (consolidado de unidades) (M)
- **Resumo (2 linhas):** questionário/consolidado anual das unidades socioassistenciais (CRAS/CREAS): equipe, serviços ofertados, capacidade. Read model consolidando o que já existe (unidades, RMA) + dados próprios da unidade.
- **Reusa:** `RMA` (volume de atendimentos por serviço PAIF/PAEFI/SCFV — já consolida do Prontuário), `Familia` (famílias referenciadas por unidade), unidade de atendimento (`UnidadeAtendimentoId` já em `Familia`).
- **Entidades novas:** `UnidadeSocioassistencial` (raiz: tipo CRAS/CREAS/Centro POP, equipe, serviços, capacidade) — hoje a unidade é só um GUID em `Familia`; promover a cadastro. `FormularioCensoSuas` (raiz anual por unidade, agregando indicadores). 
- **Backend novo:** `CadastrarUnidadeCommand`, `ConsolidarCensoCommand` (agrega RMA + famílias referenciadas + dados da unidade no exercício). Queries: cadastro de unidades, formulário consolidado.
- **Endpoints (`/api/assistenciasocial/censo`):** `POST /unidades`, `GET /unidades`, `POST /censo/consolidar?exercicio=`, `GET /censo?exercicio=`.
- **Esforço:** **M**.
- **Cred oficial:** consolidado local **não**. Envio **Censo SUAS / RMA SAGI (MDS) = M10**.

### 5.3 EstimativaIgd (M — local) e AgilizaSUAS (DEFERIR)
- **Resumo (2 linhas):** `EstimativaIgd` = cálculo **estimado** do IGD-PBF/IGD-SUAS a partir de indicadores locais (taxa de atualização cadastral, cumprimento de condicionalidades — 5.1, gestão de benefícios). É construível **localmente como estimativa gerencial**. `AgilizaSUAS` é portal/integração do MDS — **sem API pública estável documentada**, depende de credenciais/convênio → **defere para M10**.
- **Reusa:** `Familia` (vigência cadastral — `AtualizacaoVencida`/`Regularizada` já modela a taxa), `AcompanhamentoCondicionalidade` (5.1), `FMAS`/`RMA`.
- **Entidades novas:** nenhuma raiz nova. Read model/serviço de domínio `CalculadoraIgd` (fator atualização cadastral × fator condicionalidades × fator gestão → índice 0-1) — **rotulado "estimativa local, não oficial"**.
- **Backend novo:** Query `EstimarIgd(exercicio)` (combina os fatores). Sem commands.
- **Endpoints (`/api/assistenciasocial/igd`):** `GET /estimativa?exercicio=`.
- **Esforço:** **M** (IGD estimado); **AgilizaSUAS = M10** (cred MDS).
- **Cred oficial:** estimativa **não**. **IGD oficial + AgilizaSUAS (MDS) = M10.**

---

## 6. Sequência recomendada de SUB-ONDAS

Critério: **valor PoC × esforço × independência de creds**, balanceando **backend serial** (domínio rico, 1 dev sênior na trilha) e **frontend paralelo** (read models e telas que partem de dados/contratos já estáveis). Itens "puro read model / painel" são os de **maior ROI por menor esforço** e abrem trabalho de FE imediato.

### Sub-onda 3a — "Painéis e read models" (puro valor, esforço M, zero domínio novo) — *fazer primeiro*
O maior salto de percepção pelo menor custo: tudo reusa entidades prontas; FE arranca em paralelo no dia 1.
1. **Patrimônio — Painel de Frota** (3.2) — só projeção sobre `Veiculo` já rico.
2. **RH — Relatórios gerenciais** (4.1) — 4 read models sobre folha/cargo existentes.
3. **Educação — Diário por turma + Boletim/Histórico** (1.1) — orquestração de lote + projeção sobre `DiarioClasse`.

> Balanceamento: BE entrega as queries em série; FE constrói os 3 dashboards/telas em paralelo (DataTable + cards já padronizados).

### Sub-onda 3b — "Operação de estoque/consumo" (esforço M, reusa motor de estoque)
Agregados finos que penduram no `ItemEstoque`/estoque já provado.
4. **Patrimônio — Requisição self-service** (3.1) — orquestra `ItemEstoque.AtenderRequisicao`.
5. **Educação — Merenda PNAE** (1.2) — cardápio + distribuição baixando gêneros do estoque.
6. **Educação — Transporte PNATE** (1.3) — rotas reusando `Veiculo` + alunos.

> BE serial (3a→3b na mesma trilha de Educação/Patrimônio); FE de 3a já em produção enquanto BE faz 3b.

### Sub-onda 3c — "Saúde operacional" (esforço G, novos motores) — *trilha mais pesada*
7. **Saúde — Farmácia/Dispensação** (2.1) — novo motor de estoque de medicamentos + dispensação LGPD.
8. **Saúde — Imunização** (2.2) — carteira + aprazamento (acopla estoque de 2.1).
9. **Saúde — Vigilância Sanitária** (2.3) — inspeções/autos (independente, pode ir em paralelo de FE).

> 2.1 é pré-requisito de 2.2 (estoque de imunobiológico) → serial. 2.3 é independente → FE paralelo.

### Sub-onda 3d — "Carreira + Assistência" (esforço M/G)
10. **Assistência — PBF condicionalidades** (5.1) + **Censo SUAS** (5.2) + **EstimativaIgd local** (5.3) — reusam `Familia`/`RMA`; M cada.
11. **RH — Progressão/promoção funcional** (4.2) — G, matriz de carreira + efeito na folha; fecha a trilha RH.

> Assistência (M, reuso alto) primeiro libera FE; RH Progressão (G) é o item mais pesado de domínio — fecha a onda.

**Ordem-resumo:** `3a (frota+relatórios RH+diário/boletim) → 3b (requisição+merenda+transporte) → 3c (farmácia→imunização, vigilância em paralelo) → 3d (PBF+censo+IGD, depois progressão)`.

---

## 7. O que fica para M10 (depende de credenciais/convênio oficial)

| Item | Depende de |
|---|---|
| Educação — exportação EducaCenso/INEP | INEP (cred) |
| Educação — prestação de contas PNAE e PNATE | FNDE/SIGPC (cred + certificado) |
| Saúde — Farmácia: integração HÓRUS / SNGPC | DATASUS/ANVISA (cred) |
| Saúde — Imunização: SI-PNI / RNDS | DATASUS (cred + certificado ICP) |
| Saúde — Vigilância: e-SUS VS / SINAVISA | DATASUS/estadual (cred) |
| Patrimônio — Frota: multas RENAINF/DETRAN-RS, IPVA/licenciamento online | convênio DETRAN-RS |
| RH — Progressão: transmissão eSocial **S-2206** | certificado A1 no Key Vault |
| Assistência — PBF: Sistema de Condicionalidades / CECAD / SICON | MDS (cred) |
| Assistência — Censo SUAS / RMA: envio SAGI | MDS (cred) |
| Assistência — **IGD oficial** e **AgilizaSUAS** | MDS (cred/convênio; AgilizaSUAS sem API pública estável) |

> Toda a **operação local** dos itens acima é construível **agora** (Ondas 3a-3d). O M10 troca os gateways simulados por reais — sem reescrever o domínio, apenas a camada de integração atrás da ACL (CLAUDE.md §8).

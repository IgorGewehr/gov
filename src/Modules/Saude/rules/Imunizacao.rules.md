# Imunização — Rules-as-Code (Saúde)

Carteira de vacinação do `Paciente` com calendário PNI: registro de aplicação de dose + **aprazamento
da próxima dose** (data prevista calculada pelo intervalo do esquema do imunobiológico). Reusa
`Paciente`, `Estabelecimento`, `Profissional` (aplicador) por Id; a aplicação pode **baixar o estoque**
do imunobiológico reusando o `EstoqueMedicamento` da Farmácia. A carteira é **dado de saúde sensível**
(LGPD): leitura por verbo fino + trilha de acesso. Integração **SI-PNI/RNDS = M10** (atrás da ACL).

## Agregados
- `Imunobiologico` — catálogo (vacina, nº de doses do esquema, intervalos de aprazamento).
- `CarteiraVacinacao` — raiz por `PacienteId`, agrega as doses aplicadas do paciente.
- `DoseAplicada` (filha) — imunobiológico, nº da dose, lote, data de aplicação, **próxima dose aprazada**.

## Invariantes
- Cada aplicação registra a dose na carteira do paciente e, quando o esquema prevê dose seguinte,
  calcula o **aprazamento** (data prevista) que passa a constar na carteira.
- Não registra dose além do nº de doses do esquema do imunobiológico.
- O esquema progride **em ordem**: a dose N só é aceita se a dose N-1 do mesmo imunobiológico já existe (recusa dose fora de ordem).
- A **data de aplicação não pode ser futura** (maior que a referência de hoje) — registro com data impossível é recusado (fail-closed).
- Aplicação com baixa de estoque respeita as invariantes da Farmácia (saldo válido, FEFO).
- Aprazamento vencido (data prevista < hoje) entra na busca ativa por estabelecimento.

## Endpoints
- `POST /api/saude/imunizacao/imunobiologicos` — CadastrarImunobiologicoCommand `[saude.imunizacao.gerenciar]`
- `GET  /api/saude/imunizacao/imunobiologicos` — ListarImunobiologicosQuery `[saude.imunizacao.ver]`
- `GET  /api/saude/imunizacao/pacientes/{pacienteId}/carteira` — ObterCarteiraVacinacaoQuery (LGPD, trilha) `[saude.imunizacao.ver]`
- `POST /api/saude/imunizacao/pacientes/{pacienteId}/doses` — RegistrarDoseCommand `[saude.imunizacao.aplicar]`
- `GET  /api/saude/imunizacao/aprazamentos/vencidos` — ListarAprazamentosVencidosQuery (LGPD, trilha) `[saude.imunizacao.ver]`

<!-- manifest
commands: CadastrarImunobiologico, RegistrarDose
queries: ListarImunobiologicos, ObterCarteiraVacinacao, ListarAprazamentosVencidos
domainEvents: DoseAplicadaRegistrada
integrationEventsPublished: 
integrationEventsConsumed: 
-->

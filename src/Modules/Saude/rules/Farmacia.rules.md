# Farmácia / Dispensação — Rules-as-Code (Saúde)

Submódulo de **Assistência Farmacêutica**: catálogo de medicamentos (REMUME), estoque por
**lote/validade** em cada estabelecimento (UBS/farmácia/CAF) e **dispensação ao paciente** com baixa
do estoque. O motor de saldo/lote/FEFO existe em Patrimônio (`ItemEstoque`) mas é de outro Bounded
Context — Saúde tem o **seu próprio** `EstoqueMedicamento` (isolamento de BC, CLAUDE.md §2):
medicamento controlado (Portaria 344/SNGPC) tem regras próprias. Reusa `Paciente`, `Estabelecimento`,
`Profissional` por Id. Histórico de dispensação por paciente é **dado de saúde sensível** (LGPD): a
consulta passa por verbo fino + trilha de acesso. Integração **HÓRUS/SNGPC = M10** (atrás da ACL).

## Agregados
- `Medicamento` — catálogo (princípio ativo, apresentação, controlado?, CATMAT).
- `EstoqueMedicamento` — posição de UM medicamento em UM estabelecimento (saldo + lotes).
- `LoteMedicamento` (filha) — número, validade, saldo.
- `Dispensacao` — entrega ao paciente (itens dispensados, prescrição opcional, profissional).

## Invariantes
- **I-FARM-1:** saldo nunca negativo; baixa exige saldo válido (não vencido) suficiente, senão recusa.
- **I-FARM-2:** não entra lote já vencido no estoque.
- **I-FARM-3:** baixa por **FEFO** (First-Expired, First-Out), ignorando lotes vencidos.
- Catálogo: `(TenantId, PrincipioAtivo + apresentação)` coerente; controlado exige tipo de controle.
- Dispensação: `PacienteId`, `EstabelecimentoId` obrigatórios; cada item baixa o estoque correspondente.

## Endpoints
- `POST /api/saude/farmacia/medicamentos` — CadastrarMedicamentoCommand `[saude.farmacia.gerenciar]`
- `GET  /api/saude/farmacia/medicamentos` — BuscarMedicamentosQuery (paginado) `[saude.farmacia.ver]`
- `POST /api/saude/farmacia/estoque/{estabId}/{medId}/entradas` — RegistrarEntradaMedicamentoCommand `[saude.farmacia.gerenciar]`
- `GET  /api/saude/farmacia/estoque` — ObterPosicaoEstoqueQuery `[saude.farmacia.ver]`
- `GET  /api/saude/farmacia/alertas/validade` — ListarAlertasValidadeQuery `[saude.farmacia.ver]`
- `POST /api/saude/farmacia/dispensacoes` — DispensarMedicamentoCommand `[saude.farmacia.dispensar]`
- `POST /api/saude/farmacia/dispensacoes/{id}/estorno` — EstornarDispensacaoCommand `[saude.farmacia.dispensar]`
- `GET  /api/saude/farmacia/pacientes/{pacienteId}/dispensacoes` — ListarDispensacoesDoPacienteQuery (LGPD, trilha) `[saude.farmacia.ver]`

<!-- manifest
commands: CadastrarMedicamento, RegistrarEntradaMedicamento, DispensarMedicamento, EstornarDispensacao
queries: BuscarMedicamentos, ObterPosicaoEstoque, ListarAlertasValidade, ListarDispensacoesDoPaciente
domainEvents: MedicamentoCadastrado, EntradaMedicamentoRegistrada, MedicamentoDispensado, DispensacaoEstornada
integrationEventsPublished: 
integrationEventsConsumed: 
-->

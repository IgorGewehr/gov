# Vigilância Sanitária (VISA) — Rules-as-Code (Saúde)

Submódulo de **fiscalização sanitária**, **independente do PEP/CNES**: cadastro de **estabelecimentos
sujeitos à VISA** (restaurante, farmácia, clínica, salão — PJ ou PF), **inspeções/vistorias**
(roteiro/checklist com itens conformes/não conformes e resultado **derivado**), **autos** de
infração/intimação (prazos, defesa, multa) e **licença/alvará sanitário** (emissão, validade,
renovação). Máquinas de estado encadeadas: `inspeção → pendências → auto/licença`. Reusa
`Profissional` (fiscal, opcional) por Id. Operação **100% local** —
**SINAVISA / e-SUS VS = M10** (atrás da ACL, após credencial estadual/DATASUS).

Base normativa: Lei 6.437/1977 (infrações sanitárias e rito do processo administrativo),
RDC Anvisa 153/2017 (classificação de risco), Lei 13.874/2019 (dispensa de licenciamento p/ baixo risco).

## Agregados
- `EstabelecimentoFiscalizavel` — raiz: documento do responsável (CNPJ **ou** CPF), razão social,
  `RamoVisa`, `GrauRiscoSanitario`, endereço; situação (Ativo/Inativo/Interditado).
- `Inspecao` — raiz: estabelecimento, data, fiscal (opcional), roteiro; itens; situação; resultado.
  - `ItemInspecao` (filha) — requisito, `ConformidadeItem`, observação.
- `AutoVisa` — raiz: estabelecimento + inspeção, `TipoAutoVisa`, número, fundamentação, prazo, multa, defesa.
- `LicencaSanitaria` — raiz: estabelecimento, número, emissão, validade, inspeção (opcional); situação.

## Máquinas de estado
- **Inspeção:** `Aberta` → (registra itens) → `Concluída` (resultado derivado) | `Cancelada`. Concluída é **imutável**.
- **Auto (infração/penalidade):** `Lavrado` → `DefesaApresentada` → `Deferido` (cancela) | `Indeferido` (mantém).
  Decurso de prazo sem defesa → `Indeferido` (revelia).
- **Auto (intimação):** `Lavrado` → `Regularizado` (no prazo) | `Indeferido` (decurso).
- **Licença:** `Vigente` → `Vencida` (decurso) | `Cassada` (ato da VISA). Renovação gera **nova** licença vigente.

## Invariantes
- **I-VISA-1:** inspeção **concluída é imutável** (não aceita novos itens nem cancelamento).
- **I-VISA-2:** auto de **intimação não tem multa**; **infração/penalidade exige** valor (≥ 0). Prazo final > lavratura.
- **I-VISA-3:** resultado da inspeção é **DERIVADO** dos itens — sem pendência = `Aprovado`; com pendência
  = `AprovadoComPendencias`; com pendência + infração grave = `Reprovado` (nunca arbitrado).
- **I-VISA-4:** item **não conforme exige observação** (descreve a pendência).
- **I-VISA-5:** auto só se lavra a partir de **inspeção concluída** do mesmo estabelecimento; número de auto **único** por tenant.
- **I-VISA-6:** **licenciamento** de risco **médio/alto** exige inspeção **concluída e não reprovada**;
  risco **baixo** dispensa licenciamento prévio (Lei 13.874/2019) — emite por mero registro.
- **I-VISA-7:** estabelecimento **inativo/interditado** não recebe inspeção nem licença; licença **cassada** não renova.
- Unicidade do cadastro por `(TenantId, Documento, RazaoSocial)`.

## Endpoints (`/api/saude/vigilancia`)
- `POST /estabelecimentos` — CadastrarEstabelecimentoFiscalizavelCommand `[saude.vigilancia.gerenciar]`
- `GET  /estabelecimentos` — BuscarEstabelecimentosFiscalizaveisQuery (paginado) `[saude.vigilancia.ver]`
- `PUT  /estabelecimentos/{id}/classificacao` — ReclassificarEstabelecimentoCommand `[saude.vigilancia.gerenciar]`
- `POST /estabelecimentos/{id}/interdicao` — InterditarEstabelecimentoCommand `[saude.vigilancia.gerenciar]`
- `POST /estabelecimentos/{id}/levantamento-interdicao` — LevantarInterdicaoCommand `[saude.vigilancia.gerenciar]`
- `POST /inspecoes` — AbrirInspecaoCommand `[saude.vigilancia.inspecionar]`
- `GET  /inspecoes/agenda` — ListarAgendaInspecoesQuery `[saude.vigilancia.ver]`
- `GET  /inspecoes/{id}` — ObterInspecaoPorIdQuery `[saude.vigilancia.ver]`
- `POST /inspecoes/{id}/itens` — RegistrarItemInspecaoCommand `[saude.vigilancia.inspecionar]`
- `POST /inspecoes/{id}/conclusao` — ConcluirInspecaoCommand `[saude.vigilancia.inspecionar]`
- `POST /inspecoes/{id}/cancelamento` — CancelarInspecaoCommand `[saude.vigilancia.inspecionar]`
- `POST /inspecoes/{inspecaoId}/autos` — LavrarAutoCommand `[saude.vigilancia.autuar]`
- `GET  /autos?status=` — ListarAutosQuery `[saude.vigilancia.ver]`
- `POST /autos/{id}/defesa` — ApresentarDefesaAutoCommand `[saude.vigilancia.autuar]`
- `POST /autos/{id}/julgamento` — JulgarAutoCommand `[saude.vigilancia.autuar]`
- `POST /autos/{id}/regularizacao` — RegularizarIntimacaoCommand `[saude.vigilancia.autuar]`
- `POST /licencas` — EmitirLicencaCommand `[saude.vigilancia.licenciar]`
- `GET  /estabelecimentos/{id}/licencas` — ListarLicencasDoEstabelecimentoQuery `[saude.vigilancia.ver]`
- `GET  /licencas/a-vencer` — ListarLicencasAVencerQuery `[saude.vigilancia.ver]`
- `POST /licencas/{id}/renovacao` — RenovarLicencaCommand `[saude.vigilancia.licenciar]`
- `POST /licencas/{id}/cassacao` — CassarLicencaCommand `[saude.vigilancia.licenciar]`

## M10 (atrás de ACL)
- Transmissão ao **SINAVISA** (estadual) e **e-SUS VS** (DATASUS) via Outbox após credencial/A1.
- Inscrição da multa em **Dívida Ativa** (Tributos.Contracts) quando auto de infração indeferido com valor.

<!-- manifest
commands: CadastrarEstabelecimentoFiscalizavel, ReclassificarEstabelecimento, InterditarEstabelecimento, LevantarInterdicao, AbrirInspecao, RegistrarItemInspecao, ConcluirInspecao, CancelarInspecao, LavrarAuto, ApresentarDefesaAuto, JulgarAuto, RegularizarIntimacao, EmitirLicenca, RenovarLicenca, CassarLicenca
queries: BuscarEstabelecimentosFiscalizaveis, ListarAgendaInspecoes, ObterInspecaoPorId, ListarAutos, ListarLicencasDoEstabelecimento, ListarLicencasAVencer
domainEvents: EstabelecimentoFiscalizavelCadastrado, EstabelecimentoVisaInterditado, InspecaoAberta, InspecaoConcluida, AutoVisaLavrado, AutoVisaJulgado, LicencaSanitariaEmitida, LicencaSanitariaCassada
integrationEventsPublished: 
integrationEventsConsumed: 
-->

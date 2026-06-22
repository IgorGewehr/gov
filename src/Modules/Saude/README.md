# Módulo Saude
> Atenção à saúde municipal (SUS): APS/UBS, prontuário eletrônico, regulação, farmácia e imunização, com interoperabilidade RNDS. · Poder: Executivo · Schema EF Core: `saude` · Ativável por tenant.

## 1. Propósito & Marco Legal
O contexto **Saude** materializa a gestão da atenção à saúde municipal sob o SUS: registro clínico longitudinal (PEP/e-SUS APS), regulação de procedimentos, assistência farmacêutica e imunização, com interoperabilidade nacional via **RNDS**. Trata dado pessoal **sensível** (LGPD art. 11).

- **CF arts. 196–200**: saúde como direito de todos e dever do Estado; diretrizes do SUS.
- **Lei 8.080/1990 + Lei 8.142/1990**: organização e participação social no SUS.
- **PNAB — Portaria GM/MS 2.436/2017**: Política Nacional de Atenção Básica (ESF/UBS).
- **Portaria 1.412/2013**: SISAB e estratégia e-SUS APS (envio de produção).
- **Portaria 1.434/2020**: RNDS como barramento nacional de interoperabilidade.
- **Resolução CFM 1.821/2007 + Lei 13.787/2018**: prontuário eletrônico (NGS1/NGS2, ICP-Brasil), guarda mínima 20 anos.
- **Lei 14.510/2022 + Resolução CFM 2.314/2022**: telessaúde e telemedicina.
- **Portaria SVS/MS 344/1998 + RDC ANVISA 22/2014 (SNGPC) + RENAME**: controlados e assistência farmacêutica.

## 2. Linguagem Ubíqua
- **SUS** — Sistema Único de Saúde; rede pública integrada.
- **APS** — Atenção Primária à Saúde; porta de entrada preferencial.
- **UBS / ESF** — Unidade Básica de Saúde / Estratégia Saúde da Família.
- **PEP / PEC** — Prontuário Eletrônico do Paciente / do Cidadão (e-SUS APS).
- **e-SUS APS / CDS** — sistema da APS; Coleta de Dados Simplificada.
- **CNS / CADSUS** — Cartão Nacional de Saúde; base nacional de usuários.
- **CNES** — Cadastro Nacional de Estabelecimentos de Saúde.
- **RNDS** — Rede Nacional de Dados em Saúde (barramento FHIR).
- **SISREG** — sistema de regulação de consultas, exames e leitos.
- **SISAB** — Sistema de Informação em Saúde para a Atenção Básica.
- **CID-10 / CIAP-2** — classificações de diagnóstico (clínica / APS).
- **AIH / APAC** — autorizações de internação / procedimentos ambulatoriais.
- **CBO / INE** — ocupação do profissional / código da equipe.
- **HORUS / BNAFAR** — sistema e base nacional de assistência farmacêutica.
- **RENAME** — Relação Nacional de Medicamentos Essenciais.
- **Dispensacao** — entrega de medicamento ao paciente mediante receituário.
- **LoteMedicamento** — lote rastreável com validade e saldo.
- **ControleEspecial / SNGPC** — medicamentos da Portaria 344/98; monitoramento ANVISA.
- **SI-PNI / Imunobiologico** — sistema de imunização; insumo vacinal.
- **RedeDeFrio / EsquemaVacinal** — cadeia de conservação (2–8 °C) / protocolo de doses.
- **CadernetaDeVacinacao** — histórico vacinal do cidadão.
- **Telessaude / Teleconsulta** — atendimento remoto regulado (Lei 14.510).

## 3. Mapa de Domínio
- **Paciente** [raiz · chave **CNS** · `IMustHaveTenant`]
  - Entidades: `CondicaoDeSaude`, `Alergia`.
  - VOs: `Identificacao`, `Endereco`.
- **Estabelecimento** [raiz · **CNES** · `IMustHaveTenant`]
  - Entidades: `Equipe` (INE), `Profissional` (CBO), `Agenda`.
  - Evento: **PacienteAgendado**.
- **Atendimento** [raiz · `IMustHaveTenant`]
  - Entidades: `EvolucaoSOAP`, `Prescricao`, `SolicitacaoExame`.
  - VOs: `CID`, `CIAP`, `AssinaturaDigital` (ICP-Brasil).
  - Eventos: **AtendimentoRegistrado**, **AtendimentoAssinado**, **PEPLancadoNoSISAB**, **RESCompartilhadoNaRNDS**.
- **SolicitacaoRegulacao** [raiz · `IMustHaveTenant`]
  - VOs: `Procedimento` (SIGTAP), `Prioridade`, `Cota`.
  - Evento: **SolicitacaoAutorizada**.
- **Dispensacao** [raiz · `IMustHaveTenant`]
  - Entidades: `ItemDispensacao`, `Receituario`.
  - Evento: **MedicamentoDispensado**.
- **LoteMedicamento** [raiz · `IMustHaveTenant`] (lote, validade, saldo)
  - Eventos: **LoteVencido**, **EstoqueAbaixoDoMinimo**.
- **Imunizacao**: `LoteVacina` [raiz · lote, validade, fabricante, temperatura], `DoseAplicada`.
  - Evento: **VacinaAplicada**.
- **Teleconsulta** [raiz · `IMustHaveTenant`] (profissional, CRM, modalidade)
  - Evento: **TeleconsultaRealizada**.

Relações: `Atendimento` referencia `Paciente`/`Estabelecimento` por Id (sem navegação cross-aggregate); `Dispensacao` baixa `LoteMedicamento` por FEFO; `DoseAplicada` referencia `LoteVacina`.

## 4. Integrações & Padrões Técnicos
- **Clean Architecture + DDD**: invariantes na raiz; sem vazamento de entidades entre módulos.
- **MediatR**: commands/queries por caso de uso (`RegistrarAtendimentoCommand`, `DispensarMedicamentoCommand`).
- **EF Core 8**: schema `saude`; VOs como owned types; global query filter por `TenantId`.
- **Outbox**: Integration Events persistidos transacionalmente.
- **RNDS**: barramento **HL7 FHIR R4** (Bundles), **mTLS + ICP-Brasil**.
- **CADSUS** (validação CNS, PIX/PDQ), **CNES** (master data), **e-SUS→SISAB** (produção na competência), **SISREG**, **HORUS/BNAFAR**, **SNGPC/ANVISA**, **SI-PNI/SIES** (doses + rede de frio).
- Todas idempotentes, resilientes (**Polly**) e atrás de **ACL**.

## 5. Regras de Negócio Críticas
- Atendimento exige **CNS válido** + CNES e profissional/CBO ativos na competência.
- Registro clínico **append-only**: evolução assinada não se edita — só **adendo datado/reassinado**.
- Eliminação do papel só com **NGS2 + ICP-Brasil**; **guarda mínima 20 anos** (Lei 13.787).
- **FEFO** de lotes: bloquear dispensação/aplicação de lote vencido; rastreabilidade ponta a ponta (recall).
- Vacina exige **lote + fabricante + temperatura**; excursão térmica fora de **2–8 °C** invalida o lote.
- Medicamento **controlado (344/98)** exige receituário específico + farmacêutico responsável.
- Teleconsulta exige **CRM ativo** e direito a atendimento presencial.

## 6. Multi-Tenancy, Segurança & Auditoria
Todas as raízes implementam `IMustHaveTenant`; `TenantId` aplicado por global query filter e **nunca aceito do cliente**. Base legal LGPD: **tutela da saúde** (art. 11, II, "f") — sem necessidade de consentimento, com minimização. **Trilha de acesso ao prontuário** registra quem leu, quando e qual CNS, além do *audit trail* imutável (antes/depois, autor, IP, timestamp) para o Tribunal de Contas. Claims distintas por operação sensível (`saude.atender`, `saude.dispensar`, `saude.regular`).

## 7. Contratos Públicos (Integration Events)
**Publica:** `AtendimentoAssinado`, `RESCompartilhadoNaRNDS`, `MedicamentoDispensado`, `VacinaAplicada` (analítica/**Transparencia** com dados agregados/anonimizados); `LoteVencido`, `EstoqueAbaixoDoMinimo` (alertas de **Patrimonio**/almoxarifado).
**Consome:** `FornecedorHabilitado` (**Administracao**) para credenciar fornecedor de insumos; `BemIncorporado` (**Patrimonio**) para vincular equipamento à UBS.

## 8. Cenários BDD
**Cenário 1 — Atendimento sem CNS válido**
Dado um `Paciente` sem CNS confirmado no CADSUS
Quando registro um `Atendimento`
Então a operação é rejeitada e nenhum evento é publicado.

**Cenário 2 — Evolução assinada é imutável**
Dada uma `EvolucaoSOAP` já assinada (ICP-Brasil)
Quando tento editar o texto da evolução
Então a edição é bloqueada e só é permitido um adendo datado/reassinado.

**Cenário 3 — Compartilhamento na RNDS**
Dado um `Atendimento` assinado em NGS2
Quando o evento **AtendimentoAssinado** é processado
Então um Bundle FHIR R4 é enviado via mTLS e **RESCompartilhadoNaRNDS** é publicado.

**Cenário 4 — Dispensação FEFO com lote vencido**
Dados dois `LoteMedicamento` do mesmo item, um vencido
Quando dispenso o medicamento
Então o lote vencido é bloqueado, a baixa ocorre no lote válido mais próximo do vencimento e **MedicamentoDispensado** é publicado.

**Cenário 5 — Excursão térmica invalida lote de vacina**
Dado um `LoteVacina` que registrou temperatura de 12 °C (fora de 2–8 °C)
Quando o monitoramento da rede de frio processa a leitura
Então o lote é invalidado e indisponibilizado para aplicação.

**Cenário 6 — Solicitação de regulação autorizada**
Dada uma `SolicitacaoRegulacao` com `Procedimento` SIGTAP e `Cota` disponível
Quando o regulador autoriza
Então **SolicitacaoAutorizada** é publicada e a vaga é reservada no SISREG.

## 9. Fontes
- e-SUS APS / SISAB — https://sisaps.saude.gov.br/sistemas/esusaps/
- RNDS (SEIDIGI/MS) — https://www.gov.br/saude/pt-br/composicao/seidigi/rnds
- SI-PNI — https://si-pni.saude.gov.br/
- SNGPC / Portaria 344/98 (ANVISA) — https://www.gov.br/anvisa/
- Lei 14.510/2022 (telessaúde) — https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2022/lei/L14510.htm

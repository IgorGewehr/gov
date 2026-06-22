# Auditoria de Backend — Saúde, Educação, Assistência Social, Protocolo, Legislativo, Transparência

> Auditoria empírica (leitura de código, contagem de arquivos/handlers/endpoints), não estimativa.
> Base: `src/Modules/<Modulo>/` + `tests/`. Data da migration inicial de todos os módulos: 21/06/2026.

## Veredito executivo (1 parágrafo)

Os seis módulos seguem **fielmente o padrão de referência de Tributos**: Clean Architecture +
DDD tático, domínio **rico** (agregados de 150–410 LOC com máquina de estados, invariantes
numeradas I-n, domain events), CQRS via MediatR, EF Core com `DbContext`/migration/configurations
por módulo, endpoints Minimal API protegidos por `RequirePermission`, e suíte de testes substancial
(45–69 testes por módulo). **A camada de domínio e aplicação é REAL e madura.** O que é **STUB**
de forma sistemática é **toda a integração governamental externa** (classes `Simulado*` registradas
no DI): SISREG, RNDS, SISAB, CADSUS, assinatura ICP-Brasil (Saúde); CadÚnico/MDS (Assistência);
ACT ICP-Brasil (Protocolo); SIAPC/PAD (TCE-RS), SICONFI, e-Validador, catálogo de leiautes
(Transparência). **Para o dono, o ponto mais crítico é Transparência**: a máquina de estados da
remessa TCE-RS e da declaração SICONFI está pronta, mas **o leiaute real do TCE-RS / MSC do
SICONFI NÃO é gerado** — o "pacote" é texto `Tipo|Conteudo` vindo de um read model simulado.

---

## Tabela quantitativa (empírica)

| Módulo | Domain .cs | App .cs | Infra .cs | Handlers | Endpoints | DbSets | Testes | LOC total |
|---|---|---|---|---|---|---|---|---|
| Saúde | 27 | 42 | 14 | 27 | 25 | 3 | 54 | 6.356 |
| Educação | 19 | 24 | 12 | 19 | 19 | 3 | 53 | 3.973 |
| Assistência Social | 19 | 24 | 19 | 15 | 15 | 5 | 63 | 5.435 |
| Protocolo | 14 | 27 | 13 | 17 | 12 | 2 | 46 | 3.571 |
| Legislativo | 19 | 40 | 12 | 33 | 31 | 3 | 69 | 5.436 |
| Transparência | 19 | 24 | 17 | 14 | 13 | 2 | 45 | 4.971 |

Migrations iniciais reais (não vazias): Saúde 367 / Educação 282 / Assistência 412 / Protocolo 225
/ Legislativo 379 / Transparência 355 LOC.

---

## O que existe (por módulo)

### Saúde (Executivo)
- **Agregados:** `Paciente` (169 LOC), `Atendimento` (345 LOC, evolução SOAP, CID, CIAP, prescrição,
  solicitação de exame, adendo, assinatura), `SolicitacaoRegulacao` (235 LOC, fila/autorização/execução/negação/devolução/cancelamento).
- **Casos de uso (27 handlers):** cadastro/atualização/inativação de paciente, registro de alergia/condição,
  histórico clínico, registro/cancelamento/assinatura de atendimento, evolução SOAP, regulação completa.
- **Integrações como Contracts:** consome `BemIncorporado` e `FornecedorHabilitado` (Patrimônio/Administração).
- **Endpoints:** 25, todos com `RequirePermission`.

### Educação (Executivo)
- **Agregados:** `Escola` (152 LOC, código INEP, infraestrutura, censo), `Matricula` (147 LOC,
  matricular/rematricular/transferir/encerrar/situação), `DiarioClasse` (182 LOC, aula, frequência, nota, apuração).
- **19 handlers / 19 endpoints.** Eventos de integração: `AlunoMatriculado`, `MatriculaEncerrada`, `ResultadoApurado`.

### Assistência Social (Executivo)
- **Agregados:** `Familia` (263 LOC, renda per capita, NIS, membros, vigência cadastral),
  `Beneficio` (206 LOC, elegibilidade por critérios, concessão/indeferimento), `ProntuarioSuas` (221 LOC,
  acompanhamento, violação de direito, **trilha de acesso** — LGPD).
- **Parametrização real:** `ParametroVigenteProvider` lê salário mínimo/limites por vigência de tabela
  própria — **não** hardcoded (aderente a §7 da Constituição; ausência de parâmetro retorna nulo, B-11).
- **15 handlers / 15 endpoints.** 5 DbSets (inclui lookup de unidade de atendimento CRAS/CREAS).

### Protocolo (Ambos)
- **Agregados:** `Processo` (249 LOC, autuação, tramitação, despacho, sobrestamento, arquivamento, NUP,
  classificação CONARQ, prazo), `Documento` (220 LOC, juntada, assinatura, hash, sem efeito).
- **Serviços de infra reais (não simulados):** `NupSequencialGenerator` (NUP padrão Decreto 8.539/2015
  com dígito verificador módulo 11) e `CarimboDeTempoLocalService` (relógio local como ACL).
- **17 handlers / 12 endpoints.** Handlers de integração de entrada (autuar/tramitar/juntar/assinar/arquivar via Contracts).

### Legislativo (Legislativo)
- **Agregados:** `Sessao` (231 LOC, agendar/abrir/suspender/reabrir/encerrar, quórum, presença, ordem do dia),
  `Votacao` (242 LOC, **maioria simples/absoluta/qualificada**, modalidade simbólica/nominal/secreta,
  turnos, voto idempotente, resultado), `Proposicao` (408 LOC, tramitação, emenda, substitutivo, parecer, autógrafo).
- **33 handlers / 31 endpoints** (maior cobertura de casos de uso entre os seis). Painel eletrônico modelado no domínio.
- **Integração:** consome `Sancao`/`Veto` do Executivo; publica `AutografoEnviado`, `ResultadoVotacao`, `SessaoRealizada`.

### Transparência (Ambos) — **módulo mais sensível para o dono**
- **Agregados:** `RemessaTce` (251 LOC, ciclo Gerada→Validada→Enviada→Homologada/Rejeitada, hash de
  integridade, alerta de prazo LRF art. 23 §3º) e `DeclaracaoFiscal` (260 LOC, MSC/RREO/RGF/DCA,
  matriz de saldos balanceada PCASP, ciclo Consolidada→Transmitida→Homologada/Rejeitada).
- **ACL de entrada real:** `ReceberMSCGeradaHandler` consome `MSCGeradaIntegrationEvent` do módulo
  **Financas** (papel CONSUMIDOR — não recalcula contabilidade primária). Boa separação.
- **14 handlers / 13 endpoints.**

---

## Profundidade vs. escopo legal

| Módulo | Domínio rico? | Escopo legal coberto no domínio | Lacuna de escopo |
|---|---|---|---|
| Saúde | Sim | PEP/SOAP, regulação, imunização parcial | SI-PNI, HÓRUS (farmácia/estoque), telessaúde **ausentes** como agregados |
| Educação | Sim | Matrícula, diário, censo INEP | **Merenda PNAE e Transporte PNATE ausentes** (citados na Constituição §4) |
| Assistência | Sim | SUAS, CadÚnico (leitura), benefícios, prontuário | Integração CadÚnico só leitura simulada; BPC/PBF dependem de parâmetro externo |
| Protocolo | Sim | NUP CONARQ, assinatura, hash, temporalidade | GED/armazenamento de binário e ACT credenciada reais ausentes |
| Legislativo | Sim (mais completo) | Sessões, proposições, comissões parciais, votação/painel | Comissões como agregado próprio limitadas a parecer/distribuição |
| Transparência | Sim (máquina de estados) | LRF, ciclo de remessa/declaração | **Geração do leiaute real TCE-RS (SIAPC/PAD) e MSC SICONFI ausente** |

---

## Real × Stub (o mapa que importa)

**REAL e maduro (todos os 6 módulos):** entidades de domínio, value objects, domain events, handlers
CQRS, validators FluentValidation, DbContext + Fluent configurations + migration + repositories,
endpoints Minimal API com RBAC, multi-tenancy via `IMustHaveTenant`. ~33.700 LOC somadas.
Testes: **330 métodos de teste** no total nos seis módulos.

**STUB / `Simulado*` (registrados no DI hoje — substituir na Fase 5):**

| Módulo | Interface (porta) | Implementação atual | Produção esperada |
|---|---|---|---|
| Saúde | `ICadsusGateway` | `SimuladoCadsusGateway` (só valida formato CNS) | PIX/PDQ via ACL HTTP+Polly |
| Saúde | `IEstabelecimentoRepository` | `Simulado…` (retorna true se GUID != Empty) | master data CNES |
| Saúde | `IRndsGateway` | `Simulado…` (retorna string fake) | Bundle FHIR R4 mTLS+ICP-Brasil |
| Saúde | `ISisabGateway` | `Simulado…` (no-op) | e-SUS APS / SISAB CDS |
| Saúde | `ISisregGateway` | `Simulado…` (string fake) | SISREG reserva/liberação |
| Saúde | `IAssinaturaIcpBrasilService` | `Simulado…` (assina sem certificado) | A1/A3 via Azure Key Vault |
| Saúde | `ICotaRepository` | `Simulado…` (10 vagas fixas) | tabela de cotas real |
| Assistência | `ICadUnicoGateway`/`ReadModel` | `SimuladoCadUnicoGateway` (renda 1200 / 3 membros fixos) | CadÚnico/MDS leitura |
| Protocolo | `ICarimboDeTempoService` | `CarimboDeTempoLocalService` (relógio local) | ACT credenciada ICP-Brasil (Lei 14.063/2020) |
| Transparência | `ISiapcPadGateway` | `SimuladoSiapcPadGateway` (no-op) | transmissão TCE-RS assinada A1 |
| Transparência | `ISiconfiGateway` | `SimuladoSiconfiGateway` (protocolo determinístico) | SOAP/REST STN |
| Transparência | `IEValidadorTce` | `SimuladoEValidadorTce` | e-Validador (RDI) real |
| Transparência | `ILeiauteCatalogo` | `SimuladoLeiauteCatalogo` (aceita qualquer leiaute) | Resolução TCE-RS vigente |
| Transparência | `ICalendarioFiscal` | `SimuladoCalendarioFiscal` | calendário oficial de prazos |
| Transparência | `IPublicacaoTransparenciaRepository` | `Simulado…` (3 linhas fixas `CABECALHO/BALANCO/RODAPE`) | read model materializado de eventos |

> Educação e Legislativo **não registram nenhum `Simulado*`** — são auto-contidos (sem integração
> externa direta hoje), comunicando-se só por Integration Events. Isso é correto para o escopo atual.

---

## Lacunas e riscos concretos

1. **[CRÍTICO — Transparência/TCE]** O leiaute **real** do TCE-RS (SIAPC/PAD) e a **MSC do SICONFI**
   não são gerados. `GerarRemessaTceHandler.MontarPacote` produz um `.txt` com linhas `Tipo|Conteudo`
   de 3 itens fixos vindos de `SimuladoPublicacaoTransparenciaRepository`. A máquina de estados,
   o hash e o alerta de prazo estão corretos, mas **o conteúdo enviado ao Tribunal de Contas hoje é
   fictício**. É a maior preocupação do dono (prestação de contas) e a maior distância para produção.

2. **[CRÍTICO — Assinatura/A1]** `SimuladoAssinaturaIcpBrasilService` (Saúde) e
   `CarimboDeTempoLocalService` (Protocolo) **não usam certificado real do Azure Key Vault**. Toda
   assinatura digital e carimbo de tempo são juridicamente inválidos até a integração real (Lei 14.063/2020).

3. **[ALTO — concorrência] `NupSequencialGenerator`** deriva o sequencial de `Processos.CountAsync` por
   ano. Sob autuações concorrentes, dois processos podem calcular o **mesmo sequencial** (race condition);
   o índice único `(TenantId, Nup)` evita duplicata persistida, mas causa **falha/colisão** em vez de gap
   tolerado. Trocar por sequência transacional/`INSERT` atômico ou tabela de contador por (tenant, ano).

4. **[MÉDIO — escopo] PNAE (merenda) e PNATE (transporte)** não existem como agregados em Educação;
   **SI-PNI, HÓRUS (farmácia) e telessaúde** ausentes em Saúde. São escopos previstos na Constituição §4
   ainda não modelados.

5. **[MÉDIO — Transparência] `SimuladoLeiauteCatalogo`** aceita **qualquer** leiaute não vazio e deriva
   data-limite determinística; em produção precisa carregar a Resolução TCE-RS vigente e prazos
   parametrizados por tenant (não inventar prazo).

6. **[BAIXO — observação]** Não foi verificado nesta auditoria se há registro de **trilha de acesso LGPD**
   efetiva em Saúde (Atendimento/Paciente) equivalente ao `AcessoProntuario` da Assistência Social —
   recomenda-se confirmar paridade, dado que dados de saúde são igualmente sensíveis (§6).

---

## Conclusão

Domínio e aplicação dos seis módulos estão **maduros e aderentes à Constituição** (isolamento, multi-tenant,
auditoria, domínio rico, CQRS, testes). A Fase 5 (integrações governamentais reais) é o trabalho
pendente comum, com **Transparência (leiaute TCE-RS/MSC SICONFI)** e **assinatura A1/ACT (Saúde+Protocolo)**
como prioridades absolutas para o foco do dono em contabilidade e prestação de contas ao TCE.

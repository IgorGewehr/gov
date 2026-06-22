# Módulo AssistenciaSocial
> Gestão do SUAS — referenciamento de famílias, acompanhamento (PAIF/PAEFI), benefícios (BPC/PBF/eventuais) e vigilância socioassistencial. · Poder: Executivo · Schema EF Core: `assistenciasocial` · Ativável por tenant.

## 1. Propósito & Marco Legal
Bounded Context da **gestão municipal do SUAS** (Sistema Único de Assistência Social): organiza a oferta de serviços por **território**, **referencia e acompanha famílias** em situação de vulnerabilidade/risco e **opera benefícios**. Marco: **CF arts. 203–204**; **Lei 8.742/1993 (LOAS)**, alterada pela **Lei 12.435/2011** (institui o SUAS); **PNAS/2004** (Res. CNAS 145/2004); **NOB-SUAS/2012** (Res. CNAS 33/2012); **Tipificação Nacional dos Serviços Socioassistenciais** (Res. CNAS 109/2009); **Lei 14.601/2023** (Programa Bolsa Família). Organiza-se em **Proteção Social Básica (PSB)** — prevenção, via **CRAS/PAIF** — e **Proteção Social Especial (PSE)** — média/alta complexidade, via **CREAS/PAEFI** e **Centro POP**. O **CadÚnico (MDS)** é a base **federal autoritativa** de identificação socioeconômica; este módulo **consome** e **registra atendimentos**, jamais sobrescreve a base federal.

## 2. Linguagem Ubíqua
1. **SUAS** — sistema descentralizado e participativo da assistência social.
2. **LOAS** — Lei Orgânica da Assistência Social (8.742/1993).
3. **PNAS** — Política Nacional de Assistência Social (2004).
4. **ProtecaoSocialBasica (PSB)** — prevenção de riscos por fortalecimento de vínculos.
5. **ProtecaoSocialEspecial (PSE)** — proteção a direitos violados (média/alta complexidade).
6. **CRAS** — Centro de Referência de Assistência Social (porta de entrada PSB).
7. **CREAS** — Centro de Referência Especializado (PSE).
8. **CentroPOP** — unidade para população em situação de rua.
9. **PAIF** — Serviço de Proteção e Atendimento Integral à Família (só em CRAS).
10. **PAEFI** — Serviço de Proteção e Atendimento Especializado a Famílias e Indivíduos (só em CREAS).
11. **SCFV** — Serviço de Convivência e Fortalecimento de Vínculos.
12. **CadUnico** — Cadastro Único; base federal de renda/composição familiar.
13. **NIS** — Número de Identificação Social (chave do CadÚnico).
14. **BPC** — Benefício de Prestação Continuada (idoso ≥ 65 / PCD).
15. **PBF** — Programa Bolsa Família (Lei 14.601/2023).
16. **BeneficioEventual** — provisão temporária (natalidade, funeral, cesta básica).
17. **ProntuarioSUAS** — registro do acompanhamento familiar (sigiloso).
18. **RMA** — Registro Mensal de Atendimentos (consolidado por unidade).
19. **CensoSUAS** — coleta anual sobre a rede e equipamentos.
20. **VigilanciaSocioassistencial** — análise territorial de demandas e da oferta.
21. **BuscaAtiva** — identificação proativa de famílias invisíveis aos serviços.

## 3. Mapa de Domínio
**Agregados (raízes — todos `IMustHaveTenant`):**
- **Familia** — entidade `MembroFamiliar`; VOs `NIS`, `CPF`, `RendaPerCapita`, `EnderecoTerritorializado`. Calcula renda per capita e vincula a um território/CRAS.
- **Prontuario** — entidades `RegistroAcompanhamento`, `PlanoAcompanhamentoFamiliar`, `ViolacaoDireito`. Documento sigiloso do acompanhamento (PAIF/PAEFI).
- **UnidadeAtendimento** (CRAS/CREAS/CentroPOP) — entidades `EquipeReferencia`, `ServicoOfertado` (PAIF/PAEFI/SCFV); VO `TerritorioCobertura`.
- **Beneficio** — entidades `BeneficioEventual`, `ConcessaoBPC`, `VinculoPBF`; VO `CriterioElegibilidade`. Avalia elegibilidade e registra concessão/indeferimento.
- **RegistroMensal (RMA)** — consolida atendimentos por unidade/competência para exportação ao MDS.

**Read models:** resumo do CadÚnico por NIS/CPF (folha resumo federal, composição, renda — somente leitura).

**Eventos de Domínio:** `FamiliaReferenciada`, `AtendimentoRegistrado`, `AcompanhamentoEncerrado`, `BeneficioConcedido`, `BeneficioIndeferido`, `CestaBasicaEntregue`, `RmaConsolidada`.

## 4. Integrações & Padrões Técnicos
- **CadÚnico / MDS (consulta por NIS/CPF):** folha resumo, composição familiar, renda e condicionalidades. A **base federal é a fonte autoritativa**; o módulo apenas projeta um read model. `HttpClient` tipado + **Polly** (retry exponencial + jitter, circuit breaker, timeout) atrás de **ACL** que traduz o contrato federal para o domínio.
- **PBF / SISC:** consulta de vínculos e condicionalidades do Bolsa Família.
- **Prontuário Eletrônico Simplificado (MDS):** interoperabilidade do acompanhamento familiar.
- **RMA → SAGI/MDS:** **exportação mensal** do consolidado de atendimentos por unidade.
- **Censo SUAS / Rede SUAS:** **envio anual** sobre equipamentos e oferta.
- **Mensageria interna:** **cross-module exclusivamente via Integration Events** (Outbox + MediatR). Ex.: benefício concedido pode notificar **Financas** (provisão de recurso) e **Transparencia** (dados agregados/anonimizados). **Nunca** trafega dado sensível identificável entre módulos/tenants.

## 5. Regras de Negócio Críticas
- **Elegibilidade CadÚnico:** renda per capita **≤ ½ salário mínimo**; **atualização obrigatória a cada 24 meses** ou **imediata** em mudança de endereço/renda/composição.
- **BPC:** exige **idade ≥ 65 anos** OU **PCD** (avaliação biopsicossocial) e renda per capita **< ¼ do salário mínimo**; **não acumula** com outro benefício da Seguridade Social.
- **Benefícios eventuais:** renda **≤ ½ SM**, com **preferência a inscritos no CadÚnico**; provisão temporária e excepcional.
- **Oferta por unidade:** **PAIF só em CRAS**; **PAEFI só em CREAS** — bloquear oferta incompatível com o tipo de unidade.
- **Parametrização por tenant:** valor do salário mínimo, prazos e critérios são **versionados por vigência** (aplica-se a regra vigente na competência), nunca *hardcoded*.

## 6. Multi-Tenancy, Segurança & Auditoria
- Todos os agregados implementam **`IMustHaveTenant`**; query filter global por `TenantId` no schema `assistenciasocial` — isolamento por município.
- **LGPD — dado sensível (art. 11):** vulnerabilidade social, saúde, deficiência e **crianças/adolescentes**. Base legal: **execução de política pública** (art. 11, II, "b" / art. 23) — **não** consentimento. Princípios de **minimização** e finalidade.
- **Sigilo profissional** sobre o `Prontuario`; **trilha de acesso ao prontuário** (quem leu, quando, por quê) imutável, exigível pelo controle social/Tribunal de Contas.
- **Dados do CadÚnico federal NÃO trafegam entre tenants** nem para módulos não autorizados; read model isolado por `TenantId`.
- Trilha de auditoria imutável para referenciamento, registro de atendimento, concessão/indeferimento de benefício e consolidação de RMA; eventos persistidos via **Outbox**.

## 7. Contratos Públicos (Integration Events)
- `FamiliaReferenciada` — {tenantId, familiaId, nisMascarado, unidadeId, territorio, dataReferenciamento}
- `AtendimentoRegistrado` — {tenantId, prontuarioId, unidadeId, servico (PAIF/PAEFI/SCFV), dataAtendimento}
- `AcompanhamentoEncerrado` — {tenantId, prontuarioId, motivoEncerramento}
- `BeneficioConcedido` — {tenantId, beneficioId, familiaId, tipo (BPC/PBF/Eventual), competencia, valor}
- `BeneficioIndeferido` — {tenantId, beneficioId, familiaId, tipo, motivoIndeferimento}
- `CestaBasicaEntregue` — {tenantId, beneficioId, familiaId, quantidade, dataEntrega}
- `RmaConsolidada` — {tenantId, registroMensalId, unidadeId, competencia, totalAtendimentos}

> Payloads expõem apenas identificadores e dados estritamente necessários; NIS/CPF são mascarados — sem dado sensível identificável no barramento.

## 8. Cenários BDD
**Cenário: Referenciamento de família ao CRAS**
- **Dado** uma família com NIS válido no CadÚnico e endereço dentro do território de um CRAS
- **Quando** a equipe registra o referenciamento
- **Então** a `Familia` é vinculada à `UnidadeAtendimento` e `FamiliaReferenciada` é publicado.

**Cenário: Indeferimento de BPC por renda**
- **Dado** um requerente PCD com renda per capita **≥ ¼ do salário mínimo**
- **Quando** a elegibilidade do BPC é avaliada
- **Então** a concessão é **indeferida** e `BeneficioIndeferido` é publicado, com o motivo registrado.

**Cenário: Atualização cadastral obrigatória vencida**
- **Dado** uma família com última atualização do CadÚnico há **mais de 24 meses**
- **Quando** o sistema processa a vigência cadastral
- **Então** a família é sinalizada para atualização e elegibilidade a novos benefícios fica condicionada à regularização.

**Cenário: Oferta de serviço incompatível com a unidade**
- **Dado** uma `UnidadeAtendimento` do tipo **CRAS**
- **Quando** se tenta ofertar **PAEFI** (exclusivo de CREAS)
- **Então** a operação é rejeitada por invariante de domínio e nenhum evento é publicado.

**Cenário: Concessão de cesta básica (benefício eventual)**
- **Dado** uma família inscrita no CadÚnico com renda **≤ ½ SM** e situação de vulnerabilidade temporária
- **Quando** o benefício eventual é concedido e a cesta é entregue
- **Então** `BeneficioConcedido` e `CestaBasicaEntregue` são publicados, com trilha de auditoria.

**Cenário: Consolidação mensal do RMA**
- **Dado** os atendimentos registrados por uma unidade na competência
- **Quando** o RMA da unidade é fechado
- **Então** o `RegistroMensal` é consolidado, `RmaConsolidada` é publicado e o consolidado fica pronto para exportação ao SAGI/MDS.

**⭐ Cenário: Acesso sigiloso ao prontuário**
- **Dado** um `Prontuario` com dados sensíveis de violação de direitos de criança
- **Quando** um profissional o acessa
- **Então** o acesso é permitido somente sob autorização e **registrado na trilha** (quem/quando/por quê), sem trafegar o conteúdo a outros tenants.

## 9. Fontes
- LOAS — Lei 8.742/1993 (compilada, c/ Lei 12.435/2011): https://www.planalto.gov.br/ccivil_03/leis/l8742compilado.htm
- CadÚnico (MDS): https://www.gov.br/mds/pt-br/acoes-e-programas/cadastro-unico
- BPC, RMA, Prontuário SUAS, Censo SUAS (MDS): https://www.gov.br/mds/
- Complementares: CF arts. 203–204; PNAS/2004 (Res. CNAS 145/2004); NOB-SUAS/2012 (Res. CNAS 33/2012); Tipificação Nacional (Res. CNAS 109/2009); Lei 14.601/2023 (PBF); LGPD art. 11 (dado sensível).

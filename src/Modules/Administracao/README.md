# Módulo Administracao
> Gestão de compras públicas (licitações, contratações diretas, contratos e fornecedores) sob a NLLC. · Poder: Ambos · Schema EF Core: `administracao` · Ativável por tenant.

## 1. Propósito & Marco Legal
Conduz todo o ciclo de **compras públicas** do tenant: planejamento (ETP/TR), seleção do fornecedor (licitação ou contratação direta), formalização e execução contratual, sempre com publicidade no **PNCP**. Marco legal:
- **Lei 14.133/2021 (NLLC)** — substitui Lei 8.666/93, Lei 10.520/02 (pregão) e o RDC; norma central deste contexto.
- Decretos **11.462/2023** (PNCP), **10.024/2019** (pregão eletrônico), **11.246/2022** (agentes de contratação).
- **PNCP obrigatório** — art. 174: publicação é **condição de eficácia** do contrato e seus aditivos.
- **LC 101/2000 (LRF)** — vinculação a crédito orçamentário; **Lei 12.527/2011 (LAI)** — transparência ativa.
- Contabilidade patrimonial (MCASP/NBC TSP) e tombamento de bens permanecem no módulo **Patrimonio**; o empenho, no módulo **Financas**.

## 2. Linguagem Ubíqua
- **Licitacao** — procedimento de seleção competitiva da proposta mais vantajosa.
- **Pregao** — modalidade para bens/serviços comuns, julgamento por menor preço/maior desconto.
- **Concorrencia** — modalidade geral para bens/serviços especiais e obras.
- **DialogoCompetitivo** — modalidade para soluções inovadoras/complexas, com fase de diálogo.
- **Dispensa** — contratação direta por valor ou hipótese legal (art. 75).
- **Inexigibilidade** — contratação direta por inviabilidade de competição (art. 74).
- **ETP** — Estudo Técnico Preliminar; fundamenta a necessidade da contratação.
- **TR** — Termo de Referência; define objeto, requisitos e critérios.
- **Habilitacao** — verificação de aptidão jurídica, fiscal, técnica e econômica do licitante.
- **SICAF** — Sistema de Cadastramento Unificado de Fornecedores.
- **PNCP** — Portal Nacional de Contratações Públicas (publicidade oficial).
- **Aditivo** — alteração contratual quantitativa/qualitativa ou de prazo.
- **Apostilamento** — registro de alteração que dispensa termo aditivo (reajuste, dotação).
- **ReequilibrioEconomicoFinanceiro** — recomposição da equação econômica do contrato.
- **Garantia** — caução contratual de execução (≤ 5%, art. 96).
- **SRP** — Sistema de Registro de Preços; gera **Ata** com validade.
- **Fornecedor** — pessoa jurídica/física apta a contratar com a Administração.
- **Sancao** — penalidade administrativa (advertência, multa, impedimento, inidoneidade).
- **CriterioJulgamento** — menor preço, maior desconto, técnica e preço, etc.
- **Empenho (ref)** — referência ao ato de Financas que reserva dotação.

## 3. Mapa de Domínio
Todos os agregados implementam `IMustHaveTenant` (`Guid TenantId`).
- **Licitacao [raiz]** — VOs: `Modalidade`, `CriterioJulgamento`; entidades: `Lote`, `Proposta`, `Habilitacao`, `Recurso`. Eventos: `LicitacaoHomologada`, `LicitacaoFracassada`, `LicitacaoDeserta`, `LicitacaoRevogada`.
- **Fornecedor [raiz]** — VO `Cnpj`; `NivelCadastralSICAF`; entidade `Sancao`. Evento: `FornecedorSancionado`.
- **Contrato [raiz]** — entidades `Aditivo`, `Apostilamento`, `Garantia`; VO `EmpenhoRef`. Eventos: `ContratoAssinado`, `ContratoPublicadoPNCP`, `AditivoCelebrado`.

Relações: `Contrato` referencia a `Licitacao` (ou ato de Dispensa/Inexigibilidade) e o `Fornecedor` vencedor por **Id** (sem navegação cruzada entre raízes). Eventos de Domínio são despachados via MediatR; os de interesse externo viram Integration Events pelo **Outbox**.

## 4. Integrações & Padrões Técnicos
- **PNCP** — API REST/JSON, credenciamento por CNPJ; publicação de editais, contratos e aditivos. Cliente resiliente (Polly retry/circuit-breaker) chamado por handler do Outbox; idempotente por chave da contratação.
- **SICAF / Compras.gov.br** — consulta de regularidade e nível cadastral do fornecedor.
- **Receita** — validação de CNPJ na criação de `Fornecedor`.
- Padrões: Clean Architecture + DDD, MediatR (Commands/Queries/DomainEvents), EF Core 8 (schema `administracao`, filtro global por `TenantId`), Outbox para entrega confiável de Integration Events.

## 5. Regras de Negócio Críticas
- **Dispensa por valor** (art. 75): obras/eng. até **R$ 119.812,47**; demais bens/serviços até **R$ 59.906,02**.
- **Aditivo quantitativo** ≤ **25%** (bens/serviços) e até **+50%** em reforma (art. 125).
- **Garantia** ≤ **5%** do valor; até **10%** em obras de grande vulto (art. 96/98).
- **Publicação no PNCP** é **condição de eficácia** do contrato e dos aditivos (art. 174).
- **Vigência** vinculada à existência de **crédito orçamentário** (art. 105–106).
- Fornecedor com **inidoneidade** vigente **não** pode ser habilitado nem contratado.
- **Segregação de funções**: agente de contratação ≠ ordenador de despesa ≠ fiscal do contrato.

## 6. Multi-Tenancy, Segurança & Auditoria
Isolamento por `TenantId` em todo agregado (`IMustHaveTenant`) com query filter global do EF Core; nenhuma consulta omite o tenant. RBAC por papéis (agente de contratação, pregoeiro, ordenador, fiscal) reforça a segregação de funções. Auditoria imutável de homologação, assinatura, aditivos e sanções (ator, timestamp, valor); trilha alinhada à LAI para transparência ativa.

## 7. Contratos Públicos (*.Contracts)
**Publica:** `ContratoAssinadoIntegrationEvent` (consumido por **Financas** → empenho), `ContratoPublicadoPncpIntegrationEvent`, `AditivoCeleradoIntegrationEvent`, `LicitacaoHomologadaIntegrationEvent`, `AquisicaoBemHomologadaIntegrationEvent` (consumido por **Patrimonio** → tombamento), `FornecedorSancionadoIntegrationEvent`.
**Consome:** `EmpenhoEmitidoIntegrationEvent` (de **Financas**, confirma cobertura orçamentária) e `DotacaoIndisponivelIntegrationEvent` (bloqueia eficácia/assinatura). Cross-module ocorre **exclusivamente** via estes Integration Events.

## 8. Cenários BDD
**Cenário: Dispensa válida por valor**
Given um TR de serviços comuns no valor de R$ 40.000,00
When o agente de contratação registra uma `Dispensa`
Then o sistema aceita o enquadramento (≤ R$ 59.906,02) e exige justificativa.

**Cenário: Homologação publica integration event**
Given uma `Licitacao` na modalidade `Pregao` com proposta vencedora
When o ordenador homologa a licitação
Then `LicitacaoHomologada` é registrado e `LicitacaoHomologadaIntegrationEvent` é enfileirado no Outbox.

**Cenário: Eficácia condicionada ao PNCP**
Given um `Contrato` assinado mas ainda não publicado no PNCP
When se tenta executar/iniciar a vigência
Then o sistema bloqueia, pois a publicação no PNCP é condição de eficácia.

**Cenário: Aditivo acima do limite legal**
Given um `Contrato` de serviços com aditivos somando 20%
When se solicita acréscimo de mais 10% (total 30%)
Then o sistema rejeita o `Aditivo` por exceder 25% (art. 125).

**Cenário: Habilitação de fornecedor inidôneo**
Given um `Fornecedor` com `Sancao` de inidoneidade vigente
When ele apresenta proposta em uma licitação
Then a `Habilitacao` é negada automaticamente.

**Cenário: Contrato dispara empenho em Financas**
Given um `Contrato` recém-assinado com dotação informada
When `ContratoAssinadoIntegrationEvent` é publicado
Then **Financas** consome o evento e retorna `EmpenhoEmitidoIntegrationEvent`.

## 9. Fontes
- Lei 14.133/2021 — https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm
- PNCP — https://www.gov.br/pncp/
- Compras.gov.br — https://www.gov.br/compras/
- Decretos 11.462/2023, 10.024/2019, 11.246/2022; LC 101/2000; Lei 12.527/2011.

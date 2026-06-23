# Módulo Transparencia
> Transparência ativa/passiva, dados abertos (LAI) e remessas fiscais ao TCE-RS e SICONFI. · Poder: Ambos · Schema EF Core: `transparencia` · Ativável por tenant.

## 1. Propósito & Marco Legal
Contexto **CONSUMIDOR**: agrega dados produzidos pelos demais módulos para **publicar** (transparência ativa/passiva, dados abertos) e **prestar contas** (remessas TCE-RS, MSC ao SICONFI). Não é fonte primária — assina Integration Events e monta publicações/remessas.
- **Lei 12.527/2011 (LAI)** — transparência ativa (art. 8; §3º exige formato **aberto/estruturado/legível por máquina**); resposta passiva em 20 dias + 10 (art. 11).
- **LC 131/2009** — disponibilização da execução em **TEMPO REAL**.
- **LC 101/2000 (LRF)** — arts. 48 e 48-A (transparência da gestão fiscal); art. 23 §3º — atraso **bloqueia transferências voluntárias**.
- **Decretos 7.185/2010 e 10.540/2020 (SIAFIC)** — "tempo real" = até o **1º dia útil** após o registro contábil.
- **LGPD** (arts. 5, 6, 11, 23, 26) — anonimização antes de publicar base aberta.
- **Resoluções TCE-RS** (SIAPC/PAD, e-Validador), **Portarias STN** (Regras Gerais da MSC, SICONFI), **CGU** (EBT/eMAG).

## 2. Linguagem Ubíqua
- **TransparenciaAtiva** — divulgação espontânea e proativa (art. 8 LAI).
- **TransparenciaPassiva** — resposta a pedido de informação (eSIC).
- **TempoReal** — publicação até o 1º dia útil após o registro contábil (SIAFIC).
- **DadoAberto** — dado publicado em formato livre, reutilizável e legível por máquina.
- **FormatoAberto** — CSV/JSON/XML não proprietário (§3º art. 8).
- **Granularidade** — nível de detalhe do dado publicado (item, agregado).
- **MSC** — Matriz de Saldos Contábeis enviada ao SICONFI.
- **SICONFI** — Sistema de Informações Contábeis e Fiscais do Setor Público.
- **DCA** — Declaração de Contas Anuais.
- **RREO** — Relatório Resumido da Execução Orçamentária (bimestral).
- **RGF** — Relatório de Gestão Fiscal (quadrimestral).
- **SIAPC** — Sistema de Informações para Auditoria e Prestação de Contas (TCE-RS).
- **PAD** — Processo de Auditoria a Distância.
- **Remessa** — pacote de arquivos por leiaute enviado ao TCE-RS.
- **eValidador** — validador local de remessas TCE-RS (gera o RDI).
- **RDI** — Relatório de Dados e Informações (resultado da validação).
- **Leiaute** — especificação versionada de layout da remessa.
- **Anonimizacao** — mascaramento de dado pessoal (ex.: CPF) antes da publicação.
- **EBT** — Escala Brasil Transparente (avaliação CGU).
- **eMAG** — Modelo de Acessibilidade em Governo Eletrônico.
- **eSIC** — Sistema Eletrônico do Serviço de Informação ao Cidadão (canal passivo).
- **SIAFIC** — Sistema Único de execução orçamentária/financeira/contábil.

## 3. Mapa de Domínio
Todas as raízes implementam `IMustHaveTenant` (`Guid TenantId`). Eventos de Domínio via MediatR; os externos viram Integration Events pelo **Outbox**.
- **RemessaTCE [raiz]** — entidades `ArquivoRemessa`, `RegistroLeiaute`, `ResultadoValidacao` (RDI); VOs `Periodo`, `Leiaute(versao)`, `HashIntegridade`; estados `Gerada → Validada → Enviada → Homologada/Rejeitada`. Eventos: **RemessaGerada**, **RemessaValidada**, **RemessaRejeitada**, **RemessaEnviadaTCE**, **PrazoRemessaVencido**.
- **DeclaracaoFiscal [raiz]** (MSC/RREO/RGF/DCA) — entidades `MatrizSaldos`, `LinhaContabil`; VOs `Competencia`, `Quadrimestre`, `Bimestre`. Eventos: **MSCEnviadaSiconfi**, **DeclaracaoHomologada**.
- **ConjuntoDadosAberto [raiz]** — entidades `RecursoAberto` (CSV/JSON/XML), `Metadado`; VOs `Formato`, `Granularidade`, `PoliticaAnonimizacao`. Eventos: **DadoAbertoPublicado**, **DadoPessoalAnonimizado**.
- **PublicacaoTransparencia [raiz]** — itens **consumidos** dos demais módulos (despesas, contratos, folha, tributos, patrimônio) projetados para a transparência ativa e o eSIC. Evento: **PedidoLAIRespondido**.

Relações: raízes referenciam apenas por **Id**, sem navegação cruzada; `RemessaTCE` e `DeclaracaoFiscal` consolidam itens já materializados em `PublicacaoTransparencia`.

## 4. Integrações & Padrões Técnicos
- **Clean Architecture + DDD**: invariantes na raiz; sem vazamento de entidades de outros módulos.
- **MediatR**: Commands/Queries por caso de uso (`GerarRemessaTCECommand`, `PublicarDadoAbertoCommand`, `ResponderPedidoLAICommand`).
- **EF Core 8**: schema `transparencia`; owned types para VOs; global query filter por `TenantId`.
- **Outbox + Polly (ACL)**: entrega idempotente, resiliente (retry/circuit-breaker) das remessas.
- **SICONFI/STN**: envio mensal da **MSC** agregada (gera RREO/RGF/DCA).
- **TCE-RS SIAPC/PAD**: remessas por leiaute versionado, validação local via **e-Validador → RDI → SICOE**; certificado **A1** do **Azure Key Vault**, por tenant.
- **Dados abertos**: CSV/JSON/XML com dicionário de dados/metadados; portal aderente a **eMAG/WCAG 2.1 AA**.
- **`Span<T>`** para fatiar XMLs pesados de remessa sem alocação.

## 5. Regras de Negócio Críticas
- Despesa/receita publicadas em **tempo real** — até o **1º dia útil** após o registro contábil (SIAFIC).
- **MSC** mensal até o **último dia do mês subsequente**; **RGF**/**RREO** até **30 dias** após o quadrimestre/bimestre.
- **RemessaTCE** só transita a `Enviada` após **validação sem erro** (RDI limpo) — invariante da raiz.
- **Anonimização obrigatória** de dado pessoal (CPF mascarado) antes de publicar base aberta (LGPD).
- **Imutabilidade/retenção** dos arquivos de remessa com **HashIntegridade** verificável.
- Prazos parametrizáveis por tenant; atraso **bloqueia transferências voluntárias** (LRF art. 23 §3º).

## 6. Multi-Tenancy, Segurança & Auditoria
Toda raiz implementa `IMustHaveTenant`; `TenantId` por global query filter, nunca aceito do cliente. RBAC por claims (`transparencia.publicar`, `transparencia.remeter`, `transparencia.responder_lai`). Trilha **imutável** (ator, timestamp, IP, antes/depois) para o Tribunal de Contas. Acesso a dados pessoais antes da anonimização registra base legal e finalidade (LGPD); arquivos de remessa preservados com hash para auditoria do TCE-RS.

## 7. Contratos Públicos (este módulo é CONSUMIDOR)
Cross-module ocorre **exclusivamente** via Integration Events. **Assina:**
- `DespesaLiquidadaIntegrationEvent`, `DespesaPagaIntegrationEvent`, `MSCGeradaIntegrationEvent` (**Financas**) — execução em tempo real e composição da MSC/RREO/RGF.
- `ContratoAssinadoIntegrationEvent`, `ContratoPublicadoPncpIntegrationEvent`, `LicitacaoHomologadaIntegrationEvent` (**Administracao**) — transparência de contratos.
- `FolhaProcessadaIntegrationEvent` (**RecursosHumanos**) — remuneração de servidores.
- `ReceitaArrecadadaIntegrationEvent` (**Tributos**) — receita em tempo real.
- `BemIncorporadoIntegrationEvent`, `BemBaixadoIntegrationEvent` (**Patrimonio**) — bens públicos.

**Publica (notificação de estado):** `RemessaEnviadaTceIntegrationEvent`, `RemessaRejeitadaIntegrationEvent`, `PrazoRemessaVencidoIntegrationEvent` (alerta de risco de bloqueio LRF).

## 8. Cenários BDD
**Cenário 1 — Publicação em tempo real**
Dado o recebimento de `DespesaPagaIntegrationEvent` de **Financas**
Quando o evento é processado
Então um item é projetado em `PublicacaoTransparencia` até o 1º dia útil seguinte e **DadoAbertoPublicado** é registrado.

**Cenário 2 — Remessa com erro de validação**
Dada uma `RemessaTCE` no estado `Gerada`
Quando o e-Validador retorna o RDI com erros
Então a remessa transita para `Rejeitada`, **RemessaRejeitada** é publicado e o envio é bloqueado.

**Cenário 3 — Remessa válida enviada**
Dada uma `RemessaTCE` `Validada` com RDI sem erro e hash íntegro
Quando o Worker transmite ao SIAPC/PAD
Então a remessa vira `Enviada` e **RemessaEnviadaTCE** é publicado.

**Cenário 4 — Anonimização antes da publicação**
Dado um `RecursoAberto` contendo CPF de beneficiários
Quando a `PoliticaAnonimizacao` é aplicada
Então o CPF é mascarado, **DadoPessoalAnonimizado** é registrado e só então a base é publicada.

**Cenário 5 — Envio da MSC ao SICONFI**
Dado o recebimento de `MSCGeradaIntegrationEvent` de **Financas**
Quando a `DeclaracaoFiscal` consolida a `MatrizSaldos` do mês
Então a MSC é transmitida ao SICONFI e **MSCEnviadaSiconfi** é publicado.

**Cenário 6 — Prazo de remessa vencido**
Dada uma `RemessaTCE` não enviada após o prazo legal do `Periodo`
Quando o Worker verifica os vencimentos
Então **PrazoRemessaVencido** é publicado, alertando risco de bloqueio de transferências voluntárias (LRF).

## 8.1 Onda 2 — Portal Público + e-SIC + Dados Abertos (PROFUNDIDADE backend)

Superfície **PÚBLICA anônima** (`/publico/transparencia/{slug}`, fora de `/api/...`):
- **Transparência ativa (read models materializados de Integration Events JÁ publicados — I-13):** `PublicacaoDespesa`
  (empenho/liquidação/pagamento, de Finanças), `PublicacaoReceita` (RCL, de Finanças), `PublicacaoContrato`
  (contrato/PNCP/licitação, de Administração), `PublicacaoFolhaNominal` (derivada do `FolhaResumoRemessaTceIntegrationEvent`
  do RH — **sem CPF/matrícula**). Idempotentes por chave de origem.
- **Consulta em tempo real:** `GET /resumo-fiscal?exercicio` (receita × despesa por fase).
- **Dados abertos:** `GET /dados-abertos` (dicionário) + `GET /dados-abertos/{dataset}.csv?exercicio` (stream CSV: despesas,
  receitas, contratos, folha, diárias/repasses derivados).
- **e-SIC (transparência passiva, LAI art. 10-16):** `POST /esic` (abre — anônimo permitido), `GET /esic/{protocolo}`
  (status público **sem PII do solicitante**), `POST /esic/{protocolo}/recurso`.

Agregado **e-SIC** `PedidoInformacaoSic`: protocolo único por (tenant, ano); prazo **20 dias úteis CALCULADO** via
`ICalendarioDiasUteis` (parametrizável — nunca digitado); prorrogação **única, justificada, antes do vencimento** (+10
dias úteis); resposta/indeferimento sempre fundamentados; máquina de estados
`Aberto→EmAtendimento→(Respondido|Indeferido)→[RecursoAberto→RecursoRespondido]→Encerrado`.

Superfície **INTERNA** (autenticada, `/api/transparencia`): `esic` (lista; detalhe COM PII gera **trilha de acesso LG-3**
via `ISensivelLgpd`); transições atendimento/resposta/prorrogação/indeferimento/recurso (`transparencia.esic.responder`);
`portal-publico` (configura slug, `transparencia.gerenciar`).

**Resolução de tenant pública (sem JWT):** `ITenantPublicoResolver` resolve o **slug** contra o catálogo CENTRAL da
plataforma (deriva o slug do nome do ente — mesmo padrão de resolução por e-mail no login do Identidade), verifica a
**licença** do módulo (senão 404 sem revelar existência) e **fixa o `TenantOverride`** — daí o Global Query Filter por
TenantId volta a valer em TODA leitura subsequente (anti-vazamento cross-tenant).

**LGPD (público vs. minimizado):** público por lei = nome/cargo/lotação/remuneração de servidor, fornecedor/objeto/valor
de contrato, despesas/receitas. Minimizado = **CPF mascarado na origem** (`***.456.789-**`), **matrícula omitida**
(não existe no read model público), **PII do solicitante e-SIC nunca exposta a terceiros**.

> **Integração transversal (follow-up Onda 2 — CONCLUÍDA):** (1) policy de rate-limit dedicada `"publico"` (por IP,
> janela curta, fila pequena — mais restritiva que o GlobalLimiter) registrada no `AddRateLimiter` do ApiHost e
> encadeada via `.RequireRateLimiting("publico")` no grupo público, defendendo a superfície anônima contra DoS/scraping;
> (2) `transparencia.esic.ver`/`transparencia.esic.responder` no catálogo de permissões (módulo Identidade) e em
> `Permissoes.Todas` — o papel Administrador as recebe; deny-by-default preservado (o policy provider materializa a
> política dinamicamente e a permissão só vale onde concedida).

## 9. Fontes
- Lei 12.527/2011 (LAI) — https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2011/lei/l12527.htm
- LC 131/2009 — https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp131.htm
- TCE-RS (SIAPC/PAD, e-Validador) — https://portal.tce.rs.gov.br/
- SICONFI / MSC — https://siconfi.tesouro.gov.br/
- CGU / EBT — https://www.gov.br/cgu/
- LC 101/2000 (LRF), Decretos 7.185/2010 e 10.540/2020 (SIAFIC), LGPD (Lei 13.709/2018).

# Módulo Financas
> Gestão do orçamento público, execução da despesa e contabilidade aplicada ao setor público (PCASP). · Poder: Ambos · Schema EF Core: `financas` · Ativável por tenant.

## 1. Propósito & Marco Legal
O contexto **Financas** materializa o ciclo orçamentário-contábil do ente público: do planejamento (PPA/LDO/LOA) à execução da despesa em seus estágios legais e à escrituração contábil patrimonial. Cobre orçamento, execução da despesa e contabilidade pública. **Não trata NFS-e** (ingerida passivamente por **Tributos**, Worker `NfseSync`, ambiente ADN/Receita — ADR-0003).

- **Lei 4.320/1964**: estágios da despesa (empenho → liquidação → pagamento), tipos de empenho, restos a pagar, vedação de empenho sem saldo (art. 59).
- **LC 101/2000 (LRF)**: equilíbrio fiscal, vedação de despesa obrigatória de caráter continuado sem fonte de custeio, limites RREO/RGF.
- **CF arts. 165–169**: instrumentos de planejamento (PPA, LDO, LOA) e créditos adicionais.
- **MCASP + PCASP (STN)**: plano de contas e mecânica do lançamento contábil.
- **SICONFI / MSC**: Matriz de Saldos Contábeis e relatórios fiscais (RREO/RGF/DCA).

## 2. Linguagem Ubíqua
- **Empenho** — ato que cria a obrigação de pagamento, reservando dotação.
- **EmpenhoOrdinario** — empenho de valor exato e pagamento integral único.
- **EmpenhoEstimativo** — empenho de valor não exatamente quantificável (ex.: água, energia).
- **EmpenhoGlobal** — empenho de despesa contratual sujeita a parcelamento.
- **Liquidacao** — verificação do direito adquirido pelo credor (estágio 2).
- **Pagamento** — quitação da obrigação liquidada via ordem bancária (estágio 3).
- **RestoAPagarProcessado** — despesa empenhada e liquidada, não paga até 31/12.
- **RestoAPagarNaoProcessado** — despesa empenhada e não liquidada até 31/12.
- **DotacaoOrcamentaria** — limite de crédito autorizado pela LOA para uma classificação.
- **CreditoAdicionalSuplementar** — reforço de dotação existente.
- **CreditoAdicionalEspecial** — dotação para despesa sem crédito específico na LOA.
- **CreditoAdicionalExtraordinario** — crédito para despesas urgentes/imprevisíveis (calamidade, guerra).
- **PPA** — Plano Plurianual; diretrizes de médio prazo.
- **LDO** — Lei de Diretrizes Orçamentárias; metas e prioridades anuais.
- **LOA** — Lei Orçamentária Anual; estimativa de receita e fixação da despesa.
- **PCASP** — Plano de Contas Aplicado ao Setor Público.
- **MSC** — Matriz de Saldos Contábeis enviada ao SICONFI.
- **LancamentoContabil** — registro contábil por partidas dobradas no PCASP.
- **OrdemDePagamento** — documento que autoriza a saída de recurso financeiro.
- **Credor** — pessoa física/jurídica titular do direito ao recebimento.

## 3. Mapa de Domínio
- **ExercicioOrcamentario** [raiz · `IMustHaveTenant`]
  - Entidades: `DotacaoOrcamentaria`, `CreditoAdicional`.
  - VOs: `ClassificacaoFuncionalProgramatica`, `FonteRecurso`.
  - Evento: **DotacaoSuplementada**.
- **Empenho** [raiz · `IMustHaveTenant`]
  - Entidades: `Liquidacao`, `Pagamento`, `RestoAPagar`.
  - VOs: `Credor`, `ValorMonetario`, `TipoEmpenho`.
  - Eventos: **DespesaEmpenhada**, **DespesaLiquidada**, **DespesaPaga**, **RestoAPagarInscrito**.
- **LancamentoContabil** [raiz · `IMustHaveTenant`]
  - Entidade: `PartidaPCASP`.
  - VO: `ContaContabil`.
  - Evento: **MSCGerada**.
- **Retencao** [entidade-filha de `Liquidacao`] + **GuiaRecolhimento** [raiz · `IMustHaveTenant`]
  - VOs/tabela: `TabelaIrrfServicos`/`FaixaIrrfServicos` (IRRF/PJ — **IN RFB 1.234/2012**, alterada pela IN 2.145/2023; alíquotas 1,2%/0,24%/2,4%/4,8% e códigos DARF 6147/9060/6188/6175/6190; dispensa abaixo de R$ 10,00 — art. 3º, §6º).
  - Naturezas: IRRF (PJ/PF), INSS, ISS retido, contribuições federais, caução.
  - Ciclo **extra-orçamentário**: a retenção apurada na liquidação reduz o líquido a pagar e nasce como passivo a recolher (consignação — PCASP **2.1.8.8.1.xx**); o recolhimento (`GuiaRecolhimento`) baixa o passivo.
  - Eventos: **RetencaoApurada**, **GuiaRecolhimentoEmitida**, **RecolhimentoEfetuado**.
- **RemessaCnab240** [serviço de domínio — `Cnab240Writer`]
  - Geração do arquivo de remessa bancária **CNAB240 (FEBRABAN)** de pagamento a fornecedores/servidores a partir da `OrdemDePagamento` (Header de Arquivo/Lote, Segmentos A/B, Trailers; 240 posições). **Transmissão real ao banco = M10.**

Relações: `Empenho` referencia `DotacaoOrcamentaria` por Id (sem navegação cross-aggregate); cada estágio gera um `LancamentoContabil` por reação a evento de domínio.

## 4. Integrações & Padrões Técnicos
- **Clean Architecture + DDD**: agregados isolados, invariantes na raiz, sem vazamento de entidades entre módulos.
- **MediatR**: commands/queries por caso de uso (`EmpenharDespesaCommand`, `LiquidarEmpenhoCommand`).
- **EF Core 8**: schema `financas`; `ValueMonetario` como owned type; global query filter por `TenantId`.
- **Outbox**: Integration Events persistidos transacionalmente e publicados de forma assíncrona.
- **SICONFI**: Worker mensal serializa a **MSC** e a transmite; alimenta RREO/RGF/DCA.

## 5. Regras de Negócio Críticas
- Vedado empenhar sem saldo de dotação (art. 59, Lei 4.320) — invariante da raiz `Empenho`.
- Pagamento somente após liquidação regular: sequência empenho → liquidação → pagamento é obrigatória e imutável.
- Despesas não pagas até 31/12 viram restos a pagar: **processados** se liquidadas, **não processados** caso contrário.
- LRF veda criação de despesa obrigatória continuada sem indicação de fonte/custeio.
- Segregação de funções: quem empenha ≠ quem liquida ≠ quem paga.
- **Retenção na fonte (consignação)**: a soma das retenções não excede o valor liquidado; retenção só antes do pagamento. Contábil (patrimonial, balanceado, idempotente): no pagamento, D Caixa / C Consignação a Recolher (2.1.8.8.1.xx) pela parcela retida — o líquido sai do caixa e o retido fica como passivo extra-orçamentário; no recolhimento, D Consignação / C Caixa. O ciclo orçamentário (5/6) permanece pelo bruto. IRRF/PJ calculado pela tabela vigente da **IN RFB 1.234/2012** (parametrizável por tenant; sem número mágico).
- **SICAP (auditoria de pessoal TCE-RS)**: remessa de **pessoal**, alimentada pelo módulo **RecursosHumanos** (admissões/folha/atos). Por isolamento de módulo, a estrutura/transmissão pertence a RH/Transparência via `*.Contracts` — **fora do escopo de edição de Finanças**; pendente nesse módulo.

## 6. Multi-Tenancy, Segurança & Auditoria
Todas as raízes implementam `IMustHaveTenant`; `TenantId` aplicado por global query filter e nunca aceito do cliente. Operações sensíveis exigem claims distintas (`financas.empenhar`, `financas.liquidar`, `financas.pagar`) reforçando a segregação de funções. Trilha de auditoria imutável registra autor, timestamp e valores anteriores/novos de cada estágio; eventos de domínio versionados garantem rastreabilidade fiscal.

## 7. Contratos Públicos (Integration Events)
**Publica:** `DespesaEmpenhada`, `DespesaLiquidada`, `DespesaPaga` (consumidos por **Transparencia**); `MSCGerada` (SICONFI).
**Consome:** `ContratoAssinado` (**Administracao**) para lastrear o empenho; `BemIncorporado`/`BemBaixado` (**Patrimonio**) gerando lançamento contábil; `ReceitaArrecadada` (**Tributos**) para escrituração da arrecadação.

## 8. Cenários BDD
**Cenário 1 — Empenho sem saldo**
Dado uma `DotacaoOrcamentaria` com saldo R$ 0,00
Quando solicito empenhar R$ 1.000,00 nessa dotação
Então o empenho é rejeitado por violação do art. 59 e nenhum evento é publicado.

**Cenário 2 — Empenho válido**
Dado uma dotação com saldo R$ 5.000,00 e um `ContratoAssinado` recebido
Quando empenho R$ 2.000,00
Então o saldo cai para R$ 3.000,00 e **DespesaEmpenhada** é publicada.

**Cenário 3 — Pagamento sem liquidação**
Dado um `Empenho` sem `Liquidacao` registrada
Quando tento efetuar o `Pagamento`
Então a operação é bloqueada por quebra da sequência obrigatória de estágios.

**Cenário 4 — Inscrição de restos a pagar**
Dado um empenho liquidado e não pago em 31/12
Quando o exercício é encerrado
Então um `RestoAPagarProcessado` é inscrito e **RestoAPagarInscrito** é publicado.

**Cenário 5 — Incorporação patrimonial**
Dado o recebimento de `BemIncorporado` do módulo **Patrimonio**
Quando o evento é processado
Então um `LancamentoContabil` de variação patrimonial aumentativa é registrado no PCASP.

**Cenário 6 — Geração da MSC**
Dado o fechamento contábil do mês
Quando o Worker SICONFI consolida os saldos
Então a **MSC** é gerada, **MSCGerada** publicada e a matriz transmitida ao SICONFI.

## 9. Fontes
- Lei 4.320/1964 — https://www.planalto.gov.br/ccivil_03/leis/l4320.htm
- LC 101/2000 (LRF) — https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp101.htm
- MCASP / PCASP (Tesouro Nacional) — https://www.gov.br/tesouronacional/
- SICONFI / Matriz de Saldos Contábeis — https://siconfi.tesouro.gov.br/

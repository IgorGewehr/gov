# PARIDADE-POC — Lista priorizada e sequenciada (vs SAPI, incumbente de Maximiliano de Almeida/RS)

> Derivado de `GAP-VS-SYSTEM-SAPI.md`. Só o que é **CRÍTICO para o PoC** = uso diário do servidor **OU** obrigação de transmissão ao **TCE-RS**.
> Sequenciado por **valor × esforço × independência de credencial**. Separa o que é **construível JÁ** do que **defere M10** (transmissão/homologação).
> Esforço: P (≤2 dias) · M (~1 semana) · G (>1 semana: agregado + persistência + UI). Ancorado no código atual (verify-don't-trust).

---

## TRILHA A — CONSTRUÍVEL JÁ (sem credencial)

Sub-workflows em ordem de ataque. Cada um é independente o suficiente para virar uma frente de PoC.

### SW-A1 — Tesouraria Caixa-Banco (o maior buraco) · G
*Por quê:* coração operacional diário do financeiro do SAPI; hoje inexiste como agregado.
- A1.1 Agregado **ContaBancaria/Caixa** (saldo, situação) — hoje só VO em `OrdemDePagamento`.
- A1.2 **Movimento de Caixa** diário (entradas/saídas, saldo por conta/dia).
- A1.3 **Transferência entre contas/caixas** (operação auditada).
- A1.4 **Boletim de Caixa / Receita / Despesa** (relatório de fechamento diário).
- A1.5 Vincular arrecadação (`ReceitaArrecadada`) e pagamento (`OrdemDePagamento.Efetuar`) ao movimento de caixa.

### SW-A2 — Razão analítico + Diário cronológico · M
*Por quê:* cobrança direta do TCE e da rotina contábil; hoje `ConsultarRazaoQuery` só devolve saldos mensais agregados (`BalanceteProjection.cs:82`).
- A2.1 **Razão analítico**: extrato lançamento-a-lançamento por conta (read model novo).
- A2.2 **Diário**: query/endpoint cronológico de lançamentos (grep `Diario`=0 hoje).
- A2.3 (opc.) Termos de abertura/encerramento de livro Diário/Razão — P.

### SW-A3 — Certidão Negativa de Débitos (CND/CPEN) · M
*Por quê:* serviço online de altíssimo uso, exigido em licitações/contratos; temos CDA (cobrança) mas não a certidão ao contribuinte (grep `certidao negativa`=0).
- A3.1 Emissão CND/Positiva-com-efeito-Negativa por contribuinte/inscrição (verificação de débitos abertos + dívida ativa).
- A3.2 Endpoint administrativo + autosserviço no Portal Cidadão (`CidadaoEndpoints`).
- A3.3 Validação por código/hash (autenticidade da certidão).

### SW-A4 — Retenção IRRF + extra-orçamentário no pagamento · M→G
*Por quê:* obrigação de recolhimento; afeta todo pagamento; `Liquidacao.cs` registra valor cheio (grep `retenc/irrf`=0).
- A4.1 Retenções na liquidação/pagamento (IRRF, INSS, ISS-retido) → bruto/líquido.
- A4.2 Ciclo **extra-orçamentário** (consignações/cauções/retenções como passivo a recolher).
- A4.3 Lançamento contábil das retenções (roteiros no `MotorContabil`).

### SW-A5 — Cadastro de Credores + Extrato do Credor · M
*Por quê:* hoje `Credor` é só VO embutido no empenho; sem cadastro não há extrato consolidado por credor.
- A5.1 Agregado **Credor** (CRUD, dados bancários, situação, histórico).
- A5.2 **Extrato do credor** (empenhos/liquidações/pagamentos consolidados).
- A5.3 Migrar empenho para referenciar o credor cadastrado (sem quebrar VO existente).

### SW-A6 — Pagamento em meio magnético (CNAB240) · M
*Por quê:* uso diário de pagamento em lote ao banco.
- A6.1 Geração de arquivo de remessa **CNAB240** a partir de `OrdemDePagamento`.
- A6.2 (gancho p/ M10) import de retorno bancário → baixa por compensação.

### SW-A7 — RH: Portarias / atos de pessoal · G
*Por quê:* emissão diária de nomeação/exoneração/concessões; hoje "portaria" é só string (`RegistrarAfastamento.cs:21`).
- A7.1 Agregado **Portaria/AtoDePessoal** (numeração, tipo, ementa, vínculo ao servidor).
- A7.2 Emissão e efeito no vínculo (nomeação/exoneração/concessão de licença).
- A7.3 Template/PDF + assinatura (reusar Protocolo).

### SW-A8 — RH: PASEP + Reajuste de salários em lote · M (cada)
*Por quê:* PASEP é recolhimento recorrente do ente; reajuste em lote é a revisão geral anual (hoje só `AlterarVencimento` individual por cargo).
- A8.1 **PASEP**: base + apuração + rubrica de recolhimento.
- A8.2 **Reajuste em lote**: aplicação linear/percentual sobre vencimentos (por cargo/faixa/geral), com auditoria.

### SW-A9 — Compras: largura de contratação · G
*Por quê:* área onde o incumbente tem mais largura; vários itens 🔴.
- A9.1 **Catálogo Produto/Serviço (CATMAT/CATSER)** — itens estruturados, não strings.
- A9.2 **Dispensa/Inexigibilidade** com fluxo próprio (art. 75/74) — hoje só enum.
- A9.3 **Registro de Preços + Ata** (`AtaRegistro` atual é falso-positivo de `DataRegistro`).
- A9.4 **PCA — Plano de Contratações Anual** + limites de licitação.
- A9.5 **Import CEIS/CNEP** (sancionados; dataset aberto, sem credencial).

### SW-A10 — Tributos: GIA Mensal de ISSQN + importação · G
*Por quê:* declaração obrigatória do prestador/substituto; hoje apuração é 100% derivada do ADN (`ApuracaoIss.cs:23`), deixando de fora serviços sem NFS-e nacional.
- A10.1 Declaração mensal do contribuinte (GIA) com escrituração de serviços prestados/tomados.
- A10.2 Importação de arquivo por contribuinte/substituto.
- A10.3 Apuração/lançamento a partir da GIA (além do ADN).

### SW-A11 — Transparência: Demonstrativo LRF (RREO/RGF) · G
*Por quê:* publicação obrigatória; hoje RCL é entrada **manual** (`RegistrarReceitaCorrenteLiquida.cs:16`), sem apuração/geração.
- A11.1 Apuração automática de RCL a partir das receitas realizadas.
- A11.2 Geração dos anexos **RREO** (bimestral) e **RGF** (quadrimestral).
- A11.3 Publicação no portal público.

---

## TRILHA B — DEFERE M10 (depende de credencial/homologação para transmitir/validar)

Modelar/gerar **agora**; transmissão real só com cred. Sequenciar dentro do M10 (ver `M10-CREDENCIAIS-ONBOARDING.md`).

| # | Item | Construir agora | Trava M10 |
|---|---|---|---|
| B1 | **Conciliação bancária** | motor de conciliação + casamento | import extrato OFX/CNAB do banco |
| B2 | **SICAP (TCE-RS)** — remessa de pessoal | modelagem da remessa | layout/homologação TCE-RS |
| B3 | **LICITACON (TCE-RS)** — remessa de compras | modelagem | layout/homologação TCE-RS |
| B4 | **eSocial SST** (S-2210/2220/2240) + **S-2230** | geradores de evento + entidade CAT | homologação eSocial produção |
| B5 | **PNCP real** | trocar `PncpGatewaySimulado.cs` | credencial PNCP |
| B6 | **Login gov.br** | já há stub e ponto de integração | credencial gov.br (M10) |
| B7 | **Conveniados IPERGS** / **retorno consignação bancária** | geradores | layouts de terceiros |
| B8 | **Validação DCASP** (`MapaDemonstrativosCatalogo.cs:7 [validar-oficial]`) + transmissão **MSC/SICONFI/SIAPC** | conferência linha-a-linha MCASP | certificado A1 + portais oficiais |

---

## SEQUÊNCIA RECOMENDADA (valor × esforço)

1. **SW-A2 (Razão/Diário, M)** e **SW-A3 (CND, M)** primeiro — médio esforço, paridade-TCE e balcão de alto uso, destravam percepção rápido.
2. **SW-A1 (Tesouraria Caixa-Banco, G)** — maior buraco; começar em paralelo pois é o de maior valor estrutural (e habilita B1 conciliação).
3. **SW-A4 (IRRF/extra-orç.) + SW-A5 (Credores) + SW-A6 (CNAB)** — fecham o ciclo financeiro diário.
4. **SW-A7/A8 (RH: Portarias, PASEP, reajuste-lote)** — paridade RH de uso diário.
5. **SW-A10 (GIA) + SW-A11 (RREO/RGF) + SW-A9 (Compras largura)** — os G's de maior escopo, ao final da trilha construível.
6. **Trilha B** — modelar embutido em cada SW correspondente; ativar transmissão no M10.

> Regra: nenhum item da Trilha A espera o M10. Cada SW da Trilha B é "modelado na Trilha A, transmitido no M10".

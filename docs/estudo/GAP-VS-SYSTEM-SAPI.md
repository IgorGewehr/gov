# GAP-ANALYSIS — Tensorroot.Gov vs SAPI (incumbente de Maximiliano de Almeida/RS)

> Consolidação das 4 gap-analyses por cluster (Fiscal/Contábil/Tesouraria · RH/Folha/SST · Compras/Patrimônio/Frota/Protocolo · Tributos/Serviços-Online/Cidadão).
> Engenharia-chefe / análise competitiva. Método **verify-don't-trust**: cada `✅/🟡/🔴` ancorado em `arquivo:linha` do código atual — nada inventado; onde marcamos 🔴, buscamos e **não achamos**.
> SAPI = sistema da incumbente (Maximiliano de Almeida usa hoje). Critério de "paridade" = o que a prefeitura **usa no dia a dia** + o que **o TCE-RS obriga a transmitir**, não a lista total de features do concorrente.

Legenda — **Esforço:** P (≤2 dias) · M (~1 semana) · G (>1 semana: novo agregado + persistência + UI).
**Criticidade-PoC:** CRÍTICO (uso diário OU obrigação de transmissão) · DESEJÁVEL (atendimento/conforto) · PÓS-POC (nicho/baixo volume).

---

## 1. VEREDITO — quão perto estamos da paridade com o que a prefeitura USA hoje

**Estamos perto da paridade na espinha fiscal/compliance profunda e longe na largura operacional do balcão e — sobretudo — no bloco de GERAÇÕES (remessas legais).** Onde o SAPI é um motor de cálculo legal (PCASP, ciclo da despesa, folha, IPTU/ISS/ITBI/Dívida Ativa com decadência/prescrição/CDA, eSocial periódico, MSC, SIAPC/PAD), nós **temos paridade real e às vezes profundidade superior**. Mas o SAPI se vende exatamente pelas **dezenas de transmissões e pelo uso diário de balcão/tesouraria** — e é aí que sangramos: **não existe tesouraria caixa-banco com conciliação bancária** (o coração operacional diário do financeiro), **não há SICAP** (remessa de pessoal ao TCE-RS), **nem Portarias/PASEP/reajuste-em-lote** no RH, **nem Certidão Negativa de Débitos nem GIA de ISS** no balcão tributário, **nem catálogo de itens/Registro de Preços/PNCP-real/LICITACON** em compras, e o **Razão/Diário contábil é agregado, não analítico**. Resumindo honestamente para o dono: **somos competitivos no que o Tribunal cobra na prestação anual e no que é cálculo legal; ficamos atrás no que o servidor abre todo dia (caixa/banco, guias avulsas, certidões, atos de pessoal) e em metade das remessas mensais.** Nenhum dos gaps CRÍTICOS de maior valor depende de credencial para ser **construído** — credencial só trava a **transmissão real** (defere M10). Dá para fechar a distância de paridade-PoC com engenharia, sem esperar homologação.

---

## 2. CONTAGEM DE GAPS POR CRITICIDADE

Universo: ~90 funcionalidades do SAPI mapeadas nos 4 clusters.

| Status | CRÍTICO | DESEJÁVEL | PÓS-POC | Total |
|---|---:|---:|---:|---:|
| ✅ TEMOS (paridade) | 22 | 4 | 0 | **26** |
| 🟡 PARCIAL | 7 | 9 | 0 | **16** |
| 🔴 FALTA | 13 | 22 | 14 | **49** |
| **Total por criticidade** | **42** | **35** | **14** | **~91** |

Leitura: dos **42 itens CRÍTICOS**, **22 já têm paridade ✅**, **7 estão 🟡 parciais** e **13 estão 🔴 ausentes**. Os 13 ausentes-críticos + 7 parciais-críticos = **20 frentes** que separam-nos da paridade-PoC. Dessas, ~4 dependem de credencial só para a transmissão final (defere M10); o resto é construível já.

---

## 3. TABELA MESTRA DE GAPS POR ÁREA

> Detalhe completo por cluster nos docs-fonte: `gap-incumbente-sapi/COMPRAS-PATRIMONIO-FROTA-PROTOCOLO.md` e o corpo desta consolidação. Abaixo, a visão mestra (CRÍTICOS e PARCIAIS em destaque; PÓS-POC sumarizados ao final de cada bloco).

### 3.1 FISCAL / CONTÁBIL

| Funcionalidade SAPI | Status | Arquivo:linha | Crit-PoC | Esf | Cred? |
|---|---|---|---|---|---|
| Plano de Contas PCASP | ✅ | `Financas.Domain/Contabilidade/PlanoDeContas/ContaContabil.cs`; seed `PlanoDeContasCatalogo.cs`; `FinancasEndpoints.cs:58` | CRÍTICO | — | Não |
| Lançamentos manuais | ✅ | `Application/Contabilidade/Commands/RegistrarLancamentoManual.cs`; `FinancasEndpoints.cs:66` | CRÍTICO | — | Não |
| Lançamentos automáticos por roteiro | ✅ | `Application/Contabilidade/Handlers/Contabilizar*.cs`; motor `Motor/MotorContabil.cs`; `Seed/RoteirosCatalogo.cs` | CRÍTICO | — | Não |
| Balancete contábil | ✅ | `Queries/ConsultarBalancete.cs:31`; `ReadModels/BalanceteProjection.cs`; `FinancasEndpoints.cs:71` | CRÍTICO | — | Não |
| Encerramento de exercício | ✅ | `Domain/Contabilidade/Encerramento/EncerramentoExercicio.cs`; `MotorEncerramento.cs`; `FinancasEndpoints.cs:90` | CRÍTICO | — | Não |
| MSC (Matriz Saldos Contábeis/SICONFI) | ✅ | `Domain/Contabilidade/Msc/MatrizSaldosContabeis.cs`; `Msc/GerarMsc.cs`; `FinancasEndpoints.cs:132` | CRÍTICO | — | M10 (transmissão) |
| SIAPC/PAD (remessa TCE-RS) | ✅ | `Transparencia/.../RemessasTce/` (Leiautes/LeiauteSiapc, EmissorRegistroSiapc); `GerarRemessaTce.cs`; `TransparenciaEndpoints.cs:147` | CRÍTICO | — | M10 (transmissão) |
| DCASP (demonstrações) | ✅ | `Application/Contabilidade/Demonstracoes/`; `FinancasEndpoints.cs:139`. ⚠️ `MapaDemonstrativosCatalogo.cs:7` `[validar-oficial]` | CRÍTICO | — | M10 (validação) |
| **Razão analítico** (extrato lançamento-a-lançamento) | 🟡 | `ConsultarRazaoQuery` em `ConsultarBalancete.cs:36` retorna saldos mensais agregados (`BalanceteProjection.cs:82`), NÃO extrato individual | CRÍTICO | M | Não |
| **Diário** (livro cronológico) | 🔴 | grep `Diario`=0; nenhuma query lista lançamentos em ordem cronológica | CRÍTICO | M | Não |
| **Movimentação de contas bancárias/caixa** | 🔴 | `ContaBancaria` é só VO no pagamento; sem agregado com extrato. grep `Movimentac.*Conta`=0 | CRÍTICO | M | Não |
| **Conciliação bancária** | 🔴 | grep `conciliac` (bancária)=0 no Financas | CRÍTICO | G | M10 (import OFX/extrato) |
| Lançamentos em lote | 🔴 | `RegistrarLancamentoManual.cs` registra 1 por chamada | DESEJÁVEL | P | Não |
| Balancete receita/despesa por rubrica | 🟡 | demonstrativos em `MapaDemonstrativosCatalogo.cs:18`, sem balancete dedicado formato SAPI | DESEJÁVEL | M | Não |
| Termos abertura/encerramento de livro | 🔴 | há encerramento de exercício, não termos de livro Diário/Razão p/ TCE | DESEJÁVEL | P | Não |
| ~~SINCO, SIGA~~ → **OBSOLETO/NÃO-TCE-RS** (ver `normas/CONFORMIDADE-ACHADOS.md` §4.4) | ⚪ | SINCO=coleta contábil RFB legada (superada por SPED-ECD; setor público=MSC/SICONFI, já temos); SIGA=acrônimo ambíguo, sem geração contábil vigente do TCE-RS | N/A | — | — |
| Auditoria Previdência RPPS (geração) | 🔴 | grep `gerador previdenciário`=0 | PÓS-POC | M–G | parcial |

### 3.2 FINANCEIRO / TESOURARIA

| Funcionalidade SAPI | Status | Arquivo:linha | Crit-PoC | Esf | Cred? |
|---|---|---|---|---|---|
| Pagamentos (tesouraria) | ✅ | `Domain/Pagamentos/OrdemDePagamento.cs` (Emitir/Efetuar/Cancelar); `FinancasEndpoints.cs:241` | CRÍTICO | — | Não |
| **Cadastro de Credores** (agregado CRUD) | 🟡 | `Credor` é só VO no empenho (`ValueObjects/Credor.cs`); sem agregado/histórico/dados bancários | CRÍTICO | M | Não |
| **Extrato do Credor** | 🔴 | sem cadastro de credor não há extrato consolidado por credor | CRÍTICO | M | Não |
| **Movimento de Caixa** (agregado caixa, saldo/dia) | 🔴 | grep=0; sem agregado Caixa | CRÍTICO | M | Não |
| **Transferência entre caixas/contas** | 🔴 | sem operação de transferência | CRÍTICO | M | Não |
| **Conciliação bancária** (tesouraria) | 🔴 | idem contábil — função diária crítica ausente | CRÍTICO | G | M10 (extrato banco) |
| **Boletim de Caixa/Receita/Despesa** | 🔴 | grep `boletim`=0 | CRÍTICO | M | Não |
| **Pagamento em meio magnético (CNAB240)** | 🔴 | `OrdemDePagamento` não gera remessa bancária; grep `meio.?magnet`=0 | CRÍTICO | M | Não |
| **Retenção IRRF / extra-orçamentário** no pagamento | 🔴 | `Liquidacao.cs` sem retenção/bruto-líquido; grep `retenc/irrf`=0; ciclo só orçamentário | CRÍTICO | M–G | Não |
| Recebimentos (caixa/arrecadação) | 🟡 | `Domain/Receitas/ReceitaArrecadada.cs` + `RegistrarReceitaArrecadadaHandler.cs`; sem movimento de caixa por conta | CRÍTICO | M | Não |
| Baixa de pagamentos por compensação | 🟡 | `OrdemDePagamento.Efetuar()` quita (`:166`); sem baixa por conciliação/cheque compensado | DESEJÁVEL | M | Não |
| Transferências/repasses concedidos + emendas | 🟡 | `Convenios/` cobre recebidos + MROSC; sem concedidos genéricos nem emendas | DESEJÁVEL | M | Não |
| PÓS-POC: Convênios bancários, talões/cheques avulsos, custos administrativos | 🔴 | grep=0 | PÓS-POC/DESEJ | P–M | Não |

### 3.3 CONTAS PÚBLICAS / TRANSPARÊNCIA

| Funcionalidade SAPI | Status | Arquivo:linha | Crit-PoC | Esf | Cred? |
|---|---|---|---|---|---|
| Receita/Despesa/Contratos realizados (portal) | ✅ | `PortalPublico/ConsultasPublicas.cs:26,51,70`; `TransparenciaPublicaEndpoints.cs:47` | CRÍTICO | — | Não |
| e-SIC (pedidos LAI) | ✅ | `Esic/PedidoInformacaoSic.cs`; `TransparenciaEndpoints.cs:45` + portal `:147` | DESEJÁVEL | — | Não |
| Dados Abertos (LAI) | ✅ | `PortalPublico/DadosAbertos.cs`; `TransparenciaPublicaEndpoints.cs:116` | DESEJÁVEL | — | Não |
| Prestação de Contas Terceiro Setor (MROSC) | ✅ | `Convenios/.../Mrosc/` (ParceriaOsc, PrestacaoContasOsc); fluxo `/prestacao/*`. Publicação portal: 🟡 | CRÍTICO | — (port. P) | Não |
| **Demonstrativo LRF (RREO/RGF)** | 🔴 | infra de metas AMF/ARF existe; RCL é entrada **manual** (`RegistrarReceitaCorrenteLiquida.cs:16`); **não apura/gera RREO/RGF** | CRÍTICO | G | Não |
| Demonstrações/Balanços/LOA publicados no portal | 🟡 | gerados em Financas, não publicados; só `ConsultarResumoFiscalPublico` (`ConsultasPublicas.cs:111`) | DESEJÁVEL | M | Não |
| Aditivos/Tributos arrecadados/Compras no portal | 🟡/🔴 | aditivos não expostos; sem dataset tributos por contribuinte; sem compras públicas | DESEJÁVEL | P–M | Não |

### 3.4 RH / FOLHA

| Funcionalidade SAPI | Status | Arquivo:linha | Crit-PoC | Esf | Cred? |
|---|---|---|---|---|---|
| Ciclo de folha (abrir→calcular→conferir→fechar→pagar) | ✅ | `Application/Folha/*`; motor `Domain/Calculo/MotorDeCalculoFolha.cs` | CRÍTICO | — | Não |
| Eventos manuais / rubricas | ✅ | `Folha/AdicionarEvento.cs:16`; `RecursosHumanosEndpoints.cs:257` | CRÍTICO | — | Não |
| Férias / 13º / Rescisões | ✅ | `CicloAnual/GerarFerias.cs`, `GerarDecimoTerceiro.cs:93`, `GerarVerbasRescisorias.cs:13`; `CompositorRescisao.cs` | CRÍTICO | — | Não |
| Cargos e salários (plano/vagas/extinção) | ✅ | `Domain/Cargos/PlanoDeCargos.cs`; `App/Cargos/*`; `Endpoints:414` | CRÍTICO | — | Não |
| Ponto eletrônico (REP/AFD/AEJ/banco-horas) | ✅ | `App/Ponto/Coleta/ColetarRep.cs`, `ImportarAfd.cs`; `Domain/Ponto/ParserAfd.cs`; `Endpoints:230` | CRÍTICO | — | driver fabricante (defere) |
| eSocial periódico + admissão/desligamento | 🟡 | gera S-1000/1005/1010/2200/2299/1200/1202/1210/1299 (`ESocial/Mapeamento/GeradorEventosESocial.cs:7`); faltam **S-2230/2206/2306**; XSD `TODO(validar-oficial)` | CRÍTICO | M | M10 (homologação) |
| **SICAP (auditoria pessoal TCE-RS)** | 🔴 | grep `sicap`=0; só `ObterDemonstrativoTce.cs` (relatório, não remessa) | CRÍTICO | G | M10 (layout TCE-RS) |
| **Portarias / atos de pessoal** | 🔴 | grep `EmitirPortaria`=0; afastamento referencia "portaria" como string (`RegistrarAfastamento.cs:21`), não emite | CRÍTICO | G | Não |
| **PASEP** (folha/recolhimento do ente) | 🔴 | grep `pasep`=0 | CRÍTICO | M | Não |
| **Reajuste de salários em lote** | 🔴 | grep `reajuste`=0; só `AlterarVencimento.cs` individual por cargo | CRÍTICO | M | Não |
| **eSocial SST (S-2210/2220/2240) + CAT** | 🔴 | roster do enum não inclui 2210/2220/2240; só `TipoAfastamento.AcidenteTrabalho=5` (`Afastamentos/Enums.cs:23`) | CRÍTICO | G | M10 (homologação) |
| Consignação bancária (arquivo retorno) | 🟡 | módulo consignação robusto (`App/Consignacoes/*`, margem Lei 14.131); falta import retorno consignatárias | DESEJÁVEL | M | layout banco |
| Conveniados IPERGS (RPPS-RS) | 🔴 | `ipergs`=0; há `TabelaRpps` mas sem geração do arquivo | DESEJÁVEL | M | layout IPERGS |
| Médias variáveis / banco de horas (gestão) / licença-prêmio (aquisição) | 🟡 | cálculo recebe médias como entrada do operador; saldo banco-horas existe mas sem gestão; licença-prêmio sem aquisição por quinquênio | DESEJÁVEL | M | Não |
| Adiantamento salarial mensal / RPA / rescisão complementar | 🔴 | só adiantamento de 13º; grep `rpa/autonomo`=0 | DESEJÁVEL | M | Não |
| PÓS-POC: cálculo atuarial, prev. complementar, proc. trabalhistas, PCMSO/PPP, Lei 173, NIS-lote, SEFIP/GFIP/RAIS/DIRF/CAGED/seguro-desemprego, MANAD | 🔴 | grep=0 (vários substituídos por eSocial/DCTFWeb) | PÓS-POC/DESEJ | M–G | parcial |

### 3.5 COMPRAS / LICITAÇÕES / PATRIMÔNIO / FROTA / PROTOCOLO

> Fonte detalhada: `gap-incumbente-sapi/COMPRAS-PATRIMONIO-FROTA-PROTOCOLO.md`.

| Funcionalidade SAPI | Status | Arquivo:linha | Crit-PoC | Esf | Cred? |
|---|---|---|---|---|---|
| Fornecedor + Instrumento contratual (aditivo/apostila/garantia/rescisão) | ✅ | `Administracao` (Fornecedor, Contrato) | CRÍTICO | — | Não |
| Protocolo (NUP/tramitação/despacho/arquivamento + arquivística + assinatura) | ✅ | módulo mais maduro; `Processo.cs` | CRÍTICO | — | Não |
| Patrimônio — Bem + Inventário c/ comissão+divergências | ✅ | `Bens/BemPatrimonial.cs:25`; `Inventarios/Inventario.cs:28` | CRÍTICO | — | Não |
| Frota — Veículo/Motorista (CNH+validade)/Multa | ✅ | núcleo presente | CRÍTICO | — | Não |
| Licitação (lote/proposta/habilitação/recurso) | 🟡 | aggregate existe, só 5 modalidades; sem comissão de licitação | CRÍTICO | M | Não |
| **Catálogo Produto/Serviço (CATMAT/CATSER)** | 🔴 | ausente; itens são strings | CRÍTICO | G | Não |
| **Processos de compra (fase interna) + PCA** | 🔴 | sem fase interna; sem Plano de Contratações Anual | CRÍTICO | G | Não |
| **Dispensa/Inexigibilidade (fluxo próprio)** | 🟡 | só enum, sem fluxo art. 75 | CRÍTICO | M | Não |
| **Registro de Preços + Ata** | 🔴 | `AtaRegistro` no grep é falso-positivo de `DataRegistro` | CRÍTICO | G | Não |
| **PNCP (envio real)** | 🟡 | arquitetura existe mas STUB (`PncpGatewaySimulado.cs`) | CRÍTICO | M | M10 (credencial PNCP) |
| **LICITACON (TCE-RS)** | 🔴 | grep=0 | CRÍTICO | G | M10 (layout TCE-RS) |
| Consulta sancionados CEIS/CNEP | 🔴 | sanção interna existe, sem import CEIS/CNEP | DESEJÁVEL | M | Não (dataset aberto) |
| Limites de licitação / Credenciamento (art.79) / Chamada pública | 🔴 | `Credenciamento` no código é cadastro de fornecedor, não processo | DESEJÁVEL | M | Não |
| Frota — manutenção preventiva/periódica; média consumo; alerta CNH; apólice seguro; reserva/viagem; custo km | 🟡/🔴 | abastecimento sem relatório; licenciamento só IPVA | DESEJÁVEL | M | Não |
| Protocolo — recebimento explícito + apensamento/juntada | 🔴 | sem `Apensar/Juntar/Receber` em `Processo.cs` | DESEJÁVEL | P | Não |
| Patrimônio — listagem/busca navegável; termo responsabilidade; QR tombamento | 🔴 | consulta só por ID | DESEJÁVEL | M | Não |
| MD-e (Manifestação do Destinatário) | 🔴 | sem domínio NF-e/DF-e | DESEJÁVEL | G | M10 (certificado) |
| PÓS-POC: Pneu/reposicionamento, DPVAT, Banco de Preços/BLL/BPS | 🔴 | — | PÓS-POC | M | parcial |

### 3.6 TRIBUTOS / SERVIÇOS-ONLINE / CIDADÃO

| Funcionalidade SAPI | Status | Arquivo:linha | Crit-PoC | Esf | Cred? |
|---|---|---|---|---|---|
| ISSQN (apuração/lançamento) | ✅ | `Domain/Iss/ApuracaoIss.cs:29`; `ApurarIssMensal.cs`; `IssItbiEndpoints.cs:37` | CRÍTICO | — | Não |
| DAM IPTU por imóvel | ✅ | `IptuEndpoints.cs:52`; `LancarIptuAnual.cs`; `Domain/Arrecadacao/Dam.cs:77` | CRÍTICO | — | Não |
| Alvará Localização e Funcionamento | ✅ | `Domain/Alvaras/Alvara.cs:25`; `TaxasCosipAlvaraMelhoriaEndpoints.cs:50` | CRÍTICO | — | Não |
| DAMs em aberto + 2ª via (autosserviço cidadão) | ✅ | `PortalCidadao/ConsultaTributariaCidadao.cs:24`; `CidadaoEndpoints.cs:109` | CRÍTICO | — | Não |
| **Certidão Negativa de Débitos (CND/CPEN)** | 🟡 | temos CDA (cobrança, `Dividas/CertidaoDividaAtiva.cs`); **não** CND ao contribuinte; grep `certidao negativa`=0 | CRÍTICO | M | Não |
| **GIA Mensal de ISSQN + importação** | 🔴 | apuração 100% derivada do ADN (`ApuracaoIss.cs:23`); sem declaração do contribuinte/substituto; grep `gia`=0 | CRÍTICO | G | Não |
| Login gov.br (cidadão) | 🟡 | login local CPF/CNPJ pronto (`CidadaoEndpoints.cs:67`); gov.br stub 501 (`:96 TODO(M10-creds)`) | CRÍTICO | M | M10 |
| DAM avulso receitas diversas / DAM consolidação / DAM por inscrição municipal | 🔴 | `Dam` exige `LancamentoId`; sem guia avulsa/consolidada/por inscrição | DESEJÁVEL | M | Não |
| Alvará VISA (fluxo real, classes de risco) ligado a Saúde↔Tributos | 🟡 | enum `EspecieAlvara.Sanitario` (`Alvara.cs:30`) emite genérico; VISA em Saúde (`AutoVisa.cs`) não emite alvará | DESEJÁVEL | M | Não |
| DEC (Domicílio Eletrônico do Contribuinte) | 🔴 | sem caixa postal/intimação eletrônica; grep=0 | DESEJÁVEL→CRÍTICO | G | Não |
| PÓS-POC: DES-IF, CFS-e, Solicitação uso NFS-e, Título S.I.M. | 🔴 | grep=0 | PÓS-POC | G | parcial |

---

## 4. PARIDADE-POC — LISTA PRIORIZADA (resumo; detalhe sequenciado em `PARIDADE-POC.md`)

O que separa a paridade de uso-diário + obrigação-TCE-RS, sequenciado por **valor × esforço × independência de credencial**.

### Construível JÁ (sem credencial) — ordem de ataque
1. **Tesouraria Caixa-Banco** (agregado Conta/Caixa + Movimento + Transferência + Boletim de Caixa) — G — *o maior buraco; coração diário do financeiro*.
2. **Razão analítico + Diário cronológico** contábeis reais — M — *cobrança direta do TCE e da rotina contábil*.
3. **Certidão Negativa de Débitos (CND/CPEN)** ao cidadão — M — *serviço online de altíssimo uso; exigida em licitações/contratos*.
4. **Portarias / atos de pessoal** (nomeação/exoneração/concessões) — G — *emissão diária no RH*.
5. **Retenção IRRF + extra-orçamentário** no pagamento — M→G — *obrigação de recolhimento; afeta todo pagamento*.
6. **Cadastro de Credores + Extrato do Credor** — M — *destrava extrato/relatório por credor*.
7. **Pagamento em meio magnético (CNAB240)** — M — *uso diário de pagamento*.
8. **PASEP** (folha/recolhimento do ente) — M — *recorrente*.
9. **Reajuste de salários em lote** — M — *revisão geral anual; hoje só individual*.
10. **Catálogo de itens (CATMAT/CATSER) + Dispensa/Inexigibilidade (fluxo) + Registro de Preços/Ata + PCA** — G — *largura de compras onde o incumbente domina*.
11. **GIA Mensal de ISSQN + importação** — G — *declaração obrigatória do prestador/substituto; cobre serviços sem NFS-e nacional*.
12. **Demonstrativo LRF (RREO/RGF)** apurado/gerado — G — *publicação obrigatória; hoje RCL é manual*.
13. **Import CEIS/CNEP** (sancionados) — M — *dataset aberto, sem credencial*.

### Defere M10 (depende de credencial/homologação para a TRANSMISSÃO/validação)
- **Conciliação bancária** — construir o motor já; o import de extrato OFX/CNAB depende de banco.
- **SICAP (TCE-RS)** e **LICITACON (TCE-RS)** — modelar a remessa; layout/homologação no M10.
- **eSocial SST (S-2210/2220/2240) + S-2230** — gerar os eventos; transmissão em produção no M10.
- **PNCP real** (trocar `PncpGatewaySimulado.cs`) — credencial PNCP.
- **Login gov.br** — ponto de integração já há stub.
- **Conveniados IPERGS** / **retorno consignação bancária** — layouts de terceiros.
- **Validação oficial DCASP** (`MapaDemonstrativosCatalogo.cs:7`) e **transmissão MSC/SICONFI/SIAPC**.

---

## 5. PARIDADE JÁ CONQUISTADA (não regredir)

PCASP + lançamentos manuais/automáticos por roteiro · Balancete · Encerramento de exercício · MSC · SIAPC/PAD (geração+validação+empacotamento) · DCASP · ciclo da despesa (Empenho→Liquidação→Pagamento→RAP) · ciclo de folha completo + férias/13º/rescisão/cargos · ponto eletrônico · eSocial periódico+admissão/desligamento · ISSQN/IPTU/DAM/Alvará/Dívida-Ativa-CDA com profundidade legal (Tema 1.113, decadência CTN, LEF art.2º) · autosserviço cidadão (débitos/2ª-via) · portal de transparência (despesas/receitas/contratos/e-SIC/dados abertos) · MROSC (terceiro setor) · Protocolo maduro · Patrimônio (Bem+Inventário) · Frota núcleo.

---

## 6. DOCS-FONTE

- Cluster Compras/Patrimônio/Frota/Protocolo: `docs/estudo/gap-incumbente-sapi/COMPRAS-PATRIMONIO-FROTA-PROTOCOLO.md`
- Lista priorizada/sequenciada de paridade-PoC: `docs/estudo/PARIDADE-POC.md`
- Plano de profundidade (largura operacional): `docs/estudo/completude-modulos/PLANO-PROFUNDIDADE.md` (§7 incorpora estes gaps)

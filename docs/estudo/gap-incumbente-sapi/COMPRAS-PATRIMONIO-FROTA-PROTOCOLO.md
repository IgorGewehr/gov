# Gap-analysis vs incumbente SAPI — cluster COMPRAS / PATRIMÔNIO / FROTA / PROTOCOLO / MD-e

Incumbente: **SAPI / System Soluções em Sistemas de Informação** (prefeitura de Maximiliano de Almeida — RS).
Método: verify-don't-trust. Para cada funcionalidade do incumbente, busca no nosso código (`src/Modules/*`) e classificação ✅ TEMOS / 🟡 PARCIAL / 🔴 FALTA, com arquivo:linha. Estado ancorado no código atual (data: 2026-06-24).

Legenda esforço: P (pequeno) / M (médio) / G (grande). "Cred?" = depende de credencial/homologação externa (defere para M10).

---

## 1. COMPRAS E LICITAÇÕES

| # | Funcionalidade incumbente | Status | Evidência (arquivo:linha) | Criticidade PoC | Esforço | Cred? |
|---|---|---|---|---|---|---|
| 1 | Fornecedor (cadastro) | ✅ TEMOS | `Administracao.Domain/Fornecedores/Fornecedor.cs`; use cases `Application/Fornecedores/CadastrarFornecedor.cs`, `ObterFornecedorPorCnpj.cs`, `InativarFornecedor.cs`, `ReabilitarFornecedor.cs`; nível SICAF em `AtualizarNivelSicaf.cs` | — | — | — |
| 2 | Produto / Serviço (catálogo CATMAT/CATSER) | 🔴 FALTA | Não há entidade Produto/Serviço/ItemCatalogo em Administração (`grep Produto\|CatalogoItem` = 0). Existe `Patrimonio.Domain/Estoque/ItemEstoque.cs:25` (almoxarifado: código+unidade+saldo) mas NÃO é catálogo de compras com CATMAT/CATSER nem vincula preço a licitação | CRÍTICO (base de qualquer processo de compra; itens de edital/empenho) | M | Não |
| 3 | Limites de Licitação (faixas art. 75 / valores) | 🔴 FALTA | `grep limite licitac\|art.75\|LimiteDispensa` = 0. Modalidade existe como enum mas sem tabela de faixas de valor parametrizável | CRÍTICO (enquadramento legal da modalidade) | P | Não |
| 4 | Comissão (de licitação / agente de contratação) | 🔴 FALTA | Não há Comissão em Administração (`grep Comissao` = 0). Há `Patrimonio.Domain/Inventarios/MembroComissao.cs` mas é comissão de **inventário**, não de licitação | DESEJÁVEL (NLLC favorece agente de contratação; comissão p/ casos específicos) | P | Não |
| 5 | Ordenação de Despesa / Ordenador | 🔴 FALTA | `grep OrdenacaoDespesa\|Ordenador` = 0 em Administração. (Empenho/dotação tratados via integração Finanças, mas sem entidade de ordenação) | DESEJÁVEL | P | Não |
| 6 | Processos de Compra | 🔴 FALTA | Não há aggregate "ProcessoCompra"/"PedidoCompra" (fase interna pré-licitação: requisição→cotação→autorização). `Patrimonio.Domain/Requisicoes/PedidoRequisicao.cs` é requisição de almoxarifado, não processo de compra/aquisição | CRÍTICO (fluxo diário de compras) | G | Não |
| 7 | Processos Licitatórios | 🟡 PARCIAL | `Administracao.Domain/Licitacoes/Licitacao.cs:27` (aggregate com Lote, Proposta, Habilitacao, Recurso); ciclo Abrir→Julgar→Homologar→Revogar/Anular (`Application/Licitacoes/*`). Modalidades: Pregão, Concorrência, Diálogo Competitivo, Dispensa, Inexigibilidade (`Licitacoes/Enums.cs:4`). **Falta**: Concurso, Leilão; fase de impugnação/esclarecimento; sessão pública; mapa comparativo | CRÍTICO | M | Não |
| 8 | Registros de Preços (SRP) | 🔴 FALTA | `grep RegistroPreco` = 0; `RegistroPreco` não está em `ModalidadeLicitacao` nem como aggregate | CRÍTICO (uso intensivo em prefeitura pequena) | G | Não |
| 9 | Ata de Registro de Preço | 🔴 FALTA | Não há aggregate Ata. Os hits `AtaRegistro` no grep são falsos-positivos (`DataRegistro` em migrations/Apostilamento) | CRÍTICO (decorre do SRP) | G | Não |
| 10 | Dispensas | 🟡 PARCIAL | Existe como `ModalidadeLicitacao.Dispensa` (`Licitacoes/Enums.cs:16`) e `OrigemContratacao.Dispensa` no contrato (`Contratos/Enums.cs:10`). **Falta** fluxo próprio de contratação direta (justificativa, cotações, art. 75 incisos, ratificação) | CRÍTICO (compra direta é o caminho mais usado no dia a dia) | M | Não |
| 11 | Inexigibilidades | 🟡 PARCIAL | `ModalidadeLicitacao.Inexigibilidade` (`Licitacoes/Enums.cs:19`); `OrigemContratacao` no contrato. **Falta** fluxo próprio (inviabilidade de competição, fornecedor exclusivo, ratificação art. 74) | CRÍTICO | M | Não |
| 12 | Chamadas Públicas / Credenciamentos | 🔴 FALTA | `grep ChamadaPublica` = 0. `CredenciamentoNivel1` em `Fornecedores/Enums.cs:12` é nível de cadastro do fornecedor — NÃO é processo de credenciamento (art. 79) | DESEJÁVEL (saúde/agricultura usam) | M | Não |
| 13 | Registros de Preços de Outros Órgãos (carona) | 🔴 FALTA | Sem entidade de adesão a ata externa | DESEJÁVEL | M | Não |
| 14 | Licitação Compartilhada | 🔴 FALTA | Sem suporte a órgão participante/gerenciador | PÓS-POC | M | Não |
| 15 | Minuta de Contrato | 🔴 FALTA | `grep Minuta` = 0. Contrato nasce já como instrumento (`Contratos/Contrato.cs:31`), sem fase de minuta/aprovação jurídica | DESEJÁVEL | M | Não |
| 16 | Instrumento Contratual | ✅ TEMOS | `Administracao.Domain/Contratos/Contrato.cs:31` (aggregate: objeto, valor, vigência, garantia `Garantia.cs`, aditivo `Aditivo.cs`/`Contrato.Aditivos.cs`, apostilamento `Apostilamento.cs`, rescisão, encerramento); use cases em `Application/Contratos/*` | — | — | — |
| 17 | Convênio Administrativo | 🟡 PARCIAL | Módulo `Convenios` existe mas só **recebidos** (`Recebidos/ConvenioRecebido.cs:29` — dinheiro ENTRA) + MROSC/OSC (`Mrosc/ParceriaOsc.cs`). **Falta** convênio administrativo genérico concedido/celebrado pela administração na ótica de compras | DESEJÁVEL | M | Não |
| 18 | Fornecedores Sancionados | 🟡 PARCIAL | Temos sanção interna: `Fornecedores/Sancao.cs:23` (tipos Impedimento/Inidoneidade, `EImpeditiva:66`), `Application/Fornecedores/AplicarSancao.cs`, `ListarFornecedoresImpedidos.cs`, evento `FornecedorSancionadoIntegrationEvent.cs`. **Falta** importação/sincronização de cadastros externos (CEIS/CNEP/Portal Transparência, CGU) — `grep Ceis\|Cnep` = 0 | CRÍTICO (consulta a impedidos antes de contratar é obrigação) | M | Sim (CEIS/CNEP) |

### Gerações / transmissões (COMPRAS)

| # | Funcionalidade incumbente | Status | Evidência | Criticidade PoC | Esforço | Cred? |
|---|---|---|---|---|---|---|
| 19 | LICITACON (TCE-RS) | 🔴 FALTA | `grep Licitacon` = 0. Nenhum gateway/gerador para o módulo Licitações do TCE-RS | CRÍTICO (obrigação de transmissão ao TCE-RS) | G | Sim |
| 20 | PNCP | 🟡 PARCIAL | Há gateway e use cases: `Application/Abstractions/IPncpGateway.cs`, `Licitacoes/PublicarEditalNoPncp.cs`, `Contratos/PublicarContratoNoPncp.cs`, `VarrerPrazosPncp.cs`, prazos de eficácia (`Contratos/PrazoPncp.cs`, migration `W91PncpPrazoEficacia`). **MAS** implementação é **simulada/stub**: `Infrastructure/Pncp/PncpGatewaySimulado.cs` (não chama a API real do PNCP) | CRÍTICO (publicação no PNCP é obrigatória pela NLLC) | M (trocar stub por client real) | Sim |
| 21 | BLL Compras | 🔴 FALTA | `grep BLL` = 0 | PÓS-POC (plataforma de pregão privada) | M | Sim |
| 22 | BPS (Banco de Preços em Saúde) | 🔴 FALTA | `grep BPS` = 0 | DESEJÁVEL | M | Sim |
| 23 | Banco de Preços | 🔴 FALTA | Sem pesquisa de preços / mediana de mercado | DESEJÁVEL (instrução de processo) | M | Talvez |
| 24 | Portal Compras Públicas | 🔴 FALTA | `grep ComprasPublicas` = 0 | PÓS-POC | M | Sim |

### Relatórios (COMPRAS)

| # | Funcionalidade incumbente | Status | Evidência | Criticidade PoC | Esforço | Cred? |
|---|---|---|---|---|---|---|
| 25 | Planejamento de Compras (PCA — Plano de Contratações Anual) | 🔴 FALTA | `grep PlanoContratacao\|Pca` = 0. PCA é obrigatório na NLLC (art. 12) e publicado no PNCP | CRÍTICO (obrigação legal + insumo da Lei Orçamentária) | M | Não (publicação PNCP = Sim) |
| 26 | Análise de Preço | 🔴 FALTA | Sem relatório de análise de preços | DESEJÁVEL | P | Não |
| 27 | Materiais a Comprar | 🔴 FALTA | Sem relatório de ponto-de-pedido→compra. (Existe `Estoque/PontoPedido` mas não relatório de compra) | DESEJÁVEL | P | Não |

---

## 2. PATRIMÔNIO (Auditoria Patrimonial)

| # | Funcionalidade incumbente | Status | Evidência | Criticidade PoC | Esforço | Cred? |
|---|---|---|---|---|---|---|
| 28 | Cadastro de Bem | ✅ TEMOS | `Patrimonio.Domain/Bens/BemPatrimonial.cs:25` (aggregate completo: tombamento `Tombar:210`, movimentação/transferência `Transferir:321`, depreciação linear, reavaliação `Reavaliacao.cs`, impairment `Impairment.cs`, histórico `HistoricoDepreciacao.cs`). Value objects `NumeroTombamento.cs` | — | — | — |
| 29 | Auditoria de Bens (inventário) | ✅ TEMOS | `Patrimonio.Domain/Inventarios/Inventario.cs:28` (aggregate: comissão `MembroComissao.cs`, divergências `DivergenciaInventario.cs`, snapshot `SnapshotBem.cs`, item `ItemInventario.cs`, situações abertura→conciliação→encerrado). Mínimo de membros parametrizável (`:31`) | — | — | — |

---

## 3. FROTA (Controle de Frotas)

| # | Funcionalidade incumbente | Status | Evidência | Criticidade PoC | Esforço | Cred? |
|---|---|---|---|---|---|---|
| 30 | Veículo | ✅ TEMOS | `Patrimonio.Domain/Frota/Veiculo.cs:27` (placa `Placa.cs`, renavam `Renavam.cs`, odômetro/horímetro, motorista atual, ciclo patrimonial `Veiculo.CicloPatrimonial.cs`) | — | — | — |
| 31 | Pneu (cadastro) | 🔴 FALTA | `grep Pneu` = 0. Nenhuma entidade de pneu | DESEJÁVEL (gestão de pneus é controle típico de frota municipal) | M | Não |
| 32 | Motorista | ✅ TEMOS | `Patrimonio.Domain/Frota/Motorista.cs:21` (nome, CNH, categoria, validade; `CnhValidaEm:56`) | — | — | — |
| 33 | Layout / Posicionamento de Pneus | 🔴 FALTA | `grep Posicionamento` = 0 | PÓS-POC | M | Não |
| 34 | Apólices de Seguro | 🔴 FALTA | `grep Apolice\|Seguro` = 0. `Licenciamento.cs` cobre IPVA+taxa, NÃO apólice/seguro (nem DPVAT) | DESEJÁVEL (controle de vigência de apólice) | P | Não |
| 35 | Multa | ✅ TEMOS | `Patrimonio.Domain/Frota/Multa.cs:22` (código infração CTB, valor, data, motorista, situação) | — | — | — |
| 36 | Deslocamento / Viagem / Reserva (agendamento de uso) | 🔴 FALTA | `grep Reserva\|Viagem\|Deslocamento` = 0 | DESEJÁVEL (controle de uso diário do veículo) | M | Não |
| 37 | Manutenção | 🟡 PARCIAL | `Frota/ManutencaoOS.cs:22` (OS: descrição, custo estimado/realizado, odômetro, conclusão, situação). **Falta** distinção preventiva/corretiva e plano de manutenção periódica | DESEJÁVEL | P | Não |
| 38 | Controle de Pneus | 🔴 FALTA | Sem domínio de pneus (ver #31) | PÓS-POC | M | Não |
| 39 | Reposicionamento de Pneus | 🔴 FALTA | `grep Reposicionamento` = 0 | PÓS-POC | M | Não |

### Relatórios (FROTA)

| # | Funcionalidade incumbente | Status | Evidência | Criticidade PoC | Esforço | Cred? |
|---|---|---|---|---|---|---|
| 40 | Média de Consumo | 🟡 PARCIAL | Dados existem: `Frota/Abastecimento.cs:22` (litros, valor, odômetro, horímetro). **Falta** o relatório/cálculo de km/L consolidado | DESEJÁVEL | P | Não |
| 41 | Manutenções Periódicas | 🔴 FALTA | Sem plano de manutenção periódica/alerta por km (ver #37) | DESEJÁVEL | M | Não |
| 42 | Custo Km/Hora | 🔴 FALTA | Sem relatório de custo por km/hora (abastecimento + manutenção / odômetro) | DESEJÁVEL | M | Não |
| 43 | Seguro Obrigatório | 🔴 FALTA | Sem controle de DPVAT/seguro obrigatório (Licenciamento não tem campo de seguro) | PÓS-POC | P | Não |
| 44 | Controle de Habilitações / CNH | 🟡 PARCIAL | Dado existe: `Motorista.cs:51` (`ValidadeCnh`) + `CnhValidaEm:56`. **Falta** relatório/alerta de CNH a vencer | DESEJÁVEL | P | Não |
| 45 | Validade de Documentos | 🔴 FALTA | Sem painel consolidado de vencimentos (CNH + licenciamento + apólice) | DESEJÁVEL | P | Não |

---

## 4. PROTOCOLO (e-Protocolo)

| # | Funcionalidade incumbente | Status | Evidência | Criticidade PoC | Esforço | Cred? |
|---|---|---|---|---|---|---|
| 46 | Abertura de Processo (autuação + NUP) | ✅ TEMOS | `Protocolo.Domain/Processos/Processo.cs:27` (NUP `Nup.cs`, sequência `SequenciaNup.cs`, classificação, nível de acesso, prazo, data autuação `:103`) | — | — | — |
| 47 | Recebimento de Processo | 🟡 PARCIAL | Há tramitação com setor destino (`Tramitar:186`), mas não há ato explícito de "receber" (aceite no destino) separado do tramitar | DESEJÁVEL | P | Não |
| 48 | Tramitação / Movimentação | ✅ TEMOS | `Processo.cs:186` `Tramitar(setorDestino, observacao, data)`; `Processos/Movimentacao.cs`; setor atual `:81` | — | — | — |
| 49 | Fechamento / Encerramento / Arquivamento | ✅ TEMOS | `Processo.cs:253` `Arquivar(motivo, data)`; arquivística completa: `Arquivistica/DestinacaoProcesso.cs`, `PlanoDeClassificacao.cs`, `TabelaTemporalidade.cs`, `MetadadosArquivisticos.cs` | — | — | — |
| 50 | Despacho | ✅ TEMOS | `Processo.cs:212` `Despachar` (append-only); `Processos/Despacho.cs` | — | — | — |

> Observação: Protocolo é o módulo mais maduro do cluster — além do CRUD de processo, tem assinatura/carimbo de tempo (`ValueObjects/Assinatura.cs`, `CarimboDeTempo.cs`, `Hash.cs`), documentos (`Documentos/Documento.cs`) e camada arquivística (plano de classificação + temporalidade). Lacunas são refinos (recebimento explícito, apensamento/juntada não verificados como métodos próprios — `grep Apensar\|Juntar` na Processo = 0 → 🔴 apensamento/juntada FALTA, esforço P).

---

## 5. MD-e — Manifestação do Destinatário (DF-e)

| # | Funcionalidade incumbente | Status | Evidência | Criticidade PoC | Esforço | Cred? |
|---|---|---|---|---|---|---|
| 51 | Manifestação do Destinatário (DF-e / eventos NF-e: ciência, confirmação, desconhecimento, não realizada) | 🔴 FALTA | `grep ManifestacaoDestinatario\|DFe\|MDe` em src = 0. Nenhum domínio de NF-e/DF-e | DESEJÁVEL (controle fiscal de notas recebidas; reduz risco de mercadoria fantasma) | G | Sim (certificado A1 + SEFAZ) |

---

## Resumo executivo — o que falta para paridade com o SAPI

**Bloco mais frágil = COMPRAS E LICITAÇÕES.** É onde o incumbente tem mais largura e onde estamos mais rasos. Patrimônio e Protocolo já têm boa paridade; Frota tem o núcleo mas faltam relatórios e pneus/seguro.

### CRÍTICOS para o PoC (uso diário OU obrigação de transmissão) — priorizar:
1. **Catálogo de Produto/Serviço (CATMAT/CATSER)** [#2, M] — base de tudo em compras.
2. **Processos de Compra / fase interna** [#6, G] — fluxo requisição→cotação→autorização.
3. **Dispensa e Inexigibilidade como fluxo próprio** [#10, #11, M cada] — caminho mais usado no dia a dia.
4. **Registro de Preços + Ata** [#8, #9, G] — uso intensivo em prefeitura pequena.
5. **PCA — Plano de Contratações Anual** [#25, M] — obrigação legal (NLLC art. 12).
6. **Limites de Licitação parametrizáveis** [#3, P] — enquadramento legal.
7. **PNCP real** [#20, M] — hoje é stub (`PncpGatewaySimulado.cs`); obrigação de publicação.
8. **LICITACON / TCE-RS** [#19, G, cred] — obrigação de transmissão; defere parcialmente a M10 por credencial.
9. **Fornecedores Sancionados externos (CEIS/CNEP)** [#18, M, cred] — consulta obrigatória antes de contratar.

### DESEJÁVEIS (melhoram a venda, não bloqueiam PoC):
Comissão de licitação [#4, P], Credenciamento [#12, M], Convênio administrativo genérico [#17, M], relatórios de Frota (média consumo [#40, P], custo km/h [#42, M], CNH a vencer [#44, P]), Apólice de seguro [#34, P], Manutenção preventiva [#37, P], recebimento/apensamento no Protocolo [#47, P].

### PÓS-POC ou diferíveis:
Controle de pneus e reposicionamento [#31/#33/#38/#39], licitação compartilhada [#14], carona de outros órgãos [#13], plataformas privadas (BLL/Portal Compras Públicas) [#21/#24], MD-e/DF-e [#51, cred].

### Pontos onde NÃO inventamos paridade (honestidade):
- `AtaRegistro` aparece no grep mas são **falsos-positivos** (`DataRegistro` em migrations/apostilamento) — não temos Ata de Registro de Preço.
- `Credenciamento` no código é **nível de cadastro do fornecedor** (`CredenciamentoNivel1`), NÃO o processo de credenciamento do art. 79.
- `Comissao` no código é de **inventário patrimonial**, não de licitação.
- `Licenciamento` cobre IPVA/taxa, NÃO apólice de seguro nem DPVAT.
- PNCP existe na arquitetura (gateway + use cases + prazos de eficácia) mas a implementação concreta é **simulada** (`PncpGatewaySimulado`), assim como Receita CNPJ (`SimuladoReceitaCnpjGateway`).

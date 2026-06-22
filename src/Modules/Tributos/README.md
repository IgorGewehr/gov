# Módulo Tributos
> Gestão do crédito tributário municipal — do lançamento à Dívida Ativa e cobrança/protesto. · Poder: Executivo · Schema EF Core: `tributos` · Ativável por tenant.

## 1. Propósito & Marco Legal
Bounded Context responsável pela **constituição do crédito tributário** (lançamento — CTN art. 142), **arrecadação** (DAM/guias) e, sobretudo, pela **gestão da Dívida Ativa e cobrança** (CTN art. 201; LEF — Lei 6.830/1980). Cobre IPTU, ISSQN (LC 116/2003), ITBI, COSIP/CIP (CF art. 149-A), taxas de poder de polícia/serviços (CF art. 145), contribuição de melhoria, e o regime do Simples Nacional (LC 123/2006). Observa **decadência** (CTN art. 173, I; art. 150 §4º p/ homologação), **prescrição** (art. 174), requisitos da **CDA** (art. 202), emissão de **certidões** (art. 205–206), **protesto extrajudicial** (Lei 9.492/1997) e **sigilo fiscal** (art. 198). Receita classificada conforme Lei 4.320/1964.
**⭐ ADR-0003 (NFS-e Nacional/ADN — integração PASSIVA):** com o padrão nacional da NFS-e (vigente 2026; piloto Maximiliano de Almeida/RS), o módulo **NÃO emite nem assina NFS-e**. O foco é Dívida Ativa e cobrança; as notas fiscais de serviço são apenas **consumidas** do Ambiente de Dados Nacional (ADN) para compor o painel fiscal e subsidiar a fiscalização do ISSQN.

## 2. Linguagem Ubíqua
1. **Lancamento** — ato de constituição do crédito (CTN art. 142).
2. **CreditoTributario** — valor exigível decorrente do lançamento.
3. **IPTU** — imposto sobre propriedade predial e territorial urbana.
4. **ValorVenal** — base de cálculo do IPTU/ITBI, apurada pela PGV.
5. **PGV (PlantaGenericaDeValores)** — tabela de valores por logradouro, versionada por vigência.
6. **ISSQN** — imposto sobre serviços (LC 116/2003).
7. **RetencaoNaFonte** — ISSQN retido pelo tomador.
8. **ITBI** — imposto sobre transmissão de bens imóveis.
9. **COSIP/CIP** — contribuição de iluminação pública (CF art. 149-A).
10. **Taxa / PoderDePolicia** — tributo por serviço/fiscalização (ex.: Alvara).
11. **ContribuicaoDeMelhoria** — tributo por valorização decorrente de obra.
12. **DAM (Documento de Arrecadação Municipal)** — guia de pagamento (boleto/PIX).
13. **DividaAtiva** — crédito inscrito após esgotado o prazo administrativo (art. 201).
14. **InscricaoDA / CDA** — inscrição e respectiva Certidão de Dívida Ativa (art. 202).
15. **ExecucaoFiscal** — cobrança judicial da CDA (LEF).
16. **TituloProtestado** — CDA levada a protesto extrajudicial (Lei 9.492/1997).
17. **REFIS / Parcelamento** — programa de regularização; suspende exigibilidade (art. 151).
18. **Decadencia** — perda do direito de lançar (5 anos).
19. **Prescricao** — perda da pretensão de cobrar (5 anos).
20. **CND / CPEN** — certidão negativa / positiva com efeito de negativa (art. 205–206).
21. **NotaFiscalServico** — NFS-e consumida do ADN (read model).

## 3. Mapa de Domínio
**Agregados (raízes — todos `IMustHaveTenant`):**
- **Contribuinte** — VOs `CPFCNPJ`, `InscricaoMunicipal`.
- **Imovel** — VOs `ValorVenal`, `CodigoLogradouroPGV`.
- **Lancamento** — VOs `Aliquota`, `BaseDeCalculo`, `Competencia`; valida decadência.
- **Guia/DAM** — entidades `Parcela`; VOs `CodigoBarras`, `PIX`.
- **DividaAtiva** — entidades `InscricaoDA`, `CDA`, `Parcelamento`; controla prescrição.
- **Certidao** — emite CND/CPEN.

**Read models:** **NotaFiscalServico** (projeção do ADN, chave de acesso única por tenant).

**Eventos de Domínio:** `CreditoTributarioLancado`, `CarneEmitido`, `PagamentoConciliado`, `InscritoEmDividaAtiva`, `CDAEmitida`, `TituloProtestado`, `ExecucaoFiscalAjuizada`, `ParcelamentoFirmado`, `NfseSincronizada`, `CertidaoEmitida`.

## 4. Integrações & Padrões Técnicos
- **⭐ ADN / Receita Federal (NFS-e — PASSIVA):** o **Worker `Tensorroot.Gov.Workers.NfseSync`** conecta **diariamente** na API da Receita Federal/ADN, baixa os XMLs das NFS-e dos CNPJs do tenant, **deduplica por chave de acesso** e persiste. Padrões: `HttpClient` tipado + **Polly** (retry exponencial + jitter, circuit breaker, timeout) para resiliência da chamada externa; **ACL (Anti-Corruption Layer)** traduz o XML/contrato nacional para o read model `NotaFiscalServico`, isolando o domínio do schema federal. Cada lote sincronizado publica `NfseSincronizada`. Worker idempotente e retomável por cursor de data/tenant.
- **Protesto — CRA-IEPTB:** geração de **remessa XML** (CDA → título); baixa do retorno atualiza `TituloProtestado`.
- **Execução Fiscal — PJe/TJ:** ajuizamento eletrônico da CDA; petição/distribuição.
- **Arrecadação bancária:** DAM com **código de barras FEBRABAN** e **PIX QR dinâmico**; **retorno CNAB 240** processado para **conciliação** automática (`PagamentoConciliado`).
- **Mensageria interna:** **cross-module exclusivamente via Integration Events** (Outbox + MediatR). Publica eventos de arrecadação para **Financas** (receita orçamentária — Lei 4.320/1964) e **Transparencia**.

## 5. Regras de Negócio Críticas
- **Decadência (5 anos):** bloquear lançamento de crédito decaído (art. 173, I; art. 150 §4º p/ tributos por homologação).
- **Prescrição (5 anos):** marcar prescrição a partir da constituição definitiva (art. 174); **interrompida** por parcelamento e por citação na execução; impedir cobrança/protesto de DA prescrita indevidamente.
- **ValorVenal** nunca inferior à **PGV vigente** na competência.
- **CDA** só é válida com **todos os requisitos do art. 202** (sob pena de nulidade); inscrição em DA **apenas após esgotado o prazo administrativo** (art. 201).
- **REFIS/Parcelamento** suspende a exigibilidade (art. 151) e interrompe a prescrição.
- **Parametrização por tenant:** alíquotas e PGV são configuráveis e **versionados por vigência** (aplica-se a regra vigente na competência do fato gerador).

## 6. Multi-Tenancy, Segurança & Auditoria
- Todos os agregados implementam **`IMustHaveTenant`**; query filter global por `TenantId` no schema `tributos` — isolamento físico/lógico dos dados fiscais de cada município.
- **Sigilo fiscal (CTN art. 198):** acesso a dados do contribuinte restrito por autorização; consultas e exportações **auditadas** (quem/quando/qual inscrição).
- Trilha de auditoria imutável para lançamento, inscrição em DA, emissão de CDA, parcelamento e cancelamento; eventos persistidos via **Outbox** garantem rastreabilidade e entrega.
- O Worker NfseSync opera com credenciais por tenant (CNPJs autorizados); segredos fora do código.

## 7. Contratos Públicos (Integration Events)
- `CreditoTributarioLancado` — {tenantId, lancamentoId, contribuinteId, tributo, competencia, valor}
- `PagamentoConciliado` — {tenantId, damId, parcela, valorPago, dataPagamento, canal}
- `InscritoEmDividaAtiva` — {tenantId, inscricaoDaId, contribuinteId, valor, origem}
- `CDAEmitida` — {tenantId, cdaId, inscricaoDaId, numeroCDA}
- `TituloProtestado` — {tenantId, cdaId, protocoloCartorio, situacao}
- `ExecucaoFiscalAjuizada` — {tenantId, cdaId, numeroProcesso}
- `ParcelamentoFirmado` — {tenantId, parcelamentoId, inscricaoDaId, parcelas}
- `NfseSincronizada` — {tenantId, loteId, chaveAcessoInicio/fim, qtdNotas, dataReferencia}
- `CertidaoEmitida` — {tenantId, certidaoId, contribuinteId, tipo (CND/CPEN)}

## 8. Cenários BDD
**Cenário: Bloqueio de lançamento decaído**
- **Dado** um fato gerador de IPTU ocorrido há mais de 5 anos sem lançamento
- **Quando** o operador tenta constituir o crédito
- **Então** o lançamento é rejeitado por **decadência** (art. 173, I) e nenhum evento é publicado.

**Cenário: Inscrição em Dívida Ativa após prazo administrativo**
- **Dado** um crédito tributário definitivamente constituído e vencido, com prazo administrativo esgotado
- **Quando** a rotina de inscrição é executada
- **Então** é gerada a `InscricaoDA`, emitida a **CDA** com os requisitos do art. 202 e publicado `InscritoEmDividaAtiva` + `CDAEmitida`.

**Cenário: Conciliação automática via CNAB 240**
- **Dado** um DAM em aberto e um arquivo de retorno bancário CNAB 240
- **Quando** o retorno é processado
- **Então** a parcela é baixada e `PagamentoConciliado` é publicado para Financas/Transparencia.

**Cenário: REFIS suspende exigibilidade e interrompe prescrição**
- **Dado** uma Dívida Ativa em cobrança
- **Quando** o contribuinte firma parcelamento (REFIS)
- **Então** a exigibilidade é suspensa (art. 151), a prescrição é interrompida e `ParcelamentoFirmado` é publicado.

**⭐ Cenário: Sincronização passiva de NFS-e do ADN**
- **Dado** os CNPJs autorizados de um tenant e NFS-e disponíveis no ADN/Receita Federal
- **Quando** o Worker `NfseSync` executa a rotina diária e baixa os XMLs
- **Então** as notas são **deduplicadas por chave de acesso**, traduzidas pela **ACL** para `NotaFiscalServico`, persistidas e `NfseSincronizada` é publicado — mesmo com falha transitória da API, o **Polly** reexecuta sem duplicar registros.

**Cenário: Protesto extrajudicial de CDA**
- **Dado** uma CDA válida não paga
- **Quando** a remessa XML é enviada ao CRA-IEPTB e o cartório confirma
- **Então** o título passa a `TituloProtestado` e `TituloProtestado` é publicado.

## 9. Fontes
- CTN — Lei 5.172/1966: https://www.planalto.gov.br/ccivil_03/leis/l5172compilado.htm (arts. 142, 150 §4º, 151, 173, 174, 198, 201, 202, 205–206)
- LC 116/2003 (ISSQN): https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp116.htm
- LEF — Lei 6.830/1980 (execução fiscal): https://www.planalto.gov.br/ccivil_03/leis/l6830.htm
- Lei 9.492/1997 (protesto extrajudicial): https://www.planalto.gov.br/ccivil_03/leis/l9492.htm
- NFS-e Nacional / ADN: https://www.gov.br/nfse/
- Complementares: LC 123/2006 (Simples Nacional); Lei 4.320/1964; CF arts. 145, 156, 149-A.

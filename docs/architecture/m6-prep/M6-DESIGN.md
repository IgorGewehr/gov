# M6 — Tributos · DESIGN pronto-para-implementar

> **Arquiteto do M6.** Design técnico do módulo **Tributos** (schema `tributos`, Executivo, ativável por tenant), partindo do que **já existe** (`Contribuinte`, `Lancamento`, `DividaAtiva`, `NotaFiscalServico`, VOs `Competencia`/`ValorMonetario`, `NfseSincronizador`/`AdnNfseGateway`, `ReceitaArrecadadaIntegrationEvent`) e completando até cadastro imobiliário + motores de cálculo + ingestão ADN + cobrança/protesto/execução + arrecadação.
>
> **Regras inegociáveis aplicadas:** Clean Arch + DDD (CLAUDE.md §2); `IMustHaveTenant` em toda raiz (§5); auditoria imutável (§4/§6); domínio rico, VO `record`/`sealed`, factory privada (§7); MediatR CQRS + Outbox (§10); Polly + ACL em toda I/O externa (§8/§11); **NFS-e PASSIVA — não emitimos** (§8/ADR-0003); **nada hardcoded — alíquota/PGV/regra é lei municipal parametrizável por tenant+vigência** (§7/§16). Cada item traz **CONFIANÇA**, **docs oficiais a obter** e **o que é lei municipal (parametrizar)**.
>
> Legenda de confiança: **ALTA** = base legal/contrato confirmado nas verificações; **MÉDIA** = padrão correto, detalhe numérico/leiaute depende de doc oficial; **BAIXA** = bloqueado por leiaute/contrato não obtido (não implementar fiel sem o doc).

---

## 0. Princípio transversal — Parametrização temporal por tenant (a fundação de tudo)

Antes de qualquer motor, criar o subsistema de **parâmetros fiscais versionados por vigência**. NENHUM número fiscal vive no código.

- Agregado **`ParametroFiscal`** (e família por espécie): chave `(TenantId, Tipo, ExercicioOuVigenciaInicio, VigenciaFim?)`. O motor sempre lê **a regra vigente na competência/fato gerador**, nunca "a atual".
- Conteúdo modelado como tabelas/VOs tipados (não JSON solto onde houver invariante): `TabelaPgv`, `TabelaAliquotaIptu`, `CalendarioFiscal`, `RegraDescontoIptu`, `TabelaAliquotaIss` (por item LC 116), `MapaRetencaoIss`, `ListaSubstitutosIss`, `AliquotaItbi`, `TabelaTaxa`, `TabelaTll`, `FaixasCosip`, `RegraEncargosDivida` (multa/juros/correção), `LimiarCobranca` (protesto×execução), `TransicaoIssIbs` (por competência).
- Auditoria: toda alteração de parâmetro entra na trilha imutável (quem mudou alíquota/PGV, quando) — crítico para TCE-RS.
- **Lei municipal a parametrizar (TODA a tabela):** Código Tributário Municipal (CTM) + lei da PGV + decreto anual do IPTU + lei COSIP + leis de taxas/TLL de **Maximiliano de Almeida/RS** — `[a confirmar — obter doc oficial]` (bloqueador de popular o tenant piloto; o motor compila e roda sem eles, só não calcula valores). **CONFIANÇA: ALTA** (a necessidade de parametrizar é confirmada; os valores são pendência documental).

---

## 1. Cadastro Imobiliário + motor IPTU (PGV parametrizável)

### 1.1 Agregado `CadastroImobiliario` (BCI) — schema `tributos`, `IMustHaveTenant`
- **Três identificadores coexistentes** (VO `IdentificacaoImovel`): `InscricaoMunicipal` (chave histórica, sempre presente), `CibCodigo` (**opcional/nullable** — formato `AAAAAAA-D`, 7 alfanum + DV), `MatriculaRgi` (opcional). **CONFIANÇA: ALTA** (CIB formato e geração via SINTER confirmados; Decreto 11.208/2022, IN RFB 2.275/2025).
- **BCI** como entidades/VOs: `Terreno` (área, testada, topografia, situação na quadra), `Construcao` (área construída, tipo, padrão, uso, ano, conservação), `Localizacao` (logradouro, setor/quadra/lote, zona fiscal, face de quadra), `Infraestrutura` (água/esgoto/energia/pavimentação), `FracaoIdeal`. Liga ao `Contribuinte` existente como **sujeito passivo** (proprietário/possuidor — pode haver coproprietários).
- **CIB → SINTER:** modelar campo opcional + **NÃO implementar o envio** agora. Piloto (não-capital) só obrigado **jan/2027**. Quando houver manual técnico, criar `CadastroImobiliarioSync` espelhando filosofia NfseSync (ACL + Outbox + idempotência). **CONFIANÇA: BAIXA** para o envio (protocolo/XSD/DV não publicado).
- **Lei municipal a parametrizar:** os **campos efetivos do BCI** e as zonas fiscais são definidos por lei/decreto local (não há leiaute nacional de BCI) — modelar BCI extensível, atributos do tenant. **CONFIANÇA: MÉDIA** (campos recorrentes, não normativos nacionais).
- **Docs a obter:** manual técnico SINTER / convênio ENAT / NT CTAT 05/2025 (envio cadastral + DV do CIB); lei do cadastro imobiliário de Maximiliano de Almeida/RS.

### 1.2 Motor de Valor Venal (PGV) — serviço de domínio `CalculadoraValorVenal`
- Base de cálculo do IPTU = **valor venal** (CTN art. 33). **CONFIANÇA: ALTA.**
- Fórmula genérica `ValorVenal = (AreaTerreno × VUT × fatores_terreno) + (AreaConstruida × VUC × FatorPadrao × FatorDepreciacao × fatores_localizacao)` — **estrutura**, não números. O motor lê `TabelaPgv` vigente no exercício; **zero defaults numéricos** (NÃO usar fatores de Salvador/SP). **CONFIANÇA: MÉDIA** (não há fórmula nacional única — cada município define na lei da PGV).
- **Súmula 160/STJ:** decreto só pode atualizar a base por índice oficial de correção; **majoração real exige lei** → modelar `TabelaPgv` versionada por exercício, com distinção entre correção monetária (decreto) e revisão de valores (lei). **CONFIANÇA: ALTA.**
- **Valor de referência do SINTER (LC 214/2025 art. 256, IN 2.275/2025):** tratar como **dado externo informativo**, NUNCA base de cálculo automática. **CONFIANÇA: ALTA** (princípio); efeito regulatório futuro **INCERTO**.

### 1.3 Lançamento anual de IPTU + parcelamento (DAM)
- IPTU lançado **uma vez/exercício, de ofício** (CTN art. 142/149) → gera `Lancamento` (`TipoTributo.Iptu`, já existe) por imóvel. **CONFIANÇA: ALTA.**
- **Estender `Lancamento`** com vínculo ao `CadastroImobiliarioId` e suporte a **parcelamento** (cota única × N parcelas) — ver §6 (entidade `Parcela`/agregado `Dam`).
- Alíquotas IPTU **progressivas** (por valor venal e/ou uso; territorial × predial) e progressividade no tempo (EC 29/2000; CF art. 156 §1; art. 182 §4 + Estatuto da Cidade Lei 10.257/2001) → `TabelaAliquotaIptu` parametrizável. **CONFIANÇA: ALTA.**
- **Lei municipal a parametrizar:** PGV completa (VUT/VUC por zona + todos os fatores), alíquotas/progressividade, calendário fiscal (lançamento, vencimentos, nº máx. de parcelas, valor mínimo de parcela), descontos (cota única/adimplência), isenções/imunidades, índice de correção. **NÃO usar SP 3%/Recife 5%/Natal 16% como default** (risco §16). **CONFIANÇA: ALTA** (que é municipal); valores **BAIXA/pendente doc**.

---

## 2. Apuração ISS + ingestão NFS-e/ADN (worker passivo)

### 2.1 Ingestão (estender o que existe)
- Já existem `NotaFiscalServico` (read model), `AdnNfseGateway` (ACL), `NfseSincronizador`, `SimuladoNfseGateway`, Worker `NfseSync`. **Evoluir** o gateway do modelo "por CNPJ/desde" para o **padrão DF-e real do ADN: cursor por NSU** (Número Sequencial Único) persistido por tenant; `GET /DFe/{NSU}` retorna NFS-e **+ eventos**; XML **GZip+Base64**; autenticação **mTLS + certificado ICP-Brasil (e-CNPJ do ente)**. **CONFIANÇA: ALTA** (conceito NSU/DF-e/GZip+Base64/mTLS confirmados).
- **Dedup:** chave de acesso (50 dígitos) como **idempotency key**, constraint único `(TenantId, ChaveAcesso)` — já é a estratégia do `NfseSincronizador.ExistePorChaveAsync`. **Usar a chave inteira como string; NÃO parsear/validar DV por layout** (verificação: layouts publicados divergem). **CONFIANÇA: ALTA** (dedup); **BAIXA** (decompor a chave).
- **Eventos da NFS-e** (cancelamento/substituição): novo agregado/entidade `EventoNfse` ligado por chave; nota cancelada/substituída **sai da base de apuração** mas mantém histórico imutável. **CONFIANÇA: MÉDIA** (conceito padrão; códigos exatos no manual de eventos).
- **Docs a obter (bloqueador de fidelidade):** Manual de APIs do ADN família `/municipios` (recursos, cursor/paginação NSU, nº de DF-e por lote — "50" **não confirmado**, tratar como configurável), perfil/uso do certificado, leiaute XSD da NFS-e + manual de eventos (campos de retenção/substituição, item LC 116, local de incidência), layout+DV da chave de 50 díg. **CONFIANÇA do "50/lote": BAIXA.**

### 2.2 Motor de apuração ISS — serviço `ApuradorIss`
- Base legal: **LC 116/2003** (normas gerais; lista de serviços) + CTM. **CONFIANÇA: ALTA.**
- Classifica a partir dos campos do XML + parâmetros do tenant: **ISS próprio** (prestador no município — art. 3º regra geral + exceções), **ISS retido na fonte** (tomador — art. 6º §2º II), **substituição tributária** (faculdade municipal — art. 6º caput, depende de lei). **CONFIANÇA: ALTA.**
- Saída: consolidação por competência/contribuinte (a recolher próprio / retido / substituído); **divergência** (prestador local sem recolhimento) → gancho para `Lancamento` (`TipoTributo.Iss`) e, em inadimplência, Dívida Ativa.
- **Livro Eletrônico** = **read model gerado da ingestão** (notas + eventos + apuração) por tenant/competência (escrituração deriva do acervo ADN, reduz declaração manual). **CONFIANÇA: MÉDIA** (decisão de arquitetura; obrigação acessória municipal a confirmar).
- **Lei municipal a parametrizar:** `TabelaAliquotaIss` por item LC 116, `MapaRetencaoIss`, `ListaSubstitutosIss`, leiaute/periodicidade do Livro. **CONFIANÇA: ALTA** (que é municipal); valores pendentes do CTM.

### 2.3 Reforma Tributária ISS → IBS (camada parametrizável, preparada para 2029)
- 2026/2027 **não** alteram a apuração do ISS (teste CBS 0,9%/IBS 0,1% — ADCT art. 125). Coexistência ISS+IBS **2029–2032** (ISS a 90/80/70/60% + IBS complementar pela alíquota de referência do Senado — **não** "IBS 10/20/30/40%", que é a *redução* do ISS); **ISS extinto 2033**. **CONFIANÇA: ALTA** (cronograma/marcos); **percentuais de redução por ano: MÉDIA/INCERTO — obter LC 214/2025 + atos do Comitê Gestor do IBS**.
- **Modelar:** tributo como **dado versionado por vigência** (não enum perene) + `TransicaoIssIbs` por competência. **CDA de ISS sobrevive à extinção** (prescrição roda 5+ anos pós-2033) → permitir DA/cobrança de tributo extinto. **CONFIANÇA: ALTA** (implicação de domínio).

---

## 3. ITBI / Taxas / COSIP / Alvarás / Contribuição de Melhoria

### 3.1 ITBI (`TipoTributo.Itbi`) — guia avulsa por transação
- Fato gerador exigível **só no registro** no CRI (não na escritura; LC 227/2026 vetou antecipação). **CONFIANÇA: ALTA** (entendimento consolidado); texto literal `[a confirmar — Planalto LC 227/2026, EC 132/2023]`.
- **Base = valor declarado (presunção) — Tema 1.113/STJ (REsp 1.937.821):** NÃO vincula ao valor venal do IPTU, NÃO pode arbitrar por valor de referência unilateral; afastamento só via **processo administrativo (CTN art. 148)**. → motor de ITBI **não** usa PGV como base impositiva; valor de referência só **indicativo/alerta**; suportar workflow de arbitramento. **CONFIANÇA: ALTA.**
- Alíquota proporcional por lei municipal (usual 2–3%, sem teto federal; possível redução SFH na parcela financiada). Imunidades CF art. 156 §2º I (integralização de capital / fusão-incorporação-cisão, salvo atividade imobiliária preponderante). **CONFIANÇA: ALTA** (regra); alíquota **pendente CTM**.
- **Lei municipal a parametrizar:** `AliquotaItbi`, contribuinte (CTN art. 42, usual adquirente), isenções, valor de referência indicativo.

### 3.2 Taxas (`TipoTributo.Taxa`) — poder de polícia / serviço
- CTN arts. 77–80. **Invariante CTN art. 80 + SV 29/STF:** taxa NÃO pode ter base/fato gerador idênticos a imposto, NEM ser calculada sobre o capital da empresa (pode usar *elementos* como área/metragem). SV 19 valida taxa de lixo. **CONFIANÇA: ALTA.**
- Modelar `Taxa` com **subtipo** (polícia × serviço) + `TabelaTaxa` parametrizável (valor por faixa/atividade/metragem). **Lei municipal:** tabelas do CTM.

### 3.3 Alvarás + TLL (`TipoTributo.Taxa`, subtipo Licença)
- **Achado central:** "Alvará" é o **ato administrativo de polícia**; o tributo é a **Taxa de Licença de Localização/Funcionamento (TLL)** + renovação anual. **CONFIANÇA: ALTA.**
- Modelar agregado **`Alvara`/`Estabelecimento`** (vigência/renovação, espécies: localização, sanitário, ambiental, obras/Habite-se) → gera `Lancamento` `Taxa` subtipo Licença a partir de `TabelaTll` (por atividade/CNAE/área/risco). **Lei municipal:** tabela e classes de risco do CTM.

### 3.4 COSIP/CIP (`TipoTributo.Cosip`) — contribuição sui generis
- CF art. 149-A (EC 39/2002); STF RE 573.675/Tema 44 (cobrança na fatura de energia válida; progressividade OK; é tributo *sui generis*). **CONFIANÇA: ALTA.**
- Base = `FaixasCosip` (consumo kWh / classe de consumidor) por lei municipal própria. Modelar **dois caminhos:** (a) repasse/conciliação com a distribuidora (cobrança na fatura), (b) lançamento próprio para não faturados. **Docs:** lei COSIP de Maximiliano de Almeida/RS + convênio com distribuidora (RGE/CEEE na região). **CONFIANÇA: ALTA** (modelo); convênio **pendente**.

### 3.5 Contribuição de Melhoria (`TipoTributo.ContribuicaoMelhoria`)
- CTN arts. 81–82 + DL 195/1967. **Fato gerador = valorização** decorrente de obra (não a obra). **Limites:** total = despesa da obra; individual = acréscimo de valor por imóvel; rateio proporcional. **CONFIANÇA: ALTA.**
- **Requisito procedimental obrigatório (CTN art. 82):** edital prévio (memorial, orçamento, parcela a financiar, zona, fator de absorção) + **prazo de impugnação ≥ 30 dias** + processo administrativo. → agregado **`ObraContribuicaoMelhoria`** (edital, custo, zona, fator, workflow de impugnação **não opcional**) → gera `Lancamento` por imóvel com rateio respeitando os dois limites. **CONFIANÇA: ALTA.**

---

## 4. Dívida Ativa → CDA → Protesto → Execução

> Já existem `DividaAtiva` + `Inscrever`/`EmitirCda`/`Protestar`/`AjuizarExecucaoFiscal`/`FirmarParcelamento`/`Quitar` + `EstaPrescrita`. Esta seção **completa** os gaps.

- **Inscrição/TIDA e requisitos da CDA (LEF art. 2º §5º I–VI + CTN art. 202):** a CDA atual só tem `NumeroCda`. **Completar** com os campos exigidos (devedor+corresponsáveis+domicílio; valor originário + termo inicial + forma de cálculo de juros/encargos; origem/natureza/fundamento legal; correção monetária + fundamento; data/nº inscrição; nº do processo administrativo). **Ausência = nulidade.** **CONFIANÇA: ALTA.**
- **Substituição/emenda da CDA (LEF art. 2º §8º):** novo método `SubstituirCda` **versionando** sem perder histórico. **Invariantes (Súmula 392/STJ; Tema 1350/STJ):** PROIBIDO trocar **sujeito passivo** e **fundamento legal** (só erro material/formal). **CONFIANÇA: ALTA** (Súm. 392); Tema 1350 `[a confirmar tese final]`.
- **Protesto (Lei 9.492/97 art. 1º p.ú., incluído pela Lei 12.767/2012; STF ADI 5135 constitucional; STJ REsp 1.895.557 dispensa lei local):** o `Protestar()` atual só muda estado. **Adicionar** geração de **remessa ao CRA-RS/IEPTB-RS** + processamento de **retorno** (ocorrências: lavrado/pago/sustado), atrás de ACL versionada por CRA. **CONFIANÇA: ALTA** (base legal); **BAIXA** (leiaute exato CRA-RS — pode ser CNAB 240/400, XML/WebService CRA21, ou registro 600 bytes; **não generalizar marca "G" de SP**).
- **Execução fiscal (LEF):** o ERP **gera/exporta** CDA+petição; ajuizamento ocorre no PJe/eproc-RS. **Escopo de integração de saída PJe/eproc-RS = decisão de produto** `[a confirmar]`. **CONFIANÇA: MÉDIA.**
- **Relógio de prescrição (CTN art. 174 + LEF art. 40):** o `EstaPrescrita` atual é ingênuo (inscrição + 5 anos fixos). **Substituir** por **agregado/serviço de prescrição** com eventos de **interrupção** (despacho que ordena citação — LC 118/2005, retroage ao ajuizamento; protesto judicial; mora; reconhecimento/parcelamento — art. 174 p.ú. I–IV) e **prescrição intercorrente** (LEF art. 40 + Súmula 314/STJ + **Tema 566/STJ**: 1 ano suspensão + quinquênio automático). **Verificar Lei 14.195/2021** (alterou art. 40 LEF). Errar isso = perda de crédito público / responsabilização TCE-RS. **CONFIANÇA: ALTA** (regra); **obter art. 40 LEF pós-2021 no Planalto + REsp 1.340.553/RS inteiro teor**.
- **Encargos:** `RegraEncargosDivida` (multa/juros/correção) parametrizável por tenant; `LimiarCobranca` (faixa de valor → só protesto × execução — REsp 1.895.557 permite mínimo municipal). **Lei municipal:** REFIS, juros/multa/correção, limiares.
- **Certidões CND/CPEN** (CTN arts. 205–206) e **sigilo fiscal** (art. 198, acesso auditado). **CONFIANÇA: ALTA.**

---

## 5. Arrecadação (DAM / CNAB / PIX)

- **Agregado `Dam` (Documento de Arrecadação Municipal)** com entidades **`Parcela`** (cota única × N parcelas do IPTU e demais), VOs `CodigoBarras`/`LinhaDigitavel`/`Pix`. Gera guia por parcela com nosso layout.
- **Código de barras FEBRABAN de arrecadação:** **44 posições de dados** (2 de 5 intercalado); **linha digitável = 48 dígitos** (4 blocos de 11 + 4 DVs). **Corrigir o equívoco do README ("48 posições do código de barras")** — 48 é a linha digitável, 44 é o código de barras. **CONFIANÇA: ALTA** (FEBRABAN Layout Código de Barras v7).
- **PIX Cobrança com vencimento (cobv):** suporta juros/multa/desconto/abatimento; QR dinâmico (location URL); DICT mapeia chave→conta; conciliação por **webhook do PSP**. API tipicamente do **PSP/banco arrecadador**, não Bacen direto. **CONFIANÇA: ALTA** (Manual de Padrões para Iniciação do Pix, Bacen).
- **CNAB 240 (FEBRABAN v10.11):** ERP gera **remessa** → banco → **retorno** → **conciliação automática** contra `Lancamento`/`Dam`/`DividaAtiva`; segmentos O/N (arrecadação com/sem código de barras). **CONFIANÇA: ALTA** (estrutura); **convênio/versão/banco do piloto `[a confirmar]`**.
- **Integração de saída:** ao conciliar pagamento, baixar `Lancamento`/`DividaAtiva` e publicar **`ReceitaArrecadadaIntegrationEvent`** (já existe) via Outbox para **Finanças** (receita orçamentária Lei 4.320) e **Transparência**. Resiliência Polly + idempotência em toda I/O bancária.
- **Docs a obter:** banco arrecadador + nº convênio + versão CNAB do piloto; credenciais PIX Cobrança do PSP; layout do código de barras se houver especificidade do convênio.

---

## 6. Ordem de implementação (incremental, BDD-first, cada fase compila/sobe)

1. **Parâmetros fiscais versionados** (§0) — fundação; sem isto nenhum motor calcula. (Bloqueia tudo.)
2. **Cadastro Imobiliário (BCI)** + vínculo ao `Contribuinte` (§1.1) — CIB opcional, sem envio SINTER.
3. **Motor Valor Venal (PGV)** + **motor IPTU** (lançamento anual + alíquotas) (§1.2/§1.3).
4. **Arrecadação `Dam`/`Parcela`** + parcelamento + código de barras/linha digitável + PIX cobv (§5) — habilita pagar o IPTU; reuso por todas as espécies.
5. **CNAB 240 remessa/retorno + conciliação** → publica `ReceitaArrecadadaIntegrationEvent` (§5).
6. **Apuração ISS** (`ApuradorIss`) sobre `NotaFiscalServico` existente + **eventos NFS-e** + Livro Eletrônico (§2.2/§2.1 eventos).
7. **Evolução do ingestor ADN para cursor NSU/DF-e + mTLS** (§2.1) — quando obtido o Manual ADN /municipios.
8. **ITBI** (guia avulsa, base declarada/Tema 1.113) (§3.1).
9. **Taxas + Alvará/TLL** (§3.2/§3.3).
10. **COSIP** (§3.4) e **Contribuição de Melhoria** com workflow de impugnação (§3.5).
11. **Dívida Ativa: completar CDA (art. 2º §5º) + SubstituirCda versionada + relógio de prescrição** (§4) — substitui o `EstaPrescrita` ingênuo.
12. **Protesto CRA-RS** (remessa/retorno) (§4) — quando obtido o leiaute IEPTB-RS.
13. **Execução fiscal / export PJe-eproc-RS** (§4) — se for escopo (decisão de produto).
14. **Camada Reforma Tributária ISS→IBS** (`TransicaoIssIbs`) (§2.3) — preparada para 2029, sem alterar apuração 2026/2027.
15. **Certidões CND/CPEN + sigilo fiscal auditado** (§4).

> Itens **8/9/13** dependem de leiaute/contrato externo não obtido → **BAIXA confiança de fidelidade**: modelar domínio + ACL agora, mas **não cravar o adapter** sem o doc oficial (SINTER, ADN /municipios, CRA-RS, CNAB do banco do piloto).

---

## 7. Pendências documentais consolidadas (bloqueadores de fidelidade — §16)

1. **CTM de Maximiliano de Almeida/RS** + lei PGV + decreto anual IPTU + lei COSIP + leis de taxas/TLL — popula TODOS os parâmetros do tenant (sem isto o motor classifica mas não calcula valores).
2. **Manual técnico SINTER / convênio ENAT / NT CTAT 05/2025** — envio cadastral urbano + DV do CIB (CIB opcional até jan/2027).
3. **Manual de APIs do ADN família `/municipios`** + leiaute XSD NFS-e + manual de eventos + layout/DV da chave 50 díg. + perfil do certificado (mTLS).
4. **LC 214/2025 + atos do Comitê Gestor do IBS** — percentuais oficiais de transição ISS↔IBS 2029–2032.
5. **Leiaute CRA-RS/IEPTB-RS** (posições, ocorrências, endpoint, convênio) — não generalizar de SP.
6. **Art. 40 LEF pós-Lei 14.195/2021 (Planalto) + REsp 1.340.553/RS (Tema 566) inteiro teor** — relógio de prescrição.
7. **Banco arrecadador + nº convênio + versão CNAB + credenciais PIX (PSP) do piloto.**
8. **Decisão de produto:** integração de saída PJe/eproc-RS na execução fiscal é escopo do M6?
9. **Texto literal Planalto:** CTN arts. 33/35/38/42/77–82/142/148/174/198/201/202/205–206; CF 156 §2º/149-A; LEF art. 2º §5º/§8º; Lei 9.492/97; LC 227/2026/EC 132/2023 (ITBI).

---

### Resumo (≤15 linhas)
1. Fundação obrigatória: **parâmetros fiscais versionados por tenant+vigência** — zero números fiscais no código (§16). Tudo o mais lê deles.
2. **Cadastro Imobiliário (BCI)** novo agregado, 3 identificadores (Inscrição obrigatória, **CIB e Matrícula opcionais**); envio SINTER **não** agora (piloto obrigado só jan/2027). [ALTA; envio BAIXA]
3. **Motor IPTU:** `CalculadoraValorVenal` lê `TabelaPgv`; alíquotas progressivas; Súmula 160/STJ (decreto só corrige); valor de referência SINTER é informativo, não base. [ALTA]
4. **ISS:** evoluir ingestor ADN para **cursor NSU/DF-e + mTLS**, dedup pela **chave inteira** (não parsear DV); `ApuradorIss` (próprio/retido/substituição LC 116); Livro = read model. Reforma ISS→IBS como camada parametrizável p/ 2029. [ALTA; leiautes/percentuais MÉDIA/BAIXA]
5. **ITBI** base **declarada** (Tema 1.113/STJ, nunca PGV unilateral); **Taxa/Alvará=TLL** (CTN 80+SV 29); **COSIP** sui generis (RE 573.675); **Contrib. Melhoria** exige edital+impugnação ≥30d (CTN 82). [ALTA]
6. **Dívida Ativa:** completar CDA (LEF 2º §5º; nulidade), `SubstituirCda` versionada (Súm. 392 — não troca devedor/fundamento), **relógio de prescrição** real (art. 174 + LEF 40 + Tema 566 + Lei 14.195/2021), protesto CRA-RS. [ALTA; leiaute CRA-RS BAIXA]
7. **Arrecadação:** `Dam`/`Parcela`, **44 dados/48 linha digitável** (corrige README), PIX cobv, CNAB 240 conciliação → publica `ReceitaArrecadadaIntegrationEvent`→Finanças. [ALTA; convênio do piloto pendente]
8. **Ordem:** parâmetros → BCI → IPTU → DAM/arrecadação → CNAB → ISS → ITBI → taxas/alvará → COSIP/melhoria → DA/CDA/prescrição → protesto → IBS → certidões.
9. **Bloqueadores documentais:** CTM do piloto, Manual ADN /municipios, leiaute SINTER e CRA-RS, LC 214/2025 (transição), convênio/CNAB do banco — sem eles, modelar domínio+ACL mas não cravar adapters.

**Caminho do arquivo:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/architecture/m6-prep/M6-DESIGN.md`

# REQUISITOS-GOVTECH — Tensorroot.Gov (sistema de PRODUÇÃO)

> Síntese consolidada dos diagnósticos `partes/govtech-*.md`. ERP GovTech SaaS multi-tenant para Prefeituras e Câmaras do Brasil. Piloto: **Maximiliano de Almeida/RS** (jurisdição **TCE-RS**).
> Maiores preocupações do dono: **CONTABILIDADE** e **PRESTAÇÃO DE CONTAS AO TCE**.
> Data da síntese: 2026-06-22.
>
> **AVISO DE VERSIONAMENTO:** todos os leiautes de governo (TCE-RS SIAPC/PAD, SICONFI/MSC, eSocial, NFS-e Nacional, DATASUS, INEP, FNDE, PNCP, Rede SUAS) são **versionados por exercício/competência**. Prazos e versões são **dados configuráveis por tenant/exercício — NUNCA hard-coded** (CLAUDE.md §7). Itens marcados **[a confirmar]** exigem validação em fonte oficial antes de implementar.

---

## RESUMO EXECUTIVO (10 linhas)

1. A **prioridade absoluta** é o eixo CONTABILIDADE→TCE: motor PCASP/MCASP determinístico (partidas dobradas, ciclo empenho→liquidação→pagamento→restos a pagar da Lei 4.320), 7 demonstrações DCASP automáticas, e remessas que batem 1:1 com o **PAD/e-Validador (TCE-RS)** e o **SICONFI/MSC (STN)**. Nada de plano de contas ou roteiro de lançamento "caseiro" — tudo prescrito por norma.
2. A **MSC (Matriz de Saldos Contábeis)** é a fonte da verdade: dela o SICONFI deriva RREO (bimestral), RGF (quadrimestral) e DCA (anual). Produzir MSC fiel ao PCASP destrava todo o resto.
3. Remessas TCE-RS são **arquivos texto largura fixa, 1 registro/linha, CR/LF, sem packed/binário** (Manual SIAPC Vol. IV-V), com **Código de Remessa** único; sem passar no PAD não há envio.
4. **Tributos** muda de regime: NFS-e Nacional via **ADN obrigatória desde 01/01/2026** (no nosso escopo, ingestão **passiva** via Worker NfseSync); Dívida Ativa/CDA (CTN 202-203) com protesto (Lei 9.492) e execução fiscal (Lei 6.830); Reforma Tributária EC 132/2023 (ISS→IBS até 2033, destaque-teste CBS/IBS em 2026).
5. **RH** gira em torno do **eSocial S-1.3** (S-1202/S-1207 para RPPS, S-1200 para celetistas, S-1299 fecha a folha) + ponto Portaria MTP 671/2021 (AFD/AEJ). Folha alimenta a remessa de folha do TCE-RS (Res. 1099/2018).
6. **Saúde** e **Educação** carregam **risco fiscal direto**: dado clínico/escolar de baixa qualidade = perda de repasse federal (SISAB/cofinanciamento APS, SIOPE bimestral, FUNDEB, PNAE, PNATE; suspensão por 3 meses sem envio — Port. 708/2007). Convergência para RNDS (FHIR) e Educacenso/INEP.
7. **Compras/Administração** = Lei 14.133/2021 com **publicidade no PNCP como condição de eficácia** do contrato (bloquear execução financeira sem nº de controle PNCP); integra Finanças e TCE.
8. **Assistência Social** = Rede SUAS (CadÚnico/CECAD, Prontuário Eletrônico, RMA, Censo SUAS, SUASWeb) com cofinanciamento condicionado ao envio; **CPF/NIS** como chave do cidadão.
9. **Legislativo** (proposições/sessões/votação, LexML, prestação de contas art. 29-A) e **Protocolo** (processo eletrônico, assinatura Lei 14.063 em 3 níveis, e-ARQ/SIGAD, temporalidade CONARQ, e-SIC/LAI).
10. Transversal: **LGPD** (dados sensíveis Saúde/Assistência, DPO, RIPD, trilha de acesso), assinatura **ICP-Brasil A1** server-side (PAdES/XAdES/CAdES), acessibilidade **gov.br DS + eMAG + WCAG 2.1 AA**, hospedagem em datacenter nacional (ISO 27001/17/18/701; GSI IN 5/2021 e 8/2025), e **auditoria imutável** para o TCE.

---

## ⭐ DOMÍNIO PRIORITÁRIO — CONTABILIDADE PÚBLICA e PRESTAÇÃO DE CONTAS AO TCE (módulos Financas + Transparencia)

### Fundamento legal (não inventável)
| Norma | Objeto |
|---|---|
| **Lei 4.320/1964** | Orçamento, estágios da despesa/receita, restos a pagar, demonstrações (arts. 101-105) |
| **LC 101/2000 (LRF)** + LC 131/2009 + LC 156/2016 | RREO, RGF, limites de pessoal/dívida, transparência em tempo real |
| **MCASP 11ª ed. (vigência jan/2025)** + **PCASP** | Manual e Plano de Contas Aplicados ao Setor Público (STN) |
| **Portaria STN 896/2017** | Institui MSC e leiautes RREO/RGF/DCA/MSC no SICONFI |
| **Portaria STN 642 (Regras Gerais MSC, anexo anual)** | Regras da Matriz de Saldos Contábeis |
| **Portaria STN 438/2012** | Atualiza anexos da Lei 4.320 |
| **Res. TCE-RS 1134/2020** | Prazos/documentos das contas anuais e ordinárias municipais |
| **Res. TCE-RS 1099/2018** | Envio mensal de folha de pagamento ao TCE-RS |
| **IN TCE-RS 8/2025 (23/09/2025)** | Revoga IN 5/2024; novas regras dos relatórios fiscais via PAD/SIAPC |
| **NBC TSP (CFC)** | Convergência IPSAS |

### Motor contábil (o coração do produto)
- **PCASP oficial** carregado (8 classes: 1 Ativo, 2 Passivo+PL, 3 VPD, 4 VPA, 5 e 6 controles orçamentários, 7 e 8 controles), com atributos (natureza da informação, indicador superávit F/P) e **regras de integridade**. Versionável por exercício. Proibido plano de contas próprio.
- **Partidas dobradas** sempre fechadas; lançamento simultâneo nos 3 enfoques (orçamentário + patrimonial + controle) a cada evento.
- **Roteiros de contabilização do MCASP** por evento (empenho, liquidação, pagamento, anulações, RP processados/não processados, receita prevista→lançada→arrecadada→recolhida). Partida inventada = MSC inválida = reprovação.
- **Ciclo da despesa (Lei 4.320 arts. 58-65):** Empenho (vedada despesa sem prévio empenho, art. 60) → Liquidação (com documentos: NF, contrato, medição) → Pagamento → **Restos a Pagar** (processados/não processados, inscrição/baixa em 31/12).
- **DCASP — 7 demonstrações automáticas (MCASP Parte V):** Balanço Orçamentário, Balanço Financeiro, Balanço Patrimonial, DVP, DMPL, DFC, Notas Explicativas.

### Remessas TCE-RS (SIAPC / PAD / e-Validador) — a maior preocupação do dono
- **SIAPC** recebe execução orçamentária/financeira/contábil/patrimonial via **RDI (Relatório de Dados e Informações)** com flags (Folha, Receita, etc.).
- **PAD (Programa Autenticador de Dados)** valida e autentica antes do envio. Versão **25.0.0.0 (2025)** → versionamento anual (26.x para 2026). **Sem passar no validador, a remessa não é aceita.**
- **Formato (CRÍTICO):** arquivos texto **largura fixa**, **1 registro por linha**, **todas as linhas do mesmo tamanho**, terminadas em **CR/LF**. **Proibido** packed decimal/zoned/binário/float. Conteúdo **acumulado** de 1º/jan à data de referência. **Código de Remessa** único em todos os cabeçalhos. Ex.: `TCE_4810.TXT` (folha).
- Leiautes: **Resumo Leiaute Dados à Disposição (MT Vol. V)** e **Elenco de Contas Padrão (MT Vol. IV)**.

### Remessas SICONFI (STN) — consolidação nacional
- **MSC Agregada (mensal)** → gera RREO e RGF; **MSC de Encerramento (anual)** → gera DCA.
- Registros: conta PCASP, saldo, natureza da informação, indicador F/P + informações complementares (correlação receita/despesa, função/subfunção) das Regras Gerais MSC do exercício. Carga via XBRL/arquivo + API SICONFI. Falha de envio impede transferências voluntárias.

### Prazos contábeis/fiscais
| Obrigação | Periodicidade | Prazo |
|---|---|---|
| Remessa SIAPC/PAD (TCE-RS) | Mensal | até **30 dias corridos** após o mês (desde jan/2019) |
| Folha de pagamento (TCE-RS, Res. 1099/2018) | Mensal | até 30 dias corridos após o mês |
| SICOE (obras) | Trimestral | último dia útil do mês seguinte ao trimestre [a confirmar] |
| Contas Anuais/Ordinárias (Res. 1134/2020) | Anual | conforme exercício [a confirmar datas] |
| MSC Agregada (SICONFI) | Mensal | último dia do mês seguinte |
| RREO | Bimestral | 30 dias após o bimestre |
| RGF | Quadrimestral (municípios; semestral facultativo <50k hab.) | 30 dias após o quadrimestre |
| DCA | Anual | ~30/abril do exercício seguinte [a confirmar] |
| Transparência execução orçamentária (Dec. 7.185/2010) | Tempo real | até 1º dia útil subsequente ao registro |

---

## TRIBUTOS (Executivo)

**Escopo:** IPTU (Cadastro Imobiliário/CIB, PGV, valor venal, lançamento em lote, carnê/cota única), ITBI (base = valor da transação, STF Tema 1.124; integração cartórios), ISS/ISSQN (Cadastro Mobiliário, LC 116/2003, retenção/substituição, Simples), Taxas/COSIP, Alvarás, Contribuição de Melhoria. Conta Corrente Fiscal, DAM com código de barras FEBRABAN + PIX QRCode, retorno bancário CNAB 240/400, parcelamentos/REFIS, juros/multa/SELIC, prescrição/decadência.

**NFS-e Nacional (ADN) — obrigatória desde 01/01/2026.** No escopo Tensorroot.Gov a integração é **PASSIVA** (CLAUDE.md §8): o Worker `NfseSync` baixa diariamente os XMLs do ADN/Receita Federal, deduplica por chave de acesso, alimenta painel fiscal e Dívida Ativa. **Nós NÃO emitimos nem assinamos NFS-e.** API JSON / DF-e em XML assinado; autenticação por certificado ICP-Brasil (mTLS). Schemas NFSe-ESQUEMAS_XSD v1.01 (fev/2026). NBS2 sucede a lista LC 116.

**Dívida Ativa / CDA / Protesto:** CDA com requisitos obrigatórios do **CTN arts. 202-203** (omissão = nulidade); presunção de certeza/liquidez (Lei 6.830/80). Protesto extrajudicial (Lei 9.492/97 + Lei 12.767/2012; STF ADI 5.135 constitucional; STJ dispensa lei local) via **CRA/IEPTB** (remessa/retorno). Execução fiscal (LEF). CND/CPEN.

**Reforma Tributária (EC 132/2023 + LC 214/2025):** 2026 teste (destacar CBS 0,9% / IBS 0,1% informativo); 2027 CBS+IS, fim PIS/Cofins; 2029-2032 transição IBS; **2033 extinção de ISS → IBS**. Suportar coexistência ISS↔IBS. IPTU/ITBI permanecem municipais.

**Integrações:** ADN, bancos arrecadadores (FEBRABAN/PIX/CNAB), CRA/cartórios protesto, TCE-RS (receita alimenta relatórios fiscais), SICONFI/SINTER/SERPRO, cartórios de imóveis (ITBI), Simples Nacional (PGDAS-D/DAF607), PJe.

---

## RECURSOS HUMANOS (Ambos)

**eSocial — obrigatório para entes públicos. Versão S-1.3.** Particularidade-chave do ente: **S-1202** (remuneração estatutário/RPPS) vs **S-1200** (celetista/comissionado/temporário RGPS); **S-1207** + S-2400/2405/2410/2416 para benefícios RPPS (aposentadorias/pensões). Ordem: S-1000 → tabelas → S-2xxx → periódicos → **S-1299** (fecha a competência). XML assinado ICP-Brasil contra XSD; WebService com ambientes Produção e Produção Restrita.

**Ponto eletrônico — Portaria MTP 671/2021:** >20 trabalhadores celetistas obrigados (estatutários por lei municipal [a confirmar]). REP-C (INMETRO) / REP-A / REP-P (INPI). Arquivos **AFD** (assinado) e **AEJ** obrigatórios; AFDT/ACJEF eliminados. Imutabilidade das marcações.

**RPPS:** S-1202/S-1207 alimentam apuração previdenciária; correlação CADPREV/DRPPS [a confirmar layouts].

**Prazos:** periódicos até **dia 15 do mês seguinte** (antes do S-1299); 13º até 20/dez; S-2200 admissão até dia anterior ao início [a confirmar]; S-2299 desligamento até 10 dias [a confirmar]. Folha reconciliável com Finanças/Contabilidade → remessa folha TCE-RS.

**Integrações:** eSocial WebService, certificado A1/A3 ICP-Brasil, REP/ponto, CADPREV/DRPPS, TCE-RS.

---

## SAÚDE (Executivo) — risco fiscal: dado clínico = receita

**Arquitetura dupla:** regime legado/batch DATASUS (e-SUS APS→SISAB, CNES/SCNES, SIA/SIH, SISREG, BNAFAR) + regime moderno **RNDS (HL7 FHIR R4)**. Nascer **FHIR-first**, legados como adaptadores.

- **e-SUS APS → SISAB:** produção da APS (LEDI/Thrift, fichas CDS/PEC). Insumo direto do cofinanciamento.
- **RNDS:** barramento FHIR via HTTPS + certificado ICP-Brasil (e-CNPJ); eventos de imunização, exames, dispensação, sumário de alta.
- **SI-PNI (imunização):** desde 01/06/2023 integrado à RNDS — enviar evento de imunização (FHIR) direto.
- **Farmácia → BNAFAR:** **Portaria GM/MS 11.585 (16/06/2026)** substitui Hórus pelo **e-SUS AF** (janela de 180 dias). Mirar e-SUS AF/RNDS, não Hórus.
- **CADSUS/CNS** (chave de cidadão), **CNES/SCNES** (estabelecimento/profissional), **SIA/SIH** (BPA/APAC/AIH).

**Financiamento (FNS):** cofinanciamento APS pela **Portaria GM/MS 3.493/2024** (revogou Previne Brasil); 15 indicadores de qualidade via SISAB (2º quadr. 2025); **Port. 708/2007: falta de envio de CNES/SIA/SIHD por 3 meses → suspensão imediata de repasse.** Painel de cofinanciamento + monitor de obrigatoriedade são argumento de venda.

**Prazos:** CNES/SIA/SIHD mensal (~5º dia útil); APS/SISAB contínuo+avaliação quadrimestral; imunização contínua; BNAFAR mensal (migração 180 dias). [cronograma DATASUS a confirmar]

---

## EDUCAÇÃO (Executivo) — risco fiscal: descumprimento suspende repasses

- **Educacenso/Censo Escolar (INEP):** anual, 2 etapas (1ª matrícula 27/05-31/07/2026; 2ª situação do aluno 01/02-12/03/2027). Fonte de verdade do FUNDEB/PNAE/PNATE; gestores respondem solidariamente. Layout do migrador INEP [a confirmar].
- **SIOPE (FNDE):** **bimestral**, até 30 dias após o bimestre (6º = 30/01). Validação pelo Secretário + Presidente CACS/FUNDEB (SIOPE-MAVS). Conciliação com PCASP/MSC. **Não envio → suspensão de transferências voluntárias.**
- **PNAE (merenda):** prestação de contas SiGPC (~15/02, prorrogável até 30/04); parecer CAE no Sigecon (~45 dias); mín. **30% agricultura familiar** (Lei 11.947/2009).
- **PNATE (transporte rural, Lei 10.880/2004):** mensal no SiGPC + consolidação anual; parecer CACS/FUNDEB.
- **BNCC** (currículo; BNCC Computação obrigatória desde 2026).

**Integração de maior risco:** gastos exportados ao SIOPE conciliados com empenho/liquidação/pagamento (Finanças/PCASP) e mínimos constitucionais MDE (art. 212 CF)/FUNDEB → TCE-RS.

---

## ADMINISTRAÇÃO — COMPRAS/LICITAÇÕES (Ambos)

**Lei 14.133/2021** (regime único desde 30/12/2023). **PNCP é obrigatório e condição de eficácia:** editais, atas de registro de preços, contratos/aditivos, contratações diretas. **Sem nº de controle PNCP confirmado, bloquear execução financeira** (empenho/pagamento). API REST/JSON UTF-8 + **JWT (expira em 1h)**; sem entrada manual. **SICAF** (níveis I-IV) para fornecedores (cadastro próprio equivalente aceito). **PCA** (Plano de Contratações Anual) divulgado no PNCP.

**Prazos eficácia:** ~20 dias úteis (licitação) / 10 dias úteis (contratação direta) [a confirmar literal art. 94]. Contrato/empenho em Administracao dispara publicação PNCP e alimenta Finanças → remessa TCE-RS. Persistir nº de controle PNCP como chave de rastreabilidade.

---

## ASSISTÊNCIA SOCIAL (Executivo)

**Rede SUAS (MDS):** **CadÚnico (novo sistema Dataprev; CPF passa a ser a chave, NIS permanece)** + **CECAD 2.0**; **Prontuário Eletrônico SUAS** (nome+NIS); **RMA** (Registro Mensal de Atendimentos CRAS/CREAS); **Censo SUAS** (anual, abertura ~16/out — não preencher suspende cofinanciamento); **SUASWeb** (Plano de Ação + Demonstrativo Sintético, aprovação do Conselho); **SISC** (SCFV); **CadSUAS** (pré-requisito). Maioria é preenchimento manual no portal MDS → priorizar **importação/exportação por layout** + consulta CadÚnico/CECAD. **CPF/NIS** é a chave do cidadão.

**Prazos:** RMA mensal; Censo SUAS anual (~16/out); SUASWeb anual; CadÚnico atualização ~2 anos ou mudança [a confirmar]; SISC periódico. Layouts de exportação município→MDS [a confirmar].

---

## LEGISLATIVO (Câmara)

Ciclo completo de **proposições** (PL, PLC, PEC/LOM, decreto legislativo, resolução, requerimento, moção, emendas, vetos) com tramitação; **sessões** (ordinárias/extra/solenes, pauta, ata, presença, quórum); **votações** (nominal/simbólica/secreta, quóruns); **base de leis consolidada** (LC 95/1998, texto articulado); **transparência legislativa** (LAI, art. 29-A CF, duodécimo); **e-Democracia**; **sessões remotas/híbridas**. Padrão **LexML** (URN persistente, XML); benchmark/migração **SAPL (Interlegis)**. Atas em **PDF/A**; dados abertos CSV/JSON. Prestação de contas ao TCE-RS (folha vereadores/servidores, despesas, limites art. 29-A) [layout Legislativo a confirmar]. Prazos regimentais **parametrizáveis por tenant**.

---

## PROTOCOLO / PROCESSO ADMINISTRATIVO ELETRÔNICO (Ambos)

Protocolo eletrônico com nº único, autuação, juntada, tramitação, encerramento e trilha (Lei 9.784/1999). **Assinatura eletrônica — Lei 14.063/2020 (3 níveis):** Simples (gov.br bronze), Avançada (gov.br prata/ouro ou cert. não-ICP), **Qualificada (ICP-Brasil, presunção legal)** — parametrizar nível mínimo por tipo de ato. **GED/SIGAD conforme e-ARQ Brasil (CONARQ, v2 2022)**; classificação + **Tabela de Temporalidade e Destinação (TTDD)** (CONARQ, Lei 8.159/1991, Portaria AN/MGI 174/2024); **preservação PDF/A + RDC-Arq** (Res. CONARQ 39/2014 e 43/2015); cadeia de custódia. **e-SIC/LAI** (resposta 20 dias + 10 prorrogáveis). Interoperabilidade e-PING.

---

## TRANSVERSAL — PLATAFORMA & COMPLIANCE

- **LGPD (Lei 13.709/2018):** base legal própria do Poder Público (arts. 23-30); dados sensíveis Saúde/Assistência/menores (art. 11); **DPO** por tenant (art. 41); **RIPD** para grande volume sensível; direitos do titular (art. 18); ROPA (art. 37); comunicação de incidente à ANPD [Res. CD/ANPD 15/2024 a confirmar]; **trilha de acesso a dados sensíveis** (quem leu o quê, quando, por quê).
- **Assinatura ICP-Brasil A1 server-side** (PAdES/XAdES/CAdES + carimbo de tempo), renovação anual; certificados só no **Azure Key Vault** por tenant (eSocial, remessas TCE, Protocolo).
- **Acessibilidade:** **gov.br Design System + eMAG (Portaria SLTI/MP 3/2007) + WCAG 2.1 AA** (Dec. 5.296/2004, LBI 13.146/2015). Padrão visual único e obrigatório.
- **Transparência:** Portal por tenant (LAI 12.527/2011, LRF/LC 131/2009, Dec. 7.185/2010), execução orçamentária em tempo real, dados abertos CSV/JSON, retenção mínima 5 anos.
- **Hospedagem/segurança:** datacenter nacional, **ISO 27001/27017/27018/27701/22237**; GSI/PR **IN 5/2021 e IN 8/2025**; criptografia repouso/trânsito, MFA, KMS/HSM, SIEM, backup/DR.
- **Multi-tenant + auditoria imutável** (CLAUDE.md §5-6): isolamento por TenantId, Global Query Filter, AuditSaveChangesInterceptor (antes/depois, usuário, IP, timestamp) — exigência do TCE. Executivo e Legislativo do mesmo município são tenants distintos (CNPJs distintos).

---

## REGRA DE OURO (riscos de produção)
Nada de plano de contas próprio, roteiro de lançamento caseiro, formato de remessa improvisado, prazo ou versão de leiaute hard-coded. Tudo é prescrito por norma e versionado por exercício; usar versão errada = remessa rejeitada pelo PAD/SICONFI e contas reprovadas. Em dúvida sobre regra fiscal/legal: parar e pesquisar fonte oficial (Planalto, gov.br, STN, TCE-RS), não inventar.

---

## FONTES
As URLs oficiais completas (Planalto, STN/SICONFI, TCE-RS, gov.br/nfse, eSocial, DATASUS, INEP/FNDE, MDS, PNCP, CONARQ, ANPD, GSI/PR) estão nos 8 arquivos de origem em `docs/diagnostico/partes/govtech-*.md`, na seção "Fontes/URLs" de cada um. Validar sempre a versão do exercício vigente antes de gerar qualquer remessa.

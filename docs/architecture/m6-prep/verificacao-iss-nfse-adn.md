# Verificação adversarial — pesquisa-iss-nfse-adn.md (M6 Tributos)

> Auditoria cética de cada afirmação factual de `pesquisa-iss-nfse-adn.md` contra **fonte oficial**.
> Classificação: **CONFIRMADO** (fonte oficial/normativa) · **PLAUSÍVEL-SEM-FONTE** (fonte secundária coerente, sem doc oficial verificado) · **INCERTO** (não confirmado, impreciso ou contraditório).
> Regras CLAUDE.md §8 (NFS-e PASSIVA) e §16 (nada hardcoded; tudo com fonte ou parametrizável).
> Data da verificação: 2026-06-22.

---

## Quadro-resumo

| # | Afirmação da pesquisa | Veredito | Fonte de verificação |
|---|---|---|---|
| §0 | NFS-e nasce/assinada no Ambiente Nacional; município é consumidor passivo (não-emissão) | **CONFIRMADO** | gov.br/nfse; alinhado a CLAUDE.md §8 e ADR-0003 |
| §1 | Base URLs ADN produção/produção restrita; famílias `/municipios` e `/contribuintes` | **PLAUSÍVEL-SEM-FONTE** | URLs não re-validadas individualmente (gov.br/nfse cita os portais); manuais oficiais em PDF não parseáveis via fetch |
| §2 | Distribuição incremental por **NSU**; `GET /DFe/{NSU}` retorna NFS-e + eventos | **CONFIRMADO** (conceito) | notagateway + manual contribuintes gov.br/nfse |
| §2 | **Até 50 DF-e por lote** por requisição | **INCERTO** | fonte secundária não confirma o número; manual oficial PDF não parseável — exige extração |
| §2 | Consulta unitária `GET /nfse/{chave}`; XML **GZip + Base64** | **CONFIRMADO** | notagateway + busca gov.br: NFS-e em XML compactado GZip, codificado Base64, assinado |
| §2 | **Autenticação por certificado digital + credenciamento/escopo** | **CONFIRMADO** | mTLS + ICP-Brasil A1/A3, e-CNPJ/e-aplicação; credenciamento no portal federal |
| §3 | **Chave de acesso = 50 dígitos**, identificador nacional único | **CONFIRMADO** | gov.br + espiaonfe + TOTVS (50 posições) |
| §3 | **Layout campo-a-campo** da chave (Cód. Mun. IBGE 7 + ... + CNPJ 14 + Nº NFS-e 13 + ... + DV 1) | **INCERTO / PROVÁVEL-INCORRETO** | fonte divergente aponta layout diferente — ver §"Discrepância 1" |
| §3 | Eventos: **cancelamento** e **substituição** (substituta referencia substituída) | **PLAUSÍVEL-SEM-FONTE** | conceito padrão DF-e; lista/códigos completos exigem manual de eventos |
| §4 | Base legal: **LC 116/2003** + lei municipal (alíquotas/responsáveis) | **CONFIRMADO** | LC 116/2003 (Planalto) |
| §4 | **ISS próprio**: regra geral município do estabelecimento prestador (exceções LC 116) | **CONFIRMADO** | LC 116/2003 art. 3º |
| §4 | **ISS retido** pelo tomador em hipóteses da LC 116 (constr. civil, vigilância, limpeza) | **CONFIRMADO** | LC 116/2003 art. 6º §2º, II (lista de subitens) |
| §4 | **Substituição** é **faculdade** municipal ("poderão", art. 6º) — não automática | **CONFIRMADO** | LC 116/2003 art. 6º caput ("poderão atribuir... mediante lei") |
| §5 | Livro Eletrônico = read model derivado da ingestão ADN | **PLAUSÍVEL-SEM-FONTE** | decisão de arquitetura; obrigação acessória municipal a confirmar |
| §6 | EC 132/2023 + LC 214/2025 (IBS, CBS, IS) | **CONFIRMADO** | EC 132/2023; LC 214/2025 |
| §6 | **2026**: teste **CBS 0,9% + IBS 0,1%**; ISS inalterado | **CONFIRMADO** | ADCT art. 125 (EC 132): IBS 0,1% e CBS 0,9% em 2026, compensáveis |
| §6 | **2027**: CBS plena; extinção PIS/COFINS; IPI→0 | **CONFIRMADO** | cronograma EC 132/LC 214 (cobrança efetiva CBS+IS em 2027) |
| §6 | Tabela "**IBS ~10%/20%/30%/40%**" em 2029–2032 | **INCERTO / IMPRECISO** | mistura redução do ISS com alíquota IBS — ver §"Discrepância 2" |
| §6 | **2033: ISS extinto**; IBS pleno | **CONFIRMADO** | EC 132/LC 214: extinção ICMS/ISS em 2033 |
| §6 | **Comitê Gestor do IBS**: 54 membros (27 estados/DF + 27 municípios) | **CONFIRMADO** | cgibs.gov.br (Conselho Superior, 54 representantes) |
| §6 | Transição federativa **2033–2078**; base **média ICMS+ISS 2019–2026** | **PARCIAL** | janela **2019–2026 CONFIRMADA**; período de retenção é **2029–2077** (100% destino só em 2078), não "2033–2078" — ver §"Discrepância 3" |
| §6 | **Alíquota de referência** revisada pelo Senado; **split payment** | **CONFIRMADO** | EC 132/LC 214 (alíquota de referência; split payment / pagamento fracionado) |

---

## Discrepâncias materiais (corrigir antes de codar)

### Discrepância 1 — Layout da chave de acesso de 50 dígitos (§3)
A pesquisa afirma: Cód. Mun. IBGE (7) + Ambiente (1) + Tipo inscr. (1) + CNPJ/CPF (14) + Nº NFS-e (13) + AAMM (4) + Cód. numérico (9) + DV (1) = 50.

Fonte de verificação (espiaonfe, corroborada por TOTVS Datasul — "50 posições") aponta layout **diferente**:
- Código (UF/município) **2 ou 7?** — fonte secundária cita "2 primeiros = cUF"; a doc cita "7 = município IBGE";
- Inscrição federal: **11 dígitos** (CPF com zeros à esquerda) na fonte vs. **14** na doc;
- Nº NFS-e: **8** na fonte vs. **13** na doc;
- DV: **2** na fonte vs. **1** na doc.

Ambas somam 50 por caminhos distintos → **não dá para confiar em nenhuma das duas sem o manual técnico XSD oficial.** A própria pesquisa já marcou `[a confirmar]`, mas o layout publicado **não deve ser usado para gerar/validar DV ou parsear a chave**. **Ação:** extrair largura/ordem/algoritmo do DV do leiaute oficial (gov.br/nfse, anexos XSD) antes de implementar dedup por componentes. Para dedup, usar a **chave inteira como string** (não depende do layout) — seguro.

### Discrepância 2 — "IBS ~10%/20%/30%/40%" em 2029–2032 (§6, tabela)
**Impreciso.** O mecanismo legal (LC 214/2025 + fontes) é: de 2029 a 2032 as **alíquotas de ICMS e ISS são REDUZIDAS** — para **90%/80%/70%/60%** do valor (i.e., reduções de 10/20/30/40%). O IBS cresce em ritmo equivalente para neutralidade, **mas a fração de IBS não é literalmente "10%/20%/30%/40%"** — esses números são a **redução do imposto antigo**, não a alíquota do IBS. A tabela conflate os dois conceitos. **Ação:** reescrever como "ISS a 90/80/70/60% + IBS complementar (alíquota de referência fixada pelo Senado)". Parametrizar por competência: fator de redução do ISS por ano + alíquota IBS vigente — nunca os "~%" da tabela.

### Discrepância 3 — Período da transição federativa (§6)
A pesquisa diz "transição federativa **(2033–2078)**". A regra (corroborada por CNM/CTAT e fontes da Câmara): a **retenção para transição federativa ocorre de 2029 a 2077** (2029–2032: 80%; 2033: 90%; 2034–2077: 90% reduzido à razão de 1/45 a.a.); **100% por destino só em 2078**; há **seguro-receita de 5% (2029–2077)**. O intervalo "2033–2078" da doc está **deslocado**. **A janela da média de referência 2019–2026 está CONFIRMADA** (receita média de ISS — incl. dívida ativa e Simples — e parcela de ICMS, 8 anos de 2019 a 2026). **Ação:** corrigir o período para **2029–2077 (destino pleno em 2078)**.

---

## Itens que EXIGEM lei municipal / doc oficial (bloqueadores §16)

1. **Código Tributário Municipal de Maximiliano de Almeida/RS** — alíquotas por item da lista LC 116, hipóteses locais de retenção, lista de substitutos, leiaute/periodicidade do Livro Eletrônico. **Sem isto, o motor de apuração não calcula nada — só classifica.** (parametrizável por tenant.)
2. **Leiaute XSD oficial da NFS-e nacional + manual de eventos** — layout/DV da chave de acesso (Discrepância 1), códigos de evento (cancelamento/substituição), campos de retenção/substituição/item da lista/local de incidência.
3. **Manual de APIs do ADN — família `/municipios`** — nº exato de DF-e por lote (Discrepância: "50" não confirmado), nomes dos recursos, cursor/paginação por NSU, contrato dos eventos.
4. **LC 214/2025 + atos do Comitê Gestor do IBS** — fator de redução do ISS por ano e alíquota de referência IBS (Discrepância 2); tabela de transição por competência como parâmetro do tenant.

---

## Resposta direta

**Confirmados (≈14):** não-emissão/passiva; distribuição por NSU + `GET /DFe/{NSU}`; XML GZip+Base64; autenticação mTLS/ICP-Brasil e-CNPJ; chave de 50 dígitos (existência); LC 116/2003 como base; ISS próprio (art. 3º), ISS retido (art. 6º §2º), substituição como faculdade (art. 6º caput); EC 132 + LC 214; teste 2026 CBS 0,9%+IBS 0,1% (ADCT art. 125); CBS plena 2027; ISS extinto 2033; Comitê Gestor 54 membros (27+27); janela 2019–2026 da média de referência; alíquota de referência (Senado) + split payment.

**Incertos / a corrigir (≈4):** (a) **layout campo-a-campo + DV da chave de 50 díg.** — fontes divergem, usar string inteira para dedup e obter XSD oficial; (b) **"até 50 DF-e por lote"** — não confirmado em fonte oficial; (c) **tabela "IBS ~10/20/30/40%" 2029–2032** — imprecisa (são reduções do ISS, não alíquota IBS); (d) **período "2033–2078" da transição federativa** — correto é **2029–2077, destino pleno em 2078**.

**3 riscos principais:**
1. **Parsear/validar a chave de acesso pelo layout publicado** (Discrepância 1) — alto risco de DV/campos errados; mitigação: dedup por string completa `(TenantId, ChaveAcesso)` e só decompor após XSD oficial.
2. **Hardcodar percentuais de transição ISS↔IBS** (Discrepância 2/3) — viola §16 e produz cálculo errado de carga/repartição; mitigação: tabela temporal por competência + alíquota de referência, 100% parametrizável por tenant.
3. **Calibrar lote/cursor de NSU pelo "50"** não-oficial — risco de perda/duplicação de DF-e na sync; mitigação: tratar tamanho de lote como configurável, cursor de NSU idempotente, e validar contra o Manual ADN /municipios antes do go-live.

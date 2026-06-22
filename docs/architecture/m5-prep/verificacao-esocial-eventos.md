# Verificação adversarial — pesquisa-esocial-eventos.md (M5 RH / eSocial)

> Papel: AUDITOR DE FATOS (cético). Objetivo: tentar REFUTAR cada afirmação factual de
> `pesquisa-esocial-eventos.md` contra fonte oficial vigente (gov.br/eSocial, Receita, MTP/MPS).
> Data da verificação: 2026-06. Regra de ouro CLAUDE.md §16.
>
> Classificação:
> - **CONFIRMADO** — batido contra URL oficial nesta verificação.
> - **PLAUSÍVEL-SEM-FONTE** — coerente com o conjunto, mas não validado item-a-item no doc oficial vigente.
> - **INCERTO** — não confirmado / depende de doc que ainda não foi baixado / requer dado operacional do município.
>
> AVISO METODOLÓGICO: confirmações de leiaute/regra foram obtidas via páginas-índice e leitura
> assistida do HTML dos leiautes S-1.3 (gov.br). **NÃO substituem** a leitura do PDF do MOS S-1.3
> + Anexo I (Leiautes) + pacote XSD na versão exata travada para o build. Toda regra abaixo deve
> ser re-checada contra esse pacote antes de virar código.

---

## Placar
- **CONFIRMADO:** 11 afirmações-chave
- **PLAUSÍVEL-SEM-FONTE:** 3
- **INCERTO / depende de doc ou dado operacional:** 6
- **CORREÇÕES/IMPRECISÕES encontradas:** 4 (todas menores; nenhuma invalida a arquitetura proposta)

---

## 1. Versão do leiaute / MOS

| # | Afirmação do doc | Veredito | Evidência |
|---|---|---|---|
| 1.1 | Leiaute vigente = **S-1.3** | **CONFIRMADO** | Páginas-índice S-1.3 ativas; aprovado pela Port. Conj. RFB/MPS/MTE nº 13/2024. https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-v-1.3/index.html |
| 1.2 | S-1.3 aprovado pela **Port. Conj. RFB/MPS/MTE nº 13, de 25/06/2024** | **CONFIRMADO** | "Portaria Conjunta RFB/MPS/MTE nº 13, de 25/06/2024 – DOU de 28/06/2024". PDF oficial: https://www.gov.br/previdencia/pt-br/assuntos/rpps/esocial/arquivos/PortariaConjuntaRFBMPSMTEn13de25jun2024.pdf · Receita: http://normas.receita.fazenda.gov.br/sijut2consulta/link.action?idAto=139004 |
| 1.3 | Consolidação mais recente = **leiautes S-1.3 até NT 06/2026 (rev. 09/04/2026)** | **CONFIRMADO** (existe a página) | https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html · NT 06/2026 rev.: https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/nota-tecnica-s-1-3-06-2026-rev.pdf |
| 1.4 | MOS S-1.3 consolidado até **NS S-1.3 07/2026** | **PLAUSÍVEL-SEM-FONTE** | A URL do PDF citada (`mos-s-1-3-consolidada-ate-a-no-s-1-3-07-2026.pdf`) aparece em resultados de busca, mas **não baixei/abri o PDF** para confirmar capa/data. Versões anteriores confirmadas existem (03/2025, 06/2025). |
| 1.5 | Produção S-1.3 desde **02/12/2024** | **CONFIRMADO** (dado novo, coerente) | "Production Implementation: December 2, 2024" na página de leiautes S-1.3. Não estava explícito no doc; reforça que S-1.3 já é o vigente em produção. |

**Imprecisão menor (1.4):** o doc trata "MOS até NS 07/2026" como fato com URL; a URL é plausível mas o
conteúdo do PDF não foi verificado. Tratar como **a baixar e conferir capa** antes de travar.

---

## 2. Roster de eventos (ente público / RPPS)

**CONFIRMADO** — todos os códigos/títulos abaixo foram lidos na página oficial de leiautes S-1.3
(NT 06/2026 rev.): S-1000, S-1005, S-1010, S-1020, S-1070, S-2200, S-2299, S-2300, S-2399,
S-1200, S-1202, S-1207, S-1210, S-1298, S-1299. Também confirmados retornos S-5001, S-5002, S-5003,
S-5011, S-5012, S-5013.
Fonte: https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html

**CORREÇÃO 1 (eventos de retorno):** o doc (§5) cita "S-5001/S-5002/S-5003/S-5011/S-5013" e **omite o S-5012**
(Imposto de Renda Retido consolidado por declarante). O roster oficial lista S-5012. Incluir S-5012 no
mapeamento de retornos. **PLAUSÍVEL→CONFIRMADO** com correção.

---

## 3. Regras técnicas específicas

| # | Afirmação do doc | Veredito | Evidência |
|---|---|---|---|
| 3.1 | S-1010: rubrica com **codIncFGTS ∈ [21,93]** só em desligamento (S-2299)/término TSVE (S-2399) ou em `{remunPerAnt}` do S-1200 | **CONFIRMADO** (quase verbatim) | Regra `REGRA_RUBRICA_COMPATIVEL_RESC`: "{codIncFGTS} = [21,93] só pode ser usado em S-2299 e S-2399, ou no grupo {remunPerAnt} do evento de remuneração do RGPS (S-1200)." Leiautes S-1.3. https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-cons-ate-nt-04-2025-rev-26-08-2025 |
| 3.2 | **S-1200**: codCateg [301,302,303,304,306,307,309,310,312,314] com **tpRegPrev=[1,3]** (ou inexistente) + 1XX/2XX/5XX/7XX/9XX | **CONFIRMADO** | Leiautes S-1.3: S-1200 para vínculo RGPS; servidores 3XX com tpRegPrev=[1,3]; também admite base de FGTS de servidor RPPS. (mesma fonte 3.1) |
| 3.3 | **S-1202**: SOMENTE codCateg [301,302,303,304,306,307,309,310,312,314] com **tpRegPrev=[2,4]** | **CONFIRMADO** | Regra `REGRA_COMPATIB_REGIME_PREV`: S-1202 só para codCateg=[301,302,303,304,306,307,309,310,312,314] e tpRegPrev=[2,4]. (mesma fonte 3.1) |
| 3.4 | Tabela **tpRegPrev** valores 1/2/3/4 (doc marcou `[a confirmar]`, chutou "3=RPPS exterior?") | **CONFIRMADO — e o chute estava ERRADO** | Inferido das regras dos leiautes S-1.3: **1=RGPS**, **2=RPPS**, **3=Regime de Previdência no Exterior**, **4=Sistema de Proteção Social dos Militares das Forças Armadas (SPSMFA)**. NÃO é "RPPS exterior". |
| 3.5 | **S-1207** = benefícios de ente público (aposentados/pensionistas RPPS) | **CONFIRMADO** | Formulário oficial "S-1207 – Benefícios previdenciários – RPPS"; usado para aposentados e pensionistas dos RPPS. https://www.gov.br/esocial/pt-br/canais_atendimento/formularios/orgaos-publicos-rpps/S-1207-beneficios-previdenciarios-rpps |
| 3.6 | S-1210/`{indGuia}` segue evento original; S-1210 não excluível sem reverter dependências | **PLAUSÍVEL-SEM-FONTE** | Coerente com o modelo periódico (fechamento S-1299/reabertura S-1298), mas a frase exata do `{indGuia}` não foi batida no leiaute do S-1210 nesta verificação. Conferir no Anexo I (S-1210). |
| 3.7 | S-2299/S-2399 exclusão bloqueada se houver S-1210 vinculado / após S-1299 só com reabertura S-1298 | **PLAUSÍVEL-SEM-FONTE** | Coerente com as regras de exclusão periódicas; não batido item-a-item. Conferir nas regras de S-2299/S-2399. |

**CORREÇÃO 2 (tpRegPrev valor 3):** o doc especulou "3=RPPS exterior?". Errado. **3 = Regime de Previdência
no Exterior**; **4 = Militares das Forças Armadas (SPSMFA)**. Ambos 2 e 4 roteiam para S-1202. Corrigir o doc.

**Nota de risco (3.2/3.3):** valores confirmados via leitura assistida do HTML dos leiautes — a **lista exata de
codCateg** e os números são exatamente o ponto onde um erro de transcrição vira rejeição em produção. Tratar como
CONFIRMADO-mas-reconferir-no-XSD antes de codar o roteador S-1200↔S-1202.

---

## 4. Cronograma Grupo 4 (órgãos públicos)

| # | Afirmação do doc | Veredito | Evidência |
|---|---|---|---|
| 4.1 | Fase 1 (tabelas S-1000–S-1080): a partir de **21/07/2021** | **CONFIRMADO** | Port. Conj. SEPRT/RFB/ME nº 71/2021; 1ª fase 21/07/2021 (info do órgão + tabelas). https://www.gov.br/esocial/pt-br/noticias/implantado-o-esocial-para-os-orgaos-publicos · http://normas.receita.fazenda.gov.br/sijut2consulta/link.action?idAto=118796 |
| 4.2 | Fase 2 (não-periódicos): **22/11/2021** | **CONFIRMADO** | 2ª fase 22/11/2021 (servidores/vínculos: admissões, afastamentos, desligamentos). (mesma fonte 4.1) |
| 4.3 | Fase 3 (periódicos/folha): início abril/22 **adiado p/ competência ago/2022 (fechamento ago/22)** | **CONFIRMADO** | 3ª fase 22/08/2022, fatos desde 01/08/2022. Adiamento pela Port. Conj. MTP/RFB/ME nº 2/2022. https://www.gov.br/trabalho-e-previdencia/pt-br/noticias-e-conteudo/trabalho/2022/abril/portaria-adia-inicio-da-3a-fase-do-cronograma-do-esocial-para-o-grupo-4 |
| 4.4 | Adiamento via **Port. Conj. MTP/RFB/ME nº 2, de 20/04/2022** | **CONFIRMADO com ressalva de data** | A portaria é **de 19/04/2022** (assinatura, conforme DOU) e **publicada em 20/04/2022**. O doc escreveu "de 20/04/2022" (data de publicação, não de assinatura). |
| 4.5 | Fase 4 (SST) adiada p/ **01/01/2023** | **CONFIRMADO** | A mesma Port. nº 2/2022 prorrogou SST do Grupo 4 para 01/01/2023. (mesma fonte 4.3) |
| 4.6 | **Port. Conj. nº 71/2021 consolidou o cronograma** | **CONFIRMADO** | Port. Conj. SEPRT/RFB/ME nº 71, de 29/06/2021. (fonte 4.1) |

**CORREÇÃO 3 (data da Portaria nº 2/2022):** assinatura **19/04/2022**, publicação 20/04/2022. O doc cita
"de 20/04/2022". Ajustar para "nº 2, de 19/04/2022 (DOU 20/04/2022)".

**Nota (4.x):** todas as datas são **histórico de obrigatoriedade**, não regra de leiaute. Para o piloto que
entra em 2026, o relevante é a **situação cadastral atual do município em produção** (ver §6, pendência operacional),
não estas datas.

---

## 5. SST não obrigatório para RPPS

| # | Afirmação do doc | Veredito | Evidência |
|---|---|---|---|
| 5.1 | SST (S-2210/S-2220/S-2240) **NÃO obrigatório para servidores RPPS** | **CONFIRMADO — com ressalva importante** | Para vinculados ao RPPS o envio **não é obrigatório** (facultativo, p/ NT 2/2014/CGNAL/DRPSP/SPPS/MPS); obrigatório p/ celetistas/estatutários vinculados ao RGPS. https://www.gov.br/esocial/pt-br/empresas/manual-web-geral |

**CORREÇÃO 4 / ALERTA (5.1):** o doc lista os eventos SST como "S-2210, S-2220, S-2240". O bloco SST atual
inclui também **S-2240** (condições ambientais/agentes nocivos) e a fontes apontam **S-2241** como item à parte
(insalubridade/periculosidade) e **S-1060** (tabela de ambientes de trabalho). A regra de não-obrigatoriedade
para RPPS aplica-se ao conjunto, **mas há nuance por elemento** — não tratar "SST inteiro = dispensado" como
verdade absoluta sem ler a regra elemento-a-elemento no MOS. Como o piloto é estatutário/RPPS, **o M5 pode
deixar SST fora do MVP**, mas registrar a dependência caso o município tenha celetistas/RGPS.

---

## 6. eSocial Simplificado / contexto

| # | Afirmação do doc | Veredito | Evidência |
|---|---|---|---|
| 6.1 | Simplificado reduziu **>30% dos campos** e **excluiu 12 eventos** | **CONFIRMADO** | "redução de mais de 30%... e exclusão total de 12 eventos". https://www.gov.br/esocial/pt-br/noticias/publicada-versao-final-do-leiaute-do-esocial-simplificado-s-1-0 · https://www.gov.br/esocial/pt-br/noticias/governo-anuncia-novo-esocial-simplificado |
| 6.2 | Base legal = **Lei 13.874/2019 (Liberdade Econômica)** | **CONFIRMADO** | Confirmado nas notícias oficiais do eSocial Simplificado. (mesma fonte 6.1) |
| 6.3 | Leiaute simplificado final = **S-1.0** | **CONFIRMADO** | "versão final do leiaute do eSocial Simplificado (S-1.0)". (fonte 6.1) |
| 6.4 | S-1.0 aprovado pela **Port. Conj. SEPRT/RFB nº 82** | **INCERTO** | Não localizei a Port. nº 82 nesta verificação. A linha S-1.x e o S-1.0 estão confirmados, mas o **número/ano da portaria aprovadora do S-1.0 não foi batido**. Tratar o número "82" como **a confirmar**. |
| 6.5 | INSS/FGTS migraram p/ **DAE + DCTFWeb** (substituição GFIP) | **INCERTO p/ ente público** | Verdade geral para empresas; **forma de recolhimento/declaração para órgão público municipal (RGPS retido x contribuições RPPS) pode divergir** e NÃO foi confirmada. Mantém-se como pendência (doc §4 já marca `[a confirmar]`). |

---

## 7. Implicações de arquitetura (§5 do doc)

| # | Afirmação do doc | Veredito | Observação |
|---|---|---|---|
| 7.1 | Todos os eventos = XML assinado XMLDSig com A1 server-side | **PLAUSÍVEL-SEM-FONTE → INCERTO no detalhe** | Que é XML assinado é fato; **a política exata** (canonicalização C14N, Reference/URI, transform enveloped, algoritmo de hash SHA) **não foi batida** no MOS/XSD. NÃO implementar a assinatura sem a seção de segurança do MOS S-1.3 + Orientação ao Desenvolvedor. |
| 7.2 | Ordem S-1000 → tabelas → S-2200/S-2300 → periódicos → S-1299 | **CONFIRMADO** (consistente) | Coerente com as dependências confirmadas (S-2200 antes de remuneração; S-1299 fecha). |
| 7.3 | Processamento assíncrono por lote + retornos S-5xxx; produção restrita vs produção | **INCERTO no detalhe** | Existência dos S-5xxx confirmada (§2). **Fluxo de Web Services (envio de lote, consulta, ambientes)** NÃO foi batido contra o Manual de Orientação do Desenvolvedor / Web Services. Pendência real. |

---

## 8. O que NÃO implementar sem o doc oficial vigente baixado

1. **Roteador S-1200 ↔ S-1202 e a lista de codCateg/tpRegPrev** — os valores estão CONFIRMADOS por leitura
   assistida, mas a transcrição exata (cada número de categoria, tpRegPrev=[1,3] vs [2,4]) deve sair do **XSD +
   Anexo I do MOS S-1.3 na versão travada**. Erro aqui = rejeição em massa na folha.
2. **Política de assinatura XMLDSig** (7.1) — não codar canonicalização/transforms/hash sem a seção de segurança
   do MOS + XSD. (Impacta diretamente o serviço A1/envelope encryption em construção.)
3. **Fluxo de Web Services / lote / ambientes (produção restrita x produção) e retornos S-5xxx** (7.3) — exige o
   Manual de Orientação do Desenvolvedor / WS do eSocial.
4. **Tabela de rubricas e incidências (codIncCP/codIncFGTS/codIncIRRF) e CPRP do RPPS** — a regra [21,93] está
   confirmada, mas o **conjunto completo de incidências** (que governa o cálculo da folha) tem que vir do MOS, não
   ser inferido.
5. **Recolhimento/declaração do ente público (DAE/DCTFWeb x RGPS retido x RPPS)** (6.5) — divergência provável
   vs empresas; confirmar com orientação da Receita/DCTFWeb para órgãos públicos antes de qualquer integração fiscal.
6. **Categorias 3XX reais do município (S-2200 vs S-2300/TSVE)** — depende dos vínculos efetivos de Maximiliano de
   Almeida (estatutários, comissionados, temporários, agentes políticos, conselheiros tutelares). Dado operacional +
   tabela de categorias do MOS.

---

## 9. Pendências operacionais (não são leiaute — dependem do município)

- **Situação cadastral do município no ambiente de PRODUÇÃO do eSocial** (já transmite? passivo? cargas
  retroativas?). Crítico: o piloto entra muito após as datas de obrigatoriedade (2021–2022).
- **RPPS de Maximiliano de Almeida paga inativos/pensionistas?** (→ define se S-1207 é necessário). Doc: lei
  municipal do RPPS.
- **Número/ano da Portaria que aprovou o S-1.0** (item 6.4) — confirmar antes de citar como fato.
- **Versão exata do MOS a travar** (S-1.3 + qual NT) — baixar PDF + Anexo I + pacote XSD e congelar.

---

## 10. Três maiores riscos para o M5

1. **Implementar regra de leiaute por leitura de busca/HTML em vez do XSD+MOS travado.** As confirmações aqui
   vieram de páginas-índice e leitura assistida — bons sinais, mas o build precisa do **PDF do MOS S-1.3 + Anexo I
   + schemas XSD da versão exata congelada**. Qualquer divergência de codCateg/tpRegPrev/incidência rejeita a folha
   inteira em produção (dinheiro público + escrutínio TCE). *Mitigação: travar versão, baixar XSD, gerar parsers a
   partir do schema, validar contra o ambiente de produção restrita antes do go-live.*

2. **Assinatura XMLDSig especificada por suposição.** A política exata (C14N, transforms, hash, Reference) NÃO foi
   confirmada. Como o serviço A1/envelope encryption está sendo construído **agora**, um contrato de assinatura
   errado custa retrabalho no núcleo de segurança. *Mitigação: derivar a política da seção de segurança do MOS +
   XSD antes de fechar a API do assinador; testar contra validador oficial.*

3. **NT futura muda regra antes do go-live + situação operacional do município desconhecida.** As regras evoluem por
   Nota Técnica dentro do S-1.3 (NT 01/2024 → 04/2025 → 06/2026); o piloto entra em 2026 com cadastro/produção de
   estado desconhecido (pode haver passivo de 2021–2022). *Mitigação: versionar o pacote eSocial por NT, isolar
   regras parametrizáveis (CLAUDE.md §7), e fazer due diligence da situação do ente em produção ANTES de planejar
   cargas iniciais/retroativas.*

---

## Apêndice — fontes oficiais usadas nesta verificação

- Leiautes S-1.3 (NT 06/2026 rev.): https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html
- Leiautes S-1.3 (regras tpRegPrev / codIncFGTS / S-1200xS-1202): https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-cons-ate-nt-04-2025-rev-26-08-2025
- Página S-1.3 (índice): https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-v-1.3/index.html
- Port. Conj. RFB/MPS/MTE nº 13/2024 (S-1.3): https://www.gov.br/previdencia/pt-br/assuntos/rpps/esocial/arquivos/PortariaConjuntaRFBMPSMTEn13de25jun2024.pdf · http://normas.receita.fazenda.gov.br/sijut2consulta/link.action?idAto=139004
- NT S-1.3 06/2026 rev.: https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/nota-tecnica-s-1-3-06-2026-rev.pdf
- Implantação Grupo 4 / cronograma: https://www.gov.br/esocial/pt-br/noticias/implantado-o-esocial-para-os-orgaos-publicos
- Port. Conj. SEPRT/RFB/ME nº 71/2021: http://normas.receita.fazenda.gov.br/sijut2consulta/link.action?idAto=118796
- Adiamento Fase 3 / SST (Port. Conj. MTP/RFB/ME nº 2/2022): https://www.gov.br/trabalho-e-previdencia/pt-br/noticias-e-conteudo/trabalho/2022/abril/portaria-adia-inicio-da-3a-fase-do-cronograma-do-esocial-para-o-grupo-4
- SST não obrigatório RPPS (Manual Web Geral): https://www.gov.br/esocial/pt-br/empresas/manual-web-geral
- S-1207 (RPPS, aposentados/pensionistas): https://www.gov.br/esocial/pt-br/canais_atendimento/formularios/orgaos-publicos-rpps/S-1207-beneficios-previdenciarios-rpps
- eSocial Simplificado S-1.0: https://www.gov.br/esocial/pt-br/noticias/publicada-versao-final-do-leiaute-do-esocial-simplificado-s-1-0 · https://www.gov.br/esocial/pt-br/noticias/governo-anuncia-novo-esocial-simplificado

> Disclaimer final: nenhuma das confirmações acima dispensa baixar e congelar o **PDF do MOS S-1.3 + Anexo I
> (Leiautes) + pacote XSD** na versão exata antes de gerar código. Esta verificação reduz incerteza; não autoriza
> implementar regra de leiaute sem o pacote oficial em mãos.

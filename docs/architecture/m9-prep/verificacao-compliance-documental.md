# M9 — Verificação Cética de Compliance Documental

> Auditoria adversarial da pesquisa `pesquisa-compliance-documental.md`.
> Cada afirmação foi reconferida contra **fonte oficial** (Planalto, ITI/repositorio.iti.gov.br, gov.br/conarq, projeto.lexml.gov.br, Câmara/Senado).
> Classificação: **CONFIRMADO** (fonte oficial bate literalmente) · **PLAUSÍVEL** (convergência de fontes secundárias/oficial parcial, sem contradição) · **INCERTO** (não confirmado, desatualizado ou contraditório).
> Data da verificação: 2026-06-22. Disciplina §16.

---

## Placar

- **CONFIRMADO:** 16
- **PLAUSÍVEL:** 4
- **INCERTO / CORRIGIDO:** 5

A pesquisa base é majoritariamente sólida. Foram encontrados **3 erros materiais** (versão DOC-ICP-12 desatualizada; Portaria 47 revogada; vigência diferida do art. 29-A omitida) e **2 imprecisões de redação** (faixa populacional inciso II; escopo do §3º). Nenhum erro invalida a arquitetura proposta, mas três afetam diretamente o que será *hard-coded* / citado na UI.

---

## 1. Carimbo do tempo (ACT / ICP-Brasil)

| # | Afirmação da pesquisa | Verificação | Status |
|---|---|---|---|
| 1.1 | ACT = entidade credenciada pela AC-Raiz; carimbo prova existência de informação em data/hora | Confirmado em gov.br/iti (Autoridades de Carimbo do Tempo) | **CONFIRMADO** |
| 1.2 | Base normativa: RFC 3161 (ago/2001), RFC 3628 (nov/2003), ETSI TS 101861 | Confirmado: Res. 172/2020 e DPCTs das ACTs citam RFC 3161 + RFC 3628; datas batem | **CONFIRMADO** |
| 1.3 | DOC-ICP-12 aprovado pela **Resolução ITI 172/2020, versão 2.0** | **DESATUALIZADO.** A versão vigente é **2.1**, aprovada pela **Resolução CG ICP-BRASIL nº 188, de 18/05/2021** (`repositorio.iti.gov.br/resolucoes/Resolucao188_DOC-ICP-12_v.2.1.htm`). A Res. 188 *altera* (não revoga) o documento da Res. 172. Citar v2.0/Res.172 como "principal/atual" está incorreto. | **INCERTO / CORRIGIDO** |
| 1.4 | TSQ/TSR/TST; falhas via `PKIFailureInfo` (RFC 3161 §2.4.2) | Coerente com RFC 3161; não reconferido item a item no DOC-ICP-12 v2.1 | **PLAUSÍVEL** |
| 1.5 | ACT deve ter **HSM** com relógio + funções cripto | Confirmado (ITI / DPCTs ACT) | **CONFIRMADO** |
| 1.6 | Uso do carimbo é **facultativo** na ICP-Brasil | Confirmado: "A utilização de carimbos do tempo no âmbito da ICP-Brasil é facultativa" (ITI) | **CONFIRMADO** |
| 1.7 | Lista de ACTs credenciadas + datas (CAIXA 2013, SERPRO 2013, CERTISIGN/VALID/BRY 2014, …) | Datas das ACTs até 2014 confirmadas na página oficial do ITI. SOLUTI(2019)/PRODESP(2021) plausíveis (PRODESP tem PCT publicado) mas não reconferidos na lista atual | **PLAUSÍVEL** |

---

## 2. Assinatura eletrônica (Lei 14.063/2020)

| # | Afirmação da pesquisa | Verificação | Status |
|---|---|---|---|
| 2.1 | Três níveis: simples / avançada / qualificada | Confirmado (Lei 14.063/2020) | **CONFIRMADO** |
| 2.2 | Simples = identifica signatário, interações de menor impacto sem sigilo | Confirmado | **CONFIRMADO** |
| 2.3 | Avançada = certificado não-ICP ou outro meio, aceito pelas partes | Confirmado | **CONFIRMADO** |
| 2.4 | Qualificada = certificado ICP-Brasil (MP 2.200-2/2001) | Confirmado | **CONFIRMADO** |
| 2.5 | Decreto 10.543/2020 regulamenta níveis mínimos com entes públicos | Confirmado; regulamenta o **art. 5º** da Lei 14.063 (Planalto/CONARQ). Atenção: o Decreto é de **âmbito federal** (admin. pública federal) — município o usa como referência, não como vinculação direta | **CONFIRMADO** |
| 2.6 | Cada ente define o nível mínimo por ato do titular | Confirmado (art. 5º) | **CONFIRMADO** |
| 2.7 | Qualificada **admitida em qualquer interação**, sem cadastro prévio | Confirmado (art. 5º, §1º) | **CONFIRMADO** |
| 2.8 | Qualificada **obrigatória** para chefes de Poder, Ministros, titulares de órgão constitucionalmente autônomo | Confirmado (art. 5º, §2º). Complemento relevante omitido: a qualificada também é **obrigatória para atos de transferência/registro de bens imóveis** (art. 5º, §2º, IV) — relevante para Patrimônio/ITBI | **CONFIRMADO** |
| 2.9 | Numeração exata de incisos do art. 4º/5º marcada como `[a confirmar]` | §5º estruturado em §§ (não só incisos); o nível mínimo obrigatório está em **§2º** e a admissão geral em **§1º**. Conferir numeração fina direto no Planalto antes de hard-coding em UI | **PLAUSÍVEL** |

---

## 3. GED + Temporalidade (CONARQ / e-ARQ Brasil)

| # | Afirmação da pesquisa | Verificação | Status |
|---|---|---|---|
| 3.1 | CONARQ vinculado ao Arquivo Nacional, Lei 8.159/1991 | Confirmado | **CONFIRMADO** |
| 3.2 | TTD define prazos de guarda + destinação final | Confirmado (Arquivo Nacional) | **CONFIRMADO** |
| 3.3 | Código de Classificação + TTD atividades-meio aprovados pela **Portaria nº 47** (Arq. Nacional, 14/02/2020) | **REVOGADO.** A Portaria 47/2020 foi **substituída pela Portaria AN/MGI nº 174, de 23/09/2024** (revoga a 47). Usar a Portaria 47 como base de TTD está desatualizado — deve-se partir da **Portaria 174/2024**. | **INCERTO / CORRIGIDO** |
| 3.4 | Programa de gestão único (Res. CONARQ 20/2004) digital + não-digital | Confirmado | **CONFIRMADO** |
| 3.5 | e-ARQ Brasil v2 (SIGAD) aprovado pela **Resolução CONARQ 50, de 06/05/2022** | Confirmado (gov.br/conarq + PDF EARQV203MAI2022) | **CONFIRMADO** |
| 3.6 | Núcleo SIGAD: Organização/Classificação, Captura, Avaliação, Segurança, Preservação | Confirmado (lista bate literalmente com a Res. 50) | **CONFIRMADO** |
| 3.7 | Presunção de autenticidade: **Resolução CONARQ 37/2012** | **Data imprecisa.** A Res. 37 é de **19/12/2012**, não 19/04/2012 (a pesquisa escreveu "19/04/2012"). Conteúdo (integridade + identidade + cadeia de custódia) confirmado | **INCERTO / CORRIGIDO** (data) |
| 3.8 | e-ARQ v2 tem ~225 requisitos/metadados | Plausível (título "225" aparece no PDF oficial); lista completa não extraída — segue `[a confirmar]` legítimo | **PLAUSÍVEL** |

---

## 4. Legislativo — LexML

| # | Afirmação da pesquisa | Verificação | Status |
|---|---|---|---|
| 4.1 | LexML padrão aberto, 3 esferas + 3 Poderes, lançado 30/06/2009, recomendação e-PING | Confirmado (projeto.lexml.gov.br/institucional/historia) | **CONFIRMADO** |
| 4.2 | URN LexML = identificador unívoco e persistente | Confirmado | **CONFIRMADO** |
| 4.3 | Normas em XML Schema (Parte 3) | Confirmado; complemento: o schema é adaptação de **Akoma Ntoso 1.0 + Norme in Rete 2.0** para a técnica legislativa brasileira (LCP 95/1998) | **CONFIRMADO** |
| 4.4 | API de pesquisa via URL com resultado XML; acervo em dados abertos do Senado | Confirmado | **CONFIRMADO** |
| 4.5 | Versão do XML Schema vigente = `[a confirmar]` (encontrado v1.0 RC1/2009) | Confirmado que **continua v1.0 RC1 (2009)** — não há revisão posterior publicada no portal. O `[a confirmar]` resolve-se: a v1.0 RC1 **é** a versão de referência atual | **CONFIRMADO** |

---

## 5. Legislativo — Art. 29-A CF (limite de despesa da Câmara)

| # | Afirmação da pesquisa | Verificação | Status |
|---|---|---|---|
| 5.1 | Art. 29-A incluído pela EC 25/2000, alterado por EC 58/2009 e EC 109/2021 | Confirmado | **CONFIRMADO** |
| 5.2 | Caput EC 109/2021 inclui "subsídios dos Vereadores **e os demais gastos com pessoal inativo e pensionistas**" | Confirmado literalmente (texto atualizado EC 109 na Câmara/Planalto). **Mas a pesquisa apresenta caput de forma ambígua** (mistura redação original "excluídos inativos" com a nova). Redação vigente: inclui inativos e pensionistas | **CONFIRMADO** (com ressalva de redação) |
| 5.3 | **Vigência da nova regra de inativos** | **OMITIDO NA PESQUISA — RISCO.** EC 109/2021, **art. 7º**: a alteração do art. 29-A só vigora "**a partir do início da primeira legislatura municipal após a publicação**" (15/03/2021) → **legislatura iniciada em 2025**. A inclusão de inativos/pensionistas no cálculo **não** valia para 2021-2024. Crítico para o motor de cálculo histórico | **INCERTO / CORRIGIDO** |
| 5.4 | Percentuais por faixa (7/6/5/4,5/4/3,5%) | Confirmado quanto aos percentuais. **Imprecisão na faixa do inciso II:** texto constitucional diz **"de 100.000 a 300.000"** (não "100.001"); inciso I = "até 100.000". Há sobreposição literal em 100.000 no texto da CF — a pesquisa "corrigiu" para 100.001 sem fonte. Usar o texto literal e tratar a borda como regra de negócio explícita | **PLAUSÍVEL** (percentuais OK; borda imprecisa) |
| 5.5 | §1º — folha ≤ 70% da receita (incluído subsídio de vereadores) | Confirmado literalmente | **CONFIRMADO** |
| 5.6 | §2º — crime de responsabilidade do **Prefeito** (repasse acima/menor/fora do prazo) | Confirmado | **CONFIRMADO** |
| 5.7 | §3º — crime de responsabilidade do **Presidente da Câmara** ("gastar mais que o repasse, desrespeitar §1º, etc.") | **IMPRECISO.** O §3º trata especificamente do **desrespeito ao §1º** (limite de 70% de folha). O texto literal é mais restrito que o "etc." da pesquisa. Conferir incisos do §3º no Planalto antes de citar em UI | **INCERTO / CORRIGIDO** (escopo) |
| 5.8 | Base de cálculo = receita tributária + transferências (art. 153 §5º, 158, 159), exercício anterior | Confirmado | **CONFIRMADO** |

---

## 6. Pendências `[a confirmar]` da pesquisa — situação após verificação

1. **Lei 14.063 incisos art. 4º/5º** → estrutura confirmada (obrigatoriedade em §2º, admissão em §1º); numeração fina ainda a conferir no DOM Planalto. **Parcialmente resolvido.**
2. **Art. 29-A incisos/§§** → percentuais e §§1º-3º confirmados; **borda do inciso II e escopo do §3º corrigidos**; **vigência diferida (EC 109 art. 7º) adicionada.** **Resolvido com correções.**
3. **e-ARQ v2 metadados (~225)** → segue aberto (extração do PDF). **Não resolvido (legítimo).**
4. **TTD municipal atividades-fim** → segue aberto; base federal mudou de Portaria 47 para **Portaria 174/2024**. **Atualizado.**
5. **LexML XML Schema** → **resolvido**: v1.0 RC1 (2009) é a versão de referência; lineage Akoma Ntoso/Norme in Rete.
6. **ACT a contratar** → segue aberto (decisão comercial). **Não resolvido (legítimo).**
7. **Decreto 10.543 níveis por interação** → confirmado escopo federal; detalhamento por tipo segue aberto. **Parcialmente resolvido.**

---

## 7. Top 3 riscos para o M9

1. **Art. 29-A com vigência diferida (EC 109/2021, art. 7º) — RISCO ALTO.** A inclusão de inativos/pensionistas no teto só vale a partir da legislatura de 2025. Um motor de cálculo que aplicar a nova base a exercícios 2021-2024 produzirá **resultado errado de compliance fiscal** sob escrutínio do TCE-RS. Exige **regra temporal parametrizada** (base de cálculo varia por exercício/legislatura), não constante única. Combinar com a borda imprecisa da faixa de 100.000 hab. (inciso I vs II).

2. **Instrumentos arquivísticos desatualizados na pesquisa — RISCO MÉDIO/ALTO.** Portaria 47/2020 foi **revogada** pela Portaria AN/MGI 174/2024, e o DOC-ICP-12 está em **v2.1 (Res. 188/2021)**, não v2.0. Construir TTD-base ou cliente de carimbo do tempo sobre a versão antiga gera dívida de compliance. Antes de codar §1/§3 do M9, **fixar as versões vigentes** (Portaria 174/2024, DOC-ICP-12 v2.1, e-ARQ v2/Res.50). Reforça §16: confirmar layout/versão atual de cada integração.

3. **Textos legais citados em UI sem reconferência literal no Planalto — RISCO MÉDIO.** Persistem `[a confirmar]` legítimos (numeração fina art. 4º/5º Lei 14.063; incisos do §3º art. 29-A; metadados e-ARQ). O fetch ao Planalto falhou repetidamente nesta e na sessão anterior (socket closed) — qualquer string legal exibida ao cidadão/gestor ou usada como label de regra deve ser validada contra o texto consolidado oficial **antes do release**, não derivada de fontes secundárias convergentes.

---

## Fontes oficiais consultadas nesta verificação

- ITI — Res. 188/2021 DOC-ICP-12 v2.1: https://repositorio.iti.gov.br/resolucoes/Resolucao188_DOC-ICP-12_v.2.1.htm
- ITI — Autoridades de Carimbo do Tempo: https://www.gov.br/iti/pt-br/assuntos/icp-brasil/autoridades-de-carimbo-do-tempo
- Lei 14.063/2020 (Planalto): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14063.htm
- Decreto 10.543/2020 (Planalto): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/decreto/D10543.htm
- EC 109/2021 (Câmara, texto atualizado): https://www2.camara.leg.br/legin/fed/emecon/2021/emendaconstitucional-109-15-marco-2021-791136-normaatualizada-pl.html
- EC 109/2021 (Planalto): https://www.planalto.gov.br/ccivil_03/constituicao/Emendas/Emc/emc109.htm
- CF art. 29-A (PCD Legal, espelho consolidado): https://www.pcdlegal.com.br/constituicaofederal/art-29-a/
- CONARQ — Res. 50/2022 (e-ARQ v2): https://www.gov.br/conarq/pt-br/legislacao-arquivistica/resolucoes-do-conarq/resolucao-no-50-de-06-de-maio-de-2022
- CONARQ — Res. 37, de 19/12/2012: https://www.gov.br/conarq/pt-br/legislacao-arquivistica/resolucoes-do-conarq/resolucao-no-37-de-19-de-dezembro-de-2012
- Arquivo Nacional — Portaria 47/2020 (revogada pela 174/2024): https://www.gov.br/conarq/pt-br/legislacao-arquivistica/portarias-federais/portaria-no-47-de-14-de-fevereiro-de-2020
- LexML — Parte 3 XML Schema v1.0 RC1: https://projeto.lexml.gov.br/documentacao/Parte-3-XML-Schema.pdf
- LexML — História: https://projeto.lexml.gov.br/institucional/historia

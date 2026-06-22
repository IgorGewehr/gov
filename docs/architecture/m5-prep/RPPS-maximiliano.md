# M5 — Regime Previdenciário do Município-Piloto: Maximiliano de Almeida/RS

> **Status:** pesquisa de fundamentação (parametrização do motor de folha — Módulo RecursosHumanos).
> **Objetivo:** confirmar se o piloto usa **RPPS** (regime próprio, alíquota por lei municipal) ou
> **RGPS** (INSS, tabela federal já implementada), conforme decisão #1 do sumário de
> `pesquisa-folha-calculo.md` (que estava marcada `[a confirmar]`).
> **Constituição §16 (REGRA DE OURO):** não inventar alíquota/regra; toda afirmação tem FONTE (URL).
> Pontos que dependem de lei municipal não localizada online ficam `[a confirmar — <documento>]`.

---

## 0. Conclusão (resposta direta)

**Maximiliano de Almeida/RS NÃO possui RPPS. O município é vinculado ao RGPS (INSS).**

Os servidores efetivos (estatutários) do município contribuem ao **INSS** — não há instituto/fundo
de previdência municipal próprio. Logo, **a folha do piloto usa a tabela progressiva do INSS, que
já está implementada/seedada** (ver `pesquisa-folha-calculo.md`, seção 1). **Nenhuma tabela de RPPS
municipal precisa ser parametrizada para o piloto.**

> Pequenos municípios do RS (Maximiliano de Almeida tem ~4,3 mil hab.) majoritariamente **não**
> instituem RPPS — a manutenção de regime próprio exige massa de servidores e estrutura atuarial
> que entes pequenos normalmente não comportam. O caso confirma esse padrão.

---

## 1. Evidências (fontes oficiais)

| # | Evidência | Força | Fonte |
|---|---|---|---|
| 1 | Parecer/ADIN do **Ministério Público do RS** afirma textualmente: *"o Município de Maximiliano de Almeida **não possui regime próprio de previdência social**, razão pela qual seus servidores estão **vinculados ao regime geral**"* (INSS). | **Primária / determinante** | MPRS — parecer ref. ADIN nº 70075229021 (doc. 87461) |
| 2 | **Lei Municipal nº 327/2008** (Estatuto dos Servidores), art. 35, V: a aposentadoria **pelo INSS** é causa de vacância do cargo — i.e., o servidor se aposenta **pelo regime geral**, não por regime próprio. | Forte (indireta) | Estatuto dos Servidores de Maximiliano de Almeida (citado no parecer MPRS) |
| 3 | Buscas por instituto/fundo previdenciário municipal (IPAM, PreviMax, "instituto de previdência" + Maximiliano de Almeida) **não retornam nenhuma entidade própria** do município. O único "IPAM" encontrado é de **Caxias do Sul** (outro ente). | Corroborativa (ausência) | Pesquisa web — site da prefeitura e LeisMunicipais |
| 4 | O **site oficial da prefeitura** não lista RPPS, instituto de previdência ou fundo previdenciário próprio em sua estrutura de governo/secretarias. | Corroborativa (ausência) | maximilianodealmeida.rs.gov.br |

### Observação sobre CADPREV/Secretaria de Previdência
A consulta pública de entes com RPPS / CRP roda no **CADPREV** (`cadprev.previdencia.gov.br/.../pesquisarEnteCrp.xhtml`),
mas é **formulário interativo** (JSF/XHTML) que não retorna resultado por fetch estático. A confirmação
veio da fonte primária (MPRS, evidência #1), que é jurídica e explícita. **[a confirmar — checagem
manual opcional no CADPREV: pesquisar UF=RS, ente "Maximiliano de Almeida"; resultado esperado =
ente NÃO consta na lista de RPPS / sem CRP de RPPS, pois é RGPS.]**

---

## 2. O que parametrizar na folha (Módulo RecursosHumanos)

**Para o piloto (Maximiliano de Almeida/RS): regime = RGPS. Já coberto. Nenhuma ação adicional de tabela.**

1. **Atributo `RegimePrevidenciario` do vínculo** = `RGPS` para os servidores do tenant-piloto.
   (Conforme `pesquisa-folha-calculo.md` decisão #1: regime é atributo **do vínculo**, não do tenant —
   o motor fail-closed exige a tabela do regime; para RGPS a tabela federal já existe.)
2. **Tabela de contribuição = INSS progressiva federal** (já seedada — `pesquisa-folha-calculo.md` §1):
   faixas 7,5% / 9% / 12% / 14% sobre as parcelas, teto 2025 = R$ 8.157,41.
   (Manter o seed da competência vigente; tabela 2026 segue o `[a confirmar]` já registrado na pesquisa de folha.)
3. **NÃO criar** seed de alíquota de RPPS municipal para este tenant. Patronal e demais incidências
   seguem RGPS/FGTS conforme natureza eSocial das rubricas.

### Para a generalização do motor (outros tenants — fora do piloto)
O motor **deve** continuar suportando RPPS por design (alíquota mín. 14% por EC 103/2019, definida em
lei municipal, em tabela versionada por `competencia`/`tenantId`). Quando um tenant com RPPS for
onboardado, será necessário obter a **lei previdenciária municipal** dele e seedar a tabela própria
— o comportamento **fail-closed** garante que, sem tabela do regime do vínculo, a folha **não calcula**
(bloqueia em vez de inventar alíquota). Isso é correto e não muda com este achado.

---

## 3. Resumo para o seed do piloto

```
Tenant: Maximiliano de Almeida/RS (Executivo)
RegimePrevidenciario (vínculos efetivos): RGPS
Tabela de contribuição do segurado: INSS (federal progressiva) — JÁ EXISTE
Tabela RPPS municipal: N/A (município não possui RPPS)
Ação pendente: nenhuma (regime já coberto pelo seed federal)
```

---

## Fontes

- Ministério Público do RS — parecer (ADIN nº 70075229021): https://www.mprs.mp.br/adins/arquivo/parecer/87461/?filename=70075229021_001.doc
- Prefeitura Municipal de Maximiliano de Almeida: https://www.maximilianodealmeida.rs.gov.br/
- Legislação municipal (LeisMunicipais): https://leismunicipais.com.br/legislacao-municipal/4108/leis-de-maximiliano-de-almeida
- CADPREV — consulta pública de CRP/entes RPPS (Secretaria de Previdência): https://cadprev.previdencia.gov.br/Cadprev/pages/publico/crp/pesquisarEnteCrp.xhtml
- Regimes Próprios de Previdência Social — Ministério da Previdência: https://www.gov.br/previdencia/pt-br/assuntos/rpps
- (Referência interna) `docs/architecture/m5-prep/pesquisa-folha-calculo.md` — seção 1 (INSS) e decisão #1 (regime do vínculo).

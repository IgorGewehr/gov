# Auditoria de Cobertura — Mapa de Prestação de Contas

> **Escopo:** cruzar `docs/planejamento/MAPA-PRESTACAO-CONTAS.md` + designs `docs/architecture/*`
> (M4/M5/M6-prep) com o que um município (Executivo, piloto Maximiliano de Almeida/RS, ≤50k hab)
> REALMENTE precisa prestar. Achados = **lacunas** (destino/obrigação não coberto) + **excessos**
> (planejado e não necessário). Fontes oficiais ao final; `[a confirmar]` = validar na fonte do exercício.
> Data: 2026-06-22.

---

## Veredito

O mapa cobre BEM o tripé que o dono priorizou (TCE-RS SIAPC/PAD + SICONFI/MSC + setoriais
Saúde/Educação/Assistência) e a folha (eSocial). **Porém faltam obrigações FEDERAIS FISCAIS do
ente como contribuinte/tomador de serviço** — não derivadas da contabilidade, e por isso passaram
despercebidas no princípio "MSC é a fonte". Essas omissões (EFD-Reinf/DCTFWeb, SADIPEM/CDP,
Transferegov) **bloqueiam transferências (CAUC) e geram multa** — risco material para o piloto.
Há **pouco over-engineering**; os excessos reais são de granularidade/escopo, não de destinos inventados.

---

## LACUNAS (obrigação/destino NÃO coberto) — por severidade

### 🔴 CRÍTICAS (bloqueiam repasse/CAUC ou geram multa; ausentes do mapa)

| ID | Lacuna | Destino | Por que é obrigatória | Periodicidade/prazo | Evidência no nosso material |
|---|---|---|---|---|---|
| L1 | **EFD-Reinf (R-2010/R-4020 etc.) + DCTFWeb** | Receita Federal (SPED) | Ente público é **tomador de serviço** e **retentor** (INSS 11% Lei 8.212/91 art. 31; IRRF/CSRF sobre NFS-e). Substituiu a **DIRF (extinta em 2024)**. EFD-Reinf alimenta a **DCTFWeb**, que **confessa o débito** — sem ela não há recolhimento regular. | Mensal, dia 15 do mês seguinte; multa R$ 500–1.500/mês | **Só `[a confirmar]` no M5-prep** (eSocial). EFD-Reinf **nem é mencionada**; DCTFWeb **não está na tabela mestra** do mapa. |
| L2 | **SADIPEM / CDP (Cadastro da Dívida Pública)** | STN (Tesouro) | **Atualização anual obrigatória** de TODA dívida do ente; também **PVL** p/ operação de crédito e **ARO**. Não atualizar → **CAUC negativo → bloqueia transferências voluntárias e novas operações de crédito**. (>4.000 municípios bloqueados por isso.) | Anual até **30/jan**; PVL/ARO por evento | **Ausente** do mapa e dos designs. O M6-prep trata de **dívida ATIVA tributária** (a receber), não da **dívida PÚBLICA** (a pagar) — coisas distintas. |
| L3 | **Transferegov.br (ex-Plataforma +Brasil / SICONV) — prestação de contas de convênios/repasses** | Gov. Federal (transferências voluntárias) | LRF exige comprovar regularidade na prestação de contas de recursos recebidos; convênio sem PC aprovada → inadimplência → sem novos repasses. | Por convênio (PC final + parciais) | Mapa: **ausente**. Plano-Mestre só tem `W9.6 Convênios` como item 🟢 "paridade Betha", **sem ligar ao destino federal Transferegov**. |

### 🟠 ALTAS (obrigação real; lacuna de mapeamento, não bloqueia repasse imediato)

| ID | Lacuna | Destino | Observação |
|---|---|---|---|
| L4 | **DCTFWeb / MIT (Módulo de Inclusão de Tributos)** como destino próprio | Receita Federal | A DCTFWeb consolida eSocial+EFD-Reinf; o mapa trata eSocial como destino-fim, mas o **fechamento fiscal** (DAE/DCTFWeb) é destino separado — o M5-prep já reconhece a dúvida `[a confirmar p/ ente público]`, mas não virou linha no mapa. |
| L5 | **Conformidade SIAFIC (Dec. 10.540/2020) — XML nº 1 SIAFIC + requisitos de qualidade** | TCE-RS (e-Contas) / contas anuais | Nosso ERP **É o SIAFIC** do ente (sistema único de execução). Há **autodeclaração de conformidade** (XML nº 1) nas contas anuais e requisitos técnicos (base única, trilha, sem 2º SIAFIC). Mapa cita Res. 1134 mas **não nomeia a obrigação SIAFIC** nem os requisitos que o PRÓPRIO sistema precisa cumprir/comprovar. |
| L6 | **DEFIS/declarações do Simples (como tomador) e e-Financeira** | Receita | `[a confirmar]` se aplica ao ente; provável **não** p/ administração direta — listar p/ descartar formalmente, não deixar buraco silencioso. |

### 🟡 MÉDIAS (refinamento / consolidação)

- L7 — **FGTS Digital** (sucessor do recolhimento FGTS via eSocial/DAE): confirmar se o ente recolhe FGTS de celetistas (cargos comissionados/temporários) → guia FGTS Digital. `[a confirmar]`
- L8 — **MANAD** (Manual Normativo de Arquivos Digitais — fiscalização previdenciária RFB): em desuso pós-eSocial, mas pode ser requisitado em fiscalização. Citar como "sob demanda" (como já se fez com AFD/AEJ). `[a confirmar se ainda exigível]`
- L9 — **RPPS / CADPREV-DAIR/DPIN/DRAA**: o mapa cita "CADPREV/DRPPS `[a confirmar]`" de forma genérica. Maximiliano: confirmar se tem **RPPS próprio** ou é **RGPS** — se RGPS, **remover** CADPREV do escopo (vira excesso); se RPPS, detalhar DAIR (investimentos)/DRAA (atuarial), que são obrigações pesadas e hoje subespecificadas.

---

## EXCESSOS (planejado e provavelmente NÃO necessário p/ o piloto) — por severidade

### 🟠 Over-scope provável

| ID | Excesso | Por que pode ser desnecessário | Recomendação |
|---|---|---|---|
| E1 | **CNAB 400 legado + layouts por banco (BB 001 / Caixa 104 / Sicredi 748)** no framework de remessas | CNAB 400 está **descontinuado** pela Febraban; bancos novos usam **240 + API/PIX**. Implementar 3 layouts proprietários antes de saber o banco do piloto é especulativo. | Manter **só CNAB 240 + PIX**; CNAB 400 e layouts por banco **sob demanda** do banco real do tenant (data-driven, não código). |
| E2 | **Emissão/assinatura XBRL-GL de instância completa para a MSC** (M4.4) | SICONFI aceita **CSV adaptado** da MSC; gerar **instância XBRL-GL** completa é esforço alto de baixo retorno se o CSV homologa. O próprio M4-DESIGN já hesita ("CSV **ou** XBRL-GL"). | Entregar **CSV** primeiro; XBRL-GL só se a homologação exigir. Evita travar M4 em ferramental XBRL. |
| E3 | **`IConsultaSiconfi` cobrindo todos os endpoints** (/rreo,/rgf,/dca,/msc_*,/entes) p/ reconciliação | Para o ciclo mínimo basta `/extrato_entregas` (confirma que entregou). Mapear 7 endpoints + paginação 5.000 + rate-limit antes do MVP é dourar. | MVP: só `/extrato_entregas`. Demais endpoints quando houver caso de reconciliação de valor. |

### 🟡 Granularidade adiável (não cortar, mas não priorizar)

- E4 — **LexML/URN persistente (Legislativo)**: o próprio mapa marca "transparência, **não remessa obrigatória**". Correto mantê-lo como 🟢; só garantir que não consuma orçamento de M-críticos.
- E5 — **CECAD 2.0 / CadSUAS / SISC / Prontuário SUAS** agrupados como destino: vários são **consulta**, não **remessa**. Separar "presto contas" de "consulto" evita esforço de integração onde basta acesso web.

---

## 3 MAIORES ACHADOS (prioridade de correção do mapa)

1. **L1 — EFD-Reinf + DCTFWeb ausentes (🔴).** O ente retém INSS/IRRF de prestadores (inclusive sobre as NFS-e que já ingerimos via ADN!) e DEVE declarar via EFD-Reinf→DCTFWeb (substituiu a DIRF extinta). Não está na tabela mestra; só aparece como `[a confirmar]` no eSocial. **Recomendação:** criar linhas de destino "Receita Federal — EFD-Reinf (R-2010/R-4020)" e "DCTFWeb" no módulo RecursosHumanos+Tributos; cruzar a **retenção** com a ingestão NFS-e/ADN que já temos. Confirmar regime de órgão público antes de codar.

2. **L2 — SADIPEM/CDP ausente (🔴).** Atualização anual da dívida pública (até 30/jan) é condição de CAUC; sem ela o município **perde transferências voluntárias**. Não confundir com Dívida Ativa (M6, que é a receber). **Recomendação:** adicionar destino "STN — SADIPEM/CDP (anual) + PVL/ARO (evento)" alimentado pelo passivo do PCASP; vincular ao painel de pendências fiscais.

3. **L5 — Conformidade SIAFIC não nomeada (🟠) + E1/E2 over-engineering de remessa (🟠).** Como o ERP **é o SIAFIC** do ente, há obrigação de **comprovar conformidade** (Dec. 10.540/2020, XML nº 1 nas contas anuais) e requisitos técnicos que o sistema precisa satisfazer — hoje implícitos. Em paralelo, **cortar CNAB 400/layouts por banco e XBRL-GL** do escopo inicial (data-driven/sob demanda) libera foco para as lacunas críticas acima. **Recomendação:** nomear a obrigação SIAFIC no mapa e enxugar o framework de remessas para 240+PIX+CSV no MVP.

---

## FONTES (oficiais)

- STN — SADIPEM (PVL/ARO/CDP): https://www.gov.br/tesouronacional/pt-br/estados-e-municipios/operacoes-de-credito/sadipem
- Tesouro Transparente — Painel CDP (atualização anual obrigatória até 30/jan; CAUC): https://www.tesourotransparente.gov.br/visualizacao/painel-do-cdp
- CNM — bloqueio de municípios por CDP não atualizado: https://cnm.org.br/comunicacao/noticias/hoje-e-o-ultimo-dia-para-envio-do-cadastro-da-divida-publica-e-mais-de-5-mil-municipios-ainda-nao-alimentaram-o-sistema
- Receita Federal — EFD-Reinf (SPED): https://www.gov.br/pt-br/servicos/efd-reinf | Manual: http://sped.rfb.gov.br/item/show/1497
- DIRF extinta (2024) → eSocial/EFD-Reinf/DCTFWeb: (orientações Receita; `[a confirmar]` para órgão público)
- SIAFIC — Decreto 10.540/2020: https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/decreto/d10540.htm
- SICONFI — alteração Dec. 10.540 / XML nº 1 SIAFIC: https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=42703
- Transferegov (ex-+Brasil/SICONV) — prestação de contas de transferências voluntárias: CNM, "Transferências Voluntárias da União" (Coleção Gestão Pública Municipal)
- SICONFI — Regras Gerais RGF 2026 / MSC: https://siconfi.tesouro.gov.br/

> Itens `[a confirmar]` exigem validação na fonte oficial do exercício antes de virar requisito (CLAUDE.md §16).

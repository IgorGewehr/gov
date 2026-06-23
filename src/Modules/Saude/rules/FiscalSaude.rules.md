---
modulo: Saude
agregado: FundoMunicipalSaude
contexto: Saude (eixo fiscal — Fundo Municipal de Saúde, mínimo 15% ASPS, blocos de financiamento federal)
poder: Executivo
schema: saude
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["LC 141/2012 (15% ASPS, arts. 3º e 4º)", "EC 29/2000", "Portaria GM/MS 3.992/2017 (blocos Custeio/Investimento)", "Portaria GM/MS 3.493/2024 (componentes APS)", "Portaria GM/MS 204/2007 (financiamento fundo a fundo)", "Portaria MOG 42/1999 (funções/subfunções)", "SIOPS (Sistema de Informações sobre Orçamentos Públicos em Saúde)"]
---

# Fiscal de Saúde — Regras-as-Code (Rules-as-Code)

> Eixo fiscal do módulo Saúde (M7 S-1/S-2): a apuração do **mínimo constitucional de 15% ASPS** (LC
> 141/2012) e a gestão do **Fundo Municipal de Saúde (FMS)** com execução segregada por **bloco de
> financiamento federal** (Custeio/Manutenção e Investimento/Estruturação — Port. 3.992/2017). É o
> coração contábil da Saúde, que alimenta a prestação de contas fundo a fundo e os demonstrativos.

## S-1 — Mínimo 15% ASPS (`ApuradorAsps`)

- A receita-base é a de **impostos + transferências constitucionais**; o numerador é só a despesa de
  Saúde **classificada como computável** (LC 141 art. 3º computa; art. 4º **não** computa: inativos,
  assistência à saúde do servidor, saneamento básico geral, limpeza urbana, merenda).
- A classificação é **versionada por tenant+vigência** (`RegraClassificacaoAsps`) — nada hardcoded; a
  regra mais específica (subfunção > fonte > função) vence; o percentual mínimo (default legal 15%) é
  parametrizável (a Lei Orgânica municipal pode fixar maior).
- O indicador é **reprodutível** (sem relógio): mesmas despesas + regras + receita-base + % ⇒ mesmo
  resultado. Indicador no espírito do **SIOPS**.

## S-2 — Fundo Municipal de Saúde por bloco (`FundoMunicipalSaude`)

- Unidade gestora com **execução segregada por bloco**; o recurso federal entra **por bloco** e é
  executado **dentro do bloco** (vínculo à fonte de recurso do PCASP), com **transposição vedada**
  entre blocos (Port. 3.992/2017).
- Cada conta amarra `(bloco, fonte de recurso)`; o saldo do bloco é `recebido - executado`.

## S-1 (Via A2) — alimentador da execução fiscal (`RegistrarExecucaoSaude`)

- Projeta as linhas de execução de Saúde (receita-base + despesas por função/subfunção/fonte) que o
  read model do `ApuradorAsps` lê, **idempotente por `OrigemHash`**. É o ponto de entrada do ACL que
  consome a contabilidade (Finanças.Contracts); a classificação ASPS (LC 141 arts. 3º/4º) ocorre na
  **leitura**, contra as regras vigentes — o alimentador não toma decisão fiscal.

<!-- manifest
commands: AbrirFundoMunicipalSaude, ReceberParcelaFns, ExecutarDespesaBloco, RegistrarExecucaoSaude
queries: ApurarAsps, ObterExecucaoFms
domainEvents:
integrationEventsPublished:
integrationEventsConsumed:
-->

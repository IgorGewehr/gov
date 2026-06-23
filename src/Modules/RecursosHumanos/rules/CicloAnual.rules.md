---
modulo: RecursosHumanos
agregado: FolhaDePagamento (ciclo anual)
contexto: RecursosHumanos (13º salário, férias e rescisão)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/1988 art. 7º VIII (13º) e XVII (férias + 1/3)", "Lei 4.090/1962 e Lei 4.749/1965 (13º / parcelas)", "Lei 7.713/1988 art. 12-A (IRRF do 13º — exclusivo na fonte, base separada)", "CLT arts. 129–145 e 477, 484-A (férias / rescisão)", "Lei 12.506/2011 (aviso prévio)", "Súmula 386 STJ (férias/abono indenizados isentos)", "STF Tema 985 (1/3 de férias)"]
---

# Ciclo Anual da Folha — Regras-as-Code (13º, Férias, Rescisão)

> Acréscimo ao agregado `FolhaDePagamento`: cada item do ciclo anual gera **sua própria folha**
> (`TipoFolha` ∈ {Mensal, DecimoTerceiro, Ferias, Rescisao}), **reusando** o `MotorDeCalculoFolha`,
> as `RubricaFolha` (incidências) e as tabelas legais (INSS/IRRF/RPPS) — **sem duplicar cálculo**.
> O **13º** usa tributação em **base separada** (Lei 7.713/88 art. 12-A): INSS/IRRF próprios, IRRF
> **sem** desconto simplificado. **Abate-teto** (CF art. 37, XI) só na folha **Mensal**. Tudo
> determinístico: avos/períodos derivam de **datas de entrada** (sem relógio). Fundamentação completa
> em `docs/architecture/m5-prep/FOLHA-CICLO-ANUAL-DESIGN.md` e `pesquisa-folha-ciclo-anual.md`.

## Comandos

- **GerarDecimoTerceiro** — gera a folha de 13º (Tipo=DecimoTerceiro). 1ª parcela = adiantamento
  (percentual paramétrico, sem desconto); 2ª parcela = 13º integral com INSS/IRRF **próprios** do
  13º em base separada e abatimento da 1ª parcela. Avos por `Avos.Apurar` (regra dos 15 dias).
- **GerarFerias** — gera a folha de férias (Tipo=Ferias): remuneração dos dias gozados + 1/3
  constitucional (fração paramétrica) + abono pecuniário opcional (venda de até 1/3) com seu terço.
  Incidências (gozadas tributam; abono isento — Súmula 386 STJ) vivem na `RubricaFolha`.
- **GerarVerbasRescisorias** — gera a folha de rescisão (Tipo=Rescisao): compõe as verbas devidas
  por **matriz parametrizável** (tipo de desligamento × regime — `CompositorRescisao`): saldo de
  salário, 13º proporcional, férias vencidas/proporcionais + 1/3, aviso prévio e multa de FGTS
  (só celetista).

## Invariantes

- Uma folha por **(tenant, competência, tipo)** — a mesma competência comporta mensal + 13º + férias
  + rescisão coexistindo.
- O 13º apura IRRF **sem** desconto simplificado (art. 12-A); INSS/RPPS do 13º em base própria.
- 13º/férias/rescisão **não** disparam abate-teto (apuram-se em folha própria).
- `FolhaFechadaIntegrationEvent` carrega o **tipo** da folha para roteio do empenho (despesa de
  pessoal por elemento próprio).

<!-- manifest
commands: GerarDecimoTerceiro, GerarFerias, GerarVerbasRescisorias
queries:
domainEvents:
integrationEventsPublished:
integrationEventsConsumed:
-->

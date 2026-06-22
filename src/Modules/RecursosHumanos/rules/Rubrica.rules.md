# Rubrica e Tabelas Legais — Rules-as-Code (RecursosHumanos)

> **Bounded Context:** RecursosHumanos · **Agregados:** `RubricaFolha`, `TabelaInss`, `TabelaIrrf`, `TabelaRpps`
> **Fonte da verdade da folha (fundação — parte 1).** Toda alíquota/faixa/teto é PARÂMETRO LEGAL
> por exercício/competência — **nunca hardcoded** (CLAUDE.md §7/§16); o motor aplica fórmulas sobre
> números parametrizados, versionados por `tenantId`+`competencia`.

## Linguagem ubíqua

- **Rubrica (verba):** classificação de um provento/desconto/informativa (eSocial S-1010), com
  **incidências** (integra base de INSS/RPPS/IRRF/FGTS). Bases são somadas **por incidência, nunca
  por nome** de rubrica.
- **Tabela INSS (RGPS):** faixas progressivas cumulativas + teto, por competência (Portaria MPS/MF).
- **Tabela IRRF:** faixas progressivas mensais (alíquota × base − parcela a deduzir), dedução por
  dependente e desconto simplificado (regra do mais vantajoso), por competência.
- **Tabela RPPS:** contribuição do servidor efetivo — **lei municipal** (EC 103/2019). Sem ela, o
  motor opera **fail-closed** (recusa o cálculo do efetivo).

## Invariantes

- **R-1:** rubrica informativa (natureza 3/4) **não** integra base de INSS/RPPS/IRRF/FGTS.
- **R-2:** rubrica não tem valor fixo e percentual simultaneamente.
- **R-3:** código de rubrica é único por tenant.
- **R-4:** tabelas INSS/RPPS têm faixas contíguas iniciando em zero; a última do INSS fecha no teto.
- **R-5:** INSS/RPPS são progressivos cumulativos; o arredondamento ocorre **uma vez** sobre a soma.
- **R-6:** IRRF usa a **menor base** entre (rendimento − previdência − dependentes − pensão) e
  (rendimento − desconto simplificado).
- **R-7:** sem tabela RPPS municipal, o cálculo de servidor efetivo é **recusado** (fail-closed).
- **R-8:** o cálculo é **determinístico e auditável** (mesmas entradas + tabelas ⇒ mesmo resultado).

## TODO(validar-oficial)

- Códigos de incidência eSocial (Tabelas 03/20/21/23) a mapear do XSD/MOS S-1.3 (não hardcode).
- RPPS de Maximiliano de Almeida/RS: alíquota/base dependem da lei municipal (sem default federal).
- IRRF 2026: redutor da Lei 15.270/2025 na retenção mensal só após IN da Receita (não acoplado).

<!-- manifest
commands: CriarRubrica, CriarTabelaRpps, SemearTabelasFederais
queries: ListarRubricas
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->

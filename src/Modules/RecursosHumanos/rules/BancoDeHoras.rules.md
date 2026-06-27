# Banco de Horas — Rules-as-Code (RecursosHumanos)

> **Bounded Context:** RecursosHumanos · **Agregado:** `BancoDeHoras` (com `LancamentoBancoHoras`)
> **Base legal/normativa:** CLT art. 59 §§2º-5º (compensação) e a janela de compensação por acordo
> (6 meses — acordo individual; 1 ano — ACT/CCT). Aplicabilidade ao **estatutário** é decisão
> parametrizável por tenant (RJU + normas TCE-RS) — coerente com `AplicarPortaria671AoEstatutario`.

## Linguagem ubíqua

- **Banco de horas:** saldo acumulado de horas (créditos por hora extra − débitos por compensação/falta)
  de um servidor, por janela de compensação configurável.
- **Lançamento:** movimento individual (crédito ou débito) que altera o saldo, com competência e origem
  (apuração de ponto, ajuste manual autorizado, compensação).
- **Prescrição:** créditos não compensados dentro da janela legal **prescrevem** (viram hora extra a pagar
  ou caducam conforme a política do tenant) ao fim do prazo — execução periódica.

## Invariantes

- **BH-1:** o saldo de uma competência = saldo anterior + (créditos − débitos) dos lançamentos da competência.
- **BH-2:** lançamento é append-only (auditoria imutável — CLAUDE.md §4); estorno é novo lançamento contrário.
- **BH-3:** débito de compensação não pode deixar o saldo negativo além do limite parametrizado por tenant.
- **BH-4:** crédito não compensado prescreve ao fim da janela legal (6 meses / 1 ano), nunca antes.
- **BH-5:** a prescrição é idempotente por competência — reexecutar não prescreve o mesmo crédito duas vezes.

## Gancho com o ponto

- A apuração de ponto (`ApuracaoPontoFechada`) é a origem natural dos créditos/débitos; o lançamento aqui
  é o **registro contábil de horas** (vínculo, não recálculo da apuração).

<!-- manifest
commands: LancarBancoDeHoras, ExecutarPrescricaoBancoDeHoras
queries: ObterExtratoBancoDeHoras
domainEvents: LancamentoBancoHorasRegistrado, CreditosBancoHorasPrescritos
integrationEventsPublished: 
integrationEventsConsumed: 
-->

# Spec BDD — Sequencial atomico do NUP (Peca 3, W9.4)

> Autoridade: `docs/architecture/m9-prep/PROTOCOLO-ACT-CONARQ-DESIGN.md` (Peca 3).
> Corrige o race condition do `NupSequencialGenerator` (COUNT+1) com o padrao atomico do
> hash-chain (UPDLOCK/HOLDLOCK por tenant x ano), espelhando `UltimoSeloReader`.

## Contexto

- O NUP (Decreto 8.539/2015) e `nnnnnn/aaaa-dd`: sequencial anual por tenant + ano + DV (modulo 11).
- A alocacao do sequencial passa a usar um **contador atomico** por `(TenantId, Ano)` na tabela
  `SequenciasNup` (schema `protocolo`), nao mais `COUNT(*)` dos processos.
- O formato e o DV (modulo 11) permanecem IDENTICOS; muda apenas COMO o sequencial e alocado.

## Cenarios

### C-1 — Duas autuacoes concorrentes do mesmo tenant x ano geram NUPs DISTINTOS
```
Dado um tenant T e o exercicio 2026 sem nenhum NUP emitido
Quando duas autuacoes concorrentes solicitam um NUP ao mesmo tempo
Entao cada autuacao recebe um sequencial distinto (000001 e 000002)
E nenhuma autuacao falha por colisao (DbUpdateException)
E o indice unico (TenantId, Nup) permanece como backstop, nunca acionado
```

### C-2 — Primeiro NUP do ano cria a linha-contador com sequencial 1
```
Dado um tenant T sem linha em SequenciasNup para o ano 2026
Quando a primeira autuacao do ano solicita um NUP
Entao a linha (T, 2026, UltimoSequencial=1) e criada
E o NUP gerado e 000001/2026-DV
```

### C-3 — Reset anual: novo exercicio reinicia em 1
```
Dado um tenant T com (T, 2025, UltimoSequencial=42)
Quando a primeira autuacao de 2026 solicita um NUP
Entao uma nova linha (T, 2026, UltimoSequencial=1) e criada
E o sequencial de 2025 nao e afetado
```

### C-4 — Isolamento por tenant: contadores nao se misturam
```
Dado o tenant A com (A, 2026, UltimoSequencial=5)
E o tenant B sem linha para 2026
Quando B autua o primeiro processo de 2026
Entao B recebe 000001/2026-DV (e nao 000006)
```

### C-5 — DV preservado (modulo 11, Decreto 8.539/2015)
```
Dado o sequencial 000123 no ano 2026
Quando o NUP e formatado
Entao o digito verificador e o mesmo calculado pelo algoritmo modulo 11 atual
```

## Invariantes
- I-NUP1: o contador e incrementado e persistido no MESMO SaveChanges da autuacao (transacional).
- I-NUP2: em SqlServer o contador e lido sob UPDLOCK/HOLDLOCK (serializa concorrentes do tenant x ano).
- I-NUP3: em SQLite/testes (escrita serializada) usa Max+1; o indice unico (TenantId, Nup) e o backstop.
- I-NUP4: zero numero magico — ano vem do `TimeProvider`; formato/larguras sao constantes nomeadas.

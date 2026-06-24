# Spec BDD — Motor de Temporalidade / Destinacao (e-ARQ v2 / CONARQ) (Peca 2, W9.4)

<!-- manifest
commands: CadastrarPlanoClassificacao, CadastrarTabelaTemporalidade, AvaliarAptidaoDestinacoes, AutorizarEliminacao, RegistrarEliminacao
-->

> Autoridade: `docs/architecture/m9-prep/PROTOCOLO-ACT-CONARQ-DESIGN.md` (Peca 2).
> Plano de Classificacao configuravel + TTD por tenant + calculo CORRENTE -> INTERMEDIARIA ->
> DESTINACAO (eliminacao OU guarda permanente), amarrado a hash + assinatura + WORM.
> Parametrizavel por tenant; zero hardcode (CLAUDE.md S7).

## Agregados
- `PlanoDeClassificacao` (codigo x assunto; arvore de `ClasseDocumental`).
- `TabelaTemporalidade` (TTD; `RegraTemporalidade` por codigo de classificacao).
- `DestinacaoProcesso` (ficha de destinacao calculada e registrada de um processo arquivado).
- VO `MetadadosArquivisticos` (e-ARQ v2).
- Porta `IMotorTemporalidade` (calculo local deterministico, sem I/O).

## Cenarios

### C-1 — Calculo das 3 fases (determinismo)
```
Dado uma RegraTemporalidade para a classe 040 com corrente=2 anos, intermediaria=5 anos,
  destinacao=Eliminacao, eventoContagem=DataArquivamento
E o evento base 2026-01-10
Quando o motor calcula
Entao FimGuardaCorrente = 2028-01-10
E FimGuardaIntermediaria = 2033-01-10
E DataAptidaoEliminacao = 2033-01-10 (fim da intermediaria)
```

### C-2 — Guarda permanente nao tem data de aptidao a eliminacao
```
Dado uma regra com destinacao=GuardaPermanente
Quando o motor calcula
Entao DataAptidaoEliminacao e nula
E Destinacao = GuardaPermanente
```

### I-T1 — Nao eliminar antes do prazo
```
Dado uma DestinacaoProcesso com DataAptidaoEliminacao no futuro
Quando AutorizarEliminacao e chamada hoje (< aptidao)
Entao lanca (prazo de guarda nao decorrido)
```

### I-T2 — Guarda permanente nunca elimina
```
Dado uma DestinacaoProcesso com Destinacao=GuardaPermanente
Quando AutorizarEliminacao ou MarcarEliminado e chamada
Entao SEMPRE lanca (transicao para Eliminado e inalcancavel)
```

### I-T3 — Eliminacao so de processo arquivado
```
Dado um processo NAO arquivado
Quando se tenta criar/avancar destinacao para eliminacao
Entao lanca (exige Situacao == Arquivado)
```

### I-T4 — Rastreabilidade do irreversivel
```
Dado uma DestinacaoProcesso apta e autorizada
Quando MarcarEliminado e chamada SEM TermoEliminacaoHash assinado+carimbado
Entao lanca (o termo e a prova oponivel ao TCE, sob WORM)
```

### I-T5 — Classe deve existir no plano do tenant
```
Dado um PlanoDeClassificacao ativo do tenant sem a classe 999
Quando se tenta autuar/classificar com codigo 999
Entao lanca (FK logica multi-tenant: classe inexistente recusada)
```

### I-T6 — Parametro, nunca constante
```
Dado prazos/destinacao
Entao vem SEMPRE da TTD do tenant; cada regra carrega Observacao = norma-fonte (TCE)
```

### I-T7 — Imutabilidade da decisao terminal
```
Dado uma DestinacaoProcesso em estado terminal (Eliminado/Permanente)
Quando se tenta retroceder
Entao lanca (append-only, sob hash-chain/WORM)
```

## Fluxo
1. `Processo.Arquivar` -> cria `DestinacaoProcesso` (via Outbox, transacional); eventoBase do `EventoContagem`.
2. Varredura (padrao de drenagem do Outbox) promove `AguardandoPrazo -> Apto*` quando hoje >= aptidao.
3. Eliminacao exige ato humano autorizado (RBAC) + edital + termo assinado/carimbado (Peca 1) — nunca automatica.

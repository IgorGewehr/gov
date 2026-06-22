# Ponto Eletrônico (Portaria MTP 671/2021) — Rules-as-Code (RecursosHumanos)

> **Bounded Context:** RecursosHumanos · **Agregados:** `MarcacaoPonto`, `JornadaTrabalho`, `ApuracaoPonto`
> **Autoridade:** `docs/architecture/m5-prep/M5-DESIGN.md` §3 + `pesquisa-ponto-671.md`.
> **Regra de ouro (CLAUDE.md §16):** leiautes AFD/AEJ (posições/larguras/tipos de registro) e o perfil
> CAdES seguem o CONCEITO da Portaria 671 com `// TODO(validar-oficial)` nos pontos exatos — o texto
> integral dos Anexos (DOU) ainda não foi obtido. A aplicabilidade ao **estatutário** é decisão
> parametrizável por tenant (`AplicarPortaria671AoEstatutario`) — a 671 regulamenta a CLT.

## Linguagem ubíqua

- **REP (Registrador Eletrônico de Ponto):** REP-C (convencional/hardware), REP-A (alternativo,
  via ACT/CCT) ou REP-P (por programa/software; caminho do piloto).
- **Marcação:** batida bruta (entrada/saída), com **NSR** sequencial e CPF; registro **imutável** do AFD.
- **NSR (Número Sequencial de Registro):** inteiro positivo, estritamente crescente e **sem lacunas**
  por tenant (REP), gravado em cada linha do AFD.
- **AFD (Arquivo Fonte de Dados):** arquivo posicional ASCII (ISO-8859-1), bruto/cronológico/imutável,
  cabeçalho + marcações + trailer (com CRC-16), assinado em **CAdES detached (.p7s)**.
- **PTRP / AEJ (Arquivo Eletrônico de Jornada — Anexo VI):** tratamento das marcações (sem alterar o
  AFD) que apura jornada contratada, trabalhada, extras e atrasos; gera AEJ + espelho.
- **Jornada/escala:** carga diária (min), intervalo, tolerância; base da apuração.
- **Banco de horas:** saldo acumulado (extras − faltas); janela 6 meses (acordo individual) / 1 ano (ACT/CCT).

## Invariantes

- **P-1:** NSR é sequencial e sem lacunas por tenant (próximo = último + 1; inicia em 1).
- **P-2:** marcação é append-only — nunca alterada/removida (auditoria imutável — CLAUDE.md §4).
- **P-3:** o AFD não é alterado pelo tratamento; correções vivem no AEJ/espelho.
- **P-4:** carga + intervalo cabem em um dia; carga diária > 0.
- **P-5:** extras/faltas só são apurados além da tolerância diária.
- **P-6:** o saldo do banco de horas de uma competência = saldo anterior + (extras − faltas).
- **P-7:** apuração fechada congela espelho/AEJ; reabrir exige recálculo explícito.
- **P-8:** a geração do AFD/AEJ é **fail-closed** sem CNPJ do ente configurado.
- **P-9:** AFD/AEJ assinados em CAdES detached pelo A1 do tenant (reusa o Cofre); comprovante = PAdES.

## Gancho para a folha

- O fechamento da apuração emite `ApuracaoPontoFechada` (via Outbox) com extras/faltas — **gancho**
  para a folha traduzir em rubricas (hora extra/falta). **Não** recalcula a folha aqui (vínculo, não cálculo).

## TODO(validar-oficial)

- Posições/larguras/tipos de registro exatos do **AFD** e do **AEJ (Anexo VI)** — Anexos da Portaria 671 (DOU).
- Variante exata do **CRC-16** exigida (CCITT-FALSE adotado como conceito).
- Perfil **CAdES** exato (algoritmo/atributos/cadeia ICP-Brasil) do destino `Ponto`.
- Formato de data/hora por registro (`ddMMyyyy`/`HHmm`) e largura do NSR (9 adotado).
- Aplicabilidade da 671 ao **estatutário** (RJU de Maximiliano de Almeida/RS + normas TCE-RS).

<!-- manifest
commands: RegistrarMarcacao, DefinirJornada, ApurarJornada, FecharApuracaoJornada
queries: GerarAfd, GerarAej
domainEvents: MarcacaoPontoRegistrada, ApuracaoPontoFechada
integrationEventsPublished: 
integrationEventsConsumed: 
-->

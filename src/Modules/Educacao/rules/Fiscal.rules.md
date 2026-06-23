---
modulo: Educacao
agregado: Fiscal (nucleo fiscal — MDE 25% + FUNDEB 70%/distribuicao)
contexto: Educacao (camada fiscal/integracao — minimos constitucionais e repasses fundo a fundo)
poder: Executivo
schema: educacao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/1988 art. 212 (min. 25% MDE)", "LDB Lei 9.394/1996 arts. 70 (inclui) e 71 (exclui)", "EC 108/2020 (FUNDEB permanente; piso de 70% para remuneracao dos profissionais)", "Lei 14.113/2020 (FUNDEB — VAAF/VAAT/VAAR; art. 25 saldo; art. 26 remuneracao)", "Portaria Interministerial 424/2016 (prazo SIOPE)", "Manual SIOPE/FNDE [a confirmar]"]
---

# Fiscal (Educacao) — Regras-as-Code (Rules-as-Code)

> Camada **fiscal/integracao** do modulo Educacao (M7 E-1/E-2/E-3), espelhando o padrao de Saude
> (`ApuradorAsps`/FMS): apura o **minimo de 25% MDE** (CF art. 212; classificacao fina LDB arts. 70/71,
> afericao ANUAL), o **piso de 70% do FUNDEB** na remuneracao dos profissionais da educacao (EC 108/2020,
> cruzando com a folha do RH via Contracts) e a **distribuicao do FUNDEB** por origem (cota-parte +
> complementacoes VAAF/VAAT/VAAR — concilia, nao recalcula a cota). **Tudo parametrizavel/versionado por
> tenant+vigencia** (CLAUDE.md §16) e **reprodutivel** (sem relogio: ancora no exercicio).

---

## 1. Linguagem Ubiqua

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| MDE (`Mde`) | Manutencao e Desenvolvimento do Ensino (CF art. 212; LDB arts. 70/71). |
| Apurador MDE (`ApuradorMde`) | Servico de dominio que classifica a despesa de Educacao e apura o minimo de 25%. |
| Classificador MDE (`ClassificadorMde`) | Decide se uma despesa (funcao 12) computa na MDE (regra mais especifica vence). |
| Regra de Classificacao MDE (`RegraClassificacaoMde`) | Inclusao/exclusao versionada por (funcao, subfuncao?, fonte?) + vigencia. |
| Indicador MDE (`IndicadorMde`) | VO imutavel com receita-base, aplicado, %, minimo, situacao e natureza. |
| Natureza do Aferimento (`NaturezaAferimentoMde`) | Afericao ANUAL (conformidade) × indicador BIMESTRAL (acompanhamento). |
| Codigo Funcional (`CodigoFuncionalEducacao`) | Funcao (2 digitos; "12"=Educacao) + subfuncao (3 digitos). |
| FUNDEB (`Fundeb`) | Fundo de Manutencao e Desenvolvimento da Educacao Basica (EC 108/2020; Lei 14.113/2020). |
| Distribuicao FUNDEB (`DistribuicaoFundeb`) | Estrutura de recebimento/conciliacao por origem (raiz de agregado). |
| Origem do Recurso (`OrigemRecursoFundeb`) | CotaParteEstadual / ComplementacaoVaaf / Vaat / Vaar. |
| Conta por Origem (`ContaOrigemFundeb`) | Entidade-filha: recebido × esperado por origem (conciliacao). |
| Indicador de Aplicacao FUNDEB (`IndicadorAplicacaoFundeb`) | VO do piso de 70% (remuneracao dos profissionais / receita FUNDEB). |
| Remuneracao dos Profissionais (`RemuneracaoMagisterioExercicio`) | Total pago a profissionais da educacao basica (numerador dos 70%). |
| Linha de Execucao (`LinhaExecucaoEducacao`) | Projecao tenant-scoped (Via A2) da execucao contabil decomposta. |
| Parametro Fiscal (`ParametroFiscalEducacao`) | Percentual/valor versionado por tenant+vigencia (nunca hardcoded). |
| Tenant (`TenantId`) | Ente municipal (rede de ensino) dono do registro. |

---

## 2. Modelo

- **E-1 dominio (puro/reprodutivel):** `ApuradorMde` + `ClassificadorMde` (static), `IndicadorMde` (VO),
  `RegraClassificacaoMde : AggregateRoot<RegraClassificacaoMdeId>, IMustHaveTenant`, `CodigoFuncionalEducacao` (VO),
  `SeedRegrasMde` (seed default versionado).
- **E-2 dominio:** `IndicadorAplicacaoFundeb` (VO; piso de 70%).
- **E-3 dominio:** `DistribuicaoFundeb : AggregateRoot<DistribuicaoFundebId>, IMustHaveTenant` com filhas
  `ContaOrigemFundeb : Entity<ContaOrigemFundebId>` (owned); `OrigemRecursoFundeb` (enum).
- **Infra (read model/parametro):** `LinhaExecucaoEducacao`, `ParametroFiscalEducacao`,
  `RemuneracaoMagisterioExercicio` (todas `IMustHaveTenant`).

### Enum `EfeitoMde` (Inclui=1 art.70 / Exclui=2 art.71) · `NaturezaAferimentoMde` (AferimentoAnual=1 / IndicadorBimestral=2)
### Enum `OrigemRecursoFundeb` (CotaParteEstadual=1 / ComplementacaoVaaf=2 / ComplementacaoVaat=3 / ComplementacaoVaar=4)
### Enum `SituacaoMde` (Atingido=1 / NaoAtingido=2) · `SituacaoFundeb70` (Atingido=1 / NaoAtingido=2)

---

## 3. Invariantes

- **I-1.** A MDE computa **apenas** a despesa classificada como inclusa (LDB art. 70); o art. 71 exclui
  (merenda, assistencia medica ao aluno, obras urbanas fora das escolas, inativos) — sem regra, NAO computa.
- **I-2.** Despesa fora da funcao 12 (Educacao) nunca compoe MDE.
- **I-3.** A regra mais **especifica** vence (subfuncao > fonte > funcao); empate -> vigencia mais recente.
- **I-4.** O minimo de 25% (CF art. 212) e a afericao ANUAL de conformidade; o bimestral e so acompanhamento.
- **I-5.** O piso de 70% (EC 108/2020) confronta remuneracao dos profissionais × receita FUNDEB do exercicio.
- **I-6.** A `DistribuicaoFundeb` **concilia** recebido × esperado por origem; **nao recalcula a cota**.
- **I-7.** Percentuais/pisos sao **parametrizaveis por tenant+vigencia** (default legal 25%/70%); nunca hardcoded.
- **I-8.** Apuracao **reprodutivel** (sem relogio): ancora no exercicio; VO -> igualdade estrutural.
- **I-9.** Tudo tenant-scoped (Global Query Filter); `OrigemHash`/exercicio garantem idempotencia.

---

## 5. Comandos (escrita)

- **RegistrarExecucaoEducacao** — `RegistrarExecucaoEducacaoCommand(int Exercicio, IReadOnlyList<LinhaExecucaoEducacaoEntrada> Linhas) : ICommand<int>` (E-1, Via A2; idempotente por `OrigemHash`).
- **AbrirDistribuicaoFundeb** — `AbrirDistribuicaoFundebCommand(int Exercicio) : ICommand<Guid>` (E-3).
- **DefinirEsperadoFundeb** — `DefinirEsperadoFundebCommand(Guid DistribuicaoId, OrigemRecursoFundeb Origem, decimal ValorEsperado) : ICommand` (E-3, base da conciliacao).
- **ReceberParcelaFundeb** — `ReceberParcelaFundebCommand(Guid DistribuicaoId, OrigemRecursoFundeb Origem, decimal Valor) : ICommand` (E-3).
- **RegistrarRemuneracaoMagisterio** — `RegistrarRemuneracaoMagisterioCommand(int Exercicio, decimal RemuneracaoProfissionais) : ICommand` (E-2, cruzamento por parametro quando o RH ainda nao publicou o evento).

---

## 6. Consultas (leitura)

- **ApurarMde** — `ApurarMdeQuery(int Exercicio, NaturezaAferimentoMde Natureza = AferimentoAnual) : IQuery<ApuracaoMdeResultado>` (E-1; 25% MDE).
- **ApurarFundeb70** — `ApurarFundeb70Query(int Exercicio) : IQuery<ApuracaoFundeb70Resultado>` (E-2; piso de 70%).

---

## 7. Eventos

### Dominio
- Nenhum nesta versao (a camada fiscal e read-model + apuracao; os domain events ficam nos agregados pedagogicos).

### Integracao (publica)
- Nenhum publicado por este agregado nesta versao. Os resultados de apuracao alimentam o PAD mensal do M4
  (Anexo 8 do RREO) e o painel de minimos (M8) — // TODO(validar-oficial): leiaute SIOPE (contas->campos).

### Integracao (consome)
- **RemuneracaoMagisterioApuradaIntegrationEvent** (de RecursosHumanos.Contracts) — cruzamento com a folha
  para o piso de 70% (E-2). ACL `ReceberRemuneracaoMagisterioHandler` materializa o total no read model
  `RemuneracaoMagisterioExercicio`, idempotente por exercicio. // TODO(validar-oficial): rol dos
  profissionais da educacao basica (Lei 14.113/2020 + instrumento CACS-FUNDEB).

---

## 9. Persistencia (EF Core 8) — schema `educacao`

| Tabela | Conteudo |
|---|---|
| `RegrasClassificacaoMde` | regras versionadas (indice por tenant+funcao+subfuncao+fonte+vigencia). |
| `DistribuicoesFundeb` + `ContasOrigemFundeb` (owned) | distribuicao por origem (unico por tenant+exercicio; conta unica por distribuicao+origem). |
| `LinhasExecucaoEducacao` | projecao Via A2 (unico por tenant+OrigemHash; indice por tenant+exercicio). |
| `ParametrosFiscaisEducacao` | percentuais versionados (unico por tenant+chave+vigencia). |
| `RemuneracoesMagisterio` | remuneracao dos profissionais (unico por tenant+exercicio). |

> DbContext ctor **inalterado**; tabelas materializadas pela migration `NucleoFiscalEducacao` (SqlServer) /
> `SchemaProvisioner` (SQLite). Seed idempotente por tenant: regras MDE default + 25% (CF 212) + 70% (EC 108/2020).

---

## 10. Seguranca, Tenant e Auditoria

- **Tenant:** todas as entidades fiscais sao `IMustHaveTenant`; Global Query Filter por `TenantId`.
- **RBAC:** `educacao.fiscal.gerenciar` (escrita) / `educacao.fiscal.ver` (leitura); negar por padrao.
- **Auditoria imutavel:** trilha (antes/depois, usuario, IP, timestamp) em toda mutacao fiscal — TCE-RS.
- **Cross-module:** so via `*.Contracts` (RH expoe a remuneracao do magisterio; Educacao consome via ACL).

---

## 12. Cenarios BDD

**Cenario 1 — MDE computa so o classificado (exclui art. 71).** Dado receita 1M e despesas (361=250k, 306 merenda=50k); Quando `ApurarMdeQuery(2026)`; Entao aplicado=250k, 25%, atingido (merenda fora).
**Cenario 2 — 25% atingido/nao.** 250k/1M atinge; exclusoes que derrubam para 23% nao atingem.
**Cenario 3 — FUNDEB 70% magisterio atingido/nao.** 700k de 1M atinge; 600k (60%) nao atinge o piso de 70%.
**Cenario 4 — Reprodutibilidade.** Mesmas entradas -> mesmo indicador (VO, sem relogio).
**Cenario 5 — Isolamento por tenant.** Tenant B nao enxerga execucao/distribuicao do tenant A.
**Cenario 6 — Parametrizacao.** Lei Organica eleva MDE a 28% / piso FUNDEB versionado sobrepoe o default.

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versao inicial — nucleo fiscal de Educacao (M7 E-1 MDE 25% / E-2 FUNDEB 70% / E-3 distribuicao), espelhando o padrao de Saude (ApuradorAsps/FMS); cruzamento com a folha do RH via Contracts. |

<!-- manifest
commands: RegistrarExecucaoEducacao, AbrirDistribuicaoFundeb, DefinirEsperadoFundeb, ReceberParcelaFundeb, RegistrarRemuneracaoMagisterio
queries: ApurarMde, ApurarFundeb70
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: RemuneracaoMagisterioApuradaIntegrationEvent
-->

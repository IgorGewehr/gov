# Revisão de Folha e eSocial — para o Especialista de RH/Folha

> **O que esta página é.** Tudo que o especialista em folha de pagamento e eSocial precisa conferir:
> as **tabelas legais com os valores** (IRRF, INSS), os **parâmetros** de folha/previdência/eSocial,
> os **eventos eSocial** gerados (S-1200/S-1202/S-2200/SST), o **abate-teto**, o **redutor do IRRF
> 2026**, a **margem consignável** e o **ciclo anual** (13º/férias/rescisão).
>
> 🟢 = parâmetro/valor ajustável direto · 🟡 = lógica descrita no `.rules.md` (sinalize se divergir da norma).
>
> Base normativa: [`docs/normas/FONTES-NORMATIVAS.md`](../normas/FONTES-NORMATIVAS.md) (eSocial MOS S-1.3
> consolidada NO 07/2026; Leiautes S-1.3 XSD NT 06/2026, produção 01/07/2026; EFD-Reinf R-4000 NT 01/2026
> que **substitui a DIRF**; TCE-RS IN 06/2019 = TCE_4810/4820; LRF arts. 18-23 = limite de pessoal).

---

## ⭐ As tabelas com VALORES (parâmetros ajustáveis 🟢) — comece por aqui

Estas tabelas são **dado parametrizado por competência** (nunca embutidas na lógica). Mudar um valor
aqui muda o cálculo da folha. Todas no arquivo de seed:
[`src/Modules/RecursosHumanos/.../TabelasLegais/SemearTabelasFederais.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Application/TabelasLegais/SemearTabelasFederais.cs)

### Tabela INSS (RGPS) — confira faixas e teto

| Competência | Faixas (até → alíquota) | Teto | Base legal | O QUE CONFERIR |
|---|---|---|---|---|
| **2025** | 1.518,00→7,5% · 2.793,88→9% · 4.190,83→12% · 8.157,41→14% | **8.157,41** | Portaria Interministerial MPS/MF nº 6/2025 | Confira faixas/alíquotas/teto contra a portaria. |
| **2026** | 1.621,00→7,5% · 2.902,84→9% · 4.354,27→12% · 8.475,55→14% | **8.475,55** | Portaria Interministerial MPS/MF nº 13/2026 | Confira faixas/alíquotas/teto 2026 (efeitos desde 01/01/2026). |

### Tabela IRRF (folha) — confira faixas, dedução por dependente, desconto simplificado e **redutor 2026**

| Competência | Faixas (até → alíquota → parcela a deduzir) | Ded./dependente | Desc. simplificado | Base legal |
|---|---|---|---|---|
| **jan–abr/2025** | 2.259,20→0% · 2.826,65→7,5%/169,44 · 3.751,05→15%/381,44 · 4.664,68→22,5%/662,77 · acima→27,5%/896,00 | 189,59 | 564,80 | Tabela progressiva RFB jan-abr/2025 |
| **mai–dez/2025** | 2.428,80→0% · 2.826,65→7,5%/182,16 · 3.751,05→15%/394,16 · 4.664,68→22,5%/675,49 · acima→27,5%/908,73 | 189,59 | 607,20 | **Lei 15.191/2025** (ex-MP 1.294/2025) |
| **2026** | mesmos limites/parcelas de mai-dez/2025 **+ redutor mensal** | 189,59 | 607,20 | **Lei 15.270/2025, art. 3º-A da Lei 9.250/1995** |

**Redutor mensal do IRRF 2026** (a novidade de 2026): `redutor = min(312,89 ; max(0 ; 978,62 − 0,133145 × rendimento))`,
aplicado até rendimento bruto de **R$ 7.350,00** (isenção efetiva até R$ 5.000,00). Parâmetros do redutor
no mesmo arquivo (`RedutorIrrf.De(coeficienteBase: 978.62, coeficienteRendimento: 0.133145, tetoRedutor: 312.89, limiteRendimento: 7350.00)`).

> 🟡 **Conferir a LÓGICA do redutor:** ele é aplicado **APÓS** a tabela progressiva, **limitado ao
> imposto apurado**, e incide sobre o **rendimento bruto** (antes das deduções). Vale também para o
> IRRF do 13º. Lógica em [`src/Modules/RecursosHumanos/.../TabelasLegais/TabelaIrrf.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Domain/TabelasLegais/TabelaIrrf.cs) (método `CalcularImposto`).
> ⚠️ O próprio código tem `TODO(M10-validate)`: **confirmar limites/parcelas 2026 e coeficientes do
> redutor no ato infralegal anual da Receita.** Este é um item de conferência prioritário.

> **RPPS não é semeado:** a tabela do RPPS depende de lei municipal (o sistema é *fail-closed* — sem
> tabela RPPS cadastrada, o cálculo de servidor efetivo é **recusado**). Maximiliano de Almeida é
> **INSS puro** (sem RPPS próprio) — ver parâmetro de previdência abaixo.

---

## Parâmetros ajustáveis (classes `Parametros*` 🟢)

Pasta: `src/Modules/RecursosHumanos/.../Application/Configuracao/`

### ParametrosFolha — [`ParametrosFolha.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Application/Configuracao/ParametrosFolha.cs)

| Parâmetro | Valor padrão | Norma | O QUE CONFERIR |
|---|---|---|---|
| `TetoRemuneratorio` | (configurado por tenant) | **CF art. 37, XI** | Confira o teto constitucional usado no abate-teto. |
| `AliquotaPasep` | **1.00** (1%) | **LC 8/1970** | Confira a alíquota do PASEP sobre a folha. |
| `DiaLimiteEnvioPeriodico` | **15** | eSocial (I-13) | Dia-limite (mês seguinte) para envio dos eventos periódicos. |
| `PercentualPrimeiraParcela13` | **0,5** | **Lei 4.749/65 art. 2** | Confira a fração da 1ª parcela do 13º. |
| `FracaoTercoConstitucional` | **1/3** | **CF art. 7 XVII** | Confira o 1/3 de férias. |
| Códigos de rubrica (INSS, RPPS, IRRF, 13º, férias, rescisão, pensão, PASEP…) | strings padrão (ex.: `"INSS"`, `"13-SAL"`) | — | Confira se os códigos batem com o cadastro de rubricas do município. |
| `LimiteVariacaoLiquidoConferencia` | **1.000,00** | controle interno | Limiar de variação do líquido por servidor que dispara conferência no pré-fechamento. |

### ParametrosPrevidencia — [`ParametrosPrevidencia.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Application/Configuracao/ParametrosPrevidencia.cs)

| Parâmetro | Valor padrão | Norma | O QUE CONFERIR |
|---|---|---|---|
| `PossuiRppsProprio` | **false** (RGPS/INSS para todo o quadro) | **EC 103/2019** | **CONFIRMADO pelo dono:** o piloto é INSS puro. Define o motor de contribuição (RPPS×INSS) e o evento eSocial (S-1202 RPPS × S-1200 RGPS). Para outro município com RPPS, muda-se para `true`. |

### ParametrosESocial — [`ParametrosESocial.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Application/Configuracao/ParametrosESocial.cs)

| Parâmetro | Norma | O QUE CONFERIR |
|---|---|---|
| `CnpjEnte`, `NomeEnte`, `CnpjEfr` (Ente Federado Responsável) | eSocial MOS S-1.3 (S-1000) | Confira CNPJ do declarante e do EFR (obrigatório p/ ente público). |
| `ClassTrib` (classificação tributária, Tabela 08) | eSocial — tem `TODO(validar-oficial)` | **Sinalize:** a classificação tributária precisa de validação oficial. |
| `CodLotacao` (Tabela 08 / S-1200) | eSocial — tem `TODO(validar-oficial)` | Confira o código de lotação tributária. |
| `Ambiente` | padrão **ProducaoRestrita** | Só vai para Produção quando o ente estiver apto e autorizado. |

### Outros parâmetros

| Arquivo | Conteúdo | Norma | O QUE CONFERIR |
|---|---|---|---|
| [`ParametrosMargem.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Application/Configuracao/ParametrosMargem.cs) | Margem consignável: **35% geral + 5% cartão consignado + 5% cartão benefício** | **Lei 14.131/2021** | Confira os percentuais (default legal; o ente sobrepõe por lei municipal). |
| [`ParametrosAfastamento.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Application/Configuracao/ParametrosAfastamento.cs) | Maternidade 120d (180 se Empresa Cidadã); paternidade 5d (20d); doença 15d pagos pelo ente; licença-prêmio 90d | CF art. 7 XVIII; RJU; Empresa Cidadã | Confira os dias de cada licença frente ao RJU/lei municipal. |
| [`ParametrosPonto.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Application/Configuracao/ParametrosPonto.cs) | CNPJ p/ AFD/AEJ; origem REP padrão; banco de horas 6 meses | **Portaria MTP 671/2021** | `AplicarPortaria671AoEstatutario` tem `TODO(validar-oficial)` — **sinalize:** a 671 regulamenta a CLT; o estatutário segue lei municipal/RJU. |

---

## Regras de negócio da Folha (`.rules.md` 🟡) — conferir contra a norma

Pasta: `src/Modules/RecursosHumanos/rules/`

### Folha e eventos eSocial

| Regra | Arquivo | Norma | O QUE CONFERIR |
|---|---|---|---|
| **Abate-teto** (proventos acima do teto → rubrica de desconto reduz líquido ao limite) | [`FolhaDePagamento.rules.md`](../../src/Modules/RecursosHumanos/rules/FolhaDePagamento.rules.md) | **CF art. 37 XI** | Confira que o abate-teto incide **só na folha Mensal** (não no 13º/férias/rescisão). |
| **Separação RPPS/RGPS** (efetivo → S-1202; demais → S-1200) | FolhaDePagamento.rules.md | **EC 103/2019** | Confira o roteamento de eventos por regime. |
| Fechamento gera S-1299, S-1210, totalizadores S-5001/5002/5003 e DCTFWeb | FolhaDePagamento.rules.md | eSocial S-1.3; **DCTFWeb IN RFB 2.005/2021** | Confira a sequência de fechamento e a não-exclusão de S-1200/S-1202/S-2299 enquanto S-1210 vinculado estiver transmitido. |
| Máquina de estados do evento (Gerado→Assinado→Transmitido→Processado/Rejeitado); lote ≤ 50 eventos e ≤ 5 MB | [`ESocial.rules.md`](../../src/Modules/RecursosHumanos/rules/ESocial.rules.md) | eSocial Leiautes S-1.3 (NT 06/2026); MOS v1.15 | Confira limites de lote e validação XSD antes da transmissão (*fail-closed* sem CNPJ). |
| **Rubricas e incidências** (informativa não integra base; regra do mais vantajoso no IRRF; sem tabela RPPS → recusa) | [`Rubrica.rules.md`](../../src/Modules/RecursosHumanos/rules/Rubrica.rules.md) | Portaria MPS/MF; IRRF RFB; EC 103/2019; **Lei 15.270/2025** | Confira as incidências (INSS/RPPS/IRRF/FGTS) por rubrica e o redutor IRRF 2026. |

### Ciclo de vida do servidor e atos

| Regra | Arquivo | Norma | O QUE CONFERIR |
|---|---|---|---|
| Nomeação→Posse→Exercício; estabilidade 3 anos (efetivo/RPPS) | [`Servidor.rules.md`](../../src/Modules/RecursosHumanos/rules/Servidor.rules.md) | **CF art. 37 II, art. 41**; Lei 8.112/90; S-2200/S-2206/S-2230/S-2299 | Confira a sequência legal e os prazos de eventos (S-2200 até a véspera do exercício). |
| Cargo: regime derivado do tipo; vencimento sujeito ao teto; vagas | [`Cargo.rules.md`](../../src/Modules/RecursosHumanos/rules/Cargo.rules.md) | CF art. 37 II/XI; S-1005/1010/1020 | Confira efetivo→RPPS, comissionado/temporário→RGPS. |
| Portarias: numeração sequencial NNN/AAAA por exercício | [`Portaria.rules.md`](../../src/Modules/RecursosHumanos/rules/Portaria.rules.md) | CF art. 37; **Lei 14.063/2020** (assinatura) | Confira a numeração e a assinatura eletrônica. |
| Plano de carreira (progressão horizontal / promoção vertical) | [`PlanoCarreira.rules.md`](../../src/Modules/RecursosHumanos/rules/PlanoCarreira.rules.md) | CF art. 39 §1; Lei 8.112/90 art. 10; **lei municipal PCCS** | Confira a matriz classes/referências/interstícios contra o PCCS local. |

### Cálculos complementares e descontos

| Regra | Arquivo | Norma | O QUE CONFERIR |
|---|---|---|---|
| **13º** (base separada de IRRF, **sem** desconto simplificado), **férias** (+1/3), **rescisão** | [`CicloAnual.rules.md`](../../src/Modules/RecursosHumanos/rules/CicloAnual.rules.md) | CF art. 7 VIII/XVII; **Lei 7.713/88 art. 12-A**; CLT 129-145/477/484-A; Lei 12.506/11 (aviso prévio); Súmula 386/STJ; STF Tema 985 | Confira a base separada do IRRF do 13º e a matriz de rescisão por tipo×regime. |
| **PASEP** (ente sobre folha bruta) | [`Pasep.rules.md`](../../src/Modules/RecursosHumanos/rules/Pasep.rules.md) | **LC 8/1970**; CF art. 239; Lei 9.715/98 | Confira base = soma dos proventos da competência × alíquota (1% default). Recolhimento real pendente de credencial/canal de produção (`TODO(M10)` no código). |
| **Consignações** (3 baldes de margem; corte por prioridade) | [`Consignacao.rules.md`](../../src/Modules/RecursosHumanos/rules/Consignacao.rules.md) | **Lei 14.131/2021** | Confira que a averbação respeita a margem disponível e a ordem de corte (obrigatória→facultativa→benefício). |
| **Afastamentos** (percentual de remuneração por tipo; conta/não conta tempo) | [`Afastamento.rules.md`](../../src/Modules/RecursosHumanos/rules/Afastamento.rules.md) | Lei 8.112/90; Lei 12.873/13; CF art. 38 | Confira o percentual e o efeito na folha por tipo. |

### Ponto, SST, certidões e contencioso

| Regra | Arquivo | Norma | O QUE CONFERIR |
|---|---|---|---|
| **Ponto** (AFD/AEJ posicional; NSR sequencial; CRC-16; assinado CAdES A1) | [`Ponto.rules.md`](../../src/Modules/RecursosHumanos/rules/Ponto.rules.md) | **Portaria MTP 671/2021** | Confira a aplicabilidade ao estatutário (`TODO(validar-oficial)`) e o conceito de CRC-16 (validação oficial pendente). |
| **Banco de horas** (saldo; prescrição por janela 6m/1a) | [`BancoDeHoras.rules.md`](../../src/Modules/RecursosHumanos/rules/BancoDeHoras.rules.md) | **CLT art. 59 §§2º-5º** | Confira a janela de compensação e a prescrição. |
| **SST** (CAT, ASO/PCMSO, exposição → eventos S-2210/S-2220/S-2240) | [`Sst.rules.md`](../../src/Modules/RecursosHumanos/rules/Sst.rules.md) | **Lei 8.213/91 art. 22** (CAT); NR-07; NR-09; Dec. 3.048/99 | Confira a geração dos eventos SST e os prazos da CAT (1º dia útil; imediato em óbito). Dado sensível LGPD. |
| **Certidão de tempo de serviço** (vedação de concomitância; fator de conversão) | [`CertidaoTempoServico.rules.md`](../../src/Modules/RecursosHumanos/rules/CertidaoTempoServico.rules.md) | EC 103/2019; **Lei 8.213/91 art. 96**; Portaria MPS 154/2008 | Confira a vedação de tempo concomitante e o fator de conversão (oficial pendente). |
| **Processo trabalhista** (provisão por prognóstico) | [`ProcessoTrabalhista.rules.md`](../../src/Modules/RecursosHumanos/rules/ProcessoTrabalhista.rules.md) | **NBC TSP / MCASP**; CLT; LRF art. 50 II | Confira a política de provisionamento (provável/possível/remota) — validação NBC TSP pendente. |
| **Minha Folha** (autosserviço, acesso só ao próprio dado) | [`MinhaFolha.rules.md`](../../src/Modules/RecursosHumanos/rules/MinhaFolha.rules.md) | **LGPD 13.709/2018** art. 7/18/37 | Confira que o servidor só acessa os próprios dados (resolvido pelo JWT). |
| **Relatórios** gerenciais de folha (por UO/fonte; demonstrativo TCE) | [`Relatorios.rules.md`](../../src/Modules/RecursosHumanos/rules/Relatorios.rules.md) | **LRF art. 19/20**; CF art. 169/37 XI | Confira os limites de despesa de pessoal e a fonte (RPPS×RGPS). |

### Remessa de pessoal ao TCE-RS (SICAP/SIAPES)

| Regra | Arquivo | Norma | O QUE CONFERIR |
|---|---|---|---|
| Remessa de atos de pessoal (admissões) ao TCE-RS | [`SicapPessoal.rules.md`](../../src/Modules/RecursosHumanos/rules/SicapPessoal.rules.md) | **TCE-RS SIAPES/SIAPESweb**; CF art. 71; Resoluções TCE-RS | Confira regime jurídico/movimento/motivo (tabelas oficiais). **Leiaute posicional ainda pendente de validação oficial** — detalhe em [`tce-rs-integracao.md`](tce-rs-integracao.md). |

---

## Pontos de atenção a SINALIZAR (revisão federal vigente)

- **DIRF está EXTINTA** (IN RFB 2.181/2024) — as retenções vão para **EFD-Reinf R-4000 + eSocial
  S-1210**. Se aparecer geração de DIRF em 2026, é desatualização. (FONTES-NORMATIVAS D3.)
- **RAIS/CAGED** substituídos pelo eSocial — não deve haver entrega autônoma.
- **LC 173/2020** (congelamento de pessoal): vedações **expiraram em 31/12/2021** — não aplicável a
  2026. O limite de pessoal vigente é a **LRF arts. 18-23**. Se a LC 173 for tratada como regra ativa,
  sinalize para remoção.
- **IRRF 2026:** confirmar limites/parcelas e coeficientes do redutor no ato anual da Receita
  (`TODO(M10-validate)` no seed).

---

## Resumo para o especialista de folha

- **19 regras de negócio** (`.rules.md`) para revisar no módulo RecursosHumanos.
- **6 classes de parâmetros ajustáveis (🟢)** — Folha, Previdência, eSocial, Margem, Afastamento, Ponto —
  além das **tabelas IRRF e INSS** (valores por competência) e do **redutor IRRF 2026**.
- **Prioridade de conferência:** as tabelas IRRF/INSS 2025/2026 (faixas, teto, dedução, simplificado),
  o **redutor do IRRF 2026** (lógica + coeficientes, ainda pendentes de ato RFB), o `PossuiRppsProprio`
  (define o motor previdenciário), e os `TODO(validar-oficial)` do eSocial (classificação tributária,
  lotação) e do ponto (671 ao estatutário).
- **Transmissão eSocial / folha-TCE / SIAPES** está detalhada em [`tce-rs-integracao.md`](tce-rs-integracao.md).

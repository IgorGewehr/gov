---
modulo: Tributos
agregado: TabelaTaxa + Alvara + TabelaCosip + ObraContribuicaoMelhoria
contexto: Tributos (Taxas/TLL, COSIP, Alvarás e Contribuição de Melhoria — lei municipal parametrizável)
poder: Executivo
schema: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "CTN arts. 77–80 (taxas: poder de polícia/serviço); SV 19/STF (taxa de lixo); SV 29/STF (elemento da base, não a base de imposto; vedado capital)"
  - "CTN art. 78 (poder de polícia) — Taxa de Licença de Localização/Funcionamento (TLL); alvará é o ato, a TLL é o tributo"
  - "CF art. 149-A (EC 39/2002); STF RE 573.675/Tema 44 (COSIP sui generis; progressividade válida)"
  - "CTN arts. 81–82 + DL 195/1967 (Contribuição de Melhoria: valorização; limites total/individual; edital + impugnação ≥ 30 dias)"
---

<!-- manifest
commands: ConfigurarTabelaTaxa, LancarTaxa, EmitirAlvara, RenovarAlvara, ConfigurarTabelaCosip, LancarCosip, PublicarEditalMelhoria, AdicionarImovelBeneficiado, EncerrarPrazoImpugnacao, RatearContribuicaoMelhoria
queries:
domainEvents: TabelaTaxaCriada, TabelaTaxaPublicada, AlvaraEmitido, AlvaraRenovado, AlvaraVencido, AlvaraCancelado, TabelaCosipCriada, TabelaCosipPublicada, ObraMelhoriaEditalPublicado, ObraMelhoriaImpugnacaoEncerrada, ObraMelhoriaRateada, ObraMelhoriaCancelada
integrationEventsPublished:
integrationEventsConsumed:
-->

# Taxas/TLL, COSIP, Alvarás e Contribuição de Melhoria — Regras Normativas

> **Fonte da verdade.** Espécies tributárias municipais além de IPTU/ISS/ITBI. **Nenhum valor/faixa/
> alíquota vive no código** — tudo vem da lei municipal (CTM e leis específicas), parametrizável por
> tenant (CLAUDE.md §16). Cada motor é determinístico e auditável (sem relógio no cálculo). Geram
> `Lancamento` + DAM (reuso do existente). Ver M6-DESIGN §3.2–§3.5.

---

## 1. Linguagem ubíqua

- **TabelaTaxa** — tabela de uma taxa (lei municipal), por código + exercício. Espécie: poder de polícia,
  serviço ou licença (TLL). Modo de cálculo: **ValorFixo**, **PorUnidade** (valor × quantidade-base) ou
  **PorFaixa** (faixas escalonadas). Invariante CTN art. 80 + SV 29: a quantidade-base é um elemento
  físico (metragem/unidade/atividade), **nunca** o capital/faturamento, e não replica a base de imposto.
- **Alvara** — **ato administrativo de polícia** (licença), vinculado ao contribuinte e (opcional) ao
  imóvel; tem vigência e renovação. O tributo correlato é a **TLL** (Taxa de Licença), lançada à parte.
- **TabelaCosip** — tabela de COSIP/CIP por exercício; faixas de consumo (kWh) por classe de consumidor,
  progressivas (RE 573.675). Caminho de **lançamento próprio** para não faturados pela distribuidora
  (a cobrança na fatura é objeto de convênio à parte).
- **ObraContribuicaoMelhoria** — obra pública sujeita à contribuição (fato gerador = **valorização**).
  Máquina de estados: `EditalPublicado → ImpugnacaoEncerrada → Rateada` (ou `Cancelada`). Exige **edital
  prévio** (memorial, custo, zona, fator de absorção, parcela a financiar) e **prazo de impugnação ≥ 30
  dias** (CTN art. 82). Rateio proporcional à valorização, respeitando os **dois limites** (CTN art. 81):
  total ≤ custo financiável; individual ≤ valorização de cada imóvel.

---

## 2. Comandos (escrita)

- **`ConfigurarTabelaTaxaCommand`** → cria e publica a tabela de uma taxa (código, espécie, modo,
  exercício, valor base, faixas). Retorna o `Guid`.
- **`LancarTaxaCommand`** → calcula a taxa pela tabela vigente e gera `Lancamento` `Taxa` + DAM. Vínculo
  opcional a imóvel.
- **`EmitirAlvaraCommand`** → emite o alvará (ato de polícia) e lança a **TLL** correspondente (a partir
  da tabela de licença vigente): `Lancamento` `Taxa` + DAM.
- **`RenovarAlvaraCommand`** → renova o alvará por novo período e lança a **TLL de renovação anual**.
- **`ConfigurarTabelaCosipCommand`** → cria e publica a tabela de COSIP por faixa de consumo/classe.
- **`LancarCosipCommand`** → apura a COSIP (classe + consumo) e gera `Lancamento` `Cosip` + DAM
  (lançamento próprio).
- **`PublicarEditalMelhoriaCommand`** → publica o edital da obra (CTN art. 82), prazo de impugnação ≥ 30d.
- **`AdicionarImovelBeneficiadoCommand`** → adiciona um imóvel beneficiado com a valorização individual
  apurada (só na fase de edital).
- **`EncerrarPrazoImpugnacaoCommand`** → encerra o prazo (≥ fim do prazo) e habilita o rateio.
- **`RatearContribuicaoMelhoriaCommand`** → rateia proporcionalmente à valorização (respeitando os dois
  limites) e gera **um `Lancamento` `ContribuicaoMelhoria` + DAM por imóvel** com parcela positiva.

## 3. Consultas (leitura)

- Nenhuma nesta versão (apuração/lançamento são comandos; preview pode ser adicionado depois).

---

## 4. Eventos

- **Domínio (in-process):**
  - `TabelaTaxaCriada` / `TabelaTaxaPublicada`
  - `AlvaraEmitido` / `AlvaraRenovado` / `AlvaraVencido` / `AlvaraCancelado`
  - `TabelaCosipCriada` / `TabelaCosipPublicada`
  - `ObraMelhoriaEditalPublicado` / `ObraMelhoriaImpugnacaoEncerrada` / `ObraMelhoriaRateada` /
    `ObraMelhoriaCancelada`
- **Integração:** nenhum publicado/consumido nesta versão.

---

## 5. RBAC

- `tributos.gerenciar` — configurar tabelas e lançar taxas/TLL/COSIP; emitir/renovar alvará; conduzir o
  processo de Contribuição de Melhoria (edital, impugnação, rateio).

---

## 6. Pendências `// TODO(validar-oficial)`

- **Taxas/TLL:** códigos, valores, faixas, atividades/CNAE e classes de risco conforme o **CTM de
  Maximiliano de Almeida/RS**.
- **COSIP:** faixas/classes/valores da **lei municipal de COSIP**; convênio de cobrança na fatura com a
  distribuidora (RGE/CEEE na região) — caminho de fatura modelado à parte.
- **Contribuição de Melhoria:** memorial/custo/zona/fator de absorção e a apuração da **valorização
  individual** vêm da **lei específica de cada obra**; o sistema apenas registra e rateia (CTN arts. 81–82).

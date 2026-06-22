# Auditoria adversarial — Motor IPTU (Tributos)

> Auditor adversarial de correção fiscal. Régua oficial: `docs/architecture/m6-prep/M6-DESIGN.md` (§1.2/§1.3) e `pesquisa-imobiliario-iptu.md`.
> Escopo: `CalculadoraValorVenal` + `CalculadoraIptu` + `TabelaAliquotaIptu`/PGV. Premissa: há bugs sutis até prova em contrário.
> Data: 2026-06-22. Não rodei dotnet (5080 em uso por outro processo); análise estática + raciocínio aritmético.

Arquivos analisados:
- `src/Modules/Tributos/.../Domain/Calculo/CalculoValorVenal.cs`
- `src/Modules/Tributos/.../Domain/Calculo/CalculoIptu.cs`
- `src/Modules/Tributos/.../Domain/Pgv/TabelaAliquotaIptu.cs`
- `src/Modules/Tributos/.../Domain/Pgv/FaixaAliquotaIptu.cs`
- `src/Modules/Tributos/.../Domain/Pgv/PlantaValores.cs`
- `src/Modules/Tributos/.../Domain/Imoveis/CaracteristicasImovel.cs`
- `src/Modules/Tributos/.../Application/Iptu/ApuradorIptu.cs`
- `src/Modules/Tributos/.../Domain/ValueObjects/ValorMonetario.cs`
- `tests/.../MotorIptuTests.cs`

---

## A. BUGS REAIS (cálculo erra ou rota errada)

### BUG-1 — Faixa progressiva sem teto deixa o topo a descoberto: valor venal == `SemTeto` lança exceção
**`TabelaAliquotaIptu.cs:32` (const `SemTeto = 1e12`) + `:144` (`AliquotaPara`) + `:127` (`Publicar`)**

`AliquotaPara` casa a faixa com `valorVenal >= min && valorVenal < max`. A faixa "sem teto" é criada como `[min, SemTeto)`, **exclusiva no topo**. Logo, um imóvel cujo valor venal seja **exatamente 1.000.000.000.000,00** (ou maior — ver BUG-7) **não casa nenhuma faixa** e `AliquotaPara` lança `InvalidOperationException("Nenhuma faixa de alíquota cobre o valor venal …")`. `Publicar` valida continuidade a partir de zero e contiguidade, mas **não valida que a última faixa cobre o infinito** — ela só confere que `min == limite` acumulado; o teto da última faixa pode ser qualquer número e a tabela é publicada mesmo assim.

- **Caso-limite:** valor venal grandes empreendimentos/glebas industriais atingindo o sentinela; ou município que cadastra última faixa com teto != `SemTeto`.
- **Certo ou errado?** ERRADO. O nome/contrato diz "sem teto" mas o intervalo é exclusivo. Não é teórico: o sentinela é só 1e12 = R$ 1 trilhão de valor venal, alcançável por gleba grande × VUT alto, e qualquer tabela publicada com último teto finito gera "buraco" silencioso no topo. IPTU que deveria ser lançado vira exceção (não-lançamento) → renúncia de receita não auditada.
- **Teste a adicionar:**
  - `AliquotaPara(SemTeto)` deve retornar a alíquota da última faixa (hoje lança — falha que expõe o bug).
  - `Publicar` deve **rejeitar** tabela cuja última faixa não termine em `SemTeto` (cobertura do topo), análogo à validação de lacuna a partir de zero.

### BUG-2 — Rota predial × territorial decidida só por `AreaConstruida > 0`, ignorando `TipoUso.Territorial` (e sem invariante de consistência)
**`ApuradorIptu.cs:46` (`edificado = imovel.Caracteristicas.Edificado`) + `CaracteristicasImovel.cs:74` (`Edificado => AreaConstruida > 0`)**

A seleção da tabela predial × territorial usa exclusivamente `AreaConstruida > 0`. Não há nenhuma invariante ligando `TipoUso.Territorial` a `AreaConstruida == 0`. Dois casos divergem do esperado fiscal:
1. Imóvel `TipoUso = Territorial` com `AreaConstruida > 0` (construção residual/inservível lançada por engano ou benfeitoria irrelevante) → roteado para tabela **predial**, alíquota predial (tipicamente menor). Subtributação do lote essencialmente vago.
2. Imóvel edificado com `TipoUso = Territorial` → `fatorUso` é buscado com a chave `"TERRITORIAL"` (`CalculoValorVenal.cs:67`), aplicando fator de lote a uma construção real.

- **Caso-limite:** incoerência entre `TipoUso` e `AreaConstruida`. O cadastro permite criar ambos sem erro (`CaracteristicasImovel.Criar` não cruza os dois campos).
- **Certo ou errado?** ERRADO como invariante de domínio. A progressividade/diferenciação territorial×predial é justamente o eixo da EC 29 e do art. 182 §4 CF (função social do lote vago). Decidir "edificado" só pela área, sem amarrar ao uso declarado, abre divergência entre o que o cadastro diz e o que o motor tributa.
- **Teste a adicionar:** invariante em `CaracteristicasImovel.Criar` — `TipoUso.Territorial` exige `AreaConstruida == 0` (ou vice-versa); e teste no `ApuradorIptu` garantindo que `Territorial` sempre cai na tabela territorial.

### BUG-3 — `FracaoIdeal` aplicada sobre terreno + construção integrais → super/subtributação de unidade autônoma
**`CalculoValorVenal.cs:74` (`bruto = (valorTerreno + valorConstrucao) * caracteristicas.FracaoIdeal`)**

Para unidade autônoma de condomínio, o BCI normalmente já registra a **área construída privativa da unidade** (`AreaConstruida` da unidade), enquanto a **fração ideal** se aplica ao **terreno comum**. Aqui a fração multiplica o **conjunto inteiro** (terreno + construção da unidade). Se `AreaConstruida` já é a da unidade, a construção é indevidamente reduzida pela fração (subtributa); se `AreaConstruida` for a do prédio inteiro, o terreno fica correto mas exige convenção oposta. O modelo não documenta qual convenção o cadastro adota, e o único teste de fração não existe (ver C-1).

- **Caso-limite:** apartamento — fração ideal 0,015 sobre `(terreno_do_prédio + construção_da_unidade)`.
- **Certo ou errado?** POTENCIALMENTE ERRADO / ambíguo. A fórmula do M6-DESIGN §1.2 não posiciona a fração ideal; a literatura aplica fração ao **terreno** (`AreaTerreno × VUT × fração`) e usa a área privativa direta na construção. Aplicar a fração ao total é uma decisão que precisa estar explicitada e validada com a lei municipal — hoje é silenciosa e sem teste.
- **Teste a adicionar:** caso de apartamento com fração 0,02, conferindo o valor venal contra um número calculado à mão segundo a convenção oficial adotada; e fixar a convenção em doc/comentário.

---

## B. RISCOS DE CORREÇÃO sem teste (TCE exigiria) — comportamento atual plausível mas não provado

### RISCO-4 — Faixa de depreciação por idade não tem semântica de FAIXA: usa a idade exata como chave inteira
**`CalculoValorVenal.cs:97-106` (`FaixaIdade`) + `:66` (`ObterFator(Depreciacao, FaixaIdade(...))`)**

`FaixaIdade` devolve a **idade em anos como string exata** ("0","1","2",…). A PGV teria que cadastrar **um fator para cada idade inteira**. Se a lei define faixas (ex.: 0–5, 6–10, 11–20 anos), a chave "7" **não casa** e `ObterFator` retorna **1m (neutro)** silenciosamente (`PlantaValores.cs:157-165`) — depreciação **não aplicada**, valor venal **superestimado**. Além disso, `idade = DateTime.UtcNow.Year - ano` (`:104`) torna o cálculo **não determinístico por exercício**: o IPTU de 2026 é apurável retroativamente em 2028 com depreciação diferente. Isso fere "determinístico e reproduzível pelo TCE-RS" (cabeçalho do próprio arquivo).

- **Caso-limite:** idade no limite de faixa; reprocessamento histórico; ano de construção = ano corrente (idade 0); idade negativa (construção futura) é clampada para "0" (`:105`) — silenciosa.
- **Certo ou errado?** O fallback-neutro está documentado como intencional, mas para depreciação ele **mascara erro de parametrização** (deveria, no mínimo, ser observável). E o uso de `DateTime.UtcNow.Year` em vez do **exercício do fato gerador** é um bug de reprodutibilidade fiscal.
- **Teste a adicionar:** (1) depreciação deve usar o **exercício**, não o relógio — passar exercício ao motor e testar que reapuração dá o mesmo número; (2) idade no limite de faixa quando a PGV usa faixas reais; (3) chave de depreciação ausente — decidir se é erro explícito ou neutro (TCE: ausência de fator obrigatório deveria falhar, não ser silenciosamente 1).

### RISCO-5 — `ObterFator` retorna 1 (neutro) para padrão/uso ausentes: zona/fator faltante não falha
**`PlantaValores.cs:157-165` + `CalculoValorVenal.cs:65-67`**

`fatorPadrao`, `fatorDepreciacao`, `fatorUso` viram **1** quando a chave não está na PGV. Diferente de `ObterZona`, que **lança** quando a zona não existe (`CalculoValorVenal.cs:59-61`). Resultado: imóvel "ALTO" padrão numa PGV que só cadastrou "MEDIO" é tributado como se o fator fosse 1 — base de cálculo errada, sem aviso. Isso é exatamente o tipo de furo que o TCE aponta como "lançamento sem fundamento na PGV".

- **Caso-limite:** padrão/uso do imóvel não consta na PGV vigente.
- **Certo ou errado?** Comportamento é intencional/documentado, mas é **decisão de risco fiscal**: silencioso onde a zona é explícita. Não há teste cobrindo "fator ausente".
- **Teste a adicionar:** teste de fator de padrão/uso ausente — confirmar (e idealmente mudar para falhar) o comportamento; ao menos registrar na memória de cálculo que o fator veio do default.

### RISCO-6 — Isenção que zera vs. ordem isenção→desconto não testada; sem teste de cota única
**`CalculoIptu.cs:84-88`**

A ordem é: bruto → isenção (% sobre bruto) → desconto (% sobre o **saldo após isenção**). Com `PercentualIsencao = 100`, `aposIsencao = 0` e qualquer desconto rende `impostoDevido = 0` — provavelmente correto, mas **sem teste**. O desconto de **cota única** (régua §4/§4.1: prática central, SP 3%/Recife 5%/Natal 16% — variável por lei) está modelado como o `PercentualDesconto` genérico, **sem distinção** entre desconto de adimplência e desconto à vista, e **sem teste** de cota única. Arredondamento `AwayFromZero` em cada etapa pode acumular ±0,01 vs. desconto aplicado de uma só vez.

- **Caso-limite:** isenção 100% (imunidade/isenção total que zera); desconto de cota única; combinação isenção+desconto com arredondamento.
- **Certo ou errado?** Provavelmente certo, **não provado**. O TCE exige memória de cálculo da isenção/desconto bate-a-bate.
- **Teste a adicionar:** (1) isenção 100% → devido 0; (2) cota única (desconto à vista) num caso conhecido; (3) ordem isenção-antes-de-desconto com valor que exponha arredondamento (ex.: bruto 1.999, isenção 33%, desconto 7%).

### RISCO-7 — Sem proteção/teste de overflow decimal e de valor venal gigante
**`CalculoValorVenal.cs:69-74`**

`AreaConstruida × VUC × fatorPadrao × fatorDepreciacao × fatorUso` é `decimal` sem guarda. `decimal` satura em ~7,9e28. Com áreas/VUC/fatores extremos (ex.: 1e14 m² × 1e14 R$/m² × 1e3) o produto **estoura `OverflowException`** — não é o caminho normal, mas é entrada não validada (não há teto em `AreaTerreno`/`AreaConstruida`/`VUC`/fator além de "não-negativo"/">0"). Mais provável na prática: valor venal > `SemTeto` (1e12) cai no BUG-1.

- **Caso-limite:** dados de cadastro absurdos (digitação: 100000000 m²), gleba industrial gigante, fator mal cadastrado.
- **Certo ou errado?** Falta defesa. TCE-RS valoriza robustez contra entrada inválida; hoje vira exceção não mapeada (500) em vez de erro de domínio.
- **Teste a adicionar:** valor venal próximo de `SemTeto` (deve casar a faixa — ver BUG-1) e entrada que estoura decimal (deve dar erro de domínio claro, não `OverflowException`).

### RISCO-8 — `AreaTerreno == 0` e `AreaConstruida == 0` (imóvel "fantasma") produz valor venal 0 e IPTU 0 sem aviso
**`CaracteristicasImovel.cs:93-94` (só `ThrowIfNegative`) + `CalculoValorVenal.cs`**

Ambas as áreas podem ser **0** (só `>= 0`). Imóvel com terreno 0 e construção 0 → valor venal 0 → IPTU 0, lançado normalmente. Não há invariante "imóvel tem ao menos terreno". Lançamento de R$ 0,00 silencioso é achado clássico de TCE (cadastro inconsistente gerando crédito zero).

- **Caso-limite:** ambas as áreas zero; terreno 0 com construção > 0 (construção sem lote).
- **Certo ou errado?** Falta invariante. Provavelmente deveria exigir `AreaTerreno > 0`.
- **Teste a adicionar:** rejeitar (ou sinalizar) imóvel com terreno 0; testar lançamento de valor venal 0.

---

## Resumo

**Bugs reais (3):**
- BUG-1: faixa "sem teto" exclusiva no topo → valor venal == `SemTeto` (e qualquer tabela com último teto finito) não casa faixa e lança; `Publicar` não exige cobertura do topo. (`TabelaAliquotaIptu.cs:32,127,144`)
- BUG-2: rota predial×territorial só por `AreaConstruida>0`, sem invariante com `TipoUso.Territorial` → subtributação de lote/fator de uso errado. (`ApuradorIptu.cs:46`, `CaracteristicasImovel.cs:74`)
- BUG-3: `FracaoIdeal` multiplica terreno+construção integrais, convenção não fixada → super/subtributa unidade autônoma. (`CalculoValorVenal.cs:74`)

**Casos só faltando teste / risco (5):** RISCO-4 (depreciação por idade exata + `DateTime.UtcNow.Year` não-reprodutível), RISCO-5 (fator ausente vira 1 silencioso), RISCO-6 (isenção 100%/cota única/ordem+arredondamento), RISCO-7 (overflow decimal / valor venal gigante), RISCO-8 (áreas zero → IPTU 0 sem invariante).

**Os 3 mais críticos (corrigir antes do go-live M6):**
1. **RISCO-4** — depreciação usa `DateTime.UtcNow.Year` e idade exata como chave: quebra **reprodutibilidade do fato gerador** (o mesmo lançamento reapurado em ano diferente dá outro valor) e **silencia** depreciação quando a PGV usa faixas — viola o requisito de determinismo auditável do TCE-RS. É erro de base de cálculo de **todo** imóvel edificado.
2. **BUG-1** — "sem teto" que não cobre o topo: não-lançamento silencioso (exceção) de imóveis de valor venal alto e de qualquer tabela com último teto finito = renúncia de receita não auditada; `Publicar` deveria barrar.
3. **BUG-2** — incoerência territorial×predial: erra a diferenciação que é o coração da EC 29 / função social (art. 182 §4 CF); subtributa lote vago com construção residual.

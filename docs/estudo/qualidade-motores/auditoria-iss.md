# Auditoria adversarial — Motor ISS (Tributos)

> Auditor adversarial de correção fiscal. Premissa: há bugs sutis até prova em contrário.
> Régua oficial: `docs/architecture/m6-prep/M6-DESIGN.md` §2.1/§2.2 + `verificacao-iss-nfse-adn.md` + LC 116/2003.
> Escopo: `CalculadoraIss` + `ApuracaoIss` + tabela de alíquotas LC 116 + apuração mensal.
> Data: 2026-06-22.

Arquivos no escopo:
- `src/Modules/Tributos/.../Domain/Calculo/CalculoIss.cs` (CalculadoraIss + ClassificarModalidade)
- `src/Modules/Tributos/.../Domain/Iss/ApuracaoIss.cs`, `ItemApuracaoIss.cs`, `TabelaAliquotaIss.cs`, `ItemAliquotaIss.cs`
- `src/Modules/Tributos/.../Domain/Nfse/NotaFiscalServico.cs`
- `src/Modules/Tributos/.../Domain/ValueObjects/ValorMonetario.cs`, `Competencia.cs`
- `src/Modules/Tributos/.../Application/Iss/ApurarIssMensal.cs`, `ConfigurarTabelaAliquotaIss.cs`
- `src/Modules/Tributos/.../Infrastructure/.../IssItbiRepositories.cs`, `NfseSincronizador.cs`, `NotaFiscalServicoConfiguration.cs`
- `tests/Tensorroot.Gov.Modules.Tributos.Tests/MotorIssTests.cs`

Legenda: **BUG** = comportamento incorreto que produz cálculo/classificação errados ou perda de receita. **GAP-TESTE** = comportamento provavelmente correto mas sem teste que o TCE exigiria. **DESIGN/BLOQUEADOR** = decisão a confirmar contra lei municipal/leiaute oficial.

---

## ACHADO 1 — Município de incidência (LC 116 art. 3º) NUNCA é verificado contra o tenant — **BUG (correção fiscal)**

- **Arquivo:linha:** `Domain/Calculo/CalculoIss.cs:58-88` (Apurar não lê `nota.MunicipioIncidenciaIbge`) + `Infrastructure/.../IssItbiRepositories.cs:62-76` (`ListarVigentesPorPrestadorCompetenciaAsync` filtra só por `PrestadorCnpj`, `Competencia`, `Situacao == Normal`).
- **Caso-limite (do enunciado):** NFS-e cujo `MunicipioIncidenciaIbge` ≠ município do tenant. Ex.: prestador estabelecido no município, mas serviço dos subitens do art. 3º (construção civil 7.02/7.05, limpeza, vigilância, etc.) cujo ISS é devido **no local da prestação** (outro município). Ou o inverso: prestador de fora cujo serviço incide no município do tenant.
- **Certo ou errado?** **ERRADO / risco material.** O `ApuradorIss` (M6-DESIGN §2.2) deve classificar "ISS próprio = prestador **no município**, art. 3º regra geral **+ exceções**". O código trata TODA nota emitida pelo CNPJ do prestador como ISS próprio do tenant, ignorando as exceções de incidência do art. 3º (itens em que o imposto é devido no local da obra/serviço). Resultado:
  - **Super估imação**: lança ISS próprio sobre nota cujo ISS é devido a OUTRO município → lançamento (`Lancamento` `TipoTributo.Iss`, `ApurarIssMensal.cs:104`) potencialmente indevido contra o contribuinte, que vira Dívida Ativa errada — exatamente o que o TCE glosa.
  - O campo `MunicipioIncidenciaIbge` existe no modelo e é persistido (`NotaFiscalServicoConfiguration.cs:38`), mas é **dado morto** no motor.
- **Teste a adicionar:** `Nota_com_municipio_de_incidencia_diferente_do_tenant_nao_gera_iss_proprio_do_tenant` — exige decisão de design: o motor precisa receber/conhecer o código IBGE do tenant e (a) classificar como "fora do município" (não-próprio) ou (b) recusar a nota da base própria. Hoje não há sequer um ponto onde isso possa ser testado, porque o IBGE do tenant não entra na `Apurar`.
- **Ação:** propagar o código IBGE do município do tenant até `CalculadoraIss.Apurar` (ou filtrar na query `ListarVigentes...` por `MunicipioIncidenciaIbge == ibgeTenant`), com regra explícita por subitem do art. 3º. **Bloqueador:** mapear o campo de local de incidência no XSD nacional (M6-DESIGN §2.1 "local de incidência" — `// TODO(validar-oficial)`).

---

## ACHADO 2 — Item "00.00" não-classificado pode receber alíquota e ser cobrado silenciosamente — **BUG latente (fail-OPEN, não fail-closed)**

- **Arquivo:linha:** `Infrastructure/Nfse/NfseSincronizador.cs:20,44-46` (`ItemListaNaoClassificado = "00.00"` quando o ADN não informa o item) + `Domain/Iss/TabelaAliquotaIss.cs:79-90` (`DefinirItem` aceita qualquer string, inclusive "00.00") + `CalculoIss.cs:73` (`ObterItem` casa por string).
- **Caso-limite:** o gateway não informa o item da lista → nota entra como "00.00". Se a tabela municipal (por erro/conveniência de parametrização) definir um item "00.00", a apuração **calcula e cobra** ISS sobre uma nota cuja atividade é **desconhecida**.
- **Certo ou errado?** **Parcialmente errado.** O comentário em `NfseSincronizador.cs:42` promete "a apuração falhará explicitamente se a tabela não cobrir esse item". Isso só é verdade **se ninguém cadastrar "00.00"**. Não há invariante que **proíba** "00.00" (ou string vazia/whitespace já é barrada, mas "00.00" não) na tabela. O fail-closed depende de disciplina operacional, não do código — frágil para algo sob escrutínio do TCE.
- **Teste a adicionar:** (a) `Tabela_rejeita_item_nao_classificado_00_00` (invariante de domínio a criar) **ou** (b) `Apurar_nota_item_nao_classificado_sempre_falha_explicitamente` garantindo que "00.00" nunca é apurável independentemente da tabela. Também falta teste cobrindo o caminho `NfseSincronizador` → item ausente → "00.00".
- **Ação:** tornar "00.00" um sentinela reservado que `CalculadoraIss.Apurar` recusa explicitamente (ou que `DefinirItem` proíbe), garantindo fail-closed real.

---

## ACHADO 3 — `ValorMonetario.Somar` não re-arredonda e pode acumular > 2 casas — **BUG potencial de centavos (round-then-sum vs sum-then-round)**

- **Arquivo:linha:** `Domain/ValueObjects/ValorMonetario.cs:30-34` (`Somar` faz `new ValorMonetario(Valor + outro.Valor)` — construtor privado **sem** `decimal.Round`). Comparar com `De` (linha 24) e `AplicarPercentual` (linha 42), que arredondam.
- **Caso-limite:** múltiplas NFS-e no mês (caso do enunciado). Cada `IssApurado` já vem arredondado a 2 casas de `AplicarPercentual`, então a soma de valores de 2 casas continua com 2 casas — **na prática hoje não estoura**. PORÉM o invariante "ValorMonetario sempre tem 2 casas" está documentado na linha 6 ("arredondado a 2 casas") e é **violável**: se algum dia uma parcela entrar com mais casas (ex.: refactor que some um valor não arredondado), `Somar` propaga o erro sem proteção. É uma bomba-relógio de auditoria.
- **Certo ou errado?** **Hoje correto por coincidência** (toda parcela somada já vem arredondada), **mas o VO não garante o próprio invariante.** Para um motor que "processa dinheiro público sob escrutínio do TCE", o VO monetário deve ser fechado sob soma.
- **Teste a adicionar:** `Somar_preserva_invariante_de_2_casas` e `Apuracao_de_muitas_notas_nao_acumula_erro_de_centavo` (somar p.ex. 333 notas de R$ 0,01 e conferir total exato). Hoje **não há nenhum teste de agregação monetária de múltiplas notas** além do trio próprio/retido/substituição do `Livro_eletronico...` (3 notas, 1 por modalidade).
- **Ação:** arredondar dentro de `Somar` (ou validar 2 casas no construtor privado).

---

## ACHADO 4 — Precedência substituição > retido > próprio: **correta**, mas o teste é fraco — **GAP-TESTE**

- **Arquivo:linha:** `CalculoIss.cs:95-108` (`ClassificarModalidade`).
- **Análise adversarial:** a ordem está certa conforme M6-DESIGN §2.2 e LC 116 art. 6º (substituição é a responsabilidade mais forte). `SubstituicaoTributaria` vence; depois `IssRetidoNaFonte (XML) || RetencaoObrigatoria (lei)`; senão próprio. **Lógica correta.**
- **Lacunas de cobertura (casos que o TCE pediria):**
  1. `substituicaoTributaria == true` **E** `retencaoObrigatoria == true` no mesmo item → deve dar **Substituição**. Hoje o teste `Iss_por_substituicao...` (MotorIssTests.cs:90) só combina substituição + `retido(XML)=true`, **não** com `retencaoObrigatoria=true`. Falta provar a precedência sobre a retenção **da lei**.
  2. `substituicaoTributaria == true` **E** XML `retido=false` → ainda Substituição (não testado isoladamente sem o ruído do `retido:true`).
  3. `retencaoObrigatoria=true` **E** XML `retido=false` já é coberto (linha 76); mas falta o oposto explícito: `retencaoObrigatoria=false` **E** XML `retido=true` está coberto (linha 63). OK.
- **Teste a adicionar:** `Substituicao_precede_retencao_obrigatoria_da_lei_municipal` (item com `substituicao=true, retencaoObrigatoria=true`).

---

## ACHADO 5 — NFS-e **substituída** não tem teste de exclusão da base — **GAP-TESTE (assimetria com cancelada)**

- **Arquivo:linha:** `NotaFiscalServico.cs:124` (`VigenteParaApuracao => Situacao == Normal`) + `:192-200` (`Substituir`). `CalculoIss.cs:68-71` recusa nota não-vigente.
- **Caso-limite (do enunciado):** NFS-e **substituída** na apuração. O código exclui corretamente (qualquer situação ≠ Normal cai), mas:
  - `MotorIssTests.cs:114` testa **apenas `Cancelar()`** (`Nota_cancelada_sai_da_base`). **Não há teste para `Substituir()`** — a mensagem de exceção esperada seria `*Substituida*`. A simetria não está provada.
  - **Idempotência** de `Cancelar`/`Substituir` (linhas 180-186 / 194-199) **não é testada**.
  - **Transição cruzada** (cancelar uma nota já substituída, ou vice-versa) não é testada nem barrada — hoje `Cancelar()` numa nota `Substituida` **sobrescreve** para `Cancelada` (linha 180 só curto-circuita se já `Cancelada`). Possível inconsistência de histórico fiscal.
- **Teste a adicionar:** `Nota_substituida_sai_da_base_de_apuracao` (espelho de cancelada), `Cancelar_e_Substituir_sao_idempotentes`, e decidir/testar a transição cancelada↔substituída (provável: deve ser proibida ou ter precedência definida).

---

## ACHADO 6 — Apuração e dedup ignoram `TenantId` no nível da consulta — **BUG de isolamento (depende de Global Query Filter)**

- **Arquivo:linha:** `IssItbiRepositories.cs:62-76` (consulta filtra por `PrestadorCnpj`+`Competencia`+`Situacao`, **sem `TenantId` explícito**) e `NotaFiscalServicoRepository.cs:11-12` (`ExistePorChaveAsync` faz `AnyAsync(nota => nota.ChaveAcesso == chave)` **sem TenantId**) vs. índice único `(TenantId, ChaveAcesso)` em `NotaFiscalServicoConfiguration.cs` (`HasIndex(... ).IsUnique()`).
- **Caso-limite:** a mesma chave de acesso existir em dois tenants (CLAUDE.md §5: Executivo e Legislativo são tenants distintos; e múltiplos municípios clientes). `ExistePorChaveAsync` retorna `true` **cross-tenant** se o Global Query Filter não estiver ativo no caminho do Worker → a nota do tenant B seria **descartada como duplicata** do tenant A (perda de nota / sub-apuração). O índice é `(TenantId, ChaveAcesso)`, mas a query de dedup só olha `ChaveAcesso`.
- **Certo ou errado?** **Correto SE e SOMENTE SE** o Global Query Filter por `TenantId` estiver garantidamente aplicado no `TributosDbContext` **inclusive no contexto do `NfseSincronizador`/Worker** (CLAUDE.md §5 diz que Workers resolvem tenant por iteração explícita — caminho de maior risco de filtro ausente). Não há teste que prove o isolamento de tenant na dedup nem na apuração.
- **Teste a adicionar:** `Dedup_nao_confunde_mesma_chave_entre_tenants` e `Apuracao_nao_inclui_notas_de_outro_tenant` (teste de integração com o filtro global ativo). Pelo CLAUDE.md §12, isolamento de tenant é cobertura obrigatória — está faltando para o ISS.

---

## ACHADO 7 — Seleção de tabela vigente ignora `TenantId` e não tem fim de vigência — **BUG potencial + GAP-TESTE**

- **Arquivo:linha:** `IssItbiRepositories.cs:23-35` (`ObterVigentePorCompetenciaAsync`: `Where(Vigente && VigenciaInicioAaaaMm <= aaaaMm).OrderByDescending(VigenciaInicio).First`).
- **Casos-limite:**
  1. **Sem `TenantId` explícito** (mesmo risco do ACHADO 6) — pega a tabela de outro tenant se o filtro global falhar.
  2. **Sem `VigenciaFim`:** o modelo `TabelaAliquotaIss` só tem `VigenciaInicioAaaaMm` (TabelaAliquotaIss.cs:47). A "vigente" é sempre a de maior início ≤ competência. Funciona para versionamento simples, **mas** o M6-DESIGN §2.1 especifica chave `(TenantId, Tipo, ExercicioOuVigenciaInicio, VigenciaFim?)` — o fim de vigência foi omitido. Não dá para revogar uma tabela sem publicar outra posterior. Apurar uma competência **anterior** à primeira tabela publicada retorna `null` → o handler lança "Não há tabela vigente" (`ApurarIssMensal.cs:84-85`), o que é aceitável, mas não testado.
  3. **Duas tabelas com o mesmo `VigenciaInicioAaaaMm`** ambas `Vigente=true`: `OrderByDescending(VigenciaInicio).First()` é **não-determinístico** (sem desempate por data de publicação/Id). Para um motor fiscal determinístico, isso é um furo.
- **Teste a adicionar:** `Apuracao_usa_a_tabela_de_maior_vigencia_que_nao_ultrapassa_a_competencia` (com 2 tabelas: 202401 e 202501, apurar 202503 → usa a de 202501); `Competencia_anterior_a_qualquer_tabela_falha_explicitamente`; e definir/testar desempate quando há colisão de início de vigência.
- **Ação:** decidir se haverá `VigenciaFim` (alinhar ao M6-DESIGN §2.1) e desempate determinístico.

---

## ACHADO 8 — Faixa de alíquota não impõe o piso de 2% / teto de 5% (CF art. 156 §3º / LC 116) — **DESIGN / GAP-TESTE**

- **Arquivo:linha:** `Domain/Iss/ItemAliquotaIss.cs:51` (doc diz "Mínimo 2% e máximo 5%") vs. `:82-85` (validação real aceita `[0, 100]`).
- **Caso-limite:** parametrizar item com alíquota 0,5% ou 8% — passa. A doc promete o intervalo constitucional (2%–5%, ADCT/LC 157/2016 fixou o piso de 2%), mas a invariante **não** o reforça. Há isenções/reduções legítimas (ex.: alíquota 0 para itens isentos), então travar em 2%–5% pode ser indesejado — **por isso é DESIGN, não BUG puro** — mas a divergência doc×código é uma armadilha de auditoria.
- **Teste a adicionar:** decidir a regra (provável: permitir 0 para isenção, alertar/avisar fora de 2%–5% para os demais) e testar os limites. No mínimo, **alinhar o XML doc** à validação real para não enganar o auditor.

---

## ACHADO 9 — `Escriturar` aceita notas de competência/tenant divergentes da apuração — **BUG latente (sem invariante de coerência)**

- **Arquivo:linha:** `Domain/Iss/ApuracaoIss.cs:88-115` (`Escriturar` adiciona a memória **sem** validar que a nota pertence à mesma competência/tenant/contribuinte da apuração).
- **Caso-limite:** o handler atual (`ApurarIssMensal.cs:90-94`) só passa notas já filtradas por prestador+competência, então **hoje está correto**. Mas o agregado **não protege seu próprio invariante**: nada impede escriturar no livro de maio uma memória de nota de abril, ou de outro contribuinte. Para um "domínio rico" (CLAUDE.md §7 "entidades protegem invariantes; proibido modelo anêmico"), `MemoriaIssNota` deveria carregar a competência e `Escriturar` recusar divergência.
- **Teste a adicionar:** `Escriturar_recusa_nota_de_competencia_diferente_da_apuracao`. Exige enriquecer `MemoriaIssNota` (CalculoIss.cs:32) com competência/tenant.

---

## ACHADO 10 — Re-importação de evento (cancelamento/substituição) não atualiza nota já persistida — **BUG de ingestão (impacta a base de apuração)**

- **Arquivo:linha:** `NfseSincronizador.cs:35-38` — se a nota já existe por chave, faz `continue` e **pula tudo**, inclusive **eventos de cancelamento/substituição** que cheguem depois para uma nota já ingerida.
- **Caso-limite (do enunciado):** NFS-e ingerida como Normal; dias depois o ADN distribui o evento de cancelamento/substituição (M6-DESIGN §2.1: "novo agregado/entidade `EventoNfse` ligado por chave; nota cancelada/substituída sai da base"). O sincronizador **descarta** o evento porque a chave já existe → a nota permanece `Normal` → **continua sendo apurada e cobrada uma nota cancelada**. Perda de correção fiscal direta.
- **Certo ou errado?** **ERRADO** para o caminho de eventos. O `continue` por dedup confunde "nota já vista" com "nada a fazer". O M6-DESIGN previu `EventoNfse`, mas o sincronizador atual não aplica `Cancelar()`/`Substituir()` em notas existentes.
- **Teste a adicionar:** `Reimportacao_de_evento_de_cancelamento_aplica_cancelamento_a_nota_existente` (e idem substituição). Atualmente **não existe** nenhum teste do `NfseSincronizador`.
- **Ação:** separar "documento novo" de "evento sobre documento existente"; aplicar transição de situação na nota persistida (idempotente).

---

## Resumo executivo

### Bugs reais (cálculo/classificação/ingestão erram ou podem errar)
- **ACHADO 1** — município de incidência (art. 3º) ignorado: pode lançar ISS próprio indevido / não capturar incidência devida. **[mais crítico]**
- **ACHADO 10** — eventos de cancelamento/substituição pós-ingestão são descartados: apura nota cancelada. **[mais crítico]**
- **ACHADO 2** — sentinela "00.00" pode ser cobrado se cadastrado na tabela (fail-open, não fail-closed).
- **ACHADO 3** — `ValorMonetario.Somar` não garante invariante de 2 casas (bomba-relógio de centavos).
- **ACHADO 6** — dedup/apuração sem `TenantId` explícito: risco de perda de nota / vazamento entre tenants se o Global Query Filter falhar no Worker. **[mais crítico]**
- **ACHADO 7** (parcial) — colisão de início de vigência → seleção de tabela não-determinística; sem filtro de tenant.
- **ACHADO 9** — `Escriturar` não valida coerência competência/contribuinte (invariante ausente).

### Casos só faltando teste (comportamento provavelmente correto, sem cobertura que o TCE exigiria)
- **ACHADO 4** — precedência substituição > retenção-da-lei sem teste do par `substituicao+retencaoObrigatoria`.
- **ACHADO 5** — `Substituir()` sem teste de exclusão da base (assimetria com cancelada); idempotência não testada.
- **ACHADO 7** (parcial) — seleção da tabela por faixa de vigência e competência anterior a qualquer tabela sem teste.
- **ACHADO 8** — faixa 2%–5% prometida na doc mas não validada/testada (alinhar doc×código).
- Geral: **zero testes** para `NfseSincronizador`, para isolamento de tenant no ISS, e para agregação monetária de muitas notas.

### Os 3 mais críticos (para o TCE)
1. **ACHADO 10 — evento de cancelamento/substituição descartado na re-sync** (`NfseSincronizador.cs:35-38`): apura e cobra nota cancelada. Perda de correção fiscal direta e silenciosa.
2. **ACHADO 1 — município de incidência (LC 116 art. 3º) nunca verificado** (`CalculoIss.cs:58-88` + `IssItbiRepositories.cs:62-76`): ISS próprio lançado sobre nota devida a outro município (ou incidência local não capturada). Vira Dívida Ativa errada — glosa certa do TCE.
3. **ACHADO 6 — dedup e apuração sem `TenantId` explícito** (`NotaFiscalServicoRepository.cs:11-12` + `IssItbiRepositories.cs:62-76`): se o Global Query Filter não cobrir o caminho do Worker, mesma chave em tenants distintos causa descarte de nota / vazamento. Isolamento multi-tenant é falha crítica por CLAUDE.md §3/§5 e não tem teste.

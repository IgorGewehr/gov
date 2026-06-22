# Regras — ITBI (Imposto sobre Transmissão de Bens Imóveis)

<!-- manifest
commands: ConfigurarAliquotaItbi, LancarItbi, InstaurarArbitramentoItbi, AbrirContraditorioArbitramentoItbi, RegistrarContraditorioArbitramentoItbi, ConcluirArbitramentoItbi, CancelarArbitramentoItbi
queries: CalcularItbi
domainEvents: AliquotaItbiCriada, AliquotaItbiPublicada, TransmissaoImobiliariaRegistrada, AlertaDivergenciaItbi, ProcessoArbitramentoItbiInstaurado, ContraditorioArbitramentoItbiAberto, ContraditorioArbitramentoItbiApresentado, ProcessoArbitramentoItbiConcluido, ProcessoArbitramentoItbiCancelado, ArbitramentoItbiAplicado
integrationEventsPublished: 
integrationEventsConsumed: 
-->

> Bounded Context: **Tributos** — cálculo do **ITBI** na transmissão imobiliária *inter vivos* onerosa
> (CTN art. 35). **Base = VALOR DECLARADO da transação** (Tema 1.113/STJ, REsp 1.937.821: presunção de
> veracidade do valor declarado). O **valor venal de referência** (motor do Imóvel/PGV) é apenas
> **parâmetro de triagem/alerta** — NUNCA base automática. A elevação da base só ocorre por **processo
> de arbitramento (CTN art. 148)**, separado, auditado e com contraditório. Gera guia avulsa (DAM).
> Ver M6-DESIGN §3.1 e `docs/architecture/itbi-tema1113`.

---

## 1. Linguagem ubíqua

- **Alíquota do ITBI (`AliquotaItbi`)** — alíquota geral e alíquota da parcela financiada pelo SFH, por
  exercício; **lei municipal** (não há teto federal).
- **Transmissão imobiliária (`TransmissaoImobiliaria`)** — fato gerador do ITBI; vincula `Imovel` ao
  transmitente e ao adquirente (`Contribuinte`); guarda valor declarado, base adotada e sua **origem**.
- **Base de cálculo** — **valor declarado** (origem `Declarada`, padrão) ou **valor arbitrado** (origem
  `ArbitradaArt148`, só após processo de arbitramento concluído).
- **Triagem/alerta** — comparação `declarado × referência` por **margem parametrizável por tenant**
  (`MargemDivergenciaPercentual`); só *deflagra* revisão fiscal, não altera o tributo.
- **Processo de arbitramento (`ProcessoArbitramentoItbi`)** — agregado com máquina de estados
  (`Instaurado → AguardandoContraditorio → EmAnalise → Concluido | Cancelado`) que afasta a presunção do
  valor declarado (CTN art. 148) com contraditório. Único caminho para elevar a base.

---

## 2. Comandos (escrita)

- **`ConfigurarAliquotaItbiCommand`** → cria a alíquota do ITBI de um exercício (geral + SFH), publica.
  Retorna o `Guid`.
- **`LancarItbiCommand`** → registra a transmissão, apura o ITBI (**base = valor declarado**), constitui o
  `Lancamento` `TipoTributo.Itbi` contra o adquirente e gera a guia avulsa (DAM, cota única). Se a triagem
  acusar divergência, sinaliza `AlertaDivergenciaItbi` (não altera a base). Retorna o resumo do lançamento.
- **`InstaurarArbitramentoItbiCommand`** → instaura o processo de arbitramento (CTN art. 148) sobre uma
  transmissão; exige motivo individualizado e fundamentação do fisco. Não altera a guia. Retorna o `Guid`.
- **`AbrirContraditorioArbitramentoItbiCommand`** → notifica o contribuinte e abre o contraditório
  (obrigatório).
- **`RegistrarContraditorioArbitramentoItbiCommand`** → registra a defesa/avaliação contraditória e
  encaminha à análise do fisco.
- **`ConcluirArbitramentoItbiCommand`** → decisão final com valor arbitrado; aplica a base arbitrada à
  transmissão e lança de ofício a diferença (complementar). Só após o contraditório.
- **`CancelarArbitramentoItbiCommand`** → encerra o processo sem arbitrar (a declaração prevaleceu).

## 3. Consultas (leitura)

- **`CalcularItbiQuery`** → apura (preview) o ITBI de uma transmissão sem lançar (**base = valor
  declarado**); retorna a memória de cálculo achatada (valor venal de referência, declarado, base, origem,
  divergência de triagem, alíquota, isenção, imposto devido).

---

## 4. Eventos

- **Domínio (in-process):**
  - `AliquotaItbiCriada(AliquotaItbiId, TenantId, Exercicio)`
  - `AliquotaItbiPublicada(AliquotaItbiId, TenantId, Exercicio)`
  - `TransmissaoImobiliariaRegistrada(TransmissaoImobiliariaId, TenantId, ImovelId, ImpostoDevido)`
  - `AlertaDivergenciaItbi(TransmissaoImobiliariaId, TenantId, ValorDeclarado, ValorVenalReferencia)`
  - `ProcessoArbitramentoItbiInstaurado(ProcessoArbitramentoItbiId, TenantId, TransmissaoImobiliariaId)`
  - `ContraditorioArbitramentoItbiAberto(ProcessoArbitramentoItbiId, TenantId)`
  - `ContraditorioArbitramentoItbiApresentado(ProcessoArbitramentoItbiId, TenantId)`
  - `ProcessoArbitramentoItbiConcluido(ProcessoArbitramentoItbiId, TenantId, TransmissaoImobiliariaId, ValorArbitrado)`
  - `ProcessoArbitramentoItbiCancelado(ProcessoArbitramentoItbiId, TenantId)`
  - `ArbitramentoItbiAplicado(TransmissaoImobiliariaId, TenantId, ProcessoArbitramentoItbiId, ImpostoDevido)`
- **Integração:** nenhum publicado/consumido nesta versão.

---

## 5. RBAC

- `tributos.gerenciar` — lançar ITBI / configurar alíquota.
- `tributos.ver` — preview de apuração.
- `tributos.itbi.arbitrar` — verbo fino de Segregação de Funções: instaurar/conduzir/concluir/cancelar o
  arbitramento (CTN art. 148). Separado da gestão geral do módulo.

---

## 6. Pendências `// TODO(validar-oficial)`

- Alíquota (geral e SFH), contribuinte e isenções (1ª aquisição SFH, imunidades CF art. 156 §2º I)
  conforme o **CTM de Maximiliano de Almeida/RS**.
- **Tema 1.113/STJ** aplicado: base = valor declarado; arbitramento só por processo administrativo
  (CTN art. 148) com contraditório. **Exige ADR + parecer da procuradoria** antes de produção
  (contraditório diferido, regra de revelia e rito de restituição/lançamento complementar — `[a confirmar]`).

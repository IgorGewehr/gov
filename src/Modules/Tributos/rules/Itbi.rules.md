# Regras — ITBI (Imposto sobre Transmissão de Bens Imóveis)

<!-- manifest
commands: ConfigurarAliquotaItbi, LancarItbi
queries: CalcularItbi
domainEvents: AliquotaItbiCriada, AliquotaItbiPublicada, TransmissaoImobiliariaRegistrada
integrationEventsPublished: 
integrationEventsConsumed: 
-->

> Bounded Context: **Tributos** — cálculo do **ITBI** na transmissão imobiliária *inter vivos* onerosa
> (CTN art. 35). **Base = MAIOR entre o valor venal de referência (motor do Imóvel/PGV) e o valor
> declarado da transação**; alíquota por lei municipal; isenções parametrizáveis; gera guia avulsa
> (DAM). Ver M6-DESIGN §3.1.

---

## 1. Linguagem ubíqua

- **Alíquota do ITBI (`AliquotaItbi`)** — alíquota geral e alíquota da parcela financiada pelo SFH, por
  exercício; **lei municipal** (não há teto federal).
- **Transmissão imobiliária (`TransmissaoImobiliaria`)** — fato gerador do ITBI; vincula `Imovel` ao
  transmitente e ao adquirente (`Contribuinte`); guarda valor declarado, base adotada e memória de cálculo.
- **Base de cálculo** — maior entre o valor venal de referência e o valor declarado.

---

## 2. Comandos (escrita)

- **`ConfigurarAliquotaItbiCommand`** → cria a alíquota do ITBI de um exercício (geral + SFH), publica.
  Retorna o `Guid`.
- **`LancarItbiCommand`** → registra a transmissão, apura o ITBI (base = maior valor), constitui o
  `Lancamento` `TipoTributo.Itbi` contra o adquirente e gera a guia avulsa (DAM, cota única). Retorna o
  resumo do lançamento.

## 3. Consultas (leitura)

- **`CalcularItbiQuery`** → apura (preview) o ITBI de uma transmissão sem lançar; retorna a memória de
  cálculo achatada (valor venal de referência, declarado, base, alíquota, isenção, imposto devido).

---

## 4. Eventos

- **Domínio (in-process):**
  - `AliquotaItbiCriada(AliquotaItbiId, TenantId, Exercicio)`
  - `AliquotaItbiPublicada(AliquotaItbiId, TenantId, Exercicio)`
  - `TransmissaoImobiliariaRegistrada(TransmissaoImobiliariaId, TenantId, ImovelId, ImpostoDevido)`
- **Integração:** nenhum publicado/consumido nesta versão.

---

## 5. Pendências `// TODO(validar-oficial)`

- Alíquota (geral e SFH), contribuinte e isenções (1ª aquisição SFH, imunidades CF art. 156 §2º I)
  conforme o **CTM de Maximiliano de Almeida/RS**.
- Conciliação da regra "base = MAIOR valor" (M6-DESIGN/spec) com o **Tema 1.113/STJ** (presunção a favor
  do valor declarado; arbitramento só via processo administrativo CTN art. 148) — validar com a
  procuradoria antes de produção.

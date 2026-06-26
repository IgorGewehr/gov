# Regras — DES-IF (Declaração Eletrônica de Serviços de Instituições Financeiras)

<!-- manifest
commands: EntregarDesif
queries: 
domainEvents: DeclaracaoDesifEntregue
integrationEventsPublished: 
integrationEventsConsumed: 
-->

> Bounded Context: **Tributos** — apuração do **ISSQN das instituições financeiras** pela **DES-IF**
> (modelo conceitual **ABRASF**). Paridade com o incumbente **SAPI**. Base legal: **LC 116/2003** (itens
> da lista do setor financeiro) + **Plano Contábil COSIF** (BACEN) + Código Tributário Municipal. NÃO
> emitimos a declaração — a instituição **DECLARA**. Ver PARIDADE-POC.

---

## 1. Linguagem ubíqua

- **DES-IF (`DeclaracaoDesif`)** — declaração das instituições financeiras/equiparadas autorizadas pelo
  BACEN. Composta de 4 módulos (modelo ABRASF): Mód.1 Demonstrativo Contábil (BAM, anual), Mód.2
  Apuração Mensal do ISSQN (mensal), Mód.3 Informações Comuns aos Municípios (anual), Mód.4 Partidas dos
  Lançamentos Contábeis (sob demanda). Implementamos a entrega do **Módulo 2** (constitui o crédito).
- **Subtítulo (`SubtituloDesif`)** — Registro 0430: conta/subtítulo **COSIF**, código de tributação
  DES-IF (Anexo 6), item LC 116 correlato, receita tributável (base), alíquota e ISSQN devido do subtítulo.
- **ISSQN a recolher** — Registro 0440: devido bruto MENOS deduções da receita, incentivos autorizados em
  lei e depósitos judiciais (CTN art. 151, II — exigibilidade suspensa da parcela depositada).

---

## 2. Comandos (escrita)

- **`EntregarDesifCommand`** → abre a DES-IF (Módulo 2), escritura os subtítulos COSIF tributáveis,
  aplica as deduções/incentivos/depósitos (Registro 0440), apura o **ISSQN a recolher** e CONSTITUI o
  crédito tributário do ISS (`Lancamento` `TipoTributo.Iss` — CTN art. 150, lançamento por homologação).
  Recusa retificação implícita (já há DES-IF vigente na competência → retificar a existente). Retorna o
  resumo (receita tributável, devido bruto, a recolher) + o `LancamentoId`.

> Invariantes: alíquota ∈ [0,100]; deduções+incentivos+depósitos **não excedem** o devido bruto
> (fail-closed contra base negativa); nenhuma alíquota hardcoded (lei municipal).

---

## 3. Eventos

- **Domínio (in-process):** `DeclaracaoDesifEntregue(DeclaracaoDesifId, TenantId, ContribuinteId, IssqnARecolher)`
- **Integração:** nenhum publicado/consumido nesta versão.

---

## 4. Pendências `// TODO(validar-oficial)`

- Tabela de **Códigos de Tributação DES-IF** (Anexo 6) e de-para COSIF→item LC 116 conforme o CTM de
  Maximiliano de Almeida/RS.
- Geração/validação do **arquivo de leiaute** (CSV posicional ABRASF) e os **Módulos 1/3/4** (BAM anual,
  Informações Comuns, Partidas) — modelados na entrega real (defere conforme periodicidade/legislação).

# Hub de Revisão Humana — Tensorroot.Gov

> **Para quem não é programador.** Este é o mapa para o especialista de cada área (contador,
> especialista em folha/eSocial, tributarista, especialista em licitações, ligação com o TCE-RS)
> encontrar **em minutos** tudo o que precisa conferir e ajustar no sistema — **sem ler código**.
>
> Sistema: ERP público (prefeituras e câmaras), em produção, sob fiscalização do **TCE-RS**.
> Município-piloto: **Maximiliano de Almeida/RS**.

---

## 1. Como o sistema separa "regra" de "valor" (leia isto primeiro)

O sistema foi construído de um jeito que torna a revisão por especialista **possível e segura**.
Cada regra de negócio (como o IRRF é calculado, o que entra no mínimo da saúde, quando a dívida
prescreve) está descrita em **português de negócio** num arquivo `*.rules.md` — e existe uma trava
automática (a *fitness* `SpecCodeConsistency`) que **impede o código de divergir** desse arquivo.
Ou seja: o que está escrito no `.rules.md` é o que o sistema realmente faz.

Há **duas coisas diferentes** que você pode encontrar, e o hub sempre deixa claro qual é qual:

### 🟢 AJUSTÁVEL DIRETO (parâmetro) — muda o valor, muda o sistema
São os **valores** que mudam por lei, portaria ou exercício: alíquotas, faixas de tabela, prazos,
limites, índices, percentuais. Ficam em arquivos de **parâmetros** (classes `Parametros*`) ou em
**seeds de tabela legal** (ex.: tabela do IRRF, tabela do INSS, tabela de retenção de PJ). Mudar o
valor ali **muda o cálculo em produção**, e não exige reprogramar nada. O hub sempre indica:
**o arquivo + o campo/constante + o valor atual + a norma que o define**.

> Exemplo: a alíquota do PASEP está em `ParametrosFolha.AliquotaPasep = 1.00` (1%). Se a lei mudar
> para outro percentual, troca-se esse valor e o cálculo segue a nova regra. Você **sinaliza**, o
> dev troca o número (ou, no futuro, a tela de parâmetros faz isso).

### 🟡 REVISAR e SINALIZAR (lógica) — a regra descrita no `.rules.md`
É a **lógica de negócio** em si (a "receita" do cálculo, a máquina de estados, as travas). Está
descrita no `*.rules.md`. Se você, especialista, perceber que a **regra está errada frente à norma**
(ex.: a ordem de aplicação do redutor do IRRF, ou o que pode entrar no mínimo da saúde), isso **não
é um valor a trocar** — é um ajuste de lógica que o **desenvolvedor** faz no código (que está
isolado e descrito). Seu papel aqui é **conferir contra a norma e sinalizar** a divergência.

**Resumindo:** valor errado → você aponta o parâmetro e o número correto. Regra errada → você aponta
o `.rules.md` e a norma que ela viola. Em ambos os casos, este hub te leva direto ao ponto.

---

## 2. Qual arquivo é o seu (papel → página do hub)

| Seu papel | Abra este arquivo | O que vai encontrar |
|---|---|---|
| **Contador municipal** (contabilidade pública, PCASP, encerramento, MSC, RREO/RGF, retenções) | [`contabil.md`](contabil.md) | Regras de Finanças + Transparência: plano de contas, empenho/liquidação/pagamento, encerramento de exercício, MSC/SICONFI, RREO/RGF, retenções na fonte, mínimos de saúde/educação |
| **Especialista em FOLHA / eSocial** | [`rh-folha.md`](rh-folha.md) | Tabelas IRRF/INSS (com os valores), parâmetros de folha/previdência/eSocial, eventos S-1200/S-1202/S-2200/SST, abate-teto, redutor IRRF 2026, margem consignável, 13º/férias/rescisão |
| **Tributarista** (tributos municipais, dívida ativa) | [`tributos.md`](tributos.md) | ITBI (Tema 1.113/STJ), ISS/DES-IF, IPTU/PGV, CND/CPEN, decadência/prescrição, dívida ativa/CDA, taxas/COSIP/contribuição de melhoria |
| **Especialista em LICITAÇÕES** | [`licitacoes.md`](licitacoes.md) | Limites de dispensa (valores atuais), prazo de publicação no PNCP (art. 94), credenciamento (art. 79), registro de preços, contratos/aditivos, sanções |
| **Ligação TCE-RS / transmissão** | [`tce-rs-integracao.md`](tce-rs-integracao.md) | Cada gerador de leiaute (SIAPC, MSC, folha-TCE, SICAP/SIAPES, LicitaCon, eSocial), a versão/norma do leiaute, e onde fica cada ponto de transmissão que ainda espera credencial |
| **Visão geral dos demais módulos** | [`outros-modulos.md`](outros-modulos.md) | Lista enxuta das regras de Saúde, Educação, Assistência Social, Legislativo, Patrimônio, Protocolo e Identidade, por área |

---

## 3. Como usar cada página

Cada página lista os itens de revisão em formato de tabela. Para cada item você terá:

- **O arquivo** (`caminho/do/arquivo` — clicável; com `:linha` quando ajuda a achar o ponto exato).
- A **NORMA** que ancora a regra (lei, portaria, súmula, tema do STJ/STF, versão de leiaute).
- Se é 🟢 **parâmetro ajustável** (com o campo e o valor atual) ou 🟡 **lógica a revisar**.
- **O QUE CONFERIR** — uma frase direta do que você precisa validar.

> **Onde estão os arquivos:**
> - Regras de negócio: `src/Modules/<Módulo>/rules/*.rules.md`
> - Parâmetros ajustáveis: `src/Modules/<Módulo>/.../Configuracao/Parametros*.cs` (e equivalentes)
> - Tabelas legais (seeds): arquivos `SemearTabelas*` / `Tabela*Catalogo` / `Tabela*`
> - Pontos de transmissão pendentes: marcados no código com `// TODO(M10)`
> - Fonte normativa consolidada (todas as leis/portarias/URLs): [`docs/normas/FONTES-NORMATIVAS.md`](../normas/FONTES-NORMATIVAS.md)

---

## 4. Quanto há para revisar (resumo)

| Página | Regras de negócio (`.rules.md`) | Parâmetros ajustáveis | Tabelas legais / leiautes |
|---|---|---|---|
| Contábil (Finanças + Transparência) | 13 | 1 (tabela IRRF/PJ) | MSC, SIAPC, folha-TCE, CNAB240 |
| RH / Folha / eSocial | 19 | 6 classes `Parametros*` + redutor | Tabela IRRF, Tabela INSS, eventos S-1.x |
| Tributos | 14 | parâmetros municipais (PGV/alíquotas/prazos por lei) | — |
| Licitações | 9 | limite de dispensa (Decreto federal) | LicitaCon 1.4 (14 CSV) |
| TCE-RS / transmissão | — | — | 9 geradores de leiaute + 64 pontos `TODO(M10)` |
| Outros módulos | 53 | Art. 29-A, Obra (prazos PNCP) | SIAPES, SICOE |

**Totais no sistema:** 107 arquivos `*.rules.md` · ~9 famílias de parâmetros ajustáveis ·
9 geradores de leiaute oficiais · **64 pontos `TODO(M10)`** (em 47 arquivos) aguardando credencial
de transmissão.

> **Importante:** este hub é **somente leitura sobre o código** — ele aponta para os arquivos, mas
> nada aqui altera o sistema. As alterações de parâmetro/lógica são feitas pelo time de
> desenvolvimento a partir das suas sinalizações.

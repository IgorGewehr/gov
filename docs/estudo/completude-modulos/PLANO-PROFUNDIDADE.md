# PLANO DE APROFUNDAMENTO — Completude dos Módulos (Tensorroot.Gov)

> Consolidação das 5 auditorias independentes de completude (Saúde, Educação, Transparência, Patrimônio, Recursos Humanos). Engenharia-chefe de produto.
>
> **Tese central, comum aos 5 módulos:** todos têm uma **espinha fiscal/compliance madura e profunda** (apurações constitucionais, prestação de contas, eSocial, TCE/SICONFI). O que falta — e o que faz o dono perceber os módulos como "rasos" no primeiro clique — é a **largura operacional do dia a dia**: as telas de alto volume (busca/lista navegável, cadastros que hoje são GUID digitado, agenda, dispensação, inventário, consignação). O padrão é sempre o mesmo: **vertical compliance profunda, largura operacional vazia.**

---

## 1. Índice de completude honesto — por módulo

Legenda de esforço: **P** = pequeno (≤2 dias) · **M** = médio (~1 semana) · **G** = grande (>1 semana, novo agregado + persistência + UI).

### 1.1 Saúde

- **Fiscal-only?** Não. Tem espinha operacional clínica real (PEP/Atendimento SOAP, Regulação SISREG, prontuário longitudinal) + compliance pesado (RNDS, SISAB, ICP-Brasil, ASPS/FMS).
- **% operacional honesto:** ~45%. Profundo na vertical "registro clínico + compliance"; estreito e "lookup-only" na largura (só chega no paciente digitando CNS de 15 dígitos).
- **Por que parece raso:** faltam exatamente as telas de alto volume municipal — busca de pacientes, agenda, farmácia, vacina. UBS e profissional são só GUIDs de gateway simulado.

| # | Lacuna | Prio | Esforço | Dep. cred oficial? |
|---|---|---|---|---|
| 1 | Lista/busca de pacientes navegável (nome/CPF/CNS/nasc., paginada) | P1 | P | Não |
| 2 | Cadastro de UBS/Estabelecimento + Profissional (CNES/CBO, vínculos) | P1 | M | Parcial (CNES oficial; local roda sem cred) |
| 3 | Agendamento de consultas/exames (agenda, marcação, falta) | P1 | G | Não (agenda local) |
| 4 | Farmácia/dispensação (estoque, lote/validade, entrada/saída) | P2 | G | Operação local não; integração HÓRUS sim |
| 5 | Imunização/carteira de vacinas (dose, lote, aprazamento) | P2 | M/G | Registro local não; SI-PNI/RNDS sim |

P3: vigilância sanitária; catálogos estruturados (SIGTAP/CATMAT/medicamentos) no lugar de strings; sinais vitais/triagem; gestão de cotas com tela própria.

### 1.2 Educação

- **Fiscal-only?** Fiscal-first com esqueleto operacional — **não operacional de verdade.** Espinha fiscal robusta (MDE, FUNDEB, repasses). Tem 3 agregados de operação (Escola, Matrícula, DiárioClasse).
- **% operacional honesto:** ~25%. Os dois cadastros que sustentam toda a operação — **Aluno e Turma — não existem como entidades.**
- **Smoking gun:** o formulário de matrícula pede ao usuário **digitar um GUID cru** para aluno e turma. `TurmaRepository.PossuiVagaAsync` só checa `id != Guid.Empty` (stub explícito).

| # | Lacuna | Prio | Esforço | Dep. cred oficial? |
|---|---|---|---|---|
| 1 | Cadastro de Aluno (agregado + CRUD + busca; LGPD-menor) | P1 | G | Não |
| 2 | Agregado Turma + enturmação (CRUD + picker na matrícula) | P1 | M/G | Não |
| 3 | Diário por turma + Boletim/Histórico escolar (hoje diário é 1-1 por matrícula) | P2 | M | Não |
| 4 | Merenda (PNAE): cardápio + distribuição | P2 | M | Operação não; prestação FNDE sim |
| 5 | Transporte (PNATE) + Calendário escolar formal | P3 | M (cada) | Operação não; prestação FNDE sim |

Dep. cred: exportação EducaCenso/INEP e prestações PNAE/PNATE ao FNDE (todas P3, stub na PoC).

### 1.3 Transparência

- **Fiscal-only?** **Sim — totalmente.** Apesar do nome, é na prática um módulo de **Prestação de Contas ao TCE-RS + SICONFI** (profundo no que faz). LAI, e-SIC, dados abertos e anonimização LGPD estão **só no README** — nunca construídos.
- **% operacional honesto (função "transparência" real):** ~0% da superfície pública/cidadã; ~100% da prestação de contas back-office. **Não há uma única rota pública** em todo o SPA.
- **Maior risco de PoC:** "Transparência" para um avaliador municipal = portal público de consulta de gastos. O módulo entrega zero disso. É a maior distância entre o nome do módulo e o que ele faz.

| # | Lacuna | Prio | Esforço | Dep. cred oficial? |
|---|---|---|---|---|
| 1 | Portal de Transparência Ativa público (sem login) — materializar Integration Events já publicados (despesa, receita, contrato, folha) + infra de rota pública no SPA | P1 | G | Não (dados já fluem internamente) |
| 2 | e-SIC / transparência passiva (PedidoLAI, prazo 20+10, status público) | P1 | M/G | Não |
| 3 | Dados Abertos (datasets CSV/JSON + dicionário, download público) | P2 | M | Não |
| 4 | Tela do Núcleo Fiscal / semáforo de mínimos (backend já existe, falta UI) | P2 | P/M | Não |
| 5 | Anonimização LGPD (mascarar CPF) + painel de obrigações/prazos LRF | P3 | M | Não |

**Nota crítica:** nenhuma lacuna P1-P3 depende de cred externa — a matéria-prima já trafega pelos Integration Events internos. O trabalho é **materializar read models + construir a superfície pública.**

### 1.4 Patrimônio

- **Fiscal-only?** No domínio, não — profundidade real em Bens, Frota e Almoxarifado (entidades ricas, VOs, eventos). Na **camada de consulta/operação, sim**: todas as telas são fatias de compliance ou consulta por ID digitado.
- **% operacional honesto:** ~40%. Domínio profundo e correto; operação fiscal-only. **Não há como LISTAR/BUSCAR bens, veículos ou itens**, e o **INVENTÁRIO — citado no próprio README como regra crítica da Lei 4.320 — não existe** (0 ocorrências no código).
- **Por que parece raso:** a espinha está rica, a largura operacional ("chão de fábrica") está vazia.

| # | Lacuna | Prio | Esforço | Dep. cred oficial? |
|---|---|---|---|---|
| 1 | Listagem/busca geral nos 3 submódulos (Bens, Veículos, Itens), paginada+filtros | P1 | M | Não |
| 2 | Módulo de Inventário (anual/por setor; comissão, conciliação físico×contábil) | P1 | G | Não |
| 3 | Requisição de material self-service (solicitar→aprovar→atender) | P2 | M | Não |
| 4 | Painel de Frota (custo/consumo por veículo + cadastro/lista motoristas + CNH vencendo) | P2 | M | Não (multas DETRAN/RENAINF = P3, depende de convênio) |
| 5 | Termo de responsabilidade + etiqueta/QR de tombamento + categorias c/ vida útil | P3 | P/M | Não |

Dep. cred (futuro, não bloqueia PoC): multas RENAINF/DETRAN, licenciamento/IPVA DETRAN-RS, gestor de combustível, georreferenciamento de imóveis.

### 1.5 Recursos Humanos

- **Fiscal-only?** Não — tem ciclo de vida do vínculo, ponto (Portaria 671) e ciclo anual operacionais. Forte na espinha fiscal/compliance (folha, eSocial, ponto, tabelas legais).
- **% operacional honesto:** ~50%. **Raso na largura operacional de RH do dia a dia.** Ficha do servidor mínima (só nome+nascimento), afastamento é evento genérico sem tipagem nem efeito na folha; **não existe** consignação/margem, progressão funcional, concurso/posse-em-lote, nem relatórios gerenciais. Lista de servidores sem filtros/busca/paginação.
- **Por que parece raso:** "metade RH (a que o TCE/eSocial exige) sem a outra metade (o dia a dia)".

| # | Lacuna | Prio | Esforço | Dep. cred oficial? |
|---|---|---|---|---|
| 1 | Afastamentos/licenças tipados com efeito na folha (maternidade, doença/INSS, prêmio, etc.) + retorno + tela | P1 | M | Não (habilita eSocial S-2230) |
| 2 | Consignações + margem consignável (Lei 14.131; consignatárias, contratos, desconto) | P1 | G | Não |
| 3 | Ficha/histórico funcional completo + timeline do vínculo + edição + busca/filtros na lista | P1 | M | Não |
| 4 | Relatórios gerenciais (folha por secretaria, evolução despesa pessoal, mapa de cargos) | P2 | M | Não |
| 5 | Progressão/promoção funcional (avanço por tempo, enquadramento PCCS) | P2 | G | Não (habilita eSocial S-2206) |

P3: concurso/nomeação em lote (M), programação de férias em calendário (P), banco de horas/abono (M), autosserviço com aprovação (M), eventos eSocial S-2206/S-2230/S-2300 (dependem de #1 e #5).

---

## 2. Padrão transversal (vale para todos)

1. **A porta de entrada é uma caixa de busca quebrada ou inexistente.** Saúde só acha paciente por CNS exato; Patrimônio/Educação/RH têm GUIDs digitados ou listas sem filtro. **A lacuna de maior ROI imediato em quase todo módulo é a lista/busca navegável** — esforço P/M, reaproveita o DataTable já usado (Regulação/Tributos).
2. **Cadastros mestres faltando viram GUID digitado.** Aluno, Turma, UBS, Profissional, Motorista — todos são IDs sem entidade. Destravar esses cadastros destrava as telas downstream (matrícula, agenda, diário).
3. **Integrações externas estão atrás de gateways simulados** (`SimuladoGateways.cs`, `SimuladoEstabelecimentoRepository`, `SimuladoPublicacaoTransparenciaRepository`). **A operação local pode ser construída agora**; só a troca pelos gateways reais (RNDS/SISAB/SISREG/CADSUS/CNES/HÓRUS/SI-PNI/INEP/FNDE/eSocial/TCE) exige creds/certificados — item separado, alinhado ao M10.
4. **Quase nada do trabalho de completude P1-P2 depende de credencial oficial.** São cadastros, fluxos internos e read models. Isso é o que torna o aprofundamento viável **antes** do M10.

---

## 3. ORDEM DE APROFUNDAMENTO recomendada

Critério: **valor para a PoC × esforço × independência de creds.** Construir primeiro o que dá maior salto de percepção, com menor esforço, sem depender de integração oficial.

### ONDA 0 — "Navegabilidade" (quick wins, esforço P/M, zero cred) — *fazer já*
O maior ROI de percepção do projeto inteiro. Resolve o "parece raso no primeiro clique" em vários módulos de uma vez, reaproveitando o padrão DataTable existente.

1. **Saúde** — Lista/busca de pacientes (P) — *maior ROI isolado do projeto*
2. **Patrimônio** — Listagem/busca geral de Bens, Veículos, Itens (M)
3. **RH** — Busca/filtros/paginação na lista de servidores + ficha funcional completa + timeline (M)
4. **Transparência** — Tela do semáforo de mínimos constitucionais (backend já existe, só UI) (P/M)

> Resultado: 4 módulos deixam de "parecer CRUD raso" com esforço majoritariamente Pequeno/Médio.

### ONDA 1 — "Cadastros mestres que destravam o resto" (esforço M/G, zero cred)
Sem isto, as telas operacionais downstream não são usáveis.

5. **Educação** — Cadastro de Aluno (G) + Agregado Turma + picker de matrícula (M/G) — *bloqueador de tudo no módulo*
6. **Saúde** — Cadastro de UBS/Estabelecimento + Profissional (M) — destrava agenda
7. **RH** — Afastamentos tipados com efeito na folha (M) — *risco de folha errada hoje; corrige compliance + habilita S-2230*

### ONDA 2 — "Superfície de alto valor PoC" (esforço M/G, zero cred)
O que o avaliador municipal espera ver e hoje não existe.

8. **Transparência** — Portal de Transparência Ativa público + e-SIC (G + M/G) — *fecha a maior distância nome×função; dados já fluem por eventos*
9. **Patrimônio** — Módulo de Inventário anual/por setor (G) — *diferencial de "patrimônio de verdade"; cobrança recorrente do TCE*
10. **RH** — Consignações + margem consignável (G) — *uso diário de todo RH municipal*
11. **Saúde** — Agendamento de consultas/exames (G, depende de #6)

### ONDA 3 — "Largura operacional restante" (esforço M/G, operação local sem cred)
12. **Educação** — Diário por turma + boletim/histórico (M); Merenda PNAE (M)
13. **Patrimônio** — Requisição self-service (M); Painel de Frota (M)
14. **RH** — Relatórios gerenciais (M); Progressão/promoção (G)
15. **Transparência** — Dados Abertos (M)
16. **Saúde** — Farmácia/dispensação (G); Imunização (M/G) — *operação local primeiro*

### ONDA 4 — "Integrações oficiais + acabamento" (depende de creds — alinhar ao M10)
Trocar gateways simulados por reais (RNDS/SISAB/SISREG/CADSUS/CNES/HÓRUS/SI-PNI, EducaCenso/INEP, PNAE/PNATE FNDE, multas RENAINF/DETRAN, anonimização LGPD em dados abertos). + acabamentos P3 de cada módulo (catálogos estruturados, termo/QR de tombamento, calendário escolar, vigilância sanitária, concurso em lote, banco de horas).

---

## 4. Dimensionamento total — leitura honesta para o dono

**Isto é grande. Cada um dos 5 setores é, na prática, quase um mini-ERP setorial.** A espinha fiscal/compliance — que é a parte mais difícil tecnicamente e a mais cara de errar — **já está construída e madura nos 5 módulos.** O que resta é largura operacional, que é mais previsível mas volumosa.

### Estimativa de esforço por onda (somando os 5 módulos)

| Onda | Conteúdo | Itens | Esforço agregado aprox. |
|---|---|---|---|
| 0 | Navegabilidade (listas/busca/ficha/semáforo) | 4 | ~2–3 semanas |
| 1 | Cadastros mestres (Aluno, Turma, UBS/Prof, afastamentos) | 3 frentes | ~5–7 semanas |
| 2 | Superfície alto valor PoC (portal público, inventário, consignação, agenda) | 4 | ~7–10 semanas |
| 3 | Largura operacional restante | ~7 frentes | ~8–12 semanas |
| 4 | Integrações oficiais + acabamento (M10) | muitos | depende de creds/convênios — escopo próprio |

**Total para "PoC que parece completa em todos os módulos" (Ondas 0–2):** da ordem de **~3,5 a 5 meses de engenharia** focada (1 dev sênior full-time; paralelizável por módulo se houver mais gente). **Para "largura operacional cheia" (até Onda 3):** some mais **~2–3 meses.** A Onda 4 (integrações reais) é trabalho de M10 e não se dimensiona em dias de código — é creds, certificados A1, convênios e homologação em portais oficiais.

### Recomendação de sequenciamento estratégico
- **Faça a Onda 0 inteira primeiro, custe o que custar.** É barata e muda a primeira impressão de 4 dos 5 módulos. É o melhor dinheiro gasto do plano.
- **Onda 1 é o gargalo real de usabilidade** (Educação é o módulo mais bloqueado — Aluno+Turma são pré-requisito de tudo). Priorizar Educação aqui.
- **Onda 2 é o que ganha a PoC com avaliador** — Transparência (portal público) é o item de maior risco reputacional se ficar de fora, porque o nome promete o que o módulo não entrega.
- **Tudo das Ondas 0–3 é construível sem nenhuma credencial oficial** — a operação local roda sobre dados próprios e eventos internos. Não há motivo para esperar o M10 para começar.

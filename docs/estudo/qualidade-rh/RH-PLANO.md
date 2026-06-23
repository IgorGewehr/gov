# RH — Plano de Ação Consolidado

> Consolidação adversarial de 4 auditorias READ-ONLY do módulo `RecursosHumanos` (Folha, Ponto+Integração, UI/UX, Gaps funcionais).
> Lente: setor mais usado e mais problemático da prefeitura. Tudo ancorado em `arquivo:linha`.
> Módulo: `src/Modules/RecursosHumanos` + `src/Web/src/modules/recursoshumanos`.

## Veredito de uma linha

O módulo tem um **motor de cálculo legal-tributário sólido** (INSS/RPPS/IRRF progressivo, 13º/férias/rescisão, ponto Port. 671 estrutural, eSocial). Mas em volta dele faltam: (a) **consolidação fiscal entre folhas do mesmo mês**, (b) **pensão alimentícia ligada ao motor**, (c) **ponte ponto→folha** (inexistente), (d) **operação de massa na UI** (lote, conferência, impressão, paginação) e (e) **quase toda a gestão de pessoas** (afastamentos com efeito, salário-família, consignação, progressão, relatórios TCE). Hoje a folha é **100% lançamento manual** — exatamente a dor relatada do sistema anterior. É "código-pronto", não "sistema-pronto".

## Sub-workflows de destino

- **WF-FOLHA** — motor de cálculo, tabelas legais, agregados de folha, ciclo anual.
- **WF-PONTO** — apuração de jornada, AFD/AEJ, integração ponto→folha.
- **WF-UX** — front RecursosHumanos (operação mensal, conferência, impressão).
- **WF-GESTAO** — domínio de pessoas (afastamentos, consignação, progressão, ficha funcional, relatórios).

Esforço: **P** (pequeno, ~dias), **M** (médio, ~1-2 semanas), **G** (grande, ~3+ semanas / requer modelagem de agregado ou endpoints novos).

---

## (1) P0 — Bugs de cálculo / integridade / integração (corrigir já)

Ferem o servidor (paga a mais/menos, holerite errado) ou o fechamento (irreversível, eSocial, TCE).

| # | Item | O que é | Por que dói na prefeitura | Esforço | WF |
|---|------|---------|---------------------------|---------|----|
| **P0-1** | **Pensão alimentícia hardcoded em `0m`** (`ApurarDescontosLegais.cs:96`; `IServidorRegimeConsulta.cs:11` sem campo) | O motor aceita `PensaoAlimenticia` mas o caso de uso mensal sempre passa zero; o DTO `DadosCalculoServidor` nem tem o campo. Pensão lançada como rubrica cai em `outrosDescontos` e **não reduz base IRRF**. | Servidor com pensão judicial: IRRF calculado sobre base maior (paga IR a mais) **ou** pensão não descontada/repassada — **descumprimento de ordem judicial**, responsabilidade da prefeitura. | M | WF-FOLHA |
| **P0-2** | **INSS/IRRF não consolidam múltiplas folhas no mês** (`ApurarDescontosLegais.cs:68-101`; `GerarFerias.cs:79`) | Cada `FolhaDePagamento` é agregado isolado; férias (Tipo=Ferias) e mensal (Tipo=Mensal) apuram separado. INSS tem **teto mensal único** e IRRF é **progressivo sobre a soma do mês**. | Servidor sai de férias no meio do mês → cada parcela fica abaixo do teto INSS e em faixa IRRF menor → **sub-recolhimento** → autuação da prefeitura (multa+juros) e ajuste/multa no IRPF do servidor. Férias são rotina mensal. | G | WF-FOLHA |
| **P0-3** | **Ponte ponto→folha INEXISTENTE** (`ApuracaoPonto.cs:141`; `PontoDomainEvents.cs:20`; `AdicionarEvento.cs:16-22`) | `ApuracaoPontoFechada` é `IDomainEvent` in-process **sem nenhum handler** (zero handlers no módulo inteiro). Não há Integration Event de ponto em Contracts. HE/falta/noturno entram na folha **só por digitação manual**; minutos do ponto nunca viram dinheiro. | Reproduz exatamente o rework e o erro do sistema anterior no setor mais usado: operador calcula HE/falta/noturno à mão e digita. O "gancho via Outbox" prometido nos comentários é falso. | G | WF-PONTO |
| **P0-4** | **13º proporcional na rescisão sem base separada** (`GerarVerbasRescisorias.cs:98,174`) | 13º-prop, férias-venc/prop lançados como proventos comuns numa folha Tipo=Rescisao; quem apura é o motor mensal (simplificado ligado), não `CalcularBaseSeparada`. 13º exige base separada (art. 12-A), simplificado **vedado**. | IRRF de rescisão errado e misturado; férias indenizatórias podem ser tributadas se a rubrica não tiver incidência desligada → autuação. | M | WF-FOLHA |
| **P0-5** | **Líquido negativo clampado a zero silenciosamente** (`MotorDeCalculoFolha.cs:140-143`; `FolhaDePagamento.cs:199-200`) | Soma de descontos > proventos → líquido vira R$ 0 **sem erro/alerta**. Descontos legais podem ter sido recolhidos sobre base cheia mas o servidor não recebe. | Quebra de garantia de líquido mínimo (irredutibilidade); servidor "recebe" R$ 0 sem auditoria. Deveria falhar ou priorizar descontos legais sobre consignados. | P | WF-FOLHA |
| **P0-6** | **Dobra de férias (CLT 137) detectada mas nunca aplicada** (`PeriodoAquisitivoFerias.cs:38 EmDobra`; `CalculadoraFerias.cs:37-61`; `GerarVerbasRescisorias.cs:147`) | O método `EmDobra` existe mas nenhuma calculadora multiplica por 2. Período concessivo expirado = pagamento em dobro. | Servidor que não tirou férias em 2 anos (frequente) recebe **metade do devido** → passivo trabalhista / condenação. Código morto. | P | WF-FOLHA |
| **P0-7** | **Fechar folha é irreversível a um clique, sem conferência** (`FolhaDetailPage.tsx:84-92,184-191`) | `fechar.mutate` direto, sem confirmação, sem prévia de totais por servidor, sem lista de divergências. | Ação com efeito financeiro/eSocial; o erro só é descoberto **depois**, no contracheque. Para o setor que "mais dava problema", é o defeito nº1. | P | WF-UX |

---

## (2) P1 — UX do ciclo mensal + conferência (reduz a dor diária)

| # | Item | O que é | Por que dói na prefeitura | Esforço | WF |
|---|------|---------|---------------------------|---------|----|
| **P1-1** | **Sem visão da folha inteira / "sem lançamento"** (`folha.api.ts:107-116`; `RecursosHumanosEndpoints.cs`) | Contracheque só 1-a-1 via modal; sem GET de lista de contracheques nem query de quem ficou de fora. | Conferir 300 servidores = abrir 300 modais. Não há como saber, antes de fechar, quem ficou com líquido zerado. **Falta endpoint + tela.** | G | WF-UX |
| **P1-2** | **Lançamento de evento 100% unitário — zero lote** (`AdicionarEventoFormModal.tsx:82-88`; `Endpoints.cs:401`) | Um evento, um servidor por vez; sem "aplicar a cargo/seleção", sem import de planilha. | Lançar um adicional para 50 servidores = 50 modais. Trabalho mensal repetido na pior ergonomia. **Falta endpoint batch.** | G | WF-UX |
| **P1-3** | **Contracheque não imprime nem gera PDF** (`ContrachequeModal.tsx:69-71,130-149`) | Nenhum `window.print`, `@media print` ou PDF no módulo. Vale também para Minha Folha. | Servidor de prefeitura precisa do holerite físico (banco, renda). Hoje impossível pelo sistema. | M | WF-UX |
| **P1-4** | **Nenhuma lista paginada/buscável** (`DataTable.tsx:48-70`; `servidor.api.ts:82-84`; `ServidoresListPage.tsx:91-105`) | `DataTable` só ordena client-side; `/servidores/ativos` traz todos sem page/query. | 300-2000 servidores: payload gigante, render lento, rolagem infinita sem busca por nome/matrícula. Mesmo problema em ponto e eSocial. | M | WF-UX |
| **P1-5** | **Picker de rubricas existe mas o form ignora** (`rubrica.api.ts:72 useRubricasVigentes`; `AdicionarEventoFormModal.tsx:157-168`) | Form usa `<Input>` texto livre p/ código eSocial em vez do hook que já existe. | Operador decora/digita código eSocial à mão → erro de digitação vira rubrica errada na folha. Capacidade do back desperdiçada. | P | WF-UX |
| **P1-6** | **FolhaDetailPage depende de router `state` — F5 quebra** (`FolhaDetailPage.tsx:48,55-56,105-110`) | Competência só vem de `location.state`; recarregar/link direto cai em EmptyState. Falta GET folha por id no back. | O operador recarrega a folha o tempo todo; F5 = beco sem saída. Frustração diária. | M | WF-UX |
| **P1-7** | **"Calcular" não devolve relatório de exceções** (`FolhaDetailPage.tsx:74-82,142-153`) | Só toast + 3 totalizadores; sem alerta de líquido negativo, base zerada, comparação com competência anterior. | Calcular folha real precisa de relatório de divergências; aqui o erro passa despercebido. | M | WF-UX |
| **P1-8** | **Adicional noturno ausente (hora reduzida 52'30")** (`TratamentoJornada.cs:90-146`) | Hora noturna conta como 60'; adicional ≥20% não calculado. | Servidor 22h-05h subpago sistematicamente. | M | WF-PONTO |
| **P1-9** | **DSR não modelado (sobre HE / reflexo na falta)** (grep DSR=0) | Falta injustificada deveria fazer perder DSR da semana; HE habituais geram DSR. | Desconto/provento de DSR não calculados — erro recorrente clássico de prefeitura. | M | WF-PONTO |
| **P1-10** | **HE sem percentuais (50%/100%) e sem base** (`TratamentoJornada.cs:136-139`) | Só acumula minutos; não distingue dia útil (50%) de domingo/feriado (100%); sem valor monetário. | Toda HE tratada igual e sem conversão em dinheiro. | M | WF-PONTO |
| **P1-11** | **Múltiplos vínculos lícitos não somam INSS/IRRF** (sem conceito de acúmulo no domínio; `TabelaInss.cs:101`) | Teto INSS aplicado por cálculo isolado; IRRF não soma fontes. | Professor+médico (acúmulo CF 37 XVI): INSS acima do teto somado / faixas IRRF erradas. Comum em saúde/educação. | G | WF-FOLHA |
| **P1-12** | **Abate-teto (CF 37 XI) sobre todos os proventos** (`FolhaDePagamento.cs:212-222`) | Soma todo `Tipo==Provento` sem excluir indenizatórias (diárias, ajuda de custo, abono pecuniário, terço de férias). | Abate-teto inflado → servidor perde verbas que a CF §11 manda preservar. Litígio. | M | WF-FOLHA |
| **P1-13** | **Sem retroativos / recálculo de mês fechado / RRA** (grep retroativo/RRA=0; `FolhaDePagamento.cs:240`) | `Fechar` trava edição; sem reabertura, diferença de competência anterior, nem RRA (Lei 12.350/2010). | Revisão geral anual sai após a data-base (comuníssimo); diferenças retroativas viram rubrica avulsa com IRRF errado. | G | WF-FOLHA |
| **P1-14** | **Banco de horas sem prazo de compensação e sem teto** (`ApuracaoPonto.cs:88-91,25`; `PontoRepositories.cs:159-172`) | Docstring promete janela 6m/1a que o código nunca aplica; saldo cresce sem limite; HE prescritas não viram pagamento. | Saldo infinito; HE que deveria ser paga some no banco. | M | WF-PONTO |
| **P1-15** | **Abono/justificativa de falta inexistente** (grep abono/justificativa=0) | Sem entidade/fluxo de atestado, licença, feriado. | Falta justificada descontada como injustificada → erro de pagamento e reclamação. | M | WF-PONTO |
| **P1-16** | **Layout AFD/AEJ não validado contra texto oficial** (`Enums.cs:34-68`; `GeradorAej.cs:38-39`) | Posições/larguras são `// TODO(validar-oficial)`. | Arquivo pode ser **rejeitado em fiscalização do trabalho** — bloqueia uso fiscal real. | M | WF-PONTO |

---

## (3) P2 — Lacunas de funcionalidade (gestão de pessoas), priorizadas por impacto

| # | Item | O que é | Por que dói na prefeitura | Esforço | WF |
|---|------|---------|---------------------------|---------|----|
| **P2-1** | **Composição automática da folha (vencimento + verbas fixas)** (`Cargo.cs:71` tem vencimento mas nada lança; folha é manual pura) | Gerador que ao abrir a folha puxa vencimento do cargo + rubricas recorrentes por servidor ativo. | **Raiz do "trabalho manual e erro".** Hoje o RH digita o salário-base de cada um todo mês. Sem isso nenhuma prefeitura roda folha de verdade. | G | WF-GESTAO |
| **P2-2** | **Afastamentos/licenças tipados com efeito na folha** (`Servidor.cs:232-249` motivo=string; `Enums.cs:18-19` estado único `Afastado`) | Enum tipado (maternidade 180d, paternidade, auxílio-doença 15+, licença-prêmio, sem vencimento, acidente, mandato) com ônus, incidência previdenciária, dedução de avos, eSocial S-2230/2231. | Afastar alguém **não muda a folha** hoje: paga-se cheio quem está em licença sem vencimento. Erro garantido + apontamento TCE. Diário no RH. | G | WF-GESTAO |
| **P2-3** | **Salário-família + pensão real + adicionais** (`Dependente.cs` existe mas não calcula; pensão `0m` = P0-1) | Salário-família por dependente ≤14a com teto (R$ 65,00/cota até R$ 1.906,04 em 2025); adicionais (insalubridade/periculosidade/noturno/tempo) como rubricas com gatilho. | Salário-família manual = erro recorrente. (Pensão zerada já está em P0-1.) | M | WF-GESTAO |
| **P2-4** | **Margem consignável + consignações** (inexistente; consignado entra como "outros descontos" — `MotorDeCalculoFolha.cs:95`) | Cadastro de consignatárias/contratos; cálculo de margem (35% + reserva) sobre remuneração; bloqueio ao exceder; averbação/suspensão. | Funcionário público vive de consignado; sem controle de margem a prefeitura averba acima do teto legal → ilegal, judicializa, líquido negativo silencioso (liga com P0-5). | G | WF-GESTAO |
| **P2-5** | **Relatórios gerenciais + demonstrativo TCE** (só eventos de integração; nenhuma query gerencial nos endpoints) | Folha-resumo por secretaria/UO/fonte, evolução mensal, mapa de cargos (providos×vagos), demonstrativo de pessoal TCE, folha analítica por rubrica. | TCE-RS cobra toda competência; sem relatório o RH exporta CSV e monta no Excel. | M | WF-GESTAO |
| **P2-6** | **Progressão/promoção funcional** (`PlanoDeCargos.cs` só rótulo; sem nível/classe/tabela) | Estrutura nível×classe com tabela de vencimentos; regra de progressão (triênio/quinquênio/avaliação); reflexo na folha; histórico. | Progressão é direito com data-base; sem motor vira concessão manual atrasada → passivo + apontamento. | G | WF-GESTAO |
| **P2-7** | **Ficha/histórico funcional completo** (`ValueObjects.cs:62-89` só nome + nascimento) | Assentamento funcional: PIS/PASEP, RG, endereço, dados bancários, escolaridade, histórico cronológico consolidado. | **eSocial S-2200 exige PIS, raça/cor** — hoje não há onde guardar. Pagamento por banco também não tem dado. | M | WF-GESTAO |
| **P2-8** | **Aposentadoria / abono permanência** (só `Desligar("aposentadoria")` string) | Abono permanência como rubrica/regra; encaminhamento ao RPPS; cálculo de proventos/elegibilidade. | Desconto/provento recorrente comum em quadro maduro; ausência gera pagamento errado. | M | WF-GESTAO |
| **P2-9** | **Averbação de tempo (outro ente/INSS)** (inexistente) | Registro de CTC/averbação com efeito em triênio/aposentadoria/licença-prêmio. | Sem ele a contagem de tempo erra → progressão e aposentadoria erradas. | M | WF-GESTAO |
| **P2-10** | **Acúmulo de cargos (controle constitucional)** (inexistente; nada cruza CPF) | Detecção de mesmo CPF em 2+ vínculos, compatibilidade de horário e teto, registro de licitude. | TCE cobra; risco de acúmulo ilícito. Menor frequência em município pequeno. | M | WF-GESTAO |
| **P2-11** | **Concurso/pipeline + estágio probatório** (`Servidor.cs:191-220` pula de Exercício a Estável; sem avaliações) | Cadastro de concurso/classificados/convocação; estágio probatório como fase com avaliações (CF 41 §4º). | Estágio probatório é avaliação anual obrigatória; concurso é esporádico. | M | WF-GESTAO |
| **P2-12** | **Faltas/DSR e 13º complementar de dezembro** (`GerarDecimoTerceiro.cs:91,100`; faltas só informativo) | Desconto de falta com reflexo DSR; redução de avos de férias por faltas (CLT 130); recálculo da diferença de 13º em dezembro quando a remuneração sobe após a 2ª parcela. | Férias com 30 dias indevidos; diferença de 13º não paga ao servidor (caso clássico de fim de ano). | M | WF-FOLHA |
| **P2-13** | **Polimento UX** (combobox de servidor com busca — `AdicionarEventoFormModal.tsx:144-153`, `ContrachequeModal.tsx:83-91`; eSocial paginado; ciclo anual com idempotência visível; validação de intervalo de datas AFD/AEJ; a11y `role=tabpanel` em `MinhaFolhaPage.tsx`; stepper do ciclo da folha) | Itens P2 da auditoria UX agrupados. | Atrito acumulado; nenhum bloqueia o piloto isolado. | M | WF-UX |
| **P2-14** | **Robustez do ponto** (race no NSR — `RegistrarMarcacao.cs:60-61`; intervalo intrajornada não deduzido — `TratamentoJornada.cs:110-124`; marcação ímpar vira falta integral silenciosa `:126-129,145`; tolerância por dia e não por marcação `:131-143`; saldo banco anterior sem filtrar `Situacao`; `SoDigitos` quebra CPF vazio no AEJ `GeradorAej.cs:103-104`; arredondamento avos→dias `GerarVerbasRescisorias.cs:158`) | Bugs e arestas do motor de ponto/jornada agrupados. | Inconsistências pontuais; corrigir junto com WF-PONTO. | M | WF-PONTO |

---

## ORDEM DE EXECUÇÃO RECOMENDADA

Critério: primeiro o que **fere dinheiro/legalidade já no piloto** e é barato; depois o que **destrava a operação de massa**; por fim o **quadro funcional maduro**.

1. **Quick-wins de cálculo (P0-1, P0-5, P0-6) + trava de fechamento (P0-7).** Esforço baixo, risco alto. Corrige pensão hardcoded, líquido negativo silencioso, dobra de férias morta e o clique irreversível. — *WF-FOLHA + WF-UX*
2. **13º de rescisão com base separada (P0-4).** Fecha o último bug fiscal isolado da rescisão. — *WF-FOLHA*
3. **Composição automática da folha (P2-1).** Raiz do trabalho manual; precede tudo de operação de massa. — *WF-GESTAO*
4. **Consolidação fiscal multi-folha no mês (P0-2).** Requer repensar o agregado/competência; alto, mas é autuação certa. Fazer após (3) para já consolidar sobre folha composta. — *WF-FOLHA*
5. **Ponte ponto→folha (P0-3) + noturno/DSR/HE% (P1-8/9/10) + banco/abono (P1-14/15).** O bloco WF-PONTO que elimina a digitação manual de frequência. — *WF-PONTO*
6. **Operação de massa na UI: lista/conferência da folha + pré-fechamento + lote + impressão (P1-1, P1-2, P1-3, P1-7).** Inclui os endpoints de lista/batch faltantes. Destrava o uso real com 300+ servidores. — *WF-UX (+ endpoints)*
7. **Conforto diário UX: paginação/busca (P1-4), rubrica via picker (P1-5), GET folha por id / F5 (P1-6).** — *WF-UX*
8. **Afastamentos tipados com efeito (P2-2) + salário-família (P2-3) + margem consignável (P2-4).** Gestão de pessoas que roda toda competência. — *WF-GESTAO*
9. **Relatórios/TCE (P2-5) + ficha funcional/eSocial S-2200 (P2-7).** Auditoria e cadastro. — *WF-GESTAO*
10. **Validação oficial AFD/AEJ (P1-16) + retroativos/RRA (P1-13) + acúmulo/múltiplos vínculos (P1-11, P2-10) + abate-teto indenizatórias (P1-12) + maduros (P2-6/8/9/11) + polimentos (P2-12/13/14).** — *conforme WF*

> Confirmação recomendada para P0-2/P0-4: teste de integração apurando INSS/IRRF de servidor com folha Mensal + Ferias na mesma competência e comparando com o consolidado — os testes atuais cobrem cada folha isolada e não pegam o gap.

---

## Pontos que estão CORRETOS (não regredir)

- INSS/RPPS progressivo cumulativo com arredondamento único sobre a soma das faixas (`FaixaProgressiva.cs:45-62`).
- Fail-closed de tabela legal ausente e teto remuneratório não-positivo (`MotorDeCalculoFolha.cs:106-121`; `FolhaDePagamento.cs:171-174`).
- Determinismo sem relógio (datas/avos como entrada; calculadoras puras).
- 13º da folha mensal própria usa base separada + simplificado vedado corretamente (`GerarDecimoTerceiro.cs:128`).
- Higiene de componentes do front: `QueryState`, `EmptyState`, `Can` gating, mapeamento de `fieldErrors` (`AdicionarEventoFormModal.tsx:100-107`), a11y de tabela (`DataTable.tsx:91-122`), aviso honesto de eSocial simulado.

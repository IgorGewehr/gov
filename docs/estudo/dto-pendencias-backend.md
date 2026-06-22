# Pendências de BACKEND (levantadas na reconciliação de DTOs do frontend)

> Itens em que a **UI precisa de um campo/endpoint que o backend hoje não expõe**.
> O frontend já foi ajustado para CASAR com o contrato atual (não quebra o demo);
> estes pontos são melhorias de backend a tratar depois. Nada aqui foi alterado no backend.

Data: levantado durante a aplicação das correções de `dto-legislativo.md`,
`dto-recursoshumanos.md` e `dto-tributos.md`.

---

## Legislativo

### 1. Nome do vereador nas projeções de Comissão e Tribuna
- **Onde:** `GET /comissoes/{id}` (`MembroComissaoResumo`) e `GET /sessoes/{id}/tribuna`
  (`InscricaoDto`).
- **Hoje:** o backend envia apenas `VereadorId` (sem nome).
- **Mitigação no front (já aplicada):** resolvemos o nome no cliente via `useVereadores()`
  (Map `vereadorId → nomeParlamentar`). Funciona, mas faz uma busca extra e depende de o
  cadastro estar carregado.
- **Pedido (opcional):** incluir `NomeParlamentar` (ou `NomeVereador`) nessas duas projeções,
  como já é feito em `PainelVotacao.Votos[]`. Elimina o join no cliente.

### 2. Situação da sessão no painel da Tribuna
- **Onde:** `GET /sessoes/{id}/tribuna` (`TribunaDto`).
- **Hoje:** o `TribunaDto` não traz a situação da sessão; o polling do painel depende de saber
  se a sessão está "Aberta".
- **Mitigação no front (já aplicada):** o componente busca `useSessao(sessaoId)` e usa
  `situacao === 'Aberta'` para controlar o `refetchInterval`. Custa uma requisição adicional.
- **Pedido (opcional):** expor `SituacaoSessao` (string) no `TribunaDto` para o front controlar
  o polling sem a chamada extra.

---

## RecursosHumanos

### 3. Espelho de ponto vazio (apuração não devolve os minutos)
- **Onde:** `POST /ponto/apuracoes` → hoje devolve **apenas** `{ id }`.
- **Hoje:** a tela `EspelhoPonto`/`PontoServidorPage` foi desenhada para mostrar minutos
  trabalhados/extras/falta e banco de horas, mas a resposta não traz esses campos (o front os
  trata como opcionais e não quebra — fica em branco até gerar o AEJ).
- **Pedido:** enriquecer a resposta de `ApurarJornadaCommand` com os totais do espelho, **ou**
  criar `GET /ponto/apuracoes/{id}` retornando o espelho (minutos trabalhados/extras/faltas/
  banco de horas). Impacto de PoC alto na trilha de ponto (M5).

> Demais itens de RecursosHumanos: 0 divergências de contrato (módulo já alinhado ao backend).

---

## Tributos

> Observação: as divergências de **nome de campo / tipo / estrutura de payload** de
> IPTU/ISS/ITBI foram TODAS corrigidas no frontend (sem mudança de backend). Os itens abaixo
> são **dados que a UI gostaria de exibir e o backend não devolve** — a UI foi adaptada para
> não depender deles (sem crash), mas a experiência fica mais pobre.

### 4. IPTU — memória de cálculo na apuração
- **Onde:** `GET /imoveis/{id}/iptu/{exercicio}` (`ResultadoIptu`).
- **Hoje:** resposta achatada (`valorVenal`, `aliquotaPercentual`, `impostoBruto`,
  `valorIsencao`, `valorDesconto`, `impostoDevido`) — **sem** lista `memoria[]`.
- **Mitigação no front (já aplicada):** removemos a tabela de memória; exibimos os componentes
  achatados. Também removemos os campos inexistentes `descontoCotaUnica`/`valorCotaUnica`.
- **Pedido (opcional):** se a UI precisar exibir uma memória de cálculo detalhada (linha a
  linha), o backend precisaria devolvê-la (ex.: `Memoria: { rotulo, detalhe, valor }[]`).

### 5. ISS — apuração não devolve o livro fiscal nem a competência
- **Onde:** `POST /iss/contribuintes/{id}/apurar` (`ResultadoApuracaoIss`).
- **Hoje:** devolve `apuracaoId`, `lancamentoId?`, `quantidadeNotas`, `issProprio`,
  `issRetido`, `issSubstituicao`. **Não** devolve `competencia`, `baseCalculoTotal`, nem o
  `livro[]` (uma linha por NFS-e).
- **Mitigação no front (já aplicada):** removemos a tabela do "livro fiscal eletrônico" e os
  totais inexistentes; o `setQueryData` passou a chavear por `(contribuinteId, ano, mes)` (das
  variáveis do comando, não da resposta).
- **Pedido (opcional):** se a tela do livro eletrônico for requisito de PoC, o backend precisa
  expor o livro apurado (ex.: `GET /iss/apuracoes/{id}/livro` ou incluir `Livro[]` na resposta).

### 6. ITBI — preview/lançamento sem memória de cálculo
- **Onde:** `GET /itbi/imoveis/{id}/preview` (`ResultadoItbi`) e `POST /itbi/lancar`
  (`ResultadoLancamentoItbi`).
- **Hoje:** respostas achatadas (`baseCalculo`, `origem`, `haDivergenciaReferencia`,
  `impostoBruto`, `valorIsencao`, `impostoDevido`) — **sem** `memoria[]` e **sem** `guiaNumero`.
- **Mitigação no front (já aplicada):** removemos a tabela de memória e o uso de `guiaNumero`
  (exibimos `lancamentoId`/`damId`/`transmissaoId`). O indicador de SFH passou a vir do estado
  do formulário (consulta), pois o preview não devolve `sfh`.
- **Pedido (opcional):** memória de cálculo detalhada e/ou número da guia (DAM) legível, se a UI
  precisar exibi-los.

---

## Resumo

| # | Módulo | Endpoint | Campo/feature faltante | Bloqueia demo? | Mitigado no front? |
|---|---|---|---|---|---|
| 1 | Legislativo | `GET /comissoes/{id}`, `GET /sessoes/{id}/tribuna` | nome do vereador no membro/inscrição | Não | Sim (join client-side) |
| 2 | Legislativo | `GET /sessoes/{id}/tribuna` | situação da sessão (p/ polling) | Não | Sim (useSessao) |
| 3 | RecursosHumanos | `POST /ponto/apuracoes` | espelho (minutos) na resposta | Parcial (tela vazia) | Sim (campos opcionais) |
| 4 | Tributos | `GET .../iptu/{exercicio}` | memória de cálculo | Não | Sim (removida) |
| 5 | Tributos | `POST /iss/.../apurar` | livro fiscal + competência | Parcial (livro some) | Sim (removido) |
| 6 | Tributos | `GET /itbi/.../preview`, `POST /itbi/lancar` | memória + guiaNumero | Não | Sim (removidos) |

Nenhum destes itens impede o fluxo principal do demo após as correções de contrato no frontend.

# Módulo Educacao

> Gestão da rede municipal de ensino — escolas, matrículas, diário de classe eletrônico, matriz curricular (BNCC), merenda (PNAE) e transporte escolar (PNATE). · Poder: **Executivo** · Schema EF Core: `educacao` · Ativável por tenant.

## 1. Propósito & Marco Legal

Informatiza a Secretaria Municipal de Educação: cadastro de escolas, ciclo de matrícula,
registro pedagógico (frequência/notas), planejamento curricular alinhado à BNCC, operação da
alimentação escolar e do transporte do escolar — com geração dos dados exigidos pelo **EducaCenso/INEP**
e prestação de contas ao **FNDE**.

- **CF/1988, arts. 205–214** — educação como direito; ensino fundamental obrigatório; mínimo de **25%** da receita de impostos em MDE.
- **LDB — Lei 9.394/1996** (atualizada pela **Lei 14.945/2024**) — etapas, dias letivos, carga horária, frequência, avaliação.
- **FUNDEB — EC 108/2020 + Lei 14.113/2020** — financiamento por matrículas ponderadas do Censo; mín. **70%** para magistério.
- **BNCC** — Resoluções CNE/CP **2/2017** (Infantil/Fundamental) e **4/2018** (Médio).
- **Lei 13.415/2017 + Lei 14.945/2024** — Ensino Médio: **3.000h** totais, sendo **2.400h de Formação Geral Básica (FGB)**.
- **PNAE — Lei 11.947/2009** + **Res. CD/FNDE 4/2026** (mín. **30%** em agricultura familiar) + **CFN 465/2010** (nutricionista RT).
- **PNATE — Lei 10.880/2004** — apoio ao transporte do escolar.
- **LGPD — Lei 13.709/2018, art. 14** — tratamento de dados de crianças e adolescentes no seu melhor interesse.

## 2. Linguagem Ubíqua

| Termo | Definição |
|---|---|
| **EducacaoBasica** | Etapas Infantil, Fundamental e Médio. |
| **CodigoINEP** | Identificador único nacional de escola/turma/aluno — chave de integração com o Censo. |
| **MatriculaInicial** | 1ª etapa do Censo: vínculo aluno↔turma↔escola na data de referência. |
| **SituacaoDoAluno** | 2ª etapa do Censo: rendimento (aprovado/reprovado) + movimento (transferido/abandono/falecido). |
| **DiarioDeClasse** | Registro digital de frequência, conteúdos e notas. |
| **Frequencia** | Percentual de presença; mínimo **75%** da carga horária para aprovação. |
| **DiaLetivo** | Dia de efetivo trabalho escolar com aluno (mín. **200/ano**). |
| **CargaHoraria** | Horas anuais (mín. **800h** Fundamental / **1.000h** Médio). |
| **Turma** | Agrupamento de alunos por etapa/série/turno (enturmação). |
| **EtapaEnsino** | Código INEP da fase (ex.: anos iniciais/finais do Fundamental). |
| **Rematricula** | Renovação de matrícula do aluno apto ao ano seguinte. |
| **DependenciaAdministrativa** | Federal/Estadual/Municipal/Privada. |
| **MatriculaPonderada** | Matrícula multiplicada pelo fator FUNDEB. |
| **IDEB** | Indicador de qualidade (fluxo × SAEB), bienal. |
| **BNCC** | Base Nacional Comum Curricular — aprendizagens essenciais por etapa. |
| **MatrizCurricular** | Distribuição de componentes e carga horária por série/turno. |
| **ComponenteCurricular** | Disciplina/unidade de ensino (ex.: Língua Portuguesa). |
| **FGB** | Formação Geral Básica — núcleo comum obrigatório do Médio (2.400h). |
| **ItinerarioFormativo** | Trilha de aprofundamento/eletiva do Ensino Médio. |
| **Cardapio** | Instrumento operacional do PNAE com preparações, per capita e composição nutricional. |
| **PerCapita** | Quantidade de alimento por aluno/refeição. |
| **NutricionistaRT** | Responsável Técnico pelo PNAE no ente executor. |
| **ChamadaPublica** | Procedimento simplificado de compra da agricultura familiar. |
| **CAE** | Conselho de Alimentação Escolar (controle social do PNAE). |
| **RotaTransporte** | Trajeto georreferenciado com paradas e capacidade (PNATE). |

## 3. Mapa de Domínio

- **Escola** *(raiz)* — VOs `CodigoINEP`, `DependenciaAdministrativa`, `Endereco`, `Infraestrutura`.
  → eventos: `EscolaCredenciada`, `DadosCensoAtualizados`.
- **Turma** *(raiz)* — VOs `EtapaEnsino`, `Turno`, `CalendarioEscolar`. Invariante: matriculados ≤ vagas.
  → evento: `TurmaEnturmada`.
- **Aluno** *(raiz)* — VOs `CodigoINEP`, `Documentos`, `Responsavel`. → `AlunoCadastrado`.
- **Matricula** *(raiz)* — VOs `SituacaoMatricula`, `DataReferencia`; estados `Ativa → Transferida/Concluida/Abandono`.
  → `AlunoMatriculado`, `AlunoTransferido`, `MatriculaEncerrada`.
- **DiarioClasse** *(raiz)* — entidades `RegistroFrequencia`, `RegistroNota`, `RegistroAula`. Invariante: frequência apurada por período.
  → `FrequenciaRegistrada`, `NotaLancada`, `ResultadoApurado`.
- **MatrizCurricular** *(raiz)* — `ComponenteCurricular`, `CargaHorariaComponente`, `ItinerarioFormativo`, `VinculoBNCC`.
  → `MatrizCurricularPublicada`.
- **Cardapio** *(raiz)* — `Refeicao`, `ItemCardapio`, VO `PerCapita`; relaciona `NutricionistaRT`, `ChamadaPublica` (→ `LoteAgriculturaFamiliar`), `EstoqueMerenda`.
  → `CardapioAprovadoPeloRT`, `ChamadaPublicaHomologada`, `PercentualAgriculturaFamiliarAtingido`, `EstoqueAbaixoDoMinimo`.
- **RotaTransporte** *(raiz)* — `Veiculo`, `Motorista`, `Parada` (georref.), VO `VagasPorRota`, `VinculoAlunoRota`.
  → `AlunoVinculadoARota`, `RotaSuperlotada`.

**Relações:** `Aluno` 1—N `Matricula` N—1 `Turma` N—1 `Escola`; `Matricula` 1—1 vínculo no `DiarioClasse`.

## 4. Integrações & Padrões Técnicos

- **EducaCenso/INEP** — exportação por **leiaute posicional anual** (formulários Escola, Gestor, Turma, Aluno, Profissional). 1ª etapa = Matrícula Inicial; 2ª etapa = Situação do Aluno. Validação e reconciliação por `CodigoINEP`; **idempotência por código INEP**.
- **FNDE** — matrículas alimentam **FUNDEB** (coeficientes), **PNAE** e **PNATE**; **SiGPC/Contas Online** para prestação de contas.
- **BNCC/MEC** — mapeamento de componentes às habilidades (códigos alfanuméricos, ex.: `EM13LP01`).
- **Georreferenciamento** — coordenadas de paradas/escolas (GeoJSON) para otimização e auditoria de rotas.
- Integrações externas resilientes (Polly) atrás de **Anti-Corruption Layer**; geração de leiaute versionada por ano-base.

## 5. Regras de Negócio Críticas

- **Frequência ≥ 75%** da carga horária → aprovação; abaixo → **reprovado por frequência**.
- Mínimo de **200 dias letivos** e **800h** (Fundamental) / **1.000h** (Médio) no calendário.
- **Ensino Médio:** total **3.000h** com **≥ 2.400h de FGB**; Língua Portuguesa e Matemática obrigatórias nos 3 anos.
- A soma da carga horária dos componentes deve **fechar** a carga total da série/etapa.
- Matrícula Inicial reflete a **data de referência** do Censo; aluno não pode ter **duas matrículas ativas conflitantes** no mesmo período/turno.
- Encerramento do ano letivo exige **Situação do Aluno preenchida** para todos os matriculados.
- **Cardápio** elaborado/assinado pelo **nutricionista RT**; **≥ 30%** dos recursos federais do PNAE em **agricultura familiar** (chamada pública).
- **Vagas por rota ≤ capacidade do veículo** (regularização Detran/ANTT); vínculo aluno↔rota único e válido por matrícula.
- **FUNDEB**: só matrículas válidas no Censo geram repasse.

## 6. Multi-Tenancy, Segurança & Auditoria

- Isolamento por **tenant = ente municipal** (rede de ensino); escopo por `CodigoINEP` da rede; dados não cruzam tenants.
- Todas as raízes implementam **`IMustHaveTenant`**; Global Query Filter aplicado.
- **LGPD (art. 14 — menores):** tratamento no melhor interesse da criança; minimização (coletar só o necessário ao Censo/matrícula); base legal de cumprimento de obrigação legal e política pública; geolocalização aluno↔rota restrita ao estritamente necessário.
- **Auditoria imutável** de lançamentos de frequência/nota (autor, timestamp, valor anterior), versionamento do diário e logs de exportação ao INEP/FNDE.
- **RBAC:** professor lança apenas a própria turma; secretaria escolar; gestor da rede.

## 7. Contratos Públicos (Integration Events)

**Publica:** `AlunoMatriculado`, `MatriculaEncerrada`, `ResultadoApurado`, `MatrizCurricularPublicada`, `ChamadaPublicaHomologada` (consumível por **Transparencia** e **Financas**/**Patrimonio** — estoque da merenda/combustível das rotas).
**Consome:** eventos de **Administracao** (`ContratoAssinado` — gêneros da merenda, frota terceirizada) e **Patrimonio** (veículos próprios de transporte). A prestação de contas é exposta a **Transparencia**.

## 8. Cenários BDD

1. **Aprovação por frequência e nota**
   **Dado** um aluno com 75% de frequência e médias suficientes,
   **Quando** o resultado anual é apurado,
   **Então** a situação registrada é "Aprovado" e emite-se `ResultadoApurado`.
2. **Reprovação por frequência**
   **Dado** um aluno com frequência de 70%,
   **Quando** o resultado é apurado,
   **Então** a situação é "Reprovado por frequência".
3. **Matriz do Médio abaixo da FGB**
   **Dado** uma matriz curricular do Ensino Médio com 2.300h de FGB,
   **Quando** o coordenador tenta publicá-la,
   **Então** o sistema bloqueia por violar o mínimo de 2.400h.
4. **Agricultura familiar abaixo do mínimo**
   **Dado** uma chamada pública com 22% destinado à agricultura familiar,
   **Quando** se tenta homologá-la,
   **Então** o sistema emite alerta de descumprimento do mínimo de 30%.
5. **Rota superlotada**
   **Dada** uma rota com veículo de 32 lugares já com 32 alunos,
   **Quando** se vincula o 33º aluno,
   **Então** o sistema recusa por superlotação (`RotaSuperlotada`).
6. **Geração do EducaCenso**
   **Dado** o fechamento da Matrícula Inicial,
   **Quando** o leiaute do EducaCenso é gerado,
   **Então** todos os alunos têm `CodigoINEP` válido e o arquivo passa na validação.

## 9. Fontes

- LDB — https://www.planalto.gov.br/ccivil_03/leis/l9394.htm
- Lei 14.945/2024 (Ensino Médio) — https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2024/lei/l14945.htm
- BNCC — https://basenacionalcomum.mec.gov.br/
- Censo Escolar/EducaCenso (INEP) — https://www.gov.br/inep/pt-br/areas-de-atuacao/pesquisas-estatisticas-e-indicadores/censo-escolar
- FUNDEB (Lei 14.113/2020) — https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14113.htm
- PNAE / PNATE (FNDE) — https://www.gov.br/fnde/pt-br/acesso-a-informacao/acoes-e-programas/programas

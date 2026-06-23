// BOLETIM do aluno (médias por componente + % de frequência + resultado) e HISTÓRICO
// ESCOLAR longitudinal. Reusa o AlunoPicker: escolhido o aluno, lista o histórico
// (uma matrícula por ano/turma) e, ao selecionar uma matrícula, abre o boletim.
//   GET /educacao/alunos/{alunoId}/historico-escolar -> HistoricoEscolarView
//   GET /educacao/matriculas/{matriculaId}/boletim   -> BoletimAlunoView
import { useState } from 'react';
import {
  Card,
  CardSecao,
  DataTable,
  EmptyState,
  Metrica,
  MetricaGrade,
  PageHeader,
  QueryState,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { EducacaoSubNav } from './EducacaoSubNav';
import { AlunoPicker } from './MatriculaPickers';
import type { AlunoItemLista } from './aluno.api';
import {
  formatarPercentual,
  resultadoAlunoTagVariant,
  situacaoDiarioTagVariant,
  situacaoMatriculaTagVariant,
} from './educacao.helpers';
import { useBoletimDaMatricula, useHistoricoEscolar } from './diarioTurma.api';
import type {
  BoletimAlunoView,
  HistoricoEscolarView,
  ItemHistoricoEscolar,
  MediaComponente,
} from './diarioTurma.api';

function BoletimCard({ matriculaId }: { matriculaId: string }) {
  const query = useBoletimDaMatricula(matriculaId);

  const columns: Column<MediaComponente>[] = [
    { key: 'componente', header: 'Componente curricular', render: (m) => m.componenteCurricularId },
    {
      key: 'media',
      header: 'Média',
      align: 'end',
      sortAccessor: (m) => m.media,
      render: (m) => m.media.toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }),
    },
    {
      key: 'qtd',
      header: 'Notas computadas',
      align: 'end',
      render: (m) => m.quantidadeNotas,
    },
  ];

  return (
    <CardSecao
      titulo="Boletim"
      subtitulo="Médias por componente, frequência e resultado apurado."
      nota="Frequência mínima de 75% da carga horária anual (LDB art. 24, VI)."
    >
      <QueryState<BoletimAlunoView>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data ?? undefined}
        empty={
          <EmptyState
            icon="fas fa-file-circle-question"
            title="Sem boletim"
            description="Esta matrícula ainda não possui diário de classe aberto."
          />
        }
      >
        {(boletim) => (
          <>
            <MetricaGrade>
              <Metrica
                label="Situação do diário"
                valor={
                  boletim.situacao ? (
                    <Tag variant={situacaoDiarioTagVariant(boletim.situacao)}>{boletim.situacao}</Tag>
                  ) : (
                    '—'
                  )
                }
              />
              <Metrica
                label="Frequência"
                valor={formatarPercentual(boletim.percentualFrequencia)}
                secundario={`${boletim.diasLetivosRegistrados} dia(s) letivo(s)`}
                tom={boletim.percentualFrequencia >= 75 ? 'sucesso' : 'perigo'}
              />
              <Metrica
                label="Resultado"
                valor={
                  boletim.resultado ? (
                    <Tag variant={resultadoAlunoTagVariant(boletim.resultado)}>{boletim.resultado}</Tag>
                  ) : (
                    'Em curso'
                  )
                }
              />
            </MetricaGrade>

            <DataTable
              caption="Médias por componente curricular"
              columns={columns}
              rows={boletim.medias}
              rowKey={(m) => m.componenteCurricularId}
              empty={
                <EmptyState
                  icon="fas fa-pen"
                  title="Sem notas lançadas"
                  description="Nenhuma nota foi lançada para esta matrícula até o momento."
                />
              }
            />
          </>
        )}
      </QueryState>
    </CardSecao>
  );
}

export function BoletimHistoricoPage() {
  const [aluno, setAluno] = useState<AlunoItemLista | null>(null);
  const [matriculaSelecionada, setMatriculaSelecionada] = useState<string | null>(null);

  const historico = useHistoricoEscolar(aluno?.id ?? '', aluno !== null);

  function selecionarAluno(novo: AlunoItemLista | null): void {
    setAluno(novo);
    setMatriculaSelecionada(null);
  }

  const columns: Column<ItemHistoricoEscolar>[] = [
    {
      key: 'anoLetivo',
      header: 'Ano letivo',
      sortAccessor: (i) => i.anoLetivo,
      render: (i) => i.anoLetivo,
    },
    { key: 'serie', header: 'Série/ano', render: (i) => i.serie },
    {
      key: 'situacao',
      header: 'Situação',
      render: (i) => (
        <Tag variant={situacaoMatriculaTagVariant(i.situacaoMatricula)}>{i.situacaoMatricula}</Tag>
      ),
    },
    {
      key: 'frequencia',
      header: 'Frequência',
      align: 'end',
      render: (i) => (i.percentualFrequencia != null ? formatarPercentual(i.percentualFrequencia) : '—'),
    },
    {
      key: 'resultado',
      header: 'Resultado',
      render: (i) =>
        i.resultado ? (
          <Tag variant={resultadoAlunoTagVariant(i.resultado)}>{i.resultado}</Tag>
        ) : (
          '—'
        ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (i) => (
        <button
          type="button"
          className="br-button tertiary small"
          aria-pressed={matriculaSelecionada === i.matriculaId}
          onClick={() => setMatriculaSelecionada(i.matriculaId)}
        >
          Ver boletim
        </button>
      ),
    },
  ];

  return (
    <>
      <EducacaoSubNav />
      <PageHeader
        eyebrow="Educação"
        title="Boletim e histórico escolar"
        description="Consulte o histórico escolar do aluno e o boletim de cada matrícula."
      />

      <Card className="mb-4">
        <AlunoPicker selecionado={aluno} onSelecionar={selecionarAluno} />
      </Card>

      {aluno === null ? (
        <EmptyState
          icon="fas fa-user-graduate"
          title="Selecione um aluno"
          description="Busque um aluno para ver seu histórico escolar e boletins."
        />
      ) : (
        <>
          <CardSecao
            titulo="Histórico escolar"
            subtitulo="Matrículas ao longo dos anos letivos (mais recentes primeiro)."
            className="mb-4"
          >
            <QueryState<HistoricoEscolarView>
              isLoading={historico.isLoading}
              isError={historico.isError}
              error={historico.error}
              data={historico.data}
              empty={
                <EmptyState
                  icon="fas fa-clock-rotate-left"
                  title="Sem histórico"
                  description="Este aluno ainda não possui matrículas registradas."
                />
              }
            >
              {(view) =>
                view.itens.length === 0 ? (
                  <EmptyState
                    icon="fas fa-clock-rotate-left"
                    title="Sem histórico"
                    description="Este aluno ainda não possui matrículas registradas."
                  />
                ) : (
                  <DataTable
                    caption="Histórico escolar do aluno"
                    columns={columns}
                    rows={view.itens}
                    rowKey={(i) => i.matriculaId}
                  />
                )
              }
            </QueryState>
          </CardSecao>

          {matriculaSelecionada && <BoletimCard matriculaId={matriculaSelecionada} />}
        </>
      )}
    </>
  );
}

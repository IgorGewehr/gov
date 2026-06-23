// DIÁRIO DE CLASSE (coletivo) — lançar a chamada (frequência da turma) e as notas
// por componente curricular de uma turma numa data. Reusa o TurmaPicker. Carrega a
// grade (GET /turmas/{id}/diario?data=) e expõe duas abas: Chamada e Notas. Ações
// gated por permissão (educacao.gerenciar). Lançamentos só atingem alunos com
// diário 1-1 já aberto (abertura é operação prévia na ficha da matrícula).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryState,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { EducacaoSubNav } from './EducacaoSubNav';
import { TurmaPicker } from './MatriculaPickers';
import type { TurmaItemLista } from './turma.api';
import { useDiarioDaTurma } from './diarioTurma.api';
import type { DiarioTurmaView } from './diarioTurma.api';
import { ChamadaGrade, CoberturaDiario, NotasGrade } from './DiarioTurmaGrades';

type Aba = 'chamada' | 'notas';

export function DiarioTurmaPage() {
  const [turma, setTurma] = useState<TurmaItemLista | null>(null);
  const [data, setData] = useState('');
  const [consulta, setConsulta] = useState<{ turmaId: string; data: string } | null>(null);
  const [aba, setAba] = useState<Aba>('chamada');

  const query = useDiarioDaTurma(
    consulta?.turmaId ?? '',
    consulta?.data ?? '',
    consulta !== null,
  );

  function consultar(event: FormEvent): void {
    event.preventDefault();
    if (!turma || data.trim() === '') return;
    setConsulta({ turmaId: turma.id, data });
  }

  return (
    <>
      <EducacaoSubNav />
      <PageHeader
        eyebrow="Educação"
        title="Diário de Classe"
        description="Faça a chamada (frequência) e lance notas da turma inteira em uma data."
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col-sm-7">
              <TurmaPicker selecionada={turma} onSelecionar={setTurma} />
            </div>
            <div className="col-sm-3">
              <FormField label="Data da aula" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={data}
                    onChange={(e) => setData(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-2 d-flex justify-content-end mb-3">
              <Button
                variant="primary"
                type="submit"
                disabled={!turma || data.trim() === ''}
                loading={query.isFetching}
              >
                Abrir
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-book-open"
          title="Selecione a turma e a data"
          description="Escolha uma turma aberta e a data da aula para fazer a chamada ou lançar notas."
        />
      ) : (
        <QueryState<DiarioTurmaView>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data ?? undefined}
          empty={
            <EmptyState
              icon="fas fa-users-slash"
              title="Turma sem alunos ativos"
              description="Não há matrículas ativas nesta turma para a grade do diário."
            />
          }
        >
          {(view) => (
            <Card
              header={
                <div className="d-flex justify-content-between align-items-center flex-wrap gap-2">
                  <strong>
                    {view.serie} · {view.turno} · {view.anoLetivo}
                  </strong>
                  <CoberturaDiario view={view} />
                </div>
              }
            >
              <div
                className="br-tab mb-3"
                role="tablist"
                aria-label="Lançamentos do diário"
              >
                <nav className="tab-nav">
                  <ul>
                    <li className={`tab-item ${aba === 'chamada' ? 'is-active' : ''}`}>
                      <button
                        type="button"
                        role="tab"
                        aria-selected={aba === 'chamada'}
                        onClick={() => setAba('chamada')}
                      >
                        <span className="name">
                          <i className="fas fa-clipboard-check" aria-hidden="true" /> Chamada
                        </span>
                      </button>
                    </li>
                    <li className={`tab-item ${aba === 'notas' ? 'is-active' : ''}`}>
                      <button
                        type="button"
                        role="tab"
                        aria-selected={aba === 'notas'}
                        onClick={() => setAba('notas')}
                      >
                        <span className="name">
                          <i className="fas fa-pen-to-square" aria-hidden="true" /> Notas
                        </span>
                      </button>
                    </li>
                  </ul>
                </nav>
              </div>

              <Can
                permission="educacao.gerenciar"
                fallback={
                  <EmptyState
                    icon="fas fa-lock"
                    title="Sem permissão para lançar"
                    description="Você pode consultar a grade, mas não possui permissão para lançar frequência/notas."
                  />
                }
              >
                {aba === 'chamada' ? (
                  <ChamadaGrade turmaId={view.turmaId} data={consulta.data} view={view} />
                ) : (
                  <NotasGrade turmaId={view.turmaId} data={consulta.data} view={view} />
                )}
              </Can>
            </Card>
          )}
        </QueryState>
      )}
    </>
  );
}

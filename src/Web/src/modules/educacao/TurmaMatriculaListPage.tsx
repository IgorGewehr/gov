// Tela de LISTA da Matrícula Inicial de uma TURMA na data de referência do Censo
// (GET /educacao/turmas/{turmaId}/matricula-inicial). Busca sob demanda (turma +
// data) e DataTable com estados loading/vazio/erro. Liga ao diário de cada matrícula.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Tag,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { useMatriculasDaTurma } from './api';
import type { MatriculaResumo } from './api';
import { situacaoMatriculaTagVariant } from './educacao.helpers';

export function TurmaMatriculaListPage() {
  const [turmaId, setTurmaId] = useState('');
  const [dataReferencia, setDataReferencia] = useState('');
  const [consulta, setConsulta] = useState<{ turmaId: string; dataReferencia: string } | null>(null);

  const query = useMatriculasDaTurma(
    consulta?.turmaId ?? '',
    consulta?.dataReferencia ?? '',
    consulta !== null,
  );

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsulta({ turmaId: turmaId.trim(), dataReferencia });
  }

  const columns: Column<MatriculaResumo>[] = [
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (m) => m.situacao,
      render: (m) => <Tag variant={situacaoMatriculaTagVariant(m.situacao)}>{m.situacao}</Tag>,
    },
    { key: 'aluno', header: 'Aluno', render: (m) => m.alunoId },
    { key: 'escola', header: 'Escola', render: (m) => m.escolaId },
    {
      key: 'dataReferencia',
      header: 'Data de referência (Censo)',
      sortAccessor: (m) => m.dataReferencia,
      render: (m) => formatarData(m.dataReferencia),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (m) => (
        <Link className="br-button tertiary small" to={`/educacao/matriculas/${m.id}/diario`}>
          Diário
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Educação"
        title="Matrícula Inicial por turma"
        description="Liste a Matrícula Inicial de uma turma na data de referência do Censo Escolar."
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button
                variant="primary"
                type="submit"
                disabled={turmaId.trim() === '' || dataReferencia.trim() === ''}
                loading={query.isFetching}
              >
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-7">
                <FormField label="Identificador da turma" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={turmaId}
                      onChange={(e) => setTurmaId(e.target.value)}
                      placeholder="00000000-0000-0000-0000-000000000000"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-5">
                <FormField label="Data de referência (Censo)" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="date"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={dataReferencia}
                      onChange={(e) => setDataReferencia(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe a turma e a data de referência e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Matrícula Inicial da turma ${consulta.turmaId}`}
          columns={columns}
          rows={query.data}
          rowKey={(m) => m.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-users"
              title="Nenhuma matrícula encontrada"
              description="Não há Matrícula Inicial para esta turma na data informada."
            />
          }
        />
      )}
    </>
  );
}

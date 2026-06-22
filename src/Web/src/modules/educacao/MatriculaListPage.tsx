// Tela de LISTA (consulta) de Matrículas por aluno. Padrão-ouro: busca sob demanda
// (enabled), DataTable com estados loading/vazio/erro + ordenação, link para o
// diário da matrícula e ações de gestão (Matrícula Inicial, Rematrícula, Transferência,
// Encerramento, Situação do Aluno) gated por permissão (educacao.gerenciar).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Tag,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can, useHasPermission } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useMatriculasDoAluno } from './api';
import type { MatriculaResumo } from './api';
import { situacaoMatriculaTagVariant } from './educacao.helpers';
import { MatriculaFormModal } from './MatriculaFormModal';
import {
  EncerrarMatriculaModal,
  RegistrarSituacaoModal,
  RematricularModal,
  TransferirModal,
} from './MatriculaAcaoModais';

type AcaoMatricula = 'rematricular' | 'transferir' | 'encerrar' | 'situacao';

export function MatriculaListPage() {
  const [alunoId, setAlunoId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [formAberto, setFormAberto] = useState(false);
  const [acao, setAcao] = useState<{ tipo: AcaoMatricula; matriculaId: string } | null>(null);
  const podeGerenciar = useHasPermission('educacao.gerenciar');

  const query = useMatriculasDoAluno(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(alunoId.trim());
  }

  function abrir(tipo: AcaoMatricula, matriculaId: string): void {
    setAcao({ tipo, matriculaId });
  }

  const columns: Column<MatriculaResumo>[] = [
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (m) => m.situacao,
      render: (m) => <Tag variant={situacaoMatriculaTagVariant(m.situacao)}>{m.situacao}</Tag>,
    },
    {
      key: 'dataReferencia',
      header: 'Data de referência (Censo)',
      sortAccessor: (m) => m.dataReferencia,
      render: (m) => formatarData(m.dataReferencia),
    },
    { key: 'turma', header: 'Turma', render: (m) => m.turmaId },
    { key: 'escola', header: 'Escola', render: (m) => m.escolaId },
    {
      key: 'acoes',
      header: 'Ações',
      render: (m) => (
        <div className="d-flex gap-1 flex-wrap">
          <Link className="br-button tertiary small" to={`/educacao/matriculas/${m.id}/diario`}>
            Diário
          </Link>
          {podeGerenciar && (
            <>
              <Button variant="tertiary" className="small" onClick={() => abrir('situacao', m.id)}>
                Situação
              </Button>
              <Button variant="tertiary" className="small" onClick={() => abrir('rematricular', m.id)}>
                Rematricular
              </Button>
              <Button variant="tertiary" className="small" onClick={() => abrir('transferir', m.id)}>
                Transferir
              </Button>
              <Button variant="tertiary" className="small" onClick={() => abrir('encerrar', m.id)}>
                Encerrar
              </Button>
            </>
          )}
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Matrículas"
        description="Consulte as matrículas de um aluno e gerencie o ciclo de vida do vínculo (Censo Escolar)."
        actions={
          <div className="d-flex gap-2 flex-wrap">
            <Link className="br-button secondary" to="/educacao/turmas/matricula-inicial">
              <i className="fas fa-users" aria-hidden="true" /> Matrícula Inicial por turma
            </Link>
            <Can permission="educacao.gerenciar">
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Matricular aluno
              </Button>
            </Can>
          </div>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Identificador do aluno" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={alunoId}
                    onChange={(e) => setAlunoId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" disabled={alunoId.trim() === ''} loading={query.isFetching}>
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o identificador do aluno e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Matrículas do aluno ${consultaAtiva}`}
          columns={columns}
          rows={query.data}
          rowKey={(m) => m.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-user-graduate"
              title="Nenhuma matrícula encontrada"
              description="Este aluno ainda não possui matrículas registradas."
            />
          }
        />
      )}

      <MatriculaFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        alunoIdInicial={consultaAtiva}
      />

      {acao && (
        <>
          <RematricularModal
            open={acao.tipo === 'rematricular'}
            onClose={() => setAcao(null)}
            matriculaId={acao.matriculaId}
            alunoId={consultaAtiva}
          />
          <TransferirModal
            open={acao.tipo === 'transferir'}
            onClose={() => setAcao(null)}
            matriculaId={acao.matriculaId}
            alunoId={consultaAtiva}
          />
          <EncerrarMatriculaModal
            open={acao.tipo === 'encerrar'}
            onClose={() => setAcao(null)}
            matriculaId={acao.matriculaId}
            alunoId={consultaAtiva}
          />
          <RegistrarSituacaoModal
            open={acao.tipo === 'situacao'}
            onClose={() => setAcao(null)}
            matriculaId={acao.matriculaId}
            alunoId={consultaAtiva}
          />
        </>
      )}
    </>
  );
}

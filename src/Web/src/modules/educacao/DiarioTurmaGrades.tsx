// Grades de lançamento do Diário de Classe coletivo: chamada (frequência) e notas
// por componente/período. Recebem as linhas da turma (LinhaDiarioTurma) e disparam
// o POST em lote. Apenas linhas COM diário aberto (diarioId != null) são editáveis —
// a abertura do diário 1-1 é operação prévia na tela da matrícula.
import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, DataTable, FormField, Input, Tag } from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarPercentual } from './educacao.helpers';
import type {
  DiarioTurmaView,
  LinhaDiarioTurma,
} from './diarioTurma.api';
import { useLancarNotasTurma, useRegistrarFrequenciaTurma } from './diarioTurma.api';

interface GradeProps {
  turmaId: string;
  data: string;
  view: DiarioTurmaView;
}

function linhasEditaveis(view: DiarioTurmaView): LinhaDiarioTurma[] {
  return view.linhas.filter((l) => l.diarioId !== null);
}

/** Chamada (frequência) em lote: um toggle Presente/Falta por aluno com diário aberto. */
export function ChamadaGrade({ turmaId, data, view }: GradeProps) {
  const editaveis = useMemo(() => linhasEditaveis(view), [view]);
  const [cargaHorariaAula, setCargaHorariaAula] = useState('1');
  const [presencas, setPresencas] = useState<Record<string, boolean>>({});
  const mutation = useRegistrarFrequenciaTurma(turmaId, data);

  // Inicializa o estado a partir da grade (presença do dia; default Presente).
  useEffect(() => {
    setPresencas(
      Object.fromEntries(editaveis.map((l) => [l.matriculaId, l.presente ?? true])),
    );
  }, [editaveis]);

  function lancar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate({
      data,
      cargaHorariaAula: Number(cargaHorariaAula),
      presencas: editaveis.map((l) => ({
        matriculaId: l.matriculaId,
        presente: presencas[l.matriculaId] ?? true,
      })),
    });
  }

  const columns: Column<LinhaDiarioTurma>[] = [
    { key: 'aluno', header: 'Aluno', render: (l) => l.nomeAluno, sortAccessor: (l) => l.nomeAluno },
    {
      key: 'frequencia',
      header: 'Frequência acumulada',
      align: 'end',
      render: (l) => formatarPercentual(l.percentualFrequencia),
    },
    {
      key: 'presenca',
      header: 'Chamada do dia',
      align: 'center',
      render: (l) => {
        const presente = presencas[l.matriculaId] ?? true;
        return (
          <div className="br-switch" role="group" aria-label={`Presença de ${l.nomeAluno}`}>
            <input
              id={`presenca-${l.matriculaId}`}
              type="checkbox"
              checked={presente}
              onChange={(e) =>
                setPresencas((atual) => ({ ...atual, [l.matriculaId]: e.target.checked }))
              }
            />
            <label htmlFor={`presenca-${l.matriculaId}`}>{presente ? 'Presente' : 'Falta'}</label>
          </div>
        );
      },
    },
  ];

  if (editaveis.length === 0) {
    return (
      <Alert variant="info">
        Nenhum aluno desta turma possui diário de classe aberto. Abra o diário na ficha da matrícula
        antes de fazer a chamada.
      </Alert>
    );
  }

  return (
    <form className="br-form" onSubmit={lancar}>
      <div className="row align-items-end mb-3">
        <div className="col-sm-4">
          <FormField label="Carga horária da aula" required help="Horas-aula do dia (ponderação da frequência).">
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="number"
                min={1}
                aria-describedby={describedBy}
                invalid={invalid}
                value={cargaHorariaAula}
                onChange={(e) => setCargaHorariaAula(e.target.value)}
              />
            )}
          </FormField>
        </div>
        <div className="col-sm-8 d-flex justify-content-end">
          <Button variant="primary" type="submit" loading={mutation.isPending}>
            <i className="fas fa-clipboard-check" aria-hidden="true" /> Salvar chamada
          </Button>
        </div>
      </div>

      {mutation.isSuccess && (
        <Alert variant="success">Chamada salva: {mutation.data.lancados} diário(s) atualizado(s).</Alert>
      )}
      {mutation.isError && <Alert variant="danger">Não foi possível salvar a chamada.</Alert>}

      <DataTable
        caption="Chamada da turma no dia"
        columns={columns}
        rows={editaveis}
        rowKey={(l) => l.matriculaId}
      />
    </form>
  );
}

/** Lançamento de notas em lote: componente curricular + período + nota por aluno. */
export function NotasGrade({ turmaId, data, view }: GradeProps) {
  const editaveis = useMemo(() => linhasEditaveis(view), [view]);
  const [componenteCurricularId, setComponenteCurricularId] = useState('');
  const [periodo, setPeriodo] = useState('');
  const [notas, setNotas] = useState<Record<string, string>>({});
  const mutation = useLancarNotasTurma(turmaId, data);

  function lancar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate({
      componenteCurricularId: componenteCurricularId.trim(),
      periodo: periodo.trim(),
      notas: editaveis
        .filter((l) => (notas[l.matriculaId] ?? '').trim() !== '')
        .map((l) => ({ matriculaId: l.matriculaId, valor: Number(notas[l.matriculaId]) })),
    });
  }

  const camposValidos =
    componenteCurricularId.trim() !== '' &&
    periodo.trim() !== '' &&
    editaveis.some((l) => (notas[l.matriculaId] ?? '').trim() !== '');

  const columns: Column<LinhaDiarioTurma>[] = [
    { key: 'aluno', header: 'Aluno', render: (l) => l.nomeAluno, sortAccessor: (l) => l.nomeAluno },
    {
      key: 'nota',
      header: 'Nota (0 a 10)',
      align: 'center',
      render: (l) => (
        <Input
          type="number"
          min={0}
          max={10}
          step={0.1}
          aria-label={`Nota de ${l.nomeAluno}`}
          value={notas[l.matriculaId] ?? ''}
          onChange={(e) => setNotas((atual) => ({ ...atual, [l.matriculaId]: e.target.value }))}
        />
      ),
    },
  ];

  if (editaveis.length === 0) {
    return (
      <Alert variant="info">
        Nenhum aluno desta turma possui diário de classe aberto. Abra o diário na ficha da matrícula
        antes de lançar notas.
      </Alert>
    );
  }

  return (
    <form className="br-form" onSubmit={lancar}>
      <div className="row mb-3">
        <div className="col-sm-7">
          <FormField label="Componente curricular" required help="Identificador do componente (catálogo BNCC).">
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={componenteCurricularId}
                onChange={(e) => setComponenteCurricularId(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
              />
            )}
          </FormField>
        </div>
        <div className="col-sm-5">
          <FormField label="Período" required help="Ex.: 1Bim, 2Bim, 1Tri.">
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                maxLength={20}
                value={periodo}
                onChange={(e) => setPeriodo(e.target.value)}
                placeholder="1Bim"
              />
            )}
          </FormField>
        </div>
      </div>

      {mutation.isSuccess && (
        <Alert variant="success">Notas lançadas: {mutation.data.lancados} diário(s) atualizado(s).</Alert>
      )}
      {mutation.isError && <Alert variant="danger">Não foi possível lançar as notas.</Alert>}

      <DataTable
        caption="Notas da turma por componente/período"
        columns={columns}
        rows={editaveis}
        rowKey={(l) => l.matriculaId}
      />

      <div className="d-flex justify-content-end mt-3">
        <Button variant="primary" type="submit" disabled={!camposValidos} loading={mutation.isPending}>
          <i className="fas fa-pen-to-square" aria-hidden="true" /> Lançar notas
        </Button>
      </div>
    </form>
  );
}

/** Tag de cobertura do diário na grade (linhas com/sem diário aberto). */
export function CoberturaDiario({ view }: { view: DiarioTurmaView }) {
  const comDiario = view.linhas.filter((l) => l.diarioId !== null).length;
  const total = view.linhas.length;
  const completo = total > 0 && comDiario === total;
  return (
    <Tag variant={completo ? 'success' : 'warning'}>
      {comDiario}/{total} com diário aberto
    </Tag>
  );
}

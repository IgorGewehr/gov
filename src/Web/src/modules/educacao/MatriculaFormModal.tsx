// Formulário de Matrícula Inicial (Censo) em Modal — agora com PICKERS REAIS:
//   - AlunoPicker: busca por nome (GET /educacao/alunos?termo=, somente ativos);
//   - TurmaPicker: turmas ABERTAS com VAGAS DISPONÍVEIS (GET /educacao/turmas).
// A escola (escolaId) é DERIVADA da turma escolhida (coerência aluno-turma-escola que
// o backend exige) — fim do GUID digitado. Mutation + Toast de sucesso/erro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useMatricularAluno } from './api';
import type { MatricularAlunoInput } from './api';
import type { AlunoItemLista } from './aluno.api';
import type { TurmaItemLista } from './turma.api';
import { AlunoPicker, TurmaPicker } from './MatriculaPickers';

export interface MatriculaFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  aluno?: string;
  turma?: string;
  dataReferencia?: string;
}

export function MatriculaFormModal({ open, onClose }: MatriculaFormModalProps) {
  const toast = useToast();
  const mutation = useMatricularAluno();

  const [aluno, setAluno] = useState<AlunoItemLista | null>(null);
  const [turma, setTurma] = useState<TurmaItemLista | null>(null);
  const [dataReferencia, setDataReferencia] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function reiniciar(): void {
    setAluno(null);
    setTurma(null);
    setDataReferencia('');
    setErrors({});
  }

  function fechar(): void {
    reiniciar();
    onClose();
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (!aluno) next.aluno = 'Selecione o aluno.';
    if (!turma) next.turma = 'Selecione a turma.';
    if (dataReferencia.trim() === '') next.dataReferencia = 'Informe a data de referência do Censo.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0 || !aluno || !turma) return;

    const input: MatricularAlunoInput = {
      alunoId: aluno.id,
      turmaId: turma.id,
      escolaId: turma.escolaId, // derivada da turma (coerência exigida pelo backend)
      dataReferencia,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Matrícula Inicial registrada com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a matrícula.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar Matrícula Inicial"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-matricular-aluno" loading={mutation.isPending}>
            Matricular
          </Button>
        </>
      }
    >
      <form id="form-matricular-aluno" className="br-form" onSubmit={submeter} noValidate>
        <AlunoPicker selecionado={aluno} onSelecionar={setAluno} error={errors.aluno} />

        <TurmaPicker selecionada={turma} onSelecionar={setTurma} error={errors.turma} />

        <FormField label="Data de referência (Censo)" required error={errors.dataReferencia}>
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
      </form>
    </Modal>
  );
}

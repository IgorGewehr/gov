// Formulário de Matrícula Inicial (Censo) em Modal. Padrão de mutation + validação
// por campo (FormField/aria-describedby) + Toast de sucesso/erro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useMatricularAluno } from './api';
import type { MatricularAlunoInput } from './api';

export interface MatriculaFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pré-preenche o aluno quando aberto a partir de uma consulta. */
  alunoIdInicial?: string;
}

interface FormErrors {
  alunoId?: string;
  turmaId?: string;
  escolaId?: string;
  dataReferencia?: string;
}

const CAMPOS_VALIDOS: Record<keyof FormErrors, true> = {
  alunoId: true,
  turmaId: true,
  escolaId: true,
  dataReferencia: true,
};

export function MatriculaFormModal({ open, onClose, alunoIdInicial = '' }: MatriculaFormModalProps) {
  const toast = useToast();
  const mutation = useMatricularAluno();

  const [alunoId, setAlunoId] = useState(alunoIdInicial);
  const [turmaId, setTurmaId] = useState('');
  const [escolaId, setEscolaId] = useState('');
  const [dataReferencia, setDataReferencia] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (alunoId.trim() === '') next.alunoId = 'Informe o identificador do aluno.';
    if (turmaId.trim() === '') next.turmaId = 'Informe a turma de enturmação.';
    if (escolaId.trim() === '') next.escolaId = 'Informe a escola da matrícula.';
    if (dataReferencia.trim() === '') next.dataReferencia = 'Informe a data de referência do Censo.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: MatricularAlunoInput = {
      alunoId: alunoId.trim(),
      turmaId: turmaId.trim(),
      escolaId: escolaId.trim(),
      dataReferencia: dataReferencia,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Matrícula Inicial registrada com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS_VALIDOS) {
              (mapped as Record<string, string>)[key] = messages[0];
            }
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a matrícula.');
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
        <FormField label="Identificador do aluno" required error={errors.alunoId}>
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

        <FormField label="Identificador da turma" required error={errors.turmaId}>
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

        <FormField label="Identificador da escola" required error={errors.escolaId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={escolaId}
              onChange={(e) => setEscolaId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

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

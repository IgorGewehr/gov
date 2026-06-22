// Formulário de geração de Remessa (prestação de contas) ao TCE-RS em Modal.
// Contrato M4: exercício + tipo de período + número do período. Padrão de
// mutation + validação por campo (FormField/aria-describedby) + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useGerarRemessa } from './api';
import type { GerarRemessaInput, TipoPeriodo } from './api';
import { tipoPeriodoOptions } from './transparencia.helpers';

export interface RemessaFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pré-preenche o exercício a partir do filtro corrente. */
  exercicioInicial: number;
}

interface FormErrors {
  exercicio?: string;
  numeroPeriodo?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = { exercicio: true, numeroPeriodo: true };

export function RemessaFormModal({ open, onClose, exercicioInicial }: RemessaFormModalProps) {
  const toast = useToast();
  const mutation = useGerarRemessa();

  const [exercicio, setExercicio] = useState(String(exercicioInicial));
  const [tipoPeriodo, setTipoPeriodo] = useState<TipoPeriodo>('Mensal');
  const [numeroPeriodo, setNumeroPeriodo] = useState('1');
  const [errors, setErrors] = useState<FormErrors>({});

  const periodoIgnorado = tipoPeriodo === 'Anual';

  function validar(): FormErrors {
    const next: FormErrors = {};
    const ano = Number(exercicio);
    if (exercicio.trim() === '' || !Number.isInteger(ano) || ano < 1900)
      next.exercicio = 'Informe um exercício válido (>= 1900).';
    if (!periodoIgnorado) {
      const numero = Number(numeroPeriodo);
      if (numeroPeriodo.trim() === '' || !Number.isInteger(numero) || numero < 1)
        next.numeroPeriodo = 'Informe o número do período (>= 1).';
    }
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

    const input: GerarRemessaInput = {
      exercicio: Number(exercicio),
      tipoPeriodo,
      numeroPeriodo: periodoIgnorado ? 0 : Number(numeroPeriodo),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Remessa gerada e pronta para validação.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível gerar a remessa.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Gerar remessa ao TCE-RS"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-gerar-remessa" loading={mutation.isPending}>
            Gerar
          </Button>
        </>
      }
    >
      <form id="form-gerar-remessa" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Exercício" required error={errors.exercicio}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1900"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={exercicio}
              onChange={(e) => setExercicio(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Tipo de período" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={tipoPeriodoOptions}
              value={tipoPeriodo}
              onChange={(e) => setTipoPeriodo(e.target.value as TipoPeriodo)}
            />
          )}
        </FormField>

        <FormField
          label="Número do período"
          required={!periodoIgnorado}
          help={periodoIgnorado ? 'Não se aplica ao período anual.' : 'Competência dentro do exercício.'}
          error={errors.numeroPeriodo}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              disabled={periodoIgnorado}
              value={periodoIgnorado ? '' : numeroPeriodo}
              onChange={(e) => setNumeroPeriodo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

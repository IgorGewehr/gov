// Formulário de abertura de Diário de Classe em Modal. Vincula-se 1-1 a uma matrícula
// ativa. Padrão de mutation + validação por campo + Toast de sucesso/erro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAbrirDiarioClasse } from './api';
import type { AbrirDiarioClasseInput } from './api';

export interface DiarioClasseFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Matrícula à qual o diário será vinculado. */
  matriculaId: string;
}

interface FormErrors {
  cargaHorariaTotal?: string;
}

export function DiarioClasseFormModal({ open, onClose, matriculaId }: DiarioClasseFormModalProps) {
  const toast = useToast();
  const mutation = useAbrirDiarioClasse();

  const [cargaHorariaTotal, setCargaHorariaTotal] = useState('800');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    const carga = Number(cargaHorariaTotal);
    if (cargaHorariaTotal.trim() === '' || !Number.isInteger(carga) || carga <= 0)
      next.cargaHorariaTotal = 'Informe a carga horária total (maior que zero).';
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

    const input: AbrirDiarioClasseInput = {
      matriculaId,
      cargaHorariaTotal: Number(cargaHorariaTotal),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Diário de classe aberto com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && error.fieldErrors.CargaHorariaTotal) {
          setErrors({ cargaHorariaTotal: error.fieldErrors.CargaHorariaTotal[0] });
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível abrir o diário.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir Diário de Classe"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-diario" loading={mutation.isPending}>
            Abrir diário
          </Button>
        </>
      }
    >
      <form id="form-abrir-diario" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Carga horária total (h)"
          required
          error={errors.cargaHorariaTotal}
          help="Carga horária anual de referência (ex.: 800h ou 1.000h)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1"
              step="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={cargaHorariaTotal}
              onChange={(e) => setCargaHorariaTotal(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

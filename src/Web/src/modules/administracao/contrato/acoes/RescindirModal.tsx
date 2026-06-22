// RescindirContrato (destrutivo — exige motivação)
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useRescindirContrato } from '../contrato.api';
import { mapearFieldErrors } from './acaoModals.shared';
import type { AcaoModalProps } from './acaoModals.shared';

export function RescindirModal({ contratoId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useRescindirContrato(contratoId);
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('A motivação da rescisão é obrigatória (ato administrativo).');
      return;
    }
    mutation.mutate(
      { motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success('Contrato rescindido.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErro(mapearFieldErrors(error, { motivo: true }).motivo);
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível rescindir o contrato.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Rescindir contrato"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-rescindir" loading={mutation.isPending}>
            Confirmar rescisão
          </Button>
        </>
      }
    >
      <form id="form-rescindir" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning">
          A rescisão é uma extinção antecipada e <strong>irreversível</strong> do contrato. Descreva a motivação.
        </Alert>
        <FormField label="Motivação da rescisão" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// PublicarContratoNoPncp
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { usePublicarNoPncp } from '../contrato.api';
import { mapearFieldErrors } from './acaoModals.shared';
import type { AcaoModalProps } from './acaoModals.shared';

export function PublicarNoPncpModal({ contratoId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = usePublicarNoPncp(contratoId);
  const [numero, setNumero] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (numero.trim() === '') {
      setErro('Informe o número do contrato no PNCP.');
      return;
    }
    if (numero.trim().length > 60) {
      setErro('Número do PNCP deve ter no máximo 60 caracteres.');
      return;
    }
    mutation.mutate(
      { numeroContratoPncp: numero.trim() },
      {
        onSuccess: () => {
          toast.success('Contrato publicado no PNCP (condição de eficácia — art. 174).', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          const mapped = mapearFieldErrors(error, { numeroContratoPncp: true });
          setErro(mapped.numeroContratoPncp);
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível publicar no PNCP.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar no PNCP"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-publicar-pncp" loading={mutation.isPending}>
            Publicar
          </Button>
        </>
      }
    >
      <form id="form-publicar-pncp" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-60">
          A publicação no PNCP é condição de eficácia do contrato (Lei 14.133/2021, art. 174).
        </p>
        <FormField label="Número do contrato no PNCP" required error={erro} help="Máx. 60 caracteres.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={60}
              value={numero}
              onChange={(e) => setNumero(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// Modal para REGISTRAR o protocolo da transmissão ao TCE-RS. ATENÇÃO: a transmissão
// é feita FORA do sistema (PAD / e-Protocolo); aqui apenas se registra o número de
// protocolo retornado por aquele canal. Ato humano, gated em backend e UI.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useRegistrarProtocolo } from './api';

export interface RegistrarProtocoloModalProps {
  id: string;
  open: boolean;
  onClose: () => void;
}

export function RegistrarProtocoloModal({ id, open, onClose }: RegistrarProtocoloModalProps) {
  const toast = useToast();
  const mutation = useRegistrarProtocolo(id);
  const [protocolo, setProtocolo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setProtocolo('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (protocolo.trim() === '') {
      setErro('Informe o número de protocolo emitido pelo PAD/e-Protocolo.');
      return;
    }
    mutation.mutate(protocolo.trim(), {
      onSuccess: () => {
        toast.success('Protocolo registrado. Remessa marcada como Enviada.', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o protocolo.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar protocolo da transmissão"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-protocolo" loading={mutation.isPending}>
            Registrar protocolo
          </Button>
        </>
      }
    >
      <Alert variant="info" title="A transmissão é feita fora do sistema">
        Envie o pacote ZIP ao TCE-RS pelo PAD / e-Protocolo. Depois, informe aqui o número de
        protocolo retornado por aquele canal — este sistema apenas o registra, não transmite.
      </Alert>
      <form id="form-protocolo" className="br-form mt-3" onSubmit={submeter} noValidate>
        <FormField label="Número de protocolo" required error={erro}>
          {({ id: fieldId, describedBy, invalid }) => (
            <Input
              id={fieldId}
              aria-describedby={describedBy}
              invalid={invalid}
              value={protocolo}
              onChange={(e) => setProtocolo(e.target.value)}
              placeholder="Ex.: PAD-2026-000123"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

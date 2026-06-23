// [Command CancelarInventario] Cancela o inventário (terminal) sem efeito patrimonial
// (em Modal). Validação: motivo obrigatório (máx. 500). Ação destrutiva.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useCancelarInventario } from './inventario.api';
import type { CancelarInventarioInput } from './inventario.api';

export interface InventarioCancelarModalProps {
  open: boolean;
  onClose: () => void;
  inventarioId: string;
}

export function InventarioCancelarModal({
  open,
  onClose,
  inventarioId,
}: InventarioCancelarModalProps) {
  const toast = useToast();
  const mutation = useCancelarInventario(inventarioId);

  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setErro(null);
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('Informe o motivo do cancelamento.');
      return;
    }
    if (motivo.trim().length > 500) {
      setErro('Motivo deve ter no máximo 500 caracteres.');
      return;
    }
    setErro(null);

    const input: CancelarInventarioInput = { motivo: motivo.trim() };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Inventário cancelado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível cancelar o inventário.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cancelar inventário"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="danger" type="submit" form="form-cancelar-inventario" loading={mutation.isPending}>
            Cancelar inventário
          </Button>
        </>
      }
    >
      <form id="form-cancelar-inventario" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="danger" title="Ação definitiva">
          O cancelamento é terminal e não produz efeito patrimonial. Esta operação não pode ser desfeita.
        </Alert>

        <FormField label="Motivo do cancelamento" required error={erro ?? undefined}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={500}
              rows={4}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
              placeholder="Descreva o motivo do cancelamento."
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

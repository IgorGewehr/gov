// [Command EncerrarInventario] Encerra o inventário (exige conciliação prévia) e publica
// as recomendações via Outbox (em Modal). Validação: data de encerramento obrigatória.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useEncerrarInventario } from './inventario.api';
import type { EncerrarInventarioInput } from './inventario.api';
import { hojeIso } from './inventario.helpers';

export interface InventarioEncerrarModalProps {
  open: boolean;
  onClose: () => void;
  inventarioId: string;
  /** Quantidade de divergências apuradas, exibida como contexto da efetivação. */
  totalDivergencias: number;
}

export function InventarioEncerrarModal({
  open,
  onClose,
  inventarioId,
  totalDivergencias,
}: InventarioEncerrarModalProps) {
  const toast = useToast();
  const mutation = useEncerrarInventario(inventarioId);

  const [dataEncerramento, setDataEncerramento] = useState(hojeIso());
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setErro(null);
    setDataEncerramento(hojeIso());
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (dataEncerramento.trim() === '') {
      setErro('Informe a data de encerramento.');
      return;
    }
    setErro(null);

    const input: EncerrarInventarioInput = { dataEncerramento };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Inventário encerrado e recomendações publicadas.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar o inventário.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Encerrar inventário"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-encerrar-inventario" loading={mutation.isPending}>
            Encerrar inventário
          </Button>
        </>
      }
    >
      <form id="form-encerrar-inventario" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Ação definitiva">
          O encerramento exige conciliação prévia e publica as recomendações de movimentação/baixa.
          {totalDivergencias > 0
            ? ` Há ${totalDivergencias} divergência${totalDivergencias === 1 ? '' : 's'} apurada${totalDivergencias === 1 ? '' : 's'} a efetivar.`
            : ' Nenhuma divergência foi apurada.'}
        </Alert>

        <FormField label="Data de encerramento" required error={erro ?? undefined}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataEncerramento}
              onChange={(e) => setDataEncerramento(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

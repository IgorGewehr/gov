// [Command CancelarPedido] Cancela a requisição (Solicitado/Aprovado -> Cancelado),
// terminal e sem efeito de estoque (em Modal). Validação: motivo obrigatório (máx. 500).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useCancelarPedido } from './requisicao.api';

export interface RequisicaoCancelarModalProps {
  open: boolean;
  onClose: () => void;
  pedidoId: string;
}

export function RequisicaoCancelarModal({ open, onClose, pedidoId }: RequisicaoCancelarModalProps) {
  const toast = useToast();
  const mutation = useCancelarPedido(pedidoId);

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

    mutation.mutate(
      { motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success('Requisição cancelada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível cancelar a requisição.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cancelar requisição"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="danger" type="submit" form="form-cancelar-requisicao" loading={mutation.isPending}>
            Cancelar requisição
          </Button>
        </>
      }
    >
      <form id="form-cancelar-requisicao" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="danger" title="Ação definitiva">
          O cancelamento é terminal e não produz baixa de estoque. Esta operação não pode ser desfeita.
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

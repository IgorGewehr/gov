// [Command AtenderPedido] Atende a requisição APROVADA (Aprovado -> Atendido) em Modal.
// O backend baixa, por item, o mínimo entre o pendente e o saldo disponível (atendimento
// PARCIAL permitido) — esta tela informa a regra e só coleta a data do atendimento.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAtenderPedido } from './requisicao.api';
import { hojeIso } from './requisicao.helpers';

export interface RequisicaoAtenderModalProps {
  open: boolean;
  onClose: () => void;
  pedidoId: string;
}

export function RequisicaoAtenderModal({ open, onClose, pedidoId }: RequisicaoAtenderModalProps) {
  const toast = useToast();
  const mutation = useAtenderPedido(pedidoId);

  const [data, setData] = useState(hojeIso());
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (data.trim() === '') {
      setErro('Informe a data do atendimento.');
      return;
    }
    setErro(null);

    mutation.mutate(
      { data },
      {
        onSuccess: () => {
          toast.success('Requisição atendida e estoque baixado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível atender a requisição.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Atender requisição"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-atender-requisicao" loading={mutation.isPending}>
            Atender
          </Button>
        </>
      }
    >
      <form id="form-atender-requisicao" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Atendimento parcial permitido">
          Por item, será baixado o menor valor entre a quantidade pendente e o saldo disponível em
          estoque. Itens sem saldo permanecem pendentes; o pedido é concluído como Atendido.
        </Alert>

        <FormField label="Data do atendimento" required error={erro ?? undefined}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={data}
              onChange={(e) => setData(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

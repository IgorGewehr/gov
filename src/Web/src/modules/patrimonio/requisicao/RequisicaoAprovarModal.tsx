// [Command AprovarPedido] Aprova a requisição (Solicitado -> Aprovado) em Modal.
// Autoridade aprovadora (pré-preenchida com o usuário atual, editável) + data.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAuth } from '../../../auth/useAuth';
import { useAprovarPedido } from './requisicao.api';
import { hojeIso } from './requisicao.helpers';

export interface RequisicaoAprovarModalProps {
  open: boolean;
  onClose: () => void;
  pedidoId: string;
}

export function RequisicaoAprovarModal({ open, onClose, pedidoId }: RequisicaoAprovarModalProps) {
  const toast = useToast();
  const { user } = useAuth();
  const mutation = useAprovarPedido(pedidoId);

  const [aprovadorId, setAprovadorId] = useState(user?.id ?? '');
  const [data, setData] = useState(hojeIso());
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (aprovadorId.trim() === '') {
      setErro('Informe a autoridade aprovadora.');
      return;
    }
    if (data.trim() === '') {
      setErro('Informe a data da aprovação.');
      return;
    }
    setErro(null);

    mutation.mutate(
      { aprovadorId: aprovadorId.trim(), data },
      {
        onSuccess: () => {
          toast.success('Requisição aprovada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível aprovar a requisição.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Aprovar requisição"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-aprovar-requisicao" loading={mutation.isPending}>
            Aprovar
          </Button>
        </>
      }
    >
      <form id="form-aprovar-requisicao" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Autoridade aprovadora (identificador)"
          required
          error={erro && aprovadorId.trim() === '' ? erro : undefined}
          help="Pré-preenchido com o usuário atual; ajuste se aprovar em nome de outra autoridade."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={aprovadorId}
              onChange={(e) => setAprovadorId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Data da aprovação" required error={erro && data.trim() === '' ? erro : undefined}>
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

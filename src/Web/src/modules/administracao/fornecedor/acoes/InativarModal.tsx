// InativarFornecedor -> InativarFornecedorCommand (confirmacao destrutiva).
// Estado terminal administrativo — exige checkbox de confirmacao.
import { useState } from 'react';
import { Alert, Button, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useInativarFornecedor } from '../fornecedor.api';
import type { AcaoModalProps } from './acoesModais.shared';

export function InativarModal({ open, onClose, fornecedorId }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useInativarFornecedor(fornecedorId);
  const [confirmado, setConfirmado] = useState(false);

  function fechar(): void {
    setConfirmado(false);
    onClose();
  }

  function confirmar(): void {
    if (!confirmado) return;
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Fornecedor inativado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Nao foi possivel inativar o fornecedor.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Inativar fornecedor"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" onClick={confirmar} loading={mutation.isPending} disabled={!confirmado}>
            Inativar
          </Button>
        </>
      }
    >
      <Alert variant="warning" title="Acao irreversivel">
        Inativar torna o cadastro do fornecedor inativo (estado terminal administrativo). O fornecedor deixa de
        participar de certames.
      </Alert>
      <div className="br-checkbox mt-3">
        <input
          id="confirmar-inativacao"
          type="checkbox"
          checked={confirmado}
          onChange={(e) => setConfirmado(e.target.checked)}
        />
        <label htmlFor="confirmar-inativacao">Confirmo que desejo inativar este fornecedor.</label>
      </div>
    </Modal>
  );
}

// ReabilitarFornecedor -> ReabilitarFornecedorCommand (confirmacao).
// So permitido quando nao ha sancao impeditiva vigente (art. 156).
import { Alert, Button, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useReabilitarFornecedor } from '../fornecedor.api';
import type { AcaoModalProps } from './acoesModais.shared';

export function ReabilitarModal({ open, onClose, fornecedorId }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useReabilitarFornecedor(fornecedorId);

  function confirmar(): void {
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Fornecedor reabilitado. Situacao restabelecida para Ativo.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Nao foi possivel reabilitar o fornecedor.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Reabilitar fornecedor"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" onClick={confirmar} loading={mutation.isPending}>
            Reabilitar
          </Button>
        </>
      }
    >
      <Alert variant="info" title="Confirmacao">
        A reabilitacao so e permitida quando nao ha sancao impeditiva vigente (todas cumpridas/encerradas).
        A situacao do fornecedor sera restabelecida para Ativo (art. 156).
      </Alert>
    </Modal>
  );
}

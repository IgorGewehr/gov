// DeclararDeserta (Aberta -> Deserta) — confirmação (I-11).
import { Alert, Button, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useDeclararDeserta } from '../licitacao.api';
import type { AcaoModalProps } from './licitacaoModais.shared';

export function DeclararDesertaModal({ open, onClose, licitacaoId, temPropostas }: AcaoModalProps & { temPropostas: boolean }) {
  const toast = useToast();
  const mutation = useDeclararDeserta(licitacaoId);

  function confirmar(): void {
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Licitação declarada deserta.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível declarar deserta.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Declarar deserta"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" onClick={confirmar} loading={mutation.isPending} disabled={temPropostas}>
            Confirmar deserção
          </Button>
        </>
      }
    >
      <p>Encerra o certame por ausência total de interessados. Esta ação é irreversível.</p>
      {temPropostas && (
        <Alert variant="warning">
          Há propostas recebidas: não é possível declarar deserta (use Declarar fracassada quando as propostas forem
          inválidas) (I-11).
        </Alert>
      )}
    </Modal>
  );
}

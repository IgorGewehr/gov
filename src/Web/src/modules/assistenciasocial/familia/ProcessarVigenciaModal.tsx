// Modal de confirmacao do command ProcessarVigenciaCadastral: avalia a vigencia do
// cadastro (24 meses) e, quando vencida, sinaliza a familia como "Atualização vencida".
// Operacao com efeito de estado -> confirmacao explicita (DS / WCAG AA).
import { Alert, Button, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useProcessarVigenciaCadastral } from './familia.api';

export interface ProcessarVigenciaModalProps {
  open: boolean;
  onClose: () => void;
  familiaId: string;
  /** NIS mascarado para identificar a familia na confirmacao. */
  nisMascarado?: string;
}

export function ProcessarVigenciaModal({ open, onClose, familiaId, nisMascarado }: ProcessarVigenciaModalProps) {
  const toast = useToast();
  const mutation = useProcessarVigenciaCadastral();

  function confirmar(): void {
    mutation.mutate(familiaId, {
      onSuccess: () => {
        toast.success('Vigência cadastral processada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível processar a vigência cadastral.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Processar vigência cadastral"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" onClick={confirmar} loading={mutation.isPending}>
            Processar
          </Button>
        </>
      }
    >
      <Alert variant="warning">
        Esta ação avalia a validade do cadastro (24 meses). Se a última atualização do CadÚnico estiver
        vencida, a família será sinalizada como <strong>Atualização vencida</strong> e a elegibilidade a
        novos benefícios ficará condicionada à regularização.
      </Alert>
      {nisMascarado && (
        <p className="mb-0">
          Família (NIS): <strong>{nisMascarado}</strong>
        </p>
      )}
    </Modal>
  );
}

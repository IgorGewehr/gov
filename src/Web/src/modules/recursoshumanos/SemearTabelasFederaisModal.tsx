// Modal de confirmação da SEMEADURA das tabelas legais federais (INSS/IRRF oficiais).
// Os números são dado parametrizado oficial (não hardcoded no motor). Ação idempotente
// no escopo do tenant; registrada na trilha de auditoria. Sem entrada de dados.
import { Alert, Button, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useSemearTabelasFederais } from './tabelaLegal.api';

export interface SemearTabelasFederaisModalProps {
  open: boolean;
  onClose: () => void;
}

export function SemearTabelasFederaisModal({ open, onClose }: SemearTabelasFederaisModalProps) {
  const toast = useToast();
  const mutation = useSemearTabelasFederais();

  function semear(): void {
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Tabelas federais (INSS/IRRF) semeadas.', 'Sucesso');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError
            ? error.userMessage
            : 'Não foi possível semear as tabelas federais.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Semear tabelas federais"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" onClick={semear} loading={mutation.isPending}>
            Semear
          </Button>
        </>
      }
    >
      <Alert variant="info" title="Tabelas oficiais INSS e IRRF.">
        Esta ação cadastra, para o seu ente, as tabelas progressivas federais oficiais de
        INSS e IRRF por competência. Os valores seguem as portarias e tabelas vigentes da
        Receita Federal/Previdência. As tabelas RPPS NÃO são semeadas — dependem de lei
        municipal (cadastre-as separadamente).
      </Alert>
      <p className="text-down-01 text-gray-60 mb-0">
        A operação é registrada na trilha de auditoria.
      </p>
    </Modal>
  );
}

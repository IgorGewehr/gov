// [Command InativarItem §5.6] Inativa o item — operação destrutiva e TERMINAL para movimentação,
// só permitida com saldo == 0 (I-9/B-7). Modal de confirmação: quando saldo > 0, bloqueia o envio
// e explica; quando saldo == 0, confirma com botão danger.
import { Alert, Button, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useInativarItem } from './itemestoque.api';

export interface ItemEstoqueInativarModalProps {
  open: boolean;
  onClose: () => void;
  itemId: string;
  codigoItem: string;
  saldoAtual: number;
  unidadeMedida: string;
}

export function ItemEstoqueInativarModal({
  open,
  onClose,
  itemId,
  codigoItem,
  saldoAtual,
  unidadeMedida,
}: ItemEstoqueInativarModalProps) {
  const toast = useToast();
  const mutation = useInativarItem(itemId);

  const saldoZero = saldoAtual === 0;

  function confirmar(): void {
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success(`Item ${codigoItem} inativado.`, 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível inativar o item.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Inativar item de almoxarifado"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="danger"
            onClick={confirmar}
            loading={mutation.isPending}
            disabled={!saldoZero}
          >
            Inativar item
          </Button>
        </>
      }
    >
      {saldoZero ? (
        <Alert variant="warning" title="Confirmação">
          Esta ação inativa o item <strong>{codigoItem}</strong> e impede novas entradas e saídas. Deseja continuar?
        </Alert>
      ) : (
        <Alert variant="danger" title="Inativação bloqueada">
          O item possui saldo remanescente de <strong>{saldoAtual}</strong> {unidadeMedida}. Só é possível inativar
          itens com saldo zero (B-7). Atenda às requisições pendentes antes de inativar.
        </Alert>
      )}
    </Modal>
  );
}

// Modal generico de confirmacao para comandos sem corpo (transicoes de estado:
// distribuir/arquivar/abrir/suspender/encerrar/cancelar etc.). Encapsula o ciclo
// mutation + Toast + fechamento, evitando duplicacao nos modais de acao.
import { Button, Modal, useToast } from '../../components/ui';
import type { UseMutationResult } from '@tanstack/react-query';
import { mensagemErro } from './legislativoAcao.shared';

export interface ConfirmarAcaoModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  /** Texto explicativo da consequencia da acao. */
  mensagem: string;
  /** Rotulo do botao de confirmacao (ex.: "Distribuir", "Arquivar"). */
  rotuloConfirmar: string;
  /** Variante do botao de confirmacao. */
  variante?: 'primary' | 'danger';
  /** Mutation sem argumento (mutate()) a ser disparada na confirmacao. */
  mutation: UseMutationResult<unknown, unknown, void, unknown>;
  /** Mensagem de sucesso exibida no Toast. */
  sucesso: string;
  /** Mensagem de erro padrao quando a API nao fornece detalhe. */
  erroFallback: string;
}

export function ConfirmarAcaoModal({
  open,
  onClose,
  title,
  mensagem,
  rotuloConfirmar,
  variante = 'primary',
  mutation,
  sucesso,
  erroFallback,
}: ConfirmarAcaoModalProps) {
  const toast = useToast();

  function confirmar(): void {
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success(sucesso, 'Sucesso');
        onClose();
      },
      onError: (error) => toast.error(mensagemErro(error, erroFallback)),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant={variante} onClick={confirmar} loading={mutation.isPending}>
            {rotuloConfirmar}
          </Button>
        </>
      }
    >
      <p className="mb-0">{mensagem}</p>
    </Modal>
  );
}

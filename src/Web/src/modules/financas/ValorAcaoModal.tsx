// Modal genérico de ação que recebe um VALOR (R$) e dispara uma mutation. Reutilizado
// por: reforçar dotação, anular crédito, pagar/cancelar resto a pagar, anular empenho.
// Validação por campo + Toast. Mantém os modais de ação por agregado < 300 linhas.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { mensagemErro } from './financas.helpers';

export interface ValorAcaoModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  /** Rótulo do campo de valor. */
  label: string;
  /** Texto auxiliar opcional (ex.: explicação de anulação total). */
  help?: string;
  /** Rótulo do botão de confirmação. */
  confirmLabel: string;
  /** Variante do botão de confirmação. */
  confirmVariant?: 'primary' | 'danger' | 'secondary';
  /** Permite valor vazio (=> envia `undefined`, ex.: anulação total de empenho). */
  permiteVazio?: boolean;
  /** true enquanto a mutation está pendente. */
  pending: boolean;
  /** Executa a ação com o valor informado (undefined quando permiteVazio e vazio). */
  onConfirm: (valor: number | undefined) => Promise<unknown> | void;
  mensagemSucesso: string;
  mensagemErroPadrao: string;
  /** Notifica a página para fechar/atualizar após sucesso. */
  onSucesso?: () => void;
}

export function ValorAcaoModal({
  open,
  onClose,
  title,
  label,
  help,
  confirmLabel,
  confirmVariant = 'primary',
  permiteVazio = false,
  pending,
  onConfirm,
  mensagemSucesso,
  mensagemErroPadrao,
  onSucesso,
}: ValorAcaoModalProps) {
  const toast = useToast();
  const [valor, setValor] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setValor('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const vazio = valor.trim() === '';
    if (vazio && !permiteVazio) {
      setErro('Informe um valor maior que zero.');
      return;
    }
    const numero = vazio ? undefined : Number(valor);
    if (numero !== undefined && (Number.isNaN(numero) || numero <= 0)) {
      setErro('Informe um valor maior que zero.');
      return;
    }
    setErro(undefined);

    Promise.resolve(onConfirm(numero))
      .then(() => {
        toast.success(mensagemSucesso, 'Sucesso');
        fechar();
        onSucesso?.();
      })
      .catch((error: unknown) => {
        toast.error(mensagemErro(error, mensagemErroPadrao));
      });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={title}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={pending}>
            Cancelar
          </Button>
          <Button variant={confirmVariant} type="submit" form="form-valor-acao" loading={pending}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <form id="form-valor-acao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label={label} required={!permiteVazio} error={erro} help={help}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valor}
              onChange={(e) => setValor(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

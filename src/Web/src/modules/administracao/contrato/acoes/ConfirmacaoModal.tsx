// Confirmação simples (IniciarExecucao / EncerrarContrato — sem campos)
import type { ReactNode } from 'react';
import { Button, Modal } from '../../../../components/ui';

export interface ConfirmacaoModalProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  confirmLabel: string;
  loading: boolean;
  destrutivo?: boolean;
  children: ReactNode;
}

export function ConfirmacaoModal({
  open,
  onClose,
  onConfirm,
  title,
  confirmLabel,
  loading,
  destrutivo = false,
  children,
}: ConfirmacaoModalProps) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={loading}>
            Cancelar
          </Button>
          <Button variant={destrutivo ? 'danger' : 'primary'} onClick={onConfirm} loading={loading}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      {children}
    </Modal>
  );
}

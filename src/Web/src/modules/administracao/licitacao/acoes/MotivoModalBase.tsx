// Base compartilhada dos comandos com MOTIVO obrigatório (DeclararFracassada /
// Revogar / Anular). Renderiza o Modal + Textarea de motivação e centraliza a
// validação/Toast; cada command apenas injeta labels e a mutation via onConfirm.
import { useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Button, FormField, Modal, Textarea, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { primeiraMensagem } from './licitacaoModais.shared';
import type { AcaoModalProps } from './licitacaoModais.shared';

export interface MotivoCallbacks {
  onSuccess: () => void;
  onError: (error: unknown) => void;
}

export interface MotivoModalBaseProps extends AcaoModalProps {
  title: string;
  intro: ReactNode;
  confirmarLabel: string;
  formId: string;
  sucesso: string;
  falha: string;
  isPending: boolean;
  onConfirm: (motivo: string, callbacks: MotivoCallbacks) => void;
}

export function MotivoModalBase({
  open,
  onClose,
  title,
  intro,
  confirmarLabel,
  formId,
  sucesso,
  falha,
  isPending,
  onConfirm,
}: MotivoModalBaseProps) {
  const toast = useToast();
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setMotivo('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('A motivação do ato administrativo é obrigatória.');
      return;
    }
    setErro(undefined);
    onConfirm(motivo.trim(), {
      onSuccess: () => {
        toast.success(sucesso, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError) setErro(primeiraMensagem(error, 'motivo'));
        toast.error(error instanceof ApiError ? error.userMessage : falha);
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={title}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form={formId} loading={isPending}>
            {confirmarLabel}
          </Button>
        </>
      }
    >
      <form id={formId} className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">{intro}</p>
        <FormField label="Motivo" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

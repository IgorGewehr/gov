// Modal genérico de confirmação com MOTIVO (texto obrigatório) — reutilizado pelas ações
// da VISA que pedem justificativa: interdição, cancelamento de inspeção e cassação de
// licença. Mantém o padrão mutation + Toast + validação por campo.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';

interface VisaMotivoModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  label: string;
  acaoLabel: string;
  sucessoMensagem: string;
  pendente: boolean;
  executar: (motivo: string) => Promise<unknown>;
}

export function VisaMotivoModal({
  open,
  onClose,
  title,
  label,
  acaoLabel,
  sucessoMensagem,
  pendente,
  executar,
}: VisaMotivoModalProps) {
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
    if (motivo.trim().length === 0) {
      setErro('Informe o motivo.');
      return;
    }
    setErro(undefined);
    void executar(motivo.trim())
      .then(() => {
        toast.success(sucessoMensagem, 'Sucesso');
        fechar();
      })
      .catch((error: unknown) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível concluir a ação.');
      });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={title}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={pendente}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-visa-motivo" loading={pendente}>
            {acaoLabel}
          </Button>
        </>
      }
    >
      <form id="form-visa-motivo" className="br-form" onSubmit={submeter} noValidate>
        <FormField label={label} required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              rows={4}
              maxLength={1000}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

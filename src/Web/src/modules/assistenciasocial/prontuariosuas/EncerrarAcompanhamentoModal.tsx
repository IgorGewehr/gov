// Modal de acao: EncerrarAcompanhamento -> EncerrarAcompanhamentoCommand.
// Transicao terminal DESTRUTIVA (Aberto -> Encerrado): exige confirmacao explicita.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useEncerrarAcompanhamento } from './prontuariosuas.api';
import type { EncerrarAcompanhamentoInput } from './prontuariosuas.api';
import { mapearErros } from './acaoModalShared';
import type { ModalAcaoBaseProps } from './acaoModalShared';

interface EncerramentoErrors {
  motivoEncerramento?: string;
}

export function EncerrarAcompanhamentoModal({ open, onClose, prontuarioId }: ModalAcaoBaseProps) {
  const toast = useToast();
  const mutation = useEncerrarAcompanhamento(prontuarioId);

  const [motivoEncerramento, setMotivoEncerramento] = useState('');
  const [confirmado, setConfirmado] = useState(false);
  const [errors, setErrors] = useState<EncerramentoErrors>({});

  function validar(): EncerramentoErrors {
    const next: EncerramentoErrors = {};
    if (motivoEncerramento.trim() === '') next.motivoEncerramento = 'Motivo do encerramento é obrigatório.';
    else if (motivoEncerramento.length > 400)
      next.motivoEncerramento = 'O motivo deve ter no máximo 400 caracteres.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setMotivoEncerramento('');
    setConfirmado(false);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0 || !confirmado) return;

    const input: EncerrarAcompanhamentoInput = { motivoEncerramento: motivoEncerramento.trim() };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Acompanhamento encerrado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(mapearErros(error, { motivoEncerramento: 1 }));
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar o acompanhamento.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Encerrar acompanhamento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="danger"
            type="submit"
            form="form-encerrar-acompanhamento"
            loading={mutation.isPending}
            disabled={!confirmado}
          >
            Encerrar definitivamente
          </Button>
        </>
      }
    >
      <div className="mb-3">
        <Alert variant="warning">
          Esta ação é <strong>irreversível</strong>. Um prontuário encerrado não admite novos
          atendimentos, planos, violações nem novo encerramento (estado terminal).
        </Alert>
      </div>
      <form id="form-encerrar-acompanhamento" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Motivo do encerramento"
          required
          error={errors.motivoEncerramento}
          help="Máximo de 400 caracteres. Ex.: objetivos alcançados."
        >
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              rows={3}
              maxLength={400}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              value={motivoEncerramento}
              onChange={(e) => setMotivoEncerramento(e.target.value)}
            />
          )}
        </FormField>

        <div className="br-checkbox">
          <input
            id="confirmar-encerramento"
            type="checkbox"
            checked={confirmado}
            onChange={(e) => setConfirmado(e.target.checked)}
          />
          <label htmlFor="confirmar-encerramento">
            Confirmo que desejo encerrar este acompanhamento de forma definitiva.
          </label>
        </div>
      </form>
    </Modal>
  );
}

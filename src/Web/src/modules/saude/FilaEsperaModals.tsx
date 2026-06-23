// Modais de ação da Fila de Espera: convocar (Aguardando → Convocado) e remover (com
// motivo). WIRED a mutations TanStack Query + Toast. Espelha MotivoAgendaPayload(Motivo).
// A inclusão na fila vive em EntrarNaFilaModal.tsx (separação para manter < 300 linhas).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useConvocarDaFila, useRemoverDaFila } from './api';

const MOTIVO_MAX = 2000;

interface AcaoFilaProps {
  open: boolean;
  onClose: () => void;
  filaId: string;
}

// --- CONVOCAR ------------------------------------------------------------------

export function ConvocarFilaModal({ open, onClose, filaId }: AcaoFilaProps) {
  const toast = useToast();
  const mutation = useConvocarDaFila();

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(filaId, {
      onSuccess: () => {
        toast.success('Paciente convocado.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível convocar.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Convocar paciente"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="primary" type="submit" form="form-convocar-fila" loading={mutation.isPending}>
            Convocar
          </Button>
        </>
      }
    >
      <form id="form-convocar-fila" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Convocação:">
          Marca a entrada como convocada (vaga disponível) — aguardando a marcação efetiva.
        </Alert>
      </form>
    </Modal>
  );
}

// --- REMOVER (com motivo) ------------------------------------------------------

export function RemoverFilaModal({ open, onClose, filaId }: AcaoFilaProps) {
  const toast = useToast();
  const mutation = useRemoverDaFila();
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setMotivo('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = motivo.trim();
    if (valor === '') {
      setErro('Informe o motivo da remoção.');
      return;
    }
    if (valor.length > MOTIVO_MAX) {
      setErro(`O motivo deve ter no máximo ${MOTIVO_MAX} caracteres.`);
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { filaId, input: { motivo: valor } },
      {
        onSuccess: () => {
          toast.success('Entrada removida da fila.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível remover.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Remover da fila"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="danger" type="submit" form="form-remover-fila" loading={mutation.isPending}>
            Remover
          </Button>
        </>
      }
    >
      <form id="form-remover-fila" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="danger" title="Atenção:">
          A remoção é terminal (desistência/obsolescência) e retira o paciente da fila.
        </Alert>
        <FormField label="Motivo da remoção" required error={erro} help={`Máx. ${MOTIVO_MAX} caracteres.`}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              maxLength={MOTIVO_MAX}
              rows={4}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

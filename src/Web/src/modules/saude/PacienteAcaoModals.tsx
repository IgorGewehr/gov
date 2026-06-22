// Modais de AÇÃO de cadastro do agregado Paciente: confirmação no CADSUS e inativação
// (terminal/destrutiva — exige confirmação). WIRED a mutations TanStack Query + Toast.
// Ações gated por "saude.gerenciar" na DetailPage. Os modais de histórico clínico
// (condição/alergia) ficam em PacienteHistoricoModals.tsx; o de cadastro em PacienteEditModal.tsx.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useConfirmarCadastroCadsus, useInativarPaciente } from './api';
import { mapearErros } from './saude.formErrors';

const MOTIVO_MAX = 500;

interface AcaoPacienteProps {
  open: boolean;
  onClose: () => void;
  pacienteId: string;
}

// --- CONFIRMAR CADASTRO NO CADSUS ---------------------------------------------

export function ConfirmarCadsusModal({ open, onClose, pacienteId }: AcaoPacienteProps) {
  const toast = useToast();
  const mutation = useConfirmarCadastroCadsus(pacienteId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Cadastro confirmado no CADSUS.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível confirmar no CADSUS.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Confirmar cadastro no CADSUS"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-confirmar-cadsus" loading={mutation.isPending}>
            Confirmar
          </Button>
        </>
      }
    >
      <form id="form-confirmar-cadsus" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Confirmação CADSUS:">
          Marca o CNS do paciente como confirmado, habilitando o registro de atendimentos e
          solicitações de regulação.
        </Alert>
      </form>
    </Modal>
  );
}

// --- INATIVAR PACIENTE (terminal/destrutivo) ----------------------------------

export function InativarPacienteModal({ open, onClose, pacienteId }: AcaoPacienteProps) {
  const toast = useToast();
  const mutation = useInativarPaciente(pacienteId);
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = motivo.trim();
    if (valor === '') {
      setErro('Informe o motivo da inativação.');
      return;
    }
    if (valor.length > MOTIVO_MAX) {
      setErro(`O motivo deve ter no máximo ${MOTIVO_MAX} caracteres.`);
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { motivo: valor },
      {
        onSuccess: () => {
          toast.success('Paciente inativado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          const mapped = mapearErros(error, { motivo: 1 });
          if (mapped.motivo) setErro(mapped.motivo);
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível inativar o paciente.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Inativar paciente"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-inativar-paciente" loading={mutation.isPending}>
            Inativar
          </Button>
        </>
      }
    >
      <form id="form-inativar-paciente" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="danger" title="Ação terminal:">
          A inativação (óbito, transferência ou duplicidade) impede novos atendimentos para este
          cadastro. Confirme para prosseguir.
        </Alert>
        <FormField label="Motivo da inativação" required error={erro} help={`Máx. ${MOTIVO_MAX} caracteres.`}>
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

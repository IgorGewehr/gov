// Modais de ação do Agendamento: confirmar, registrar falta, realizar (ponte ao PEP) e
// cancelar (motivo + origem). WIRED a mutations TanStack Query + Toast. Espelham os
// payloads reais: RealizarAgendamentoPayload(AtendimentoId?) e CancelarAgendamentoPayload(Motivo, Origem).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  useCancelarAgendamento,
  useConfirmarAgendamento,
  useRealizarAgendamento,
  useRegistrarFalta,
} from './api';
import { opcoesOrigemCancelamento, paraOrigemCancelamento } from './saude.helpers';

const MOTIVO_MAX = 2000;

interface AcaoAgendamentoProps {
  open: boolean;
  onClose: () => void;
  agendamentoId: string;
}

// --- CONFIRMAR ----------------------------------------------------------------

export function ConfirmarAgendamentoModal({ open, onClose, agendamentoId }: AcaoAgendamentoProps) {
  const toast = useToast();
  const mutation = useConfirmarAgendamento();

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(agendamentoId, {
      onSuccess: () => {
        toast.success('Agendamento confirmado.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível confirmar.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Confirmar agendamento"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-confirmar-agendamento" loading={mutation.isPending}>
            Confirmar
          </Button>
        </>
      }
    >
      <form id="form-confirmar-agendamento" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Confirmação:">
          Registra que o paciente/unidade confirmou a presença na consulta agendada.
        </Alert>
      </form>
    </Modal>
  );
}

// --- REGISTRAR FALTA ----------------------------------------------------------

export function RegistrarFaltaModal({ open, onClose, agendamentoId }: AcaoAgendamentoProps) {
  const toast = useToast();
  const mutation = useRegistrarFalta();

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(agendamentoId, {
      onSuccess: () => {
        toast.success('Falta registrada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a falta.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Registrar falta"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="danger" type="submit" form="form-falta-agendamento" loading={mutation.isPending}>
            Registrar falta
          </Button>
        </>
      }
    >
      <form id="form-falta-agendamento" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="warning" title="Atenção:">
          Registra o não comparecimento do paciente. A vaga é liberada. Estado terminal.
        </Alert>
      </form>
    </Modal>
  );
}

// --- REALIZAR (ponte opcional ao PEP) -----------------------------------------

export function RealizarAgendamentoModal({ open, onClose, agendamentoId }: AcaoAgendamentoProps) {
  const toast = useToast();
  const mutation = useRealizarAgendamento();
  const [atendimentoId, setAtendimentoId] = useState('');

  function fechar(): void {
    setAtendimentoId('');
    onClose();
  }

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(
      { agendamentoId, input: { atendimentoId: atendimentoId.trim() || null } },
      {
        onSuccess: () => {
          toast.success('Agendamento marcado como realizado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível concluir.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar realização"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="primary" type="submit" form="form-realizar-agendamento" loading={mutation.isPending}>
            Confirmar realização
          </Button>
        </>
      }
    >
      <form id="form-realizar-agendamento" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Realização:">
          Registra que o paciente foi atendido. Estado terminal. Vincule, opcionalmente, o
          atendimento gerado no prontuário (PEP).
        </Alert>
        <FormField label="Atendimento vinculado (PEP) — opcional">
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={atendimentoId}
              onChange={(e) => setAtendimentoId(e.target.value)}
              placeholder="Identificador do atendimento (se já registrado)"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// --- CANCELAR (motivo + origem) -----------------------------------------------

export function CancelarAgendamentoModal({ open, onClose, agendamentoId }: AcaoAgendamentoProps) {
  const toast = useToast();
  const mutation = useCancelarAgendamento();
  const [motivo, setMotivo] = useState('');
  const [origem, setOrigem] = useState('1');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setMotivo('');
    setOrigem('1');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = motivo.trim();
    if (valor === '') {
      setErro('Informe o motivo do cancelamento.');
      return;
    }
    if (valor.length > MOTIVO_MAX) {
      setErro(`O motivo deve ter no máximo ${MOTIVO_MAX} caracteres.`);
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { agendamentoId, input: { motivo: valor, origem: paraOrigemCancelamento(origem) } },
      {
        onSuccess: () => {
          toast.success('Agendamento cancelado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível cancelar.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cancelar agendamento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="danger" type="submit" form="form-cancelar-agendamento" loading={mutation.isPending}>
            Cancelar agendamento
          </Button>
        </>
      }
    >
      <form id="form-cancelar-agendamento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="danger" title="Atenção:">
          O cancelamento libera a vaga e é terminal. A origem alimenta o indicador de absenteísmo.
        </Alert>
        <FormField label="Origem do cancelamento" required>
          {({ id }) => (
            <Select
              id={id}
              options={opcoesOrigemCancelamento}
              value={origem}
              onChange={(e) => setOrigem(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Motivo do cancelamento" required error={erro} help={`Máx. ${MOTIVO_MAX} caracteres.`}>
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

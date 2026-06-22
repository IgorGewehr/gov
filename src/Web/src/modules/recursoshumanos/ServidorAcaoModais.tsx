// Modais de AÇÃO (commands de ciclo de vida) do agregado Servidor — etapas iniciais do
// provimento: posse, exercício e estabilidade. Abertos a partir da ServidorDetailPage,
// wired a mutations TanStack Query, com validação por campo e Toast de sucesso/erro.
// Afastamento e desligamento (maiores) ficam em ServidorAfastamentoModais e são
// reexportados aqui para manter um único ponto de import.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { tratarErroCampos } from './acaoModal.helpers';
import { useConcederEstabilidade, useIniciarExercicio, useRegistrarPosse } from './api';

export {
  RegistrarAfastamentoModal,
  DesligarServidorModal,
} from './ServidorAfastamentoModais';

interface AcaoModalBaseProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
}

// ---------------------------------------------------------------------------
// POSSE (RegistrarPosse) — Nomeado → Empossado
// ---------------------------------------------------------------------------

export function RegistrarPosseModal({ open, onClose, servidorId }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarPosse(servidorId);
  const [dataPosse, setDataPosse] = useState('');
  const [errors, setErrors] = useState<{ dataPosse?: string }>({});

  function fechar(): void {
    setErrors({});
    setDataPosse('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (dataPosse.trim() === '') {
      setErrors({ dataPosse: 'Informe a data de posse.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { dataPosse },
      {
        onSuccess: () => {
          toast.success('Posse registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { dataPosse: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a posse.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar posse"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-posse" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-posse" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Data de posse" required error={errors.dataPosse}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataPosse}
              onChange={(e) => setDataPosse(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// EXERCÍCIO (IniciarExercicio) — Empossado → EmExercicio
// ---------------------------------------------------------------------------

export function IniciarExercicioModal({ open, onClose, servidorId }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useIniciarExercicio(servidorId);
  const [dataExercicio, setDataExercicio] = useState('');
  const [errors, setErrors] = useState<{ dataExercicio?: string }>({});

  function fechar(): void {
    setErrors({});
    setDataExercicio('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (dataExercicio.trim() === '') {
      setErrors({ dataExercicio: 'Informe a data de início do exercício.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { dataExercicio },
      {
        onSuccess: () => {
          toast.success('Exercício iniciado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { dataExercicio: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível iniciar o exercício.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Iniciar exercício"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-exercicio" loading={mutation.isPending}>
            Iniciar
          </Button>
        </>
      }
    >
      <form id="form-exercicio" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Data de exercício" required error={errors.dataExercicio}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataExercicio}
              onChange={(e) => setDataExercicio(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// ESTABILIDADE (ConcederEstabilidade) — EmExercicio → Estavel (sem payload)
// ---------------------------------------------------------------------------

export function ConcederEstabilidadeModal({ open, onClose, servidorId }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useConcederEstabilidade(servidorId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Estabilidade concedida.', 'Sucesso');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível conceder estabilidade.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Conceder estabilidade"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-estabilidade" loading={mutation.isPending}>
            Conceder
          </Button>
        </>
      }
    >
      <form id="form-estabilidade" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Estágio probatório:">
          A estabilidade (art. 41 CF/88) é concedida após o estágio probatório. Confirme para
          marcar o servidor como Estável.
        </Alert>
      </form>
    </Modal>
  );
}

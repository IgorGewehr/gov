// Modais de AÇÃO do agregado Servidor que alteram disponibilidade do vínculo:
// afastamento (transição → Afastado) e desligamento (terminal → Desligado, destrutivo).
// Abertos a partir da ServidorDetailPage. Wired a mutations TanStack Query, com Toast,
// validação por campo e mapeamento de ProblemDetails.fieldErrors.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { MOTIVO_MAX, tratarErroCampos } from './acaoModal.helpers';
import { useDesligarServidor, useRegistrarAfastamento } from './api';

interface AcaoModalBaseProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
}

// ---------------------------------------------------------------------------
// AFASTAMENTO (RegistrarAfastamento) — EmExercicio/Estavel → Afastado
// ---------------------------------------------------------------------------

export function RegistrarAfastamentoModal({ open, onClose, servidorId }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarAfastamento(servidorId);
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [motivo, setMotivo] = useState('');
  const [errors, setErrors] = useState<{ inicio?: string; motivo?: string }>({});

  function fechar(): void {
    setErrors({});
    setInicio('');
    setFim('');
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { inicio?: string; motivo?: string } = {};
    if (inicio.trim() === '') next.inicio = 'Informe a data de início do afastamento.';
    if (motivo.trim() === '') next.motivo = 'Informe o motivo do afastamento.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { inicio, fim: fim.trim() === '' ? null : fim, motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success('Afastamento registrado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { inicio: 1, motivo: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o afastamento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar afastamento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-afastamento" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-afastamento" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Início do afastamento" required error={errors.inicio}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={inicio}
              onChange={(e) => setInicio(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Fim do afastamento" help="Opcional (afastamento por prazo indeterminado).">
          {({ id, describedBy }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              value={fim}
              onChange={(e) => setFim(e.target.value)}
            />
          )}
        </FormField>
        <FormField
          label="Motivo"
          required
          error={errors.motivo}
          help={`Máx. ${MOTIVO_MAX} caracteres.`}
        >
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              maxLength={MOTIVO_MAX}
              rows={3}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// DESLIGAMENTO (DesligarServidor) — transição TERMINAL → Desligado (destrutivo)
// ---------------------------------------------------------------------------

export function DesligarServidorModal({ open, onClose, servidorId }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useDesligarServidor(servidorId);
  const [dataDesligamento, setDataDesligamento] = useState('');
  const [motivo, setMotivo] = useState('');
  const [errors, setErrors] = useState<{ dataDesligamento?: string; motivo?: string }>({});

  function fechar(): void {
    setErrors({});
    setDataDesligamento('');
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { dataDesligamento?: string; motivo?: string } = {};
    if (dataDesligamento.trim() === '') next.dataDesligamento = 'Informe a data de desligamento.';
    if (motivo.trim() === '') next.motivo = 'Informe o motivo do desligamento.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { dataDesligamento, motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success('Servidor desligado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { dataDesligamento: 1, motivo: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível desligar o servidor.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Desligar servidor"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-desligamento" loading={mutation.isPending}>
            Desligar definitivamente
          </Button>
        </>
      }
    >
      <form id="form-desligamento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="danger" title="Ação terminal:">
          O desligamento encerra o vínculo e vaga o cargo. O servidor deixa o quadro ativo.
        </Alert>
        <FormField label="Data de desligamento" required error={errors.dataDesligamento}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataDesligamento}
              onChange={(e) => setDataDesligamento(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Motivo" required error={errors.motivo} help={`Máx. ${MOTIVO_MAX} caracteres.`}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              maxLength={MOTIVO_MAX}
              rows={3}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

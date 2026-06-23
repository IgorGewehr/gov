// Modais dos AFASTAMENTOS/LICENÇAS tipados (Onda 1): lançar (tipo + período + documento),
// encerrar (retorno) e cancelar (lançado por engano). Wired a mutations TanStack Query,
// com validação por campo, Toast e mapeamento de ProblemDetails.fieldErrors.
// O usuário escolhe o TIPO; o efeito na folha vem da regra do tipo no backend.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { MOTIVO_MAX, tratarErroCampos } from './acaoModal.helpers';
import { TIPOS_AFASTAMENTO, formatarTipoAfastamento } from './afastamento.helpers';
import {
  useCancelarAfastamento,
  useEncerrarAfastamento,
  useRegistrarAfastamentoTipado,
} from './afastamento.api';
import type { TipoAfastamento } from './afastamento.api';

interface RegistrarProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
}

// ---------------------------------------------------------------------------
// LANÇAR AFASTAMENTO (RegistrarAfastamento) — tipo + início + fim previsto + documento
// ---------------------------------------------------------------------------

export function LancarAfastamentoModal({ open, onClose, servidorId }: RegistrarProps) {
  const toast = useToast();
  const mutation = useRegistrarAfastamentoTipado(servidorId);
  const [tipo, setTipo] = useState('');
  const [inicio, setInicio] = useState('');
  const [fimPrevisto, setFimPrevisto] = useState('');
  const [documento, setDocumento] = useState('');
  const [errors, setErrors] = useState<{ tipo?: string; inicio?: string; fimPrevisto?: string }>({});

  function fechar(): void {
    setErrors({});
    setTipo('');
    setInicio('');
    setFimPrevisto('');
    setDocumento('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { tipo?: string; inicio?: string; fimPrevisto?: string } = {};
    if (tipo === '') next.tipo = 'Selecione o tipo de afastamento.';
    if (inicio.trim() === '') next.inicio = 'Informe a data de início.';
    if (fimPrevisto !== '' && inicio !== '' && fimPrevisto < inicio) {
      next.fimPrevisto = 'O fim previsto não pode ser anterior ao início.';
    }
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      {
        tipo: tipo as TipoAfastamento,
        inicio,
        fimPrevisto: fimPrevisto.trim() === '' ? null : fimPrevisto,
        documento: documento.trim() === '' ? null : documento.trim(),
      },
      {
        onSuccess: () => {
          toast.success('Afastamento lançado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { tipo: 1, inicio: 1, fimPrevisto: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível lançar o afastamento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Lançar afastamento / licença"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-lancar-afastamento" loading={mutation.isPending}>
            Lançar
          </Button>
        </>
      }
    >
      <form id="form-lancar-afastamento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Efeito na folha:">
          O efeito na folha (suspensão, percentual mantido pelo ente, contagem de tempo) é definido
          pela regra do tipo escolhido — não é digitado aqui.
        </Alert>
        <FormField label="Tipo de afastamento" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione o tipo"
              options={TIPOS_AFASTAMENTO}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
            />
          )}
        </FormField>
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
        <FormField
          label="Fim previsto"
          help="Opcional (deixe em branco para prazo indeterminado, ex.: auxílio-doença)."
          error={errors.fimPrevisto}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={fimPrevisto}
              onChange={(e) => setFimPrevisto(e.target.value)}
            />
          )}
        </FormField>
        <FormField
          label="Documento"
          help="Opcional — referência do atestado/portaria/laudo."
        >
          {({ id, describedBy }) => (
            <Input
              id={id}
              type="text"
              aria-describedby={describedBy}
              maxLength={120}
              value={documento}
              onChange={(e) => setDocumento(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

interface AcaoAfastamentoProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
  afastamentoId: string;
  tipo: string;
  inicio: string;
}

// ---------------------------------------------------------------------------
// ENCERRAR AFASTAMENTO (retorno do servidor) — grava o fim efetivo
// ---------------------------------------------------------------------------

export function EncerrarAfastamentoModal({
  open,
  onClose,
  servidorId,
  afastamentoId,
  tipo,
  inicio,
}: AcaoAfastamentoProps) {
  const toast = useToast();
  const mutation = useEncerrarAfastamento(servidorId);
  const [fimEfetivo, setFimEfetivo] = useState('');
  const [errors, setErrors] = useState<{ fimEfetivo?: string }>({});

  function fechar(): void {
    setErrors({});
    setFimEfetivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { fimEfetivo?: string } = {};
    if (fimEfetivo.trim() === '') next.fimEfetivo = 'Informe a data efetiva de retorno.';
    else if (fimEfetivo < inicio) next.fimEfetivo = 'O retorno não pode ser anterior ao início.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { afastamentoId, input: { fimEfetivo } },
      {
        onSuccess: () => {
          toast.success('Afastamento encerrado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { fimEfetivo: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar o afastamento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Encerrar afastamento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-encerrar-afastamento" loading={mutation.isPending}>
            Encerrar (registrar retorno)
          </Button>
        </>
      }
    >
      <form id="form-encerrar-afastamento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title={formatarTipoAfastamento(tipo)}>
          Registre a data efetiva de retorno. O servidor volta ao exercício na mesma operação.
        </Alert>
        <FormField label="Data de retorno" required error={errors.fimEfetivo}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              min={inicio}
              value={fimEfetivo}
              onChange={(e) => setFimEfetivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// CANCELAR AFASTAMENTO (lançado por engano/revogado) — sem efeito na folha
// ---------------------------------------------------------------------------

export function CancelarAfastamentoModal({
  open,
  onClose,
  servidorId,
  afastamentoId,
  tipo,
}: Omit<AcaoAfastamentoProps, 'inicio'>) {
  const toast = useToast();
  const mutation = useCancelarAfastamento(servidorId);
  const [motivo, setMotivo] = useState('');
  const [errors, setErrors] = useState<{ motivo?: string }>({});

  function fechar(): void {
    setErrors({});
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErrors({ motivo: 'Informe o motivo do cancelamento.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { afastamentoId, input: { motivo: motivo.trim() } },
      {
        onSuccess: () => {
          toast.success('Afastamento cancelado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { motivo: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível cancelar o afastamento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cancelar afastamento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="danger" type="submit" form="form-cancelar-afastamento" loading={mutation.isPending}>
            Cancelar afastamento
          </Button>
        </>
      }
    >
      <form id="form-cancelar-afastamento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title={`Revogar ${formatarTipoAfastamento(tipo)}:`}>
          Use o cancelamento apenas quando o afastamento foi lançado por engano ou revogado. O
          registro deixa de ter efeito na folha e o servidor volta ao exercício.
        </Alert>
        <FormField label="Motivo do cancelamento" required error={errors.motivo} help={`Máx. ${MOTIVO_MAX} caracteres.`}>
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

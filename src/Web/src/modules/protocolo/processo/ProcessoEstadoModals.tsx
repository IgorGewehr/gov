// Modais de ACAO de mudanca de ESTADO do agregado Processo (transicoes de situacao):
//   - SobrestarProcessoModal -> Sobrestado
//   - ArquivarProcessoModal   -> Arquivado (terminal, destrutivo, exige confirmacao)
// Cada um e wired a uma mutation TanStack Query, com validacao por campo e Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useArquivarProcesso, useSobrestarProcesso } from './processo.api';
import { MOTIVO_MAX, tratarErroCampos } from './processoAcao.shared';
import type { AcaoModalBaseProps } from './processoAcao.shared';

// ---------------------------------------------------------------------------
// SOBRESTAR (command SobrestarProcesso) — transicao -> Sobrestado
// ---------------------------------------------------------------------------

export function SobrestarProcessoModal({ open, onClose, processoId, nup }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useSobrestarProcesso(nup);
  const [motivo, setMotivo] = useState('');
  const [errors, setErrors] = useState<{ motivo?: string }>({});

  function fechar(): void {
    setErrors({});
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = motivo.trim();
    if (valor === '') {
      setErrors({ motivo: 'Informe o motivo do sobrestamento.' });
      return;
    }
    if (valor.length > MOTIVO_MAX) {
      setErrors({ motivo: `O motivo deve ter no máximo ${MOTIVO_MAX} caracteres.` });
      return;
    }
    setErrors({});
    mutation.mutate(
      { processoId, input: { motivo: valor } },
      {
        onSuccess: () => {
          toast.success('Processo sobrestado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { motivo: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível sobrestar o processo.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Sobrestar processo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-sobrestar" loading={mutation.isPending}>
            Sobrestar
          </Button>
        </>
      }
    >
      <form id="form-sobrestar" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Atenção:">
          O sobrestamento suspende o andamento. O processo não tramita até ser reativado.
        </Alert>
        <FormField
          label="Motivo do sobrestamento"
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

// ---------------------------------------------------------------------------
// ARQUIVAR (command ArquivarProcesso) — transicao TERMINAL -> Arquivado (destrutivo)
// ---------------------------------------------------------------------------

export function ArquivarProcessoModal({ open, onClose, processoId, nup }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useArquivarProcesso(nup);
  const [motivo, setMotivo] = useState('');

  function fechar(): void {
    setMotivo('');
    onClose();
  }

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(
      { processoId, input: { motivo: motivo.trim() || null } },
      {
        onSuccess: () => {
          toast.success('Processo arquivado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível arquivar o processo.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Arquivar processo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-arquivar" loading={mutation.isPending}>
            Arquivar definitivamente
          </Button>
        </>
      }
    >
      <form id="form-arquivar" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="danger" title="Ação irreversível:">
          O arquivamento é terminal. O processo deixa de tramitar e a guarda passa a reger-se pela
          Tabela de Temporalidade (TTD/CONARQ). Confirme para prosseguir.
        </Alert>
        <FormField label="Motivo do arquivamento" help="Opcional.">
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
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

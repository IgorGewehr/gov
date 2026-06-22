// Modais de ACAO do agregado Processo, abertos a partir da DetailPage. Cada um e
// wired a uma mutation TanStack Query, com validacao por campo (FormField/
// aria-describedby), mapeamento de ProblemDetails.fieldErrors e Toast.
//
// Este arquivo concentra TRAMITAR e DESPACHAR (movimentacao/registro). As transicoes
// de estado (Sobrestar/Arquivar) vivem em ProcessoEstadoModals.tsx e sao reexportadas
// aqui para preservar o ponto unico de import da DetailPage (cada arquivo < 300 linhas).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useDespacharProcesso, useTramitarProcesso } from './processo.api';
import { tratarErroCampos } from './processoAcao.shared';
import type { AcaoModalBaseProps } from './processoAcao.shared';

export { SobrestarProcessoModal, ArquivarProcessoModal } from './ProcessoEstadoModals';

// ---------------------------------------------------------------------------
// TRAMITAR (command TramitarProcesso) — transicao -> EmTramitacao
// ---------------------------------------------------------------------------

export function TramitarProcessoModal({ open, onClose, processoId, nup }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useTramitarProcesso(nup);
  const [setorDestinoId, setSetorDestinoId] = useState('');
  const [observacao, setObservacao] = useState('');
  const [errors, setErrors] = useState<{ setorDestinoId?: string }>({});

  function fechar(): void {
    setErrors({});
    setSetorDestinoId('');
    setObservacao('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (setorDestinoId.trim() === '') {
      setErrors({ setorDestinoId: 'Informe o setor de destino.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      {
        processoId,
        input: { setorDestinoId: setorDestinoId.trim(), observacao: observacao.trim() || null },
      },
      {
        onSuccess: () => {
          toast.success('Processo tramitado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { setorDestinoId: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível tramitar o processo.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Tramitar processo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-tramitar" loading={mutation.isPending}>
            Tramitar
          </Button>
        </>
      }
    >
      <form id="form-tramitar" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Setor de destino (identificador)" required error={errors.setorDestinoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={setorDestinoId}
              onChange={(e) => setSetorDestinoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField label="Observação" help="Opcional.">
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              value={observacao}
              onChange={(e) => setObservacao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// DESPACHAR (command DespacharProcesso) — registro append-only, situacao inalterada
// ---------------------------------------------------------------------------

export function DespacharProcessoModal({ open, onClose, processoId, nup }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useDespacharProcesso(nup);
  const [texto, setTexto] = useState('');
  const [autoridadeId, setAutoridadeId] = useState('');
  const [errors, setErrors] = useState<{ texto?: string; autoridadeId?: string }>({});

  function fechar(): void {
    setErrors({});
    setTexto('');
    setAutoridadeId('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { texto?: string; autoridadeId?: string } = {};
    if (texto.trim() === '') next.texto = 'Informe o conteúdo do despacho.';
    if (autoridadeId.trim() === '') next.autoridadeId = 'Informe a autoridade do despacho.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { processoId, input: { texto: texto.trim(), autoridadeId: autoridadeId.trim() } },
      {
        onSuccess: () => {
          toast.success('Despacho registrado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { texto: 1, autoridadeId: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o despacho.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar despacho"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-despachar" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-despachar" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Autoridade (identificador)" required error={errors.autoridadeId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={autoridadeId}
              onChange={(e) => setAutoridadeId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField label="Texto do despacho" required error={errors.texto}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              rows={5}
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

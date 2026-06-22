// Modal de AÇÃO "Emitir CDA" (command EmitirCda) sobre uma Dívida Ativa inscrita.
// Espelha o validator: NumeroCda NotEmpty + MaximumLength(40). WIRED a uma mutation
// TanStack Query com validação por campo, mapeamento de ProblemDetails e Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useEmitirCda } from './api';

const NUMERO_CDA_MAX = 40;

export interface EmitirCdaModalProps {
  open: boolean;
  onClose: () => void;
  dividaAtivaId: string;
  /** Contribuinte cuja lista de dívidas deve ser invalidada após a emissão. */
  contribuinteIdParaInvalidar?: string;
}

export function EmitirCdaModal({
  open,
  onClose,
  dividaAtivaId,
  contribuinteIdParaInvalidar,
}: EmitirCdaModalProps) {
  const toast = useToast();
  const mutation = useEmitirCda(contribuinteIdParaInvalidar);
  const [numeroCda, setNumeroCda] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setNumeroCda('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = numeroCda.trim();
    if (valor === '') {
      setErro('Informe o número da CDA.');
      return;
    }
    if (valor.length > NUMERO_CDA_MAX) {
      setErro(`O número da CDA deve ter no máximo ${NUMERO_CDA_MAX} caracteres.`);
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { dividaAtivaId, input: { numeroCda: valor } },
      {
        onSuccess: () => {
          toast.success('CDA emitida.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.NumeroCda) {
            setErro(error.fieldErrors.NumeroCda[0]);
          }
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível emitir a CDA.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Emitir Certidão de Dívida Ativa (CDA)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-emitir-cda" loading={mutation.isPending}>
            Emitir CDA
          </Button>
        </>
      }
    >
      <form id="form-emitir-cda" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Certidão de Dívida Ativa">
          A CDA confere exequibilidade ao crédito inscrito (título executivo extrajudicial). Informe
          o número de controle gerado pela Procuradoria/Fazenda municipal.
        </Alert>
        <FormField
          label="Número da CDA"
          required
          error={erro}
          help={`Máx. ${NUMERO_CDA_MAX} caracteres.`}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={NUMERO_CDA_MAX}
              value={numeroCda}
              onChange={(e) => setNumeroCda(e.target.value)}
              placeholder="Ex.: CDA-2026-000123"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

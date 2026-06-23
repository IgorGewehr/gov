// Modal de AÇÃO "Ajuizar execução fiscal" (command AjuizarExecucaoFiscal, Lei 6.830/80) — gancho de
// saída que exporta a CDA para ajuizamento. Espelha o ExecucaoFiscalPayload: { dataAjuizamento }. O
// despacho que ordena a citação INTERROMPE a prescrição (CTN art. 174 p.ú. I) na data informada.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAjuizarExecucaoFiscal } from './api';

export interface ExecucaoFiscalModalProps {
  open: boolean;
  onClose: () => void;
  dividaAtivaId: string;
  contribuinteIdParaInvalidar?: string;
}

function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function ExecucaoFiscalModal({
  open,
  onClose,
  dividaAtivaId,
  contribuinteIdParaInvalidar,
}: ExecucaoFiscalModalProps) {
  const toast = useToast();
  const mutation = useAjuizarExecucaoFiscal(contribuinteIdParaInvalidar);
  const [dataAjuizamento, setDataAjuizamento] = useState(hojeIso());
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setDataAjuizamento(hojeIso());
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (dataAjuizamento === '') {
      setErro('Informe a data do ajuizamento.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { dividaAtivaId, dataAjuizamento },
      {
        onSuccess: () => {
          toast.success('Execução fiscal ajuizada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível ajuizar a execução.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Ajuizar execução fiscal"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-execucao-fiscal" loading={mutation.isPending}>
            Ajuizar
          </Button>
        </>
      }
    >
      <form id="form-execucao-fiscal" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Execução fiscal (Lei 6.830/80)">
          Exige CDA já emitida (ou protestada). O despacho que ordena a citação interrompe a
          prescrição (CTN art. 174, p.ú., I) na data do ajuizamento.
        </Alert>
        <FormField label="Data do ajuizamento" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataAjuizamento}
              onChange={(e) => setDataAjuizamento(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

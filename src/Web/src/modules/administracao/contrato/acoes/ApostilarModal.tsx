// ApostilarContrato
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Modal, Select, Textarea, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import {
  TIPO_APOSTILAMENTO_NUMERICO,
  TIPO_APOSTILAMENTO_ROTULO,
  useApostilarContrato,
} from '../contrato.api';
import type { TipoApostilamento } from '../contrato.api';
import { mapearFieldErrors } from './acaoModals.shared';
import type { AcaoModalProps } from './acaoModals.shared';

export function ApostilarModal({ contratoId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useApostilarContrato(contratoId);
  const [tipo, setTipo] = useState<TipoApostilamento>('Reajuste');
  const [descricao, setDescricao] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (descricao.trim() === '') {
      setErro('Informe a descrição do apostilamento.');
      return;
    }
    mutation.mutate(
      { tipo: TIPO_APOSTILAMENTO_NUMERICO[tipo], descricao: descricao.trim() },
      {
        onSuccess: () => {
          toast.success('Apostilamento registrado (dispensa termo aditivo — art. 136).', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErro(mapearFieldErrors(error, { descricao: true }).descricao);
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível apostilar o contrato.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Apostilar contrato"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-apostilar" loading={mutation.isPending}>
            Registrar apostilamento
          </Button>
        </>
      }
    >
      <form id="form-apostilar" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-60">
          O apostilamento registra alteração que dispensa termo aditivo (reajuste/dotação — art. 136).
        </p>
        <FormField label="Tipo de apostilamento" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoApostilamento)}
              options={(Object.keys(TIPO_APOSTILAMENTO_ROTULO) as TipoApostilamento[]).map((t) => ({
                value: t,
                label: TIPO_APOSTILAMENTO_ROTULO[t],
              }))}
            />
          )}
        </FormField>
        <FormField label="Descrição" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

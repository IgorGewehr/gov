// Acao "Justificar descumprimento" de uma condicionalidade (command JustificarDescumprimento).
// Registra o motivo do CRAS, retira o registro da contagem efetiva e reduz a gradacao do efeito.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useJustificarDescumprimento } from './pbf.api';
import type { AcompanhamentoResultado, CondicionalidadeResultado } from './pbf.api';
import { tipoCondicionalidadeLabel } from './pbf.helpers';

export interface JustificarDescumprimentoModalProps {
  open: boolean;
  onClose: () => void;
  /** Acompanhamento que contem o registro. */
  acompanhamento: AcompanhamentoResultado;
  /** Registro de condicionalidade descumprida a justificar. */
  registro: CondicionalidadeResultado;
}

export function JustificarDescumprimentoModal({
  open,
  onClose,
  acompanhamento,
  registro,
}: JustificarDescumprimentoModalProps) {
  const toast = useToast();
  const mutation = useJustificarDescumprimento();

  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setMotivo('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('A justificativa exige um motivo.');
      return;
    }
    setErro(undefined);

    mutation.mutate(
      {
        acompanhamentoId: acompanhamento.acompanhamentoId,
        registroId: registro.registroId,
        motivo: motivo.trim(),
      },
      {
        onSuccess: () => {
          toast.success('Descumprimento justificado.', 'Justificativa registrada');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.Motivo) {
            setErro(error.fieldErrors.Motivo[0]);
          }
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível justificar o descumprimento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Justificar descumprimento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-justificar-descumprimento" loading={mutation.isPending}>
            Justificar
          </Button>
        </>
      }
    >
      <form id="form-justificar-descumprimento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning">
          Justificar <strong>{tipoCondicionalidadeLabel(registro.tipo)}</strong> retira o registro da
          contagem de descumprimentos efetivos e recalcula o efeito gradativo. A gradação federal
          (MDS/SICON) permanece a fonte autoritativa.
        </Alert>

        <FormField
          label="Motivo da justificativa"
          required
          error={erro}
          help="Registro do CRAS (motivo apurado na busca ativa)."
        >
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
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

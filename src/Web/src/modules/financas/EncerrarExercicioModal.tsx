// Modal de encerramento de exercício (POST /restos-a-pagar/encerrar-exercicio). Inscreve
// como Restos a Pagar as despesas empenhadas e não pagas do exercício informado.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { useEncerrarExercicio } from './financas.api';
import { exercicioCorrente, mensagemErro } from './financas.helpers';

export interface EncerrarExercicioModalProps {
  open: boolean;
  onClose: () => void;
}

export function EncerrarExercicioModal({ open, onClose }: EncerrarExercicioModalProps) {
  const toast = useToast();
  const mutation = useEncerrarExercicio();
  const [exercicio, setExercicio] = useState(String(exercicioCorrente() - 1));
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = Number(exercicio);
    if (!Number.isInteger(valor) || valor < 2000) {
      setErro('Informe um exercício válido (a partir de 2000).');
      return;
    }
    setErro(undefined);
    mutation.mutate(valor, {
      onSuccess: (data) => {
        toast.success(`Exercício encerrado. ${data.inscritos} resto(s) inscrito(s).`, 'Sucesso');
        fechar();
      },
      onError: (error) => toast.error(mensagemErro(error, 'Não foi possível encerrar o exercício.')),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Encerrar exercício"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-encerrar-exercicio" loading={mutation.isPending}>
            Encerrar
          </Button>
        </>
      }
    >
      <form id="form-encerrar-exercicio" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Ação contábil relevante.">
          O encerramento inscreve em Restos a Pagar as despesas empenhadas e não pagas do exercício.
          A operação é registrada na trilha de auditoria.
        </Alert>
        <FormField label="Exercício a encerrar" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min="2000" step="1" inputMode="numeric"
              aria-describedby={describedBy} invalid={invalid}
              value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

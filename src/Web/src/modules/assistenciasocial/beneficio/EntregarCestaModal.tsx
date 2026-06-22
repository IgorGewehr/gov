// Acao de TRANSICAO "Entregar cesta basica" em Modal (command EntregarCestaBasica).
// Guarda I-8: so e permitida sobre beneficio Concedida do tipo Eventual; quantidade >= 1.
// A situacao permanece Concedida (a entrega apenas registra a quantidade/data).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useEntregarCestaBasica } from './beneficio.api';
import type { BeneficioResumo } from './beneficio.api';

export interface EntregarCestaModalProps {
  open: boolean;
  onClose: () => void;
  /** Beneficio alvo da entrega (deve ser Concedida + Eventual). */
  beneficio: BeneficioResumo;
}

export function EntregarCestaModal({ open, onClose, beneficio }: EntregarCestaModalProps) {
  const toast = useToast();
  const mutation = useEntregarCestaBasica(beneficio.familiaId);

  const [quantidade, setQuantidade] = useState('1');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const qtd = Number(quantidade);
    if (quantidade.trim() === '' || !Number.isInteger(qtd) || qtd < 1) {
      setErro('A quantidade de cestas deve ser um inteiro ≥ 1.');
      return;
    }
    setErro(undefined);

    mutation.mutate(
      { beneficioId: beneficio.id, quantidade: qtd },
      {
        onSuccess: () => {
          toast.success(
            `Entrega de ${qtd} cesta(s) básica(s) registrada.`,
            'Entrega registrada',
          );
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.Quantidade) {
            setErro(error.fieldErrors.Quantidade[0]);
          }
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível registrar a entrega da cesta básica.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Entregar cesta básica"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-entregar-cesta" loading={mutation.isPending}>
            Registrar entrega
          </Button>
        </>
      }
    >
      <form id="form-entregar-cesta" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          A entrega é permitida apenas sobre benefício eventual concedido (cesta básica) e não
          altera a situação do benefício, que permanece <strong>Concedida</strong>.
        </Alert>

        <FormField
          label="Quantidade de cestas"
          required
          error={erro}
          help="Quantidade física entregue (mínimo 1)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1"
              step="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={quantidade}
              onChange={(e) => setQuantidade(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

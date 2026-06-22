// Formulário de EFETIVAÇÃO de pagamento de uma folha FECHADA, em Modal. Padrão-ouro:
// mutation (escopada ao folhaId) + validação + Toast + mapeamento de erros. Transição
// terminal Fechada → Paga; dispara o PagamentoEfetuadoIntegrationEvent no backend.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useEfetuarPagamento } from './api';

export interface EfetuarPagamentoFormModalProps {
  open: boolean;
  onClose: () => void;
  folhaId: string;
}

export function EfetuarPagamentoFormModal({ open, onClose, folhaId }: EfetuarPagamentoFormModalProps) {
  const toast = useToast();
  const mutation = useEfetuarPagamento(folhaId);
  const [dataPagamento, setDataPagamento] = useState('');
  const [errors, setErrors] = useState<{ dataPagamento?: string }>({});

  function fechar(): void {
    setErrors({});
    setDataPagamento('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (dataPagamento.trim() === '') {
      setErrors({ dataPagamento: 'Informe a data do pagamento.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { dataPagamento },
      {
        onSuccess: () => {
          toast.success('Pagamento efetuado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.DataPagamento) {
            setErrors({ dataPagamento: error.fieldErrors.DataPagamento[0] });
          }
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível efetuar o pagamento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Efetuar pagamento da folha"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-pagamento" loading={mutation.isPending}>
            Efetuar pagamento
          </Button>
        </>
      }
    >
      <form id="form-pagamento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Atenção:">
          O pagamento só pode ser efetuado após o fechamento da folha e é definitivo para a
          competência.
        </Alert>
        <FormField label="Data do pagamento" required error={errors.dataPagamento}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataPagamento}
              onChange={(e) => setDataPagamento(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

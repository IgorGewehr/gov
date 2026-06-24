// Modal de REAJUSTE SALARIAL EM LOTE (revisão geral anual): aplica um percentual linear ao
// vencimento dos cargos ativos, opcionalmente restrito a um tipo. Confirma a quantidade reajustada.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAplicarReajusteEmLote } from './api';
import type { AplicarReajusteEmLoteInput } from './api';
import { TIPOS_CARGO } from './recursosHumanos.helpers';

export interface ReajusteEmLoteModalProps {
  open: boolean;
  onClose: () => void;
}

export function ReajusteEmLoteModal({ open, onClose }: ReajusteEmLoteModalProps) {
  const toast = useToast();
  const mutation = useAplicarReajusteEmLote();

  const [percentual, setPercentual] = useState('');
  const [tipo, setTipo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const p = Number(percentual);
    if (percentual.trim() === '' || Number.isNaN(p) || p <= 0 || p > 100) {
      setErro('Informe um percentual entre 0 (exclusivo) e 100.');
      return;
    }
    setErro(undefined);

    const input: AplicarReajusteEmLoteInput = {
      percentual: p,
      tipo: tipo.trim() ? Number(tipo) : null,
    };

    mutation.mutate(input, {
      onSuccess: (res) => {
        toast.success(
          `Reajuste de ${res.percentual}% aplicado a ${res.cargosReajustados} cargo(s).`,
          'Sucesso',
        );
        setPercentual('');
        setTipo('');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível aplicar o reajuste.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Reajuste salarial em lote"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-reajuste-lote" loading={mutation.isPending}>
            Aplicar reajuste
          </Button>
        </>
      }
    >
      <form id="form-reajuste-lote" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Percentual de reajuste (%)"
          required
          error={erro}
          help="Aplicado linearmente ao vencimento-base dos cargos ativos (revisão geral — CF art. 37, X)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={percentual}
              onChange={(e) => setPercentual(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Restringir ao tipo" help="Opcional; em branco reajusta todos os cargos ativos.">
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              options={[{ value: '', label: 'Todos os tipos' }, ...TIPOS_CARGO]}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

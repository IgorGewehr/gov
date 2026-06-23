// Acao "Consolidar Censo" de uma unidade num exercicio (command ConsolidarCenso).
// O volume anual de atendimentos e as familias referenciadas DERIVAM do RMA/Familia
// (sem dupla digitacao) — aqui apenas escolhemos a unidade a (re)consolidar.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useConsolidarCenso, useUnidades } from './censo.api';
import { tipoUnidadeSigla } from './censo.helpers';

export interface ConsolidarCensoModalProps {
  open: boolean;
  onClose: () => void;
  /** Exercicio (ano) corrente da consulta. */
  exercicio: number;
}

export function ConsolidarCensoModal({ open, onClose, exercicio }: ConsolidarCensoModalProps) {
  const toast = useToast();
  const mutation = useConsolidarCenso();
  const unidadesQuery = useUnidades(open);

  const [unidadeId, setUnidadeId] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setUnidadeId('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (unidadeId === '') {
      setErro('Selecione a unidade a consolidar.');
      return;
    }
    setErro(undefined);

    mutation.mutate(
      { unidadeId, exercicio },
      {
        onSuccess: () => {
          toast.success('Censo da unidade consolidado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível consolidar o Censo.',
          );
        },
      },
    );
  }

  const options = (unidadesQuery.data ?? []).map((u) => ({
    value: u.id,
    label: `${u.nome} — ${tipoUnidadeSigla(u.tipo)}`,
  }));

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Consolidar Censo SUAS"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-consolidar-censo"
            loading={mutation.isPending}
            disabled={options.length === 0}
          >
            Consolidar
          </Button>
        </>
      }
    >
      <form id="form-consolidar-censo" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          A consolidação do exercício <strong>{exercicio}</strong> deriva o volume anual de
          atendimentos do RMA e as famílias referenciadas do cadastro — sem dupla digitação. É
          idempotente enquanto o formulário não estiver fechado.
        </Alert>

        {options.length === 0 ? (
          <Alert variant="warning">
            Nenhuma unidade cadastrada. Cadastre uma unidade antes de consolidar o Censo.
          </Alert>
        ) : (
          <FormField label="Unidade" required error={erro}>
            {({ id, describedBy, invalid }) => (
              <Select
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                options={options}
                placeholder="Selecione a unidade"
                value={unidadeId}
                onChange={(e) => setUnidadeId(e.target.value)}
              />
            )}
          </FormField>
        )}
      </form>
    </Modal>
  );
}

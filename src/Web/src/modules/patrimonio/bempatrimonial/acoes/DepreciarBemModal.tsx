// DepreciarBem — POST /bens/{id}/depreciacao
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useDepreciarBem } from '../bempatrimonial.api';
import type { DepreciarBemInput } from '../bempatrimonial.api';
import { competenciaAtual } from '../bemPatrimonial.helpers';
import type { AcaoModalProps } from './acoesModais.shared';

export function DepreciarBemModal({ bemId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useDepreciarBem(bemId);
  const inicial = competenciaAtual();
  const [ano, setAno] = useState(String(inicial.ano));
  const [mes, setMes] = useState(String(inicial.mes));
  const [errors, setErrors] = useState<{ ano?: string; mes?: string }>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { ano?: string; mes?: string } = {};
    const anoN = Number(ano);
    const mesN = Number(mes);
    if (!Number.isInteger(anoN) || anoN < 2000 || anoN > 2100) next.ano = 'Ano de competência inválido.';
    if (!Number.isInteger(mesN) || mesN < 1 || mesN > 12) next.mes = 'Mês de competência deve estar entre 1 e 12.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: DepreciarBemInput = { anoCompetencia: anoN, mesCompetencia: mesN };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Depreciação reconhecida (competência ${String(mesN).padStart(2, '0')}/${anoN}).`, 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível depreciar o bem.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Reconhecer depreciação"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-depreciar" loading={mutation.isPending}>
            Depreciar
          </Button>
        </>
      }
    >
      <form id="form-depreciar" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-70">
          Depreciação linear da competência. O valor contábil nunca fica abaixo do residual; em imóveis,
          deprecia apenas a benfeitoria.
        </p>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Ano de competência" required error={errors.ano}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="2000"
                  max="2100"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={ano}
                  onChange={(e) => setAno(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Mês de competência" required error={errors.mes}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="1"
                  max="12"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

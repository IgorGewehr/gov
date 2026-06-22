// PrestarGarantia
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import {
  MODALIDADE_GARANTIA_NUMERICA,
  MODALIDADE_GARANTIA_ROTULO,
  usePrestarGarantia,
} from '../contrato.api';
import type { ModalidadeGarantia } from '../contrato.api';
import { mapearFieldErrors } from './acaoModals.shared';
import type { AcaoModalProps } from './acaoModals.shared';

interface GarantiaErrors {
  modalidade?: string;
  percentual?: string;
  valor?: string;
  validadeFim?: string;
}

export function PrestarGarantiaModal({ contratoId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = usePrestarGarantia(contratoId);

  const [modalidade, setModalidade] = useState<ModalidadeGarantia>('SeguroGarantia');
  const [percentual, setPercentual] = useState('');
  const [valor, setValor] = useState('');
  const [validadeFim, setValidadeFim] = useState('');
  const [ehGrandeVulto, setEhGrandeVulto] = useState(false);
  const [errors, setErrors] = useState<GarantiaErrors>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function validar(): GarantiaErrors {
    const next: GarantiaErrors = {};
    const pct = Number(percentual);
    if (percentual.trim() === '' || Number.isNaN(pct) || pct < 0 || pct > 10)
      next.percentual = 'Percentual deve estar entre 0 e 10 (5% comum / 10% grande vulto).';
    const val = Number(valor);
    if (valor.trim() === '' || Number.isNaN(val) || val <= 0)
      next.valor = 'Informe um valor maior que zero.';
    if (validadeFim.trim() === '') next.validadeFim = 'Informe a data-fim de validade.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    mutation.mutate(
      {
        modalidade: MODALIDADE_GARANTIA_NUMERICA[modalidade],
        percentual: Number(percentual),
        valor: Number(valor),
        validadeFim,
        ehGrandeVulto,
      },
      {
        onSuccess: () => {
          toast.success('Garantia de execução registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(
            mapearFieldErrors(error, {
              modalidade: true,
              percentual: true,
              valor: true,
              validadeFim: true,
            }) as GarantiaErrors,
          );
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a garantia.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Prestar garantia"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-garantia" loading={mutation.isPending}>
            Registrar garantia
          </Button>
        </>
      }
    >
      <form id="form-garantia" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-60">
          Garantia de execução limitada a 5% do valor (art. 96); até 10% em obras de grande vulto (art. 98).
        </p>

        <FormField label="Modalidade" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={modalidade}
              onChange={(e) => setModalidade(e.target.value as ModalidadeGarantia)}
              options={(Object.keys(MODALIDADE_GARANTIA_ROTULO) as ModalidadeGarantia[]).map((m) => ({
                value: m,
                label: MODALIDADE_GARANTIA_ROTULO[m],
              }))}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-4">
            <FormField label="Percentual (%)" required error={errors.percentual}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  max="10"
                  step="0.01"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={percentual}
                  onChange={(e) => setPercentual(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Valor (R$)" required error={errors.valor}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={valor}
                  onChange={(e) => setValor(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Validade (fim)" required error={errors.validadeFim}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={validadeFim}
                  onChange={(e) => setValidadeFim(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="br-checkbox">
          <input
            id="garantia-grande-vulto"
            type="checkbox"
            checked={ehGrandeVulto}
            onChange={(e) => setEhGrandeVulto(e.target.checked)}
          />
          <label htmlFor="garantia-grande-vulto">Obra de grande vulto (limite ampliado a 10%)</label>
        </div>
      </form>
    </Modal>
  );
}

// CederBem — POST /bens/{id}/cessao
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useCederBem } from '../bempatrimonial.api';
import type { CederBemInput } from '../bempatrimonial.api';
import { guidInvalido } from '../bemPatrimonial.helpers';
import { hoje, mapearFieldErrors } from './acoesModais.shared';
import type { AcaoModalProps } from './acoesModais.shared';

export function CederBemModal({ bemId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useCederBem(bemId);
  const [terceiroId, setTerceiroId] = useState('');
  const [gratuito, setGratuito] = useState('true');
  const [dataInicio, setDataInicio] = useState(hoje());
  const [dataFim, setDataFim] = useState('');
  const [errors, setErrors] = useState<{ terceiroId?: string; dataInicio?: string; dataFim?: string }>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    if (guidInvalido(terceiroId)) next.terceiroId = 'Informe um identificador de terceiro válido.';
    if (dataInicio.trim() === '') next.dataInicio = 'Informe a data de início.';
    if (dataFim.trim() !== '' && dataFim < dataInicio) next.dataFim = 'A data de fim não pode anteceder o início.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: CederBemInput = {
      terceiroId: terceiroId.trim(),
      gratuito: gratuito === 'true',
      dataInicio,
      dataFim: dataFim.trim() === '' ? null : dataFim,
    };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Bem cedido (permanece no acervo).', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        const mapped = mapearFieldErrors<typeof errors & Record<string, string>>(error, {
          terceiroId: true,
          dataInicio: true,
          dataFim: true,
        });
        if (mapped) setErrors((prev) => ({ ...prev, ...mapped }));
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível ceder o bem.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Ceder bem (cessão/comodato)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-ceder" loading={mutation.isPending}>
            Ceder
          </Button>
        </>
      }
    >
      <form id="form-ceder" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-70">
          Uso por terceiro. A título gratuito caracteriza comodato. O bem permanece no acervo, sem baixa contábil.
          Permitido apenas a partir da situação <strong>Tombado</strong>.
        </p>
        <FormField label="Terceiro (identificador)" required error={errors.terceiroId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={terceiroId}
              onChange={(e) => setTerceiroId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField label="Modalidade">
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={[
                { value: 'true', label: 'Gratuita (comodato)' },
                { value: 'false', label: 'Onerosa (cessão)' },
              ]}
              value={gratuito}
              onChange={(e) => setGratuito(e.target.value)}
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Data de início" required error={errors.dataInicio}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataInicio}
                  onChange={(e) => setDataInicio(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Data de fim" error={errors.dataFim} help="Opcional (cessão por prazo indeterminado).">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataFim}
                  onChange={(e) => setDataFim(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

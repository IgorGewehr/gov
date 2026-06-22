// CelebrarAditivo
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import {
  TIPO_ADITIVO_NUMERICO,
  TIPO_ADITIVO_ROTULO,
  useCelebrarAditivo,
} from '../contrato.api';
import type { TipoAditivo } from '../contrato.api';
import { mapearFieldErrors } from './acaoModals.shared';
import type { AcaoModalProps } from './acaoModals.shared';

interface AditivoErrors {
  tipo?: string;
  percentual?: string;
  valorDelta?: string;
  novaVigenciaFim?: string;
  justificativa?: string;
}

export function CelebrarAditivoModal({ contratoId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useCelebrarAditivo(contratoId);

  const [tipo, setTipo] = useState<TipoAditivo>('Acrescimo');
  const [percentual, setPercentual] = useState('');
  const [valorDelta, setValorDelta] = useState('');
  const [novaVigenciaFim, setNovaVigenciaFim] = useState('');
  const [justificativa, setJustificativa] = useState('');
  const [ehReforma, setEhReforma] = useState(false);
  const [errors, setErrors] = useState<AditivoErrors>({});

  const ehPrazo = tipo === 'Prazo';

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function validar(): AditivoErrors {
    const next: AditivoErrors = {};
    const pct = Number(percentual);
    if (percentual.trim() === '' || Number.isNaN(pct) || pct < 0 || pct > 50)
      next.percentual = 'Percentual deve estar entre 0 e 50 (limite legal).';
    if (justificativa.trim() === '') next.justificativa = 'Justificativa do aditivo é obrigatória.';
    const delta = Number(valorDelta);
    if (valorDelta.trim() !== '' && Number.isNaN(delta))
      next.valorDelta = 'Informe um valor de variação válido.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    mutation.mutate(
      {
        tipo: TIPO_ADITIVO_NUMERICO[tipo],
        percentual: Number(percentual),
        valorDelta: valorDelta.trim() === '' ? 0 : Number(valorDelta),
        novaVigenciaFim: ehPrazo && novaVigenciaFim.trim() !== '' ? novaVigenciaFim : null,
        justificativa: justificativa.trim(),
        ehReforma,
      },
      {
        onSuccess: () => {
          toast.success('Aditivo celebrado. Publicação no PNCP é condição de sua eficácia.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(
            mapearFieldErrors(error, {
              tipo: true,
              percentual: true,
              valorDelta: true,
              novaVigenciaFim: true,
              justificativa: true,
            }) as AditivoErrors,
          );
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível celebrar o aditivo.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Celebrar aditivo"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-celebrar-aditivo" loading={mutation.isPending}>
            Celebrar aditivo
          </Button>
        </>
      }
    >
      <form id="form-celebrar-aditivo" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-60">
          Acréscimos/supressões somados são limitados a 25% do valor original (art. 125); reformas admitem até 50%.
        </p>

        <FormField label="Tipo de aditivo" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoAditivo)}
              options={(Object.keys(TIPO_ADITIVO_ROTULO) as TipoAditivo[]).map((t) => ({
                value: t,
                label: TIPO_ADITIVO_ROTULO[t],
              }))}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Percentual sobre o valor original (%)" required error={errors.percentual}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  max="50"
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
          <div className="col-sm-6">
            <FormField label="Variação de valor (R$)" error={errors.valorDelta} help="Opcional.">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  step="0.01"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={valorDelta}
                  onChange={(e) => setValorDelta(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        {ehPrazo && (
          <FormField label="Nova data-fim de vigência" error={errors.novaVigenciaFim}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="date"
                aria-describedby={describedBy}
                invalid={invalid}
                value={novaVigenciaFim}
                onChange={(e) => setNovaVigenciaFim(e.target.value)}
              />
            )}
          </FormField>
        )}

        <FormField label="Justificativa" required error={errors.justificativa}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={justificativa}
              onChange={(e) => setJustificativa(e.target.value)}
            />
          )}
        </FormField>

        <div className="br-checkbox">
          <input
            id="aditivo-reforma"
            type="checkbox"
            checked={ehReforma}
            onChange={(e) => setEhReforma(e.target.checked)}
          />
          <label htmlFor="aditivo-reforma">Reforma de edifício/equipamento (limite ampliado a 50%)</label>
        </div>
      </form>
    </Modal>
  );
}

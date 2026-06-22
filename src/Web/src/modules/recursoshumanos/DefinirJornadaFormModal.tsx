// Formulario de DEFINICAO/substituicao de jornada de um servidor (Portaria 671/2021) em
// Modal. Padrao-ouro: mutation + validacao por campo + Toast + mapeamento de
// ProblemDetails.errors. A jornada vigente anterior, se houver, e desativada pelo backend.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useDefinirJornada } from './api';
import type { DefinirJornadaInput } from './api';
import { REGIMES_JORNADA } from './recursosHumanos.helpers';

export interface DefinirJornadaFormModalProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
  servidorNome: string;
}

interface FormErrors {
  cargaDiariaMinutos?: string;
  intervaloMinutos?: string;
  toleranciaMinutos?: string;
  regime?: string;
  vigenciaInicio?: string;
}

const HOJE = new Date().toISOString().slice(0, 10);

export function DefinirJornadaFormModal({
  open,
  onClose,
  servidorId,
  servidorNome,
}: DefinirJornadaFormModalProps) {
  const toast = useToast();
  const mutation = useDefinirJornada();

  const [carga, setCarga] = useState('480');
  const [intervalo, setIntervalo] = useState('60');
  const [tolerancia, setTolerancia] = useState('10');
  const [regime, setRegime] = useState('1');
  const [vigencia, setVigencia] = useState(HOJE);
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    const cargaNum = Number(carga);
    if (!Number.isInteger(cargaNum) || cargaNum < 1 || cargaNum > 1440)
      next.cargaDiariaMinutos = 'Informe a carga diária em minutos (1 a 1440).';
    const intervaloNum = Number(intervalo);
    if (!Number.isInteger(intervaloNum) || intervaloNum < 0 || intervaloNum > 1440)
      next.intervaloMinutos = 'Informe o intervalo em minutos (0 a 1440).';
    const tolNum = Number(tolerancia);
    if (!Number.isInteger(tolNum) || tolNum < 0 || tolNum > 1440)
      next.toleranciaMinutos = 'Informe a tolerância em minutos (0 a 1440).';
    if (!vigencia) next.vigenciaInicio = 'Informe a data de início da vigência.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: DefinirJornadaInput = {
      servidorId,
      cargaDiariaMinutos: Number(carga),
      intervaloMinutos: Number(intervalo),
      toleranciaMinutos: Number(tolerancia),
      regime: Number(regime),
      vigenciaInicio: vigencia,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Jornada definida para o servidor.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const CAMPOS: ReadonlyArray<keyof FormErrors> = [
            'cargaDiariaMinutos',
            'intervaloMinutos',
            'toleranciaMinutos',
            'regime',
            'vigenciaInicio',
          ];
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (CAMPOS.includes(key)) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível definir a jornada.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={`Definir jornada — ${servidorNome}`}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-definir-jornada"
            loading={mutation.isPending}
          >
            Definir jornada
          </Button>
        </>
      }
    >
      <form id="form-definir-jornada" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Carga diária (minutos)" required error={errors.cargaDiariaMinutos}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1"
              max="1440"
              step="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={carga}
              onChange={(e) => setCarga(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Intervalo intrajornada (minutos)" required error={errors.intervaloMinutos}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              max="1440"
              step="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={intervalo}
              onChange={(e) => setIntervalo(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Tolerância diária (minutos)" required error={errors.toleranciaMinutos}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              max="1440"
              step="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={tolerancia}
              onChange={(e) => setTolerancia(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Regime" required error={errors.regime}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={regime}
              onChange={(e) => setRegime(e.target.value)}
              options={REGIMES_JORNADA}
            />
          )}
        </FormField>

        <FormField label="Início da vigência" required error={errors.vigenciaInicio}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={vigencia}
              onChange={(e) => setVigencia(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

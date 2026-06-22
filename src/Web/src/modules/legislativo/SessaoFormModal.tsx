// Formulario de agendamento de Sessao em Modal. Mutation + validacao por campo +
// Toast de feedback. O TotalMembros e parametrizado (CF art. 29-A), nunca hardcoded.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { TIPOS_SESSAO, useAgendarSessao } from './api';
import type { AgendarSessaoInput } from './api';

export interface SessaoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  tipo?: string;
  dataHora?: string;
  totalMembros?: string;
}

const CAMPOS: ReadonlyArray<keyof FormErrors> = ['tipo', 'dataHora', 'totalMembros'];

export function SessaoFormModal({ open, onClose }: SessaoFormModalProps) {
  const toast = useToast();
  const mutation = useAgendarSessao();

  const [tipo, setTipo] = useState<string>(String(TIPOS_SESSAO[0].value));
  const [dataHora, setDataHora] = useState('');
  const [totalMembros, setTotalMembros] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (dataHora.trim() === '') next.dataHora = 'Informe a data e a hora da sessão.';
    const total = Number(totalMembros);
    if (totalMembros.trim() === '' || !Number.isInteger(total) || total <= 0)
      next.totalMembros = 'Informe o total de membros (maior que zero).';
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

    const input: AgendarSessaoInput = {
      tipo: Number(tipo),
      // <input type="datetime-local"> => ISO local; o backend recebe DateTimeOffset.
      dataHora: new Date(dataHora).toISOString(),
      totalMembros: Number(totalMembros),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Sessão agendada.', 'Sucesso');
        setDataHora('');
        setTotalMembros('');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (CAMPOS.includes(key)) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível agendar a sessão.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Agendar sessão"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-agendar-sessao" loading={mutation.isPending}>
            Agendar
          </Button>
        </>
      }
    >
      <form id="form-agendar-sessao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo de sessão" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              options={TIPOS_SESSAO.map((t) => ({ value: String(t.value), label: t.label }))}
            />
          )}
        </FormField>

        <FormField label="Data e hora" required error={errors.dataHora}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="datetime-local"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataHora}
              onChange={(e) => setDataHora(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Total de membros"
          required
          error={errors.totalMembros}
          help="Número de vereadores da Câmara (base do quórum de instalação)."
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
              value={totalMembros}
              onChange={(e) => setTotalMembros(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

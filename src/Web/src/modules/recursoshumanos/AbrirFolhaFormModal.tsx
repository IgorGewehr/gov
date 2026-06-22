// Formulário de ABERTURA de folha em Modal. Padrão-ouro: mutation + validação +
// Toast + mapeamento de ProblemDetails.errors. Pré-preenche a competência consultada.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  FormField,
  Input,
  Modal,
  Select,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAbrirFolha } from './api';
import type { AbrirFolhaInput } from './api';
import { MESES } from './recursosHumanos.helpers';

export interface AbrirFolhaFormModalProps {
  open: boolean;
  onClose: () => void;
  anoInicial: number;
  mesInicial: number;
  /** Notifica a competência aberta para a página atualizar a consulta. */
  onAberta?: (competencia: { ano: number; mes: number }) => void;
}

interface FormErrors {
  ano?: string;
  mes?: string;
}

export function AbrirFolhaFormModal({
  open,
  onClose,
  anoInicial,
  mesInicial,
  onAberta,
}: AbrirFolhaFormModalProps) {
  const toast = useToast();
  const mutation = useAbrirFolha();

  const [ano, setAno] = useState(String(anoInicial));
  const [mes, setMes] = useState(String(mesInicial));
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    const anoNum = Number(ano);
    if (!Number.isInteger(anoNum) || anoNum < 2000 || anoNum > 2100)
      next.ano = 'Informe um ano entre 2000 e 2100.';
    const mesNum = Number(mes);
    if (!Number.isInteger(mesNum) || mesNum < 1 || mesNum > 12)
      next.mes = 'Selecione um mês válido.';
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

    const input: AbrirFolhaInput = { ano: Number(ano), mes: Number(mes) };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Folha aberta para ${String(input.mes).padStart(2, '0')}/${input.ano}.`, 'Sucesso');
        onAberta?.(input);
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key === 'ano' || key === 'mes') mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a folha.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir folha de pagamento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-abrir-folha"
            loading={mutation.isPending}
          >
            Abrir
          </Button>
        </>
      }
    >
      <form id="form-abrir-folha" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Mês" required error={errors.mes}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={mes}
              onChange={(e) => setMes(e.target.value)}
              options={MESES}
            />
          )}
        </FormField>

        <FormField label="Ano" required error={errors.ano}>
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
      </form>
    </Modal>
  );
}

// Formulário de LANÇAMENTO de evento (provento/desconto) numa folha aberta, em Modal.
// Padrão-ouro: mutation (escopada ao folhaId) + validação + Toast + mapeamento de erros.
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
import { useAdicionarEvento, useServidoresAtivos } from './api';
import type { AdicionarEventoInput } from './api';
import { TIPOS_EVENTO } from './recursosHumanos.helpers';

export interface AdicionarEventoFormModalProps {
  open: boolean;
  onClose: () => void;
  folhaId: string;
}

interface FormErrors {
  servidorId?: string;
  rubrica?: string;
  tipo?: string;
  baseCalculo?: string;
  valor?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  servidorId: true,
  rubrica: true,
  tipo: true,
  baseCalculo: true,
  valor: true,
};

export function AdicionarEventoFormModal({ open, onClose, folhaId }: AdicionarEventoFormModalProps) {
  const toast = useToast();
  const mutation = useAdicionarEvento(folhaId);
  const servidoresQuery = useServidoresAtivos();

  const [servidorId, setServidorId] = useState('');
  const [rubrica, setRubrica] = useState('');
  const [tipo, setTipo] = useState('');
  const [baseCalculo, setBaseCalculo] = useState('');
  const [valor, setValor] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  const servidorOptions = (servidoresQuery.data ?? []).map((s) => ({
    value: s.id,
    label: `${s.matricula} — ${s.nomeServidor}`,
  }));

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (servidorId.trim() === '') next.servidorId = 'Selecione o servidor.';
    if (rubrica.trim() === '') next.rubrica = 'Informe o código da rubrica.';
    if (tipo.trim() === '') next.tipo = 'Selecione o tipo do evento.';
    const base = Number(baseCalculo);
    if (baseCalculo.trim() === '' || Number.isNaN(base) || base < 0)
      next.baseCalculo = 'Informe uma base de cálculo não negativa.';
    const val = Number(valor);
    if (valor.trim() === '' || Number.isNaN(val) || val <= 0)
      next.valor = 'Informe um valor maior que zero.';
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

    const input: AdicionarEventoInput = {
      servidorId,
      rubrica: rubrica.trim(),
      tipo: Number(tipo),
      baseCalculo: Number(baseCalculo),
      valor: Number(valor),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Evento ${input.rubrica} lançado.`, 'Sucesso');
        setRubrica('');
        setBaseCalculo('');
        setValor('');
        setErrors({});
        onClose();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível lançar o evento.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Lançar evento na folha"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-adicionar-evento"
            loading={mutation.isPending}
          >
            Lançar
          </Button>
        </>
      }
    >
      <form id="form-adicionar-evento" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Servidor"
          required
          error={errors.servidorId}
          help={servidoresQuery.isLoading ? 'Carregando servidores…' : undefined}
        >
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={servidorId}
              onChange={(e) => setServidorId(e.target.value)}
              placeholder="Selecione o servidor"
              options={servidorOptions}
              disabled={servidoresQuery.isLoading}
            />
          )}
        </FormField>

        <FormField label="Rubrica (S-1010)" required error={errors.rubrica}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={rubrica}
              onChange={(e) => setRubrica(e.target.value)}
              maxLength={30}
            />
          )}
        </FormField>

        <FormField label="Tipo do evento" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              placeholder="Provento ou Desconto"
              options={TIPOS_EVENTO}
            />
          )}
        </FormField>

        <FormField label="Base de cálculo (R$)" required error={errors.baseCalculo}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={baseCalculo}
              onChange={(e) => setBaseCalculo(e.target.value)}
            />
          )}
        </FormField>

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
      </form>
    </Modal>
  );
}

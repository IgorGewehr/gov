// [Command AjustarValorRealizavelLiquido §5.4] Ajusta o VRL e aplica o menor entre custo e VRL
// (I-2; NBC TSP 12). Validação: VRL >= 0; data obrigatória. Alerta quando VRL < custo médio
// (haverá redução ao valor realizável).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { formatarMoeda } from '../../../i18n/format';
import { useAjustarVrl } from './itemestoque.api';
import type { AjustarVrlInput } from './itemestoque.api';
import { hojeIso, mapearFieldErrors } from './itemEstoque.helpers';

export interface ItemEstoqueVrlModalProps {
  open: boolean;
  onClose: () => void;
  itemId: string;
  /** Custo médio atual, para alerta de redução ao valor realizável. */
  custoMedio: number;
}

const CAMPOS = ['valorRealizavelLiquido', 'data'] as const;
type Campo = (typeof CAMPOS)[number];
type FormErrors = Partial<Record<Campo, string>>;

export function ItemEstoqueVrlModal({ open, onClose, itemId, custoMedio }: ItemEstoqueVrlModalProps) {
  const toast = useToast();
  const mutation = useAjustarVrl(itemId);

  const [valorRealizavelLiquido, setValorRealizavelLiquido] = useState('');
  const [data, setData] = useState(hojeIso());
  const [errors, setErrors] = useState<FormErrors>({});

  const vrlNum = Number(valorRealizavelLiquido);
  const haveraReducao =
    valorRealizavelLiquido.trim() !== '' && !Number.isNaN(vrlNum) && vrlNum >= 0 && vrlNum < custoMedio;

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (valorRealizavelLiquido.trim() === '' || Number.isNaN(vrlNum) || vrlNum < 0)
      next.valorRealizavelLiquido = 'O valor realizável líquido não pode ser negativo.';
    if (data.trim() === '') next.data = 'Informe a data do ajuste.';
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

    const input: AjustarVrlInput = { valorRealizavelLiquido: vrlNum, data };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Valor realizável líquido ajustado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          setErrors(mapearFieldErrors(error.fieldErrors, CAMPOS));
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível ajustar o VRL.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Ajustar valor realizável líquido (VRL)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-vrl-item" loading={mutation.isPending}>
            Ajustar VRL
          </Button>
        </>
      }
    >
      <form id="form-vrl-item" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">
          Custo médio atual: <strong>{formatarMoeda(custoMedio)}</strong>.
        </p>

        {haveraReducao && (
          <Alert variant="warning" title="Redução ao valor realizável">
            O VRL informado é inferior ao custo médio; o item passará a ser mensurado pelo VRL (I-2; NBC TSP 12).
          </Alert>
        )}

        <FormField
          label="Valor realizável líquido (R$)"
          required
          error={errors.valorRealizavelLiquido}
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
              value={valorRealizavelLiquido}
              onChange={(e) => setValorRealizavelLiquido(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Data do ajuste" required error={errors.data}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={data}
              onChange={(e) => setData(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

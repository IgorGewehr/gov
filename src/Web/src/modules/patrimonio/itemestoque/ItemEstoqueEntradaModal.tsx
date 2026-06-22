// [Command RegistrarEntrada §5.2] Registra entrada de itens no almoxarifado: cria/atualiza
// lote (PEPS), recalcula o custo médio e incrementa o saldo (I-5). Não reconhece despesa (I-4).
// Validações: Quantidade > 0 (I-11), CustoUnitario >= 0, documento e data obrigatórios.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useRegistrarEntrada } from './itemestoque.api';
import type { RegistrarEntradaInput } from './itemestoque.api';
import { hojeIso, mapearFieldErrors } from './itemEstoque.helpers';

export interface ItemEstoqueEntradaModalProps {
  open: boolean;
  onClose: () => void;
  itemId: string;
  /** Código do item, exibido no título para contexto. */
  codigoItem?: string;
}

const CAMPOS = ['quantidade', 'custoUnitario', 'dataEntrada', 'validade', 'documento'] as const;
type Campo = (typeof CAMPOS)[number];
type FormErrors = Partial<Record<Campo, string>>;

export function ItemEstoqueEntradaModal({ open, onClose, itemId, codigoItem }: ItemEstoqueEntradaModalProps) {
  const toast = useToast();
  const mutation = useRegistrarEntrada(itemId);

  const [quantidade, setQuantidade] = useState('');
  const [custoUnitario, setCustoUnitario] = useState('');
  const [dataEntrada, setDataEntrada] = useState(hojeIso());
  const [validade, setValidade] = useState('');
  const [documento, setDocumento] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    const qtd = Number(quantidade);
    if (quantidade.trim() === '' || Number.isNaN(qtd) || qtd <= 0)
      next.quantidade = 'Quantidade deve ser maior que zero.';
    const custo = Number(custoUnitario);
    if (custoUnitario.trim() === '' || Number.isNaN(custo) || custo < 0)
      next.custoUnitario = 'Custo unitário não pode ser negativo.';
    if (dataEntrada.trim() === '') next.dataEntrada = 'Informe a data de entrada.';
    if (documento.trim() === '') next.documento = 'Informe o documento de respaldo (NF/recebimento).';
    if (validade.trim() !== '' && validade < dataEntrada)
      next.validade = 'A validade não pode ser anterior à entrada.';
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

    const input: RegistrarEntradaInput = {
      quantidade: Number(quantidade),
      custoUnitario: Number(custoUnitario),
      dataEntrada,
      validade: validade.trim() || null,
      documento: documento.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Entrada registrada e saldo atualizado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          setErrors(mapearFieldErrors(error.fieldErrors, CAMPOS));
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a entrada.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={`Registrar entrada${codigoItem ? ` — ${codigoItem}` : ''}`}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-entrada-item" loading={mutation.isPending}>
            Registrar entrada
          </Button>
        </>
      }
    >
      <form id="form-entrada-item" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Quantidade" required error={errors.quantidade}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={quantidade}
              onChange={(e) => setQuantidade(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Custo unitário (R$)"
          required
          error={errors.custoUnitario}
          help="Use 0 para doações (B-4)."
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
              value={custoUnitario}
              onChange={(e) => setCustoUnitario(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Data de entrada" required error={errors.dataEntrada}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataEntrada}
              onChange={(e) => setDataEntrada(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Validade do lote" error={errors.validade} help="Opcional.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={validade}
              onChange={(e) => setValidade(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Documento" required error={errors.documento} help="NF, termo de recebimento ou doação.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={documento}
              onChange={(e) => setDocumento(e.target.value)}
              placeholder="NF 12345"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
